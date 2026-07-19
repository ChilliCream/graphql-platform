"use client";

/**
 * Query planning in the stream language. One white query bead rises into the
 * node; sparks shoot up three streams; Catalog answers fast, Billing mid,
 * User slow — three collect dots fill beside the node while it visibly waits
 * at 2/3 — then one response rides the line home. Latency captions sit by
 * the three markers so the asymmetry reads as fact, not accident.
 */

import { MONO_FONT } from "../palette";
import { PulseGlyph, easeInOutCubic, measure, ramp, useVisual } from "./anim";
import {
  CANON,
  GlowNode,
  INK_DIM,
  NodeCaption,
  StreamMarker,
  stream,
} from "./stage";

const T = 11500;

const HEADS = [
  { x: 130, y: 46 },
  { x: 290, y: 84 },
  { x: 440, y: 120 },
  { x: 615, y: 66 },
  { x: 785, y: 102 },
] as const;

const NODE: readonly [number, number] = [450, 330];
const STREAMS = HEADS.map((hd) => stream(hd.x, hd.y + 8, NODE));
const OUT_UP = measure([
  [450, 452],
  [450, 342],
]);
const OUT_DOWN = measure([
  [450, 342],
  [450, 452],
]);

// The plan touches Catalog (fast), Billing (mid), User (slow).
const CALLS = [
  { s: 0, lat: "~80ms", back: [3000, 3800] as const },
  { s: 1, lat: "~300ms", back: [3400, 4200] as const },
  { s: 4, lat: "~1.2s", back: [5000, 5800] as const },
];

export function QueryPlanVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 11000, 11400);

    // One query in.
    if (t >= 500 && t < 1300) {
      h.placePulse(
        "q",
        OUT_UP,
        easeInOutCubic(ramp(t, 500, 1300)),
        Math.min((t - 500) / 150, 1),
        2.7,
      );
    } else {
      h.hidePulse("q");
    }
    h.setRing("ringQ", (t - 1300) / 500, 11, 22);

    // The plan brightens the three streams it will use.
    CALLS.forEach(({ s }, k) => {
      const plan =
        t >= 1400 && t < 2000 ? 0.7 * (0.6 + 0.4 * Math.sin(t / 90)) : 0;
      h.setO(`plan${k}`, plan);
      // Sparks up.
      if (t >= 1900 && t < 2700) {
        const u = easeInOutCubic(ramp(t, 1900, 2700)) * 0.92;
        h.placePulse(`up${k}`, STREAMS[s].up, u, 0.85, 2);
      } else {
        h.hidePulse(`up${k}`);
      }
      h.setRing(`ringS${k}`, (t - 2700) / 450, 4, 9);
    });

    // Answers come home at their own speeds; collect dots fill.
    CALLS.forEach(({ s, back }, k) => {
      if (t >= back[0] && t < back[1]) {
        const u = 0.08 + easeInOutCubic(ramp(t, back[0], back[1])) * 0.92;
        h.placePulse(`dn${k}`, STREAMS[s].poly, u, 1, 2.2);
      } else {
        h.hidePulse(`dn${k}`);
      }
      const on = ramp(t, back[1], back[1] + 150) * (1 - ramp(t, 9600, 10000));
      h.setO(`got${k}`, on * 0.95);
    });

    // The visible wait at 2/3.
    const wait = t >= 4400 && t < 5800 ? 1 : 0;
    h.setO("waiting", wait * (0.5 + 0.35 * Math.sin(t / 250)));

    // Merge; one response out.
    h.setRing("ringM", (t - 5900) / 500, 11, 24);
    if (t >= 6100 && t < 6900) {
      h.placePulse(
        "resp",
        OUT_DOWN,
        easeInOutCubic(ramp(t, 6100, 6900)),
        1,
        2.8,
      );
    } else {
      h.hidePulse("resp");
    }
    const tag = easeInOutCubic(ramp(t, 6900, 7300));
    h.setPop("tag", tag * 0.92 * master, tag);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 460" width="100%" className="block">
        <defs>
          <filter id="qv-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Streams. */}
        {STREAMS.map((s, i) => (
          <path
            key={i}
            d={s.d}
            fill="none"
            stroke={CANON[i].color}
            strokeWidth={2}
            strokeOpacity={0.85}
            strokeLinecap="round"
          />
        ))}
        {/* Plan overlays (brighten the used streams). */}
        {CALLS.map(({ s }, k) => (
          <path
            key={k}
            ref={set(`plan${k}`)}
            d={STREAMS[s].d}
            fill="none"
            stroke="#ffffff"
            strokeWidth={2}
            opacity={0}
            strokeLinecap="round"
          />
        ))}

        {/* Markers, with honest latencies on the three the plan uses. */}
        {HEADS.map((hd, i) => (
          <StreamMarker
            key={CANON[i].name}
            x={hd.x}
            y={hd.y}
            color={CANON[i].color}
            label={CANON[i].name}
            labelSide={i === 4 ? "left" : "right"}
          />
        ))}
        {CALLS.map(({ s, lat }, k) => (
          <text
            key={k}
            x={s === 4 ? HEADS[s].x - 16 : HEADS[s].x + 16}
            y={HEADS[s].y + 20}
            textAnchor={s === 4 ? "end" : "start"}
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={INK_DIM}
            opacity={0.7}
          >
            {lat}
          </text>
        ))}

        {/* Output line. */}
        <rect
          x={449.25}
          y={342}
          width={1.5}
          height={112}
          fill="#f5f0ea"
          opacity={0.4}
        />

        {/* The node, the wait, the collect dots. */}
        <GlowNode x={NODE[0]} y={NODE[1]} id="qv-node" r={8} />
        <NodeCaption x={330} y={NODE[1]} label="query plan" toX={424} />
        <circle
          ref={set("ringQ")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={11}
          fill="none"
          stroke="#fff"
          strokeWidth={1.5}
          opacity={0}
        />
        <circle
          ref={set("ringM")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={11}
          fill="none"
          stroke="#fff"
          strokeWidth={1.5}
          opacity={0}
        />
        {CALLS.map(({ s }, k) => (
          <g key={k}>
            <circle
              ref={set(`ringS${k}`)}
              cx={HEADS[s].x}
              cy={HEADS[s].y + 10}
              r={4}
              fill="none"
              stroke={CANON[s].color}
              strokeWidth={1.5}
              opacity={0}
            />
            <circle
              ref={set(`got${k}`)}
              cx={506 + k * 16}
              cy={NODE[1] + 24}
              r={3.5}
              fill={CANON[s].color}
              opacity={0}
            />
          </g>
        ))}
        <text
          ref={set("waiting")}
          x={506}
          y={NODE[1] + 46}
          fontFamily={MONO_FONT}
          fontSize={9}
          fill={CANON[4].color}
          opacity={0}
        >
          2/3 · waiting on user
        </text>

        <g ref={set("tag")} opacity={0.92}>
          <text
            x={466}
            y={446}
            fontFamily={MONO_FONT}
            fontSize={10}
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
          filter="qv-soft"
        />
        <PulseGlyph
          set={set}
          id="resp"
          main="#ffffff"
          soft="#ffffff"
          filter="qv-soft"
        />
        {CALLS.map(({ s }, k) => (
          <g key={k}>
            <PulseGlyph
              set={set}
              id={`up${k}`}
              main={CANON[s].color}
              soft={CANON[s].soft}
              filter="qv-soft"
            />
            <PulseGlyph
              set={set}
              id={`dn${k}`}
              main={CANON[s].color}
              soft={CANON[s].soft}
              filter="qv-soft"
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
