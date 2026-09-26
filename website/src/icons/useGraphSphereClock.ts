"use client";

import { useEffect, useRef } from "react";
import type { RefObject } from "react";

/** Runs `onFrame` on a single rAF loop only while `target` is on screen, the tab is visible, and `enabled` is true. */
export function useGraphSphereClock(
  target: RefObject<Element | null>,
  onFrame: (elapsedMs: number) => void,
  enabled: boolean,
): void {
  const onFrameRef = useRef(onFrame);
  useEffect(() => {
    onFrameRef.current = onFrame;
  });

  useEffect(() => {
    const el = target.current;
    if (!enabled || !el) {
      return;
    }

    const start = performance.now();
    let rafId = 0;
    let inView = false;
    let running = false;

    const tick = (now: number) => {
      onFrameRef.current(now - start);
      rafId = requestAnimationFrame(tick);
    };

    const setRunning = (next: boolean) => {
      if (next === running) {
        return;
      }
      running = next;
      if (running) {
        rafId = requestAnimationFrame(tick);
      } else {
        cancelAnimationFrame(rafId);
      }
    };

    const evaluate = () => {
      setRunning(inView && document.visibilityState === "visible");
    };

    const io = new IntersectionObserver(
      ([entry]) => {
        inView = entry.isIntersecting;
        evaluate();
      },
      { threshold: 0 },
    );
    io.observe(el);

    document.addEventListener("visibilitychange", evaluate);

    return () => {
      io.disconnect();
      document.removeEventListener("visibilitychange", evaluate);
      cancelAnimationFrame(rafId);
    };
  }, [target, enabled]);
}
