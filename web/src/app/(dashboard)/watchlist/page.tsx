'use client';

import { useState, useMemo } from 'react';
import { useStocks } from '@/lib/api/queries';
import { formatRs, formatPct } from '@/lib/utils/format';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { Search } from 'lucide-react';
import { StockLogo } from '@/components/stock-logo';
import { VerdictBadge } from '@/components/signal/verdict-badge';
import { SafetyTier } from '@/components/signal/safety-tier';
import { ConvictionDots } from '@/components/signal/conviction-dots';
import { CompareButton } from '@/components/compare/compare-sticky-bar';
import type { StockSummaryDto } from '@/lib/api/types';

type SortKey = 'symbol' | 'price' | 'netYield' | 'dividendCoverage' | 'compositeScore' | 'opportunityScore' | 'verdict';

function sortValue(s: StockSummaryDto, key: SortKey): number | string | null | undefined {
  switch (key) {
    case 'symbol':           return s.symbol;
    case 'price':            return s.price;
    case 'netYield':         return s.netYield;
    case 'dividendCoverage': return s.dividendCoverage;
    case 'compositeScore':   return s.compositeScore;
    case 'opportunityScore': return s.opportunityScore;
    case 'verdict':          return { ACCUMULATE: 0, REVIEW: 1, AVOID: 2, WATCH: 3, HOLD: 4 }[s.verdict];
  }
}

