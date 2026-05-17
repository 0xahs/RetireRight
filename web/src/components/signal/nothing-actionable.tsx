'use client';

import Link from 'next/link';
import { cn } from '@/lib/utils';

interface NothingActionableProps {
  trackedCount: number;
  nextEventLabel?: string;
  className?: string;
}

export function NothingActionable({ trackedCount, nextEventLabel, className }: NothingActionableProps) {
  return (
    <div className={cn('panel relative', className)}>
      {/* Cyan rail — calm signal */}
      <span aria-hidden className="absolute top-0 left-0 right-0 h-[2px]" style={{ background: 'var(--verdict-acc)' }} />

      <div className="panel-strip flex items-center justify-between px-4 py-1.5">
        <span className="text-cyan-pos font-bold text-[10px] tracking-[0.18em] uppercase">
          ▌ STATUS · STABLE
        </span>
        <span className="text-[9px] tracking-[0.18em] uppercase text-warm-dim font-bold num">
          {trackedCount} STOCKS · ALL SIGNALS HELD
        </span>
      </div>

      <div className="px-6 py-10 text-center">
        <div className="text-cyan-pos text-[11px] font-bold tracking-[0.25em] uppercase mb-3">
          NO ACTION REQUIRED
        </div>
        <h3 className="text-foreground font-bold text-[18px] tracking-tight">
          Nothing actionable today.
        </h3>
        <p className="text-warm-muted text-[11px] mt-2 uppercase tracking-[0.12em] font-medium num">
          DATA FRESH · ALL VERDICTS STABLE · DOING NOTHING IS CORRECT
        </p>
        {nextEventLabel && (
          <p className="text-[10px] text-warm-dim mt-4 uppercase tracking-[0.15em] font-bold">
            NEXT EVENT · <span className="text-foreground/85">{nextEventLabel}</span>
          </p>
        )}
        <div className="flex items-center justify-center gap-1 mt-6">
          <Link
            href="/watchlist"
            className="inline-flex items-center text-[10px] uppercase tracking-[0.18em] text-warm-muted hover:text-amber transition-colors px-3 py-1.5 border border-border hover:border-primary/50 font-bold"
          >
            [ OPEN · WATCHLIST ]
          </Link>
          <Link
            href="/dividends"
            className="inline-flex items-center text-[10px] uppercase tracking-[0.18em] text-warm-muted hover:text-amber transition-colors px-3 py-1.5 border border-border hover:border-primary/50 font-bold"
          >
            [ OPEN · CALENDAR ]
          </Link>
        </div>
      </div>
    </div>
  );
}
