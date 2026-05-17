'use client';

import { useParams } from 'next/navigation';
import { useStock } from '@/lib/api/queries';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';
import { formatRs, formatPct, formatScore } from '@/lib/utils/format';
import { FlagBadge } from '@/components/stock-ui';
import { StockLogo } from '@/components/stock-logo';
import { PriceChart } from '@/components/price-chart';
import { VerdictStrip } from '@/components/signal/verdict-strip';
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
  const up = (summary.changePct ?? 0) >= 0;

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
                up ? 'text-cyan-pos' : 'text-warn',
              )}>
                {up ? '▲' : '▼'} {Math.abs(summary.changePct).toFixed(2)}% TODAY
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
        </TabsList>

        <TabsContent value="overview" className="mt-3">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <MetricGroup title="DIVIDEND SAFETY" metrics={[
              ['EARNINGS PAYOUT',       formatPct(metrics.dividendSafety.earningsPayoutRatio)],
              ['DIVIDEND COVERAGE',     `${metrics.dividendSafety.dividendCoverage?.toFixed(2) ?? '—'}X`],
              ['DEBT / EQUITY',         `${metrics.dividendSafety.debtToEquity?.toFixed(2) ?? '—'}X`],
              ['INTEREST COVERAGE',     `${metrics.dividendSafety.interestCoverage?.toFixed(1) ?? '—'}X`],
              ['CONSECUTIVE DIV YEARS', `${metrics.dividendSafety.consecutiveDivYears ?? '—'}`],
            ]} />
            <MetricGroup title="VALUATION" metrics={[
              ['P/E RATIO',         `${metrics.valuation.peRatio?.toFixed(1) ?? '—'}X`],
              ['P/B RATIO',         `${metrics.valuation.pbRatio?.toFixed(2) ?? '—'}X`],
              ['DIVIDEND YIELD',    formatPct(metrics.valuation.dividendYield)],
              ['EARNINGS YIELD',    formatPct(metrics.valuation.earningsYield)],
              ['MARGIN OF SAFETY',  formatPct(metrics.valuation.marginOfSafety)],
            ]} />
            <MetricGroup title="RISK" metrics={[
              ['BETA',              `${metrics.risk.beta?.toFixed(2) ?? '—'}`],
              ['RSI (14)',          `${metrics.risk.rsi14?.toFixed(1) ?? '—'}`],
              ['VOLATILITY 30D',    formatPct(metrics.risk.volatility30d)],
              ['52W RANGE POS.',    formatPct(metrics.risk.rangePosition52w)],
              ['DRAWDOWN 52W',      formatPct(metrics.risk.drawdownFrom52wHigh)],
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
              ['DPS CURRENT',         formatRs(metrics.dividendGrowth.dividendPerShare)],
              ['CAGR 3Y',             formatPct(metrics.dividendGrowth.dividendCagr3y)],
              ['CAGR 5Y',             formatPct(metrics.dividendGrowth.dividendCagr5y)],
              ['EARNINGS CAGR 3Y',    formatPct(metrics.dividendGrowth.earningsCagr3y)],
              ['GROWTH CONSISTENCY',  formatScore(metrics.dividendGrowth.growthConsistency)],
            ]} />
            <MetricGroup title="OPPORTUNITY SIGNALS" metrics={[
              ['QUALITY SIGNAL',      formatScore(metrics.opportunity.qualitySignal)],
              ['VALUE SIGNAL',        formatScore(metrics.opportunity.valueSignal)],
              ['TECHNICAL SIGNAL',    formatScore(metrics.opportunity.technicalSignal)],
              ['OPPORTUNITY SCORE',   formatScore(metrics.opportunity.opportunityScore)],
            ]} />
          </div>
        </TabsContent>

        {hasBanking && metrics.banking && (
          <TabsContent value="banking" className="mt-3">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <MetricGroup title="PROFITABILITY" metrics={[
                ['NET INTEREST MARGIN', formatPct(metrics.banking.nim)],
                ['RETURN ON ASSETS',    formatPct(metrics.banking.roa)],
                ['RETURN ON EQUITY',    formatPct(metrics.banking.roe)],
                ['COST TO INCOME',      formatPct(metrics.banking.costToIncome)],
                ['FEE INCOME RATIO',    formatPct(metrics.banking.feeIncomeRatio)],
              ]} />
              <MetricGroup title="CAPITAL & LIQUIDITY" metrics={[
                ['CAR',       formatPct(metrics.banking.car)],
                ['TIER 1',    formatPct(metrics.banking.tier1Ratio)],
                ['LCR',       formatPct(metrics.banking.lcr)],
                ['ADR',       formatPct(metrics.banking.adr)],
                ['NPL RATIO', formatPct(metrics.banking.nplRatio)],
              ]} />
            </div>
          </TabsContent>
        )}
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

function MetricGroup({ title, metrics }: { title: string; metrics: [string, string][] }) {
  return (
    <div className="panel">
      <div className="panel-strip px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
          ▌ {title}
        </span>
      </div>
      <div className="divide-y divide-border">
        {metrics.map(([label, value]) => (
          <div key={label} className="flex justify-between items-center px-4 py-2 hover:bg-white/[0.02] transition-colors">
            <span className="text-[10px] uppercase tracking-[0.15em] text-warm-muted font-bold">{label}</span>
            <span className="num font-bold text-[12px] text-foreground">{value}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
