# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RetireRight is a .NET 10 (C# 13) CLI data pipeline for Pakistani Stock Exchange (PSX) dividend investors. It scrapes market data from sources like sarmaaya.pk, computes financial metrics, screens stocks, and exports analyzed data as JSON/markdown briefings.

## Build & Run Commands

```bash
# Restore, build, run (from repo root — solution file is RetireRight.slnx)
dotnet restore
dotnet build
dotnet run --project src/RetireRight.Cli/RetireRight.Cli.csproj -- <command> [options]

# CLI commands
dotnet run --project src/RetireRight.Cli/RetireRight.Cli.csproj -- run --all              # scrape all enabled sources
dotnet run --project src/RetireRight.Cli/RetireRight.Cli.csproj -- run --source Sarmaaya   # single source
dotnet run --project src/RetireRight.Cli/RetireRight.Cli.csproj -- run --source Sarmaaya --symbols EFERT MCB  # specific symbols
dotnet run --project src/RetireRight.Cli/RetireRight.Cli.csproj -- export --format all     # export json + md
dotnet run --project src/RetireRight.Cli/RetireRight.Cli.csproj -- status                  # db health check
dotnet run --project src/RetireRight.Cli/RetireRight.Cli.csproj -- compute --all           # compute metrics for all stocks
dotnet run --project src/RetireRight.Cli/RetireRight.Cli.csproj -- compute --symbols MCB   # compute for specific stock
```

## EF Core Migrations

Run from the `src/RetireRight.Infrastructure` directory:

```bash
dotnet ef database update --startup-project ../RetireRight.Cli/RetireRight.Cli.csproj
dotnet ef migrations add <Name> --startup-project ../RetireRight.Cli/RetireRight.Cli.csproj
```

Migration `AddFundamentalsOwnershipScrapeRuns` exists and is applied. Tables: stocks, daily_prices, dividends, stock_fundamentals, stock_ownerships, insider_transactions, index_levels, computed_metrics, raw_responses, scrape_runs, portfolio_holdings, portfolio_transactions.

## Architecture

```
CLI (System.CommandLine)
  ├── run    → Scrapers → DB (PostgreSQL via EF Core)
  ├── export → DB → Exporters (JSON, Briefing markdown)
  └── status → DB health & record counts
```

**Seven projects, one dependency direction:**

| Project | Role |
|---------|------|
| `RetireRight.Cli` | Entry point, DI setup, CLI commands. References all other projects. |
| `RetireRight.Core` | Domain models, interfaces, enums, config DTOs. No dependencies. |
| `RetireRight.Infrastructure` | DbContext, repositories, entity config. Depends on Core. |
| `RetireRight.Scrapers` | Playwright/AngleSharp web scraping with anti-bot (throttle, retry, stealth). Depends on Core + Infrastructure. |
| `RetireRight.MetricsEngine` | Six `IMetricCalculator` implementations: DividendSafety, DividendGrowth, Valuation, FinancialHealth, Risk, CompositeScore. Depends on Core. |
| `RetireRight.Screening` | `StockScreener` + preset filters (dividend_safety, high_yield, growth, value, retirement_core). Depends on Core. |
| `RetireRight.Export` | `JsonExporter` and `BriefingExporter` implementing `IExporter`. Depends on Core. |

**Key interfaces** — new scrapers implement `IScraper`, new exporters implement `IExporter`, new metric calculators implement `IMetricCalculator`. All are registered in `Program.cs` DI container.

## Database

PostgreSQL (Supabase-hosted). Key tables: `stocks`, `daily_prices`, `dividends`, `macro_indicators`, `index_levels`, `computed_metrics` (JSONB metrics field), `raw_responses`, `portfolio_holdings`, `portfolio_transactions`. Most tables have composite unique constraints (see `RetireRightDbContext` fluent config).

## Configuration

- `src/RetireRight.Cli/appsettings.json` — defaults to **localhost** PostgreSQL (`retireright_dev`), export path, watchlist symbols, data source rate limits
- For Supabase (production): set env var `RETIRERIGHT_RetireRight__Database__ConnectionString=<supabase_connection_string>`
- No `appsettings.local.json` or `appsettings.Production.json` — keep it simple, one file + optional env var override
- JSONB columns (`Dictionary<string, decimal?>`) use explicit `HasConversion()` in DbContext — do NOT use `EnableDynamicJson()` (it silently drops keys on read)
- Per-source `RateLimit` and `Retry` policies configured in `DataSources` array
- Options pattern: `RetireRightOptions` → `DatabaseOptions`, `ExportOptions`, `WatchlistOptions`, `DataSourceOptions[]`, `PortfolioOptions`

