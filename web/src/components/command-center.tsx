'use client';

import { useMemo } from 'react';
import { useStocks, useUpcomingDividends } from '@/lib/api/queries';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import type { StockSummaryDto, Verdict } from '@/lib/api/types';
import { ASUCard } from '@/components/signal/asu-card';
import { NothingActionable } from '@/components/signal/nothing-actionable';
import { yieldTone, changeTone, changeMarker } from '@/lib/utils/format';

/* ── Cockpit picker ──────────────────────────────────────────────────────── */

function pickCockpitStocks(stocks: StockSummaryDto[]): {
  cockpit: StockSummaryDto[];
  hasUrgent: boolean;
} {
  if (!stocks?.length) return { cockpit: [], hasUrgent: false };

  const byVerdict = (v: Verdict) => stocks.filter(s => s.verdict === v);
  const accumulates = byVerdict('ACCUMULATE').sort((a, b) => (b.compositeScore ?? 0) - (a.compositeScore ?? 0));
  const watches = byVerdict('WATCH').sort((a, b) => (b.opportunityScore ?? 0) - (a.opportunityScore ?? 0));
  const holds = byVerdict('HOLD')
    .filter(s => s.convictionLevel !== 'LOW')
    .sort((a, b) => (b.compositeScore ?? 0) - (a.compositeScore ?? 0));

  const slot1 = accumulates[0] ?? watches[0] ?? holds[0];

  const avoids = byVerdict('AVOID').sort((a, b) => (a.compositeScore ?? 0) - (b.compositeScore ?? 0));
  const reviews = byVerdict('REVIEW');
  const slot2 = avoids[0] ?? reviews[0];

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
  if (counts.ACCUMULATE) parts.push(`${counts.ACCUMULATE} ACC`);
  if (counts.REVIEW) parts.push(`${counts.REVIEW} REV`);
  if (counts.AVOID) parts.push(`${counts.AVOID} AVD`);
  if (counts.WATCH) parts.push(`${counts.WATCH} WCH`);
  if (counts.HOLD) parts.push(`${counts.HOLD} HLD`);
  return parts.join(' · ');
}

/* ── Page ─────────────────────────────────────────────────────────────────── */

export function CommandCenterContent() {
  return (
    <div className="space-y-5">
      <PageHeading />
      <CockpitStrip />
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2">
          <WatchlistPulse />
        </div>
        <UpcomingDividendsWidget />
      </div>
    </div>
  );
}

function PageHeading() {
  const { data: stocks } = useStocks();
  const line = stocks ? summaryLine(stocks) : 'LOADING SIGNALS…';
  const now = new Date();
  const dateStr = now.toLocaleDateString('en-PK', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  const hour = now.getHours();
  const greeting = hour < 12 ? 'GOOD MORNING' : hour < 17 ? 'GOOD AFTERNOON' : 'GOOD EVENING';

  return (
    <div className="flex items-baseline justify-between gap-3 flex-wrap">
      <div>
        <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
          ┌── {greeting} · AHMAD
        </div>
        <h1 className="text-foreground font-bold text-[20px] tracking-tight">
          PSX INTEL · COCKPIT
        </h1>
      </div>
      <div className="text-right">
        <div className="text-[9px] text-warm-dim font-bold tracking-[0.18em] uppercase num">
          {dateStr.toUpperCase()}
        </div>
        <div className="text-[10px] text-amber font-bold tracking-[0.15em] uppercase mt-1 num">
          {line}
        </div>
      </div>
    </div>
  );
}

/* ── COCKPIT ─────────────────────────────────────────────────────────────── */

function CockpitStrip() {
  const { data: stocks, isLoading } = useStocks();
  const { data: upcoming } = useUpcomingDividends(30);

  const { cockpit, hasUrgent } = useMemo(() => pickCockpitStocks(stocks ?? []), [stocks]);

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        {[0, 1, 2].map(i => <Skeleton key={i} className="h-64 w-full" style={{ borderRadius: 0 }} />)}
      </div>
    );
  }
  if (!stocks?.length) return null;

  if (!hasUrgent) {
    const nextEvent = upcoming?.[0]
      ? `${upcoming[0].symbol} EX-DATE IN ${upcoming[0].daysUntilEx}D`
      : undefined;
    return <NothingActionable trackedCount={stocks.length} nextEventLabel={nextEvent} />;
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
      {cockpit.map(stock => <ASUCard key={stock.symbol} stock={stock} />)}
    </div>
  );
}

