import type { NextAuthConfig } from 'next-auth';

export const authConfig: NextAuthConfig = {
  pages: { signIn: '/login' },
  session: { strategy: 'jwt', maxAge: 30 * 24 * 60 * 60 },
  callbacks: {
    async jwt({ token, user }) {
      // 'user' is only present on the first call (right after authorize()).
      // Persist the .NET bearer token + role flags into the next-auth JWT so
      // subsequent session() calls can hand them back to fetch interceptors / UI.
      if (user) {
        if (user.accessToken) token.accessToken = user.accessToken;
        if (user.accessTokenExpires) token.accessTokenExpires = user.accessTokenExpires;
        if (user.isAdmin !== undefined) token.isAdmin = user.isAdmin;
        if (user.isEmailVerified !== undefined) token.isEmailVerified = user.isEmailVerified;
      }
      return token;
    },
    async session({ session, token }) {
      if (token.accessToken) session.accessToken = token.accessToken;
      if (token.accessTokenExpires) session.accessTokenExpires = token.accessTokenExpires;
      if (session.user) {
        if (token.sub) session.user.id = token.sub;
        if (token.isAdmin !== undefined) session.user.isAdmin = token.isAdmin;
        if (token.isEmailVerified !== undefined) session.user.isEmailVerified = token.isEmailVerified;
      }
      return session;
    },
    authorized({ auth, request: { nextUrl } }) {
      const isLoggedIn = !!auth?.user;
      const path = nextUrl.pathname;
      const isPublicAuthPage = path.startsWith('/login') || path.startsWith('/signup') || path.startsWith('/verify-email');
      if (isPublicAuthPage) {
        // Verify-email is open regardless of auth state; login/signup redirect away when logged in.
        if (path.startsWith('/verify-email')) return true;
        return isLoggedIn ? Response.redirect(new URL('/dashboard', nextUrl)) : true;
      }
      return isLoggedIn;
    },
  },
  providers: [],
};
