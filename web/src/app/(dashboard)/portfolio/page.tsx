'use client';

import { useState } from 'react';
import {
  useHoldings, usePortfolioSummary, useTransactions,
  useAddTransaction, useDeleteHolding, useStocks,
} from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { formatRs, formatPct, formatScore, scoreColor } from '@/lib/utils/format';
import { Plus, Trash2, ChevronDown, Briefcase, TrendingUp, TrendingDown } from 'lucide-react';
import type { AddTransactionRequest } from '@/lib/api/types';

export default function PortfolioPage() {
  const { data: holdings, isLoading } = useHoldings();
  const { data: summary } = usePortfolioSummary();
  const [showTxDialog, setShowTxDialog] = useState(false);
  const [showTxHistory, setShowTxHistory] = useState(false);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold tracking-tight">Portfolio</h1>
          <p className="text-muted-foreground text-xs mt-0.5">Your PSX dividend holdings</p>
        </div>
        <Button
          onClick={() => setShowTxDialog(true)}
          className="h-8 text-xs gap-2 cursor-pointer"
        >
          <Plus className="h-3.5 w-3.5" />
          Add Transaction
        </Button>
      </div>

      {/* Summary stat cards */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card className="border-border/50 card-panel">
            <CardContent className="p-4">
              <div className="text-[10px] text-muted-foreground/60 uppercase tracking-widest font-semibold mb-1.5">Total Invested</div>
              <div className="num text-xl font-bold tracking-tight">{formatRs(summary.totalCostBasis)}</div>
            </CardContent>
          </Card>
          <Card className="border-border/50 card-panel">
            <CardContent className="p-4">
              <div className="text-[10px] text-muted-foreground/60 uppercase tracking-widest font-semibold mb-1.5">Market Value</div>
              <div className="num text-xl font-bold tracking-tight">{formatRs(summary.totalMarketValue)}</div>
              {summary.totalGainLossPct != null && (
                <div className={cn(
                  'num text-[11px] flex items-center gap-1 mt-1 font-medium',
                  summary.totalGainLossPct >= 0 ? 'text-emerald-500' : 'text-red-500',
                )}>
                  {summary.totalGainLossPct >= 0
                    ? <TrendingUp className="h-3 w-3" />
                    : <TrendingDown className="h-3 w-3" />}
                  {summary.totalGainLossPct >= 0 ? '+' : ''}{formatPct(summary.totalGainLossPct)}
                </div>
              )}
            </CardContent>
          </Card>
          <Card className="border-border/50 card-panel">
            <CardContent className="p-4">
              <div className="text-[10px] text-muted-foreground/60 uppercase tracking-widest font-semibold mb-1.5">Annual Income</div>
              <div className="num text-xl font-bold tracking-tight text-emerald-500">{formatRs(summary.projectedAnnualIncome)}</div>
              <div className="num text-[11px] text-muted-foreground mt-1">
                Yield: {formatPct(summary.portfolioYield)}
              </div>
            </CardContent>
          </Card>
          <Card className="border-border/50 card-panel">
            <CardContent className="p-4">
              <div className="text-[10px] text-muted-foreground/60 uppercase tracking-widest font-semibold mb-1.5">Holdings</div>
              <div className="num text-xl font-bold tracking-tight">{summary.holdingCount}</div>
              <div className="text-[11px] text-muted-foreground mt-1">Stocks</div>
            </CardContent>
          </Card>
        </div>
      )}

      <HoldingsTable holdings={holdings} isLoading={isLoading} />

      {/* Transaction history toggle */}
      <div>
        <button
          onClick={() => setShowTxHistory(!showTxHistory)}
          className="flex items-center gap-1.5 text-[12px] text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
        >
          <ChevronDown className={cn('h-3.5 w-3.5 transition-transform', showTxHistory && 'rotate-180')} />
          Transaction History
        </button>
        {showTxHistory && <TransactionHistory />}
      </div>

      {showTxDialog && (
        <AddTransactionDialog onClose={() => setShowTxDialog(false)} />
      )}
    </div>
  );
}

