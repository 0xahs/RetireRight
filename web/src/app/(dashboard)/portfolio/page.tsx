'use client';

import { useState } from 'react';
import {
  useHoldings, usePortfolioSummary, useTransactions,
  useAddTransaction, useDeleteHolding, useStocks,
} from '@/lib/api/queries';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { formatRs, formatPct, formatScore, scoreTone, yieldTone, changeTone, changeMarker } from '@/lib/utils/format';
import { StockLogo } from '@/components/stock-logo';
import type { AddTransactionRequest } from '@/lib/api/types';

export default function PortfolioPage() {
  const { data: holdings, isLoading } = useHoldings();
  const { data: summary } = usePortfolioSummary();
  const [showTxDialog, setShowTxDialog] = useState(false);
  const [showTxHistory, setShowTxHistory] = useState(false);

  return (
    <div className="space-y-4">
      <div className="flex items-baseline justify-between gap-3 flex-wrap">
        <div>
          <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
            ┌── PORTFOLIO
          </div>
          <h1 className="text-foreground font-bold text-[20px] tracking-tight">
            HOLDINGS · DIVIDEND POSITIONS
          </h1>
        </div>
        <button
          onClick={() => setShowTxDialog(true)}
          className="inline-flex items-center gap-1.5 text-[10px] uppercase tracking-[0.18em] font-bold px-3 py-1.5 border border-primary/60 bg-primary/10 text-amber hover:bg-primary/20 transition-colors cursor-pointer"
        >
          [ + ADD TRANSACTION ]
        </button>
      </div>

      {/* Summary panels */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <SummaryPanel label="INVESTED" value={formatRs(summary.totalCostBasis)} />
          <SummaryPanel
            label="MARKET VALUE"
            value={formatRs(summary.totalMarketValue)}
            subValue={summary.totalGainLossPct != null ? `${changeMarker(summary.totalGainLossPct)} ${Math.abs(summary.totalGainLossPct).toFixed(2)}%` : undefined}
            subTone={summary.totalGainLossPct != null ? changeTone(summary.totalGainLossPct) : undefined}
          />
          <SummaryPanel
            label="ANNUAL INCOME"
            value={formatRs(summary.projectedAnnualIncome)}
            valueTone="text-cyan-pos"
            subValue={summary.portfolioYield != null ? `YIELD ${summary.portfolioYield.toFixed(2)}%` : undefined}
            subTone={yieldTone(summary.portfolioYield)}
          />
          <SummaryPanel
            label="POSITIONS"
            value={`${summary.holdingCount}`}
            subValue="STOCKS"
          />
        </div>
      )}

      <HoldingsTable holdings={holdings} isLoading={isLoading} />

      <div>
        <button
          onClick={() => setShowTxHistory(!showTxHistory)}
          className="inline-flex items-center gap-1.5 text-[10px] uppercase tracking-[0.18em] font-bold text-warm-muted hover:text-amber transition-colors cursor-pointer"
        >
          <span className="text-amber">{showTxHistory ? '▼' : '▶'}</span> TRANSACTION HISTORY
        </button>
        {showTxHistory && <TransactionHistory />}
      </div>

      {showTxDialog && <AddTransactionDialog onClose={() => setShowTxDialog(false)} />}
    </div>
  );
}

