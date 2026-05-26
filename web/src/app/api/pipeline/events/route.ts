import { auth } from '@/auth';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:59931';

// SSE proxy: EventSource can't attach Authorization headers, so the browser hits this
// same-origin route, we read the NextAuth session server-side, and forward the upstream
// stream to the .NET API with a bearer token. Long-lived; never cached.
export const dynamic = 'force-dynamic';
export const runtime = 'nodejs';

export async function GET(req: Request) {
  const session = await auth();
  const token = session?.accessToken;
  if (!token) {
    return new Response('Unauthorized', { status: 401 });
  }

  // Forward Last-Event-ID so the upstream SSE endpoint can resume from where the
  // client dropped off — EventSource sets this header automatically on reconnect.
  const headers: Record<string, string> = { Authorization: `Bearer ${token}` };
  const lastEventId = req.headers.get('last-event-id');
  if (lastEventId) headers['Last-Event-ID'] = lastEventId;

  const upstream = await fetch(`${API_URL}/api/pipeline/events`, {
    headers,
    signal: req.signal,
    cache: 'no-store',
  });

  if (!upstream.ok || !upstream.body) {
    return new Response('Upstream error', { status: upstream.status });
  }

  return new Response(upstream.body, {
    status: 200,
    headers: {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache, no-transform',
      'Connection': 'keep-alive',
      'X-Accel-Buffering': 'no',
    },
  });
}
