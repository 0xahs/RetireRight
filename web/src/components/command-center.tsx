'use client';

import { useStocks, useMacro, useUpcomingDividends } from '@/lib/api/queries';
import { formatRs, formatPct, freshnessBadge } from '@/lib/utils/format';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { TrendingUp, TrendingDown, CalendarDays, AlertCircle, Clock, ArrowRight } from 'lucide-react';
import { ScoreBar, SignalBadge } from '@/components/stock-ui';
import { StockLogo } from '@/components/stock-logo';

export function CommandCenterContent() {
  return (
    <div className="space-y-6">
      {/* Page heading */}
      <div>
        <h1 className="text-xl font-semibold tracking-tight">Command Center</h1>
        <p className="text-muted-foreground text-xs mt-0.5">PSX Dividend Intelligence — live market overview</p>
      </div>

      {/* Macro strip */}
      <MacroStrip />

      {/* Main grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        <div className="lg:col-span-2">
          <WatchlistSnapshot />
        </div>
        <UpcomingDividendsWidget />
      </div>
    </div>
  );
}

/* ── Macro strip ──────────────────────────────────────────────────────────── */

function MacroStrip() {
  const { data, isLoading } = useMacro();

  if (isLoading) {
    return (
      <div className="grid grid-cols-3 gap-4">
        {[1, 2, 3].map((i) => (
          <Card key={i} className="border-border/50 card-panel">
            <CardContent className="p-4"><Skeleton className="h-12 w-full rounded-sm" /></CardContent>
          </Card>
        ))}
      </div>
    );
  }

  if (!data) return null;

  const kseUp = (data.kse100ChangePct ?? 0) >= 0;

  return (
    <div className="grid grid-cols-3 gap-4">
      {/* KSE-100 — primary, larger */}
      <Card className={cn(
        'border-border/50 card-panel relative overflow-hidden',
        kseUp ? 'bg-emerald-500/5' : 'bg-red-500/5',
      )}>
        <div className={cn(
          'absolute top-0 left-0 right-0 h-0.5',
          kseUp ? 'bg-emerald-500/60' : 'bg-red-500/60',
        )} />
        <CardContent className="p-4">
          <div className="text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest mb-1.5">KSE-100</div>
          <div className="num text-2xl font-bold tracking-tight">
            {data.kse100Level?.toLocaleString('en-PK', { maximumFractionDigits: 0 }) ?? '—'}
          </div>
          {data.kse100ChangePct != null && (
            <div className={cn(
              'num text-sm flex items-center gap-1 mt-1 font-medium',
              kseUp ? 'text-emerald-500' : 'text-red-500',
            )}>
              {kseUp ? <TrendingUp className="h-3.5 w-3.5" /> : <TrendingDown className="h-3.5 w-3.5" />}
              {kseUp ? '+' : ''}{formatPct(data.kse100ChangePct)} today
            </div>
          )}
        </CardContent>
      </Card>

      {/* SBP / PKR-USD */}
      <Card className="border-border/50 card-panel">
        <CardContent className="p-4">
          <div className="text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest mb-1.5">Policy Rate</div>
          <div className="num text-2xl font-bold tracking-tight">{formatPct(data.sbpRate)}</div>
          <div className="num text-sm text-muted-foreground mt-1">
            PKR/USD: <span className="font-medium text-foreground">{data.pkrUsd?.toFixed(2) ?? '—'}</span>
          </div>
        </CardContent>
      </Card>

      {/* Brent Crude */}
      <Card className="border-border/50 card-panel">
        <CardContent className="p-4">
          <div className="text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest mb-1.5">Brent Crude</div>
          <div className="num text-2xl font-bold tracking-tight">
            ${data.brentCrude?.toFixed(2) ?? '—'}
          </div>
          <div className="text-[11px] text-muted-foreground mt-1">
            As of {data.asOf ? new Date(data.asOf).toLocaleDateString('en-PK', { day: 'numeric', month: 'short' }) : '—'}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

/* ── Watchlist snapshot ───────────────────────────────────────────────────── */

function WatchlistSnapshot() {
  const { data: stocks, isLoading } = useStocks();

  return (
    <Card className="border-border/50 card-panel overflow-hidden">
      <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-semibold">Top Watchlist</CardTitle>
          <Link
            href="/watchlist"
            className="text-[11px] text-muted-foreground/60 hover:text-primary transition-colors flex items-center gap-1"
          >
            View all <ArrowRight className="h-3 w-3" />
          </Link>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        {isLoading ? (
          <div className="px-5 py-4 space-y-2.5">
            {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-9 w-full rounded-sm" />)}
          </div>
        ) : !stocks?.length ? (
          <div className="px-5 py-8 text-sm text-muted-foreground text-center">No stocks in watchlist</div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border/40" style={{ background: 'oklch(1 0 0 / 2%)' }}>
                <th className="text-left pl-5 pr-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Symbol</th>
                <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Price</th>
                <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Yield</th>
                <th className="px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest w-28">Score</th>
                <th className="text-left pr-5 pl-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Signal</th>
              </tr>
            </thead>
            <tbody>
              {[...stocks]
                .sort((a, b) => (b.compositeScore ?? 0) - (a.compositeScore ?? 0))
                .slice(0, 8)
                .map((s) => {
                  const freshness = freshnessBadge(s.scrapedAt);
                  return (
                    <tr key={s.symbol} className="row-hover relative border-b border-border/30 hover:bg-primary/[0.04] transition-colors">
                      <td className="pl-5 pr-3 py-2.5">
                        <div className="flex items-center gap-2">
                          <StockLogo symbol={s.symbol} size={24} />
                          <div>
                            <div className="flex items-center gap-1.5">
                              <Link href={`/stock/${s.symbol}`} className="num font-semibold text-[13px] hover:text-primary transition-colors">
                                {s.symbol}
                              </Link>
                              {freshness === 'stale' && (
                                <AlertCircle className="h-2.5 w-2.5 text-amber-500/70 shrink-0" />
                              )}
                            </div>
                            <div className="text-[10px] text-muted-foreground/60 truncate max-w-[100px] leading-tight">
                              {s.name}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="text-right px-3 py-2.5 whitespace-nowrap">
                        <div className="num text-[13px] font-semibold">{formatRs(s.price)}</div>
                        {s.changePct != null && (
                          <div className={cn(
                            'num text-[10px] leading-tight',
                            s.changePct >= 0 ? 'text-emerald-500' : 'text-red-500',
                          )}>
                            {s.changePct >= 0 ? '+' : ''}{formatPct(s.changePct)}
                          </div>
                        )}
                      </td>
                      <td className="text-right px-3 py-2.5">
                        <span className="num font-semibold text-[13px]">{formatPct(s.dividendYield)}</span>
                      </td>
                      <td className="px-3 py-2.5 w-28">
                        <ScoreBar score={s.compositeScore} />
                      </td>
                      <td className="pr-5 pl-3 py-2.5">
                        <SignalBadge signal={s.signal} />
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

/* ── Upcoming dividends ───────────────────────────────────────────────────── */

function UpcomingDividendsWidget() {
  const { data: dividends, isLoading } = useUpcomingDividends(60);

  return (
    <Card className="border-border/50 card-panel overflow-hidden">
      <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <CalendarDays className="h-3.5 w-3.5 text-primary" />
            Upcoming Ex-Dates
          </CardTitle>
          <Link
            href="/dividends"
            className="text-[11px] text-muted-foreground/60 hover:text-primary transition-colors flex items-center gap-1"
          >
            View all <ArrowRight className="h-3 w-3" />
          </Link>
        </div>
      </CardHeader>
      <CardContent className="p-3 space-y-1">
        {isLoading ? (
          <div className="space-y-1.5">
            {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-12 w-full rounded-sm" />)}
          </div>
        ) : !dividends?.length ? (
          <p className="text-sm text-muted-foreground py-4 text-center">No upcoming dividends (60 days)</p>
        ) : (
          dividends.slice(0, 7).map((d, i) => {
            const urgent = d.daysUntilEx <= 3;
            const soon = d.daysUntilEx <= 7;
            return (
              <div
                key={i}
                className={cn(
                  'flex items-center justify-between px-3 py-2.5 rounded-sm border transition-colors',
                  urgent
                    ? 'bg-red-500/5 border-red-500/15 hover:bg-red-500/10'
                    : soon
                    ? 'bg-amber-500/5 border-amber-500/15 hover:bg-amber-500/10'
                    : 'bg-muted/[0.04] border-border/30 hover:bg-muted/[0.08]',
                )}
              >
                <div>
                  <Link href={`/stock/${d.symbol}`} className="num font-semibold text-[13px] hover:text-primary transition-colors cursor-pointer">
                    {d.symbol}
                  </Link>
                  <div className="flex items-center gap-1 num text-[10px] text-muted-foreground/60 mt-0.5">
                    <Clock className="h-2.5 w-2.5" />
                    Ex: {new Date(d.exDate).toLocaleDateString('en-PK', { day: 'numeric', month: 'short' })}
                    {' · '}{d.daysUntilEx === 0 ? 'today' : `${d.daysUntilEx}d`}
                  </div>
                </div>
                <div className="text-right">
                  <div className="num font-semibold text-[13px]">Rs. {d.dps}</div>
                  <div className={cn(
                    'num text-[10px] font-medium',
                    urgent ? 'text-red-400' : soon ? 'text-amber-400' : 'text-muted-foreground/50',
                  )}>
                    {urgent ? 'Urgent' : soon ? 'Soon' : `${d.daysUntilEx}d away`}
                  </div>
                </div>
              </div>
            );
          })
        )}
      </CardContent>
    </Card>
  );
}