function SummaryPanel({
  label, value, valueTone, subValue, subTone,
}: { label: string; value: string; valueTone?: string; subValue?: string; subTone?: string }) {
  return (
    <div className="panel">
      <div className="panel-strip px-3 py-1.5">
        <span className="text-amber font-bold text-[9px] tracking-[0.2em] uppercase">▌ {label}</span>
      </div>
      <div className="px-4 py-3">
        <div className={cn('num text-[20px] font-bold tracking-tight leading-none', valueTone ?? 'text-foreground')}>
          {value}
        </div>
        {subValue && (
          <div className={cn('num text-[10px] font-bold tracking-[0.15em] uppercase mt-1.5', subTone ?? 'text-warm-muted')}>
            {subValue}
          </div>
        )}
      </div>
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
    <div className="panel">
      <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ HOLDINGS</span>
        {holdings?.length ? (
          <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold num">
            {holdings.length} POSITION{holdings.length !== 1 ? 'S' : ''}
          </span>
        ) : null}
      </div>
      {isLoading ? (
        <div className="p-4 space-y-1.5">
          {[1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full" style={{ borderRadius: 0 }} />)}
        </div>
      ) : !holdings?.length ? (
        <p className="px-6 py-10 text-[11px] uppercase tracking-[0.18em] text-warm-dim text-center font-bold">
          NO HOLDINGS · ADD A TRANSACTION TO BEGIN
        </p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-[11px] num min-w-[1000px]">
            <thead>
              <tr className="border-b border-border bg-white/[0.015]">
                <th className="text-left pl-4 pr-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">STOCK</th>
                <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">SHARES</th>
                <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">AVG COST</th>
                <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">CURRENT</th>
                <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">G/L</th>
                <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">YOC</th>
                <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">ANNUAL</th>
                <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">SAFE</th>
                <th className="pr-4 px-3 py-2 w-8" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {holdings.map((h) => (
                <tr key={h.id} className="row-hover hover:bg-amber-500/[0.04] transition-colors">
                  <td className="pl-4 pr-3 py-2">
                    <Link href={`/stock/${h.symbol}`} className="flex items-center gap-2 group">
                      <StockLogo symbol={h.symbol} size={22} />
                      <div>
                        <div className="font-bold text-[12px] text-foreground tracking-[0.05em] group-hover:text-amber transition-colors">
                          {h.symbol}
                        </div>
                        <div className="text-[9px] uppercase tracking-[0.12em] text-warm-dim font-medium truncate max-w-[110px]">
                          {h.name}
                        </div>
                      </div>
                    </Link>
                  </td>
                  <td className="text-right px-3 py-2 text-foreground font-bold">{h.shares.toLocaleString()}</td>
                  <td className="text-right px-3 py-2 text-warm-muted whitespace-nowrap">{formatRs(h.avgCost)}</td>
                  <td className="text-right px-3 py-2 text-foreground font-bold whitespace-nowrap">{h.currentPrice ? formatRs(h.currentPrice) : '—'}</td>
                  <td className={cn('text-right px-3 py-2 font-bold', changeTone(h.gainLossPct))}>
                    {h.gainLossPct != null ? `${changeMarker(h.gainLossPct)} ${Math.abs(h.gainLossPct).toFixed(2)}%` : '—'}
                  </td>
                  <td className={cn('text-right px-3 py-2 font-bold', yieldTone(h.yieldOnCost))}>{formatPct(h.yieldOnCost)}</td>
                  <td className="text-right px-3 py-2 text-cyan-pos font-bold whitespace-nowrap">
                    {h.annualIncome ? formatRs(h.annualIncome) : '—'}
                  </td>
                  <td className={cn('text-right px-3 py-2 font-bold', scoreTone(h.safetyScore))}>{formatScore(h.safetyScore)}</td>
                  <td className="pr-4 pl-3 py-2 text-right">
                    <button
                      onClick={() => {
                        if (confirm(`Remove ${h.symbol} from holdings?`)) deleteHolding.mutate(h.id);
                      }}
                      className="text-warm-dim hover:text-warn transition-colors cursor-pointer text-[10px] font-bold"
                      aria-label={`Remove ${h.symbol}`}
                    >
                      ✕
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function TransactionHistory() {
  const { data: transactions, isLoading } = useTransactions();

  return (
    <div className="panel mt-3">
      <div className="panel-strip px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ TRANSACTIONS</span>
      </div>
      {isLoading ? (
        <div className="p-3 space-y-1.5">
          {[1, 2, 3].map((i) => <Skeleton key={i} className="h-8 w-full" style={{ borderRadius: 0 }} />)}
        </div>
      ) : !transactions?.length ? (
        <p className="px-6 py-8 text-[11px] uppercase tracking-[0.18em] text-warm-dim text-center font-bold">
          NO TRANSACTIONS LOGGED
        </p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-[11px] num">
            <thead>
              <tr className="border-b border-border bg-white/[0.015]">
                <th className="text-left pl-4 pr-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">DATE</th>
                <th className="text-left px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">SYMBOL</th>
                <th className="text-left px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">ACTION</th>
                <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">SHARES</th>
                <th className="text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">PRICE</th>
                <th className="text-right pr-4 pl-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">TOTAL</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {transactions.map((t) => (
                <tr key={t.id} className="hover:bg-amber-500/[0.04] transition-colors">
                  <td className="pl-4 pr-3 py-2 text-warm-muted uppercase tracking-[0.05em]">
                    {new Date(t.transactionDate).toLocaleDateString('en-PK', { year: '2-digit', month: 'short', day: '2-digit' }).toUpperCase()}
                  </td>
                  <td className="px-3 py-2 font-bold text-foreground tracking-[0.05em]">{t.symbol}</td>
                  <td className="px-3 py-2">
                    <span className={cn(
                      'text-[9px] font-bold px-1.5 py-0.5 border uppercase tracking-[0.15em]',
                      t.action === 'BUY'
                        ? 'text-cyan-pos border-[oklch(0.79_0.13_180_/_40%)] bg-[oklch(0.79_0.13_180_/_10%)]'
                        : 'text-warn border-[oklch(0.68_0.22_25_/_40%)] bg-[oklch(0.68_0.22_25_/_10%)]',
                    )}>
                      {t.action}
                    </span>
                  </td>
                  <td className="text-right px-3 py-2 text-foreground font-bold">{t.shares.toLocaleString()}</td>
                  <td className="text-right px-3 py-2 text-foreground whitespace-nowrap">{formatRs(t.price)}</td>
                  <td className="text-right pr-4 pl-3 py-2 text-foreground font-bold whitespace-nowrap">{formatRs(t.totalCost)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
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
      setError('SYMBOL, SHARES, AND PRICE ARE REQUIRED');
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
      setError(err instanceof Error ? err.message.toUpperCase() : 'FAILED TO ADD TRANSACTION');
    }
  }

  const inputClass = 'w-full num px-2.5 py-1.5 border border-border bg-card text-[12px] text-foreground focus:outline-none focus:border-primary/60 transition-colors';
  const labelClass = 'block text-[9px] font-bold uppercase tracking-[0.18em] text-warm-muted mb-1';

  return (
    <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
      <div className="panel-strong w-full max-w-md bg-card relative">
        <span aria-hidden className="absolute top-0 left-0 right-0 h-[2px] bg-primary" />
        <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ NEW TRANSACTION</span>
          <button
            onClick={onClose}
            className="text-warm-dim hover:text-warn transition-colors text-[12px] leading-none cursor-pointer font-bold"
          >
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-4 space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className={labelClass}>SYMBOL</label>
              <select
                className={inputClass}
                value={form.symbol ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, symbol: e.target.value }))}
                required
              >
                <option value="">SELECT</option>
                {stocks?.map((s) => (
                  <option key={s.symbol} value={s.symbol}>{s.symbol}</option>
                ))}
              </select>
            </div>
            <div>
              <label className={labelClass}>ACTION</label>
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
              <label className={labelClass}>SHARES</label>
              <input
                type="number" min="1" step="1"
                className={inputClass}
                value={form.shares ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, shares: e.target.valueAsNumber }))}
                required
              />
            </div>
            <div>
              <label className={labelClass}>PRICE (RS.)</label>
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
              <label className={labelClass}>COMMISSION</label>
              <input
                type="number" min="0" step="0.01"
                className={inputClass}
                value={form.commission ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, commission: e.target.valueAsNumber || undefined }))}
                placeholder="OPTIONAL"
              />
            </div>
            <div>
              <label className={labelClass}>DATE</label>
              <input
                type="date"
                className={inputClass}
                value={form.transactionDate ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, transactionDate: e.target.value }))}
              />
            </div>
          </div>

          <div>
            <label className={labelClass}>PLATFORM</label>
            <input
              type="text"
              className={inputClass}
              value={form.platform ?? ''}
              onChange={(e) => setForm((f) => ({ ...f, platform: e.target.value }))}
              placeholder="FINQALAB"
            />
          </div>

          {form.shares && form.price && (
            <div className="border border-border bg-white/[0.02] px-3 py-2 num text-[11px] flex items-baseline justify-between">
              <span className="text-warm-dim uppercase tracking-[0.18em] font-bold text-[9px]">TOTAL</span>
              <span className="text-amber text-[14px] font-bold">
                {formatRs(Number(form.shares) * Number(form.price) + (Number(form.commission) || 0))}
              </span>
            </div>
          )}

          {error && (
            <p className="text-[10px] text-warn font-bold uppercase tracking-[0.15em]">✕ {error}</p>
          )}

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 text-[10px] uppercase tracking-[0.18em] font-bold py-2 border border-border bg-card text-warm-muted hover:text-foreground hover:border-border/80 cursor-pointer"
            >
              CANCEL
            </button>
            <button
              type="submit"
              disabled={addTx.isPending}
              className="flex-1 text-[10px] uppercase tracking-[0.18em] font-bold py-2 border border-primary/60 bg-primary/10 text-amber hover:bg-primary/20 cursor-pointer disabled:opacity-50"
            >
              {addTx.isPending ? '◐ ADDING…' : `[ ADD ${form.action ?? 'BUY'} ]`}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
