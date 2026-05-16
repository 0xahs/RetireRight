import NextAuth from 'next-auth';
import Credentials from 'next-auth/providers/credentials';
import { authConfig } from './auth.config';

export const { handlers, auth, signIn, signOut } = NextAuth({
  ...authConfig,
  providers: [
    Credentials({
      credentials: {
        email: { label: 'Email', type: 'email' },
        password: { label: 'Password', type: 'password' },
      },
      async authorize(credentials) {
        if (
          credentials?.email === process.env.AUTH_EMAIL &&
          credentials?.password === process.env.AUTH_PASSWORD
        ) {
          return { id: '1', email: credentials.email as string, name: 'Ahmad' };
        }
        return null;
      },
    }),
  ],
});
