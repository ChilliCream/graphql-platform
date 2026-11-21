"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  clamp01,
  easeInOutCubic,
  easeOutCubic,
  ramp,
} from "@/src/components/mocha/geometry";
import {
  CAPS_SIZE,
  LABEL,
  LABEL_CHAR_W,
  LABEL_DIM,
  LABEL_SIZE,
} from "@/src/components/mocha/labels";
import {
  CORAL,
  CORAL_SOFT,
  CYAN,
  GREEN,
  MONO_FONT,
  NAVY,
} from "@/src/components/mocha/palette";
import { useElementRegistry } from "@/src/components/mocha/useElementRegistry";
import { useRafLoop } from "@/src/components/mocha/useRafLoop";

const T = 9000;
const REST_T = 7600;
const START = 300;
const HOP = 520;
const DWELL = 260;

/** Below this container width the timeline runs top to bottom. */
const NARROW_BELOW = 560;
const WIDE_MIN_W = 560;
const NARROW_MIN_W = 280;
const NODE_R = 7;

/** Node positions along the timeline, as fractions of its length. */
const STOPS = [0, 0.11, 0.23, 0.36, 0.55, 0.76, 1] as const;
const GAPS = ["200 ms", "400 ms", "800 ms", "5 min", "15 min", "30 min"];
const RETRIES = 3;
const DEAD_LETTER_AT = 0.36;
const DEAD_W = 84;
const DEAD_H = 28;

const GRID_DOT = "rgba(150,166,194,0.10)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const TRACK = "rgba(139,160,188,0.4)";
const HAIR = "rgba(139,160,188,0.3)";

interface Pt {
  readonly x: number;
  readonly y: number;
}

interface Label {
  readonly x: number;
  readonly y: number;
  readonly anchor: "start" | "middle" | "end";
}

interface Group {
  /** Bracket over the group's nodes; the narrow layout has none. */
  readonly d: string | null;
  readonly label: Label;
  readonly text: string;
}

interface Layout {
  readonly w: number;
  readonly h: number;
  readonly nodes: readonly Pt[];
  readonly nodeLabels: readonly Label[];
  readonly gapLabels: readonly Label[];
  readonly groups: readonly Group[];
  readonly timeout: Label;
  readonly handled: Label;
  readonly validation: Label;
  readonly start: Pt;
  readonly dead: Pt;
  readonly note: Label;
}

const NODE_TEXT = STOPS.map((_, i) =>
  i === 0 ? "try" : i <= RETRIES ? `retry ${i}` : `redelivery ${i - RETRIES}`,
);

/** The timeline runs left to right, the error path below it. */
function wideLayout(lw: number): Layout {
  const x0 = 40;
  const x1 = lw - 48;
  const topY = 116;
  const bottomY = 220;
  const span = x1 - x0;
  const nodes = STOPS.map((s) => ({ x: x0 + s * span, y: topY }));
  const deadX = x0 + DEAD_LETTER_AT * span;
  const last = nodes.length - 1;
  const bracket = (a: number, b: number) => `M${a} 56 V50 H${b} V56`;
  return {
    w: lw,
    h: 256,
    nodes,
    nodeLabels: nodes.map((n) => ({ x: n.x, y: topY + 28, anchor: "middle" })),
    gapLabels: GAPS.map((_, i) => ({
      x: (nodes[i].x + nodes[i + 1].x) / 2,
      y: topY - 12,
      anchor: "middle",
    })),
    groups: [
      {
        d: bracket(nodes[0].x, nodes[RETRIES].x),
        label: {
          x: (nodes[0].x + nodes[RETRIES].x) / 2,
          y: 40,
          anchor: "middle",
        },
        text: "RETRY · IN PROCESS",
      },
      {
        d: bracket(nodes[RETRIES + 1].x, nodes[last].x),
        label: {
          x: (nodes[RETRIES + 1].x + nodes[last].x) / 2,
          y: 40,
          anchor: "middle",
        },
        text: "REDELIVER · SCHEDULED",
      },
    ],
    timeout: { x: x0 - 24, y: topY - 34, anchor: "start" },
    handled: { x: nodes[last].x, y: topY + 46, anchor: "middle" },
    validation: { x: x0 - 24, y: bottomY - 22, anchor: "start" },
    start: { x: x0, y: bottomY },
    dead: { x: deadX, y: bottomY },
    note: { x: deadX + DEAD_W + 12, y: bottomY + 4, anchor: "start" },
  };
}

