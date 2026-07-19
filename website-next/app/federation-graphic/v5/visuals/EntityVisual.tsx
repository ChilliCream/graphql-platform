"use client";

/**
 * The entity in the stream language. A lookup rises into the node; sparks ask
 * Catalog, Billing, and Shipping for P-401; each returns its facet in its own
 * color — and the facets assemble into one segmented ring on the output line:
 * one bead, three colored arcs, one identity. The assembled Product then
 * rides the line down to the client.
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

const T = 11000;

// Catalog knows the name, Billing the price, Shipping the delivery window.
const OWNERS = [
  { s: 0, x: 170, y: 54, facet: "name", back: [2400, 3200] as const },
  { s: 1, x: 450, y: 92, facet: "price", back: [2800, 3600] as const },
  { s: 3, x: 730, y: 64, facet: "delivery", back: [3400, 4200] as const },
] as const;

const NODE: readonly [number, number] = [450, 300];
const STREAMS = OWNERS.map((o) => stream(o.x, o.y + 8, NODE));
const OUT_UP = measure([
  [450, 404],
  [450, 312],
]);
const RING_Y = 352;

function arc(
  cx: number,
  cy: number,
  r: number,
  a0: number,
  a1: number,
): string {
  const x0 = cx + r * Math.cos(a0);
  const y0 = cy + r * Math.sin(a0);
  const x1 = cx + r * Math.cos(a1);
  const y1 = cy + r * Math.sin(a1);
  return `M${x0} ${y0} A${r} ${r} 0 0 1 ${x1} ${y1}`;
}

const GAP = 0.3;
const ARCS = OWNERS.map((_, i) => {
  const a0 = -Math.PI / 2 + (i * 2 * Math.PI) / 3 + GAP / 2;
  const a1 = -Math.PI / 2 + ((i + 1) * 2 * Math.PI) / 3 - GAP / 2;
  return arc(450, RING_Y, 15, a0, a1);
});

export function EntityVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 10300, 10700);

    // The lookup goes out to everyone at once.
    if (t >= 400 && t < 1200) {
      h.placePulse(
        "q",
        OUT_UP,
        easeInOutCubic(ramp(t, 400, 1200)),
        Math.min((t - 400) / 150, 1),
        2.6,
      );
    } else {
      h.hidePulse("q");
    }
    h.setRing("ringN", (t - 1200) / 500, 11, 20);
    const lk = t < 2200 ? ramp(t, 1200, 1400) : 1 - ramp(t, 2200, 2800);
    h.setO("lkTag", Math.max(0, lk) * 0.8);
    OWNERS.forEach((_, k) => {
      if (t >= 1300 && t < 2100) {
        const u = easeInOutCubic(ramp(t, 1300, 2100)) * 0.92;
        h.placePulse(`up${k}`, STREAMS[k].up, u, 0.85, 2);
      } else {
        h.hidePulse(`up${k}`);
      }
      h.setRing(`ringS${k}`, (t - 2100) / 450, 4, 9);
    });

    // Facets return and clip into the ring, arc by arc.
    OWNERS.forEach((o, k) => {
      if (t >= o.back[0] && t < o.back[1]) {
        const u = 0.08 + easeInOutCubic(ramp(t, o.back[0], o.back[1])) * 0.92;
        h.placePulse(`dn${k}`, STREAMS[k].poly, u, 1, 2.2);
      } else {
        h.hidePulse(`dn${k}`);
      }
      const on = easeInOutCubic(ramp(t, o.back[1] + 100, o.back[1] + 400));
      h.setO(`arc${k}`, on * 0.95);
      h.setO(`facet${k}`, on * 0.8);
    });

    // The identity closes the ring; the assembled Product rides down.
    const core = easeInOutCubic(ramp(t, 4500, 4900));
    h.setO("core", core);
    h.setO("idTag", core * 0.85);
    h.setRing("ringDone", (t - 4900) / 600, 17, 26);
    const slide = easeInOutCubic(ramp(t, 5800, 6800));
    h.setX("ring", 0, slide * 46);
    const gone = 1 - ramp(t, 6800, 7100);
    h.setO("ring", Math.min(1, core + 0.001) * gone);
    h.setO("cap", (t >= 5200 && t < 9800 ? 0.75 : 0) * master);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 420" width="100%" className="block">
        <defs>
          <filter id="ev-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {STREAMS.map((s, k) => (
          <path
            key={k}
            d={s.d}
            fill="none"
            stroke={CANON[OWNERS[k].s].color}
            strokeWidth={2}
            strokeOpacity={0.85}
            strokeLinecap="round"
          />
        ))}
        {OWNERS.map((o, k) => (
          <g key={k}>
            <StreamMarker
              x={o.x}
              y={o.y}
              color={CANON[o.s].color}
              label={CANON[o.s].name}
              labelSide={k === 2 ? "left" : "right"}
            />
            <text
              x={k === 2 ? o.x - 16 : o.x + 16}
              y={o.y + 20}
              textAnchor={k === 2 ? "end" : "start"}
              fontFamily={MONO_FONT}
              fontSize={9}
              fill={INK_DIM}
              opacity={0.7}
            >
              knows: {o.facet}
            </text>
            <circle
              ref={set(`ringS${k}`)}
              cx={o.x}
              cy={o.y + 10}
              r={4}
              fill="none"
              stroke={CANON[o.s].color}
              strokeWidth={1.5}
              opacity={0}
            />
          </g>
        ))}

        {/* Output line. */}
        <rect
          x={449.25}
          y={312}
          width={1.5}
          height={96}
          fill="#f5f0ea"
          opacity={0.4}
        />

        <GlowNode x={NODE[0]} y={NODE[1]} id="ev-node" r={7} />
        <NodeCaption x={330} y={NODE[1]} label="entity resolution" toX={426} />
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
          y={276}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          fill={INK_DIM}
          opacity={0}
        >
          {'lookup(id: "P-401")'}
        </text>

        {/* The segmented entity ring: one bead, three owners. */}
        <g ref={set("ring")} opacity={0}>
          {ARCS.map((d, k) => (
            <path
              key={k}
              ref={set(`arc${k}`)}
              d={d}
              fill="none"
              stroke={CANON[OWNERS[k].s].color}
              strokeWidth={3.5}
              strokeLinecap="round"
              opacity={0}
            />
          ))}
          <circle
            ref={set("core")}
            cx={450}
            cy={RING_Y}
            r={4.5}
            fill="#fff"
            opacity={0}
          />
          <text
            ref={set("idTag")}
            x={476}
            y={RING_Y + 4}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={INK_DIM}
            opacity={0}
          >
            Product · P-401
          </text>
        </g>
        <circle
          ref={set("ringDone")}
          cx={450}
          cy={RING_Y}
          r={17}
          fill="none"
          stroke="#fff"
          strokeWidth={1.25}
          opacity={0}
        />

        {/* Facet labels drift in beside the arcs. */}
        {OWNERS.map((o, k) => (
          <text
            key={k}
            ref={set(`facet${k}`)}
            x={k === 0 ? 386 : k === 1 ? 514 : 450}
            y={k === 2 ? RING_Y + 38 : RING_Y - 24}
            textAnchor={k === 0 ? "end" : k === 1 ? "start" : "middle"}
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={CANON[o.s].color}
            opacity={0}
          >
            {o.facet}
          </text>
        ))}

        <text
          ref={set("cap")}
          x={450}
          y={412}
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
          filter="ev-soft"
        />
        {OWNERS.map((o, k) => (
          <g key={k}>
            <PulseGlyph
              set={set}
              id={`up${k}`}
              main="#ffffff"
              soft="#ffffff"
              filter="ev-soft"
            />
            <PulseGlyph
              set={set}
              id={`dn${k}`}
              main={CANON[o.s].color}
              soft={CANON[o.s].soft}
              filter="ev-soft"
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
