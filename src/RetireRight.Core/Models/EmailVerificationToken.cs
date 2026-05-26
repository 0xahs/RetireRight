namespace RetireRight.Core.Models;

public class EmailVerificationToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
