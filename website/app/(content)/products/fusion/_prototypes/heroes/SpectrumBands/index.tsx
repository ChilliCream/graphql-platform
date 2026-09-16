"use client";

import { useRef } from "react";
import type { CSSProperties } from "react";

import { CC } from "../../../tokens";
import { anim, useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";

/**
 * The prism in reverse: five service-coloured bands enter from the right
 * edge and converge on a focal point behind the hero copy, merging into one
 * white line that exits left. A soft pulse travels along each band toward
 * the focal point in sequence; the rest frame shows all five already
 * converged.
 */

const VIEW_W = 1000;
const VIEW_H = 600;

const FOCAL = { x: 380, y: 300 };
const ENTRY_X = VIEW_W + 60;
const EXIT_X = -80;
const BAND_Y = [80, 195, 300, 405, 520] as const;
const BAND_HALF_START = 48;
const BAND_HALF_END = 6;

const KEYFRAMES = `
@keyframes fx-spectrumbands-pulse {
  0% { offset-distance: 0%; opacity: 0; }
  12% { opacity: 1; }
  85% { opacity: 1; }
  100% { offset-distance: 100%; opacity: 0; }
}
@keyframes fx-spectrumbands-glow {
  0%, 100% { opacity: 0.7; }
  50% { opacity: 1; }
}
`;

type PulseStyle = CSSProperties & {
  offsetPath: string;
  offsetRotate: string;
};

export default function SpectrumBands() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>

      <svg
        className="absolute inset-0 h-full w-full"
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="none"
      >
        <defs>
          <filter
            id="sb-pulse-blur"
            x="-100%"
            y="-100%"
            width="300%"
            height="300%"
          >
            <feGaussianBlur stdDeviation={3} />
          </filter>
          <filter id="sb-grain" x="0" y="0" width="100%" height="100%">
            <feTurbulence
              type="fractalNoise"
              baseFrequency={0.85}
              numOctaves={2}
              stitchTiles="stitch"
              result="sb-noise"
            />
            <feColorMatrix
              in="sb-noise"
              type="matrix"
              values="0 0 0 0 1  0 0 0 0 1  0 0 0 0 1  0 0 0 0.05 0"
            />
          </filter>
          <radialGradient id="sb-focal-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={CC.white} stopOpacity={0.9} />
            <stop offset="100%" stopColor={CC.white} stopOpacity={0} />
          </radialGradient>
          <linearGradient
            id="sb-merge-gradient"
            gradientUnits="userSpaceOnUse"
            x1={EXIT_X}
            y1={FOCAL.y}
            x2={FOCAL.x}
            y2={FOCAL.y}
          >
            <stop offset="0%" stopColor={CC.white} stopOpacity={0.4} />
            <stop offset="100%" stopColor={CC.white} stopOpacity={1} />
          </linearGradient>
          {SERVICE_SPECTRUM.map((stop, i) => (
            <linearGradient
              key={stop.label}
              id={`sb-band-gradient-${i}`}
              x1="0"
              y1="0"
              x2="1"
              y2="0"
            >
              <stop offset="0%" stopColor={CC.white} stopOpacity={0.95} />
              <stop offset="35%" stopColor={stop.color} stopOpacity={0.85} />
              <stop offset="100%" stopColor={stop.color} stopOpacity={0.3} />
            </linearGradient>
          ))}
          {SERVICE_SPECTRUM.map((stop, i) => (
            <radialGradient key={stop.label} id={`sb-pulse-gradient-${i}`}>
              <stop offset="0%" stopColor={CC.white} stopOpacity={1} />
              <stop offset="45%" stopColor={stop.color} stopOpacity={0.9} />
              <stop offset="100%" stopColor={stop.color} stopOpacity={0} />
            </radialGradient>
          ))}
        </defs>

        {SERVICE_SPECTRUM.map((stop, i) => {
          const y = BAND_Y[i];
          const points = [
            `${ENTRY_X},${y - BAND_HALF_START}`,
            `${ENTRY_X},${y + BAND_HALF_START}`,
            `${FOCAL.x},${FOCAL.y + BAND_HALF_END}`,
            `${FOCAL.x},${FOCAL.y - BAND_HALF_END}`,
          ].join(" ");

          return (
            <polygon
              key={stop.label}
              points={points}
              fill={`url(#sb-band-gradient-${i})`}
            />
          );
        })}

        <circle
          cx={FOCAL.x}
          cy={FOCAL.y}
          r={70}
          fill="url(#sb-focal-glow)"
          style={{
            animation: anim(
              running,
              "fx-spectrumbands-glow 5s ease-in-out infinite",
            ),
          }}
        />

        <line
          x1={FOCAL.x}
          y1={FOCAL.y}
          x2={EXIT_X}
          y2={FOCAL.y}
          stroke="url(#sb-merge-gradient)"
          strokeWidth={9}
          strokeLinecap="round"
        />

        {SERVICE_SPECTRUM.map((stop, i) => {
          const y = BAND_Y[i];
          const pulseStyle: PulseStyle = {
            offsetPath: `path("M ${ENTRY_X} ${y} L ${FOCAL.x} ${FOCAL.y}")`,
            offsetRotate: "0deg",
            opacity: 0,
            animation: anim(
              running,
              `fx-spectrumbands-pulse 3s ease-in-out ${i * 0.55}s infinite`,
            ),
          };

          return (
            <circle
              key={stop.label}
              r={16}
              fill={`url(#sb-pulse-gradient-${i})`}
              filter="url(#sb-pulse-blur)"
              style={pulseStyle}
            />
          );
        })}

        <rect
          x={0}
          y={0}
          width={VIEW_W}
          height={VIEW_H}
          fill={CC.white}
          filter="url(#sb-grain)"
          opacity={0.5}
          style={{ mixBlendMode: "overlay" }}
        />
      </svg>

      <div
        className="absolute inset-0 sm:hidden"
        style={{
          background: `linear-gradient(90deg, ${CC.bg} 0%, ${CC.bg} 50%, color-mix(in srgb, ${CC.bg} 85%, transparent) 100%)`,
        }}
      />
      <div
        className="absolute inset-0"
        style={{
          background: `radial-gradient(55% 60% at 26% 52%, color-mix(in srgb, ${CC.bg} 90%, transparent) 0%, color-mix(in srgb, ${CC.bg} 40%, transparent) 55%, transparent 85%)`,
        }}
      />
    </div>
  );
}
