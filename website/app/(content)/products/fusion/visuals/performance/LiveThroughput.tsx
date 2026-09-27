"use client";

import { useEffect, useRef, useState } from "react";
import { animate, motion, useMotionValue, useTransform } from "motion/react";

import { token } from "@/src/nitro";
import { ChartCanvas } from "@/src/nitro/primitives/ChartCanvas";
import { ease } from "@/src/nitro/lib/motion";

import { GAUGE_MAX, SETTLE_VALUE } from "./data";

export interface LiveThroughputProps {
  readonly active: boolean;
}

const STEP_MS = 900;
const SEED_LEN = 24;

function nextThroughput(t: number): number {
  return Math.round(
    SETTLE_VALUE + 220 * Math.sin(t * 0.6 + 0.5) + 80 * Math.sin(t * 1.9),
  );
}

const BARS: readonly number[] = Array.from({ length: SEED_LEN }, (_, i) =>
  nextThroughput(i),
);

const CHART_W = 600;
const CHART_H = 40;
const BAR_GAP = 2;
const BAR_RADIUS = 1;

function useLiveBars(active: boolean) {
  const [bars, setBars] = useState<number[]>(() => [...BARS]);
  const [preview, setPreview] = useState(() => nextThroughput(SEED_LEN));
  const counter = useRef(SEED_LEN - 1);
  const scrollT = useMotionValue(0);

  useEffect(() => {
    if (!active) return;
    let cancelled = false;

    const step = () => {
      animate(scrollT, 1, { duration: STEP_MS / 1000, ease: ease.linear }).then(
        () => {
          if (cancelled) return;
          const t = counter.current + 1;
          counter.current = t;
          setBars((prev) => [...prev.slice(1), nextThroughput(t)]);
          setPreview(nextThroughput(t + 1));
          scrollT.set(0);
          step();
        },
      );
    };
    step();

    return () => {
      cancelled = true;
    };
  }, [active, scrollT]);

  return { bars, preview, scrollT };
}

export function LiveThroughput({ active }: LiveThroughputProps) {
  const { bars, preview, scrollT } = useLiveBars(active);
  const n = bars.length;
  const slot = CHART_W / n;
  const barW = Math.max(0.5, slot - BAR_GAP);
  const domainMax = GAUGE_MAX;
  const translateX = useTransform(scrollT, (t) => -t * slot);
  const all = [...bars, preview];

  return (
    <ChartCanvas
      label={`Live throughput, scrolling bars near ${Math.round(SETTLE_VALUE / 1000)}K ops/s`}
      style={{ height: CHART_H }}
    >
      <svg
        viewBox={`0 0 ${CHART_W} ${CHART_H}`}
        preserveAspectRatio="none"
        width="100%"
        height="100%"
        style={{ display: "block", overflow: "hidden" }}
      >
        <motion.g style={{ x: translateX }}>
          {all.map((v, i) => {
            const x = i * slot + (slot - barW) / 2;
            const h = Math.max(2, (v / domainMax) * CHART_H);
            const y = CHART_H - h;
            return (
              <rect
                key={i}
                x={x}
                y={y}
                width={barW}
                height={h}
                rx={BAR_RADIUS}
                ry={BAR_RADIUS}
                vectorEffect="non-scaling-stroke"
                fill={token.cThroughput}
                opacity={0.8}
              />
            );
          })}
        </motion.g>
      </svg>
    </ChartCanvas>
  );
}
