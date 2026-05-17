'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';
import { useCompareBasket, COMPARE_MAX } from './compare-basket-context';
import { X, GitCompare, ArrowRight } from 'lucide-react';

export function CompareStickyBar() {
  const { symbols, remove, clear } = useCompareBasket();
  const pathname = usePathname();

  // Hide the bar when already on the compare page (it'd be redundant)
  if (symbols.length === 0 || pathname === '/compare') return null;

  const href = `/compare?syms=${symbols.join(',')}`;

  return (
    <div className="fixed bottom-4 left-1/2 -translate-x-1/2 z-40 pointer-events-none">
      <div
        className={cn(
          'pointer-events-auto flex items-center gap-3 px-4 py-2.5 rounded-sm border border-border bg-card/95 backdrop-blur',
          'shadow-[0_10px_30px_oklch(0_0_0_/_40%)]',
        )}
      >
        <div className="flex items-center gap-2">
          <GitCompare className="h-3.5 w-3.5 text-primary" />
          <span className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">
            Compare
          </span>
          <span className="text-[10px] text-muted-foreground/50 num">
            {symbols.length}/{COMPARE_MAX}
          </span>
        </div>

        <div className="flex items-center gap-1.5">
          {symbols.map(sym => (
            <span
              key={sym}
              className="inline-flex items-center gap-1 num font-semibold text-[12px] px-1.5 py-0.5 rounded-sm bg-primary/10 text-primary border border-primary/20"
            >
              {sym}
              <button
                onClick={() => remove(sym)}
                className="hover:text-foreground transition-colors cursor-pointer"
                aria-label={`Remove ${sym}`}
              >
                <X className="h-2.5 w-2.5" />
              </button>
            </span>
          ))}
        </div>

        <div className="flex items-center gap-1 border-l border-border/40 pl-3 ml-1">
          {symbols.length >= 2 ? (
            <Link
              href={href}
              className="inline-flex items-center gap-1.5 text-[11px] font-semibold text-primary hover:text-primary/80 transition-colors px-2.5 py-1 rounded-sm border border-primary/30 hover:bg-primary/5"
            >
              Open compare <ArrowRight className="h-3 w-3" />
            </Link>
          ) : (
            <span className="text-[10px] text-muted-foreground/45 italic">
              Add 1+ more to compare
            </span>
          )}
          <button
            onClick={clear}
            className="text-[10px] text-muted-foreground/45 hover:text-muted-foreground transition-colors px-2 py-1 cursor-pointer"
            aria-label="Clear basket"
          >
            Clear
          </button>
        </div>
      </div>
    </div>
  );
}

/* ── Compare button — used in stock pages and cards ────────────────────── */

interface CompareButtonProps {
  symbol: string;
  variant?: 'inline' | 'icon';
  className?: string;
}

export function CompareButton({ symbol, variant = 'inline', className }: CompareButtonProps) {
  const { has, toggle, isFull } = useCompareBasket();
  const active = has(symbol);
  const disabled = !active && isFull;

  const handle = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!disabled) toggle(symbol);
  };

  if (variant === 'icon') {
    return (
      <button
        onClick={handle}
        disabled={disabled}
        title={active ? 'Remove from Compare' : disabled ? 'Compare basket full' : 'Add to Compare'}
        className={cn(
          'inline-flex items-center justify-center w-6 h-6 rounded-sm border transition-colors cursor-pointer',
          active
            ? 'border-primary/40 bg-primary/10 text-primary'
            : disabled
            ? 'border-border/30 text-muted-foreground/30 cursor-not-allowed'
            : 'border-border/40 text-muted-foreground/60 hover:text-primary hover:border-primary/40',
          className,
        )}
      >
        <GitCompare className="h-3 w-3" />
      </button>
    );
  }

  return (
    <button
      onClick={handle}
      disabled={disabled}
      className={cn(
        'inline-flex items-center gap-1.5 text-[11px] font-semibold px-2.5 py-1 rounded-sm border transition-colors cursor-pointer',
        active
          ? 'border-primary/40 bg-primary/10 text-primary hover:bg-primary/15'
          : disabled
          ? 'border-border/30 text-muted-foreground/30 cursor-not-allowed'
          : 'border-border/40 text-muted-foreground hover:text-primary hover:border-primary/40',
        className,
      )}
    >
      <GitCompare className="h-3 w-3" />
      {active ? 'In compare' : 'Compare'}
    </button>
  );
}
