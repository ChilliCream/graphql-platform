"use client";

/**
 * Composition: the schemas are the story, so the schemas are on screen. Three
 * service cards publish real SDL; contract chips drop down their streams into
 * the composition node; merge and validate tick; and the composite schema
 * assembles below, field by field, each with its owner's dot. Billing then
 * ships an update mid-loop: quick recompose, version bump, zero ceremony.
 */

import { MONO_FONT } from "../palette";
import { easeInOutCubic, pointAt, ramp, useVisual } from "./anim";
import {
  CANON,
  GlowNode,
  INK_DIM,
  NodeCaption,
  SchemaCard,
  stream,
} from "./stage";

const T = 12500;

const CARDS = [
  { s: 0, x: 40, facet: "name: String!" },
  { s: 1, x: 340, facet: "price: Money!" },
  { s: 3, x: 640, facet: "delivery: Date!" },
] as const;
const CARD_Y = 30;
const CARD_W = 220;
const CARD_BOTTOM = 154;

const NODE: readonly [number, number] = [450, 310];
const STREAMS = CARDS.map((c) =>
  stream(c.x + CARD_W / 2, CARD_BOTTOM, NODE, 0.3),
);

const OUT = { x: 310, y: 364, w: 280 } as const;
const COMPOSED = [
  { code: "type Product {", dot: undefined },
  { code: "  id: ID!", dot: "key" },
  { code: "  name: String!", dot: CANON[0].color },
  { code: "  price: Money!", dot: CANON[1].color },
  { code: "  delivery: Date!", dot: CANON[3].color },
  { code: "}", dot: undefined },
] as const;

