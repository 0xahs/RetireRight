'use client';

import { cn } from '@/lib/utils';

interface BrandMarkProps {
  size?: number;
  className?: string;
  /** When true, use accent colors; when false, monochrome amber */
  accent?: boolean;
}

/**
 * RetireRight brand mark.
 *
 * Geometry: two stacked-block "R" glyphs side-by-side. Each R is rendered on a
 * 6-column × 8-row grid where filled cells form the letter. The cyan tick on
 * the top-right of each R represents the dividend/growth signal.
 *
 *  ▆▆▆░░░    ▆▆▆░░░
 *  ▆░░▆░░    ▆░░▆░░
 *  ▆░░▆░░    ▆░░▆░░
 *  ▆▆▆░░░    ▆▆▆░░░
 *  ▆▆░░░░    ▆▆░░░░
 *  ▆░▆░░░    ▆░▆░░░
 *  ▆░░▆░░    ▆░░▆░░
 *
 * Color tokens come from CSS vars so the mark inherits the theme.
 */
export function BrandMark({ size = 24, className, accent = true }: BrandMarkProps) {
  const cell = size / 16; // 16-unit canvas total (7 cells per R + 1 gap + 7 cells)
  // Single-R cell map (6 wide × 7 tall) — 1 = filled
  const R: number[][] = [
    [1, 1, 1, 1, 0, 0],
    [1, 0, 0, 0, 1, 0],
    [1, 0, 0, 0, 1, 0],
    [1, 1, 1, 1, 0, 0],
    [1, 0, 1, 0, 0, 0],
    [1, 0, 0, 1, 0, 0],
    [1, 0, 0, 0, 1, 0],
  ];

  const stroke = 'var(--primary)';     // amber
  const accentCol = accent ? 'var(--verdict-acc)' : stroke; // cyan tick

  // Each R block uses 6 cells width + 1 cell gap between letters
  const blockWidth = 7;
  const blockHeight = 7;
  const totalWidth = 2 * blockWidth - 1;  // 13 cells wide (overlap none, 1 gap)
  const totalHeight = blockHeight + 1;    // extra row for tick

  const px = (x: number) => x * cell;

  return (
    <svg
      width={size}
      height={size * (totalHeight / totalWidth)}
      viewBox={`0 0 ${px(totalWidth)} ${px(totalHeight)}`}
      className={cn('shrink-0', className)}
      aria-label="RetireRight"
    >
      {[0, 1].map((blockIdx) => {
        const offsetX = blockIdx * blockWidth;
        return (
          <g key={blockIdx}>
            {/* Cyan tick above each R — represents dividend / growth signal */}
            <rect
              x={px(offsetX + 0)}
              y={0}
              width={px(4)}
              height={px(0.6)}
              fill={accentCol}
            />
            {/* The R glyph */}
            {R.map((row, ry) =>
              row.map((cellOn, cx) =>
                cellOn ? (
                  <rect
                    key={`${blockIdx}-${ry}-${cx}`}
                    x={px(offsetX + cx)}
                    y={px(ry + 1)}
                    width={px(0.96)}
                    height={px(0.96)}
                    fill={stroke}
                  />
                ) : null,
              ),
            )}
          </g>
        );
      })}
    </svg>
  );
}

/**
 * Inline RetireRight wordmark — used next to BrandMark in headers.
 * Renders as styled HTML so the existing font (JetBrains Mono) is preserved
 * and the typography aligns with surrounding UI chrome.
 */
export function BrandWordmark({
  size = 'md',
  showSubtitle = true,
  className,
}: {
  size?: 'sm' | 'md' | 'lg';
  showSubtitle?: boolean;
  className?: string;
}) {
  const titleClass = {
    sm: 'text-[12px] tracking-[-0.01em]',
    md: 'text-[15px] tracking-[-0.015em]',
    lg: 'text-[28px] tracking-[-0.02em]',
  }[size];

  const subClass = {
    sm: 'text-[8px] tracking-[0.22em]',
    md: 'text-[9px] tracking-[0.22em]',
    lg: 'text-[10px] tracking-[0.28em]',
  }[size];

  return (
    <div className={cn('leading-none', className)}>
      <div className={cn('font-bold leading-none', titleClass)}>
        <span className="text-foreground">Retire</span>
        <span className="text-amber">Right</span>
      </div>
      {showSubtitle && (
        <div className={cn('font-bold uppercase text-warm-dim mt-1.5', subClass)}>
          PSX · TERMINAL
        </div>
      )}
    </div>
  );
}
