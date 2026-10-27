"use client";

import { motion, useTransform, type MotionValue } from "motion/react";

import { token } from "@/src/nitro";

import { DialFace } from "./DialFace";
import { DialNumeral } from "./DialNumeral";
import { OPS_BURST_LABEL, OPS_MAX, OPS_RED_START, formatOps } from "./data";
import { polarPoint, sweepAngle } from "./gauge";
import {
  CONTENT_GAP,
  CX,
  CY,
  DANGER,
  ELECTRIC,
  ELECTRIC_BRIGHT,
  OPS_NUMERAL_COLOR,
  READOUT_FONT,
  READOUT_LINE_HEIGHT,
  VB,
} from "./hud";
import { OpsScreen } from "./OpsScreen";
import type { TelemetryMotion } from "./useTelemetryClock";

interface CenterOpsDialProps {
  readonly telemetry: TelemetryMotion;
}

interface SegmentProps {
  readonly index: number;
  readonly fraction: MotionValue<number>;
}

const FACE_R = 96;
const SEGMENT_INNER = 130;
const SEGMENT_OUTER = 141;
const MAJOR_SEGMENT_INNER = 127;
const SEGMENT_WIDTH = 3;
const NUMERAL_EDGE = MAJOR_SEGMENT_INNER - SEGMENT_WIDTH / 2;
const SCALE_STEPS = 7;
const SEGMENTS_PER_STEP = 8;
const SEGMENT_STEPS = SCALE_STEPS * SEGMENTS_PER_STEP;
const ARC_START = 210;
const ARC_END = -30;

function Segment({ index, fraction }: SegmentProps) {
  const threshold = index / SEGMENT_STEPS;
  const red = index * (OPS_MAX / SEGMENT_STEPS) >= OPS_RED_START;
  const major = index % SEGMENTS_PER_STEP === 0;
  const angle = sweepAngle(threshold, ARC_START, ARC_END);
  const [x1, y1] = polarPoint(
    CX,
    CY,
    major ? MAJOR_SEGMENT_INNER : SEGMENT_INNER,
    angle,
  );
  const [x2, y2] = polarPoint(CX, CY, SEGMENT_OUTER, angle);
  const unlit = red ? 0.3 : 0.18;
  const opacity = useTransform(fraction, (f) => (f >= threshold ? 1 : unlit));
  return (
    <motion.line
      x1={x1}
      y1={y1}
      x2={x2}
      y2={y2}
      stroke={red ? DANGER : major ? ELECTRIC_BRIGHT : ELECTRIC}
      strokeWidth={SEGMENT_WIDTH}
      strokeLinecap="round"
      style={{ opacity }}
    />
  );
}

const SEGMENTS = Array.from({ length: SEGMENT_STEPS + 1 }, (_, i) => i);
const NUMERALS = Array.from({ length: SCALE_STEPS + 1 }, (_, i) => ({
  angle: sweepAngle(i / SCALE_STEPS, ARC_START, ARC_END),
  label: i === 0 ? "0" : `${(i * OPS_MAX) / SCALE_STEPS / 1000}K`,
}));

export function CenterOpsDial({ telemetry }: CenterOpsDialProps) {
  const { ops } = telemetry;
  const fraction = useTransform(ops, (v) => v / OPS_MAX);
  const readout = useTransform(ops, formatOps);
  const phase = useTransform(ops, (v): string =>
    v >= OPS_BURST_LABEL ? "BURST" : "NORMAL",
  );
  const phaseColor = useTransform(ops, (v) =>
    v >= OPS_BURST_LABEL ? ELECTRIC_BRIGHT : token.textSecondary,
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
          aria-label="Operations per second, about 1K in normal traffic and about 5.5K in a burst, on a 7K scale with a red zone from 5.5K"
        >
          <DialFace faceRadius={FACE_R} />

          {SEGMENTS.map((i) => (
            <Segment key={i} index={i} fraction={fraction} />
          ))}
        </svg>

        {NUMERALS.map((n) => (
          <DialNumeral
            key={n.label}
            angle={n.angle}
            label={n.label}
            edge={NUMERAL_EDGE}
            color={OPS_NUMERAL_COLOR}
          />
        ))}

        <div
          className="absolute flex flex-col items-center"
          style={{
            left: "50%",
            top: "50%",
            transform: "translate(-50%, -50%)",
            gap: CONTENT_GAP,
          }}
        >
          <div
            aria-hidden="true"
            className="flex flex-col items-center"
            style={{ gap: 3 }}
          >
            <motion.span
              className="whitespace-nowrap"
              style={{
                display: "inline-block",
                minWidth: "7ch",
                textAlign: "center",
                fontSize: 11,
                lineHeight: 1,
                letterSpacing: "0.08em",
                color: phaseColor,
                fontFamily: token.mono,
              }}
            >
              {phase}
            </motion.span>
            <span
              className="whitespace-nowrap uppercase"
              style={{
                fontSize: 11,
                lineHeight: 1,
                letterSpacing: "0.08em",
                color: token.textSecondary,
                fontFamily: token.mono,
              }}
            >
              ops/s
            </span>
          </div>

          <motion.span
            aria-hidden="true"
            className="whitespace-nowrap"
            style={{
              fontFamily: token.mono,
              fontSize: READOUT_FONT,
              lineHeight: READOUT_LINE_HEIGHT,
              fontWeight: 700,
              fontStyle: "italic",
              letterSpacing: "-0.02em",
              color: token.textStrong,
              fontVariantNumeric: "tabular-nums",
              filter: `drop-shadow(0 0 10px ${ELECTRIC})`,
            }}
          >
            {readout}
          </motion.span>

          <OpsScreen telemetry={telemetry} />
        </div>
      </div>
    </div>
  );
}
