"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  clamp01,
  easeInOutCubic,
  easeOutCubic,
  ramp,
} from "@/src/components/mocha/geometry";
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

const MIN_W = 640;
const H = 236;
const TRACK_X0 = 34;
const MARGIN_R = 40;
const TOP_Y = 104;
const BOTTOM_Y = 196;
const NODE_R = 7;

/** Node positions along the top track, as fractions of its length. */
const STOPS = [0, 0.1, 0.21, 0.34, 0.53, 0.74, 1] as const;
const GAPS = ["200 ms", "400 ms", "800 ms", "5 min", "15 min", "30 min"];
const RETRIES = 3;
const DEAD_LETTER_AT = 0.36;

const GRID_DOT = "rgba(150,166,194,0.10)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const TRACK = "rgba(139,160,188,0.4)";
const HAIR = "rgba(139,160,188,0.3)";
const SILK = "rgba(154,172,200,0.75)";
const SILK_SOFT = "rgba(154,172,200,0.55)";

interface Layout {
  readonly xs: readonly number[];
  readonly x1: number;
  readonly deadX: number;
}

function buildLayout(lw: number): Layout {
  const x1 = lw - MARGIN_R;
  const span = x1 - TRACK_X0;
  return {
    xs: STOPS.map((s) => TRACK_X0 + s * span),
    x1,
    deadX: TRACK_X0 + DEAD_LETTER_AT * span,
  };
}

/** Arrival time of the pulse at each top-track node. */
const ARRIVALS = STOPS.map((_, i) => START + i * (HOP + DWELL));
const BOTTOM_START = START + 900;
const BOTTOM_ARRIVE = BOTTOM_START + 1400;

/**
 * Mocha's default exception policy as a timeline: three in-process retries
 * with exponential backoff, redelivery after 5, 15, and 30 minutes, and a
 * validation error that goes straight to the error endpoint.
 */
