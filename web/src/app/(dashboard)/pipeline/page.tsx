'use client';

import { usePipelineStatus, usePipelineRuns, useTriggerScrape, useTriggerCompute, useTriggerExport, useScheduleConfig, useUpdateScheduleConfig } from '@/lib/api/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { Play, RefreshCw, Download, CheckCircle, XCircle, Clock, AlertCircle } from 'lucide-react';
import { useState, useEffect, useRef } from 'react';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:59931';

export default function PipelinePage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Pipeline Monitor</h1>
        <p className="text-muted-foreground text-sm mt-1">Scrape, compute, and export pipeline management</p>
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

  if (isLoading) return <Skeleton className="h-40 w-full" />;
  if (!data) return null;

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center justify-between">
          Pipeline Status
          {data.activeJob && (
            <Badge variant="default" className="animate-pulse">
              {data.activeJob.jobType} running
            </Badge>
          )}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {data.activeJob && (
          <div className="rounded-md bg-accent/50 p-3 space-y-1">
            <div className="flex justify-between text-sm">
              <span className="font-medium">{data.activeJob.jobType}</span>
              <span className="text-muted-foreground">{data.activeJob.progress}/{data.activeJob.total}</span>
            </div>
            {data.activeJob.currentSymbol && (
              <div className="text-xs text-muted-foreground">Processing: {data.activeJob.currentSymbol}</div>
            )}
            <div className="w-full bg-border rounded-full h-1.5 mt-2">
              <div
                className="bg-primary h-1.5 rounded-full transition-all"
                style={{ width: `${data.activeJob.total > 0 ? (data.activeJob.progress / data.activeJob.total) * 100 : 0}%` }}
              />
            </div>
          </div>
        )}
        <div className="grid grid-cols-2 gap-3">
          {data.sources.map((s) => (
            <div key={s.name} className="flex items-start gap-2 p-2 rounded bg-accent/20">
              {s.status === 'completed' ? (
                <CheckCircle className="h-4 w-4 text-emerald-500 mt-0.5 shrink-0" />
              ) : s.status === 'failed' ? (
                <XCircle className="h-4 w-4 text-red-500 mt-0.5 shrink-0" />
              ) : s.status === 'running' ? (
                <RefreshCw className="h-4 w-4 text-blue-500 mt-0.5 shrink-0 animate-spin" />
              ) : (
                <Clock className="h-4 w-4 text-muted-foreground mt-0.5 shrink-0" />
              )}
              <div>
                <div className="text-xs font-medium">{s.name}</div>
                <div className="text-xs text-muted-foreground">
                  {s.lastRunAt ? new Date(s.lastRunAt).toLocaleDateString() : 'Never'}
                  {s.stocksScraped != null && ` · ${s.stocksScraped} stocks`}
                </div>
              </div>
            </div>
          ))}
        </div>
        <div className="flex gap-4 text-xs text-muted-foreground pt-1">
          <span>Last computed: {data.lastComputedAt ? new Date(data.lastComputedAt).toLocaleDateString() : 'Never'}</span>
          <span>Queue: {data.queueDepth}</span>
        </div>
      </CardContent>
    </Card>
  );
}

