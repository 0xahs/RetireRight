'use client';

import { cn } from '@/lib/utils';
import type { ConvictionLevel } from '@/lib/api/types';

interface ConvictionDotsProps {
  level: ConvictionLevel | null | undefined;
  confidence?: number;
  className?: string;
  showLabel?: boolean;
}

export function ConvictionDots({ level, confidence, className, showLabel = false }: ConvictionDotsProps) {
  const filled =
    level === 'HIGH' ? 3 :
    level === 'MEDIUM' ? 2 :
    level === 'LOW' ? 1 : 0;

  const color =
    level === 'HIGH' ? 'var(--verdict-acc)' :
    level === 'MEDIUM' ? 'var(--primary)' :
    level === 'LOW' ? 'oklch(0.55 0.015 80)' :
    'oklch(0.30 0 0)';

  const labelColor =
    level === 'HIGH' ? 'text-cyan-pos' :
    level === 'MEDIUM' ? 'text-amber' :
    level === 'LOW' ? 'text-warm-muted' :
    'text-warm-dim';

  const title = level
    ? `${level} conviction${confidence != null ? ` · ${(confidence * 100).toFixed(0)}%` : ''}`
    : 'Unknown conviction';

  return (
    <span
      className={cn('inline-flex items-center gap-1.5', className)}
      title={title}
    >
      {/* Bar segments instead of dots — more Bloomberg */}
      <span className="inline-flex items-stretch gap-[1px] h-2.5">
        {[0, 1, 2].map(i => (
          <span
            key={i}
            className="w-[3px] transition-colors"
            style={{
              background: i < filled ? color : 'oklch(1 0 0 / 8%)',
            }}
          />
        ))}
      </span>
      {showLabel && level && (
        <span className={cn('text-[9px] font-bold uppercase tracking-[0.15em] num', labelColor)}>
          {level}
        </span>
      )}
    </span>
  );
}
