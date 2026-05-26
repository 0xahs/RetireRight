using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RetireRight.Core.Configuration;
using RetireRight.Core.Models;

namespace RetireRight.Api.Services;

public class JwtTokenService
{
    private readonly AuthOptions _opts;

    public JwtTokenService(IOptions<RetireRightOptions> rrOptions)
    {
        _opts = rrOptions.Value.Auth;
        if (string.IsNullOrWhiteSpace(_opts.JwtSigningKey) || _opts.JwtSigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "RetireRight:Auth:JwtSigningKey must be set and at least 32 characters. " +
                "Provide it via appsettings.local.json or environment variable RETIRERIGHT_RetireRight__Auth__JwtSigningKey.");
        }
    }

    public (string Token, DateTime ExpiresAt) Issue(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.JwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(_opts.TokenLifetimeDays);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("email_verified", user.EmailVerifiedAt.HasValue ? "true" : "false"),
        };
        if (user.IsAdmin) claims.Add(new Claim("is_admin", "true"));

        var jwt = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expires);
    }
}
