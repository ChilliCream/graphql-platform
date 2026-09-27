"use client";

import { useEffect, useId, useRef } from "react";
import {
  animate,
  motion,
  useMotionValue,
  useTransform,
  type MotionValue,
} from "motion/react";

import { token } from "@/src/nitro";
import { Badge } from "@/src/nitro/primitives/Badge";
import { ease, useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { useElementMotion } from "../hooks";
import {
  fractionAngle,
  gaugeArcPath,
  needleRotation,
  polarPoint,
} from "./gauge";

export interface TachometerProps {
  readonly max: number;
  readonly redZoneStart: number;
  readonly settleValue: number;
  readonly idleBand: readonly [number, number];
  readonly unit: string;
  readonly cores: number;
}

const LIGHT_R = 126;
const MARGIN = 6;
const CX = LIGHT_R + MARGIN;
const W = CX * 2;
const H = 176;
const CY = 140;
const TRACK_R = 84;
const BEZEL_R = 112;
const NUMERAL_R = 100;
const NEEDLE_LEN = 70;
const HUB_R = 6;

const SWEEP_MS = 1300;
const IDLE_MS = 4200;

const TICK_STEPS = 14;
const SCALE_MAX_LABEL = 7;

const SHIFT_LIGHTS = 12;
const SHIFT_GREEN = 6;
const SHIFT_AMBER = 4;

const CORE_BAR_W = 5;
const CORE_BAR_GAP = 2;
const CORE_BAR_MAX_H = 14;
const CORE_BAR_MIN_H = 3;

function formatOps(n: number): string {
  return `${(n / 1000).toFixed(1)}K`;
}

function shiftLightColor(index: number): string {
  if (index < SHIFT_GREEN) return token.cSuccess;
  if (index < SHIFT_GREEN + SHIFT_AMBER) return token.warning;
  return token.error;
}

function coreLoadFactor(index: number, cores: number): number {
  const phase = (index / Math.max(1, cores)) * Math.PI * 2;
  // Rounded so Math.sin's last-bit drift between the server and browser JS
  // engines can't turn into an SSR/CSR attribute mismatch.
  return (
    Math.round((0.78 + 0.18 * Math.sin(phase * 1.7 + index)) * 1000) / 1000
  );
}

function ShiftLight({
  index,
  cx,
  cy,
  fraction,
}: {
  index: number;
  cx: number;
  cy: number;
  fraction: MotionValue<number>;
}) {
  const threshold = (index + 1) / SHIFT_LIGHTS;
  const color = shiftLightColor(index);
  const opacity = useTransform(fraction, (f) => (f >= threshold ? 1 : 0.16));
  return (
    <motion.circle cx={cx} cy={cy} r={3.4} style={{ fill: color, opacity }} />
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

export function Tachometer({
  max,
  redZoneStart,
  settleValue,
  idleBand,
  unit,
  cores,
}: TachometerProps) {
  const ref = useRef<HTMLDivElement>(null);
  const filterId = useId().replace(/:/g, "");
  const reduced = useReducedMotionPreference();
  const active = useElementMotion(ref);

  const value = useMotionValue(reduced ? settleValue : 0);
  const fraction = useTransform(value, (v) => v / max);
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

  const redZoneAngleStart = fractionAngle(redZoneStart / max);
  const trackPath = gaugeArcPath(CX, CY, TRACK_R, 180, 0);
  const redZonePath = gaugeArcPath(CX, CY, TRACK_R, redZoneAngleStart, 0);
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
  for (let i = 0; i <= TICK_STEPS; i++) {
    const fractionAt = i / TICK_STEPS;
    const major = i % 2 === 0;
    const angle = fractionAngle(fractionAt);
    const red = fractionAt * max >= redZoneStart;
    const [x1, y1] = polarPoint(CX, CY, TRACK_R + 8, angle);
    const [x2, y2] = polarPoint(CX, CY, TRACK_R - (major ? 13 : 6), angle);
    ticks.push({ x1, y1, x2, y2, major, red });
    if (major) {
      const label = String(Math.round(fractionAt * SCALE_MAX_LABEL));
      const [nx, ny] = polarPoint(CX, CY, NUMERAL_R, angle);
      numerals.push({ x: nx, y: ny, label, red });
    }
  }

  const lights: { x: number; y: number }[] = [];
  for (let i = 0; i < SHIFT_LIGHTS; i++) {
    const threshold = (i + 1) / SHIFT_LIGHTS;
    const [x, y] = polarPoint(CX, CY, LIGHT_R, fractionAngle(threshold));
    lights.push({ x, y });
  }

  const cores8 = Array.from({ length: cores });
  const coreBarsWidth = cores * CORE_BAR_W + (cores - 1) * CORE_BAR_GAP;
  const coreBarsLeft = CX - coreBarsWidth / 2;
  const coreBarsBaseY = CY - 46;

  return (
    <div
      ref={ref}
      className="relative flex w-full flex-col items-center"
      style={{ maxWidth: W }}
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
            <feGaussianBlur stdDeviation="3.2" />
          </filter>
        </defs>

        <path
          d={facePath}
          fill={token.card}
          stroke={token.borderStrong}
          strokeWidth={1.5}
        />

        {lights.map((l, i) => (
          <ShiftLight key={i} index={i} cx={l.x} cy={l.y} fraction={fraction} />
        ))}

        <path
          d={trackPath}
          fill="none"
          stroke={token.border}
          strokeWidth={10}
          strokeLinecap="butt"
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
            strokeWidth={t.major ? 1.6 : 1}
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
            fontWeight={700}
            style={{ fontVariantNumeric: "tabular-nums" }}
            fill={n.red ? token.error : token.textSecondary}
          >
            {n.label}
          </text>
        ))}

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
            <circle
              cx={-NEEDLE_LEN}
              cy={0}
              r={5}
              fill={token.cThroughput}
              opacity={0.55}
              filter={`url(#glow-${filterId})`}
            />
            <circle cx={-NEEDLE_LEN} cy={0} r={2.2} fill={token.textStrong} />
          </motion.g>
        </g>
        <circle cx={CX} cy={CY} r={HUB_R} fill={token.textStrong} />

        {cores8.map((_, i) => (
          <CoreBar
            key={i}
            index={i}
            cores={cores}
            x={coreBarsLeft + i * (CORE_BAR_W + CORE_BAR_GAP)}
            baseY={coreBarsBaseY}
            value={value}
            max={max}
          />
        ))}
      </svg>

      <div
        className="pointer-events-none absolute flex flex-col items-center gap-1"
        style={{ top: "56%", left: "50%", transform: "translate(-50%, 0)" }}
      >
        <div
          className="flex items-baseline gap-1"
          role="img"
          aria-label={`${formatOps(settleValue)} ${unit}`}
        >
          <motion.span
            aria-hidden="true"
            style={{
              display: "inline-block",
              minWidth: "4ch",
              textAlign: "right",
              fontFamily: token.mono,
              fontSize: 26,
              fontWeight: 700,
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
        <Badge size="sm" mono border={token.border} background={token.surface}>
          {cores} logical cores
        </Badge>
      </div>
    </div>
  );
}
