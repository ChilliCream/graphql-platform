"use client";

import { type RefObject, useEffect, useRef } from "react";

import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

export type RafFrame = (t: number, dt: number, life: number) => void;

export interface RafLoopSetup {
  readonly frame: RafFrame;
  readonly rest: () => void;
}

interface RafLoopOptions {
  readonly period?: number;
  readonly threshold?: number;
}

export function useRafLoop(
  rootRef: RefObject<HTMLElement | null>,
  setup: () => RafLoopSetup,
  { period = Infinity, threshold = 0.2 }: RafLoopOptions = {},
): void {
  const setupRef = useRef(setup);
  const reducedMotion = useReducedMotionPreference();

  useEffect(() => {
    const root = rootRef.current;
    if (!root) {
      return;
    }

    const { frame, rest } = setupRef.current();

    if (reducedMotion) {
      rest();
      return;
    }

    let raf = 0;
    let running = false;
    let inView = false;
    let t = 0;
    let life = 0;
    let last = 0;

    const step = (now: number) => {
      const dt = Math.min(now - last, 50);
      last = now;
      t = (t + dt) % period;
      life += dt;
      frame(t, dt, life);
      raf = requestAnimationFrame(step);
    };
    const sync = () => {
      const should = inView && !document.hidden;
      if (should && !running) {
        running = true;
        last = performance.now();
        raf = requestAnimationFrame(step);
      } else if (!should && running) {
        running = false;
        cancelAnimationFrame(raf);
      }
    };
    const io = new IntersectionObserver(
      (entries) => {
        inView = entries[entries.length - 1]?.isIntersecting ?? false;
        sync();
      },
      { threshold },
    );
    io.observe(root);
    document.addEventListener("visibilitychange", sync);
    return () => {
      io.disconnect();
      document.removeEventListener("visibilitychange", sync);
      cancelAnimationFrame(raf);
    };
  }, [rootRef, period, threshold, reducedMotion]);
}