/** The timeline runs top to bottom, with delays left of it, attempts right. */
function narrowLayout(lw: number): Layout {
  const pad = 4;
  const trackX = 62;
  const labelX = trackX + 18;
  const step = 40;
  const ys = STOPS.map((_, i) => 62 + i * step + (i > RETRIES ? 16 : 0));
  const nodes = ys.map((y) => ({ x: trackX, y }));
  const last = nodes.length - 1;
  const startY = ys[last] + 74;
  return {
    w: lw,
    h: startY + 54,
    nodes,
    nodeLabels: nodes.map((n) => ({ x: labelX, y: n.y + 4, anchor: "start" })),
    gapLabels: GAPS.map((_, i) => ({
      x: trackX - 16,
      y: (ys[i] + ys[i + 1]) / 2 + 4,
      anchor: "end",
    })),
    groups: [
      {
        d: null,
        label: { x: labelX, y: ys[0] - 22, anchor: "start" },
        text: "RETRY · IN PROCESS",
      },
      {
        d: null,
        label: { x: labelX, y: ys[RETRIES + 1] - 22, anchor: "start" },
        text: "REDELIVER · SCHEDULED",
      },
    ],
    timeout: { x: pad, y: 14, anchor: "start" },
    handled: {
      x: labelX + NODE_TEXT[last].length * LABEL_CHAR_W + 12,
      y: ys[last] + 4,
      anchor: "start",
    },
    validation: { x: pad, y: startY - 28, anchor: "start" },
    start: { x: trackX, y: startY },
    dead: { x: trackX + 56, y: startY },
    note: { x: trackX - NODE_R, y: startY + 38, anchor: "start" },
  };
}

function buildLayout(w: number): Layout {
  return w < NARROW_BELOW
    ? narrowLayout(Math.max(w, NARROW_MIN_W))
    : wideLayout(Math.max(w, WIDE_MIN_W));
}

/** Arrival time of the pulse at each node on the timeline. */
const ARRIVALS = STOPS.map((_, i) => START + i * (HOP + DWELL));
const BOTTOM_START = START + 900;
const BOTTOM_ARRIVE = BOTTOM_START + 1400;

/**
 * Mocha's default exception policy as a timeline: three in-process retries
 * with exponential backoff, redelivery after 5, 15, and 30 minutes, and a
 * validation error that goes straight to the error endpoint. Narrow
 * containers get a vertical timeline so the labels keep their size.
 */
