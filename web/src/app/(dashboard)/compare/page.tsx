'use client';

import { useSearchParams, useRouter } from 'next/navigation';
import { useEffect, useMemo } from 'react';
import { useQueries } from '@tanstack/react-query';
import Link from 'next/link';
import { api } from '@/lib/api/client';
import type { StockDetailDto } from '@/lib/api/types';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { formatRs, formatPct } from '@/lib/utils/format';
import { VerdictBadge } from '@/components/signal/verdict-badge';
import { ConvictionDots } from '@/components/signal/conviction-dots';
import { SafetyTier } from '@/components/signal/safety-tier';
import { WhyLine } from '@/components/signal/why-line';
import { StockLogo } from '@/components/stock-logo';
import { useCompareBasket } from '@/components/compare/compare-basket-context';

export default function ComparePage() {
  const router = useRouter();
  const params = useSearchParams();
  const { symbols: basketSymbols, remove, clear } = useCompareBasket();

  const urlSyms = useMemo(() => {
    const raw = params.get('syms');
    if (!raw) return [];
    return raw.split(',').map(s => s.trim().toUpperCase()).filter(Boolean).slice(0, 4);
  }, [params]);

  const symbols = urlSyms.length > 0 ? urlSyms : basketSymbols;

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

  if (symbols.length === 0) return <EmptyState />;

  if (!allLoaded) {
    return (
      <div className="space-y-4">
        <Header symbols={symbols} onRemove={remove} onClear={clear} />
        {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-24" style={{ borderRadius: 0 }} />)}
      </div>
    );
  }

  if (anyError || details.length === 0) {
    return (
      <div className="space-y-4">
        <Header symbols={symbols} onRemove={remove} onClear={clear} />
        <div className="panel px-5 py-8 text-center">
          <p className="text-warn text-[11px] uppercase tracking-[0.18em] font-bold">
            ✕ FAILED TO LOAD · IS API ALIVE?
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <Header symbols={symbols} onRemove={remove} onClear={clear} />
      <CompareTable details={details} />
    </div>
  );
}

function Header({ symbols, onRemove, onClear }: { symbols: string[]; onRemove: (s: string) => void; onClear: () => void }) {
  return (
    <div className="flex items-baseline justify-between gap-3 flex-wrap">
      <div>
        <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
          ┌── COMPARE
        </div>
        <h1 className="text-foreground font-bold text-[20px] tracking-tight">
          SIDE-BY-SIDE · {symbols.length} INSTRUMENT{symbols.length === 1 ? '' : 'S'}
        </h1>
      </div>
      <div className="flex items-center gap-1.5">
        {symbols.map(sym => (
          <span
            key={sym}
            className="inline-flex items-center gap-1.5 num font-bold text-[11px] px-2 py-1 border border-border bg-card text-foreground tracking-[0.05em]"
          >
            {sym}
            <button
              onClick={() => onRemove(sym)}
              className="text-warm-dim hover:text-warn transition-colors cursor-pointer text-[9px] font-bold"
              aria-label={`Remove ${sym}`}
            >
              ✕
            </button>
          </span>
        ))}
        {symbols.length > 0 && (
          <button
            onClick={onClear}
            className="text-[9px] uppercase tracking-[0.18em] text-warm-dim hover:text-warn transition-colors px-2 py-1 cursor-pointer font-bold border border-border bg-card"
          >
            CLEAR
          </button>
        )}
      </div>
    </div>
  );
}

function EmptyState() {
  return (
    <div className="space-y-4">
      <div>
        <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
          ┌── COMPARE
        </div>
        <h1 className="text-foreground font-bold text-[20px] tracking-tight">SIDE-BY-SIDE</h1>
      </div>
      <div className="panel px-6 py-12 text-center">
        <div className="text-amber text-[11px] font-bold tracking-[0.25em] uppercase mb-3">
          ░░░░░ EMPTY ░░░░░
        </div>
        <p className="text-foreground font-bold text-[14px] tracking-tight">
          NO INSTRUMENTS SELECTED
        </p>
        <p className="text-[11px] text-warm-muted mt-2 uppercase tracking-[0.12em] font-medium">
          ADD UP TO 4 STOCKS VIA THE [+ COMPARE] BUTTON
        </p>
        <Link
          href="/watchlist"
          className="inline-flex items-center gap-1.5 text-[10px] uppercase tracking-[0.18em] font-bold text-amber hover:text-amber/80 transition-colors mt-5 px-3 py-1.5 border border-primary/50 hover:border-primary"
        >
          [ BROWSE WATCHLIST › ]
        </Link>
      </div>
    </div>
  );
}

function CompareTable({ details }: { details: StockDetailDto[] }) {
  const anyBanking = details.some(d => d.metrics.banking != null);
  const cols = details.length;

  return (
    <div className="space-y-4">
      {/* Identity panels — terminal cards */}
      <div className="grid gap-3" style={{ gridTemplateColumns: `180px repeat(${cols}, minmax(220px, 1fr))` }}>
        <div />
        {details.map(d => (
          <div key={d.summary.symbol} className="panel">
            <div className="panel-strip px-3 py-1.5 flex items-center justify-between">
              <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ {d.summary.symbol}</span>
              <VerdictBadge verdict={d.summary.verdict} size="sm" />
            </div>
            <div className="px-3 py-3">
              <div className="flex items-center gap-2.5">
                <StockLogo symbol={d.summary.symbol} size={28} />
                <div className="min-w-0">
                  <Link
                    href={`/stock/${d.summary.symbol}`}
                    className="num font-bold text-[15px] text-foreground tracking-tight hover:text-amber transition-colors"
                  >
                    {d.summary.symbol}
                  </Link>
                  <div className="text-[9px] text-warm-dim tracking-[0.12em] uppercase font-medium truncate">{d.summary.name}</div>
                </div>
              </div>
              <div className="flex items-center justify-between gap-2 mt-3">
                <ConvictionDots level={d.summary.convictionLevel} confidence={d.summary.confidence} showLabel />
                <SafetyTier tier={d.summary.safetyTier} />
              </div>
              <div className="flex items-baseline justify-between pt-3 mt-2 border-t border-border">
                <span className="num text-[18px] font-bold tracking-tight text-foreground">
                  {formatRs(d.summary.price)}
                </span>
                {d.summary.changePct != null && (
                  <span className={cn(
                    'num text-[11px] font-bold tracking-tight',
                    d.summary.changePct >= 0 ? 'text-cyan-pos' : 'text-warn',
                  )}>
                    {d.summary.changePct >= 0 ? '▲' : '▼'}{Math.abs(d.summary.changePct).toFixed(2)}%
                  </span>
                )}
              </div>
              <div className="mt-3">
                <WhyLine why={d.summary.whyLine} risks={d.summary.risks} />
              </div>
            </div>
          </div>
        ))}
      </div>

      <Section title="SCORES">
        <Row label="COMPOSITE" cols={cols} values={details.map(d => d.summary.compositeScore)} fmt={fmtScore} better="high" />
        <Row label="SAFETY" cols={cols} values={details.map(d => d.summary.dividendSafetyScore)} fmt={fmtScore} better="high" />
        <Row label="GROWTH" cols={cols} values={details.map(d => d.summary.dividendGrowthScore)} fmt={fmtScore} better="high" />
        <Row label="VALUATION" cols={cols} values={details.map(d => d.summary.valuationScore)} fmt={fmtScore} better="high" />
        <Row label="OPPORTUNITY" cols={cols} values={details.map(d => d.summary.opportunityScore)} fmt={fmtScore} better="high" />
      </Section>

      <Section title="DIVIDEND">
        <Row label="GROSS YIELD" cols={cols} values={details.map(d => d.summary.dividendYield)} fmt={fmtPct} better="high" />
        <Row label="NET YIELD (WHT)" cols={cols} values={details.map(d => d.summary.netYield)} fmt={fmtPct} better="high" />
        <Row label="COVERAGE" cols={cols} values={details.map(d => d.summary.dividendCoverage)} fmt={v => v != null ? `${v.toFixed(2)}X` : '—'} better="high" />
        <Row label="PAYOUT RATIO" cols={cols} values={details.map(d => d.metrics.dividendSafety.earningsPayoutRatio)} fmt={v => v != null ? `${(v * 100).toFixed(0)}%` : '—'} better="low" />
        <Row label="CONSECUTIVE YEARS" cols={cols} values={details.map(d => d.metrics.dividendSafety.consecutiveDivYears)} fmt={v => v != null ? `${Math.round(v)}` : '—'} better="high" />
      </Section>

      <Section title="VALUATION">
        <Row label="P/E" cols={cols} values={details.map(d => d.metrics.valuation.peRatio)} fmt={v => v != null ? `${v.toFixed(1)}X` : '—'} better="low" />
        <Row label="P/B" cols={cols} values={details.map(d => d.metrics.valuation.pbRatio)} fmt={v => v != null ? `${v.toFixed(2)}X` : '—'} better="low" />
        <Row label="MARGIN OF SAFETY" cols={cols} values={details.map(d => d.metrics.valuation.marginOfSafety)} fmt={v => v != null ? `${v.toFixed(0)}%` : '—'} better="high" />
        <Row label="EARNINGS YIELD" cols={cols} values={details.map(d => d.metrics.valuation.earningsYield)} fmt={fmtPct} better="high" />
      </Section>

      <Section title="RISK · TECHNICAL">
        <Row label="RSI (14)" cols={cols} values={details.map(d => d.metrics.risk.rsi14)} fmt={v => v != null ? v.toFixed(0) : '—'} better="neutral" />
        <Row label="DRAWDOWN 52W" cols={cols} values={details.map(d => d.metrics.opportunity.drawdownFrom52wHigh)} fmt={fmtPct} better="neutral" />
        <Row label="52W POSITION" cols={cols} values={details.map(d => d.metrics.opportunity.rangePosition52w)} fmt={v => v != null ? `${v.toFixed(0)}%` : '—'} better="neutral" />
      </Section>

      {anyBanking && (
        <Section title="BANKING · H-METRICS" subtitle="BANKING / ISLAMICBANKING SECTORS ONLY">
          <Row label="NIM" cols={cols} values={details.map(d => d.metrics.banking?.nim ?? null)} fmt={fmtPct} better="high" />
          <Row label="COST TO INCOME" cols={cols} values={details.map(d => d.metrics.banking?.costToIncome ?? null)} fmt={fmtPct} better="low" />
          <Row label="NPL RATIO" cols={cols} values={details.map(d => d.metrics.banking?.nplRatio ?? null)} fmt={fmtPct} better="low" />
          <Row label="CAR" cols={cols} values={details.map(d => d.metrics.banking?.car ?? null)} fmt={fmtPct} better="high" />
          <Row label="TIER 1" cols={cols} values={details.map(d => d.metrics.banking?.tier1Ratio ?? null)} fmt={fmtPct} better="high" />
          <Row label="ROE" cols={cols} values={details.map(d => d.metrics.banking?.roe ?? null)} fmt={fmtPct} better="high" />
        </Section>
      )}
    </div>
  );
}

function Section({ title, subtitle, children }: { title: string; subtitle?: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="mb-1.5 px-1 flex items-baseline gap-2">
        <h2 className="text-[10px] uppercase tracking-[0.22em] text-amber font-bold">▌ {title}</h2>
        {subtitle && <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">{subtitle}</span>}
      </div>
      <div className="panel">
        <div className="divide-y divide-border">{children}</div>
      </div>
    </div>
  );
}

type Better = 'high' | 'low' | 'neutral';

interface RowProps<T> {
  label: string;
  cols: number;
  values: T[];
  fmt: (value: T) => string;
  better: Better;
}

function Row<T>({ label, cols, values, fmt, better }: RowProps<T>) {
  const numeric = values.map(v => (typeof v === 'number' ? v : null));
  const valid = numeric.filter((v): v is number => v != null);
  const allEqual = valid.length > 0 && valid.every(v => v === valid[0]);
  let bestIdx = -1;
  let worstIdx = -1;
  if (better !== 'neutral' && valid.length > 1 && !allEqual) {
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
      className="grid hover:bg-white/[0.025] transition-colors num"
      style={{ gridTemplateColumns: `180px repeat(${cols}, minmax(220px, 1fr))` }}
    >
      <div className="px-4 py-2 text-[10px] uppercase tracking-[0.15em] text-warm-muted font-bold border-r border-border">{label}</div>
      {values.map((v, i) => (
        <div
          key={i}
          className={cn(
            'px-4 py-2 text-[12px] font-bold tabular-nums border-r border-border last:border-r-0',
            i === bestIdx && 'text-cyan-pos',
            i === worstIdx && 'text-warn',
            bestIdx !== i && worstIdx !== i && 'text-foreground',
          )}
        >
          {fmt(v)}
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
