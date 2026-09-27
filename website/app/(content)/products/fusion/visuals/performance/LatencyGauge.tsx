"use client";

import { useEffect, useId } from "react";
import { animate, motion, useMotionValue, useTransform } from "motion/react";

import { token } from "@/src/nitro";
import { ease } from "@/src/nitro/lib/motion";

import { fractionAngle, gaugeArcPath, polarPoint } from "./gauge";
import { LATENCY_MAX, P50_MARK, P95_SETTLE } from "./data";

export interface LatencyGaugeProps {
  readonly active: boolean;
  readonly reduced: boolean;
}

const MARGIN = 10;
const BEZEL_R = 130;
const CX = BEZEL_R + MARGIN;
const CY = BEZEL_R + MARGIN;
const MAX_W = 240;
const TRACK_R = 96;
const TRACK_WIDTH = 10;
const FILL_GLOW_WIDTH = TRACK_WIDTH + 6;
const TICK_STEPS = 8;
const MAJOR_EVERY = 2;
const NUMERAL_R = TRACK_R + 17;
const W = CX * 2;
const H = CY + 14;

const READOUT_CX = CX;
const READOUT_CY = CY - 45;

const SWEEP_MS = 1300;

export function LatencyGauge({ active, reduced }: LatencyGaugeProps) {
  const filterId = useId().replace(/:/g, "");
  const value = useMotionValue(reduced ? P95_SETTLE : 0);
  const readout = useTransform(value, (v) => Math.round(v).toString());
  const fraction = useTransform(value, (v) => v / LATENCY_MAX);
  const fillOffset = useTransform(fraction, (f) => f - 1);

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
  const facePath = `${gaugeArcPath(CX, CY, BEZEL_R, 180, 0)} Z`;
  const p50Angle = fractionAngle(P50_MARK / LATENCY_MAX, true);
  const [p50x1, p50y1] = polarPoint(CX, CY, TRACK_R + 5, p50Angle);
  const [p50x2, p50y2] = polarPoint(CX, CY, TRACK_R - 9, p50Angle);

  const ticks: { x1: number; y1: number; x2: number; y2: number }[] = [];
  const numerals: { x: number; y: number; label: string }[] = [];
  for (let i = 0; i <= TICK_STEPS; i++) {
    const fractionAt = i / TICK_STEPS;
    const major = i % MAJOR_EVERY === 0;
    const angle = fractionAngle(fractionAt, true);
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
    <div className="@container w-full" style={{ maxWidth: MAX_W }}>
      <div
        style={{
          position: "relative",
          width: "100%",
          aspectRatio: `${W} / ${H}`,
        }}
      >
        <svg
          viewBox={`0 0 ${W} ${H}`}
          width="100%"
          height="100%"
          style={{ display: "block", overflow: "visible" }}
          role="img"
          aria-label={`Latency gauge, p95 near ${P95_SETTLE} ms, p50 mark at ${P50_MARK} ms, of ${LATENCY_MAX} ms scale`}
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

          <path
            d={facePath}
            fill={token.card}
            stroke={token.borderStrong}
            strokeWidth={1.25}
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
            stroke={token.cP95}
            strokeWidth={FILL_GLOW_WIDTH}
            strokeLinecap="round"
            opacity={0.55}
            filter={`url(#glow-${filterId})`}
            style={{ strokeDasharray: "1 1", strokeDashoffset: fillOffset }}
          />
          <motion.path
            d={trackPath}
            pathLength={1}
            fill="none"
            stroke={token.cP95}
            strokeWidth={TRACK_WIDTH}
            strokeLinecap="round"
            style={{ strokeDasharray: "1 1", strokeDashoffset: fillOffset }}
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

        <div
          role="img"
          aria-label={`${P95_SETTLE} ms p95`}
          style={{
            position: "absolute",
            left: `${(READOUT_CX / W) * 100}%`,
            top: `${(READOUT_CY / H) * 100}%`,
            transform: "translate(-50%, -50%)",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
          }}
        >
          <motion.span
            aria-hidden="true"
            style={{
              fontFamily: token.mono,
              fontSize: "clamp(16px, 13cqw, 32px)",
              fontWeight: 800,
              fontStyle: "italic",
              lineHeight: 1,
              color: token.textStrong,
              fontVariantNumeric: "tabular-nums",
              filter: `drop-shadow(0 0 5px ${token.cP95})`,
            }}
          >
            {readout}
          </motion.span>
          <span
            aria-hidden="true"
            className="whitespace-nowrap"
            style={{
              marginTop: 1,
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
