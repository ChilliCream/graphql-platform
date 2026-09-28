"use client";

import { useEffect, useId } from "react";
import {
  animate,
  motion,
  useMotionValue,
  useTransform,
  type MotionValue,
} from "motion/react";

import { token } from "@/src/nitro";
import { ease } from "@/src/nitro/lib/motion";

import { DialFace } from "./DialFace";
import { DialNumeral } from "./DialNumeral";
import { gaugeArcPath, polarPoint, sweepAngle } from "./gauge";
import { CX, CY, ELECTRIC, ELECTRIC_BRIGHT, VB } from "./hud";
import { OPS_IDLE_BAND, OPS_MAX, OPS_SETTLE, formatOps } from "./data";

interface CenterOpsDialProps {
  readonly active: boolean;
  readonly reduced: boolean;
}

const FACE_R = 86;
const SEGMENT_INNER = 91;
const SEGMENT_OUTER = 103;
const MAJOR_SEGMENT_INNER = 89;
const RING_R = (SEGMENT_INNER + SEGMENT_OUTER) / 2;
const SCALE_STEPS = 8;
const SEGMENTS_PER_STEP = 7;
const SEGMENT_STEPS = SCALE_STEPS * SEGMENTS_PER_STEP;
const ARC_START = 225;
const ARC_END = -45;
const SWEEP_SPAN = 16;
const SWEEP_MS = 1300;
const IDLE_MS = 4400;
const ROTATE_MS = 5200;

function Segment({
  index,
  fraction,
}: {
  readonly index: number;
  readonly fraction: MotionValue<number>;
}) {
  const threshold = index / SEGMENT_STEPS;
  const major = index % SEGMENTS_PER_STEP === 0;
  const angle = sweepAngle(threshold, ARC_START, ARC_END);
  const [x1, y1] = polarPoint(
    CX,
    CY,
    major ? MAJOR_SEGMENT_INNER : SEGMENT_INNER,
    angle,
  );
  const [x2, y2] = polarPoint(CX, CY, SEGMENT_OUTER, angle);
  const opacity = useTransform(fraction, (f) => (f >= threshold ? 1 : 0.18));
  return (
    <motion.line
      x1={x1}
      y1={y1}
      x2={x2}
      y2={y2}
      stroke={major ? ELECTRIC_BRIGHT : ELECTRIC}
      strokeWidth={3}
      strokeLinecap="round"
      style={{ opacity }}
    />
  );
}

export function CenterOpsDial({ active, reduced }: CenterOpsDialProps) {
  const filterId = useId().replace(/:/g, "");
  const value = useMotionValue(reduced ? OPS_SETTLE : 0);
  const fraction = useTransform(value, (v) => v / OPS_MAX);
  const readout = useTransform(value, formatOps);
  const rotate = useMotionValue(0);

  useEffect(() => {
    if (reduced) {
      value.set(OPS_SETTLE);
      return;
    }
    if (!active) return;

    let cancelled = false;
    const sweep = animate(value, OPS_SETTLE, {
      duration: SWEEP_MS / 1000,
      ease: ease.out,
    });
    let idle = sweep;
    sweep.then(() => {
      if (cancelled) return;
      idle = animate(
        value,
        [OPS_SETTLE, OPS_IDLE_BAND[0], OPS_IDLE_BAND[1], OPS_SETTLE],
        { duration: IDLE_MS / 1000, ease: ease.inOut, repeat: Infinity },
      );
    });

    return () => {
      cancelled = true;
      sweep.stop();
      idle.stop();
    };
  }, [active, reduced, value]);

  useEffect(() => {
    if (reduced || !active) return;
    const from = rotate.get() % 360;
    const spin = animate(rotate, [from, from + 360], {
      duration: ROTATE_MS / 1000,
      ease: ease.linear,
      repeat: Infinity,
    });
    return () => spin.stop();
  }, [active, reduced, rotate]);

  const highlightPath = gaugeArcPath(
    CX,
    CY,
    RING_R,
    90 + SWEEP_SPAN / 2,
    90 - SWEEP_SPAN / 2,
  );
  const segments = Array.from({ length: SEGMENT_STEPS + 1 }, (_, i) => i);
  const numerals = Array.from({ length: SCALE_STEPS + 1 }, (_, i) => ({
    angle: sweepAngle(i / SCALE_STEPS, ARC_START, ARC_END),
    label: i === 0 ? "0" : `${(i * OPS_MAX) / SCALE_STEPS / 1000}K`,
  }));

  return (
    <div className="@container w-full" style={{ aspectRatio: "1 / 1" }}>
      <div style={{ position: "relative", width: "100%", height: "100%" }}>
        <svg
          viewBox={`0 0 ${VB} ${VB}`}
          width="100%"
          height="100%"
          style={{ display: "block", overflow: "visible" }}
          role="img"
          aria-label={`Operations per second, near ${formatOps(OPS_SETTLE)} of ${formatOps(OPS_MAX)} scale`}
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

          {segments.map((i) => (
            <Segment key={i} index={i} fraction={fraction} />
          ))}

          <motion.g
            style={{
              rotate,
              transformOrigin: `${CX}px ${CY}px`,
              transformBox: "view-box",
            }}
          >
            <path
              d={highlightPath}
              fill="none"
              stroke={ELECTRIC_BRIGHT}
              strokeWidth={6}
              strokeLinecap="round"
              filter={`url(#glow-${filterId})`}
              opacity={0.85}
            />
          </motion.g>
        </svg>

        {numerals.map((n) => (
          <DialNumeral key={n.label} angle={n.angle} label={n.label} />
        ))}

        <div
          role="img"
          aria-label={`${formatOps(OPS_SETTLE)} operations per second`}
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
              fontFamily: token.mono,
              fontSize: "clamp(30px, 18cqw, 52px)",
              lineHeight: 1,
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
          <span
            aria-hidden="true"
            className="whitespace-nowrap uppercase"
            style={{
              marginTop: 3,
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
      </div>
    </div>
  );
}
