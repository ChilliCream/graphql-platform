"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { drawStyle, fadeStyle, HAND, MARKER, NOTES, PAPER } from "./palette";
import { Folio, PaperGround, Wobble } from "./Sketch";

/**
 * The Nitro band, drawn as the editor's margin notes: a hand-plotted chart of
 * latency, throughput and error rate for the gateway and for each subgraph
 * behind it, with the highlighter left over the gateway row and a note that
 * every schema change is read against the operations clients have published.
 *
 * Rest state: every chart plotted and the highlighter in place.
 */

const CSS = `
.ed-note .ed-note-draw { stroke-dasharray: var(--len, 140); stroke-dashoffset: 0; }
.ed-note .ed-note-in { opacity: 1; }
.ed-note .ed-note-sweep { transform: scaleX(1); transform-box: fill-box; transform-origin: left center; }
.ed-note[data-run="true"] .ed-note-draw { animation: ed-note-draw 12s ease-in-out infinite; }
.ed-note[data-run="true"] .ed-note-in { animation: ed-note-in 12s ease-in-out infinite; }
.ed-note[data-run="true"] .ed-note-sweep { animation: ed-note-sweep 12s cubic-bezier(0.5, 0, 0.2, 1) infinite; }
@keyframes ed-note-draw {
  0% { stroke-dashoffset: var(--len, 140); }
  16%, 100% { stroke-dashoffset: 0; }
}
@keyframes ed-note-in {
  0%, 8% { opacity: 0; }
  20%, 100% { opacity: 1; }
}
@keyframes ed-note-sweep {
  0%, 10% { transform: scaleX(0); }
  26%, 100% { transform: scaleX(1); }
}
`;

const ROW_TOP = 86;
const ROW_H = 36;
const CHART_W = 108;
const CHART_H = 26;
const COLUMNS = [244, 380, 516];
const METRICS = ["latency", "throughput", "error rate"];

/** Gateway first, then the subgraphs it calls. */
const ROWS = ["Gateway", ...NOTES.map((note) => note.name)];

const rowY = (i: number) => ROW_TOP + i * ROW_H;

/**
 * Deterministic plot: the same eight readings every render, so the server and
 * the browser draw the same chart. Row and metric shift the curve.
 */
function series(row: number, metric: number): string {
  const points = Array.from({ length: 8 }, (_, i) => {
    const wave = Math.sin((i + row * 2 + metric * 3) * 0.9);
    const drift = ((i * 37 + row * 13 + metric * 7) % 11) / 11;
    const v = 0.5 + wave * 0.26 + drift * 0.22;
    return {
      x: (i / 7) * CHART_W,
      y: CHART_H - Math.min(0.95, Math.max(0.08, v)) * CHART_H,
    };
  });

  return points
    .map((p, i) => `${i === 0 ? "M" : "L"}${p.x.toFixed(1)} ${p.y.toFixed(1)}`)
    .join(" ");
}

export function MarginNotes() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div className="ed-note absolute inset-0" data-run={run ? "true" : "false"}>
      <style>{CSS}</style>
      <svg viewBox="0 0 640 360" className="h-full w-full" aria-hidden="true">
        <defs>
          <Wobble id="ed-note-wob" scale={1.5} frequency={0.022} seed={19} />
          <Wobble id="ed-note-wob2" scale={1} frequency={0.04} seed={12} />
        </defs>

        <PaperGround width={640} height={360} />
        <Folio x={20} y={26}>
          Fig. 5 - margin notes
        </Folio>
        <text x={20} y={58} fill={PAPER.ink} fontSize={20} style={MARKER}>
          the gateway, and each subgraph behind it
        </text>

        {COLUMNS.map((x, i) => (
          <Folio key={METRICS[i]} x={x} y={78}>
            {METRICS[i]}
          </Folio>
        ))}

        {/* The highlighter someone left over the gateway row */}
        <rect
          className="ed-note-sweep"
          x={16}
          y={rowY(0) - 6}
          width={604}
          height={32}
          fill={PAPER.highlight}
        />

        {ROWS.map((name, row) => (
          <g key={name}>
            <text
              x={20}
              y={rowY(row) + 16}
              fill={row === 0 ? PAPER.ink : PAPER.inkSoft}
              fontSize={row === 0 ? 18 : 16}
              style={HAND}
            >
              {name}
            </text>
            {row > 0 ? (
              <text
                x={140}
                y={rowY(row) + 16}
                fill={PAPER.pencil}
                fontSize={12}
                style={HAND}
              >
                {NOTES[row - 1].language}
              </text>
            ) : (
              <text
                x={140}
                y={rowY(row) + 16}
                fill={PAPER.pencil}
                fontSize={12}
                style={HAND}
              >
                composite schema
              </text>
            )}

            {COLUMNS.map((x, metric) => (
              <g
                key={METRICS[metric]}
                transform={`translate(${x} ${rowY(row) - 8})`}
              >
                <path
                  d={`M0 ${CHART_H} h${CHART_W}`}
                  stroke={PAPER.ink}
                  strokeWidth={1}
                  opacity={0.2}
                />
                <path
                  className="ed-note-draw"
                  style={drawStyle(200, 0.3 + row * 0.24 + metric * 0.12)}
                  d={series(row, metric)}
                  fill="none"
                  stroke={metric === 2 ? PAPER.red : PAPER.blue}
                  strokeWidth={2.2}
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  opacity={row === 0 ? 1 : 0.75}
                  filter="url(#ed-note-wob2)"
                />
              </g>
            ))}
          </g>
        ))}

        {/* The note in the margin, written last */}
        <g className="ed-note-in" style={fadeStyle(2.6)}>
          <path
            className="ed-note-draw"
            style={drawStyle(560, 2.2)}
            d="M20 306 C 180 312, 420 300, 620 308"
            fill="none"
            stroke={PAPER.pencil}
            strokeWidth={1.6}
            strokeLinecap="round"
            filter="url(#ed-note-wob)"
          />
          <text x={20} y={334} fill={PAPER.pencil} fontSize={16} style={HAND}>
            every schema change read against the operations clients published
          </text>
        </g>
      </svg>
    </div>
  );
}
