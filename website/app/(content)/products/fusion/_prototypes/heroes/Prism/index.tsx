"use client";

import { useRef } from "react";
import type { CSSProperties } from "react";

import { CC } from "../../../tokens";
import { anim, useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";

/**
 * White light enters a glass prism on the left and splits into five rays in
 * the service colours that fan out across the hero and thin into the page
 * background. The incoming beam breathes and the rays drift slowly; the rest
 * frame (server render, reduced motion, off-screen) shows the full split
 * with no motion.
 */

const VIEW_W = 1000;
const VIEW_H = 600;

const PRISM_APEX = { x: 300, y: 300 };
const PRISM_TOP = { x: 130, y: 180 };
const PRISM_BOTTOM = { x: 130, y: 420 };

const RAY_END_X = 980;
const RAY_END_Y = [60, 180, 300, 420, 540] as const;
const RAY_HALF_END = 52;
const RAY_HALF_START = 4;

const KEYFRAMES = `
@keyframes fx-prism-breathe {
  0%, 100% { opacity: 0.65; transform: scaleX(1); }
  50% { opacity: 1; transform: scaleX(1.05); }
}
@keyframes fx-prism-drift {
  0%, 100% { transform: translateY(0); opacity: 0.9; }
  50% { transform: translateY(var(--fx-prism-drift, 6px)); opacity: 1; }
}
`;

type DriftStyle = CSSProperties & { "--fx-prism-drift"?: string };

export default function Prism() {
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
          <linearGradient id="prism-beam-gradient" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor={CC.white} stopOpacity={0} />
            <stop offset="100%" stopColor={CC.white} stopOpacity={0.9} />
          </linearGradient>
          <linearGradient id="prism-wedge-gradient" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor={CC.white} stopOpacity={0.22} />
            <stop offset="35%" stopColor={CC.surface} stopOpacity={0.88} />
            <stop offset="100%" stopColor={CC.surface} stopOpacity={0.62} />
          </linearGradient>
          {SERVICE_SPECTRUM.map((stop, i) => (
            <linearGradient
              key={stop.label}
              id={`prism-ray-gradient-${i}`}
              x1="0"
              y1="0"
              x2="1"
              y2="0"
            >
              <stop offset="0%" stopColor={stop.color} stopOpacity={0.85} />
              <stop offset="55%" stopColor={stop.color} stopOpacity={0.45} />
              <stop offset="100%" stopColor={stop.color} stopOpacity={0} />
            </linearGradient>
          ))}
        </defs>

        {SERVICE_SPECTRUM.map((stop, i) => {
          const endY = RAY_END_Y[i];
          const points = [
            `${PRISM_APEX.x},${PRISM_APEX.y - RAY_HALF_START}`,
            `${PRISM_APEX.x},${PRISM_APEX.y + RAY_HALF_START}`,
            `${RAY_END_X},${endY + RAY_HALF_END}`,
            `${RAY_END_X},${endY - RAY_HALF_END}`,
          ].join(" ");
          const driftStyle: DriftStyle = {
            animation: anim(running, "fx-prism-drift 6s ease-in-out infinite"),
            animationDelay: `${i * 0.35}s`,
            "--fx-prism-drift": i % 2 === 0 ? "7px" : "-7px",
          };

          return (
            <polygon
              key={stop.label}
              points={points}
              fill={`url(#prism-ray-gradient-${i})`}
              style={driftStyle}
            />
          );
        })}

        <polygon
          points={`${PRISM_TOP.x},${PRISM_TOP.y} ${PRISM_BOTTOM.x},${PRISM_BOTTOM.y} ${PRISM_APEX.x},${PRISM_APEX.y}`}
          fill="url(#prism-wedge-gradient)"
          stroke={CC.white}
          strokeOpacity={0.65}
          strokeWidth={2}
        />

        <line
          x1={-40}
          y1={PRISM_APEX.y}
          x2={PRISM_TOP.x}
          y2={PRISM_APEX.y}
          stroke="url(#prism-beam-gradient)"
          strokeWidth={10}
          strokeLinecap="round"
          style={{
            animation: anim(
              running,
              "fx-prism-breathe 4.5s ease-in-out infinite",
            ),
            transformOrigin: `${PRISM_TOP.x}px ${PRISM_APEX.y}px`,
          }}
        />
      </svg>

      <div
        className="absolute inset-0"
        style={{
          background: `radial-gradient(60% 55% at 24% 54%, color-mix(in srgb, ${CC.bg} 92%, transparent) 0%, color-mix(in srgb, ${CC.bg} 45%, transparent) 55%, transparent 85%)`,
        }}
      />
    </div>
  );
}