export function RetryVisual() {
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

      const placePulse = (k: string, p: Pt, op: number) => {
        setAttr(k, "opacity", op);
        setAttr(`${k}core`, "cx", p.x);
        setAttr(`${k}core`, "cy", p.y);
        setAttr(`${k}glow`, "cx", p.x);
        setAttr(`${k}glow`, "cy", p.y);
      };

      const lerp = (a: Pt, b: Pt, u: number): Pt => ({
        x: a.x + u * (b.x - a.x),
        y: a.y + u * (b.y - a.y),
      });

      const apply = (t: number) => {
        const L = layoutRef.current;
        const fade = 1 - ramp(t, REST_T + 500, T - 100);
        const last = STOPS.length - 1;

        // Timeline: the pulse hops node to node and waits at each one.
        let p = L.nodes[0];
        let pOn = 0;
        for (let i = 0; i < last; i++) {
          const leave = ARRIVALS[i] + DWELL;
          if (t >= ARRIVALS[i] && t < leave) {
            p = L.nodes[i];
            pOn = 1;
          } else if (t >= leave && t < ARRIVALS[i + 1]) {
            const u = easeInOutCubic(ramp(t, leave, ARRIVALS[i + 1]));
            p = lerp(L.nodes[i], L.nodes[i + 1], u);
            pOn = 1;
          }
        }
        placePulse(
          "top",
          p,
          pOn * (1 - ramp(t, ARRIVALS[last] - 120, ARRIVALS[last])),
        );

        STOPS.forEach((_, i) => {
          const since = t - ARRIVALS[i];
          const reached = since >= 0;
          setAttr(`node${i}`, "opacity", reached ? fade : 0);
          const ring = clamp01(since / 800);
          setAttr(
            `ring${i}`,
            "opacity",
            reached && ring < 1 ? 0.6 * (1 - ring) : 0,
          );
          setAttr(`ring${i}`, "r", NODE_R + 9 * easeOutCubic(ring));
          if (i < last) {
            setAttr(`gap${i}`, "opacity", t >= ARRIVALS[i] + DWELL ? fade : 0);
          }
        });
        setAttr("handled", "opacity", t >= ARRIVALS[last] ? fade : 0);

        // Error path: a validation error skips the retries.
        const bu = easeInOutCubic(ramp(t, BOTTOM_START + DWELL, BOTTOM_ARRIVE));
        const bOn =
          t >= BOTTOM_START && t < BOTTOM_ARRIVE
            ? 1 - ramp(t, BOTTOM_ARRIVE - 120, BOTTOM_ARRIVE)
            : 0;
        placePulse("bottom", lerp(L.start, L.dead, bu), bOn);
        setAttr("validationNode", "opacity", t >= BOTTOM_START ? fade : 0);
        setAttr("dead", "opacity", t >= BOTTOM_ARRIVE ? fade : 0);
        const deadProgress = clamp01((t - BOTTOM_ARRIVE) / 800);
        setAttr(
          "deadRing",
          "opacity",
          t >= BOTTOM_ARRIVE && deadProgress < 1 ? 0.6 * (1 - deadProgress) : 0,
        );
      };

      apply(0);

      return { frame: (t) => apply(t), rest: () => apply(REST_T) };
    },
    { period: T },
  );

  const L = layout;
  const last = STOPS.length - 1;
  const first = L.nodes[0];
  const end = L.nodes[last];
  const box = { x: L.dead.x, y: L.dead.y - DEAD_H / 2 };

  const pulseGlyph = (k: string, color: string, inner: string) => (
    <g ref={set(k)} opacity={0}>
      <circle
        ref={set(`${k}glow`)}
        r={7}
        fill={color}
        opacity={0.25}
        filter="url(#retry-soft)"
      />
      <circle ref={set(`${k}core`)} r={3} fill={color} />
      <circle r={1.2} fill={inner} />
    </g>
  );

  /** A label; with `key` it is registered and starts hidden. */
  const text = (
    l: Label,
    content: string,
    fill: string,
    { caps = false, key }: { caps?: boolean; key?: string } = {},
  ) => (
    <text
      ref={key ? set(key) : undefined}
      x={l.x}
      y={l.y}
      textAnchor={l.anchor}
      fontFamily={MONO_FONT}
      fontSize={caps ? CAPS_SIZE : LABEL_SIZE}
      letterSpacing={caps ? "0.12em" : undefined}
      fill={fill}
      opacity={key ? 0 : undefined}
    >
      {content}
    </text>
  );

  const cross = (p: Pt) =>
    `M${p.x - 2.6} ${p.y - 2.6} l5.2 5.2 M${p.x + 2.6} ${p.y - 2.6} l-5.2 5.2`;

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
            <filter
              id="retry-soft"
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern
              id="retry-grid"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          <rect x={0} y={0} width={L.w} height={L.h} fill="url(#retry-grid)" />

          {L.groups.map((g) => (
            <g key={g.text}>
              {g.d ? (
                <path d={g.d} fill="none" stroke={HAIR} strokeWidth={1} />
              ) : null}
              <text
                x={g.label.x}
                y={g.label.y}
                textAnchor={g.label.anchor}
                fontFamily={MONO_FONT}
                fontSize={CAPS_SIZE}
                letterSpacing="0.16em"
                fill={LABEL}
              >
                {g.text}
              </text>
            </g>
          ))}

          {text(L.timeout, "TimeoutException", LABEL)}
          <path
            d={`M${first.x} ${first.y} L${end.x} ${end.y}`}
            stroke={TRACK}
            strokeWidth={1.5}
          />

          {GAPS.map((gap, i) => (
            <g key={gap}>
              {text(L.gapLabels[i], gap, LABEL_DIM)}
              {text(L.gapLabels[i], gap, i < RETRIES ? CYAN : LABEL, {
                key: `gap${i}`,
              })}
            </g>
          ))}

          {L.nodes.map((n, i) => {
            const ok = i === last;
            const color = ok ? GREEN : CORAL;
            return (
              <g key={STOPS[i]}>
                <circle
                  cx={n.x}
                  cy={n.y}
                  r={NODE_R}
                  fill={NAVY}
                  stroke={PANEL_STROKE}
                  strokeWidth={1.2}
                />
                <g ref={set(`node${i}`)} opacity={0}>
                  <circle
                    cx={n.x}
                    cy={n.y}
                    r={NODE_R}
                    fill={color}
                    opacity={0.16}
                  />
                  <circle
                    cx={n.x}
                    cy={n.y}
                    r={NODE_R}
                    fill="none"
                    stroke={color}
                    strokeWidth={1.4}
                  />
                  {ok ? (
                    <path
                      d={`M${n.x - 3} ${n.y} l2 2.4 l4 -4.6`}
                      fill="none"
                      stroke={color}
                      strokeWidth={1.6}
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  ) : (
                    <path
                      d={cross(n)}
                      stroke={color}
                      strokeWidth={1.5}
                      strokeLinecap="round"
                    />
                  )}
                </g>
                <circle
                  ref={set(`ring${i}`)}
                  cx={n.x}
                  cy={n.y}
                  r={NODE_R}
                  fill="none"
                  stroke={color}
                  strokeWidth={1.4}
                  opacity={0}
                />
                {text(L.nodeLabels[i], NODE_TEXT[i], LABEL_DIM)}
              </g>
            );
          })}
          {text(L.handled, "HANDLED", GREEN, { caps: true, key: "handled" })}

          {text(L.validation, "ValidationException", LABEL)}
          <path
            d={`M${L.start.x} ${L.start.y} H${L.dead.x}`}
            stroke={TRACK}
            strokeWidth={1.5}
            strokeDasharray="4 4"
          />
          <circle
            cx={L.start.x}
            cy={L.start.y}
            r={NODE_R}
            fill={NAVY}
            stroke={PANEL_STROKE}
            strokeWidth={1.2}
          />
          <g ref={set("validationNode")} opacity={0}>
            <circle
              cx={L.start.x}
              cy={L.start.y}
              r={NODE_R}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.4}
            />
            <path
              d={cross(L.start)}
              stroke={CORAL}
              strokeWidth={1.5}
              strokeLinecap="round"
            />
          </g>
          <rect
            x={box.x}
            y={box.y}
            width={DEAD_W}
            height={DEAD_H}
            rx={5}
            fill={NAVY}
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <text
            x={box.x + DEAD_W / 2}
            y={L.dead.y + 4}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={LABEL_SIZE}
            fill={LABEL}
          >
            _error
          </text>
          <g ref={set("dead")} opacity={0}>
            <rect
              x={box.x}
              y={box.y}
              width={DEAD_W}
              height={DEAD_H}
              rx={5}
              fill={CORAL}
              opacity={0.1}
            />
            <rect
              x={box.x}
              y={box.y}
              width={DEAD_W}
              height={DEAD_H}
              rx={5}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.2}
            />
            <text
              x={box.x + DEAD_W / 2}
              y={L.dead.y + 4}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={LABEL_SIZE}
              fill={CORAL}
            >
              _error
            </text>
            {text(L.note, "DEAD-LETTERED, NO RETRIES", CORAL, { caps: true })}
          </g>
          <rect
            ref={set("deadRing")}
            x={box.x - 3}
            y={box.y - 3}
            width={DEAD_W + 6}
            height={DEAD_H + 6}
            rx={7}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.4}
            opacity={0}
          />

          {pulseGlyph("top", CYAN, "#c9f1fb")}
          {pulseGlyph("bottom", CORAL, CORAL_SOFT)}
        </svg>
      </div>
    </div>
  );
}
