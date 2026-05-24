/**
 * Format a Pakistani-rupee value.
 *
 * Rules tuned for dividend-investor UX:
 *   - Prices (small values < Rs. 10,000): 2 decimals always (e.g. Rs. 379.50)
 *   - Mid range (10K–100K): 0 decimals + thousand separators (e.g. Rs. 12,500)
 *   - Lakhs (≥ 1L): compact e.g. Rs. 12.3L
 *
 * Pass { compact: false } to disable the lakh shortening for projection charts.
 */
export function formatRs(
  value: number | null | undefined,
  opts?: { compact?: boolean; decimals?: number },
): string {
  if (value == null) return '-';
  const abs = Math.abs(value);
  const compact = opts?.compact ?? true;

  // Lakhs shortening for big numbers (portfolio totals, annual income at scale)
  if (compact && abs >= 100_000) {
    return `Rs. ${(value / 100_000).toFixed(1)}L`;
  }

  // Explicit decimals override
  if (opts?.decimals != null) {
    return `Rs. ${value.toLocaleString('en-PK', { minimumFractionDigits: opts.decimals, maximumFractionDigits: opts.decimals })}`;
  }

  // Stock prices and other small values keep 2 decimals
  if (abs < 10_000) {
    return `Rs. ${value.toLocaleString('en-PK', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  // Mid-range whole numbers (transaction totals, monthly budget)
  return `Rs. ${value.toLocaleString('en-PK', { maximumFractionDigits: 0 })}`;
}

export function formatPct(value: number | null | undefined, decimals = 1): string {
  if (value == null) return '-';
  return `${value.toFixed(decimals)}%`;
}

export function formatScore(value: number | null | undefined): string {
  if (value == null) return '-';
  return `${Math.round(value)}/100`;
}

export function formatNumber(value: number | null | undefined, decimals = 2): string {
  if (value == null) return '-';
  return value.toFixed(decimals);
}

export function freshnessBadge(scrapedAt: string | null | undefined): 'fresh' | 'aging' | 'stale' {
  if (!scrapedAt) return 'stale';
  const hours = (Date.now() - new Date(scrapedAt).getTime()) / 3_600_000;
  if (hours < 4) return 'fresh';
  if (hours < 24) return 'aging';
  return 'stale';
}

/** Bloomberg-style tone for any 0–100 score */
export function scoreTone(score: number | null | undefined): string {
  if (score == null) return 'text-warm-dim';
  if (score >= 70) return 'text-cyan-pos';
  if (score >= 50) return 'text-amber';
  return 'text-warn';
}

/** Tone for a yield (higher = better, but flag obvious traps) */
export function yieldTone(y: number | null | undefined): string {
  if (y == null) return 'text-warm-dim';
  if (y >= 15) return 'text-warn';        // yield trap territory
  if (y >= 7)  return 'text-cyan-pos';
  if (y >= 4)  return 'text-amber';
  return 'text-warm-muted';
}

/** Tone for dividend coverage (higher = safer) */
export function coverageTone(cov: number | null | undefined): string {
  if (cov == null) return 'text-warm-dim';
  if (cov >= 2)   return 'text-cyan-pos';
  if (cov >= 1.5) return 'text-amber';
  if (cov >= 1)   return 'text-warm-muted';
  return 'text-warn';
}

/** Tone for any change-percent (up/down) */
export function changeTone(pct: number | null | undefined): string {
  if (pct == null) return 'text-warm-dim';
  return pct >= 0 ? 'text-cyan-pos' : 'text-warn';
}

/** Triangle marker for change-percent */
export function changeMarker(pct: number | null | undefined): string {
  if (pct == null) return '·';
  return pct >= 0 ? '▲' : '▼';
}

export function formatDateOnly(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString('en-PK', {
    year: 'numeric', month: 'short', day: 'numeric',
  });
}

export function formatFlag(flag: string): string {
  return flag.split('_').map((w) => w.charAt(0).toUpperCase() + w.slice(1).toLowerCase()).join(' ');
}

const POSITIVE_FLAGS = new Set(['DEEP_VALUE', 'GRAHAM_DISCOUNT', 'HIGH_QUALITY_DIP', 'YIELD_ATTRACTIVE', 'SECTOR_CHEAP', 'OVERSOLD']);
const NEGATIVE_FLAGS = new Set(['OVERBOUGHT', 'OVERVALUED']);

export function flagStyle(flag: string): { label: string; className: string } {
  const label = formatFlag(flag);
  if (POSITIVE_FLAGS.has(flag)) return { label, className: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' };
  if (NEGATIVE_FLAGS.has(flag)) return { label, className: 'bg-red-500/10 text-red-400 border-red-500/20' };
  return { label, className: 'bg-blue-500/10 text-blue-400 border-blue-500/20' };
}

/**
 * Terminal-palette TEXT color for a sector, matched by substring against the
 * full PSX / StockAnalysis sector names (e.g. "Banking Services", "Food & Tobacco",
 * "Electrical Utilities & IPPs"). Used where we want just colored text, not a badge.
 * Order matters — more specific patterns first.
 */
export function sectorTone(sector: string | null | undefined): string {
  const s = (sector ?? '').toLowerCase();
  if (!s) return 'text-warm-muted';
  if (/bank|financ|insur|capital market|credit|leasing|mortgage|brokerage/.test(s)) return 'text-[oklch(0.72_0.11_235)]'; // blue
  if (/chemical|fertiliz|plastic|paint|rubber/.test(s))                   return 'text-[oklch(0.80_0.15_140)]'; // green
  if (/oil|gas|petroleum|refin|fuel/.test(s))                             return 'text-[oklch(0.74_0.16_55)]';  // orange
  if (/power|electric|utilit|ipp|energy/.test(s))                         return 'text-amber';                   // amber
  if (/tech|comput|software|data process|communic|network|semiconduct|electronic/.test(s)) return 'text-[oklch(0.74_0.16_290)]'; // violet
  if (/cement|building material|construc|homebuild|engineering/.test(s))  return 'text-[oklch(0.72_0.05_70)]';  // stone
  if (/food|beverage|tobacco|agri|grain|mill|drug retail|sugar|dairy|livestock|tea/.test(s)) return 'text-[oklch(0.80_0.13_115)]'; // lime
  if (/auto|vehicle|tyre|tire/.test(s))                                   return 'text-[oklch(0.78_0.12_200)]'; // cyan-blue
  if (/textile|apparel|fabric|footwear|cloth|spinning|weaving|leather/.test(s)) return 'text-[oklch(0.74_0.13_350)]'; // pink
  if (/pharma|health|medical|hospital|drug/.test(s))                      return 'text-[oklch(0.79_0.12_165)]'; // teal
  if (/container|packag|paper|board|glass/.test(s))                       return 'text-[oklch(0.72_0.06_85)]';  // taupe
  if (/transport|logistic|freight|airline|air transp|shipping|courier/.test(s)) return 'text-[oklch(0.74_0.10_250)]'; // sky
  if (/cable|wire|steel|metal|iron|alumin/.test(s))                       return 'text-[oklch(0.70_0.08_45)]';  // bronze
  if (/real estate|reit|property|hotel|entertainment/.test(s))            return 'text-[oklch(0.76_0.10_320)]'; // mauve
  return 'text-warm-muted';
}
