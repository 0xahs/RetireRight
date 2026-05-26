'use client';

import Link from 'next/link';
import { useStocks, useUpcomingDividends, useHoldings } from '@/lib/api/queries';
import { formatRs } from '@/lib/utils/format';
import { cn } from '@/lib/utils';
import { Skeleton } from '@/components/ui/skeleton';
import type { StockSummaryDto, UpcomingDividendDto, HoldingDto } from '@/lib/api/types';

// ── Alert primitives ─────────────────────────────────────────────────────────

type Severity = 'critical' | 'warn' | 'info';

const SEVERITY = {
  critical: { dot: 'bg-warn',    label: 'CRITICAL', text: 'text-warn'   },
  warn:     { dot: 'bg-amber',   label: 'WARNING',  text: 'text-amber'  },
  info:     { dot: 'bg-warm-dim',label: 'INFO',     text: 'text-warm-dim'},
} as const;

function SectionHeader({ severity, title, count }: { severity: Severity; title: string; count: number }) {
  const s = SEVERITY[severity];
  return (
    <div className="flex items-center gap-2 mb-2">
      <span className={cn('w-1.5 h-1.5 rounded-full shrink-0', s.dot)} />
      <span className={cn('text-[9px] uppercase tracking-[0.2em] font-bold', s.text)}>
        {title}
      </span>
      <span className="h-px flex-1 bg-border" />
      <span className={cn('text-[9px] num font-bold', s.text)}>{count}</span>
    </div>
  );
}

function AlertRow({
  symbol,
  name,
  detail,
  severity,
}: {
  symbol: string;
  name: string;
  detail: string;
  severity: Severity;
}) {
  return (
    <Link
      href={`/stock/${symbol}`}
      className="flex items-center justify-between gap-3 px-3 py-2 border border-border hover:bg-white/[0.03] transition-colors group"
    >
      <div className="flex items-center gap-3 min-w-0">
        <span className="text-[10px] font-bold text-amber num tracking-[0.05em] shrink-0 w-12">{symbol}</span>
        <span className="text-[10px] text-warm-muted truncate group-hover:text-foreground transition-colors">{name}</span>
      </div>
      <span className={cn('text-[10px] num font-bold shrink-0', {
        'text-warn':     severity === 'critical',
        'text-amber':    severity === 'warn',
        'text-warm-dim': severity === 'info',
      })}>
        {detail}
      </span>
    </Link>
  );
}

function ExDateRow({ d, severity }: { d: UpcomingDividendDto; severity: Severity }) {
  const detailTone = severity === 'critical'
    ? (d.daysUntilEx <= 2 ? 'text-warn' : 'text-amber')
    : (d.daysUntilEx <= 2 ? 'text-warn' : 'text-amber');
  return (
    <Link
      href={`/stock/${d.symbol}`}
      className="flex items-center justify-between gap-3 px-3 py-2 border border-border hover:bg-white/[0.03] transition-colors group"
    >
      <div className="flex items-center gap-3 min-w-0">
        <span className="text-[10px] font-bold text-amber num tracking-[0.05em] shrink-0 w-12">{d.symbol}</span>
        <span className="text-[10px] text-warm-muted truncate group-hover:text-foreground transition-colors">{d.stockName}</span>
      </div>
      <div className="flex items-center gap-3 shrink-0">
        <span className="text-[10px] num text-warm-dim">
          {formatRs(d.dps)} DPS{d.isEstimated ? ' (EST)' : ''}
        </span>
        <span className={cn('text-[10px] num font-bold', detailTone)}>
          {d.daysUntilEx === 0 ? 'TODAY' : d.daysUntilEx === 1 ? 'TOMORROW' : `${d.daysUntilEx}D`}
        </span>
      </div>
    </Link>
  );
}

function EmptySection({ message }: { message: string }) {
  return (
    <p className="text-[10px] text-warm-dim uppercase tracking-[0.15em] font-bold px-3 py-2">
      {message}
    </p>
  );
}

// ── Alert derivation ──────────────────────────────────────────────────────────

interface SectorConcentration {
  sector: string;
  marketValue: number;
  pct: number;
}

