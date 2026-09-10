"use client";

import { useEffect, useState, type ReactNode } from "react";
import { MobileDrawer } from "@/src/components/MobileDrawer";

export const SIDEBAR_OPEN_EVENT = "docs:open-sidebar";

export function SidebarDrawer({
  children,
  closeLabel = "Close documentation menu",
}: {
  children: ReactNode;
  closeLabel?: string;
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
        breakpoint="lg"
        open={open}
        onOpenChange={setOpen}
        closeOnPathnameChange
        closeOnContentClickSelector="a"
        closeLabel={closeLabel}
      >
        {children}
      </MobileDrawer>

      <aside className="cc-content-dark border-cc-card-border hidden border-r lg:block">
        <div className="sticky top-18 flex max-h-[calc(100vh-4.5rem-var(--docs-rail-bottom,0px))] flex-col">
          {children}
        </div>
      </aside>
    </>
  );
}
