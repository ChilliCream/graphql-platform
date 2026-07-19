"use client";

/**
 * The hero: the gateway pipeline, left to right and back. A query arrives
 * from the left edge; the gateway splits it into execution steps; labeled
 * field pulses travel to the subgraphs that own them; results return at
 * their own pace and fill the compose slots; the merged response leaves the
 * way the query came. Loops continuously via the shared rAF runtime.
 */

import { MONO_FONT } from "../palette";
import {
  PulseGlyph,
  easeInOutCubic,
  measure,
  pointAt,
  ramp,
  useVisual,
} from "./anim";
import { CANON, INK_DIM } from "./stage";

const T = 7600;

const SURFACE = "rgba(12,19,34,0.85)";
const BORDER = "rgba(245,241,234,0.14)";
const HAIR = "rgba(245,241,234,0.1)";
const CODE = "#c9d4e8";

const GW = { x: 150, y: 110, w: 190, h: 210 } as const;
const MID = 215;

const SUBS = [
  { s: 0, y: 140, field: "name", value: '"Aero Mug"' },
  { s: 1, y: 215, field: "price", value: '"24.90 EUR"' },
  { s: 3, y: 290, field: "delivery", value: '"2 days"' },
] as const;

const STEP_Y = [172, 210, 248] as const;
const SLOT_X = [296, 312, 328] as const;

const Q_IN = measure([
  [-30, MID],
  [GW.x + 8, MID],
]);
const Q_OUT = measure([
  [GW.x + 8, MID],
  [-30, MID],
]);

const OUT_LANES = SUBS.map((sub, i) =>
  measure([
    [GW.x + GW.w, STEP_Y[i]],
    [470, STEP_Y[i]],
    [560, sub.y],
    [648, sub.y],
  ]),
);
const BACK_LANES = OUT_LANES.map((l) => measure([...l.pts].reverse()));

// Staggered work: catalog fast, billing mid, shipping slow.
const RETURNS = [
  [3050, 3850],
  [3400, 4200],
  [3900, 4700],
] as const;

