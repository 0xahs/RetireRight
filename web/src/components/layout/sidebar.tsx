'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';
import {
  LayoutDashboard,
  List,
  Filter,
  CalendarDays,
  Briefcase,
  Activity,
  TrendingUp,
  FileJson,
} from 'lucide-react';

const NAV_ITEMS = [
  { href: '/dashboard', label: 'Command Center', icon: LayoutDashboard },
  { href: '/watchlist',  label: 'Watchlist',      icon: List },
  { href: '/screener',   label: 'Screener',        icon: Filter },
  { href: '/dividends',  label: 'Dividends',       icon: CalendarDays },
  { href: '/portfolio',  label: 'Portfolio',        icon: Briefcase },
  { href: '/pipeline',   label: 'Pipeline',         icon: Activity },
  { href: '/export',     label: 'Export Center',    icon: FileJson },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="w-56 shrink-0 flex flex-col border-r border-border bg-sidebar">
      {/* Brand */}
      <div className="h-12 flex items-center gap-3 px-4 border-b border-border/60 shrink-0">
        <div className="h-7 w-7 rounded-md bg-primary flex items-center justify-center shrink-0 shadow-sm">
          <TrendingUp className="h-3.5 w-3.5 text-primary-foreground" />
        </div>
        <div className="min-w-0">
          <div className="font-semibold text-sm tracking-tight leading-tight text-sidebar-foreground">RetireRight</div>
          <div className="text-[10px] text-muted-foreground leading-tight">PSX Dividend Intel</div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 py-3 px-2 space-y-0.5 overflow-y-auto">
        <div className="px-3 pb-2">
          <span className="text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/50">Analytics</span>
        </div>
        {NAV_ITEMS.slice(0, 3).map(({ href, label, icon: Icon }) => {
          const active = pathname === href || (href !== '/dashboard' && pathname.startsWith(href));
          return (
            <NavLink key={href} href={href} label={label} icon={Icon} active={active} />
          );
        })}

        <div className="px-3 py-2 mt-2">
          <span className="text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/50">Management</span>
        </div>
        {NAV_ITEMS.slice(3).map(({ href, label, icon: Icon }) => {
          const active = pathname === href || (href !== '/dashboard' && pathname.startsWith(href));
          return (
            <NavLink key={href} href={href} label={label} icon={Icon} active={active} />
          );
        })}
      </nav>

      {/* Footer */}
      <div className="border-t border-border/60 px-4 py-3 shrink-0">
        <div className="flex items-center gap-2.5">
          <div className="h-6 w-6 rounded-full bg-primary/20 flex items-center justify-center shrink-0">
            <span className="text-[10px] font-bold text-primary">A</span>
          </div>
          <div className="min-w-0 flex-1">
            <div className="text-xs font-medium text-sidebar-foreground truncate">Ahmad Sahi</div>
            <div className="text-[10px] text-muted-foreground truncate">PSX Investor</div>
          </div>
        </div>
        <div className="mt-2.5 text-[9px] text-muted-foreground/40 text-center font-medium tracking-widest uppercase">
          KSE-100 · PSX
        </div>
      </div>
    </aside>
  );
}

function NavLink({
  href, label, icon: Icon, active,
}: {
  href: string; label: string; icon: React.ElementType; active: boolean;
}) {
  return (
    <Link
      href={href}
      className={cn(
        'group flex items-center gap-2.5 px-3 py-2 rounded-md text-sm transition-all duration-150 cursor-pointer',
        active
          ? 'bg-primary/12 text-primary font-medium'
          : 'text-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-foreground',
      )}
    >
      <Icon className={cn(
        'h-3.5 w-3.5 shrink-0 transition-colors',
        active ? 'text-primary' : 'text-muted-foreground/60 group-hover:text-sidebar-foreground',
      )} />
      <span className="text-[13px]">{label}</span>
      {active && (
        <div className="ml-auto h-1.5 w-1.5 rounded-full bg-primary" />
      )}
    </Link>
  );
}
