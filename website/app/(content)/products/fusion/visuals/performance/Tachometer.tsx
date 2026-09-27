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

import { fractionAngle, gaugeArcPath, polarPoint } from "./gauge";
import {
  GAUGE_MAX,
  IDLE_BAND,
  RED_ZONE_START,
  SETTLE_VALUE,
  formatOps,
} from "./data";

export interface TachometerProps {
  readonly active: boolean;
  readonly reduced: boolean;
}

const MARGIN = 14;
const BEZEL_R = 200;
const CX = BEZEL_R + MARGIN;
const MAX_W = 420;
const TRACK_R = 150;
const TRACK_WIDTH = 14;
const FILL_GLOW_WIDTH = TRACK_WIDTH + 8;
const TICK_OUT = 9;
const TICK_IN_MAJOR = 20;
const TICK_IN_MINOR = 9;
const NUMERAL_R = TRACK_R + 40;

const LIGHT_COUNT = 10;
const LIGHT_R = 4;
const LIGHT_GAP = 17;
const LIGHT_ROW_Y = 16;
const LIGHT_GREEN = 5;
const LIGHT_AMBER = 3;

const DIAL_TOP_Y = LIGHT_ROW_Y + LIGHT_R + 18;
const CY = DIAL_TOP_Y + BEZEL_R;
const W = CX * 2;
const H = CY + 16;

const READOUT_CX = CX;
const READOUT_CY = CY - 70;
const LEGEND_CX = CX;
const LEGEND_CY = CY - 16;

const SWEEP_MS = 1300;
const IDLE_MS = 4200;

const TICK_STEPS = 28;
const SCALE_MAX_LABEL = 7;

function shiftLightColor(index: number): string {
  if (index < LIGHT_GREEN) return token.cSuccess;
  if (index < LIGHT_GREEN + LIGHT_AMBER) return token.warning;
  return token.error;
}

function ShiftLight({
  index,
  x,
  y,
  fraction,
}: {
  index: number;
  x: number;
  y: number;
  fraction: MotionValue<number>;
}) {
  const threshold = (index + 1) / LIGHT_COUNT;
  const color = shiftLightColor(index);
  const opacity = useTransform(fraction, (f) => (f >= threshold ? 1 : 0.16));
  return (
    <motion.circle cx={x} cy={y} r={LIGHT_R} style={{ fill: color, opacity }} />
  );
}

