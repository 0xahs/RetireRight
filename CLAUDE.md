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
dotnet run --project src/RetireRight.Api/RetireRight.Api.csproj   # http://localhost:59931
```

Web app (Next.js — separate process, talks to the API over HTTP):

```bash
cd web
npm install
npm run dev      # http://localhost:3000 (proxies to API at NEXT_PUBLIC_API_URL, default :59931)
npm run build    # production build (also the typecheck/lint gate — there is no separate lint script)
```

Dev requires **both** processes: the .NET API on :59931 and the web app on :3000. CORS in the API allows `http://localhost:3000` (`Cors:AllowedOrigins`).

Pipeline operations are triggered via HTTP (or from the web app's Pipeline page):

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
  ├── Write/pipeline (background queue):
  │     POST /api/pipeline/scrape   → PipelineJobService → Scrapers → DB
  │     POST /api/pipeline/compute  → PipelineJobService → MetricsEngine → DB
  │     POST /api/export/run        → PipelineJobService → Exporters (JSON, Briefing markdown)
  │     GET  /api/pipeline/status   → DB health, last runs, active job
  └── Read (serve the web app, DB → DTO via Mapping/ presentation layer):
        /api/stocks · /api/sectors · /api/screener · /api/dividends · /api/macro
        /api/portfolio · /api/scrapers · /api/settings/schedule
```

Endpoints live in `src/RetireRight.Api/Endpoints/*.cs` (one file per group, each a `MapXxxEndpoints()` extension wired up in `Program.cs`). The read endpoints don't return raw metrics — they pass `computed_metrics` JSONB through the **presentation layer** in `src/RetireRight.Api/Mapping/`:

| Component | Produces |
|-----------|----------|
| `SignalGrammar` | Per-stock verdict (`ACCUMULATE`/`HOLD`/`WATCH`/`REVIEW`/`AVOID`), conviction, safety tier (S–D), confidence (0–1), why-line, risks, net (post-WHT) yield. Pure function of metrics + freshness + sector. |
| `MemoGenerator` | The `/api/stocks/{symbol}/memo` AI-memo envelope (citations to specific metric keys). |
| `MetricsMapper` | Maps raw `Dictionary<string, decimal?>` metric keys into the typed DTO shapes the web consumes. |

These are presentation-only (no DB writes, no metric computation). When the web shows a verdict/conviction/why-line, it comes from `SignalGrammar` — not from a stored column.

**Seven projects, one dependency direction:**

| Project | Role |
|---------|------|
| `RetireRight.Api` | Entry point, DI setup, HTTP endpoints, background `PipelineJobService`. References all other projects. |
| `RetireRight.Core` | Domain models, interfaces, enums, config DTOs. No dependencies. |
| `RetireRight.Infrastructure` | DbContext, repositories, entity config. Depends on Core. |
| `RetireRight.Scrapers` | Playwright/AngleSharp web scraping with anti-bot (throttle, retry, stealth). Depends on Core + Infrastructure. |
| `RetireRight.MetricsEngine` | Eight `IMetricCalculator` implementations: DividendSafety, DividendGrowth, Valuation, FinancialHealth, GrowthQuality, Risk, Banking, CompositeScore. Plus `FundamentalsEnricher` (pre-processor) and `OpportunityScorer` (post-processor). Depends on Core. Note: calculators run independently and see only `ctx.Fundamentals` (raw scraped data), never each other's output — derive shared building blocks from raw, don't read another category's keys. Some Category-E metrics deliberately live under other prefixes (ROE/ROA/ROIC = `c12/c13/c14`, margins = `c15–c18`, asset turnover/equity multiplier = `d7/d8`). |
| `RetireRight.Screening` | `StockScreener` + preset filters (dividend_safety, high_yield, growth, value, retirement_core). Depends on Core. |
| `RetireRight.Export` | `JsonExporter` and `BriefingExporter` implementing `IExporter`. Depends on Core. |

**Key interfaces** — new scrapers implement `IScraper`, new exporters implement `IExporter`, new metric calculators implement `IMetricCalculator`. All are registered in the API's `Program.cs` DI container.

## Web App (`web/` — Next.js 16, React 19)

> ⚠️ `web/AGENTS.md`: **this is not the Next.js you know.** Next.js 16 has breaking changes vs. older training data. Read `web/node_modules/next/dist/docs/` before writing framework code. Note: middleware is now `proxy.ts` (NextAuth's `auth` middleware lives at `src/proxy.ts`, not `middleware.ts`).

Stack: App Router with route groups, **TanStack Query** (all server state), **shadcn/ui** + Radix + **Tailwind v4**, **Zustand** (UI-only state, `stores/uiStore.ts`), **NextAuth v5** (credentials), **lightweight-charts** for price charts.

Data flow — the web app never talks to the DB; it only calls the .NET API:

```
component → hook in src/lib/api/queries.ts (useQuery/useMutation)
          → src/lib/api/client.ts (thin fetch wrapper, NEXT_PUBLIC_API_URL)
          → .NET API → DTO typed in src/lib/api/types.ts
```

When adding a screen that needs new data: add the DTO to `types.ts`, a hook to `queries.ts`, then a page under `src/app/(dashboard)/`. Don't scatter raw `fetch` calls or hardcode the API base URL — go through `client.ts`.

Pages (`src/app/(dashboard)/`): `stocks`, `stock/[symbol]`, `screener`, `sector`, `dividends`, `watchlist`, `compare`, `portfolio` (+ `portfolio/plan` monthly planner), `alerts`, `export`, `pipeline`. Signal-grammar UI primitives (verdict badge, conviction dots, safety tier, why-line, ASU card) live in `src/components/signal/`.

**Auth:** single-user credentials login (`AUTH_EMAIL`/`AUTH_PASSWORD` env vars checked in `src/auth.ts`); `proxy.ts` gates every route except `/login` and `/api/auth`. Env vars (`web/.env.local`, gitignored): `NEXT_PUBLIC_API_URL`, `AUTH_SECRET`, `AUTH_EMAIL`, `AUTH_PASSWORD`.

## Database

PostgreSQL (Supabase-hosted). Key tables: `stocks`, `daily_prices`, `dividends`, `macro_indicators`, `index_levels`, `computed_metrics` (JSONB metrics field), `raw_responses`, `portfolio_holdings`, `portfolio_transactions`. Most tables have composite unique constraints (see `RetireRightDbContext` fluent config).

## Configuration

- `src/RetireRight.Api/appsettings.json` — defaults to **localhost** PostgreSQL (`retireright_dev`), export path, watchlist symbols. Credentials fields are **empty strings** — never commit real credentials here.
- `src/RetireRight.Api/appsettings.local.json` — **gitignored**, contains real Sarmaaya credentials. Loaded as an optional layer over `appsettings.json`. Per-source credentials also live in the `data_source_configs` table (`credential_email`/`credential_password`) and can be updated via `PUT /api/scrapers/{name}/credentials`.
- For Supabase (production): set env var `RETIRERIGHT_RetireRight__Database__ConnectionString=<supabase_connection_string>`
- JSONB columns (`Dictionary<string, decimal?>`) use explicit `HasConversion()` in DbContext — do NOT use `EnableDynamicJson()` (it silently drops keys on read)
- Per-source `RateLimit` and `Retry` policies configured in `DataSources` array
- Options pattern: `RetireRightOptions` → `DatabaseOptions`, `ExportOptions`, `WatchlistOptions`, `DataSourceOptions[]`, `PortfolioOptions`, `SchedulerOptions`
- `Scheduler` block (`DailyRunTime`, `Enabled`, `Jobs`) drives `SchedulerService`; `Cors:AllowedOrigins` whitelists the web app origin

## DI Lifetimes

- **Scoped**: DbContext, all repositories, `PipelineOrchestrator`, `ScraperFactory`, `MetricsEngine`, calculators, `StockScreener`
- **Transient**: Scrapers (created fresh per pipeline job)
- **Singleton**: RequestThrottler, RetryPolicy, Exporters, `PipelineJobService`, `PipelineEventBroadcaster`
- **Hosted (background)**: `PipelineJobService` (job queue) and `SchedulerService` (checks the daily schedule every minute, auto-enqueues `scrape`/`compute`/`export` when `Scheduler.Enabled`)

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

**Metric counts:**
- Emitted keys (populated + null): **90 non-bank / 105 banking** — base 77/92 + 13 Category-E (Growth Quality) keys added 2026-05-24. These totals are the `SignalGrammar` completeness denominators.
- Populated (verified 2026-05-16, pre-Growth-Quality): 61/77 non-bank, 67/92 banking. Re-verify the populated counts after the next compute run now that Category E is live.

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

**Phase 1.5 — Web App & Signal Layer (COMPLETE, Sprints A–F)**
- [x] Next.js 16 / React 19 web app (`web/`) — dashboard, stocks, screener, sector, dividends, watchlist, compare, portfolio, alerts, export, pipeline pages
- [x] Signal grammar presentation layer (`Mapping/SignalGrammar`, `MemoGenerator`, `MetricsMapper`): verdict, conviction, safety tier, why-line, net yield
- [x] Read API surface (stocks/sectors/screener/dividends/macro/portfolio/scrapers/settings)
- [x] AI memo + metric citations + Claude envelope (Sprint E)
- [x] Sector analysis, alerts page, scrape-on-demand, Lightweight Charts price charts (Sprint F)
- [x] Monthly investment planner (`/api/portfolio/monthly-plan`, `portfolio/plan` page)
- [x] `SchedulerService` daily auto-run (scrape → compute → export)
- [x] Scraper identity decoupled from enum via `[DataSource]` attribute + `ScraperFactory` reflection

**Phase 2 — Data Pipeline (NEXT)**
- [x] StockAnalysis scraper (`StockAnalysisScraper`)
- [x] Macro data scrapers (`MacroScraper` — PKR/USD, Brent, SBP rate)
- [ ] Source failover chains (if Sarmaaya fails, try PsxOfficial) — full cash-flow coverage still blocked (a2, a3, a5, a10, b8, c8, d9)
- [x] GrowthQualityCalculator (Category E) — 13 new keys: e4 ROCE, e9 EBITDA margin, e12 inventory turnover, e13/e14 per-employee, e15 DuPont (3-factor), e16 DuPont 5-factor pieces (tax/interest burden, EBIT margin), e17 SGR, e18 plowback, e19/e20 capex intensity. (E1/E2/E3/E6/E7/E8/E10/E11 already emitted under c12–c18/d7; E5 ROIC−WACC omitted — no WACC source.)
- [ ] E&P, Power, Fertilizer sector-specific calculators

**Phase 3 — Intelligence**
- [x] Alert engine (yield traps, payout > 100%, upcoming ex-dates)
- [ ] Portfolio integration (import from Finqalab screenshots) — manual entry done via portfolio endpoints
- [ ] CGT calculator

**Phase 4 — Automation**
- [x] Scheduled daily runs (`SchedulerService`, configurable via `Scheduler` block / settings UI)
- [ ] Data freshness monitoring — freshness already feeds `SignalGrammar` confidence; no standalone monitor yet
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
