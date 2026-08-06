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

const DEFAULT_HIDDEN_CLASS_NAME = "translate-y-4 opacity-0";
const DEFAULT_SHOWN_CLASS_NAME = "translate-y-0 opacity-100";
const DEFAULT_TRANSITION_CLASS_NAME = "transition-all duration-500 ease-out";
const REDUCED_MOTION_CLASS_NAME =
  "motion-reduce:translate-y-0 motion-reduce:transform-none motion-reduce:opacity-100 motion-reduce:transition-none";

/** Reveals its content once when it first scrolls into view. */
export function RevealOnScroll({
  children,
  className,
  hiddenClassName = DEFAULT_HIDDEN_CLASS_NAME,
  shownClassName = DEFAULT_SHOWN_CLASS_NAME,
  rootMargin = "0px 0px 15% 0px",
  threshold = 0,
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

    // Rescue content the observer should already have revealed, in case it
    // missed or never fired. Anything still genuinely below the extended root
    // stays hidden so it can animate in on scroll.
    const fallbackId = window.setTimeout(() => {
      if (node.getBoundingClientRect().top < window.innerHeight * 1.15) {
        setShown(true);
      }
    }, 1200);

    return () => {
      window.clearTimeout(fallbackId);
      observer.disconnect();
    };
  }, [rootMargin, threshold]);

  return (
    <div
      ref={ref}
      className={[
        className,
        DEFAULT_TRANSITION_CLASS_NAME,
        REDUCED_MOTION_CLASS_NAME,
        shown ? shownClassName : hiddenClassName,
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {children}
    </div>
  );
}
