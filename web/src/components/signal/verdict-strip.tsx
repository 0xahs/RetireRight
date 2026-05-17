'use client';

import { cn } from '@/lib/utils';
import type { StockSummaryDto } from '@/lib/api/types';
import { VerdictBadge, verdictColor } from './verdict-badge';
import { ConvictionDots } from './conviction-dots';
import { SafetyTier } from './safety-tier';
import { WhyLine } from './why-line';
import { SignalHistorySparkline } from './signal-history-sparkline';
import { Clock, Database } from 'lucide-react';

interface VerdictStripProps {
  summary: StockSummaryDto;
  className?: string;
}

export function VerdictStrip({ summary, className }: VerdictStripProps) {
  const accent = verdictColor(summary.verdict);
  const ageHours = summary.scrapedAt
    ? (Date.now() - new Date(summary.scrapedAt).getTime()) / 3_600_000
    : null;
  const ageText =
    ageHours == null ? '—' :
    ageHours < 1 ? '<1h ago' :
    ageHours < 48 ? `${Math.round(ageHours)}h ago` :
    `${Math.round(ageHours / 24)}d ago`;
  const stale = ageHours != null && ageHours > 24;

  return (
    <div
      className={cn(
        'relative rounded-sm border border-border/50 bg-card overflow-hidden',
        className,
      )}
    >
      {/* Verdict accent bar */}
      <span
        aria-hidden
        className="absolute top-0 left-0 right-0 h-[3px]"
        style={{ backgroundColor: accent, opacity: 0.9 }}
      />

      <div className="px-5 py-4">
        {/* Top row: verdict + conviction + freshness */}
        <div className="flex items-center justify-between gap-3 flex-wrap">
          <div className="flex items-center gap-3 flex-wrap">
            <VerdictBadge verdict={summary.verdict} size="lg" />
            <ConvictionDots
              level={summary.convictionLevel}
              confidence={summary.confidence}
              showLabel
            />
            <SafetyTier tier={summary.safetyTier} />
            <span className="text-[10px] text-muted-foreground/55 uppercase tracking-widest font-semibold">
              Safety {summary.safetyTier}
            </span>
          </div>
          <div className="flex items-center gap-3 num text-[10px] text-muted-foreground/55">
            <span className={cn('inline-flex items-center gap-1', stale && 'text-amber-500')}>
              <Clock className="h-2.5 w-2.5" />
              {ageText}
            </span>
            <span className="inline-flex items-center gap-1">
              <Database className="h-2.5 w-2.5" />
              {summary.metricCompleteness}% / {summary.metricsTotal} metrics
            </span>
          </div>
        </div>

        {/* Signal history sparkline */}
        <div className="flex items-center gap-3 mt-3">
          <span className="text-[9px] text-muted-foreground/55 uppercase tracking-widest font-semibold w-20 shrink-0">
            30-day signal
          </span>
          <SignalHistorySparkline symbol={summary.symbol} days={30} />
        </div>

        {/* Component scores strip */}
        <div className="grid grid-cols-2 md:grid-cols-5 gap-2 mt-4 pt-3 border-t border-border/30">
          <ScorePill label="Composite" score={summary.compositeScore} primary />
          <ScorePill label="Safety" score={summary.dividendSafetyScore} />
          <ScorePill label="Growth" score={summary.dividendGrowthScore} />
          <ScorePill label="Valuation" score={summary.valuationScore} />
          <ScorePill label="Opportunity" score={summary.opportunityScore} />
        </div>

        {/* Why + risks */}
        {(summary.whyLine || summary.risks) && (
          <div className="mt-3 pt-3 border-t border-border/30">
            <WhyLine why={summary.whyLine} risks={summary.risks} />
          </div>
        )}
      </div>
    </div>
  );
}

function ScorePill({ label, score, primary }: { label: string; score: number | null; primary?: boolean }) {
  const tone =
    score == null ? 'text-muted-foreground/45' :
    score >= 70 ? 'text-emerald-400' :
    score >= 50 ? 'text-yellow-500' :
    'text-red-400';

  const barColor =
    score == null ? 'oklch(0.4 0.02 250)' :
    score >= 70 ? 'oklch(0.72 0.16 158)' :
    score >= 50 ? 'oklch(0.78 0.15 80)' :
    'oklch(0.62 0.22 22)';

  const pct = score != null ? Math.min(100, Math.max(0, score)) : 0;

  return (
    <div className={cn('rounded-sm px-3 py-2', primary && 'bg-muted/[0.04]')}>
      <div className="flex items-baseline justify-between gap-1">
        <span className="text-[9px] uppercase tracking-widest text-muted-foreground/55 font-semibold">
          {label}
        </span>
        <span className={cn('num text-[15px] font-bold tabular-nums leading-none', tone)}>
          {score != null ? Math.round(score) : '—'}
        </span>
      </div>
      <div className="mt-1.5 h-[2px] w-full rounded-full bg-border/30 overflow-hidden">
        <div
          className="h-full rounded-full transition-all duration-700"
          style={{ width: `${pct}%`, backgroundColor: barColor }}
        />
      </div>
    </div>
  );
}
