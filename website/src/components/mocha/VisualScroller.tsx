"use client";

import { type ReactNode, useEffect, useRef, useState } from "react";

import { ArrowRightIcon } from "@/src/icons/ArrowRight";

interface VisualScrollerProps {
  readonly children: ReactNode;
}

/**
 * Lets a diagram that is wider than the column scroll sideways, so its labels
 * keep their size on phones. While it overflows, an edge fade and a hint show
 * that there is more, and the scroller takes keyboard focus for arrow keys.
 */
export function VisualScroller({ children }: VisualScrollerProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [overflows, setOverflows] = useState(false);
  const [atEnd, setAtEnd] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) {
      return;
    }
    const update = () => {
      setOverflows(el.scrollWidth > el.clientWidth + 1);
      setAtEnd(el.scrollLeft + el.clientWidth >= el.scrollWidth - 1);
    };
    update();
    const ro = new ResizeObserver(update);
    ro.observe(el);
    if (el.firstElementChild) {
      ro.observe(el.firstElementChild);
    }
    el.addEventListener("scroll", update, { passive: true });
    return () => {
      ro.disconnect();
      el.removeEventListener("scroll", update);
    };
  }, []);

  return (
    <>
      <div className="relative">
        <div
          ref={ref}
          tabIndex={overflows ? 0 : undefined}
          role={overflows ? "region" : undefined}
          aria-label={overflows ? "Diagram, scrolls sideways" : undefined}
          className="focus-visible:ring-cc-accent/50 overflow-x-auto overscroll-x-contain rounded-2xl focus-visible:ring-2 focus-visible:outline-none"
        >
          {children}
        </div>
        {overflows && !atEnd ? (
          <div
            aria-hidden="true"
            className="from-cc-bg pointer-events-none absolute inset-y-0 right-0 w-12 rounded-r-2xl bg-gradient-to-l to-transparent"
          />
        ) : null}
      </div>
      {overflows ? (
        <p
          aria-hidden="true"
          className="text-cc-ink-dim mt-2 flex items-center justify-center gap-1.5 text-xs"
        >
          Scroll sideways to see the whole diagram
          <ArrowRightIcon className="size-3.5" />
        </p>
      ) : null}
    </>
  );
}
