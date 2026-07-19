"use client";

/**
 * The entity, animated as a resolution. The gateway asks all three services
 * for P-401 at once; each returns only the facet it owns, at its own pace,
 * and the facets slot into one Product. The key row lights last: identity is
 * what made three answers one thing.
 */

import { CYAN, GREEN, MONO_FONT, SLATE, TEAL, VIOLET } from "../palette";
import {
  DIM,
  HAIR,
  LANE,
  PANEL_STROKE,
  PulseGlyph,
  SURFACE,
  VisualCard,
  easeInOutCubic,
  measure,
  ramp,
  useVisual,
} from "./anim";

const T = 10500;

const OWNERS = [
  {
    name: "catalog",
    color: CYAN,
    soft: "#b7e8f7",
    y: 70,
    facet: "name",
    back: [1400, 2200] as const,
  },
  {
    name: "inventory",
    color: GREEN,
    soft: "#a7f3d0",
    y: 150,
    facet: "stock",
    back: [2000, 2800] as const,
  },
  {
    name: "reviews",
    color: VIOLET,
    soft: "#cdd7f2",
    y: 230,
    facet: "rating",
    back: [2600, 3400] as const,
  },
] as const;

const CARD = { x: 560, y: 56, w: 312, h: 196 } as const;
// Row y baselines inside the card: id, name, stock, rating.
const ROW_Y = [104, 132, 160, 188] as const;

const ASKS = OWNERS.map((o, i) =>
  measure([
    [CARD.x, ROW_Y[i + 1] - 4],
    [300, ROW_Y[i + 1] - 4],
    [260, o.y],
    [216, o.y],
  ]),
);
const BACKS = ASKS.map((l) => measure([...l.pts].reverse()));

