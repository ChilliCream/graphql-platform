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

const VB = 300;
const CX = VB / 2;
const CY = VB / 2;
const BEZEL_R = 148;
const FACE_R = 104;
const ARC_R = 118;
const ARC_WIDTH = 6;
const GLOW_WIDTH = ARC_WIDTH + 6;
const WORD_R = FACE_R - 26;
const VALUE_R = ARC_R + 29;
const SWEEP_MS = 1200;
const IDLE_MS = 5200;

const CPU_START = 200;
const CPU_END = 270;
const MEM_START = 270;
const MEM_END = 340;

const GLOW = token.info;
const GLOW_BRIGHT = `color-mix(in srgb, ${token.info} 65%, white)`;

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

  const cpuFraction = useTransform(cpu, (v) => v / 100);
  const memFraction = useTransform(mem, (v) => v / 100);
  const cpuOffset = useTransform(cpuFraction, (f) => 1 - f);
  const memOffset = useTransform(memFraction, (f) => 1 - f);
  const cpuLabel = useTransform(cpu, (v) => `${Math.round(v)}%`);
  const memLabel = useTransform(mem, (v) => `${Math.round(v)}%`);

  const cpuTrack = gaugeArcPath(CX, CY, ARC_R, CPU_START, CPU_END);
  const memTrack = gaugeArcPath(CX, CY, ARC_R, MEM_START, MEM_END);

  const cpuMidAngle = sweepAngle(0.5, CPU_START, CPU_END);
  const memMidAngle = sweepAngle(0.5, MEM_START, MEM_END);
  const [cpuWordX, cpuWordY] = polarPoint(CX, CY, WORD_R, cpuMidAngle);
  const [memWordX, memWordY] = polarPoint(CX, CY, WORD_R, memMidAngle);
  const [cpuValueX, cpuValueY] = polarPoint(CX, CY, VALUE_R, cpuMidAngle);
  const [memValueX, memValueY] = polarPoint(CX, CY, VALUE_R, memMidAngle);

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

          <circle
            cx={CX}
            cy={CY}
            r={FACE_R}
            fill={`color-mix(in srgb, ${token.bg} 55%, black)`}
            stroke={token.borderStrong}
            strokeWidth={1.25}
          />
          <circle
            cx={CX}
            cy={CY}
            r={BEZEL_R}
            fill="none"
            stroke={token.border}
            strokeWidth={1.25}
            opacity={0.5}
          />

          <path
            d={cpuTrack}
            fill="none"
            stroke={token.border}
            strokeWidth={ARC_WIDTH}
            strokeLinecap="round"
            opacity={0.3}
          />
          <motion.path
            d={cpuTrack}
            pathLength={1}
            fill="none"
            stroke={GLOW}
            strokeWidth={GLOW_WIDTH}
            strokeLinecap="round"
            opacity={0.45}
            filter={`url(#glow-${filterId})`}
            style={{ strokeDasharray: "1 1", strokeDashoffset: cpuOffset }}
          />
          <motion.path
            d={cpuTrack}
            pathLength={1}
            fill="none"
            stroke={GLOW}
            strokeWidth={ARC_WIDTH}
            strokeLinecap="round"
            style={{ strokeDasharray: "1 1", strokeDashoffset: cpuOffset }}
          />

          <path
            d={memTrack}
            fill="none"
            stroke={token.border}
            strokeWidth={ARC_WIDTH}
            strokeLinecap="round"
            opacity={0.3}
          />
          <motion.path
            d={memTrack}
            pathLength={1}
            fill="none"
            stroke={GLOW_BRIGHT}
            strokeWidth={GLOW_WIDTH}
            strokeLinecap="round"
            opacity={0.45}
            filter={`url(#glow-${filterId})`}
            style={{ strokeDasharray: "1 1", strokeDashoffset: memOffset }}
          />
          <motion.path
            d={memTrack}
            pathLength={1}
            fill="none"
            stroke={GLOW_BRIGHT}
            strokeWidth={ARC_WIDTH}
            strokeLinecap="round"
            style={{ strokeDasharray: "1 1", strokeDashoffset: memOffset }}
          />
        </svg>

        <div
          role="img"
          aria-label={`CPU pressure near ${CPU_PRESSURE}%`}
          style={{
            position: "absolute",
            left: `${(cpuWordX / VB) * 100}%`,
            top: `${(cpuWordY / VB) * 100}%`,
            transform: "translate(-50%, -50%)",
            whiteSpace: "nowrap",
          }}
        >
          <span
            aria-hidden="true"
            className="uppercase"
            style={{
              fontSize: 11,
              letterSpacing: "0.06em",
              color: token.textSecondary,
              fontFamily: token.mono,
            }}
          >
            cpu
          </span>
        </div>
        <div
          aria-hidden="true"
          style={{
            position: "absolute",
            left: `${(cpuValueX / VB) * 100}%`,
            top: `${(cpuValueY / VB) * 100}%`,
            transform: "translate(-50%, -50%)",
            whiteSpace: "nowrap",
          }}
        >
          <motion.span
            style={{
              fontSize: 11,
              fontWeight: 700,
              color: token.textStrong,
              fontFamily: token.mono,
              fontVariantNumeric: "tabular-nums",
            }}
          >
            {cpuLabel}
          </motion.span>
        </div>

        <div
          role="img"
          aria-label={`Memory pressure near ${MEMORY_PRESSURE}%`}
          style={{
            position: "absolute",
            left: `${(memWordX / VB) * 100}%`,
            top: `${(memWordY / VB) * 100}%`,
            transform: "translate(-50%, -50%)",
            whiteSpace: "nowrap",
          }}
        >
          <span
            aria-hidden="true"
            className="uppercase"
            style={{
              fontSize: 11,
              letterSpacing: "0.06em",
              color: token.textSecondary,
              fontFamily: token.mono,
            }}
          >
            mem
          </span>
        </div>
        <div
          aria-hidden="true"
          style={{
            position: "absolute",
            left: `${(memValueX / VB) * 100}%`,
            top: `${(memValueY / VB) * 100}%`,
            transform: "translate(-50%, -50%)",
            whiteSpace: "nowrap",
          }}
        >
          <motion.span
            style={{
              fontSize: 11,
              fontWeight: 700,
              color: token.textStrong,
              fontFamily: token.mono,
              fontVariantNumeric: "tabular-nums",
            }}
          >
            {memLabel}
          </motion.span>
        </div>

        <div
          role="group"
          aria-label={`${formatCount(CACHED_DOCUMENTS)} cached documents, ${formatCount(CACHED_PLANS)} cached operation plans`}
          style={{
            position: "absolute",
            left: "50%",
            top: "36%",
            transform: "translate(-50%, -50%)",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            gap: 8,
          }}
        >
          <div
            aria-hidden="true"
            style={{
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              gap: 1,
            }}
          >
            <span
              className="text-center whitespace-nowrap uppercase"
              style={{
                fontSize: 11,
                letterSpacing: "0.04em",
                color: token.textSecondary,
                fontFamily: token.mono,
              }}
            >
              cached docs
            </span>
            <span
              className="whitespace-nowrap"
              style={{
                fontSize: "clamp(14px, 8cqw, 18px)",
                fontWeight: 800,
                fontStyle: "italic",
                color: token.textStrong,
                fontFamily: token.mono,
                fontVariantNumeric: "tabular-nums",
              }}
            >
              {formatCount(CACHED_DOCUMENTS)}
            </span>
          </div>
          <div
            aria-hidden="true"
            style={{
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
            }}
          >
            <span
              className="text-center whitespace-nowrap uppercase"
              style={{
                fontSize: 11,
                letterSpacing: "0.04em",
                color: token.textSecondary,
                fontFamily: token.mono,
              }}
            >
              cached plans
            </span>
            <span
              className="whitespace-nowrap"
              style={{
                fontSize: "clamp(14px, 8cqw, 18px)",
                fontWeight: 800,
                fontStyle: "italic",
                color: token.textStrong,
                fontFamily: token.mono,
                fontVariantNumeric: "tabular-nums",
              }}
            >
              {formatCount(CACHED_PLANS)}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}
