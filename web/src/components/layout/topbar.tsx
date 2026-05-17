'use client';

import { usePathname } from 'next/navigation';
import { signOut } from 'next-auth/react';
import { LogOut, ChevronRight, Search, ChevronDown } from 'lucide-react';
import { CommandPalette, useCommandPalette } from '@/components/command-palette';

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
  '/dashboard': 'Command Center',
  '/watchlist': 'Watchlist',
  '/screener': 'Screener',
  '/dividends': 'Dividend Calendar',
  '/portfolio': 'Portfolio',
  '/pipeline': 'Pipeline Monitor',
  '/export': 'Export Center',
};

function usePageTitle(pathname: string): { section: string; page: string } {
  if (pathname.startsWith('/stock/')) {
    const symbol = pathname.split('/')[2]?.toUpperCase() ?? '';
    return { section: 'Watchlist', page: symbol };
  }
  const label = ROUTE_LABELS[pathname] ?? 'RetireRight';
  return { section: '', page: label };
}

export function Topbar() {
  const pathname = usePathname();
  const { section, page } = usePageTitle(pathname);
  const { open, setOpen } = useCommandPalette();

  return (
    <>
      <header className="h-12 shrink-0 flex items-center justify-between px-6 border-b border-border bg-card/50 backdrop-blur-sm">
        {/* Breadcrumb */}
        <nav className="flex items-center gap-1.5 text-sm">
          <span className="text-muted-foreground/60 font-medium text-xs uppercase tracking-wider">RetireRight</span>
          {section && (
            <>
              <ChevronRight className="h-3 w-3 text-muted-foreground/40" />
              <span className="text-muted-foreground text-xs">{section}</span>
            </>
          )}
          <ChevronRight className="h-3 w-3 text-muted-foreground/40" />
          <span className="text-foreground font-medium text-xs">{page}</span>
        </nav>

        {/* Actions */}
        <div className="flex items-center gap-2">
          {/* Search trigger */}
          <button
            onClick={() => setOpen(true)}
            className="flex items-center gap-2 h-7 px-3 rounded-sm border border-border/40 bg-muted/[0.05] hover:bg-muted/[0.12] hover:border-border/60 transition-all cursor-pointer group"
          >
            <Search className="h-3 w-3 text-muted-foreground/50" />
            <span className="text-[11px] text-muted-foreground/50 group-hover:text-muted-foreground/70 transition-colors">
              Search…
            </span>
            <kbd className="ml-1 text-[9px] text-muted-foreground/35 font-mono border border-border/30 rounded px-1 py-px">
              Ctrl K
            </kbd>
          </button>

          <DropdownMenu>
            <DropdownMenuTrigger className="flex items-center gap-2 h-7 px-2 rounded-sm hover:bg-muted/[0.12] transition-colors cursor-pointer group bg-transparent border-0 outline-none">
              <div className="h-6 w-6 rounded-full bg-primary/20 flex items-center justify-center shrink-0">
                <span className="text-[10px] font-bold text-primary">A</span>
              </div>
              <span className="text-[12px] font-medium text-muted-foreground group-hover:text-foreground transition-colors">
                Ahmad Sahi
              </span>
              <ChevronDown className="h-3 w-3 text-muted-foreground/50" />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="min-w-48">
              <DropdownMenuGroup>
                <DropdownMenuLabel>
                  <div className="flex flex-col gap-0.5 py-0.5">
                    <span className="text-[13px] font-semibold text-foreground">Ahmad Sahi</span>
                    <span className="text-[11px] text-muted-foreground">PSX Investor</span>
                  </div>
                </DropdownMenuLabel>
              </DropdownMenuGroup>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                variant="destructive"
                className="gap-2 cursor-pointer"
                onClick={() => signOut({ redirectTo: '/login' })}
              >
                <LogOut className="h-3.5 w-3.5" />
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
