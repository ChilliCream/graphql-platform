"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import { clamp01, easeOutCubic, ramp } from "@/src/components/mocha/geometry";
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

const MIN_W = 640;
const H = 248;
const LABEL_W = 96;
const MARGIN_R = 12;
const HEAD_Y = 22;
const LANE_H = 92;
const LANE_GAP = 14;
const LANE_Y = [36, 36 + LANE_H + LANE_GAP] as const;
const CHIP_H = 26;
const CHAR_W = 6.1;

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
  plain: "rgba(154,172,200,0.75)",
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
const SILK = "rgba(154,172,200,0.75)";
const SILK_SOFT = "rgba(154,172,200,0.45)";
const INK = "#a1a3af";

interface PlacedChip extends Chip {
  readonly x: number;
  readonly y: number;
  readonly w: number;
}

interface Layout {
  readonly colX: readonly number[];
  readonly colW: number;
  readonly endX: number;
  readonly chips: readonly PlacedChip[];
}

function buildLayout(lw: number): Layout {
  const endX = lw - MARGIN_R;
  const colW = (endX - LABEL_W) / PHASES.length;
  const colX = PHASES.map((_, i) => LABEL_W + i * colW);
  const chips = CHIPS.map((chip) => {
    const w = Math.min(colW - 16, chip.text.length * CHAR_W + 26);
    const laneMid = LANE_Y[chip.lane] + LANE_H / 2;
    return {
      ...chip,
      w,
      x: colX[chip.phase] + (colW - w) / 2,
      y: laneMid + chip.slot * 19 - CHIP_H / 2,
    };
  });
  return { colX, colW, endX, chips };
}

/**
 * Two lanes over build, startup, and the first send: Mocha wires handlers and
 * reports a missing one at build time, MediatR scans and fails at runtime.
 */
export function BuildTimeVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const { els, set } = useElementRegistry();
  const [w, setW] = useState(1100);
  const lw = Math.max(w, MIN_W);
  const layout = useMemo(() => buildLayout(lw), [lw]);
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
        const cursorX = L.colX[0] + sweep * (L.endX - L.colX[0]);
        const cursorOn = t >= SWEEP_START && t < SWEEP_END + 300;
        setAttr("cursor", "x1", cursorX);
        setAttr("cursor", "x2", cursorX);
        setAttr("cursor", "opacity", cursorOn ? 0.8 * fade : 0);
        setAttr("cursorGlow", "x", cursorX - 14);
        setAttr("cursorGlow", "opacity", cursorOn ? 0.5 * fade : 0);

        L.chips.forEach((chip, i) => {
          const reached = cursorX >= chip.x + 8 || t >= SWEEP_END;
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

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur"
    >
      <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/10 to-transparent" />

      <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
        <svg
          viewBox={`0 0 ${lw} ${H}`}
          width="100%"
          height={(H * w) / lw}
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
            <linearGradient id="build-time-cursor" x1="0" x2="1">
              <stop offset="0" stopColor={CYAN} stopOpacity={0} />
              <stop offset="0.5" stopColor={CYAN} stopOpacity={0.25} />
              <stop offset="1" stopColor={CYAN} stopOpacity={0} />
            </linearGradient>
          </defs>

          <rect
            x={0}
            y={0}
            width={lw}
            height={H}
            fill="url(#build-time-grid)"
          />

          {PHASES.map((phase, i) => (
            <g key={phase}>
              <text
                x={L.colX[i] + L.colW / 2}
                y={HEAD_Y}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={10}
                letterSpacing="0.2em"
                fill={SILK}
              >
                {phase}
              </text>
              {i > 0 ? (
                <line
                  x1={L.colX[i]}
                  x2={L.colX[i]}
                  y1={LANE_Y[0] - 4}
                  y2={LANE_Y[1] + LANE_H + 4}
                  stroke={HAIR}
                  strokeDasharray="3 4"
                />
              ) : null}
            </g>
          ))}

          {LANES.map((lane, i) => (
            <g key={lane}>
              <rect
                x={4}
                y={LANE_Y[i]}
                width={L.endX - 4}
                height={LANE_H}
                rx={3}
                fill={i === 0 ? MOCHA_FILL : PANEL_FILL}
                stroke={PANEL_STROKE}
                strokeWidth={1}
              />
              <circle cx={10} cy={LANE_Y[i] + 6} r={1.2} fill={SILK} />
              <text
                x={16}
                y={LANE_Y[i] + LANE_H / 2 + 3.5}
                fontFamily={MONO_FONT}
                fontSize={10}
                letterSpacing="0.16em"
                fill={i === 0 ? CYAN : SILK}
              >
                {lane}
              </text>
            </g>
          ))}

          <text
            x={L.colX[0] + L.colW / 2}
            y={LANE_Y[1] + LANE_H / 2 + 3.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={SILK_SOFT}
          >
            nothing checked
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
                  y={chip.y + 5}
                  width={3}
                  height={CHIP_H - 10}
                  rx={1.5}
                  fill={color}
                  opacity={0.6}
                />
                <text
                  x={chip.x + 12}
                  y={chip.y + 17}
                  fontFamily={MONO_FONT}
                  fontSize={10}
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
                    x={chip.x + 12}
                    y={chip.y + 17}
                    fontFamily={MONO_FONT}
                    fontSize={10}
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
            x={0}
            y={LANE_Y[0] - 6}
            width={28}
            height={LANE_Y[1] + LANE_H - LANE_Y[0] + 12}
            fill="url(#build-time-cursor)"
            opacity={0}
          />
          <line
            ref={set("cursor")}
            x1={0}
            x2={0}
            y1={LANE_Y[0] - 6}
            y2={LANE_Y[1] + LANE_H + 6}
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
  const start = layout.colX[0];
  const u = (chip.x + 8 - start) / (layout.endX - start);
  // Invert easeOutCubic so the ring starts when the cursor arrives.
  return 1 - Math.cbrt(1 - clamp01(u));
}
