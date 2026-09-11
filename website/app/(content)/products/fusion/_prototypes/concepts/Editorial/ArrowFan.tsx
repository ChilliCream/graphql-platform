"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import {
  drawStyle,
  fadeStyle,
  FONT,
  HAND,
  MARKER,
  NOTES,
  PAPER,
} from "./palette";
import { Folio, PaperGround, StickyNote, Wobble } from "./Sketch";

/**
 * "What is Fusion?" drawn as an arrow fan: one client writes one query, the
 * hand-drawn arrow reaches the gateway, the gateway fans out to the subgraphs
 * that hold the data, the answers come back along the blue return arrows and
 * the response is written up as one card.
 *
 * Rest state: the finished diagram, every arrow drawn and the response filled.
 *
 * Every line is lettered in `FONT` units, so the smallest one still reads at
 * the site's 11px label on a 375px screen; the hub, the note column and the
 * response card are spaced for that lettering.
 */

const CSS = `
.ed-fan .ed-fan-draw { stroke-dasharray: var(--len, 300); stroke-dashoffset: 0; }
.ed-fan .ed-fan-in { opacity: 1; }
.ed-fan[data-run="true"] .ed-fan-draw { animation: ed-fan-draw 1.2s ease-out both; }
.ed-fan[data-run="true"] .ed-fan-in { animation: ed-fan-in 0.7s ease-out both; }
.ed-fan[data-run="true"] .ed-fan-flow { animation: ed-fan-flow 6s linear infinite; }
@keyframes ed-fan-draw {
  from { stroke-dashoffset: var(--len, 300); }
  to { stroke-dashoffset: 0; }
}
@keyframes ed-fan-in {
  from { opacity: 0; }
  to { opacity: 1; }
}
@keyframes ed-fan-flow { to { stroke-dashoffset: -180; } }
`;

const HUB = { x: 290, y: 196, r: 54 };

/** The four subgraphs this query touches, written on notes down the margin. */
const ROWS = [36, 112, 188, 264];
const NOTE_X = 388;
const NOTE_W = 232;
const NOTE_H = 64;

/** Arrow from the gateway out to a note, and the blue answer coming back. */
function outPath(row: number): string {
  const ty = row + NOTE_H / 2;
  return `M${HUB.x + HUB.r - 4} ${HUB.y - 8} C ${HUB.x + 110} ${HUB.y - 8}, ${NOTE_X - 70} ${ty}, ${NOTE_X - 12} ${ty - 8}`;
}

function backPath(row: number): string {
  const ty = row + NOTE_H / 2;
  return `M${NOTE_X - 12} ${ty + 10} C ${NOTE_X - 80} ${ty + 12}, ${HUB.x + 100} ${HUB.y + 18}, ${HUB.x + HUB.r - 6} ${HUB.y + 12}`;
}

interface HeadProps {
  readonly x: number;
  readonly y: number;
  /** Degrees the arrowhead is turned to follow its line. */
  readonly angle: number;
  readonly color: string;
}

/** A two-stroke arrowhead, drawn open the way a marker leaves it. */
function Head({ x, y, angle, color }: HeadProps) {
  return (
    <g transform={`translate(${x} ${y}) rotate(${angle})`}>
      <path
        d="M0 0 l-13 -6 M0 0 l-13 7"
        fill="none"
        stroke={color}
        strokeWidth={2.2}
        strokeLinecap="round"
      />
    </g>
  );
}

const QUERY_LINES = [
  { w: 96, y: 0 },
  { w: 74, y: 16 },
  { w: 86, y: 32 },
];

const RESPONSE_LINES = [
  { w: 108, y: 0 },
  { w: 88, y: 15 },
  { w: 116, y: 30 },
  { w: 72, y: 45 },
];

