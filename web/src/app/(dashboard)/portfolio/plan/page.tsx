'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useMonthlyPlan } from '@/lib/api/queries';
import { formatRs, scoreTone, yieldTone } from '@/lib/utils/format';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import type { PlanPickDto } from '@/lib/api/types';
import { VerdictBadge } from '@/components/signal/verdict-badge';
import { ConvictionDots } from '@/components/signal/conviction-dots';
import { StockLogo } from '@/components/stock-logo';
import { CompareButton } from '@/components/compare/compare-sticky-bar';

const PRESET_BUDGETS = [6000, 10000, 12000, 15000, 25000, 50000];

export default function MonthlyPlanPage() {
  const [budget, setBudget] = useState(12000);
  const [inputValue, setInputValue] = useState('12000');
  const { data, isLoading, error, refetch } = useMonthlyPlan(budget);

  function applyBudget(value: number) {
    if (value <= 0) return;
    setBudget(value);
    setInputValue(String(value));
  }

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    const n = Number(inputValue.replace(/[^\d.]/g, ''));
    if (Number.isFinite(n) && n > 0) applyBudget(n);
  }

  return (
    <div className="space-y-4">
      {/* Page header */}
      <div className="flex items-baseline justify-between gap-3 flex-wrap">
        <div>
          <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
            ┌── PORTFOLIO · MONTHLY PLAN
          </div>
          <h1 className="text-foreground font-bold text-[20px] tracking-tight">
            BUDGET ALLOCATOR · WATCHLIST PICKS
          </h1>
          <p className="text-warm-muted text-[11px] mt-1 uppercase tracking-[0.05em]">
            DETERMINISTIC RANKING USING SIGNAL GRAMMAR + PORTFOLIO BALANCE + EX-DATE TIMING
          </p>
        </div>
        <Link
          href="/portfolio"
          className="text-[10px] uppercase tracking-[0.18em] font-bold px-3 py-1.5 border border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50 transition-colors"
        >
          ← HOLDINGS
        </Link>
      </div>

      {/* Budget input */}
      <div className="panel">
        <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ THIS MONTH&apos;S BUDGET</span>
          <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold num">
            PKR · WHOLE SHARES
          </span>
        </div>
        <div className="p-4 flex flex-wrap items-center gap-3">
          <form onSubmit={onSubmit} className="flex items-center gap-2">
            <label className="text-[10px] uppercase tracking-[0.18em] font-bold text-warm-muted">RS.</label>
            <input
              type="text"
              inputMode="numeric"
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              className="num px-3 py-1.5 border border-border bg-card text-[14px] font-bold text-foreground w-32 focus:outline-none focus:border-primary/60 tracking-[0.05em]"
            />
            <button
              type="submit"
              className="text-[10px] uppercase tracking-[0.18em] font-bold px-3 py-1.5 border border-primary/60 bg-primary/10 text-amber hover:bg-primary/20 transition-colors cursor-pointer"
            >
              [ ▶ GENERATE ]
            </button>
          </form>
          <div className="flex items-center gap-1 ml-auto">
            <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold mr-1">PRESET</span>
            {PRESET_BUDGETS.map((b) => (
              <button
                key={b}
                onClick={() => applyBudget(b)}
                className={cn(
                  'num text-[10px] uppercase tracking-[0.1em] font-bold px-2 py-1 border transition-colors cursor-pointer',
                  budget === b
                    ? 'border-primary/60 bg-primary/10 text-amber'
                    : 'border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50',
                )}
              >
                {b >= 1000 ? `${(b / 1000).toFixed(0)}K` : b}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Summary */}
      {data && !isLoading && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <SummaryPanel label="ALLOCATED" value={formatRs(data.summary.totalAllocated)} sub={`OF ${formatRs(data.budget)}`} />
          <SummaryPanel label="LEFTOVER" value={formatRs(data.summary.leftover)} sub="TOO SMALL FOR ANOTHER LOT" subTone={data.summary.leftover > 0 ? 'text-warm-muted' : 'text-cyan-pos'} />
          <SummaryPanel label="PICKS" value={`${data.summary.pickCount}`} sub="TOP-RANKED WATCHLIST" />
          <SummaryPanel
            label="AVG NET YIELD"
            value={data.summary.averageNetYield != null ? `${data.summary.averageNetYield.toFixed(2)}%` : '—'}
            sub="POST-WHT (FILER)"
            valueTone={data.summary.averageNetYield != null ? yieldTone(data.summary.averageNetYield) : undefined}
          />
        </div>
      )}

      {/* Loading / Error / Empty / Picks */}
      {isLoading ? (
        <div className="space-y-3">
          {[0, 1, 2].map(i => <Skeleton key={i} className="h-44 w-full" style={{ borderRadius: 0 }} />)}
        </div>
      ) : error ? (
        <div className="panel px-5 py-8 text-center">
          <p className="text-warn text-[11px] uppercase tracking-[0.18em] font-bold">
            ✕ PLAN UNAVAILABLE · IS API ALIVE?
          </p>
        </div>
      ) : data?.picks.length === 0 ? (
        <EmptyState notes={data.notes} onRetry={refetch} />
      ) : data ? (
        <>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            {data.picks.map((pick, i) => (
              <PickCard key={pick.symbol} pick={pick} rank={i + 1} />
            ))}
          </div>
          {data.notes.length > 0 && (
            <div className="panel">
              <div className="panel-strip px-4 py-1.5">
                <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ NOTES</span>
              </div>
              <div className="p-4 space-y-1.5">
                {data.notes.map((n, i) => (
                  <p key={i} className="text-[11px] text-warm-muted leading-relaxed">▸ {n}</p>
                ))}
              </div>
            </div>
          )}
        </>
      ) : null}

      {/* Disclaimer */}
      <div className="panel px-4 py-3 border-l-[3px] border-l-amber">
        <p className="text-[10px] uppercase tracking-[0.15em] font-bold text-warm-muted">▌ HOW THIS WORKS</p>
        <p className="text-[11px] text-warm-muted mt-1.5 leading-relaxed">
          Picks are drawn from the watchlist where verdict is ACCUMULATE, WATCH, or HIGH/MEDIUM-conviction HOLD.
          Scoring weights composite + opportunity scores, with bonuses for upcoming ex-dates and penalties for
          sector over-concentration in your portfolio. Top 3 picks share the budget with 20-60% weight clamps
          for diversification. No LLM. NOT investment advice — use it as a starting point, not a final decision.
        </p>
      </div>
    </div>
  );
}

