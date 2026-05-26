'use client';

import { FormEvent, useState, Suspense } from 'react';
import Link from 'next/link';
import { BrandMark, BrandWordmark } from '@/components/brand/brand-mark';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:59931';

interface SignupSuccessResponse {
  ok: true;
  message: string;
}

interface SignupErrorResponse {
  error: string;
}

function SignupForm() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [status, setStatus] = useState<'idle' | 'submitting' | 'success' | 'error'>('idle');
  const [message, setMessage] = useState('');

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setMessage('');

    if (password.length < 8) {
      setStatus('error');
      setMessage('PASSWORD MUST BE AT LEAST 8 CHARACTERS');
      return;
    }
    if (password !== confirm) {
      setStatus('error');
      setMessage('PASSWORDS DO NOT MATCH');
      return;
    }

    setStatus('submitting');
    try {
      const res = await fetch(`${API_URL}/api/auth/signup`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email.trim(), password }),
      });

      if (res.status === 429) {
        setStatus('error');
        setMessage('TOO MANY ATTEMPTS · TRY AGAIN IN AN HOUR');
        return;
      }
      if (res.status === 403) {
        setStatus('error');
        setMessage('PUBLIC SIGN-UP IS DISABLED');
        return;
      }
      if (!res.ok) {
        const err = (await res.json().catch(() => ({}))) as SignupErrorResponse;
        setStatus('error');
        setMessage((err.error ?? 'SIGN-UP FAILED').toUpperCase());
        return;
      }

      const data = (await res.json()) as SignupSuccessResponse;
      setStatus('success');
      setMessage(data.message.toUpperCase());
      setPassword(''); setConfirm('');
    } catch {
      setStatus('error');
      setMessage('NETWORK ERROR · IS THE API RUNNING?');
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
            <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ AUTH · SIGN UP</span>
          </div>
          <div className="p-6 space-y-5">
            <div className="flex items-center gap-3 pb-1">
              <BrandMark size={42} />
              <BrandWordmark size="lg" />
            </div>
            <p className="text-warm-muted text-[10px] uppercase tracking-[0.22em] font-bold border-t border-border pt-3">
              CREATE ACCOUNT
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
                  disabled={status === 'submitting' || status === 'success'}
                />
              </div>

              <div>
                <label className={labelClass} htmlFor="password">PASSWORD · MIN 8</label>
                <input
                  id="password"
                  type="password"
                  required
                  autoComplete="new-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className={inputClass}
                  placeholder="••••••••"
                  disabled={status === 'submitting' || status === 'success'}
                />
              </div>

              <div>
                <label className={labelClass} htmlFor="confirm">CONFIRM PASSWORD</label>
                <input
                  id="confirm"
                  type="password"
                  required
                  autoComplete="new-password"
                  value={confirm}
                  onChange={(e) => setConfirm(e.target.value)}
                  className={inputClass}
                  placeholder="••••••••"
                  disabled={status === 'submitting' || status === 'success'}
                />
              </div>

              {message && (
                <p className={`text-[9px] uppercase tracking-[0.18em] font-bold ${status === 'success' ? 'text-cyan-pos' : 'text-warn'}`}>
                  {status === 'success' ? '✓' : '✕'} {message}
                </p>
              )}

              <button
                type="submit"
                disabled={status === 'submitting' || status === 'success'}
                className="w-full text-[11px] uppercase tracking-[0.18em] font-bold py-2.5 border border-primary/60 bg-primary/10 text-amber hover:bg-primary/20 transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {status === 'submitting' ? '◐ CREATING…' : status === 'success' ? '✓ EMAIL SENT' : '[ ▶ CREATE ACCOUNT ]'}
              </button>
            </form>

            <p className="text-[10px] text-warm-muted uppercase tracking-[0.15em] font-bold text-center pt-2 border-t border-border">
              ALREADY HAVE ACCOUNT? <Link href="/login" className="text-amber hover:underline">SIGN IN</Link>
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

export default function SignupPage() {
  return (
    <Suspense>
      <SignupForm />
    </Suspense>
  );
}