export function ArrowFan() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div className="ed-fan absolute inset-0" data-run={run ? "true" : "false"}>
      <style>{CSS}</style>
      <svg viewBox="0 0 640 480" className="h-full w-full" aria-hidden="true">
        <defs>
          <Wobble id="ed-fan-wob" scale={1.6} frequency={0.018} seed={7} />
          <Wobble id="ed-fan-wob2" scale={1} frequency={0.035} seed={2} />
        </defs>

        <PaperGround width={640} height={480} ruled />
        <Folio x={20} y={26}>
          Fig. 1 - one query, one response
        </Folio>

        {/* The client and the query it writes */}
        <g filter="url(#ed-fan-wob2)">
          <path
            d="M34 74 h84 v50 h-84 Z M20 124 h112 l-10 12 h-92 Z"
            fill="none"
            stroke={PAPER.ink}
            strokeWidth={2.2}
            strokeLinejoin="round"
          />
          <text
            x={140}
            y={110}
            fill={PAPER.inkSoft}
            fontSize={FONT.label}
            style={HAND}
          >
            Web
          </text>
        </g>
        <text
          x={20}
          y={170}
          fill={PAPER.ink}
          fontSize={FONT.caption}
          style={HAND}
        >
          one query
        </text>
        <g transform="translate(20 182)">
          <rect
            width={148}
            height={72}
            fill={PAPER.sheet}
            stroke={PAPER.ink}
            strokeWidth={1.6}
            filter="url(#ed-fan-wob2)"
          />
          {QUERY_LINES.map((line) => (
            <path
              key={line.y}
              className="ed-fan-draw"
              style={drawStyle(line.w, 0.2 + line.y * 0.02)}
              d={`M16 ${22 + line.y} h${line.w}`}
              stroke={PAPER.inkSoft}
              strokeWidth={2.4}
              strokeLinecap="round"
            />
          ))}
        </g>

        {/* Into the gateway */}
        <path
          className="ed-fan-draw"
          style={drawStyle(140, 0.6)}
          d={`M172 218 C 200 216, 212 206, ${HUB.x - HUB.r - 6} ${HUB.y}`}
          fill="none"
          stroke={PAPER.ink}
          strokeWidth={2.4}
          strokeLinecap="round"
          filter="url(#ed-fan-wob)"
        />
        <g className="ed-fan-in" style={fadeStyle(1.1)}>
          <Head x={HUB.x - HUB.r - 4} y={HUB.y} angle={-4} color={PAPER.ink} />
        </g>

        <g filter="url(#ed-fan-wob)">
          <circle
            cx={HUB.x}
            cy={HUB.y}
            r={HUB.r}
            fill={PAPER.sheet}
            stroke={PAPER.ink}
            strokeWidth={2.8}
          />
          <circle
            cx={HUB.x}
            cy={HUB.y}
            r={HUB.r + 7}
            fill="none"
            stroke={PAPER.ink}
            strokeWidth={1.4}
            opacity={0.4}
          />
        </g>
        <text
          x={HUB.x}
          y={HUB.y + 8}
          textAnchor="middle"
          fill={PAPER.ink}
          fontSize={FONT.caption}
          style={MARKER}
        >
          gateway
        </text>
        <text
          x={HUB.x}
          y={HUB.y + HUB.r + 30}
          textAnchor="middle"
          fill={PAPER.pencil}
          fontSize={FONT.label}
          style={HAND}
        >
          one endpoint
        </text>

        {/* The fan out to the subgraphs, and the answers back */}
        {ROWS.map((row, i) => (
          <g key={NOTES[i].name}>
            <path
              className="ed-fan-draw"
              style={drawStyle(240, 1 + i * 0.35)}
              d={outPath(row)}
              fill="none"
              stroke={PAPER.ink}
              strokeWidth={2.2}
              strokeLinecap="round"
              filter="url(#ed-fan-wob)"
            />
            <path
              className="ed-fan-flow"
              d={outPath(row)}
              fill="none"
              stroke={PAPER.blue}
              strokeWidth={2.6}
              strokeDasharray="3 26"
              strokeLinecap="round"
              opacity={0.65}
              style={{ animationDelay: `${i * 0.6}s` }}
            />
            <g className="ed-fan-in" style={fadeStyle(1.6 + i * 0.35)}>
              <Head
                x={NOTE_X - 10}
                y={row + NOTE_H / 2 - 8}
                angle={row < HUB.y ? -18 : 18}
                color={PAPER.ink}
              />
            </g>
            <path
              className="ed-fan-draw"
              style={drawStyle(240, 3.2 + i * 0.3)}
              d={backPath(row)}
              fill="none"
              stroke={PAPER.blue}
              strokeWidth={2}
              strokeLinecap="round"
              opacity={0.85}
              filter="url(#ed-fan-wob)"
            />
          </g>
        ))}

        {ROWS.map((row, i) => (
          <g key={`${NOTES[i].name}-note`}>
            <StickyNote
              x={NOTE_X}
              y={row}
              width={NOTE_W}
              height={NOTE_H}
              tilt={i % 2 === 0 ? -1.6 : 1.4}
              stock={NOTES[i].stock}
              name={NOTES[i].name}
              caption={`${NOTES[i].language} · ${NOTES[i].specLabel}`}
            />
          </g>
        ))}

        {/* The one response, written up under the query */}
        <text
          x={20}
          y={300}
          fill={PAPER.blue}
          fontSize={FONT.caption}
          style={HAND}
        >
          one response
        </text>
        <g transform="translate(20 312)">
          <rect
            width={196}
            height={104}
            fill={PAPER.sheet}
            stroke={PAPER.blue}
            strokeWidth={1.8}
            filter="url(#ed-fan-wob2)"
          />
          {RESPONSE_LINES.map((line, i) => (
            <path
              key={line.y}
              className="ed-fan-draw"
              style={drawStyle(line.w, 4.6 + i * 0.3)}
              d={`M16 ${26 + line.y} h${line.w}`}
              stroke={PAPER.blue}
              strokeWidth={2.4}
              strokeLinecap="round"
              opacity={0.85}
            />
          ))}
        </g>
        <path
          className="ed-fan-draw"
          style={drawStyle(180, 4.4)}
          d={`M${HUB.x - 20} ${HUB.y + 56} C 256 290, 232 310, 200 330`}
          fill="none"
          stroke={PAPER.blue}
          strokeWidth={2.2}
          strokeLinecap="round"
          filter="url(#ed-fan-wob)"
        />
        <g className="ed-fan-in" style={fadeStyle(5.2)}>
          <Head x={200} y={330} angle={145} color={PAPER.blue} />
        </g>

        <text
          x={20}
          y={452}
          fill={PAPER.pencil}
          fontSize={FONT.label}
          style={HAND}
        >
          the gateway works out who holds what
        </text>
      </svg>
    </div>
  );
}
