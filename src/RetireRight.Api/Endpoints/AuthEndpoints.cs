using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using RetireRight.Api.Services;
using RetireRight.Core.Configuration;
using RetireRight.Core.DTOs;
using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;

namespace RetireRight.Api.Endpoints;

public static class AuthEndpoints
{
    // Pre-computed dummy bcrypt hash so a Verify() against an unknown email takes
    // roughly the same wall-clock as Verify() against a real user — mitigates
    // user-enumeration via timing. Generated once per process lifetime.
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword("not-a-real-password", workFactor: 12);

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting("login");

        group.MapPost("/signup", SignupAsync)
            .WithName("Signup")
            .AllowAnonymous()
            .RequireRateLimiting("signup");

        group.MapPost("/verify-email", VerifyEmailAsync)
            .WithName("VerifyEmail")
            .AllowAnonymous();

        // Anonymous + credential-gated: caller proves they own the inbox by providing
        // a valid password. Without this gate, anyone with an email could spam another
        // user's inbox with verification mail.
        group.MapPost("/resend-verification", ResendVerificationAsync)
            .WithName("ResendVerification")
            .AllowAnonymous()
            .RequireRateLimiting("login");

        group.MapPost("/change-password", ChangePasswordAsync)
            .WithName("ChangePassword")
            .RequireAuthorization();

        group.MapGet("/me", GetMeAsync)
            .WithName("GetMe")
            .RequireAuthorization();
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest req,
        IUserRepository users,
        JwtTokenService tokens,
        IOptions<RetireRightOptions> rrOptions,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        var auth = rrOptions.Value.Auth;

        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Results.Json(new { error = "Email and password are required." }, statusCode: 400);

        var user = await users.GetByEmailAsync(req.Email, ct);
        if (user is null)
        {
            BCrypt.Net.BCrypt.Verify(req.Password, DummyHash);
            logger.LogWarning("Login failed: unknown email {Email}", req.Email);
            return Results.Json(new { error = "Invalid credentials." }, statusCode: 401);
        }

