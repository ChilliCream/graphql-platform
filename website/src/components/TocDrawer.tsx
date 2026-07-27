"use client";

import { useEffect, useState, type ReactNode } from "react";
import { MobileDrawer } from "@/src/components/MobileDrawer";

export const TOC_OPEN_EVENT = "docs:open-toc";

export function TocDrawer({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const handler = () => setOpen(true);
    window.addEventListener(TOC_OPEN_EVENT, handler);
    return () => window.removeEventListener(TOC_OPEN_EVENT, handler);
  }, []);

  return (
    <MobileDrawer
      side="right"
      breakpoint="2xl"
      open={open}
      onOpenChange={setOpen}
      closeOnPathnameChange
      closeOnContentClickSelector="a[href^='#']"
      panelClassName="flex flex-col"
      closeLabel="Close table of contents"
    >
      <div className="px-5 py-4">{children}</div>
    </MobileDrawer>
  );
}
