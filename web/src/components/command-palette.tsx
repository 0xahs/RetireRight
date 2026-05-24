'use client';

import { useEffect, useState, useCallback } from 'react';
import { Command } from 'cmdk';
import * as VisuallyHidden from '@radix-ui/react-visually-hidden';
import * as DialogPrimitive from '@radix-ui/react-dialog';
import { StockLogo } from '@/components/stock-logo';
import { useRouter } from 'next/navigation';
import { useStocks } from '@/lib/api/queries';
import { cn } from '@/lib/utils';
import { scoreTone, yieldTone } from '@/lib/utils/format';
import { NAV_ITEMS, NAV_GROUP_LABELS } from '@/lib/nav';
import type { Verdict } from '@/lib/api/types';

const VERDICT_DOT: Record<Verdict, string> = {
  ACCUMULATE: 'bg-[oklch(0.79_0.13_180)]',
  HOLD:       'bg-[oklch(0.55_0.015_80)]',
  REVIEW:     'bg-[oklch(0.78_0.16_75)]',
  AVOID:      'bg-[oklch(0.68_0.22_25)]',
  WATCH:      'bg-[oklch(0.72_0.10_230)]',
};

interface CommandPaletteProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CommandPalette({ open, onOpenChange }: CommandPaletteProps) {
  const router = useRouter();
  const { data: stocks } = useStocks();

  function navigate(href: string) {
    router.push(href);
    onOpenChange(false);
  }

