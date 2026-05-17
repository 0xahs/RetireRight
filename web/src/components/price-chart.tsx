'use client';

import { useMemo, useState } from 'react';
import {
  Area, AreaChart, CartesianGrid, ResponsiveContainer,
  Tooltip, XAxis, YAxis,
} from 'recharts';
import { cn } from '@/lib/utils';
import type { PricePointDto } from '@/lib/api/types';

const PERIODS = [
  { label: '1M', days: 30 },
  { label: '3M', days: 90 },
  { label: '6M', days: 180 },
  { label: '1Y', days: 365 },
  { label: '2Y', days: 730 },
] as const;

type Period = typeof PERIODS[number]['label'];

interface PriceChartProps {
  data: PricePointDto[];
  symbol: string;
}

export function PriceChart({ data, symbol }: PriceChartProps) {
  const [period, setPeriod] = useState<Period>('1Y');

  const chartData = useMemo(() => {
    const sorted = [...data].sort((a, b) => String(a.date).localeCompare(String(b.date)));
    const days = PERIODS.find((p) => p.label === period)?.days ?? 365;
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - days);
    return sorted
      .filter((p) => new Date(String(p.date)) >= cutoff)
      .map((p) => ({ date: String(p.date), close: Number(p.close), volume: p.volume }));
  }, [data, period]);

  const prices = chartData.map((d) => d.close);
  const minPrice = prices.length ? Math.min(...prices) * 0.985 : 0;
  const maxPrice = prices.length ? Math.max(...prices) * 1.015 : 100;
  const first = chartData[0]?.close ?? 0;
  const last = chartData[chartData.length - 1]?.close ?? 0;
  const isUp = last >= first;
  const changePct = first > 0 ? ((last - first) / first) * 100 : 0;
  const lineColor = isUp ? '#10b981' : '#ef4444';

  if (!data.length) {
    return (
      <div className="h-[200px] flex items-center justify-center text-sm text-muted-foreground">
        No price history available
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {/* Header row */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className="num text-xs text-muted-foreground">{symbol} — {period} performance</span>
          <span className={cn('num text-xs font-semibold', isUp ? 'text-emerald-500' : 'text-red-500')}>
            {isUp ? '+' : ''}{changePct.toFixed(2)}%
          </span>
        </div>
        {/* Period buttons */}
        <div className="flex items-center gap-0.5">
          {PERIODS.map((p) => (
            <button
              key={p.label}
              onClick={() => setPeriod(p.label)}
              className={cn(
                'px-2 py-1 text-[11px] font-medium rounded-sm transition-colors cursor-pointer',
                period === p.label
                  ? 'bg-primary/15 text-primary'
                  : 'text-muted-foreground/60 hover:text-muted-foreground hover:bg-muted/30',
              )}
            >
              {p.label}
            </button>
          ))}
        </div>
      </div>

      {/* Chart */}
      <div className="h-[190px] w-full">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={chartData} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
            <defs>
              <linearGradient id={`price-grad-${symbol}`} x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%"  stopColor={lineColor} stopOpacity={0.18} />
                <stop offset="100%" stopColor={lineColor} stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid
              strokeDasharray="4 4"
              stroke="oklch(1 0 0 / 6%)"
              vertical={false}
            />
            <XAxis
              dataKey="date"
              tickLine={false}
              axisLine={false}
              tick={{ fontSize: 10, fill: 'oklch(0.50 0 0)', fontFamily: 'var(--font-mono)' }}
              tickFormatter={(v: string) =>
                new Date(v).toLocaleDateString('en-PK', { month: 'short', day: 'numeric' })
              }
              minTickGap={55}
              interval="preserveStartEnd"
            />
            <YAxis
              domain={[minPrice, maxPrice]}
              tickLine={false}
              axisLine={false}
              orientation="right"
              width={58}
              tick={{ fontSize: 10, fill: 'oklch(0.50 0 0)', fontFamily: 'var(--font-mono)' }}
              tickFormatter={(v: number) => `${Math.round(v).toLocaleString('en-PK')}`}
            />
            <Tooltip
              content={<PriceTooltip />}
              cursor={{ stroke: lineColor, strokeWidth: 1, strokeDasharray: '3 3' }}
            />
            <Area
              type="monotone"
              dataKey="close"
              stroke={lineColor}
              strokeWidth={1.5}
              fill={`url(#price-grad-${symbol})`}
              dot={false}
              activeDot={{ r: 3, fill: lineColor, strokeWidth: 0 }}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      {/* Bottom stats */}
      <div className="flex items-center justify-between text-[10px] text-muted-foreground/50 px-0.5">
        <span className="num">{chartData.length} trading days</span>
        <span className="num">
          Range: {Math.min(...prices).toLocaleString('en-PK')} – {Math.max(...prices).toLocaleString('en-PK')}
        </span>
      </div>
    </div>
  );
}

function PriceTooltip({ active, payload, label }: {
  active?: boolean;
  payload?: Array<{ value: number; payload: { volume: number | null } }>;
  label?: string;
}) {
  if (!active || !payload?.length) return null;
  const close = payload[0]?.value;
  const volume = payload[0]?.payload?.volume;
  const date = label ? new Date(label).toLocaleDateString('en-PK', {
    weekday: 'short', year: 'numeric', month: 'short', day: 'numeric',
  }) : '';

  return (
    <div className="bg-card border border-border/50 rounded-sm px-3 py-2 shadow-lg text-[11px]">
      <div className="text-muted-foreground mb-1.5">{date}</div>
      <div className="num font-semibold text-[13px]">Rs. {close?.toLocaleString('en-PK')}</div>
      {volume != null && (
        <div className="num text-muted-foreground mt-1">
          Vol: {(Number(volume) / 1000).toFixed(0)}K
        </div>
      )}
    </div>
  );
}
