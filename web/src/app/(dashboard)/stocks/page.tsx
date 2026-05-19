'use client';

import { useState, useMemo } from 'react';
import Link from 'next/link';
import { useAllStocks, useSetWatchlist } from '@/lib/api/queries';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';

const SECTOR_COLORS: Record<string, string> = {
  Banking: 'text-cyan-pos',
  IslamicBanking: 'text-cyan-pos',
  Energy: 'text-amber',
  Fertilizer: 'text-[oklch(0.82_0.15_130)]',
  Cement: 'text-warm-muted',
  Technology: 'text-[oklch(0.75_0.18_280)]',
  Power: 'text-amber',
  Oil: 'text-amber',
  Chemicals: 'text-[oklch(0.82_0.15_130)]',
};

function sectorColor(sector: string) {
  return SECTOR_COLORS[sector] ?? 'text-warm-muted';
}

export default function StocksPage() {
  const { data: stocks, isLoading } = useAllStocks();
  const setWatchlist = useSetWatchlist();

  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState<'all' | 'watchlist' | 'other'>('all');

  const sectors = useMemo(() => {
    if (!stocks) return [];
    return Array.from(new Set(stocks.map(s => s.sector))).sort();
  }, [stocks]);

  const [sectorFilter, setSectorFilter] = useState('');

  const filtered = useMemo(() => {
    if (!stocks) return [];
    const q = search.toLowerCase();
    return stocks.filter(s => {
      if (q && !s.symbol.toLowerCase().includes(q) && !s.name.toLowerCase().includes(q)) return false;
      if (filter === 'watchlist' && !s.isWatchlist) return false;
      if (filter === 'other' && s.isWatchlist) return false;
      if (sectorFilter && s.sector !== sectorFilter) return false;
      return true;
    });
  }, [stocks, search, filter, sectorFilter]);

  const watchlistCount = stocks?.filter(s => s.isWatchlist).length ?? 0;
  const totalCount = stocks?.length ?? 0;

  function handleToggle(symbol: string, current: boolean) {
    setWatchlist.mutate({ symbol, isWatchlist: !current });
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-[13px] font-bold uppercase tracking-[0.18em] text-foreground">
            Stock Universe
          </h1>
          <p className="text-[11px] text-warm-muted mt-0.5">
            Configure which stocks appear in your watchlist and drive the pipeline
          </p>
        </div>
        <div className="text-right">
          <div className="text-[11px] text-warm-muted uppercase tracking-[0.12em]">Watchlist</div>
          <div className="text-[22px] font-bold num text-primary leading-tight">
            {isLoading ? '—' : watchlistCount}
            <span className="text-[13px] text-warm-dim font-normal"> / {isLoading ? '—' : totalCount}</span>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div className="flex gap-2 flex-wrap">
        <input
          type="text"
          placeholder="Search symbol or name…"
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="h-7 px-3 rounded border border-border bg-card text-[11px] text-foreground placeholder:text-warm-dim focus:outline-none focus:border-primary/60 w-48"
        />

        <div className="flex border border-border rounded overflow-hidden text-[10px] uppercase tracking-[0.1em] font-bold">
          {(['all', 'watchlist', 'other'] as const).map(f => (
            <button
              key={f}
              onClick={() => setFilter(f)}
              className={cn(
                'px-3 h-7 transition-colors',
                filter === f
                  ? 'bg-primary/[0.12] text-primary'
                  : 'text-warm-muted hover:text-foreground hover:bg-white/[0.03]',
              )}
            >
              {f}
            </button>
          ))}
        </div>

        <select
          value={sectorFilter}
          onChange={e => setSectorFilter(e.target.value)}
          className="h-7 px-2 rounded border border-border bg-card text-[11px] text-foreground focus:outline-none focus:border-primary/60"
        >
          <option value="">All sectors</option>
          {sectors.map(s => <option key={s} value={s}>{s}</option>)}
        </select>

        {(search || filter !== 'all' || sectorFilter) && (
          <button
            onClick={() => { setSearch(''); setFilter('all'); setSectorFilter(''); }}
            className="h-7 px-3 text-[10px] uppercase tracking-[0.1em] font-bold text-warm-muted hover:text-foreground border border-border rounded hover:bg-white/[0.03] transition-colors"
          >
            Clear
          </button>
        )}

        <span className="ml-auto text-[10px] text-warm-dim self-center num">
          {isLoading ? '…' : `${filtered.length} shown`}
        </span>
      </div>

      {/* Table */}
      <div className="border border-border rounded-sm overflow-auto max-h-[calc(100vh-280px)]">
        <table className="w-full text-[11px]">
          <thead>
            <tr className="border-b border-border">
              <th className="sticky top-0 z-10 bg-card text-left px-3 py-2 text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold w-20">Symbol</th>
              <th className="sticky top-0 z-10 bg-card text-left px-3 py-2 text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold">Company</th>
              <th className="sticky top-0 z-10 bg-card text-left px-3 py-2 text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold w-36">Sector</th>
              <th className="sticky top-0 z-10 bg-card text-center px-3 py-2 text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold w-28">Watchlist</th>
            </tr>
          </thead>
          <tbody>
            {isLoading
              ? Array.from({ length: 12 }).map((_, i) => (
                  <tr key={i} className="border-b border-border/40">
                    <td className="px-3 py-2"><Skeleton className="h-3 w-12" /></td>
                    <td className="px-3 py-2"><Skeleton className="h-3 w-40" /></td>
                    <td className="px-3 py-2"><Skeleton className="h-3 w-20" /></td>
                    <td className="px-3 py-2 text-center"><Skeleton className="h-4 w-8 mx-auto" /></td>
                  </tr>
                ))
              : filtered.map(stock => (
                  <tr
                    key={stock.symbol}
                    className={cn(
                      'border-b border-border/40 transition-colors',
                      stock.isWatchlist
                        ? 'bg-primary/[0.025] hover:bg-primary/[0.05]'
                        : 'hover:bg-white/[0.015]',
                    )}
                  >
                    <td className="px-3 py-2 font-bold num tracking-wider">
                      <Link href={`/stock/${stock.symbol}`} className="text-amber hover:text-foreground transition-colors">
                        {stock.symbol}
                      </Link>
                    </td>
                    <td className="px-3 py-2 text-foreground/80">
                      <Link href={`/stock/${stock.symbol}`} className="hover:text-foreground transition-colors">
                        {stock.name}
                      </Link>
                    </td>
                    <td className={cn('px-3 py-2 font-medium', sectorColor(stock.sector))}>
                      {stock.sector}
                    </td>
                    <td className="px-3 py-2 text-center">
                      <button
                        onClick={() => handleToggle(stock.symbol, stock.isWatchlist)}
                        disabled={setWatchlist.isPending}
                        className={cn(
                          'relative inline-flex h-4 w-8 rounded-full transition-colors duration-200 focus:outline-none',
                          stock.isWatchlist
                            ? 'bg-primary'
                            : 'bg-border hover:bg-warm-muted/40',
                          setWatchlist.isPending && 'opacity-50 cursor-not-allowed',
                        )}
                        title={stock.isWatchlist ? 'Remove from watchlist' : 'Add to watchlist'}
                      >
                        <span
                          className={cn(
                            'inline-block h-3 w-3 rounded-full bg-white shadow-sm transition-transform duration-200 my-0.5',
                            stock.isWatchlist ? 'translate-x-4' : 'translate-x-0.5',
                          )}
                        />
                      </button>
                    </td>
                  </tr>
                ))}
            {!isLoading && filtered.length === 0 && (
              <tr>
                <td colSpan={4} className="px-3 py-8 text-center text-[11px] text-warm-dim">
                  No stocks match the current filters
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
