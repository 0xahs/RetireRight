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
  ACCUMULATE: 'oklch(0.79 0.13 180)',   // cyan
  HOLD:       'oklch(0.55 0.015 80)',   // warm gray
  REVIEW:     'oklch(0.78 0.16 75)',    // amber
  AVOID:      'oklch(0.68 0.22 25)',    // red
  WATCH:      'oklch(0.72 0.10 230)',   // muted blue
};

const GAP_COLOR = 'oklch(0.20 0.005 65)';

export function SignalHistorySparkline({ symbol, days = 30, className }: SignalHistorySparklineProps) {
  const { data, isLoading } = useSignalHistory(symbol, days);

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
      <span className={cn('text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold', className)}>
        — NO DATA —
      </span>
    );
  }

  return (
    <div className={cn('flex items-center gap-2.5', className)}>
      <div className="flex items-end gap-[1px]" title={`Last ${days} days of verdict history`}>
        {timeline.map(p => {
          const color = p.verdict ? VERDICT_COLOR[p.verdict] : null;
          return (
            <span
              key={p.date}
              className=""
              style={{
                width: 5,
                height: 11,
                background: color ?? GAP_COLOR,
                opacity: color ? 1 : 0.5,
              }}
              title={`${p.date}: ${p.verdict ?? 'no data'}${p.composite != null ? ` (comp ${Math.round(p.composite)})` : ''}`}
            />
          );
        })}
      </div>
      {stability.verdict && stability.runLength >= 2 && (
        <span className="text-[9px] uppercase tracking-[0.15em] text-warm-muted num font-bold">
          STABLE {stability.runLength}D
        </span>
      )}
    </div>
  );
}
