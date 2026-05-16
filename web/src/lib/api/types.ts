// ── Stocks ────────────────────────────────────────────────────────────────────

export interface StockSummaryDto {
  id: number;
  symbol: string;
  name: string;
  sector: string;
  price: number | null;
  changePct: number | null;
  compositeScore: number | null;
  opportunityScore: number | null;
  signal: string | null;
  dividendYield: number | null;
  dividendCoverage: number | null;
  opportunityFlags: string[];
  scrapedAt: string | null;
  computedAt: string | null;
}

export interface StockDetailDto {
  summary: StockSummaryDto;
  metrics: StockMetricsDto;
  recentDividends: DividendSummaryDto[];
  priceHistory: PricePointDto[];
}

export interface PricePointDto {
  date: string;
  open: number | null;
  high: number | null;
  low: number | null;
  close: number;
  volume: number | null;
  changePct: number | null;
}

// ── Metrics ───────────────────────────────────────────────────────────────────

export interface StockMetricsDto {
  dividendSafety: DividendSafetyMetrics;
  dividendGrowth: DividendGrowthMetrics;
  valuation: ValuationMetrics;
  risk: RiskMetrics;
  banking: BankingMetrics | null;
  opportunity: OpportunityMetrics;
  scores: ScoreBreakdown;
}

export interface DividendSafetyMetrics {
  earningsPayoutRatio: number | null;
  dividendCoverage: number | null;
  debtToEquity: number | null;
  interestCoverage: number | null;
  consecutiveDivYears: number | null;
}

export interface DividendGrowthMetrics {
  dividendCagr3y: number | null;
  dividendCagr5y: number | null;
  earningsCagr3y: number | null;
  dividendPerShare: number | null;
  growthConsistency: number | null;
}

export interface ValuationMetrics {
  peRatio: number | null;
  pbRatio: number | null;
  dividendYield: number | null;
  marginOfSafety: number | null;
  earningsYield: number | null;
  grahamNumber: number | null;
}

export interface RiskMetrics {
  beta: number | null;
  rsi14: number | null;
  volatility30d: number | null;
  drawdownFrom52wHigh: number | null;
  rangePosition52w: number | null;
}

export interface BankingMetrics {
  nim: number | null;
  costToIncome: number | null;
  nplRatio: number | null;
  car: number | null;
  tier1Ratio: number | null;
  lcr: number | null;
  adr: number | null;
  roa: number | null;
  roe: number | null;
  loansGrowth: number | null;
  feeIncomeRatio: number | null;
}

export interface OpportunityMetrics {
  rangePosition52w: number | null;
  drawdownFrom52wHigh: number | null;
  qualitySignal: number | null;
  valueSignal: number | null;
  technicalSignal: number | null;
  opportunityScore: number | null;
  flags: string[];
}

export interface ScoreBreakdown {
  composite: number | null;
  safety: number | null;
  growth: number | null;
  valuation: number | null;
}

// ── Dividends ─────────────────────────────────────────────────────────────────

export interface DividendSummaryDto {
  exDate: string;
  dps: number;
  percentage: number | null;
  dividendType: string;
  paymentDate: string | null;
  daysUntilEx: number;
  year: string | null;
}

export interface UpcomingDividendDto {
  symbol: string;
  stockName: string;
  exDate: string;
  dps: number;
  lastBuyDate: string;
  daysUntilEx: number;
  paymentDate: string | null;
}

// ── Screener ──────────────────────────────────────────────────────────────────

export interface ScreenerRequestDto {
  preset?: string;
  filters?: ScreenerFilterDto[];
  sortBy?: string;
  ascending?: boolean;
  limit?: number;
  watchlistOnly?: boolean;
}

export interface ScreenerFilterDto {
  metricKey: string;
  min?: number;
  max?: number;
}

export interface ScreenerResultDto {
  stocks: ScreenerStockDto[];
  totalMatched: number;
  totalEvaluated: number;
  appliedPreset: string | null;
}

export interface ScreenerStockDto {
  symbol: string;
  name: string;
  sector: string;
  signal: string | null;
  compositeScore: number | null;
  opportunityScore: number | null;
  dividendYield: number | null;
  dividendCoverage: number | null;
  peRatio: number | null;
  opportunityFlags: string[];
  rank: number;
}

// ── Macro ─────────────────────────────────────────────────────────────────────

export interface MacroDto {
  kse100Level: number | null;
  kse100ChangePct: number | null;
  sbpRate: number | null;
  pkrUsd: number | null;
  brentCrude: number | null;
  cpi: number | null;
  asOf: string;
}

// ── Portfolio ─────────────────────────────────────────────────────────────────

export interface HoldingDto {
  id: number;
  symbol: string;
  name: string;
  shares: number;
  avgCost: number;
  currentPrice: number | null;
  marketValue: number | null;
  gainLossPct: number | null;
  yieldOnCost: number | null;
  annualIncome: number | null;
  nextExDate: string | null;
  safetyScore: number | null;
}

export interface TransactionDto {
  id: number;
  symbol: string;
  action: string;
  shares: number;
  price: number;
  totalCost: number;
  transactionDate: string;
  platform: string | null;
}

export interface PortfolioSummaryDto {
  totalInvested: number;
  currentValue: number;
  totalGainLoss: number;
  totalGainLossPct: number;
  annualDividendIncome: number;
  portfolioYield: number;
  holdingCount: number;
}

// ── Pipeline ──────────────────────────────────────────────────────────────────

export interface PipelineStatusDto {
  sources: SourceStatusDto[];
  activeJob: PipelineJobStatusDto | null;
  lastComputedAt: string | null;
  lastExportedAt: string | null;
  topMissingMetrics: MissingMetricDto[];
  queueDepth: number;
}

export interface SourceStatusDto {
  name: string;
  status: string;
  lastRunAt: string | null;
  stocksScraped: number | null;
  stocksFailed: number | null;
  duration: string | null;
}

export interface PipelineJobStatusDto {
  jobType: string;
  status: string;
  currentSymbol: string | null;
  progress: number;
  total: number;
  startedAt: string;
}

export interface ScrapeRunDto {
  id: string;
  source: string;
  mode: string;
  status: string;
  startedAt: string;
  completedAt: string | null;
  symbolCount: number;
  recordsScraped: number;
  recordsFailed: number;
}

export interface MissingMetricDto {
  metricKey: string;
  affectedStockCount: number;
  reason: string;
}

export interface ScheduleConfigDto {
  dailyRunTime: string;
  enabled: boolean;
  jobs: string[];
}

export interface PagedResponseDto<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
