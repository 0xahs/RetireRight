export interface MetricMeta {
  label: string;
  unit: 'pct' | 'x' | 'rs' | 'count' | 'score' | 'raw';
  higherIsBetter: boolean;
  benchmarks?: { good: number; bad: number };
  description?: string;
}

export const METRIC_KEYS: Record<string, MetricMeta> = {
  // Dividend Safety (a)
  a1_earnings_payout_ratio: { label: 'Earnings Payout Ratio', unit: 'pct', higherIsBetter: false, benchmarks: { good: 60, bad: 90 } },
  a4_dividend_coverage: { label: 'Dividend Coverage', unit: 'x', higherIsBetter: true, benchmarks: { good: 1.5, bad: 1.0 } },
  a6_debt_to_equity: { label: 'Debt to Equity', unit: 'x', higherIsBetter: false, benchmarks: { good: 0.5, bad: 1.5 } },
  a7_interest_coverage: { label: 'Interest Coverage', unit: 'x', higherIsBetter: true, benchmarks: { good: 3, bad: 1.5 } },
  a14_consecutive_div_years: { label: 'Consecutive Div Years', unit: 'count', higherIsBetter: true, benchmarks: { good: 5, bad: 2 } },

  // Dividend Growth (b)
  b1_dividend_cagr_3y: { label: 'Dividend CAGR 3Y', unit: 'pct', higherIsBetter: true },
  b2_dividend_cagr_5y: { label: 'Dividend CAGR 5Y', unit: 'pct', higherIsBetter: true },
  b3_earnings_cagr_3y: { label: 'Earnings CAGR 3Y', unit: 'pct', higherIsBetter: true },
  b9_dps_current: { label: 'DPS (Current)', unit: 'rs', higherIsBetter: true },
  b10_growth_consistency: { label: 'Growth Consistency', unit: 'score', higherIsBetter: true },

  // Valuation (c)
  c1_pe_ratio: { label: 'P/E Ratio', unit: 'x', higherIsBetter: false, benchmarks: { good: 8, bad: 20 } },
  c2_pb_ratio: { label: 'P/B Ratio', unit: 'x', higherIsBetter: false, benchmarks: { good: 1, bad: 3 } },
  c9_dividend_yield: { label: 'Dividend Yield', unit: 'pct', higherIsBetter: true, benchmarks: { good: 7, bad: 3 } },
  c18_margin_of_safety: { label: 'Margin of Safety', unit: 'pct', higherIsBetter: true },
  c19_earnings_yield: { label: 'Earnings Yield', unit: 'pct', higherIsBetter: true },
  c20_graham_number: { label: 'Graham Number', unit: 'rs', higherIsBetter: true },

  // Risk (f)
  f1_beta: { label: 'Beta', unit: 'x', higherIsBetter: false, benchmarks: { good: 0.8, bad: 1.5 } },
  f4_rsi_14: { label: 'RSI (14)', unit: 'raw', higherIsBetter: false },
  f5_volatility_30d: { label: 'Volatility (30D)', unit: 'pct', higherIsBetter: false },
  f7_drawdown_from_52w_high: { label: 'Drawdown from 52W High', unit: 'pct', higherIsBetter: false },
  f8_range_position_52w: { label: '52W Range Position', unit: 'pct', higherIsBetter: false },

  // Banking (h)
  h1_nim: { label: 'Net Interest Margin', unit: 'pct', higherIsBetter: true, benchmarks: { good: 4, bad: 2 } },
  h2_cost_to_income: { label: 'Cost to Income', unit: 'pct', higherIsBetter: false, benchmarks: { good: 45, bad: 65 } },
  h3_npl_ratio: { label: 'NPL Ratio', unit: 'pct', higherIsBetter: false, benchmarks: { good: 5, bad: 10 } },
  h4_gross_npl_ratio: { label: 'Gross NPL Ratio', unit: 'pct', higherIsBetter: false },
  h5_car: { label: 'Capital Adequacy Ratio', unit: 'pct', higherIsBetter: true, benchmarks: { good: 15, bad: 10 } },
  h6_tier1_ratio: { label: 'Tier 1 Capital Ratio', unit: 'pct', higherIsBetter: true },
  h7_lcr: { label: 'Liquidity Coverage Ratio', unit: 'pct', higherIsBetter: true, benchmarks: { good: 150, bad: 100 } },
  h8_adr: { label: 'Advance to Deposit Ratio', unit: 'pct', higherIsBetter: false, benchmarks: { good: 60, bad: 80 } },
  h9_roa: { label: 'Return on Assets', unit: 'pct', higherIsBetter: true, benchmarks: { good: 1.5, bad: 0.5 } },
  h10_roe: { label: 'Return on Equity', unit: 'pct', higherIsBetter: true, benchmarks: { good: 18, bad: 10 } },
  h11_loans_growth: { label: 'Loans Growth', unit: 'pct', higherIsBetter: true },
  h12_fee_income_ratio: { label: 'Fee Income Ratio', unit: 'pct', higherIsBetter: true },

  // Opportunity (o)
  o1_quality_signal: { label: 'Quality Signal', unit: 'score', higherIsBetter: true },
  o2_value_signal: { label: 'Value Signal', unit: 'score', higherIsBetter: true },
  o3_technical_signal: { label: 'Technical Signal', unit: 'score', higherIsBetter: true },
  o6_opportunity_score: { label: 'Opportunity Score', unit: 'score', higherIsBetter: true },
};

export const OPPORTUNITY_FLAGS: Record<number, { name: string; label: string; sentiment: 'positive' | 'negative' | 'neutral' }> = {
  1: { name: 'OVERSOLD', label: 'RSI < 30 - oversold', sentiment: 'positive' },
  2: { name: 'DEEP_VALUE', label: 'PE < 6 or earnings yield > 15%', sentiment: 'positive' },
  4: { name: 'GRAHAM_DISCOUNT', label: 'Below Graham Number', sentiment: 'positive' },
  8: { name: 'HIGH_QUALITY_DIP', label: 'Good coverage + 15% below 52W high', sentiment: 'positive' },
  16: { name: 'YIELD_ATTRACTIVE', label: 'Yield > 7% with sustainable payout', sentiment: 'positive' },
  32: { name: 'BELOW_200DMA', label: 'Below 200-day moving average', sentiment: 'neutral' },
  64: { name: 'SECTOR_CHEAP', label: 'PE 15% below industry average', sentiment: 'positive' },
  128: { name: 'OVERBOUGHT', label: 'RSI > 70 - caution', sentiment: 'negative' },
  256: { name: 'OVERVALUED', label: 'Price 20% above Graham Number', sentiment: 'negative' },
};
