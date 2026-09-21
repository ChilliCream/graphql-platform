"use client";

import { useEffect, useLayoutEffect, useState } from "react";
import type { RefObject } from "react";

import { useReducedMotionPreference, useSceneActive } from "./Scene";

/**
 * Motion gates for the Fusion page's console visuals. Every visual renders
 * its rest frame first and only starts moving once the gate is open.
 */

/**
 * Below this viewport width a visual switches from its desktop layout to a
 * re-flowed, narrower-`viewBox` mobile column (see `MOBILE_W` in each
 * visual). `useNarrowViewport` is the single source of truth for that
 * switch, kept separate from `useSvgLabelScale` below: a panel can render
 * narrower than its own desktop `viewBox` purely by desktop layout choice
 * (a sidebar slot narrower than the visual's design width, say), and that
 * must still boost its label size to meet the floor without also flipping
 * the visual into its mobile re-flow.
 */
const DESKTOP_BREAKPOINT_PX = 1024;

/**
 * `true` once the viewport has narrowed under `breakpointPx`, kept current
 * across resizes. `false` until the first client measurement lands, so
 * server-rendered markup matches the common desktop case.
 */
export function useNarrowViewport(
  breakpointPx: number = DESKTOP_BREAKPOINT_PX,
): boolean {
  const [narrow, setNarrow] = useState(false);

  useEffect(() => {
    const query = window.matchMedia(`(max-width: ${breakpointPx - 1}px)`);
    const measure = () => setNarrow(query.matches);
    measure();
    query.addEventListener("change", measure);
    return () => query.removeEventListener("change", measure);
  }, [breakpointPx]);

  return narrow;
}

/**
 * An SVG's rendered-width / viewBox-width ratio, kept current across
 * resizes. Pairs with `svgLabelSize` (`../tokens`) so a visual's `<text>`
 * sizes hold their minimum pixel size at every width, whether the viewBox
 * scales down for a narrow viewport or a panel simply sits in a slot
 * narrower than its own design width. Defaults to `1` (no boost) until the
 * first client measurement lands, so server-rendered markup matches the
 * common desktop case.
 */
export function useSvgLabelScale(
  ref: RefObject<SVGSVGElement | null>,
  viewBoxWidth: number,
): number {
  const [scale, setScale] = useState(1);

  useLayoutEffect(() => {
    const node = ref.current;
    if (!node || viewBoxWidth <= 0) return;

    const measure = () => {
      const width = node.getBoundingClientRect().width;
      if (width <= 0) return;
      setScale(width / viewBoxWidth);
    };
    measure();

    if (typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(measure);
    observer.observe(node);
    window.addEventListener("resize", measure);
    return () => {
      observer.disconnect();
      window.removeEventListener("resize", measure);
    };
  }, [ref, viewBoxWidth]);

  return scale;
}

/** True while the enclosing `Scene` is in view, the tab is visible and motion is allowed. */
export function useSceneMotion(): boolean {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  return active && !reduced;
}

/**
 * Same gate for a visual that is not inside a `Scene`, such as a full-bleed
 * hero that sizes itself to the viewport instead of a fixed ratio.
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
