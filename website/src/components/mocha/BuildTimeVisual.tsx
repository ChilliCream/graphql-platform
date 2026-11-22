"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import { clamp01, easeOutCubic, ramp } from "@/src/components/mocha/geometry";
import {
  CAPS_SIZE,
  capsCharW,
  LABEL,
  LABEL_CHAR_W,
  LABEL_DIM,
  LABEL_SIZE,
} from "@/src/components/mocha/labels";
import {
  AMBER,
  CORAL,
  CYAN,
  GREEN,
  MONO_FONT,
} from "@/src/components/mocha/palette";
import { useElementRegistry } from "@/src/components/mocha/useElementRegistry";
import { useRafLoop } from "@/src/components/mocha/useRafLoop";

const T = 7600;
const SWEEP_START = 300;
const SWEEP_END = 5600;
const REST_T = 6400;

/** Below this container width the phases stack top to bottom. */
const NARROW_BELOW = 616;
const WIDE_MIN_W = 616;
/** Below this width the lane labels move from their own column into the lanes. */
const LABEL_COLUMN_FROM = 760;
const NARROW_MIN_W = 280;
const CHIP_H = 28;
const CHIP_GAP = 6;
/** Chip text starts this far in, after the tone bar. */
const CHIP_PAD_L = 10;
const CHIP_PAD_R = 10;
/** Width kept free in the top-right corner for `MochaVisual`'s pause button. */
const PAUSE_CLEAR = 36;

const PHASES = ["BUILD", "STARTUP", "FIRST SEND"] as const;
const LANES = ["MOCHA", "MEDIATR"] as const;

type Tone = "mocha" | "plain" | "warn" | "error" | "ok";

interface Chip {
  readonly lane: 0 | 1;
  readonly phase: 0 | 1 | 2;
  /** Vertical slot inside the lane: -1 upper, 0 centered, 1 lower. */
  readonly slot: -1 | 0 | 1;
  readonly text: string;
  readonly tone: Tone;
}

const CHIPS: readonly Chip[] = [
  {
    lane: 0,
    phase: 0,
    slot: -1,
    text: "generates AddOrdering()",
    tone: "mocha",
  },
  { lane: 0, phase: 0, slot: 1, text: "MO0001: no handler", tone: "warn" },
  { lane: 0, phase: 1, slot: 0, text: "pipelines built once", tone: "mocha" },
  { lane: 0, phase: 2, slot: 0, text: "direct delegate call", tone: "ok" },
  { lane: 1, phase: 1, slot: 0, text: "scans assemblies", tone: "plain" },
  {
    lane: 1,
    phase: 2,
    slot: -1,
    text: "wrapper via reflection",
    tone: "plain",
  },
  {
    lane: 1,
    phase: 2,
    slot: 1,
    text: "missing handler throws",
    tone: "error",
  },
];

const TONE_COLOR: Record<Tone, string> = {
  mocha: CYAN,
  plain: LABEL,
  warn: AMBER,
  error: CORAL,
  ok: GREEN,
};

const SURFACE = "#0c1322";
const GRID_DOT = "rgba(150,166,194,0.10)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const PANEL_FILL = "rgba(139,160,188,0.03)";
const MOCHA_FILL = "rgba(22,185,228,0.04)";
const HAIR = "rgba(139,160,188,0.22)";
const INK = "#a1a3af";

interface Text {
  readonly x: number;
  readonly y: number;
  readonly anchor: "start" | "middle";
  readonly text: string;
}

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

interface Band extends Rect {
  readonly lane: 0 | 1;
}

interface PlacedChip extends Chip {
  readonly x: number;
  readonly y: number;
  readonly w: number;
}

interface Layout {
  readonly w: number;
  readonly h: number;
  /** The cursor sweeps along x (wide) or y (narrow). */
  readonly axis: "x" | "y";
  readonly sweepFrom: number;
  readonly sweepTo: number;
  /** Extent of the cursor line across the sweep axis. */
  readonly cross: readonly [number, number];
  readonly phases: readonly Text[];
  readonly lanes: readonly (Text & { readonly lane: 0 | 1 })[];
  readonly bands: readonly Band[];
  readonly rules: readonly string[];
  readonly idle: Text;
  readonly chips: readonly PlacedChip[];
}

function chipW(text: string, max: number): number {
  return Math.min(max, text.length * LABEL_CHAR_W + CHIP_PAD_L + CHIP_PAD_R);
}

/**
 * Phases are columns, Mocha and MediatR are lanes across them. The lane
 * labels sit in a column of their own, or at the top of each lane when the
 * columns need that width.
 */
