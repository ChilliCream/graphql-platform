"use client";

import type { ReactNode } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";

interface TrendLineProps {
  readonly values: MotionValue<readonly number[]>;
  readonly domain: readonly [number, number];
  readonly color: string;
  readonly width?: number;
}

const CHART_W = 100;
const CHART_H = 32;

export function TrendLine({
  values,
  domain,
  color,
  width = 1.6,
}: TrendLineProps) {
  const [lo, hi] = domain;
  const d = useTransform(values, (list) =>
    list
      .map((v, i) => {
        const x = (i / (list.length - 1)) * CHART_W;
        const t = Math.min(1, Math.max(0, (v - lo) / (hi - lo)));
        return `${i === 0 ? "M" : "L"}${x.toFixed(2)} ${(CHART_H - t * CHART_H).toFixed(2)}`;
      })
      .join(" "),
  );

  return (
    <motion.path
      d={d}
      fill="none"
      stroke={color}
      strokeWidth={width}
      strokeLinecap="round"
      strokeLinejoin="round"
      vectorEffect="non-scaling-stroke"
    />
  );
}

interface TrendChartProps {
  readonly children: ReactNode;
  readonly label: string;
}

export function TrendChart({ children, label }: TrendChartProps) {
  return (
    <svg
      viewBox={`0 0 ${CHART_W} ${CHART_H}`}
      preserveAspectRatio="none"
      width="100%"
      height="100%"
      style={{ display: "block", overflow: "visible" }}
      role="img"
      aria-label={label}
    >
      {children}
    </svg>
  );
}
