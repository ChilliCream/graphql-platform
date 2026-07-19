"use client";

/**
 * Composition in the stream language. Contract chips (each service's square,
 * in its color) drop down their streams into the node; merge and validate
 * tick beside it; then the composite comes alive as a conveyor of ownership
 * dashes riding the single output line. Mid-loop Billing publishes an update:
 * a quick recompose, a version bump, and the conveyor never stops.
 */

import { MONO_FONT } from "../palette";
import { easeInOutCubic, measure, pointAt, ramp, useVisual } from "./anim";
import {
  CANON,
  GlowNode,
  INK_DIM,
  NodeCaption,
  StreamMarker,
  stream,
} from "./stage";

const T = 12500;

const HEADS = [
  { x: 130, y: 46 },
  { x: 290, y: 84 },
  { x: 440, y: 120 },
  { x: 615, y: 66 },
  { x: 785, y: 102 },
] as const;

const NODE: readonly [number, number] = [450, 300];
const STREAMS = HEADS.map((hd) => stream(hd.x, hd.y + 8, NODE));
const OUT = measure([
  [450, 312],
  [450, 430],
]);
const DROPS = [400, 700, 1000, 1300, 1600] as const;

export function CompositionVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 11900, 12300);

    // Contract chips fall into the node.
    HEADS.forEach((_, i) => {
      const fire = DROPS[i];
      if (t >= fire && t < fire + 900) {
        const u = easeInOutCubic(ramp(t, fire, fire + 900));
        const [x, y] = pointAt(STREAMS[i].poly, u);
        h.setX(`chip${i}`, x, y);
        h.setO(`chip${i}`, Math.min((t - fire) / 120, 1));
      } else {
        h.setO(`chip${i}`, 0);
      }
    });
    h.setRing("ringIn", (t - 2500) / 550, 11, 22);

    // Merge ✓ then validate ✓ beside the node.
    const off = 1 - ramp(t, 11000, 11600);
    h.setO("merge", (t < 3300 ? ramp(t, 2600, 2900) : 1) * 0.8 * off);
    h.setO("mergeTick", ramp(t, 3000, 3150) * off);
    h.setO("validate", (t < 3900 ? ramp(t, 3200, 3500) : 1) * 0.8 * off);
    h.setO("validateTick", ramp(t, 3600, 3750) * off);

    // The composite goes live: an ownership conveyor rides the output line.
    const live = ramp(t, 3900, 4400);
    h.setO("outLine", 0.25 + 0.35 * live);
    CANON.forEach((_, i) => {
      const u = (((t / 5200 + i * 0.2) % 1) + 1) % 1;
      const on = live * (u < 0.06 ? u / 0.06 : u > 0.92 ? (1 - u) / 0.08 : 1);
      const [x, y] = pointAt(OUT, u);
      h.setX(`dash${i}`, x, y);
      h.setO(`dash${i}`, on * 0.95);
    });
    const tag1 =
      easeInOutCubic(ramp(t, 4400, 4800)) * (1 - ramp(t, 8900, 9100));
    h.setPop("v42", tag1 * 0.92, tag1);

    // Billing ships an update: recompose is routine.
    const upd = 7400;
    if (t >= upd && t < upd + 800) {
      const u = easeInOutCubic(ramp(t, upd, upd + 800));
      const [x, y] = pointAt(STREAMS[1].poly, u);
      h.setX("chipB", x, y);
      h.setO("chipB", 1);
    } else {
      h.setO("chipB", 0);
    }
    h.setO("mergeTick2", t >= 8300 && t < 8700 ? 0.95 : 0);
    h.setO("validateTick2", t >= 8500 && t < 8900 ? 0.95 : 0);
    h.setRing("ringUpd", (t - 8300) / 500, 11, 20);
    const tag2 = easeInOutCubic(ramp(t, 9000, 9400));
    h.setPop("v43", tag2 * 0.92 * master, tag2);
    h.setO("cap", (t >= 9400 && t < 11400 ? 0.75 : 0) * master);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 440" width="100%" className="block">
        {/* Streams and markers. */}
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

        {/* Output line: the composite itself. */}
        <rect
          ref={set("outLine")}
          x={449.25}
          y={312}
          width={1.5}
          height={118}
          fill="#f5f0ea"
          opacity={0.25}
        />

        {/* The node and its checks. */}
        <GlowNode x={NODE[0]} y={NODE[1]} id="cv-node" r={8} />
        <NodeCaption x={330} y={NODE[1]} label="Fusion composition" toX={424} />
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
        <g>
          <text
            ref={set("merge")}
            x={510}
            y={288}
            fontFamily={MONO_FONT}
            fontSize={10.5}
            letterSpacing="0.14em"
            fill={INK_DIM}
            opacity={0}
          >
            MERGE
          </text>
          <text
            ref={set("mergeTick")}
            x={575}
            y={288}
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#66be77"
            opacity={0}
          >
            ✓
          </text>
          <text
            ref={set("mergeTick2")}
            x={592}
            y={288}
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#66be77"
            opacity={0}
          >
            ✓
          </text>
          <text
            ref={set("validate")}
            x={510}
            y={310}
            fontFamily={MONO_FONT}
            fontSize={10.5}
            letterSpacing="0.14em"
            fill={INK_DIM}
            opacity={0}
          >
            VALIDATE
          </text>
          <text
            ref={set("validateTick")}
            x={598}
            y={310}
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#66be77"
            opacity={0}
          >
            ✓
          </text>
          <text
            ref={set("validateTick2")}
            x={615}
            y={310}
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#66be77"
            opacity={0}
          >
            ✓
          </text>
        </g>

        {/* Version tags and caption. */}
        <g ref={set("v42")} opacity={0.92}>
          <text
            x={466}
            y={402}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.18em"
            fill={INK_DIM}
          >
            COMPOSITE · V42
          </text>
        </g>
        <g ref={set("v43")} opacity={0}>
          <text
            x={466}
            y={402}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.18em"
            fill="#66be77"
          >
            COMPOSITE · V43
          </text>
        </g>
        <text
          ref={set("cap")}
          x={450}
          y={434}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.2em"
          fill={INK_DIM}
          opacity={0}
        >
          BILLING UPDATED · RECOMPOSED · ZERO DOWNTIME
        </text>

        {/* Contract chips: each service's own square, traveling. */}
        {CANON.map((c, i) => (
          <g key={c.name} ref={set(`chip${i}`)} opacity={0}>
            <rect
              x={-5}
              y={-5}
              width={10}
              height={10}
              rx={2.5}
              fill={c.color}
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

        {/* Ownership dashes riding the composite. */}
        {CANON.map((c, i) => (
          <g key={c.name} ref={set(`dash${i}`)} opacity={0}>
            <rect
              x={-1.75}
              y={-5}
              width={3.5}
              height={10}
              rx={1.5}
              fill={c.color}
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
