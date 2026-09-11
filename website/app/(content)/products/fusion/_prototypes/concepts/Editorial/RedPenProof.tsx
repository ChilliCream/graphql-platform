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
import { Folio, PaperGround, Wobble } from "./Sketch";

/**
 * "Any GraphQL server, no plugin" drawn as a proof sheet. Composition reads the
 * five source schemas the way an editor reads a galley: a green tick in the
 * margin for each line that agrees with the others, and where the Ordering
 * schema declares `total` as a different type, the red pen crosses the field
 * out, writes the conflict in the margin and stops the build.
 *
 * Rest state: the proof marked up, the conflict struck and the stamp in place.
 *
 * Every line is lettered in `FONT` units, so the smallest one still reads at
 * the site's 11px label on a 375px screen; the rows, the margin and the stamp
 * are spaced for that lettering.
 */

const CSS = `
.ed-pen .ed-pen-draw { stroke-dasharray: var(--len, 160); stroke-dashoffset: 0; }
.ed-pen .ed-pen-in { opacity: 1; }
.ed-pen .ed-pen-stamp { transform: rotate(-7deg); transform-box: fill-box; transform-origin: center; }
.ed-pen[data-run="true"] .ed-pen-draw { animation: ed-pen-draw 1.3s ease-out both; }
.ed-pen[data-run="true"] .ed-pen-in { animation: ed-pen-in 0.7s ease-out both; }
.ed-pen[data-run="true"] .ed-pen-stamp { animation: ed-pen-stamp 0.7s cubic-bezier(0.3, 1.4, 0.5, 1) 6.8s both; }
@keyframes ed-pen-draw {
  from { stroke-dashoffset: var(--len, 160); }
  to { stroke-dashoffset: 0; }
}
@keyframes ed-pen-in {
  from { opacity: 0; }
  to { opacity: 1; }
}
@keyframes ed-pen-stamp {
  from { opacity: 0; transform: scale(1.5) rotate(-14deg); }
  to { opacity: 1; transform: scale(1) rotate(-7deg); }
}
`;

const ROW_TOP = 92;
const ROW_H = 56;
const ROW_GAP = 10;
const TICK_X = 470;

/** The field every source schema declares, and the one that disagrees. */
const FIELD = "total: Money";
const CONFLICT_FIELD = "total: Int";
const CONFLICT = "Ordering";

const rowY = (i: number) => ROW_TOP + i * (ROW_H + ROW_GAP);