export function Tachometer({ active, reduced }: TachometerProps) {
  const filterId = useId().replace(/:/g, "");
  const max = GAUGE_MAX;
  const redZoneStart = RED_ZONE_START;
  const settleValue = SETTLE_VALUE;
  const idleBand = IDLE_BAND;
  const unit = "ops/s";

  const value = useMotionValue(reduced ? settleValue : 0);
  const fraction = useTransform(value, (v) => v / max);
  const fillOffset = useTransform(fraction, (f) => 1 - f);
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

  const trackPath = gaugeArcPath(CX, CY, TRACK_R, 180, 0);
  const facePath = `${gaugeArcPath(CX, CY, BEZEL_R, 180, 0)} Z`;

  const ticks: {
    x1: number;
    y1: number;
    x2: number;
    y2: number;
    major: boolean;
    red: boolean;
  }[] = [];
  const numerals: { x: number; y: number; label: string; red: boolean }[] = [];
  const majorEvery = TICK_STEPS / SCALE_MAX_LABEL;
  for (let i = 0; i <= TICK_STEPS; i++) {
    const fractionAt = i / TICK_STEPS;
    const major = i % majorEvery === 0;
    const angle = fractionAngle(fractionAt);
    const red = fractionAt * max >= redZoneStart;
    const [x1, y1] = polarPoint(CX, CY, TRACK_R + TICK_OUT, angle);
    const [x2, y2] = polarPoint(
      CX,
      CY,
      TRACK_R - (major ? TICK_IN_MAJOR : TICK_IN_MINOR),
      angle,
    );
    ticks.push({ x1, y1, x2, y2, major, red });
    if (major) {
      const label = String(Math.round(fractionAt * SCALE_MAX_LABEL));
      const [nx, ny] = polarPoint(CX, CY, NUMERAL_R, angle);
      numerals.push({ x: nx, y: ny, label, red });
    }
  }

  const lightsRowWidth = (LIGHT_COUNT - 1) * LIGHT_GAP;
  const lightsLeft = CX - lightsRowWidth / 2;
  const lights: { x: number; y: number }[] = [];
  for (let i = 0; i < LIGHT_COUNT; i++) {
    lights.push({ x: lightsLeft + i * LIGHT_GAP, y: LIGHT_ROW_Y });
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
          aria-label={`Throughput gauge, near ${formatOps(settleValue)} ${unit} of ${formatOps(max)} scale`}
        >
          <defs>
            <filter
              id={`glow-${filterId}`}
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="5" />
            </filter>
            <linearGradient
              id={`sweep-${filterId}`}
              gradientUnits="userSpaceOnUse"
              x1={CX - TRACK_R}
              y1={CY}
              x2={CX + TRACK_R}
              y2={CY}
            >
              <stop offset="0%" stopColor={token.accent} />
              <stop offset="42%" stopColor={token.info} />
              <stop offset="74%" stopColor={token.pink} />
              <stop offset="88%" stopColor={token.error} />
              <stop offset="100%" stopColor={token.error} />
            </linearGradient>
          </defs>

          {lights.map((l, i) => (
            <ShiftLight key={i} index={i} x={l.x} y={l.y} fraction={fraction} />
          ))}

          <path
            d={facePath}
            fill={`color-mix(in srgb, ${token.bg} 60%, black)`}
            stroke={token.borderStrong}
            strokeWidth={1.5}
          />

          <path
            d={trackPath}
            fill="none"
            stroke={token.border}
            strokeWidth={TRACK_WIDTH}
            strokeLinecap="round"
            opacity={0.35}
          />
          <motion.path
            d={trackPath}
            pathLength={1}
            fill="none"
            stroke={`url(#sweep-${filterId})`}
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
            stroke={`url(#sweep-${filterId})`}
            strokeWidth={TRACK_WIDTH}
            strokeLinecap="round"
            style={{ strokeDasharray: "1 1", strokeDashoffset: fillOffset }}
          />
          {ticks.map((t, i) => (
            <line
              key={i}
              x1={t.x1}
              y1={t.y1}
              x2={t.x2}
              y2={t.y2}
              stroke={
                t.red
                  ? token.error
                  : t.major
                    ? token.textSecondary
                    : token.borderStrong
              }
              strokeWidth={t.major ? 2 : 1}
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
              fontSize={24}
              fontWeight={800}
              fontStyle="italic"
              style={{ fontVariantNumeric: "tabular-nums" }}
              fill={n.red ? token.error : token.textSecondary}
            >
              {n.label}
            </text>
          ))}
        </svg>

        <div
          aria-hidden="true"
          style={{
            position: "absolute",
            left: `${(LEGEND_CX / W) * 100}%`,
            top: `${(LEGEND_CY / H) * 100}%`,
            transform: "translate(-50%, -50%)",
            fontSize: 11,
            letterSpacing: "0.02em",
            whiteSpace: "nowrap",
            color: token.textSecondary,
            fontFamily: token.mono,
          }}
        >
          x1000 ops/s
        </div>

        <div
          role="img"
          aria-label={`${formatOps(settleValue)} ${unit}`}
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
              display: "inline-block",
              minWidth: "3ch",
              textAlign: "center",
              fontFamily: token.mono,
              fontSize: "clamp(22px, 10cqw, 46px)",
              lineHeight: 1,
              fontWeight: 700,
              fontStyle: "italic",
              letterSpacing: "-0.02em",
              color: token.textStrong,
              fontVariantNumeric: "tabular-nums",
              filter: `drop-shadow(0 0 6px ${token.cThroughput})`,
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
            {unit}
          </span>
        </div>
      </div>
    </div>
  );
}
