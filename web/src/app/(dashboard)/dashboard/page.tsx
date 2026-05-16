import { Suspense } from 'react';
import { CommandCenterContent } from '@/components/command-center';

export default function CommandCenterPage() {
  return (
    <Suspense fallback={<div className="text-muted-foreground text-sm">Loading...</div>}>
      <CommandCenterContent />
    </Suspense>
  );
}
