"use client";

/**
 * Query planning: the query is the story, so the query is on screen — every
 * field gutter-tagged with its owner's color. The white bead carries it to
 * the plan node; sparks climb three streams at three honest speeds; the node
 * waits at 2/3 for Billing; and one response lands in a JSON card whose rows
 * pop in the same owner colors. One request in, one response out.
 */

import { MONO_FONT } from "../palette";
import { PulseGlyph, easeInOutCubic, measure, ramp, useVisual } from "./anim";
import {
  sampleCubic,
  CANON,
  GlowNode,
  INK_DIM,
  NodeCaption,
  StreamMarker,
  stream,
} from "./stage";

const T = 12000;

const Q = { x: 36, y: 120, w: 250 } as const;
const QUERY_LINES = [
  { code: "{", bar: undefined },
  { code: '  product(id: "P-401") {', bar: "#ffffff" },
  { code: "    name", bar: CANON[0].color },
  { code: "    price", bar: CANON[1].color },
  { code: "    delivery", bar: CANON[3].color },
  { code: "  }", bar: undefined },
  { code: "}", bar: undefined },
] as const;

const NODE: readonly [number, number] = [430, 250];
const HEADS = [
  { s: 0, x: 560, y: 44, lat: "~80ms", back: [3000, 3800] as const },
  { s: 1, x: 700, y: 76, lat: "~900ms", back: [4900, 5700] as const },
  { s: 3, x: 840, y: 50, lat: "~300ms", back: [3400, 4200] as const },
] as const;
const STREAMS = HEADS.map((hd) => stream(hd.x, hd.y + 8, NODE, 0.28));

const Q_IN = measure([
  [Q.x + Q.w, 250],
  [418, 250],
]);
const TO_CARD = measure(
  sampleCubic([430, 262], [430, 330], [520, 372], [608, 388]).pts,
);

const R = { x: 608, y: 356, w: 256 } as const;
const R_LINES = [
  '{ "product": {',
  '    "name": "Aero Mug",',
  '    "price": "24.90 EUR",',
  '    "delivery": "2d",',
  "} }",
] as const;

