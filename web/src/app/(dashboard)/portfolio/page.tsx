'use client';

import { useState } from 'react';
import { useHoldings, usePortfolioSummary, useTransactions, useAddTransaction, useDeleteHolding } from '@/lib/api/queries';
import { useStocks } from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { formatRs, formatPct, formatScore, scoreColor } from '@/lib/utils/format';
import { Plus, Trash2, ChevronDown } from 'lucide-react';
import type { AddTransactionRequest } from '@/lib/api/types';

export default function PortfolioPage() {
  const { data: holdings, isLoading } = useHoldings();
  const { data: summary } = usePortfolioSummary();
  const [showTxDialog, setShowTxDialog] = useState(false);
  const [showTxHistory, setShowTxHistory] = useState(false);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Portfolio</h1>
          <p className="text-muted-foreground text-sm mt-1">Your PSX dividend holdings</p>
        </div>
        <button
          onClick={() => setShowTxDialog(true)}
          className="flex items-center gap-1.5 px-3 py-2 rounded-md bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Add Transaction
        </button>
      </div>

      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card>
            <CardContent className="p-4">
              <div className="text-xs text-muted-foreground">Total Invested</div>
              <div className="text-lg font-semibold">{formatRs(summary.totalCostBasis)}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="text-xs text-muted-foreground">Current Value</div>
              <div className="text-lg font-semibold">{formatRs(summary.totalMarketValue)}</div>
              {summary.totalGainLossPct != null && (
                <div className={cn('text-xs', summary.totalGainLossPct >= 0 ? 'text-emerald-500' : 'text-red-500')}>
                  {summary.totalGainLossPct >= 0 ? '+' : ''}{formatPct(summary.totalGainLossPct)}
                </div>
              )}
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="text-xs text-muted-foreground">Annual Income</div>
              <div className="text-lg font-semibold">{formatRs(summary.projectedAnnualIncome)}</div>
              <div className="text-xs text-muted-foreground">Yield: {formatPct(summary.portfolioYield)}</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="text-xs text-muted-foreground">Holdings</div>
              <div className="text-lg font-semibold">{summary.holdingCount} stocks</div>
            </CardContent>
          </Card>
        </div>
      )}

      <HoldingsTable holdings={holdings} isLoading={isLoading} />

      <div>
        <button
          onClick={() => setShowTxHistory(!showTxHistory)}
          className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ChevronDown className={cn('h-4 w-4 transition-transform', showTxHistory && 'rotate-180')} />
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

