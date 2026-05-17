'use client';

import { useState } from 'react';
import { cn } from '@/lib/utils';
import { useStockMemo } from '@/lib/api/queries';
import { Skeleton } from '@/components/ui/skeleton';
import { VerdictBadge } from '@/components/signal/verdict-badge';
import { MetricCitation } from '@/components/signal/metric-citation';
import type { MemoLine, MemoSection } from '@/lib/api/types';

interface AiMemoTabProps {
  symbol: string;
}

export function AiMemoTab({ symbol }: AiMemoTabProps) {
  const { data, isLoading, error } = useStockMemo(symbol);
  const [copied, setCopied] = useState(false);

  async function copyEnvelope() {
    if (!data) return;
    await navigator.clipboard.writeText(data.claudeEnvelope.json);
    setCopied(true);
    setTimeout(() => setCopied(false), 2400);
  }

  function openInClaude() {
    if (!data) return;
    // Copy first, then open Claude in a new tab — the user pastes manually.
    navigator.clipboard.writeText(data.claudeEnvelope.json);
    setCopied(true);
    setTimeout(() => setCopied(false), 2400);
    window.open('https://claude.ai/new', '_blank', 'noopener,noreferrer');
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-20 w-full" style={{ borderRadius: 0 }} />
        <Skeleton className="h-40 w-full" style={{ borderRadius: 0 }} />
        <Skeleton className="h-40 w-full" style={{ borderRadius: 0 }} />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="panel px-5 py-8 text-center">
        <p className="text-warn text-[11px] uppercase tracking-[0.18em] font-bold">
          ✕ MEMO UNAVAILABLE · IS API ALIVE?
        </p>
      </div>
    );
  }

  const ageHours = data.dataAsOf
    ? (Date.now() - new Date(data.dataAsOf).getTime()) / 3_600_000
    : null;
  const ageText =
    ageHours == null ? '—' :
    ageHours < 1 ? '<1H' :
    ageHours < 48 ? `${Math.round(ageHours)}H` :
    `${Math.round(ageHours / 24)}D`;

  const sections = [data.snapshot, data.thesis, data.risks, data.valuation, data.verdictSection];

  return (
    <div className="space-y-3">
      {/* Memo header */}
      <div className="panel">
        <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
            ▌ AI MEMO · {data.symbol}
          </span>
          <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold num">
            DATA {ageText} · {data.metricsPopulated}/{data.metricsTotal} METRICS · CONF {data.sourceConfidence.toUpperCase()}
          </span>
        </div>
        <div className="px-4 py-3 flex items-center justify-between gap-3 flex-wrap">
          <div className="flex items-center gap-2">
            <VerdictBadge verdict={data.verdict} size="md" />
            <span className="text-[10px] uppercase tracking-[0.18em] text-warm-muted font-bold">
              GENERATED {new Date(data.generatedAt).toLocaleString('en-PK', { month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit' }).toUpperCase()}
            </span>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={copyEnvelope}
              className={cn(
                'text-[10px] uppercase tracking-[0.18em] font-bold px-2.5 py-1.5 border transition-colors cursor-pointer',
                copied
                  ? 'border-cyan-pos bg-[oklch(0.79_0.13_180_/_10%)] text-cyan-pos'
                  : 'border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50',
              )}
            >
              {copied ? '✓ COPIED' : '⎘ COPY ENVELOPE'}
            </button>
            <button
              onClick={openInClaude}
              className="text-[10px] uppercase tracking-[0.18em] font-bold px-2.5 py-1.5 border border-primary/60 bg-primary/10 text-amber hover:bg-primary/20 transition-colors cursor-pointer"
            >
              [ ↗ OPEN IN CLAUDE ]
            </button>
          </div>
        </div>
        <div className="border-t border-border px-4 py-2 flex items-center justify-between bg-white/[0.015]">
          <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold num">
            ENVELOPE · <span className="text-cyan-pos">{data.claudeEnvelope.estimatedTokens.toLocaleString()} TOKENS</span> · {(data.claudeEnvelope.sizeBytes / 1024).toFixed(1)} KB
          </span>
          <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">
            ≈ <span className="text-foreground">$0.0{Math.round(data.claudeEnvelope.estimatedTokens * 3 / 1000)}</span> per CLAUDE CALL
          </span>
        </div>
      </div>

      {/* Sections */}
      {sections.map((section) => (
        <MemoSectionPanel key={section.title} section={section} />
      ))}

      {/* Footer disclaimer */}
      <div className="panel px-4 py-3 border-l-[3px] border-l-amber">
        <p className="text-[10px] uppercase tracking-[0.15em] font-bold text-warm-muted">
          ▌ DISCLAIMER
        </p>
        <p className="text-[11px] text-warm-muted mt-1.5 leading-relaxed">
          Generated from RetireRight computed metrics using deterministic templates.
          Verdict thresholds are rule-based — no LLM involvement.
          NOT investment advice; NOT a price forecast; NOT a position-sizing recommendation.
        </p>
      </div>
    </div>
  );
}

function MemoSectionPanel({ section }: { section: MemoSection }) {
  return (
    <div className="panel">
      <div className="panel-strip px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">
          ▌ {section.title}
        </span>
      </div>
      <div className="divide-y divide-border">
        {section.lines.map((line, i) => (
          <MemoLineRow key={i} line={line} />
        ))}
      </div>
    </div>
  );
}

function MemoLineRow({ line }: { line: MemoLine }) {
  return (
    <div className="px-4 py-3 hover:bg-amber-500/[0.04] transition-colors">
      <div className="flex items-start gap-3">
        <span className={cn(
          'text-[9px] uppercase tracking-[0.18em] font-bold mt-px shrink-0 w-12',
          line.isOpinion ? 'text-amber/70' : 'text-cyan-pos',
        )}>
          {line.isOpinion ? 'OPN' : 'FACT'}
        </span>
        <span className="text-[12px] text-foreground/90 leading-relaxed flex-1">{line.text}</span>
      </div>
      {line.citations.length > 0 && (
        <div className="flex flex-wrap gap-1 mt-2 ml-[60px]">
          {line.citations.map((c) => (
            <MetricCitation key={c} metricKey={c} />
          ))}
        </div>
      )}
    </div>
  );
}
