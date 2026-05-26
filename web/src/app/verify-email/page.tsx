'use client';

import { Suspense, useEffect, useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { BrandMark, BrandWordmark } from '@/components/brand/brand-mark';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:59931';

type VerifyState = 'idle' | 'verifying' | 'success' | 'error';

function VerifyEmail() {
  const params = useSearchParams();
  const token = params.get('token');
  const [state, setState] = useState<VerifyState>('idle');
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!token) {
      setState('error');
      setMessage('NO TOKEN PROVIDED · USE THE LINK FROM YOUR EMAIL');
      return;
    }
    let cancelled = false;
    (async () => {
      setState('verifying');
      try {
        const res = await fetch(`${API_URL}/api/auth/verify-email`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ token }),
        });
        if (cancelled) return;
        if (res.ok) {
          setState('success');
          setMessage('EMAIL VERIFIED · YOU CAN SIGN IN NOW');
        } else {
          const err = await res.json().catch(() => ({ error: 'Verification failed.' }));
          setState('error');
          setMessage((err.error ?? 'VERIFICATION FAILED').toUpperCase());
        }
      } catch {
        if (cancelled) return;
        setState('error');
        setMessage('NETWORK ERROR · IS THE API RUNNING?');
      }
    })();
    return () => { cancelled = true; };
  }, [token]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-background relative scanlines">
      <div className="absolute top-0 left-0 right-0 h-7 flex items-center px-3 border-b border-border bg-background">
        <span className="text-amber text-[9px] font-bold tracking-[0.22em] uppercase">▌ PSX TERMINAL</span>
      </div>

      <div className="w-full max-w-sm relative">
        <div className="panel-strong relative bg-card">
          <span aria-hidden className="absolute top-0 left-0 right-0 h-[2px] bg-primary" />
          <div className="panel-strip px-4 py-1.5">
            <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ AUTH · VERIFY EMAIL</span>
          </div>
          <div className="p-6 space-y-5">
            <div className="flex items-center gap-3 pb-1">
              <BrandMark size={42} />
              <BrandWordmark size="lg" />
            </div>

            <div className="border-t border-border pt-4 min-h-[80px] flex items-center justify-center">
              {state === 'verifying' && (
                <p className="text-[11px] uppercase tracking-[0.18em] font-bold text-amber animate-pulse">
                  ◐ VERIFYING…
                </p>
              )}
              {state === 'success' && (
                <p className="text-[11px] uppercase tracking-[0.18em] font-bold text-cyan-pos">
                  ✓ {message}
                </p>
              )}
              {state === 'error' && (
                <p className="text-[11px] uppercase tracking-[0.18em] font-bold text-warn text-center leading-relaxed">
                  ✕ {message}
                </p>
              )}
            </div>

            <Link
              href="/login"
              className="block text-center w-full text-[11px] uppercase tracking-[0.18em] font-bold py-2.5 border border-primary/60 bg-primary/10 text-amber hover:bg-primary/20 transition-colors cursor-pointer"
            >
              [ ▶ GO TO SIGN IN ]
            </Link>
          </div>
          <div className="border-t border-border px-4 py-2 flex items-center justify-between text-[9px] uppercase tracking-[0.15em] font-bold text-warm-dim">
            <span>v0.3</span>
            <span>KSE-100 · PSX</span>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function VerifyEmailPage() {
  return (
    <Suspense>
      <VerifyEmail />
    </Suspense>
  );
}
