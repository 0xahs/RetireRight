# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RetireRight is a .NET 10 (C# 13) HTTP API + Next.js web app for Pakistani Stock Exchange (PSX) dividend investors. It scrapes market data from sources like sarmaaya.pk, computes financial metrics, screens stocks, and exports analyzed data as JSON/markdown briefings. The legacy CLI was removed once the API exposed equivalents for every pipeline operation.

## Build & Run Commands

```bash
# Restore + build (from repo root — solution file is RetireRight.slnx)
dotnet restore
dotnet build

# Run the API (the user manages this process manually — do not start/stop it for them)
dotnet run --project src/RetireRight.Api/RetireRight.Api.csproj
```

Pipeline operations are now triggered via HTTP:

```bash
# Scrape (source = configured Name like "Profile"/"Cash Flow", or Type like "Sarmaaya", or "stock" for all per-stock sources)
curl -X POST http://localhost:59931/api/pipeline/scrape -H "Content-Type: application/json" \
     -d '{"source":"Profile","symbols":["EFERT","MCB"]}'

# Compute metrics
curl -X POST http://localhost:59931/api/pipeline/compute -H "Content-Type: application/json" \
     -d '{"symbols":["MCB"]}'   # omit body for all watchlist

# Export json + markdown briefing
curl -X POST http://localhost:59931/api/export/run

# Pipeline + DB status
curl http://localhost:59931/api/pipeline/status
```

## EF Core Migrations

Run from the `src/RetireRight.Infrastructure` directory:

```bash
dotnet ef database update --startup-project ../RetireRight.Api/RetireRight.Api.csproj
dotnet ef migrations add <Name> --startup-project ../RetireRight.Api/RetireRight.Api.csproj
```

Migrations applied: `AddFundamentalsOwnershipScrapeRuns`, `FundamentalsKeyBySource`. Tables: stocks, daily_prices, dividends, stock_fundamentals, stock_ownerships, insider_transactions, index_levels, computed_metrics, raw_responses, scrape_runs, portfolio_holdings, portfolio_transactions.

`stock_fundamentals` unique key is `(StockId, Periodicity, Source)` — each scraper owns its own row per periodicity. Rows from different sources are merged at read time in `FundamentalsRepository.GetLatestAsync`.

## Architecture

```
HTTP API (RetireRight.Api, ASP.NET Core Minimal API)
  ├── POST /api/pipeline/scrape  → PipelineJobService (background queue) → Scrapers → DB
  ├── POST /api/pipeline/compute → PipelineJobService → MetricsEngine → DB
  ├── POST /api/export/run       → PipelineJobService → Exporters (JSON, Briefing markdown)
  └── GET  /api/pipeline/status  → DB health, last runs, active job
```

**Seven projects, one dependency direction:**

| Project | Role |
|---------|------|
| `RetireRight.Api` | Entry point, DI setup, HTTP endpoints, background `PipelineJobService`. References all other projects. |
| `RetireRight.Core` | Domain models, interfaces, enums, config DTOs. No dependencies. |
| `RetireRight.Infrastructure` | DbContext, repositories, entity config. Depends on Core. |
| `RetireRight.Scrapers` | Playwright/AngleSharp web scraping with anti-bot (throttle, retry, stealth). Depends on Core + Infrastructure. |
| `RetireRight.MetricsEngine` | Seven `IMetricCalculator` implementations: DividendSafety, DividendGrowth, Valuation, FinancialHealth, Risk, Banking, CompositeScore. Plus `FundamentalsEnricher` (pre-processor) and `OpportunityScorer` (post-processor). Depends on Core. |
| `RetireRight.Screening` | `StockScreener` + preset filters (dividend_safety, high_yield, growth, value, retirement_core). Depends on Core. |
| `RetireRight.Export` | `JsonExporter` and `BriefingExporter` implementing `IExporter`. Depends on Core. |

**Key interfaces** — new scrapers implement `IScraper`, new exporters implement `IExporter`, new metric calculators implement `IMetricCalculator`. All are registered in the API's `Program.cs` DI container.

## Database

