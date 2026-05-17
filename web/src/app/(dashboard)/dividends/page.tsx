'use client';

import { useUpcomingDividends } from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { CalendarDays } from 'lucide-react';

function urgencyClass(days: number) {
  if (days <= 3) return 'text-red-400 border-red-500/30 bg-red-500/10';
  if (days <= 7) return 'text-amber-400 border-amber-500/30 bg-amber-500/10';
  return 'text-muted-foreground border-border/40 bg-muted/20';
}

export default function DividendsPage() {
  const { data: dividends, isLoading } = useUpcomingDividends(90);

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-semibold tracking-tight">Dividend Calendar</h1>
        <p className="text-muted-foreground text-xs mt-0.5">
          Upcoming ex-dates in the next 90 days
          {dividends && (
            <> · <span className="num text-foreground font-medium">{dividends.length}</span> upcoming</>
          )}
        </p>
      </div>

      <Card className="overflow-hidden border-border/50 card-panel">
        <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
          <div className="flex items-center gap-2">
            <CalendarDays className="h-3.5 w-3.5 text-primary" />
            <CardTitle className="text-sm font-semibold">Upcoming Ex-Dates</CardTitle>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-5 space-y-2">
              {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-10 w-full rounded-sm" />)}
            </div>
          ) : !dividends?.length ? (
            <p className="px-6 py-10 text-sm text-muted-foreground text-center">
              No upcoming dividends in the next 90 days
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-border/40" style={{ background: 'oklch(1 0 0 / 2%)' }}>
                    <th className="text-left pl-5 pr-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Stock</th>
                    <th className="text-left px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Ex Date</th>
                    <th className="text-left px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Last Buy</th>
                    <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">DPS</th>
                    <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Payment</th>
                    <th className="text-right pr-5 pl-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Days</th>
                  </tr>
                </thead>
                <tbody>
                  {dividends.map((d, i) => (
                    <tr
                      key={i}
                      className="row-hover border-b border-border/30 hover:bg-primary/[0.04] transition-colors"
                    >
                      <td className="pl-5 pr-3 py-3">
                        <Link
                          href={`/stock/${d.symbol}`}
                          className="num font-semibold text-[13px] hover:text-primary transition-colors"
                        >
                          {d.symbol}
                        </Link>
                        <div className="text-[11px] text-muted-foreground/60 truncate max-w-[140px] leading-tight mt-0.5">
                          {d.stockName}
                        </div>
                      </td>
                      <td className="px-3 py-3">
                        <span className="num text-[13px]">
                          {new Date(d.exDate).toLocaleDateString('en-PK', { weekday: 'short', month: 'short', day: 'numeric' })}
                        </span>
                      </td>
                      <td className="px-3 py-3">
                        <span className="num text-[13px] text-muted-foreground">
                          {new Date(d.lastBuyDate).toLocaleDateString('en-PK', { month: 'short', day: 'numeric' })}
                        </span>
                      </td>
                      <td className="text-right px-3 py-3">
                        <span className="num font-semibold text-[13px]">Rs. {d.dps}</span>
                      </td>
                      <td className="text-right px-3 py-3">
                        <span className="num text-[13px] text-muted-foreground">
                          {d.paymentDate
                            ? new Date(d.paymentDate).toLocaleDateString('en-PK', { month: 'short', day: 'numeric' })
                            : '—'}
                        </span>
                      </td>
                      <td className="text-right pr-5 pl-3 py-3">
                        <span className={cn(
                          'num text-[11px] font-medium px-2 py-0.5 rounded-sm border',
                          urgencyClass(d.daysUntilEx),
                        )}>
                          {d.daysUntilEx === 0 ? 'Today' : `${d.daysUntilEx}d`}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
