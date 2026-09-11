"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { drawStyle, fadeStyle, FONT, HAND, MARKER, PAPER } from "./palette";
import { Folio, PaperGround, Wobble } from "./Sketch";

/**
 * "Composition protects the graph, Nitro protects your clients": the subgraph
 * team removes a field, the proof still passes composition in green, and the
 * highlighter then runs down the operations real clients publish. Where an
 * operation still asks for the removed field the verdict is written as
 * breaking, next to the risky and safe ones.
 *
 * Rest state: the change ticked, every operation highlighted and judged.
 *
 * Every line is lettered in `FONT` units, so the smallest one still reads at
 * the site's 11px label on a 375px screen; the two boxes and the operation
 * rows are spaced for that lettering.
 */

const CSS = `
.ed-hi .ed-hi-draw { stroke-dasharray: var(--len, 160); stroke-dashoffset: 0; }
.ed-hi .ed-hi-in { opacity: 1; }
.ed-hi .ed-hi-sweep { transform: scaleX(1); transform-box: fill-box; transform-origin: left center; }
.ed-hi[data-run="true"] .ed-hi-draw { animation: ed-hi-draw 1.2s ease-out both; }
.ed-hi[data-run="true"] .ed-hi-in { animation: ed-hi-in 0.7s ease-out both; }
.ed-hi[data-run="true"] .ed-hi-sweep { animation: ed-hi-sweep 0.9s cubic-bezier(0.5, 0, 0.2, 1) both; }
@keyframes ed-hi-draw {
  from { stroke-dashoffset: var(--len, 160); }
  to { stroke-dashoffset: 0; }
}
@keyframes ed-hi-in {
  from { opacity: 0; }
  to { opacity: 1; }
}
@keyframes ed-hi-sweep {
  from { transform: scaleX(0); }
  to { transform: scaleX(1); }
}
`;

const REMOVED = "subtotal: Money";

interface Operation {
  /** The client that published the operation. */
  readonly client: string;
  /** The operation Nitro has on file for it. */
  readonly name: string;
  /** What the operation asks for. */
  readonly asks: string;
  readonly verdict: "safe" | "risky" | "breaking";
}

/** The operations Nitro checks the change against, worst last. */
const OPERATIONS: readonly Operation[] = [
  {
    client: "Web",
    name: "orderDetail",
    asks: "total, lines",
    verdict: "safe",
  },
  {
    client: "Partner API",
    name: "invoiceExport",
    asks: "total, tax",
    verdict: "risky",
  },
  {
    client: "Mobile",
    name: "checkoutSummary",
    asks: REMOVED.split(":")[0],
    verdict: "breaking",
  },
];

const VERDICT_COLOR: Record<Operation["verdict"], string> = {
  safe: PAPER.green,
  risky: PAPER.inkSoft,
  breaking: PAPER.red,
};

const ROW_TOP = 250;
const ROW_H = 60;
const ROW_GAP = 14;
/** The change on the left, what the change cannot see on the right. */
const BOX = { left: 20, leftW: 320, right: 350, rightW: 270, h: 150 };

const rowY = (i: number) => ROW_TOP + i * (ROW_H + ROW_GAP);

