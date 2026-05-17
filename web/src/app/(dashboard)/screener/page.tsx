'use client';

import { useState } from 'react';
import { useScreenerPresets, useRunScreener } from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { formatPct, formatScore } from '@/lib/utils/format';
import { ScoreBar, SignalBadge, FlagBadge, SectorBadge } from '@/components/stock-ui';
import type { ScreenerResultDto } from '@/lib/api/types';
import {
  Shield, Percent, TrendingUp, Tag, Landmark,
  Filter, ChevronRight, Loader2,
} from 'lucide-react';

const PRESET_META: Record<string, {
  label: string;
  description: string;
  icon: React.ElementType;
  accent: string;
}> = {
  dividend_safety: {
    label: 'Dividend Safety',
    description: 'High coverage · sustainable payout',
    icon: Shield,
    accent: 'text-emerald-400',
  },
  high_yield: {
    label: 'High Yield',
    description: 'Above-average dividend yield',
    icon: Percent,
    accent: 'text-blue-400',
  },
  growth: {
    label: 'Growth',
    description: 'Growing dividends + earnings',
    icon: TrendingUp,
    accent: 'text-violet-400',
  },
  value: {
    label: 'Value',
    description: 'Undervalued by fundamentals',
    icon: Tag,
    accent: 'text-amber-400',
  },
  retirement_core: {
    label: 'Retirement Core',
    description: 'Balanced safety + yield + growth',
    icon: Landmark,
    accent: 'text-primary',
  },
};

