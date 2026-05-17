'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTriggerExport } from '@/lib/api/queries';
import { api } from '@/lib/api/client';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';

function useLatestExport() {
  return useQuery({
    queryKey: ['export', 'latest'],
    queryFn: () => api.get<unknown>('/api/export/latest'),
    staleTime: 60_000,
  });
}

function estimateTokens(json: string): number {
  return Math.round(json.length / 3.8);
}

export default function ExportPage() {
  const { data, isLoading, error, refetch, isFetching } = useLatestExport();
  const triggerExport = useTriggerExport();
  const [copied, setCopied] = useState(false);

  const json = data ? JSON.stringify(data, null, 2) : '';
  const tokens = json ? estimateTokens(json) : 0;
  const sizeKb = json ? (json.length / 1024).toFixed(1) : '0';

  async function handleCopy() {
    if (!json) return;
    await navigator.clipboard.writeText(json);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  function handleDownload() {
    if (!json) return;
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `retireright-export-${new Date().toISOString().slice(0, 10)}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }

  async function handleRunExport() {
    await triggerExport.mutateAsync();
    setTimeout(() => refetch(), 3000);
  }

  return (
    <div className="space-y-4">
      <div>
        <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
          ┌── EXPORT
        </div>
        <h1 className="text-foreground font-bold text-[20px] tracking-tight">
          JSON SNAPSHOT · CLAUDE-READY
        </h1>
        <p className="text-warm-muted text-[11px] mt-1 uppercase tracking-[0.05em]">
          PRE-COMPUTED CONTEXT · REDUCES AI SESSION TOKENS FROM ~15K TO ~2K
        </p>
      </div>

      {/* Stat panels */}
      <div className="grid grid-cols-3 gap-3">
        <Panel label="EST. TOKENS" value={isLoading ? '—' : tokens > 0 ? `~${(tokens / 1000).toFixed(1)}K` : '—'} sub="VS ~15-20K LIVE FETCH" />
        <Panel label="FILE SIZE" value={isLoading ? '—' : `${sizeKb} KB`} sub="COMPRESSED JSON" />
        <Panel
          label="SAVINGS"
          value={isLoading || !tokens ? '—' : `${Math.round((1 - tokens / 17500) * 100)}%`}
          valueTone="text-cyan-pos"
          sub="TOKEN REDUCTION"
        />
      </div>

      {/* Action bar */}
      <div className="flex items-center gap-2 flex-wrap">
        <button
          onClick={handleCopy}
          disabled={!json || isLoading}
          className={cn(
            'text-[10px] uppercase tracking-[0.18em] font-bold px-3 py-1.5 border transition-colors cursor-pointer disabled:opacity-50',
            copied
              ? 'border-cyan-pos bg-[oklch(0.79_0.13_180_/_10%)] text-cyan-pos'
              : 'border-primary/60 bg-primary/10 text-amber hover:bg-primary/20',
          )}
        >
          {copied ? '✓ COPIED' : '[ ⎘ COPY TO CLIPBOARD ]'}
        </button>
        <button
          onClick={handleDownload}
          disabled={!json || isLoading}
          className="text-[10px] uppercase tracking-[0.18em] font-bold px-3 py-1.5 border border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50 transition-colors cursor-pointer disabled:opacity-50"
        >
          ↓ DOWNLOAD JSON
        </button>
        <div className="flex-1" />
        <button
          onClick={() => refetch()}
          disabled={isFetching}
          className="text-[10px] uppercase tracking-[0.18em] font-bold px-3 py-1.5 border border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50 transition-colors cursor-pointer disabled:opacity-50"
        >
          {isFetching ? '◐ LOADING' : '⟳ REFRESH'}
        </button>
        <button
          onClick={handleRunExport}
          disabled={triggerExport.isPending}
          className="text-[10px] uppercase tracking-[0.18em] font-bold px-3 py-1.5 border border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50 transition-colors cursor-pointer disabled:opacity-50"
        >
          {triggerExport.isPending ? '◐ QUEUING…' : '▶ RE-RUN EXPORT'}
        </button>
      </div>

      {/* JSON preview panel */}
      <div className="panel">
        <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ EXPORT PREVIEW</span>
          {data != null && (
            <span className="text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold num">
              {sizeKb} KB · ~{tokens.toLocaleString()} TOKENS
            </span>
          )}
        </div>
        {isLoading ? (
          <div className="p-4 space-y-1.5">
            {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-4 w-full" style={{ borderRadius: 0 }} />)}
          </div>
        ) : error ? (
          <div className="px-6 py-8 text-[11px] uppercase tracking-[0.18em] text-warn text-center font-bold">
            ✕ FAILED TO LOAD · IS API ALIVE?
          </div>
        ) : !json ? (
          <div className="px-6 py-8 text-[11px] uppercase tracking-[0.18em] text-warm-dim text-center font-bold">
            NO EXPORT DATA · RUN AN EXPORT FIRST
          </div>
        ) : (
          <pre className="num text-[10px] text-warm-muted leading-relaxed p-4 overflow-auto max-h-[420px] bg-black/30">
            {json.slice(0, 8000)}
            {json.length > 8000 && (
              <span className="text-warm-dim block mt-3 uppercase tracking-[0.15em] font-bold text-[9px]">
                — TRUNCATED · {Math.round((json.length - 8000) / 1024)} KB MORE · COPY OR DOWNLOAD FOR FULL CONTENT
              </span>
            )}
          </pre>
        )}
      </div>

      {/* Claude usage hint */}
      <div className="panel">
        <div className="panel-strip px-4 py-1.5">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ USAGE · CLAUDE SESSION</span>
        </div>
        <div className="p-4">
          <p className="text-[11px] text-warm-muted uppercase tracking-[0.05em]">
            COPY THE JSON ABOVE INTO YOUR CLAUDE CONVERSATION:
          </p>
          <div className="mt-2 px-3 py-2 border border-border bg-black/30 num text-[10px] text-foreground/85">
            &quot;Here is my latest PSX portfolio data: [paste JSON]. Based on this, …&quot;
          </div>
        </div>
      </div>
    </div>
  );
}

function Panel({ label, value, sub, valueTone }: { label: string; value: string; sub: string; valueTone?: string }) {
  return (
    <div className="panel">
      <div className="panel-strip px-3 py-1.5">
        <span className="text-amber font-bold text-[9px] tracking-[0.2em] uppercase">▌ {label}</span>
      </div>
      <div className="px-4 py-3">
        <div className={cn('num text-[22px] font-bold tracking-tight leading-none', valueTone ?? 'text-foreground')}>
          {value}
        </div>
        <div className="num text-[9px] uppercase tracking-[0.15em] font-bold text-warm-dim mt-1.5">{sub}</div>
      </div>
    </div>
  );
}
