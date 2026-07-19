"use client";

/**
 * Composition, animated in two acts. Act one: three schemas feed the compose
 * step, merge and validate tick green, and the composite schema assembles
 * field by field, each with its owner's dot. Act two: inventory publishes an
 * update, the graph recomposes, the stock row flashes, the version bumps —
 * and the composite never went away. Composition is routine, not ceremony.
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

const T = 12500;

const SOURCES = [
  { name: "catalog", color: CYAN, soft: "#b7e8f7", y: 80 },
  { name: "inventory", color: GREEN, soft: "#a7f3d0", y: 160 },
  { name: "reviews", color: VIOLET, soft: "#cdd7f2", y: 240 },
] as const;

const IN_LANES = [
  measure([
    [148, 80],
    [240, 80],
    [310, 140],
    [382, 140],
  ]),
  measure([
    [148, 160],
    [382, 160],
  ]),
  measure([
    [148, 240],
    [240, 240],
    [310, 180],
    [382, 180],
  ]),
];
const OUT_LANE = measure([
  [522, 160],
  [588, 160],
]);

const ROWS = [
  { code: "id: ID!", color: undefined },
  { code: "name: String!", color: CYAN },
  { code: "price: Money!", color: CYAN },
  { code: "stock: Int!", color: GREEN },
  { code: "rating: Float!", color: VIOLET },
] as const;

const CARD = { x: 588, y: 44, w: 292 } as const;

export function CompositionVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 11900, 12300);

    // Act one: three schemas arrive.
    SOURCES.forEach((s, i) => {
      const fire = 400 + i * 300;
      if (t >= fire && t < fire + 700) {
        const u = easeInOutCubic(ramp(t, fire, fire + 700));
        h.placePulse(
          `in${i}`,
          IN_LANES[i],
          u,
          Math.min((t - fire) / 150, 1),
          2.4,
        );
      } else {
        h.hidePulse(`in${i}`);
      }
    });
    h.setRing("ringIn", (t - 1700) / 550, 4, 12);

    // Merge, then validate: two visible steps, two green ticks.
    const stepOff = 1 - ramp(t, 11000, 11600);
    const m1 = t < 2500 ? ramp(t, 1900, 2200) : 1;
    h.setO("step0", m1 * 0.9 * stepOff);
    h.setO("tick0", ramp(t, 2350, 2500) * stepOff);
    const m2 = t < 3100 ? ramp(t, 2500, 2800) : 1;
    h.setO("step1", m2 * 0.9 * stepOff);
    h.setO("tick1", ramp(t, 2950, 3100) * stepOff);

    // The composite ships and assembles.
    if (t >= 3100 && t < 3500) {
      const u = easeInOutCubic(ramp(t, 3100, 3500));
      h.placePulse("out", OUT_LANE, u, 1, 2.6);
    } else {
      h.hidePulse("out");
    }
    h.setRing("ringCard", (t - 3500) / 600, 6, 16);
    ROWS.forEach((_, j) => {
      const on = easeInOutCubic(ramp(t, 3500 + j * 280, 3780 + j * 280));
      h.setPop(`row${j}`, on, on);
    });
    const tag1 = easeInOutCubic(ramp(t, 5200, 5600));
    const tag1off = 1 - ramp(t, 8800, 9000);
    h.setPop("tagV42", tag1 * tag1off * 0.92, tag1);

    // Act two: inventory publishes an update; recompose is quick and boring.
    const upd = 7300;
    h.setO("chipFlash", t >= upd - 300 && t < upd + 200 ? 0.9 : 0);
    if (t >= upd && t < upd + 700) {
      const u = easeInOutCubic(ramp(t, upd, upd + 700));
      h.placePulse("in1b", IN_LANES[1], u, 1, 2.4);
    } else {
      h.hidePulse("in1b");
    }
    const q1 = t >= 8000 && t < 8400 ? 1 : 0;
    const q2 = t >= 8400 && t < 8800 ? 1 : 0;
    h.setO("step0b", q1 * 0.9);
    h.setO("step1b", q2 * 0.9);
    if (t >= 8800 && t < 9200) {
      const u = easeInOutCubic(ramp(t, 8800, 9200));
      h.placePulse("outb", OUT_LANE, u, 1, 2.6);
    } else {
      h.hidePulse("outb");
    }
    const stockFlash = t >= 9200 && t < 9900 ? 1 - ramp(t, 9200, 9900) : 0;
    h.setO("stockFlash", stockFlash * 0.85);
    const tag2 = easeInOutCubic(ramp(t, 9200, 9600));
    h.setPop("tagV43", tag2 * 0.92 * master, tag2);
    const cap = t >= 9600 && t < 11400 ? 0.7 : 0;
    h.setO("cap", cap * master);
  });

  return (
    <VisualCard rootRef={rootRef}>
      <svg viewBox="0 0 900 320" width="100%" className="block">
        <defs>
          <filter id="cp-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Source schema chips. */}
        {SOURCES.map((s, i) => (
          <g key={s.name}>
            <rect
              x={8}
              y={s.y - 15}
              width={140}
              height={30}
              rx={8}
              fill={SURFACE}
              stroke={PANEL_STROKE}
            />
            <circle cx={26} cy={s.y} r={3.5} fill={s.color} />
            <text
              x={38}
              y={s.y + 4}
              fontFamily={MONO_FONT}
              fontSize={10.5}
              fill={SLATE}
            >
              {s.name}
            </text>
            <text
              x={140}
              y={s.y + 4}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={8.5}
              fill={DIM}
            >
              sdl
            </text>
            {i === 1 && (
              <rect
                ref={set("chipFlash")}
                x={8}
                y={s.y - 15}
                width={140}
                height={30}
                rx={8}
                fill="none"
                stroke={GREEN}
                strokeWidth={1.5}
                opacity={0}
              />
            )}
          </g>
        ))}

        {IN_LANES.map((l, i) => (
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
        <path d="M522 160 H588" fill="none" stroke={LANE} strokeWidth={1.5} />

        {/* Compose step with its two visible sub-steps. */}
        <rect
          x={382}
          y={104}
          width={140}
          height={112}
          rx={12}
          fill={SURFACE}
          stroke={TEAL}
          strokeOpacity={0.45}
        />
        <text
          x={452}
          y={128}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.18em"
          fill={TEAL}
        >
          COMPOSE
        </text>
        <line x1={382} x2={522} y1={138} y2={138} stroke={HAIR} />
        {["merge", "validate"].map((step, i) => (
          <g key={step}>
            <rect
              ref={set(`step${i}`)}
              x={396}
              y={150 + i * 30}
              width={112}
              height={22}
              rx={5}
              fill="rgba(139,160,188,0.08)"
              stroke={PANEL_STROKE}
              opacity={0.9}
            />
            <rect
              ref={set(`step${i}b`)}
              x={396}
              y={150 + i * 30}
              width={112}
              height={22}
              rx={5}
              fill="none"
              stroke={GREEN}
              strokeOpacity={0.8}
              opacity={0}
            />
            <text
              x={408}
              y={165 + i * 30}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              fill={DIM}
            >
              {step}
            </text>
            <text
              ref={set(`tick${i}`)}
              x={496}
              y={165 + i * 30}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={10}
              fill={GREEN}
              opacity={0}
            >
              ✓
            </text>
          </g>
        ))}
        <circle
          ref={set("ringIn")}
          cx={386}
          cy={160}
          r={4}
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          opacity={0}
        />

        {/* The composite schema, assembling field by field. */}
        <rect
          x={CARD.x}
          y={CARD.y}
          width={CARD.w}
          height={232}
          rx={14}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <circle
          ref={set("ringCard")}
          cx={CARD.x}
          cy={160}
          r={6}
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          opacity={0}
        />
        <text
          x={CARD.x + 20}
          y={CARD.y + 26}
          fontFamily={MONO_FONT}
          fontSize={11.5}
          fill="#e8eef8"
          fontWeight={600}
        >
          composite schema
        </text>
        <text
          x={CARD.x + CARD.w - 20}
          y={CARD.y + 26}
          textAnchor="end"
          fontFamily={MONO_FONT}
          fontSize={8.5}
          fill={DIM}
        >
          aka supergraph
        </text>
        <line
          x1={CARD.x}
          x2={CARD.x + CARD.w}
          y1={CARD.y + 38}
          y2={CARD.y + 38}
          stroke={HAIR}
        />
        {ROWS.map((r, j) => (
          <g key={r.code} ref={set(`row${j}`)} opacity={1}>
            <text
              x={CARD.x + 20}
              y={CARD.y + 66 + j * 26}
              fontFamily={MONO_FONT}
              fontSize={12.5}
              fill="#c9d4e8"
            >
              {r.code}
            </text>
            {r.color ? (
              <circle
                cx={CARD.x + CARD.w - 26}
                cy={CARD.y + 62 + j * 26}
                r={3.5}
                fill={r.color}
              />
            ) : (
              <g>
                <circle
                  cx={CARD.x + CARD.w - 54}
                  cy={CARD.y + 62}
                  r={3}
                  fill={CYAN}
                />
                <circle
                  cx={CARD.x + CARD.w - 40}
                  cy={CARD.y + 62}
                  r={3}
                  fill={GREEN}
                />
                <circle
                  cx={CARD.x + CARD.w - 26}
                  cy={CARD.y + 62}
                  r={3}
                  fill={VIOLET}
                />
              </g>
            )}
          </g>
        ))}
        <rect
          ref={set("stockFlash")}
          x={CARD.x + 12}
          y={CARD.y + 128}
          width={CARD.w - 24}
          height={24}
          rx={5}
          fill="none"
          stroke={GREEN}
          strokeOpacity={0.8}
          opacity={0}
        />

        {/* Version tags. */}
        <g ref={set("tagV42")} opacity={0.92}>
          <rect
            x={CARD.x}
            y={288}
            width={104}
            height={22}
            rx={6}
            fill="rgba(52,211,153,0.1)"
            stroke={GREEN}
            strokeOpacity={0.55}
          />
          <text
            x={CARD.x + 52}
            y={303}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={GREEN}
          >
            live ✓ v42
          </text>
        </g>
        <g ref={set("tagV43")} opacity={0}>
          <rect
            x={CARD.x}
            y={288}
            width={104}
            height={22}
            rx={6}
            fill="rgba(52,211,153,0.1)"
            stroke={GREEN}
            strokeOpacity={0.55}
          />
          <text
            x={CARD.x + 52}
            y={303}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={GREEN}
          >
            live ✓ v43
          </text>
        </g>
        <text
          ref={set("cap")}
          x={330}
          y={306}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.14em"
          fill={DIM}
          opacity={0}
        >
          recomposed · validated · clients stayed online
        </text>

        <PulseGlyph
          set={set}
          id="out"
          main={TEAL}
          soft="#c8faf0"
          filter="cp-soft"
        />
        <PulseGlyph
          set={set}
          id="outb"
          main={TEAL}
          soft="#c8faf0"
          filter="cp-soft"
        />
        {SOURCES.map((s, i) => (
          <PulseGlyph
            key={s.name}
            set={set}
            id={`in${i}`}
            main={s.color}
            soft={s.soft}
            filter="cp-soft"
          />
        ))}
        <PulseGlyph
          set={set}
          id="in1b"
          main={GREEN}
          soft="#a7f3d0"
          filter="cp-soft"
        />
      </svg>
    </VisualCard>
  );
}
