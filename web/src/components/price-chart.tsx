'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import {
  createChart,
  CandlestickSeries,
  HistogramSeries,
  LineSeries,
  CrosshairMode,
  LineStyle,
} from 'lightweight-charts';
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
  const chartRef   = useRef<HTMLDivElement>(null);
  const volumeRef  = useRef<HTMLDivElement>(null);
  const [period, setPeriod]   = useState<Period>('1Y');
  const [tooltip, setTooltip] = useState<{ date: string; open: number; high: number; low: number; close: number; changePct: number | null; volume: number | null } | null>(null);

  const filtered = useMemo(() => {
    const sorted = [...data].sort((a, b) => String(a.date).localeCompare(String(b.date)));
    const days   = PERIODS.find(p => p.label === period)?.days ?? 365;
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - days);
    return sorted.filter(p => new Date(String(p.date)) >= cutoff);
  }, [data, period]);

  const hasOhlc = filtered.some(p => p.open != null && p.high != null && p.low != null);

  // Summary stats for header
  const first = filtered[0]?.close ?? 0;
  const last  = filtered[filtered.length - 1]?.close ?? 0;
  const periodChangePct = first > 0 ? ((last - first) / first) * 100 : 0;
  const isUp  = periodChangePct >= 0;

  useEffect(() => {
    const container = chartRef.current;
    if (!container || filtered.length === 0) return;

    const FONT = "'JetBrains Mono', 'Fira Code', monospace";
    const GRID  = 'rgba(255,255,255,0.04)';
    const TEXT  = 'rgba(255,255,255,0.35)';
    const BORDER = 'rgba(255,255,255,0.08)';
    // Match the app palette so candles agree with the change % in the header above.
    // sRGB equivalents of the CSS tokens — lightweight-charts can't parse oklch:
    //   #3bd5be ≈ --verdict-acc oklch(0.79 0.13 180)  (cyan / up)
    //   #ff4757 ≈ --destructive oklch(0.68 0.22 25)    (red / down)
    const UP   = '#3bd5be';
    const DOWN = '#ff4757';
    const UP_FILL   = 'rgba(59, 213, 190, 0.35)';
    const DOWN_FILL = 'rgba(255, 71, 87, 0.35)';

    // ── Main price chart ─────────────────────────────────────
    const chart = createChart(container, {
      layout: {
        background: { color: 'transparent' },
        textColor: TEXT,
        fontFamily: FONT,
        fontSize: 10,
      },
      grid: {
        vertLines: { color: GRID, style: LineStyle.Solid },
        horzLines: { color: GRID, style: LineStyle.Solid },
      },
      rightPriceScale: {
        borderColor: BORDER,
        scaleMargins: { top: 0.08, bottom: 0.08 },
      },
      timeScale: {
        borderColor: BORDER,
        timeVisible: false,
        fixLeftEdge: true,
        fixRightEdge: true,
      },
      crosshair: {
        mode: CrosshairMode.Normal,
        vertLine: { color: 'rgba(255,255,255,0.2)', style: LineStyle.Dashed, labelBackgroundColor: '#1a1a1a' },
        horzLine: { color: 'rgba(255,255,255,0.2)', style: LineStyle.Dashed, labelBackgroundColor: '#1a1a1a' },
      },
      handleScroll: true,
      handleScale: true,
    });

    // ── Series ───────────────────────────────────────────────
    let priceSeries: ReturnType<typeof chart.addSeries<'Candlestick'>> | ReturnType<typeof chart.addSeries<'Line'>>;

    if (hasOhlc) {
      priceSeries = chart.addSeries(CandlestickSeries, {
        upColor:        UP,
        downColor:      DOWN,
        borderUpColor:  UP,
        borderDownColor:DOWN,
        wickUpColor:    UP,
        wickDownColor:  DOWN,
      });
      priceSeries.setData(
        filtered.map(p => ({
          time:  String(p.date) as import('lightweight-charts').Time,
          open:  p.open  ?? p.close,
          high:  p.high  ?? p.close,
          low:   p.low   ?? p.close,
          close: p.close,
        }))
      );
    } else {
      priceSeries = chart.addSeries(LineSeries, {
        color:     UP,
        lineWidth: 1,
        priceLineVisible: false,
      });
      priceSeries.setData(
        filtered.map(p => ({
          time:  String(p.date) as import('lightweight-charts').Time,
          value: p.close,
        }))
      );
    }

    // ── Crosshair tooltip ────────────────────────────────────
    chart.subscribeCrosshairMove(param => {
      if (!param.time || !param.seriesData.size) {
        setTooltip(null);
        return;
      }
      const raw = param.seriesData.get(priceSeries);
      if (!raw) return;

      const dateStr = String(param.time);
      const match   = filtered.find(p => String(p.date) === dateStr);

      if (hasOhlc && 'open' in raw) {
        setTooltip({
          date: dateStr,
          open: (raw as { open: number }).open,
          high: (raw as { high: number }).high,
          low:  (raw as { low: number }).low,
          close:(raw as { close: number }).close,
          changePct: match?.changePct ?? null,
          volume:    match?.volume ?? null,
        });
      } else if ('value' in raw) {
        const val = (raw as { value: number }).value;
        setTooltip({
          date: dateStr, open: val, high: val, low: val, close: val,
          changePct: match?.changePct ?? null,
          volume:    match?.volume ?? null,
        });
      }
    });

    // ── Volume pane ──────────────────────────────────────────
    let volumeChart: ReturnType<typeof createChart> | null = null;
    const hasVolume = filtered.some(p => p.volume != null && p.volume > 0);
    if (hasVolume && volumeRef.current) {
      volumeChart = createChart(volumeRef.current, {
        layout: { background: { color: 'transparent' }, textColor: TEXT, fontFamily: FONT, fontSize: 10 },
        grid: { vertLines: { color: 'transparent' }, horzLines: { color: 'transparent' } },
        rightPriceScale: { borderColor: BORDER, scaleMargins: { top: 0.1, bottom: 0 } },
        timeScale: { borderColor: BORDER, visible: false },
        crosshair: { mode: CrosshairMode.Normal, vertLine: { color: 'rgba(255,255,255,0.2)', style: LineStyle.Dashed, labelVisible: false }, horzLine: { visible: false, labelVisible: false } },
        handleScroll: false,
        handleScale:  false,
      });

      const volSeries = volumeChart.addSeries(HistogramSeries, {
        priceFormat: { type: 'volume' },
        priceScaleId: 'vol',
      });
      volumeChart.priceScale('vol').applyOptions({ scaleMargins: { top: 0.1, bottom: 0 } });

      volSeries.setData(
        filtered
          .filter(p => p.volume != null)
          .map(p => {
            const prev = filtered[filtered.indexOf(p) - 1];
            const up   = prev ? p.close >= prev.close : true;
            return {
              time:  String(p.date) as import('lightweight-charts').Time,
              value: p.volume!,
              color: up ? UP_FILL : DOWN_FILL,
            };
          })
      );

      // Sync time ranges
      chart.timeScale().subscribeVisibleLogicalRangeChange(range => {
        if (range && volumeChart) volumeChart.timeScale().setVisibleLogicalRange(range);
      });
    }

    chart.timeScale().fitContent();

    // ── Resize observer ──────────────────────────────────────
    const ro = new ResizeObserver(() => {
      if (chartRef.current) chart.applyOptions({ width: chartRef.current.clientWidth });
      if (volumeRef.current && volumeChart) volumeChart.applyOptions({ width: volumeRef.current.clientWidth });
    });
    if (chartRef.current)  ro.observe(chartRef.current);
    if (volumeRef.current) ro.observe(volumeRef.current);

    return () => {
      ro.disconnect();
      chart.remove();
      volumeChart?.remove();
      setTooltip(null);
    };
  }, [filtered, hasOhlc]);

  if (!data.length) {
    return (
      <div className="h-[200px] flex items-center justify-center text-[11px] uppercase tracking-[0.18em] text-warm-dim font-bold">
        NO PRICE HISTORY
      </div>
    );
  }

  return (
    <div className="space-y-0">
      {/* Header */}
      <div className="flex items-center justify-between px-1 pb-2">
        <div className="flex items-center gap-3">
          <span className="text-[10px] uppercase tracking-[0.15em] text-warm-dim font-bold num">
            {symbol}
          </span>
          {tooltip ? (
            <span className="text-[10px] num text-warm-muted">
              O <span className="text-foreground font-bold">{tooltip.open.toFixed(2)}</span>
              {'  '}H <span className="text-cyan-pos font-bold">{tooltip.high.toFixed(2)}</span>
              {'  '}L <span className="text-warn font-bold">{tooltip.low.toFixed(2)}</span>
              {'  '}C <span className="text-foreground font-bold">{tooltip.close.toFixed(2)}</span>
              {tooltip.changePct != null && (
                <>
                  {'  '}
                  <span className={tooltip.changePct >= 0 ? 'text-cyan-pos font-bold' : 'text-warn font-bold'}>
                    {tooltip.changePct >= 0 ? '+' : ''}{tooltip.changePct.toFixed(2)}%
                  </span>
                </>
              )}
            </span>
          ) : (
            <span className={cn('num text-[11px] font-bold', isUp ? 'text-cyan-pos' : 'text-warn')}>
              {isUp ? '+' : ''}{periodChangePct.toFixed(2)}%
            </span>
          )}
        </div>
        <div className="flex items-center gap-0.5">
          {PERIODS.map(p => (
            <button
              key={p.label}
              onClick={() => setPeriod(p.label)}
              className={cn(
                'px-2 py-1 text-[10px] font-bold uppercase tracking-[0.12em] rounded-sm transition-colors cursor-pointer',
                period === p.label
                  ? 'bg-amber/10 text-amber'
                  : 'text-warm-dim hover:text-foreground hover:bg-white/[0.04]',
              )}
            >
              {p.label}
            </button>
          ))}
        </div>
      </div>

      {/* Price chart */}
      <div ref={chartRef} className="w-full" style={{ height: 280 }} />

      {/* Volume pane */}
      <div ref={volumeRef} className="w-full" style={{ height: 52 }} />

      {/* Footer */}
      <div className="flex justify-between items-center pt-1 px-1">
        <span className="text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold num">
          {filtered.length} SESSIONS · {hasOhlc ? 'CANDLESTICK' : 'LINE'}
        </span>
        <a
          href={`https://www.tradingview.com/chart/?symbol=PSX%3A${symbol}`}
          target="_blank"
          rel="noopener noreferrer"
          className="text-[9px] uppercase tracking-[0.15em] text-warm-dim font-bold hover:text-amber transition-colors"
        >
          TRADINGVIEW ↗
        </a>
      </div>
    </div>
  );
}
