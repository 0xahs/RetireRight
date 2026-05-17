'use client';

import { cn } from '@/lib/utils';

/**
 * Metric registry — human-readable label, definition, formatter, benchmark band.
 * Keys mirror the FundamentalKeys constants on the .NET side.
 */
interface MetricMeta {
  label: string;
  category: 'safety' | 'growth' | 'valuation' | 'risk' | 'banking' | 'opportunity' | 'composite' | 'meta';
  description: string;
  benchmark?: string;
}

const REGISTRY: Record<string, MetricMeta> = {
  'price.close':              { label: 'Close Price',          category: 'meta',        description: 'Last close price from the most recent daily price record.' },
  'composite_score':          { label: 'Composite Score',      category: 'composite',   description: 'Weighted 0-100 score across safety, growth, valuation, and risk dimensions.',  benchmark: '≥70 strong · 50-70 fair · <50 weak' },
  'safety_score':             { label: 'Safety Score',         category: 'composite',   description: 'Dividend Safety component score.', benchmark: '≥70 = A/S tier · <40 = D tier' },

  'a1_earnings_payout_ratio': { label: 'Earnings Payout',      category: 'safety',      description: 'Dividends paid ÷ net earnings. Above 100% means dividends exceed earnings.', benchmark: '<60% safe · 60-80% rising · >100% red line' },
  'a4_dividend_coverage':     { label: 'Dividend Coverage',    category: 'safety',      description: 'Earnings per share ÷ dividend per share. Higher = more cushion.', benchmark: '≥2x strong · ≥1.5x safe · <1x thin' },
  'a7_debt_to_equity':        { label: 'Debt / Equity',        category: 'safety',      description: 'Total debt divided by shareholders equity.', benchmark: '<1x healthy · 1-2x elevated · >2x risky (non-banks)' },
  'a12_consecutive_div_years':{ label: 'Consecutive Div Years',category: 'safety',      description: 'Number of unbroken years paying dividends.', benchmark: '≥10 strongest · ≥5 maturing' },

  'c1_pe_ratio':              { label: 'P/E Ratio',            category: 'valuation',   description: 'Price-to-earnings multiple.', benchmark: '<8 cheap · <15 fair · ≥20 expensive' },
  'c3_pb_ratio':              { label: 'P/B Ratio',            category: 'valuation',   description: 'Price-to-book multiple.', benchmark: '<1 deep value · <1.5 modest · <3 fair' },
  'c9_dividend_yield':        { label: 'Dividend Yield',       category: 'valuation',   description: 'Annual dividends per share ÷ price. Gross of withholding tax.', benchmark: '≥7% attractive · >15% trap risk' },
  'c11_earnings_yield':       { label: 'Earnings Yield',       category: 'valuation',   description: 'Inverse of P/E. Compare to SBP policy rate as opportunity cost.', benchmark: '> SBP rate = positive carry' },
  'c20_price_vs_52w_high':    { label: 'Drawdown from 52W',    category: 'opportunity', description: 'Current price relative to the 52-week high.' },
  'c24_margin_of_safety':     { label: 'Margin of Safety',     category: 'valuation',   description: 'Discount/premium to the Benjamin-Graham intrinsic value.', benchmark: '≥20% strong · ≥0% modest · <-20% overvalued' },

  'f9_price_vs_200dma':       { label: 'Price vs 200DMA',      category: 'risk',        description: 'Percent above/below the 200-day moving average.' },
  'f10_rsi_14':               { label: 'RSI (14)',             category: 'risk',        description: 'Relative Strength Index over 14 sessions.', benchmark: '<30 oversold · >70 overbought' },

  'h1_nim':                   { label: 'NIM',                  category: 'banking',     description: 'Net Interest Margin — banks-only.', benchmark: '≥4% strong · ≥3% fair · <3% weak' },
  'h3_npl_ratio':              { label: 'NPL Ratio',           category: 'banking',     description: 'Non-Performing Loans ratio.', benchmark: '<3% healthy · 3-6% moderate · >6% stress' },
  'h5_car':                   { label: 'CAR',                  category: 'banking',     description: 'Capital Adequacy Ratio — regulatory cushion.', benchmark: '≥17% strong · ≥14% adequate · <12% concern' },
};

const CATEGORY_COLOR: Record<MetricMeta['category'], string> = {
  composite:   'text-amber',
  safety:      'text-cyan-pos',
  growth:      'text-cyan-pos',
  valuation:   'text-amber',
  risk:        'text-foreground/80',
  banking:     'text-cyan-pos',
  opportunity: 'text-amber',
  meta:        'text-warm-muted',
};

interface MetricCitationProps {
  metricKey: string;
  /** Optional: when known, render with the current value highlighted */
  value?: number | null;
  className?: string;
}

export function MetricCitation({ metricKey, value, className }: MetricCitationProps) {
  const meta = REGISTRY[metricKey];
  const label = meta?.label ?? metricKey;
  const tone = meta ? CATEGORY_COLOR[meta.category] : 'text-warm-muted';

  const title = meta
    ? `${meta.label}\n\n${meta.description}${meta.benchmark ? `\n\nBenchmark: ${meta.benchmark}` : ''}${value != null ? `\n\nCurrent value: ${value}` : ''}`
    : `${metricKey}${value != null ? ` = ${value}` : ''}`;

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 num text-[10px] uppercase tracking-[0.1em] font-bold',
        'border border-border/60 px-1 py-px cursor-help transition-colors',
        'hover:border-primary/50 hover:bg-primary/[0.05]',
        tone,
        className,
      )}
      title={title}
    >
      <span className="text-warm-dim">⌖</span>
      {label}
    </span>
  );
}
