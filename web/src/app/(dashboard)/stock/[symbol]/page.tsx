'use client';

import { useParams } from 'next/navigation';
import { useStock } from '@/lib/api/queries';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';
import { formatRs, formatPct, formatScore, changeTone, changeMarker, scoreTone, yieldTone, coverageTone } from '@/lib/utils/format';
import { FlagBadge } from '@/components/stock-ui';
import { StockLogo } from '@/components/stock-logo';
import { PriceChart } from '@/components/price-chart';
import { VerdictStrip } from '@/components/signal/verdict-strip';
import { AiMemoTab } from '@/components/signal/ai-memo-tab';
import { CompareButton } from '@/components/compare/compare-sticky-bar';

export default function StockPage() {
  const { symbol } = useParams<{ symbol: string }>();
  const { data, isLoading, error } = useStock(symbol);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-14 w-full" style={{ borderRadius: 0 }} />
        <Skeleton className="h-40 w-full" style={{ borderRadius: 0 }} />
        <Skeleton className="h-64 w-full" style={{ borderRadius: 0 }} />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="panel px-5 py-6">
        <p className="text-warn text-[11px] uppercase tracking-[0.18em] font-bold">
          ✕ FAILED TO LOAD {symbol?.toUpperCase()} · IS API ALIVE?
        </p>
      </div>
    );
  }

  const { summary, metrics, recentDividends } = data;
  const hasBanking = metrics.banking != null;

  return (
    <div className="space-y-4">
      {/* TERMINAL TITLE BAR */}
      <div className="panel">
        <div className="panel-strip flex items-center justify-between px-3 py-1.5">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
            ▌ INSTRUMENT
          </span>
          <span className="text-[9px] tracking-[0.18em] uppercase text-warm-dim font-bold num">
            {summary.sector.toUpperCase()} · ID {summary.id}
          </span>
        </div>
        <div className="flex items-center justify-between px-4 py-3 gap-3">
          <div className="flex items-center gap-3">
            <StockLogo symbol={summary.symbol} size={42} />
            <div>
              <h1 className="num font-bold text-[28px] tracking-tight leading-none text-foreground">
                {summary.symbol}
              </h1>
              <p className="text-warm-muted text-[11px] tracking-[0.05em] uppercase mt-1.5 font-medium">
                {summary.name}
              </p>
            </div>
          </div>
          <div className="text-right">
            <div className="num text-[26px] font-bold tracking-tight leading-none text-foreground">
              {formatRs(summary.price)}
            </div>
            {summary.changePct != null && (
              <div className={cn(
                'num text-[12px] mt-1.5 font-bold tracking-tight',
                changeTone(summary.changePct),
              )}>
                {changeMarker(summary.changePct)} {Math.abs(summary.changePct).toFixed(2)}% TODAY
              </div>
            )}
            <div className="mt-2.5">
              <CompareButton symbol={summary.symbol} />
            </div>
          </div>
        </div>
      </div>

      {/* Verdict strip */}
      <VerdictStrip summary={summary} />

      {/* Flags strip */}
      {summary.opportunityFlags.length > 0 && (
        <div className="flex items-center gap-1.5 flex-wrap panel px-3 py-2">
          <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold mr-1.5">
            FLAGS ›
          </span>
          {summary.opportunityFlags.map((f) => (
            <FlagBadge key={f} flag={f} />
          ))}
        </div>
      )}

      {/* Price chart */}
      {data.priceHistory.length > 0 && (
        <div className="panel px-4 py-3">
          <PriceChart data={data.priceHistory} symbol={summary.symbol} />
        </div>
      )}

      {/* Tabs */}
      <Tabs defaultValue="overview">
        <TabsList className="bg-transparent border border-border p-0 h-auto rounded-none">
          <TerminalTabTrigger value="overview" label="OVERVIEW" />
          <TerminalTabTrigger value="dividends" label="DIVIDENDS" />
          <TerminalTabTrigger value="fundamentals" label="FUNDAMENTALS" />
          {hasBanking && <TerminalTabTrigger value="banking" label="BANKING" />}
          <TerminalTabTrigger value="memo" label="AI MEMO" />
        </TabsList>

        <TabsContent value="overview" className="mt-3">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <MetricGroup title="DIVIDEND SAFETY" metrics={[
              { label: 'EARNINGS PAYOUT',       value: metrics.dividendSafety.earningsPayoutRatio != null ? `${(metrics.dividendSafety.earningsPayoutRatio * 100).toFixed(1)}%` : '—', tone: payoutTone(metrics.dividendSafety.earningsPayoutRatio) },
              { label: 'DIVIDEND COVERAGE',     value: fmtX(metrics.dividendSafety.dividendCoverage), tone: coverageTone(metrics.dividendSafety.dividendCoverage) },
              { label: 'DEBT / EQUITY',         value: fmtX(metrics.dividendSafety.debtToEquity), tone: debtToEquityTone(metrics.dividendSafety.debtToEquity) },
              { label: 'INTEREST COVERAGE',     value: fmtX(metrics.dividendSafety.interestCoverage, 1), tone: coverageTone(metrics.dividendSafety.interestCoverage) },
              { label: 'CONSECUTIVE DIV YEARS', value: `${metrics.dividendSafety.consecutiveDivYears ?? '—'}`, tone: yearsTone(metrics.dividendSafety.consecutiveDivYears) },
            ]} />
            <MetricGroup title="VALUATION" metrics={[
              { label: 'P/E RATIO',         value: fmtX(metrics.valuation.peRatio, 1), tone: peTone(metrics.valuation.peRatio) },
              { label: 'P/B RATIO',         value: fmtX(metrics.valuation.pbRatio), tone: pbTone(metrics.valuation.pbRatio) },
              { label: 'DIVIDEND YIELD',    value: formatPct(metrics.valuation.dividendYield), tone: yieldTone(metrics.valuation.dividendYield) },
              { label: 'EARNINGS YIELD',    value: formatPct(metrics.valuation.earningsYield), tone: yieldTone(metrics.valuation.earningsYield) },
              { label: 'MARGIN OF SAFETY',  value: formatPct(metrics.valuation.marginOfSafety), tone: mosTone(metrics.valuation.marginOfSafety) },
            ]} />
            <MetricGroup title="RISK" metrics={[
              { label: 'BETA',              value: `${metrics.risk.beta?.toFixed(2) ?? '—'}` },
              { label: 'RSI (14)',          value: `${metrics.risk.rsi14?.toFixed(1) ?? '—'}`, tone: rsiTone(metrics.risk.rsi14) },
              { label: 'VOLATILITY 30D',    value: formatPct(metrics.risk.volatility30d) },
              { label: '52W RANGE POS.',    value: formatPct(metrics.risk.rangePosition52w) },
              { label: 'DRAWDOWN 52W',      value: formatPct(metrics.risk.drawdownFrom52wHigh), tone: drawdownTone(metrics.risk.drawdownFrom52wHigh) },
            ]} />
          </div>
        </TabsContent>

        <TabsContent value="dividends" className="mt-3">
          <div className="panel">
            <div className="panel-strip px-4 py-1.5">
              <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
                ▌ RECENT DIVIDENDS · {recentDividends.length}
              </span>
            </div>
            {!recentDividends.length ? (
              <p className="px-6 py-8 text-[11px] uppercase tracking-[0.18em] text-warm-dim text-center font-bold">
                NO DIVIDEND HISTORY
              </p>
            ) : (
              <table className="w-full text-[11px] num">
                <thead>
                  <tr className="border-b border-border bg-white/[0.015]">
                    <th className="text-left px-4 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">EX DATE</th>
                    <th className="text-left px-2 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">TYPE</th>
                    <th className="text-right px-2 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">DPS</th>
                    <th className="text-right px-4 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">PAYMENT</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {recentDividends.map((d, i) => (
                    <tr key={i} className="row-hover hover:bg-white/[0.025] transition-colors">
                      <td className="px-4 py-2 text-foreground tracking-[0.05em] uppercase">
                        {new Date(d.exDate).toLocaleDateString('en-PK', { year: '2-digit', month: 'short', day: '2-digit' }).toUpperCase()}
                      </td>
                      <td className="px-2 py-2">
                        <span className="text-[9px] uppercase tracking-[0.15em] text-warm-muted font-bold">
                          {d.dividendType}
                        </span>
                      </td>
                      <td className="text-right px-2 py-2 text-foreground font-bold">Rs.{d.dps}</td>
                      <td className="text-right px-4 py-2 text-warm-muted uppercase tracking-[0.05em]">
                        {d.paymentDate
                          ? new Date(d.paymentDate).toLocaleDateString('en-PK', { month: 'short', day: '2-digit', year: '2-digit' }).toUpperCase()
                          : '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </TabsContent>

        <TabsContent value="fundamentals" className="mt-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <MetricGroup title="DIVIDEND GROWTH" metrics={[
              { label: 'DPS CURRENT',         value: formatRs(metrics.dividendGrowth.dividendPerShare) },
              { label: 'CAGR 3Y',             value: formatPct(metrics.dividendGrowth.dividendCagr3y), tone: growthTone(metrics.dividendGrowth.dividendCagr3y) },
              { label: 'CAGR 5Y',             value: formatPct(metrics.dividendGrowth.dividendCagr5y), tone: growthTone(metrics.dividendGrowth.dividendCagr5y) },
              { label: 'EARNINGS CAGR 3Y',    value: formatPct(metrics.dividendGrowth.earningsCagr3y), tone: growthTone(metrics.dividendGrowth.earningsCagr3y) },
              { label: 'GROWTH CONSISTENCY',  value: formatScore(metrics.dividendGrowth.growthConsistency), tone: scoreTone(metrics.dividendGrowth.growthConsistency) },
            ]} />
            <MetricGroup title="OPPORTUNITY SIGNALS" metrics={[
              { label: 'QUALITY SIGNAL',      value: formatScore(metrics.opportunity.qualitySignal),     tone: scoreTone(metrics.opportunity.qualitySignal) },
              { label: 'VALUE SIGNAL',        value: formatScore(metrics.opportunity.valueSignal),       tone: scoreTone(metrics.opportunity.valueSignal) },
              { label: 'TECHNICAL SIGNAL',    value: formatScore(metrics.opportunity.technicalSignal),   tone: scoreTone(metrics.opportunity.technicalSignal) },
              { label: 'OPPORTUNITY SCORE',   value: formatScore(metrics.opportunity.opportunityScore),  tone: scoreTone(metrics.opportunity.opportunityScore) },
            ]} />
          </div>
        </TabsContent>

        {hasBanking && metrics.banking && (
          <TabsContent value="banking" className="mt-3">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <MetricGroup title="PROFITABILITY" metrics={[
                { label: 'NET INTEREST MARGIN', value: formatPct(metrics.banking.nim),            tone: nimTone(metrics.banking.nim) },
                { label: 'RETURN ON ASSETS',    value: formatPct(metrics.banking.roa),            tone: roaTone(metrics.banking.roa) },
                { label: 'RETURN ON EQUITY',    value: formatPct(metrics.banking.roe),            tone: roeTone(metrics.banking.roe) },
                { label: 'COST TO INCOME',      value: formatPct(metrics.banking.costToIncome),   tone: cirTone(metrics.banking.costToIncome) },
                { label: 'FEE INCOME RATIO',    value: formatPct(metrics.banking.feeIncomeRatio) },
              ]} />
              <MetricGroup title="CAPITAL & LIQUIDITY" metrics={[
                { label: 'CAR',       value: formatPct(metrics.banking.car),         tone: carTone(metrics.banking.car) },
                { label: 'TIER 1',    value: formatPct(metrics.banking.tier1Ratio),  tone: carTone(metrics.banking.tier1Ratio) },
                { label: 'LCR',       value: formatPct(metrics.banking.lcr) },
                { label: 'ADR',       value: formatPct(metrics.banking.adr) },
                { label: 'NPL RATIO', value: formatPct(metrics.banking.nplRatio),    tone: nplTone(metrics.banking.nplRatio) },
              ]} />
            </div>
          </TabsContent>
        )}

        <TabsContent value="memo" className="mt-3">
          <AiMemoTab symbol={summary.symbol} />
        </TabsContent>
      </Tabs>
    </div>
  );
}

function TerminalTabTrigger({ value, label }: { value: string; label: string }) {
  return (
    <TabsTrigger
      value={value}
      className={cn(
        'rounded-none border-r border-border last:border-r-0 bg-transparent px-4 py-2',
        'text-[10px] uppercase tracking-[0.18em] font-bold text-warm-dim',
        'data-[state=active]:bg-primary/[0.08] data-[state=active]:text-amber data-[state=active]:shadow-none',
        'hover:text-foreground transition-colors',
      )}
    >
      {label}
    </TabsTrigger>
  );
}

interface MetricRow {
  label: string;
  value: string;
  tone?: string;
}

function MetricGroup({ title, metrics }: { title: string; metrics: MetricRow[] }) {
  return (
    <div className="panel">
      <div className="panel-strip px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
          ▌ {title}
        </span>
      </div>
      <div className="divide-y divide-border">
        {metrics.map(({ label, value, tone }) => (
          <div key={label} className="flex justify-between items-center px-4 py-2 hover:bg-amber-500/[0.04] transition-colors">
            <span className="text-[10px] uppercase tracking-[0.15em] text-warm-muted font-bold">{label}</span>
            <span className={cn('num font-bold text-[12px]', tone ?? 'text-foreground')}>{value}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

/* ── Per-metric tone helpers (live in this file because they're stock-specific) ── */

function fmtX(v: number | null | undefined, decimals = 2): string {
  if (v == null) return '—';
  return `${v.toFixed(decimals)}X`;
}

function payoutTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v > 1)    return 'text-warn';   // > 100% payout — unsustainable
  if (v > 0.8)  return 'text-amber';  // 80-100% rising
  return 'text-cyan-pos';             // healthy
}

function debtToEquityTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v > 2)    return 'text-warn';
  if (v > 1)    return 'text-amber';
  return 'text-cyan-pos';
}

function yearsTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v >= 10) return 'text-cyan-pos';
  if (v >= 5)  return 'text-amber';
  return 'text-warm-muted';
}

function peTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v <= 0)  return 'text-warn';
  if (v < 8)   return 'text-cyan-pos';
  if (v < 15)  return 'text-amber';
  return 'text-warn';
}

function pbTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v < 1)    return 'text-cyan-pos';
  if (v < 2)    return 'text-amber';
  return 'text-warm-muted';
}

function mosTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v >= 20)  return 'text-cyan-pos';
  if (v >= 0)   return 'text-amber';
  return 'text-warn';
}

function rsiTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v < 30)  return 'text-cyan-pos';   // oversold = opportunity
  if (v > 70)  return 'text-warn';       // overbought = caution
  return 'text-warm-muted';
}

function drawdownTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v < -20) return 'text-warn';
  if (v < -10) return 'text-amber';
  return 'text-warm-muted';
}

function growthTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v >= 15) return 'text-cyan-pos';
  if (v >= 5)  return 'text-amber';
  if (v >= 0)  return 'text-warm-muted';
  return 'text-warn';
}

/* Banking-specific bands */
function nimTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v >= 4) return 'text-cyan-pos';
  if (v >= 3) return 'text-amber';
  return 'text-warn';
}

function roaTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v >= 1.5) return 'text-cyan-pos';
  if (v >= 1)   return 'text-amber';
  return 'text-warn';
}

function roeTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v >= 18) return 'text-cyan-pos';
  if (v >= 12) return 'text-amber';
  return 'text-warn';
}

function cirTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v < 45)  return 'text-cyan-pos';
  if (v < 55)  return 'text-amber';
  return 'text-warn';
}

function carTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v >= 17) return 'text-cyan-pos';
  if (v >= 14) return 'text-amber';
  return 'text-warn';
}

function nplTone(v: number | null | undefined): string {
  if (v == null) return 'text-warm-dim';
  if (v < 3)  return 'text-cyan-pos';
  if (v < 6)  return 'text-amber';
  return 'text-warn';
}
