'use client';

import { useStocks } from '@/lib/api/queries';
import { formatRs, formatPct, formatScore, signalLabel, signalColorClass, scoreColor, freshnessBadge } from '@/lib/utils/format';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { AlertCircle, TrendingUp, TrendingDown } from 'lucide-react';

export default function WatchlistPage() {
  const { data: stocks, isLoading, error } = useStocks();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Watchlist</h1>
        <p className="text-muted-foreground text-sm mt-1">
          {stocks ? `${stocks.length} stocks tracked` : 'Loading...'}
        </p>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">All Stocks</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="px-6 pb-4 space-y-3">
              {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => <Skeleton key={i} className="h-10 w-full" />)}
            </div>
          ) : error ? (
            <div className="px-6 pb-4 text-sm text-red-500">Failed to load stocks. Is the API running?</div>
          ) : !stocks?.length ? (
            <div className="px-6 pb-4 text-sm text-muted-foreground">No stocks in watchlist</div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border">
                  <th className="text-left px-6 py-3 text-xs text-muted-foreground font-medium">Symbol</th>
                  <th className="text-left px-3 py-3 text-xs text-muted-foreground font-medium">Sector</th>
                  <th className="text-right px-3 py-3 text-xs text-muted-foreground font-medium">Price</th>
                  <th className="text-right px-3 py-3 text-xs text-muted-foreground font-medium">Yield</th>
                  <th className="text-right px-3 py-3 text-xs text-muted-foreground font-medium">Coverage</th>
                  <th className="text-right px-3 py-3 text-xs text-muted-foreground font-medium">Score</th>
                  <th className="text-right px-3 py-3 text-xs text-muted-foreground font-medium">Opportunity</th>
                  <th className="text-right px-6 py-3 text-xs text-muted-foreground font-medium">Signal</th>
                </tr>
              </thead>
              <tbody>
                {[...stocks].sort((a, b) => (b.compositeScore ?? 0) - (a.compositeScore ?? 0)).map((s) => {
                  const freshness = freshnessBadge(s.scrapedAt);
                  return (
                    <tr key={s.symbol} className="border-b border-border/50 hover:bg-accent/30 transition-colors">
                      <td className="px-6 py-3">
                        <Link href={`/stock/${s.symbol}`} className="font-medium hover:text-primary transition-colors">
                          {s.symbol}
                        </Link>
                        <div className="text-xs text-muted-foreground">{s.name}</div>
                      </td>
                      <td className="px-3 py-3 text-xs text-muted-foreground">{s.sector}</td>
                      <td className="text-right px-3 py-3">
                        <div>{formatRs(s.price)}</div>
                        {s.changePct != null && (
                          <div className={cn('text-xs flex items-center justify-end gap-0.5', s.changePct >= 0 ? 'text-emerald-500' : 'text-red-500')}>
                            {s.changePct >= 0 ? <TrendingUp className="h-2.5 w-2.5" /> : <TrendingDown className="h-2.5 w-2.5" />}
                            {formatPct(s.changePct)}
                          </div>
                        )}
                      </td>
                      <td className="text-right px-3 py-3 tabular-nums">{formatPct(s.dividendYield)}</td>
                      <td className="text-right px-3 py-3 tabular-nums">{s.dividendCoverage?.toFixed(2) ?? '-'}x</td>
                      <td className={cn('text-right px-3 py-3 tabular-nums font-semibold', scoreColor(s.compositeScore))}>
                        {formatScore(s.compositeScore)}
                      </td>
                      <td className={cn('text-right px-3 py-3 tabular-nums font-semibold', scoreColor(s.opportunityScore))}>
                        {formatScore(s.opportunityScore)}
                      </td>
                      <td className="text-right px-6 py-3">
                        <div className="flex items-center justify-end gap-1.5">
                          {freshness === 'stale' && (
                            <AlertCircle className="h-3 w-3 text-yellow-500 shrink-0" aria-label="Data may be stale" />
                          )}
                          <span className={cn('text-xs font-medium', signalColorClass(s.signal))}>
                            {signalLabel(s.signal)}
                          </span>
                        </div>
                        {s.opportunityFlags.length > 0 && (
                          <div className="flex flex-wrap justify-end gap-1 mt-1">
                            {s.opportunityFlags.slice(0, 2).map((f) => (
                              <Badge key={f} variant="outline" className="text-xs px-1 py-0">{f}</Badge>
                            ))}
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