export function EntityVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 9800, 10200);

    // 1 · the gateway asks everyone at once: who is P-401?
    OWNERS.forEach((_, i) => {
      if (t >= 300 && t < 1100) {
        const u = easeInOutCubic(ramp(t, 300, 1100));
        h.placePulse(
          `ask${i}`,
          ASKS[i],
          u,
          Math.min((t - 300) / 150, 1) * 0.7,
          1.9,
        );
      } else {
        h.hidePulse(`ask${i}`);
      }
      h.setRing(`ringP${i}`, (t - 1100) / 500, 3, 10);
    });
    const lk = t < 1400 ? ramp(t, 300, 500) : 1 - ramp(t, 1400, 2000);
    h.setO("lookupTag", Math.max(0, lk) * 0.8);

    // 2 · each owner returns its facet at its own pace; rows pop on arrival.
    OWNERS.forEach((o, i) => {
      const [b0, b1] = o.back;
      if (t >= b0 && t < b1) {
        const u = easeInOutCubic(ramp(t, b0, b1));
        h.placePulse(`fac${i}`, BACKS[i], u, 1, 2.4);
      } else {
        h.hidePulse(`fac${i}`);
      }
      const on = easeInOutCubic(ramp(t, b1, b1 + 250));
      h.setPop(`row${i + 1}`, on, on);
    });

    // 3 · the key row lights last: identity made them one thing.
    const keyOn = easeInOutCubic(ramp(t, 3700, 4100));
    h.setPop("row0", keyOn, keyOn);
    [0, 1, 2].forEach((i) => {
      h.setO(`iddot${i}`, ramp(t, 3900 + i * 180, 4050 + i * 180) * 0.95);
    });
    h.setRing("ringKey", (t - 4500) / 700, 5, 16);
    const tag = easeInOutCubic(ramp(t, 4700, 5100));
    h.setPop("tag", tag * 0.92 * master, tag);
  });

  return (
    <VisualCard rootRef={rootRef}>
      <svg viewBox="0 0 900 300" width="100%" className="block">
        <defs>
          <filter id="en-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Lanes. */}
        {ASKS.map((l, i) => (
          <path
            key={i}
            d={l.pts
              .map(([x, y], k) => `${k === 0 ? "M" : "L"}${x} ${y}`)
              .join(" ")}
            fill="none"
            stroke={LANE}
            strokeWidth={1.5}
          />
        ))}

        {/* Owning services. */}
        {OWNERS.map((o, i) => (
          <g key={o.name}>
            <rect
              x={8}
              y={o.y - 28}
              width={208}
              height={56}
              rx={10}
              fill={SURFACE}
              stroke={PANEL_STROKE}
            />
            <circle cx={30} cy={o.y - 8} r={4} fill={o.color} />
            <text
              x={44}
              y={o.y - 4}
              fontFamily={MONO_FONT}
              fontSize={11}
              fill={SLATE}
            >
              {o.name}
            </text>
            <text
              x={30}
              y={o.y + 16}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              fill={DIM}
            >
              knows: {o.facet}
            </text>
            <circle
              ref={set(`ringP${i}`)}
              cx={216}
              cy={o.y}
              r={3}
              fill="none"
              stroke={o.color}
              strokeWidth={1.5}
              opacity={0}
            />
          </g>
        ))}

        {/* The entity. */}
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
          x={CARD.x + CARD.w - 20}
          y={CARD.y + 26}
          textAnchor="end"
          fontFamily={MONO_FONT}
          fontSize={9}
          letterSpacing="0.2em"
          fill={TEAL}
          fontWeight={600}
        >
          ENTITY
        </text>
        <line
          x1={CARD.x}
          x2={CARD.x + CARD.w}
          y1={CARD.y + 38}
          y2={CARD.y + 38}
          stroke={HAIR}
        />

        <g ref={set("row0")} opacity={1}>
          <text
            x={CARD.x + 20}
            y={ROW_Y[0]}
            fontFamily={MONO_FONT}
            fontSize={12.5}
            fill="#e8eef8"
          >
            {'id: "P-401"'}
          </text>
          {[CYAN, GREEN, VIOLET].map((c, i) => (
            <circle
              key={i}
              ref={set(`iddot${i}`)}
              cx={CARD.x + CARD.w - 58 + i * 15}
              cy={ROW_Y[0] - 4}
              r={3.2}
              fill={c}
              opacity={0.95}
            />
          ))}
          <circle
            ref={set("ringKey")}
            cx={CARD.x + CARD.w - 43}
            cy={ROW_Y[0] - 4}
            r={5}
            fill="none"
            stroke={TEAL}
            strokeWidth={1.5}
            opacity={0}
          />
        </g>
        {OWNERS.map((o, i) => (
          <g key={o.facet} ref={set(`row${i + 1}`)} opacity={1}>
            <text
              x={CARD.x + 20}
              y={ROW_Y[i + 1]}
              fontFamily={MONO_FONT}
              fontSize={12.5}
              fill="#c9d4e8"
            >
              {o.facet}
              {": "}
              {o.facet === "name"
                ? "String!"
                : o.facet === "stock"
                  ? "Int!"
                  : "Float!"}
            </text>
            <circle
              cx={CARD.x + CARD.w - 28}
              cy={ROW_Y[i + 1] - 4}
              r={3.5}
              fill={o.color}
            />
          </g>
        ))}

        <text
          ref={set("lookupTag")}
          x={CARD.x - 12}
          y={40}
          textAnchor="end"
          fontFamily={MONO_FONT}
          fontSize={10}
          fill={TEAL}
          opacity={0}
        >
          {'lookup(id: "P-401") → everyone'}
        </text>
        <g ref={set("tag")} opacity={0.92}>
          <rect
            x={CARD.x}
            y={CARD.y + CARD.h + 12}
            width={196}
            height={22}
            rx={6}
            fill={SURFACE}
            stroke={TEAL}
            strokeOpacity={0.5}
          />
          <text
            x={CARD.x + 98}
            y={CARD.y + CARD.h + 27}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={TEAL}
          >
            one identity · three owners
          </text>
        </g>

        {OWNERS.map((o, i) => (
          <g key={o.name}>
            <PulseGlyph
              set={set}
              id={`ask${i}`}
              main={TEAL}
              soft="#c8faf0"
              filter="en-soft"
            />
            <PulseGlyph
              set={set}
              id={`fac${i}`}
              main={o.color}
              soft={o.soft}
              filter="en-soft"
            />
          </g>
        ))}
      </svg>
    </VisualCard>
  );
}
