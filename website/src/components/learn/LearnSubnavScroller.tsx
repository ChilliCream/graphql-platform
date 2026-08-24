"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";

interface LearnSubnavScrollerProps {
  readonly children: ReactNode;
}

const EDGE_FADE = 24;

/**
 * Horizontally scrollable subnav link track. Fades only the side that
 * currently has more content to scroll toward, tracked from `scrollLeft`
 * and `scrollWidth` on scroll and on resize.
 */
export function LearnSubnavScroller({ children }: LearnSubnavScrollerProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

  const measure = () => {
    const el = ref.current;
    if (!el) {
      return;
    }
    setCanScrollLeft(el.scrollLeft > 0);
    setCanScrollRight(el.scrollLeft + el.clientWidth < el.scrollWidth - 1);
  };

  useEffect(() => {
    measure();
    const el = ref.current;
    if (!el) {
      return;
    }
    const observer = new ResizeObserver(measure);
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const maskImage = canScrollLeft
    ? canScrollRight
      ? `linear-gradient(to right, transparent, black ${EDGE_FADE}px, black calc(100% - ${EDGE_FADE}px), transparent)`
      : `linear-gradient(to right, transparent, black ${EDGE_FADE}px)`
    : canScrollRight
      ? `linear-gradient(to right, black calc(100% - ${EDGE_FADE}px), transparent)`
      : "none";

  return (
    <div
      ref={ref}
      onScroll={measure}
      className="flex [scroll-padding-inline:24px] [scrollbar-width:none]! items-stretch gap-6 overflow-x-auto [&::-webkit-scrollbar]:hidden!"
      style={{ maskImage, WebkitMaskImage: maskImage }}
    >
      {children}
    </div>
  );
}
