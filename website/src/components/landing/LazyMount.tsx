"use client";

import React, { useEffect, useRef, useState } from "react";

interface LazyMountProps {
  children: React.ReactNode;
  minHeight?: number;
}

function findScrollParent(el: HTMLElement | null): HTMLElement | null {
  let cur = el?.parentElement;
  while (cur && cur !== document.body) {
    const cs = getComputedStyle(cur);
    if (/(auto|scroll|overlay)/.test(cs.overflowY)) return cur;
    cur = cur.parentElement;
  }
  return null;
}

export const LazyMount: React.FC<LazyMountProps> = ({
  children,
  minHeight = 600,
}) => {
  const ref = useRef<HTMLDivElement>(null);
  const [shown, setShown] = useState(false);

  useEffect(() => {
    if (shown) return;
    if (typeof IntersectionObserver === "undefined") {
      setShown(true);
      return;
    }
    const scrollRoot = findScrollParent(ref.current);
    const obs = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) {
          setShown(true);
          obs.disconnect();
        }
      },
      { root: scrollRoot, rootMargin: "200px 0px" }
    );
    if (ref.current) obs.observe(ref.current);
    return () => obs.disconnect();
  }, [shown]);

  if (shown) return <>{children}</>;
  return <div ref={ref} className="lazy-placeholder" style={{ minHeight }} />;
};
