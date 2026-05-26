'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import { signOut } from 'next-auth/react';
import { api } from '@/lib/api/client';
import { cn } from '@/lib/utils';

interface ChangePasswordResult {
  ok: boolean;
}

export default function SecuritySettingsPage() {
  const router = useRouter();
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [status, setStatus] = useState<'idle' | 'submitting' | 'success' | 'error'>('idle');
  const [message, setMessage] = useState('');

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setMessage('');

    if (next.length < 8) {
      setStatus('error');
      setMessage('NEW PASSWORD MUST BE AT LEAST 8 CHARACTERS');
      return;
    }
    if (next !== confirm) {
      setStatus('error');
      setMessage('NEW PASSWORD AND CONFIRMATION DO NOT MATCH');
      return;
    }
    if (next === current) {
      setStatus('error');
      setMessage('NEW PASSWORD MUST DIFFER FROM CURRENT');
      return;
    }

    setStatus('submitting');
    try {
      await api.post<ChangePasswordResult>('/api/auth/change-password', {
        currentPassword: current,
        newPassword: next,
      });
      setStatus('success');
      setMessage('PASSWORD UPDATED · SIGNING OUT IN 3S');
      setCurrent(''); setNext(''); setConfirm('');
      // Force re-login with new password so the JWT issued for the old hash isn't reused.
      setTimeout(() => signOut({ callbackUrl: '/login' }), 3000);
    } catch (err) {
      setStatus('error');
      const m = err instanceof Error ? err.message : 'UNKNOWN ERROR';
      // Server returns 400 when the current password is wrong (so the global
      // 401-interceptor in client.ts doesn't sign the user out mid-form).
      if (m.includes('400') && m.toLowerCase().includes('current password')) {
        setMessage('CURRENT PASSWORD IS INCORRECT');
      } else {
        setMessage(m.toUpperCase());
      }
    }
  }

  const inputClass = 'w-full num px-3 py-2 border border-border bg-card text-[12px] text-foreground placeholder:text-warm-dim focus:outline-none focus:border-primary/60 transition-colors tracking-[0.05em] font-bold';
  const labelClass = 'block text-[9px] font-bold uppercase tracking-[0.2em] text-warm-muted mb-1.5';

  return (
    <div className="space-y-4 max-w-xl">
      <div className="flex items-baseline justify-between gap-3 flex-wrap">
        <div>
          <div className="text-[9px] text-amber font-bold tracking-[0.22em] uppercase mb-1">
            ┌── SETTINGS · SECURITY
          </div>
          <h1 className="text-foreground font-bold text-[20px] tracking-tight">
            CHANGE PASSWORD
          </h1>
          <p className="text-warm-muted text-[11px] mt-1 uppercase tracking-[0.05em]">
            REPLACES THE STORED BCRYPT HASH · ACTIVE SESSION ENDS ON SUCCESS
          </p>
        </div>
        <button
          onClick={() => router.back()}
          className="text-[10px] uppercase tracking-[0.18em] font-bold px-3 py-1.5 border border-border bg-card text-warm-muted hover:text-amber hover:border-primary/50 transition-colors cursor-pointer self-center"
        >
          ← BACK
        </button>
      </div>

      <div className="panel">
        <div className="panel-strip px-4 py-1.5">
          <span className="text-amber font-bold text-[10px] tracking-[0.18em] uppercase">▌ NEW CREDENTIALS</span>
        </div>
        <form onSubmit={handleSubmit} className="p-4 space-y-4">
          <div>
            <label className={labelClass} htmlFor="current">CURRENT PASSWORD</label>
            <input
              id="current"
              type="password"
              required
              autoComplete="current-password"
              value={current}
              onChange={(e) => setCurrent(e.target.value)}
              className={inputClass}
              placeholder="••••••••"
              disabled={status === 'submitting'}
            />
          </div>

          <div>
            <label className={labelClass} htmlFor="next">NEW PASSWORD · MIN 8 CHARS</label>
            <input
              id="next"
              type="password"
              required
              autoComplete="new-password"
              value={next}
              onChange={(e) => setNext(e.target.value)}
              className={inputClass}
              placeholder="••••••••"
              disabled={status === 'submitting'}
            />
          </div>

          <div>
            <label className={labelClass} htmlFor="confirm">CONFIRM NEW PASSWORD</label>
            <input
              id="confirm"
              type="password"
              required
              autoComplete="new-password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              className={inputClass}
              placeholder="••••••••"
              disabled={status === 'submitting'}
            />
          </div>

          {message && (
            <p
              className={cn(
                'text-[10px] uppercase tracking-[0.18em] font-bold',
                status === 'success' ? 'text-cyan-pos' : 'text-warn',
              )}
            >
              {status === 'success' ? '✓' : '✕'} {message}
            </p>
          )}

          <button
            type="submit"
            disabled={status === 'submitting' || status === 'success'}
            className="w-full text-[11px] uppercase tracking-[0.18em] font-bold py-2.5 border border-primary/60 bg-primary/10 text-amber hover:bg-primary/20 transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {status === 'submitting' ? '◐ UPDATING…' : status === 'success' ? '✓ UPDATED' : '[ ▶ UPDATE PASSWORD ]'}
          </button>
        </form>
      </div>

      <div className="panel px-4 py-3 border-l-[3px] border-l-amber">
        <p className="text-[10px] uppercase tracking-[0.15em] font-bold text-warm-muted">▌ NOTE</p>
        <p className="text-[11px] text-warm-muted mt-1.5 leading-relaxed">
          The password is hashed with bcrypt (work factor 12) before storage. After a
          successful change you&apos;ll be signed out so the JWT issued for the previous
          hash can&apos;t be reused. Sign in again with the new password.
        </p>
      </div>
    </div>
  );
}
