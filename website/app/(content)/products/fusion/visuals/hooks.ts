"use client";

import { useEffect, useState } from "react";
import type { RefObject } from "react";

import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

/**
 * Motion gates for the Fusion page's console visuals. Every visual renders
 * its rest frame first and only starts moving once the gate is open.
 */

/**
 * Gate for a visual that sizes itself to its container, such as a full-bleed
 * hero or a feature-row panel, rather than a fixed-ratio stage: `true` once
 * the element is in the viewport, the tab is visible and motion is allowed.
 */
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

/**
 * Discrete step driver: counts `0 .. steps - 1` on an interval while
 * `running`, and parks on `rest` whenever the gate is closed.
 */
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

  // Parked value is derived, not stored, so a closed gate renders the rest
  // frame without a state write and the sequence resumes where it stopped.
  return running ? step : rest;
}

/** `animation` shorthand that collapses to `none` while the gate is closed. */
export function anim(running: boolean, value: string): string {
  return running ? value : "none";
}
