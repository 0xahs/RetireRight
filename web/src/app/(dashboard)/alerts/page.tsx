'use client';

import Link from 'next/link';
import { useStocks, useUpcomingDividends } from '@/lib/api/queries';
import { formatRs } from '@/lib/utils/format';
import { cn } from '@/lib/utils';
import { Skeleton } from '@/components/ui/skeleton';
import type { StockSummaryDto, UpcomingDividendDto } from '@/lib/api/types';

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

function EmptySection({ message }: { message: string }) {
  return (
    <p className="text-[10px] text-warm-dim uppercase tracking-[0.15em] font-bold px-3 py-2">
      {message}
    </p>
  );
}

// ── Alert derivation ──────────────────────────────────────────────────────────

function deriveAlerts(stocks: StockSummaryDto[]) {
  const now = new Date();

  const yieldTraps = stocks.filter(
    s => s.dividendCoverage != null && s.dividendCoverage < 1.0,
  );

  const avoidVerdicts = stocks.filter(s => s.verdict === 'AVOID');

  const stale = stocks.filter(s => {
    if (!s.scrapedAt) return true;
    const age = (now.getTime() - new Date(s.scrapedAt).getTime()) / 86_400_000;
    return age > 5;
  });

  return { yieldTraps, avoidVerdicts, stale };
}

function daysAgoLabel(dateStr: string | null): string {
  if (!dateStr) return 'never scraped';
  const days = Math.round((Date.now() - new Date(dateStr).getTime()) / 86_400_000);
  return days === 0 ? 'scraped today' : `${days}d stale`;
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function AlertsPage() {
  const { data: stocks, isLoading: loadingStocks } = useStocks();
  const { data: upcoming, isLoading: loadingDiv } = useUpcomingDividends(7);

  const isLoading = loadingStocks || loadingDiv;

  const { yieldTraps, avoidVerdicts, stale } = stocks
    ? deriveAlerts(stocks)
    : { yieldTraps: [], avoidVerdicts: [], stale: [] };

  const exSoon: UpcomingDividendDto[] = upcoming ?? [];

  const totalAlerts = yieldTraps.length + avoidVerdicts.length + stale.length + exSoon.length;

  return (
    <div className="flex-1 overflow-auto">
      <div className="max-w-3xl mx-auto px-6 py-6">
        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center gap-2 mb-1">
            <span className="h-px w-6 bg-primary/60" />
            <span className="text-[9px] uppercase tracking-[0.22em] text-amber font-bold">SIGNAL</span>
          </div>
          <div className="flex items-center justify-between">
            <h1 className="text-[18px] font-bold uppercase tracking-[0.1em] text-foreground">Alerts</h1>
            {!isLoading && (
              <span className={cn(
                'text-[10px] num font-bold uppercase tracking-[0.12em] px-2 py-1 border',
                totalAlerts > 0
                  ? 'text-warn border-warn/40 bg-warn/5'
                  : 'text-cyan-pos border-cyan-pos/40 bg-cyan-pos/5',
              )}>
                {totalAlerts > 0 ? `${totalAlerts} ACTIVE` : 'ALL CLEAR'}
              </span>
            )}
          </div>
          <p className="text-[11px] text-warm-dim mt-1 uppercase tracking-[0.12em] font-bold">
            Watchlist · yield traps · ex-dates · stale data
          </p>
        </div>

        {isLoading && (
          <div className="space-y-4">
            {[80, 60, 100, 60].map((h, i) => (
              <Skeleton key={i} className={`h-${h === 60 ? 16 : h === 80 ? 20 : 24} rounded-sm`} />
            ))}
          </div>
        )}

        {!isLoading && (
          <div className="space-y-6">

            {/* ── CRITICAL: Yield traps ───────────────────────── */}
            <section>
              <SectionHeader severity="critical" title="Yield Traps — Coverage < 1×" count={yieldTraps.length} />
              {yieldTraps.length === 0 ? (
                <EmptySection message="No yield traps detected" />
              ) : (
                <div className="flex flex-col gap-px">
                  {yieldTraps.map(s => (
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

            {/* ── WARN: AVOID verdicts ─────────────────────────── */}
            <section>
              <SectionHeader severity="warn" title="Avoid Verdicts" count={avoidVerdicts.length} />
              {avoidVerdicts.length === 0 ? (
                <EmptySection message="No AVOID verdicts in watchlist" />
              ) : (
                <div className="flex flex-col gap-px">
                  {avoidVerdicts.map(s => (
                    <AlertRow
                      key={s.symbol}
                      symbol={s.symbol}
                      name={s.name}
                      detail={s.compositeScore != null ? `Score ${Math.round(s.compositeScore)}/100` : 'No score'}
                      severity="warn"
                    />
                  ))}
                </div>
              )}
            </section>

            {/* ── INFO: Upcoming ex-dates ──────────────────────── */}
            <section>
              <SectionHeader severity="info" title={`Ex-Dates ≤ 7 Days`} count={exSoon.length} />
              {exSoon.length === 0 ? (
                <EmptySection message="No ex-dates in the next 7 days" />
              ) : (
                <div className="flex flex-col gap-px">
                  {exSoon.map(d => (
                    <Link
                      key={`${d.symbol}-${d.exDate}`}
                      href={`/stock/${d.symbol}`}
                      className="flex items-center justify-between gap-3 px-3 py-2 border border-border hover:bg-white/[0.03] transition-colors group"
                    >
                      <div className="flex items-center gap-3 min-w-0">
                        <span className="text-[10px] font-bold text-amber num tracking-[0.05em] shrink-0 w-12">{d.symbol}</span>
                        <span className="text-[10px] text-warm-muted truncate group-hover:text-foreground transition-colors">{d.stockName}</span>
                      </div>
                      <div className="flex items-center gap-3 shrink-0">
                        <span className="text-[10px] num text-warm-dim">
                          {formatRs(d.dps)} DPS
                        </span>
                        <span className={cn('text-[10px] num font-bold', d.daysUntilEx <= 2 ? 'text-warn' : 'text-amber')}>
                          {d.daysUntilEx === 0 ? 'TODAY' : d.daysUntilEx === 1 ? 'TOMORROW' : `${d.daysUntilEx}D`}
                        </span>
                      </div>
                    </Link>
                  ))}
                </div>
              )}
            </section>

            {/* ── INFO: Stale data ─────────────────────────────── */}
            <section>
              <SectionHeader severity="info" title="Stale Data (> 5 days)" count={stale.length} />
              {stale.length === 0 ? (
                <EmptySection message="All watchlist data is fresh" />
              ) : (
                <div className="flex flex-col gap-px">
                  {stale.map(s => (
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

          </div>
        )}
      </div>
    </div>
  );
}
