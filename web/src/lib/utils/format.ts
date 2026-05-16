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