PostgreSQL (Supabase-hosted). Key tables: `stocks`, `daily_prices`, `dividends`, `macro_indicators`, `index_levels`, `computed_metrics` (JSONB metrics field), `raw_responses`, `portfolio_holdings`, `portfolio_transactions`. Most tables have composite unique constraints (see `RetireRightDbContext` fluent config).

## Configuration

- `src/RetireRight.Api/appsettings.json` — defaults to **localhost** PostgreSQL (`retireright_dev`), export path, watchlist symbols. Credentials fields are **empty strings** — never commit real credentials here.
- `src/RetireRight.Api/appsettings.local.json` — **gitignored**, contains real Sarmaaya credentials. Loaded as an optional layer over `appsettings.json`. Per-source credentials also live in the `data_source_configs` table (`credential_email`/`credential_password`) and can be updated via `PUT /api/scrapers/{name}/credentials`.
- For Supabase (production): set env var `RETIRERIGHT_RetireRight__Database__ConnectionString=<supabase_connection_string>`
- JSONB columns (`Dictionary<string, decimal?>`) use explicit `HasConversion()` in DbContext — do NOT use `EnableDynamicJson()` (it silently drops keys on read)
- Per-source `RateLimit` and `Retry` policies configured in `DataSources` array
- Options pattern: `RetireRightOptions` → `DatabaseOptions`, `ExportOptions`, `WatchlistOptions`, `DataSourceOptions[]`, `PortfolioOptions`

## DI Lifetimes

- **Scoped**: DbContext, all repositories, `PipelineOrchestrator`
- **Transient**: Scrapers (created fresh per pipeline job)
- **Singleton**: RequestThrottler, RetryPolicy, Exporters, `PipelineJobService`, `PipelineEventBroadcaster`

## Anti-Bot Infrastructure (Scrapers)

`RequestThrottler` (sliding window per-source), `RetryPolicy` (exponential backoff), `UserAgentRotator`, `StealthConfigurator` (Playwright headless evasion). Rate limits are configurable per data source — respect these when adding new scrapers.

## Full Pipeline: Scrape → Store → Compute → Export

```
POST /api/pipeline/scrape  → SarmaayaApi fetches 10 endpoints per stock → PersistAsync writes to 7 tables
POST /api/pipeline/compute → Loads fundamentals + prices + dividends
                           → FundamentalsEnricher (derives DPS, market cap, bank revenue)
                           → 7 calculators (incl. BankingCalculator for banking/Islamic banking sectors)
                           → OpportunityScorer (quality + value + technical signals, o1-o7 keys)
                           → computed_metrics table
POST /api/export/run       → Reads computed_metrics → JSON + Markdown to exports folder
```

Data source identifiers used in the `source` field on pipeline requests, `scrape_runs.source`, and the active-job payload come from `data_source_configs.name` (e.g. `Profile`, `Cash Flow`, `Dividends`, `Macro`, `Listings`). The underlying scraper enum (`Sarmaaya`, `StockAnalysis`, `PsxTerminal`, `TradingEconomics`, `PsxUniverse`) is stored in `data_source_configs.type` as the technical bridge to the C# scraper class.

**Metric counts (verified 2026-05-16):**
- Non-bank stocks: 61/77 metrics populated
- Banking stocks (MCB, UBL, MEBL): 67/92 metrics (15 banking H-category metrics added)

**MetricContext.Sector** is typed as `Sector?` (enum, not string). Banking gate uses `HashSet<Sector> { Banking, IslamicBanking }` — consistent across BankingCalculator, CompositeScoreCalculator, OpportunityScorer, and FundamentalsEnricher.

## Sarmaaya API Critical Patterns

- **Base URL**: `https://beta-restapi.sarmaaya.pk`
- **Placeholder data**: Time-series `data[0]` is JSON number (real), `data[1..5]` are often JSON strings with repeated placeholder values. Only trust `data[1]` as prev_year if it's a JSON number or differs from `data[2]`.
- **Industry averages broken**: Returns `2.22` for all metrics across all sectors — code guards against this.
- **Cash flow endpoint 404s**: No operating/free cash flow from Sarmaaya — blocks 7 metrics.
- **Key normalization**: Display names → `NormalizeKey()` → prefixed snake_case (`ratio_*`, `income_*`, `bs_*`) → `SarmaayaKeyMapper.MapToStandard()` → `FundamentalKeys` constants.
- **Fundamentals upsert**: Same source = REPLACE entire JSONB dict (clean slate). Different source = MERGE (accumulate).