function wideLayout(lw: number): Layout {
  const inLane = lw < LABEL_COLUMN_FROM;
  const labelW = inLane ? 4 : 96;
  const endX = lw - 12;
  const laneH = inLane ? 112 : 96;
  /** Chips sit lower when the lane label is above them. */
  const chipDrop = inLane ? 8 : 0;
  const laneY = [38, 38 + laneH + 14] as const;
  const colW = (endX - labelW) / PHASES.length;
  const colX = PHASES.map((_, i) => labelW + i * colW);
  const chips = CHIPS.map((chip) => {
    const w = chipW(chip.text, colW - 12);
    const laneMid = laneY[chip.lane] + laneH / 2 + chipDrop;
    return {
      ...chip,
      w,
      x: colX[chip.phase] + (colW - w) / 2,
      y: laneMid + chip.slot * 20 - CHIP_H / 2,
    };
  });
  return {
    w: lw,
    h: laneY[1] + laneH + 10,
    axis: "x",
    sweepFrom: colX[0],
    sweepTo: endX,
    cross: [laneY[0] - 6, laneY[1] + laneH + 6],
    phases: PHASES.map((text, i) => ({
      x: colX[i] + colW / 2,
      y: 24,
      anchor: "middle",
      text,
    })),
    lanes: LANES.map((text, i) => ({
      x: 16,
      y: inLane ? laneY[i] + 20 : laneY[i] + laneH / 2 + 4,
      anchor: "start",
      text,
      lane: i as 0 | 1,
    })),
    bands: LANES.map((_, i) => ({
      x: 4,
      y: laneY[i],
      w: endX - 4,
      h: laneH,
      lane: i as 0 | 1,
    })),
    rules: colX
      .slice(1)
      .map((x) => `M${x} ${laneY[0] - 4} V${laneY[1] + laneH + 4}`),
    idle: {
      x: colX[0] + colW / 2,
      y: laneY[1] + laneH / 2 + chipDrop + 4,
      anchor: "middle",
      text: "nothing checked",
    },
    chips,
  };
}

/** Phases stack top to bottom, each with a Mocha row and a MediatR row. */
function narrowLayout(lw: number): Layout {
  const pad = 4;
  const bandPad = 6;
  const chipX = pad + 78;
  const chipMax = lw - pad - 8 - chipX;
  const phases: Text[] = [];
  const lanes: (Text & { lane: 0 | 1 })[] = [];
  const bands: Band[] = [];
  const rules: string[] = [];
  const rowTop = new Map<string, number>();
  let idle: Text = { x: 0, y: 0, anchor: "start", text: "" };
  let y = 0;
  let firstBand = 0;

  PHASES.forEach((phase, p) => {
    phases.push({ x: pad, y: y + 12, anchor: "start", text: phase });
    const ruleX = pad + phase.length * capsCharW(0.2) + 10;
    // The first rule stops short of the pause button in the corner.
    const ruleEnd = lw - pad - (p === 0 ? PAUSE_CLEAR : 0);
    rules.push(`M${ruleX} ${y + 8} H${ruleEnd}`);
    y += p === 0 ? 30 : 24;
    if (p === 0) {
      firstBand = y;
    }
    LANES.forEach((lane, l) => {
      const own = rowOf(p, l);
      const n = Math.max(own.length, 1);
      const rowH = n * CHIP_H + (n - 1) * CHIP_GAP;
      bands.push({
        x: pad,
        y,
        w: lw - 2 * pad,
        h: rowH + 2 * bandPad,
        lane: l as 0 | 1,
      });
      lanes.push({
        x: pad + 10,
        y: y + bandPad + rowH / 2 + 4,
        anchor: "start",
        text: lane,
        lane: l as 0 | 1,
      });
      rowTop.set(`${p} ${l}`, y + bandPad);
      if (own.length === 0) {
        idle = {
          x: chipX,
          y: y + bandPad + CHIP_H / 2 + 4,
          anchor: "start",
          text: "nothing checked",
        };
      }
      y += rowH + 2 * bandPad + CHIP_GAP;
    });
    y += 10;
  });

  // Chips stay in source order: the animated elements are keyed by index.
  const chips = CHIPS.map((chip) => ({
    ...chip,
    x: chipX,
    y:
      (rowTop.get(`${chip.phase} ${chip.lane}`) ?? 0) +
      rowOf(chip.phase, chip.lane).indexOf(chip) * (CHIP_H + CHIP_GAP),
    w: chipW(chip.text, chipMax),
  }));

  return {
    w: lw,
    h: y - 10,
    axis: "y",
    sweepFrom: firstBand - 4,
    sweepTo: y - 10,
    cross: [pad, lw - pad],
    phases,
    lanes,
    bands,
    rules,
    idle,
    chips,
  };
}

