'use client';

import { useMemo } from 'react';
import { cn } from '@/lib/utils';
import { useSignalHistory } from '@/lib/api/queries';
import type { Verdict } from '@/lib/api/types';
import { Skeleton } from '@/components/ui/skeleton';

interface SignalHistorySparklineProps {
  symbol: string;
  days?: number;
  className?: string;
}

const VERDICT_COLOR: Record<Verdict, string> = {
  ACCUMULATE: 'oklch(0.72 0.16 158)',  // emerald
  HOLD:       'oklch(0.55 0.04 250)',  // slate
  REVIEW:     'oklch(0.78 0.16 80)',   // amber
  AVOID:      'oklch(0.62 0.22 22)',   // red
  WATCH:      'oklch(0.72 0.14 230)',  // cyan
};

const GAP_COLOR = 'oklch(0.25 0.02 250)';
const GAP_STRIPE = 'repeating-linear-gradient(135deg, oklch(0.30 0.02 250) 0, oklch(0.30 0.02 250) 1px, oklch(0.22 0.02 250) 1px, oklch(0.22 0.02 250) 3px)';

export function SignalHistorySparkline({ symbol, days = 30, className }: SignalHistorySparklineProps) {
  const { data, isLoading } = useSignalHistory(symbol, days);

  // Build a continuous day-by-day array: today is the rightmost square.
  // Pre-compute timeline so missing-data days render as hatched gray squares.
  const timeline = useMemo(() => {
    if (!data) return [];
    const byDate = new Map(data.points.map(p => [p.date, p]));
    const out: { date: string; verdict: Verdict | null; composite: number | null }[] = [];
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    for (let i = days - 1; i >= 0; i--) {
      const d = new Date(today);
      d.setDate(d.getDate() - i);
      const key = d.toISOString().slice(0, 10);
      const found = byDate.get(key);
      out.push({
        date: key,
        verdict: found?.verdict ?? null,
        composite: found?.composite ?? null,
      });
    }
    return out;
  }, [data, days]);

  // Stability calc: longest run of same verdict ending today (most recent)
  const stability = useMemo(() => {
    if (timeline.length === 0) return { runLength: 0, verdict: null as Verdict | null };
    let last: Verdict | null = null;
    let run = 0;
    for (let i = timeline.length - 1; i >= 0; i--) {
      const v = timeline[i].verdict;
      if (v == null) break;
      if (last == null) {
        last = v;
        run = 1;
      } else if (v === last) {
        run++;
      } else {
        break;
      }
    }
    return { runLength: run, verdict: last };
  }, [timeline]);

  if (isLoading) {
    return <Skeleton className={cn('h-3 w-48', className)} />;
  }

  if (!data || timeline.length === 0) {
    return (
      <span className={cn('text-[10px] text-muted-foreground/45 italic', className)}>
        no history yet
      </span>
    );
  }

  return (
    <div className={cn('flex items-center gap-2', className)}>
      <div className="flex items-end gap-[1px]" title={`Last ${days} days of verdict history`}>
        {timeline.map(p => {
          const color = p.verdict ? VERDICT_COLOR[p.verdict] : null;
          return (
            <span
              key={p.date}
              className="rounded-[1px]"
              style={{
                width: 5,
                height: 12,
                backgroundColor: color ?? GAP_COLOR,
                backgroundImage: color ? undefined : GAP_STRIPE,
                opacity: color ? 0.95 : 0.65,
              }}
              title={`${p.date}: ${p.verdict ?? 'no data'}${p.composite != null ? ` (comp ${Math.round(p.composite)})` : ''}`}
            />
          );
        })}
      </div>
      {stability.verdict && stability.runLength >= 2 && (
        <span className="text-[10px] text-muted-foreground/55 num">
          stable {stability.runLength}d
        </span>
      )}
    </div>
  );
}
