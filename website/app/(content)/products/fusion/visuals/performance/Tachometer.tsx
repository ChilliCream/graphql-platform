"use client";

import { useEffect, useRef } from "react";
import { animate, motion, useMotionValue, useTransform } from "motion/react";

import { token } from "@/src/nitro";
import { ease, useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { useElementMotion } from "../hooks";
import { gaugeArcPath, needleRotation, polarPoint } from "./gauge";

export interface TachometerProps {
  readonly max: number;
  readonly redZoneStart: number;
  readonly settleValue: number;
  readonly idleBand: readonly [number, number];
  readonly unit: string;
}

const W = 220;
const H = 122;
const CX = 110;
const CY = 108;
const TRACK_R = 92;
const NEEDLE_LEN = 78;
const HUB_R = 6;

const SWEEP_MS = 1300;
const IDLE_MS = 4200;

const TICK_STEP = 0.1;
const MAJOR_TICKS = new Set([0, 0.2, 0.4, 0.6, 0.8, 1]);

function formatOps(n: number): string {
  return `${(n / 1000).toFixed(1)}K`;
}

function tickAngle(fraction: number): number {
  return 180 - fraction * 180;
}

export function Tachometer({
  max,
  redZoneStart,
  settleValue,
  idleBand,
  unit,
}: TachometerProps) {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotionPreference();
  const active = useElementMotion(ref);

  const value = useMotionValue(reduced ? settleValue : 0);
  const rotate = useTransform(value, (v) => needleRotation(v, max));
  const dashOffset = useTransform(value, (v) => 1 - v / max);
  const readout = useTransform(value, formatOps);

  useEffect(() => {
    if (reduced) {
      value.set(settleValue);
      return;
    }
    if (!active) return;

    let cancelled = false;
    const sweep = animate(value, settleValue, {
      duration: SWEEP_MS / 1000,
      ease: ease.out,
    });
    let idle = sweep;
    sweep.then(() => {
      if (cancelled) return;
      idle = animate(
        value,
        [settleValue, idleBand[0], idleBand[1], settleValue],
        {
          duration: IDLE_MS / 1000,
          ease: ease.inOut,
          repeat: Infinity,
        },
      );
    });

    return () => {
      cancelled = true;
      sweep.stop();
      idle.stop();
    };
  }, [active, reduced, settleValue, idleBand, value]);

  const redZoneAngleStart = 180 - (redZoneStart / max) * 180;
  const trackPath = gaugeArcPath(CX, CY, TRACK_R, 180, 0);
  const redZonePath = gaugeArcPath(CX, CY, TRACK_R, redZoneAngleStart, 0);

  const ticks: {
    x1: number;
    y1: number;
    x2: number;
    y2: number;
    major: boolean;
  }[] = [];
  for (let f = 0; f <= 1 + 1e-6; f += TICK_STEP) {
    const fraction = Math.round(f * 100) / 100;
    const major = MAJOR_TICKS.has(fraction);
    const angle = tickAngle(fraction);
    const [x1, y1] = polarPoint(CX, CY, TRACK_R + 9, angle);
    const [x2, y2] = polarPoint(CX, CY, TRACK_R - (major ? 15 : 7), angle);
    ticks.push({ x1, y1, x2, y2, major });
  }

  return (
    <div ref={ref} className="flex flex-col items-center gap-1">
      <svg
        viewBox={`0 0 ${W} ${H}`}
        width="100%"
        style={{ display: "block", maxWidth: 220, overflow: "visible" }}
        role="img"
        aria-label={`Throughput gauge, needle near ${formatOps(settleValue)} ${unit} of ${formatOps(max)} scale`}
      >
        <path
          d={trackPath}
          fill="none"
          stroke={token.border}
          strokeWidth={10}
          strokeLinecap="round"
        />
        <path
          d={redZonePath}
          fill="none"
          stroke={token.error}
          strokeWidth={10}
          strokeLinecap="butt"
          opacity={0.85}
        />
        <motion.path
          d={trackPath}
          pathLength={1}
          fill="none"
          stroke={token.cThroughput}
          strokeWidth={10}
          strokeLinecap="butt"
          style={{ strokeDasharray: "1 1", strokeDashoffset: dashOffset }}
        />
        <g transform={`translate(${CX}, ${CY})`}>
          <motion.g
            style={{
              rotate,
              transformBox: "view-box",
              originX: "0px",
              originY: "0px",
            }}
          >
            <line
              x1={0}
              y1={0}
              x2={-NEEDLE_LEN}
              y2={0}
              stroke={token.cThroughput}
              strokeWidth={3}
              strokeLinecap="round"
            />
          </motion.g>
        </g>
        <circle cx={CX} cy={CY} r={HUB_R} fill={token.textStrong} />
        {ticks.map((t, i) => (
          <line
            key={i}
            x1={t.x1}
            y1={t.y1}
            x2={t.x2}
            y2={t.y2}
            stroke={t.major ? token.textSecondary : token.borderStrong}
            strokeWidth={t.major ? 1.5 : 1}
          />
        ))}
      </svg>
      <div
        className="flex flex-col items-center"
        role="img"
        aria-label={`${formatOps(settleValue)} ${unit}`}
      >
        <motion.span
          aria-hidden="true"
          className="text-center"
          style={{
            display: "inline-block",
            minWidth: "5ch",
            fontFamily: token.mono,
            fontSize: 32,
            fontWeight: 600,
            lineHeight: 1,
            letterSpacing: "-0.02em",
            color: token.textStrong,
            fontVariantNumeric: "tabular-nums",
          }}
        >
          {readout}
        </motion.span>
        <span
          aria-hidden="true"
          className="text-[11px] whitespace-nowrap"
          style={{ color: token.textSecondary, fontFamily: token.mono }}
        >
          {unit}
        </span>
      </div>
    </div>
  );
}
