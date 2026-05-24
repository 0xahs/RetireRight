// Single source of truth for primary navigation. Consumed by the sidebar,
// the command palette (⌘K), and the topbar breadcrumb/tab-title. Keeping one
// list prevents the three from drifting (pages missing from ⌘K or the title bar).

export type NavGroup = 'signal' | 'data' | 'admin';

export interface NavItem {
  code: string;
  href: string;
  label: string;
  group: NavGroup;
}

export const NAV_ITEMS: NavItem[] = [
  { code: '01', href: '/dashboard', label: 'Brief',     group: 'signal' },
  { code: '02', href: '/watchlist', label: 'Watchlist', group: 'signal' },
  { code: '03', href: '/screener',  label: 'Screener',  group: 'signal' },
  { code: '04', href: '/compare',   label: 'Compare',   group: 'signal' },
  { code: '05', href: '/sector',    label: 'Sectors',   group: 'signal' },
  { code: '06', href: '/alerts',    label: 'Alerts',    group: 'signal' },
  { code: '07', href: '/dividends', label: 'Calendar',  group: 'data'   },
  { code: '08', href: '/portfolio', label: 'Portfolio', group: 'data'   },
  { code: '09', href: '/stocks',    label: 'Stocks',    group: 'admin'  },
  { code: '10', href: '/pipeline',  label: 'Pipeline',  group: 'admin'  },
  { code: '11', href: '/export',    label: 'Export',    group: 'admin'  },
];

export const NAV_GROUP_LABELS: Record<NavGroup, string> = {
  signal: 'SIGNAL',
  data: 'DATA',
  admin: 'INFRA',
};

/** href → uppercase label, for the topbar breadcrumb and document title. */
export const ROUTE_LABELS: Record<string, string> = Object.fromEntries(
  NAV_ITEMS.map((i) => [i.href, i.label.toUpperCase()]),
);
