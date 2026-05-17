'use client';

import Link from 'next/link';
import { CheckCircle2, ArrowRight } from 'lucide-react';
import { cn } from '@/lib/utils';

interface NothingActionableProps {
  trackedCount: number;
  nextEventLabel?: string;
  className?: string;
}

export function NothingActionable({ trackedCount, nextEventLabel, className }: NothingActionableProps) {
  return (
    <div className={cn(
      'rounded-sm border border-emerald-500/20 bg-emerald-500/[0.03] py-10 px-6 text-center',
      className,
    )}>
      <CheckCircle2 className="h-6 w-6 text-emerald-500/70 mx-auto mb-3" />
      <h3 className="text-base font-semibold text-foreground tracking-tight">
        Nothing actionable today.
      </h3>
      <p className="text-[12px] text-muted-foreground/70 mt-1.5 num">
        {trackedCount} stocks tracked · all signals stable · data fresh
      </p>
      {nextEventLabel && (
        <p className="text-[11px] text-muted-foreground/55 mt-3">
          Next scheduled event: <span className="text-foreground/80">{nextEventLabel}</span>
        </p>
      )}
      <div className="flex items-center justify-center gap-2 mt-5">
        <Link
          href="/watchlist"
          className="inline-flex items-center gap-1 text-[11px] text-muted-foreground hover:text-primary transition-colors px-3 py-1.5 rounded-sm border border-border/40 hover:border-primary/40"
        >
          Open watchlist <ArrowRight className="h-3 w-3" />
        </Link>
        <Link
          href="/dividends"
          className="inline-flex items-center gap-1 text-[11px] text-muted-foreground hover:text-primary transition-colors px-3 py-1.5 rounded-sm border border-border/40 hover:border-primary/40"
        >
          Open calendar <ArrowRight className="h-3 w-3" />
        </Link>
      </div>
    </div>
  );
}
