import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type {
  StockSummaryDto, StockDetailDto, StockMetricsDto, StockMemoDto,
  DividendSummaryDto, UpcomingDividendDto, PricePointDto, SignalHistoryDto,
  ScreenerRequestDto, ScreenerResultDto,
  MacroDto,
  HoldingDto, TransactionDto, PortfolioSummaryDto,
  AddTransactionRequest, UpdateHoldingRequest,
  PipelineStatusDto, ScrapeRunDto, PagedResponseDto,
  ScheduleConfigDto,
} from './types';

// ── Keys ──────────────────────────────────────────────────────────────────────

export const queryKeys = {
  stocks: ['stocks'] as const,
  stock: (symbol: string) => ['stocks', symbol] as const,
  stockMetrics: (symbol: string) => ['stocks', symbol, 'metrics'] as const,
  stockDividends: (symbol: string) => ['stocks', symbol, 'dividends'] as const,
  stockPriceHistory: (symbol: string, from?: string, to?: string) =>
    ['stocks', symbol, 'price-history', from, to] as const,
  upcomingDividends: (days: number) => ['dividends', 'upcoming', days] as const,
  macro: ['macro'] as const,
  screenerPresets: ['screener', 'presets'] as const,
  holdings: ['portfolio', 'holdings'] as const,
  transactions: ['portfolio', 'transactions'] as const,
  portfolioSummary: ['portfolio', 'summary'] as const,
  pipelineStatus: ['pipeline', 'status'] as const,
  pipelineRuns: (page: number) => ['pipeline', 'runs', page] as const,
  scheduleConfig: ['settings', 'schedule'] as const,
};

// ── Stocks ────────────────────────────────────────────────────────────────────

export function useStocks() {
  return useQuery({
    queryKey: queryKeys.stocks,
    queryFn: () => api.get<StockSummaryDto[]>('/api/stocks'),
    staleTime: 60_000,
  });
}

export function useStock(symbol: string) {
  return useQuery({
    queryKey: queryKeys.stock(symbol),
    queryFn: () => api.get<StockDetailDto>(`/api/stocks/${symbol}`),
    staleTime: 60_000,
    enabled: !!symbol,
  });
}

export function useStockMetrics(symbol: string) {
  return useQuery({
    queryKey: queryKeys.stockMetrics(symbol),
    queryFn: () => api.get<StockMetricsDto>(`/api/stocks/${symbol}/metrics`),
    staleTime: 300_000,
    enabled: !!symbol,
  });
}

export function useStockDividends(symbol: string) {
  return useQuery({
    queryKey: queryKeys.stockDividends(symbol),
    queryFn: () => api.get<DividendSummaryDto[]>(`/api/stocks/${symbol}/dividends`),
    staleTime: 300_000,
    enabled: !!symbol,
  });
}

export function useStockMemo(symbol: string) {
  return useQuery({
    queryKey: ['stocks', symbol, 'memo'] as const,
    queryFn: () => api.get<StockMemoDto>(`/api/stocks/${symbol}/memo`),
    staleTime: 300_000,
    enabled: !!symbol,
  });
}

export function useSignalHistory(symbol: string, days = 30) {
  return useQuery({
    queryKey: ['stocks', symbol, 'signal-history', days] as const,
    queryFn: () => api.get<SignalHistoryDto>(`/api/stocks/${symbol}/signal-history?days=${days}`),
    staleTime: 300_000,
    enabled: !!symbol,
  });
}

export function useStockPriceHistory(symbol: string, from?: string, to?: string) {
  const params = new URLSearchParams();
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  const qs = params.size > 0 ? `?${params}` : '';
  return useQuery({
    queryKey: queryKeys.stockPriceHistory(symbol, from, to),
    queryFn: () => api.get<PricePointDto[]>(`/api/stocks/${symbol}/price-history${qs}`),
    staleTime: 300_000,
    enabled: !!symbol,
  });
}

// ── Dividends ─────────────────────────────────────────────────────────────────

export function useUpcomingDividends(days = 60) {
  return useQuery({
    queryKey: queryKeys.upcomingDividends(days),
    queryFn: () => api.get<UpcomingDividendDto[]>(`/api/dividends/upcoming?days=${days}`),
    staleTime: 300_000,
  });
}

