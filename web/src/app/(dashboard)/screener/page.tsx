'use client';

import { useState } from 'react';
import { useScreenerPresets, useRunScreener } from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { formatPct, formatScore, signalLabel, signalColorClass, scoreColor } from '@/lib/utils/format';
import type { ScreenerResultDto } from '@/lib/api/types';

const PRESET_LABELS: Record<string, { label: string; description: string }> = {
  dividend_safety: { label: 'Dividend Safety', description: 'High coverage + sustainable payout' },
  high_yield: { label: 'High Yield', description: 'Above-average dividend yield' },
  growth: { label: 'Growth', description: 'Growing dividends + earnings' },
  value: { label: 'Value', description: 'Undervalued by fundamental metrics' },
  retirement_core: { label: 'Retirement Core', description: 'Balanced safety + yield + growth' },
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

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Screener</h1>
        <p className="text-muted-foreground text-sm mt-1">Filter and rank PSX stocks by investment criteria</p>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Presets</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-2">
            {(presets ?? Object.keys(PRESET_LABELS)).map((preset) => {
              const meta = PRESET_LABELS[preset];
              return (
                <button
                  key={preset}
                  onClick={() => handlePreset(preset)}
                  disabled={runScreener.isPending}
                  className={cn(
                    'p-3 rounded-lg border text-left transition-colors disabled:opacity-50',
                    selectedPreset === preset
                      ? 'border-primary bg-primary/10'
                      : 'border-border hover:border-primary/50 hover:bg-accent/50',
                  )}
                >
                  <div className="text-sm font-medium">{meta?.label ?? preset}</div>
                  {meta?.description && (
                    <div className="text-xs text-muted-foreground mt-0.5">{meta.description}</div>
                  )}
                </button>
              );
            })}
          </div>
        </CardContent>
      </Card>

      {runScreener.isPending && (
        <div className="space-y-2">
          {[1, 2, 3].map((i) => <Skeleton key={i} className="h-12 w-full" />)}
        </div>
      )}

      {result && !runScreener.isPending && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base flex items-center gap-2">
              Results
              <Badge variant="secondary">{result.totalMatched} matched / {result.totalEvaluated} evaluated</Badge>
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border">
                  <th className="text-left px-6 py-2 text-xs text-muted-foreground font-medium">#</th>
                  <th className="text-left px-3 py-2 text-xs text-muted-foreground font-medium">Symbol</th>
                  <th className="text-left px-3 py-2 text-xs text-muted-foreground font-medium">Sector</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Yield</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Score</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Opp.</th>
                  <th className="text-right px-6 py-2 text-xs text-muted-foreground font-medium">Signal</th>
                </tr>
              </thead>
              <tbody>
                {result.stocks.map((s) => (
                  <tr key={s.symbol} className="border-b border-border/50 hover:bg-accent/30 transition-colors">
                    <td className="px-6 py-2.5 text-muted-foreground">{s.rank}</td>
                    <td className="px-3 py-2.5">
                      <Link href={`/stock/${s.symbol}`} className="font-medium hover:text-primary transition-colors">
                        {s.symbol}
                      </Link>
                      <div className="text-xs text-muted-foreground">{s.name}</div>
                    </td>
                    <td className="px-3 py-2.5 text-xs text-muted-foreground">{s.sector}</td>
                    <td className="text-right px-3 py-2.5">{formatPct(s.dividendYield)}</td>
                    <td className={cn('text-right px-3 py-2.5 font-semibold', scoreColor(s.compositeScore))}>
                      {formatScore(s.compositeScore)}
                    </td>
                    <td className={cn('text-right px-3 py-2.5 font-semibold', scoreColor(s.opportunityScore))}>
                      {formatScore(s.opportunityScore)}
                    </td>
                    <td className={cn('text-right px-6 py-2.5 text-xs font-medium', signalColorClass(s.signal))}>
                      {signalLabel(s.signal)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
