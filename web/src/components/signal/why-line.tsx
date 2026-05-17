'use client';

import { cn } from '@/lib/utils';

interface WhyLineProps {
  why?: string | null;
  risks?: string | null;
  className?: string;
}

export function WhyLine({ why, risks, className }: WhyLineProps) {
  if (!why && !risks) return null;

  return (
    <div className={cn('space-y-[3px] text-[11px] leading-snug num', className)}>
      {why && (
        <div className="flex items-baseline gap-2">
          <span className="text-cyan-pos font-bold tracking-[0.18em] uppercase text-[9px] shrink-0 w-9">
            WHY
          </span>
          <span className="text-foreground/85">{why}</span>
        </div>
      )}
      {risks && (
        <div className="flex items-baseline gap-2">
          <span className="text-warn font-bold tracking-[0.18em] uppercase text-[9px] shrink-0 w-9">
            RISK
          </span>
          <span className="text-foreground/70">{risks}</span>
        </div>
      )}
    </div>
  );
}
