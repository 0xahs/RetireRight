'use client';

import { usePathname } from 'next/navigation';
import { signOut } from 'next-auth/react';
import { LogOut, ChevronRight, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { CommandPalette, useCommandPalette } from '@/components/command-palette';

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

          <Button
            variant="ghost"
            size="sm"
            className="h-7 px-2.5 text-xs text-muted-foreground hover:text-foreground gap-1.5 cursor-pointer"
            onClick={() => signOut({ redirectTo: '/login' })}
          >
            <LogOut className="h-3.5 w-3.5" />
            Sign out
          </Button>
        </div>
      </header>

      <CommandPalette open={open} onOpenChange={setOpen} />
    </>
  );
}