function SummaryPanel({
  label, value, sub, valueTone, subTone,
}: { label: string; value: string; sub: string; valueTone?: string; subTone?: string }) {
  return (
    <div className="panel">
      <div className="panel-strip px-3 py-1.5">
        <span className="text-amber font-bold text-[9px] tracking-[0.2em] uppercase">▌ {label}</span>
      </div>
      <div className="px-4 py-3">
        <div className={cn('num text-[20px] font-bold tracking-tight leading-none', valueTone ?? 'text-foreground')}>
          {value}
        </div>
        <div className={cn('num text-[9px] uppercase tracking-[0.15em] font-bold mt-1.5', subTone ?? 'text-warm-dim')}>
          {sub}
        </div>
      </div>
    </div>
  );
}

function PickCard({ pick, rank }: { pick: PlanPickDto; rank: number }) {
  const verdictAccent =
    pick.verdict === 'ACCUMULATE' ? 'var(--verdict-acc)' :
    pick.verdict === 'WATCH' ? 'var(--verdict-wch)' :
    pick.verdict === 'REVIEW' ? 'var(--verdict-rev)' :
    pick.verdict === 'AVOID' ? 'var(--verdict-avd)' :
    'var(--verdict-hold)';

  return (
    <Link
      href={`/stock/${pick.symbol}`}
      className="group panel relative overflow-hidden hover:border-primary/40 transition-colors flex flex-col h-full"
    >
      <span aria-hidden className="absolute top-0 left-0 right-0 h-[2px]" style={{ background: verdictAccent }} />

      <div className="panel-strip px-3 py-1.5 flex items-center justify-between">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase num">
          ▌ PICK {String(rank).padStart(2, '0')}
        </span>
        <span className="text-[10px] tracking-[0.18em] uppercase text-amber font-bold num">
          {(pick.weight * 100).toFixed(1)}%
        </span>
      </div>

      <div className="p-4 space-y-3 flex-1">
        {/* Identity */}
        <div className="flex items-center gap-2.5">
          <StockLogo symbol={pick.symbol} size={28} />
          <div className="min-w-0 flex-1">
            <div className="flex items-baseline gap-2">
              <span className="num font-bold text-[16px] text-foreground tracking-tight">{pick.symbol}</span>
              <VerdictBadge verdict={pick.verdict} size="sm" />
            </div>
            <div className="text-[9px] uppercase tracking-[0.12em] text-warm-dim font-medium mt-0.5 truncate">
              {pick.sector} · {pick.name}
            </div>
          </div>
        </div>

        {/* Allocation */}
        <div className="grid grid-cols-3 gridline-t gridline-b py-2">
          <Stat label="AMOUNT" value={formatRs(pick.allocatedAmount)} />
          <Stat label="SHARES" value={`${pick.shares}`} className="gridline-l" />
          <Stat label="PRICE" value={pick.currentPrice != null ? formatRs(pick.currentPrice) : '—'} className="gridline-l" />
        </div>

        {/* Income + yield */}
        <div className="flex items-baseline justify-between text-[10px] uppercase tracking-[0.15em] font-bold">
          <span className="text-warm-muted">EXPECTED INCOME</span>
          <span className="num text-cyan-pos">
            {pick.expectedAnnualIncome != null ? `+${formatRs(pick.expectedAnnualIncome)}/YR` : '—'}
          </span>
        </div>
        <div className="flex items-baseline justify-between text-[10px] uppercase tracking-[0.15em] font-bold">
          <span className="text-warm-muted">NET YIELD</span>
          <span className={cn('num', yieldTone(pick.netYield))}>
            {pick.netYield != null ? `${pick.netYield.toFixed(2)}%` : '—'}
          </span>
        </div>

        {/* Composite / Opportunity */}
        <div className="flex items-center gap-3 text-[10px] uppercase tracking-[0.15em] font-bold">
          <span className="text-warm-dim">COMP</span>
          <span className={cn('num', scoreTone(pick.compositeScore))}>
            {pick.compositeScore != null ? Math.round(pick.compositeScore) : '—'}
          </span>
          <span className="text-warm-dim">·</span>
          <span className="text-warm-dim">OPP</span>
          <span className={cn('num', scoreTone(pick.opportunityScore))}>
            {pick.opportunityScore != null ? Math.round(pick.opportunityScore) : '—'}
          </span>
          <ConvictionDots level={pick.convictionLevel} className="ml-auto" />
        </div>

        {/* Reasons */}
        <div className="border-t border-border pt-2 space-y-1">
          {pick.reasons.map((r, i) => (
            <div key={i} className="flex items-start gap-1.5 text-[10px]">
              <span className="text-amber font-bold shrink-0">▸</span>
              <span className="text-foreground/80 leading-snug">{r}</span>
            </div>
          ))}
          {pick.exDateInDays != null && pick.exDateInDays <= 14 && (
            <div className="flex items-start gap-1.5 text-[10px] mt-1.5">
              <span className="text-warn font-bold shrink-0">⚠</span>
              <span className="text-warn leading-snug uppercase tracking-[0.1em] font-bold">
                BUY BEFORE EX-DATE · {pick.exDateInDays}D
              </span>
            </div>
          )}
        </div>
      </div>

      <div className="px-3 py-1.5 border-t border-border flex items-center justify-between bg-white/[0.015]">
        <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">OPEN ›</span>
        <CompareButton symbol={pick.symbol} variant="icon" />
      </div>
    </Link>
  );
}

