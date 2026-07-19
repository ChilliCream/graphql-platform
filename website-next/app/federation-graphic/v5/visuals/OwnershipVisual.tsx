"use client";

/**
 * Ownership migration in the stream language. The composite's output line
 * carries an ownership conveyor; the price dash rides it in Catalog coral.
 * Mid-loop the price contract chip arcs across the sky from Catalog's marker
 * to Billing's — and the dashes simply change color on their next lap. Query
 * beads run the line the whole time. Same field, new owner, zero drama.
 */

import { MONO_FONT } from "../palette";
import {
  PulseGlyph,
  clamp01,
  easeInOutCubic,
  measure,
  pointAt,
  ramp,
  useVisual,
} from "./anim";
import {
  CANON,
  GlowNode,
  INK_DIM,
  NodeCaption,
  StreamMarker,
  sampleCubic,
  stream,
} from "./stage";

const T = 12000;
const MOVE = [4200, 5400] as const;

const CAT = { x: 300, y: 56 } as const;
const BILL = { x: 620, y: 64 } as const;
const NODE: readonly [number, number] = [450, 270];

const S_CAT = stream(CAT.x, CAT.y + 8, NODE);
const S_BILL = stream(BILL.x, BILL.y + 8, NODE);

// The handover: price arcs over the sky from Catalog to Billing.
const HANDOVER = measure(
  sampleCubic(
    [CAT.x, CAT.y - 10],
    [CAT.x + 90, CAT.y - 44],
    [BILL.x - 90, BILL.y - 48],
    [BILL.x, BILL.y - 10],
  ).pts,
);

const OUT_UP = measure([
  [450, 404],
  [450, 282],
]);
const OUT = measure([
  [450, 282],
  [450, 404],
]);
const Q_FIRES = [600, 2200, 3800, 6400, 8000, 9600] as const;

export function OwnershipVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 11500, 11900);

    // Queries run the line all loop long.
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
          OUT,
          easeInOutCubic(ramp(t, f + 650, f + 1200)),
          1,
          2.2,
        );
      } else {
        h.hidePulse(`r${k}`);
      }
    });

    // The ownership conveyor: price dash coral before the move, gold after.
    const moved = ramp(t, MOVE[1] + 200, MOVE[1] + 500);
    for (let i = 0; i < 3; i++) {
      const u = (((t / 4600 + i / 3) % 1) + 1) % 1;
      const fade = u < 0.08 ? u / 0.08 : u > 0.9 ? (1 - u) / 0.1 : 1;
      const [x, y] = pointAt(OUT, u);
      h.setX(`dashC${i}`, x, y);
      h.setX(`dashB${i}`, x, y);
      h.setO(`dashC${i}`, fade * 0.95 * (1 - moved));
      h.setO(`dashB${i}`, fade * 0.95 * moved);
    }

    // The handover chip arcs across; the markers acknowledge.
    if (t >= MOVE[0] && t < MOVE[1]) {
      const u = easeInOutCubic(ramp(t, MOVE[0], MOVE[1]));
      const [x, y] = pointAt(HANDOVER, u);
      h.setX("chip", x, y);
      h.setO("chip", Math.min((t - MOVE[0]) / 150, 1));
    } else {
      h.setO("chip", 0);
    }
    h.setRing("ringB", (t - MOVE[1]) / 500, 8, 16);
    h.setO("priceCat", 1 - 0.65 * clamp01(ramp(t, MOVE[1], MOVE[1] + 300)));
    h.setO("priceBill", ramp(t, MOVE[1], MOVE[1] + 300) * 0.85);
    const ov =
      easeInOutCubic(ramp(t, MOVE[1] + 100, MOVE[1] + 450)) *
      (1 - ramp(t, 7400, 7800));
    h.setPop("movedTag", ov * 0.9, ov);

    h.setO("cap", (t >= 7600 && t < 11200 ? 0.75 : 0) * master);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 420" width="100%" className="block">
        <defs>
          <filter id="ov-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        <path
          d={S_CAT.d}
          fill="none"
          stroke={CANON[0].color}
          strokeWidth={2}
          strokeOpacity={0.85}
          strokeLinecap="round"
        />
        <path
          d={S_BILL.d}
          fill="none"
          stroke={CANON[1].color}
          strokeWidth={2}
          strokeOpacity={0.85}
          strokeLinecap="round"
        />
        <StreamMarker
          x={CAT.x}
          y={CAT.y}
          color={CANON[0].color}
          label={CANON[0].name}
        />
        <StreamMarker
          x={BILL.x}
          y={BILL.y}
          color={CANON[1].color}
          label={CANON[1].name}
        />
        <text
          ref={set("priceCat")}
          x={CAT.x + 16}
          y={CAT.y + 22}
          fontFamily={MONO_FONT}
          fontSize={9}
          fill={INK_DIM}
          opacity={1}
        >
          owns: price
        </text>
        <text
          ref={set("priceBill")}
          x={BILL.x + 16}
          y={BILL.y + 22}
          fontFamily={MONO_FONT}
          fontSize={9}
          fill={CANON[1].color}
          opacity={0}
        >
          owns: price
        </text>
        <g ref={set("movedTag")} opacity={0}>
          <text
            x={BILL.x + 16}
            y={BILL.y + 40}
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={CANON[1].color}
          >
            @override accepted
          </text>
        </g>
        <circle
          ref={set("ringB")}
          cx={BILL.x}
          cy={BILL.y}
          r={8}
          fill="none"
          stroke={CANON[1].color}
          strokeWidth={1.5}
          opacity={0}
        />

        {/* Output line: the stable surface. */}
        <rect
          x={449.25}
          y={282}
          width={1.5}
          height={122}
          fill="#f5f0ea"
          opacity={0.4}
        />

        <GlowNode x={NODE[0]} y={NODE[1]} id="ov-node" r={7} />
        <NodeCaption
          x={330}
          y={NODE[1]}
          label="composite · unchanged"
          toX={426}
        />

        {/* The handover chip. */}
        <g ref={set("chip")} opacity={0}>
          <rect
            x={-5}
            y={-5}
            width={10}
            height={10}
            rx={2.5}
            fill={CANON[0].color}
            stroke={CANON[1].color}
            strokeWidth={1.5}
          />
          <text
            x={10}
            y={4}
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={INK_DIM}
          >
            price
          </text>
        </g>

        {/* Ownership dashes: coral lap, then gold lap. */}
        {[0, 1, 2].map((i) => (
          <g key={i}>
            <g ref={set(`dashC${i}`)} opacity={0}>
              <rect
                x={-1.75}
                y={-5}
                width={3.5}
                height={10}
                rx={1.5}
                fill={CANON[0].color}
              />
            </g>
            <g ref={set(`dashB${i}`)} opacity={0}>
              <rect
                x={-1.75}
                y={-5}
                width={3.5}
                height={10}
                rx={1.5}
                fill={CANON[1].color}
              />
            </g>
          </g>
        ))}

        <text
          ref={set("cap")}
          x={450}
          y={414}
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
              filter="ov-soft"
            />
            <PulseGlyph
              set={set}
              id={`r${k}`}
              main="#66be77"
              soft="#bce5c4"
              filter="ov-soft"
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
