"use client";

import { useEffect, useId } from "react";
import { animate, motion, useMotionValue, useTransform } from "motion/react";

import { token } from "@/src/nitro";
import { ease } from "@/src/nitro/lib/motion";

import { gaugeArcPath, polarPoint, sweepAngle } from "./gauge";
import { LATENCY_MAX, LATENCY_P50, LATENCY_P95 } from "./data";

interface LatencyDialProps {
  readonly active: boolean;
  readonly reduced: boolean;
}

const VB = 300;
const CX = VB / 2;
const CY = VB / 2;
const BEZEL_R = 148;
const FACE_R = 104;
const TRACK_R = 118;
const TRACK_WIDTH = 6;
const FILL_GLOW_WIDTH = TRACK_WIDTH + 6;
const START_ANGLE = 320;
const END_ANGLE = 40;
const TICK_STEPS = 10;
const MAJOR_EVERY = 2;
const NUMERAL_R = TRACK_R + 27;
const SWEEP_MS = 1200;

const GLOW = token.info;
const GLOW_BRIGHT = `color-mix(in srgb, ${token.info} 65%, white)`;

export function LatencyDial({ active, reduced }: LatencyDialProps) {
  const filterId = useId().replace(/:/g, "");
  const value = useMotionValue(reduced ? LATENCY_P95 : 0);
  const readout = useTransform(value, (v) => Math.round(v).toString());
  const fraction = useTransform(value, (v) => v / LATENCY_MAX);
  const fillOffset = useTransform(fraction, (f) => 1 - f);

  useEffect(() => {
    if (reduced) {
      value.set(LATENCY_P95);
      return;
    }
    if (!active) return;
    const sweep = animate(value, LATENCY_P95, {
      duration: SWEEP_MS / 1000,
      ease: ease.out,
    });
    return () => sweep.stop();
  }, [active, reduced, value]);

  const trackPath = gaugeArcPath(CX, CY, TRACK_R, START_ANGLE, END_ANGLE);
  const p50Angle = sweepAngle(
    LATENCY_P50 / LATENCY_MAX,
    START_ANGLE,
    END_ANGLE,
  );
  const [p50x1, p50y1] = polarPoint(CX, CY, TRACK_R + 6, p50Angle);
  const [p50x2, p50y2] = polarPoint(CX, CY, TRACK_R - 10, p50Angle);

  const ticks: { x1: number; y1: number; x2: number; y2: number }[] = [];
  const numerals: { x: number; y: number; label: string }[] = [];
  for (let i = 0; i <= TICK_STEPS; i++) {
    const f = i / TICK_STEPS;
    const major = i % MAJOR_EVERY === 0;
    const angle = sweepAngle(f, START_ANGLE, END_ANGLE);
    const [x1, y1] = polarPoint(CX, CY, TRACK_R + 5, angle);
    const [x2, y2] = polarPoint(CX, CY, TRACK_R - (major ? 10 : 5), angle);
    ticks.push({ x1, y1, x2, y2 });
    if (major) {
      const [nx, ny] = polarPoint(CX, CY, NUMERAL_R, angle);
      numerals.push({
        x: nx,
        y: ny,
        label: String(Math.round(f * LATENCY_MAX)),
      });
    }
  }

  return (
    <div className="@container w-full" style={{ aspectRatio: "1 / 1" }}>
      <div style={{ position: "relative", width: "100%", height: "100%" }}>
        <svg
          viewBox={`0 0 ${VB} ${VB}`}
          width="100%"
          height="100%"
          style={{ display: "block", overflow: "visible" }}
          role="img"
          aria-label={`Latency gauge, p95 near ${LATENCY_P95} ms, p50 mark at ${LATENCY_P50} ms, of ${LATENCY_MAX} ms scale`}
        >
          <defs>
            <filter
              id={`glow-${filterId}`}
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="4" />
            </filter>
          </defs>

          <circle
            cx={CX}
            cy={CY}
            r={FACE_R}
            fill={`color-mix(in srgb, ${token.bg} 55%, black)`}
            stroke={token.borderStrong}
            strokeWidth={1.25}
          />
          <circle
            cx={CX}
            cy={CY}
            r={BEZEL_R}
            fill="none"
            stroke={token.border}
            strokeWidth={1.25}
            opacity={0.5}
          />
          <path
            d={trackPath}
            fill="none"
            stroke={token.border}
            strokeWidth={TRACK_WIDTH}
            strokeLinecap="round"
            opacity={0.3}
          />
          <motion.path
            d={trackPath}
            pathLength={1}
            fill="none"
            stroke={GLOW}
            strokeWidth={FILL_GLOW_WIDTH}
            strokeLinecap="round"
            opacity={0.5}
            filter={`url(#glow-${filterId})`}
            style={{ strokeDasharray: "1 1", strokeDashoffset: fillOffset }}
          />
          <motion.path
            d={trackPath}
            pathLength={1}
            fill="none"
            stroke={GLOW}
            strokeWidth={TRACK_WIDTH}
            strokeLinecap="round"
            style={{ strokeDasharray: "1 1", strokeDashoffset: fillOffset }}
          />
          <line
            x1={p50x1}
            y1={p50y1}
            x2={p50x2}
            y2={p50y2}
            stroke={GLOW_BRIGHT}
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
        </svg>

        {numerals.map((n, i) => (
          <div
            key={i}
            aria-hidden="true"
            className="whitespace-nowrap"
            style={{
              position: "absolute",
              left: `${(n.x / VB) * 100}%`,
              top: `${(n.y / VB) * 100}%`,
              transform: "translate(-50%, -50%)",
              fontSize: 11,
              fontFamily: token.mono,
              color: token.textDim,
            }}
          >
            {n.label}
          </div>
        ))}

        <div
          role="img"
          aria-label={`${LATENCY_P95} ms p95`}
          style={{
            position: "absolute",
            inset: 0,
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <motion.span
            aria-hidden="true"
            style={{
              display: "inline-block",
              minWidth: "2ch",
              textAlign: "center",
              fontFamily: token.mono,
              fontSize: "clamp(22px, 15cqw, 34px)",
              fontWeight: 800,
              fontStyle: "italic",
              lineHeight: 1,
              color: token.textStrong,
              fontVariantNumeric: "tabular-nums",
              filter: `drop-shadow(0 0 6px ${GLOW})`,
            }}
          >
            {readout}
          </motion.span>
          <span
            aria-hidden="true"
            className="whitespace-nowrap"
            style={{
              marginTop: 2,
              fontSize: 11,
              color: token.textSecondary,
              fontFamily: token.mono,
            }}
          >
            ms p95
          </span>
        </div>
      </div>
    </div>
  );
}
