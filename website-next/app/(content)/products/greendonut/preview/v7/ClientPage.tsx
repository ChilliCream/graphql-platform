"use client";

import type { CSSProperties, ReactNode } from "react";
import { useEffect, useRef, useState } from "react";
import type { MotionValue, Variants } from "motion/react";
import {
  MotionConfig,
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { GreenDonut } from "@/src/icons/GreenDonut";

// Brand spectrum, used at most once on this page (the MIT band hairline).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const ACCENT = "#5eead4"; // cc-accent teal, used for all motion strokes.

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface IndexTagProps {
  readonly value: string;
}

function IndexTag({ value }: IndexTagProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex h-6 items-center justify-center rounded-full border px-2 font-mono text-[11px] tabular-nums">
      {value}
    </span>
  );
}

// -----------------------------------------------------------------------------
// Hero preview: a small looped-in-view version of the tick collapse, sized for
// the hero card column. Renders a static final frame under prefers-reduced-motion.
// -----------------------------------------------------------------------------

const HERO_KEYS: readonly string[] = [
  "id: 7",
  "id: 12",
  "id: 3",
  "id: 9",
  "id: 21",
  "id: 4",
];

function HeroPreview() {
  const reduce = useReducedMotion() ?? false;
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.4 });

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-6 shadow-2xl"
    >
      <div className="flex items-center justify-between">
        <Eyebrow>Tick collapse</Eyebrow>
        <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
          one tick
        </span>
      </div>

      <div className="mt-5 grid grid-cols-1 gap-5 md:grid-cols-2">
        <div>
          <div className="text-cc-ink-dim mb-3 font-mono text-[11px] tracking-[0.18em] uppercase">
            Calls
          </div>
          <div className="border-cc-card-border bg-cc-bg/60 rounded-lg border p-4">
            <ul className="space-y-2">
              {HERO_KEYS.map((k, i) => (
                <motion.li
                  key={k}
                  initial={reduce ? false : { opacity: 0, x: -6 }}
                  animate={inView || reduce ? { opacity: 1, x: 0 } : undefined}
                  transition={{
                    duration: 0.35,
                    delay: reduce ? 0 : 0.05 * i,
                  }}
                  className="flex items-center justify-between font-mono text-[12px]"
                >
                  <span className="text-cc-ink">LoadAsync({k})</span>
                  <span className="text-cc-accent/80 font-mono text-[10.5px] tracking-[0.16em] uppercase">
                    pending
                  </span>
                </motion.li>
              ))}
            </ul>
          </div>
        </div>

        <div>
          <div className="text-cc-ink-dim mb-3 font-mono text-[11px] tracking-[0.18em] uppercase">
            One batch
          </div>
          <div className="border-cc-card-border bg-cc-bg/60 rounded-lg border p-4">
            <div className="mb-3 font-mono text-[12px]">
              <span className="text-cc-ink">Fetch(</span>
              <span className="text-cc-accent">[7, 12, 3, 9, 21, 4]</span>
              <span className="text-cc-ink">)</span>
            </div>
            <svg
              viewBox="0 0 220 90"
              className="w-full"
              role="img"
              aria-label="Six keys merge into one batched fetch on a tick boundary"
            >
              <defs>
                <linearGradient id="gd-hero-line" x1="0" y1="0" x2="1" y2="0">
                  <stop offset="0%" stopColor={ACCENT} stopOpacity="0.25" />
                  <stop offset="100%" stopColor={ACCENT} stopOpacity="0.9" />
                </linearGradient>
              </defs>
              {[10, 24, 38, 52, 66, 80].map((y, i) => (
                <g key={y}>
                  <circle
                    cx="14"
                    cy={y}
                    r="3"
                    fill={ACCENT}
                    fillOpacity="0.85"
                  />
                  <motion.path
                    d={`M18,${y} C 90,${y} 130,45 184,45`}
                    fill="none"
                    stroke="url(#gd-hero-line)"
                    strokeWidth="1.25"
                    strokeLinecap="round"
                    initial={reduce ? false : { pathLength: 0, opacity: 0.4 }}
                    animate={
                      inView || reduce
                        ? { pathLength: 1, opacity: 0.55 + i * 0.06 }
                        : undefined
                    }
                    transition={{
                      duration: 0.7,
                      delay: reduce ? 0 : 0.15 + 0.05 * i,
                      ease: "easeOut",
                    }}
                  />
                </g>
              ))}
              <motion.line
                x1="120"
                x2="120"
                y1="6"
                y2="84"
                stroke={ACCENT}
                strokeOpacity="0.35"
                strokeDasharray="2 3"
                initial={reduce ? false : { opacity: 0 }}
                animate={
                  inView || reduce ? { opacity: [0, 0.9, 0.35] } : undefined
                }
                transition={{ duration: 0.9, delay: reduce ? 0 : 0.45 }}
              />
              <rect
                x="184"
                y="36"
                width="30"
                height="18"
                rx="4"
                fill={ACCENT}
                fillOpacity="0.18"
                stroke={ACCENT}
                strokeOpacity="0.65"
              />
              <text
                x="199"
                y="48"
                textAnchor="middle"
                fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
                fontSize="9"
                fill={ACCENT}
              >
                IN (...)
              </text>
            </svg>
            <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
              <span className="text-cc-ink-dim font-mono text-[11px]">
                round trips
              </span>
              <span className="text-cc-accent font-mono text-[13px] font-semibold tabular-nums">
                1
              </span>
            </div>
          </div>
        </div>
      </div>

      <div className="border-cc-card-border mt-5 grid grid-cols-3 divide-x divide-[var(--color-cc-card-border)] overflow-hidden rounded-lg border text-center">
        <div className="px-3 py-3">
          <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
            Batch
          </div>
          <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
            one fetch
          </div>
        </div>
        <div className="px-3 py-3">
          <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
            Cache
          </div>
          <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
            per request
          </div>
        </div>
        <div className="px-3 py-3">
          <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
            Dedup
          </div>
          <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
            same key, once
          </div>
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// CENTERPIECE: on-enter Tick Collapse
//
// A single progress MotionValue animates from 0 -> 1 once the section enters
// the viewport. useTransform fans that out to: lane arrow positions, the
// tick-boundary pulse, the merged-arrow stroke draw, the DB node scale, the
// fan-out opacities, and the round-trips counter ticking 6 -> 1.
// useReducedMotion short-circuits to the final frame.
// -----------------------------------------------------------------------------