        if (user.LockedUntil is { } locked && locked > DateTime.UtcNow)
        {
            logger.LogWarning("Login blocked: user {UserId} locked until {LockedUntil}", user.Id, locked);
            return Results.Json(new { error = "Account temporarily locked. Try again later.", lockedUntil = locked }, statusCode: 423);
        }

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= auth.LockoutMaxAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(auth.LockoutDurationMinutes);
                logger.LogWarning("Locked user {UserId} after {Attempts} failed attempts", user.Id, user.FailedLoginCount);
            }
            await users.UpdateAsync(user, ct);
            return Results.Json(new { error = "Invalid credentials." }, statusCode: 401);
        }

        // Block unverified users. Web app shows a "resend" button on this code.
        if (user.EmailVerifiedAt is null)
        {
            logger.LogInformation("Login refused: user {UserId} email not verified", user.Id);
            return Results.Json(new { error = "Email not verified. Check your inbox.", code = "EMAIL_NOT_VERIFIED" }, statusCode: 403);
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await users.UpdateAsync(user, ct);

        var (token, expires) = tokens.Issue(user);
        return Results.Ok(new LoginResponse(
            Token: token,
            ExpiresAt: expires,
            User: new AuthenticatedUserDto(user.Id, user.Email, user.IsAdmin,
                EmailVerified: user.EmailVerifiedAt.HasValue,
                CreatedAt: user.CreatedAt,
                LastLoginAt: user.LastLoginAt)));
    }

    private static async Task<IResult> SignupAsync(
        [FromBody] SignupRequest req,
        IUserRepository users,
        IEmailVerificationTokenRepository tokenRepo,
        IEmailService emailService,
        IOptions<RetireRightOptions> rrOptions,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        var auth = rrOptions.Value.Auth;

        if (!auth.AllowPublicSignup)
            return Results.Json(new { error = "Public sign-up is disabled." }, statusCode: 403);

        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Results.Json(new { error = "Email and password are required." }, statusCode: 400);

        if (!IsValidEmail(req.Email))
            return Results.Json(new { error = "Invalid email address." }, statusCode: 400);

        if (req.Password.Length < 8)
            return Results.Json(new { error = "Password must be at least 8 characters." }, statusCode: 400);

        var normalizedEmail = req.Email.Trim().ToLowerInvariant();

        // Idempotent for unknown emails; if it exists we return 200 with a generic
        // message so we don't leak which emails are registered.
        var existing = await users.GetByEmailAsync(normalizedEmail, ct);
        if (existing is not null)
        {
            logger.LogInformation("Signup attempt for existing email {Email} — returning generic success", normalizedEmail);
            return Results.Ok(new { ok = true, message = "If that email isn't already registered, a verification link has been sent." });
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12);
        var user = await users.CreateAsync(new User
        {
            Email = normalizedEmail,
            PasswordHash = hash,
        }, ct);

        await SendVerificationEmailAsync(user, tokenRepo, emailService, auth, logger, ct);

        return Results.Ok(new { ok = true, message = "If that email isn't already registered, a verification link has been sent." });
    }

    private static async Task<IResult> VerifyEmailAsync(
        [FromBody] VerifyEmailRequest req,
        IUserRepository users,
        IEmailVerificationTokenRepository tokenRepo,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");

        if (string.IsNullOrWhiteSpace(req.Token))
            return Results.Json(new { error = "Token is required." }, statusCode: 400);

        var entry = await tokenRepo.GetByTokenAsync(req.Token, ct);
        if (entry is null)
            return Results.Json(new { error = "Token not found." }, statusCode: 404);

        if (entry.UsedAt.HasValue)
            return Results.Json(new { error = "Token already used." }, statusCode: 410);

        if (entry.ExpiresAt < DateTime.UtcNow)
            return Results.Json(new { error = "Token expired. Sign in and request a new one." }, statusCode: 410);

        var user = await users.GetByIdAsync(entry.UserId, ct);
        if (user is null)
            return Results.Json(new { error = "User not found." }, statusCode: 404);

        if (user.EmailVerifiedAt is null)
        {
            user.EmailVerifiedAt = DateTime.UtcNow;
            await users.UpdateAsync(user, ct);
        }
        await tokenRepo.MarkUsedAsync(entry.Id, ct);

        logger.LogInformation("Email verified for user {UserId}", user.Id);
        return Results.Ok(new { ok = true, email = user.Email });
    }

    private static async Task<IResult> ResendVerificationAsync(
        [FromBody] ResendVerificationRequest req,
        IUserRepository users,
        IEmailVerificationTokenRepository tokenRepo,
        IEmailService emailService,
        IOptions<RetireRightOptions> rrOptions,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        var auth = rrOptions.Value.Auth;

        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Results.Json(new { error = "Email and password are required." }, statusCode: 400);

        var user = await users.GetByEmailAsync(req.Email, ct);
        // Generic response either way — don't leak whether the email exists.
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            BCrypt.Net.BCrypt.Verify(req.Password, DummyHash);
            return Results.Ok(new { ok = true, message = "If credentials are valid and email unverified, a new link has been sent." });
        }

        if (user.EmailVerifiedAt is not null)
        {
            return Results.Ok(new { ok = true, message = "Email already verified — you can sign in." });
        }

        await SendVerificationEmailAsync(user, tokenRepo, emailService, auth, logger, ct);
        return Results.Ok(new { ok = true, message = "Verification email sent." });
    }

    private static async Task<IResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest req,
        ClaimsPrincipal principal,
        IUserRepository users,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
            return Results.Json(new { error = "Current and new passwords are required." }, statusCode: 400);

        if (req.NewPassword.Length < 8)
            return Results.Json(new { error = "New password must be at least 8 characters." }, statusCode: 400);

        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userId, out var id))
            return Results.Json(new { error = "Invalid token." }, statusCode: 401);

        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
            return Results.Json(new { error = "User not found." }, statusCode: 404);

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            // 400 (not 401) — a wrong current password is a form-field error, not a
            // bad session token. Returning 401 would trip the web client's auto-signOut
            // interceptor and kick the user out of the change-password screen.
            return Results.Json(new { error = "Current password is incorrect." }, statusCode: 400);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 12);
        await users.UpdateAsync(user, ct);

        return Results.Ok(new { ok = true });
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        IUserRepository users,
        CancellationToken ct)
    {
        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userId, out var id))
            return Results.Json(new { error = "Invalid token." }, statusCode: 401);

        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
            return Results.Json(new { error = "User not found." }, statusCode: 404);

        return Results.Ok(new AuthenticatedUserDto(
            user.Id, user.Email, user.IsAdmin,
            EmailVerified: user.EmailVerifiedAt.HasValue,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt));
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private static async Task SendVerificationEmailAsync(
        User user,
        IEmailVerificationTokenRepository tokenRepo,
        IEmailService emailService,
        AuthOptions auth,
        ILogger logger,
        CancellationToken ct)
    {
        // Invalidate any previously-issued tokens so old links can't be reused.
        await tokenRepo.InvalidateAllForUserAsync(user.Id, ct);

        var ttl = TimeSpan.FromHours(Math.Max(1, auth.EmailVerificationTtlHours));
        var entry = await tokenRepo.CreateAsync(user.Id, ttl, ct);

        var baseUrl = auth.AppBaseUrl.TrimEnd('/');
        var encodedToken = WebUtility.UrlEncode(entry.Token);
        var verifyUrl = $"{baseUrl}/verify-email?token={encodedToken}";

        if (!emailService.Enabled)
        {
            // Dev/local mode without SMTP: log the link so the developer can manually
            // open it. Never relied on in production.
            logger.LogWarning("Email disabled — verification link for {Email}: {Url}", user.Email, verifyUrl);
            return;
        }

        var html = BuildVerificationEmailHtml(user.Email, verifyUrl, ttl);
        try
        {
            await emailService.SendAsync(user.Email, "Verify your RetireRight email", html, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
        }
    }

    private static string BuildVerificationEmailHtml(string email, string verifyUrl, TimeSpan ttl)
    {
        var safeEmail = HtmlEncoder.Default.Encode(email);
        var safeUrl = HtmlEncoder.Default.Encode(verifyUrl);
        return $@"<!DOCTYPE html>
<html><body style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;color:#1a1a1a;max-width:560px;margin:0 auto;padding:32px 24px"">
  <h2 style=""margin-top:0;color:#0a0a0a"">Verify your email</h2>
  <p>Hello,</p>
  <p>You're seeing this because you signed up for <strong>RetireRight</strong> with the address <strong>{safeEmail}</strong>.</p>
  <p>Click the button below to verify your email and unlock sign-in:</p>
  <p style=""margin:32px 0""><a href=""{safeUrl}"" style=""background:#0c7c59;color:#fff;text-decoration:none;padding:12px 22px;border-radius:4px;font-weight:600;display:inline-block"">Verify email</a></p>
  <p style=""color:#666;font-size:13px"">Or paste this link into your browser:<br/><a href=""{safeUrl}"" style=""color:#0c7c59;word-break:break-all"">{safeUrl}</a></p>
  <p style=""color:#666;font-size:13px"">The link expires in {(int)ttl.TotalHours} hours. If you didn't sign up, ignore this email.</p>
</body></html>";
    }

    private static bool IsValidEmail(string email)
    {
        try { return new System.Net.Mail.MailAddress(email).Address == email.Trim(); }
        catch { return false; }
    }
}
