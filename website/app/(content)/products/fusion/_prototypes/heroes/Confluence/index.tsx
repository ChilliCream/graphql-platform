"use client";

import { useRef } from "react";

import { CC } from "../../../tokens";
import { anim, useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";

/**
 * A topographic map: five service-coloured tributaries wind in from the top
 * and right edges through fine contour lines and merge into one broad white
 * channel that flows out behind the hero copy. The rest frame is the still
 * map; only fine dashes along each tributary drift toward the confluence.
 */

const VIEW_W = 1000;
const VIEW_H = 600;
const CONFLUENCE: readonly [number, number] = [640, 330];
const RIGHT_EDGE = VIEW_W + 40;
const LEFT_EDGE = -40;
const TOP_EDGE = -40;

const KEYFRAMES = `
@keyframes fx-confluence-drift {
  from { stroke-dashoffset: 0; }
  to { stroke-dashoffset: -108; }
}
@keyframes fx-confluence-pulse {
  0%, 100% { opacity: 0.55; }
  50% { opacity: 0.9; }
}
`;

interface TributarySource {
  readonly start: [number, number];
  readonly c1: [number, number];
  readonly c2: [number, number];
}

/** One bezier per tributary, each ending exactly at the confluence point. */
const TRIBUTARY_SOURCES: readonly TributarySource[] = [
  { start: [230, TOP_EDGE], c1: [205, 150], c2: [430, 230] },
  { start: [460, TOP_EDGE], c1: [480, 110], c2: [575, 230] },
  { start: [RIGHT_EDGE, 120], c1: [860, 95], c2: [730, 190] },
  { start: [RIGHT_EDGE, 310], c1: [880, 300], c2: [740, 320] },
  { start: [RIGHT_EDGE, 500], c1: [865, 515], c2: [730, 420] },
];

function tributaryPath({ start, c1, c2 }: TributarySource): string {
  return `M ${start[0]} ${start[1]} C ${c1[0]} ${c1[1]} ${c2[0]} ${c2[1]} ${CONFLUENCE[0]} ${CONFLUENCE[1]}`;
}

const TRIBUTARIES = SERVICE_SPECTRUM.map((stop, i) => ({
  stop,
  path: tributaryPath(TRIBUTARY_SOURCES[i]),
  duration: 4.6 + i * 0.5,
  delay: i * 0.35,
}));

const TRUNK_PATH = `M ${CONFLUENCE[0]} ${CONFLUENCE[1]} C 500 300 260 372 ${LEFT_EDGE} 340`;

/** A gently wavy contour row, sampled across the full viewBox width. */
function contourPoints(row: number): [number, number][] {
  const baseY = 50 + row * 62;
  const amplitude = 16 + (row % 3) * 9;
  const freq = 0.0055 + (row % 4) * 0.0014;
  const phase = row * 0.85;
  const points: [number, number][] = [];
  for (let x = LEFT_EDGE; x <= RIGHT_EDGE; x += 104) {
    points.push([x, baseY + Math.sin(x * freq + phase) * amplitude]);
  }
  return points;
}

/** Smooths a polyline into a quadratic path through consecutive midpoints. */
function smoothPath(points: readonly [number, number][]): string {
  const [first, ...rest] = points;
  let d = `M ${first[0]} ${first[1]}`;
  for (let i = 0; i < rest.length; i++) {
    const [cx, cy] = rest[i];
    const next = rest[i + 1];
    const [ex, ey] = next ? [(cx + next[0]) / 2, (cy + next[1]) / 2] : [cx, cy];
    d += ` Q ${cx} ${cy} ${ex} ${ey}`;
  }
  return d;
}

const CONTOUR_ROWS = 9;
const CONTOURS = Array.from({ length: CONTOUR_ROWS }, (_, row) =>
  smoothPath(contourPoints(row)),
);

export default function Confluence() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>

      <svg
        className="absolute inset-0 h-full w-full -translate-x-[48%] translate-y-[32%] scale-[0.62] overflow-visible sm:translate-x-0 sm:translate-y-0 sm:scale-100"
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid slice"
      >
        <defs>
          <linearGradient
            id="confluence-trunk-gradient"
            gradientUnits="userSpaceOnUse"
            x1={CONFLUENCE[0]}
            y1={CONFLUENCE[1]}
            x2={LEFT_EDGE}
            y2={340}
          >
            <stop offset="0%" stopColor={CC.white} stopOpacity={0.95} />
            <stop offset="100%" stopColor={CC.white} stopOpacity={0.22} />
          </linearGradient>
          <radialGradient id="confluence-pool-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={CC.white} stopOpacity={0.85} />
            <stop offset="100%" stopColor={CC.white} stopOpacity={0} />
          </radialGradient>
        </defs>

        {CONTOURS.map((d, i) => (
          <path
            key={i}
            d={d}
            fill="none"
            stroke={CC.inkFaint}
            strokeWidth={1.5}
            opacity={0.4}
          />
        ))}

        <path
          d={TRUNK_PATH}
          fill="none"
          stroke={CC.white}
          strokeOpacity={0.12}
          strokeWidth={56}
          strokeLinecap="round"
        />
        <path
          d={TRUNK_PATH}
          fill="none"
          stroke="url(#confluence-trunk-gradient)"
          strokeWidth={30}
          strokeLinecap="round"
        />

        {TRIBUTARIES.map(({ stop, path }) => (
          <path
            key={`${stop.label}-bank`}
            d={path}
            fill="none"
            stroke={stop.color}
            strokeOpacity={0.16}
            strokeWidth={16}
            strokeLinecap="round"
          />
        ))}
        {TRIBUTARIES.map(({ stop, path }) => (
          <path
            key={`${stop.label}-channel`}
            d={path}
            fill="none"
            stroke={stop.color}
            strokeOpacity={0.85}
            strokeWidth={6}
            strokeLinecap="round"
          />
        ))}

        <circle
          cx={CONFLUENCE[0]}
          cy={CONFLUENCE[1]}
          r={70}
          fill="url(#confluence-pool-glow)"
          style={{
            animation: anim(
              running,
              "fx-confluence-pulse 6s ease-in-out infinite",
            ),
          }}
        />

        {TRIBUTARIES.map(({ stop, path, duration, delay }) => (
          <path
            key={`${stop.label}-flow`}
            d={path}
            fill="none"
            stroke={CC.white}
            strokeOpacity={0.8}
            strokeWidth={3}
            strokeLinecap="round"
            strokeDasharray="3 24"
            style={{
              animation: anim(
                running,
                `fx-confluence-drift ${duration}s linear ${delay}s infinite`,
              ),
            }}
          />
        ))}
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
          background: `radial-gradient(58% 62% at 24% 52%, color-mix(in srgb, ${CC.bg} 90%, transparent) 0%, color-mix(in srgb, ${CC.bg} 44%, transparent) 55%, transparent 85%)`,
        }}
      />
    </div>
  );
}
