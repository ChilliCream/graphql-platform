"use client";

import { useEffect, useState, type MouseEvent, type ReactNode } from "react";
import { usePathname } from "next/navigation";
import { IconButton } from "@/src/design-system/IconButton";

export const SIDEBAR_OPEN_EVENT = "docs:open-sidebar";

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

  useEffect(() => {
    const root = document.documentElement;
    const HEADER = 72;
    let frame = 0;
    const update = () => {
      frame = 0;
      const footer = document.querySelector("footer");
      const footerTop = footer
        ? footer.getBoundingClientRect().top
        : Number.POSITIVE_INFINITY;
      const intrusion = Math.max(0, window.innerHeight - footerTop);
      // Only extend once the footer is actually on screen; while reading the
      // rails stay shrunk to content (min-height 0).
      const min =
        footerTop < window.innerHeight ? Math.max(0, footerTop - HEADER) : 0;
      root.style.setProperty("--docs-rail-bottom", `${intrusion}px`);
      root.style.setProperty("--docs-rail-min", `${min}px`);
    };
    const schedule = () => {
      if (!frame) {
        frame = requestAnimationFrame(update);
      }
    };
    update();
    window.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("resize", schedule, { passive: true });
    return () => {
      window.removeEventListener("scroll", schedule);
      window.removeEventListener("resize", schedule);
      if (frame) {
        cancelAnimationFrame(frame);
      }
      root.style.removeProperty("--docs-rail-bottom");
      root.style.removeProperty("--docs-rail-min");
    };
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
          className={`absolute inset-0 bg-cc-black/40 transition-opacity ${
            open ? "opacity-100" : "opacity-0"
          }`}
          onClick={() => setOpen(false)}
        />
        <div
          className={`absolute inset-y-0 left-0 w-80 max-w-[85vw] overflow-y-auto bg-cc-bg shadow-xl transition-transform duration-200 ${
            open ? "translate-x-0" : "-translate-x-full"
          }`}
        >
          <div className="flex items-center justify-end border-b border-cc-card-border px-3 py-2">
            <IconButton
              aria-label="Close documentation menu"
              onClick={() => setOpen(false)}
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
            </IconButton>
          </div>
          <div onClick={handleContentClick}>{children}</div>
        </div>
      </div>

      <aside className="hidden lg:block" aria-hidden="true" />

      <div className="cc-content-dark fixed left-0 top-18 z-30 -mt-px hidden max-h-[calc(100vh-4.5rem-var(--docs-rail-bottom,0px))] min-h-[var(--docs-rail-min,0px)] w-80 flex-col border-r border-cc-card-border lg:flex">
        {children}
      </div>
    </>
  );
}
