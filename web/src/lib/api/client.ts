import { getSession, signOut } from 'next-auth/react';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:59931';

// Endpoints that legitimately return 401 for reasons other than "session expired"
// (e.g. wrong current password on change-password) — the global signOut interceptor
// is skipped here so the caller can show a field-level error instead.
const SKIP_AUTH_INTERCEPTOR_PATHS = new Set<string>([
  '/api/auth/change-password',
]);

async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const session = await getSession();
  const token = session?.accessToken;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...((init?.headers as Record<string, string>) ?? {}),
  };
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(`${API_URL}${path}`, { ...init, headers });

  if (res.status === 401 && !SKIP_AUTH_INTERCEPTOR_PATHS.has(path)) {
    // Bearer rejected — kick the user back to login so they can re-auth.
    await signOut({ callbackUrl: '/login' });
    throw new Error('API 401: session expired');
  }

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`API ${res.status}: ${text}`);
  }
  if (res.status === 204 || res.headers.get('Content-Length') === '0') {
    return undefined as T;
  }
  return res.json() as Promise<T>;
}

export const api = {
  get: <T>(path: string) => apiFetch<T>(path),
  post: <T>(path: string, body: unknown) =>
    apiFetch<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    apiFetch<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  patch: <T>(path: string, body: unknown) =>
    apiFetch<T>(path, { method: 'PATCH', body: JSON.stringify(body) }),
  delete: <T>(path: string) => apiFetch<T>(path, { method: 'DELETE' }),
};