export function HeroPipeline() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // 1 · a query arrives from the left.
    if (t >= 200 && t < 1000) {
      h.placePulse(
        "q",
        Q_IN,
        easeInOutCubic(ramp(t, 200, 1000)),
        Math.min((t - 200) / 130, 1),
        2.8,
      );
      h.setX("qTag", pointAt(Q_IN, easeInOutCubic(ramp(t, 200, 1000)))[0], MID);
      h.setO("qTag", Math.min((t - 200) / 130, 1) * 0.9);
    } else {
      h.hidePulse("q");
      h.setO("qTag", 0);
    }
    h.setRing("ringIn", (t - 1000) / 450, 8, 16);

    // 2 · the plan: the query splits into execution steps.
    SUBS.forEach((_, i) => {
      const on = ramp(t, 1150 + i * 160, 1350 + i * 160);
      const off = 1 - ramp(t, 6900, 7300);
      h.setO(`step${i}`, on * off * 0.95);
    });

    // 3 · field pulses travel to the subgraphs that own them.
    SUBS.forEach((sub, i) => {
      if (t >= 1900 && t < 2800) {
        const u = easeInOutCubic(ramp(t, 1900, 2800));
        h.placePulse(
          `f${i}`,
          OUT_LANES[i],
          u,
          Math.min((t - 1900) / 130, 1),
          2.3,
        );
        const [x, y] = pointAt(OUT_LANES[i], u);
        h.setX(`fTag${i}`, x, y);
        h.setO(`fTag${i}`, Math.min((t - 1900) / 130, 1) * 0.85);
      } else {
        h.hidePulse(`f${i}`);
        h.setO(`fTag${i}`, 0);
      }
      h.setRing(`ringS${i}`, (t - 2800) / 400, 5, 11);
      const busy = t >= 2800 && t < RETURNS[i][0] ? 0.8 : 0;
      h.setO(`busy${i}`, busy);

      // 4 · results return at their own pace.
      const [r0, r1] = RETURNS[i];
      if (t >= r0 && t < r1) {
        const u = easeInOutCubic(ramp(t, r0, r1));
        h.placePulse(`r${i}`, BACK_LANES[i], u, 1, 2.3);
        const [x, y] = pointAt(BACK_LANES[i], u);
        h.setX(`rTag${i}`, x, y);
        h.setO(`rTag${i}`, 0.85);
      } else {
        h.hidePulse(`r${i}`);
        h.setO(`rTag${i}`, 0);
      }
      const slot = ramp(t, r1, r1 + 140) * (1 - ramp(t, 6900, 7300));
      h.setO(`slot${i}`, slot * 0.95);
    });

    // 5 · compose, then one response leaves the way the query came.
    const composed = ramp(t, 4850, 5050) * (1 - ramp(t, 6900, 7300));
    h.setO("composed", composed * 0.95);
    h.setRing("ringC", (t - 4850) / 450, 8, 18);
    if (t >= 5150 && t < 5950) {
      h.placePulse("resp", Q_OUT, easeInOutCubic(ramp(t, 5150, 5950)), 1, 2.8);
      h.setX(
        "respTag",
        pointAt(Q_OUT, easeInOutCubic(ramp(t, 5150, 5950)))[0],
        MID,
      );
      h.setO("respTag", 0.9);
    } else {
      h.hidePulse("resp");
      h.setO("respTag", 0);
    }
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 920 420" width="100%" className="block">
        <defs>
          <filter id="hp-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Lanes. */}
        <path
          d={`M0 ${MID} H${GW.x}`}
          fill="none"
          stroke="rgba(139,160,188,0.45)"
          strokeWidth={1.75}
        />
        {OUT_LANES.map((l, i) => (
          <path
            key={i}
            d={l.pts
              .map(([x, y], k) => `${k === 0 ? "M" : "L"}${x} ${y}`)
              .join(" ")}
            fill="none"
            stroke="rgba(139,160,188,0.35)"
            strokeWidth={1.5}
          />
        ))}

        {/* The gateway: plan, execute, compose. */}
        <rect
          x={GW.x}
          y={GW.y}
          width={GW.w}
          height={GW.h}
          rx={14}
          fill={SURFACE}
          stroke={BORDER}
        />
        <text
          x={GW.x + 18}
          y={GW.y + 28}
          fontFamily={MONO_FONT}
          fontSize={11}
          letterSpacing="0.2em"
          fill={INK_DIM}
        >
          GATEWAY
        </text>
        <text
          x={GW.x + GW.w - 16}
          y={GW.y + 28}
          textAnchor="end"
          fontFamily={MONO_FONT}
          fontSize={8.5}
          fill={INK_DIM}
          opacity={0.6}
        >
          plan · execute
        </text>
        <line
          x1={GW.x}
          x2={GW.x + GW.w}
          y1={GW.y + 40}
          y2={GW.y + 40}
          stroke={HAIR}
        />
        <circle
          ref={set("ringIn")}
          cx={GW.x + 8}
          cy={MID}
          r={8}
          fill="none"
          stroke="#fff"
          strokeWidth={1.5}
          opacity={0}
        />
        {SUBS.map((sub, i) => (
          <g key={i}>
            <rect
              ref={set(`step${i}`)}
              x={GW.x + 14}
              y={STEP_Y[i] - 15}
              width={GW.w - 28}
              height={28}
              rx={7}
              fill="rgba(139,160,188,0.08)"
              stroke={CANON[sub.s].color}
              strokeOpacity={0.6}
              opacity={0.95}
            />
            <text
              x={GW.x + 26}
              y={STEP_Y[i] + 4}
              fontFamily={MONO_FONT}
              fontSize={10.5}
              fill={CODE}
            >
              {i + 1}. {sub.field}
            </text>
            <text
              x={GW.x + GW.w - 24}
              y={STEP_Y[i] + 4}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={8.5}
              fill={CANON[sub.s].color}
            >
              {CANON[sub.s].name.toLowerCase()}
            </text>
          </g>
        ))}
        {/* Compose slots. */}
        <text
          x={GW.x + 18}
          y={GW.y + GW.h - 14}
          fontFamily={MONO_FONT}
          fontSize={8.5}
          letterSpacing="0.14em"
          fill={INK_DIM}
        >
          COMPOSE
        </text>
        {SLOT_X.map((x, i) => (
          <circle
            key={i}
            ref={set(`slot${i}`)}
            cx={x}
            cy={GW.y + GW.h - 18}
            r={4}
            fill={CANON[SUBS[i].s].color}
            opacity={0.95}
          />
        ))}
        <text
          ref={set("composed")}
          x={GW.x + 74}
          y={GW.y + GW.h - 14}
          fontFamily={MONO_FONT}
          fontSize={9}
          fill="#8fd6a0"
          opacity={0.95}
        >
          ✓ one response
        </text>
        <circle
          ref={set("ringC")}
          cx={GW.x + 8}
          cy={MID}
          r={8}
          fill="none"
          stroke="#8fd6a0"
          strokeWidth={1.5}
          opacity={0}
        />

        {/* The subgraphs. */}
        <text
          x={648}
          y={104}
          fontFamily={MONO_FONT}
          fontSize={9}
          letterSpacing="0.2em"
          fill={INK_DIM}
          opacity={0.7}
        >
          SUBGRAPHS
        </text>
        {SUBS.map((sub, i) => (
          <g key={i}>
            <rect
              x={648}
              y={sub.y - 26}
              width={252}
              height={52}
              rx={11}
              fill={SURFACE}
              stroke={BORDER}
            />
            <rect
              x={664}
              y={sub.y - 6}
              width={11}
              height={11}
              rx={3}
              fill={CANON[sub.s].color}
            />
            <text
              x={686}
              y={sub.y + 4}
              fontFamily={MONO_FONT}
              fontSize={11.5}
              fill={CODE}
            >
              {CANON[sub.s].name}
            </text>
            <text
              x={884}
              y={sub.y + 4}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={8.5}
              fill={INK_DIM}
              opacity={0.7}
            >
              owns: {sub.field}
            </text>
            <rect
              ref={set(`busy${i}`)}
              x={648}
              y={sub.y - 26}
              width={252}
              height={52}
              rx={11}
              fill="none"
              stroke={CANON[sub.s].color}
              strokeOpacity={0.7}
              strokeWidth={1.25}
              opacity={0}
            />
            <circle
              ref={set(`ringS${i}`)}
              cx={648}
              cy={sub.y}
              r={5}
              fill="none"
              stroke={CANON[sub.s].color}
              strokeWidth={1.5}
              opacity={0}
            />
          </g>
        ))}

        {/* Traveling labels: the fields go out, the values come home. */}
        <g ref={set("qTag")} opacity={0}>
          <text
            x={0}
            y={-12}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill="#e8eef8"
          >
            {"query { name price delivery }"}
          </text>
        </g>
        <g ref={set("respTag")} opacity={0}>
          <text
            x={0}
            y={-12}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill="#8fd6a0"
          >
            one response
          </text>
        </g>
        {SUBS.map((sub, i) => (
          <g key={i}>
            <g ref={set(`fTag${i}`)} opacity={0}>
              <text
                x={0}
                y={-10}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={9.5}
                fill={CANON[sub.s].color}
              >
                {sub.field}
              </text>
            </g>
            <g ref={set(`rTag${i}`)} opacity={0}>
              <text
                x={0}
                y={-10}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={9.5}
                fill={CANON[sub.s].color}
              >
                {sub.value}
              </text>
            </g>
          </g>
        ))}

        <PulseGlyph
          set={set}
          id="q"
          main="#ffffff"
          soft="#ffffff"
          filter="hp-soft"
        />
        <PulseGlyph
          set={set}
          id="resp"
          main="#8fd6a0"
          soft="#d3f2db"
          filter="hp-soft"
        />
        {SUBS.map((sub, i) => (
          <g key={i}>
            <PulseGlyph
              set={set}
              id={`f${i}`}
              main={CANON[sub.s].color}
              soft={CANON[sub.s].soft}
              filter="hp-soft"
            />
            <PulseGlyph
              set={set}
              id={`r${i}`}
              main={CANON[sub.s].color}
              soft={CANON[sub.s].soft}
              filter="hp-soft"
            />
          </g>
        ))}

        <text
          x={460}
          y={404}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.18em"
          fill={INK_DIM}
          opacity={0.7}
        >
          ONE QUERY IN · PLANNED · EXECUTED PER SUBGRAPH · COMPOSED · ONE
          RESPONSE OUT
        </text>
      </svg>
    </div>
  );
}
