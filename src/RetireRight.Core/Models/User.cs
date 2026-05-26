namespace RetireRight.Core.Models;

public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
}
