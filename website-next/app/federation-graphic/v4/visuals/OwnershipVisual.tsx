"use client";

/**
 * Ownership migration, animated with live traffic as the witness. Client
 * queries keep hitting the composed Product; each answer briefly lights a leg
 * to whichever team owns price. Mid-loop the field physically moves from
 * catalog to pricing — and the very next query's leg simply points at the new
 * owner. Same field, new team, not one dropped request.
 */

import { AMBER, CYAN, MONO_FONT, SLATE, TEAL } from "../palette";
import {
  DIM,
  HAIR,
  LANE,
  PANEL_STROKE,
  PulseGlyph,
  SURFACE,
  VisualCard,
  easeInOutCubic,
  ramp,
  measure,
  useVisual,
} from "./anim";

const T = 12000;
const MOVE = 4400;

const CAT_ROW = 116;
const PRC_ROW = 236;
const CARD = { x: 430, y: 84, w: 260, h: 152 } as const;
const PRICE_Y = 184;

const QUERY = measure([
  [800, 160],
  [690, 160],
]);
const FIRES = [500, 2000, 3500, 6900, 8400, 9900] as const;

const LEG_C = `M${CARD.x} ${PRICE_Y - 4} C 350 ${PRICE_Y - 4}, 300 ${CAT_ROW - 4}, 224 ${CAT_ROW - 4}`;
const LEG_A = `M${CARD.x} ${PRICE_Y - 4} C 350 ${PRICE_Y - 4}, 300 ${PRC_ROW - 4}, 224 ${PRC_ROW - 4}`;

