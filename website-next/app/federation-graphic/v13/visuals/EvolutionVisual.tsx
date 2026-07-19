"use client";

/**
 * The graph evolves; clients never notice. One client keeps sending the same
 * query against the composite schema while the actual subgraph schemas on the
 * right churn behind it: Catalog strikes out price and hands it to Billing,
 * whose @override row lands in teal, in the same window where the composite's
 * owner dot crossfades coral to amber. The composite card itself never dims;
 * its UNCHANGED tag only pulses gently as the change lands. Query and
 * response beads ride the client lane without pause for the whole loop, and a
 * frozen frame still shows the complete end state: price struck out of
 * Catalog, alive in Billing.
 */

import { MONO_FONT } from "../palette";
import { PulseGlyph, clamp01, measure, ramp, useVisual } from "./anim";
import { CANON, INK_DIM } from "./stage";

const T = 12000;
const BEAD_CYCLE = 2400;

const C = { x: 40, y: 88, w: 240 } as const;
const QUERY_LINES = [
  "{",
  '  product(id: "P-401") {',
  "    name",
  "    price",
  "    delivery",
  "  }",
  "}",
] as const;

const M = { x: 339, y: 115, w: 242 } as const;
const DOT_X = M.x + M.w - 20;
const ROWS = [
  { code: "id: ID!", dot: CANON[0].color },
  { code: "name: String!", dot: CANON[0].color },
  { code: "price: Money!", dot: null },
  { code: "delivery: String!", dot: CANON[3].color },
] as const;
const PRICE_DOT: readonly [number, number] = [DOT_X, M.y + 44 + 2 * 18];

const LANE_Y = 177;
const LANE_Q = measure([
  [C.x + C.w + 4, LANE_Y - 2.5],
  [M.x - 2, LANE_Y - 2.5],
]);
const LANE_R = measure([
  [M.x - 2, LANE_Y + 2.5],
  [C.x + C.w + 4, LANE_Y + 2.5],
]);
const Q_PHASES = [0] as const;
const R_PHASES = [1200] as const;

const CAT = { x: 640, y: 88, w: 250 } as const;
const BIL = { x: 640, y: 194, w: 250 } as const;
const STEM_X = CAT.x + 4;
const COMP_EDGE = M.x + M.w;

const TAG_PULSES = [2600, 6200, 9600] as const;

