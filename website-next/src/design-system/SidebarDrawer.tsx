"use client";

import { useEffect, useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";

/**
 * Wraps the sidebar to provide:
 * - Desktop (lg+): visible inline, sticky.
 * - Mobile (<lg): hidden behind an off-canvas drawer with a hamburger trigger.
 *   Body scroll is locked while the drawer is open. The drawer auto-closes
 *   on route change.
 */
export function SidebarDrawer({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const pathname = usePathname();

  useEffect(() => {
    setOpen(false);
  }, [pathname]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previous;
    };
  }, [open]);

  return (
    <>
      {/* Mobile trigger — hidden on lg+ */}
      <button
        type="button"
        aria-label="Open documentation menu"
        aria-expanded={open}
        onClick={() => setOpen(true)}
        className="fixed bottom-4 left-4 z-30 inline-flex h-12 w-12 items-center justify-center rounded-full bg-stone-900 text-white shadow-lg lg:hidden"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="h-6 w-6"
          aria-hidden="true"
        >
          <line x1="4" y1="6" x2="20" y2="6" />
          <line x1="4" y1="12" x2="20" y2="12" />
          <line x1="4" y1="18" x2="20" y2="18" />
        </svg>
      </button>

      {/* Mobile overlay + drawer */}
      <div
        className={`fixed inset-0 z-40 lg:hidden ${open ? "" : "pointer-events-none"}`}
        aria-hidden={!open}
      >
        <div
          className={`absolute inset-0 bg-black/40 transition-opacity ${
            open ? "opacity-100" : "opacity-0"
          }`}
          onClick={() => setOpen(false)}
        />
        <div
          className={`absolute inset-y-0 left-0 w-[20rem] max-w-[85vw] overflow-y-auto bg-white shadow-xl transition-transform duration-200 ${
            open ? "translate-x-0" : "-translate-x-full"
          }`}
        >
          <div className="flex items-center justify-end px-3 py-2 border-b border-stone-200">
            <button
              type="button"
              aria-label="Close documentation menu"
              onClick={() => setOpen(false)}
              className="inline-flex h-9 w-9 items-center justify-center rounded-full text-stone-700 hover:bg-stone-100"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                className="h-5 w-5"
                aria-hidden="true"
              >
                <line x1="6" y1="6" x2="18" y2="18" />
                <line x1="6" y1="18" x2="18" y2="6" />
              </svg>
            </button>
          </div>
          {children}
        </div>
      </div>

      {/* Desktop sidebar — sticky inline */}
      <aside className="hidden lg:block sticky top-0 self-start max-h-[calc(100vh-4rem)] overflow-y-auto border-r border-stone-200">
        {children}
      </aside>
    </>
  );
}
