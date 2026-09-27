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
import { Badge } from "@/src/nitro/primitives/Badge";
import { ease } from "@/src/nitro/lib/motion";

import {
  fractionAngle,
  gaugeArcPath,
  needleRotation,
  polarPoint,
} from "./gauge";
import {
  CORES,
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
const MAX_W = 440;
const TRACK_R = 150;
const TRACK_WIDTH = 14;
const TICK_OUT = 9;
const TICK_IN_MAJOR = 20;
const TICK_IN_MINOR = 9;
const NUMERAL_R = TRACK_R + 40;
const NEEDLE_LEN = 128;
const HUB_R = 11;

const LIGHT_COUNT = 10;
const LIGHT_R = 4;
const LIGHT_GAP = 17;
const LIGHT_ROW_Y = 16;
const LIGHT_GREEN = 5;
const LIGHT_AMBER = 3;

const DIAL_TOP_Y = LIGHT_ROW_Y + LIGHT_R + 18;
const CY = DIAL_TOP_Y + BEZEL_R;
const W = CX * 2;
const LEGEND_X = CX - 58;
const LEGEND_Y = CY + 22;
const H = LEGEND_Y + 18;

const SWEEP_MS = 1300;
const IDLE_MS = 4200;

const TICK_STEPS = 28;
const SCALE_MAX_LABEL = 7;

const CORE_BAR_W = 5;
const CORE_BAR_GAP = 2;
const CORE_BAR_MAX_H = 16;
const CORE_BAR_MIN_H = 3;

function coreLoadFactor(index: number, cores: number): number {
  const phase = (index / Math.max(1, cores)) * Math.PI * 2;
  // Rounded to keep server/client sin() drift out of the SSR/CSR markup.
  return (
    Math.round((0.78 + 0.18 * Math.sin(phase * 1.7 + index)) * 1000) / 1000
  );
}

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

function CoreBar({
  index,
  cores,
  x,
  baseY,
  value,
  max,
}: {
  index: number;
  cores: number;
  x: number;
  baseY: number;
  value: MotionValue<number>;
  max: number;
}) {
  const factor = coreLoadFactor(index, cores);
  const height = useTransform(value, (v) => {
    const load = Math.min(1, Math.max(0, (v / max) * factor));
    return CORE_BAR_MIN_H + load * (CORE_BAR_MAX_H - CORE_BAR_MIN_H);
  });
  const y = useTransform(height, (h) => baseY - h);
  return (
    <motion.rect
      x={x}
      width={CORE_BAR_W}
      height={height}
      y={y}
      rx={1}
      style={{ fill: token.cThroughput, opacity: 0.75 }}
    />
  );
}

export function Tachometer({ active, reduced }: TachometerProps) {
  const filterId = useId().replace(/:/g, "");
  const max = GAUGE_MAX;
  const redZoneStart = RED_ZONE_START;
  const settleValue = SETTLE_VALUE;
  const idleBand = IDLE_BAND;
  const unit = "ops/s";
  const cores = CORES;

  const value = useMotionValue(reduced ? settleValue : 0);
  const fraction = useTransform(value, (v) => v / max);
  const rotate = useTransform(value, (v) => needleRotation(v, max));
  const readout = useTransform(value, formatOps);
  const tipColor = useTransform(value, (v) =>
    v >= redZoneStart ? token.error : token.textStrong,
  );

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

  const cores8 = Array.from({ length: cores });
  const coreBarsWidth = cores * CORE_BAR_W + (cores - 1) * CORE_BAR_GAP;

  return (
    <div
      className="flex w-full flex-col items-center"
      style={{ maxWidth: MAX_W }}
    >
      <svg
        viewBox={`0 0 ${W} ${H}`}
        width="100%"
        style={{ display: "block", overflow: "visible" }}
        role="img"
        aria-label={`Throughput gauge, needle near ${formatOps(settleValue)} ${unit} of ${formatOps(max)} scale, ${cores} logical cores`}
      >
        <defs>
          <filter
            id={`glow-${filterId}`}
            x="-150%"
            y="-150%"
            width="400%"
            height="400%"
          >
            <feGaussianBlur stdDeviation="3.4" />
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
          fill={token.card}
          stroke={token.borderStrong}
          strokeWidth={1.5}
        />

        <path
          d={trackPath}
          fill="none"
          stroke={`url(#sweep-${filterId})`}
          strokeWidth={TRACK_WIDTH}
          strokeLinecap="butt"
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
        <text
          x={LEGEND_X}
          y={LEGEND_Y}
          textAnchor="middle"
          dominantBaseline="middle"
          fontFamily={token.mono}
          fontSize={11}
          letterSpacing="0.02em"
          fill={token.textSecondary}
        >
          x1000 ops/s
        </text>

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
              stroke={token.textStrong}
              strokeWidth={4}
              strokeLinecap="round"
              opacity={0.85}
            />
            <motion.circle
              cx={-NEEDLE_LEN}
              cy={0}
              r={8}
              style={{ fill: tipColor, opacity: 0.55 }}
              filter={`url(#glow-${filterId})`}
            />
            <motion.circle
              cx={-NEEDLE_LEN}
              cy={0}
              r={3.5}
              style={{ fill: tipColor }}
            />
          </motion.g>
        </g>
        <circle cx={CX} cy={CY} r={HUB_R} fill={token.textStrong} />
      </svg>

      <div className="mt-2 flex flex-col items-center gap-1.5">
        <div
          className="flex flex-col items-center"
          role="img"
          aria-label={`${formatOps(settleValue)} ${unit}`}
        >
          <motion.span
            aria-hidden="true"
            style={{
              display: "inline-block",
              minWidth: "3ch",
              textAlign: "center",
              fontFamily: token.mono,
              fontSize: 44,
              lineHeight: 1,
              fontWeight: 700,
              letterSpacing: "-0.02em",
              color: token.textStrong,
              fontVariantNumeric: "tabular-nums",
            }}
          >
            {readout}
          </motion.span>
          <span
            aria-hidden="true"
            className="mt-0.5 text-[11px] whitespace-nowrap"
            style={{ color: token.textSecondary, fontFamily: token.mono }}
          >
            {unit}
          </span>
        </div>
        <Badge size="sm" mono border={token.border} background={token.surface}>
          {cores} logical cores
        </Badge>
        <svg
          width={coreBarsWidth}
          height={CORE_BAR_MAX_H}
          viewBox={`0 0 ${coreBarsWidth} ${CORE_BAR_MAX_H}`}
          style={{ display: "block" }}
          aria-hidden="true"
        >
          {cores8.map((_, i) => (
            <CoreBar
              key={i}
              index={i}
              cores={cores}
              x={i * (CORE_BAR_W + CORE_BAR_GAP)}
              baseY={CORE_BAR_MAX_H}
              value={value}
              max={max}
            />
          ))}
        </svg>
      </div>
    </div>
  );
}