/* ── WATCHLIST PULSE ─────────────────────────────────────────────────────── */

function WatchlistPulse() {
  const { data: stocks, isLoading } = useStocks();

  return (
    <div className="panel">
      <div className="panel-strip flex items-center justify-between px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
          ▌ WATCHLIST · PULSE
        </span>
        <Link
          href="/watchlist"
          className="text-[9px] uppercase tracking-[0.18em] text-warm-dim hover:text-amber transition-colors font-bold"
        >
          [ FULL TABLE ›]
        </Link>
      </div>

      {isLoading ? (
        <div className="p-4 space-y-1.5">
          {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-8 w-full" style={{ borderRadius: 0 }} />)}
        </div>
      ) : !stocks?.length ? (
        <div className="px-4 py-10 text-center text-[11px] uppercase tracking-[0.18em] text-warm-dim font-bold">
          NO STOCKS · ADD VIA WATCHLIST
        </div>
      ) : (
        <PulseList stocks={stocks} />
      )}
    </div>
  );
}

function PulseList({ stocks }: { stocks: StockSummaryDto[] }) {
  const ranked = [...stocks].sort((a, b) => {
    const order: Record<Verdict, number> = {
      ACCUMULATE: 0, REVIEW: 1, AVOID: 2, WATCH: 3, HOLD: 4,
    };
    const diff = order[a.verdict] - order[b.verdict];
    if (diff !== 0) return diff;
    return (b.compositeScore ?? 0) - (a.compositeScore ?? 0);
  });

  return (
    <div className="divide-y divide-border">
      {/* Header row */}
      <div className="grid grid-cols-[110px_72px_1fr_72px_72px_56px] items-center px-4 py-1.5 text-[8.5px] uppercase tracking-[0.18em] text-warm-dim font-bold bg-white/[0.015]">
        <span>SYMBOL</span>
        <span>VERDICT</span>
        <span>WHY · RISK</span>
        <span className="text-right">NET·Y</span>
        <span className="text-right">CHG</span>
        <span className="text-right">AGE</span>
      </div>
      {ranked.map(s => <PulseRow key={s.symbol} stock={s} />)}
    </div>
  );
}

function PulseRow({ stock }: { stock: StockSummaryDto }) {
  const ageHours = stock.scrapedAt
    ? (Date.now() - new Date(stock.scrapedAt).getTime()) / 3_600_000
    : null;
  const ageText = ageHours == null ? '—' :
    ageHours < 1 ? '<1H' :
    ageHours < 48 ? `${Math.round(ageHours)}H` :
    `${Math.round(ageHours / 24)}D`;
  const stale = ageHours != null && ageHours > 24;

  const verdictBarClass =
    stock.verdict === 'ACCUMULATE' ? 'verdict-bar-acc' :
    stock.verdict === 'REVIEW' ? 'verdict-bar-rev' :
    stock.verdict === 'AVOID' ? 'verdict-bar-avd' :
    stock.verdict === 'WATCH' ? 'verdict-bar-wch' :
    'verdict-bar-hold';

  const verdictDotColor =
    stock.verdict === 'ACCUMULATE' ? 'bg-[oklch(0.79_0.13_180)]' :
    stock.verdict === 'REVIEW' ? 'bg-[oklch(0.78_0.16_75)]' :
    stock.verdict === 'AVOID' ? 'bg-[oklch(0.68_0.22_25)]' :
    stock.verdict === 'WATCH' ? 'bg-[oklch(0.72_0.10_230)]' :
    'bg-[oklch(0.55_0.015_80)]';

  const verdictTone =
    stock.verdict === 'ACCUMULATE' ? 'text-cyan-pos' :
    stock.verdict === 'REVIEW' ? 'text-amber' :
    stock.verdict === 'AVOID' ? 'text-warn' :
    stock.verdict === 'WATCH' ? 'text-[oklch(0.72_0.10_230)]' :
    'text-warm-muted';

  const whyText = stock.whyLine
    ? <span className="text-foreground/80">{stock.whyLine}</span>
    : stock.risks
    ? <span className="text-warn/85">{stock.risks}</span>
    : <span className="text-warm-dim">—</span>;

  return (
    <Link
      href={`/stock/${stock.symbol}`}
      className={cn(
        'grid grid-cols-[110px_72px_1fr_72px_72px_56px] items-center px-4 py-[7px] hover:bg-amber-500/[0.04] transition-colors group num text-[11px]',
        verdictBarClass,
      )}
    >
      <span className="font-bold text-foreground tracking-[0.05em] group-hover:text-amber transition-colors flex items-center gap-1.5">
        <span className={cn('h-1.5 w-1.5 rounded-full shrink-0', verdictDotColor)} />
        {stock.symbol}
      </span>
      <span className={cn('font-bold tracking-[0.15em] uppercase text-[9px]', verdictTone)}>
        {stock.verdict}
      </span>
      <span className="truncate text-[10px]">{whyText}</span>
      <span className={cn('text-right font-bold', yieldTone(stock.netYield))}>
        {stock.netYield != null ? `${stock.netYield.toFixed(1)}%` : '—'}
      </span>
      <span className={cn('text-right font-bold', changeTone(stock.changePct))}>
        {stock.changePct != null ? `${changeMarker(stock.changePct)} ${Math.abs(stock.changePct).toFixed(2)}` : '—'}
      </span>
      <span className={cn('text-right font-bold text-[10px] tracking-wider', stale ? 'text-warn' : 'text-warm-dim')}>
        {ageText}
      </span>
    </Link>
  );
}

/* ── UPCOMING DIVIDENDS ──────────────────────────────────────────────────── */

function UpcomingDividendsWidget() {
  const { data: dividends, isLoading } = useUpcomingDividends(60);

  return (
    <div className="panel">
      <div className="panel-strip flex items-center justify-between px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
          ▌ EX-DATES · 60D
        </span>
        <Link
          href="/dividends"
          className="text-[9px] uppercase tracking-[0.18em] text-warm-dim hover:text-amber transition-colors font-bold"
        >
          [ FULL ›]
        </Link>
      </div>

      {isLoading ? (
        <div className="p-3 space-y-1.5">
          {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-12 w-full" style={{ borderRadius: 0 }} />)}
        </div>
      ) : !dividends?.length ? (
        <div className="px-3 py-10 text-center text-[11px] uppercase tracking-[0.18em] text-warm-dim font-bold">
          NONE · 60D HORIZON
        </div>
      ) : (
        <div className="divide-y divide-border">
          {dividends.slice(0, 8).map((d, i) => {
            const urgent = d.daysUntilEx <= 3;
            const soon = d.daysUntilEx <= 7;
            const tone = urgent ? 'text-warn' : soon ? 'text-amber' : 'text-warm-muted';
            const barClass = urgent ? 'verdict-bar-avd' : soon ? 'verdict-bar-rev' : 'row-hover';
            const dotColor = urgent
              ? 'bg-[oklch(0.68_0.22_25)]'
              : soon
              ? 'bg-[oklch(0.78_0.16_75)]'
              : 'bg-[oklch(0.55_0.015_80)]';
            return (
              <Link
                key={i}
                href={`/stock/${d.symbol}`}
                className={cn(
                  'flex items-center justify-between px-3 py-2 hover:bg-amber-500/[0.04] transition-colors num',
                  barClass,
                )}
              >
                <div className="flex items-center gap-2">
                  <span className={cn('h-1.5 w-1.5 rounded-full shrink-0', dotColor)} />
                  <div>
                    <div className="font-bold text-foreground text-[12px] tracking-[0.05em]">{d.symbol}</div>
                    <div className="text-[9px] uppercase tracking-[0.15em] text-warm-dim mt-0.5 font-bold">
                      EX {new Date(d.exDate).toLocaleDateString('en-PK', { day: '2-digit', month: 'short' }).toUpperCase()} · {d.daysUntilEx === 0 ? 'TODAY' : `${d.daysUntilEx}D`}
                    </div>
                  </div>
                </div>
                <div className="text-right">
                  <div className="font-bold text-foreground text-[12px]">Rs.{d.dps}</div>
                  <div className={cn('text-[9px] uppercase tracking-[0.15em] font-bold mt-0.5', tone)}>
                    {urgent ? '⚠ URGENT' : soon ? '◐ SOON' : `${d.daysUntilEx}D AWAY`}
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
