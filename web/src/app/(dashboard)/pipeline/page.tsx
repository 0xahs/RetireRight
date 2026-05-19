'use client';

import {
  usePipelineStatus, usePipelineRuns, useTriggerScrape,
  useTriggerCompute, useTriggerExport, useScheduleConfig, useUpdateScheduleConfig,
  useScraperConfigs, useToggleScraper,
} from '@/lib/api/queries';
import { useQueryClient } from '@tanstack/react-query';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { useState, useEffect, useRef, useCallback } from 'react';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:59931';

interface LiveProgress {
  jobType: string;
  source: string | null;
  progress: number;
  total: number;
  running: boolean;
}

function statusChip(status: string) {
  switch (status) {
    case 'completed': return 'text-cyan-pos bg-[oklch(0.79_0.13_180_/_10%)] border-[oklch(0.79_0.13_180_/_40%)]';
    case 'failed':    return 'text-warn bg-[oklch(0.68_0.22_25_/_10%)] border-[oklch(0.68_0.22_25_/_40%)]';
    case 'running':   return 'text-amber bg-[oklch(0.78_0.16_75_/_10%)] border-[oklch(0.78_0.16_75_/_40%)]';
    default:          return 'text-warm-muted bg-white/[0.02] border-border';
  }
}

function statusMarker(status: string) {
  switch (status) {
    case 'completed': return '✓';
    case 'failed':    return '✕';
    case 'running':   return '◐';
    default:          return '·';
  }
}

// Shared SSE hook — one connection, distributes events to subscribers
function usePipelineEvents(onEvent: (evt: { eventType: string; symbol?: string; message?: string; progress?: number; total?: number }) => void) {
  const onEventRef = useRef(onEvent);
  onEventRef.current = onEvent;

  useEffect(() => {
    const es = new EventSource(`${API_URL}/api/pipeline/events`);
    const handler = (e: MessageEvent) => {
      try { onEventRef.current(JSON.parse(e.data)); } catch {}
    };
    const types = ['JOB_QUEUED','SCRAPE_STARTED','SCRAPE_PROGRESS','SCRAPE_COMPLETE',
                   'COMPUTE_PROGRESS','COMPUTE_COMPLETE','EXPORT_COMPLETE','JOB_FAILED'];
    types.forEach(t => es.addEventListener(t, handler));
    es.onerror = () => es.close();
    return () => es.close();
  }, []);
}

export default function PipelinePage() {
  const qc = useQueryClient();
  const [liveProgress, setLiveProgress] = useState<LiveProgress | null>(null);
  const [events, setEvents] = useState<string[]>([]);

  const handleEvent = useCallback((evt: { eventType: string; symbol?: string; message?: string; progress?: number; total?: number }) => {
    const ts = new Date().toLocaleTimeString();
    const label = `[${ts}] ${evt.eventType}${evt.symbol ? ` ${evt.symbol}` : ''}${evt.message ? `: ${evt.message}` : ''}`;
    setEvents(prev => [...prev.slice(-29), label]);

    if (evt.eventType === 'SCRAPE_PROGRESS' || evt.eventType === 'COMPUTE_PROGRESS') {
      setLiveProgress({
        jobType: evt.eventType === 'SCRAPE_PROGRESS' ? 'scrape' : 'compute',
        source: evt.symbol ?? null,
        progress: evt.progress ?? 0,
        total: evt.total ?? 0,
        running: true,
      });
    } else if (['SCRAPE_COMPLETE','COMPUTE_COMPLETE','EXPORT_COMPLETE','JOB_FAILED'].includes(evt.eventType)) {
      setLiveProgress(null);
      // Refresh status + run history after job completes
      qc.invalidateQueries({ queryKey: ['pipeline', 'status'] });
      qc.invalidateQueries({ queryKey: ['pipeline', 'runs'] });
    } else if (evt.eventType === 'JOB_QUEUED' || evt.eventType.endsWith('_STARTED')) {
      qc.invalidateQueries({ queryKey: ['pipeline', 'status'] });
    }
  }, [qc]);

  usePipelineEvents(handleEvent);

  return (
    <div className="space-y-4">
      <div className="flex items-baseline justify-between gap-3 flex-wrap">
        <div>
          <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
            ┌── PIPELINE
          </div>
          <h1 className="text-foreground font-bold text-[20px] tracking-tight">
            SCRAPE · COMPUTE · EXPORT
          </h1>
        </div>
        <button
          onClick={() => {
            qc.invalidateQueries({ queryKey: ['pipeline', 'status'] });
            qc.invalidateQueries({ queryKey: ['pipeline', 'runs'] });
          }}
          className="inline-flex items-center gap-1.5 text-[10px] uppercase tracking-[0.18em] font-bold px-3 py-1.5 border border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50 cursor-pointer transition-colors"
        >
          ⟳ REFRESH
        </button>
      </div>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 space-y-4">
          <StatusPanel liveProgress={liveProgress} />
          <ScraperConfigPanel />
          <RunHistoryTable />
        </div>
        <div className="space-y-4">
          <TriggerPanel />
          <SchedulePanel />
          <LiveEventsLog events={events} />
        </div>
      </div>
    </div>
  );
}

