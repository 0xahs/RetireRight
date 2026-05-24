'use client';

import { cn } from '@/lib/utils';
import { flagStyle } from '@/lib/utils/format';

// ── Flag badge ────────────────────────────────────────────────────────────────

export function FlagBadge({ flag }: { flag: string }) {
  const { label, className } = flagStyle(flag);
  return (
    <span className={cn(
      'inline-flex items-center px-1.5 py-[2px] rounded-sm text-[10px] font-medium border leading-none tracking-wide',
      className,
    )}>
      {label}
    </span>
  );
}