## DI Lifetimes

- **Scoped**: DbContext, all repositories
- **Transient**: Scrapers (created fresh per command invocation)
- **Singleton**: RequestThrottler, RetryPolicy, Exporters

## Anti-Bot Infrastructure (Scrapers)

`RequestThrottler` (sliding window per-source), `RetryPolicy` (exponential backoff), `UserAgentRotator`, `StealthConfigurator` (Playwright headless evasion). Rate limits are configurable per data source — respect these when adding new scrapers.

## Full Pipeline: Scrape → Store → Compute → Export

```
run --source Sarmaaya    → SarmaayaApi fetches 10 endpoints per stock → PersistAsync writes to 7 tables
compute --all            → Loads fundamentals + prices + dividends → 6 calculators → computed_metrics table
export --format all      → Reads computed_metrics → JSON + Markdown to exports folder
```

## Sarmaaya API Critical Patterns

- **Base URL**: `https://beta-restapi.sarmaaya.pk`
- **Placeholder data**: Time-series `data[0]` is JSON number (real), `data[1..5]` are often JSON strings with repeated placeholder values. Only trust `data[1]` as prev_year if it's a JSON number or differs from `data[2]`.
- **Industry averages broken**: Returns `2.22` for all metrics across all sectors — code guards against this.
- **Cash flow endpoint 404s**: No operating/free cash flow from Sarmaaya — blocks 7 metrics.
- **Key normalization**: Display names → `NormalizeKey()` → prefixed snake_case (`ratio_*`, `income_*`, `bs_*`) → `SarmaayaKeyMapper.MapToStandard()` → `FundamentalKeys` constants.
- **Fundamentals upsert**: Same source = REPLACE entire JSONB dict (clean slate). Different source = MERGE (accumulate).

## Export Data Flow

The `export` command queries the DB for watchlist stocks, latest prices, computed metrics, upcoming dividends (60-day window), and macro indicators (KSE-100, PKR/USD, Brent, SBP rate). Outputs timestamped files plus a `latest.{format}` copy for stable access paths.

## No Test Suite

There is currently no test project. When adding tests, follow the existing project naming pattern (`RetireRight.Tests`) and target `net10.0`.

---

## Reference Documents (External — Investement Repo)

