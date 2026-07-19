"use client";

/**
 * The hero: the homepage FusionFlow motif, continued. The canon five streams
 * fall from their labeled markers and converge into the glowing composition
 * node; one line leaves it. The idle story loops the whole page in miniature:
 * a query bead rises into the node, sparks run up three streams at three
 * speeds, and one response returns. Built apart, queried together.
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

const T = 9000;
const NODE: readonly [number, number] = [450, 420];

const HEADS = [
  { x: 120, y: 50 },
  { x: 285, y: 96 },
  { x: 430, y: 140 },
  { x: 610, y: 76 },
  { x: 790, y: 122 },
] as const;

const STREAMS = HEADS.map((hd) => stream(hd.x, hd.y + 8, NODE));
const OUT_UP = measure([
  [450, 552],
  [450, 432],
]);
const OUT_DOWN = measure([
  [450, 432],
  [450, 552],
]);

// The three sparks: Catalog fast, Billing mid, Shipping slow.
const SPARKS = [
  { s: 0, back: [2600, 3400] as const },
  { s: 1, back: [3000, 3800] as const },
  { s: 3, back: [3600, 4400] as const },
];

export function HeroFlow() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // A query rises into the node.
    if (t >= 600 && t < 1400) {
      const u = easeInOutCubic(ramp(t, 600, 1400));
      h.placePulse("q", OUT_UP, u, Math.min((t - 600) / 150, 1), 2.6);
    } else {
      h.hidePulse("q");
    }
    h.setRing("ringN", (t - 1400) / 600, 12, 26);

    // Sparks run up three streams and return at three speeds.
    SPARKS.forEach(({ s, back }, k) => {
      if (t >= 1500 && t < 2300) {
        const u = easeInOutCubic(ramp(t, 1500, 2300)) * 0.92;
        h.placePulse(`up${k}`, STREAMS[s].up, u, 0.85, 2);
      } else {
        h.hidePulse(`up${k}`);
      }
      h.setRing(`ringM${k}`, (t - 2300) / 450, 4, 9);
      if (t >= back[0] && t < back[1]) {
        const u = 0.08 + easeInOutCubic(ramp(t, back[0], back[1])) * 0.92;
        h.placePulse(`dn${k}`, STREAMS[s].poly, u, 1, 2.2);
      } else {
        h.hidePulse(`dn${k}`);
      }
    });

    // Merge, and one response leaves.
    h.setRing("ringM", (t - 4500) / 500, 12, 22);
    if (t >= 4700 && t < 5500) {
      const u = easeInOutCubic(ramp(t, 4700, 5500));
      h.placePulse("resp", OUT_DOWN, u, 1, 2.8);
    } else {
      h.hidePulse("resp");
    }
    const cap = t >= 5400 && t < 8200 ? 0.75 : 0;
    h.setO("cap", cap);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 560" width="100%" className="block">
        <defs>
          <filter id="hf-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
          <linearGradient
            id="hf-out"
            x1="0"
            y1="432"
            x2="0"
            y2="560"
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor="#fff" stopOpacity="0.85" />
            <stop offset="1" stopColor="#66be77" stopOpacity="0.25" />
          </linearGradient>
        </defs>

        {/* The streams. */}
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

        {/* One line leaves the node. */}
        <rect x={449.25} y={432} width={1.5} height={128} fill="url(#hf-out)" />

        {/* Markers and labels. */}
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

        {/* The composition node. */}
        <GlowNode x={NODE[0]} y={NODE[1]} id="hf-node" />
        <NodeCaption x={330} y={NODE[1]} label="Fusion composition" toX={422} />
        <circle
          ref={set("ringN")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={12}
          fill="none"
          stroke="#fff"
          strokeWidth={1.5}
          opacity={0}
        />
        <circle
          ref={set("ringM")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={12}
          fill="none"
          stroke="#fff"
          strokeWidth={1.5}
          opacity={0}
        />
        {SPARKS.map(({ s }, k) => (
          <circle
            key={k}
            ref={set(`ringM${k}`)}
            cx={HEADS[s].x}
            cy={HEADS[s].y + 10}
            r={4}
            fill="none"
            stroke={CANON[s].color}
            strokeWidth={1.5}
            opacity={0}
          />
        ))}

        <text
          ref={set("cap")}
          x={466}
          y={548}
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.2em"
          fill={INK_DIM}
          opacity={0}
        >
          ONE RESPONSE
        </text>

        <PulseGlyph
          set={set}
          id="q"
          main="#ffffff"
          soft="#ffffff"
          filter="hf-soft"
        />
        <PulseGlyph
          set={set}
          id="resp"
          main="#ffffff"
          soft="#ffffff"
          filter="hf-soft"
        />
        {SPARKS.map(({ s }, k) => (
          <g key={k}>
            <PulseGlyph
              set={set}
              id={`up${k}`}
              main={CANON[s].color}
              soft={CANON[s].soft}
              filter="hf-soft"
            />
            <PulseGlyph
              set={set}
              id={`dn${k}`}
              main={CANON[s].color}
              soft={CANON[s].soft}
              filter="hf-soft"
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
