'use client';

import { useMemo } from 'react';
import { useStocks, useMacro, useUpcomingDividends } from '@/lib/api/queries';
import { formatPct } from '@/lib/utils/format';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { TrendingUp, TrendingDown, CalendarDays, Clock, ArrowRight } from 'lucide-react';
import type { StockSummaryDto, Verdict } from '@/lib/api/types';
import { ASUCard } from '@/components/signal/asu-card';
import { NothingActionable } from '@/components/signal/nothing-actionable';

/* ── Cockpit picker ──────────────────────────────────────────────────────────
   Picks at most 3 stocks for the ASU strip:
     1 ACCUMULATE (highest composite)  · or fallback to WATCH or top HOLD
     1 REVIEW or AVOID (most urgent)   · or skip if none
     1 OPPORTUNITY (high opportunity score, climbing)
   Cap = 3; deduplicate by symbol. */

function pickCockpitStocks(stocks: StockSummaryDto[]): {
  cockpit: StockSummaryDto[];
  hasUrgent: boolean;
} {
  if (!stocks?.length) return { cockpit: [], hasUrgent: false };

  const byVerdict = (v: Verdict) => stocks.filter(s => s.verdict === v);

  // Slot 1: Best ACCUMULATE; fallback to WATCH (climbing); else best HOLD with conviction
  const accumulates = byVerdict('ACCUMULATE').sort((a, b) => (b.compositeScore ?? 0) - (a.compositeScore ?? 0));
  const watches = byVerdict('WATCH').sort((a, b) => (b.opportunityScore ?? 0) - (a.opportunityScore ?? 0));
  const holds = byVerdict('HOLD')
    .filter(s => s.convictionLevel !== 'LOW')
    .sort((a, b) => (b.compositeScore ?? 0) - (a.compositeScore ?? 0));

  const slot1 = accumulates[0] ?? watches[0] ?? holds[0];

  // Slot 2: AVOID first (most urgent), else REVIEW
  const avoids = byVerdict('AVOID').sort((a, b) => (a.compositeScore ?? 0) - (b.compositeScore ?? 0));
  const reviews = byVerdict('REVIEW');
  const slot2 = avoids[0] ?? reviews[0];

  // Slot 3: Highest opportunity score that isn't in slots 1/2
  const picked = new Set([slot1?.symbol, slot2?.symbol].filter(Boolean));
  const slot3 = [...stocks]
    .filter(s => !picked.has(s.symbol))
    .sort((a, b) => (b.opportunityScore ?? 0) - (a.opportunityScore ?? 0))[0];

  const cockpit = [slot1, slot2, slot3]
    .filter((s): s is StockSummaryDto => Boolean(s))
    .filter((s, i, arr) => arr.findIndex(x => x.symbol === s.symbol) === i)
    .slice(0, 3);

  const hasUrgent = avoids.length > 0 || reviews.length > 0 || accumulates.length > 0;

  return { cockpit, hasUrgent };
}

function summaryLine(stocks: StockSummaryDto[]): string {
  const counts = { ACCUMULATE: 0, HOLD: 0, REVIEW: 0, AVOID: 0, WATCH: 0 };
  for (const s of stocks) counts[s.verdict] = (counts[s.verdict] ?? 0) + 1;
  const parts: string[] = [];
  if (counts.ACCUMULATE) parts.push(`${counts.ACCUMULATE} accumulate`);
  if (counts.REVIEW) parts.push(`${counts.REVIEW} review`);
  if (counts.AVOID) parts.push(`${counts.AVOID} risk`);
  if (counts.WATCH) parts.push(`${counts.WATCH} watching`);
  return parts.length ? `Today: ${parts.join(' · ')}` : `${stocks.length} stocks · all signals stable`;
}

/* ── Page ─────────────────────────────────────────────────────────────────── */

export function CommandCenterContent() {
  return (
    <div className="space-y-6">
      <PageHeading />
      <MacroStrip />
      <CockpitStrip />
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        <div className="lg:col-span-2">
          <PortfolioPulse />
        </div>
        <UpcomingDividendsWidget />
      </div>
    </div>
  );
}

function PageHeading() {
  const { data: stocks } = useStocks();
  const line = stocks ? summaryLine(stocks) : 'Loading signals…';
  const hour = new Date().getHours();
  const greeting = hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening';

  return (
    <div>
      <h1 className="text-xl font-semibold tracking-tight">{greeting}, Ahmad.</h1>
      <p className="text-muted-foreground text-xs mt-0.5">{line}</p>
    </div>
  );
}

/* ── Macro strip (unchanged from existing implementation) ─────────────────── */

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

      <Card className="border-border/50 card-panel">
        <CardContent className="p-4">
          <div className="text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest mb-1.5">Policy Rate</div>
          <div className="num text-2xl font-bold tracking-tight">{formatPct(data.sbpRate)}</div>
          <div className="num text-sm text-muted-foreground mt-1">
            PKR/USD: <span className="font-medium text-foreground">{data.pkrUsd?.toFixed(2) ?? '—'}</span>
          </div>
        </CardContent>
      </Card>

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

