"use client";

import { useEffect, useId } from "react";
import { animate, motion, useMotionValue, useTransform } from "motion/react";

import { token } from "@/src/nitro";
import { ease } from "@/src/nitro/lib/motion";

import { DialFace } from "./DialFace";
import { DialNumeral } from "./DialNumeral";
import { gaugeArcPath, polarPoint, sweepAngle } from "./gauge";
import { CX, CY, ELECTRIC, ELECTRIC_BRIGHT, ELECTRIC_DIM, VB } from "./hud";
import { LATENCY_MAX, LATENCY_P50, LATENCY_P95 } from "./data";

interface LatencyDialProps {
  readonly active: boolean;
  readonly reduced: boolean;
}

const FACE_R = 74;
const TRACK_R = 86;
const TRACK_WIDTH = 6;
const FILL_GLOW_WIDTH = TRACK_WIDTH * 2;
const MAJOR_TICK = FILL_GLOW_WIDTH / 2;
const MINOR_TICK = TRACK_WIDTH / 2;
const P50_TICK = MAJOR_TICK + 0.5;
const START_ANGLE = 225;
const END_ANGLE = -45;
const TICK_STEPS = 10;
const MAJOR_EVERY = 2;
const SWEEP_MS = 1200;

export function LatencyDial({ active, reduced }: LatencyDialProps) {
  const filterId = useId().replace(/:/g, "");
  const value = useMotionValue(reduced ? LATENCY_P95 : 0);
  const readout = useTransform(value, (v) => Math.round(v).toString());
  const fillOffset = useTransform(value, (v) => 1 - v / LATENCY_MAX);

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
  const [p50x1, p50y1] = polarPoint(CX, CY, TRACK_R + P50_TICK, p50Angle);
  const [p50x2, p50y2] = polarPoint(CX, CY, TRACK_R - P50_TICK, p50Angle);

  const ticks = Array.from({ length: TICK_STEPS + 1 }, (_, i) => {
    const f = i / TICK_STEPS;
    const angle = sweepAngle(f, START_ANGLE, END_ANGLE);
    const reach = i % MAJOR_EVERY === 0 ? MAJOR_TICK : MINOR_TICK;
    const [x1, y1] = polarPoint(CX, CY, TRACK_R + reach, angle);
    const [x2, y2] = polarPoint(CX, CY, TRACK_R - reach, angle);
    return { x1, y1, x2, y2 };
  });
  const numerals = Array.from(
    { length: TICK_STEPS / MAJOR_EVERY + 1 },
    (_, i) => {
      const f = i / (TICK_STEPS / MAJOR_EVERY);
      return {
        angle: sweepAngle(f, START_ANGLE, END_ANGLE),
        label: String(Math.round(f * LATENCY_MAX)),
      };
    },
  );

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

          <DialFace faceRadius={FACE_R} />

          <path
            d={trackPath}
            fill="none"
            stroke={ELECTRIC_DIM}
            strokeWidth={TRACK_WIDTH}
            strokeLinecap="butt"
          />
          <motion.path
            d={trackPath}
            pathLength={1}
            fill="none"
            stroke={ELECTRIC}
            strokeWidth={FILL_GLOW_WIDTH}
            strokeLinecap="butt"
            opacity={0.6}
            filter={`url(#glow-${filterId})`}
            style={{ strokeDasharray: "1 1", strokeDashoffset: fillOffset }}
          />
          <motion.path
            d={trackPath}
            pathLength={1}
            fill="none"
            stroke={ELECTRIC}
            strokeWidth={TRACK_WIDTH}
            strokeLinecap="butt"
            style={{ strokeDasharray: "1 1", strokeDashoffset: fillOffset }}
          />
          {ticks.map((t, i) => (
            <line
              key={i}
              x1={t.x1}
              y1={t.y1}
              x2={t.x2}
              y2={t.y2}
              stroke={token.bg}
              strokeWidth={1}
            />
          ))}
          <line
            x1={p50x1}
            y1={p50y1}
            x2={p50x2}
            y2={p50y2}
            stroke={ELECTRIC_BRIGHT}
            strokeWidth={2}
          />
        </svg>

        {numerals.map((n) => (
          <DialNumeral key={n.label} angle={n.angle} label={n.label} />
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
            className="whitespace-nowrap"
            style={{
              display: "inline-block",
              minWidth: "2ch",
              textAlign: "center",
              fontFamily: token.mono,
              fontSize: "clamp(22px, 15cqw, 36px)",
              fontWeight: 800,
              fontStyle: "italic",
              lineHeight: 1,
              color: token.textStrong,
              fontVariantNumeric: "tabular-nums",
              filter: `drop-shadow(0 0 8px ${ELECTRIC})`,
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
              lineHeight: 1,
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
