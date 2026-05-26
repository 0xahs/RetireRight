namespace RetireRight.Core.Models;

public class PortfolioHolding
{
    public int Id { get; set; }
    // Nullable in the schema during multi-user backfill; set NOT NULL once the startup
    // bootstrap assigns the existing rows to the first admin user.
    public int? UserId { get; set; }
    public int StockId { get; set; }
    public int Shares { get; set; }
    public decimal AvgCost { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string? Platform { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public User? User { get; set; }
}

public class PortfolioTransaction
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int StockId { get; set; }
    public required string Action { get; set; }
    public int Shares { get; set; }
    public decimal Price { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? Commission { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? Platform { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public User? User { get; set; }
}
