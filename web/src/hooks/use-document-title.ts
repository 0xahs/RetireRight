'use client';

import { useEffect } from 'react';

/**
 * Sets document.title for client components. Format: "{title} · RetireRight".
 * Falls back to the layout default when title is null/empty.
 */
export function useDocumentTitle(title: string | null | undefined) {
  useEffect(() => {
    if (!title) {
      document.title = 'RetireRight · PSX Terminal';
      return;
    }
    document.title = `${title} · RetireRight`;
  }, [title]);
}