const LANES: readonly { readonly key: number; readonly y: number }[] = [
  { key: 7, y: 28 },
  { key: 12, y: 68 },
  { key: 3, y: 108 },
  { key: 9, y: 148 },
  { key: 21, y: 188 },
  { key: 4, y: 228 },
];

const BOUNDARY_X = 360;
const DB_X = 580;
const DB_Y = 128;
const FANOUT_X = 760;

interface TickCounterProps {
  readonly inView: boolean;
  readonly reduce: boolean;
}

function TickCounter({ inView, reduce }: TickCounterProps) {
  // One-shot count down 6 -> 1 once the section enters view, stepped across the
  // same window as the collapse animation. No scroll coupling.
  const [display, setDisplay] = useState(reduce ? 1 : 6);
  useEffect(() => {
    if (reduce || !inView) {
      return;
    }
    let i = 6;
    const id = setInterval(() => {
      i = i - 1;
      if (i <= 1) {
        setDisplay(1);
        clearInterval(id);
        return;
      }
      setDisplay(i);
    }, 420);
    return () => clearInterval(id);
  }, [inView, reduce]);
  return (
    <span className="text-cc-accent font-mono text-[28px] font-semibold tabular-nums">
      {display}
    </span>
  );
}

interface LaneArrowProps {
  readonly lane: { readonly key: number; readonly y: number };
  readonly index: number;
  readonly active: MotionValue<number>;
  readonly reduce: boolean;
}

function LaneArrow({ lane, index, active, reduce }: LaneArrowProps) {
  // Each lane has a slight stagger so the front isn't a flat wall.
  const stagger = index * 0.04;
  const headX = useTransform(
    active,
    [0 + stagger, 0.45 + stagger],
    [80, BOUNDARY_X],
    { clamp: true },
  );
  return (
    <g>
      <motion.line
        x1="80"
        y1={lane.y}
        x2={reduce ? BOUNDARY_X : headX}
        y2={lane.y}
        stroke={ACCENT}
        strokeOpacity="0.55"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <motion.circle
        cx={reduce ? BOUNDARY_X : headX}
        cy={lane.y}
        r="4"
        fill={ACCENT}
      />
      <text
        x="92"
        y={lane.y - 6}
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill={ACCENT}
        opacity="0.85"
      >
        LoadAsync({lane.key})
      </text>
    </g>
  );
}

interface FanOutArrowProps {
  readonly lane: { readonly key: number; readonly y: number };
  readonly index: number;
  readonly active: MotionValue<number>;
  readonly reduce: boolean;
}

function FanOutArrow({ lane, index, active, reduce }: FanOutArrowProps) {
  const fanOutStart = 0.78;
  const fanOutEnd = 0.98;
  const start = fanOutStart + (index * (fanOutEnd - fanOutStart)) / 12;
  const end = start + 0.05;
  const opacity = useTransform(active, [start, end], [0, 1], { clamp: true });
  return (
    <motion.g style={{ opacity: reduce ? 1 : opacity }}>
      <path
        d={`M${DB_X + 52},128 C ${DB_X + 110},128 ${FANOUT_X - 40},${lane.y} ${FANOUT_X},${lane.y}`}
        fill="none"
        stroke={ACCENT}
        strokeOpacity="0.55"
        strokeWidth="1.25"
        strokeLinecap="round"
      />
      <circle cx={FANOUT_X + 4} cy={lane.y} r="3.5" fill={ACCENT} />
      <text
        x={FANOUT_X + 14}
        y={lane.y + 4}
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill="currentColor"
        className="text-cc-ink"
      >
        [{lane.key}] {"=>"} User
      </text>
    </motion.g>
  );
}

