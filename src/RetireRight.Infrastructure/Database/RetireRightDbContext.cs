using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using RetireRight.Core.Models;

namespace RetireRight.Infrastructure.Database;

public class RetireRightDbContext : DbContext
{
    public RetireRightDbContext(DbContextOptions<RetireRightDbContext> options) : base(options) { }

    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<DailyPrice> DailyPrices => Set<DailyPrice>();
    public DbSet<Dividend> Dividends => Set<Dividend>();
    public DbSet<MacroIndicator> MacroIndicators => Set<MacroIndicator>();
    public DbSet<IndexLevel> IndexLevels => Set<IndexLevel>();
    public DbSet<ComputedMetrics> ComputedMetrics => Set<ComputedMetrics>();
    public DbSet<RawApiResponse> RawResponses => Set<RawApiResponse>();
    public DbSet<PortfolioHolding> PortfolioHoldings => Set<PortfolioHolding>();
    public DbSet<PortfolioTransaction> PortfolioTransactions => Set<PortfolioTransaction>();
    public DbSet<ScrapeRun> ScrapeRuns => Set<ScrapeRun>();
    public DbSet<StockFundamental> StockFundamentals => Set<StockFundamental>();
    public DbSet<StockOwnership> StockOwnerships => Set<StockOwnership>();
    public DbSet<InsiderTransaction> InsiderTransactions => Set<InsiderTransaction>();

    // Npgsql's EnableDynamicJson() silently drops keys when deserializing JSONB → Dictionary<string, decimal?>.
    // These explicit converters use Newtonsoft.Json for reliable round-trip serialization.
    private static readonly ValueComparer<Dictionary<string, decimal?>> DictComparer = new(
        (a, b) => JsonConvert.SerializeObject(a) == JsonConvert.SerializeObject(b),
        d => JsonConvert.SerializeObject(d).GetHashCode(),
        d => new Dictionary<string, decimal?>(d));

