'use client';

import { cn } from '@/lib/utils';
import {
  scoreColor, scoreBarColor, signalLabel,
  flagStyle, sectorColor,
} from '@/lib/utils/format';

// ── Score bar ─────────────────────────────────────────────────────────────────
// Monospace number + thin bar with glow fill

export function ScoreBar({ score, className }: { score: number | null | undefined; className?: string }) {
  const pct = score != null ? Math.min(100, Math.max(0, score)) : 0;
  const color = scoreBarColor(score);
  const glowClass =
    score == null ? '' :
    score >= 70 ? 'score-glow-green' :
    score >= 50 ? 'score-glow-yellow' :
    'score-glow-red';

  return (
    <div className={cn('flex items-center gap-2', className)}>
      <span
        className="num text-[13px] font-medium w-6 shrink-0 text-right leading-none"
        style={{ color: score != null ? color : undefined }}
      >
        {score != null ? Math.round(score) : <span className="text-muted-foreground/40">-</span>}
      </span>
      <div className="flex-1 h-[3px] rounded-full bg-border/40 overflow-visible min-w-[40px]">
        <div
          className={cn('h-full rounded-full transition-all duration-500', glowClass)}
          style={{ width: `${pct}%`, backgroundColor: color }}
        />
      </div>
    </div>
  );
}

// ── Score card ────────────────────────────────────────────────────────────────
// Large gauge for stock detail page

export function ScoreCard({ label, score }: { label: string; score: number | null | undefined }) {
  const pct = score != null ? Math.min(100, Math.max(0, score)) : 0;
  const color = scoreBarColor(score);
  const glowClass =
    score == null ? '' :
    score >= 70 ? 'score-glow-green' :
    score >= 50 ? 'score-glow-yellow' :
    'score-glow-red';

  return (
    <div className="card-panel flex flex-col items-center gap-2.5 p-5 rounded-lg border border-border/50 bg-card">
      <div className="num text-4xl font-semibold tracking-tight leading-none" style={{ color: score != null ? color : undefined }}>
        {score != null ? Math.round(score) : '—'}
      </div>
      <div className="text-[10px] text-muted-foreground/60 uppercase tracking-[0.15em] font-medium">{label}</div>
      <div className="w-full h-[3px] rounded-full bg-border/30 overflow-visible mt-0.5">
        <div
          className={cn('h-full rounded-full transition-all duration-700', glowClass)}
          style={{ width: `${pct}%`, backgroundColor: color }}
        />
      </div>
    </div>
  );
}

// ── Signal badge ──────────────────────────────────────────────────────────────
// Left-bar indicator instead of pill — reads like a Bloomberg category tag

export function SignalBadge({ signal, className }: { signal: string | null | undefined; className?: string }) {
  const color = signalIndicatorColor(signal);
  return (
    <span className={cn(
      'inline-flex items-center gap-1.5 px-2 py-[3px] rounded-sm border border-border/30 bg-card/60 text-[11px] font-medium tracking-wide',
      className,
    )}>
      <span
        className="w-[2px] h-3 rounded-full shrink-0"
        style={{ backgroundColor: color }}
      />
      <span style={{ color }}>{signalLabel(signal)}</span>
    </span>
  );
}

// ── Flag badge ────────────────────────────────────────────────────────────────

export function FlagBadge({ flag }: { flag: string }) {
  const { label, className } = flagStyle(flag);
  return (
    <span className={cn(
      'inline-flex items-center px-1.5 py-[2px] rounded-sm text-[10px] font-medium border leading-none tracking-wide',
      className,
    )}>
      {label}
    </span>
  );
}

// ── Sector badge ──────────────────────────────────────────────────────────────

export function SectorBadge({ sector }: { sector: string }) {
  return (
    <span className={cn(
      'inline-flex items-center px-1.5 py-[2px] rounded-sm text-[10px] font-medium tracking-wide',
      sectorColor(sector),
    )}>
      {sector}
    </span>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function signalIndicatorColor(signal: string | null | undefined): string {
  switch (signal?.toLowerCase()) {
    case 'strongbuy':  return '#10b981';
    case 'buy':        return '#22c55e';
    case 'accumulate': return '#84cc16';
    case 'hold':       return '#eab308';
    case 'sell':       return '#f97316';
    case 'strongsell': return '#ef4444';
    default:           return '#6b7280';
  }
}