function TickCollapse() {
  const reduce = useReducedMotion() ?? false;
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.35 });

  // A single progress MotionValue is animated 0 -> 1 once when the section
  // enters the viewport. All downstream useTransform calls read from this.
  const active = useMotionValue(reduce ? 1 : 0);

  useEffect(() => {
    if (reduce) {
      active.set(1);
      return;
    }
    if (!inView) {
      return;
    }
    const controls = animate(active, 1, {
      duration: 2.4,
      ease: "easeInOut",
    });
    return () => controls.stop();
  }, [active, inView, reduce]);

  // Tick boundary pulses around progress 0.42 -> 0.55.
  const boundaryOpacity = useTransform(
    active,
    [0.35, 0.45, 0.55, 0.65],
    [0.15, 0.95, 0.95, 0.3],
  );

  // Merged arrow stroke pathLength 0 -> 1 during 0.5 -> 0.7.
  const mergedDraw = useTransform(active, [0.5, 0.72], [0, 1], { clamp: true });

  // DB node pulse around 0.7 -> 0.78.
  const dbScale = useTransform(active, [0.65, 0.74, 0.85], [1, 1.18, 1]);

  return (
    <div ref={ref} className="relative">
      <div className="grid grid-cols-1 gap-8 lg:grid-cols-12 lg:gap-10">
        <div className="lg:col-span-4">
          <Eyebrow>Centerpiece</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2 mt-4">
            Watch six calls collapse on one tick.
          </h2>
          <p className="text-cc-ink mt-4">
            Resolvers fire LoadAsync from their own corners of the request. They
            queue up. On the next tick boundary, the loader merges every key
            into one batched fetch. The database does a single round trip, and
            the keyed result map fans back out to the callers.
          </p>
          <p className="text-cc-ink-dim mt-4 text-sm">
            Scroll the page to step through the phases: fan-in, coalesce,
            batched fetch, fan-out. The counter on the right tracks round trips,
            6 down to 1.
          </p>

          <div className="border-cc-card-border bg-cc-surface mt-6 inline-flex items-center gap-4 rounded-lg border px-4 py-3">
            <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
              round trips
            </span>
            <TickCounter inView={inView} reduce={reduce} />
          </div>
        </div>

        <div className="lg:col-span-8">
          <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border shadow-2xl shadow-black/30">
            <div className="border-cc-card-border flex items-center justify-between border-b px-5 py-3">
              <div className="flex items-center gap-3 font-mono text-[11px] tracking-[0.18em] uppercase">
                <span className="text-cc-accent">Fan-in</span>
                <span className="text-cc-ink-dim">/</span>
                <span className="text-cc-ink-dim">Coalesce</span>
                <span className="text-cc-ink-dim">/</span>
                <span className="text-cc-ink-dim">Batched fetch</span>
                <span className="text-cc-ink-dim">/</span>
                <span className="text-cc-ink-dim">Fan-out</span>
              </div>
              <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
                t = 1 tick
              </span>
            </div>

            <div className="px-4 py-6">
              <svg
                viewBox="0 0 820 260"
                className="w-full"
                role="img"
                aria-label="Tick collapse timeline: six lanes merge on a tick boundary into one batched fetch and a keyed fan-out"
              >
                <defs>
                  <linearGradient id="gd-merged" x1="0" y1="0" x2="1" y2="0">
                    <stop offset="0%" stopColor={ACCENT} stopOpacity="0.4" />
                    <stop offset="100%" stopColor={ACCENT} stopOpacity="1" />
                  </linearGradient>
                </defs>

                {/* Lane rails */}
                {LANES.map((lane) => (
                  <line
                    key={`rail-${lane.key}`}
                    x1="80"
                    y1={lane.y}
                    x2={BOUNDARY_X}
                    y2={lane.y}
                    stroke="currentColor"
                    className="text-cc-card-border"
                    strokeWidth="1"
                    strokeDasharray="2 3"
                  />
                ))}

                {/* Lane labels */}
                {LANES.map((lane, i) => (
                  <g key={`label-${lane.key}`}>
                    <text
                      x="14"
                      y={lane.y + 4}
                      fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
                      fontSize="11"
                      fill="currentColor"
                      className="text-cc-ink-dim"
                    >
                      lane {i + 1}
                    </text>
                    <circle cx="74" cy={lane.y} r="3" fill={ACCENT} />
                  </g>
                ))}

                {/* Animated lane arrow heads riding toward the boundary */}
                {LANES.map((lane, i) => (
                  <LaneArrow
                    key={`arrow-${lane.key}`}
                    lane={lane}
                    index={i}
                    active={active}
                    reduce={reduce}
                  />
                ))}

                {/* Tick boundary */}
                <motion.line
                  x1={BOUNDARY_X}
                  x2={BOUNDARY_X}
                  y1="8"
                  y2="252"
                  stroke={ACCENT}
                  strokeWidth="1.25"
                  strokeDasharray="3 4"
                  style={{ opacity: reduce ? 0.5 : boundaryOpacity }}
                />
                <text
                  x={BOUNDARY_X}
                  y="20"
                  textAnchor="middle"
                  fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
                  fontSize="10"
                  fill={ACCENT}
                  opacity="0.85"
                >
                  tick boundary
                </text>

                {/* Merged arrow toward DB */}
                <motion.path
                  d={`M${BOUNDARY_X + 4},128 C ${BOUNDARY_X + 80},128 ${DB_X - 80},128 ${DB_X - 12},128`}
                  fill="none"
                  stroke="url(#gd-merged)"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                  style={{ pathLength: reduce ? 1 : mergedDraw }}
                />
                <text
                  x={(BOUNDARY_X + DB_X) / 2}
                  y={118}
                  textAnchor="middle"
                  fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
                  fontSize="11"
                  fill={ACCENT}
                >
                  IN (7, 12, 3, 9, 21, 4)
                </text>

                {/* DB node */}
                <motion.g
                  style={{
                    transformBox: "fill-box",
                    transformOrigin: "center",
                    scale: reduce ? 1 : dbScale,
                  }}
                >
                  <rect
                    x={DB_X - 4}
                    y={DB_Y - 24}
                    width="56"
                    height="48"
                    rx="6"
                    fill={ACCENT}
                    fillOpacity="0.14"
                    stroke={ACCENT}
                    strokeOpacity="0.7"
                  />
                  <text
                    x={DB_X + 24}
                    y={DB_Y + 6}
                    textAnchor="middle"
                    fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
                    fontSize="11"
                    fill={ACCENT}
                  >
                    db
                  </text>
                </motion.g>

                {/* Fan-out arrows to each lane */}
                {LANES.map((lane, i) => (
                  <FanOutArrow
                    key={`fanout-${lane.key}`}
                    lane={lane}
                    index={i}
                    active={active}
                    reduce={reduce}
                  />
                ))}
              </svg>
            </div>

            <div className="border-cc-card-border grid grid-cols-4 divide-x divide-[var(--color-cc-card-border)] border-t text-center">
              <PhaseCell label="Fan-in" detail="lanes 1..6" />
              <PhaseCell label="Coalesce" detail="on tick" />
              <PhaseCell label="Batched fetch" detail="1 round trip" />
              <PhaseCell label="Fan-out" detail="keyed result map" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

interface PhaseCellProps {
  readonly label: string;
  readonly detail: string;
}

function PhaseCell({ label, detail }: PhaseCellProps) {
  return (
    <div className="px-3 py-3">
      <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
        {label}
      </div>
      <div className="text-cc-heading mt-1 font-mono text-[12px] font-semibold">
        {detail}
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Problem strip: 21 SELECTs draw in, then collapse into 2 statements. The
// counts tick up via a motion number on view.
// -----------------------------------------------------------------------------

interface CountUpProps {
  readonly to: number;
  readonly inView: boolean;
  readonly reduce: boolean;
  readonly className?: string;
}

function CountUp({ to, inView, reduce, className }: CountUpProps) {
  const [display, setDisplay] = useState(reduce ? to : 0);

  useEffect(() => {
    if (reduce || !inView) {
      return;
    }
    const start = performance.now();
    const duration = 700;
    let raf = 0;
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / duration);
      const v = Math.round(t * to);
      setDisplay(v);
      if (t < 1) {
        raf = requestAnimationFrame(tick);
      }
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [inView, reduce, to]);

  return <span className={className}>{display}</span>;
}

function ProblemStrip() {
  const reduce = useReducedMotion() ?? false;
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.35 });

  const beforeLines: readonly string[] = [
    "SELECT * FROM orders LIMIT 20",
    "SELECT * FROM customers WHERE id = 1",
    "SELECT * FROM customers WHERE id = 2",
    "SELECT * FROM customers WHERE id = 3",
    "SELECT * FROM customers WHERE id = 4",
    "SELECT * FROM customers WHERE id = 5",
    "SELECT * FROM customers WHERE id = 6",
    "SELECT * FROM customers WHERE id = 7",
    "SELECT * FROM customers WHERE id = 8",
    "SELECT * FROM customers WHERE id = 9",
    "SELECT * FROM customers WHERE id = 10",
    ". . . 11 more",
  ];

  const afterLines: readonly string[] = [
    "SELECT * FROM orders LIMIT 20",
    "SELECT * FROM customers WHERE id IN (1..20)",
  ];

  return (
    <div
      ref={ref}
      className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12"
    >
      <div className="lg:col-span-5">
        <Eyebrow>The N+1 problem</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mt-4">
          One query for the list. N more for every row.
        </h2>
        <p className="text-cc-ink mt-4">
          Resolvers run per field, per node. A list of orders loads in one
          query. Then each order asks for its customer, its line items, its
          shipping address. The database does the same point lookup, again and
          again, for a single request.
        </p>
        <p className="text-cc-ink-dim mt-4">
          Green Donut collects the keys that arrive on the same tick, sends a
          single batched fetch, and hands each resolver its result.
        </p>
      </div>

      <div className="lg:col-span-7">
        <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="border-cc-card-border bg-cc-bg/50 rounded-lg border p-4">
              <div className="text-cc-danger font-mono text-[11px] tracking-[0.18em] uppercase">
                Without DataLoader
              </div>
              <div className="mt-3 space-y-1.5 font-mono text-[12px]">
                {beforeLines.map((line, i) => (
                  <motion.div
                    key={`b-${i}`}
                    initial={reduce ? false : { opacity: 0, x: -4 }}
                    animate={
                      inView || reduce
                        ? { opacity: i === 0 ? 1 : 0.78, x: 0 }
                        : undefined
                    }
                    transition={{ duration: 0.3, delay: reduce ? 0 : i * 0.04 }}
                    className={i === 0 ? "text-cc-ink" : "text-cc-ink-dim"}
                  >
                    {line}
                  </motion.div>
                ))}
              </div>
              <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
                <span className="text-cc-ink-dim font-mono text-[11px]">
                  queries
                </span>
                <CountUp
                  to={21}
                  inView={inView}
                  reduce={reduce}
                  className="text-cc-danger font-mono text-[13px] font-semibold tabular-nums"
                />
              </div>
            </div>

            <div className="border-cc-card-border bg-cc-bg/50 rounded-lg border p-4">
              <div className="text-cc-accent font-mono text-[11px] tracking-[0.18em] uppercase">
                With Green Donut
              </div>
              <div className="mt-3 space-y-1.5 font-mono text-[12px]">
                {afterLines.map((line, i) => (
                  <motion.div
                    key={`a-${i}`}
                    initial={reduce ? false : { opacity: 0, x: 4 }}
                    animate={
                      inView || reduce ? { opacity: 1, x: 0 } : undefined
                    }
                    transition={{
                      duration: 0.4,
                      delay: reduce ? 0 : 0.6 + i * 0.12,
                    }}
                    className="text-cc-ink"
                  >
                    {line}
                  </motion.div>
                ))}
              </div>
              <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
                <span className="text-cc-ink-dim font-mono text-[11px]">
                  queries
                </span>
                <CountUp
                  to={2}
                  inView={inView}
                  reduce={reduce}
                  className="text-cc-accent font-mono text-[13px] font-semibold tabular-nums"
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Behaviors trio: Batch / Cache / Dedup. Each a small motion vignette revealed
// whileInView with staggered children.
// -----------------------------------------------------------------------------

const TRIO_CONTAINER: Variants = {
  hidden: {},
  show: { transition: { staggerChildren: 0.12 } },
};

const TRIO_CHILD: Variants = {
  hidden: { opacity: 0, y: 10 },
  show: { opacity: 1, y: 0, transition: { duration: 0.45, ease: "easeOut" } },
};

function BatchVignette() {
  return (
    <svg viewBox="0 0 220 70" className="w-full" aria-hidden="true">
      {[14, 28, 42, 56].map((y, i) => (
        <motion.circle
          key={y}
          cx="14"
          cy={y}
          r="3"
          fill={ACCENT}
          initial={{ x: 0 }}
          whileInView={{ x: 170 }}
          viewport={{ once: true, amount: 0.6 }}
          transition={{ duration: 0.7, delay: 0.1 + i * 0.06, ease: "easeOut" }}
        />
      ))}
      <rect
        x="190"
        y="22"
        width="20"
        height="28"
        rx="4"
        fill={ACCENT}
        fillOpacity="0.18"
        stroke={ACCENT}
        strokeOpacity="0.7"
      />
    </svg>
  );
}

function CacheVignette() {
  return (
    <svg viewBox="0 0 220 70" className="w-full" aria-hidden="true">
      <rect
        x="80"
        y="20"
        width="60"
        height="30"
        rx="4"
        fill={ACCENT}
        fillOpacity="0.1"
        stroke={ACCENT}
        strokeOpacity="0.6"
      />
      <text
        x="110"
        y="40"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill={ACCENT}
      >
        cache
      </text>
      <motion.circle
        cx="20"
        cy="35"
        r="4"
        fill={ACCENT}
        initial={{ x: 0, opacity: 1 }}
        whileInView={{ x: 60, opacity: [1, 1, 0] }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 0.9, ease: "easeOut" }}
      />
      <motion.circle
        cx="20"
        cy="35"
        r="4"
        fill={ACCENT}
        initial={{ opacity: 0 }}
        whileInView={{ opacity: [0, 1] }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 0.4, delay: 1.0 }}
      />
      <motion.path
        d="M85,35 C 110,35 120,35 140,35"
        stroke={ACCENT}
        strokeWidth="1.25"
        strokeLinecap="round"
        fill="none"
        initial={{ pathLength: 0, opacity: 0 }}
        whileInView={{ pathLength: 1, opacity: 0.7 }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 0.6, delay: 1.1 }}
      />
      <motion.circle
        cx="200"
        cy="35"
        r="3.5"
        fill={ACCENT}
        initial={{ opacity: 0 }}
        whileInView={{ opacity: 1 }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 0.3, delay: 1.6 }}
      />
    </svg>
  );
}

