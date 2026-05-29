"use client";

import { useEffect, useState, type MouseEvent, type ReactNode } from "react";
import { usePathname } from "next/navigation";

export const SIDEBAR_OPEN_EVENT = "docs:open-sidebar";

/**
 * Wraps the sidebar to provide:
 * - Desktop (lg+): visible inline, sticky.
 * - Mobile (<lg): hidden behind an off-canvas drawer opened via the
 *   `docs:open-sidebar` window event (dispatched by DocsToolbar).
 *   Body scroll is locked while the drawer is open. The drawer auto-closes
 *   on route change.
 */
export function SidebarDrawer({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const pathname = usePathname();
  const [prevPathname, setPrevPathname] = useState(pathname);
  if (prevPathname !== pathname) {
    setPrevPathname(pathname);
    if (open) {
      setOpen(false);
    }
  }

  useEffect(() => {
    const handler = () => setOpen(true);
    window.addEventListener(SIDEBAR_OPEN_EVENT, handler);
    return () => window.removeEventListener(SIDEBAR_OPEN_EVENT, handler);
  }, []);

  function handleContentClick(event: MouseEvent<HTMLDivElement>) {
    const target = event.target as HTMLElement;
    if (target.closest("a")) {
      setOpen(false);
    }
  }

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
          className={`absolute inset-y-0 left-0 w-[20rem] max-w-[85vw] overflow-y-auto bg-[#0c1322] shadow-xl transition-transform duration-200 ${
            open ? "translate-x-0" : "-translate-x-full"
          }`}
        >
          <div className="flex items-center justify-end px-3 py-2 border-b border-white/10">
            <button
              type="button"
              aria-label="Close documentation menu"
              onClick={() => setOpen(false)}
              className="inline-flex h-9 w-9 items-center justify-center rounded-full text-slate-300 hover:bg-white/5"
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
          <div onClick={handleContentClick}>{children}</div>
        </div>
      </div>

      {/* Desktop sidebar — sticky inline so it scrolls with the content and the
          footer sits cleanly below it. */}
      <aside className="hidden lg:block sticky top-[72px] self-start h-[calc(100vh-72px)] overflow-y-auto border-r border-white/10">
        {children}
      </aside>
    </>
  );
}
