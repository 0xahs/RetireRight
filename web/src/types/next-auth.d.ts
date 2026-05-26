// Module augmentation so the bearer token issued by the .NET API is typed
// through NextAuth's Session and JWT, killing the `as unknown as { ... }`
// casts that would otherwise spread across every consumer.

import 'next-auth';
import 'next-auth/jwt';

declare module 'next-auth' {
  interface Session {
    accessToken?: string;
    accessTokenExpires?: string;
    user?: {
      id?: string;
      email?: string | null;
      name?: string | null;
      image?: string | null;
      isAdmin?: boolean;
      isEmailVerified?: boolean;
    };
  }

  // Note: `emailVerified: Date | null` is reserved by NextAuth's AdapterUser; we use
  // `isEmailVerified` to avoid the type clash and keep our flag a plain boolean.
  interface User {
    accessToken?: string;
    accessTokenExpires?: string;
    isAdmin?: boolean;
    isEmailVerified?: boolean;
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    accessToken?: string;
    accessTokenExpires?: string;
    isAdmin?: boolean;
    isEmailVerified?: boolean;
  }
}
