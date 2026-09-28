"use client";

import { useId } from "react";

import {
  BEZEL_R,
  BEZEL_WIDTH,
  CX,
  CY,
  ELECTRIC,
  ELECTRIC_BRIGHT,
  DISC_CORE,
  DISC_RIM,
  FACE_CORE,
  FACE_RIM,
} from "./hud";

interface DialFaceProps {
  readonly faceRadius: number;
}

export function DialFace({ faceRadius }: DialFaceProps) {
  const id = useId().replace(/:/g, "");

  return (
    <>
      <defs>
        <radialGradient id={`disc-${id}`}>
          <stop offset="45%" style={{ stopColor: DISC_CORE }} />
          <stop offset="100%" style={{ stopColor: DISC_RIM }} />
        </radialGradient>
        <radialGradient id={`face-${id}`}>
          <stop offset="55%" style={{ stopColor: FACE_CORE }} />
          <stop offset="100%" style={{ stopColor: FACE_RIM }} />
        </radialGradient>
        <filter id={`bezel-${id}`} x="-20%" y="-20%" width="140%" height="140%">
          <feGaussianBlur stdDeviation="3" />
        </filter>
      </defs>
      <circle cx={CX} cy={CY} r={BEZEL_R} fill={`url(#disc-${id})`} />
      <circle
        cx={CX}
        cy={CY}
        r={BEZEL_R}
        fill="none"
        stroke={ELECTRIC}
        strokeWidth={BEZEL_WIDTH}
        filter={`url(#bezel-${id})`}
      />
      <circle
        cx={CX}
        cy={CY}
        r={BEZEL_R}
        fill="none"
        stroke={ELECTRIC_BRIGHT}
        strokeWidth={BEZEL_WIDTH}
      />
      <circle
        cx={CX}
        cy={CY}
        r={faceRadius}
        fill={`url(#face-${id})`}
        stroke={ELECTRIC}
        strokeWidth={1.5}
      />
    </>
  );
}
