"use client";

/**
 * Ownership migration with the schemas on screen. Catalog's card owns price;
 * a ghost row detaches, travels down to Billing's card, and lands — while
 * query beads ride the composite's line without a gap and the composed
 * Product card on the right only changes one thing: the color of the dot
 * beside price. Same field, new owner, zero missed queries.
 */

import { MONO_FONT } from "../palette";
import { PulseGlyph, easeInOutCubic, measure, ramp, useVisual } from "./anim";
import {
  CANON,
  GlowNode,
  INK_DIM,
  NodeCaption,
  SchemaCard,
  stream,
} from "./stage";

const T = 12000;
const MOVE = [4300, 5400] as const;

const CAT = { x: 50, y: 30, w: 230 } as const;
const BILL = { x: 50, y: 230, w: 230 } as const;
const CAT_PRICE_Y = CAT.y + 48 + 2 * 18;
const BILL_SLOT_Y = BILL.y + 48 + 2 * 18;

const NODE: readonly [number, number] = [520, 250];
const S_CAT = stream(CAT.x + CAT.w, CAT.y + 60, NODE, 0.2);
const S_BILL = stream(BILL.x + BILL.w, BILL.y + 60, NODE, 0.2);

const OUT_UP = measure([
  [520, 430],
  [520, 262],
]);
const OUT_DOWN = measure([
  [520, 262],
  [520, 430],
]);
const Q_FIRES = [500, 2100, 3700, 6600, 8200, 9800] as const;

const PROD = { x: 660, y: 150, w: 210 } as const;

