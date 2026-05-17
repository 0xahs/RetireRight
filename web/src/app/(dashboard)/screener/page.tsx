'use client';

import { useState } from 'react';
import { useScreenerPresets, useRunScreener } from '@/lib/api/queries';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { formatPct, scoreTone, yieldTone, coverageTone } from '@/lib/utils/format';
import { FlagBadge } from '@/components/stock-ui';
import type { ScreenerResultDto } from '@/lib/api/types';
import { StockLogo } from '@/components/stock-logo';

const PRESET_META: Record<string, { code: string; label: string; description: string }> = {
  dividend_safety: { code: 'S1', label: 'DIVIDEND SAFETY',  description: 'HIGH COVERAGE · SUSTAINABLE PAYOUT' },
  high_yield:      { code: 'S2', label: 'HIGH YIELD',       description: 'ABOVE-AVERAGE DIVIDEND YIELD' },
  growth:          { code: 'S3', label: 'GROWTH',           description: 'GROWING DIVIDENDS + EARNINGS' },
  value:           { code: 'S4', label: 'VALUE',            description: 'UNDERVALUED BY FUNDAMENTALS' },
  retirement_core: { code: 'S5', label: 'RETIREMENT CORE',  description: 'BALANCED SAFETY + YIELD + GROWTH' },
  banking_quality: { code: 'S6', label: 'BANKING QUALITY',  description: 'CAR · NPL · CIR FILTERS' },
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
      <div>
        <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
          ┌── SCREENER
        </div>
        <h1 className="text-foreground font-bold text-[20px] tracking-tight">
          FILTER · RANK · DISCOVER
        </h1>
      </div>

      <div className="panel">
        <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ PRESETS</span>
          <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">SELECT A PRESET TO RUN</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-0 divide-x divide-y divide-border border-t border-border" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))' }}>
          {presetList.map((preset) => {
            const meta = PRESET_META[preset] ?? { code: '?', label: preset.toUpperCase(), description: '' };
            const isActive = selectedPreset === preset;
            const isPending = runScreener.isPending && selectedPreset === preset;
            return (
              <button
                key={preset}
                onClick={() => handlePreset(preset)}
                disabled={runScreener.isPending}
                className={cn(
                  'relative text-left px-4 py-3 cursor-pointer transition-colors group',
                  isActive
                    ? 'bg-primary/[0.07] text-amber'
                    : 'text-foreground hover:bg-amber-500/[0.03] hover:text-amber',
                  'disabled:cursor-not-allowed',
                )}
              >
                {isActive && <span aria-hidden className="absolute left-0 top-0 bottom-0 w-[3px] bg-primary" />}
                <div className="flex items-baseline justify-between gap-2">
                  <div>
                    <div className="text-[9px] text-warm-dim group-hover:text-amber/70 font-bold tracking-[0.18em] uppercase">
                      [{meta.code}]
                    </div>
                    <div className={cn(
                      'text-[13px] font-bold tracking-[0.05em] uppercase mt-1',
                      isActive ? 'text-amber' : 'text-foreground group-hover:text-amber',
                    )}>
                      {meta.label}
                    </div>
                    <div className="text-[10px] text-warm-muted mt-1 tracking-[0.05em]">
                      {meta.description}
                    </div>
                  </div>
                  <span className="text-amber text-[14px]">
                    {isPending ? '◐' : isActive ? '▶' : ''}
                  </span>
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {runScreener.isPending && (
        <div className="panel">
          <div className="p-3 space-y-1.5">
            {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-10 w-full" style={{ borderRadius: 0 }} />)}
          </div>
        </div>
      )}

      {result && !runScreener.isPending && (
        <div className="panel">
          <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ RESULTS</span>
              {result.appliedPreset && (
                <span className="text-[9px] uppercase tracking-[0.18em] text-amber font-bold border border-primary/40 px-1.5 py-0.5">
                  {PRESET_META[result.appliedPreset]?.label ?? result.appliedPreset}
                </span>
              )}
            </div>
            <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold num">
              <span className="text-foreground font-bold">{result.totalMatched}</span> MATCHED · {result.totalEvaluated} EVALUATED
            </span>
          </div>
          {result.stocks.length === 0 ? (
            <p className="px-6 py-10 text-center text-[11px] uppercase tracking-[0.18em] text-warm-dim font-bold">
              NO STOCKS MATCHED · TRY ANOTHER PRESET
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-[11px] num min-w-[1000px]">
                <thead>
                  <tr className="border-b border-border bg-white/[0.015]">
                    <th className="text-right pl-4 pr-2 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold w-8">#</th>
                    <th className="text-left px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">SYMBOL</th>
                    <th className="text-left px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">SECTOR</th>
                    <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">YIELD</th>
                    <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">COV</th>
                    <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">P/E</th>
                    <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">COMP</th>
                    <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">OPP</th>
                    <th className="px-3 py-2 text-left text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">FLAGS</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {result.stocks.map((s) => (
                    <tr
                      key={s.symbol}
                      className="row-hover hover:bg-amber-500/[0.04] transition-colors"
                    >
                      <td className="text-right pl-4 pr-2 py-2">
                        <span className="text-[10px] text-warm-dim font-bold">{s.rank}</span>
                      </td>
                      <td className="px-3 py-2">
                        <Link href={`/stock/${s.symbol}`} className="flex items-center gap-2 group">
                          <StockLogo symbol={s.symbol} size={22} />
                          <div>
                            <div className="font-bold text-[12px] text-foreground tracking-[0.05em] group-hover:text-amber transition-colors">
                              {s.symbol}
                            </div>
                            <div className="text-[9px] uppercase tracking-[0.12em] text-warm-dim font-medium truncate max-w-[110px]">
                              {s.name}
                            </div>
                          </div>
                        </Link>
                      </td>
                      <td className="px-3 py-2 text-[10px] uppercase tracking-[0.1em] text-warm-muted font-medium">{s.sector}</td>
                      <td className={cn('text-right px-3 py-2 font-bold', yieldTone(s.dividendYield))}>{formatPct(s.dividendYield)}</td>
                      <td className={cn('text-right px-3 py-2 font-bold', coverageTone(s.dividendCoverage))}>
                        {s.dividendCoverage != null ? `${s.dividendCoverage.toFixed(2)}X` : '—'}
                      </td>
                      <td className="text-right px-3 py-2 text-warm-muted">{s.peRatio != null ? `${s.peRatio.toFixed(1)}X` : '—'}</td>
                      <td className={cn('text-right px-3 py-2 font-bold', scoreTone(s.compositeScore))}>{s.compositeScore != null ? Math.round(s.compositeScore) : '—'}</td>
                      <td className={cn('text-right px-3 py-2 font-bold', scoreTone(s.opportunityScore))}>{s.opportunityScore != null ? Math.round(s.opportunityScore) : '—'}</td>
                      <td className="pl-3 pr-4 py-2">
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
        </div>
      )}

      {!result && !runScreener.isPending && (
        <div className="panel">
          <div className="px-6 py-12 text-center">
            <div className="text-amber text-[11px] font-bold tracking-[0.25em] uppercase mb-3">
              ░░░░░ READY ░░░░░
            </div>
            <p className="text-foreground font-bold text-[14px] tracking-tight">
              SELECT A PRESET ABOVE
            </p>
            <p className="text-[11px] text-warm-muted mt-2 uppercase tracking-[0.12em] font-medium">
              RESULTS RANK BY COMPOSITE SCORE ACROSS WATCHLIST
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
