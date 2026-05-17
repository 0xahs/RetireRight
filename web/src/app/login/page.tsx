'use client';

import { signIn } from 'next-auth/react';
import { useState, FormEvent } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense } from 'react';
import { BrandMark, BrandWordmark } from '@/components/brand/brand-mark';

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get('callbackUrl') ?? '/dashboard';
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError('');
    setLoading(true);

    const form = new FormData(e.currentTarget);
    const result = await signIn('credentials', {
      email: form.get('email'),
      password: form.get('password'),
      redirect: false,
    });

    setLoading(false);

    if (result?.error) {
      setError('INVALID CREDENTIALS · ACCESS DENIED');
    } else {
      router.push(callbackUrl);
      router.refresh();
    }
  }

  const inputClass = 'w-full num px-3 py-2 border border-border bg-card text-[12px] text-foreground placeholder:text-warm-dim focus:outline-none focus:border-primary/60 transition-colors uppercase tracking-[0.05em] font-bold';
  const labelClass = 'block text-[9px] font-bold uppercase tracking-[0.2em] text-warm-muted mb-1.5';

  return (
    <div className="min-h-screen flex items-center justify-center bg-background relative scanlines">
      {/* Ticker strip at top */}
      <div className="absolute top-0 left-0 right-0 h-7 flex items-center px-3 border-b border-border bg-background">
        <span className="text-amber text-[9px] font-bold tracking-[0.22em] uppercase">▌ PSX TERMINAL</span>
        <span className="ml-auto text-[9px] text-warm-dim tracking-[0.18em] uppercase font-bold num">
          SECURE · TLS-AUTH
        </span>
      </div>

      <div className="w-full max-w-sm relative">
        <div className="panel-strong relative bg-card">
          <span aria-hidden className="absolute top-0 left-0 right-0 h-[2px] bg-primary" />
          <div className="panel-strip px-4 py-1.5">
            <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ AUTH · SIGN IN</span>
          </div>
          <div className="p-6 space-y-5">
            {/* Brand hero */}
            <div className="flex items-center gap-3 pb-1">
              <BrandMark size={42} />
              <BrandWordmark size="lg" />
            </div>
            <p className="text-warm-muted text-[10px] uppercase tracking-[0.22em] font-bold border-t border-border pt-3">
              PSX DIVIDEND INTELLIGENCE
            </p>

            <form onSubmit={handleSubmit} className="space-y-3">
              <div>
                <label className={labelClass} htmlFor="email">EMAIL</label>
                <input
                  id="email"
                  name="email"
                  type="email"
                  required
                  autoComplete="email"
                  className={inputClass}
                  defaultValue={process.env.NEXT_PUBLIC_AUTH_EMAIL_HINT ?? ''}
                  placeholder="USER@EXAMPLE.COM"
                />
              </div>

              <div>
                <label className={labelClass} htmlFor="password">PASSWORD</label>
                <input
                  id="password"
                  name="password"
                  type="password"
                  required
                  autoComplete="current-password"
                  className={inputClass}
                  placeholder="••••••••"
                />
              </div>

              {error && (
                <p className="text-[9px] uppercase tracking-[0.18em] font-bold text-warn">✕ {error}</p>
              )}

              <button
                type="submit"
                disabled={loading}
                className="w-full text-[11px] uppercase tracking-[0.18em] font-bold py-2.5 border border-primary/60 bg-primary/10 text-amber hover:bg-primary/20 transition-colors cursor-pointer disabled:opacity-50"
              >
                {loading ? '◐ AUTHENTICATING…' : '[ ▶ SIGN IN ]'}
              </button>
            </form>
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

export default function LoginPage() {
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}
