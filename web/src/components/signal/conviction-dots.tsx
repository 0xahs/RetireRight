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
    level === 'HIGH' ? 'bg-emerald-400' :
    level === 'MEDIUM' ? 'bg-amber-400' :
    level === 'LOW' ? 'bg-muted-foreground/50' :
    'bg-muted/40';

  const labelColor =
    level === 'HIGH' ? 'text-emerald-400' :
    level === 'MEDIUM' ? 'text-amber-400' :
    level === 'LOW' ? 'text-muted-foreground' :
    'text-muted-foreground/50';

  const title = level
    ? `${level} conviction${confidence != null ? ` (${(confidence * 100).toFixed(0)}%)` : ''}`
    : 'Unknown conviction';

  return (
    <span
      className={cn('inline-flex items-center gap-1', className)}
      title={title}
    >
      <span className="inline-flex items-center gap-[2px]">
        {[0, 1, 2].map(i => (
          <span
            key={i}
            className={cn(
              'h-1.5 w-1.5 rounded-full transition-colors',
              i < filled ? color : 'bg-border/40',
            )}
          />
        ))}
      </span>
      {showLabel && level && (
        <span className={cn('text-[10px] font-semibold uppercase tracking-widest', labelColor)}>
          {level}
        </span>
      )}
    </span>
  );
}
