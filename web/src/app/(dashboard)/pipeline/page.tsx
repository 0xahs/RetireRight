'use client';

import {
  usePipelineStatus, usePipelineRuns, useTriggerScrape,
  useTriggerCompute, useTriggerExport, useScheduleConfig, useUpdateScheduleConfig,
} from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import {
  Play, RefreshCw, Download, CheckCircle, XCircle,
  Clock, Activity, Zap, CalendarClock,
} from 'lucide-react';
import { useState, useEffect, useRef } from 'react';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:59931';

function statusChip(status: string) {
  switch (status) {
    case 'completed': return 'text-emerald-400 bg-emerald-500/10 border-emerald-500/25';
    case 'failed':    return 'text-red-400 bg-red-500/10 border-red-500/25';
    case 'running':   return 'text-blue-400 bg-blue-500/10 border-blue-500/25';
    default:          return 'text-muted-foreground bg-muted/20 border-border/40';
  }
}

export default function PipelinePage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-semibold tracking-tight">Pipeline Monitor</h1>
        <p className="text-muted-foreground text-xs mt-0.5">
          Scrape, compute, and export pipeline management
        </p>
      </div>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 space-y-4">
          <StatusPanel />
          <RunHistoryTable />
        </div>
        <div className="space-y-4">
          <TriggerPanel />
          <SchedulePanel />
          <LiveEventsPanel />
        </div>
      </div>
    </div>
  );
}