export function OwnershipVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 11500, 11900);

    // Queries never pause.
    Q_FIRES.forEach((f, k) => {
      if (t >= f && t < f + 550) {
        h.placePulse(
          `q${k}`,
          OUT_UP,
          easeInOutCubic(ramp(t, f, f + 550)),
          Math.min((t - f) / 120, 1),
          2.2,
        );
      } else {
        h.hidePulse(`q${k}`);
      }
      if (t >= f + 650 && t < f + 1200) {
        h.placePulse(
          `r${k}`,
          OUT_DOWN,
          easeInOutCubic(ramp(t, f + 650, f + 1200)),
          1,
          2.2,
        );
      } else {
        h.hidePulse(`r${k}`);
      }
    });

    // The move: strike in Catalog, ghost row travels, lands in Billing.
    const before = t < MOVE[0];
    h.setO("catPrice", before ? 1 : 0.4);
    h.setO("catStrike", before ? 0 : ramp(t, MOVE[0] + 50, MOVE[0] + 350));
    const ghostIn = ramp(t, MOVE[0], MOVE[0] + 150);
    const ghostOut = 1 - ramp(t, MOVE[1] - 100, MOVE[1]);
    h.setO("ghost", Math.min(ghostIn, ghostOut) * 0.95);
    h.setX(
      "ghost",
      0,
      (BILL_SLOT_Y - CAT_PRICE_Y) *
        easeInOutCubic(ramp(t, MOVE[0] + 100, MOVE[1] - 100)),
    );
    h.setO(
      "slotEmpty",
      before ? 0.55 : 0.55 * (1 - ramp(t, MOVE[1] - 150, MOVE[1])),
    );
    const landed = easeInOutCubic(ramp(t, MOVE[1] - 50, MOVE[1] + 200));
    h.setPop("billPrice", landed, landed);
    const ov =
      easeInOutCubic(ramp(t, MOVE[1], MOVE[1] + 350)) *
      (1 - ramp(t, 7400, 7800));
    h.setPop("movedTag", ov * 0.9, ov);
    h.setRing("ringB", (t - MOVE[1]) / 500, 8, 16);

    // In the composed Product, only the dot changes.
    h.setO("dotCat", 1 - ramp(t, MOVE[1] + 300, MOVE[1] + 600));
    h.setO("dotBill", ramp(t, MOVE[1] + 300, MOVE[1] + 600));
    const unch =
      ramp(t, MOVE[1] + 300, MOVE[1] + 500) * (1 - ramp(t, 7800, 8300));
    h.setO("unchanged", unch * 0.9);

    h.setO("cap", (t >= 7600 ? 0.75 : 0) * master);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full overflow-x-auto">
      <svg
        viewBox="0 0 900 460"
        width="100%"
        className="block min-w-[640px] sm:min-w-0"
      >
        <defs>
          <filter id="o6-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Streams from both owners into the node. */}
        <path
          d={S_CAT.d}
          fill="none"
          stroke={CANON[0].color}
          strokeWidth={2}
          strokeOpacity={0.75}
          strokeLinecap="round"
        />
        <path
          d={S_BILL.d}
          fill="none"
          stroke={CANON[1].color}
          strokeWidth={2}
          strokeOpacity={0.75}
          strokeLinecap="round"
        />

        {/* Catalog: the old owner (price is dynamic). */}
        <SchemaCard
          x={CAT.x}
          y={CAT.y}
          w={CAT.w}
          label={CANON[0].name}
          color={CANON[0].color}
          lines={[
            { code: "type Product {" },
            { code: "  name: String!" },
            { code: "" },
            { code: "}" },
          ]}
        />
        <text
          ref={set("catPrice")}
          x={CAT.x + 16}
          y={CAT_PRICE_Y}
          xmlSpace="preserve"
          fontFamily={MONO_FONT}
          fontSize={12}
          fill="#c9d4e8"
          opacity={0.4}
        >
          {"  price: Money!"}
        </text>
        <line
          ref={set("catStrike")}
          x1={CAT.x + 26}
          x2={CAT.x + 136}
          y1={CAT_PRICE_Y - 4}
          y2={CAT_PRICE_Y - 4}
          stroke={INK_DIM}
          strokeWidth={1.25}
          opacity={1}
        />

        {/* Billing: the new owner. */}
        <SchemaCard
          x={BILL.x}
          y={BILL.y}
          w={BILL.w}
          label={CANON[1].name}
          color={CANON[1].color}
          lines={[
            { code: "type Product {" },
            { code: "  id: ID!", dim: true },
            { code: "" },
            { code: "}" },
          ]}
        />
        {/* The slot's resting hint: where the field will land. */}
        <text
          x={BILL.x + 16}
          y={BILL_SLOT_Y}
          xmlSpace="preserve"
          fontFamily={MONO_FONT}
          fontSize={12}
          fill="#c9d4e8"
          opacity={0.28}
        >
          {"  price: Money!"}
        </text>
        <rect
          ref={set("slotEmpty")}
          x={BILL.x + 12}
          y={BILL_SLOT_Y - 13}
          width={140}
          height={18}
          rx={4}
          fill="none"
          stroke={INK_DIM}
          strokeOpacity={0.6}
          strokeDasharray="4 5"
          opacity={0}
        />
        <g ref={set("billPrice")} opacity={1}>
          <text
            x={BILL.x + 16}
            y={BILL_SLOT_Y}
            xmlSpace="preserve"
            fontFamily={MONO_FONT}
            fontSize={12}
            fill="#c9d4e8"
          >
            {"  price: Money!"}
          </text>
        </g>
        <g ref={set("movedTag")} opacity={0.9}>
          <text
            x={BILL.x + 164}
            y={BILL_SLOT_Y}
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={CANON[1].color}
          >
            @override
          </text>
        </g>
        <circle
          ref={set("ringB")}
          cx={BILL.x + BILL.w / 2}
          cy={BILL.y}
          r={8}
          fill="none"
          stroke={CANON[1].color}
          strokeWidth={1.5}
          opacity={0}
        />

        {/* The traveling ghost row. */}
        <g ref={set("ghost")} opacity={0}>
          <rect
            x={CAT.x + 12}
            y={CAT_PRICE_Y - 13}
            width={140}
            height={18}
            rx={4}
            fill="rgba(12,19,34,0.85)"
            stroke={CANON[1].color}
            strokeOpacity={0.7}
          />
          <text
            x={CAT.x + 20}
            y={CAT_PRICE_Y}
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#e8eef8"
          >
            price: Money!
          </text>
        </g>

        {/* Node + live line. */}
        <GlowNode x={NODE[0]} y={NODE[1]} id="o6-node" r={7} />
        <NodeCaption x={402} y={NODE[1]} label="composite" toX={496} />
        {/* Lane from the hub into the composed Product card. */}
        <path
          d={`M536 ${NODE[1]} H${PROD.x}`}
          fill="none"
          stroke="rgba(139,160,188,0.4)"
          strokeWidth={1.5}
        />
        <rect
          x={519.25}
          y={262}
          width={1.5}
          height={172}
          fill="#f5f0ea"
          opacity={0.4}
        />
        <text
          x={536}
          y={426}
          fontFamily={MONO_FONT}
          fontSize={9}
          letterSpacing="0.16em"
          fill={INK_DIM}
          opacity={0.6}
        >
          QUERIES · NEVER PAUSED
        </text>

        {/* The composed Product: one dot changes. */}
        <rect
          x={PROD.x}
          y={PROD.y}
          width={PROD.w}
          height={118}
          rx={12}
          fill="rgba(12,19,34,0.5)"
          stroke="rgba(245,241,234,0.13)"
        />
        <text
          x={PROD.x + 16}
          y={PROD.y + 22}
          fontFamily={MONO_FONT}
          fontSize={12}
          fill="#e8eef8"
          fontWeight={600}
        >
          Product
        </text>
        <text
          ref={set("unchanged")}
          x={PROD.x + PROD.w - 14}
          y={PROD.y + 22}
          textAnchor="end"
          fontFamily={MONO_FONT}
          fontSize={8.5}
          letterSpacing="0.14em"
          fill="#5eead4"
          opacity={0}
        >
          UNCHANGED
        </text>
        <line
          x1={PROD.x}
          x2={PROD.x + PROD.w}
          y1={PROD.y + 32}
          y2={PROD.y + 32}
          stroke="rgba(245,241,234,0.1)"
        />
        {["id: ID!", "name: String!", "price: Money!"].map((code, j) => (
          <g key={j}>
            <text
              x={PROD.x + 16}
              y={PROD.y + 54 + j * 20}
              fontFamily={MONO_FONT}
              fontSize={11.5}
              fill="#c9d4e8"
            >
              {code}
            </text>
            {j === 1 && (
              <circle
                cx={PROD.x + PROD.w - 24}
                cy={PROD.y + 50 + j * 20}
                r={3.2}
                fill={CANON[0].color}
              />
            )}
            {j === 2 && (
              <g>
                <circle
                  ref={set("dotCat")}
                  cx={PROD.x + PROD.w - 24}
                  cy={PROD.y + 50 + j * 20}
                  r={3.2}
                  fill={CANON[0].color}
                  opacity={0}
                />
                <circle
                  ref={set("dotBill")}
                  cx={PROD.x + PROD.w - 24}
                  cy={PROD.y + 50 + j * 20}
                  r={3.2}
                  fill={CANON[1].color}
                  opacity={1}
                />
              </g>
            )}
          </g>
        ))}

        <text
          ref={set("cap")}
          x={450}
          y={452}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.2em"
          fill={INK_DIM}
          opacity={0}
        >
          SAME FIELD · NEW OWNER · NOT ONE DROPPED QUERY
        </text>

        {Q_FIRES.map((_, k) => (
          <g key={k}>
            <PulseGlyph
              set={set}
              id={`q${k}`}
              main="#f5f0ea"
              soft="#ffffff"
              filter="o6-soft"
            />
            <PulseGlyph
              set={set}
              id={`r${k}`}
              main="#66be77"
              soft="#bce5c4"
              filter="o6-soft"
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
