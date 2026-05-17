'use client';

import { cn } from '@/lib/utils';
import type { StockSummaryDto } from '@/lib/api/types';
import { VerdictBadge, verdictColor } from './verdict-badge';
import { ConvictionDots } from './conviction-dots';
import { SafetyTier } from './safety-tier';
import { WhyLine } from './why-line';
import { SignalHistorySparkline } from './signal-history-sparkline';

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
    ageHours < 1 ? '<1H' :
    ageHours < 48 ? `${Math.round(ageHours)}H` :
    `${Math.round(ageHours / 24)}D`;
  const stale = ageHours != null && ageHours > 24;

  return (
    <div className={cn('panel relative', className)}>
      {/* Verdict accent rail */}
      <span aria-hidden className="absolute top-0 left-0 right-0 h-[2px]" style={{ background: accent }} />

      {/* Panel header strip */}
      <div className="panel-strip flex items-center justify-between px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
          ▌ VERDICT
        </span>
        <span className="text-[9px] tracking-[0.18em] uppercase text-warm-dim font-bold num">
          {ageText} <span className={stale ? 'text-amber' : ''}>{stale ? '⚠ STALE' : ''}</span> · {summary.metricCompleteness}% / {summary.metricsTotal} METRICS
        </span>
      </div>

      <div className="px-4 py-3">
        {/* Verdict row */}
        <div className="flex items-center gap-3 flex-wrap">
          <VerdictBadge verdict={summary.verdict} size="lg" />
          <ConvictionDots level={summary.convictionLevel} confidence={summary.confidence} showLabel />
          <span className="text-warm-dim">|</span>
          <div className="flex items-center gap-1.5">
            <SafetyTier tier={summary.safetyTier} />
            <span className="text-[9px] uppercase tracking-[0.18em] font-bold text-warm-muted">
              SAFETY {summary.safetyTier}
            </span>
          </div>
        </div>

        {/* Signal history */}
        <div className="flex items-center gap-3 mt-3">
          <span className="text-[9px] tracking-[0.18em] uppercase font-bold text-warm-dim w-24 shrink-0">
            30-DAY HISTORY
          </span>
          <SignalHistorySparkline symbol={summary.symbol} days={30} />
        </div>

        {/* Component scores — 5 cells in a single gridlined row */}
        <div className="grid grid-cols-2 md:grid-cols-5 mt-4 border border-border bg-white/[0.015]">
          <ScoreCell label="COMPOSITE" score={summary.compositeScore} primary />
          <ScoreCell label="SAFETY" score={summary.dividendSafetyScore} className="md:gridline-l gridline-l" />
          <ScoreCell label="GROWTH" score={summary.dividendGrowthScore} className="md:gridline-l" />
          <ScoreCell label="VALUATION" score={summary.valuationScore} className="md:gridline-l gridline-l md:gridline-t-0" />
          <ScoreCell label="OPPORTUNITY" score={summary.opportunityScore} className="md:gridline-l" />
        </div>

        {/* Why + risks */}
        {(summary.whyLine || summary.risks) && (
          <div className="mt-3 pt-3 border-t border-border">
            <WhyLine why={summary.whyLine} risks={summary.risks} />
          </div>
        )}
      </div>
    </div>
  );
}

function ScoreCell({ label, score, primary, className }: { label: string; score: number | null; primary?: boolean; className?: string }) {
  const tone =
    score == null ? 'text-warm-dim' :
    score >= 70 ? 'text-cyan-pos' :
    score >= 50 ? 'text-amber' :
    'text-warn';

  const barColor =
    score == null ? 'oklch(0.30 0 0)' :
    score >= 70 ? 'var(--verdict-acc)' :
    score >= 50 ? 'var(--primary)' :
    'var(--destructive)';

  const pct = score != null ? Math.min(100, Math.max(0, score)) : 0;

  return (
    <div className={cn('px-3 py-2.5', primary && 'bg-primary/[0.05]', className)}>
      <div className="flex items-baseline justify-between gap-1">
        <span className="text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold">{label}</span>
        <span className={cn('num text-[17px] font-bold tabular-nums leading-none', tone)}>
          {score != null ? Math.round(score) : '—'}
        </span>
      </div>
      <div className="mt-1.5 h-[2px] w-full bg-white/[0.07]">
        <div
          className="h-full transition-all duration-500"
          style={{ width: `${pct}%`, background: barColor }}
        />
      </div>
    </div>
  );
}
