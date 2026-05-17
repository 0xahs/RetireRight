'use client';

import Image from 'next/image';
import { useState } from 'react';
import { cn } from '@/lib/utils';

interface StockLogoProps {
  symbol: string;
  size?: number;
  className?: string;
}

export function StockLogo({ symbol, size = 32, className }: StockLogoProps) {
  const [failed, setFailed] = useState(false);

  if (failed) {
    return <StockInitial symbol={symbol} size={size} className={className} />;
  }

  return (
    <div
      className={cn('rounded-full overflow-hidden bg-white shrink-0 flex items-center justify-center', className)}
      style={{ width: size, height: size }}
    >
      <Image
        src={`/logos/${symbol}.png`}
        alt={symbol}
        width={size}
        height={size}
        className="object-contain"
        onError={() => setFailed(true)}
        unoptimized
      />
    </div>
  );
}

function StockInitial({ symbol, size, className }: { symbol: string; size: number; className?: string }) {
  return (
    <div
      className={cn('rounded-full bg-primary/20 flex items-center justify-center shrink-0', className)}
      style={{ width: size, height: size }}
    >
      <span
        className="font-bold text-primary"
        style={{ fontSize: Math.max(9, size * 0.34) }}
      >
        {symbol[0]}
      </span>
    </div>
  );
}