function StatusPanel() {
  const { data, isLoading } = usePipelineStatus();

  if (isLoading) return <Skeleton className="h-40 w-full rounded-sm" />;
  if (!data) return null;

  return (
    <Card className="border-border/50 card-panel overflow-hidden">
      <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
        <div className="flex items-center gap-2">
          <Activity className="h-3.5 w-3.5 text-primary" />
          <CardTitle className="text-sm font-semibold">Pipeline Status</CardTitle>
          {data.activeJob && (
            <span className="ml-auto text-[10px] px-1.5 py-0.5 rounded-sm border border-blue-500/25 bg-blue-500/10 text-blue-400 font-medium uppercase tracking-wide animate-pulse">
              {data.activeJob.jobType} running
            </span>
          )}
        </div>
      </CardHeader>
      <CardContent className="p-4 space-y-3">
        {data.activeJob && (
          <div className="rounded-sm border border-primary/15 bg-primary/[0.04] p-3 space-y-2">
            <div className="flex justify-between text-[12px]">
              <span className="font-medium">{data.activeJob.jobType}</span>
              <span className="num text-muted-foreground">{data.activeJob.progress}/{data.activeJob.total}</span>
            </div>
            {data.activeJob.currentSymbol && (
              <div className="num text-[11px] text-muted-foreground">Processing: {data.activeJob.currentSymbol}</div>
            )}
            <div className="w-full bg-border/40 rounded-full h-1 mt-1">
              <div
                className="bg-primary h-1 rounded-full transition-all duration-300"
                style={{ width: `${data.activeJob.total > 0 ? (data.activeJob.progress / data.activeJob.total) * 100 : 0}%` }}
              />
            </div>
          </div>
        )}

        <div className="grid grid-cols-2 gap-2">
          {data.sources.map((s) => (
            <div key={s.name} className="flex items-start gap-2.5 p-2.5 rounded-sm border border-border/30 bg-muted/[0.03]">
              {s.status === 'completed' ? (
                <CheckCircle className="h-3.5 w-3.5 text-emerald-500 mt-0.5 shrink-0" />
              ) : s.status === 'failed' ? (
                <XCircle className="h-3.5 w-3.5 text-red-500 mt-0.5 shrink-0" />
              ) : s.status === 'running' ? (
                <RefreshCw className="h-3.5 w-3.5 text-blue-500 mt-0.5 shrink-0 animate-spin" />
              ) : (
                <Clock className="h-3.5 w-3.5 text-muted-foreground/50 mt-0.5 shrink-0" />
              )}
              <div className="min-w-0">
                <div className="text-[12px] font-medium">{s.name}</div>
                <div className="num text-[10px] text-muted-foreground/60 mt-0.5">
                  {s.lastRunAt
                    ? new Date(s.lastRunAt).toLocaleDateString('en-PK', { month: 'short', day: 'numeric' })
                    : 'Never'}
                  {s.stocksScraped != null && ` · ${s.stocksScraped} stocks`}
                </div>
              </div>
            </div>
          ))}
        </div>

        <div className="flex gap-5 num text-[10px] text-muted-foreground/50 pt-1 border-t border-border/20">
          <span>
            Computed:{' '}
            <span className="text-muted-foreground">
              {data.lastComputedAt
                ? new Date(data.lastComputedAt).toLocaleDateString('en-PK', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
                : 'Never'}
            </span>
          </span>
          <span>Queue depth: <span className="text-muted-foreground">{data.queueDepth}</span></span>
        </div>
      </CardContent>
    </Card>
  );
}

function TriggerPanel() {
  const scrape = useTriggerScrape();
  const compute = useTriggerCompute();
  const exportRun = useTriggerExport();
  const anySuccess = scrape.isSuccess || compute.isSuccess || exportRun.isSuccess;
  const anyError = scrape.isError || compute.isError || exportRun.isError;

  return (
    <Card className="border-border/50 card-panel overflow-hidden">
      <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
        <div className="flex items-center gap-2">
          <Zap className="h-3.5 w-3.5 text-primary" />
          <CardTitle className="text-sm font-semibold">Run Jobs</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="p-4 space-y-2">
        <Button
          onClick={() => scrape.mutate(undefined)}
          disabled={scrape.isPending}
          className="w-full h-8 text-xs gap-2 cursor-pointer"
        >
          <Play className="h-3.5 w-3.5" />
          {scrape.isPending ? 'Queuing…' : 'Scrape All'}
        </Button>
        <Button
          variant="outline"
          onClick={() => compute.mutate(undefined)}
          disabled={compute.isPending}
          className="w-full h-8 text-xs gap-2 cursor-pointer border-border/50"
        >
          <RefreshCw className="h-3.5 w-3.5" />
          {compute.isPending ? 'Queuing…' : 'Compute Metrics'}
        </Button>
        <Button
          variant="outline"
          onClick={() => exportRun.mutate()}
          disabled={exportRun.isPending}
          className="w-full h-8 text-xs gap-2 cursor-pointer border-border/50"
        >
          <Download className="h-3.5 w-3.5" />
          {exportRun.isPending ? 'Queuing…' : 'Export Data'}
        </Button>
        {anySuccess && (
          <p className="text-[11px] text-emerald-500 text-center">Job queued successfully</p>
        )}
        {anyError && (
          <p className="text-[11px] text-red-500 text-center">Failed to queue job</p>
        )}
      </CardContent>
    </Card>
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
    <Card className="border-border/50 card-panel overflow-hidden">
      <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
        <div className="flex items-center gap-2">
          <CalendarClock className="h-3.5 w-3.5 text-primary" />
          <CardTitle className="text-sm font-semibold">Auto Schedule</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="p-4 space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-[12px] font-medium">Daily Run</span>
          <button
            onClick={() => {
              const next = !enabled;
              setEnabled(next);
              if (schedule) update.mutate({ ...schedule, enabled: next });
            }}
            className={cn(
              'relative inline-flex h-5 w-9 items-center rounded-full transition-colors cursor-pointer',
              enabled ? 'bg-primary' : 'bg-border/60',
            )}
          >
            <span className={cn(
              'inline-block h-3.5 w-3.5 rounded-full bg-white shadow-sm transition-transform',
              enabled ? 'translate-x-4' : 'translate-x-0.5',
            )} />
          </button>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-[11px] text-muted-foreground">Time (PKT)</span>
          <input
            type="time"
            value={time}
            onChange={(e) => setTime(e.target.value)}
            onBlur={() => {
              if (schedule) update.mutate({ ...schedule, dailyRunTime: time });
            }}
            className="ml-auto num text-[12px] bg-muted/20 rounded-sm px-2 py-1 border border-border/40 text-foreground focus:outline-none focus:border-primary/50"
          />
        </div>
        <p className="num text-[10px] text-muted-foreground/60">
          {enabled ? `Next run at ${time} PKT` : 'Automatic scheduling disabled'}
        </p>
      </CardContent>
    </Card>
  );
}

function LiveEventsPanel() {
  const [events, setEvents] = useState<string[]>([]);
  const eventsRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const es = new EventSource(`${API_URL}/api/pipeline/events`);
    const handler = (e: MessageEvent) => {
      try {
        const evt = JSON.parse(e.data);
        const ts = evt.timestamp ? new Date(evt.timestamp).toLocaleTimeString() : '';
        const label = `[${ts}] ${evt.eventType}${evt.symbol ? ` ${evt.symbol}` : ''}${evt.message ? `: ${evt.message}` : ''}`;
        setEvents((prev) => [...prev.slice(-19), label]);
      } catch {
        setEvents((prev) => [...prev.slice(-19), e.data]);
      }
    };
    const types = ['JOB_QUEUED', 'SCRAPE_STARTED', 'SCRAPE_PROGRESS', 'SCRAPE_COMPLETE', 'COMPUTE_COMPLETE', 'EXPORT_COMPLETE', 'JOB_FAILED'];
    types.forEach((type) => es.addEventListener(type, handler));
    es.onerror = () => es.close();
    return () => es.close();
  }, []);

  useEffect(() => {
    if (eventsRef.current) eventsRef.current.scrollTop = eventsRef.current.scrollHeight;
  }, [events]);

  return (
    <Card className="border-border/50 card-panel overflow-hidden">
      <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
        <div className="flex items-center gap-2">
          <span className="inline-block h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse" />
          <CardTitle className="text-sm font-semibold">Live Events</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="p-3">
        <div
          ref={eventsRef}
          className="h-40 overflow-y-auto num text-[10px] space-y-0.5 text-muted-foreground/60 leading-relaxed"
        >
          {events.length === 0 ? (
            <p className="text-muted-foreground/40">Waiting for events…</p>
          ) : (
            events.map((e, i) => (
              <div key={i} className="border-b border-border/10 pb-0.5">{e}</div>
            ))
          )}
        </div>
      </CardContent>
    </Card>
  );
}

