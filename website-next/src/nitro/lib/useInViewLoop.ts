/**
 * Looping animation engine (PLAN.md §2.4, §9, §10).
 *
 * Two hooks, one model — a normalized progress value 0→1:
 *
 *  - `useMasterClock()` produces a raw looping clock (sawtooth 0→1, repeat ∞). The
 *    Monitoring Overview owns one and passes its `progress` down so the whole dashboard
 *    shares a single seamless cycle.
 *  - `useChartClock({ progress?, playWindow? })` is what every primitive calls. Given a
 *    shared `progress` it maps the master cycle onto the primitive's own play window
 *    (e.g. "draw during 0.10→0.25, then hold"). With no `progress` it spins up its own
 *    in-view-gated clock so the primitive animates standalone in its story.
 *
 * Both gate on `useInView` (off-screen demos idle) and collapse to a static final frame
 * (`t = 1`) under `prefers-reduced-motion` — Motion does not auto-cancel custom
 * `pathLength`/`repeat` loops, so we freeze them by hand.
 */
import { useEffect, useRef } from "react";
import type { RefObject } from "react";
import {
  animate,
  useInView,
  useMotionValue,
  useTransform,
  type MotionValue,
} from "motion/react";
import { useReducedMotionPreference } from "./motion";

export interface MasterClock {
  ref: RefObject<HTMLDivElement | null>;
  /** Raw looping progress, 0→1 sawtooth (frozen at 1 under reduced motion). */
  progress: MotionValue<number>;
  reduced: boolean;
}

export function useMasterClock({
  durationMs = 11000,
  amount = 0.25,
}: { durationMs?: number; amount?: number } = {}): MasterClock {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotionPreference();
  const inView = useInView(ref, { amount });
  const progress = useMotionValue(reduced ? 1 : 0);

  useEffect(() => {
    if (reduced) {
      progress.set(1);
      return;
    }
    if (!inView) return;
    const controls = animate(progress, [0, 1], {
      duration: durationMs / 1000,
      ease: "linear",
      repeat: Infinity,
      repeatType: "loop",
    });
    return () => controls.stop();
  }, [reduced, inView, durationMs, progress]);

  return { ref, progress, reduced };
}

export interface ChartClockProps {
  /** Shared master progress; omit to run a self-contained clock (standalone story). */
  progress?: MotionValue<number>;
  /** When this primitive draws within the cycle, as [start,end] fractions of 0..1. */
  playWindow?: [number, number];
  /** Standalone-clock cycle length (ignored when `progress` is provided). */
  durationMs?: number;
  /** In-view threshold for the standalone clock. */
  amount?: number;
  /**
   * Play the standalone clock a single time when first scrolled into view,
   * then hold the final frame instead of looping (calm marketing surfaces).
   */
  once?: boolean;
}

export interface ChartClock {
  /** Attach to the primitive's root (drives in-view gating when standalone). */
  ref: RefObject<HTMLDivElement | null>;
  /** Local animation progress 0→1 across the play window, then holds at 1. */
  t: MotionValue<number>;
  reduced: boolean;
  /** Whether the root is on screen — gate any free-running decorative loop on this. */
  inView: boolean;
}

/**
 * The primitive-facing hook. Build visuals off the returned `t` with `useTransform`,
 * optionally easing per segment, e.g.
 *   const draw = useTransform(t, [0, 1], [0, 1], { ease: ease.out })
 */
export function useChartClock({
  progress,
  playWindow = [0, 0.62],
  durationMs = 9000,
  amount = 0.35,
  once = false,
}: ChartClockProps = {}): ChartClock {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotionPreference();
  const inView = useInView(ref, { amount });
  const own = useMotionValue(reduced ? 1 : 0);
  const standalone = progress === undefined;
  // Set when a `once` clock has finished its single pass, so leaving and
  // re-entering the viewport does not replay the draw-in.
  const playedOnce = useRef(false);

  useEffect(() => {
    if (!standalone) return;
    if (reduced) {
      own.set(1);
      return;
    }
    if (once && playedOnce.current) {
      own.set(1);
      return;
    }
    if (!inView) return;
    const controls = animate(own, [0, 1], {
      duration: durationMs / 1000,
      ease: "linear",
      repeat: once ? 0 : Infinity,
      repeatType: "loop",
      onComplete: () => {
        playedOnce.current = true;
      },
    });
    return () => controls.stop();
  }, [standalone, reduced, inView, durationMs, once, own]);

  // Map the (shared or own) cycle onto this primitive's window, then hold at 1.
  const source = progress ?? own;
  const w0 = Math.max(0, Math.min(playWindow[0], 0.98));
  const w1 = Math.max(w0 + 0.001, Math.min(playWindow[1], 0.999));
  const windowed = useTransform(source, [w0, w1, 1], [0, 1, 1]);

  // Constant final-frame value for reduced motion (kept stable across renders).
  const frozen = useMotionValue(1);

  return { ref, t: reduced ? frozen : windowed, reduced, inView };
}