// ── Macro ─────────────────────────────────────────────────────────────────────

export function useMacro() {
  return useQuery({
    queryKey: queryKeys.macro,
    queryFn: () => api.get<MacroDto>('/api/macro'),
    staleTime: 300_000,
  });
}

// ── Screener ──────────────────────────────────────────────────────────────────

export function useScreenerPresets() {
  return useQuery({
    queryKey: queryKeys.screenerPresets,
    queryFn: () => api.get<string[]>('/api/screener/presets'),
    staleTime: Infinity,
  });
}

export function useRunScreener() {
  return useMutation({
    mutationFn: (req: ScreenerRequestDto) =>
      api.post<ScreenerResultDto>('/api/screener/run', req),
  });
}

// ── Portfolio ─────────────────────────────────────────────────────────────────

export function useHoldings() {
  return useQuery({
    queryKey: queryKeys.holdings,
    queryFn: () => api.get<HoldingDto[]>('/api/portfolio/holdings'),
    staleTime: 60_000,
  });
}

export function useTransactions() {
  return useQuery({
    queryKey: queryKeys.transactions,
    queryFn: () => api.get<TransactionDto[]>('/api/portfolio/transactions'),
    staleTime: 300_000,
  });
}

export function usePortfolioSummary() {
  return useQuery({
    queryKey: queryKeys.portfolioSummary,
    queryFn: () => api.get<PortfolioSummaryDto>('/api/portfolio/summary'),
    staleTime: 60_000,
  });
}

export function useAddTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: AddTransactionRequest) =>
      api.post<TransactionDto>('/api/portfolio/transactions', req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.holdings });
      qc.invalidateQueries({ queryKey: queryKeys.transactions });
      qc.invalidateQueries({ queryKey: queryKeys.portfolioSummary });
    },
  });
}

export function useUpdateHolding() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, req }: { id: number; req: UpdateHoldingRequest }) =>
      api.put<void>(`/api/portfolio/holdings/${id}`, req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.holdings });
      qc.invalidateQueries({ queryKey: queryKeys.portfolioSummary });
    },
  });
}

export function useDeleteHolding() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => api.delete<void>(`/api/portfolio/holdings/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.holdings });
      qc.invalidateQueries({ queryKey: queryKeys.portfolioSummary });
    },
  });
}

// ── Pipeline ──────────────────────────────────────────────────────────────────

export function usePipelineStatus() {
  return useQuery({
    queryKey: queryKeys.pipelineStatus,
    queryFn: () => api.get<PipelineStatusDto>('/api/pipeline/status'),
    refetchInterval: 5_000,
  });
}

export function usePipelineRuns(page = 1) {
  return useQuery({
    queryKey: queryKeys.pipelineRuns(page),
    queryFn: () => api.get<PagedResponseDto<ScrapeRunDto>>(`/api/pipeline/runs?page=${page}`),
    staleTime: 30_000,
  });
}

export function useTriggerScrape() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (opts?: { symbols?: string[]; source?: string }) =>
      api.post<{ runId: string; queued: boolean }>('/api/pipeline/scrape', opts ?? {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.pipelineStatus });
      qc.invalidateQueries({ queryKey: ['pipeline', 'runs'] });
    },
  });
}

export function useTriggerCompute() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (symbols?: string[]) =>
      api.post<{ queued: boolean }>('/api/pipeline/compute', { symbols }),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.pipelineStatus }),
  });
}

export function useTriggerExport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<{ queued: boolean }>('/api/export/run', {}),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.pipelineStatus }),
  });
}

// ── Settings ──────────────────────────────────────────────────────────────────

export function useScheduleConfig() {
  return useQuery({
    queryKey: queryKeys.scheduleConfig,
    queryFn: () => api.get<ScheduleConfigDto>('/api/settings/schedule'),
    staleTime: Infinity,
  });
}

export function useUpdateScheduleConfig() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (config: ScheduleConfigDto) =>
      api.put<ScheduleConfigDto>('/api/settings/schedule', config),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.scheduleConfig }),
  });
}
