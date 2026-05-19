// ── Stocks ────────────────────────────────────────────────────────────────────

export type Verdict = 'ACCUMULATE' | 'HOLD' | 'REVIEW' | 'AVOID' | 'WATCH';
export type ConvictionLevel = 'HIGH' | 'MEDIUM' | 'LOW';
export type SafetyTier = 'S' | 'A' | 'B' | 'C' | 'D' | '-';

export interface StockListItemDto {
  id: number;
  symbol: string;
  name: string;
  sector: string;
  isWatchlist: boolean;
  isActive: boolean;
}

export interface StockSummaryDto {
  id: number;
  symbol: string;
  name: string;
  sector: string;
  price: number | null;
  changePct: number | null;
  compositeScore: number | null;
  dividendSafetyScore: number | null;
  dividendGrowthScore: number | null;
  valuationScore: number | null;
  opportunityScore: number | null;
  signal: string | null;
  dividendYield: number | null;
  dividendCoverage: number | null;
  opportunityFlags: string[];
  scrapedAt: string | null;
  computedAt: string | null;
  // Signal grammar (CapyTrade-evolution layer)
  verdict: Verdict;
  confidence: number;          // 0..1
  convictionLevel: ConvictionLevel;
  safetyTier: SafetyTier;
  netYield: number | null;     // gross × 0.85
  whyLine: string | null;
  risks: string | null;
  metricCompleteness: number;  // 0..100
  metricsTotal: number;
}

export interface ReturnMetrics {
  return1M: number | null;
  return6M: number | null;
  returnYtd: number | null;
  return1Y: number | null;
  return5Y: number | null;
  return10Y: number | null;
}

export interface CompanyProfileDto {
  marketCapM: number | null;
  netCashDebtM: number | null;
  employees: number | null;
  foundedYear: number | null;
  week52Low: number | null;
  week52High: number | null;
}

export interface StockDetailDto {
  summary: StockSummaryDto;
  metrics: StockMetricsDto;
  recentDividends: DividendSummaryDto[];
  priceHistory: PricePointDto[];
  returns: ReturnMetrics | null;
  profile: CompanyProfileDto | null;
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

export interface SignalHistoryDto {
  symbol: string;
  days: number;
  points: SignalHistoryPointDto[];
}

export interface SignalHistoryPointDto {
  date: string;
  verdict: Verdict;
  composite: number | null;
  safety: number | null;
  convictionLevel: ConvictionLevel;
}

export interface StockMemoDto {
  symbol: string;
  verdict: Verdict;
  generatedAt: string;
  dataAsOf: string | null;
  sourceConfidence: string;
  metricsPopulated: number;
  metricsTotal: number;
  snapshot: MemoSection;
  thesis: MemoSection;
  risks: MemoSection;
  valuation: MemoSection;
  verdictSection: MemoSection;
  claudeEnvelope: ClaudeEnvelopeDto;
}

export interface MemoSection {
  title: string;
  lines: MemoLine[];
}

export interface MemoLine {
  text: string;
  citations: string[];
  isOpinion: boolean;
}

export interface ClaudeEnvelopeDto {
  json: string;
  estimatedTokens: number;
  sizeBytes: number;
}

export interface MonthlyPlanDto {
  budget: number;
  generatedAt: string;
  picks: PlanPickDto[];
  summary: PlanSummaryDto;
  notes: string[];
}

export interface PlanPickDto {
  symbol: string;
  name: string;
  sector: string;
  verdict: Verdict;
  convictionLevel: ConvictionLevel;
  compositeScore: number | null;
  opportunityScore: number | null;
  currentPrice: number | null;
  shares: number;
  allocatedAmount: number;
  targetAmount: number;
  weight: number;
  expectedAnnualIncome: number | null;
  netYield: number | null;
  exDateInDays: number | null;
  reasons: string[];
}

export interface PlanSummaryDto {
  totalAllocated: number;
  leftover: number;
  pickCount: number;
  expectedAnnualIncome: number;
  averageNetYield: number | null;
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
  dpsYoyGrowth: number | null;
  dpsCagr3y: number | null;
  dpsCagr5y: number | null;
  epsGrowth1y: number | null;
  revenueGrowth1y: number | null;
  bookValueGrowth: number | null;
  roeTrend: number | null;
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
  volatility1y: number | null;
  maxDrawdown: number | null;
  priceVs50dma: number | null;
  priceVs200dma: number | null;
  currentRatio: number | null;
  quickRatio: number | null;
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

// ── Sectors ───────────────────────────────────────────────────────────────────

export interface SectorTopPickDto {
  symbol: string;
  name: string;
  price: number | null;
  compositeScore: number | null;
  dividendYield: number | null;
  verdict: Verdict;
  convictionLevel: ConvictionLevel;
  safetyTier: SafetyTier;
}

export interface SectorSummaryDto {
  sector: string;
  stockCount: number;
  avgCompositeScore: number | null;
  avgDividendYield: number | null;
  avgPeRatio: number | null;
  avgOpportunityScore: number | null;
  topPicks: SectorTopPickDto[];
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
  sector: string;
  shares: number;
  avgCost: number;
  currentPrice: number | null;
  marketValue: number | null;
  costBasis: number | null;
  gainLoss: number | null;
  gainLossPct: number | null;
  yieldOnCost: number | null;
  annualIncome: number | null;
  nextExDate: string | null;
  safetyScore: number | null;
  platform: string | null;
  purchaseDate: string | null;
}

export interface TransactionDto {
  id: number;
  symbol: string;
  name: string;
  action: string;
  shares: number;
  price: number;
  totalCost: number;
  commission: number | null;
  transactionDate: string;
  platform: string | null;
}

export interface PortfolioSummaryDto {
  totalCostBasis: number;
  totalMarketValue: number | null;
  totalGainLoss: number | null;
  totalGainLossPct: number | null;
  projectedAnnualIncome: number | null;
  portfolioYield: number | null;
  holdingCount: number;
}

export interface AddTransactionRequest {
  symbol: string;
  action: 'BUY' | 'SELL';
  shares: number;
  price: number;
  commission?: number;
  platform?: string;
  transactionDate?: string;
}

export interface UpdateHoldingRequest {
  shares: number;
  avgCost: number;
  platform?: string;
  purchaseDate?: string;
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
  key: string;
  description: string;
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

export interface ScraperConfigDto {
  name: string;
  type: string;
  description: string;
  baseUrl: string;
  enabled: boolean;
  maxRequestsPerMinute: number;
  minDelayMs: number;
  maxDelayMs: number;
  retryMaxAttempts: number;
  retryInitialDelayMs: number;
  retryBackoffMultiplier: number;
  hasCredentials: boolean;
}

export interface ScraperConfigPatchRequest {
  enabled?: boolean;
  description?: string;
  baseUrl?: string;
  maxRequestsPerMinute?: number;
  minDelayMs?: number;
  maxDelayMs?: number;
  retryMaxAttempts?: number;
}
