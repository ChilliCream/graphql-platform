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
import { CORAL, CYAN, GREEN, MONO_FONT } from "@/src/components/mocha/palette";
import { useElementRegistry } from "@/src/components/mocha/useElementRegistry";
import { useRafLoop } from "@/src/components/mocha/useRafLoop";

const T = 8000;
const START_AT = 300;
const SEG1: readonly [number, number] = [500, 1700];
const SEG2: readonly [number, number] = [3700, 4900];
const REST_T = 6600;
const FADE: readonly [number, number] = [7100, 7900];

/** Below this container width the states stack top to bottom. */
const NARROW_BELOW = 560;
const WIDE_MIN_W = 560;
const NARROW_MIN_W = 280;
const PANEL_H = 40;
const STORE_H = 28;

const STATES = ["SUBMITTED", "ACCEPTED"] as const;
const MESSAGES = ["OrderSubmitted", "OrderAccepted"] as const;
const STORE_TEXT = "OrderState { Id }";
const STORE_NOTE = "kept between messages";

const SURFACE = "#0c1322";
const GRID_DOT = "rgba(150,166,194,0.10)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const HAIR = "rgba(139,160,188,0.3)";

interface Pt {
  readonly x: number;
  readonly y: number;
}

interface Box {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

interface Label {
  readonly x: number;
  readonly y: number;
  readonly anchor: "start" | "middle";
}

interface Layout {
  readonly w: number;
  readonly h: number;
  readonly start: Pt;
  readonly startLabel: Label;
  readonly panels: readonly Box[];
  /** Rail segments into each state, as [from, to]. */
  readonly segs: readonly (readonly [Pt, Pt])[];
  readonly msgLabels: readonly Label[];
  readonly final: Label;
  readonly store: Box;
  readonly link: readonly [Pt, Pt];
  readonly note: Label | null;
}

const storeW = STORE_TEXT.length * LABEL_CHAR_W + 20;

/** States left to right, the saga state below the one that waits. */
function wideLayout(lw: number): Layout {
  const railY = 64;
  const panelW = 120;
  const start = { x: 40, y: railY };
  const sub = {
    x: Math.round(lw * 0.46) - panelW / 2,
    y: railY - PANEL_H / 2,
    w: panelW,
    h: PANEL_H,
  };
  const acc = { x: lw - 8 - panelW, y: sub.y, w: panelW, h: PANEL_H };
  const store = {
    x: sub.x + panelW / 2 - storeW / 2,
    y: railY + 66,
    w: storeW,
    h: STORE_H,
  };
  const segs = [
    [
      { x: start.x + 6, y: railY },
      { x: sub.x, y: railY },
    ],
    [
      { x: sub.x + panelW, y: railY },
      { x: acc.x, y: railY },
    ],
  ] as const;
  return {
    w: lw,
    h: store.y + STORE_H + 12,
    start,
    startLabel: { x: start.x, y: railY + 28, anchor: "middle" },
    panels: [sub, acc],
    segs,
    msgLabels: segs.map(([a, b]) => ({
      x: (a.x + b.x) / 2,
      y: railY - 12,
      anchor: "middle",
    })),
    final: { x: acc.x + panelW / 2, y: sub.y - 14, anchor: "middle" },
    store,
    link: [
      { x: sub.x + panelW / 2, y: sub.y + PANEL_H },
      { x: sub.x + panelW / 2, y: store.y },
    ],
    note: { x: store.x + storeW + 12, y: store.y + 18, anchor: "start" },
  };
}

/** States top to bottom, the saga state beside the one that waits. */
function narrowLayout(lw: number): Layout {
  const pad = 4;
  const panelW = 116;
  const cx = pad + panelW / 2;
  const start = { x: cx, y: 14 };
  const sub = { x: pad, y: 74, w: panelW, h: PANEL_H };
  const acc = { x: pad, y: 174, w: panelW, h: PANEL_H };
  const store = {
    x: pad + panelW + 12,
    y: sub.y + (PANEL_H - STORE_H) / 2,
    w: Math.min(storeW, lw - pad - (pad + panelW + 12)),
    h: STORE_H,
  };
  const segs = [
    [
      { x: cx, y: start.y + 6 },
      { x: cx, y: sub.y },
    ],
    [
      { x: cx, y: sub.y + PANEL_H },
      { x: cx, y: acc.y },
    ],
  ] as const;
  return {
    w: lw,
    h: acc.y + PANEL_H + 8,
    start,
    startLabel: { x: cx + 14, y: start.y + 4, anchor: "start" },
    panels: [sub, acc],
    segs,
    msgLabels: segs.map(([a, b]) => ({
      x: cx + 14,
      y: (a.y + b.y) / 2 + 4,
      anchor: "start",
    })),
    final: {
      x: pad + panelW + 12,
      y: acc.y + PANEL_H / 2 + 4,
      anchor: "start",
    },
    store,
    link: [
      { x: pad + panelW, y: sub.y + PANEL_H / 2 },
      { x: store.x, y: sub.y + PANEL_H / 2 },
    ],
    note: null,
  };
}

function buildLayout(w: number): Layout {
  return w < NARROW_BELOW
    ? narrowLayout(Math.max(w, NARROW_MIN_W))
    : wideLayout(Math.max(w, WIDE_MIN_W));
}

/**
 * The order saga from the comparison articles: `OrderSubmitted` creates the
 * saga in `Submitted`, its state is kept while it waits, and `OrderAccepted`
 * moves it to the final `Accepted` state.
 */
export function OrderSagaVisual() {
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

      /** Opacity of something shown from `a` to `b`, with short fades. */
      const window01 = (t: number, a: number, b: number) =>
        Math.min(ramp(t, a, a + 150), 1 - ramp(t, b - 160, b));

      const flash = (k: string, since: number) => {
        const q = since / 700;
        const on = since >= 0 && q < 1;
        setAttr(k, "opacity", on ? 0.5 * (1 - q) : 0);
        setAttr(k, "r", 4 + 12 * easeOutCubic(clamp01(q)));
      };

      const apply = (t: number) => {
        const L = layoutRef.current;
        const fade = 1 - ramp(t, FADE[0], FADE[1]);
        const enterSub = SEG1[1];
        const enterAcc = SEG2[1];

        // The message token rides the rail into the next state.
        const seg = t < SEG2[0] ? 0 : 1;
        const span = seg === 0 ? SEG1 : SEG2;
        const [a, b] = L.segs[seg];
        const u = easeInOutCubic(ramp(t, span[0], span[1]));
        const x = a.x + u * (b.x - a.x);
        const y = a.y + u * (b.y - a.y);
        setAttr("tok", "opacity", window01(t, span[0], span[1]));
        for (const k of ["tokcore", "tokglow"]) {
          setAttr(k, "cx", x);
          setAttr(k, "cy", y);
        }
        setAttr("msg0", "opacity", window01(t, SEG1[0], enterSub + 200));
        setAttr("msg1", "opacity", window01(t, SEG2[0], enterAcc + 200));

        setAttr(
          "startOn",
          "opacity",
          t >= START_AT ? (t < enterSub ? 1 : 0.5) * fade : 0,
        );
        flash("startRing", t - START_AT);

        // Submitted is active while the saga waits, then visited.
        setAttr(
          "p0",
          "opacity",
          t >= enterSub ? (t < SEG2[0] ? 1 : 0.55) * fade : 0,
        );
        setAttr("led0", "opacity", t >= enterSub && t < SEG2[0] ? 0.9 : 0);
        flash("ring0", t - enterSub);

        setAttr("store", "opacity", ramp(t, enterSub, enterSub + 300) * fade);

        setAttr("p1", "opacity", t >= enterAcc ? fade : 0);
        setAttr("led1", "opacity", t >= enterAcc ? 0.9 * fade : 0);
        setAttr("fin", "opacity", t >= enterAcc ? fade : 0);
        flash("ring1", t - enterAcc);
      };

      apply(0);

      return { frame: (t) => apply(t), rest: () => apply(REST_T) };
    },
    { period: T },
  );

  const L = layout;
  const [sub, acc] = L.panels;

  const panel = (box: Box, i: number, title: string, color: string) => (
    <g key={title}>
      <rect
        x={box.x}
        y={box.y}
        width={box.w}
        height={box.h}
        rx={3}
        fill={SURFACE}
        stroke={PANEL_STROKE}
        strokeWidth={1}
      />
      <circle cx={box.x + 6} cy={box.y + 6} r={1.2} fill={LABEL_DIM} />
      {panelTitle(box, title, LABEL)}
      <g ref={set(`p${i}`)} opacity={0}>
        <rect
          x={box.x}
          y={box.y}
          width={box.w}
          height={box.h}
          rx={3}
          fill={color}
          opacity={0.07}
        />
        <rect
          x={box.x}
          y={box.y}
          width={box.w}
          height={box.h}
          rx={3}
          fill="none"
          stroke={color}
          strokeWidth={1.2}
          opacity={0.6}
        />
        {panelTitle(box, title, color)}
      </g>
      <circle
        cx={box.x + box.w - 9}
        cy={box.y + 9}
        r={2}
        fill={LABEL_DIM}
        opacity={0.35}
      />
      <circle
        ref={set(`led${i}`)}
        cx={box.x + box.w - 9}
        cy={box.y + 9}
        r={2}
        fill={color}
        opacity={0}
      />
      <circle
        ref={set(`ring${i}`)}
        cx={L.segs[i][1].x}
        cy={L.segs[i][1].y}
        r={4}
        fill="none"
        stroke={color}
        strokeWidth={1.5}
        opacity={0}
      />
    </g>
  );

  const label = (l: Label, text: string, fill: string, key?: string) => (
    <text
      ref={key ? set(key) : undefined}
      x={l.x}
      y={l.y}
      textAnchor={l.anchor}
      fontFamily={MONO_FONT}
      fontSize={LABEL_SIZE}
      fill={fill}
      opacity={key ? 0 : undefined}
    >
      {text}
    </text>
  );

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
              id="order-saga-soft"
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern
              id="order-saga-grid"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          <rect
            x={0}
            y={0}
            width={L.w}
            height={L.h}
            fill="url(#order-saga-grid)"
          />

          {L.segs.map(([a, b]) => (
            <path
              key={`${a.x} ${a.y}`}
              d={`M${a.x} ${a.y} L${b.x} ${b.y}`}
              stroke={LANE_STROKE}
              strokeWidth={1.5}
            />
          ))}
          <path
            d={`M${L.link[0].x} ${L.link[0].y} L${L.link[1].x} ${L.link[1].y}`}
            stroke={HAIR}
            strokeWidth={1}
            strokeDasharray="3 3"
          />

          <circle cx={L.start.x} cy={L.start.y} r={5} fill={LABEL_DIM} />
          <circle
            ref={set("startOn")}
            cx={L.start.x}
            cy={L.start.y}
            r={5}
            fill={CYAN}
            opacity={0}
          />
          <circle
            ref={set("startRing")}
            cx={L.start.x}
            cy={L.start.y}
            r={4}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.5}
            opacity={0}
          />
          {label(L.startLabel, "Initially", LABEL_DIM)}

          {MESSAGES.map((m, i) => (
            <g key={m}>
              {label(L.msgLabels[i], m, LABEL)}
              {label(L.msgLabels[i], m, CORAL, `msg${i}`)}
            </g>
          ))}

          {panel(sub, 0, STATES[0], CYAN)}
          {panel(acc, 1, STATES[1], GREEN)}

          <text
            ref={set("fin")}
            x={L.final.x}
            y={L.final.y}
            textAnchor={L.final.anchor}
            fontFamily={MONO_FONT}
            fontSize={CAPS_SIZE}
            letterSpacing="0.26em"
            fill={GREEN}
            opacity={0}
          >
            FINAL
          </text>

          <rect
            x={L.store.x}
            y={L.store.y}
            width={L.store.w}
            height={L.store.h}
            rx={5}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <text
            x={L.store.x + 10}
            y={L.store.y + 18}
            fontFamily={MONO_FONT}
            fontSize={LABEL_SIZE}
            fill={LABEL}
          >
            {STORE_TEXT}
          </text>
          <g ref={set("store")} opacity={0}>
            <rect
              x={L.store.x}
              y={L.store.y}
              width={L.store.w}
              height={L.store.h}
              rx={5}
              fill={CYAN}
              opacity={0.07}
            />
            <rect
              x={L.store.x}
              y={L.store.y}
              width={L.store.w}
              height={L.store.h}
              rx={5}
              fill="none"
              stroke={CYAN}
              strokeWidth={1.2}
              opacity={0.6}
            />
            <text
              x={L.store.x + 10}
              y={L.store.y + 18}
              fontFamily={MONO_FONT}
              fontSize={LABEL_SIZE}
              fill={CYAN}
            >
              {STORE_TEXT}
            </text>
          </g>
          {L.note ? label(L.note, STORE_NOTE, LABEL_DIM) : null}

          <g ref={set("tok")} opacity={0}>
            <circle
              ref={set("tokglow")}
              r={6}
              fill={CORAL}
              opacity={0.25}
              filter="url(#order-saga-soft)"
            />
            <circle ref={set("tokcore")} r={2.8} fill={CORAL} />
          </g>
        </svg>
      </div>
    </div>
  );
}

function panelTitle(box: Box, title: string, fill: string) {
  return (
    <text
      x={box.x + box.w / 2}
      y={box.y + box.h / 2 + 4}
      textAnchor="middle"
      fontFamily={MONO_FONT}
      fontSize={CAPS_SIZE}
      letterSpacing="0.14em"
      fill={fill}
    >
      {title}
    </text>
  );
}