These design documents live in `C:\Users\ahmad\OneDrive\Documents\Investement\` and should be read when making architectural decisions or adding new features:

| Document | When to Read | Purpose |
|----------|-------------|---------|
| `psx-scraper-design-prompt.md` | **Before any architecture changes** | Original design brief — defines the Senior .NET Architect persona and all design constraints |
| `retireright-architecture.md` | When adding new projects, tables, or CLI commands | Full architecture doc: solution structure, DB schema, interfaces, anti-bot strategy, data flow, 5-phase dev plan |
| `dividend-metrics-reference.md` | When adding new metric calculators | 112+ metrics across 13 categories (A-M) with formulas, rationale, and data requirements |
| `retireright-gap-analysis.md` | When planning next features | 15 gaps identified: P0 (critical), P1 (important), P2 (moderate), P3 (future) |
| `portfolio.md` | When testing export/screening | Ahmad's live portfolio — current holdings, tranche plans, dividend calendar |
| `research.md` | When adding data source scrapers | Fundamental data, broker targets, dividend data, macro outlook for PSX stocks |
| `resources.md` | When adding new data sources | 60+ bookmarked PSX data source URLs |

## Investor Context

This tool is built for **Ahmad Hasan Sahi** — a middle-class Pakistani software engineer building a retirement dividend portfolio on PSX. Key constraints:
- **Strategy:** Long-term (10-15yr), dividend-focused, Warren Buffett style
- **Risk:** Moderate — no penny stocks, no leverage
- **Platform:** Finqalab (Next Capital) for trades
- **Tax:** Pakistan filer, 15% WHT on dividends
- **Budget:** Rs. 10-15K/month systematic investment
- **Token problem:** This tool exists to reduce Claude session token usage from ~15-20K (live web fetching) to ~2-3K (pre-computed JSON exports)

## PSX Data Sources

| Source | URL Pattern | Method | Data Types |
|--------|------------|--------|-----------|
| **Sarmaaya** (implemented) | `beta-restapi.sarmaaya.pk/api/stocks/*` | REST API | Prices, fundamentals (54 ratios + income + balance sheet), dividends, ownership, insiders, returns, price history, market indices. No cash flow statement (404). |
| PSX Official | `dps.psx.com.pk/company/{TICKER}` | API/HTML | Official prices, corporate actions |
| KSE Stocks | `ksestocks.com/BookClosures` | HTML | Book closures, dividends |
| StockAnalysis | `stockanalysis.com/quote/psx/{TICKER}/` | HTML | Fundamentals, dividends, statistics |
| Trading Economics | `tradingeconomics.com/commodity/brent-crude-oil` | HTML | Brent crude, PKR/USD |
| SBP | `sbp.org.pk/m_policy/index.asp` | HTML | Policy rate |
| Sarmaaya Index | `sarmaaya.pk/indexes/KSE100` | Browser | KSE-100 index level |

## Development Phases

**Phase 1 — Core Pipeline (COMPLETE)**
- [x] Solution structure (7 projects + references + NuGet packages)
- [x] Core: enums, models, interfaces, configuration, FundamentalKeys constants
- [x] Scrapers: anti-bot infra + Sarmaaya API client (10 endpoints per stock)
- [x] Infrastructure: DbContext + 10 repositories, EF migration applied
- [x] Export: JSON + Markdown briefing exporters
- [x] MetricsEngine: 6 calculators (Safety, Growth, Valuation, Health, Risk, Composite) — 61/77 metrics for non-banks
- [x] Screening: StockScreener + 5 presets
- [x] CLI: Program.cs + DI + 4 commands (run, export, status, compute)
- [x] Full 50-stock scrape → compute → export pipeline working
- [x] Local PostgreSQL database with all tables populated
- [x] Sarmaaya key mapper: all 54 ratio keys + income + balance sheet mapped

**Phase 2 — Data Pipeline (NEXT)**
- [ ] StockAnalysis scraper (unlocks cash flow data → 7 metrics)
- [ ] Source failover chains (if Sarmaaya fails, try PsxOfficial)
- [ ] Macro data scrapers (PKR/USD, Brent, SBP rate — KSE-100 already done)
- [ ] GrowthQualityCalculator (Category E — 20 metrics, data available)
- [ ] Sector-specific metric calculators (Banking, E&P, Power, Fertilizer)

**Phase 3 — Intelligence**
- [ ] Alert engine (yield traps, payout > 100%, upcoming ex-dates)
- [ ] Portfolio integration (import from Finqalab screenshots)
- [ ] CGT calculator

**Phase 4 — Automation**
- [ ] Scheduled daily runs (Windows Task Scheduler or cron)
- [ ] Data freshness monitoring
- [ ] PSX trading calendar awareness

**Phase 5 — Polish**
- [ ] Test suite
- [ ] Error recovery and logging improvements
- [ ] Performance optimization for batch scraping

## How to Add a New Scraper

1. Create `src/RetireRight.Scrapers/Sources/{Name}Scraper.cs`
2. Inherit from `BaseScraper` (for HTTP/HTML) or `BrowserScraper` (for Playwright)
3. Implement `ScrapeAsync()` — use `ThrottleAsync()` and `RetryAsync()` from base
4. Add `DataSourceType.{Name}` to the enum in `Core/Enums/DataSourceType.cs`
5. Register in `Program.cs` DI switch statement under `// Scrapers`
6. Add config entry in `appsettings.json` `DataSources` array with rate limits

## How to Add a New Metric Calculator

1. Create `src/RetireRight.MetricsEngine/Calculators/{Name}Calculator.cs`
2. Implement `IMetricCalculator` — return `Dictionary<string, decimal?>` with metric keys
3. Use naming convention: `{category_letter}{number}_{snake_case_name}` (e.g., `h1_nim` for banking NIM)
4. Register in `Program.cs` DI as `IMetricCalculator`
5. Document the metric in `dividend-metrics-reference.md`

## How to Add a Screen Preset

1. Add a new `ScreenPreset` entry in `src/RetireRight.Screening/Screens/ScreenPresets.cs`
2. Define filters with metric key, operator, and threshold
3. The `StockScreener` will automatically apply all filters and sort by composite score
