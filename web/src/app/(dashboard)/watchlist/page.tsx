'use client';

import { useState, useMemo } from 'react';
import { useStocks } from '@/lib/api/queries';
import { formatRs, formatPct, freshnessBadge } from '@/lib/utils/format';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import {
  AlertCircle, TrendingUp, TrendingDown,
  ChevronUp, ChevronDown, ChevronsUpDown, Search,
} from 'lucide-react';
import { ScoreBar, SignalBadge, FlagBadge, SectorBadge } from '@/components/stock-ui';
import type { StockSummaryDto } from '@/lib/api/types';

type SortKey = 'symbol' | 'price' | 'dividendYield' | 'dividendCoverage' | 'compositeScore' | 'opportunityScore';

function sortValue(s: StockSummaryDto, key: SortKey): number | string | null | undefined {
  switch (key) {
    case 'symbol':           return s.symbol;
    case 'price':            return s.price;
    case 'dividendYield':    return s.dividendYield;
    case 'dividendCoverage': return s.dividendCoverage;
    case 'compositeScore':   return s.compositeScore;
    case 'opportunityScore': return s.opportunityScore;
  }
}

export default function WatchlistPage() {
  const { data: stocks, isLoading, error } = useStocks();
  const [sort, setSort] = useState<{ key: SortKey; dir: 'asc' | 'desc' }>({ key: 'compositeScore', dir: 'desc' });
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
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold tracking-tight">Watchlist</h1>
          <p className="text-muted-foreground text-xs mt-0.5">
            <span className="num">{stocks?.length ?? 0}</span> stocks tracked
            {search && displayed.length !== stocks?.length
              ? <> · <span className="num">{displayed.length}</span> shown</>
              : null}
          </p>
        </div>
        <div className="relative w-64">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground/50 pointer-events-none" />
          <Input
            placeholder="Symbol, name, sector…"
            value={search}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSearch(e.target.value)}
            className="pl-8 h-8 text-xs bg-card border-border/50 rounded-sm focus:border-primary/50 focus:ring-0"
          />
        </div>
      </div>

      <Card className="overflow-hidden border-border/50 card-panel bg-card">
        <CardHeader className="hidden" />
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-6 space-y-3">
              {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-11 w-full rounded-sm" />)}
            </div>
          ) : error ? (
            <div className="px-6 py-10 text-sm text-destructive text-center">
              Failed to load stocks. Is the API running?
            </div>
          ) : !displayed.length ? (
            <div className="px-6 py-10 text-sm text-muted-foreground text-center">
              {search ? `No stocks match "${search}"` : 'No stocks in watchlist'}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-border/40" style={{ background: 'oklch(1 0 0 / 2%)' }}>
                    <SortableCol label="Symbol"      sortKey="symbol"          current={sort} onSort={toggleSort} className="text-left pl-5 pr-3" />
                    <th className="px-3 py-2.5 text-left text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Sector</th>
                    <SortableCol label="Price"        sortKey="price"           current={sort} onSort={toggleSort} className="text-right px-3" />
                    <SortableCol label="Yield"        sortKey="dividendYield"   current={sort} onSort={toggleSort} className="text-right px-3" />
                    <SortableCol label="Coverage"     sortKey="dividendCoverage" current={sort} onSort={toggleSort} className="text-right px-3" />
                    <SortableCol label="Score"        sortKey="compositeScore"  current={sort} onSort={toggleSort} className="px-3 w-36" />
                    <SortableCol label="Opportunity"  sortKey="opportunityScore" current={sort} onSort={toggleSort} className="px-3 w-36" />
                    <th className="px-3 py-2.5 text-left text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Signal</th>
                    <th className="pl-3 pr-6 py-2.5 text-left text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Flags</th>
                  </tr>
                </thead>
                <tbody>
                  {displayed.map((s) => {
                    const freshness = freshnessBadge(s.scrapedAt);
                    return (
                      <tr
                        key={s.symbol}
                        className="row-hover relative border-b border-border/30 hover:bg-primary/[0.04] transition-colors cursor-pointer"
                      >
                        {/* Symbol */}
                        <td className="pl-5 pr-3 py-3">
                          <div className="flex items-center gap-2">
                            {freshness === 'stale' && (
                              <AlertCircle className="h-2.5 w-2.5 text-amber-500/70 shrink-0" />
                            )}
                            <div>
                              <Link
                                href={`/stock/${s.symbol}`}
                                className="num font-semibold text-[13px] hover:text-primary transition-colors"
                              >
                                {s.symbol}
                              </Link>
                              <div className="text-[11px] text-muted-foreground/60 truncate max-w-[120px] leading-tight mt-0.5">
                                {s.name}
                              </div>
                            </div>
                          </div>
                        </td>

                        {/* Sector */}
                        <td className="px-3 py-3">
                          <SectorBadge sector={s.sector ?? ''} />
                        </td>

                        {/* Price */}
                        <td className="text-right px-3 py-3 whitespace-nowrap">
                          <div className="num font-semibold text-[13px]">{formatRs(s.price)}</div>
                          {s.changePct != null && (
                            <div className={cn(
                              'num text-[11px] flex items-center justify-end gap-0.5 leading-tight',
                              s.changePct >= 0 ? 'text-emerald-500' : 'text-red-500'
                            )}>
                              {s.changePct >= 0
                                ? <TrendingUp className="h-2.5 w-2.5" />
                                : <TrendingDown className="h-2.5 w-2.5" />}
                              {s.changePct >= 0 ? '+' : ''}{formatPct(s.changePct)}
                            </div>
                          )}
                        </td>

                        {/* Yield */}
                        <td className="text-right px-3 py-3">
                          <span className="num font-semibold text-[13px]">{formatPct(s.dividendYield)}</span>
                        </td>

                        {/* Coverage */}
                        <td className="text-right px-3 py-3">
                          <span className="num text-[13px] text-muted-foreground">
                            {s.dividendCoverage != null ? `${s.dividendCoverage.toFixed(2)}x` : '—'}
                          </span>
                        </td>

                        {/* Score */}
                        <td className="px-3 py-3 w-36">
                          <ScoreBar score={s.compositeScore} />
                        </td>

                        {/* Opportunity */}
                        <td className="px-3 py-3 w-36">
                          <ScoreBar score={s.opportunityScore} />
                        </td>

                        {/* Signal */}
                        <td className="px-3 py-3">
                          <SignalBadge signal={s.signal} />
                        </td>

                        {/* Flags */}
                        <td className="pl-3 pr-6 py-3">
                          <div className="flex flex-wrap gap-1">
                            {s.opportunityFlags.slice(0, 2).map((f) => (
                              <FlagBadge key={f} flag={f} />
                            ))}
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function SortableCol({
  label, sortKey, current, onSort, className,
}: {
  label: string;
  sortKey: SortKey;
  current: { key: SortKey; dir: 'asc' | 'desc' };
  onSort: (key: SortKey) => void;
  className?: string;
}) {
  const active = current.key === sortKey;
  return (
    <th
      className={cn(
        'py-2.5 text-[10px] font-semibold uppercase tracking-widest select-none cursor-pointer transition-colors',
        active ? 'text-primary' : 'text-muted-foreground/60 hover:text-muted-foreground',
        className,
      )}
      onClick={() => onSort(sortKey)}
    >
      <span className="flex items-center gap-1 whitespace-nowrap">
        {label}
        {active
          ? current.dir === 'desc'
            ? <ChevronDown className="h-3 w-3" />
            : <ChevronUp className="h-3 w-3" />
          : <ChevronsUpDown className="h-3 w-3 opacity-25" />}
      </span>
    </th>
  );
}
