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
} from 'lucide-react';

const nav = [
  { href: '/dashboard', label: 'Command Center', icon: LayoutDashboard },
  { href: '/watchlist', label: 'Watchlist', icon: List },
  { href: '/screener', label: 'Screener', icon: Filter },
  { href: '/dividends', label: 'Dividends', icon: CalendarDays },
  { href: '/portfolio', label: 'Portfolio', icon: Briefcase },
  { href: '/pipeline', label: 'Pipeline', icon: Activity },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="w-56 shrink-0 border-r border-border bg-card flex flex-col">
      <div className="p-4 border-b border-border">
        <span className="font-semibold text-sm tracking-tight text-foreground">RetireRight</span>
        <p className="text-xs text-muted-foreground mt-0.5">PSX Dividend Intel</p>
      </div>
      <nav className="flex-1 py-2">
        {nav.map(({ href, label, icon: Icon }) => (
          <Link
            key={href}
            href={href}
            className={cn(
              'flex items-center gap-2.5 px-3 py-2 mx-2 rounded-md text-sm transition-colors',
              pathname === href
                ? 'bg-accent text-accent-foreground font-medium'
                : 'text-muted-foreground hover:text-foreground hover:bg-accent/50',
            )}
          >
            <Icon className="h-4 w-4 shrink-0" />
            {label}
          </Link>
        ))}
      </nav>
    </aside>
  );
}
