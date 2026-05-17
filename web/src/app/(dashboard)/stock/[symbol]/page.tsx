'use client';

import { useParams } from 'next/navigation';
import { useStock } from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';
import { formatRs, formatPct, formatScore } from '@/lib/utils/format';
import { ScoreCard, SignalBadge, FlagBadge, SectorBadge } from '@/components/stock-ui';
import { PriceChart } from '@/components/price-chart';
import { TrendingUp, TrendingDown } from 'lucide-react';

export default function StockPage() {
  const { symbol } = useParams<{ symbol: string }>();
  const { data, isLoading, error } = useStock(symbol);

  if (isLoading) {
    return (
      <div className="space-y-5">
        <Skeleton className="h-16 w-full rounded-sm" />
        <div className="grid grid-cols-4 gap-3">
          {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-28 w-full rounded-sm" />)}
        </div>
        <Skeleton className="h-64 w-full rounded-sm" />
      </div>
    );
  }

  if (error || !data) {
    return <div className="text-sm text-destructive py-4">Failed to load {symbol}. Is the API running?</div>;
  }

  const { summary, metrics, recentDividends } = data;
  const hasBanking = metrics.banking != null;

  return (
    <div className="space-y-5">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-2.5 mb-1.5">
            <h1 className="num text-2xl font-bold tracking-tight">{summary.symbol}</h1>
            <SectorBadge sector={summary.sector ?? ''} />
          </div>
          <p className="text-muted-foreground text-sm">{summary.name}</p>
        </div>
        <div className="text-right">
          <div className="num text-2xl font-bold tracking-tight">{formatRs(summary.price)}</div>
          {summary.changePct != null && (
            <div className={cn(
              'num text-sm flex items-center justify-end gap-1 mt-0.5 font-medium',
              summary.changePct >= 0 ? 'text-emerald-500' : 'text-red-500'
            )}>
              {summary.changePct >= 0
                ? <TrendingUp className="h-3.5 w-3.5" />
                : <TrendingDown className="h-3.5 w-3.5" />}
              {summary.changePct >= 0 ? '+' : ''}{formatPct(summary.changePct)} today
            </div>
          )}
        </div>
      </div>

      {/* Score cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <ScoreCard label="Composite" score={metrics.scores.composite} />
        <ScoreCard label="Safety"    score={metrics.scores.safety} />
        <ScoreCard label="Growth"    score={metrics.scores.growth} />
        <ScoreCard label="Valuation" score={metrics.scores.valuation} />
      </div>

      {/* Signal + flags strip */}
      <div className="flex items-center gap-2 flex-wrap">
        <SignalBadge signal={summary.signal} />
        {summary.opportunityFlags.map((f) => (
          <FlagBadge key={f} flag={f} />
        ))}
      </div>

      {/* Price chart */}
      {data.priceHistory.length > 0 && (
        <Card className="border-border/50 card-panel px-5 py-4">
          <PriceChart data={data.priceHistory} symbol={summary.symbol} />
        </Card>
      )}

      <Tabs defaultValue="overview">
        <TabsList className="rounded-sm">
          <TabsTrigger value="overview"      className="rounded-sm text-xs">Overview</TabsTrigger>
          <TabsTrigger value="dividends"     className="rounded-sm text-xs">Dividends</TabsTrigger>
          <TabsTrigger value="fundamentals"  className="rounded-sm text-xs">Fundamentals</TabsTrigger>
          {hasBanking && <TabsTrigger value="banking" className="rounded-sm text-xs">Banking</TabsTrigger>}
        </TabsList>

        <TabsContent value="overview" className="mt-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <MetricGroup title="Dividend Safety" metrics={[
              ['Earnings Payout',       formatPct(metrics.dividendSafety.earningsPayoutRatio)],
              ['Dividend Coverage',     `${metrics.dividendSafety.dividendCoverage?.toFixed(2) ?? '—'}x`],
              ['Debt / Equity',         `${metrics.dividendSafety.debtToEquity?.toFixed(2) ?? '—'}x`],
              ['Interest Coverage',     `${metrics.dividendSafety.interestCoverage?.toFixed(1) ?? '—'}x`],
              ['Consecutive Div Years', `${metrics.dividendSafety.consecutiveDivYears ?? '—'}`],
            ]} />
            <MetricGroup title="Valuation" metrics={[
              ['P/E Ratio',         `${metrics.valuation.peRatio?.toFixed(1) ?? '—'}x`],
              ['P/B Ratio',         `${metrics.valuation.pbRatio?.toFixed(2) ?? '—'}x`],
              ['Dividend Yield',    formatPct(metrics.valuation.dividendYield)],
              ['Earnings Yield',    formatPct(metrics.valuation.earningsYield)],
              ['Margin of Safety',  formatPct(metrics.valuation.marginOfSafety)],
            ]} />
            <MetricGroup title="Risk" metrics={[
              ['Beta',              `${metrics.risk.beta?.toFixed(2) ?? '—'}`],
              ['RSI (14)',          `${metrics.risk.rsi14?.toFixed(1) ?? '—'}`],
              ['Volatility 30D',   formatPct(metrics.risk.volatility30d)],
              ['52W Range Pos.',   formatPct(metrics.risk.rangePosition52w)],
              ['Drawdown from 52W', formatPct(metrics.risk.drawdownFrom52wHigh)],
            ]} />
          </div>
        </TabsContent>

        <TabsContent value="dividends" className="mt-4">
          <Card className="overflow-hidden border-border/50 card-panel">
            <CardHeader className="pb-3 border-b border-border/40 bg-muted/5">
              <CardTitle className="text-sm">Recent Dividends</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              {!recentDividends.length ? (
                <p className="px-6 py-8 text-sm text-muted-foreground text-center">No dividend history available</p>
              ) : (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border/40" style={{ background: 'oklch(1 0 0 / 2%)' }}>
                      <th className="text-left px-6 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Ex Date</th>
                      <th className="text-left px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Type</th>
                      <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">DPS</th>
                      <th className="text-right px-6 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Payment Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {recentDividends.map((d, i) => (
                      <tr key={i} className="row-hover relative border-b border-border/30 hover:bg-primary/[0.04] transition-colors">
                        <td className="px-6 py-3 num text-[13px]">
                          {new Date(d.exDate).toLocaleDateString('en-PK', { year: 'numeric', month: 'short', day: 'numeric' })}
                        </td>
                        <td className="px-3 py-3">
                          <span className="text-[10px] px-1.5 py-0.5 rounded-sm border border-border/40 bg-muted/30 text-muted-foreground font-medium uppercase tracking-wide">
                            {d.dividendType}
                          </span>
                        </td>
                        <td className="text-right px-3 py-3 num font-semibold text-[13px]">Rs. {d.dps}</td>
                        <td className="text-right px-6 py-3 num text-[13px] text-muted-foreground">
                          {d.paymentDate
                            ? new Date(d.paymentDate).toLocaleDateString('en-PK', { month: 'short', day: 'numeric', year: 'numeric' })
                            : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="fundamentals" className="mt-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <MetricGroup title="Dividend Growth" metrics={[
              ['DPS Current',       formatRs(metrics.dividendGrowth.dividendPerShare)],
              ['CAGR 3Y',           formatPct(metrics.dividendGrowth.dividendCagr3y)],
              ['CAGR 5Y',           formatPct(metrics.dividendGrowth.dividendCagr5y)],
              ['Earnings CAGR 3Y',  formatPct(metrics.dividendGrowth.earningsCagr3y)],
              ['Growth Consistency', formatScore(metrics.dividendGrowth.growthConsistency)],
            ]} />
            <MetricGroup title="Opportunity Signals" metrics={[
              ['Quality Signal',    formatScore(metrics.opportunity.qualitySignal)],
              ['Value Signal',      formatScore(metrics.opportunity.valueSignal)],
              ['Technical Signal',  formatScore(metrics.opportunity.technicalSignal)],
              ['Opportunity Score', formatScore(metrics.opportunity.opportunityScore)],
            ]} />
          </div>
        </TabsContent>

        {hasBanking && metrics.banking && (
          <TabsContent value="banking" className="mt-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <MetricGroup title="Profitability" metrics={[
                ['Net Interest Margin', formatPct(metrics.banking.nim)],
                ['Return on Assets',   formatPct(metrics.banking.roa)],
                ['Return on Equity',   formatPct(metrics.banking.roe)],
                ['Cost to Income',     formatPct(metrics.banking.costToIncome)],
                ['Fee Income Ratio',   formatPct(metrics.banking.feeIncomeRatio)],
              ]} />
              <MetricGroup title="Capital & Liquidity" metrics={[
                ['CAR',       formatPct(metrics.banking.car)],
                ['Tier 1',    formatPct(metrics.banking.tier1Ratio)],
                ['LCR',       formatPct(metrics.banking.lcr)],
                ['ADR',       formatPct(metrics.banking.adr)],
                ['NPL Ratio', formatPct(metrics.banking.nplRatio)],
              ]} />
            </div>
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}

function MetricGroup({ title, metrics }: { title: string; metrics: [string, string][] }) {
  return (
    <Card className="overflow-hidden border-border/50 card-panel">
      <CardHeader className="pb-0 pt-3.5 px-5 border-b border-border/30">
        <CardTitle className="text-[10px] font-semibold uppercase tracking-[0.12em] text-muted-foreground/70 pb-2.5">
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="px-5 py-3 space-y-0 divide-y divide-border/20">
        {metrics.map(([label, value]) => (
          <div key={label} className="flex justify-between items-center py-2.5">
            <span className="text-[12px] text-muted-foreground">{label}</span>
            <span className="num font-semibold text-[13px]">{value}</span>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
