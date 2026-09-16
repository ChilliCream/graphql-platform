"use client";

import { useRef } from "react";
import type { CSSProperties } from "react";

import { CC, FONTS } from "../../../tokens";
import { anim, useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";

/**
 * A transit-map diagram: five service-coloured lines bend into a FUSION
 * interchange and continue on as one line to the edge, each with a station
 * tick and label. The rest frame shows every line already converged.
 */

const VIEW_W = 1000;
const VIEW_H = 600;

const INTERCHANGE = { x: 580, y: 300 };
const APPROACH = 55;
const RIGHT_EDGE = VIEW_W + 40;
const LEFT_EDGE = -40;
const LINE_Y = [70, 195, 300, 405, 530] as const;
const TICK_R = 5;
/** Trunk exit row, well below the copy block so no line crosses it. */
const TRUNK_Y = 520;
/** Where the trunk finishes dropping to TRUNK_Y, still right of the copy block. */
const TRUNK_BEND_X = 560;

const KEYFRAMES = `
@keyframes fx-subwaymap-train {
  0% { offset-distance: 0%; opacity: 0; }
  10% { opacity: 1; }
  88% { opacity: 1; }
  100% { offset-distance: 100%; opacity: 0; }
}
@keyframes fx-subwaymap-pulse {
  0%, 100% { opacity: 0.7; }
  50% { opacity: 1; }
}
`;

type TrainStyle = CSSProperties & {
  offsetPath: string;
  offsetRotate: string;
};

interface LineGeometry {
  readonly vertices: readonly (readonly [number, number])[];
  /** [entry tick, approach tick], each a station along the line. */
  readonly ticks: readonly [
    readonly [number, number],
    readonly [number, number],
  ];
}

/** One elbowed line from the right edge to the interchange, station ticks included. */
function buildLine(y0: number): LineGeometry {
  const dy = Math.abs(INTERCHANGE.y - y0);
  const bendEndX = INTERCHANGE.x + APPROACH;
  const bendStartX = bendEndX + dy;

  return {
    vertices: [
      [RIGHT_EDGE, y0],
      [bendStartX, y0],
      [bendEndX, INTERCHANGE.y],
      [INTERCHANGE.x, INTERCHANGE.y],
    ],
    ticks: [
      [(RIGHT_EDGE + bendStartX) / 2, y0],
      [(bendStartX + bendEndX) / 2, (y0 + INTERCHANGE.y) / 2],
    ],
  };
}

function pathD(vertices: readonly (readonly [number, number])[]): string {
  return vertices
    .map(([x, y], i) => `${i === 0 ? "M" : "L"} ${x} ${y}`)
    .join(" ");
}

const LINES = SERVICE_SPECTRUM.map((stop, i) => ({
  stop,
  geometry: buildLine(LINE_Y[i]),
}));

/** Hub to bend, staying right of the copy column while it drops to TRUNK_Y. */
const TRUNK_NEAR: readonly (readonly [number, number])[] = [
  [INTERCHANGE.x, INTERCHANGE.y],
  [TRUNK_BEND_X, TRUNK_Y],
];
/** Bend to the left edge, a flat run below the copy block. */
const TRUNK_FAR: readonly (readonly [number, number])[] = [
  [TRUNK_BEND_X, TRUNK_Y],
  [LEFT_EDGE, TRUNK_Y],
];

export default function SubwayMap() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>

      <svg
        className="absolute inset-0 h-full w-full -translate-x-[48%] translate-y-[25%] scale-[0.54] overflow-visible sm:translate-x-0 sm:translate-y-0 sm:scale-100"
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid slice"
      >
        <defs>
          <linearGradient
            id="sm-trunk-gradient"
            gradientUnits="userSpaceOnUse"
            x1={INTERCHANGE.x}
            y1={INTERCHANGE.y}
            x2={LEFT_EDGE}
            y2={TRUNK_Y}
          >
            <stop offset="0%" stopColor={CC.white} stopOpacity={0.9} />
            <stop offset="100%" stopColor={CC.white} stopOpacity={0.15} />
          </linearGradient>
          <radialGradient id="sm-hub-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={CC.white} stopOpacity={0.85} />
            <stop offset="100%" stopColor={CC.white} stopOpacity={0} />
          </radialGradient>
          {LINES.map(({ stop }, i) => (
            <radialGradient key={stop.label} id={`sm-train-gradient-${i}`}>
              <stop offset="0%" stopColor={CC.white} stopOpacity={1} />
              <stop offset="45%" stopColor={stop.color} stopOpacity={0.9} />
              <stop offset="100%" stopColor={stop.color} stopOpacity={0} />
            </radialGradient>
          ))}
        </defs>

        <path
          d={pathD(TRUNK_NEAR)}
          stroke="url(#sm-trunk-gradient)"
          strokeWidth={5}
          strokeLinecap="round"
          fill="none"
        />
        <path
          d={pathD(TRUNK_FAR)}
          stroke="url(#sm-trunk-gradient)"
          strokeWidth={5}
          strokeLinecap="round"
          fill="none"
        />

        {LINES.map(({ stop, geometry }) => (
          <g key={stop.label}>
            <path
              d={pathD(geometry.vertices)}
              stroke={stop.color}
              strokeWidth={5}
              strokeLinecap="round"
              strokeLinejoin="round"
              fill="none"
              opacity={0.85}
            />
            {geometry.ticks.map(([x, y], i) => (
              <circle
                key={i}
                cx={x}
                cy={y}
                r={TICK_R}
                fill={CC.bg}
                stroke={stop.color}
                strokeWidth={2}
              />
            ))}
          </g>
        ))}

        <circle
          cx={INTERCHANGE.x}
          cy={INTERCHANGE.y}
          r={64}
          fill="url(#sm-hub-glow)"
          style={{
            animation: anim(
              running,
              "fx-subwaymap-pulse 5s ease-in-out infinite",
            ),
          }}
        />
        <circle
          cx={INTERCHANGE.x}
          cy={INTERCHANGE.y}
          r={30}
          fill={CC.surface}
          stroke={CC.white}
          strokeWidth={4}
          strokeOpacity={0.85}
        />
        <circle
          cx={INTERCHANGE.x}
          cy={INTERCHANGE.y}
          r={44}
          fill="none"
          stroke={CC.white}
          strokeOpacity={0.35}
          strokeWidth={3}
        />

        {LINES.map(({ stop, geometry }, i) => {
          const trainStyle: TrainStyle = {
            offsetPath: `path("${pathD(geometry.vertices)}")`,
            offsetRotate: "0deg",
            opacity: 0,
            animation: anim(
              running,
              `fx-subwaymap-train 3.4s ease-in-out ${i * 0.5}s infinite`,
            ),
          };
          return (
            <circle
              key={stop.label}
              r={9}
              fill={`url(#sm-train-gradient-${i})`}
              style={trainStyle}
            />
          );
        })}

        {LINES.map(({ stop, geometry }) => {
          const [tickX, tickY] = geometry.ticks[0];
          return (
            <text
              key={stop.label}
              x={tickX}
              y={tickY - (TICK_R + 8)}
              textAnchor="middle"
              fill={stop.color}
              fontFamily={FONTS.mono}
              className="text-[17px] sm:text-[10px]"
              style={{ letterSpacing: "0.15em", textTransform: "uppercase" }}
            >
              {stop.label}
            </text>
          );
        })}

        <text
          x={INTERCHANGE.x}
          y={INTERCHANGE.y + 60}
          textAnchor="middle"
          fill={CC.heading}
          fontFamily={FONTS.heading}
          className="text-[17px] sm:text-[13px]"
          style={{ letterSpacing: "0.2em", textTransform: "uppercase" }}
        >
          Fusion
        </text>
      </svg>

      <div
        className="absolute inset-0 sm:hidden"
        style={{
          background: `color-mix(in srgb, ${CC.bg} 55%, transparent)`,
        }}
      />
      <div
        className="absolute inset-0"
        style={{
          background: `radial-gradient(58% 62% at 25% 53%, color-mix(in srgb, ${CC.bg} 92%, transparent) 0%, color-mix(in srgb, ${CC.bg} 42%, transparent) 55%, transparent 85%)`,
        }}
      />
    </div>
  );
}