    private static readonly ValueComparer<List<OwnershipEntry>> OwnersComparer = new(
        (a, b) => JsonConvert.SerializeObject(a) == JsonConvert.SerializeObject(b),
        d => JsonConvert.SerializeObject(d).GetHashCode(),
        d => JsonConvert.DeserializeObject<List<OwnershipEntry>>(JsonConvert.SerializeObject(d)) ?? new List<OwnershipEntry>());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Stock
        modelBuilder.Entity<Stock>(e =>
        {
            e.ToTable("stocks");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.Symbol).HasColumnName("symbol").HasMaxLength(10).IsRequired();
            e.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(s => s.Sector).HasColumnName("sector").HasMaxLength(50).HasConversion<string>();
            e.Property(s => s.Industry).HasColumnName("industry").HasMaxLength(100);
            e.Property(s => s.ListedDate).HasColumnName("listed_date");
            e.Property(s => s.IsActive).HasColumnName("is_active");
            e.Property(s => s.IsWatchlist).HasColumnName("is_watchlist");
            e.Property(s => s.CircuitLimitPct).HasColumnName("circuit_limit_pct");
            e.Property(s => s.LotSize).HasColumnName("lot_size");
            e.Property(s => s.Isin).HasColumnName("isin").HasMaxLength(20);
            e.Property(s => s.TradingBoard).HasColumnName("trading_board").HasMaxLength(20);
            e.Property(s => s.ScrapeRunId).HasColumnName("scrape_run_id");
            e.Property(s => s.CreatedAt).HasColumnName("created_at");
            e.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(s => s.Symbol).IsUnique();
            e.HasIndex(s => s.Sector);
            e.HasOne(s => s.ScrapeRun).WithMany().HasForeignKey(s => s.ScrapeRunId).OnDelete(DeleteBehavior.SetNull);
        });

        // DailyPrice
        modelBuilder.Entity<DailyPrice>(e =>
        {
            e.ToTable("daily_prices");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.StockId).HasColumnName("stock_id");
            e.Property(p => p.Date).HasColumnName("date");
            e.Property(p => p.Open).HasColumnName("open").HasPrecision(12, 2);
            e.Property(p => p.High).HasColumnName("high").HasPrecision(12, 2);
            e.Property(p => p.Low).HasColumnName("low").HasPrecision(12, 2);
            e.Property(p => p.Close).HasColumnName("close").HasPrecision(12, 2);
            e.Property(p => p.Volume).HasColumnName("volume");
            e.Property(p => p.Trades).HasColumnName("trades");
            e.Property(p => p.Value).HasColumnName("value").HasPrecision(18, 2);
            e.Property(p => p.ChangePct).HasColumnName("change_pct").HasPrecision(8, 4);
            e.Property(p => p.Source).HasColumnName("source").HasMaxLength(50);
            e.Property(p => p.ScrapeRunId).HasColumnName("scrape_run_id");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(p => new { p.StockId, p.Date }).IsUnique();
            e.HasOne(p => p.Stock).WithMany().HasForeignKey(p => p.StockId);
            e.HasOne(p => p.ScrapeRun).WithMany().HasForeignKey(p => p.ScrapeRunId).OnDelete(DeleteBehavior.SetNull);
        });

        // Dividend
        modelBuilder.Entity<Dividend>(e =>
        {
            e.ToTable("dividends");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.StockId).HasColumnName("stock_id");
            e.Property(d => d.ExDate).HasColumnName("ex_date");
            e.Property(d => d.RecordDate).HasColumnName("record_date");
            e.Property(d => d.PaymentDate).HasColumnName("payment_date");
            e.Property(d => d.Dps).HasColumnName("dps").HasPrecision(10, 2);
            e.Property(d => d.Percentage).HasColumnName("percentage").HasPrecision(8, 4);
            e.Property(d => d.Year).HasColumnName("year").HasMaxLength(10);
            e.Property(d => d.DividendType).HasColumnName("dividend_type").HasMaxLength(20);
            e.Property(d => d.Frequency).HasColumnName("frequency").HasMaxLength(20);
            e.Property(d => d.Source).HasColumnName("source").HasMaxLength(50);
            e.Property(d => d.ScrapeRunId).HasColumnName("scrape_run_id");
            e.Property(d => d.CreatedAt).HasColumnName("created_at");
            e.Property(d => d.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(d => new { d.StockId, d.ExDate, d.DividendType }).IsUnique();
            e.HasOne(d => d.Stock).WithMany().HasForeignKey(d => d.StockId);
            e.HasOne(d => d.ScrapeRun).WithMany().HasForeignKey(d => d.ScrapeRunId).OnDelete(DeleteBehavior.SetNull);
        });

        // MacroIndicator
        modelBuilder.Entity<MacroIndicator>(e =>
        {
            e.ToTable("macro_indicators");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.IndicatorName).HasColumnName("indicator_name").HasMaxLength(50);
            e.Property(m => m.Date).HasColumnName("date");
            e.Property(m => m.Value).HasColumnName("value").HasPrecision(12, 4);
            e.Property(m => m.Unit).HasColumnName("unit").HasMaxLength(20);
            e.Property(m => m.Source).HasColumnName("source").HasMaxLength(50);
            e.Property(m => m.ScrapeRunId).HasColumnName("scrape_run_id");
            e.Property(m => m.CreatedAt).HasColumnName("created_at");
            e.Property(m => m.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(m => new { m.IndicatorName, m.Date }).IsUnique();
            e.HasOne(m => m.ScrapeRun).WithMany().HasForeignKey(m => m.ScrapeRunId).OnDelete(DeleteBehavior.SetNull);
        });

        // IndexLevel
        modelBuilder.Entity<IndexLevel>(e =>
        {
            e.ToTable("index_levels");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.IndexName).HasColumnName("index_name").HasMaxLength(20);
            e.Property(i => i.Date).HasColumnName("date");
            e.Property(i => i.Open).HasColumnName("open").HasPrecision(12, 2);
            e.Property(i => i.High).HasColumnName("high").HasPrecision(12, 2);
            e.Property(i => i.Low).HasColumnName("low").HasPrecision(12, 2);
            e.Property(i => i.Close).HasColumnName("close").HasPrecision(12, 2);
            e.Property(i => i.Volume).HasColumnName("volume");
            e.Property(i => i.ChangePct).HasColumnName("change_pct").HasPrecision(8, 4);
            e.Property(i => i.Source).HasColumnName("source").HasMaxLength(50);
            e.Property(i => i.ScrapeRunId).HasColumnName("scrape_run_id");
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(i => new { i.IndexName, i.Date }).IsUnique();
            e.HasOne(i => i.ScrapeRun).WithMany().HasForeignKey(i => i.ScrapeRunId).OnDelete(DeleteBehavior.SetNull);
        });

        // ComputedMetrics
        modelBuilder.Entity<ComputedMetrics>(e =>
        {
            e.ToTable("computed_metrics");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.StockId).HasColumnName("stock_id");
            e.Property(c => c.ComputedDate).HasColumnName("computed_date");
            e.Property(c => c.Metrics).HasColumnName("metrics").HasColumnType("jsonb")
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<Dictionary<string, decimal?>>(v) ?? new())
                .Metadata.SetValueComparer(DictComparer);
            e.Property(c => c.CompositeScore).HasColumnName("composite_score").HasPrecision(5, 2);
            e.Property(c => c.DividendSafetyScore).HasColumnName("dividend_safety_score").HasPrecision(5, 2);
            e.Property(c => c.DividendGrowthScore).HasColumnName("dividend_growth_score").HasPrecision(5, 2);
            e.Property(c => c.ValuationScore).HasColumnName("valuation_score").HasPrecision(5, 2);
            e.Property(c => c.Signal).HasColumnName("signal").HasMaxLength(20).HasConversion<string>();
            e.Property(c => c.ScrapeRunId).HasColumnName("scrape_run_id");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(c => new { c.StockId, c.ComputedDate }).IsUnique();
            e.HasOne(c => c.Stock).WithMany().HasForeignKey(c => c.StockId);
            e.HasOne(c => c.ScrapeRun).WithMany().HasForeignKey(c => c.ScrapeRunId).OnDelete(DeleteBehavior.SetNull);
        });

        // RawApiResponse
        modelBuilder.Entity<RawApiResponse>(e =>
        {
            e.ToTable("raw_responses");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.Source).HasColumnName("source").HasMaxLength(50);
            e.Property(r => r.Endpoint).HasColumnName("endpoint").HasMaxLength(500);
            e.Property(r => r.Symbol).HasColumnName("symbol").HasMaxLength(10);
            e.Property(r => r.ResponseBody).HasColumnName("response_body");
            e.Property(r => r.ContentType).HasColumnName("content_type").HasMaxLength(50);
            e.Property(r => r.HttpStatus).HasColumnName("http_status");
            e.Property(r => r.ScrapedAt).HasColumnName("scraped_at");
        });

        // PortfolioHolding
        modelBuilder.Entity<PortfolioHolding>(e =>
        {
            e.ToTable("portfolio_holdings");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.StockId).HasColumnName("stock_id");
            e.Property(p => p.Shares).HasColumnName("shares");
            e.Property(p => p.AvgCost).HasColumnName("avg_cost").HasPrecision(12, 2);
            e.Property(p => p.PurchaseDate).HasColumnName("purchase_date");
            e.Property(p => p.Platform).HasColumnName("platform").HasMaxLength(50);
            e.Property(p => p.IsActive).HasColumnName("is_active");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(p => p.Stock).WithMany().HasForeignKey(p => p.StockId);
        });

        // PortfolioTransaction
        modelBuilder.Entity<PortfolioTransaction>(e =>
        {
            e.ToTable("portfolio_transactions");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.StockId).HasColumnName("stock_id");
            e.Property(p => p.Action).HasColumnName("action").HasMaxLength(10);
            e.Property(p => p.Shares).HasColumnName("shares");
            e.Property(p => p.Price).HasColumnName("price").HasPrecision(12, 2);
            e.Property(p => p.TotalCost).HasColumnName("total_cost").HasPrecision(14, 2);
            e.Property(p => p.Commission).HasColumnName("commission").HasPrecision(10, 2);
            e.Property(p => p.TransactionDate).HasColumnName("transaction_date");
            e.Property(p => p.Platform).HasColumnName("platform").HasMaxLength(50);
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(p => p.Stock).WithMany().HasForeignKey(p => p.StockId);
        });

        // ScrapeRun
        modelBuilder.Entity<ScrapeRun>(e =>
        {
            e.ToTable("scrape_runs");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.StartedAt).HasColumnName("started_at");
            e.Property(r => r.CompletedAt).HasColumnName("completed_at");
            e.Property(r => r.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
            e.Property(r => r.Mode).HasColumnName("mode").HasMaxLength(10).IsRequired();
            e.Property(r => r.SymbolCount).HasColumnName("symbol_count");
            e.Property(r => r.RecordsScraped).HasColumnName("records_scraped");
            e.Property(r => r.RecordsFailed).HasColumnName("records_failed");
            e.Property(r => r.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(r => r.CreatedAt).HasColumnName("created_at");
        });

        // StockFundamental
        modelBuilder.Entity<StockFundamental>(e =>
        {
            e.ToTable("stock_fundamentals");
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasColumnName("id");
            e.Property(f => f.StockId).HasColumnName("stock_id");
            e.Property(f => f.ScrapedDate).HasColumnName("scraped_date");
            e.Property(f => f.Periodicity).HasColumnName("periodicity").HasMaxLength(5).IsRequired();
            e.Property(f => f.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
            e.Property(f => f.Data).HasColumnName("data").HasColumnType("jsonb")
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<Dictionary<string, decimal?>>(v) ?? new())
                .Metadata.SetValueComparer(DictComparer);
            e.Property(f => f.ScrapeRunId).HasColumnName("scrape_run_id");
            e.Property(f => f.CreatedAt).HasColumnName("created_at");
            e.Property(f => f.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(f => new { f.StockId, f.Periodicity, f.Source }).IsUnique();
            e.HasOne(f => f.Stock).WithMany().HasForeignKey(f => f.StockId);
            e.HasOne(f => f.ScrapeRun).WithMany().HasForeignKey(f => f.ScrapeRunId).OnDelete(DeleteBehavior.SetNull);
        });

        // StockOwnership
        modelBuilder.Entity<StockOwnership>(e =>
        {
            e.ToTable("stock_ownerships");
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).HasColumnName("id");
            e.Property(o => o.StockId).HasColumnName("stock_id");
            e.Property(o => o.AsOfDate).HasColumnName("as_of_date");
            e.Property(o => o.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
            e.Property(o => o.Holders).HasColumnName("holders").HasColumnType("jsonb")
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<List<OwnershipEntry>>(v) ?? new List<OwnershipEntry>())
                .Metadata.SetValueComparer(OwnersComparer);
            e.Property(o => o.ScrapeRunId).HasColumnName("scrape_run_id");
            e.Property(o => o.CreatedAt).HasColumnName("created_at");
            e.Property(o => o.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(o => new { o.StockId, o.AsOfDate }).IsUnique();
            e.HasOne(o => o.Stock).WithMany().HasForeignKey(o => o.StockId);
            e.HasOne(o => o.ScrapeRun).WithMany().HasForeignKey(o => o.ScrapeRunId).OnDelete(DeleteBehavior.SetNull);
        });

        // InsiderTransaction
        modelBuilder.Entity<InsiderTransaction>(e =>
        {
            e.ToTable("insider_transactions");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.StockId).HasColumnName("stock_id");
            e.Property(t => t.PersonName).HasColumnName("person_name").HasMaxLength(200).IsRequired();
            e.Property(t => t.Position).HasColumnName("position").HasMaxLength(100);
            e.Property(t => t.Action).HasColumnName("action").HasMaxLength(20).IsRequired();
            e.Property(t => t.Quantity).HasColumnName("quantity");
            e.Property(t => t.Rate).HasColumnName("rate").HasPrecision(12, 2);
            e.Property(t => t.TransactionDate).HasColumnName("transaction_date");
            e.Property(t => t.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
            e.Property(t => t.ScrapeRunId).HasColumnName("scrape_run_id");
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(t => new { t.StockId, t.PersonName, t.TransactionDate, t.Action }).IsUnique();
            e.HasOne(t => t.Stock).WithMany().HasForeignKey(t => t.StockId);
            e.HasOne(t => t.ScrapeRun).WithMany().HasForeignKey(t => t.ScrapeRunId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
