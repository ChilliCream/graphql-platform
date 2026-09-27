"use client";

import { useEffect } from "react";
import { animate, motion, useMotionValue } from "motion/react";

import { token } from "@/src/nitro";
import { ChartCanvas } from "@/src/nitro/primitives/ChartCanvas";
import { ease } from "@/src/nitro/lib/motion";

import { SETTLE_VALUE } from "./data";

interface LiveThroughputProps {
  readonly active: boolean;
}

const BAR_COUNT = 28;
const DOMAIN_MIN = SETTLE_VALUE - 480;
const DOMAIN_MAX = SETTLE_VALUE + 480;
const SCROLL_MS = 14000;

const CHART_W = 600;
const CHART_H = 40;
const BAR_GAP = 2;
const BAR_RADIUS = 1;

function barValue(i: number): number {
  return Math.round(
    SETTLE_VALUE + 340 * Math.sin(i * 0.6 + 0.5) + 140 * Math.sin(i * 1.9),
  );
}

const BARS: readonly number[] = Array.from({ length: BAR_COUNT }, (_, i) =>
  barValue(i),
);
const LOOP: readonly number[] = [...BARS, ...BARS];

export function LiveThroughput({ active }: LiveThroughputProps) {
  const slot = CHART_W / BAR_COUNT;
  const barW = Math.max(0.5, slot - BAR_GAP);
  const x = useMotionValue(0);

  useEffect(() => {
    if (!active) {
      x.set(0);
      return;
    }
    const controls = animate(x, -CHART_W, {
      duration: SCROLL_MS / 1000,
      ease: ease.linear,
      repeat: Infinity,
      repeatType: "loop",
    });
    return () => controls.stop();
  }, [active, x]);

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
        <motion.g style={{ x }}>
          {LOOP.map((v, i) => {
            const xPos = i * slot + (slot - barW) / 2;
            const clamped = Math.min(DOMAIN_MAX, Math.max(DOMAIN_MIN, v));
            const h = Math.max(
              2,
              ((clamped - DOMAIN_MIN) / (DOMAIN_MAX - DOMAIN_MIN)) * CHART_H,
            );
            const y = CHART_H - h;
            return (
              <rect
                key={i}
                x={xPos}
                y={y}
                width={barW}
                height={h}
                rx={BAR_RADIUS}
                ry={BAR_RADIUS}
                vectorEffect="non-scaling-stroke"
                fill={token.cThroughput}
                opacity={0.85}
              />
            );
          })}
        </motion.g>
      </svg>
    </ChartCanvas>
  );
}
