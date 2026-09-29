"use client";

import { useId } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";

import { token } from "@/src/nitro";

import { DialFace } from "./DialFace";
import { DialNumeral } from "./DialNumeral";
import { LATENCY_DOMAIN, LATENCY_MAX, LATENCY_P50, formatMs } from "./data";
import { gaugeArcPath, polarPoint, sweepAngle } from "./gauge";
import {
  CLUSTER,
  CX,
  CY,
  ELECTRIC,
  ELECTRIC_BRIGHT,
  ELECTRIC_DIM,
  LABEL_COLOR,
  VB,
  type Arc,
} from "./hud";
import type { TelemetryMotion } from "./useTelemetryClock";
import { TrendChart, TrendLine } from "./TrendLine";

interface LatencyDialProps {
  readonly telemetry: TelemetryMotion;
}

interface LatencyScaleProps {
  readonly arc: Arc;
  readonly fillOffset: MotionValue<number>;
  readonly glowId: string;
}

interface LatencyNumeralsProps {
  readonly arc: Arc;
}

const FACE_R = 85;
const TRACK_R = 140;
const TRACK_WIDTH = 6;
const FILL_GLOW_WIDTH = TRACK_WIDTH * 2;
const TICK_REACH = TRACK_WIDTH / 2;
const NUMERAL_EDGE = TRACK_R - TICK_REACH;
const P50_REACH = TICK_REACH + 0.5;
const TICK_STEPS = 8;
const MAJOR_EVERY = 2;

const ROW_ONLY = "hidden @min-[504px]/hud:block";
const STACK_ONLY = "@min-[504px]/hud:hidden";

function LatencyScale({ arc, fillOffset, glowId }: LatencyScaleProps) {
  const trackPath = gaugeArcPath(CX, CY, TRACK_R, arc.start, arc.end);
  const p50Angle = sweepAngle(LATENCY_P50 / LATENCY_MAX, arc.start, arc.end);
  const [p50x1, p50y1] = polarPoint(CX, CY, TRACK_R + P50_REACH, p50Angle);
  const [p50x2, p50y2] = polarPoint(CX, CY, TRACK_R - P50_REACH, p50Angle);

  return (
    <>
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
        filter={`url(#${glowId})`}
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
      {Array.from({ length: TICK_STEPS + 1 }, (_, i) => {
        const angle = sweepAngle(i / TICK_STEPS, arc.start, arc.end);
        const [x1, y1] = polarPoint(CX, CY, TRACK_R + TICK_REACH, angle);
        const [x2, y2] = polarPoint(CX, CY, TRACK_R - TICK_REACH, angle);
        return (
          <line
            key={i}
            x1={x1}
            y1={y1}
            x2={x2}
            y2={y2}
            stroke={token.bg}
            strokeWidth={1}
          />
        );
      })}
      <line
        x1={p50x1}
        y1={p50y1}
        x2={p50x2}
        y2={p50y2}
        stroke={ELECTRIC_BRIGHT}
        strokeWidth={2}
      />
    </>
  );
}

function LatencyNumerals({ arc }: LatencyNumeralsProps) {
  const stops = TICK_STEPS / MAJOR_EVERY;
  return (
    <>
      {Array.from({ length: stops + 1 }, (_, i) => (
        <DialNumeral
          key={i}
          angle={sweepAngle(i / stops, arc.start, arc.end)}
          label={String(Math.round((i / stops) * LATENCY_MAX))}
          edge={NUMERAL_EDGE}
        />
      ))}
    </>
  );
}

export function LatencyDial({ telemetry }: LatencyDialProps) {
  const filterId = useId().replace(/:/g, "");
  const glowId = `glow-${filterId}`;
  const { latency } = telemetry;
  const readout = useTransform(latency, formatMs);
  const fillOffset = useTransform(latency, (v) => 1 - v / LATENCY_MAX);
  const headTop = useTransform(latency, (v) => {
    const [lo, hi] = LATENCY_DOMAIN;
    const t = Math.min(1, Math.max(0, (v - lo) / (hi - lo)));
    return `${(1 - t) * 100}%`;
  });

  return (
    <div className="@container w-full" style={{ aspectRatio: "1 / 1" }}>
      <div style={{ position: "relative", width: "100%", height: "100%" }}>
        <svg
          viewBox={`0 0 ${VB} ${VB}`}
          width="100%"
          height="100%"
          style={{ display: "block", overflow: "visible" }}
          role="img"
          aria-label="Latency gauge, p95 between 9 and 13 ms, p50 mark at 7 ms, on a 40 ms scale"
        >
          <defs>
            <filter id={glowId} x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="4" />
            </filter>
          </defs>

          <DialFace faceRadius={FACE_R} />

          <g className={ROW_ONLY}>
            <LatencyScale
              arc={CLUSTER.row.latencyScale}
              fillOffset={fillOffset}
              glowId={glowId}
            />
          </g>
          <g className={STACK_ONLY}>
            <LatencyScale
              arc={CLUSTER.stack.latencyScale}
              fillOffset={fillOffset}
              glowId={glowId}
            />
          </g>
        </svg>

        <div className={`absolute inset-0 ${ROW_ONLY}`}>
          <LatencyNumerals arc={CLUSTER.row.latencyScale} />
        </div>
        <div className={`absolute inset-0 ${STACK_ONLY}`}>
          <LatencyNumerals arc={CLUSTER.stack.latencyScale} />
        </div>

        <div
          aria-hidden="true"
          className="absolute flex flex-col items-center"
          style={{
            left: "50%",
            top: "50%",
            transform: "translate(-50%, -50%)",
            gap: 1,
          }}
        >
          <span
            className="whitespace-nowrap uppercase"
            style={{
              fontSize: 11,
              lineHeight: 1,
              color: LABEL_COLOR,
              fontFamily: token.mono,
            }}
          >
            latency
          </span>
          <motion.span
            className="whitespace-nowrap"
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
              filter: `drop-shadow(0 0 8px ${ELECTRIC})`,
            }}
          >
            {readout}
          </motion.span>
          <span
            className="whitespace-nowrap"
            style={{
              fontSize: 11,
              lineHeight: 1,
              color: token.textSecondary,
              fontFamily: token.mono,
            }}
          >
            ms p95
          </span>
          <div
            className="relative"
            style={{
              width: "clamp(36px, 34cqw, 84px)",
              height: "clamp(12px, 9cqw, 26px)",
            }}
          >
            <TrendChart label="Latency over the last seconds, in milliseconds">
              <TrendLine
                values={telemetry.latencyHistory}
                domain={LATENCY_DOMAIN}
                color={token.chLatency}
              />
            </TrendChart>
            <motion.span
              className="absolute right-0"
              style={{
                top: headTop,
                width: 5,
                height: 5,
                marginTop: -2.5,
                marginRight: -2.5,
                borderRadius: 999,
                background: token.chLatency,
              }}
            />
          </div>
        </div>
      </div>
    </div>
  );
}