function HoldingsTable({
  holdings,
  isLoading,
}: {
  holdings: ReturnType<typeof useHoldings>['data'];
  isLoading: boolean;
}) {
  const deleteHolding = useDeleteHolding();

  return (
    <Card className="overflow-hidden border-border/50 card-panel">
      <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
        <div className="flex items-center gap-2">
          <Briefcase className="h-3.5 w-3.5 text-primary" />
          <CardTitle className="text-sm font-semibold">Holdings</CardTitle>
          {holdings?.length ? (
            <span className="ml-auto num text-[10px] text-muted-foreground/60">
              {holdings.length} position{holdings.length !== 1 ? 's' : ''}
            </span>
          ) : null}
        </div>
      </CardHeader>
      <CardContent className="p-0">
        {isLoading ? (
          <div className="p-5 space-y-2">
            {[1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full rounded-sm" />)}
          </div>
        ) : !holdings?.length ? (
          <p className="px-6 py-10 text-sm text-muted-foreground text-center">
            No holdings yet. Add a transaction to get started.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border/40" style={{ background: 'oklch(1 0 0 / 2%)' }}>
                  <th className="text-left pl-5 pr-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Stock</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Shares</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Avg Cost</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Current</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Gain/Loss</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">YoC</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Annual Inc.</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Safety</th>
                  <th className="pr-5 px-3 py-2.5 w-8" />
                </tr>
              </thead>
              <tbody>
                {holdings.map((h) => (
                  <tr key={h.id} className="row-hover relative border-b border-border/30 hover:bg-primary/[0.04] transition-colors">
                    <td className="pl-5 pr-3 py-3">
                      <Link
                        href={`/stock/${h.symbol}`}
                        className="num font-semibold text-[13px] hover:text-primary transition-colors"
                      >
                        {h.symbol}
                      </Link>
                      <div className="text-[11px] text-muted-foreground/60 truncate max-w-[110px] leading-tight mt-0.5">
                        {h.name}
                      </div>
                    </td>
                    <td className="text-right px-3 py-3">
                      <span className="num text-[13px]">{h.shares.toLocaleString()}</span>
                    </td>
                    <td className="text-right px-3 py-3">
                      <span className="num text-[13px] whitespace-nowrap">{formatRs(h.avgCost)}</span>
                    </td>
                    <td className="text-right px-3 py-3">
                      <span className="num text-[13px] whitespace-nowrap">{h.currentPrice ? formatRs(h.currentPrice) : '—'}</span>
                    </td>
                    <td className={cn(
                      'text-right px-3 py-3',
                      h.gainLossPct == null ? '' : h.gainLossPct >= 0 ? 'text-emerald-500' : 'text-red-500',
                    )}>
                      <span className="num font-semibold text-[13px]">
                        {h.gainLossPct != null ? `${h.gainLossPct >= 0 ? '+' : ''}${formatPct(h.gainLossPct)}` : '—'}
                      </span>
                    </td>
                    <td className="text-right px-3 py-3">
                      <span className="num text-[13px]">{formatPct(h.yieldOnCost)}</span>
                    </td>
                    <td className="text-right px-3 py-3">
                      <span className="num font-semibold text-[13px] whitespace-nowrap">
                        {h.annualIncome ? formatRs(h.annualIncome) : '—'}
                      </span>
                    </td>
                    <td className={cn('text-right px-3 py-3 font-semibold', scoreColor(h.safetyScore))}>
                      <span className="num text-[13px]">{formatScore(h.safetyScore)}</span>
                    </td>
                    <td className="pr-5 pl-3 py-3 text-right">
                      <button
                        onClick={() => {
                          if (confirm(`Remove ${h.symbol} from holdings?`)) {
                            deleteHolding.mutate(h.id);
                          }
                        }}
                        className="text-muted-foreground/40 hover:text-red-500 transition-colors cursor-pointer"
                        aria-label={`Remove ${h.symbol}`}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function TransactionHistory() {
  const { data: transactions, isLoading } = useTransactions();

  return (
    <Card className="overflow-hidden border-border/50 card-panel mt-3">
      <CardContent className="p-0">
        {isLoading ? (
          <div className="p-5 space-y-2">
            {[1, 2, 3].map((i) => <Skeleton key={i} className="h-8 w-full rounded-sm" />)}
          </div>
        ) : !transactions?.length ? (
          <p className="px-6 py-6 text-sm text-muted-foreground text-center">No transactions</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border/40" style={{ background: 'oklch(1 0 0 / 2%)' }}>
                  <th className="text-left pl-5 pr-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Date</th>
                  <th className="text-left px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Symbol</th>
                  <th className="text-left px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Action</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Shares</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Price</th>
                  <th className="text-right pr-5 pl-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Total</th>
                </tr>
              </thead>
              <tbody>
                {transactions.map((t) => (
                  <tr key={t.id} className="border-b border-border/30 hover:bg-primary/[0.04] transition-colors">
                    <td className="pl-5 pr-3 py-2.5">
                      <span className="num text-[12px] text-muted-foreground">{t.transactionDate}</span>
                    </td>
                    <td className="px-3 py-2.5">
                      <span className="num font-semibold text-[13px]">{t.symbol}</span>
                    </td>
                    <td className="px-3 py-2.5">
                      <span className={cn(
                        'text-[10px] font-medium px-1.5 py-0.5 rounded-sm border uppercase tracking-wide',
                        t.action === 'BUY'
                          ? 'text-emerald-400 bg-emerald-500/10 border-emerald-500/25'
                          : 'text-red-400 bg-red-500/10 border-red-500/25',
                      )}>
                        {t.action}
                      </span>
                    </td>
                    <td className="text-right px-3 py-2.5">
                      <span className="num text-[13px]">{t.shares.toLocaleString()}</span>
                    </td>
                    <td className="text-right px-3 py-2.5">
                      <span className="num text-[13px] whitespace-nowrap">{formatRs(t.price)}</span>
                    </td>
                    <td className="text-right pr-5 pl-3 py-2.5">
                      <span className="num font-semibold text-[13px] whitespace-nowrap">{formatRs(t.totalCost)}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function AddTransactionDialog({ onClose }: { onClose: () => void }) {
  const { data: stocks } = useStocks();
  const addTx = useAddTransaction();
  const [form, setForm] = useState<Partial<AddTransactionRequest>>({
    action: 'BUY',
    platform: 'Finqalab',
    transactionDate: new Date().toISOString().split('T')[0],
  });
  const [error, setError] = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    if (!form.symbol || !form.shares || !form.price) {
      setError('Symbol, shares, and price are required');
      return;
    }
    try {
      await addTx.mutateAsync({
        symbol: form.symbol,
        action: form.action as 'BUY' | 'SELL',
        shares: Number(form.shares),
        price: Number(form.price),
        commission: form.commission ? Number(form.commission) : undefined,
        platform: form.platform,
        transactionDate: form.transactionDate,
      });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add transaction');
    }
  }

  const inputClass = 'w-full num px-3 py-2 rounded-sm border border-border/50 bg-muted/10 text-[12px] focus:outline-none focus:border-primary/50 transition-colors';
  const labelClass = 'block text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60 mb-1';

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4 backdrop-blur-[2px]">
      <div className="bg-card rounded-sm border border-border/60 card-panel w-full max-w-md p-6 space-y-4 shadow-xl">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-semibold">Add Transaction</h2>
          <button
            onClick={onClose}
            className="text-muted-foreground/50 hover:text-foreground transition-colors text-lg leading-none cursor-pointer"
          >
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className={labelClass}>Symbol</label>
              <select
                className={inputClass}
                value={form.symbol ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, symbol: e.target.value }))}
                required
              >
                <option value="">Select stock</option>
                {stocks?.map((s) => (
                  <option key={s.symbol} value={s.symbol}>{s.symbol}</option>
                ))}
              </select>
            </div>
            <div>
              <label className={labelClass}>Action</label>
              <select
                className={inputClass}
                value={form.action}
                onChange={(e) => setForm((f) => ({ ...f, action: e.target.value as 'BUY' | 'SELL' }))}
              >
                <option value="BUY">BUY</option>
                <option value="SELL">SELL</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className={labelClass}>Shares</label>
              <input
                type="number" min="1" step="1"
                className={inputClass}
                value={form.shares ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, shares: e.target.valueAsNumber }))}
                required
              />
            </div>
            <div>
              <label className={labelClass}>Price (Rs.)</label>
              <input
                type="number" min="0.01" step="0.01"
                className={inputClass}
                value={form.price ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, price: e.target.valueAsNumber }))}
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className={labelClass}>Commission (Rs.)</label>
              <input
                type="number" min="0" step="0.01"
                className={inputClass}
                value={form.commission ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, commission: e.target.valueAsNumber || undefined }))}
                placeholder="Optional"
              />
            </div>
            <div>
              <label className={labelClass}>Date</label>
              <input
                type="date"
                className={inputClass}
                value={form.transactionDate ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, transactionDate: e.target.value }))}
              />
            </div>
          </div>

          <div>
            <label className={labelClass}>Platform</label>
            <input
              type="text"
              className={inputClass}
              value={form.platform ?? ''}
              onChange={(e) => setForm((f) => ({ ...f, platform: e.target.value }))}
              placeholder="Finqalab"
            />
          </div>

          {form.shares && form.price && (
            <div className="rounded-sm border border-border/30 bg-muted/[0.05] px-3 py-2 num text-[12px]">
              <span className="text-muted-foreground">Total: </span>
              <span className="font-semibold">{formatRs(Number(form.shares) * Number(form.price) + (Number(form.commission) || 0))}</span>
            </div>
          )}

          {error && <p className="text-[11px] text-red-500">{error}</p>}

          <div className="flex gap-2 pt-1">
            <Button type="button" variant="outline" onClick={onClose} className="flex-1 h-8 text-xs cursor-pointer border-border/50">
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={addTx.isPending}
              className="flex-1 h-8 text-xs cursor-pointer"
            >
              {addTx.isPending ? 'Adding…' : `Add ${form.action ?? 'BUY'}`}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