export function CompositionVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 11900, 12300);

    // Contract chips drop into the node.
    CARDS.forEach((_, i) => {
      const fire = 400 + i * 350;
      if (t >= fire && t < fire + 900) {
        const u = easeInOutCubic(ramp(t, fire, fire + 900));
        const [x, y] = pointAt(STREAMS[i].poly, u);
        h.setX(`chip${i}`, x, y);
        h.setO(`chip${i}`, Math.min((t - fire) / 120, 1));
      } else {
        h.setO(`chip${i}`, 0);
      }
    });
    h.setRing("ringIn", (t - 2300) / 550, 11, 22);

    // Merge ✓ validate ✓.
    const off = 1 - ramp(t, 11000, 11600);
    h.setO("merge", (t < 3100 ? ramp(t, 2400, 2700) : 1) * 0.8 * off);
    h.setO("mergeTick", ramp(t, 2800, 2950) * off);
    h.setO("validate", (t < 3700 ? ramp(t, 3000, 3300) : 1) * 0.8 * off);
    h.setO("validateTick", ramp(t, 3400, 3550) * off);

    // The composite assembles, field by field, owners visible.
    h.setO("outLine", 0.2 + 0.4 * ramp(t, 3600, 4000));
    COMPOSED.forEach((_, j) => {
      const on = easeInOutCubic(ramp(t, 3800 + j * 240, 4040 + j * 240));
      h.setPop(`row${j}`, on, on);
    });
    const tag1 =
      easeInOutCubic(ramp(t, 5400, 5800)) * (1 - ramp(t, 9000, 9200));
    h.setPop("v42", tag1 * 0.92, tag1);

    // Billing ships an update.
    const upd = 7600;
    h.setO("billFlash", t >= upd - 400 && t < upd + 300 ? 0.9 : 0);
    if (t >= upd && t < upd + 800) {
      const u = easeInOutCubic(ramp(t, upd, upd + 800));
      const [x, y] = pointAt(STREAMS[1].poly, u);
      h.setX("chipB", x, y);
      h.setO("chipB", 1);
    } else {
      h.setO("chipB", 0);
    }
    h.setO("tick2", t >= 8500 && t < 9100 ? 0.95 : 0);
    h.setRing("ringUpd", (t - 8500) / 500, 11, 20);
    const priceFlash = t >= 9000 && t < 9700 ? 1 - ramp(t, 9000, 9700) : 0;
    h.setO("priceFlash", priceFlash * 0.8);
    const tag2 = easeInOutCubic(ramp(t, 9200, 9600));
    h.setPop("v43", tag2 * 0.92 * master, tag2);
    h.setO("cap", (t >= 9700 && t < 11400 ? 0.75 : 0) * master);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 540" width="100%" className="block">
        {/* Streams from the cards into the node. */}
        {STREAMS.map((s, i) => (
          <path
            key={i}
            d={s.d}
            fill="none"
            stroke={CANON[CARDS[i].s].color}
            strokeWidth={2}
            strokeOpacity={0.8}
            strokeLinecap="round"
          />
        ))}

        {/* The three published schemas: real SDL, real owners. */}
        {CARDS.map((c) => (
          <SchemaCard
            key={c.s}
            x={c.x}
            y={CARD_Y}
            w={CARD_W}
            label={CANON[c.s].name}
            color={CANON[c.s].color}
            lines={[
              { code: "type Product {" },
              { code: "  id: ID!", dim: true },
              { code: `  ${c.facet}` },
              { code: "}" },
            ]}
          />
        ))}
        <rect
          ref={set("billFlash")}
          x={CARDS[1].x}
          y={CARD_Y}
          width={CARD_W}
          height={124}
          rx={12}
          fill="none"
          stroke={CANON[1].color}
          strokeWidth={1.5}
          opacity={0}
        />

        {/* The node with its checks. */}
        <GlowNode x={NODE[0]} y={NODE[1]} id="c6-node" r={8} />
        <NodeCaption x={318} y={NODE[1]} label="Fusion composition" toX={424} />
        <circle
          ref={set("ringIn")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={11}
          fill="none"
          stroke="#fff"
          strokeWidth={1.5}
          opacity={0}
        />
        <circle
          ref={set("ringUpd")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={11}
          fill="none"
          stroke={CANON[1].color}
          strokeWidth={1.5}
          opacity={0}
        />
        <text
          ref={set("merge")}
          x={510}
          y={302}
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.14em"
          fill={INK_DIM}
          opacity={0}
        >
          MERGE
        </text>
        <text
          ref={set("mergeTick")}
          x={572}
          y={302}
          fontFamily={MONO_FONT}
          fontSize={10.5}
          fill="#66be77"
          opacity={0}
        >
          ✓
        </text>
        <text
          ref={set("validate")}
          x={510}
          y={322}
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.14em"
          fill={INK_DIM}
          opacity={0}
        >
          VALIDATE
        </text>
        <text
          ref={set("validateTick")}
          x={594}
          y={322}
          fontFamily={MONO_FONT}
          fontSize={10.5}
          fill="#66be77"
          opacity={0}
        >
          ✓
        </text>
        <text
          ref={set("tick2")}
          x={510}
          y={342}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          fill="#66be77"
          opacity={0}
        >
          recomposed ✓
        </text>

        {/* Output into the composite schema card. */}
        <rect
          ref={set("outLine")}
          x={449.25}
          y={322}
          width={1.5}
          height={42}
          fill="#f5f0ea"
          opacity={0.2}
        />
        <g>
          <rect
            x={OUT.x}
            y={OUT.y}
            width={OUT.w}
            height={40 + COMPOSED.length * 18 + 12}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <circle cx={OUT.x + 19} cy={OUT.y + 17} r={4.5} fill="#fff" />
          <text
            x={OUT.x + 32}
            y={OUT.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            COMPOSITE SCHEMA
          </text>
          <text
            x={OUT.x + OUT.w - 14}
            y={OUT.y + 21}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={INK_DIM}
            opacity={0.6}
          >
            aka supergraph
          </text>
          <line
            x1={OUT.x}
            x2={OUT.x + OUT.w}
            y1={OUT.y + 32}
            y2={OUT.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {COMPOSED.map((r, j) => (
            <g key={j} ref={set(`row${j}`)} opacity={1}>
              <text
                x={OUT.x + 16}
                y={OUT.y + 48 + j * 18}
                fontFamily={MONO_FONT}
                fontSize={12}
                fill="#c9d4e8"
              >
                {r.code}
              </text>
              {r.dot === "key" ? (
                <g>
                  <circle
                    cx={OUT.x + OUT.w - 52}
                    cy={OUT.y + 44 + j * 18}
                    r={3}
                    fill={CANON[0].color}
                  />
                  <circle
                    cx={OUT.x + OUT.w - 39}
                    cy={OUT.y + 44 + j * 18}
                    r={3}
                    fill={CANON[1].color}
                  />
                  <circle
                    cx={OUT.x + OUT.w - 26}
                    cy={OUT.y + 44 + j * 18}
                    r={3}
                    fill={CANON[3].color}
                  />
                </g>
              ) : r.dot ? (
                <circle
                  cx={OUT.x + OUT.w - 26}
                  cy={OUT.y + 44 + j * 18}
                  r={3.5}
                  fill={r.dot}
                />
              ) : null}
            </g>
          ))}
          <rect
            ref={set("priceFlash")}
            x={OUT.x + 8}
            y={OUT.y + 40 + 3 * 18 - 6}
            width={OUT.w - 16}
            height={19}
            rx={4}
            fill="none"
            stroke={CANON[1].color}
            strokeOpacity={0.8}
            opacity={0}
          />
        </g>

        {/* Version + caption. */}
        <g ref={set("v42")} opacity={0.92}>
          <text
            x={OUT.x + OUT.w + 18}
            y={OUT.y + 21}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            LIVE · V42
          </text>
        </g>
        <g ref={set("v43")} opacity={0}>
          <text
            x={OUT.x + OUT.w + 18}
            y={OUT.y + 21}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.16em"
            fill="#66be77"
          >
            LIVE · V43
          </text>
        </g>
        <text
          ref={set("cap")}
          x={450}
          y={532}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.2em"
          fill={INK_DIM}
          opacity={0}
        >
          BILLING UPDATED · RECOMPOSED · ZERO DOWNTIME
        </text>

        {/* Traveling contract chips. */}
        {CARDS.map((c, i) => (
          <g key={i} ref={set(`chip${i}`)} opacity={0}>
            <rect
              x={-5}
              y={-5}
              width={10}
              height={10}
              rx={2.5}
              fill={CANON[c.s].color}
            />
          </g>
        ))}
        <g ref={set("chipB")} opacity={0}>
          <rect
            x={-5}
            y={-5}
            width={10}
            height={10}
            rx={2.5}
            fill={CANON[1].color}
            stroke="#fff"
            strokeWidth={1}
          />
        </g>
      </svg>
    </div>
  );
}