/* ── The Cockpit: 3 ASU cards or NothingActionable ────────────────────────── */

function CockpitStrip() {
  const { data: stocks, isLoading } = useStocks();
  const { data: upcoming } = useUpcomingDividends(30);

  const { cockpit, hasUrgent } = useMemo(() => pickCockpitStocks(stocks ?? []), [stocks]);

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {[0, 1, 2].map(i => <Skeleton key={i} className="h-56 w-full rounded-sm" />)}
      </div>
    );
  }

  if (!stocks?.length) return null;

  // "Nothing actionable" criteria: no ACCUMULATE, no REVIEW, no AVOID
  // (HOLD-only days = doing nothing is correct)
  if (!hasUrgent) {
    const nextEvent = upcoming?.[0]
      ? `${upcoming[0].symbol} ex-date in ${upcoming[0].daysUntilEx}d`
      : undefined;
    return <NothingActionable trackedCount={stocks.length} nextEventLabel={nextEvent} />;
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      {cockpit.map(stock => (
        <ASUCard key={stock.symbol} stock={stock} />
      ))}
    </div>
  );
}

/* ── Portfolio pulse — secondary widget (renamed from Watchlist Snapshot) ── */

function PortfolioPulse() {
  const { data: stocks, isLoading } = useStocks();

  return (
    <Card className="border-border/50 card-panel overflow-hidden">
      <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-semibold">Watchlist Pulse</CardTitle>
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
          <PulseList stocks={stocks} />
        )}
      </CardContent>
    </Card>
  );
}

function PulseList({ stocks }: { stocks: StockSummaryDto[] }) {
  // Show ALL stocks (not just sorted by composite). Group implicit by verdict urgency.
  const ranked = [...stocks].sort((a, b) => {
    const order: Record<Verdict, number> = {
      ACCUMULATE: 0, REVIEW: 1, AVOID: 2, WATCH: 3, HOLD: 4,
    };
    const diff = order[a.verdict] - order[b.verdict];
    if (diff !== 0) return diff;
    return (b.compositeScore ?? 0) - (a.compositeScore ?? 0);
  });

  return (
    <div className="divide-y divide-border/30">
      {ranked.map(s => (
        <PulseRow key={s.symbol} stock={s} />
      ))}
    </div>
  );
}

function PulseRow({ stock }: { stock: StockSummaryDto }) {
  const ageHours = stock.scrapedAt
    ? (Date.now() - new Date(stock.scrapedAt).getTime()) / 3_600_000
    : null;
  const ageText = ageHours == null ? '—' :
    ageHours < 1 ? '<1h' :
    ageHours < 48 ? `${Math.round(ageHours)}h` :
    `${Math.round(ageHours / 24)}d`;

  const up = (stock.changePct ?? 0) >= 0;
  const accentColor =
    stock.verdict === 'ACCUMULATE' ? 'oklch(0.72 0.16 158)' :
    stock.verdict === 'REVIEW' ? 'oklch(0.78 0.16 80)' :
    stock.verdict === 'AVOID' ? 'oklch(0.62 0.22 22)' :
    stock.verdict === 'WATCH' ? 'oklch(0.72 0.14 230)' :
    'oklch(0.55 0.05 250 / 50%)';

  return (
    <Link
      href={`/stock/${stock.symbol}`}
      className="flex items-center gap-3 px-5 py-2.5 hover:bg-primary/[0.04] transition-colors group"
    >
      <span
        aria-hidden
        className="w-[2px] h-7 rounded-full shrink-0"
        style={{ backgroundColor: accentColor }}
      />
      <div className="flex items-center gap-2 flex-1 min-w-0">
        <span className="num font-semibold text-[13px] w-14 shrink-0 group-hover:text-primary transition-colors">{stock.symbol}</span>
        <span className="text-[11px] text-muted-foreground/55 uppercase tracking-wider font-semibold w-[88px] shrink-0">
          {stock.verdict.toLowerCase()}
        </span>
        <span className="text-[10px] text-muted-foreground/45 num truncate hidden sm:inline">
          {stock.whyLine || stock.risks || '—'}
        </span>
      </div>
      <div className="flex items-center gap-3 shrink-0 num">
        {stock.netYield != null && (
          <span className="text-[11px] text-muted-foreground">
            <span className="font-semibold text-foreground">{stock.netYield.toFixed(1)}%</span>
            <span className="text-muted-foreground/45 ml-0.5">net</span>
          </span>
        )}
        {stock.changePct != null && (
          <span className={cn('text-[11px] w-12 text-right', up ? 'text-emerald-500' : 'text-red-500')}>
            {up ? '+' : ''}{stock.changePct.toFixed(2)}%
          </span>
        )}
        <span className="text-[10px] text-muted-foreground/45 w-8 text-right">{ageText}</span>
      </div>
    </Link>
  );
}

/* ── Upcoming dividends widget (unchanged behavior) ───────────────────────── */

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
