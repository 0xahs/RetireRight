'use client';

import { useSearchParams, useRouter } from 'next/navigation';
import { useEffect, useMemo } from 'react';
import { useQueries } from '@tanstack/react-query';
import Link from 'next/link';
import { api } from '@/lib/api/client';
import type { StockDetailDto } from '@/lib/api/types';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { formatRs, formatPct } from '@/lib/utils/format';
import { VerdictBadge } from '@/components/signal/verdict-badge';
import { ConvictionDots } from '@/components/signal/conviction-dots';
import { SafetyTier } from '@/components/signal/safety-tier';
import { WhyLine } from '@/components/signal/why-line';
import { StockLogo } from '@/components/stock-logo';
import { useCompareBasket } from '@/components/compare/compare-basket-context';
import { GitCompare, X, ArrowRight } from 'lucide-react';

export default function ComparePage() {
  const router = useRouter();
  const params = useSearchParams();
  const { symbols: basketSymbols, remove, clear } = useCompareBasket();

  // Source of truth: URL query string. Falls back to basket if URL is empty.
  const urlSyms = useMemo(() => {
    const raw = params.get('syms');
    if (!raw) return [];
    return raw
      .split(',')
      .map(s => s.trim().toUpperCase())
      .filter(Boolean)
      .slice(0, 4);
  }, [params]);

  const symbols = urlSyms.length > 0 ? urlSyms : basketSymbols;

  // If URL is empty but basket has stocks, push them into URL
  useEffect(() => {
    if (urlSyms.length === 0 && basketSymbols.length > 0) {
      router.replace(`/compare?syms=${basketSymbols.join(',')}`);
    }
  }, [urlSyms.length, basketSymbols, router]);

  const queries = useQueries({
    queries: symbols.map(sym => ({
      queryKey: ['stocks', sym, 'detail-compare'],
      queryFn: () => api.get<StockDetailDto>(`/api/stocks/${sym}`),
      staleTime: 60_000,
      enabled: symbols.length > 0,
    })),
  });

  const allLoaded = symbols.length > 0 && queries.every(q => !q.isLoading);
  const anyError = queries.some(q => q.error);
  const details = queries.map(q => q.data).filter((d): d is StockDetailDto => Boolean(d));

  if (symbols.length === 0) {
    return <EmptyState />;
  }

  if (!allLoaded) {
    return (
      <div className="space-y-5">
        <Header symbols={symbols} onRemove={remove} onClear={clear} />
        <div className="grid gap-3" style={{ gridTemplateColumns: `220px repeat(${symbols.length}, minmax(200px, 1fr))` }}>
          {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-24 rounded-sm col-span-full" />)}
        </div>
      </div>
    );
  }

  if (anyError || details.length === 0) {
    return (
      <div className="space-y-5">
        <Header symbols={symbols} onRemove={remove} onClear={clear} />
        <Card className="border-border/50">
          <CardContent className="px-6 py-8 text-center text-sm text-destructive">
            Failed to load one or more stocks. Is the API running?
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <Header symbols={symbols} onRemove={remove} onClear={clear} />
      <CompareTable details={details} />
    </div>
  );
}

/* ── Header ──────────────────────────────────────────────────────────────── */

function Header({ symbols, onRemove, onClear }: { symbols: string[]; onRemove: (s: string) => void; onClear: () => void }) {
  return (
    <div className="flex items-center justify-between gap-3 flex-wrap">
      <div>
        <h1 className="text-xl font-semibold tracking-tight flex items-center gap-2">
          <GitCompare className="h-4 w-4 text-primary" />
          Compare
        </h1>
        <p className="text-muted-foreground text-xs mt-0.5">
          {symbols.length} {symbols.length === 1 ? 'stock' : 'stocks'} side-by-side
          {symbols.length < 4 && <span className="text-muted-foreground/45"> · add up to {4 - symbols.length} more from any stock page</span>}
        </p>
      </div>
      <div className="flex items-center gap-1.5">
        {symbols.map(sym => (
          <span
            key={sym}
            className="inline-flex items-center gap-1 num font-semibold text-[12px] px-1.5 py-0.5 rounded-sm bg-primary/10 text-primary border border-primary/20"
          >
            {sym}
            <button
              onClick={() => onRemove(sym)}
              className="hover:text-foreground transition-colors cursor-pointer"
              aria-label={`Remove ${sym}`}
            >
              <X className="h-2.5 w-2.5" />
            </button>
          </span>
        ))}
        {symbols.length > 0 && (
          <button
            onClick={onClear}
            className="text-[10px] text-muted-foreground/55 hover:text-muted-foreground transition-colors px-2 py-1 cursor-pointer"
          >
            Clear
          </button>
        )}
      </div>
    </div>
  );
}

function EmptyState() {
  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-semibold tracking-tight flex items-center gap-2">
          <GitCompare className="h-4 w-4 text-primary" />
          Compare
        </h1>
        <p className="text-muted-foreground text-xs mt-0.5">Side-by-side stock comparison</p>
      </div>
      <Card className="border-border/50 card-panel">
        <CardContent className="px-6 py-12 text-center">
          <GitCompare className="h-8 w-8 text-muted-foreground/30 mx-auto mb-3" />
          <p className="text-sm text-foreground font-medium">No stocks selected for comparison</p>
          <p className="text-[12px] text-muted-foreground/60 mt-1.5">
            Open any stock page and click <span className="font-semibold text-foreground/80">Compare</span> to add up to 4 stocks here.
          </p>
          <Link
            href="/watchlist"
            className="inline-flex items-center gap-1.5 text-[11px] text-muted-foreground hover:text-primary transition-colors mt-5 px-3 py-1.5 rounded-sm border border-border/40 hover:border-primary/40"
          >
            Browse watchlist <ArrowRight className="h-3 w-3" />
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}

/* ── Compare table ──────────────────────────────────────────────────────── */

function CompareTable({ details }: { details: StockDetailDto[] }) {
  const anyBanking = details.some(d => d.metrics.banking != null);
  const cols = details.length;

  // Helper: format a row of values, highlight best.
  return (
    <div className="space-y-6">
      {/* Identity row */}
      <div
        className="grid gap-3"
        style={{ gridTemplateColumns: `220px repeat(${cols}, minmax(220px, 1fr))` }}
      >
        <div />
        {details.map(d => (
          <Card key={d.summary.symbol} className="border-border/50 card-panel">
            <CardContent className="p-4 space-y-3">
              <div className="flex items-center gap-2.5">
                <StockLogo symbol={d.summary.symbol} size={32} />
                <div className="min-w-0 flex-1">
                  <Link
                    href={`/stock/${d.summary.symbol}`}
                    className="num font-bold text-[15px] tracking-tight hover:text-primary transition-colors"
                  >
                    {d.summary.symbol}
                  </Link>
                  <div className="text-[10px] text-muted-foreground/55 truncate">{d.summary.name}</div>
                </div>
              </div>
              <div className="flex items-center justify-between gap-2 flex-wrap">
                <VerdictBadge verdict={d.summary.verdict} size="md" />
                <ConvictionDots level={d.summary.convictionLevel} confidence={d.summary.confidence} />
              </div>
              <div className="flex items-baseline justify-between pt-1.5 border-t border-border/30">
                <span className="num text-[18px] font-bold tracking-tight">
                  {formatRs(d.summary.price)}
                </span>
                {d.summary.changePct != null && (
                  <span
                    className={cn(
                      'num text-[12px]',
                      d.summary.changePct >= 0 ? 'text-emerald-500' : 'text-red-500',
                    )}
                  >
                    {d.summary.changePct >= 0 ? '+' : ''}
                    {formatPct(d.summary.changePct)}
                  </span>
                )}
              </div>
              <WhyLine why={d.summary.whyLine} risks={d.summary.risks} />
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Scores section */}
      <Section title="Scores">
        <Row label="Composite" cols={cols} values={details.map(d => d.summary.compositeScore)} fmt={fmtScore} better="high" />
        <Row label="Safety" cols={cols} values={details.map(d => d.summary.dividendSafetyScore)} fmt={fmtScore} better="high" />
        <Row label="Growth" cols={cols} values={details.map(d => d.summary.dividendGrowthScore)} fmt={fmtScore} better="high" />
        <Row label="Valuation" cols={cols} values={details.map(d => d.summary.valuationScore)} fmt={fmtScore} better="high" />
        <Row label="Opportunity" cols={cols} values={details.map(d => d.summary.opportunityScore)} fmt={fmtScore} better="high" />
        <Row label="Safety Tier" cols={cols} values={details.map(d => d.summary.safetyTier)} fmt={s => String(s ?? '—')} better="custom" customRender={(v) => <SafetyTier tier={v as any} />} />
        <Row label="Conviction" cols={cols} values={details.map(d => d.summary.convictionLevel)} fmt={(v) => String(v ?? '—')} better="custom" customRender={(v) => <ConvictionDots level={v as any} showLabel />} />
      </Section>

      {/* Dividend section */}
      <Section title="Dividend">
        <Row label="Gross yield" cols={cols} values={details.map(d => d.summary.dividendYield)} fmt={fmtPct} better="high" />
        <Row label="Net yield (post-WHT)" cols={cols} values={details.map(d => d.summary.netYield)} fmt={fmtPct} better="high" />
        <Row label="Coverage" cols={cols} values={details.map(d => d.summary.dividendCoverage)} fmt={v => v != null ? `${v.toFixed(2)}x` : '—'} better="high" />
        <Row label="Payout ratio" cols={cols} values={details.map(d => d.metrics.dividendSafety.earningsPayoutRatio)} fmt={v => v != null ? `${(v * 100).toFixed(0)}%` : '—'} better="low" />
        <Row label="Consecutive years" cols={cols} values={details.map(d => d.metrics.dividendSafety.consecutiveDivYears)} fmt={v => v != null ? `${Math.round(v)}` : '—'} better="high" />
      </Section>

      {/* Valuation section */}
      <Section title="Valuation">
        <Row label="P/E" cols={cols} values={details.map(d => d.metrics.valuation.peRatio)} fmt={v => v != null ? `${v.toFixed(1)}x` : '—'} better="low" />
        <Row label="P/B" cols={cols} values={details.map(d => d.metrics.valuation.pbRatio)} fmt={v => v != null ? `${v.toFixed(2)}x` : '—'} better="low" />
        <Row label="Margin of safety" cols={cols} values={details.map(d => d.metrics.valuation.marginOfSafety)} fmt={v => v != null ? `${v.toFixed(0)}%` : '—'} better="high" />
        <Row label="Earnings yield" cols={cols} values={details.map(d => d.metrics.valuation.earningsYield)} fmt={fmtPct} better="high" />
      </Section>

      {/* Risk section */}
      <Section title="Risk & Technical">
        <Row label="RSI (14)" cols={cols} values={details.map(d => d.metrics.risk.rsi14)} fmt={v => v != null ? v.toFixed(0) : '—'} better="neutral" />
        <Row label="Drawdown from 52W" cols={cols} values={details.map(d => d.metrics.opportunity.drawdownFrom52wHigh)} fmt={fmtPct} better="neutral" />
        <Row label="52W position" cols={cols} values={details.map(d => d.metrics.opportunity.rangePosition52w)} fmt={v => v != null ? `${v.toFixed(0)}%` : '—'} better="neutral" />
      </Section>

      {/* Banking section (only if any bank in comparison) */}
      {anyBanking && (
        <Section title="Banking Metrics" subtitle="Only applies to Banking / IslamicBanking sectors">
          <Row label="NIM" cols={cols} values={details.map(d => d.metrics.banking?.nim ?? null)} fmt={fmtPct} better="high" />
          <Row label="Cost to income" cols={cols} values={details.map(d => d.metrics.banking?.costToIncome ?? null)} fmt={fmtPct} better="low" />
          <Row label="NPL ratio" cols={cols} values={details.map(d => d.metrics.banking?.nplRatio ?? null)} fmt={fmtPct} better="low" />
          <Row label="CAR" cols={cols} values={details.map(d => d.metrics.banking?.car ?? null)} fmt={fmtPct} better="high" />
          <Row label="Tier 1" cols={cols} values={details.map(d => d.metrics.banking?.tier1Ratio ?? null)} fmt={fmtPct} better="high" />
          <Row label="ROE" cols={cols} values={details.map(d => d.metrics.banking?.roe ?? null)} fmt={fmtPct} better="high" />
        </Section>
      )}
    </div>
  );
}

function Section({ title, subtitle, children }: { title: string; subtitle?: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="mb-2 px-1">
        <h2 className="text-[10px] uppercase tracking-widest text-muted-foreground/60 font-semibold">{title}</h2>
        {subtitle && <p className="text-[10px] text-muted-foreground/40 mt-0.5">{subtitle}</p>}
      </div>
      <Card className="border-border/50 card-panel overflow-hidden">
        <CardContent className="p-0">
          <div className="divide-y divide-border/30">{children}</div>
        </CardContent>
      </Card>
    </div>
  );
}

type Better = 'high' | 'low' | 'neutral' | 'custom';

interface RowProps<T> {
  label: string;
  cols: number;
  values: T[];
  fmt: (value: T) => string;
  better: Better;
  customRender?: (value: T) => React.ReactNode;
}

function Row<T>({ label, cols, values, fmt, better, customRender }: RowProps<T>) {
  // Compute best/worst index for highlighting (numeric values only)
  const numeric = values.map(v => (typeof v === 'number' ? v : null));
  const valid = numeric.filter((v): v is number => v != null);
  const allEqual = valid.length > 0 && valid.every(v => v === valid[0]);
  let bestIdx = -1;
  let worstIdx = -1;
  if (better !== 'neutral' && better !== 'custom' && valid.length > 1 && !allEqual) {
    if (better === 'high') {
      bestIdx = numeric.indexOf(Math.max(...valid));
      worstIdx = numeric.indexOf(Math.min(...valid));
    } else {
      bestIdx = numeric.indexOf(Math.min(...valid));
      worstIdx = numeric.indexOf(Math.max(...valid));
    }
  }

  return (
    <div
      className="grid gap-3 hover:bg-primary/[0.03] transition-colors"
      style={{ gridTemplateColumns: `220px repeat(${cols}, minmax(220px, 1fr))` }}
    >
      <div className="px-4 py-2.5 text-[11px] text-muted-foreground/70 font-medium">{label}</div>
      {values.map((v, i) => (
        <div
          key={i}
          className={cn(
            'px-4 py-2.5 num text-[13px] font-semibold tabular-nums',
            i === bestIdx && 'text-emerald-400',
            i === worstIdx && 'text-red-400/85',
            bestIdx !== i && worstIdx !== i && 'text-foreground',
          )}
        >
          {customRender ? customRender(v) : fmt(v)}
        </div>
      ))}
    </div>
  );
}

function fmtScore(v: number | null): string {
  if (v == null) return '—';
  return `${Math.round(v)}`;
}

function fmtPct(v: number | null): string {
  if (v == null) return '—';
  return `${v.toFixed(1)}%`;
}
