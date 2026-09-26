"use client";

import { useEffect, useRef } from "react";
import { animate, motion, useMotionValue } from "motion/react";

import { CountUp, token } from "@/src/nitro";
import { ease } from "@/src/nitro/lib/motion";
import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { useElementMotion } from "../hooks";
import { gaugeArcPath, needleRotation } from "./gauge";

export interface TachometerProps {
  /** Scale ceiling, in the gauge's unit. */
  readonly max: number;
  /** Where the red zone begins; the needle settles and idles below this. */
  readonly redZoneStart: number;
  /** Settled reading once the reveal sweep completes. */
  readonly settleValue: number;
  /** Idle fluctuation band around `settleValue`, both ends kept below `redZoneStart`. */
  readonly idleBand: readonly [number, number];
  readonly unit: string;
}

const W = 220;
const H = 122;
const CX = 110;
const CY = 108;
const TRACK_R = 92;
const NEEDLE_LEN = 78;
const HUB_R = 6;

const SWEEP_MS = 1300;
const IDLE_MS = 4200;

function formatOps(n: number): string {
  return `${(n / 1000).toFixed(1)}K`;
}

export function Tachometer({
  max,
  redZoneStart,
  settleValue,
  idleBand,
  unit,
}: TachometerProps) {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotionPreference();
  const active = useElementMotion(ref);

  const settleRotate = needleRotation(settleValue, max);
  const idleLowRotate = needleRotation(idleBand[0], max);
  const idleHighRotate = needleRotation(idleBand[1], max);
  const rotate = useMotionValue(reduced ? settleRotate : 0);

  useEffect(() => {
    if (reduced) {
      rotate.set(settleRotate);
      return;
    }
    if (!active) return;

    let cancelled = false;
    const sweep = animate(rotate, settleRotate, {
      duration: SWEEP_MS / 1000,
      ease: ease.out,
    });
    let idle = sweep;
    sweep.then(() => {
      if (cancelled) return;
      idle = animate(
        rotate,
        [settleRotate, idleLowRotate, idleHighRotate, settleRotate],
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
  }, [active, reduced, settleRotate, idleLowRotate, idleHighRotate, rotate]);

  const redZoneAngleStart = 180 - (redZoneStart / max) * 180;
  const trackPath = gaugeArcPath(CX, CY, TRACK_R, 180, 0);
  const redZonePath = gaugeArcPath(CX, CY, TRACK_R, redZoneAngleStart, 0);

  return (
    <div ref={ref} className="flex flex-col items-center gap-1">
      <svg
        viewBox={`0 0 ${W} ${H}`}
        width="100%"
        style={{ display: "block", maxWidth: 220, overflow: "visible" }}
        role="img"
        aria-label={`Throughput gauge, needle near ${formatOps(settleValue)} ${unit} of ${formatOps(max)} scale`}
      >
        <path
          d={trackPath}
          fill="none"
          stroke={token.border}
          strokeWidth={10}
          strokeLinecap="round"
        />
        <path
          d={redZonePath}
          fill="none"
          stroke={token.error}
          strokeWidth={10}
          strokeLinecap="butt"
          opacity={0.85}
        />
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
          </motion.g>
        </g>
        <circle cx={CX} cy={CY} r={HUB_R} fill={token.textStrong} />
      </svg>
      <div className="flex flex-col items-center">
        <CountUp
          value={settleValue}
          format={formatOps}
          once
          durationMs={SWEEP_MS}
          style={{ width: "auto", height: "auto" }}
        />
        <span
          className="text-[11px] whitespace-nowrap"
          style={{ color: token.textSecondary, fontFamily: token.mono }}
        >
          {unit}
        </span>
      </div>
    </div>
  );
}
