'use client';

import { usePathname } from 'next/navigation';
import { signOut } from 'next-auth/react';
import { LogOut, Search } from 'lucide-react';
import { CommandPalette, useCommandPalette } from '@/components/command-palette';
import { useMacro } from '@/lib/api/queries';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { cn } from '@/lib/utils';

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

const ROUTE_LABELS: Record<string, string> = {
  '/dashboard':  'BRIEF',
  '/watchlist':  'WATCHLIST',
  '/screener':   'SCREENER',
  '/compare':    'COMPARE',
  '/dividends':  'CALENDAR',
  '/portfolio':  'PORTFOLIO',
  '/pipeline':   'PIPELINE',
  '/export':     'EXPORT',
};

function usePageTitle(pathname: string): { section: string; page: string } {
  if (pathname.startsWith('/stock/')) {
    const symbol = pathname.split('/')[2]?.toUpperCase() ?? '';
    return { section: 'WATCHLIST', page: symbol };
  }
  if (pathname === '/portfolio/plan') {
    return { section: 'PORTFOLIO', page: 'MONTHLY PLAN' };
  }
  const label = ROUTE_LABELS[pathname] ?? 'RETIRERIGHT';
  return { section: '', page: label };
}

export function Topbar() {
  const pathname = usePathname();
  const { section, page } = usePageTitle(pathname);
  const { open, setOpen } = useCommandPalette();
  const { data: macro } = useMacro();

  // Tab title: "<section> · <page>" or just "<page>"
  const tabTitle = section ? `${section} · ${page}` : page;
  useDocumentTitle(tabTitle === 'RETIRERIGHT' ? null : tabTitle);

  return (
    <>
      {/* TICKER STRIP — Bloomberg signature */}
      <div className="h-7 shrink-0 flex items-center px-3 border-b border-border bg-background overflow-hidden">
        <span className="text-primary text-[9px] font-bold tracking-[0.18em] mr-3">▌ MARKET</span>
        <TickerSegment label="KSE-100" value={macro?.kse100Level?.toLocaleString('en-PK', { maximumFractionDigits: 0 })} change={macro?.kse100ChangePct} />
        <TickerSegment label="SBP" value={fmtPct(macro?.sbpRate)} />
        <TickerSegment label="PKR/USD" value={macro?.pkrUsd?.toFixed(2)} />
        <TickerSegment label="BRENT" value={macro?.brentCrude != null ? `$${macro.brentCrude.toFixed(2)}` : '—'} />
        <div className="ml-auto flex items-center gap-2">
          <span className="text-[9px] text-warm-dim tracking-[0.15em]">PKT</span>
          <Clock />
        </div>
      </div>

      {/* MAIN BAR */}
      <header className="h-10 shrink-0 flex items-center justify-between pl-4 pr-3 border-b border-border bg-card/40">
        {/* Breadcrumb — bracket markers */}
        <nav className="flex items-center gap-2 text-[11px]">
          <span className="text-primary font-bold tracking-[0.05em]">
            {section ? `${section}` : page}
          </span>
          {section && (
            <>
              <span className="text-warm-dim">›</span>
              <span className="text-foreground font-semibold tracking-[0.05em]">{page}</span>
            </>
          )}
        </nav>

        {/* Actions */}
        <div className="flex items-center gap-2">
          {/* Search trigger */}
          <button
            onClick={() => setOpen(true)}
            className="flex items-center gap-2 h-6 px-2 border border-border/60 bg-card hover:border-primary/40 transition-colors cursor-pointer group"
          >
            <Search className="h-2.5 w-2.5 text-warm-dim group-hover:text-primary" />
            <span className="text-[10px] text-warm-muted group-hover:text-foreground transition-colors tracking-[0.05em] uppercase">
              Search
            </span>
            <span className="text-[9px] text-warm-dim font-bold border border-border/60 px-1 py-px tracking-wider">
              ⌃K
            </span>
          </button>

          {/* User dropdown — terminal style */}
          <DropdownMenu>
            <DropdownMenuTrigger className="flex items-center gap-2 h-6 px-2 border border-border/40 bg-card hover:border-primary/40 transition-colors cursor-pointer group outline-none">
              <span className="text-[9px] text-warm-dim font-bold uppercase tracking-[0.15em]">USR</span>
              <span className="text-[10px] font-semibold text-foreground/85 group-hover:text-foreground tracking-[0.05em]">
                AHMAD
              </span>
              <span className="text-[9px] text-warm-dim">▾</span>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="min-w-48 rounded-none border-border bg-card">
              <DropdownMenuGroup>
                <DropdownMenuLabel>
                  <div className="flex flex-col gap-0.5 py-0.5">
                    <span className="text-[12px] font-semibold text-foreground tracking-tight">Ahmad Sahi</span>
                    <span className="text-[10px] text-warm-muted uppercase tracking-widest">PSX Investor · Filer</span>
                  </div>
                </DropdownMenuLabel>
              </DropdownMenuGroup>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                variant="destructive"
                className="gap-2 cursor-pointer text-[11px] uppercase tracking-wider"
                onClick={() => signOut({ redirectTo: '/login' })}
              >
                <LogOut className="h-3 w-3" />
                Sign out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </header>

      <CommandPalette open={open} onOpenChange={setOpen} />
    </>
  );
}

function TickerSegment({ label, value, change }: { label: string; value: string | undefined | null; change?: number | null }) {
  const up = change != null && change >= 0;
  const down = change != null && change < 0;
  return (
    <span className="inline-flex items-baseline gap-1.5 mr-4 num text-[10px]">
      <span className="text-warm-dim font-bold tracking-[0.12em] uppercase">{label}</span>
      <span className="text-foreground font-semibold">{value ?? '—'}</span>
      {change != null && (
        <span className={cn('text-[10px] font-semibold', up && 'text-cyan-pos', down && 'text-warn')}>
          {up ? '▲' : '▼'}{Math.abs(change).toFixed(2)}%
        </span>
      )}
    </span>
  );
}

function Clock() {
  // Simple client clock — re-renders every minute
  const now = new Date();
  const time = now.toLocaleTimeString('en-PK', { hour: '2-digit', minute: '2-digit', hour12: false });
  return <span className="text-[10px] text-foreground/80 font-semibold tabular-nums tracking-wider">{time}</span>;
}

function fmtPct(v: number | null | undefined): string {
  if (v == null) return '—';
  return `${v.toFixed(1)}%`;
}
