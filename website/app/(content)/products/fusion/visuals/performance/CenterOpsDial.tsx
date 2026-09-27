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

import { gaugeArcPath, polarPoint, sweepAngle } from "./gauge";
import { OPS_IDLE_BAND, OPS_MAX, OPS_SETTLE, formatOps } from "./data";

interface CenterOpsDialProps {
  readonly active: boolean;
  readonly reduced: boolean;
}

const VB = 300;
const CX = VB / 2;
const CY = VB / 2;
const BEZEL_R = 148;
const FACE_R = 104;
const RING_R = 124;
const TICK_OUTER = RING_R + 7;
const TICK_INNER = RING_R - 7;
const SEGMENT_COUNT = 56;
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
  const angle = sweepAngle(index / SEGMENT_COUNT, 90, 90 - 360);
  const [x1, y1] = polarPoint(CX, CY, TICK_INNER, angle);
  const [x2, y2] = polarPoint(CX, CY, TICK_OUTER, angle);
  const threshold = index / SEGMENT_COUNT;
  const opacity = useTransform(fraction, (f) => (f >= threshold ? 1 : 0.16));
  return (
    <motion.line
      x1={x1}
      y1={y1}
      x2={x2}
      y2={y2}
      stroke={token.accent}
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
    const spin = animate(rotate, 360, {
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
  const segments = Array.from({ length: SEGMENT_COUNT }, (_, i) => i);

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

          <circle
            cx={CX}
            cy={CY}
            r={FACE_R}
            fill={`color-mix(in srgb, ${token.bg} 55%, black)`}
            stroke={token.borderStrong}
            strokeWidth={1.5}
          />
          <circle
            cx={CX}
            cy={CY}
            r={BEZEL_R}
            fill="none"
            stroke={token.border}
            strokeWidth={1.5}
            opacity={0.5}
          />

          {segments.map((i) => (
            <Segment key={i} index={i} fraction={fraction} />
          ))}

          <motion.g style={{ rotate, transformOrigin: `${CX}px ${CY}px` }}>
            <path
              d={highlightPath}
              fill="none"
              stroke={token.accentHover}
              strokeWidth={6}
              strokeLinecap="round"
              filter={`url(#glow-${filterId})`}
              opacity={0.85}
            />
          </motion.g>
        </svg>

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
            style={{
              display: "inline-block",
              minWidth: "3ch",
              textAlign: "center",
              fontFamily: token.mono,
              fontSize: "clamp(42px, 17cqw, 58px)",
              lineHeight: 1,
              fontWeight: 700,
              fontStyle: "italic",
              letterSpacing: "-0.02em",
              color: token.textStrong,
              fontVariantNumeric: "tabular-nums",
              filter: `drop-shadow(0 0 8px ${token.accent})`,
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
