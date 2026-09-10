"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import {
  drawStyle,
  fadeStyle,
  HAND,
  MARKER,
  NOTES,
  PAPER,
  SOURCES,
} from "./palette";
import { Folio, PaperGround, StickyNote, Wobble } from "./Sketch";

/**
 * "Both specifications, one gateway" drawn as two columns of sticky notes over
 * one sheet: the notes written to the GraphQL Federation specification on the
 * left, the Apollo Federation ones on the right, both stuck onto the same
 * composite schema, with the taped OpenAPI and gRPC cards joining it too. The
 * Shipping note walks from one column to the other and back, on its own.
 *
 * Rest state: both columns written up, the ghost slot outlined in pencil.
 */

const CSS = `
.ed-spec .ed-spec-draw { stroke-dasharray: var(--len, 200); stroke-dashoffset: 0; }
.ed-spec .ed-spec-in { opacity: 1; }
.ed-spec .ed-spec-move { transform: translate(0px, 0px); }
.ed-spec[data-run="true"] .ed-spec-draw { animation: ed-spec-draw 14s ease-in-out infinite; }
.ed-spec[data-run="true"] .ed-spec-in { animation: ed-spec-in 14s ease-in-out infinite; }
.ed-spec[data-run="true"] .ed-spec-move { animation: ed-spec-move 14s cubic-bezier(0.7, 0, 0.3, 1) infinite; }
@keyframes ed-spec-draw {
  0% { stroke-dashoffset: var(--len, 200); }
  16%, 100% { stroke-dashoffset: 0; }
}
@keyframes ed-spec-in {
  0%, 8% { opacity: 0; }
  20%, 100% { opacity: 1; }
}
@keyframes ed-spec-move {
  0%, 34% { transform: translate(0px, 0px); }
  50%, 74% { transform: translate(-342px, 128px); }
  90%, 100% { transform: translate(0px, 0px); }
}
`;

const NOTE_W = 240;
const NOTE_H = 56;
const LEFT_X = 24;
const RIGHT_X = 366;
const SHEET = { x: 42, y: 336, w: 556, h: 126 };

/** Left column: source schemas written to the GraphQL Federation spec. */
const LEFT = [
  { note: NOTES[0], y: 72 },
  { note: NOTES[2], y: 136 },
  { note: NOTES[4], y: 200 },
];

/** Right column: source schemas written to Apollo Federation. */
const RIGHT = [{ note: NOTES[1], y: 72 }];

/** The note that walks across, and the slot it walks into. */
const MOVER = { note: NOTES[3], y: 136 };
const GHOST = { x: LEFT_X, y: 264 };

/** Stroke from a note down onto the composite sheet. */
function feedPath(x: number, y: number): string {
  const sx = x + NOTE_W / 2;
  return `M${sx} ${y + NOTE_H + 4} C ${sx} ${y + 90}, ${SHEET.x + SHEET.w / 2} ${SHEET.y - 70}, ${SHEET.x + SHEET.w / 2} ${SHEET.y - 6}`;
}

