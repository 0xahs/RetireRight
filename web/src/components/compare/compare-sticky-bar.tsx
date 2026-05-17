'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';
import { useCompareBasket, COMPARE_MAX } from './compare-basket-context';

export function CompareStickyBar() {
  const { symbols, remove, clear } = useCompareBasket();
  const pathname = usePathname();

  if (symbols.length === 0 || pathname === '/compare') return null;

  const href = `/compare?syms=${symbols.join(',')}`;

  return (
    <div className="fixed bottom-3 left-1/2 -translate-x-1/2 z-40 pointer-events-none">
      <div className="pointer-events-auto panel-strong flex items-stretch border bg-background relative">
        <span aria-hidden className="absolute top-0 left-0 right-0 h-[2px] bg-primary" />

        <span className="px-3 py-2 flex items-center gap-2 border-r border-border bg-card">
          <span className="text-amber font-bold text-[10px] tracking-[0.2em] uppercase">▌ CMP</span>
          <span className="text-[9px] text-warm-dim font-bold num tracking-wider">
            {symbols.length}/{COMPARE_MAX}
          </span>
        </span>

        <div className="flex items-stretch divide-x divide-border">
          {symbols.map(sym => (
            <span
              key={sym}
              className="inline-flex items-center gap-1.5 px-3 num font-bold text-[12px] text-foreground hover:bg-white/[0.025] tracking-[0.05em]"
            >
              {sym}
              <button
                onClick={() => remove(sym)}
                className="text-warm-dim hover:text-warn transition-colors cursor-pointer text-[10px] leading-none font-bold"
                aria-label={`Remove ${sym}`}
              >
                ✕
              </button>
            </span>
          ))}
        </div>

        <div className="flex items-stretch border-l border-border">
          {symbols.length >= 2 ? (
            <Link
              href={href}
              className="px-3 py-2 inline-flex items-center text-[10px] uppercase tracking-[0.18em] font-bold text-amber hover:bg-primary/10 transition-colors"
            >
              [ OPEN COMPARE › ]
            </Link>
          ) : (
            <span className="px-3 py-2 inline-flex items-center text-[10px] uppercase tracking-[0.18em] font-bold text-warm-dim">
              ADD 1+ MORE
            </span>
          )}
          <button
            onClick={clear}
            className="px-2 py-2 text-[9px] uppercase tracking-[0.18em] font-bold text-warm-dim hover:text-warn transition-colors border-l border-border cursor-pointer"
            aria-label="Clear basket"
          >
            CLEAR
          </button>
        </div>
      </div>
    </div>
  );
}

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
        title={active ? 'In compare basket' : disabled ? 'Basket full' : 'Add to compare'}
        className={cn(
          'inline-flex items-center justify-center w-5 h-5 border transition-colors cursor-pointer text-[10px] font-bold',
          active
            ? 'border-amber bg-primary/15 text-amber'
            : disabled
            ? 'border-border text-warm-dim cursor-not-allowed'
            : 'border-border text-warm-muted hover:text-amber hover:border-primary/50',
          className,
        )}
      >
        +
      </button>
    );
  }

  return (
    <button
      onClick={handle}
      disabled={disabled}
      className={cn(
        'inline-flex items-center gap-1.5 text-[10px] uppercase tracking-[0.18em] font-bold px-2.5 py-1.5 border transition-colors cursor-pointer',
        active
          ? 'border-amber bg-primary/10 text-amber hover:bg-primary/20'
          : disabled
          ? 'border-border text-warm-dim cursor-not-allowed'
          : 'border-border text-warm-muted hover:text-amber hover:border-primary/50',
        className,
      )}
    >
      {active ? '[ ✓ IN COMPARE ]' : '[ + COMPARE ]'}
    </button>
  );
}
