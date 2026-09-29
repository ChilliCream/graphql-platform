"use client";

import { useEffect, useMemo, useRef } from "react";
import { useMotionValue, type MotionValue } from "motion/react";

import { ROTATE_MS } from "./data";
import { STATIC_BURST, telemetryAt, type Telemetry } from "./sim";

export interface TelemetryMotion {
  readonly ops: MotionValue<number>;
  readonly cores: MotionValue<readonly number[]>;
  readonly cpuTotal: MotionValue<number>;
  readonly memory: MotionValue<number>;
  readonly netIn: MotionValue<number>;
  readonly netOut: MotionValue<number>;
  readonly netInHistory: MotionValue<readonly number[]>;
  readonly netOutHistory: MotionValue<readonly number[]>;
  readonly latency: MotionValue<number>;
  readonly latencyHistory: MotionValue<readonly number[]>;
  readonly rotate: MotionValue<number>;
}

function write(target: TelemetryMotion, snap: Telemetry) {
  target.ops.set(snap.ops);
  target.cores.set(snap.cores);
  target.cpuTotal.set(snap.cpuTotal);
  target.memory.set(snap.memory);
  target.netIn.set(snap.netIn);
  target.netOut.set(snap.netOut);
  target.netInHistory.set(snap.netInHistory);
  target.netOutHistory.set(snap.netOutHistory);
  target.latency.set(snap.latency);
  target.latencyHistory.set(snap.latencyHistory);
}

// One rAF loop writing motion values; elapsed time accumulates only while active, so a pause resumes without a jump.
export function useTelemetryClock(
  active: boolean,
  reduced: boolean,
): TelemetryMotion {
  const initial = reduced ? STATIC_BURST : telemetryAt(0);

  const ops = useMotionValue(initial.ops);
  const cores = useMotionValue<readonly number[]>(initial.cores);
  const cpuTotal = useMotionValue(initial.cpuTotal);
  const memory = useMotionValue(initial.memory);
  const netIn = useMotionValue(initial.netIn);
  const netOut = useMotionValue(initial.netOut);
  const netInHistory = useMotionValue<readonly number[]>(initial.netInHistory);
  const netOutHistory = useMotionValue<readonly number[]>(
    initial.netOutHistory,
  );
  const latency = useMotionValue(initial.latency);
  const latencyHistory = useMotionValue<readonly number[]>(
    initial.latencyHistory,
  );
  const rotate = useMotionValue(0);

  const motion = useMemo<TelemetryMotion>(
    () => ({
      ops,
      cores,
      cpuTotal,
      memory,
      netIn,
      netOut,
      netInHistory,
      netOutHistory,
      latency,
      latencyHistory,
      rotate,
    }),
    [
      ops,
      cores,
      cpuTotal,
      memory,
      netIn,
      netOut,
      netInHistory,
      netOutHistory,
      latency,
      latencyHistory,
      rotate,
    ],
  );

  const elapsedRef = useRef(0);
  const lastRef = useRef<number | null>(null);

  useEffect(() => {
    if (reduced) {
      write(motion, STATIC_BURST);
      return;
    }
    if (!active) {
      lastRef.current = null;
      return;
    }

    let frame = requestAnimationFrame(function tick(ts) {
      if (lastRef.current === null) lastRef.current = ts;
      elapsedRef.current += ts - lastRef.current;
      lastRef.current = ts;
      write(motion, telemetryAt(elapsedRef.current));
      motion.rotate.set(((elapsedRef.current % ROTATE_MS) / ROTATE_MS) * 360);
      frame = requestAnimationFrame(tick);
    });

    return () => cancelAnimationFrame(frame);
  }, [active, reduced, motion]);

  return motion;
}