export function SpecStickies() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div className="ed-spec absolute inset-0" data-run={run ? "true" : "false"}>
      <style>{CSS}</style>
      <svg viewBox="0 0 640 480" className="h-full w-full" aria-hidden="true">
        <defs>
          <Wobble id="ed-spec-wob" scale={1.6} frequency={0.018} seed={9} />
          <Wobble id="ed-spec-wob2" scale={1} frequency={0.034} seed={4} />
        </defs>

        <PaperGround width={640} height={480} />
        <Folio x={20} y={24}>
          Fig. 2 - two columns, one sheet
        </Folio>

        {/* Column headings, underlined by hand */}
        <text x={LEFT_X} y={52} fill={PAPER.ink} fontSize={19} style={MARKER}>
          GraphQL Federation
        </text>
        <path
          className="ed-spec-draw"
          style={drawStyle(250, 0.1)}
          d={`M${LEFT_X} 60 C ${LEFT_X + 80} 65, ${LEFT_X + 170} 56, ${LEFT_X + 246} 62`}
          fill="none"
          stroke={PAPER.ink}
          strokeWidth={2.2}
          strokeLinecap="round"
          filter="url(#ed-spec-wob)"
        />
        <text x={RIGHT_X} y={52} fill={PAPER.ink} fontSize={19} style={MARKER}>
          Apollo Federation
        </text>
        <path
          className="ed-spec-draw"
          style={drawStyle(250, 0.3)}
          d={`M${RIGHT_X} 60 C ${RIGHT_X + 80} 66, ${RIGHT_X + 168} 56, ${RIGHT_X + 240} 62`}
          fill="none"
          stroke={PAPER.ink}
          strokeWidth={2.2}
          strokeLinecap="round"
          filter="url(#ed-spec-wob)"
        />

        {/* The slot the travelling note can take, outlined in pencil */}
        <rect
          x={GHOST.x}
          y={GHOST.y}
          width={NOTE_W}
          height={NOTE_H}
          fill="none"
          stroke={PAPER.pencil}
          strokeWidth={1.6}
          strokeDasharray="7 8"
          opacity={0.7}
        />
        <text
          x={GHOST.x + 14}
          y={GHOST.y + 34}
          fill={PAPER.pencil}
          fontSize={15}
          style={HAND}
        >
          moves across, no cutover
        </text>

        {/* Feed strokes onto the sheet */}
        {[...LEFT, ...RIGHT].map((row, i) => (
          <path
            key={row.note.name}
            className="ed-spec-draw"
            style={drawStyle(260, 0.8 + i * 0.2)}
            d={feedPath(i < LEFT.length ? LEFT_X : RIGHT_X, row.y)}
            fill="none"
            stroke={PAPER.ink}
            strokeWidth={1.8}
            strokeLinecap="round"
            opacity={0.55}
            filter="url(#ed-spec-wob)"
          />
        ))}

        {/* The two columns of source schemas */}
        {LEFT.map((row) => (
          <StickyNote
            key={row.note.name}
            x={LEFT_X}
            y={row.y}
            width={NOTE_W}
            height={NOTE_H}
            tilt={-1.4}
            stock={row.note.stock}
            name={row.note.name}
            caption={`${row.note.language} · ${row.note.specLabel}`}
          />
        ))}
        {RIGHT.map((row) => (
          <StickyNote
            key={row.note.name}
            x={RIGHT_X}
            y={row.y}
            width={NOTE_W}
            height={NOTE_H}
            tilt={1.6}
            stock={row.note.stock}
            name={row.note.name}
            caption={`${row.note.language} · ${row.note.specLabel}`}
          />
        ))}

        {/* The subgraph that moves across, one note at a time */}
        <g className="ed-spec-move">
          <StickyNote
            x={RIGHT_X}
            y={MOVER.y}
            width={NOTE_W}
            height={NOTE_H}
            tilt={1.2}
            stock={MOVER.note.stock}
            name={MOVER.note.name}
            caption={`${MOVER.note.language} · ${MOVER.note.specLabel}`}
          />
        </g>

        {/* The composite schema everything is stuck onto */}
        <g filter="url(#ed-spec-wob)">
          <rect
            x={SHEET.x}
            y={SHEET.y}
            width={SHEET.w}
            height={SHEET.h}
            fill={PAPER.sheet}
            stroke={PAPER.ink}
            strokeWidth={2.6}
          />
          <rect
            x={SHEET.x + 6}
            y={SHEET.y + 6}
            width={SHEET.w}
            height={SHEET.h}
            fill="none"
            stroke={PAPER.ink}
            strokeWidth={1.2}
            opacity={0.3}
          />
        </g>
        <text
          x={SHEET.x + 20}
          y={SHEET.y + 34}
          fill={PAPER.ink}
          fontSize={22}
          style={MARKER}
        >
          composite schema
        </text>

        {/* Sources that are not GraphQL servers, taped onto the same sheet */}
        {SOURCES.map((source, i) => (
          <g
            key={source.name}
            className="ed-spec-in"
            style={fadeStyle(1.6 + i * 0.3)}
            transform={`translate(${SHEET.x + 20 + i * 268} ${SHEET.y + 48})`}
          >
            <rect
              width={248}
              height={62}
              fill={PAPER.sheet}
              stroke={PAPER.inkSoft}
              strokeWidth={1.6}
              strokeDasharray="9 7"
              filter="url(#ed-spec-wob2)"
            />
            <rect
              x={92}
              y={-9}
              width={60}
              height={18}
              fill={PAPER.highlight}
              opacity={0.8}
              transform="rotate(-2 122 0)"
            />
            <text x={16} y={26} fill={PAPER.ink} fontSize={16} style={HAND}>
              {source.name}
            </text>
            <text x={16} y={48} fill={PAPER.inkSoft} fontSize={13} style={HAND}>
              {`${source.kind} document, same composition`}
            </text>
          </g>
        ))}
      </svg>
    </div>
  );
}
