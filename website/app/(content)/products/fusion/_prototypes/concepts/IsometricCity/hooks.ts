"use client";

import { useEffect, useState } from "react";
import type { RefObject } from "react";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";

/**
 * Motion gates for the Isometric City visuals.
 *
 * Every scene renders its rest frame first - the finished city, the route
 * fully drawn, the permit already stamped - and only starts moving once the
 * gate opens, so the server render, the reduced-motion render and the
 * off-screen render all show the same meaningful still frame.
 */

/** True while the enclosing `Scene` is in view, the tab is visible and motion is allowed. */
export function useCityMotion(): boolean {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  return active && !reduced;
}

/** The same gate for the hero, which sizes itself to the section, not a ratio. */
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
export function useCityCycle(
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

  // The parked value is derived, not stored, so a closed gate renders the rest
  // frame without a state write and the sequence resumes where it stopped.
  return running ? step : rest;
}
