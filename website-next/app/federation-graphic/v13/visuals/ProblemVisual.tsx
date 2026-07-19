"use client";

/**
 * The problem, told in the stream language. Act one: the five streams never
 * converge: five separate endpoints, and a client bead has to shuttle across
 * all of them to build one screen. Act two (crossfade): the same streams bend
 * into the composition node, and the client just rides one line.
 */

import { MONO_FONT } from "../palette";
import {
  PulseGlyph,
  clamp01,
  easeInOutCubic,
  measure,
  ramp,
  useVisual,
} from "./anim";
import {
  CANON,
  GlowNode,
  INK_DIM,
  NodeCaption,
  StreamMarker,
  stream,
} from "./stage";

const T = 15000;

const HEADS = [
  { x: 120, y: 44 },
  { x: 285, y: 82 },
  { x: 430, y: 118 },
  { x: 610, y: 64 },
  { x: 790, y: 100 },
] as const;

const NODE: readonly [number, number] = [450, 320];
const CONVERGE = HEADS.map((hd) => stream(hd.x, hd.y + 8, NODE));

// Act one shuttle: the client crosses all five endpoints, then back.
const SHUTTLE = measure([
  [70, 372],
  [840, 372],
]);
const SHUTTLE_BACK = measure([
  [840, 372],
  [70, 372],
]);
const PASS_1 = HEADS.map((hd) => 600 + ((hd.x - 70) / 770) * 2800);
const PASS_2 = HEADS.map((hd) => 4000 + ((840 - hd.x) / 770) * 2800);

const OUT_UP = measure([
  [450, 404],
  [450, 332],
]);
const OUT_DOWN = measure([
  [450, 332],
  [450, 404],
]);
const B_TRIPS = [8600, 10300, 12000] as const;

export function ProblemVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const down = ramp(t, 7000, 7700);
    const up = ramp(t, 14300, 14900);
    const gA = 1 - 0.9 * clamp01(down - up);
    const gB = 0.1 + 0.9 * clamp01(down - up);
    h.setO("gA", gA);
    h.setO("gB", gB);

    // Act one: the shuttle runs, endpoint rings fire as it passes.
    if (t >= 600 && t < 3400) {
      h.placePulse("sh1", SHUTTLE, easeInOutCubic(ramp(t, 600, 3400)), 1, 2.6);
    } else {
      h.hidePulse("sh1");
    }
    if (t >= 4000 && t < 6800) {
      h.placePulse(
        "sh2",
        SHUTTLE_BACK,
        easeInOutCubic(ramp(t, 4000, 6800)),
        1,
        2.6,
      );
    } else {
      h.hidePulse("sh2");
    }
    HEADS.forEach((_, i) => {
      h.setRing(`ringE1${i}`, (t - PASS_1[i]) / 450, 3, 9);
      h.setRing(`ringE2${i}`, (t - PASS_2[i]) / 450, 3, 9);
    });
    h.setO("capA", (t >= 1200 && t < 6600 ? 0.75 : 0) * gA);

    // Act two: one endpoint, quick round trips.
    B_TRIPS.forEach((f, k) => {
      if (t >= f && t < f + 600) {
        h.placePulse(
          `bq${k}`,
          OUT_UP,
          easeInOutCubic(ramp(t, f, f + 600)),
          1,
          2.4,
        );
      } else {
        h.hidePulse(`bq${k}`);
      }
      h.setRing(`ringN${k}`, (t - (f + 600)) / 400, 11, 20);
      if (t >= f + 750 && t < f + 1350) {
        h.placePulse(
          `br${k}`,
          OUT_DOWN,
          easeInOutCubic(ramp(t, f + 750, f + 1350)),
          1,
          2.4,
        );
      } else {
        h.hidePulse(`br${k}`);
      }
    });
    h.setO("capB", (t >= 9200 && t < 14000 ? 0.75 : 0) * gB);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 420" width="100%" className="block">
        <defs>
          <filter id="pv-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Markers stay through both acts. */}
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

        {/* Act one: parallel streams, five endpoints, one weary client. */}
        <g ref={set("gA")}>
          {HEADS.map((hd, i) => (
            <g key={i}>
              <path
                d={`M${hd.x} ${hd.y + 8} V340`}
                fill="none"
                stroke={CANON[i].color}
                strokeWidth={2}
                strokeOpacity={0.85}
                strokeLinecap="round"
              />
              <line
                x1={hd.x - 7}
                x2={hd.x + 7}
                y1={348}
                y2={348}
                stroke={CANON[i].color}
                strokeWidth={2.5}
                strokeLinecap="round"
              />
              <circle
                ref={set(`ringE1${i}`)}
                cx={hd.x}
                cy={360}
                r={3}
                fill="none"
                stroke={CANON[i].color}
                strokeWidth={1.5}
                opacity={0}
              />
              <circle
                ref={set(`ringE2${i}`)}
                cx={hd.x}
                cy={360}
                r={3}
                fill="none"
                stroke={CANON[i].color}
                strokeWidth={1.5}
                opacity={0}
              />
            </g>
          ))}
          <text
            ref={set("capA")}
            x={450}
            y={412}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.2em"
            fill={INK_DIM}
            opacity={0.75}
          >
            FIVE ENDPOINTS · EVERY CLIENT VISITS THEM ALL
          </text>
          <PulseGlyph
            set={set}
            id="sh1"
            main="#f5f0ea"
            soft="#ffffff"
            filter="pv-soft"
          />
          <PulseGlyph
            set={set}
            id="sh2"
            main="#f5f0ea"
            soft="#ffffff"
            filter="pv-soft"
          />
        </g>

        {/* Act two: the streams converge; the client rides one line. */}
        <g ref={set("gB")} opacity={0.1}>
          {CONVERGE.map((s, i) => (
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
          <rect
            x={449.25}
            y={332}
            width={1.5}
            height={78}
            fill="#f5f0ea"
            opacity={0.5}
          />
          <GlowNode x={NODE[0]} y={NODE[1]} id="pv-node" r={7} />
          <NodeCaption
            x={336}
            y={NODE[1]}
            label="Fusion composition"
            toX={426}
          />
          {B_TRIPS.map((_, k) => (
            <g key={k}>
              <circle
                ref={set(`ringN${k}`)}
                cx={NODE[0]}
                cy={NODE[1]}
                r={11}
                fill="none"
                stroke="#fff"
                strokeWidth={1.5}
                opacity={0}
              />
              <PulseGlyph
                set={set}
                id={`bq${k}`}
                main="#f5f0ea"
                soft="#ffffff"
                filter="pv-soft"
              />
              <PulseGlyph
                set={set}
                id={`br${k}`}
                main="#f5f0ea"
                soft="#ffffff"
                filter="pv-soft"
              />
            </g>
          ))}
          <text
            ref={set("capB")}
            x={450}
            y={412}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.2em"
            fill={INK_DIM}
            opacity={0}
          >
            ONE ENDPOINT · THE GRAPH DOES THE TRAVELING
          </text>
        </g>
      </svg>
    </div>
  );
}
