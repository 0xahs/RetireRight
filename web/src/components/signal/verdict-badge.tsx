'use client';

import type { ElementType } from 'react';
import { TrendingUp, Minus, AlertTriangle, Ban, Eye } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { Verdict } from '@/lib/api/types';

interface VerdictBadgeProps {
  verdict: Verdict | null | undefined;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  /** When true, render with strong amber/cyan/red background. When false, just colored text. */
  filled?: boolean;
}

const VERDICT_CONFIG: Record<Verdict, { label: string; Icon: ElementType; color: string; filled: string }> = {
  ACCUMULATE: {
    label: 'ACCUMULATE',
    Icon: TrendingUp,
    color: 'text-cyan-pos border-current',
    filled: 'bg-[oklch(0.79_0.13_180_/_12%)] text-cyan-pos border-[oklch(0.79_0.13_180_/_35%)]',
  },
  HOLD: {
    label: 'HOLD',
    Icon: Minus,
    color: 'text-warm-muted border-current',
    filled: 'bg-white/[0.04] text-warm-muted border-border',
  },
  REVIEW: {
    label: 'REVIEW',
    Icon: AlertTriangle,
    color: 'text-amber border-current',
    filled: 'bg-[oklch(0.78_0.16_75_/_12%)] text-amber border-[oklch(0.78_0.16_75_/_35%)]',
  },
  AVOID: {
    label: 'AVOID',
    Icon: Ban,
    color: 'text-warn border-current',
    filled: 'bg-[oklch(0.68_0.22_25_/_12%)] text-warn border-[oklch(0.68_0.22_25_/_35%)]',
  },
  WATCH: {
    label: 'WATCH',
    Icon: Eye,
    color: 'text-[oklch(0.72_0.10_230)] border-current',
    filled: 'bg-[oklch(0.72_0.10_230_/_12%)] text-[oklch(0.72_0.10_230)] border-[oklch(0.72_0.10_230_/_35%)]',
  },
};

// chip text/padding + matching icon px, kept in sync so the glyph optically centers
const SIZE_CLASSES: Record<NonNullable<VerdictBadgeProps['size']>, { chip: string; icon: number }> = {
  sm: { chip: 'text-[9px] px-1.5 py-[2px] gap-1',    icon: 10 },
  md: { chip: 'text-[10px] px-2 py-[3px] gap-1.5',   icon: 11 },
  lg: { chip: 'text-[11px] px-2.5 py-1 gap-1.5',     icon: 13 },
};

export function VerdictBadge({ verdict, size = 'md', className, filled = true }: VerdictBadgeProps) {
  const sz = SIZE_CLASSES[size];

  if (!verdict) {
    return (
      <span className={cn(
        'inline-flex items-center border bg-white/[0.02] text-warm-dim font-bold tracking-[0.18em] uppercase leading-none',
        sz.chip,
        className,
      )}>—</span>
    );
  }

  const { label, Icon, color, filled: filledCls } = VERDICT_CONFIG[verdict];

  return (
    <span className={cn(
      'inline-flex items-center border font-bold tracking-[0.12em] uppercase whitespace-nowrap rounded-none leading-none num',
      sz.chip,
      filled ? filledCls : color,
      className,
    )}>
      <Icon size={sz.icon} strokeWidth={2.5} className="shrink-0 -ml-0.5" aria-hidden />
      <span className="relative top-px">{label}</span>
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