export function RetryVisual() {
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

      const placePulse = (k: string, x: number, y: number, op: number) => {
        setAttr(k, "opacity", op);
        setAttr(`${k}core`, "cx", x);
        setAttr(`${k}core`, "cy", y);
        setAttr(`${k}glow`, "cx", x);
        setAttr(`${k}glow`, "cy", y);
      };

      const apply = (t: number) => {
        const L = layoutRef.current;
        const fade = 1 - ramp(t, REST_T + 500, T - 100);
        const last = STOPS.length - 1;

        // Top track: the pulse hops node to node and waits at each one.
        let px = L.xs[0];
        let pOn = 0;
        for (let i = 0; i < last; i++) {
          const leave = ARRIVALS[i] + DWELL;
          if (t >= ARRIVALS[i] && t < leave) {
            px = L.xs[i];
            pOn = 1;
          } else if (t >= leave && t < ARRIVALS[i + 1]) {
            const u = easeInOutCubic(ramp(t, leave, ARRIVALS[i + 1]));
            px = L.xs[i] + u * (L.xs[i + 1] - L.xs[i]);
            pOn = 1;
          }
        }
        placePulse(
          "top",
          px,
          TOP_Y,
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

        // Bottom track: a validation error skips the retries.
        const bu = easeInOutCubic(ramp(t, BOTTOM_START + DWELL, BOTTOM_ARRIVE));
        const bOn =
          t >= BOTTOM_START && t < BOTTOM_ARRIVE
            ? 1 - ramp(t, BOTTOM_ARRIVE - 120, BOTTOM_ARRIVE)
            : 0;
        placePulse("bottom", L.xs[0] + bu * (L.deadX - L.xs[0]), BOTTOM_Y, bOn);
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
  const retryEnd = L.xs[RETRIES];

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

  const bracket = (x0: number, x1: number, label: string) => (
    <g>
      <path
        d={`M${x0} 52 V46 H${x1} V52`}
        fill="none"
        stroke={HAIR}
        strokeWidth={1}
      />
      <text
        x={(x0 + x1) / 2}
        y={38}
        textAnchor="middle"
        fontFamily={MONO_FONT}
        fontSize={10}
        letterSpacing="0.16em"
        fill={SILK}
      >
        {label}
      </text>
    </g>
  );

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

          <rect x={0} y={0} width={lw} height={H} fill="url(#retry-grid)" />

          {bracket(L.xs[0], retryEnd, "RETRY · IN PROCESS")}
          {bracket(L.xs[RETRIES + 1], L.xs[last], "REDELIVER · SCHEDULED")}

          <text
            x={TRACK_X0 - 20}
            y={TOP_Y - 28}
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={SILK_SOFT}
          >
            TimeoutException
          </text>
          <path
            d={`M${L.xs[0]} ${TOP_Y} H${L.xs[last]}`}
            stroke={TRACK}
            strokeWidth={1.5}
          />

          {GAPS.map((gap, i) => (
            <g key={gap}>
              <text
                x={(L.xs[i] + L.xs[i + 1]) / 2}
                y={TOP_Y - 10}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={9}
                fill={SILK_SOFT}
              >
                {gap}
              </text>
              <text
                ref={set(`gap${i}`)}
                x={(L.xs[i] + L.xs[i + 1]) / 2}
                y={TOP_Y - 10}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={9}
                fill={i < RETRIES ? CYAN : SILK}
                opacity={0}
              >
                {gap}
              </text>
            </g>
          ))}

          {L.xs.map((x, i) => {
            const ok = i === last;
            const color = ok ? GREEN : CORAL;
            return (
              <g key={STOPS[i]}>
                <circle
                  cx={x}
                  cy={TOP_Y}
                  r={NODE_R}
                  fill={NAVY}
                  stroke={PANEL_STROKE}
                  strokeWidth={1.2}
                />
                <g ref={set(`node${i}`)} opacity={0}>
                  <circle
                    cx={x}
                    cy={TOP_Y}
                    r={NODE_R}
                    fill={color}
                    opacity={0.16}
                  />
                  <circle
                    cx={x}
                    cy={TOP_Y}
                    r={NODE_R}
                    fill="none"
                    stroke={color}
                    strokeWidth={1.4}
                  />
                  {ok ? (
                    <path
                      d={`M${x - 3} ${TOP_Y} l2 2.4 l4 -4.6`}
                      fill="none"
                      stroke={color}
                      strokeWidth={1.6}
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  ) : (
                    <path
                      d={`M${x - 2.6} ${TOP_Y - 2.6} l5.2 5.2 M${x + 2.6} ${TOP_Y - 2.6} l-5.2 5.2`}
                      stroke={color}
                      strokeWidth={1.5}
                      strokeLinecap="round"
                    />
                  )}
                </g>
                <circle
                  ref={set(`ring${i}`)}
                  cx={x}
                  cy={TOP_Y}
                  r={NODE_R}
                  fill="none"
                  stroke={color}
                  strokeWidth={1.4}
                  opacity={0}
                />
                <text
                  x={x}
                  y={TOP_Y + 26}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={9}
                  fill={SILK_SOFT}
                >
                  {i === 0
                    ? "try"
                    : i <= RETRIES
                      ? `retry ${i}`
                      : `redelivery ${i - RETRIES}`}
                </text>
              </g>
            );
          })}
          <text
            ref={set("handled")}
            x={L.xs[last]}
            y={TOP_Y + 40}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.12em"
            fill={GREEN}
            opacity={0}
          >
            HANDLED
          </text>

          <text
            x={TRACK_X0 - 20}
            y={BOTTOM_Y - 20}
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={SILK_SOFT}
          >
            ValidationException
          </text>
          <path
            d={`M${L.xs[0]} ${BOTTOM_Y} H${L.deadX}`}
            stroke={TRACK}
            strokeWidth={1.5}
            strokeDasharray="4 4"
          />
          <circle
            cx={L.xs[0]}
            cy={BOTTOM_Y}
            r={NODE_R}
            fill={NAVY}
            stroke={PANEL_STROKE}
            strokeWidth={1.2}
          />
          <g ref={set("validationNode")} opacity={0}>
            <circle
              cx={L.xs[0]}
              cy={BOTTOM_Y}
              r={NODE_R}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.4}
            />
            <path
              d={`M${L.xs[0] - 2.6} ${BOTTOM_Y - 2.6} l5.2 5.2 M${L.xs[0] + 2.6} ${BOTTOM_Y - 2.6} l-5.2 5.2`}
              stroke={CORAL}
              strokeWidth={1.5}
              strokeLinecap="round"
            />
          </g>
          <rect
            x={L.deadX}
            y={BOTTOM_Y - 13}
            width={76}
            height={26}
            rx={5}
            fill={NAVY}
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <text
            x={L.deadX + 38}
            y={BOTTOM_Y + 3.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={SILK}
          >
            _error
          </text>
          <g ref={set("dead")} opacity={0}>
            <rect
              x={L.deadX}
              y={BOTTOM_Y - 13}
              width={76}
              height={26}
              rx={5}
              fill={CORAL}
              opacity={0.1}
            />
            <rect
              x={L.deadX}
              y={BOTTOM_Y - 13}
              width={76}
              height={26}
              rx={5}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.2}
            />
            <text
              x={L.deadX + 38}
              y={BOTTOM_Y + 3.5}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={10}
              fill={CORAL}
            >
              _error
            </text>
            <text
              x={L.deadX + 88}
              y={BOTTOM_Y + 3.5}
              fontFamily={MONO_FONT}
              fontSize={9}
              letterSpacing="0.12em"
              fill={CORAL}
            >
              DEAD-LETTERED, NO RETRIES
            </text>
          </g>
          <rect
            ref={set("deadRing")}
            x={L.deadX - 3}
            y={BOTTOM_Y - 16}
            width={82}
            height={32}
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
