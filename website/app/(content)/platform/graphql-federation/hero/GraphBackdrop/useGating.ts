"use client";

// A small in-view + tab-visibility gate for the canvas RAF loop, the same
// IntersectionObserver + visibilitychange shape this page's hero/anim.tsx
// uses for its SVG visuals, standalone so it can gate a canvas element
// instead of a map of ref-driven SVG helpers.
import { useEffect, useRef, useState } from "react";

export function useInViewAndVisible<T extends Element>(): readonly [
  React.RefObject<T | null>,
  boolean,
] {
  const ref = useRef<T | null>(null);
  const [active, setActive] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) {
      return;
    }
    let inView = false;
    const sync = () => setActive(inView && !document.hidden);
    const io = new IntersectionObserver(
      (entries) => {
        inView = entries[entries.length - 1].isIntersecting;
        sync();
      },
      { threshold: 0.01 },
    );
    io.observe(el);
    document.addEventListener("visibilitychange", sync);
    return () => {
      io.disconnect();
      document.removeEventListener("visibilitychange", sync);
    };
  }, []);

  return [ref, active] as const;
}