function deriveAlerts(
  stocks: StockSummaryDto[],
  holdings: HoldingDto[],
  upcoming: UpcomingDividendDto[],
) {
  const now = new Date();
  const holdingSymbols = new Set(holdings.map(h => h.symbol.toUpperCase()));

  // CRITICAL ──
  const yieldTraps = stocks.filter(
    s => s.dividendCoverage != null && s.dividendCoverage < 1.0,
  );
  const holdingsExSoon = upcoming.filter(
    d => holdingSymbols.has(d.symbol.toUpperCase()) && d.daysUntilEx <= 14,
  );

  // WARN ──
  const concerningVerdicts = stocks.filter(
    s => s.verdict === 'AVOID' || s.verdict === 'REVIEW',
  );

  const totalMarketValue = holdings.reduce((sum, h) => sum + (h.marketValue ?? 0), 0);
  const sectorTotals = new Map<string, number>();
  for (const h of holdings) {
    if (h.marketValue == null) continue;
    sectorTotals.set(h.sector, (sectorTotals.get(h.sector) ?? 0) + h.marketValue);
  }
  const overConcentrated: SectorConcentration[] = totalMarketValue > 0
    ? Array.from(sectorTotals.entries())
        .map(([sector, marketValue]) => ({ sector, marketValue, pct: marketValue / totalMarketValue }))
        .filter(s => s.pct > 0.30)
        .sort((a, b) => b.pct - a.pct)
    : [];

  const redHoldings = holdings.filter(
    h => h.gainLossPct != null && h.gainLossPct < -15,
  );

  const lowConfHighYield = stocks.filter(
    s => s.dividendYield != null && s.dividendYield > 10 && s.confidence < 0.5,
  );

  // INFO ──
  // Non-holdings only — owned ones surface in the CRITICAL section above.
  const generalExSoon = upcoming.filter(
    d => d.daysUntilEx <= 7 && !holdingSymbols.has(d.symbol.toUpperCase()),
  );

  const sharpMoves = stocks.filter(
    s => s.changePct != null && (s.changePct <= -5 || s.changePct >= 7),
  );

  const stale = stocks.filter(s => {
    if (!s.scrapedAt) return true;
    const age = (now.getTime() - new Date(s.scrapedAt).getTime()) / 86_400_000;
    return age > 5;
  });

  const estimatedDivs = upcoming.filter(d => d.isEstimated);

  return {
    yieldTraps, holdingsExSoon,
    concerningVerdicts, overConcentrated, redHoldings, lowConfHighYield,
    generalExSoon, sharpMoves, stale, estimatedDivs,
  };
}

function daysAgoLabel(dateStr: string | null): string {
  if (!dateStr) return 'never scraped';
  const days = Math.round((Date.now() - new Date(dateStr).getTime()) / 86_400_000);
  return days === 0 ? 'scraped today' : `${days}d stale`;
}

