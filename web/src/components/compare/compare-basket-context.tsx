'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';

export const COMPARE_MAX = 4;
const STORAGE_KEY = 'rr.compare.basket';

interface BasketContext {
  symbols: string[];
  add: (symbol: string) => void;
  remove: (symbol: string) => void;
  toggle: (symbol: string) => void;
  clear: () => void;
  has: (symbol: string) => boolean;
  isFull: boolean;
}

const Ctx = createContext<BasketContext | null>(null);

/**
 * Compare basket persists to localStorage so it survives page reloads and is
 * shared across tabs in real time via the storage event.
 */
export function CompareBasketProvider({ children }: { children: React.ReactNode }) {
  const [symbols, setSymbols] = useState<string[]>([]);

  // Hydrate from localStorage on mount
  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw);
        if (Array.isArray(parsed)) {
          setSymbols(parsed.filter((s): s is string => typeof s === 'string').slice(0, COMPARE_MAX));
        }
      }
    } catch {}

    const onStorage = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY && e.newValue) {
        try {
          const parsed = JSON.parse(e.newValue);
          if (Array.isArray(parsed)) {
            setSymbols(parsed.filter((s): s is string => typeof s === 'string').slice(0, COMPARE_MAX));
          }
        } catch {}
      }
    };
    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  }, []);

  const persist = useCallback((next: string[]) => {
    setSymbols(next);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    } catch {}
  }, []);

  const add = useCallback((symbol: string) => {
    const sym = symbol.toUpperCase();
    setSymbols(prev => {
      if (prev.includes(sym) || prev.length >= COMPARE_MAX) return prev;
      const next = [...prev, sym];
      try { localStorage.setItem(STORAGE_KEY, JSON.stringify(next)); } catch {}
      return next;
    });
  }, []);

  const remove = useCallback((symbol: string) => {
    const sym = symbol.toUpperCase();
    setSymbols(prev => {
      const next = prev.filter(s => s !== sym);
      try { localStorage.setItem(STORAGE_KEY, JSON.stringify(next)); } catch {}
      return next;
    });
  }, []);

  const toggle = useCallback((symbol: string) => {
    const sym = symbol.toUpperCase();
    setSymbols(prev => {
      const next = prev.includes(sym)
        ? prev.filter(s => s !== sym)
        : prev.length >= COMPARE_MAX ? prev : [...prev, sym];
      try { localStorage.setItem(STORAGE_KEY, JSON.stringify(next)); } catch {}
      return next;
    });
  }, []);

  const clear = useCallback(() => persist([]), [persist]);

  const value = useMemo<BasketContext>(() => ({
    symbols,
    add,
    remove,
    toggle,
    clear,
    has: (s: string) => symbols.includes(s.toUpperCase()),
    isFull: symbols.length >= COMPARE_MAX,
  }), [symbols, add, remove, toggle, clear]);

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useCompareBasket(): BasketContext {
  const v = useContext(Ctx);
  if (!v) throw new Error('useCompareBasket must be used within CompareBasketProvider');
  return v;
}