/** The chips of one phase in one lane, top to bottom. */
function rowOf(phase: number, lane: number): readonly Chip[] {
  return CHIPS.filter((c) => c.phase === phase && c.lane === lane).sort(
    (a, b) => a.slot - b.slot,
  );
}

function buildLayout(w: number): Layout {
  return w < NARROW_BELOW
    ? narrowLayout(Math.max(w, NARROW_MIN_W))
    : wideLayout(Math.max(w, WIDE_MIN_W));
}

/** Position of a chip along the sweep axis, where the cursor lights it. */
function chipPos(layout: Layout, chip: PlacedChip): number {
  return (layout.axis === "x" ? chip.x : chip.y) + 8;
}

/**
 * Two lanes over build, startup, and the first send: Mocha wires handlers and
 * reports a missing one at build time, MediatR scans and fails at runtime.
 * Narrow containers stack the phases so the labels keep their size.
 */
export function BuildTimeVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const { els, set } = useElementRegistry();
  const [w, setW] = useState(1100);
  const layout = useMemo(() => buildLayout(w), [w]);
  const layoutRef = useRef(layout);

  useEffect(() => {
    layoutRef.current = layout;
  }, [layout]);

  useEffect(() => {
    const node = wrapRef.current;
    if (!node) {
      return;
    }
    const ro = new ResizeObserver((entries) => {
      const cw = entries[0]?.contentRect.width;
      if (cw && cw > 80) {
        setW(Math.round(cw));
      }
    });
    ro.observe(node);
    return () => ro.disconnect();
  }, []);

  useRafLoop(
    rootRef,
    () => {
      const setAttr = (k: string, name: string, v: number) => {
        els.get(k)?.setAttribute(name, v.toFixed(3));
      };

      const apply = (t: number) => {
        const L = layoutRef.current;
        const fade = 1 - ramp(t, REST_T + 400, T - 100);
        const sweep = easeOutCubic(ramp(t, SWEEP_START, SWEEP_END));
        const cursor = L.sweepFrom + sweep * (L.sweepTo - L.sweepFrom);
        const cursorOn = t >= SWEEP_START && t < SWEEP_END + 300;
        const [a1, a2] = L.axis === "x" ? ["x1", "x2"] : ["y1", "y2"];
        setAttr("cursor", a1, cursor);
        setAttr("cursor", a2, cursor);
        setAttr("cursor", "opacity", cursorOn ? 0.8 * fade : 0);
        setAttr("cursorGlow", L.axis, cursor - 14);
        setAttr("cursorGlow", "opacity", cursorOn ? 0.5 * fade : 0);

        L.chips.forEach((chip, i) => {
          const reached = cursor >= chipPos(L, chip) || t >= SWEEP_END;
          const since = reached
            ? t -
              (SWEEP_START + chipArrival(L, chip) * (SWEEP_END - SWEEP_START))
            : -1;
          const lit = reached ? fade : 0;
          setAttr(`lit${i}`, "opacity", lit);
          const ring = since >= 0 && since < 900 ? clamp01(since / 900) : 1;
          const flashes = chip.tone === "warn" || chip.tone === "error";
          setAttr(
            `ring${i}`,
            "opacity",
            flashes && reached && ring < 1 ? 0.6 * (1 - ring) : 0,
          );
          setAttr(`ring${i}`, "stroke-width", 1 + 5 * ring);
        });
      };

      apply(0);

      return { frame: (t) => apply(t), rest: () => apply(REST_T) };
    },
    { period: T },
  );

  const L = layout;
  const wide = L.axis === "x";

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-4 backdrop-blur sm:p-5"
    >
      <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/10 to-transparent" />

      <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
        <svg
          viewBox={`0 0 ${L.w} ${L.h}`}
          width="100%"
          height={(L.h * Math.min(w, L.w)) / L.w}
          className="block"
        >
          <defs>
            <pattern
              id="build-time-grid"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
            <linearGradient
              id="build-time-cursor"
              x1="0"
              y1="0"
              x2={wide ? "1" : "0"}
              y2={wide ? "0" : "1"}
            >
              <stop offset="0" stopColor={CYAN} stopOpacity={0} />
              <stop offset="0.5" stopColor={CYAN} stopOpacity={0.25} />
              <stop offset="1" stopColor={CYAN} stopOpacity={0} />
            </linearGradient>
          </defs>

          <rect
            x={0}
            y={0}
            width={L.w}
            height={L.h}
            fill="url(#build-time-grid)"
          />

          {L.phases.map((phase) => (
            <text
              key={phase.text}
              x={phase.x}
              y={phase.y}
              textAnchor={phase.anchor}
              fontFamily={MONO_FONT}
              fontSize={CAPS_SIZE}
              letterSpacing="0.2em"
              fill={LABEL}
            >
              {phase.text}
            </text>
          ))}
          {L.rules.map((d) => (
            <path
              key={d}
              d={d}
              stroke={HAIR}
              strokeDasharray={wide ? "3 4" : undefined}
            />
          ))}

          {L.bands.map((band) => (
            <g key={`${band.x} ${band.y}`}>
              <rect
                x={band.x}
                y={band.y}
                width={band.w}
                height={band.h}
                rx={3}
                fill={band.lane === 0 ? MOCHA_FILL : PANEL_FILL}
                stroke={PANEL_STROKE}
                strokeWidth={1}
              />
              <circle
                cx={band.x + 6}
                cy={band.y + 6}
                r={1.2}
                fill={LABEL_DIM}
              />
            </g>
          ))}
          {L.lanes.map((lane) => (
            <text
              key={`${lane.text} ${lane.y}`}
              x={lane.x}
              y={lane.y}
              textAnchor={lane.anchor}
              fontFamily={MONO_FONT}
              fontSize={CAPS_SIZE}
              letterSpacing={wide ? "0.16em" : "0.12em"}
              fill={lane.lane === 0 ? CYAN : LABEL}
            >
              {lane.text}
            </text>
          ))}

          <text
            x={L.idle.x}
            y={L.idle.y}
            textAnchor={L.idle.anchor}
            fontFamily={MONO_FONT}
            fontSize={LABEL_SIZE}
            fill={LABEL_DIM}
          >
            {L.idle.text}
          </text>

          {L.chips.map((chip, i) => {
            const color = TONE_COLOR[chip.tone];
            return (
              <g key={chip.text}>
                <rect
                  x={chip.x}
                  y={chip.y}
                  width={chip.w}
                  height={CHIP_H}
                  rx={5}
                  fill={SURFACE}
                  stroke={HAIR}
                  strokeWidth={1}
                />
                <rect
                  x={chip.x}
                  y={chip.y + 6}
                  width={3}
                  height={CHIP_H - 12}
                  rx={1.5}
                  fill={color}
                  opacity={0.6}
                />
                <text
                  x={chip.x + CHIP_PAD_L}
                  y={chip.y + 18}
                  fontFamily={MONO_FONT}
                  fontSize={LABEL_SIZE}
                  fill={INK}
                >
                  {chip.text}
                </text>
                <g ref={set(`lit${i}`)} opacity={0}>
                  <rect
                    x={chip.x}
                    y={chip.y}
                    width={chip.w}
                    height={CHIP_H}
                    rx={5}
                    fill={color}
                    opacity={0.08}
                  />
                  <rect
                    x={chip.x}
                    y={chip.y}
                    width={chip.w}
                    height={CHIP_H}
                    rx={5}
                    fill="none"
                    stroke={color}
                    strokeWidth={1.2}
                    opacity={0.8}
                  />
                  <text
                    x={chip.x + CHIP_PAD_L}
                    y={chip.y + 18}
                    fontFamily={MONO_FONT}
                    fontSize={LABEL_SIZE}
                    fill={color}
                  >
                    {chip.text}
                  </text>
                </g>
                <rect
                  ref={set(`ring${i}`)}
                  x={chip.x - 2}
                  y={chip.y - 2}
                  width={chip.w + 4}
                  height={CHIP_H + 4}
                  rx={7}
                  fill="none"
                  stroke={color}
                  opacity={0}
                />
              </g>
            );
          })}

          <rect
            ref={set("cursorGlow")}
            x={wide ? 0 : L.cross[0]}
            y={wide ? L.cross[0] : 0}
            width={wide ? 28 : L.cross[1] - L.cross[0]}
            height={wide ? L.cross[1] - L.cross[0] : 28}
            fill="url(#build-time-cursor)"
            opacity={0}
          />
          <line
            ref={set("cursor")}
            x1={wide ? 0 : L.cross[0]}
            x2={wide ? 0 : L.cross[1]}
            y1={wide ? L.cross[0] : 0}
            y2={wide ? L.cross[1] : 0}
            stroke={CYAN}
            strokeWidth={1.2}
            opacity={0}
          />
        </svg>
      </div>
    </div>
  );
}

/** Fraction of the sweep at which the cursor reaches a chip. */
function chipArrival(layout: Layout, chip: PlacedChip): number {
  const u =
    (chipPos(layout, chip) - layout.sweepFrom) /
    (layout.sweepTo - layout.sweepFrom);
  // Invert easeOutCubic so the ring starts when the cursor arrives.
  return 1 - Math.cbrt(1 - clamp01(u));
}
