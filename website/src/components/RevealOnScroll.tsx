"use client";

import type { ReactNode } from "react";
import { useLayoutEffect, useRef, useState } from "react";

interface RevealOnScrollProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly hiddenClassName?: string;
  readonly shownClassName?: string;
  readonly rootMargin?: string;
  readonly threshold?: number;
}

const DEFAULT_HIDDEN_CLASS_NAME = "translate-y-6 opacity-0";
const DEFAULT_SHOWN_CLASS_NAME = "translate-y-0 opacity-100";
const DEFAULT_TRANSITION_CLASS_NAME = "transition-all duration-700 ease-out";

/** Reveals its content once when it first scrolls into view. */
export function RevealOnScroll({
  children,
  className,
  hiddenClassName = DEFAULT_HIDDEN_CLASS_NAME,
  shownClassName = DEFAULT_SHOWN_CLASS_NAME,
  rootMargin = "0px 0px -10% 0px",
  threshold = 0.2,
}: RevealOnScrollProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [shown, setShown] = useState(false);

  useLayoutEffect(() => {
    const node = ref.current;
    if (node === null) {
      return;
    }

    // polyfill-free fallback for SSR and old browsers. The content will be visible
    if (typeof IntersectionObserver === "undefined") {
      const id = requestAnimationFrame(() => setShown(true));
      return () => cancelAnimationFrame(id);
    }

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            setShown(true);
            observer.disconnect();
          }
        }
      },
      { threshold, rootMargin },
    );

    observer.observe(node);
    return () => observer.disconnect();
  }, [rootMargin, threshold]);

  return (
    <div
      ref={ref}
      className={[
        className,
        DEFAULT_TRANSITION_CLASS_NAME,
        shown ? shownClassName : hiddenClassName,
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {children}
    </div>
  );
}
