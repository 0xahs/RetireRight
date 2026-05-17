'use client';

import { useUpcomingDividends } from '@/lib/api/queries';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';

function urgencyClass(days: number) {
  if (days <= 3) return 'text-warn border-[oklch(0.68_0.22_25_/_40%)] bg-[oklch(0.68_0.22_25_/_10%)]';
  if (days <= 7) return 'text-amber border-[oklch(0.78_0.16_75_/_40%)] bg-[oklch(0.78_0.16_75_/_10%)]';
  return 'text-warm-muted border-border bg-white/[0.02]';
}

function urgencyBar(days: number) {
  if (days <= 3) return 'verdict-bar-avd';
  if (days <= 7) return 'verdict-bar-rev';
  return '';
}

export default function DividendsPage() {
  const { data: dividends, isLoading } = useUpcomingDividends(90);

  return (
    <div className="space-y-4">
      <div>
        <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
          ┌── DIVIDEND CALENDAR
        </div>
        <h1 className="text-foreground font-bold text-[20px] tracking-tight">
          EX-DATES · NEXT 90 DAYS
          {dividends && (
            <span className="text-warm-muted text-[12px] ml-2">
              · <span className="num">{dividends.length}</span> UPCOMING
            </span>
          )}
        </h1>
      </div>

      <div className="panel">
        <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ UPCOMING EX-DATES</span>
          <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">RED ≤3D · AMBER ≤7D</span>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-1.5">
            {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-10 w-full" style={{ borderRadius: 0 }} />)}
          </div>
        ) : !dividends?.length ? (
          <p className="px-6 py-10 text-[11px] uppercase tracking-[0.18em] text-warm-dim text-center font-bold">
            NONE · 90D HORIZON
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-[11px] num">
              <thead>
                <tr className="border-b border-border bg-white/[0.015]">
                  <th className="text-left pl-4 pr-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">STOCK</th>
                  <th className="text-left px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">EX DATE</th>
                  <th className="text-left px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">LAST BUY</th>
                  <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">DPS</th>
                  <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">PAYMENT</th>
                  <th className="text-right pr-4 pl-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">DAYS</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {dividends.map((d, i) => (
                  <tr key={i} className={cn('hover:bg-white/[0.025] transition-colors', urgencyBar(d.daysUntilEx))}>
                    <td className="pl-4 pr-3 py-2">
                      <Link href={`/stock/${d.symbol}`} className="font-bold text-[12px] text-foreground tracking-[0.05em] hover:text-amber transition-colors">
                        {d.symbol}
                      </Link>
                      <div className="text-[9px] uppercase tracking-[0.12em] text-warm-dim truncate max-w-[160px] mt-0.5 font-medium">
                        {d.stockName}
                      </div>
                    </td>
                    <td className="px-3 py-2 text-foreground uppercase tracking-[0.05em]">
                      {new Date(d.exDate).toLocaleDateString('en-PK', { weekday: 'short', month: 'short', day: '2-digit' }).toUpperCase()}
                    </td>
                    <td className="px-3 py-2 text-warm-muted uppercase tracking-[0.05em]">
                      {new Date(d.lastBuyDate).toLocaleDateString('en-PK', { month: 'short', day: '2-digit' }).toUpperCase()}
                    </td>
                    <td className="text-right px-3 py-2 font-bold text-foreground">Rs.{d.dps}</td>
                    <td className="text-right px-3 py-2 text-warm-muted uppercase tracking-[0.05em]">
                      {d.paymentDate
                        ? new Date(d.paymentDate).toLocaleDateString('en-PK', { month: 'short', day: '2-digit' }).toUpperCase()
                        : '—'}
                    </td>
                    <td className="text-right pr-4 pl-3 py-2">
                      <span className={cn(
                        'inline-block text-[9px] font-bold px-2 py-0.5 border uppercase tracking-[0.15em]',
                        urgencyClass(d.daysUntilEx),
                      )}>
                        {d.daysUntilEx === 0 ? 'TODAY' : `${d.daysUntilEx}D`}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
