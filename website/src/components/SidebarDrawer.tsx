"use client";

import { useEffect, useState, type ReactNode } from "react";
import { MobileDrawer } from "@/src/components/MobileDrawer";

export const SIDEBAR_OPEN_EVENT = "docs:open-sidebar";

/**
 * Where the rail takes over from the drawer. The classes are spelled out so
 * Tailwind generates them; `MobileDrawer` builds `<breakpoint>:hidden` itself,
 * so that class must also appear literally somewhere (e.g. the toolbar pill).
 */
const RAIL_CLASSES = {
  lg: "hidden lg:block",
  "min-[110rem]": "hidden min-[110rem]:block",
} as const;

type RailBreakpoint = keyof typeof RAIL_CLASSES;

export function SidebarDrawer({
  children,
  closeLabel = "Close documentation menu",
  railBreakpoint = "lg",
}: {
  children: ReactNode;
  closeLabel?: string;
  /** Width from which the content shows as a rail instead of a drawer. Defaults to `lg`. */
  railBreakpoint?: RailBreakpoint;
}) {
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const handler = () => setOpen(true);
    window.addEventListener(SIDEBAR_OPEN_EVENT, handler);
    return () => window.removeEventListener(SIDEBAR_OPEN_EVENT, handler);
  }, []);

  // Pin the rail below the header and let it shrink from the bottom exactly as
  // the footer scrolls into view, so the top (product selector + nav) stays
  // visible while the rail never overlaps the footer.
  useEffect(() => {
    const root = document.documentElement;
    let frame = 0;
    const update = () => {
      frame = 0;
      // Target the site footer only. Doc pages also render a small "last
      // updated" <footer> inside the article, which would otherwise shrink the
      // rail prematurely.
      const footer = document.querySelector("body > footer");
      const footerTop = footer
        ? footer.getBoundingClientRect().top
        : Number.POSITIVE_INFINITY;
      const intrusion = Math.max(0, window.innerHeight - footerTop);
      root.style.setProperty("--docs-rail-bottom", `${intrusion}px`);
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
    };
  }, []);

  return (
    <>
      <MobileDrawer
        side="left"
        breakpoint={railBreakpoint}
        open={open}
        onOpenChange={setOpen}
        closeOnPathnameChange
        closeOnContentClickSelector="a"
        closeLabel={closeLabel}
      >
        {children}
      </MobileDrawer>

      <aside
        className={`cc-content-dark border-cc-card-border border-r ${RAIL_CLASSES[railBreakpoint]}`}
      >
        <div className="sticky top-18 flex max-h-[calc(100vh-4.5rem-var(--docs-rail-bottom,0px))] flex-col">
          {children}
        </div>
      </aside>
    </>
  );
}
