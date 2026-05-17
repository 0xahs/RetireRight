'use client';

import { cn } from '@/lib/utils';
import type { SafetyTier as SafetyTierType } from '@/lib/api/types';

interface SafetyTierProps {
  tier: SafetyTierType | null | undefined;
  className?: string;
  size?: 'sm' | 'md';
}

const TIER_COLOR: Record<SafetyTierType, string> = {
  S: 'text-cyan-pos border-[oklch(0.79_0.13_180_/_40%)] bg-[oklch(0.79_0.13_180_/_10%)]',
  A: 'text-cyan-pos border-[oklch(0.79_0.13_180_/_30%)] bg-[oklch(0.79_0.13_180_/_6%)]',
  B: 'text-amber border-[oklch(0.78_0.16_75_/_30%)] bg-[oklch(0.78_0.16_75_/_5%)]',
  C: 'text-amber border-[oklch(0.78_0.16_75_/_45%)] bg-[oklch(0.78_0.16_75_/_10%)]',
  D: 'text-warn border-[oklch(0.68_0.22_25_/_40%)] bg-[oklch(0.68_0.22_25_/_8%)]',
  '-': 'text-warm-dim border-border bg-white/[0.02]',
};

export function SafetyTier({ tier, className, size = 'md' }: SafetyTierProps) {
  const t = tier ?? '-';
  const sizeClass = size === 'sm'
    ? 'w-[14px] h-[14px] text-[9px]'
    : 'w-[18px] h-[18px] text-[11px]';
  return (
    <span
      className={cn(
        'inline-flex items-center justify-center border font-bold tracking-tight font-mono leading-none num',
        sizeClass,
        TIER_COLOR[t],
        className,
      )}
      title={`Safety tier ${t}`}
    >
      {t}
    </span>
  );
}