export function HighlighterOps() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div className="ed-hi absolute inset-0" data-run={run ? "true" : "false"}>
      <style>{CSS}</style>
      <svg viewBox="0 0 640 480" className="h-full w-full" aria-hidden="true">
        <defs>
          <Wobble id="ed-hi-wob" scale={1.6} frequency={0.02} seed={17} />
          <Wobble id="ed-hi-wob2" scale={1} frequency={0.036} seed={8} />
        </defs>

        <PaperGround width={640} height={480} />
        <Folio x={20} y={26}>
          Fig. 4 - the same change, read twice
        </Folio>

        {/* The schema change, and the build that stays green */}
        <g transform={`translate(${BOX.left} 48)`}>
          <rect
            width={BOX.leftW}
            height={BOX.h}
            fill={PAPER.sheet}
            stroke={PAPER.ink}
            strokeWidth={2}
            filter="url(#ed-hi-wob2)"
          />
          <text
            x={18}
            y={36}
            fill={PAPER.ink}
            fontSize={FONT.heading}
            style={MARKER}
          >
            schema change
          </text>
          <text
            x={18}
            y={70}
            fill={PAPER.inkSoft}
            fontSize={FONT.caption}
            style={HAND}
          >
            {REMOVED}
          </text>
          <path
            className="ed-hi-draw"
            style={drawStyle(150, 0.4)}
            d="M14 64 C 70 60, 140 70, 214 64"
            fill="none"
            stroke={PAPER.red}
            strokeWidth={2.6}
            strokeLinecap="round"
            filter="url(#ed-hi-wob)"
          />
          <text
            x={18}
            y={98}
            fill={PAPER.pencil}
            fontSize={FONT.label}
            style={HAND}
          >
            field removed by the team
          </text>
          <g
            className="ed-hi-draw"
            style={drawStyle(46, 1.2)}
            transform="translate(14 104)"
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
          <text
            x={44}
            y={124}
            fill={PAPER.green}
            fontSize={FONT.label}
            style={HAND}
          >
            composition still passes
          </text>
        </g>

        {/* What federation cannot see: who still asks for it */}
        <g transform={`translate(${BOX.right} 48)`}>
          <rect
            width={BOX.rightW}
            height={BOX.h}
            fill={PAPER.sheet}
            stroke={PAPER.pencil}
            strokeWidth={1.8}
            strokeDasharray="8 8"
            filter="url(#ed-hi-wob2)"
          />
          <text
            x={18}
            y={36}
            fill={PAPER.pencil}
            fontSize={FONT.heading}
            style={MARKER}
          >
            out of sight
          </text>
          <text
            x={18}
            y={74}
            fill={PAPER.pencil}
            fontSize={FONT.label}
            style={HAND}
          >
            a client the subgraph
          </text>
          <text
            x={18}
            y={98}
            fill={PAPER.pencil}
            fontSize={FONT.label}
            style={HAND}
          >
            team never sees
          </text>
          <path
            className="ed-hi-draw"
            style={drawStyle(200, 1.8)}
            d="M40 120 C 90 138, 170 134, 232 116"
            fill="none"
            stroke={PAPER.pencil}
            strokeWidth={2}
            strokeLinecap="round"
            filter="url(#ed-hi-wob)"
          />
          <path
            className="ed-hi-in"
            style={fadeStyle(2.6)}
            d="M232 116 l-16 -3 M232 116 l-11 11"
            fill="none"
            stroke={PAPER.pencil}
            strokeWidth={2}
            strokeLinecap="round"
          />
        </g>

        <text
          x={20}
          y={230}
          fill={PAPER.ink}
          fontSize={FONT.heading}
          style={MARKER}
        >
          operations real clients publish
        </text>

        {/* The highlighter run: one verdict per published operation */}
        {OPERATIONS.map((op, i) => {
          const y = rowY(i);

          return (
            <g key={op.name}>
              <rect
                className="ed-hi-sweep"
                style={fadeStyle(3 + i * 0.8)}
                x={20}
                y={y + 6}
                width={430}
                height={34}
                fill={PAPER.highlight}
              />
              <text
                x={28}
                y={y + 30}
                fill={PAPER.ink}
                fontSize={FONT.caption}
                style={HAND}
              >
                {`${op.client} · ${op.name}`}
              </text>
              <text
                x={28}
                y={y + 54}
                fill={PAPER.inkSoft}
                fontSize={FONT.label}
                style={HAND}
              >
                {`asks for ${op.asks}`}
              </text>
              <g className="ed-hi-in" style={fadeStyle(3.9 + i * 0.8)}>
                <text
                  x={472}
                  y={y + 34}
                  fill={VERDICT_COLOR[op.verdict]}
                  fontSize={FONT.caption}
                  style={HAND}
                >
                  {op.verdict}
                </text>
                {op.verdict === "breaking" ? (
                  <path
                    className="ed-hi-draw"
                    style={drawStyle(140, 5.6)}
                    d={`M468 ${y + 46} C 502 ${y + 52}, 548 ${y + 40}, 590 ${y + 48}`}
                    fill="none"
                    stroke={PAPER.red}
                    strokeWidth={2.4}
                    strokeLinecap="round"
                    filter="url(#ed-hi-wob)"
                  />
                ) : null}
              </g>
            </g>
          );
        })}
      </svg>
    </div>
  );
}
