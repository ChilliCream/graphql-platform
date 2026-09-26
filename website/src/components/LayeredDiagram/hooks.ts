"use client";

import { useEffect, useState } from "react";
import type { RefObject } from "react";

import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

/** Copied from the Fusion page's `visuals/hooks.ts` rather than imported across pages. */
export function useElementMotion(ref: RefObject<Element | null>): boolean {
  const reduced = useReducedMotionPreference();
  const [inView, setInView] = useState(false);
  const [visible, setVisible] = useState(true);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          setInView(entry.isIntersecting);
        }
      },
      { rootMargin: "10% 0px", threshold: 0 },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, [ref]);

  useEffect(() => {
    const onChange = () => setVisible(document.visibilityState === "visible");
    onChange();
    document.addEventListener("visibilitychange", onChange);
    return () => document.removeEventListener("visibilitychange", onChange);
  }, []);

  return inView && visible && !reduced;
}

/** Counts `0 .. steps - 1` on an interval while `running`, parks on `rest` otherwise. */
export function useCycle(
  running: boolean,
  steps: number,
  intervalMs: number,
  rest: number,
): number {
  const [step, setStep] = useState(rest);

  useEffect(() => {
    if (!running) return;

    const id = window.setInterval(
      () => setStep((current) => (current + 1) % steps),
      intervalMs,
    );
    return () => window.clearInterval(id);
  }, [running, steps, intervalMs]);

  // Derived, not stored, so a closed gate renders the rest frame with no state write.
  return running ? step : rest;
}

/** `animation` shorthand that collapses to `none` while the gate is closed. */
export function anim(running: boolean, value: string): string {
  return running ? value : "none";
}
