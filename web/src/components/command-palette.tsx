'use client';

import { useEffect, useState, useCallback } from 'react';
import { Command } from 'cmdk';
import { useRouter } from 'next/navigation';
import { useStocks } from '@/lib/api/queries';
import { formatPct, formatScore } from '@/lib/utils/format';
import { cn } from '@/lib/utils';
import {
  LayoutDashboard, List, Filter, CalendarDays,
  Briefcase, Activity, FileJson, Search,
} from 'lucide-react';

const NAV_ITEMS = [
  { label: 'Command Center',   href: '/dashboard',  icon: LayoutDashboard },
  { label: 'Watchlist',        href: '/watchlist',   icon: List },
  { label: 'Screener',         href: '/screener',    icon: Filter },
  { label: 'Dividend Calendar',href: '/dividends',   icon: CalendarDays },
  { label: 'Portfolio',        href: '/portfolio',   icon: Briefcase },
  { label: 'Pipeline Monitor', href: '/pipeline',    icon: Activity },
  { label: 'Export Center',    href: '/export',      icon: FileJson },
];

function signalDot(signal: string | null | undefined): string {
  switch (signal?.toLowerCase()) {
    case 'strongbuy':  return '#10b981';
    case 'buy':        return '#22c55e';
    case 'accumulate': return '#84cc16';
    case 'hold':       return '#eab308';
    case 'sell':       return '#f97316';
    case 'strongsell': return '#ef4444';
    default:           return '#4b5563';
  }
}

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
      {/* Overlay */}
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-[2px]"
        onClick={() => onOpenChange(false)}
      />

      {/* Panel */}
      <div className="relative mx-auto mt-[15vh] w-full max-w-lg">
        <div className="bg-card border border-border/60 rounded-sm shadow-2xl overflow-hidden"
          style={{ boxShadow: '0 25px 60px oklch(0 0 0 / 60%), inset 0 1px 0 oklch(1 0 0 / 8%)' }}
        >
          {/* Search input row */}
          <div className="flex items-center gap-3 px-4 border-b border-border/40">
            <Search className="h-3.5 w-3.5 text-muted-foreground/50 shrink-0" />
            <Command.Input
              placeholder="Search stocks, pages…"
              className="flex-1 py-3.5 bg-transparent num text-[13px] text-foreground placeholder:text-muted-foreground/40 focus:outline-none"
            />
            <kbd className="shrink-0 text-[9px] text-muted-foreground/40 font-mono border border-border/30 rounded px-1 py-0.5">
              ESC
            </kbd>
          </div>

          {/* Results list */}
          <Command.List className="max-h-[360px] overflow-y-auto py-2">
            <Command.Empty className="px-4 py-8 text-center text-[12px] text-muted-foreground/50">
              No results found.
            </Command.Empty>

            {/* Navigation group */}
            <Command.Group
              heading="Navigation"
              className="[&_[cmdk-group-heading]]:px-4 [&_[cmdk-group-heading]]:py-1.5 [&_[cmdk-group-heading]]:text-[9px] [&_[cmdk-group-heading]]:font-semibold [&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-widest [&_[cmdk-group-heading]]:text-muted-foreground/40"
            >
              {NAV_ITEMS.map((item) => {
                const Icon = item.icon;
                return (
                  <Command.Item
                    key={item.href}
                    value={item.label}
                    onSelect={() => navigate(item.href)}
                    className={cn(
                      'group flex items-center gap-3 px-4 py-2.5 cursor-pointer transition-colors',
                      'data-[selected=true]:bg-primary/[0.08]',
                    )}
                  >
                    <Icon className="h-3.5 w-3.5 text-muted-foreground/50 shrink-0 group-data-[selected=true]:text-primary" />
                    <span className="text-[13px] text-foreground/80 group-data-[selected=true]:text-foreground">
                      {item.label}
                    </span>
                  </Command.Item>
                );
              })}
            </Command.Group>

            {/* Stocks group */}
            {stocks && stocks.length > 0 && (
              <>
                <div className="my-1 mx-4 border-t border-border/20" />
                <Command.Group
                  heading="Stocks"
                  className="[&_[cmdk-group-heading]]:px-4 [&_[cmdk-group-heading]]:py-1.5 [&_[cmdk-group-heading]]:text-[9px] [&_[cmdk-group-heading]]:font-semibold [&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-widest [&_[cmdk-group-heading]]:text-muted-foreground/40"
                >
                  {stocks.map((s) => (
                    <Command.Item
                      key={s.symbol}
                      value={`${s.symbol} ${s.name} ${s.sector}`}
                      onSelect={() => navigate(`/stock/${s.symbol}`)}
                      className={cn(
                        'group flex items-center gap-3 px-4 py-2.5 cursor-pointer transition-colors',
                        'data-[selected=true]:bg-primary/[0.08]',
                      )}
                    >
                      {/* Signal dot */}
                      <span
                        className="h-1.5 w-1.5 rounded-full shrink-0 mt-0.5"
                        style={{ backgroundColor: signalDot(s.signal) }}
                      />

                      {/* Symbol + name */}
                      <div className="flex-1 min-w-0">
                        <span className="num font-semibold text-[13px] text-foreground group-data-[selected=true]:text-primary">
                          {s.symbol}
                        </span>
                        <span className="ml-2 text-[11px] text-muted-foreground/60 truncate">
                          {s.name}
                        </span>
                      </div>

                      {/* Right side: yield + score */}
                      <div className="shrink-0 flex items-center gap-3 num text-[11px] text-muted-foreground/50">
                        {s.dividendYield != null && (
                          <span>{formatPct(s.dividendYield)}</span>
                        )}
                        {s.compositeScore != null && (
                          <span className={cn(
                            'font-medium',
                            s.compositeScore >= 70 ? 'text-emerald-500/70' :
                            s.compositeScore >= 50 ? 'text-yellow-500/70' :
                            'text-red-500/70',
                          )}>
                            {formatScore(s.compositeScore)}
                          </span>
                        )}
                      </div>
                    </Command.Item>
                  ))}
                </Command.Group>
              </>
            )}
          </Command.List>

          {/* Footer hint */}
          <div className="border-t border-border/30 px-4 py-2 flex items-center gap-4 bg-muted/[0.03]">
            <span className="num text-[9px] text-muted-foreground/35 flex items-center gap-1">
              <kbd className="border border-border/30 rounded px-1">↑↓</kbd> navigate
            </span>
            <span className="num text-[9px] text-muted-foreground/35 flex items-center gap-1">
              <kbd className="border border-border/30 rounded px-1">↵</kbd> select
            </span>
            <span className="num text-[9px] text-muted-foreground/35 flex items-center gap-1">
              <kbd className="border border-border/30 rounded px-1">esc</kbd> close
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
