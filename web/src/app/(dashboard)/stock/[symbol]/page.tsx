'use client';

import { useParams } from 'next/navigation';
import { useStock } from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';
import { formatRs, formatPct, formatScore, signalLabel, signalColorClass, scoreColor } from '@/lib/utils/format';
import { METRIC_KEYS } from '@/lib/constants/metricKeys';

export default function StockPage() {
  const { symbol } = useParams<{ symbol: string }>();
  const { data, isLoading, error } = useStock(symbol);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-20 w-full" />
        <div className="grid grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => <Skeleton key={i} className="h-32 w-full" />)}
        </div>
      </div>
    );
  }

  if (error || !data) {
    return <div className="text-sm text-red-500">Failed to load {symbol}. Is the API running?</div>;
  }

  const { summary, metrics, recentDividends } = data;
  const hasBanking = metrics.banking != null;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{summary.symbol}</h1>
          <p className="text-muted-foreground">{summary.name} · {summary.sector}</p>
        </div>
        <div className="text-right">
          <div className="text-2xl font-semibold">{formatRs(summary.price)}</div>
          {summary.changePct != null && (
            <div className={cn('text-sm', summary.changePct >= 0 ? 'text-emerald-500' : 'text-red-500')}>
              {summary.changePct >= 0 ? '+' : ''}{formatPct(summary.changePct)} today
            </div>
          )}
        </div>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <ScoreBadge label="Composite" score={metrics.scores.composite} />
        <ScoreBadge label="Safety" score={metrics.scores.safety} />
        <ScoreBadge label="Growth" score={metrics.scores.growth} />
        <ScoreBadge label="Valuation" score={metrics.scores.valuation} />
      </div>

      <div className="flex items-center gap-2 flex-wrap">
        <span className={cn('font-medium text-sm', signalColorClass(summary.signal))}>
          {signalLabel(summary.signal)}
        </span>
        {summary.opportunityFlags.map((f) => (
          <Badge key={f} variant="outline" className="text-xs">{f}</Badge>
        ))}
      </div>

      <Tabs defaultValue="overview">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="dividends">Dividends</TabsTrigger>
          <TabsTrigger value="fundamentals">Fundamentals</TabsTrigger>
          {hasBanking && <TabsTrigger value="banking">Banking</TabsTrigger>}
        </TabsList>

        <TabsContent value="overview" className="mt-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <MetricGroup title="Dividend Safety" metrics={[
              ['Earnings Payout', formatPct(metrics.dividendSafety.earningsPayoutRatio)],
              ['Dividend Coverage', `${metrics.dividendSafety.dividendCoverage?.toFixed(2) ?? '-'}x`],
              ['Debt/Equity', `${metrics.dividendSafety.debtToEquity?.toFixed(2) ?? '-'}x`],
              ['Interest Coverage', `${metrics.dividendSafety.interestCoverage?.toFixed(1) ?? '-'}x`],
              ['Consecutive Div Years', `${metrics.dividendSafety.consecutiveDivYears ?? '-'}`],
            ]} />
            <MetricGroup title="Valuation" metrics={[
              ['P/E Ratio', `${metrics.valuation.peRatio?.toFixed(1) ?? '-'}x`],
              ['P/B Ratio', `${metrics.valuation.pbRatio?.toFixed(2) ?? '-'}x`],
              ['Dividend Yield', formatPct(metrics.valuation.dividendYield)],
              ['Earnings Yield', formatPct(metrics.valuation.earningsYield)],
              ['Margin of Safety', formatPct(metrics.valuation.marginOfSafety)],
            ]} />
            <MetricGroup title="Risk" metrics={[
              ['Beta', `${metrics.risk.beta?.toFixed(2) ?? '-'}`],
              ['RSI (14)', `${metrics.risk.rsi14?.toFixed(1) ?? '-'}`],
              ['Volatility 30D', formatPct(metrics.risk.volatility30d)],
              ['52W Range Pos.', formatPct(metrics.risk.rangePosition52w)],
              ['Drawdown from 52W', formatPct(metrics.risk.drawdownFrom52wHigh)],
            ]} />
          </div>
        </TabsContent>

        <TabsContent value="dividends" className="mt-4">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Recent Dividends</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              {!recentDividends.length ? (
                <p className="px-6 pb-4 text-sm text-muted-foreground">No dividend history available</p>
              ) : (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border">
                      <th className="text-left px-6 py-2 text-xs text-muted-foreground font-medium">Ex Date</th>
                      <th className="text-left px-3 py-2 text-xs text-muted-foreground font-medium">Type</th>
                      <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">DPS</th>
                      <th className="text-right px-6 py-2 text-xs text-muted-foreground font-medium">Payment Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {recentDividends.map((d, i) => (
                      <tr key={i} className="border-b border-border/50">
                        <td className="px-6 py-2">{new Date(d.exDate).toLocaleDateString('en-PK', { year: 'numeric', month: 'short', day: 'numeric' })}</td>
                        <td className="px-3 py-2 text-muted-foreground">{d.dividendType}</td>
                        <td className="text-right px-3 py-2 font-medium">Rs. {d.dps}</td>
                        <td className="text-right px-6 py-2 text-muted-foreground">
                          {d.paymentDate ? new Date(d.paymentDate).toLocaleDateString('en-PK', { month: 'short', day: 'numeric', year: 'numeric' }) : '-'}
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
              ['DPS Current', formatRs(metrics.dividendGrowth.dividendPerShare)],
              ['CAGR 3Y', formatPct(metrics.dividendGrowth.dividendCagr3y)],
              ['CAGR 5Y', formatPct(metrics.dividendGrowth.dividendCagr5y)],
              ['Earnings CAGR 3Y', formatPct(metrics.dividendGrowth.earningsCagr3y)],
              ['Growth Consistency', formatScore(metrics.dividendGrowth.growthConsistency)],
            ]} />
            <MetricGroup title="Opportunity Signals" metrics={[
              ['Quality Signal', formatScore(metrics.opportunity.qualitySignal)],
              ['Value Signal', formatScore(metrics.opportunity.valueSignal)],
              ['Technical Signal', formatScore(metrics.opportunity.technicalSignal)],
              ['Opportunity Score', formatScore(metrics.opportunity.opportunityScore)],
            ]} />
          </div>
        </TabsContent>

        {hasBanking && metrics.banking && (
          <TabsContent value="banking" className="mt-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <MetricGroup title="Profitability" metrics={[
                ['Net Interest Margin', formatPct(metrics.banking.nim)],
                ['Return on Assets', formatPct(metrics.banking.roa)],
                ['Return on Equity', formatPct(metrics.banking.roe)],
                ['Cost to Income', formatPct(metrics.banking.costToIncome)],
                ['Fee Income Ratio', formatPct(metrics.banking.feeIncomeRatio)],
              ]} />
              <MetricGroup title="Capital & Liquidity" metrics={[
                ['CAR', formatPct(metrics.banking.car)],
                ['Tier 1 Ratio', formatPct(metrics.banking.tier1Ratio)],
                ['LCR', formatPct(metrics.banking.lcr)],
                ['ADR', formatPct(metrics.banking.adr)],
                ['NPL Ratio', formatPct(metrics.banking.nplRatio)],
              ]} />
            </div>
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}

function ScoreBadge({ label, score }: { label: string; score: number | null }) {
  return (
    <Card>
      <CardContent className="p-3 text-center">
        <div className="text-xs text-muted-foreground">{label}</div>
        <div className={cn('text-xl font-semibold mt-1', scoreColor(score))}>
          {formatScore(score)}
        </div>
      </CardContent>
    </Card>
  );
}

function MetricGroup({ title, metrics }: { title: string; metrics: [string, string][] }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm">{title}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        {metrics.map(([label, value]) => (
          <div key={label} className="flex justify-between text-sm">
            <span className="text-muted-foreground">{label}</span>
            <span className="font-medium tabular-nums">{value}</span>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
