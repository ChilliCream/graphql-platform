"use client";

import type { AnimHelpers, Visual } from "./anim";
import { easeInOutCubic, Envelope, ramp } from "./anim";

const TEAL = "#5eead4";
const GREEN = "#8fd6a0";
const INK = "#f5f0ea";

const W = 1000;
const LANE_Y = 246;

const ENTRY_TOTAL = W + 118;

const OUT_SEAM_T = 34727;
const OUT_V0 = 0.402;
const OUT_ACCEL = 0.00032;

const CHEV_L = [850, 670, 460, 220] as const;
const CHEV_R = [150, 330, 540, 780] as const;

function chevronD(x: number): string {
  return `M${x - 7} ${LANE_Y - 8} L${x + 2} ${LANE_Y} L${x - 7} ${LANE_Y + 8}`;
}

interface FlankLayerProps {
  readonly side: "left" | "right";
  readonly set: Visual["set"];
}

export function FlankLayer({ side, set }: FlankLayerProps) {
  const left = side === "left";
  const s = left ? "L" : "R";
  const color = left ? TEAL : GREEN;
  const chevrons = left ? CHEV_L : CHEV_R;
  return (
    <div
      className={
        "pointer-events-none absolute inset-y-0 hidden w-[max(0px,calc((100%-1480px)/2))] sm:block " +
        (left ? "left-0" : "right-0")
      }
    >
      <svg
        viewBox={`0 76 ${W} 624`}
        preserveAspectRatio={left ? "xMaxYMid slice" : "xMinYMid slice"}
        className="h-full w-full"
      >
        <defs>
          <linearGradient
            id={`fjlane-${s}`}
            gradientUnits="userSpaceOnUse"
            x1={left ? 0 : W}
            y1={0}
            x2={left ? W : 0}
            y2={0}
          >
            <stop offset="0" stopColor={INK} stopOpacity="0.02" />
            <stop offset="0.55" stopColor={INK} stopOpacity="0.05" />
            <stop offset="1" stopColor={INK} stopOpacity="0.09" />
          </linearGradient>
          <linearGradient
            id={`fjres-${s}`}
            gradientUnits="userSpaceOnUse"
            x1={left ? 0 : W}
            y1={0}
            x2={left ? W : 0}
            y2={0}
          >
            <stop offset="0" stopColor={color} stopOpacity="0.25" />
            <stop offset="1" stopColor={color} stopOpacity="1" />
          </linearGradient>
        </defs>
        <line
          x1={0}
          x2={W}
          y1={LANE_Y}
          y2={LANE_Y}
          stroke={`url(#fjlane-${s})`}
          strokeWidth={1.5}
        />
        <line
          ref={set(`fj${s}res`)}
          x1={0}
          x2={W}
          y1={LANE_Y}
          y2={LANE_Y}
          stroke={`url(#fjres-${s})`}
          strokeWidth={1.5}
          opacity={0.03}
        />
        {chevrons.map((x, i) => (
          <g key={x}>
            <path
              d={chevronD(x)}
              fill="none"
              stroke={`url(#fjlane-${s})`}
              strokeWidth={1.5}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <path
              ref={set(`fj${s}w${i}`)}
              d={chevronD(x)}
              fill="none"
              stroke={color}
              strokeWidth={1.5}
              strokeLinecap="round"
              strokeLinejoin="round"
              opacity={0}
            />
          </g>
        ))}
        {left ? (
          <Envelope set={set} id="fjLenv" stroke={TEAL} />
        ) : (
          <Envelope set={set} id="fjRenv" stroke={GREEN} />
        )}
      </svg>
    </div>
  );
}

function wake(d: number, cx: number): number {
  return 0.35 * ramp(d, cx - 30, cx) * (1 - ramp(d, cx + 40, cx + 340));
}

export function driveFlanks(t: number, _wall: number, h: AnimHelpers) {
  const inDist = easeInOutCubic(ramp(t, 300, 800)) * ENTRY_TOTAL;
  if (inDist < W) {
    h.setX("fjLenv", inDist, LANE_Y);
    h.setO("fjLenv", ramp(t, 300, 400));
  } else {
    h.setO("fjLenv", 0);
  }
  CHEV_L.forEach((cx, i) => {
    h.setO(`fjLw${i}`, wake(inDist, cx));
  });
  h.setO("fjLres", 0.03 * ramp(t, 650, 1400));

  const tau = t - OUT_SEAM_T;
  const outDist = OUT_V0 * tau + OUT_ACCEL * tau * tau;
  if (tau > 0 && outDist < W + 50) {
    h.setX("fjRenv", outDist, LANE_Y);
    h.setO("fjRenv", ramp(tau, 0, 90));
  } else {
    h.setO("fjRenv", 0);
  }
  CHEV_R.forEach((cx, i) => {
    h.setO(`fjRw${i}`, tau > 0 ? wake(outDist, cx) : 0);
  });
  h.setO("fjRres", 0.05 * ramp(t, 33400, 33900) - 0.02 * ramp(t, 36000, 36400));
}