function RunHistoryTable() {
  const { data, isLoading } = usePipelineRuns(1);

  return (
    <Card className="overflow-hidden border-border/50 card-panel">
      <CardHeader className="py-3 px-5 border-b border-border/40 bg-muted/5">
        <CardTitle className="text-sm font-semibold">Run History</CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        {isLoading ? (
          <div className="p-5 space-y-2">
            <Skeleton className="h-8 w-full rounded-sm" />
            <Skeleton className="h-8 w-full rounded-sm" />
          </div>
        ) : !data?.items.length ? (
          <p className="px-6 py-8 text-sm text-muted-foreground text-center">No runs yet</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border/40" style={{ background: 'oklch(1 0 0 / 2%)' }}>
                  <th className="text-left pl-5 pr-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Source</th>
                  <th className="text-left px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Mode</th>
                  <th className="text-left px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Status</th>
                  <th className="text-right px-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Records</th>
                  <th className="text-right pr-5 pl-3 py-2.5 text-[10px] text-muted-foreground/60 font-semibold uppercase tracking-widest">Started</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((r) => (
                  <tr key={r.id} className="border-b border-border/30 hover:bg-primary/[0.04] transition-colors">
                    <td className="pl-5 pr-3 py-3">
                      <span className="num font-semibold text-[13px]">{r.source}</span>
                    </td>
                    <td className="px-3 py-3">
                      <span className="text-[12px] text-muted-foreground">{r.mode}</span>
                    </td>
                    <td className="px-3 py-3">
                      <span className={cn(
                        'text-[10px] font-medium px-1.5 py-0.5 rounded-sm border uppercase tracking-wide',
                        statusChip(r.status),
                      )}>
                        {r.status}
                      </span>
                    </td>
                    <td className="text-right px-3 py-3">
                      <span className="num text-[13px] text-muted-foreground">
                        {r.recordsScraped}/{r.symbolCount}
                      </span>
                    </td>
                    <td className="text-right pr-5 pl-3 py-3">
                      <span className="num text-[11px] text-muted-foreground/60">
                        {new Date(r.startedAt).toLocaleString('en-PK', {
                          month: 'short', day: 'numeric',
                          hour: '2-digit', minute: '2-digit',
                        })}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
