'use client';

import { useUpcomingDividends } from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import Link from 'next/link';

export default function DividendsPage() {
  const { data: dividends, isLoading } = useUpcomingDividends(90);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Dividend Calendar</h1>
        <p className="text-muted-foreground text-sm mt-1">Upcoming ex-dates in the next 90 days</p>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            Upcoming Ex-Dates
            {dividends && <Badge variant="secondary">{dividends.length} upcoming</Badge>}
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="px-6 pb-4 space-y-2">
              {[1, 2, 3, 4, 5].map((i) => <Skeleton key={i} className="h-12 w-full" />)}
            </div>
          ) : !dividends?.length ? (
            <p className="px-6 pb-4 text-sm text-muted-foreground">No upcoming dividends in the next 90 days</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border">
                  <th className="text-left px-6 py-2 text-xs text-muted-foreground font-medium">Stock</th>
                  <th className="text-left px-3 py-2 text-xs text-muted-foreground font-medium">Ex Date</th>
                  <th className="text-left px-3 py-2 text-xs text-muted-foreground font-medium">Last Buy Date</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">DPS</th>
                  <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Payment Date</th>
                  <th className="text-right px-6 py-2 text-xs text-muted-foreground font-medium">Days Until Ex</th>
                </tr>
              </thead>
              <tbody>
                {dividends.map((d, i) => (
                  <tr key={i} className="border-b border-border/50 hover:bg-accent/30 transition-colors">
                    <td className="px-6 py-2.5">
                      <Link href={`/stock/${d.symbol}`} className="font-medium hover:text-primary transition-colors">
                        {d.symbol}
                      </Link>
                      <div className="text-xs text-muted-foreground">{d.stockName}</div>
                    </td>
                    <td className="px-3 py-2.5 text-sm">
                      {new Date(d.exDate).toLocaleDateString('en-PK', { weekday: 'short', month: 'short', day: 'numeric' })}
                    </td>
                    <td className="px-3 py-2.5 text-sm text-muted-foreground">
                      {new Date(d.lastBuyDate).toLocaleDateString('en-PK', { month: 'short', day: 'numeric' })}
                    </td>
                    <td className="text-right px-3 py-2.5 font-medium">Rs. {d.dps}</td>
                    <td className="text-right px-3 py-2.5 text-sm text-muted-foreground">
                      {d.paymentDate ? new Date(d.paymentDate).toLocaleDateString('en-PK', { month: 'short', day: 'numeric' }) : '-'}
                    </td>
                    <td className="text-right px-6 py-2.5">
                      <Badge variant={d.daysUntilEx <= 3 ? 'destructive' : d.daysUntilEx <= 7 ? 'default' : 'secondary'}>
                        {d.daysUntilEx === 0 ? 'Today' : `${d.daysUntilEx}d`}
                      </Badge>
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
