import type { NextAuthConfig } from 'next-auth';

export const authConfig: NextAuthConfig = {
  pages: { signIn: '/login' },
  session: { strategy: 'jwt', maxAge: 30 * 24 * 60 * 60 },
  callbacks: {
    authorized({ auth, request: { nextUrl } }) {
      const isLoggedIn = !!auth?.user;
      const isLoginPage = nextUrl.pathname.startsWith('/login');
      if (isLoginPage) return isLoggedIn ? Response.redirect(new URL('/dashboard', nextUrl)) : true;
      return isLoggedIn;
    },
  },
  providers: [],
};
