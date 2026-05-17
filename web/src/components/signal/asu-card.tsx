'use client';

import Link from 'next/link';
import { cn } from '@/lib/utils';
import { formatRs, formatPct, freshnessBadge } from '@/lib/utils/format';
import type { StockSummaryDto } from '@/lib/api/types';
import { VerdictBadge, verdictColor } from './verdict-badge';
import { ConvictionDots } from './conviction-dots';
import { SafetyTier } from './safety-tier';
import { WhyLine } from './why-line';
import { StockLogo } from '@/components/stock-logo';
import { CompareButton } from '@/components/compare/compare-sticky-bar';
import { TrendingUp, TrendingDown, ArrowRight, Clock } from 'lucide-react';

interface ASUCardProps {
  stock: StockSummaryDto;
  className?: string;
}

export function ASUCard({ stock, className }: ASUCardProps) {
  const accentColor = verdictColor(stock.verdict);
  const fresh = freshnessBadge(stock.scrapedAt);
  const upTone = (stock.changePct ?? 0) >= 0;

  const ageHours = stock.scrapedAt
    ? (Date.now() - new Date(stock.scrapedAt).getTime()) / 3_600_000
    : null;

  const ageText = ageHours == null ? 'no data' :
    ageHours < 1 ? '<1h' :
    ageHours < 48 ? `${Math.round(ageHours)}h` :
    `${Math.round(ageHours / 24)}d`;

  return (
    <Link
      href={`/stock/${stock.symbol}`}
      className={cn(
        'group block rounded-sm border border-border/50 bg-card overflow-hidden',
        'hover:border-border transition-colors relative',
        className,
      )}
    >
      {/* Verdict accent bar */}
      <span
        aria-hidden
        className="absolute top-0 left-0 right-0 h-[2px]"
        style={{ backgroundColor: accentColor, opacity: 0.85 }}
      />

      <div className="p-4 space-y-3">
        {/* Top row: verdict + conviction */}
        <div className="flex items-center justify-between">
          <VerdictBadge verdict={stock.verdict} size="md" />
          <ConvictionDots
            level={stock.convictionLevel}
            confidence={stock.confidence}
            showLabel
          />
        </div>

        {/* Symbol + price */}
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-2.5 min-w-0">
            <StockLogo symbol={stock.symbol} size={28} />
            <div className="min-w-0">
              <div className="flex items-center gap-1.5">
                <span className="num font-bold text-[15px] tracking-tight">{stock.symbol}</span>
                <SafetyTier tier={stock.safetyTier} />
              </div>
              <div className="text-[10px] text-muted-foreground/60 truncate max-w-[160px] leading-tight">
                {stock.name}
              </div>
            </div>
          </div>
          <div className="text-right shrink-0">
            <div className="num text-[15px] font-bold tracking-tight leading-none">
              {formatRs(stock.price)}
            </div>
            {stock.changePct != null && (
              <div
                className={cn(
                  'num text-[10px] flex items-center justify-end gap-0.5 leading-tight mt-0.5',
                  upTone ? 'text-emerald-500' : 'text-red-500',
                )}
              >
                {upTone ? <TrendingUp className="h-2.5 w-2.5" /> : <TrendingDown className="h-2.5 w-2.5" />}
                {upTone ? '+' : ''}{formatPct(stock.changePct)}
              </div>
            )}
          </div>
        </div>

        {/* Key stats strip */}
        <div className="grid grid-cols-3 gap-2 pt-1 pb-0.5 border-t border-border/30">
          <Stat label="Composite" value={stock.compositeScore != null ? Math.round(stock.compositeScore).toString() : '—'} accent={stock.compositeScore} />
          <Stat label="Net Yield" value={stock.netYield != null ? `${stock.netYield.toFixed(1)}%` : '—'} />
          <Stat label="Coverage" value={stock.dividendCoverage != null ? `${stock.dividendCoverage.toFixed(2)}x` : '—'} accent={stock.dividendCoverage != null ? Math.min(100, stock.dividendCoverage * 30) : null} />
        </div>

        {/* Why + risk lines */}
        <WhyLine why={stock.whyLine} risks={stock.risks} />

        {/* Footer: source + completeness + arrow */}
        <div className="flex items-center justify-between gap-2 pt-1 border-t border-border/20">
          <div className="flex items-center gap-2 text-[9px] text-muted-foreground/55 num">
            <span className={cn(
              'inline-flex items-center gap-1',
              fresh === 'stale' && 'text-amber-500/70',
            )}>
              <Clock className="h-2 w-2" />
              {ageText}
            </span>
            <span>·</span>
            <span>{stock.metricCompleteness}% metrics</span>
          </div>
          <div className="flex items-center gap-2">
            <CompareButton symbol={stock.symbol} variant="icon" />
            <span className="text-[10px] text-muted-foreground/40 group-hover:text-primary inline-flex items-center gap-0.5 transition-colors">
              Open <ArrowRight className="h-2.5 w-2.5" />
            </span>
          </div>
        </div>
      </div>
    </Link>
  );
}

function Stat({ label, value, accent }: { label: string; value: string; accent?: number | null }) {
  const tone =
    accent == null ? 'text-foreground' :
    accent >= 70 ? 'text-emerald-400' :
    accent >= 50 ? 'text-yellow-500' :
    'text-red-400';

  return (
    <div>
      <div className="text-[9px] text-muted-foreground/55 font-semibold uppercase tracking-wider leading-none">
        {label}
      </div>
      <div className={cn('num text-[13px] font-bold tracking-tight mt-0.5 leading-none', tone)}>
        {value}
      </div>
    </div>
  );
}