export function QueryPlanVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 11400, 11800);

    // The plan reads the query: gutter bars glow in owner order.
    QUERY_LINES.forEach((l, i) => {
      if (!l.bar) {
        return;
      }
      const glow =
        t >= 900 + i * 150 && t < 2100 ? 0.65 + 0.35 * Math.sin(t / 110) : 1;
      h.setO(`bar${i}`, ramp(t, 500 + i * 120, 650 + i * 120) * 0.95 * glow);
    });

    // One request in.
    if (t >= 500 && t < 1200) {
      h.placePulse(
        "q",
        Q_IN,
        easeInOutCubic(ramp(t, 500, 1200)),
        Math.min((t - 500) / 130, 1),
        2.6,
      );
    } else {
      h.hidePulse("q");
    }
    h.setRing("ringQ", (t - 1200) / 500, 10, 20);

    // Sparks out, answers back at their own pace.
    HEADS.forEach((hd, k) => {
      if (t >= 1700 && t < 2500) {
        const u = easeInOutCubic(ramp(t, 1700, 2500)) * 0.94;
        h.placePulse(`up${k}`, STREAMS[k].up, u, 0.8, 1.9);
      } else {
        h.hidePulse(`up${k}`);
      }
      h.setRing(`ringS${k}`, (t - 2500) / 450, 4, 9);
      if (t >= hd.back[0] && t < hd.back[1]) {
        const u = 0.06 + easeInOutCubic(ramp(t, hd.back[0], hd.back[1])) * 0.94;
        h.placePulse(`dn${k}`, STREAMS[k].poly, u, 1, 2.2);
      } else {
        h.hidePulse(`dn${k}`);
      }
      const on =
        ramp(t, hd.back[1], hd.back[1] + 150) * (1 - ramp(t, 10200, 10600));
      h.setO(`got${k}`, on * 0.95);
    });

    // The wait at 2/3.
    const wait = t >= 4300 && t < 5700 ? 1 : 0;
    h.setO("waiting", wait * (0.5 + 0.35 * Math.sin(t / 250)));

    // Merge; one response into the JSON card.
    h.setRing("ringM", (t - 5800) / 500, 10, 22);
    if (t >= 6000 && t < 6900) {
      h.placePulse(
        "resp",
        TO_CARD,
        easeInOutCubic(ramp(t, 6000, 6900)),
        1,
        2.7,
      );
    } else {
      h.hidePulse("resp");
    }
    R_LINES.forEach((_, j) => {
      const on = easeInOutCubic(ramp(t, 6900 + j * 160, 7060 + j * 160));
      h.setPop(`r${j}`, on, on);
    });
    const tag = easeInOutCubic(ramp(t, 7900, 8300));
    h.setPop("tag", tag * 0.92 * master, tag);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 480" width="100%" className="block">
        <defs>
          <filter id="q6-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* The three service streams, falling into the plan node. */}
        {STREAMS.map((s, k) => (
          <path
            key={k}
            d={s.d}
            fill="none"
            stroke={CANON[HEADS[k].s].color}
            strokeWidth={2}
            strokeOpacity={0.8}
            strokeLinecap="round"
          />
        ))}
        {HEADS.map((hd, k) => (
          <g key={k}>
            <StreamMarker
              x={hd.x}
              y={hd.y}
              color={CANON[hd.s].color}
              label={CANON[hd.s].name}
              labelSide={k === 2 ? "left" : "right"}
            />
            <text
              x={k === 2 ? hd.x - 16 : hd.x + 16}
              y={hd.y + 20}
              textAnchor={k === 2 ? "end" : "start"}
              fontFamily={MONO_FONT}
              fontSize={9}
              fill={INK_DIM}
              opacity={0.7}
            >
              {hd.lat}
            </text>
            <circle
              ref={set(`ringS${k}`)}
              cx={hd.x}
              cy={hd.y + 10}
              r={4}
              fill="none"
              stroke={CANON[hd.s].color}
              strokeWidth={1.5}
              opacity={0}
            />
          </g>
        ))}

        {/* The query, with owners in the gutter. */}
        <rect
          x={Q.x}
          y={Q.y}
          width={Q.w}
          height={40 + QUERY_LINES.length * 18 + 12}
          rx={12}
          fill="rgba(12,19,34,0.5)"
          stroke="rgba(245,241,234,0.13)"
        />
        <text
          x={Q.x + 14}
          y={Q.y + 21}
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.16em"
          fill={INK_DIM}
        >
          ONE REQUEST
        </text>
        <line
          x1={Q.x}
          x2={Q.x + Q.w}
          y1={Q.y + 32}
          y2={Q.y + 32}
          stroke="rgba(245,241,234,0.1)"
        />
        {QUERY_LINES.map((l, i) => (
          <g key={i}>
            {l.bar && (
              <rect
                ref={set(`bar${i}`)}
                x={Q.x + 10}
                y={Q.y + 40 + i * 18}
                width={3}
                height={12}
                rx={1.5}
                fill={l.bar}
                opacity={0.95}
              />
            )}
            <text
              x={Q.x + 22}
              y={Q.y + 50 + i * 18}
              fontFamily={MONO_FONT}
              fontSize={11.5}
              fill="#c9d4e8"
            >
              {l.code}
            </text>
          </g>
        ))}

        {/* Lane into the node. */}
        <path
          d={`M${Q.x + Q.w} 250 H418`}
          fill="none"
          stroke="rgba(139,160,188,0.4)"
          strokeWidth={1.5}
        />

        <GlowNode x={NODE[0]} y={NODE[1]} id="q6-node" r={8} />
        <NodeCaption x={430} y={190} label="query plan" toX={430} />
        <circle
          ref={set("ringQ")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={10}
          fill="none"
          stroke="#fff"
          strokeWidth={1.5}
          opacity={0}
        />
        <circle
          ref={set("ringM")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={10}
          fill="none"
          stroke="#fff"
          strokeWidth={1.5}
          opacity={0}
        />
        {HEADS.map((hd, k) => (
          <circle
            key={k}
            ref={set(`got${k}`)}
            cx={402 + k * 16}
            cy={292}
            r={3.5}
            fill={CANON[hd.s].color}
            opacity={0}
          />
        ))}
        <text
          ref={set("waiting")}
          x={402}
          y={314}
          fontFamily={MONO_FONT}
          fontSize={9}
          fill={CANON[1].color}
          opacity={0}
        >
          2/3 · waiting on billing
        </text>

        {/* One response: JSON, owners still visible. */}
        <rect
          x={R.x}
          y={R.y}
          width={R.w}
          height={40 + R_LINES.length * 18 + 12}
          rx={12}
          fill="rgba(12,19,34,0.5)"
          stroke="rgba(94,234,212,0.35)"
        />
        <text
          x={R.x + 14}
          y={R.y + 21}
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.16em"
          fill="#5eead4"
        >
          ONE RESPONSE
        </text>
        <line
          x1={R.x}
          x2={R.x + R.w}
          y1={R.y + 32}
          y2={R.y + 32}
          stroke="rgba(245,241,234,0.1)"
        />
        {R_LINES.map((code, j) => (
          <g key={j} ref={set(`r${j}`)} opacity={1}>
            <text
              x={R.x + 16}
              y={R.y + 50 + j * 18}
              fontFamily={MONO_FONT}
              fontSize={11.5}
              fill={j === 0 || j === R_LINES.length - 1 ? "#c9d4e8" : "#e8eef8"}
            >
              {code}
            </text>
            {j > 0 && j < 4 && (
              <circle
                cx={R.x + R.w - 22}
                cy={R.y + 46 + j * 18}
                r={3}
                fill={[CANON[0].color, CANON[1].color, CANON[3].color][j - 1]}
              />
            )}
          </g>
        ))}
        <g ref={set("tag")} opacity={0.92}>
          <text
            x={R.x + 14}
            y={R.y + 40 + R_LINES.length * 18 + 26}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.18em"
            fill={INK_DIM}
          >
            200 · ONE ROUND TRIP
          </text>
        </g>

        <PulseGlyph
          set={set}
          id="q"
          main="#ffffff"
          soft="#ffffff"
          filter="q6-soft"
        />
        <PulseGlyph
          set={set}
          id="resp"
          main="#ffffff"
          soft="#ffffff"
          filter="q6-soft"
        />
        {HEADS.map((hd, k) => (
          <g key={k}>
            <PulseGlyph
              set={set}
              id={`up${k}`}
              main="#ffffff"
              soft="#ffffff"
              filter="q6-soft"
            />
            <PulseGlyph
              set={set}
              id={`dn${k}`}
              main={CANON[hd.s].color}
              soft={CANON[hd.s].soft}
              filter="q6-soft"
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
