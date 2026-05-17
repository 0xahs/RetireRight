'use client';

import { cn } from '@/lib/utils';
import type { Verdict } from '@/lib/api/types';

interface VerdictBadgeProps {
  verdict: Verdict | null | undefined;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  /** When true, render with strong amber/cyan/red background. When false, just colored text. */
  filled?: boolean;
}

const VERDICT_CONFIG: Record<Verdict, { label: string; marker: string; color: string; filled: string }> = {
  ACCUMULATE: {
    label: 'ACCUMULATE',
    marker: '▲',
    color: 'text-cyan-pos border-current',
    filled: 'bg-[oklch(0.79_0.13_180_/_15%)] text-cyan-pos border-[oklch(0.79_0.13_180_/_40%)]',
  },
  HOLD: {
    label: 'HOLD',
    marker: '◆',
    color: 'text-warm-muted border-current',
    filled: 'bg-white/[0.04] text-warm-muted border-border',
  },
  REVIEW: {
    label: 'REVIEW',
    marker: '!',
    color: 'text-amber border-current',
    filled: 'bg-[oklch(0.78_0.16_75_/_15%)] text-amber border-[oklch(0.78_0.16_75_/_40%)]',
  },
  AVOID: {
    label: 'AVOID',
    marker: '✕',
    color: 'text-warn border-current',
    filled: 'bg-[oklch(0.68_0.22_25_/_15%)] text-warn border-[oklch(0.68_0.22_25_/_40%)]',
  },
  WATCH: {
    label: 'WATCH',
    marker: '◐',
    color: 'text-[oklch(0.72_0.10_230)] border-current',
    filled: 'bg-[oklch(0.72_0.10_230_/_15%)] text-[oklch(0.72_0.10_230)] border-[oklch(0.72_0.10_230_/_40%)]',
  },
};

const SIZE_CLASSES = {
  sm: 'text-[9px] px-1 py-[1px] gap-1',
  md: 'text-[10px] px-1.5 py-[2px] gap-1',
  lg: 'text-[11px] px-2 py-[3px] gap-1.5',
};

export function VerdictBadge({ verdict, size = 'md', className, filled = true }: VerdictBadgeProps) {
  if (!verdict) {
    return (
      <span className={cn(
        'inline-flex items-center border bg-white/[0.02] text-warm-dim font-bold tracking-[0.18em] uppercase',
        SIZE_CLASSES[size],
        className,
      )}>—</span>
    );
  }

  const config = VERDICT_CONFIG[verdict];

  return (
    <span className={cn(
      'inline-flex items-center border font-bold tracking-[0.15em] uppercase whitespace-nowrap rounded-none num',
      SIZE_CLASSES[size],
      filled ? config.filled : config.color,
      className,
    )}>
      <span className="font-mono" aria-hidden>{config.marker}</span>
      {config.label}
    </span>
  );
}

export function verdictColor(verdict: Verdict | null | undefined): string {
  if (!verdict) return 'oklch(0.55 0 0)';
  return {
    ACCUMULATE: 'oklch(0.79 0.13 180)',
    HOLD:       'oklch(0.62 0.015 80)',
    REVIEW:     'oklch(0.78 0.16 75)',
    AVOID:      'oklch(0.68 0.22 25)',
    WATCH:      'oklch(0.72 0.10 230)',
  }[verdict];
}