function TriggerPanel() {
  const scrape = useTriggerScrape();
  const compute = useTriggerCompute();
  const exportRun = useTriggerExport();

  const buttonClass = 'flex items-center gap-2 px-3 py-2 rounded-md text-sm font-medium transition-colors disabled:opacity-50';

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Run Jobs</CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        <button
          onClick={() => scrape.mutate(undefined)}
          disabled={scrape.isPending}
          className={cn(buttonClass, 'w-full bg-primary text-primary-foreground hover:bg-primary/90')}
        >
          <Play className="h-4 w-4" />
          {scrape.isPending ? 'Queuing...' : 'Scrape All'}
        </button>
        <button
          onClick={() => compute.mutate(undefined)}
          disabled={compute.isPending}
          className={cn(buttonClass, 'w-full bg-secondary text-secondary-foreground hover:bg-secondary/80')}
        >
          <RefreshCw className="h-4 w-4" />
          {compute.isPending ? 'Queuing...' : 'Compute Metrics'}
        </button>
        <button
          onClick={() => exportRun.mutate()}
          disabled={exportRun.isPending}
          className={cn(buttonClass, 'w-full bg-accent text-accent-foreground hover:bg-accent/80')}
        >
          <Download className="h-4 w-4" />
          {exportRun.isPending ? 'Queuing...' : 'Export Data'}
        </button>
        {(scrape.isSuccess || compute.isSuccess || exportRun.isSuccess) && (
          <p className="text-xs text-emerald-500">Job queued successfully</p>
        )}
        {(scrape.isError || compute.isError || exportRun.isError) && (
          <p className="text-xs text-red-500">Failed to queue job</p>
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
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Auto Schedule</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex items-center justify-between">
          <label className="text-sm font-medium">Daily Run</label>
          <button
            onClick={() => {
              setEnabled(!enabled);
              if (schedule) update.mutate({ ...schedule, enabled: !enabled });
            }}
            className={cn(
              'relative inline-flex h-5 w-9 items-center rounded-full transition-colors',
              enabled ? 'bg-primary' : 'bg-border',
            )}
          >
            <span className={cn('inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform', enabled ? 'translate-x-5' : 'translate-x-1')} />
          </button>
        </div>
        <div className="flex items-center gap-2">
          <label className="text-sm text-muted-foreground">Time (PKT)</label>
          <input
            type="time"
            value={time}
            onChange={(e) => setTime(e.target.value)}
            onBlur={() => {
              if (schedule) update.mutate({ ...schedule, dailyRunTime: time });
            }}
            className="ml-auto text-sm bg-accent rounded px-2 py-1 border border-border"
          />
        </div>
        <p className="text-xs text-muted-foreground">
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
        const label = `[${ts}] ${evt.eventType}${evt.symbol ? ` — ${evt.symbol}` : ''}${evt.message ? `: ${evt.message}` : ''}`;
        setEvents((prev) => [...prev.slice(-19), label]);
      } catch {
        setEvents((prev) => [...prev.slice(-19), e.data]);
      }
    };
    const eventTypes = ['JOB_QUEUED', 'SCRAPE_STARTED', 'SCRAPE_PROGRESS', 'SCRAPE_COMPLETE', 'COMPUTE_COMPLETE', 'EXPORT_COMPLETE', 'JOB_FAILED'];
    eventTypes.forEach((type) => es.addEventListener(type, handler));
    es.onerror = () => es.close();
    return () => es.close();
  }, []);

  useEffect(() => {
    if (eventsRef.current) eventsRef.current.scrollTop = eventsRef.current.scrollHeight;
  }, [events]);

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <span className="inline-block h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
          Live Events
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div ref={eventsRef} className="h-40 overflow-y-auto font-mono text-xs space-y-1 text-muted-foreground">
          {events.length === 0 ? (
            <p>Waiting for events...</p>
          ) : (
            events.map((e, i) => <div key={i}>{e}</div>)
          )}
        </div>
      </CardContent>
    </Card>
  );
}

function RunHistoryTable() {
  const { data, isLoading } = usePipelineRuns(1);

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Run History</CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        {isLoading ? (
          <div className="px-6 pb-4 space-y-2"><Skeleton className="h-8 w-full" /><Skeleton className="h-8 w-full" /></div>
        ) : !data?.items.length ? (
          <p className="px-6 pb-4 text-sm text-muted-foreground">No runs yet</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border">
                <th className="text-left px-6 py-2 text-xs text-muted-foreground font-medium">Source</th>
                <th className="text-left px-3 py-2 text-xs text-muted-foreground font-medium">Mode</th>
                <th className="text-left px-3 py-2 text-xs text-muted-foreground font-medium">Status</th>
                <th className="text-right px-3 py-2 text-xs text-muted-foreground font-medium">Scraped</th>
                <th className="text-right px-6 py-2 text-xs text-muted-foreground font-medium">Started</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((r) => (
                <tr key={r.id} className="border-b border-border/50">
                  <td className="px-6 py-2 font-medium">{r.source}</td>
                  <td className="px-3 py-2 text-muted-foreground">{r.mode}</td>
                  <td className="px-3 py-2">
                    <Badge variant={r.status === 'completed' ? 'default' : r.status === 'failed' ? 'destructive' : 'secondary'}>
                      {r.status}
                    </Badge>
                  </td>
                  <td className="text-right px-3 py-2">{r.recordsScraped}/{r.symbolCount}</td>
                  <td className="text-right px-6 py-2 text-muted-foreground text-xs">
                    {new Date(r.startedAt).toLocaleString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </CardContent>
    </Card>
  );
}
