"use client";

import { useEffect } from "react";
import { animate, motion, useMotionValue, useTransform } from "motion/react";

import { token } from "@/src/nitro";
import { ease } from "@/src/nitro/lib/motion";

import { fractionAngle, gaugeArcPath, polarPoint } from "./gauge";
import { LATENCY_MAX, P50_MARK, P95_SETTLE } from "./data";

export interface LatencyGaugeProps {
  readonly active: boolean;
  readonly reduced: boolean;
}

const MARGIN = 8;
const BEZEL_R = 66;
const CX = BEZEL_R + MARGIN;
const TRACK_R = 50;
const TRACK_WIDTH = 8;
const TICK_STEPS = 8;
const MAJOR_EVERY = 2;
const NUMERAL_R = TRACK_R + 15;
const CY = BEZEL_R + MARGIN;
const W = CX * 2;
const H = CY + 12;

const SWEEP_MS = 1300;

export function LatencyGauge({ active, reduced }: LatencyGaugeProps) {
  const value = useMotionValue(reduced ? P95_SETTLE : 0);
  const readout = useTransform(value, (v) => Math.round(v).toString());
  const dashOffset = useTransform(value, (v) => 1 - v / LATENCY_MAX);

  useEffect(() => {
    if (reduced) {
      value.set(P95_SETTLE);
      return;
    }
    if (!active) return;
    const sweep = animate(value, P95_SETTLE, {
      duration: SWEEP_MS / 1000,
      ease: ease.out,
    });
    return () => sweep.stop();
  }, [active, reduced, value]);

  const trackPath = gaugeArcPath(CX, CY, TRACK_R, 180, 0);
  const p50Angle = fractionAngle(P50_MARK / LATENCY_MAX);
  const [p50x1, p50y1] = polarPoint(CX, CY, TRACK_R + 5, p50Angle);
  const [p50x2, p50y2] = polarPoint(CX, CY, TRACK_R - 9, p50Angle);

  const ticks: { x1: number; y1: number; x2: number; y2: number }[] = [];
  const numerals: { x: number; y: number; label: string }[] = [];
  for (let i = 0; i <= TICK_STEPS; i++) {
    const fractionAt = i / TICK_STEPS;
    const major = i % MAJOR_EVERY === 0;
    const angle = fractionAngle(fractionAt);
    const [x1, y1] = polarPoint(CX, CY, TRACK_R + 4, angle);
    const [x2, y2] = polarPoint(CX, CY, TRACK_R - (major ? 8 : 4), angle);
    ticks.push({ x1, y1, x2, y2 });
    if (major) {
      const [nx, ny] = polarPoint(CX, CY, NUMERAL_R, angle);
      numerals.push({
        x: nx,
        y: ny,
        label: String(Math.round(fractionAt * LATENCY_MAX)),
      });
    }
  }

  return (
    <div
      className="flex flex-col items-center"
      role="img"
      aria-label={`Latency gauge, p95 near ${P95_SETTLE} ms, p50 mark at ${P50_MARK} ms, of ${LATENCY_MAX} ms scale`}
    >
      <svg
        viewBox={`0 0 ${W} ${H}`}
        width={W}
        height={H}
        style={{ display: "block", overflow: "visible" }}
        aria-hidden="true"
      >
        <path
          d={`${gaugeArcPath(CX, CY, BEZEL_R, 180, 0)} Z`}
          fill={token.card}
          stroke={token.borderStrong}
          strokeWidth={1.25}
        />
        <path
          d={trackPath}
          fill="none"
          stroke={token.border}
          strokeWidth={TRACK_WIDTH}
          strokeLinecap="butt"
        />
        <motion.path
          d={trackPath}
          pathLength={1}
          fill="none"
          stroke={token.cP95}
          strokeWidth={TRACK_WIDTH}
          strokeLinecap="butt"
          style={{
            strokeDasharray: "1 1",
            strokeDashoffset: dashOffset,
          }}
        />
        <line
          x1={p50x1}
          y1={p50y1}
          x2={p50x2}
          y2={p50y2}
          stroke={token.cLatency}
          strokeWidth={2}
        />
        {ticks.map((t, i) => (
          <line
            key={i}
            x1={t.x1}
            y1={t.y1}
            x2={t.x2}
            y2={t.y2}
            stroke={token.borderStrong}
            strokeWidth={1}
          />
        ))}
        {numerals.map((n, i) => (
          <text
            key={i}
            x={n.x}
            y={n.y}
            textAnchor="middle"
            dominantBaseline="middle"
            fontFamily={token.mono}
            fontSize={11}
            fill={token.textDim}
          >
            {n.label}
          </text>
        ))}
      </svg>
      <div className="mt-1 flex flex-col items-center">
        <motion.span
          aria-hidden="true"
          style={{
            fontFamily: token.mono,
            fontSize: 18,
            fontWeight: 800,
            fontStyle: "italic",
            lineHeight: 1,
            color: token.textStrong,
            fontVariantNumeric: "tabular-nums",
          }}
        >
          {readout}
        </motion.span>
        <span
          className="text-[11px] whitespace-nowrap"
          style={{ color: token.textSecondary, fontFamily: token.mono }}
        >
          ms p95
        </span>
      </div>
    </div>
  );
}
