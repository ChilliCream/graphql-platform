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
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  amount?: number;
}

export interface ChartClock {
  ref: RefObject<HTMLDivElement | null>;
  t: MotionValue<number>;
  reduced: boolean;
  inView: boolean;
}

export function useChartClock({
  progress,
  playWindow = [0, 0.62],
  durationMs = 9000,
  amount = 0.35,
}: ChartClockProps = {}): ChartClock {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotionPreference();
  const inView = useInView(ref, { amount });
  const own = useMotionValue(reduced ? 1 : 0);
  const standalone = progress === undefined;

  useEffect(() => {
    if (!standalone) return;
    if (reduced) {
      own.set(1);
      return;
    }
    if (!inView) return;
    const controls = animate(own, [0, 1], {
      duration: durationMs / 1000,
      ease: "linear",
      repeat: Infinity,
      repeatType: "loop",
    });
    return () => controls.stop();
  }, [standalone, reduced, inView, durationMs, own]);

  const source = progress ?? own;
  const w0 = Math.max(0, Math.min(playWindow[0], 0.98));
  const w1 = Math.max(w0 + 0.001, Math.min(playWindow[1], 0.999));
  const windowed = useTransform(source, [w0, w1, 1], [0, 1, 1]);

  const frozen = useMotionValue(1);

  return { ref, t: reduced ? frozen : windowed, reduced, inView };
}
