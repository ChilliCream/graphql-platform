"use client";

import { useId, type CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";

import { token } from "@/src/nitro";

import { DialFace } from "./DialFace";
import { CORE_COUNT, formatPercent } from "./data";
import { gaugeArcPath } from "./gauge";
import {
  CLUSTER,
  CX,
  CY,
  ELECTRIC,
  ELECTRIC_BRIGHT,
  ELECTRIC_DIM,
  VB,
  type Arc,
} from "./hud";
import type { TelemetryMotion } from "./useTelemetryClock";

interface PressureDialProps {
  readonly telemetry: TelemetryMotion;
}

interface PressureArcsProps {
  readonly cpu: Arc;
  readonly mem: Arc;
  readonly cpuOffset: MotionValue<number>;
  readonly memOffset: MotionValue<number>;
  readonly glowId: string;
}

interface RimArcProps {
  readonly arc: Arc;
  readonly color: string;
  readonly offset: MotionValue<number>;
  readonly glowId: string;
}

interface ReadRowProps {
  readonly label: string;
  readonly value: MotionValue<string>;
  readonly color: string;
}

interface CoreBarProps {
  readonly index: number;
  readonly cores: MotionValue<readonly number[]>;
}

const FACE_R = 128;
const ARC_R = 139;
const ARC_WIDTH = 7;
const GLOW_WIDTH = 12;

const ROW_ONLY = "hidden @min-[464px]/hud:block";
const STACK_ONLY = "@min-[464px]/hud:hidden";

const CONTENT_WIDTH = "clamp(50px, 36cqw, 92px)";
const SHIFT_CQW = 4;

const cqw = (n: number) => `${(n * SHIFT_CQW).toFixed(2)}cqw`;

const SHIFT_VARS = {
  "--stack-dx": cqw(CLUSTER.stack.pressureShift.x),
  "--stack-dy": cqw(CLUSTER.stack.pressureShift.y),
  "--row-dx": cqw(CLUSTER.row.pressureShift.x),
  "--row-dy": cqw(CLUSTER.row.pressureShift.y),
} as CSSProperties;

function RimArc({ arc, color, offset, glowId }: RimArcProps) {
  const d = gaugeArcPath(CX, CY, ARC_R, arc.start, arc.end);
  return (
    <g>
      <path
        d={d}
        fill="none"
        stroke={ELECTRIC_DIM}
        strokeWidth={ARC_WIDTH}
        strokeLinecap="butt"
      />
      <motion.path
        d={d}
        pathLength={1}
        fill="none"
        stroke={color}
        strokeWidth={GLOW_WIDTH}
        strokeLinecap="butt"
        opacity={0.55}
        filter={`url(#${glowId})`}
        style={{ strokeDasharray: "1 1", strokeDashoffset: offset }}
      />
      <motion.path
        d={d}
        pathLength={1}
        fill="none"
        stroke={color}
        strokeWidth={ARC_WIDTH}
        strokeLinecap="butt"
        style={{ strokeDasharray: "1 1", strokeDashoffset: offset }}
      />
    </g>
  );
}

function PressureArcs({
  cpu,
  mem,
  cpuOffset,
  memOffset,
  glowId,
}: PressureArcsProps) {
  return (
    <>
      <RimArc arc={cpu} color={ELECTRIC} offset={cpuOffset} glowId={glowId} />
      <RimArc
        arc={mem}
        color={ELECTRIC_BRIGHT}
        offset={memOffset}
        glowId={glowId}
      />
    </>
  );
}

function ReadRow({ label, value, color }: ReadRowProps) {
  return (
    <div
      className="flex items-baseline justify-between whitespace-nowrap"
      style={{ width: CONTENT_WIDTH, gap: 4 }}
    >
      <span
        className="uppercase"
        style={{
          fontSize: 11,
          lineHeight: 1,
          letterSpacing: "0.04em",
          color: token.textSecondary,
          fontFamily: token.mono,
        }}
      >
        {label}
      </span>
      <motion.span
        style={{
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

function CoreBar({ index, cores }: CoreBarProps) {
  const height = useTransform(
    cores,
    (list) => `${Math.max(4, list[index] ?? 0)}%`,
  );
  return (
    <div
      className="relative h-full flex-1"
      style={{ background: ELECTRIC_DIM, borderRadius: 1 }}
    >
      <motion.div
        className="absolute inset-x-0 bottom-0"
        style={{ height, background: ELECTRIC, borderRadius: 1 }}
      />
    </div>
  );
}

export function PressureDial({ telemetry }: PressureDialProps) {
  const filterId = useId().replace(/:/g, "");
  const glowId = `glow-${filterId}`;
  const { cores, cpuTotal, memory } = telemetry;
  const cpuOffset = useTransform(cpuTotal, (v) => 1 - v / 100);
  const memOffset = useTransform(memory, (v) => 1 - v / 100);
  const cpuLabel = useTransform(cpuTotal, formatPercent);
  const memLabel = useTransform(memory, formatPercent);
  const memWidth = useTransform(
    memory,
    (v) => `${Math.min(100, Math.max(0, v))}%`,
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
          aria-label="CPU pressure across eight cores and memory pressure, calm in normal traffic and near 90 and 84 percent in a burst"
        >
          <defs>
            <filter id={glowId} x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="4" />
            </filter>
          </defs>

          <DialFace faceRadius={FACE_R} />

          <g className={ROW_ONLY}>
            <PressureArcs
              {...CLUSTER.row.pressureArcs}
              cpuOffset={cpuOffset}
              memOffset={memOffset}
              glowId={glowId}
            />
          </g>
          <g className={STACK_ONLY}>
            <PressureArcs
              {...CLUSTER.stack.pressureArcs}
              cpuOffset={cpuOffset}
              memOffset={memOffset}
              glowId={glowId}
            />
          </g>
        </svg>

        <div
          aria-hidden="true"
          className="absolute flex flex-col items-center [--dx:var(--stack-dx)] [--dy:var(--stack-dy)] @min-[464px]/hud:[--dx:var(--row-dx)] @min-[464px]/hud:[--dy:var(--row-dy)]"
          style={{
            left: "50%",
            top: "50%",
            transform:
              "translate(calc(-50% + var(--dx)), calc(-50% + var(--dy)))",
            gap: 3,
            ...SHIFT_VARS,
          }}
        >
          <ReadRow label="cpu" value={cpuLabel} color={ELECTRIC} />
          <div
            className="flex items-end"
            style={{
              width: CONTENT_WIDTH,
              height: "clamp(18px, 12cqw, 34px)",
              gap: 2,
            }}
          >
            {Array.from({ length: CORE_COUNT }, (_, i) => (
              <CoreBar key={i} index={i} cores={cores} />
            ))}
          </div>
          <div style={{ height: 2 }} />
          <ReadRow label="mem" value={memLabel} color={ELECTRIC_BRIGHT} />
          <div
            className="overflow-hidden"
            style={{
              width: CONTENT_WIDTH,
              height: 5,
              borderRadius: 999,
              background: ELECTRIC_DIM,
            }}
          >
            <motion.div
              style={{
                width: memWidth,
                height: "100%",
                borderRadius: 999,
                background: ELECTRIC_BRIGHT,
              }}
            />
          </div>
        </div>
      </div>
    </div>
  );
}
