'use client';

import { signIn } from 'next-auth/react';
import { useState, FormEvent, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { BrandMark, BrandWordmark } from '@/components/brand/brand-mark';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:59931';

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get('callbackUrl') ?? '/dashboard';
  const [email, setEmail] = useState(process.env.NEXT_PUBLIC_AUTH_EMAIL_HINT ?? '');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [resendStatus, setResendStatus] = useState<'idle' | 'sending' | 'sent' | 'failed'>('idle');

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError('');
    setResendStatus('idle');
    setLoading(true);

    const result = await signIn('credentials', {
      email,
      password,
      redirect: false,
    });

    setLoading(false);

    if (result?.error) {
      // NextAuth collapses 401 / 403 (email-not-verified) / 423 (locked) / 429 (rate-limited)
      // into a single CredentialsSignin error. Generic message + resend link covers the
      // most actionable case (unverified email) without leaking which path failed.
      setError('INVALID CREDENTIALS · ACCOUNT LOCKED · OR EMAIL UNVERIFIED');
    } else {
      router.push(callbackUrl);
      router.refresh();
    }
  }

  async function handleResend() {
    if (!email || !password) {
      setResendStatus('failed');
      return;
    }
    setResendStatus('sending');
    try {
      const res = await fetch(`${API_URL}/api/auth/resend-verification`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      });
      // Endpoint always returns 200 with a generic message — we don't leak whether
      // the credentials were valid. Still surface the click feedback.
      setResendStatus(res.ok ? 'sent' : 'failed');
    } catch {
      setResendStatus('failed');
    }
  }

  const inputClass = 'w-full num px-3 py-2 border border-border bg-card text-[12px] text-foreground placeholder:text-warm-dim focus:outline-none focus:border-primary/60 transition-colors uppercase tracking-[0.05em] font-bold';
  const labelClass = 'block text-[9px] font-bold uppercase tracking-[0.2em] text-warm-muted mb-1.5';

  return (
    <div className="min-h-screen flex items-center justify-center bg-background relative scanlines">
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
                  type="email"
                  required
                  autoComplete="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className={inputClass}
                  placeholder="USER@EXAMPLE.COM"
                />
              </div>

              <div>
                <label className={labelClass} htmlFor="password">PASSWORD</label>
                <input
                  id="password"
                  type="password"
                  required
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className={inputClass}
                  placeholder="••••••••"
                />
              </div>

              {error && (
                <div className="space-y-2">
                  <p className="text-[9px] uppercase tracking-[0.18em] font-bold text-warn">✕ {error}</p>
                  <button
                    type="button"
                    onClick={handleResend}
                    disabled={resendStatus === 'sending' || resendStatus === 'sent'}
                    className="text-[9px] uppercase tracking-[0.18em] font-bold text-amber hover:underline cursor-pointer disabled:opacity-50"
                  >
                    {resendStatus === 'sending' && '◐ SENDING…'}
                    {resendStatus === 'sent' && '✓ IF UNVERIFIED · CHECK YOUR INBOX'}
                    {resendStatus === 'failed' && '✕ RESEND FAILED · TRY LATER'}
                    {resendStatus === 'idle' && '▸ RESEND VERIFICATION EMAIL'}
                  </button>
                </div>
              )}

              <button
                type="submit"
                disabled={loading}
                className="w-full text-[11px] uppercase tracking-[0.18em] font-bold py-2.5 border border-primary/60 bg-primary/10 text-amber hover:bg-primary/20 transition-colors cursor-pointer disabled:opacity-50"
              >
                {loading ? '◐ AUTHENTICATING…' : '[ ▶ SIGN IN ]'}
              </button>
            </form>

            <p className="text-[10px] text-warm-muted uppercase tracking-[0.15em] font-bold text-center pt-2 border-t border-border">
              NEW HERE? <Link href="/signup" className="text-amber hover:underline">CREATE ACCOUNT</Link>
            </p>
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