function ScraperConfigPanel() {
  const { data: configs, isLoading } = useScraperConfigs();
  const toggle = useToggleScraper();

  if (isLoading) return <Skeleton className="h-32 w-full" style={{ borderRadius: 0 }} />;
  if (!configs?.length) return null;

  return (
    <div className="panel">
      <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ DATA SOURCES</span>
        <span className="text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold">DB CONFIG</span>
      </div>
      <div className="divide-y divide-border">
        {configs.map((c) => (
          <div key={c.name} className="flex items-center gap-3 px-4 py-2.5">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <span className="text-[11px] font-bold uppercase tracking-[0.06em] text-foreground">{c.name}</span>
                {c.hasCredentials && (
                  <span className="text-[8px] uppercase tracking-[0.15em] font-bold text-amber border border-amber/30 px-1 py-px">KEY</span>
                )}
              </div>
              <div className="text-[9px] uppercase tracking-[0.1em] text-warm-dim font-medium mt-0.5">
                {c.description} · {c.maxRequestsPerMinute} req/min · {c.minDelayMs/1000}-{c.maxDelayMs/1000}s delay
              </div>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <span className={cn(
                'text-[9px] font-bold uppercase tracking-[0.15em]',
                c.enabled ? 'text-cyan-pos' : 'text-warm-dim',
              )}>
                {c.enabled ? 'ON' : 'OFF'}
              </span>
              <button
                onClick={() => toggle.mutate(c.name)}
                disabled={toggle.isPending}
                className={cn(
                  'relative inline-flex h-4 w-7 items-center transition-colors cursor-pointer border',
                  c.enabled
                    ? 'bg-[oklch(0.79_0.13_180_/_20%)] border-[oklch(0.79_0.13_180_/_50%)]'
                    : 'bg-white/[0.04] border-border',
                  toggle.isPending && 'opacity-50 cursor-not-allowed',
                )}
                aria-label={`Toggle ${c.name}`}
              >
                <span className={cn(
                  'inline-block h-2.5 w-2.5 transition-transform',
                  c.enabled ? 'translate-x-3.5 bg-cyan-pos' : 'translate-x-0.5 bg-warm-dim',
                )} />
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function StatusPanel({ liveProgress }: { liveProgress: LiveProgress | null }) {
  const { data, isLoading } = usePipelineStatus();

  if (isLoading) return <Skeleton className="h-40 w-full" style={{ borderRadius: 0 }} />;
  if (!data) return null;

  const active = liveProgress ?? (data.activeJob ? {
    jobType: data.activeJob.jobType,
    source: data.activeJob.currentSymbol ?? null,
    progress: data.activeJob.progress ?? 0,
    total: data.activeJob.total ?? 0,
    running: true,
  } : null);

  const pct = active && active.total > 0 ? (active.progress / active.total) * 100 : 0;

  return (
    <div className="panel">
      <div className="panel-strip px-4 py-1.5 flex items-center justify-between">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ PIPELINE STATUS</span>
        {active && (
          <span className="text-[9px] tracking-[0.18em] uppercase text-amber font-bold border border-primary/40 bg-primary/10 px-1.5 py-0.5 animate-pulse">
            ◐ {active.jobType} RUNNING
          </span>
        )}
      </div>
      <div className="p-4 space-y-3">
        {active && (
          <div className="border border-primary/30 bg-primary/[0.05] p-3 space-y-2">
            <div className="flex justify-between text-[11px]">
              <span className="font-bold text-amber uppercase tracking-[0.15em]">{active.jobType}</span>
              <span className="num text-foreground font-bold">
                {active.total > 0 ? `${active.progress}/${active.total}` : 'RUNNING…'}
              </span>
            </div>
            {active.source && (
              <div className="num text-[10px] text-warm-muted uppercase tracking-[0.1em]">
                PROCESSING <span className="text-foreground font-bold">{active.source}</span>
              </div>
            )}
            <div className="w-full bg-white/[0.08] h-[3px] overflow-hidden">
              <div
                className={cn(
                  'h-full transition-all duration-500 bg-primary',
                  active.total === 0 && 'animate-pulse w-full',
                )}
                style={{ width: active.total > 0 ? `${pct}%` : '100%' }}
              />
            </div>
          </div>
        )}

        <div className="grid grid-cols-2 gap-2">
          {data.sources.map((s) => {
            const tone =
              s.status === 'completed' ? 'text-cyan-pos border-[oklch(0.79_0.13_180_/_30%)]' :
              s.status === 'failed' ? 'text-warn border-[oklch(0.68_0.22_25_/_30%)]' :
              s.status === 'running' ? 'text-amber border-[oklch(0.78_0.16_75_/_30%)]' :
              'text-warm-dim border-border';
            return (
              <div key={s.name} className={cn('flex items-start gap-2 p-2 border bg-white/[0.015]', tone)}>
                <span className="text-[12px] font-bold leading-none mt-px shrink-0">
                  {statusMarker(s.status)}
                </span>
                <div className="min-w-0">
                  <div className="text-[11px] font-bold text-foreground uppercase tracking-[0.08em]">{s.name}</div>
                  <div className="num text-[9px] text-warm-dim uppercase tracking-[0.1em] mt-0.5 font-medium">
                    {s.lastRunAt
                      ? new Date(s.lastRunAt).toLocaleDateString('en-PK', { month: 'short', day: '2-digit' }).toUpperCase()
                      : 'NEVER'}
                    {s.stocksScraped != null && s.stocksScraped > 0 && (
                      s.name === 'Macro'
                        ? ` · ${s.stocksScraped} IND`
                        : ` · ${s.stocksScraped} STK`
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        <div className="flex gap-5 num text-[9px] uppercase tracking-[0.15em] text-warm-dim pt-2 border-t border-border font-bold">
          <span>
            COMPUTED ·{' '}
            <span className="text-foreground">
              {data.lastComputedAt
                ? new Date(data.lastComputedAt).toLocaleDateString('en-PK', { month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit' }).toUpperCase()
                : 'NEVER'}
            </span>
          </span>
          <span>QUEUE · <span className="text-foreground">{data.queueDepth}</span></span>
        </div>
      </div>
    </div>
  );
}

function TriggerPanel() {
  const { data: status } = usePipelineStatus();
  const scrape = useTriggerScrape();
  const compute = useTriggerCompute();
  const exportRun = useTriggerExport();
  const [pendingSource, setPendingSource] = useState<string | null>(null);

  const triggerSource = (key: string) => {
    setPendingSource(key);
    scrape.mutate({ source: key }, { onSettled: () => setPendingSource(null) });
  };

  const sources = status?.sources ?? [];

  return (
    <div className="panel">
      <div className="panel-strip px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ RUN JOBS</span>
      </div>
      <div className="p-3 space-y-3">
        <div>
          <p className="text-[9px] font-bold uppercase tracking-[0.18em] text-warm-dim mb-2">─ SCRAPE</p>
          <div className="space-y-1">
            {sources.map((s) => (
              <div key={s.key} className="flex items-center gap-2 px-2 py-1.5 border border-border bg-white/[0.015]">
                <div className="flex-1 min-w-0">
                  <div className="text-[11px] font-bold uppercase tracking-[0.05em] text-foreground">{s.name}</div>
                  {s.description && (
                    <div className="text-[9px] uppercase tracking-[0.1em] text-warm-dim font-medium mt-0.5">{s.description}</div>
                  )}
                </div>
                <button
                  onClick={() => triggerSource(s.key)}
                  disabled={scrape.isPending}
                  className={cn(
                    'inline-flex items-center gap-1 px-2 py-1 text-[9px] uppercase tracking-[0.18em] font-bold transition-colors cursor-pointer',
                    'border border-primary/50 text-amber hover:bg-primary/15',
                    scrape.isPending && 'opacity-50 cursor-not-allowed',
                  )}
                >
                  {pendingSource === s.key ? '◐ QUEUE' : '▶ RUN'}
                </button>
              </div>
            ))}
            <button
              onClick={() => scrape.mutate({})}
              disabled={scrape.isPending}
              className={cn(
                'w-full mt-1 text-[10px] uppercase tracking-[0.18em] font-bold py-1.5 border bg-primary/10 border-primary/40 text-amber hover:bg-primary/20 transition-colors cursor-pointer',
                scrape.isPending && 'opacity-50 cursor-not-allowed',
              )}
            >
              {scrape.isPending && !pendingSource ? '◐ QUEUING…' : '[ ▶ SCRAPE ALL ]'}
            </button>
          </div>
        </div>

        <div className="border-t border-border pt-2 space-y-1">
          <p className="text-[9px] font-bold uppercase tracking-[0.18em] text-warm-dim mb-2">─ PIPELINE</p>
          <button
            onClick={() => compute.mutate(undefined)}
            disabled={compute.isPending}
            className="w-full text-[10px] uppercase tracking-[0.18em] font-bold py-1.5 border border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50 transition-colors cursor-pointer disabled:opacity-50"
          >
            {compute.isPending ? '◐ COMPUTING…' : '⟳ COMPUTE METRICS'}
          </button>
          <button
            onClick={() => exportRun.mutate()}
            disabled={exportRun.isPending}
            className="w-full text-[10px] uppercase tracking-[0.18em] font-bold py-1.5 border border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50 transition-colors cursor-pointer disabled:opacity-50"
          >
            {exportRun.isPending ? '◐ EXPORTING…' : '↓ EXPORT DATA'}
          </button>
        </div>

        {(scrape.isSuccess || compute.isSuccess || exportRun.isSuccess) && (
          <p className="text-[9px] uppercase tracking-[0.18em] font-bold text-cyan-pos text-center">✓ JOB QUEUED</p>
        )}
        {(scrape.isError || compute.isError || exportRun.isError) && (
          <p className="text-[9px] uppercase tracking-[0.18em] font-bold text-warn text-center">✕ QUEUE FAILED</p>
        )}
      </div>
    </div>
  );
}

function SchedulePanel() {
  const { data: schedule } = useScheduleConfig();
  const update = useUpdateScheduleConfig();
  const [time, setTime] = useState('06:00');
  const [enabled, setEnabled] = useState(false);

  useEffect(() => {
    if (schedule) {
      setTime(schedule.dailyRunTime);
      setEnabled(schedule.enabled);
    }
  }, [schedule]);

  return (
    <div className="panel">
      <div className="panel-strip px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ AUTO SCHEDULE</span>
      </div>
      <div className="p-3 space-y-2.5">
        <div className="flex items-center justify-between">
          <span className="text-[10px] uppercase tracking-[0.15em] font-bold text-warm-muted">DAILY RUN</span>
          <button
            onClick={() => {
              const next = !enabled;
              setEnabled(next);
              if (schedule) update.mutate({ ...schedule, enabled: next });
            }}
            className={cn(
              'inline-flex items-center gap-1.5 text-[9px] uppercase tracking-[0.18em] font-bold px-2 py-1 border cursor-pointer transition-colors',
              enabled
                ? 'border-cyan-pos text-cyan-pos bg-[oklch(0.79_0.13_180_/_10%)]'
                : 'border-border text-warm-dim bg-card',
            )}
          >
            {enabled ? '● ENABLED' : '○ DISABLED'}
          </button>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-[9px] uppercase tracking-[0.15em] font-bold text-warm-muted">TIME (PKT)</span>
          <input
            type="time"
            value={time}
            onChange={(e) => setTime(e.target.value)}
            onBlur={() => {
              if (schedule) update.mutate({ ...schedule, dailyRunTime: time });
            }}
            className="ml-auto num text-[11px] bg-card border border-border px-2 py-1 text-foreground focus:outline-none focus:border-primary/50 font-bold"
          />
        </div>
        <p className="num text-[9px] uppercase tracking-[0.15em] font-bold text-warm-dim border-t border-border pt-2">
          {enabled ? `NEXT RUN · ${time} PKT` : 'AUTO SCHEDULE DISABLED'}
        </p>
      </div>
    </div>
  );
}

function LiveEventsLog({ events }: { events: string[] }) {
  const eventsRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (eventsRef.current) eventsRef.current.scrollTop = eventsRef.current.scrollHeight;
  }, [events]);

  return (
    <div className="panel">
      <div className="panel-strip px-4 py-1.5 flex items-center gap-2">
        <span className="inline-block h-1.5 w-1.5 bg-cyan-pos animate-pulse" style={{ background: 'var(--verdict-acc)' }} />
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ LIVE EVENTS</span>
      </div>
      <div className="p-2">
        <div
          ref={eventsRef}
          className="h-40 overflow-y-auto num text-[10px] space-y-px text-warm-muted leading-relaxed bg-black/30 border border-border p-2"
        >
          {events.length === 0 ? (
            <p className="text-warm-dim uppercase tracking-[0.15em] font-bold">▶ WAITING FOR EVENTS…</p>
          ) : (
            events.map((e, i) => (
              <div key={i} className="border-b border-border/30 pb-px text-foreground/80">{e}</div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}

function RunHistoryTable() {
  const { data, isLoading } = usePipelineRuns(1);

  return (
    <div className="panel">
      <div className="panel-strip px-4 py-1.5">
        <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ RUN HISTORY</span>
      </div>
      {isLoading ? (
        <div className="p-3 space-y-1.5">
          <Skeleton className="h-8 w-full" style={{ borderRadius: 0 }} />
          <Skeleton className="h-8 w-full" style={{ borderRadius: 0 }} />
        </div>
      ) : !data?.items.length ? (
        <p className="px-6 py-8 text-[11px] uppercase tracking-[0.18em] text-warm-dim text-center font-bold">
          NO RUNS LOGGED
        </p>
      ) : (
        <div className="overflow-auto max-h-72">
          <table className="w-full text-[11px] num">
            <thead>
              <tr className="border-b border-border">
                <th className="sticky top-0 bg-card text-left pl-4 pr-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">SOURCE</th>
                <th className="sticky top-0 bg-card text-left px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">MODE</th>
                <th className="sticky top-0 bg-card text-left px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">STATUS</th>
                <th className="sticky top-0 bg-card text-right px-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">RECORDS</th>
                <th className="sticky top-0 bg-card text-right pr-4 pl-3 py-2 text-[9px] uppercase tracking-[0.18em] text-warm-dim font-bold">STARTED</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data.items.map((r) => (
                <tr key={r.id} className="hover:bg-amber-500/[0.04] transition-colors">
                  <td className="pl-4 pr-3 py-2 text-foreground font-bold tracking-[0.05em] uppercase">{r.source}</td>
                  <td className="px-3 py-2 text-warm-muted uppercase tracking-[0.1em]">{r.mode}</td>
                  <td className="px-3 py-2">
                    <span className={cn(
                      'text-[9px] font-bold px-1.5 py-0.5 border uppercase tracking-[0.15em]',
                      statusChip(r.status),
                    )}>
                      {statusMarker(r.status)} {r.status}
                    </span>
                  </td>
                  <td className="text-right px-3 py-2 text-foreground font-bold">
                    {r.symbolCount > 0 ? `${r.recordsScraped}/${r.symbolCount}` : r.recordsScraped}
                  </td>
                  <td className="text-right pr-4 pl-3 py-2 text-warm-muted uppercase tracking-[0.05em]">
                    {new Date(r.startedAt).toLocaleString('en-PK', {
                      month: 'short', day: '2-digit',
                      hour: '2-digit', minute: '2-digit',
                    }).toUpperCase()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