  return (
    <Command.Dialog
      open={open}
      onOpenChange={onOpenChange}
      label="Command Palette"
      className="fixed inset-0 z-50"
    >
      <VisuallyHidden.Root>
        <DialogPrimitive.Title>Command Palette</DialogPrimitive.Title>
      </VisuallyHidden.Root>

      {/* Overlay — slight scanline */}
      <div
        className="absolute inset-0 bg-black/70 scanlines"
        onClick={() => onOpenChange(false)}
      />

      {/* Panel */}
      <div className="relative mx-auto mt-[14vh] w-full max-w-xl">
        <div className="panel-strong bg-card relative">
          {/* Amber accent rail */}
          <span aria-hidden className="absolute top-0 left-0 right-0 h-[2px] bg-primary" />

          {/* Panel-strip header */}
          <div className="panel-strip px-3 py-1.5 flex items-center justify-between">
            <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
              ▌ COMMAND
            </span>
            <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold num">
              SEARCH · NAVIGATE
            </span>
          </div>

          {/* Search input row */}
          <div className="flex items-center gap-3 px-4 py-1 border-b border-border">
            <span className="text-amber text-[12px] font-bold">▶</span>
            <Command.Input
              placeholder="TYPE TO SEARCH · STOCKS / PAGES / METRICS"
              className="flex-1 py-3 bg-transparent num text-[12px] uppercase tracking-[0.05em] font-bold text-foreground placeholder:text-warm-dim focus:outline-none"
            />
            <kbd className="shrink-0 text-[9px] text-warm-dim font-bold border border-border px-1.5 py-0.5 uppercase tracking-[0.15em]">
              ESC
            </kbd>
          </div>

          {/* Results list */}
          <Command.List className="max-h-[380px] overflow-y-auto py-1">
            <Command.Empty className="px-4 py-8 text-center text-[10px] uppercase tracking-[0.18em] text-warm-dim font-bold">
              ░░ NO RESULTS ░░
            </Command.Empty>

            {/* Navigation group */}
            <Command.Group
              heading="NAVIGATION"
              className={cn(
                '[&_[cmdk-group-heading]]:px-4 [&_[cmdk-group-heading]]:py-1.5',
                '[&_[cmdk-group-heading]]:text-[9px] [&_[cmdk-group-heading]]:font-bold',
                '[&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-[0.22em]',
                '[&_[cmdk-group-heading]]:text-amber',
                '[&_[cmdk-group-heading]]:bg-white/[0.015]',
                '[&_[cmdk-group-heading]]:border-y [&_[cmdk-group-heading]]:border-border',
              )}
            >
              {NAV_ITEMS.map((item) => (
                <Command.Item
                  key={item.href}
                  value={`${item.code} ${item.label.toUpperCase()} ${NAV_GROUP_LABELS[item.group]}`}
                  onSelect={() => navigate(item.href)}
                  className={cn(
                    'group flex items-center gap-3 px-4 py-2 cursor-pointer transition-colors num',
                    'data-[selected=true]:bg-primary/[0.08]',
                    'data-[selected=true]:shadow-[inset_3px_0_0_var(--primary)]',
                  )}
                >
                  <span className={cn(
                    'text-[9px] font-bold tabular-nums tracking-[0.05em] w-6',
                    'text-warm-dim group-data-[selected=true]:text-amber',
                  )}>
                    {item.code}
                  </span>
                  <span className={cn(
                    'text-[11px] uppercase tracking-[0.1em] font-bold flex-1',
                    'text-foreground/80 group-data-[selected=true]:text-amber',
                  )}>
                    {item.label.toUpperCase()}
                  </span>
                  <span className={cn(
                    'text-[8px] uppercase tracking-[0.18em] font-bold',
                    'text-warm-dim group-data-[selected=true]:text-amber/60',
                  )}>
                    {NAV_GROUP_LABELS[item.group]}
                  </span>
                  <span className={cn(
                    'text-amber text-[12px] opacity-0',
                    'group-data-[selected=true]:opacity-100',
                  )}>
                    ▶
                  </span>
                </Command.Item>
              ))}
            </Command.Group>

            {/* Stocks group */}
            {stocks && stocks.length > 0 && (
              <Command.Group
                heading="INSTRUMENTS"
                className={cn(
                  '[&_[cmdk-group-heading]]:px-4 [&_[cmdk-group-heading]]:py-1.5',
                  '[&_[cmdk-group-heading]]:text-[9px] [&_[cmdk-group-heading]]:font-bold',
                  '[&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-[0.22em]',
                  '[&_[cmdk-group-heading]]:text-amber',
                  '[&_[cmdk-group-heading]]:bg-white/[0.015]',
                  '[&_[cmdk-group-heading]]:border-y [&_[cmdk-group-heading]]:border-border',
                )}
              >
                {stocks.map((s) => (
                  <Command.Item
                    key={s.symbol}
                    value={`${s.symbol} ${s.name} ${s.sector} ${s.verdict}`}
                    onSelect={() => navigate(`/stock/${s.symbol}`)}
                    className={cn(
                      'group flex items-center gap-3 px-4 py-2 cursor-pointer transition-colors num',
                      'data-[selected=true]:bg-primary/[0.08]',
                      'data-[selected=true]:shadow-[inset_3px_0_0_var(--primary)]',
                    )}
                  >
                    <StockLogo symbol={s.symbol} size={22} />

                    {/* Symbol + verdict dot + name */}
                    <div className="flex-1 min-w-0 flex items-center gap-2">
                      <span className={cn('h-1.5 w-1.5 rounded-full shrink-0', VERDICT_DOT[s.verdict])} />
                      <span className={cn(
                        'font-bold text-[12px] tracking-[0.05em] shrink-0',
                        'text-foreground group-data-[selected=true]:text-amber',
                      )}>
                        {s.symbol}
                      </span>
                      <span className="text-[9px] uppercase tracking-[0.12em] text-warm-dim font-medium truncate">
                        {s.sector}
                      </span>
                    </div>

                    {/* Right side: yield + composite */}
                    <div className="shrink-0 flex items-center gap-3 text-[10px] tabular-nums font-bold">
                      {s.netYield != null && (
                        <span className={cn('w-12 text-right', yieldTone(s.netYield))}>
                          {s.netYield.toFixed(1)}%
                        </span>
                      )}
                      {s.compositeScore != null && (
                        <span className={cn('w-8 text-right', scoreTone(s.compositeScore))}>
                          {Math.round(s.compositeScore)}
                        </span>
                      )}
                    </div>
                  </Command.Item>
                ))}
              </Command.Group>
            )}
          </Command.List>

          {/* Footer — terminal hints */}
          <div className="border-t border-border px-3 py-2 flex items-center justify-between bg-white/[0.015]">
            <div className="flex items-center gap-3 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">
              <span className="flex items-center gap-1.5">
                <kbd className="border border-border px-1 py-px num">↑↓</kbd> NAV
              </span>
              <span className="flex items-center gap-1.5">
                <kbd className="border border-border px-1 py-px num">↵</kbd> OPEN
              </span>
              <span className="flex items-center gap-1.5">
                <kbd className="border border-border px-1 py-px num">ESC</kbd> CLOSE
              </span>
            </div>
            <span className="text-[9px] uppercase tracking-[0.22em] text-amber font-bold">
              ▌ RR · TERMINAL
            </span>
          </div>
        </div>
      </div>
    </Command.Dialog>
  );
}

export function useCommandPalette() {
  const [open, setOpen] = useState(false);

  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
      e.preventDefault();
      setOpen((v) => !v);
    }
  }, []);

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [handleKeyDown]);

  return { open, setOpen };
}
