'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';

const NAV_ITEMS = [
  { code: '01', href: '/dashboard',  label: 'Cockpit',     group: 'signal' },
  { code: '02', href: '/watchlist',  label: 'Watchlist',   group: 'signal' },
  { code: '03', href: '/screener',   label: 'Screener',    group: 'signal' },
  { code: '04', href: '/compare',    label: 'Compare',     group: 'signal' },
  { code: '05', href: '/dividends',  label: 'Calendar',    group: 'data'   },
  { code: '06', href: '/portfolio',  label: 'Portfolio',   group: 'data'   },
  { code: '07', href: '/pipeline',   label: 'Pipeline',    group: 'admin'  },
  { code: '08', href: '/export',     label: 'Export',      group: 'admin'  },
];

const GROUP_LABELS: Record<string, string> = {
  signal: 'SIGNAL',
  data:   'DATA',
  admin:  'INFRA',
};

export function Sidebar() {
  const pathname = usePathname();
  const grouped = NAV_ITEMS.reduce<Record<string, typeof NAV_ITEMS>>((acc, item) => {
    (acc[item.group] ??= []).push(item);
    return acc;
  }, {});

  return (
    <aside className="w-52 shrink-0 flex flex-col border-r border-border bg-sidebar relative">
      {/* Top accent line — terminal signature */}
      <div className="absolute top-0 left-0 right-0 h-px bg-primary opacity-60" />

      {/* Brand */}
      <div className="h-11 flex items-center px-3 border-b border-border shrink-0">
        <div className="flex items-baseline gap-2">
          <span className="text-primary font-bold text-[13px] tracking-tight">RR</span>
          <span className="text-foreground/55 text-[10px] uppercase tracking-[0.18em] font-medium">
            Terminal
          </span>
        </div>
        <div className="ml-auto flex items-center gap-1">
          <span className="block w-1.5 h-1.5 rounded-full bg-verdict-acc" style={{ background: 'var(--verdict-acc)' }} />
          <span className="text-[9px] text-warm-muted">LIVE</span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 py-3 overflow-y-auto">
        {Object.entries(grouped).map(([group, items], gIdx) => (
          <div key={group} className={cn(gIdx > 0 && 'mt-3')}>
            <div className="px-3 pb-1.5 flex items-center gap-1.5">
              <span className="h-px w-3 bg-primary/40" />
              <span className="text-[9px] font-bold uppercase tracking-[0.2em] text-amber">
                {GROUP_LABELS[group]}
              </span>
              <span className="h-px flex-1 bg-border" />
            </div>
            {items.map(({ code, href, label }) => {
              const active = pathname === href || (href !== '/dashboard' && pathname.startsWith(href));
              return <NavLink key={href} code={code} href={href} label={label} active={active} />;
            })}
          </div>
        ))}
      </nav>

      {/* Footer status */}
      <div className="border-t border-border px-3 py-2 shrink-0 flex items-center justify-between text-[9px]">
        <span className="text-warm-dim font-medium">KSE-100 · PSX</span>
        <span className="text-primary tracking-widest">v0.3</span>
      </div>
    </aside>
  );
}

function NavLink({ code, href, label, active }: { code: string; href: string; label: string; active: boolean }) {
  return (
    <Link
      href={href}
      className={cn(
        'group flex items-center px-3 py-[5px] text-[12px] transition-colors cursor-pointer',
        active
          ? 'bg-primary/[0.08] text-primary'
          : 'text-foreground/70 hover:text-foreground hover:bg-white/[0.025]',
      )}
    >
      <span
        className={cn(
          'font-bold w-7 shrink-0 text-[10px] tabular-nums tracking-[0.05em]',
          active ? 'text-primary' : 'text-warm-dim',
        )}
      >
        {code}
      </span>
      <span className={cn('uppercase tracking-[0.05em] font-medium', active && 'font-semibold')}>
        {label}
      </span>
      {active && (
        <span className="ml-auto text-primary text-[10px]">▶</span>
      )}
    </Link>
  );
}
