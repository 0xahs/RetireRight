namespace RetireRight.Core.Models;

public class UserWatchlist
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int StockId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Stock? Stock { get; set; }
}