export default function ScreenerPage() {
  const { data: presets } = useScreenerPresets();
  const runScreener = useRunScreener();
  const [selectedPreset, setSelectedPreset] = useState<string | null>(null);
  const [result, setResult] = useState<ScreenerResultDto | null>(null);

  async function handlePreset(preset: string) {
    setSelectedPreset(preset);
    const res = await runScreener.mutateAsync({ preset, limit: 50, watchlistOnly: false });
    setResult(res);
  }

  const presetList = presets ?? Object.keys(PRESET_META);

  return (
    <div className="space-y-4">
      {/* Page header */}
      <div>
        <h1 className="text-xl font-semibold tracking-tight">Screener</h1>
        <p className="text-muted-foreground text-xs mt-0.5">
          Filter and rank PSX stocks by investment criteria
        </p>
      </div>

      {/* Preset selector */}
      <Card className="border-border/50 card-panel overflow-hidden">
        <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
          <div className="flex items-center gap-2">
            <Filter className="h-3.5 w-3.5 text-primary" />
            <CardTitle className="text-sm font-semibold">Screening Presets</CardTitle>
          </div>
        </CardHeader>
        <CardContent className="p-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-2">
            {presetList.map((preset) => {
              const meta = PRESET_META[preset];
              const Icon = meta?.icon ?? Filter;
              const isActive = selectedPreset === preset;
              const isPending = runScreener.isPending && selectedPreset === preset;

              return (
                <button
                  key={preset}
                  onClick={() => handlePreset(preset)}
                  disabled={runScreener.isPending}
                  className={cn(
                    'relative group flex flex-col gap-2 p-3.5 rounded-sm border text-left transition-all duration-150 cursor-pointer disabled:cursor-not-allowed',
                    isActive
                      ? 'border-primary/40 bg-primary/[0.07]'
                      : 'border-border/40 bg-muted/[0.03] hover:border-border hover:bg-muted/[0.06]',
                  )}
                >
                  {/* Active left bar */}
                  {isActive && (
                    <div className="absolute left-0 top-3 bottom-3 w-[2px] rounded-full bg-primary" />
                  )}

                  <div className="flex items-center justify-between">
                    <Icon className={cn(
                      'h-3.5 w-3.5 shrink-0',
                      isActive ? (meta?.accent ?? 'text-primary') : 'text-muted-foreground/50 group-hover:text-muted-foreground/70',
                    )} />
                    {isPending
                      ? <Loader2 className="h-3 w-3 text-primary animate-spin" />
                      : isActive
                        ? <ChevronRight className="h-3 w-3 text-primary/60" />
                        : null}
                  </div>

                  <div>
                    <div className={cn(
                      'text-[12px] font-semibold leading-tight',
                      isActive ? 'text-foreground' : 'text-muted-foreground group-hover:text-foreground/80',
                    )}>
                      {meta?.label ?? preset}
                    </div>
                    {meta?.description && (
                      <div className="text-[10px] text-muted-foreground/60 mt-0.5 leading-tight">
                        {meta.description}
                      </div>
                    )}
                  </div>
                </button>
              );
            })}
          </div>
        </CardContent>
      </Card>

      {/* Loading skeleton */}
      {runScreener.isPending && (
        <Card className="border-border/50 card-panel overflow-hidden">
          <CardContent className="p-5 space-y-2">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-10 w-full rounded-sm" />
            ))}
          </CardContent>
        </Card>
      )}

      {/* Results */}
      {result && !runScreener.isPending && (
        <Card className="overflow-hidden border-border/50 card-panel">
          <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
            <div className="flex items-center gap-3">
              <CardTitle className="text-sm font-semibold">Results</CardTitle>
              {result.appliedPreset && (
                <span className="text-[10px] px-1.5 py-0.5 rounded-sm border border-primary/30 bg-primary/10 text-primary font-medium uppercase tracking-wider">
                  {PRESET_META[result.appliedPreset]?.label ?? result.appliedPreset}
                </span>
              )}
              <span className="ml-auto num text-[10px] text-muted-foreground/60">
                <span className="text-foreground font-semibold">{result.totalMatched}</span> matched ·{' '}
                {result.totalEvaluated} evaluated
              </span>
            </div>
          </CardHeader>
          <CardContent className="p-0">
            {result.stocks.length === 0 ? (
              <p className="px-6 py-10 text-sm text-muted-foreground text-center">
                No stocks matched this preset
              </p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border/40" style={{ background: 'oklch(1 0 0 / 2%)' }}>
                      <th className="text-right pl-5 pr-2 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest w-8">#</th>
                      <th className="text-left px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Symbol</th>
                      <th className="text-left px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Sector</th>
                      <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Yield</th>
                      <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Coverage</th>
                      <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">P/E</th>
                      <th className="px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest w-32">Score</th>
                      <th className="px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest w-32">Opp.</th>
                      <th className="px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Signal</th>
                      <th className="pl-3 pr-5 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Flags</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.stocks.map((s) => (
                      <tr
                        key={s.symbol}
                        className="row-hover relative border-b border-border/30 hover:bg-primary/[0.04] transition-colors cursor-pointer"
                      >
                        {/* Rank */}
                        <td className="text-right pl-5 pr-2 py-3">
                          <span className="num text-[11px] text-muted-foreground/40 font-medium">
                            {s.rank}
                          </span>
                        </td>

                        {/* Symbol */}
                        <td className="px-3 py-3">
                          <Link
                            href={`/stock/${s.symbol}`}
                            className="num font-semibold text-[13px] hover:text-primary transition-colors"
                          >
                            {s.symbol}
                          </Link>
                          <div className="text-[11px] text-muted-foreground/60 truncate max-w-[110px] leading-tight mt-0.5">
                            {s.name}
                          </div>
                        </td>

                        {/* Sector */}
                        <td className="px-3 py-3">
                          <SectorBadge sector={s.sector ?? ''} />
                        </td>

                        {/* Yield */}
                        <td className="text-right px-3 py-3">
                          <span className="num font-semibold text-[13px]">{formatPct(s.dividendYield)}</span>
                        </td>

                        {/* Coverage */}
                        <td className="text-right px-3 py-3">
                          <span className="num text-[13px] text-muted-foreground">
                            {s.dividendCoverage != null ? `${s.dividendCoverage.toFixed(2)}x` : '—'}
                          </span>
                        </td>

                        {/* P/E */}
                        <td className="text-right px-3 py-3">
                          <span className="num text-[13px] text-muted-foreground">
                            {s.peRatio != null ? `${s.peRatio.toFixed(1)}x` : '—'}
                          </span>
                        </td>

                        {/* Composite score */}
                        <td className="px-3 py-3 w-32">
                          <ScoreBar score={s.compositeScore} />
                        </td>

                        {/* Opportunity score */}
                        <td className="px-3 py-3 w-32">
                          <ScoreBar score={s.opportunityScore} />
                        </td>

                        {/* Signal */}
                        <td className="px-3 py-3">
                          <SignalBadge signal={s.signal} />
                        </td>

                        {/* Flags */}
                        <td className="pl-3 pr-5 py-3">
                          <div className="flex flex-wrap gap-1">
                            {s.opportunityFlags.slice(0, 2).map((f) => (
                              <FlagBadge key={f} flag={f} />
                            ))}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Empty state — no preset selected */}
      {!result && !runScreener.isPending && (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <div className="h-10 w-10 rounded-sm border border-border/40 bg-muted/10 flex items-center justify-center mb-3">
            <Filter className="h-4 w-4 text-muted-foreground/40" />
          </div>
          <p className="text-sm text-muted-foreground">Select a preset above to screen stocks</p>
          <p className="text-[11px] text-muted-foreground/50 mt-1">
            Results rank by composite score across your watchlist
          </p>
        </div>
      )}
    </div>
  );
}