export default function WatchlistPage() {
  const { data: stocks, isLoading, error } = useStocks();
  const [sort, setSort] = useState<{ key: SortKey; dir: 'asc' | 'desc' }>({ key: 'verdict', dir: 'asc' });
  const [search, setSearch] = useState('');

  function toggleSort(key: SortKey) {
    setSort(prev =>
      prev.key === key
        ? { key, dir: prev.dir === 'desc' ? 'asc' : 'desc' }
        : { key, dir: key === 'symbol' ? 'asc' : 'desc' }
    );
  }

  const displayed = useMemo(() => {
    if (!stocks) return [];
    const q = search.toLowerCase();
    const arr = q
      ? stocks.filter(s =>
          s.symbol.toLowerCase().includes(q) ||
          (s.name?.toLowerCase().includes(q)) ||
          (s.sector?.toLowerCase().includes(q))
        )
      : [...stocks];
    arr.sort((a, b) => {
      const av = sortValue(a, sort.key);
      const bv = sortValue(b, sort.key);
      if (av == null && bv == null) return 0;
      if (av == null) return 1;
      if (bv == null) return -1;
      const cmp = av < bv ? -1 : av > bv ? 1 : 0;
      return sort.dir === 'desc' ? -cmp : cmp;
    });
    return arr;
  }, [stocks, sort, search]);

  return (
    <div className="space-y-4">
      {/* Page header */}
      <div className="flex items-baseline justify-between gap-3 flex-wrap">
        <div>
          <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
            ┌── WATCHLIST
          </div>
          <h1 className="text-foreground font-bold text-[20px] tracking-tight">
            TRACKED · <span className="num">{stocks?.length ?? 0}</span> INSTRUMENTS
            {search && displayed.length !== stocks?.length && (
              <span className="text-warm-muted text-[12px] ml-2">
                · <span className="num">{displayed.length}</span> SHOWN
              </span>
            )}
          </h1>
        </div>
        <div className="relative w-60">
          <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3 w-3 text-warm-dim pointer-events-none" />
          <input
            placeholder="SYMBOL · NAME · SECTOR"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-7 h-7 text-[10px] tracking-[0.05em] uppercase font-bold bg-card border border-border focus:border-primary/60 focus:outline-none text-foreground placeholder:text-warm-dim"
          />
        </div>
      </div>

      <div className="panel">
        <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ WATCHLIST · ALL</span>
          <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">CLICK · OPEN INSTRUMENT</span>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-1.5">
            {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-10 w-full" style={{ borderRadius: 0 }} />)}
          </div>
        ) : error ? (
          <div className="px-6 py-10 text-[11px] uppercase tracking-[0.18em] text-warn text-center font-bold">
            ✕ FAILED TO LOAD STOCKS · IS API ALIVE?
          </div>
        ) : !displayed.length ? (
          <div className="px-6 py-10 text-[11px] uppercase tracking-[0.18em] text-warm-dim text-center font-bold">
            {search ? `NO MATCH "${search.toUpperCase()}"` : 'NO STOCKS · ADD VIA SETTINGS'}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-[11px] min-w-[1000px] num">
              <thead>
                <tr className="border-b border-border bg-white/[0.015]">
                  <SortableCol label="SYMBOL"   sortKey="symbol"           current={sort} onSort={toggleSort} className="text-left pl-4 pr-3" style={{ minWidth: 160 }} />
                  <SortableCol label="VERDICT"  sortKey="verdict"          current={sort} onSort={toggleSort} className="text-left px-3" style={{ minWidth: 130 }} />
                  <th style={{ minWidth: 60 }} className="px-3 py-2 text-left text-[9px] text-warm-dim font-bold uppercase tracking-[0.18em]">SAFE</th>
                  <SortableCol label="PRICE"    sortKey="price"            current={sort} onSort={toggleSort} className="text-right px-3" style={{ minWidth: 90 }} />
                  <SortableCol label="NET·Y"    sortKey="netYield"         current={sort} onSort={toggleSort} className="text-right px-3" style={{ minWidth: 70 }} />
                  <SortableCol label="COV"      sortKey="dividendCoverage" current={sort} onSort={toggleSort} className="text-right px-3" style={{ minWidth: 70 }} />
                  <SortableCol label="COMP"     sortKey="compositeScore"   current={sort} onSort={toggleSort} className="text-right px-3" style={{ minWidth: 70 }} />
                  <SortableCol label="OPP"      sortKey="opportunityScore" current={sort} onSort={toggleSort} className="text-right px-3" style={{ minWidth: 70 }} />
                  <th style={{ minWidth: 200 }} className="px-3 py-2 text-left text-[9px] text-warm-dim font-bold uppercase tracking-[0.18em]">WHY · RISK</th>
                  <th style={{ minWidth: 60 }} className="px-3 py-2 text-right text-[9px] text-warm-dim font-bold uppercase tracking-[0.18em]">+</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {displayed.map((s) => <WatchRow key={s.symbol} stock={s} />)}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

function WatchRow({ stock: s }: { stock: StockSummaryDto }) {
  const verdictBar =
    s.verdict === 'ACCUMULATE' ? 'verdict-bar-acc' :
    s.verdict === 'REVIEW' ? 'verdict-bar-rev' :
    s.verdict === 'AVOID' ? 'verdict-bar-avd' :
    s.verdict === 'WATCH' ? 'verdict-bar-wch' :
    'verdict-bar-hold';

  const up = (s.changePct ?? 0) >= 0;

  return (
    <tr className={cn('hover:bg-white/[0.02] transition-colors cursor-pointer', verdictBar)}>
      <td className="pl-4 pr-3 py-2">
        <Link href={`/stock/${s.symbol}`} className="flex items-center gap-2.5 group">
          <StockLogo symbol={s.symbol} size={24} />
          <div>
            <div className="font-bold text-[12px] text-foreground tracking-[0.05em] group-hover:text-amber transition-colors">
              {s.symbol}
            </div>
            <div className="text-[9px] text-warm-dim uppercase tracking-[0.12em] truncate max-w-[120px] font-medium">
              {s.sector}
            </div>
          </div>
        </Link>
      </td>
      <td className="px-3 py-2">
        <div className="flex items-center gap-1.5">
          <VerdictBadge verdict={s.verdict} size="sm" />
          <ConvictionDots level={s.convictionLevel} confidence={s.confidence} />
        </div>
      </td>
      <td className="px-3 py-2">
        <SafetyTier tier={s.safetyTier} size="sm" />
      </td>
      <td className="text-right px-3 py-2 whitespace-nowrap">
        <div className="font-bold text-[12px] text-foreground">{formatRs(s.price)}</div>
        {s.changePct != null && (
          <div className={cn('text-[10px] font-bold mt-0.5', up ? 'text-cyan-pos' : 'text-warn')}>
            {up ? '▲' : '▼'}{Math.abs(s.changePct).toFixed(2)}%
          </div>
        )}
      </td>
      <td className="text-right px-3 py-2 font-bold text-foreground">{formatPct(s.netYield)}</td>
      <td className="text-right px-3 py-2 text-warm-muted">{s.dividendCoverage != null ? `${s.dividendCoverage.toFixed(2)}X` : '—'}</td>
      <td className="text-right px-3 py-2 font-bold text-foreground">{s.compositeScore != null ? Math.round(s.compositeScore) : '—'}</td>
      <td className="text-right px-3 py-2 text-warm-muted">{s.opportunityScore != null ? Math.round(s.opportunityScore) : '—'}</td>
      <td className="px-3 py-2 text-[10px] text-warm-muted truncate max-w-[260px]">
        {s.whyLine || s.risks || '—'}
      </td>
      <td className="px-3 py-2 text-right">
        <CompareButton symbol={s.symbol} variant="icon" />
      </td>
    </tr>
  );
}

function SortableCol({
  label, sortKey, current, onSort, className, style,
}: {
  label: string;
  sortKey: SortKey;
  current: { key: SortKey; dir: 'asc' | 'desc' };
  onSort: (key: SortKey) => void;
  className?: string;
  style?: React.CSSProperties;
}) {
  const active = current.key === sortKey;
  return (
    <th
      className={cn(
        'py-2 text-[9px] font-bold uppercase tracking-[0.18em] select-none cursor-pointer transition-colors',
        active ? 'text-amber' : 'text-warm-dim hover:text-warm-muted',
        className,
      )}
      style={style}
      onClick={() => onSort(sortKey)}
    >
      <span className="inline-flex items-center gap-1 whitespace-nowrap">
        {label}
        {active ? (current.dir === 'desc' ? '▼' : '▲') : <span className="opacity-25">↕</span>}
      </span>
    </th>
  );
}