function DedupVignette() {
  return (
    <svg viewBox="0 0 220 70" className="w-full" aria-hidden="true">
      {[18, 36, 54].map((y, i) => (
        <g key={y}>
          <motion.circle
            cx="14"
            cy={y}
            r="3"
            fill={ACCENT}
            initial={{ x: 0 }}
            whileInView={{ x: 170 }}
            viewport={{ once: true, amount: 0.6 }}
            transition={{
              duration: 0.7,
              delay: 0.1 + i * 0.08,
              ease: "easeOut",
            }}
          />
          <motion.path
            d={`M18,${y} L 180,${y}`}
            stroke={ACCENT}
            strokeWidth="1"
            strokeOpacity="0.45"
            initial={{ pathLength: 0 }}
            whileInView={{ pathLength: 1 }}
            viewport={{ once: true, amount: 0.6 }}
            transition={{ duration: 0.6, delay: 0.1 + i * 0.08 }}
            fill="none"
          />
          {i === 1 ? (
            <motion.line
              x1="40"
              y1={y - 6}
              x2="80"
              y2={y + 6}
              stroke="currentColor"
              className="text-cc-danger"
              strokeWidth="1.5"
              initial={{ opacity: 0 }}
              whileInView={{ opacity: 0.85 }}
              viewport={{ once: true, amount: 0.6 }}
              transition={{ duration: 0.3, delay: 0.5 }}
            />
          ) : null}
        </g>
      ))}
      <rect
        x="190"
        y="18"
        width="20"
        height="40"
        rx="4"
        fill={ACCENT}
        fillOpacity="0.18"
        stroke={ACCENT}
        strokeOpacity="0.7"
      />
    </svg>
  );
}

