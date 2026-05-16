'use client';

import { useHoldings, usePortfolioSummary } from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { formatRs, formatPct, formatScore, scoreColor } from '@/lib/utils/format';

export default function PortfolioPage() {
  const { data: holdings, isLoading } = useHoldings();
  const { data: summary } = usePortfolioSummary();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Portfolio</h1>
        <p className="text-muted-foreground text-sm mt-1">Your PSX dividend holdings</p>
      </div>

      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card>
            <CardContent className="p-4">
              <div className="text-xs text-muted-foreground">Total Invested</div>
              <div className="text-lg font-semibold">{formatRs(summary.totalInvested)}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="text-xs text-muted-foreground">Current Value</div>
              <div className="text-lg font-semibold">{formatRs(summary.currentValue)}</div>
              <div className={cn('text-xs', summary.totalGainLossPct >= 0 ? 'text-emerald-500' : 'text-red-500')}>
                {summary.totalGainLossPct >= 0 ? '+' : ''}{formatPct(summary.totalGainLossPct)}
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="text-xs text-muted-foreground">Annual Income</div>
              <div className="text-lg font-semibold">{formatRs(summary.annualDividendIncome)}</div>
              <div className="text-xs text-muted-foreground">Portfolio yield: {formatPct(summary.portfolioYield)}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="text-xs text-muted-foreground">Holdings</div>
              <div className="text-lg font-semibold">{summary.holdingCount} stocks</div>
            </CardContent>
          </Card>
        </div>
      )}

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Holdings</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="px-6 pb-4 space-y-2">{[1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full" />)}</div>
          ) : !holdings?.length ? (
            <p className="px-6 pb-4 text-sm text-muted-foreground">No holdings yet</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border">
                  <th className="text-left px-6 py-2 text-xs text-muted-foreground font-medium">Stock</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Shares</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Avg Cost</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Current</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Gain/Loss</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Yield on Cost</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Annual Income</th>
                  <th className="text-right px-6 py-2 text-xs text-muted-foreground font-medium">Safety</th>
                </tr>
              </thead>
              <tbody>
                {holdings.map((h) => (
                  <tr key={h.id} className="border-b border-border/50 hover:bg-accent/30 transition-colors">
                    <td className="px-6 py-2.5">
                      <Link href={`/stock/${h.symbol}`} className="font-medium hover:text-primary transition-colors">
                        {h.symbol}
                      </Link>
                      <div className="text-xs text-muted-foreground">{h.name}</div>
                    </td>
                    <td className="text-right px-3 py-2.5">{h.shares.toLocaleString()}</td>
                    <td className="text-right px-3 py-2.5">{formatRs(h.avgCost)}</td>
                    <td className="text-right px-3 py-2.5">{h.currentPrice ? formatRs(h.currentPrice) : '-'}</td>
                    <td className={cn('text-right px-3 py-2.5', h.gainLossPct == null ? '' : h.gainLossPct >= 0 ? 'text-emerald-500' : 'text-red-500')}>
                      {h.gainLossPct != null ? `${h.gainLossPct >= 0 ? '+' : ''}${formatPct(h.gainLossPct)}` : '-'}
                    </td>
                    <td className="text-right px-3 py-2.5">{formatPct(h.yieldOnCost)}</td>
                    <td className="text-right px-3 py-2.5 font-medium">{h.annualIncome ? formatRs(h.annualIncome) : '-'}</td>
                    <td className={cn('text-right px-6 py-2.5 font-semibold', scoreColor(h.safetyScore))}>
                      {formatScore(h.safetyScore)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
