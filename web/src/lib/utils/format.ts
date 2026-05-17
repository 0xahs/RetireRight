export function formatRs(value: number | null | undefined): string {
  if (value == null) return '-';
  if (Math.abs(value) >= 100_000) return `Rs. ${(value / 100_000).toFixed(1)}L`;
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

export function signalColorClass(signal: string | null | undefined): string {
  switch (signal?.toLowerCase()) {
    case 'strongbuy': return 'text-emerald-500';
    case 'buy': return 'text-green-500';
    case 'accumulate': return 'text-lime-500';
    case 'hold': return 'text-yellow-500';
    case 'sell': return 'text-orange-500';
    case 'strongsell': return 'text-red-500';
    default: return 'text-muted-foreground';
  }
}

export function signalLabel(signal: string | null | undefined): string {
  switch (signal?.toLowerCase()) {
    case 'strongbuy': return 'Strong Buy';
    case 'buy': return 'Buy';
    case 'accumulate': return 'Accumulate';
    case 'hold': return 'Hold';
    case 'sell': return 'Sell';
    case 'strongsell': return 'Strong Sell';
    default: return 'N/A';
  }
}

export function freshnessBadge(scrapedAt: string | null | undefined): 'fresh' | 'aging' | 'stale' {
  if (!scrapedAt) return 'stale';
  const hours = (Date.now() - new Date(scrapedAt).getTime()) / 3_600_000;
  if (hours < 4) return 'fresh';
  if (hours < 24) return 'aging';
  return 'stale';
}

export function scoreColor(score: number | null | undefined): string {
  if (score == null) return 'text-muted-foreground';
  if (score >= 70) return 'text-emerald-500';
  if (score >= 50) return 'text-yellow-500';
  return 'text-red-500';
}

export function formatDateOnly(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString('en-PK', {
    year: 'numeric', month: 'short', day: 'numeric',
  });
}

export function scoreBarColor(score: number | null | undefined): string {
  if (score == null) return '#6b7280';
  if (score >= 70) return '#10b981';
  if (score >= 50) return '#eab308';
  return '#ef4444';
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

export function signalBadgeClass(signal: string | null | undefined): string {
  switch (signal?.toLowerCase()) {
    case 'strongbuy':  return 'bg-emerald-500/15 text-emerald-400 border-emerald-500/25';
    case 'buy':        return 'bg-green-500/15 text-green-400 border-green-500/25';
    case 'accumulate': return 'bg-lime-500/15 text-lime-400 border-lime-500/25';
    case 'hold':       return 'bg-yellow-500/15 text-yellow-500 border-yellow-500/25';
    case 'sell':       return 'bg-orange-500/15 text-orange-400 border-orange-500/25';
    case 'strongsell': return 'bg-red-500/15 text-red-400 border-red-500/25';
    default:           return 'bg-muted/50 text-muted-foreground border-border';
  }
}

export function sectorColor(sector: string): string {
  const s = sector?.toLowerCase() ?? '';
  if (s.includes('bank') || s.includes('islamic')) return 'bg-blue-500/10 text-blue-400';
  if (s.includes('fertilizer') || s.includes('chemical')) return 'bg-emerald-500/10 text-emerald-400';
  if (s.includes('power') || s.includes('energy')) return 'bg-amber-500/10 text-amber-400';
  if (s.includes('oil') || s.includes('gas')) return 'bg-orange-500/10 text-orange-400';
  if (s.includes('tech')) return 'bg-violet-500/10 text-violet-400';
  if (s.includes('cement')) return 'bg-stone-400/10 text-stone-400';
  return 'bg-muted/50 text-muted-foreground';
}
