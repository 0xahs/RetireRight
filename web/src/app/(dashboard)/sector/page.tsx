'use client';

import Link from 'next/link';
import { useSectors } from '@/lib/api/queries';
import { formatPct, scoreTone } from '@/lib/utils/format';
import { Skeleton } from '@/components/ui/skeleton';
import { VerdictBadge } from '@/components/signal/verdict-badge';
import { cn } from '@/lib/utils';
import type { SectorSummaryDto } from '@/lib/api/types';

function ScoreBar({ value }: { value: number | null }) {
  if (value == null) return <span className="text-warm-dim num text-[11px]">—</span>;
  const pct = Math.min(100, Math.max(0, value));
  const tone = scoreTone(value);
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-1 bg-white/[0.06] rounded-full overflow-hidden">
        <div
          className={cn('h-full rounded-full transition-all', {
            'bg-cyan-pos': tone === 'pos',
            'bg-amber':    tone === 'warn',
            'bg-warn':     tone === 'neg',
            'bg-warm-dim': tone === 'dim',
          })}
          style={{ width: `${pct}%` }}
        />
      </div>
      <span className={cn('num text-[11px] font-bold w-8 text-right', {
        'text-cyan-pos': tone === 'pos',
        'text-amber':    tone === 'warn',
        'text-warn':     tone === 'neg',
        'text-warm-dim': tone === 'dim',
      })}>
        {Math.round(value)}
      </span>
    </div>
  );
}

function SectorCard({ s }: { s: SectorSummaryDto }) {
  return (
    <div className="border border-border bg-card rounded-sm p-4 flex flex-col gap-3">
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="text-[11px] uppercase tracking-[0.14em] font-bold text-foreground leading-tight">
            {s.sector}
          </p>
          <p className="text-[10px] text-warm-dim mt-0.5 uppercase tracking-[0.12em] font-bold num">
            {s.stockCount} {s.stockCount === 1 ? 'STOCK' : 'STOCKS'}
          </p>
        </div>
        {s.avgDividendYield != null && (
          <span className="text-[11px] num font-bold text-cyan-pos shrink-0">
            {s.avgDividendYield.toFixed(1)}% yield
          </span>
        )}
      </div>

      {/* Composite score bar */}
      <div>
        <p className="text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold mb-1">COMPOSITE</p>
        <ScoreBar value={s.avgCompositeScore} />
      </div>

      {/* Metric strip */}
      <div className="grid grid-cols-2 gap-x-4 gap-y-1.5">
        <MetricCell label="AVG OPP." value={s.avgOpportunityScore != null ? `${Math.round(s.avgOpportunityScore)}/100` : null} />
        <MetricCell label="AVG P/E"  value={s.avgPeRatio != null ? s.avgPeRatio.toFixed(1) : null} />
      </div>

      {/* Top picks */}
      {s.topPicks.length > 0 && (
        <div>
          <p className="text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold mb-1.5">TOP PICKS</p>
          <div className="flex flex-col gap-1">
            {s.topPicks.map(pick => (
              <Link
                key={pick.symbol}
                href={`/stock/${pick.symbol}`}
                className="flex items-center justify-between gap-2 px-2 py-1 rounded-sm hover:bg-white/[0.04] transition-colors group"
              >
                <div className="flex items-center gap-2 min-w-0">
                  <span className="text-[10px] font-bold text-amber num tracking-[0.05em] shrink-0">
                    {pick.symbol}
                  </span>
                  <span className="text-[10px] text-warm-muted truncate group-hover:text-foreground transition-colors">
                    {pick.name}
                  </span>
                </div>
                <div className="flex items-center gap-1.5 shrink-0">
                  {pick.dividendYield != null && (
                    <span className="text-[9px] num text-warm-dim">
                      {pick.dividendYield.toFixed(1)}%
                    </span>
                  )}
                  <VerdictBadge verdict={pick.verdict} size="sm" />
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function MetricCell({ label, value }: { label: string; value: string | null }) {
  return (
    <div>
      <p className="text-[9px] uppercase tracking-[0.12em] text-warm-dim font-bold">{label}</p>
      <p className="text-[11px] num font-bold text-foreground mt-0.5">{value ?? '—'}</p>
    </div>
  );
}

export default function SectorPage() {
  const { data: sectors, isLoading, error } = useSectors();

  return (
    <div className="space-y-4">
      {/* Page header */}
      <div className="flex items-baseline justify-between gap-3 flex-wrap">
        <div>
          <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
            ┌── SECTOR ANALYSIS
          </div>
          <h1 className="text-foreground font-bold text-[20px] tracking-tight">
            PSX UNIVERSE · BY SECTOR
          </h1>
        </div>
        {!isLoading && sectors && (
          <div className="text-[10px] uppercase tracking-[0.15em] text-warm-dim font-bold num self-center">
            {sectors.length} SECTORS · {sectors.reduce((n, s) => n + s.stockCount, 0)} STOCKS
          </div>
        )}
      </div>

        {/* Loading */}
        {isLoading && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {Array.from({ length: 9 }).map((_, i) => (
              <Skeleton key={i} className="h-56 rounded-sm" />
            ))}
          </div>
        )}

        {/* Error */}
        {error && (
          <div className="text-[11px] text-warn uppercase tracking-[0.15em] font-bold px-1">
            Failed to load sector data
          </div>
        )}

        {/* Sector grid */}
        {sectors && !isLoading && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {sectors.map(s => (
              <SectorCard key={s.sector} s={s} />
            ))}
          </div>
        )}

        {sectors?.length === 0 && (
          <div className="text-[11px] text-warm-dim uppercase tracking-[0.15em] font-bold px-1">
            No sector data — run compute pipeline first
          </div>
        )}
    </div>
  );
}
