"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { BP, FONT, PARTS } from "./palette";
import { DUR, draw, fade, Sheet } from "./Sheet";

/**
 * Nitro band: the instrumentation schedule of the drawing set. Three dials
 * read the main assembly - latency, throughput and error rate - and a smaller
 * dial reads each sub-assembly behind it, so the gateway and every subgraph
 * are on the same sheet with needles live.
 *
 * Rest state: every dial drawn with its needle standing at its reading.
 */

interface Dial {
  readonly id: string;
  readonly label: string;
  readonly reading: string;
  /** Needle position, 0 to 1 across the scale. */
  readonly value: number;
  /** How far the needle drifts around that reading while it is live. */
  readonly drift: number;
  readonly cx: number;
  readonly cy: number;
  readonly r: number;
}

const MAIN: readonly Dial[] = [
  {
    id: "latency",
    label: "LATENCY",
    reading: "42 ms",
    value: 0.34,
    drift: 0.06,
    cx: 84,
    cy: 88,
    r: 32,
  },
  {
    id: "throughput",
    label: "THROUGHPUT",
    reading: "18.4k / min",
    value: 0.62,
    drift: 0.05,
    cx: 240,
    cy: 88,
    r: 32,
  },
  {
    id: "errors",
    label: "ERROR RATE",
    reading: "0.04 %",
    value: 0.09,
    drift: 0.03,
    cx: 396,
    cy: 88,
    r: 32,
  },
];

const SUB: readonly Dial[] = PARTS.slice(0, 4).map((part, i) => ({
  id: part.no,
  label: part.name.toUpperCase(),
  reading: part.no,
  value: 0.3 + i * 0.12,
  drift: 0.07,
  cx: 66 + i * 112,
  cy: 214,
  r: 13,
}));

const DIALS = [...MAIN, ...SUB];

/** Needle angle for a scale reading: the dial sweeps 270 degrees. */
function angle(value: number): number {
  return -135 + 270 * value;
}

function point(cx: number, cy: number, r: number, deg: number): string {
  const rad = ((deg - 90) * Math.PI) / 180;
  return `${(cx + r * Math.cos(rad)).toFixed(1)} ${(cy + r * Math.sin(rad)).toFixed(1)}`;
}

/** The dial face: a 270 degree arc, open at the bottom. */
function face(dial: Dial): string {
  return `M${point(dial.cx, dial.cy, dial.r, -135)}A${dial.r} ${dial.r} 0 1 1 ${point(
    dial.cx,
    dial.cy,
    dial.r,
    135,
  )}`;
}

const CSS = `
${fade("bp-g-head", 1)}
${DIALS.map((_, i) => draw(`bp-g-face${i}`, 4 + i * 3, 6)).join("\n")}
${DIALS.map((_, i) => fade(`bp-g-label${i}`, 12 + i * 3)).join("\n")}
${DIALS.map(
  (dial, i) => `
.bp-g-needle${i}{opacity:1;transform:rotate(${angle(dial.value).toFixed(1)}deg);animation:bp-g-needle${i} ${DUR}s ease-in-out infinite}
@keyframes bp-g-needle${i}{
0%,${12 + i * 3}%{opacity:0;transform:rotate(${angle(dial.value).toFixed(1)}deg)}
${14 + i * 3}%{opacity:1;transform:rotate(${angle(dial.value).toFixed(1)}deg)}
${30 + i * 2}%{transform:rotate(${angle(Math.max(0, dial.value - dial.drift)).toFixed(1)}deg)}
${55 + i * 2}%{transform:rotate(${angle(Math.min(1, dial.value + dial.drift)).toFixed(1)}deg)}
${80 + i}%,100%{transform:rotate(${angle(dial.value).toFixed(1)}deg)}}`,
).join("\n")}
${draw("bp-g-rule", 46, 6)}
${fade("bp-g-note", 88)}
`;

interface DialFaceProps {
  readonly dial: Dial;
  readonly index: number;
  readonly detail: boolean;
}

function DialFace({ dial, index, detail }: DialFaceProps) {
  return (
    <g>
      <path
        className={`bp-g-face${index}`}
        d={face(dial)}
        pathLength={1}
        fill="none"
        stroke={BP.ink}
        strokeWidth={1.2}
      />
      <g className={`bp-g-label${index}`}>
        {[0, 0.25, 0.5, 0.75, 1].map((tick) => (
          <path
            key={tick}
            d={`M${point(dial.cx, dial.cy, dial.r - dial.r / 5, angle(tick))}L${point(
              dial.cx,
              dial.cy,
              dial.r,
              angle(tick),
            )}`}
            fill="none"
            stroke={BP.inkFaint}
            strokeWidth={0.9}
          />
        ))}
        <circle cx={dial.cx} cy={dial.cy} r={2.4} fill={BP.dim} />
        {detail ? (
          <text
            className="bp-t-cyan"
            x={dial.cx}
            y={dial.cy + dial.r + 22}
            textAnchor="middle"
            fontSize={FONT.label}
          >
            {dial.reading}
          </text>
        ) : null}
        <text
          className="bp-t-dim"
          x={dial.cx}
          y={dial.cy + dial.r + (detail ? 42 : 22)}
          textAnchor="middle"
          fontSize={FONT.label}
        >
          {dial.label}
        </text>
      </g>
      <path
        className={`bp-g-needle${index}`}
        style={{ transformOrigin: `${dial.cx}px ${dial.cy}px` }}
        d={`M${dial.cx} ${dial.cy}V${dial.cy - dial.r + dial.r / 5}`}
        fill="none"
        stroke={BP.dim}
        strokeWidth={1.4}
      />
    </g>
  );
}

export function GaugeSchedule() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();

  return (
    <Sheet
      title="Instrumentation schedule"
      no="DWG-105"
      rev="A"
      run={active && !reduced}
    >
      <svg
        viewBox="0 0 480 276"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        <text
          className="bp-g-head bp-t-dim"
          x={14}
          y={22}
          fontSize={FONT.label}
        >
          ASSY-100 GATEWAY · READINGS
        </text>

        {MAIN.map((dial, i) => (
          <DialFace key={dial.id} dial={dial} index={i} detail />
        ))}

        <path
          className="bp-g-rule"
          d="M14 176h452"
          pathLength={1}
          fill="none"
          stroke={BP.inkFaint}
          strokeWidth={0.9}
        />
        <text
          className="bp-g-head bp-t-dim"
          x={14}
          y={196}
          fontSize={FONT.label}
        >
          EACH SUBGRAPH BEHIND IT
        </text>

        {SUB.map((dial, i) => (
          <DialFace
            key={dial.id}
            dial={dial}
            index={MAIN.length + i}
            detail={false}
          />
        ))}

        <text
          className="bp-g-note bp-t-dim"
          x={14}
          y={270}
          fontSize={FONT.label}
        >
          CHECKED AGAINST PUBLISHED OPERATIONS
        </text>
      </svg>
    </Sheet>
  );
}
