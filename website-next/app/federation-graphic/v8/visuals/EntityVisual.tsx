"use client";

/**
 * The entity: three schema cards each hold a slice of Product; dashed
 * identity streams tie their id lines to the resolution node. A lookup rises
 * from the composed card, sparks visit each service, and the facets return in
 * their owners' colors, popping the composed rows in one by one. The key row
 * lights last with all three owner dots: identity made three answers one.
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

const T = 11500;

const CARDS = [
  { s: 0, x: 40, facet: "name", type: "String!", back: [2600, 3400] as const },
  { s: 1, x: 340, facet: "price", type: "Money!", back: [3000, 3800] as const },
  {
    s: 3,
    x: 640,
    facet: "delivery",
    type: "Date!",
    back: [3600, 4400] as const,
  },
] as const;
const CARD_Y = 30;
const CARD_W = 220;
const CARD_BOTTOM = 154;

const NODE: readonly [number, number] = [450, 300];
const STREAMS = CARDS.map((c) =>
  stream(c.x + CARD_W / 2, CARD_BOTTOM, NODE, 0.3),
);

const OUT = { x: 310, y: 360, w: 280 } as const;
const ROWS: readonly {
  readonly code: string;
  readonly key?: boolean;
  readonly color?: string;
}[] = [
  { code: 'id: "P-401"', key: true },
  { code: "name: String!", color: CANON[0].color },
  { code: "price: Money!", color: CANON[1].color },
  { code: "delivery: String!", color: CANON[3].color },
];

const LOOKUP_UP = measure([
  [450, 356],
  [450, 312],
]);

export function EntityVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 10800, 11200);

    // The lookup leaves the composed card for the node, then fans out.
    if (t >= 400 && t < 900) {
      h.placePulse(
        "q",
        LOOKUP_UP,
        easeInOutCubic(ramp(t, 400, 900)),
        Math.min((t - 400) / 120, 1),
        2.5,
      );
    } else {
      h.hidePulse("q");
    }
    h.setRing("ringN", (t - 900) / 500, 11, 20);
    const lk = t < 2000 ? ramp(t, 900, 1100) : 1 - ramp(t, 2000, 2600);
    h.setO("lkTag", Math.max(0, lk) * 0.8);
    CARDS.forEach((_, k) => {
      if (t >= 1100 && t < 1900) {
        const u = easeInOutCubic(ramp(t, 1100, 1900)) * 0.94;
        h.placePulse(`up${k}`, STREAMS[k].up, u, 0.8, 1.9);
      } else {
        h.hidePulse(`up${k}`);
      }
      h.setRing(`ringC${k}`, (t - 1900) / 450, 5, 11);
    });

    // Facets return in their owners' colors; rows pop as they land.
    CARDS.forEach((c, k) => {
      if (t >= c.back[0] && t < c.back[1]) {
        const u = 0.06 + easeInOutCubic(ramp(t, c.back[0], c.back[1])) * 0.94;
        h.placePulse(`dn${k}`, STREAMS[k].poly, u, 1, 2.2);
      } else {
        h.hidePulse(`dn${k}`);
      }
      const on = easeInOutCubic(ramp(t, c.back[1], c.back[1] + 260));
      h.setPop(`row${k + 1}`, on, on);
    });

    // The key row closes it: three owner dots, one identity.
    const keyOn = easeInOutCubic(ramp(t, 4700, 5100));
    h.setPop("row0", keyOn, keyOn);
    [0, 1, 2].forEach((i) => {
      h.setO(`iddot${i}`, ramp(t, 4900 + i * 180, 5050 + i * 180) * 0.95);
    });
    h.setRing("ringKey", (t - 5500) / 650, 6, 18);
    h.setO("cap", (t >= 5700 && t < 10200 ? 0.75 : 0) * master);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 500" width="100%" className="block">
        <defs>
          <filter id="e6-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Dashed identity streams: the joins, not pipes. */}
        {STREAMS.map((s, i) => (
          <path
            key={i}
            d={s.d}
            fill="none"
            stroke={CANON[CARDS[i].s].color}
            strokeWidth={1.5}
            strokeOpacity={0.6}
            strokeDasharray="3 6"
            strokeLinecap="round"
          />
        ))}

        {/* Each service's slice of Product. */}
        {CARDS.map((c, k) => (
          <g key={c.s}>
            <SchemaCard
              x={c.x}
              y={CARD_Y}
              w={CARD_W}
              label={CANON[c.s].name}
              color={CANON[c.s].color}
              lines={[
                { code: "type Product {" },
                { code: "  id: ID!" },
                { code: `  ${c.facet}: ${c.type}` },
                { code: "}" },
              ]}
            />
            <circle
              ref={set(`ringC${k}`)}
              cx={c.x + CARD_W / 2}
              cy={CARD_BOTTOM}
              r={5}
              fill="none"
              stroke={CANON[c.s].color}
              strokeWidth={1.5}
              opacity={0}
            />
          </g>
        ))}

        {/* Resolution node. */}
        <GlowNode x={NODE[0]} y={NODE[1]} id="e6-node" r={7} />
        <NodeCaption x={318} y={NODE[1]} label="entity resolution" toX={426} />
        <circle
          ref={set("ringN")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={11}
          fill="none"
          stroke="#fff"
          strokeWidth={1.5}
          opacity={0}
        />
        <text
          ref={set("lkTag")}
          x={472}
          y={280}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          fill={INK_DIM}
          opacity={0}
        >
          {'lookup(id: "P-401") → every owner'}
        </text>

        {/* The composed Product. */}
        <rect
          x={449.25}
          y={312}
          width={1.5}
          height={44}
          fill="#f5f0ea"
          opacity={0.35}
        />
        <g>
          <rect
            x={OUT.x}
            y={OUT.y}
            width={OUT.w}
            height={40 + ROWS.length * 20 + 14}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <text
            x={OUT.x + 16}
            y={OUT.y + 22}
            fontFamily={MONO_FONT}
            fontSize={12.5}
            fill="#e8eef8"
            fontWeight={600}
          >
            Product
          </text>
          <text
            x={OUT.x + OUT.w - 14}
            y={OUT.y + 22}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.18em"
            fill="#5eead4"
          >
            ENTITY
          </text>
          <line
            x1={OUT.x}
            x2={OUT.x + OUT.w}
            y1={OUT.y + 32}
            y2={OUT.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {ROWS.map((r, j) => (
            <g key={j} ref={set(`row${j}`)} opacity={1}>
              <text
                x={OUT.x + 16}
                y={OUT.y + 54 + j * 20}
                fontFamily={MONO_FONT}
                fontSize={12}
                fill={r.key ? "#e8eef8" : "#c9d4e8"}
              >
                {r.code}
              </text>
              {r.key ? (
                <g>
                  {[CANON[0].color, CANON[1].color, CANON[3].color].map(
                    (c, i) => (
                      <circle
                        key={i}
                        ref={set(`iddot${i}`)}
                        cx={OUT.x + OUT.w - 54 + i * 14}
                        cy={OUT.y + 50}
                        r={3.2}
                        fill={c}
                        opacity={0.95}
                      />
                    ),
                  )}
                  <circle
                    ref={set("ringKey")}
                    cx={OUT.x + OUT.w - 40}
                    cy={OUT.y + 50}
                    r={6}
                    fill="none"
                    stroke="#5eead4"
                    strokeWidth={1.25}
                    opacity={0}
                  />
                </g>
              ) : (
                <circle
                  cx={OUT.x + OUT.w - 26}
                  cy={OUT.y + 50 + j * 20}
                  r={3.5}
                  fill={r.color}
                />
              )}
            </g>
          ))}
        </g>

        <text
          ref={set("cap")}
          x={450}
          y={492}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.2em"
          fill={INK_DIM}
          opacity={0}
        >
          ONE IDENTITY · THREE OWNERS · ONE PRODUCT
        </text>

        <PulseGlyph
          set={set}
          id="q"
          main="#ffffff"
          soft="#ffffff"
          filter="e6-soft"
        />
        {CARDS.map((c, k) => (
          <g key={k}>
            <PulseGlyph
              set={set}
              id={`up${k}`}
              main="#ffffff"
              soft="#ffffff"
              filter="e6-soft"
            />
            <PulseGlyph
              set={set}
              id={`dn${k}`}
              main={CANON[c.s].color}
              soft={CANON[c.s].soft}
              filter="e6-soft"
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
