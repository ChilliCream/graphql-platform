"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { drawStyle, fadeStyle, HAND, MARKER, NOTES, PAPER } from "./palette";
import { Folio, PaperGround, StickyNote, Wobble } from "./Sketch";

/**
 * "What is Fusion?" drawn as an arrow fan: one client writes one query, the
 * hand-drawn arrow reaches the gateway, the gateway fans out to the subgraphs
 * that hold the data, the answers come back along the blue return arrows and
 * the response is written up as one card.
 *
 * Rest state: the finished diagram, every arrow drawn and the response filled.
 */

const CSS = `
.ed-fan .ed-fan-draw { stroke-dasharray: var(--len, 300); stroke-dashoffset: 0; }
.ed-fan .ed-fan-in { opacity: 1; }
.ed-fan[data-run="true"] .ed-fan-draw { animation: ed-fan-draw 13s ease-in-out infinite; }
.ed-fan[data-run="true"] .ed-fan-in { animation: ed-fan-in 13s ease-in-out infinite; }
@keyframes ed-fan-draw {
  0% { stroke-dashoffset: var(--len, 300); opacity: 0.9; }
  14%, 74% { stroke-dashoffset: 0; opacity: 1; }
  88%, 100% { stroke-dashoffset: 0; opacity: 0; }
}
@keyframes ed-fan-in {
  0%, 6% { opacity: 0; }
  16%, 74% { opacity: 1; }
  88%, 100% { opacity: 0; }
}
`;

const HUB = { x: 296, y: 214, r: 58 };

/** The four subgraphs this query touches, written on notes down the margin. */
const ROWS = [40, 122, 204, 286];
const NOTE_X = 424;
const NOTE_W = 196;
const NOTE_H = 66;

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
          <text x={140} y={104} fill={PAPER.inkSoft} fontSize={16} style={HAND}>
            Web
          </text>
        </g>
        <text x={20} y={172} fill={PAPER.ink} fontSize={17} style={HAND}>
          one query
        </text>
        <g transform="translate(20 186)">
          <rect
            width={168}
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
          d={`M192 208 C 224 206, 236 200, ${HUB.x - HUB.r - 6} ${HUB.y - 4}`}
          fill="none"
          stroke={PAPER.ink}
          strokeWidth={2.4}
          strokeLinecap="round"
          filter="url(#ed-fan-wob)"
        />
        <g className="ed-fan-in" style={fadeStyle(1.1)}>
          <Head
            x={HUB.x - HUB.r - 4}
            y={HUB.y - 4}
            angle={-4}
            color={PAPER.ink}
          />
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
          y={HUB.y + 4}
          textAnchor="middle"
          fill={PAPER.ink}
          fontSize={21}
          style={MARKER}
        >
          gateway
        </text>
        <text
          x={HUB.x}
          y={HUB.y + 24}
          textAnchor="middle"
          fill={PAPER.pencil}
          fontSize={13}
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
        <text x={20} y={296} fill={PAPER.blue} fontSize={17} style={HAND}>
          one response
        </text>
        <g transform="translate(20 308)">
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
          d={`M${HUB.x - 40} ${HUB.y + 52} C 210 320, 180 330, 150 340`}
          fill="none"
          stroke={PAPER.blue}
          strokeWidth={2.2}
          strokeLinecap="round"
          filter="url(#ed-fan-wob)"
        />
        <g className="ed-fan-in" style={fadeStyle(5.2)}>
          <Head x={150} y={340} angle={168} color={PAPER.blue} />
        </g>

        <text x={332} y={452} fill={PAPER.pencil} fontSize={15} style={HAND}>
          the gateway works out who holds what
        </text>
      </svg>
    </div>
  );
}