function Stat({ label, value, className }: { label: string; value: string; className?: string }) {
  return (
    <div className={cn('px-2 first:pl-0', className)}>
      <div className="text-[8.5px] uppercase tracking-[0.18em] text-warm-dim font-bold leading-none">{label}</div>
      <div className="num text-[13px] font-bold tracking-tight text-foreground mt-1.5 leading-none">{value}</div>
    </div>
  );
}

function EmptyState({ notes, onRetry }: { notes: string[]; onRetry: () => void }) {
  return (
    <div className="panel px-6 py-12 text-center">
      <div className="text-amber text-[11px] font-bold tracking-[0.25em] uppercase mb-3">
        ░░░░░ NO PICKS ░░░░░
      </div>
      <p className="text-foreground font-bold text-[14px] tracking-tight">
        WATCHLIST OFFERS NO ACTIONABLE SETUPS THIS MONTH
      </p>
      <p className="text-[11px] text-warm-muted mt-2 uppercase tracking-[0.12em] font-medium">
        EVERY STOCK IS HOLD LOW-CONVICTION, REVIEW, OR AVOID
      </p>
      {notes.map((n, i) => (
        <p key={i} className="text-[10px] text-warm-dim mt-3">▸ {n}</p>
      ))}
      <button
        onClick={onRetry}
        className="inline-flex items-center gap-1.5 text-[10px] uppercase tracking-[0.18em] font-bold text-amber mt-5 px-3 py-1.5 border border-primary/50 hover:border-primary cursor-pointer"
      >
        [ ⟳ REFRESH ]
      </button>
    </div>
  );
}
