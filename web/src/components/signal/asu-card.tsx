'use client';

import Link from 'next/link';
import { cn } from '@/lib/utils';
import { formatRs, formatPct } from '@/lib/utils/format';
import type { StockSummaryDto } from '@/lib/api/types';
import { VerdictBadge, verdictColor } from './verdict-badge';
import { ConvictionDots } from './conviction-dots';
import { SafetyTier } from './safety-tier';
import { WhyLine } from './why-line';
import { CompareButton } from '@/components/compare/compare-sticky-bar';

interface ASUCardProps {
  stock: StockSummaryDto;
  className?: string;
}

export function ASUCard({ stock, className }: ASUCardProps) {
  const accent = verdictColor(stock.verdict);
  const up = (stock.changePct ?? 0) >= 0;

  const ageHours = stock.scrapedAt
    ? (Date.now() - new Date(stock.scrapedAt).getTime()) / 3_600_000
    : null;
  const ageText =
    ageHours == null ? '—' :
    ageHours < 1 ? '<1H' :
    ageHours < 48 ? `${Math.round(ageHours)}H` :
    `${Math.round(ageHours / 24)}D`;
  const stale = ageHours != null && ageHours > 24;

  return (
    <Link
      href={`/stock/${stock.symbol}`}
      className={cn(
        'group block panel relative overflow-hidden hover:border-primary/40 transition-colors',
        className,
      )}
    >
      {/* Verdict accent rail */}
      <span aria-hidden className="absolute top-0 left-0 right-0 h-[2px]" style={{ background: accent }} />

      {/* PANEL HEADER STRIP */}
      <div className="panel-strip flex items-center justify-between px-3 py-1.5">
        <div className="flex items-center gap-2">
          <VerdictBadge verdict={stock.verdict} size="md" />
          <ConvictionDots level={stock.convictionLevel} confidence={stock.confidence} showLabel />
        </div>
        <span className="text-[9px] tracking-[0.18em] uppercase text-warm-dim font-bold num">
          {ageText} <span className={cn(stale ? 'text-amber' : 'text-warm-dim')}>{stale ? '⚠' : '·'}</span> {stock.metricCompleteness}%
        </span>
      </div>

      {/* MAIN BODY */}
      <div className="px-3 py-3">
        {/* Symbol + Sector + Price */}
        <div className="flex items-start justify-between gap-3 mb-3">
          <div className="min-w-0">
            <div className="flex items-baseline gap-2">
              <span className="num font-bold text-[18px] text-foreground tracking-tight leading-none">{stock.symbol}</span>
              <SafetyTier tier={stock.safetyTier} size="sm" />
            </div>
            <div className="text-[9px] text-warm-dim uppercase tracking-[0.12em] mt-1 truncate max-w-[170px] font-medium">
              {stock.sector} · {stock.name}
            </div>
          </div>
          <div className="text-right shrink-0">
            <div className="num text-[18px] font-bold tracking-tight leading-none">
              {formatRs(stock.price)}
            </div>
            {stock.changePct != null && (
              <div className={cn(
                'num text-[11px] font-semibold leading-tight mt-1 tracking-tight',
                up ? 'text-cyan-pos' : 'text-warn',
              )}>
                {up ? '▲' : '▼'} {Math.abs(stock.changePct).toFixed(2)}%
              </div>
            )}
          </div>
        </div>

        {/* Stats row — terminal layout: 3 cells with gridlines */}
        <div className="grid grid-cols-3 gridline-t gridline-b py-2">
          <Stat label="COMP" value={stock.compositeScore != null ? Math.round(stock.compositeScore).toString() : '—'} accent={stock.compositeScore} />
          <Stat label="NET·Y" value={stock.netYield != null ? `${stock.netYield.toFixed(1)}%` : '—'} className="gridline-l" />
          <Stat label="COV" value={stock.dividendCoverage != null ? `${stock.dividendCoverage.toFixed(2)}x` : '—'} accent={stock.dividendCoverage != null ? Math.min(100, stock.dividendCoverage * 30) : null} className="gridline-l" />
        </div>

        {/* Why / Risks */}
        <div className="pt-3">
          <WhyLine why={stock.whyLine} risks={stock.risks} />
        </div>
      </div>

      {/* FOOTER ACTIONS */}
      <div className="px-3 py-1.5 border-t border-border flex items-center justify-between bg-white/[0.015]">
        <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">
          OPEN ›
        </span>
        <CompareButton symbol={stock.symbol} variant="icon" />
      </div>
    </Link>
  );
}

function Stat({ label, value, accent, className }: { label: string; value: string; accent?: number | null; className?: string }) {
  const tone =
    accent == null ? 'text-foreground' :
    accent >= 70 ? 'text-cyan-pos' :
    accent >= 50 ? 'text-amber' :
    'text-warn';

  return (
    <div className={cn('px-2 first:pl-0', className)}>
      <div className="text-[8.5px] uppercase tracking-[0.18em] text-warm-dim font-bold leading-none">
        {label}
      </div>
      <div className={cn('num text-[15px] font-bold tracking-tight mt-1.5 leading-none', tone)}>
        {value}
      </div>
    </div>
  );
}
