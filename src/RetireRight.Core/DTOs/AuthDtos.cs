namespace RetireRight.Core.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    AuthenticatedUserDto User);

public record AuthenticatedUserDto(
    int Id,
    string Email,
    bool IsAdmin,
    bool EmailVerified,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record SignupRequest(string Email, string Password);

public record VerifyEmailRequest(string Token);

public record ResendVerificationRequest(string Email, string Password);
