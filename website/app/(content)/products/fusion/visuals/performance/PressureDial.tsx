"use client";

import { useEffect, useId, type ReactNode } from "react";
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
import { gaugeArcPath } from "./gauge";
import { CX, CY, ELECTRIC, ELECTRIC_BRIGHT, ELECTRIC_DIM, VB } from "./hud";
import {
  CACHED_DOCUMENTS,
  CACHED_PLANS,
  CPU_IDLE_BAND,
  CPU_PRESSURE,
  MEMORY_IDLE_BAND,
  MEMORY_PRESSURE,
  formatCount,
} from "./data";

interface PressureDialProps {
  readonly active: boolean;
  readonly reduced: boolean;
}

const FACE_R = 128;
const ARC_R = 139;
const ARC_WIDTH = 7;
const GLOW_WIDTH = 12;
const SWEEP_MS = 1200;
const IDLE_MS = 5200;

const CPU_START = 145;
const CPU_END = 35;
const MEM_START = 215;
const MEM_END = 325;

const LABEL_STYLE = {
  fontSize: 11,
  lineHeight: 1,
  letterSpacing: "0.04em",
  color: token.textSecondary,
  fontFamily: token.mono,
} as const;

const COUNT_STYLE = {
  fontSize: "clamp(14px, 9cqw, 22px)",
  lineHeight: 1,
  fontWeight: 800,
  fontStyle: "italic",
  color: token.textStrong,
  fontFamily: token.mono,
  fontVariantNumeric: "tabular-nums",
} as const;

function PressureRow({
  label,
  value,
  color,
}: {
  readonly label: string;
  readonly value: MotionValue<string>;
  readonly color: string;
}) {
  return (
    <div
      aria-hidden="true"
      className="flex items-baseline whitespace-nowrap"
      style={{ gap: 6 }}
    >
      <span className="uppercase" style={LABEL_STYLE}>
        {label}
      </span>
      <motion.span
        style={{
          display: "inline-block",
          minWidth: "3ch",
          textAlign: "right",
          fontSize: 11,
          lineHeight: 1,
          fontWeight: 700,
          color,
          fontFamily: token.mono,
          fontVariantNumeric: "tabular-nums",
        }}
      >
        {value}
      </motion.span>
    </div>
  );
}

function CountRow({
  label,
  children,
}: {
  readonly label: string;
  readonly children: ReactNode;
}) {
  return (
    <div
      aria-hidden="true"
      className="flex flex-col items-center whitespace-nowrap"
      style={{ gap: 2 }}
    >
      <span className="uppercase" style={LABEL_STYLE}>
        {label}
      </span>
      <span style={COUNT_STYLE}>{children}</span>
    </div>
  );
}

function useArcValue(
  settle: number,
  idleBand: readonly [number, number],
  active: boolean,
  reduced: boolean,
): MotionValue<number> {
  const value = useMotionValue(reduced ? settle : 0);

  useEffect(() => {
    if (reduced) {
      value.set(settle);
      return;
    }
    if (!active) return;

    let cancelled = false;
    const sweep = animate(value, settle, {
      duration: SWEEP_MS / 1000,
      ease: ease.out,
    });
    let idle = sweep;
    sweep.then(() => {
      if (cancelled) return;
      idle = animate(value, [settle, idleBand[0], idleBand[1], settle], {
        duration: IDLE_MS / 1000,
        ease: ease.inOut,
        repeat: Infinity,
      });
    });

    return () => {
      cancelled = true;
      sweep.stop();
      idle.stop();
    };
  }, [active, reduced, settle, idleBand, value]);

  return value;
}

export function PressureDial({ active, reduced }: PressureDialProps) {
  const filterId = useId().replace(/:/g, "");
  const cpu = useArcValue(CPU_PRESSURE, CPU_IDLE_BAND, active, reduced);
  const mem = useArcValue(MEMORY_PRESSURE, MEMORY_IDLE_BAND, active, reduced);

  const cpuOffset = useTransform(cpu, (v) => 1 - v / 100);
  const memOffset = useTransform(mem, (v) => 1 - v / 100);
  const cpuLabel = useTransform(cpu, (v) => `${Math.round(v)}%`);
  const memLabel = useTransform(mem, (v) => `${Math.round(v)}%`);

  const arcs = [
    {
      key: "cpu",
      d: gaugeArcPath(CX, CY, ARC_R, CPU_START, CPU_END),
      color: ELECTRIC,
      offset: cpuOffset,
    },
    {
      key: "mem",
      d: gaugeArcPath(CX, CY, ARC_R, MEM_START, MEM_END),
      color: ELECTRIC_BRIGHT,
      offset: memOffset,
    },
  ];

  return (
    <div className="@container w-full" style={{ aspectRatio: "1 / 1" }}>
      <div style={{ position: "relative", width: "100%", height: "100%" }}>
        <svg
          viewBox={`0 0 ${VB} ${VB}`}
          width="100%"
          height="100%"
          style={{ display: "block", overflow: "visible" }}
          role="img"
          aria-label={`CPU pressure near ${CPU_PRESSURE}%, memory pressure near ${MEMORY_PRESSURE}%`}
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

          {arcs.map((arc) => (
            <g key={arc.key}>
              <path
                d={arc.d}
                fill="none"
                stroke={ELECTRIC_DIM}
                strokeWidth={ARC_WIDTH}
                strokeLinecap="butt"
              />
              <motion.path
                d={arc.d}
                pathLength={1}
                fill="none"
                stroke={arc.color}
                strokeWidth={GLOW_WIDTH}
                strokeLinecap="butt"
                opacity={0.55}
                filter={`url(#glow-${filterId})`}
                style={{ strokeDasharray: "1 1", strokeDashoffset: arc.offset }}
              />
              <motion.path
                d={arc.d}
                pathLength={1}
                fill="none"
                stroke={arc.color}
                strokeWidth={ARC_WIDTH}
                strokeLinecap="butt"
                style={{ strokeDasharray: "1 1", strokeDashoffset: arc.offset }}
              />
            </g>
          ))}
        </svg>

        <div
          role="group"
          aria-label={`${formatCount(CACHED_DOCUMENTS)} cached documents, ${formatCount(CACHED_PLANS)} cached operation plans`}
          className="flex flex-col items-center"
          style={{
            position: "absolute",
            left: "50%",
            top: "50%",
            transform: "translate(-50%, -50%)",
            gap: 4,
          }}
        >
          <PressureRow label="cpu" value={cpuLabel} color={ELECTRIC} />
          <CountRow label="cached docs">
            {formatCount(CACHED_DOCUMENTS)}
          </CountRow>
          <CountRow label="cached plans">{formatCount(CACHED_PLANS)}</CountRow>
          <PressureRow label="mem" value={memLabel} color={ELECTRIC_BRIGHT} />
        </div>
      </div>
    </div>
  );
}