function HoldingsTable({ holdings, isLoading }: { holdings: ReturnType<typeof useHoldings>['data']; isLoading: boolean }) {
  const deleteHolding = useDeleteHolding();

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Holdings</CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        {isLoading ? (
          <div className="px-6 pb-4 space-y-2">{[1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full" />)}</div>
        ) : !holdings?.length ? (
          <p className="px-6 pb-4 text-sm text-muted-foreground">No holdings yet. Add a transaction to get started.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border">
                <th className="text-left px-6 py-2 text-xs text-muted-foreground font-medium">Stock</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Shares</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Avg Cost</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Current</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Gain/Loss</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Yield on Cost</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Annual Income</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Safety</th>
                <th className="px-6 py-2" />
              </tr>
            </thead>
            <tbody>
              {holdings.map((h) => (
                <tr key={h.id} className="border-b border-border/50 hover:bg-accent/30 transition-colors">
                  <td className="px-6 py-2.5">
                    <Link href={`/stock/${h.symbol}`} className="font-medium hover:text-primary transition-colors">
                      {h.symbol}
                    </Link>
                    <div className="text-xs text-muted-foreground">{h.name}</div>
                  </td>
                  <td className="text-right px-3 py-2.5">{h.shares.toLocaleString()}</td>
                  <td className="text-right px-3 py-2.5">{formatRs(h.avgCost)}</td>
                  <td className="text-right px-3 py-2.5">{h.currentPrice ? formatRs(h.currentPrice) : '-'}</td>
                  <td className={cn('text-right px-3 py-2.5', h.gainLossPct == null ? '' : h.gainLossPct >= 0 ? 'text-emerald-500' : 'text-red-500')}>
                    {h.gainLossPct != null ? `${h.gainLossPct >= 0 ? '+' : ''}${formatPct(h.gainLossPct)}` : '-'}
                  </td>
                  <td className="text-right px-3 py-2.5">{formatPct(h.yieldOnCost)}</td>
                  <td className="text-right px-3 py-2.5 font-medium">{h.annualIncome ? formatRs(h.annualIncome) : '-'}</td>
                  <td className={cn('text-right px-3 py-2.5 font-semibold', scoreColor(h.safetyScore))}>
                    {formatScore(h.safetyScore)}
                  </td>
                  <td className="px-6 py-2.5 text-right">
                    <button
                      onClick={() => {
                        if (confirm(`Remove ${h.symbol} from holdings?`)) {
                          deleteHolding.mutate(h.id);
                        }
                      }}
                      className="text-muted-foreground hover:text-red-500 transition-colors"
                      aria-label={`Remove ${h.symbol}`}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </CardContent>
    </Card>
  );
}

function TransactionHistory() {
  const { data: transactions, isLoading } = useTransactions();

  return (
    <Card className="mt-3">
      <CardContent className="p-0">
        {isLoading ? (
          <div className="px-6 py-4 space-y-2">{[1, 2, 3].map((i) => <Skeleton key={i} className="h-8 w-full" />)}</div>
        ) : !transactions?.length ? (
          <p className="px-6 py-4 text-sm text-muted-foreground">No transactions</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border">
                <th className="text-left px-6 py-2 text-xs text-muted-foreground font-medium">Date</th>
                <th className="text-left px-3 py-2 text-xs text-muted-foreground font-medium">Symbol</th>
                <th className="text-left px-3 py-2 text-xs text-muted-foreground font-medium">Action</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Shares</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Price</th>
                <th className="text-right px-6 py-2 text-xs text-muted-foreground font-medium">Total</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((t) => (
                <tr key={t.id} className="border-b border-border/50">
                  <td className="px-6 py-2 text-muted-foreground text-xs">{t.transactionDate}</td>
                  <td className="px-3 py-2 font-medium">{t.symbol}</td>
                  <td className="px-3 py-2">
                    <Badge variant={t.action === 'BUY' ? 'default' : 'destructive'} className="text-xs">
                      {t.action}
                    </Badge>
                  </td>
                  <td className="text-right px-3 py-2">{t.shares.toLocaleString()}</td>
                  <td className="text-right px-3 py-2">{formatRs(t.price)}</td>
                  <td className="text-right px-6 py-2 font-medium">{formatRs(t.totalCost)}</td>
                </tr>
              ))}
            </tbody>
          </table>
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

  const inputClass = 'w-full px-3 py-2 rounded-md border border-border bg-background text-sm focus:outline-none focus:ring-1 focus:ring-ring';
  const labelClass = 'block text-xs font-medium text-muted-foreground mb-1';

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-background rounded-lg border border-border w-full max-w-md p-6 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-base font-semibold">Add Transaction</h2>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground text-lg leading-none">&times;</button>
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
                  <option key={s.symbol} value={s.symbol}>{s.symbol} — {s.name}</option>
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
                type="number"
                min="1"
                step="1"
                className={inputClass}
                value={form.shares ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, shares: e.target.valueAsNumber }))}
                required
              />
            </div>
            <div>
              <label className={labelClass}>Price per share (Rs.)</label>
              <input
                type="number"
                min="0.01"
                step="0.01"
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
                type="number"
                min="0"
                step="0.01"
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
            <div className="rounded-md bg-accent/30 px-3 py-2 text-sm">
              <span className="text-muted-foreground">Total: </span>
              <span className="font-semibold">{formatRs(Number(form.shares) * Number(form.price) + (Number(form.commission) || 0))}</span>
            </div>
          )}

          {error && <p className="text-sm text-red-500">{error}</p>}

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 py-2 rounded-md border border-border text-sm hover:bg-accent transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={addTx.isPending}
              className="flex-1 py-2 rounded-md bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 disabled:opacity-50 transition-colors"
            >
              {addTx.isPending ? 'Adding...' : `Add ${form.action}`}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