interface BehaviorCardProps {
  readonly index: string;
  readonly title: string;
  readonly body: string;
  readonly children: ReactNode;
}

function BehaviorCard({ index, title, body, children }: BehaviorCardProps) {
  return (
    <motion.article
      variants={TRIO_CHILD}
      className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full flex-col rounded-xl border p-6 transition-colors"
    >
      <div className="flex items-center justify-between">
        <IndexTag value={index} />
        <span
          className="bg-cc-accent/70 h-1.5 w-1.5 rounded-full"
          aria-hidden
        />
      </div>
      <h3 className="text-cc-heading font-heading text-h5 mt-4">{title}</h3>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{body}</p>
      <div className="mt-6">{children}</div>
    </motion.article>
  );
}

function BehaviorsTrio() {
  return (
    <section className="mt-24">
      <div className="flex flex-col items-start gap-4">
        <Eyebrow>Three behaviors, one loader</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 max-w-3xl">
          Batch many keys. Cache each result. Skip repeats entirely.
        </h2>
      </div>

      <motion.div
        variants={TRIO_CONTAINER}
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.3 }}
        className="mt-10 grid grid-cols-1 gap-5 md:grid-cols-3"
      >
        <BehaviorCard
          index="01"
          title="Batch"
          body="Sibling resolver calls on the same tick collapse into one fetch. Results return in the original key order."
        >
          <BatchVignette />
        </BehaviorCard>
        <BehaviorCard
          index="02"
          title="Cache"
          body="The default scope is the request. A second LoadAsync for the same key is served from the in-memory cache, no fetch."
        >
          <CacheVignette />
        </BehaviorCard>
        <BehaviorCard
          index="03"
          title="Dedup"
          body="A duplicate key inside the same batch is collapsed before the fetch leaves. Same key, same task, one result."
        >
          <DedupVignette />
        </BehaviorCard>
      </motion.div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// [DataLoader] attribute spotlight: animated generated-wiring list on the left,
// static code card on the right.
// -----------------------------------------------------------------------------

const C = {
  kw: { color: "#ff7b72" } as CSSProperties,
  type: { color: "#ffa657" } as CSSProperties,
  str: { color: "#a5d6ff" } as CSSProperties,
  comment: { color: "#8b949e", fontStyle: "italic" as const } as CSSProperties,
  attr: { color: "#d2a8ff" } as CSSProperties,
  fn: { color: "#d2a8ff" } as CSSProperties,
  param: { color: "#79c0ff" } as CSSProperties,
  plain: { color: "#c9d1d9" } as CSSProperties,
};

interface CodeLineProps {
  readonly n: number;
  readonly children: ReactNode;
}

function CodeLine({ n, children }: CodeLineProps) {
  return (
    <div className="flex gap-4 px-5">
      <span
        className="w-6 shrink-0 text-right font-mono text-[11px] text-[#484f58] tabular-nums select-none"
        aria-hidden
      >
        {n}
      </span>
      <span className="font-mono text-[12.5px] leading-6 whitespace-pre">
        {children}
      </span>
    </div>
  );
}

function DataLoaderCodeCard() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-3">
        <span
          className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span
          className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span
          className="bg-cc-success/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span className="ml-3 font-mono text-[11px] text-[#8b949e]">
          UserDataLoader.cs
        </span>
      </div>

      <div className="py-4">
        <CodeLine n={1}>
          <span style={C.comment}>{"// One method. Wiring is generated."}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.kw}>public static class</span>{" "}
          <span style={C.type}>UserDataLoader</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{"{"}</span>
        </CodeLine>
        <CodeLine n={4}>
          {"    "}
          <span style={C.attr}>[DataLoader]</span>
        </CodeLine>
        <CodeLine n={5}>
          {"    "}
          <span style={C.kw}>public static async</span>{" "}
          <span style={C.type}>{"Task<IReadOnlyDictionary<int, User>>"}</span>{" "}
          <span style={C.fn}>GetUsersAsync</span>
          <span style={C.plain}>(</span>
        </CodeLine>
        <CodeLine n={6}>
          {"        "}
          <span style={C.type}>{"IReadOnlyList<int>"}</span>{" "}
          <span style={C.param}>ids</span>
          <span style={C.plain}>,</span>
        </CodeLine>
        <CodeLine n={7}>
          {"        "}
          <span style={C.type}>AppDbContext</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.plain}>,</span>
        </CodeLine>
        <CodeLine n={8}>
          {"        "}
          <span style={C.type}>CancellationToken</span>{" "}
          <span style={C.param}>ct</span>
          <span style={C.plain}>{")"}</span>
        </CodeLine>
        <CodeLine n={9}>
          {"        "}
          <span style={C.plain}>{"=>"}</span> <span style={C.kw}>await</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.plain}>.Users</span>
        </CodeLine>
        <CodeLine n={10}>
          {"            "}
          <span style={C.plain}>.</span>
          <span style={C.fn}>Where</span>
          <span style={C.plain}>(u {"=>"} </span>
          <span style={C.param}>ids</span>
          <span style={C.plain}>.</span>
          <span style={C.fn}>Contains</span>
          <span style={C.plain}>(u.Id))</span>
        </CodeLine>
        <CodeLine n={11}>
          {"            "}
          <span style={C.plain}>.</span>
          <span style={C.fn}>ToDictionaryAsync</span>
          <span style={C.plain}>(u {"=>"} u.Id, </span>
          <span style={C.param}>ct</span>
          <span style={C.plain}>);</span>
        </CodeLine>
        <CodeLine n={12}>
          <span style={C.plain}>{"}"}</span>
        </CodeLine>
      </div>
    </div>
  );
}