export function OwnershipVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 11500, 11900);

    // Queries never stop; the answer leg lights toward the current owner.
    FIRES.forEach((f, k) => {
      const p = `q${k}`;
      if (t >= f && t < f + 450) {
        h.placePulse(
          p,
          QUERY,
          easeInOutCubic(ramp(t, f, f + 450)),
          Math.min((t - f) / 120, 1),
          2.3,
        );
      } else {
        h.hidePulse(p);
      }
      const legOn =
        t >= f + 450 && t < f + 850 ? 1 - ramp(t, f + 450, f + 850) : 0;
      if (f < MOVE) {
        h.setO(`legC${k}`, legOn * 0.85);
      } else {
        h.setO(`legA${k}`, legOn * 0.85);
      }
    });

    // The move: the row detaches, travels down, and lands with the new owner.
    const before = t < MOVE;
    h.setO("catPrice", before ? 1 : 0.4);
    h.setO("catStrike", before ? 0 : ramp(t, MOVE + 50, MOVE + 350));
    const ghostIn = ramp(t, MOVE, MOVE + 150);
    const ghostOut = 1 - ramp(t, 5250, 5400);
    h.setO("ghost", Math.min(ghostIn, ghostOut) * 0.95);
    h.setX(
      "ghost",
      0,
      (PRC_ROW - CAT_ROW) * easeInOutCubic(ramp(t, MOVE + 100, 5300)),
    );
    h.setO("slotEmpty", before ? 0.6 : 0.6 * (1 - ramp(t, 5250, 5400)));
    const landed = easeInOutCubic(ramp(t, 5300, 5550));
    h.setPop("prcPrice", landed, landed);
    const ov = easeInOutCubic(ramp(t, 5350, 5700)) * (1 - ramp(t, 6600, 7000));
    h.setPop("override", ov * 0.95, ov);

    // The composed card: only the owner chip changes color.
    h.setO("dotCyan", 1 - ramp(t, 5600, 5900));
    h.setO("dotAmber", ramp(t, 5600, 5900));
    const unch = ramp(t, 5600, 5800) * (1 - ramp(t, 6900, 7400));
    h.setO("unchanged", unch * 0.9);

    const cap = t >= 7200 && t < 11200 ? 0.7 : 0;
    h.setO("cap", cap * master);
  });

  return (
    <VisualCard rootRef={rootRef}>
      <svg viewBox="0 0 900 320" width="100%" className="block">
        <defs>
          <filter id="ow-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Answer legs (flashing) and the query lane. */}
        {FIRES.map((f, k) =>
          f < MOVE ? (
            <path
              key={k}
              ref={set(`legC${k}`)}
              d={LEG_C}
              fill="none"
              stroke={CYAN}
              strokeWidth={1.75}
              opacity={0}
            />
          ) : (
            <path
              key={k}
              ref={set(`legA${k}`)}
              d={LEG_A}
              fill="none"
              stroke={AMBER}
              strokeWidth={1.75}
              opacity={0}
            />
          ),
        )}
        <path d="M690 160 H800" fill="none" stroke={LANE} strokeWidth={1.75} />

        {/* Catalog: the old owner. */}
        <rect
          x={8}
          y={40}
          width={216}
          height={104}
          rx={12}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <circle cx={30} cy={62} r={4} fill={CYAN} />
        <text
          x={44}
          y={66}
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.18em"
          fill={SLATE}
        >
          CATALOG
        </text>
        <line x1={8} x2={224} y1={76} y2={76} stroke={HAIR} />
        <text x={30} y={98} fontFamily={MONO_FONT} fontSize={12} fill="#c9d4e8">
          name: String!
        </text>
        <g>
          <text
            ref={set("catPrice")}
            x={30}
            y={CAT_ROW + 4}
            fontFamily={MONO_FONT}
            fontSize={12}
            fill="#c9d4e8"
            opacity={0.4}
          >
            price: Money!
          </text>
          <line
            ref={set("catStrike")}
            x1={28}
            x2={138}
            y1={CAT_ROW}
            y2={CAT_ROW}
            stroke={SLATE}
            strokeWidth={1.25}
            opacity={1}
          />
        </g>

        {/* Pricing: the new owner. */}
        <rect
          x={8}
          y={180}
          width={216}
          height={104}
          rx={12}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <circle cx={30} cy={202} r={4} fill={AMBER} />
        <text
          x={44}
          y={206}
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.18em"
          fill={SLATE}
        >
          PRICING
        </text>
        <line x1={8} x2={224} y1={216} y2={216} stroke={HAIR} />
        <rect
          ref={set("slotEmpty")}
          x={26}
          y={PRC_ROW - 16}
          width={130}
          height={24}
          rx={5}
          fill="none"
          stroke={SLATE}
          strokeOpacity={0.5}
          strokeDasharray="4 5"
          opacity={0}
        />
        <g ref={set("prcPrice")} opacity={1}>
          <text
            x={30}
            y={PRC_ROW + 4}
            fontFamily={MONO_FONT}
            fontSize={12}
            fill="#c9d4e8"
          >
            price: Money!
          </text>
        </g>
        <g ref={set("override")} opacity={0}>
          <rect
            x={164}
            y={PRC_ROW - 16}
            width={54}
            height={22}
            rx={5}
            fill="rgba(251,191,36,0.12)"
            stroke={AMBER}
            strokeOpacity={0.6}
          />
          <text
            x={191}
            y={PRC_ROW - 1}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={AMBER}
          >
            moved
          </text>
        </g>

        {/* The traveling row ghost. */}
        <g ref={set("ghost")} opacity={0}>
          <rect
            x={26}
            y={CAT_ROW - 16}
            width={130}
            height={24}
            rx={5}
            fill={SURFACE}
            stroke={AMBER}
            strokeOpacity={0.7}
          />
          <text
            x={36}
            y={CAT_ROW + 1}
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#e8eef8"
          >
            price: Money!
          </text>
        </g>

        {/* The composed Product: the stable surface. */}
        <rect
          x={CARD.x}
          y={CARD.y}
          width={CARD.w}
          height={CARD.h}
          rx={14}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <rect
          x={CARD.x - 7}
          y={CARD.y - 7}
          width={CARD.w + 14}
          height={CARD.h + 14}
          rx={20}
          fill="none"
          stroke={TEAL}
          strokeOpacity={0.35}
          strokeWidth={2}
        />
        <text
          x={CARD.x + 20}
          y={CARD.y + 26}
          fontFamily={MONO_FONT}
          fontSize={13}
          fill="#e8eef8"
          fontWeight={600}
        >
          Product
        </text>
        <text
          ref={set("unchanged")}
          x={CARD.x + CARD.w - 20}
          y={CARD.y + 26}
          textAnchor="end"
          fontFamily={MONO_FONT}
          fontSize={9}
          letterSpacing="0.18em"
          fill={TEAL}
          opacity={0}
        >
          UNCHANGED
        </text>
        <line
          x1={CARD.x}
          x2={CARD.x + CARD.w}
          y1={CARD.y + 38}
          y2={CARD.y + 38}
          stroke={HAIR}
        />
        <text
          x={CARD.x + 20}
          y={132}
          fontFamily={MONO_FONT}
          fontSize={12.5}
          fill="#c9d4e8"
        >
          id: ID!
        </text>
        <text
          x={CARD.x + 20}
          y={158}
          fontFamily={MONO_FONT}
          fontSize={12.5}
          fill="#c9d4e8"
        >
          name: String!
        </text>
        <text
          x={CARD.x + 20}
          y={PRICE_Y}
          fontFamily={MONO_FONT}
          fontSize={12.5}
          fill="#c9d4e8"
        >
          price: Money!
        </text>
        <circle
          ref={set("dotCyan")}
          cx={CARD.x + CARD.w - 28}
          cy={PRICE_Y - 4}
          r={3.5}
          fill={CYAN}
          opacity={0}
        />
        <circle
          ref={set("dotAmber")}
          cx={CARD.x + CARD.w - 28}
          cy={PRICE_Y - 4}
          r={3.5}
          fill={AMBER}
          opacity={1}
        />

        {/* Clients. */}
        <rect
          x={800}
          y={145}
          width={92}
          height={30}
          rx={8}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <text
          x={846}
          y={164}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={10.5}
          fill={SLATE}
        >
          clients
        </text>

        <text
          ref={set("cap")}
          x={450}
          y={310}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.14em"
          fill={DIM}
          opacity={0}
        >
          same field · new owner · not one dropped query
        </text>

        {FIRES.map((_, k) => (
          <PulseGlyph
            key={k}
            set={set}
            id={`q${k}`}
            main={TEAL}
            soft="#c8faf0"
            filter="ow-soft"
          />
        ))}
      </svg>
    </VisualCard>
  );
}