export function RedPenProof() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;
  const conflictRow = NOTES.findIndex((n) => n.name === CONFLICT);

  return (
    <div className="ed-pen absolute inset-0" data-run={run ? "true" : "false"}>
      <style>{CSS}</style>
      <svg viewBox="0 0 640 480" className="h-full w-full" aria-hidden="true">
        <defs>
          <Wobble id="ed-pen-wob" scale={1.7} frequency={0.02} seed={13} />
          <Wobble id="ed-pen-wob2" scale={1.1} frequency={0.036} seed={6} />
        </defs>

        <PaperGround width={640} height={480} ruled />
        <Folio x={20} y={26}>
          Fig. 3 - the proof, marked up
        </Folio>
        <text
          x={20}
          y={62}
          fill={PAPER.ink}
          fontSize={FONT.heading}
          style={MARKER}
        >
          composition, the one build step
        </text>
        <Folio x={TICK_X} y={80}>
          Margin
        </Folio>

        {NOTES.map((note, i) => {
          const y = rowY(i);
          const conflicted = note.name === CONFLICT;

          return (
            <g key={note.name}>
              <path
                className="ed-pen-draw"
                style={drawStyle(430, 0.2 + i * 0.18)}
                d={`M20 ${y + ROW_H} C 160 ${y + ROW_H + 4}, 320 ${y + ROW_H - 4}, 448 ${y + ROW_H + 2}`}
                fill="none"
                stroke={PAPER.ink}
                strokeWidth={1.4}
                strokeLinecap="round"
                opacity={0.35}
              />
              <text
                x={22}
                y={y + 24}
                fill={PAPER.ink}
                fontSize={FONT.caption}
                style={HAND}
              >
                {note.name}
              </text>
              <text
                x={22}
                y={y + 48}
                fill={PAPER.inkSoft}
                fontSize={FONT.label}
                style={HAND}
              >
                {`${note.language} · ${note.specLabel}`}
              </text>
              <text
                x={262}
                y={y + 34}
                fill={conflicted ? PAPER.red : PAPER.inkSoft}
                fontSize={FONT.label}
                style={HAND}
              >
                {conflicted ? CONFLICT_FIELD : FIELD}
              </text>

              {conflicted ? null : (
                <g
                  className="ed-pen-draw"
                  style={drawStyle(46, 1.4 + i * 0.5)}
                  transform={`translate(${TICK_X} ${y + 16})`}
                >
                  <path
                    d="M0 10 l9 11 l19 -24"
                    fill="none"
                    stroke={PAPER.green}
                    strokeWidth={3}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </g>
              )}
            </g>
          );
        })}

        {/* The red pen: the conflicting field is struck out and written up */}
        <g>
          <path
            className="ed-pen-draw"
            style={drawStyle(120, 5.4)}
            d={`M256 ${rowY(conflictRow) + 30} C 300 ${rowY(conflictRow) + 24}, 350 ${rowY(conflictRow) + 34}, 400 ${rowY(conflictRow) + 28}`}
            fill="none"
            stroke={PAPER.red}
            strokeWidth={3}
            strokeLinecap="round"
            filter="url(#ed-pen-wob)"
          />
          <path
            className="ed-pen-draw"
            style={drawStyle(320, 5.7)}
            d={`M250 ${rowY(conflictRow) + 10} C 350 ${rowY(conflictRow) - 2}, 412 ${rowY(conflictRow) + 22}, 392 ${rowY(conflictRow) + 44} C 344 ${rowY(conflictRow) + 58}, 262 ${rowY(conflictRow) + 54}, 250 ${rowY(conflictRow) + 30}`}
            fill="none"
            stroke={PAPER.red}
            strokeWidth={2}
            strokeLinecap="round"
            opacity={0.9}
            filter="url(#ed-pen-wob)"
          />
          <path
            className="ed-pen-draw"
            style={drawStyle(120, 6.1)}
            d={`M408 ${rowY(conflictRow) + 24} C 430 ${rowY(conflictRow) + 20}, 448 ${rowY(conflictRow) + 18}, 466 ${rowY(conflictRow) + 14}`}
            fill="none"
            stroke={PAPER.red}
            strokeWidth={2}
            strokeLinecap="round"
            filter="url(#ed-pen-wob)"
          />
          <g className="ed-pen-in" style={fadeStyle(6.4)}>
            <text
              x={TICK_X}
              y={rowY(conflictRow) + 22}
              fill={PAPER.red}
              fontSize={FONT.label}
              style={HAND}
            >
              type
            </text>
            <text
              x={TICK_X}
              y={rowY(conflictRow) + 46}
              fill={PAPER.red}
              fontSize={FONT.label}
              style={HAND}
            >
              conflict
            </text>
          </g>
        </g>

        {/* The pipeline stops here, not the gateway */}
        <g className="ed-pen-stamp">
          <rect
            x={196}
            y={416}
            width={256}
            height={56}
            fill="none"
            stroke={PAPER.red}
            strokeWidth={4}
            opacity={0.85}
            filter="url(#ed-pen-wob2)"
          />
          <text
            x={324}
            y={454}
            textAnchor="middle"
            fill={PAPER.red}
            fontSize={FONT.heading}
            style={MARKER}
            opacity={0.9}
          >
            build stopped
          </text>
        </g>
        <text
          x={22}
          y={438}
          fill={PAPER.pencil}
          fontSize={FONT.label}
          style={HAND}
        >
          nothing
        </text>
        <text
          x={22}
          y={462}
          fill={PAPER.pencil}
          fontSize={FONT.label}
          style={HAND}
        >
          deploys
        </text>
      </svg>
    </div>
  );
}