interface GeneratedLineProps {
  readonly index: number;
  readonly label: string;
  readonly value: string;
}

function GeneratedLine({ index, label, value }: GeneratedLineProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 6 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.5 }}
      transition={{
        duration: 0.45,
        delay: 0.15 + index * 0.18,
        ease: "easeOut",
      }}
      className="border-cc-card-border bg-cc-card-bg flex items-center justify-between rounded-lg border px-4 py-3"
    >
      <div className="flex items-center gap-3">
        <IndexTag value={String(index + 1).padStart(2, "0")} />
        <span className="text-cc-heading font-mono text-[12.5px]">{label}</span>
      </div>
      <div className="flex items-center gap-2 font-mono text-[12px]">
        <span className="text-cc-ink-dim">{value}</span>
        <motion.span
          initial={{ opacity: 0 }}
          whileInView={{ opacity: [0, 1, 0, 1, 0] }}
          viewport={{ once: true, amount: 0.5 }}
          transition={{
            duration: 0.9,
            delay: 0.25 + index * 0.18,
            times: [0, 0.25, 0.5, 0.75, 1],
          }}
          className="text-cc-accent"
        >
          |
        </motion.span>
      </div>
    </motion.div>
  );
}

function AttributeSpotlight() {
  return (
    <section className="mt-24">
      <div className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-5">
          <Eyebrow>The [DataLoader] attribute</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2 mt-4">
            Write the method. Skip the plumbing.
          </h2>
          <p className="text-cc-ink mt-4">
            Mark a static method with{" "}
            <span className="text-cc-heading font-mono text-[13px]">
              [DataLoader]
            </span>
            . Take an{" "}
            <span className="text-cc-heading font-mono text-[13px]">
              IReadOnlyList&lt;TKey&gt;
            </span>{" "}
            and return an{" "}
            <span className="text-cc-heading font-mono text-[13px]">
              IReadOnlyDictionary&lt;TKey, TValue&gt;
            </span>
            . The source generator emits the rest.
          </p>

          <div className="mt-6 space-y-3">
            <div className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
              Generated at build time
            </div>
            <GeneratedLine
              index={0}
              label="Interface"
              value="IUserDataLoader"
            />
            <GeneratedLine
              index={1}
              label="Registration"
              value="services.AddDataLoader(...)"
            />
            <GeneratedLine
              index={2}
              label="Accessor"
              value="LoadAsync(id, ct)"
            />
          </div>

          <ul className="mt-8 space-y-3">
            <li className="text-cc-ink-dim flex items-start gap-3 text-sm">
              <span className="text-cc-accent mt-[3px]">
                <CheckIcon />
              </span>
              <span>
                Dependencies after the keys are resolved from DI per batch.
              </span>
            </li>
            <li className="text-cc-ink-dim flex items-start gap-3 text-sm">
              <span className="text-cc-accent mt-[3px]">
                <CheckIcon />
              </span>
              <span>
                Returns are looked up by key. Missing keys yield null.
              </span>
            </li>
            <li className="text-cc-ink-dim flex items-start gap-3 text-sm">
              <span className="text-cc-accent mt-[3px]">
                <CheckIcon />
              </span>
              <span>
                Cancellation flows through to the database driver, not just the
                loader.
              </span>
            </li>
          </ul>
        </div>

        <div className="lg:col-span-7">
          <DataLoaderCodeCard />
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Loader shapes table: each row's return shape gets a sliding accent bar
// underline on hover.
// -----------------------------------------------------------------------------

interface ShapeRowProps {
  readonly name: string;
  readonly shape: string;
  readonly use: string;
}

function ShapeRow({ name, shape, use }: ShapeRowProps) {
  return (
    <motion.div
      initial="rest"
      whileHover="hover"
      animate="rest"
      className="border-cc-card-border grid grid-cols-12 gap-3 border-b px-5 py-4 last:border-b-0"
    >
      <div className="col-span-12 sm:col-span-3">
        <span className="text-cc-heading font-mono text-[12.5px] font-semibold">
          {name}
        </span>
      </div>
      <div className="col-span-12 sm:col-span-4">
        <span className="text-cc-ink font-mono text-[12px]">{shape}</span>
        <motion.div
          className="bg-cc-accent mt-1 h-[2px] origin-left rounded-full"
          variants={{
            rest: { scaleX: 0 },
            hover: { scaleX: 1 },
          }}
          transition={{ duration: 0.35, ease: "easeOut" }}
        />
      </div>
      <div className="col-span-12 sm:col-span-5">
        <span className="text-cc-ink-dim text-sm">{use}</span>
      </div>
    </motion.div>
  );
}

function LoaderShapes() {
  return (
    <section className="mt-24">
      <div className="flex flex-col items-start gap-3">
        <Eyebrow>Loader shapes</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2">
          One attribute, three signatures.
        </h2>
        <p className="text-cc-ink max-w-3xl">
          The signature picks the loader. Same attribute, same generated wiring,
          the right shape for the relationship you are loading.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg mt-8 overflow-hidden rounded-xl border">
        <div className="border-cc-card-border bg-cc-surface/60 text-cc-ink-dim grid grid-cols-12 gap-3 border-b px-5 py-3 font-mono text-[11px] tracking-[0.18em] uppercase">
          <div className="col-span-12 sm:col-span-3">Loader</div>
          <div className="col-span-12 sm:col-span-4">Return shape</div>
          <div className="col-span-12 sm:col-span-5">When to reach for it</div>
        </div>
        <ShapeRow
          name="Keyed"
          shape="IReadOnlyDictionary<TKey, TValue>"
          use="One result per key. The default for foreign key lookups."
        />
        <ShapeRow
          name="Grouped"
          shape="ILookup<TKey, TValue>"
          use="Many results per key. One to many relationships, like an order to its line items."
        />
        <ShapeRow
          name="Pagination"
          shape="Page<TKey, TValue>"
          use="A cursor window per key. Connections on a parent type that need paging per node."
        />
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Hot Chocolate integration: code card on one side, an auto-discovery graph
// on the other where loader nodes pulse and snap into a DI container box.
// -----------------------------------------------------------------------------

function AutoDiscoveryGraph() {
  const ref = useRef<SVGSVGElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  const reduce = useReducedMotion() ?? false;

  const loaders: readonly { readonly label: string; readonly y: number }[] = [
    { label: "UserDataLoader", y: 32 },
    { label: "OrderDataLoader", y: 92 },
    { label: "ProductDataLoader", y: 152 },
  ];

  return (
    <svg
      ref={ref}
      viewBox="0 0 360 200"
      className="w-full"
      role="img"
      aria-label="Auto-discovered loaders snap into the DI container at startup"
    >
      {loaders.map((node, i) => (
        <g key={node.label}>
          <motion.rect
            x="10"
            y={node.y - 14}
            width="140"
            height="28"
            rx="6"
            fill={ACCENT}
            fillOpacity="0.1"
            stroke={ACCENT}
            strokeOpacity="0.55"
            initial={reduce ? false : { opacity: 0 }}
            animate={inView || reduce ? { opacity: 1 } : undefined}
            transition={{ duration: 0.35, delay: reduce ? 0 : 0.1 + i * 0.12 }}
          />
          <motion.text
            x="80"
            y={node.y + 4}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="11"
            fill={ACCENT}
            initial={reduce ? false : { opacity: 0 }}
            animate={inView || reduce ? { opacity: 1 } : undefined}
            transition={{ duration: 0.35, delay: reduce ? 0 : 0.15 + i * 0.12 }}
          >
            {node.label}
          </motion.text>
          <motion.path
            d={`M152,${node.y} C 200,${node.y} 230,100 270,100`}
            fill="none"
            stroke={ACCENT}
            strokeOpacity="0.55"
            strokeWidth="1.25"
            strokeLinecap="round"
            initial={reduce ? false : { pathLength: 0 }}
            animate={inView || reduce ? { pathLength: 1 } : undefined}
            transition={{
              duration: 0.6,
              delay: reduce ? 0 : 0.5 + i * 0.15,
              ease: "easeOut",
            }}
          />
          <motion.circle
            cx="152"
            cy={node.y}
            r="3"
            fill={ACCENT}
            initial={reduce ? false : { scale: 0 }}
            animate={inView || reduce ? { scale: [0, 1.3, 1] } : undefined}
            transition={{
              duration: 0.5,
              delay: reduce ? 0 : 0.4 + i * 0.15,
            }}
          />
        </g>
      ))}

      <motion.rect
        x="270"
        y="76"
        width="80"
        height="48"
        rx="8"
        fill={ACCENT}
        fillOpacity="0.16"
        stroke={ACCENT}
        strokeOpacity="0.75"
        initial={reduce ? false : { opacity: 0, scale: 0.9 }}
        animate={inView || reduce ? { opacity: 1, scale: 1 } : undefined}
        style={{ transformBox: "fill-box", transformOrigin: "center" }}
        transition={{ duration: 0.4, delay: reduce ? 0 : 0.2 }}
      />
      <text
        x="310"
        y="105"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="12"
        fill={ACCENT}
      >
        DI container
      </text>
    </svg>
  );
}

function HotChocolateIntegration() {
  return (
    <section className="mt-24">
      <div className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-7">
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6">
            <div className="flex items-center justify-between">
              <Eyebrow>In a Hot Chocolate resolver</Eyebrow>
              <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
                auto discovered
              </span>
            </div>

            <div className="bg-cc-code-bg border-cc-card-border mt-4 overflow-hidden rounded-lg border py-4">
              <CodeLine n={1}>
                <span style={C.kw}>public class</span>{" "}
                <span style={C.type}>OrderType</span>{" "}
                <span style={C.plain}>:</span>{" "}
                <span style={C.type}>{"ObjectType<Order>"}</span>
              </CodeLine>
              <CodeLine n={2}>
                <span style={C.plain}>{"{"}</span>
              </CodeLine>
              <CodeLine n={3}>
                {"    "}
                <span style={C.kw}>public static async</span>{" "}
                <span style={C.type}>{"Task<User?>"}</span>{" "}
                <span style={C.fn}>GetCustomerAsync</span>
                <span style={C.plain}>(</span>
              </CodeLine>
              <CodeLine n={4}>
                {"        "}
                <span style={C.attr}>[Parent]</span>{" "}
                <span style={C.type}>Order</span>{" "}
                <span style={C.param}>order</span>
                <span style={C.plain}>,</span>
              </CodeLine>
              <CodeLine n={5}>
                {"        "}
                <span style={C.type}>IUserDataLoader</span>{" "}
                <span style={C.param}>users</span>
                <span style={C.plain}>,</span>
              </CodeLine>
              <CodeLine n={6}>
                {"        "}
                <span style={C.type}>CancellationToken</span>{" "}
                <span style={C.param}>ct</span>
                <span style={C.plain}>{")"}</span>
              </CodeLine>
              <CodeLine n={7}>
                {"        "}
                <span style={C.plain}>{"=>"}</span>{" "}
                <span style={C.kw}>await</span>{" "}
                <span style={C.param}>users</span>
                <span style={C.plain}>.</span>
                <span style={C.fn}>LoadAsync</span>
                <span style={C.plain}>(</span>
                <span style={C.param}>order</span>
                <span style={C.plain}>.CustomerId, </span>
                <span style={C.param}>ct</span>
                <span style={C.plain}>);</span>
              </CodeLine>
              <CodeLine n={8}>
                <span style={C.plain}>{"}"}</span>
              </CodeLine>
            </div>

            <p className="text-cc-ink-dim mt-4 text-sm">
              Hot Chocolate discovers loaders marked with{" "}
              <span className="text-cc-heading font-mono text-[12.5px]">
                [DataLoader]
              </span>{" "}
              during startup and registers them in DI. Resolvers just inject the
              generated interface.
            </p>
          </div>
        </div>

        <div className="lg:col-span-5">
          <Eyebrow>Inside Hot Chocolate, standalone outside</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2 mt-4">
            Same loader. Different host.
          </h2>
          <p className="text-cc-ink mt-4">
            Green Donut is the engine Hot Chocolate uses to batch resolver work
            and eliminate N+1, and you do not need a GraphQL server to use it.
            Drop a loader in a Worker, a controller, or a console app.
          </p>

          <div className="border-cc-card-border bg-cc-card-bg mt-6 rounded-xl border p-5">
            <div className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
              Auto discovery
            </div>
            <div className="mt-3">
              <AutoDiscoveryGraph />
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// MIT band with a single-pass sheen on the spectrum border.
// -----------------------------------------------------------------------------

function MitBand() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  const reduce = useReducedMotion() ?? false;

  return (
    <section ref={ref} className="mt-24">
      <div
        className="relative overflow-hidden rounded-xl p-[1px]"
        style={{ background: SPECTRUM }}
      >
        <motion.div
          aria-hidden
          className="pointer-events-none absolute inset-y-0 w-1/3"
          style={{
            background:
              "linear-gradient(90deg, transparent 0%, rgba(255,255,255,0.18) 50%, transparent 100%)",
          }}
          initial={reduce ? false : { x: "-120%" }}
          animate={inView || reduce ? { x: "320%" } : undefined}
          transition={{ duration: 1.6, ease: "easeOut", delay: 0.2 }}
        />
        <div className="bg-cc-surface relative flex flex-col items-start justify-between gap-6 rounded-[11px] px-8 py-8 sm:flex-row sm:items-center">
          <div>
            <Eyebrow>Open source</Eyebrow>
            <div className="text-cc-heading font-heading text-h4 mt-2">
              MIT licensed. Maintained in the open.
            </div>
            <p className="text-cc-ink-dim mt-2 max-w-2xl text-sm">
              Green Donut ships in the same repository as Hot Chocolate, Fusion,
              Strawberry Shake, and Cookie Crumble. No per request fee, no per
              seat fee, no commercial fork.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
            <SolidButton href="/docs/greendonut">Read the docs</SolidButton>
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Closing CTA with one final 6 -> 1 tick of the round-trips digit.
// -----------------------------------------------------------------------------

function ClosingTick() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  const reduce = useReducedMotion() ?? false;

  const [n, setN] = useState(reduce ? 1 : 6);
  useEffect(() => {
    if (reduce || !inView) {
      return;
    }
    let i = 6;
    const id = setInterval(() => {
      i = i - 1;
      if (i <= 1) {
        setN(1);
        clearInterval(id);
        return;
      }
      setN(i);
    }, 140);
    return () => clearInterval(id);
  }, [inView, reduce]);

  return (
    <section ref={ref} className="mt-24 text-center">
      <Eyebrow>Stop paying for N+1</Eyebrow>
      <h2 className="font-heading text-cc-heading text-h2 mx-auto mt-4 max-w-3xl">
        Six round trips. One batched fetch.
      </h2>

      <div className="mt-8 inline-flex items-center gap-5 rounded-full border border-[var(--color-cc-card-border)] bg-[var(--color-cc-surface)] px-6 py-3">
        <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
          round trips
        </span>
        <span className="text-cc-accent font-mono text-[28px] font-semibold tabular-nums">
          {n}
        </span>
      </div>

      <p className="text-cc-ink mx-auto mt-6 max-w-2xl">
        Add Green Donut to your service, mark a method with{" "}
        <span className="text-cc-heading font-mono text-[13px]">
          [DataLoader]
        </span>
        , and inject the generated interface. The N+1 disappears on the next
        request.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/docs/greendonut">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      <main className="mx-auto w-full max-w-7xl px-6 pt-16 pb-24 sm:px-8 lg:px-10">
        {/* HERO */}
        <section className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-6">
            <div className="flex items-center gap-3">
              <GreenDonut className="h-9 w-9" />
              <Eyebrow>Green Donut / DataLoader for .NET</Eyebrow>
            </div>

            <h1 className="font-heading text-cc-heading text-h1 mt-6">
              Kill N+1 in your .NET resolvers.
            </h1>

            <p className="lead text-cc-ink mt-5 max-w-xl">
              Green Donut is the DataLoader for .NET. It collapses many key
              requests from the same tick into one batched fetch, caches each
              result inside the request, and deduplicates repeat keys. Source
              generated, AOT friendly, MIT licensed.
            </p>

            <div className="mt-8 flex flex-wrap items-center gap-3">
              <SolidButton href="/docs/greendonut">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>

            <ul className="text-cc-ink-dim mt-8 grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
              <li className="flex items-center gap-2">
                <span className="text-cc-accent">
                  <CheckIcon />
                </span>
                Batching, caching, dedup
              </li>
              <li className="flex items-center gap-2">
                <span className="text-cc-accent">
                  <CheckIcon />
                </span>
                [DataLoader] attribute
              </li>
              <li className="flex items-center gap-2">
                <span className="text-cc-accent">
                  <CheckIcon />
                </span>
                Per request scoped cache
              </li>
              <li className="flex items-center gap-2">
                <span className="text-cc-accent">
                  <CheckIcon />
                </span>
                Auto discovered by Hot Chocolate
              </li>
            </ul>
          </div>

          <div className="lg:col-span-6">
            <HeroPreview />
          </div>
        </section>

        {/* CENTERPIECE */}
        <section className="mt-28">
          <TickCollapse />
        </section>

        {/* PROBLEM */}
        <section className="mt-24">
          <ProblemStrip />
        </section>

        {/* BEHAVIORS TRIO */}
        <BehaviorsTrio />

        {/* ATTRIBUTE SPOTLIGHT */}
        <AttributeSpotlight />

        {/* LOADER SHAPES */}
        <LoaderShapes />

        {/* HOT CHOCOLATE INTEGRATION */}
        <HotChocolateIntegration />

        {/* MIT BAND */}
        <MitBand />

        {/* CLOSING TICK */}
        <ClosingTick />
      </main>
    </MotionConfig>
  );
}
