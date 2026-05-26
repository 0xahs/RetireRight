import NextAuth from 'next-auth';
import Credentials from 'next-auth/providers/credentials';
import { authConfig } from './auth.config';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:59931';

interface LoginResponse {
  token: string;
  expiresAt: string;
  user: {
    id: number;
    email: string;
    isAdmin: boolean;
    emailVerified: boolean;
    createdAt: string;
    lastLoginAt: string | null;
  };
}

export const { handlers, auth, signIn, signOut } = NextAuth({
  ...authConfig,
  providers: [
    Credentials({
      credentials: {
        email: { label: 'Email', type: 'email' },
        password: { label: 'Password', type: 'password' },
      },
      async authorize(credentials) {
        if (!credentials?.email || !credentials?.password) return null;

        try {
          const res = await fetch(`${API_URL}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              email: credentials.email,
              password: credentials.password,
            }),
          });

          if (!res.ok) return null;

          const data = (await res.json()) as LoginResponse;
          // Stashed on the user object so the jwt() callback can copy it into the JWT.
          // The shape is typed via the next-auth module augmentation in src/types/next-auth.d.ts.
          return {
            id: String(data.user.id),
            email: data.user.email,
            name: data.user.email,
            accessToken: data.token,
            accessTokenExpires: data.expiresAt,
            isAdmin: data.user.isAdmin,
            isEmailVerified: data.user.emailVerified,
          };
        } catch {
          return null;
        }
      },
    }),
  ],
});
