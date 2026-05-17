'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTriggerExport } from '@/lib/api/queries';
import { api } from '@/lib/api/client';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { Download, Copy, Check, RefreshCw, FileJson, Zap } from 'lucide-react';

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
        <h1 className="text-xl font-semibold tracking-tight">Export Center</h1>
        <p className="text-muted-foreground text-xs mt-0.5">
          Pre-computed JSON snapshot for Claude sessions — reduces token usage from ~15K to ~2-3K
        </p>
      </div>

      {/* Stats + actions */}
      <div className="grid grid-cols-3 gap-4">
        <Card className="border-border/50 card-panel">
          <CardContent className="p-4">
            <div className="text-[10px] text-muted-foreground/60 uppercase tracking-widest font-semibold mb-1.5">Est. Tokens</div>
            <div className="num text-2xl font-bold">
              {isLoading ? '—' : tokens > 0 ? `~${(tokens / 1000).toFixed(1)}K` : '—'}
            </div>
            <div className="text-[11px] text-muted-foreground mt-1">
              vs ~15-20K live fetching
            </div>
          </CardContent>
        </Card>
        <Card className="border-border/50 card-panel">
          <CardContent className="p-4">
            <div className="text-[10px] text-muted-foreground/60 uppercase tracking-widest font-semibold mb-1.5">File Size</div>
            <div className="num text-2xl font-bold">
              {isLoading ? '—' : `${sizeKb} KB`}
            </div>
            <div className="text-[11px] text-muted-foreground mt-1">
              Compressed JSON
            </div>
          </CardContent>
        </Card>
        <Card className="border-border/50 card-panel">
          <CardContent className="p-4">
            <div className="text-[10px] text-muted-foreground/60 uppercase tracking-widest font-semibold mb-1.5">Savings</div>
            <div className="num text-2xl font-bold text-emerald-500">
              {isLoading || !tokens ? '—' : `${Math.round((1 - tokens / 17500) * 100)}%`}
            </div>
            <div className="text-[11px] text-muted-foreground mt-1">
              Token reduction
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Action buttons */}
      <div className="flex items-center gap-2">
        <Button
          onClick={handleCopy}
          disabled={!json || isLoading}
          className="gap-2 h-8 text-xs cursor-pointer"
        >
          {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
          {copied ? 'Copied!' : 'Copy to clipboard'}
        </Button>
        <Button
          variant="outline"
          onClick={handleDownload}
          disabled={!json || isLoading}
          className="gap-2 h-8 text-xs cursor-pointer border-border/50"
        >
          <Download className="h-3.5 w-3.5" />
          Download JSON
        </Button>
        <div className="flex-1" />
        <Button
          variant="outline"
          onClick={() => refetch()}
          disabled={isFetching}
          className="gap-2 h-8 text-xs cursor-pointer border-border/50"
        >
          <RefreshCw className={cn('h-3.5 w-3.5', isFetching && 'animate-spin')} />
          Refresh
        </Button>
        <Button
          variant="outline"
          onClick={handleRunExport}
          disabled={triggerExport.isPending}
          className="gap-2 h-8 text-xs cursor-pointer border-border/50"
        >
          <Zap className="h-3.5 w-3.5" />
          {triggerExport.isPending ? 'Queuing…' : 'Re-run export'}
        </Button>
      </div>

      {/* JSON preview */}
      <Card className="border-border/50 card-panel overflow-hidden">
        <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
          <div className="flex items-center gap-2">
            <FileJson className="h-3.5 w-3.5 text-primary" />
            <CardTitle className="text-sm font-semibold">Export Preview</CardTitle>
            {data != null && (
              <span className="ml-auto num text-[10px] text-muted-foreground/60">
                {sizeKb} KB · ~{tokens.toLocaleString()} tokens
              </span>
            )}
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-5 space-y-2">
              {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-4 w-full rounded-sm" />)}
            </div>
          ) : error ? (
            <div className="px-6 py-8 text-sm text-destructive text-center">
              Failed to load export data. Is the API running?
            </div>
          ) : !json ? (
            <div className="px-6 py-8 text-sm text-muted-foreground text-center">
              No export data available. Run an export first.
            </div>
          ) : (
            <pre className="num text-[11px] text-muted-foreground leading-relaxed p-5 overflow-auto max-h-[420px] bg-background/40">
              {json.slice(0, 8000)}
              {json.length > 8000 && (
                <span className="text-muted-foreground/40">
                  {'\n\n… and {Math.round((json.length - 8000) / 1024)} KB more (copy or download for full content)'}
                </span>
              )}
            </pre>
          )}
        </CardContent>
      </Card>

      {/* Claude prompt hint */}
      <Card className="border-primary/15 bg-primary/[0.03] card-panel">
        <CardContent className="p-4">
          <div className="flex items-start gap-3">
            <Zap className="h-4 w-4 text-primary mt-0.5 shrink-0" />
            <div>
              <div className="text-sm font-medium mb-1">How to use with Claude</div>
              <div className="text-xs text-muted-foreground leading-relaxed">
                Copy the JSON above and paste it into a Claude conversation. Start with:
              </div>
              <div className="mt-2 px-3 py-2 rounded-sm bg-muted/30 border border-border/30 num text-[11px] text-muted-foreground/80">
                {`"Here is my latest PSX portfolio data: [paste JSON]. Based on this, ..."`}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
