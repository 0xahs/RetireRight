'use client';

import { cn } from '@/lib/utils';
import type { Verdict } from '@/lib/api/types';
import { TrendingUp, Minus, AlertTriangle, XCircle, Eye } from 'lucide-react';

interface VerdictBadgeProps {
  verdict: Verdict | null | undefined;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  showIcon?: boolean;
}

const VERDICT_CONFIG: Record<Verdict, { label: string; icon: typeof TrendingUp; classes: string; ring: string }> = {
  ACCUMULATE: {
    label: 'Accumulate',
    icon: TrendingUp,
    classes: 'text-emerald-400 bg-emerald-500/12 border-emerald-500/30',
    ring: 'ring-emerald-500/30',
  },
  HOLD: {
    label: 'Hold',
    icon: Minus,
    classes: 'text-slate-300 bg-slate-500/10 border-slate-500/25',
    ring: 'ring-slate-500/25',
  },
  REVIEW: {
    label: 'Review',
    icon: AlertTriangle,
    classes: 'text-amber-400 bg-amber-500/12 border-amber-500/30',
    ring: 'ring-amber-500/30',
  },
  AVOID: {
    label: 'Avoid',
    icon: XCircle,
    classes: 'text-red-400 bg-red-500/12 border-red-500/30',
    ring: 'ring-red-500/30',
  },
  WATCH: {
    label: 'Watch',
    icon: Eye,
    classes: 'text-cyan-400 bg-cyan-500/12 border-cyan-500/30',
    ring: 'ring-cyan-500/30',
  },
};

const SIZE_CLASSES = {
  sm: 'text-[10px] px-1.5 py-[2px] gap-1',
  md: 'text-[11px] px-2 py-[3px] gap-1.5',
  lg: 'text-[13px] px-2.5 py-1 gap-2',
};

const ICON_SIZE = { sm: 10, md: 12, lg: 14 };

export function VerdictBadge({ verdict, size = 'md', className, showIcon = true }: VerdictBadgeProps) {
  if (!verdict) {
    return (
      <span className={cn(
        'inline-flex items-center rounded-sm border border-border/40 bg-muted/30 text-muted-foreground font-medium tracking-wide uppercase',
        SIZE_CLASSES[size],
        className,
      )}>
        —
      </span>
    );
  }

  const config = VERDICT_CONFIG[verdict];
  const Icon = config.icon;
  const iconPx = ICON_SIZE[size];

  return (
    <span className={cn(
      'inline-flex items-center rounded-sm border font-semibold tracking-wide uppercase whitespace-nowrap',
      SIZE_CLASSES[size],
      config.classes,
      className,
    )}>
      {showIcon && <Icon style={{ width: iconPx, height: iconPx }} />}
      {config.label}
    </span>
  );
}

export function verdictColor(verdict: Verdict | null | undefined): string {
  if (!verdict) return 'oklch(0.65 0.02 250)';
  return {
    ACCUMULATE: 'oklch(0.72 0.16 158)',
    HOLD: 'oklch(0.65 0.05 250)',
    REVIEW: 'oklch(0.78 0.16 80)',
    AVOID: 'oklch(0.62 0.22 22)',
    WATCH: 'oklch(0.72 0.14 230)',
  }[verdict];
}