function fmtPct(n: number, signed = false): string {
  const v = n.toFixed(1);
  return signed && n > 0 ? `+${v}%` : `${v}%`;
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function AlertsPage() {
  const { data: stocks, isLoading: loadingStocks } = useStocks();
  const { data: upcoming, isLoading: loadingDiv } = useUpcomingDividends(14);
  const { data: holdings, isLoading: loadingHoldings } = useHoldings();

  const isLoading = loadingStocks || loadingDiv || loadingHoldings;

  const a = (stocks && holdings && upcoming)
    ? deriveAlerts(stocks, holdings, upcoming)
    : null;

  const totalAlerts = a
    ? a.yieldTraps.length + a.holdingsExSoon.length
      + a.concerningVerdicts.length + a.overConcentrated.length + a.redHoldings.length + a.lowConfHighYield.length
      + a.generalExSoon.length + a.sharpMoves.length + a.stale.length + a.estimatedDivs.length
    : 0;

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-baseline justify-between gap-3 flex-wrap">
        <div>
          <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
            ┌── ALERTS
          </div>
          <h1 className="text-foreground font-bold text-[20px] tracking-tight">
            ACTION ITEMS · WATCHLIST + PORTFOLIO
          </h1>
        </div>
        {!isLoading && (
          <span className={cn(
            'text-[10px] num font-bold uppercase tracking-[0.12em] px-2 py-1 border self-center',
            totalAlerts > 0
              ? 'text-warn border-warn/40 bg-warn/5'
              : 'text-cyan-pos border-cyan-pos/40 bg-cyan-pos/5',
          )}>
            {totalAlerts > 0 ? `${totalAlerts} ACTIVE` : 'ALL CLEAR'}
          </span>
        )}
      </div>

      {isLoading && (
        <div className="space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-20 rounded-sm" />
          ))}
        </div>
      )}

      {!isLoading && a && (
        <div className="space-y-6">

          {/* ── CRITICAL ────────────────────────────────────────── */}

          <section>
            <SectionHeader severity="critical" title="Yield Traps — Coverage < 1×" count={a.yieldTraps.length} />
            {a.yieldTraps.length === 0 ? (
              <EmptySection message="No yield traps detected" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.yieldTraps.map(s => (
                  <AlertRow
                    key={s.symbol}
                    symbol={s.symbol}
                    name={s.name}
                    detail={`Coverage ${s.dividendCoverage!.toFixed(2)}×`}
                    severity="critical"
                  />
                ))}
              </div>
            )}
          </section>

          <section>
            <SectionHeader
              severity="critical"
              title="Holdings Ex-Date Imminent — Within 14 Days"
              count={a.holdingsExSoon.length}
            />
            {a.holdingsExSoon.length === 0 ? (
              <EmptySection message="No upcoming ex-dates on owned positions" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.holdingsExSoon.map(d => (
                  <ExDateRow key={`${d.symbol}-${d.exDate}`} d={d} severity="critical" />
                ))}
              </div>
            )}
          </section>

          {/* ── WARN ────────────────────────────────────────────── */}

          <section>
            <SectionHeader
              severity="warn"
              title="AVOID / REVIEW Verdicts"
              count={a.concerningVerdicts.length}
            />
            {a.concerningVerdicts.length === 0 ? (
              <EmptySection message="No AVOID or REVIEW verdicts in watchlist" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.concerningVerdicts.map(s => (
                  <AlertRow
                    key={s.symbol}
                    symbol={s.symbol}
                    name={s.name}
                    detail={`${s.verdict} · ${s.compositeScore != null ? `${Math.round(s.compositeScore)}/100` : 'no score'}`}
                    severity="warn"
                  />
                ))}
              </div>
            )}
          </section>

          <section>
            <SectionHeader
              severity="warn"
              title="Sector Over-Concentration — > 30% of Portfolio"
              count={a.overConcentrated.length}
            />
            {a.overConcentrated.length === 0 ? (
              <EmptySection message="Portfolio is diversified across sectors" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.overConcentrated.map(s => (
                  <div
                    key={s.sector}
                    className="flex items-center justify-between gap-3 px-3 py-2 border border-border"
                  >
                    <div className="flex items-center gap-3 min-w-0">
                      <span className="text-[10px] font-bold text-amber num tracking-[0.05em] shrink-0">SECTOR</span>
                      <span className="text-[10px] text-warm-muted truncate">{s.sector}</span>
                    </div>
                    <span className="text-[10px] num font-bold text-amber shrink-0">
                      {fmtPct(s.pct * 100)} · {formatRs(s.marketValue)}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </section>

          <section>
            <SectionHeader
              severity="warn"
              title="Holdings In The Red — Loss > 15%"
              count={a.redHoldings.length}
            />
            {a.redHoldings.length === 0 ? (
              <EmptySection message="No holdings down more than 15%" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.redHoldings.map(h => (
                  <AlertRow
                    key={h.id}
                    symbol={h.symbol}
                    name={h.name}
                    detail={`${fmtPct(h.gainLossPct!, true)} · ${formatRs(h.gainLoss ?? 0)}`}
                    severity="warn"
                  />
                ))}
              </div>
            )}
          </section>

          <section>
            <SectionHeader
              severity="warn"
              title="High Yield + Low Confidence — Possible Trap"
              count={a.lowConfHighYield.length}
            />
            {a.lowConfHighYield.length === 0 ? (
              <EmptySection message="No high-yield / low-confidence pairs" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.lowConfHighYield.map(s => (
                  <AlertRow
                    key={s.symbol}
                    symbol={s.symbol}
                    name={s.name}
                    detail={`Yield ${s.dividendYield!.toFixed(1)}% · Conf ${(s.confidence * 100).toFixed(0)}%`}
                    severity="warn"
                  />
                ))}
              </div>
            )}
          </section>

          {/* ── INFO ────────────────────────────────────────────── */}

          <section>
            <SectionHeader
              severity="info"
              title="Watchlist Ex-Dates ≤ 7 Days (Non-Holdings)"
              count={a.generalExSoon.length}
            />
            {a.generalExSoon.length === 0 ? (
              <EmptySection message="No non-holding ex-dates in the next 7 days" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.generalExSoon.map(d => (
                  <ExDateRow key={`${d.symbol}-${d.exDate}`} d={d} severity="info" />
                ))}
              </div>
            )}
          </section>

          <section>
            <SectionHeader
              severity="info"
              title="Sharp 1-Day Move (≤ −5% or ≥ +7%)"
              count={a.sharpMoves.length}
            />
            {a.sharpMoves.length === 0 ? (
              <EmptySection message="No notable single-day moves today" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.sharpMoves.map(s => (
                  <AlertRow
                    key={s.symbol}
                    symbol={s.symbol}
                    name={s.name}
                    detail={fmtPct(s.changePct!, true)}
                    severity="info"
                  />
                ))}
              </div>
            )}
          </section>

          <section>
            <SectionHeader severity="info" title="Stale Data (> 5 days)" count={a.stale.length} />
            {a.stale.length === 0 ? (
              <EmptySection message="All watchlist data is fresh" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.stale.map(s => (
                  <AlertRow
                    key={s.symbol}
                    symbol={s.symbol}
                    name={s.name}
                    detail={daysAgoLabel(s.scrapedAt)}
                    severity="info"
                  />
                ))}
              </div>
            )}
          </section>

          <section>
            <SectionHeader
              severity="info"
              title="Estimated Upcoming DPS — Verify Before Relying"
              count={a.estimatedDivs.length}
            />
            {a.estimatedDivs.length === 0 ? (
              <EmptySection message="All upcoming DPS values are confirmed" />
            ) : (
              <div className="flex flex-col gap-px">
                {a.estimatedDivs.map(d => (
                  <ExDateRow key={`${d.symbol}-${d.exDate}`} d={d} severity="info" />
                ))}
              </div>
            )}
          </section>

        </div>
      )}
    </div>
  );
}
