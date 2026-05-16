'use client';

import { useStocks, useMacro, useUpcomingDividends } from '@/lib/api/queries';
import { formatRs, formatPct, formatScore, signalLabel, signalColorClass, freshnessBadge, scoreColor } from '@/lib/utils/format';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { TrendingUp, TrendingDown, Calendar, AlertCircle } from 'lucide-react';

export function CommandCenterContent() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Command Center</h1>
        <p className="text-muted-foreground text-sm mt-1">PSX Dividend Intelligence Dashboard</p>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <MacroStrip />
      </div>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <WatchlistSnapshot />
        </div>
        <div>
          <UpcomingDividendsWidget />
        </div>
      </div>
    </div>
  );
}

function MacroStrip() {
  const { data, isLoading } = useMacro();

  if (isLoading) {
    return (
      <>
        {[1, 2, 3].map((i) => (
          <Card key={i}><CardContent className="p-4"><Skeleton className="h-10 w-full" /></CardContent></Card>
        ))}
      </>
    );
  }

  if (!data) return null;

  return (
    <>
      <Card>
        <CardHeader className="pb-1 pt-4 px-4">
          <CardTitle className="text-xs text-muted-foreground font-normal">KSE-100</CardTitle>
        </CardHeader>
        <CardContent className="px-4 pb-4">
          <div className="text-xl font-semibold">
            {data.kse100Level?.toLocaleString('en-PK', { maximumFractionDigits: 0 }) ?? '-'}
          </div>
          {data.kse100ChangePct != null && (
            <div className={cn('text-sm flex items-center gap-1', data.kse100ChangePct >= 0 ? 'text-emerald-500' : 'text-red-500')}>
              {data.kse100ChangePct >= 0 ? <TrendingUp className="h-3 w-3" /> : <TrendingDown className="h-3 w-3" />}
              {formatPct(data.kse100ChangePct)}
            </div>
          )}
        </CardContent>
      </Card>
      <Card>
        <CardHeader className="pb-1 pt-4 px-4">
          <CardTitle className="text-xs text-muted-foreground font-normal">SBP Rate / PKR-USD</CardTitle>
        </CardHeader>
        <CardContent className="px-4 pb-4">
          <div className="text-xl font-semibold">{formatPct(data.sbpRate)}</div>
          <div className="text-sm text-muted-foreground">PKR/USD: {data.pkrUsd?.toFixed(2) ?? '-'}</div>
        </CardContent>
      </Card>
      <Card>
        <CardHeader className="pb-1 pt-4 px-4">
          <CardTitle className="text-xs text-muted-foreground font-normal">Brent Crude</CardTitle>
        </CardHeader>
        <CardContent className="px-4 pb-4">
          <div className="text-xl font-semibold">${data.brentCrude?.toFixed(2) ?? '-'}</div>
          <div className="text-xs text-muted-foreground">
            As of {data.asOf ? new Date(data.asOf).toLocaleDateString() : '-'}
          </div>
        </CardContent>
      </Card>
    </>
  );
}

function WatchlistSnapshot() {
  const { data: stocks, isLoading } = useStocks();

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base">Watchlist</CardTitle>
          <Link href="/watchlist" className="text-xs text-muted-foreground hover:text-foreground transition-colors">
            View all
          </Link>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        {isLoading ? (
          <div className="px-6 pb-4 space-y-2">
            {[1, 2, 3, 4, 5].map((i) => <Skeleton key={i} className="h-8 w-full" />)}
          </div>
        ) : !stocks?.length ? (
          <div className="px-6 pb-4 text-sm text-muted-foreground">No stocks in watchlist</div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border">
                <th className="text-left px-6 py-2 text-xs text-muted-foreground font-medium">Symbol</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Price</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Yield</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Score</th>
                <th className="text-right px-6 py-2 text-xs text-muted-foreground font-medium">Signal</th>
              </tr>
            </thead>
            <tbody>
              {stocks.map((s) => {
                const freshness = freshnessBadge(s.scrapedAt);
                return (
                  <tr key={s.symbol} className="border-b border-border/50 hover:bg-accent/30 transition-colors">
                    <td className="px-6 py-2.5">
                      <Link href={`/stock/${s.symbol}`} className="font-medium hover:text-primary transition-colors">
                        {s.symbol}
                      </Link>
                      <div className="text-xs text-muted-foreground truncate max-w-[140px]">{s.name}</div>
                    </td>
                    <td className="text-right px-3 py-2.5">
                      <div>{formatRs(s.price)}</div>
                      {s.changePct != null && (
                        <div className={cn('text-xs', s.changePct >= 0 ? 'text-emerald-500' : 'text-red-500')}>
                          {s.changePct >= 0 ? '+' : ''}{formatPct(s.changePct)}
                        </div>
                      )}
                    </td>
                    <td className="text-right px-3 py-2.5 tabular-nums">
                      {formatPct(s.dividendYield)}
                    </td>
                    <td className={cn('text-right px-3 py-2.5 tabular-nums font-medium', scoreColor(s.compositeScore))}>
                      {formatScore(s.compositeScore)}
                    </td>
                    <td className="text-right px-6 py-2.5">
                      <div className="flex items-center justify-end gap-1.5">
                        {freshness === 'stale' && (
                          <AlertCircle className="h-3 w-3 text-yellow-500" aria-label="Data may be stale" />
                        )}
                        <span className={cn('text-xs font-medium', signalColorClass(s.signal))}>
                          {signalLabel(s.signal)}
                        </span>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </CardContent>
    </Card>
  );
}

function UpcomingDividendsWidget() {
  const { data: dividends, isLoading } = useUpcomingDividends(60);

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <Calendar className="h-4 w-4" />
            Upcoming Ex-Dates
          </CardTitle>
          <Link href="/dividends" className="text-xs text-muted-foreground hover:text-foreground transition-colors">
            View all
          </Link>
        </div>
      </CardHeader>
      <CardContent className="space-y-2">
        {isLoading ? (
          <div className="space-y-2">
            {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-12 w-full" />)}
          </div>
        ) : !dividends?.length ? (
          <p className="text-sm text-muted-foreground">No upcoming dividends in the next 60 days</p>
        ) : (
          dividends.slice(0, 8).map((d, i) => (
            <div key={i} className="flex items-start justify-between p-2.5 rounded-md bg-accent/30">
              <div>
                <div className="font-medium text-sm">{d.symbol}</div>
                <div className="text-xs text-muted-foreground">
                  Ex: {new Date(d.exDate).toLocaleDateString('en-PK', { month: 'short', day: 'numeric' })}
                  {d.daysUntilEx === 0 ? ' (today)' : ` — ${d.daysUntilEx}d`}
                </div>
              </div>
              <div className="text-right">
                <div className="text-sm font-medium">Rs. {d.dps}</div>
                <Badge
                  variant={d.daysUntilEx <= 3 ? 'destructive' : d.daysUntilEx <= 7 ? 'default' : 'secondary'}
                  className="text-xs"
                >
                  {d.daysUntilEx <= 3 ? 'Urgent' : d.daysUntilEx <= 7 ? 'Soon' : `${d.daysUntilEx}d`}
                </Badge>
              </div>
            </div>
          ))
        )}
      </CardContent>
    </Card>
  );
}
