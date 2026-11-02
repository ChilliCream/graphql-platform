"use client";

import { useEffect, useRef } from "react";
import type { RefObject } from "react";

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

    let accumulatedMs = 0;
    let lastElapsedMs = 0;
    let rafBaseline: number | null = null;
    let rafId = 0;
    let inView = false;
    let running = false;

    const tick = (now: number) => {
      if (rafBaseline === null) {
        rafBaseline = now;
      }
      lastElapsedMs = accumulatedMs + (now - rafBaseline);
      onFrameRef.current(lastElapsedMs);
      rafId = requestAnimationFrame(tick);
    };

    const setRunning = (next: boolean) => {
      if (next === running) {
        return;
      }
      running = next;
      if (running) {
        rafBaseline = null;
        rafId = requestAnimationFrame(tick);
      } else {
        accumulatedMs = lastElapsedMs;
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
