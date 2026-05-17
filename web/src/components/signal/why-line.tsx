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
    <div className={cn('space-y-1 text-[11px] leading-snug', className)}>
      {why && (
        <div className="flex items-start gap-1.5">
          <span className="text-emerald-400/70 font-semibold tracking-wide uppercase text-[9px] mt-px shrink-0 w-9">
            Why
          </span>
          <span className="text-foreground/80">{why}</span>
        </div>
      )}
      {risks && (
        <div className="flex items-start gap-1.5">
          <span className="text-red-400/70 font-semibold tracking-wide uppercase text-[9px] mt-px shrink-0 w-9">
            Risk
          </span>
          <span className="text-foreground/65">{risks}</span>
        </div>
      )}
    </div>
  );
}
