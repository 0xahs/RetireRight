'use client';

import { cn } from '@/lib/utils';
import type { SafetyTier as SafetyTierType } from '@/lib/api/types';

interface SafetyTierProps {
  tier: SafetyTierType | null | undefined;
  className?: string;
}

const TIER_COLOR: Record<SafetyTierType, string> = {
  S: 'text-emerald-300 bg-emerald-500/15 border-emerald-500/30',
  A: 'text-emerald-400 bg-emerald-500/10 border-emerald-500/25',
  B: 'text-lime-400 bg-lime-500/10 border-lime-500/25',
  C: 'text-amber-400 bg-amber-500/10 border-amber-500/25',
  D: 'text-red-400 bg-red-500/10 border-red-500/25',
  '-': 'text-muted-foreground bg-muted/30 border-border/40',
};

export function SafetyTier({ tier, className }: SafetyTierProps) {
  const t = tier ?? '-';
  return (
    <span
      className={cn(
        'inline-flex items-center justify-center w-[18px] h-[18px] rounded-sm border text-[11px] font-bold tracking-tight font-mono leading-none',
        TIER_COLOR[t],
        className,
      )}
      title={`Safety tier ${t}`}
    >
      {t}
    </span>
  );
}
