'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useSession } from 'next-auth/react';
import { cn } from '@/lib/utils';
import { BrandMark, BrandWordmark } from '@/components/brand/brand-mark';
import { NAV_ITEMS, NAV_GROUP_LABELS, type NavGroup } from '@/lib/nav';

export function Sidebar() {
  const pathname = usePathname();
  const { data: session } = useSession();
  const isAdmin = session?.user?.isAdmin === true;
  const visibleItems = NAV_ITEMS.filter(i => !i.requiresAdmin || isAdmin);
  const grouped = visibleItems.reduce<Record<string, typeof NAV_ITEMS>>((acc, item) => {
    (acc[item.group] ??= []).push(item);
    return acc;
  }, {});

  return (
    <aside className="w-52 shrink-0 flex flex-col border-r border-border bg-sidebar relative">
      {/* Top accent line — terminal signature */}
      <div className="absolute top-0 left-0 right-0 h-[2px] bg-primary" />

      {/* Brand hero */}
      <div className="px-4 pt-4 pb-3 border-b border-border shrink-0">
        <Link href="/dashboard" className="flex items-center gap-3 group">
          <BrandMark size={28} />
          <BrandWordmark size="md" />
        </Link>
        <div className="flex items-center justify-between mt-3 pt-2.5 border-t border-border/60">
          <div className="flex items-center gap-1.5">
            <span
              className="block w-1.5 h-1.5 animate-pulse"
              style={{ background: 'var(--verdict-acc)' }}
            />
            <span className="text-[9px] text-cyan-pos uppercase tracking-[0.22em] font-bold">LIVE</span>
          </div>
          <span className="text-[9px] text-warm-dim uppercase tracking-[0.22em] font-bold num">
            v0.3
          </span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 py-3 overflow-y-auto">
        {Object.entries(grouped).map(([group, items], gIdx) => (
          <div key={group} className={cn(gIdx > 0 && 'mt-3')}>
            <div className="px-3 pb-1.5 flex items-center gap-1.5">
              <span className="h-px w-3 bg-primary/40" />
              <span className="text-[9px] font-bold uppercase tracking-[0.2em] text-amber">
                {NAV_GROUP_LABELS[group as NavGroup]}
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
      <div className="border-t border-border px-3 py-2 shrink-0 flex items-center justify-between text-[9px] tracking-[0.22em] uppercase font-bold">
        <span className="text-warm-dim">KSE-100 · PSX</span>
        <span className="text-amber">2026</span>
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