export function EvolutionVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // The lane is animated by pulses; the static placeholder beads hide.
    h.setO("sq", 0);
    h.setO("sr", 0);

    // Queries out and responses back, riding the lane the whole loop.
    Q_PHASES.forEach((ph, i) => {
      const u = ((t + BEAD_CYCLE - ph) % BEAD_CYCLE) / BEAD_CYCLE;
      const op = 0.95 * clamp01(Math.min(u, 1 - u) * 6);
      h.placePulse(`qb${i}`, LANE_Q, u, op, 1.9);
    });
    R_PHASES.forEach((ph, i) => {
      const u = ((t + BEAD_CYCLE - ph) % BEAD_CYCLE) / BEAD_CYCLE;
      const op = 0.95 * clamp01(Math.min(u, 1 - u) * 6);
      h.placePulse(`rb${i}`, LANE_R, u, op, 1.9);
    });

    // Price changes owner. Coral overlay fades out over the amber dot, then
    // quietly restores near the loop end so the wrap is seamless.
    const coral = clamp01(1 - ramp(t, 2200, 3200) + ramp(t, 11400, 11900));
    h.setO("priceCoral", coral);
    h.setRing("priceRing", (t - 2200) / 600, 4, 9);
    // The owner micro-caption holds while price is billing-owned (amber).
    h.setO("ownerCaption", 1 - coral);

    // The source schemas follow the same window: catalog's price row strikes
    // out while billing's @override row lands (never below its 0.4 floor);
    // both restore quietly with the dot so the wrap is seamless.
    h.setO("catPriceFull", coral);
    h.setO("catStrike", 1 - coral);
    h.setO("bilPrice", 0.4 + 0.6 * (1 - coral));
    h.setRing("ovrRing", (t - 3000) / 650, 5, 11);
    h.setO("ovrGlow", 0.55 * Math.sin(Math.PI * ramp(t, 2950, 3950)));

    // The UNCHANGED tag pulses gently once per change.
    let bump = 0;
    TAG_PULSES.forEach((s) => {
      bump = Math.max(bump, Math.sin(Math.PI * ramp(t, s, s + 900)));
    });
    h.setO("tag", 0.78 + 0.22 * bump);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      {/* Phones: the same story as a plain stacked column. */}
      <div className="space-y-4 sm:hidden">
        <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
          <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
            One client · same query
          </div>
          <div className="border-cc-card-border mt-2 overflow-x-auto border-t pt-2 font-mono text-[12px] leading-6 text-[#c9d4e8]">
            {QUERY_LINES.map((code, i) => (
              <div key={i} className="whitespace-pre">
                {code}
              </div>
            ))}
          </div>
        </div>

        <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
          <div className="flex items-center justify-between gap-2">
            <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
              Composite · what clients see
            </span>
            <span className="rounded-full border border-[#5eead4]/45 bg-[#5eead4]/10 px-2 py-0.5 font-mono text-[9px] tracking-[0.16em] text-[#5eead4] uppercase">
              Unchanged
            </span>
          </div>
          <div className="border-cc-card-border mt-2 space-y-1 border-t pt-2 font-mono text-[12px] leading-6">
            {[
              { code: "id: ID!", dot: CANON[0].color },
              { code: "name: String!", dot: CANON[0].color },
              { code: "price: Money!", dot: CANON[1].color },
              { code: "delivery: String!", dot: CANON[3].color },
            ].map((r) => (
              <div key={r.code} className="flex items-center gap-2">
                <span className="whitespace-pre text-[#c9d4e8]">{r.code}</span>
                <span
                  className="ml-auto inline-block h-2 w-2 shrink-0 rounded-full"
                  style={{ background: r.dot }}
                />
              </div>
            ))}
          </div>
        </div>

        <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
          Behind the composite
        </div>

        <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
          <div className="flex items-center gap-2">
            <span
              className="inline-block h-2.5 w-2.5 rounded-[3px]"
              style={{ background: CANON[0].color }}
            />
            <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
              Catalog
            </span>
          </div>
          <div className="border-cc-card-border mt-2 border-t pt-2 font-mono text-[12px] leading-6 text-[#c9d4e8]">
            <div className="whitespace-pre">{"type Product {"}</div>
            <div className="whitespace-pre">{"  name: String!"}</div>
            <div className="flex items-center gap-2">
              <span className="whitespace-pre text-[#c9d4e8] line-through decoration-[#f27765] opacity-60">
                {"  price: Money!"}
              </span>
              <span className="ml-auto flex shrink-0 items-center gap-1 text-[10px]">
                <svg
                  width={9}
                  height={8}
                  viewBox="0 0 9 8"
                  aria-hidden="true"
                  className="shrink-0"
                  style={{ color: CANON[0].color }}
                >
                  <path
                    d="M0 4 h8 M5.5 1.5 L8 4 L5.5 6.5"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth={1.5}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
                <span style={{ color: CANON[1].color }}>billing</span>
              </span>
            </div>
            <div className="whitespace-pre">{"}"}</div>
          </div>
        </div>

        <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
          <div className="flex items-center gap-2">
            <span
              className="inline-block h-2.5 w-2.5 rounded-[3px]"
              style={{ background: CANON[1].color }}
            />
            <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
              Billing
            </span>
          </div>
          <div className="border-cc-card-border mt-2 overflow-x-auto border-t pt-2 font-mono text-[12px] leading-6 text-[#c9d4e8]">
            <div className="whitespace-pre">{"type Product {"}</div>
            <div className="whitespace-pre">{"  price: Money!"}</div>
            <div className="whitespace-pre">
              <span className="text-[#5eead4]">
                {'    @override(from: "Catalog")'}
              </span>
            </div>
            <div className="whitespace-pre">{"}"}</div>
          </div>
        </div>

        <div className="text-cc-ink-dim font-mono text-[10px]">
          v42 · price moved to billing · the surface holds
        </div>
      </div>

      {/* Larger screens: the animated stream canvas. */}
      <div className="hidden overflow-x-auto sm:block">
        <svg
          viewBox="28 58 872 286"
          width="100%"
          className="block min-w-[640px]"
        >
          <defs>
            <filter id="ev8-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
          </defs>

          {/* The one client and its unchanging query. */}
          <rect
            x={C.x}
            y={C.y}
            width={C.w}
            height={40 + QUERY_LINES.length * 18 + 12}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <text
            x={C.x + 14}
            y={C.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            ONE CLIENT · SAME QUERY
          </text>
          <line
            x1={C.x}
            x2={C.x + C.w}
            y1={C.y + 32}
            y2={C.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {QUERY_LINES.map((code, i) => (
            <text
              key={i}
              x={C.x + 16}
              y={C.y + 50 + i * 18}
              xmlSpace="preserve"
              fontFamily={MONO_FONT}
              fontSize={12}
              fill="#c9d4e8"
            >
              {code}
            </text>
          ))}
          <text
            x={C.x + C.w / 2}
            y={C.y + 40 + QUERY_LINES.length * 18 + 32}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.18em"
            fill={INK_DIM}
            opacity={0.75}
          >
            QUERIES · NEVER PAUSED
          </text>

          {/* The client lane into the composite. */}
          <line
            x1={C.x + C.w}
            x2={M.x}
            y1={LANE_Y}
            y2={LANE_Y}
            stroke="rgba(139,160,188,0.55)"
            strokeWidth={1.5}
          />
          <circle
            ref={set("sq")}
            cx={306}
            cy={LANE_Y - 2.5}
            r={2}
            fill="#ffffff"
            opacity={0.9}
          />
          <circle
            ref={set("sr")}
            cx={322}
            cy={LANE_Y + 2.5}
            r={2}
            fill={CANON[2].color}
            opacity={0.9}
          />

          {/* The composite: the stable surface clients see. */}
          <rect
            x={M.x}
            y={M.y}
            width={M.w}
            height={40 + ROWS.length * 18 + 12}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <text
            x={M.x + 14}
            y={M.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            COMPOSITE · WHAT CLIENTS SEE
          </text>
          <line
            x1={M.x}
            x2={M.x + M.w}
            y1={M.y + 32}
            y2={M.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {ROWS.map((r, i) => (
            <g key={i}>
              <text
                x={M.x + 16}
                y={M.y + 48 + i * 18}
                xmlSpace="preserve"
                fontFamily={MONO_FONT}
                fontSize={12}
                fill="#c9d4e8"
              >
                {r.code}
              </text>
              {r.dot && (
                <circle cx={DOT_X} cy={M.y + 44 + i * 18} r={3} fill={r.dot} />
              )}
            </g>
          ))}
          {/* The price owner dot: amber end state, coral overlay for the loop. */}
          <circle
            cx={PRICE_DOT[0]}
            cy={PRICE_DOT[1]}
            r={3}
            fill={CANON[1].color}
          />
          <circle
            ref={set("priceCoral")}
            cx={PRICE_DOT[0]}
            cy={PRICE_DOT[1]}
            r={3}
            fill={CANON[0].color}
            opacity={0}
          />
          <circle
            ref={set("priceRing")}
            cx={PRICE_DOT[0]}
            cy={PRICE_DOT[1]}
            r={4}
            fill="none"
            stroke={CANON[1].color}
            strokeWidth={1.5}
            opacity={0}
          />

          {/* The UNCHANGED badge above the card's top-right corner. */}
          <g ref={set("tag")} opacity={0.92}>
            <rect
              x={M.x + M.w - 84}
              y={M.y - 26}
              width={84}
              height={18}
              rx={9}
              fill="rgba(94,234,212,0.08)"
              stroke="rgba(94,234,212,0.45)"
            />
            <text
              x={M.x + M.w - 42}
              y={M.y - 13.5}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9}
              letterSpacing="0.16em"
              fill="#5eead4"
            >
              UNCHANGED
            </text>
          </g>

          {/* The owner line under the composite: price now resolves from billing,
            yet the surface the client sees is untouched. */}
          <text
            ref={set("ownerCaption")}
            x={M.x + 14}
            y={M.y + 144}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.04em"
            fill={INK_DIM}
            opacity={1}
          >
            <tspan>{"price · owner: "}</tspan>
            <tspan fill={CANON[1].color}>billing</tspan>
            <tspan>{" · clients unaffected"}</tspan>
          </text>

          {/* The source schemas: the churn the composite hides. */}
          <text
            x={CAT.x}
            y={76}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            BEHIND THE COMPOSITE
          </text>

          {/* The source columns feed the composite; the cards hang on them and
            each runs a dim connector into the composite's price row. */}
          <path
            d={`M${STEM_X} ${CAT.y + 66.5} C ${STEM_X - 30} ${CAT.y + 66.5}, ${COMP_EDGE + 22} 192, ${COMP_EDGE} 192`}
            fill="none"
            stroke={CANON[0].color}
            strokeWidth={1.5}
            strokeOpacity={0.4}
            strokeDasharray="3 4"
          />
          <path
            d={`M${STEM_X} ${BIL.y + 52.5} C ${STEM_X - 30} ${BIL.y + 52.5}, ${COMP_EDGE + 22} 198, ${COMP_EDGE} 198`}
            fill="none"
            stroke={CANON[1].color}
            strokeWidth={1.5}
            strokeOpacity={0.4}
            strokeDasharray="3 4"
          />
          <line
            x1={STEM_X}
            x2={STEM_X}
            y1={CAT.y - 4}
            y2={CAT.y + 94}
            stroke={CANON[0].color}
            strokeWidth={2}
            strokeOpacity={0.85}
          />
          <line
            x1={STEM_X}
            x2={STEM_X}
            y1={BIL.y - 4}
            y2={BIL.y + 94}
            stroke={CANON[1].color}
            strokeWidth={2}
            strokeOpacity={0.85}
          />

          {/* Catalog: hands price to billing mid-loop. */}
          <rect
            x={CAT.x}
            y={CAT.y}
            width={CAT.w}
            height={90}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <rect
            x={CAT.x + 14}
            y={CAT.y + 10}
            width={10}
            height={10}
            rx={3}
            fill={CANON[0].color}
          />
          <text
            x={CAT.x + 32}
            y={CAT.y + 19}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            CATALOG
          </text>
          <line
            x1={CAT.x}
            x2={CAT.x + CAT.w}
            y1={CAT.y + 29}
            y2={CAT.y + 29}
            stroke="rgba(245,241,234,0.1)"
          />
          <text
            x={CAT.x + 16}
            y={CAT.y + 42}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={10.5}
            fill="#c9d4e8"
          >
            {"type Product {"}
          </text>
          <text
            x={CAT.x + 16}
            y={CAT.y + 56}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={10.5}
            fill="#c9d4e8"
          >
            {"  name: String!"}
          </text>
          {/* The price row: dim and struck in the end state, bright before. */}
          <text
            x={CAT.x + 16}
            y={CAT.y + 70}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={10.5}
            fill="#c9d4e8"
            opacity={0.45}
          >
            {"  price: Money!"}
          </text>
          <text
            ref={set("catPriceFull")}
            x={CAT.x + 16}
            y={CAT.y + 70}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={10.5}
            fill="#c9d4e8"
            opacity={0}
          >
            {"  price: Money!"}
          </text>
          <g ref={set("catStrike")} opacity={1}>
            <line
              x1={CAT.x + 14}
              x2={CAT.x + 114}
              y1={CAT.y + 66.5}
              y2={CAT.y + 66.5}
              stroke={CANON[0].color}
              strokeWidth={2}
            />
            <path
              d="M0 4 h8 M5.5 1.5 L8 4 L5.5 6.5"
              transform={`translate(${CAT.x + CAT.w - 72}, ${CAT.y + 62.5})`}
              fill="none"
              stroke={CANON[0].color}
              strokeWidth={1.5}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <text
              x={CAT.x + CAT.w - 14}
              y={CAT.y + 70}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={10.5}
              fill={CANON[1].color}
            >
              billing
            </text>
          </g>
          <text
            x={CAT.x + 16}
            y={CAT.y + 84}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={10.5}
            fill="#c9d4e8"
          >
            {"}"}
          </text>

          {/* Billing: where price now lives. */}
          <rect
            x={BIL.x}
            y={BIL.y}
            width={BIL.w}
            height={90}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <rect
            x={BIL.x + 14}
            y={BIL.y + 10}
            width={10}
            height={10}
            rx={3}
            fill={CANON[1].color}
          />
          <text
            x={BIL.x + 32}
            y={BIL.y + 19}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            BILLING
          </text>
          <line
            x1={BIL.x}
            x2={BIL.x + BIL.w}
            y1={BIL.y + 29}
            y2={BIL.y + 29}
            stroke="rgba(245,241,234,0.1)"
          />
          <text
            x={BIL.x + 16}
            y={BIL.y + 42}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={10.5}
            fill="#c9d4e8"
          >
            {"type Product {"}
          </text>
          <rect
            ref={set("ovrGlow")}
            x={BIL.x + 8}
            y={BIL.y + 60}
            width={BIL.w - 16}
            height={14}
            rx={4}
            fill="rgba(94,234,212,0.12)"
            opacity={0}
          />
          <g ref={set("bilPrice")} opacity={1}>
            <text
              x={BIL.x + 16}
              y={BIL.y + 56}
              xmlSpace="preserve"
              fontFamily={MONO_FONT}
              fontSize={10.5}
              fill="#c9d4e8"
            >
              {"  price: Money!"}
            </text>
            <text
              x={BIL.x + 16}
              y={BIL.y + 70}
              xmlSpace="preserve"
              fontFamily={MONO_FONT}
              fontSize={10.5}
              fill="#5eead4"
            >
              {'    @override(from: "Catalog")'}
            </text>
          </g>
          <circle
            ref={set("ovrRing")}
            cx={BIL.x + 70}
            cy={BIL.y + 66.5}
            r={5}
            fill="none"
            stroke="#5eead4"
            strokeWidth={1.5}
            opacity={0}
          />
          <text
            x={BIL.x + 16}
            y={BIL.y + 84}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={10.5}
            fill="#c9d4e8"
          >
            {"}"}
          </text>

          {/* The version history, in one quiet breath. */}
          <text
            x={CAT.x}
            y={302}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={INK_DIM}
            opacity={0.7}
          >
            v42 · price moved to billing
          </text>

          {/* The moral. */}
          <text
            x={465}
            y={330}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.18em"
            fill={INK_DIM}
            opacity={0.8}
          >
            THE SURFACE HOLDS STILL · EVERYTHING BEHIND IT MOVES
          </text>

          {/* Traveling beads: white queries out, green responses back. */}
          {Q_PHASES.map((_, i) => (
            <PulseGlyph
              key={i}
              set={set}
              id={`qb${i}`}
              main="#ffffff"
              soft="#ffffff"
              filter="ev8-soft"
            />
          ))}
          {R_PHASES.map((_, i) => (
            <PulseGlyph
              key={i}
              set={set}
              id={`rb${i}`}
              main={CANON[2].color}
              soft={CANON[2].soft}
              filter="ev8-soft"
            />
          ))}
        </svg>
      </div>
    </div>
  );
}
