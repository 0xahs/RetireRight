import { Sidebar } from '@/components/layout/sidebar';
import { Topbar } from '@/components/layout/topbar';
import { CompareBasketProvider } from '@/components/compare/compare-basket-context';
import { CompareStickyBar } from '@/components/compare/compare-sticky-bar';

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <CompareBasketProvider>
      <div className="flex h-screen overflow-hidden bg-background">
        <Sidebar />
        <div className="flex flex-col flex-1 min-w-0 overflow-hidden">
          <Topbar />
          <main className="flex-1 overflow-y-auto p-6 space-y-0">{children}</main>
        </div>
        <CompareStickyBar />
      </div>
    </CompareBasketProvider>
  );
}
