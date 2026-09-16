"use client";

import { useRef } from "react";

import { CC } from "../../../tokens";
import { anim, useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";

/**
 * Hero art: a gyroscope of five thin rings in the service colours, each
 * tilted at its own angle around one bright core behind the right half of
 * the hero, with faint orbit paths reaching the viewport edges. The rest
 * frame is the rings parked at their distinct resting angles; running
 * motion spins each ring on its own axis at its own speed and direction.
 */

const VIEW_W = 1600;
const VIEW_H = 900;

const CORE_X = 1120;
const CORE_Y = 450;
const CORE_R = 20;

const RING_STROKE = 2.5;

/** One tilted ring per service colour, in `SERVICE_SPECTRUM` order. */
const RING_GEOMETRY: readonly {
  readonly rx: number;
  readonly ry: number;
  readonly baseAngle: number;
  readonly durationMs: number;
  readonly reverse: boolean;
}[] = [
  { rx: 260, ry: 70, baseAngle: -30, durationMs: 26000, reverse: false },
  { rx: 235, ry: 105, baseAngle: 18, durationMs: 33000, reverse: true },
  { rx: 205, ry: 140, baseAngle: -58, durationMs: 21000, reverse: false },
  { rx: 285, ry: 45, baseAngle: 46, durationMs: 29000, reverse: true },
  { rx: 165, ry: 165, baseAngle: 72, durationMs: 38000, reverse: false },
];

/** Faint orbit paths reaching past the viewport edges, well outside the rings. */
const ORBIT_PATHS = [
  { rx: 1500, ry: 260, angle: -8 },
  { rx: 1500, ry: 400, angle: 11 },
];

const KEYFRAMES = `
${RING_GEOMETRY.map(
  (ring, i) => `
@keyframes fx-orbitalrings-spin-${i} {
  from { transform: rotate(${ring.baseAngle}deg); }
  to { transform: rotate(${ring.baseAngle + (ring.reverse ? -360 : 360)}deg); }
}`,
).join("\n")}
@keyframes fx-orbitalrings-pulse {
  0%, 100% { opacity: 0.85; }
  50% { opacity: 1; }
}
`;

export default function OrbitalRings() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>
      <svg
        className="absolute inset-0 h-full w-full"
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid slice"
      >
        <defs>
          <radialGradient id="orbitalrings-core-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={CC.white} stopOpacity="0.9" />
            <stop offset="55%" stopColor={CC.white} stopOpacity="0.28" />
            <stop offset="100%" stopColor={CC.white} stopOpacity="0" />
          </radialGradient>
        </defs>

        {/* Faint orbit paths, reaching the viewport edges behind the rings. */}
        {ORBIT_PATHS.map((orbit, i) => (
          <ellipse
            key={i}
            cx={CORE_X}
            cy={CORE_Y}
            rx={orbit.rx}
            ry={orbit.ry}
            transform={`rotate(${orbit.angle} ${CORE_X} ${CORE_Y})`}
            fill="none"
            stroke={CC.inkFaint}
            strokeWidth="1"
            strokeOpacity="0.35"
          />
        ))}

        {/* Five rings, each tilted and spinning on its own axis around the core. */}
        {SERVICE_SPECTRUM.map((stop, i) => {
          const ring = RING_GEOMETRY[i];
          return (
            <g
              key={stop.label}
              style={{
                transformOrigin: `${CORE_X}px ${CORE_Y}px`,
                transform: `rotate(${ring.baseAngle}deg)`,
                animation: anim(
                  running,
                  `fx-orbitalrings-spin-${i} ${ring.durationMs}ms linear infinite`,
                ),
              }}
            >
              <ellipse
                cx={CORE_X}
                cy={CORE_Y}
                rx={ring.rx}
                ry={ring.ry}
                fill="none"
                stroke={stop.color}
                strokeWidth={RING_STROKE}
                strokeOpacity="0.9"
              />
            </g>
          );
        })}

        {/* Bright white core the rings gyrate around. */}
        <circle
          cx={CORE_X}
          cy={CORE_Y}
          r={CORE_R * 4.5}
          fill="url(#orbitalrings-core-glow)"
          style={{
            animation: anim(
              running,
              "fx-orbitalrings-pulse 5000ms ease-in-out infinite",
            ),
          }}
        />
        <circle cx={CORE_X} cy={CORE_Y} r={CORE_R} fill={CC.white} />
      </svg>

      {/* Scrim so the hero copy stays readable over the orbit paths. */}
      <div className="from-cc-bg via-cc-bg/80 pointer-events-none absolute inset-y-0 left-0 w-full max-w-3xl bg-gradient-to-r via-60% to-transparent" />
    </div>
  );
}