## Export Data Flow

`POST /api/export/run` (handled by `PipelineOrchestrator.ExportAsync`) queries the DB for watchlist stocks, latest prices, computed metrics, upcoming dividends (60-day window), and macro indicators (KSE-100, PKR/USD, Brent, SBP rate). Outputs timestamped files plus a `latest.{format}` copy for stable access paths.

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
- [x] Infrastructure: DbContext + 10 repositories, EF migrations applied
- [x] Export: JSON + Markdown briefing exporters
- [x] MetricsEngine: 7 calculators (Safety, Growth, Valuation, Health, Risk, Banking, Composite) — 61/77 non-banks, 67/92 banks
- [x] FundamentalsEnricher (pre-processor: DPS, market cap, bank revenue)
- [x] OpportunityScorer (post-processor: quality + value + technical, o1-o7 keys, bitmask flags)
- [x] BankingCalculator: H1-H15 (NIM, cost-to-income, NPL, CAR, Tier1, LCR, ADR, ROA, ROE, etc.)
- [x] Screening: StockScreener + 5 presets
- [x] HTTP API: ASP.NET Core Minimal API with `PipelineJobService` background queue (legacy CLI commands removed)
- [x] Full pipeline tested: 8-stock scrape → compute → export working
- [x] Local PostgreSQL 17 database with all tables populated
- [x] Sarmaaya key mapper: all 54 ratio keys + income + balance sheet mapped
- [x] Fundamentals unique key: (StockId, Periodicity, Source) — per-source row ownership
- [x] appsettings.local.json pattern for gitignored credentials
- [x] Repo on GitHub (private): github.com/0xahs/RetireRight

**Phase 2 — Data Pipeline (NEXT)**
- [ ] StockAnalysis scraper (unlocks cash flow data → 7 metrics: a2, a3, a5, a10, b8, c8, d9)
- [ ] Source failover chains (if Sarmaaya fails, try PsxOfficial)
- [ ] Macro data scrapers (PKR/USD, Brent, SBP rate — KSE-100 already done via Sarmaaya)
- [ ] GrowthQualityCalculator (Category E — 20 metrics, data available)
- [ ] E&P, Power, Fertilizer sector-specific calculators

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

1. Create `src/RetireRight.Scrapers/Sources/{Identifier}Scraper.cs`
2. Inherit from `BaseScraper` (for HTTP/HTML / Playwright — both routes use this base)
3. Decorate the class with `[DataSource("{Identifier}", PerSymbol = true|false)]` from `RetireRight.Core.Attributes` — this is the only place the type identifier is hardcoded. `ScraperFactory` discovers all decorated classes at startup via reflection and `ActivatorUtilities.CreateInstance`, so no factory edit is needed
4. Implement `ScrapeAsync()` via the `ExecuteScrapeAsync` hook — use `ThrottleAsync()` and `WithRetryAsync()` from the base
5. Add a row to the `data_source_configs` table (via seed in `src/RetireRight.Api/Program.cs` or a migration) — the `name` column drives the user-facing label, `type` matches the attribute identifier

## How to Add a New Metric Calculator

1. Create `src/RetireRight.MetricsEngine/Calculators/{Name}Calculator.cs`
2. Implement `IMetricCalculator` — return `Dictionary<string, decimal?>` with metric keys
3. Use naming convention: `{category_letter}{number}_{snake_case_name}` (e.g., `h1_nim` for banking NIM)
4. Register in `src/RetireRight.Api/Program.cs` DI as `IMetricCalculator`
5. Document the metric in `dividend-metrics-reference.md`

## How to Add a Screen Preset

1. Add a new `ScreenPreset` entry in `src/RetireRight.Screening/Screens/ScreenPresets.cs`
2. Define filters with metric key, operator, and threshold
3. The `StockScreener` will automatically apply all filters and sort by composite score
