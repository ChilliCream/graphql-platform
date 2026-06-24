"use client";

import { useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import {
  motion,
  useInView,
  useReducedMotion,
  MotionConfig,
} from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Note: this file is a client component because the centerpiece motion theatre
// relies on useInView, useReducedMotion, and useState. The phases advance on a
// one-shot timer once the stage enters view, not on scroll position.

// Brand spectrum, allowed at most once per screen, used on the closing CTA rule.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const ACCENT = "#5eead4";

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
// Hero composition snapshot, static, but the chip and headline fade in on mount.
// -----------------------------------------------------------------------------

function HeroSnapshot() {
  const prefersReduced = useReducedMotion();
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-6 shadow-2xl sm:p-8">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 220px at 78% 50%, rgba(94, 234, 212, 0.18), transparent 70%), radial-gradient(280px 180px at 82% 56%, rgba(22, 185, 228, 0.14), transparent 70%)",
        }}
      />

      <div className="relative">
        <div className="mb-5 flex items-center justify-between gap-3">
          <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
            fusion compose
          </span>
          <motion.span
            initial={
              prefersReduced ? false : { opacity: 0, y: -4, scale: 0.96 }
            }
            animate={{ opacity: 1, y: 0, scale: 1 }}
            transition={{ delay: 0.8, duration: 0.45, ease: "easeOut" }}
            className="border-cc-accent/40 text-cc-accent bg-cc-accent/10 inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10.5px] tracking-wider uppercase"
          >
            <span className="text-cc-accent" aria-hidden>
              <CheckIcon size={11} />
            </span>
            satisfiable
          </motion.span>
        </div>

        <svg
          viewBox="0 0 520 320"
          className="h-auto w-full"
          role="img"
          aria-label="Three subgraph schemas composed into one gateway endpoint"
        >
          <defs>
            <linearGradient
              id="fusion-v7-hero-flow"
              x1="0"
              x2="1"
              y1="0"
              y2="0"
            >
              <stop offset="0%" stopColor={ACCENT} stopOpacity="0.15" />
              <stop offset="100%" stopColor={ACCENT} stopOpacity="0.9" />
            </linearGradient>
            <radialGradient id="fusion-v7-hero-node" cx="0.5" cy="0.5" r="0.6">
              <stop offset="0%" stopColor={ACCENT} stopOpacity="0.25" />
              <stop offset="100%" stopColor={ACCENT} stopOpacity="0" />
            </radialGradient>
          </defs>

          {[
            { y: 28, label: "catalog", sub: "Hot Chocolate" },
            { y: 130, label: "checkout", sub: "Federation v2" },
            { y: 232, label: "reviews", sub: "Hot Chocolate" },
          ].map((s) => (
            <g key={s.label}>
              <rect
                x="12"
                y={s.y}
                width="172"
                height="60"
                rx="8"
                fill="rgba(245,241,234,0.04)"
                stroke="rgba(245,241,234,0.16)"
              />
              <text
                x="28"
                y={s.y + 26}
                fontFamily="var(--font-body)"
                fontSize="13"
                fill="#f5f0ea"
              >
                {s.label}
              </text>
              <text
                x="28"
                y={s.y + 44}
                fontFamily="ui-monospace, monospace"
                fontSize="10.5"
                fill="rgba(245,241,234,0.62)"
              >
                {s.sub}
              </text>
              <text
                x="172"
                y={s.y + 26}
                textAnchor="end"
                fontFamily="ui-monospace, monospace"
                fontSize="10"
                fill="rgba(245,241,234,0.45)"
                dx="-8"
              >
                subgraph
              </text>
              <path
                d={`M 184 ${s.y + 30} C 260 ${s.y + 30}, 280 160, 348 160`}
                stroke="url(#fusion-v7-hero-flow)"
                strokeWidth="1.5"
                fill="none"
              />
            </g>
          ))}

          <circle cx="402" cy="160" r="74" fill="url(#fusion-v7-hero-node)" />
          <rect
            x="348"
            y="128"
            width="148"
            height="64"
            rx="10"
            fill="rgba(12,19,34,0.85)"
            stroke="rgba(94,234,212,0.6)"
          />
          <text
            x="422"
            y="152"
            textAnchor="middle"
            fontFamily="var(--font-body)"
            fontSize="13"
            fill="#f5f0ea"
          >
            Fusion gateway
          </text>
          <text
            x="422"
            y="170"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill={ACCENT}
          >
            /graphql
          </text>
          <text
            x="422"
            y="184"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9.5"
            fill="rgba(245,241,234,0.5)"
          >
            one composite schema
          </text>

          <rect
            x="364"
            y="244"
            width="116"
            height="22"
            rx="11"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="422"
            y="259"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            gateway.far (built)
          </text>
        </svg>

        <div className="border-cc-card-border text-cc-ink-dim mt-5 flex items-center justify-between border-t pt-4 font-mono text-[11px]">
          <span>3 subgraphs, 0 conflicts, 0 unreachable paths</span>
          <span className="text-cc-accent">planned at build time</span>
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// CENTERPIECE: scroll-driven Distributed Query Plan theatre.
// Four phases drive a single stage: Plan, Parallel, Batch, Compose.
// -----------------------------------------------------------------------------

const PHASES = [
  {
    key: "plan",
    label: "Plan",
    caption: "the operation lands at the gateway, the planner sweeps the graph",
  },
  {
    key: "parallel",
    label: "Parallel",
    caption: "independent fetches fan out to catalog and checkout in parallel",
  },
  {
    key: "batch",
    label: "Batch",
    caption: "dependent reviews fetch fires once, batched by productIds",
  },
  {
    key: "compose",
    label: "Compose",
    caption: "the gateway returns one composed response, satisfied",
  },
] as const;

// Geometry for the SVG stage (520 x 320 viewBox to mirror the hero).
const CLIENT_X = 60;
const CLIENT_Y = 160;
const GATEWAY_X = 260;
const GATEWAY_Y = 160;
const SUBGRAPHS: ReadonlyArray<{
  readonly key: string;
  readonly x: number;
  readonly y: number;
  readonly label: string;
}> = [
  { key: "catalog", x: 460, y: 64, label: "catalog" },
  { key: "checkout", x: 460, y: 160, label: "checkout" },
  { key: "reviews", x: 460, y: 256, label: "reviews" },
];

// Phase timing, in seconds, for the one-shot animation on enter view.
const PHASE_DURATION = 1.1;
const PHASE_GAP = 0.35;

function PhaseTheatre() {
  const containerRef = useRef<HTMLDivElement>(null);
  const inView = useInView(containerRef, { amount: 0.4, once: true });
  const prefersReduced = useReducedMotion();

  // Discrete phase index for scrubber highlight, advances on a timer once in view.
  const [phaseIndex, setPhaseIndex] = useState(0);
  useEffect(() => {
    if (!inView || prefersReduced) {
      return;
    }
    const stepMs = (PHASE_DURATION + PHASE_GAP) * 1000;
    const timers = [1, 2, 3].map((i) =>
      window.setTimeout(() => setPhaseIndex(i), stepMs * i),
    );
    return () => {
      for (const t of timers) {
        window.clearTimeout(t);
      }
    };
  }, [inView, prefersReduced]);

  // Reduced motion short-circuit: render the final frame.
  if (prefersReduced) {
    return <PhaseTheatreStatic />;
  }

  const planActive = inView;
  const parallelActive = inView && phaseIndex >= 1;
  const batchActive = inView && phaseIndex >= 2;
  const composeActive = inView && phaseIndex >= 3;

  return (
    <div ref={containerRef} className="relative">
      <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border shadow-2xl">
        <div className="border-cc-card-border flex items-center justify-between gap-3 border-b px-5 py-3 sm:px-6">
          <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
            distributed query plan
          </span>
          <motion.span
            initial={{ opacity: 0 }}
            animate={{ opacity: composeActive ? 1 : 0 }}
            transition={{ duration: 0.4, ease: "easeOut" }}
            className="border-cc-accent/40 text-cc-accent bg-cc-accent/10 inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10.5px] tracking-wider uppercase"
          >
            <span className="text-cc-accent" aria-hidden>
              <CheckIcon size={11} />
            </span>
            satisfied
          </motion.span>
        </div>

        <div className="relative">
          <svg
            viewBox="0 0 520 320"
            className="h-auto w-full"
            role="img"
            aria-label="A client operation is planned, fans out to catalog and checkout in parallel, batches reviews, then a composed response returns to the client"
          >
            <defs>
              <linearGradient id="fusion-v7-wire" x1="0" x2="1" y1="0" y2="0">
                <stop offset="0%" stopColor={ACCENT} stopOpacity="0.15" />
                <stop offset="100%" stopColor={ACCENT} stopOpacity="0.9" />
              </linearGradient>
              <radialGradient id="fusion-v7-gw-glow" cx="0.5" cy="0.5" r="0.6">
                <stop offset="0%" stopColor={ACCENT} stopOpacity="0.28" />
                <stop offset="100%" stopColor={ACCENT} stopOpacity="0" />
              </radialGradient>
            </defs>

            {/* Client node */}
            <rect
              x={CLIENT_X - 38}
              y={CLIENT_Y - 22}
              width="76"
              height="44"
              rx="8"
              fill="rgba(245,241,234,0.04)"
              stroke="rgba(245,241,234,0.22)"
            />
            <text
              x={CLIENT_X}
              y={CLIENT_Y - 2}
              textAnchor="middle"
              fontFamily="var(--font-body)"
              fontSize="12"
              fill="#f5f0ea"
            >
              client
            </text>
            <text
              x={CLIENT_X}
              y={CLIENT_Y + 14}
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="9.5"
              fill="rgba(245,241,234,0.55)"
            >
              one operation
            </text>

            {/* Wire client -> gateway, lit when op is planned. */}
            <line
              x1={CLIENT_X + 38}
              y1={CLIENT_Y}
              x2={GATEWAY_X - 56}
              y2={GATEWAY_Y}
              stroke="rgba(245,241,234,0.18)"
              strokeWidth="1.2"
            />
            <motion.line
              x1={CLIENT_X + 38}
              y1={CLIENT_Y}
              x2={GATEWAY_X - 56}
              y2={GATEWAY_Y}
              stroke={ACCENT}
              strokeWidth="2"
              strokeLinecap="round"
              initial={{ pathLength: 0, opacity: 0 }}
              animate={
                planActive
                  ? { pathLength: 1, opacity: 1 }
                  : { pathLength: 0, opacity: 0 }
              }
              transition={{ duration: PHASE_DURATION, ease: "easeOut" }}
            />

            {/* Op packet animating along the client->gateway wire */}
            <motion.circle
              r="5"
              fill={ACCENT}
              cy={CLIENT_Y}
              initial={{ cx: CLIENT_X + 38, opacity: 0 }}
              animate={
                planActive
                  ? { cx: GATEWAY_X - 56, opacity: [0, 1, 1, 0] }
                  : { cx: CLIENT_X + 38, opacity: 0 }
              }
              transition={{
                duration: PHASE_DURATION,
                ease: "easeOut",
                opacity: {
                  duration: PHASE_DURATION,
                  times: [0, 0.05, 0.95, 1],
                },
              }}
            />

            {/* Gateway node */}
            <circle
              cx={GATEWAY_X}
              cy={GATEWAY_Y}
              r="62"
              fill="url(#fusion-v7-gw-glow)"
            />
            <rect
              x={GATEWAY_X - 56}
              y={GATEWAY_Y - 28}
              width="112"
              height="56"
              rx="10"
              fill="rgba(12,19,34,0.85)"
              stroke="rgba(94,234,212,0.6)"
            />
            <text
              x={GATEWAY_X}
              y={GATEWAY_Y - 8}
              textAnchor="middle"
              fontFamily="var(--font-body)"
              fontSize="12"
              fill="#f5f0ea"
            >
              Fusion gateway
            </text>
            <text
              x={GATEWAY_X}
              y={GATEWAY_Y + 8}
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="10"
              fill={ACCENT}
            >
              /graphql
            </text>
            <text
              x={GATEWAY_X}
              y={GATEWAY_Y + 22}
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="9"
              fill="rgba(245,241,234,0.5)"
            >
              gateway.far
            </text>

            {/* Planning ring sweep, phase 1 only */}
            <motion.circle
              cx={GATEWAY_X}
              cy={GATEWAY_Y}
              r="72"
              fill="none"
              stroke={ACCENT}
              strokeOpacity="0.5"
              strokeWidth="1.4"
              strokeDasharray="6 4"
              style={{ transformOrigin: `${GATEWAY_X}px ${GATEWAY_Y}px` }}
              initial={{ pathLength: 0, opacity: 0, rotate: 0 }}
              animate={
                planActive
                  ? { pathLength: [0, 1, 0], opacity: [0, 1, 0], rotate: 240 }
                  : { pathLength: 0, opacity: 0, rotate: 0 }
              }
              transition={{
                duration: PHASE_DURATION * 1.1,
                ease: "easeOut",
              }}
            />

            {/* Subgraph nodes */}
            {SUBGRAPHS.map((s) => (
              <g key={s.key}>
                <rect
                  x={s.x - 38}
                  y={s.y - 18}
                  width="76"
                  height="36"
                  rx="6"
                  fill="rgba(245,241,234,0.04)"
                  stroke="rgba(245,241,234,0.22)"
                />
                <text
                  x={s.x}
                  y={s.y + 4}
                  textAnchor="middle"
                  fontFamily="ui-monospace, monospace"
                  fontSize="11"
                  fill="rgba(245,241,234,0.78)"
                >
                  {s.label}
                </text>
              </g>
            ))}

            {/* Gateway -> catalog wire */}
            <line
              x1={GATEWAY_X + 56}
              y1={GATEWAY_Y - 14}
              x2={SUBGRAPHS[0].x - 38}
              y2={SUBGRAPHS[0].y}
              stroke="rgba(245,241,234,0.16)"
              strokeWidth="1.2"
            />
            <motion.line
              x1={GATEWAY_X + 56}
              y1={GATEWAY_Y - 14}
              x2={SUBGRAPHS[0].x - 38}
              y2={SUBGRAPHS[0].y}
              stroke={ACCENT}
              strokeWidth="2"
              strokeLinecap="round"
              initial={{ pathLength: 0, opacity: 0 }}
              animate={
                parallelActive
                  ? { pathLength: 1, opacity: 1 }
                  : { pathLength: 0, opacity: 0 }
              }
              transition={{ duration: PHASE_DURATION, ease: "easeOut" }}
            />
            <motion.circle
              r="4.5"
              fill={ACCENT}
              initial={{
                cx: GATEWAY_X + 56,
                cy: GATEWAY_Y - 14,
                opacity: 0,
              }}
              animate={
                parallelActive
                  ? {
                      cx: SUBGRAPHS[0].x - 38,
                      cy: SUBGRAPHS[0].y,
                      opacity: [0, 1, 1, 0.6],
                    }
                  : { cx: GATEWAY_X + 56, cy: GATEWAY_Y - 14, opacity: 0 }
              }
              transition={{
                duration: PHASE_DURATION,
                ease: "easeOut",
                opacity: {
                  duration: PHASE_DURATION,
                  times: [0, 0.05, 0.95, 1],
                },
              }}
            />

            {/* Gateway -> checkout wire */}
            <line
              x1={GATEWAY_X + 56}
              y1={GATEWAY_Y}
              x2={SUBGRAPHS[1].x - 38}
              y2={SUBGRAPHS[1].y}
              stroke="rgba(245,241,234,0.16)"
              strokeWidth="1.2"
            />
            <motion.line
              x1={GATEWAY_X + 56}
              y1={GATEWAY_Y}
              x2={SUBGRAPHS[1].x - 38}
              y2={SUBGRAPHS[1].y}
              stroke={ACCENT}
              strokeWidth="2"
              strokeLinecap="round"
              initial={{ pathLength: 0, opacity: 0 }}
              animate={
                parallelActive
                  ? { pathLength: 1, opacity: 1 }
                  : { pathLength: 0, opacity: 0 }
              }
              transition={{ duration: PHASE_DURATION, ease: "easeOut" }}
            />
            <motion.circle
              r="4.5"
              fill={ACCENT}
              initial={{ cx: GATEWAY_X + 56, cy: GATEWAY_Y, opacity: 0 }}
              animate={
                parallelActive
                  ? {
                      cx: SUBGRAPHS[1].x - 38,
                      cy: SUBGRAPHS[1].y,
                      opacity: [0, 1, 1, 0.6],
                    }
                  : { cx: GATEWAY_X + 56, cy: GATEWAY_Y, opacity: 0 }
              }
              transition={{
                duration: PHASE_DURATION,
                ease: "easeOut",
                opacity: {
                  duration: PHASE_DURATION,
                  times: [0, 0.05, 0.95, 1],
                },
              }}
            />

            {/* Gateway -> reviews wire, dashed, fires in batch phase */}
            <line
              x1={GATEWAY_X + 56}
              y1={GATEWAY_Y + 14}
              x2={SUBGRAPHS[2].x - 38}
              y2={SUBGRAPHS[2].y}
              stroke="rgba(245,241,234,0.16)"
              strokeWidth="1.2"
              strokeDasharray="3 3"
            />
            <motion.line
              x1={GATEWAY_X + 56}
              y1={GATEWAY_Y + 14}
              x2={SUBGRAPHS[2].x - 38}
              y2={SUBGRAPHS[2].y}
              stroke={ACCENT}
              strokeWidth="2"
              strokeLinecap="round"
              strokeDasharray="4 3"
              initial={{ pathLength: 0, opacity: 0 }}
              animate={
                batchActive
                  ? { pathLength: 1, opacity: 1 }
                  : { pathLength: 0, opacity: 0 }
              }
              transition={{ duration: PHASE_DURATION, ease: "easeOut" }}
            />
            <motion.circle
              r="4.5"
              fill={ACCENT}
              initial={{
                cx: GATEWAY_X + 56,
                cy: GATEWAY_Y + 14,
                opacity: 0,
              }}
              animate={
                batchActive
                  ? {
                      cx: SUBGRAPHS[2].x - 38,
                      cy: SUBGRAPHS[2].y,
                      opacity: [0, 1, 1, 0.6],
                    }
                  : { cx: GATEWAY_X + 56, cy: GATEWAY_Y + 14, opacity: 0 }
              }
              transition={{
                duration: PHASE_DURATION,
                ease: "easeOut",
                opacity: {
                  duration: PHASE_DURATION,
                  times: [0, 0.05, 0.95, 1],
                },
              }}
            />

            {/* Compose: response packet back to client */}
            <motion.circle
              r="5"
              fill={ACCENT}
              cy={CLIENT_Y}
              initial={{ cx: GATEWAY_X - 56, opacity: 0 }}
              animate={
                composeActive
                  ? { cx: CLIENT_X + 38, opacity: [0, 1, 1, 0] }
                  : { cx: GATEWAY_X - 56, opacity: 0 }
              }
              transition={{
                duration: PHASE_DURATION,
                ease: "easeOut",
                opacity: {
                  duration: PHASE_DURATION,
                  times: [0, 0.05, 0.95, 1],
                },
              }}
            />
            <motion.line
              x1={GATEWAY_X - 56}
              y1={CLIENT_Y}
              x2={CLIENT_X + 38}
              y2={CLIENT_Y}
              stroke={ACCENT}
              strokeWidth="2"
              strokeOpacity="0.6"
              strokeLinecap="round"
              strokeDasharray="2 4"
              initial={{ pathLength: 0, opacity: 0 }}
              animate={
                composeActive
                  ? { pathLength: 1, opacity: 1 }
                  : { pathLength: 0, opacity: 0 }
              }
              transition={{ duration: PHASE_DURATION, ease: "easeOut" }}
            />

            {/* ms tickers next to each subgraph */}
            <MsTicker
              x={SUBGRAPHS[0].x + 46}
              y={SUBGRAPHS[0].y + 4}
              target={12}
              active={parallelActive}
            />
            <MsTicker
              x={SUBGRAPHS[1].x + 46}
              y={SUBGRAPHS[1].y + 4}
              target={10}
              active={parallelActive}
            />
            <MsTicker
              x={SUBGRAPHS[2].x + 46}
              y={SUBGRAPHS[2].y + 4}
              target={14}
              active={batchActive}
            />

            {/* Key list pill assembled during batch phase, sits under gateway */}
            <motion.g
              initial={{ opacity: 0 }}
              animate={{ opacity: batchActive ? 1 : 0 }}
              transition={{ duration: 0.4, ease: "easeOut" }}
            >
              <rect
                x={GATEWAY_X - 88}
                y={GATEWAY_Y + 50}
                width="176"
                height="22"
                rx="11"
                fill="rgba(94,234,212,0.08)"
                stroke="rgba(94,234,212,0.55)"
              />
              <text
                x={GATEWAY_X}
                y={GATEWAY_Y + 65}
                textAnchor="middle"
                fontFamily="ui-monospace, monospace"
                fontSize="10"
                fill={ACCENT}
              >
                reviews(productIds: [...])
              </text>
            </motion.g>
          </svg>

          {/* Phase scrubber */}
          <div className="border-cc-card-border border-t px-5 py-4 sm:px-6">
            <div className="grid grid-cols-4 gap-2">
              {PHASES.map((p, i) => (
                <div
                  key={p.key}
                  className={[
                    "flex flex-col gap-1.5 rounded-md px-3 py-2 transition-colors",
                    i === phaseIndex
                      ? "bg-cc-accent/10 border-cc-accent/50 border"
                      : "border-cc-card-border border",
                  ].join(" ")}
                >
                  <span
                    className={[
                      "font-mono text-[10.5px] tracking-widest uppercase",
                      i === phaseIndex ? "text-cc-accent" : "text-cc-ink-dim",
                    ].join(" ")}
                  >
                    {String(i + 1).padStart(2, "0")} {p.label}
                  </span>
                  <span className="text-cc-ink-dim hidden text-[11px] leading-tight sm:block">
                    {p.caption}
                  </span>
                </div>
              ))}
            </div>
            <div className="border-cc-card-border mt-3 h-1 overflow-hidden rounded-full border">
              <motion.div
                className="bg-cc-accent h-full origin-left"
                initial={{ scaleX: 0 }}
                animate={{ scaleX: (phaseIndex + 1) / 4 }}
                transition={{ duration: 0.5, ease: "easeOut" }}
              />
            </div>
          </div>
        </div>
      </div>

      <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-center font-mono text-[11px] tracking-widest uppercase">
        scroll to advance the plan
      </p>
    </div>
  );
}

interface MsTickerProps {
  readonly x: number;
  readonly y: number;
  readonly target: number;
  readonly active: boolean;
}

function MsTicker({ x, y, target, active }: MsTickerProps) {
  const [display, setDisplay] = useState(0);
  useEffect(() => {
    if (!active) {
      return;
    }
    const start = performance.now();
    const duration = PHASE_DURATION * 1000;
    let raf = 0;
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / duration);
      setDisplay(Math.round(t * target));
      if (t < 1) {
        raf = requestAnimationFrame(tick);
      }
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [active, target]);
  return (
    <text
      x={x}
      y={y}
      textAnchor="start"
      fontFamily="ui-monospace, monospace"
      fontSize="10.5"
      fill={ACCENT}
    >
      {display}ms
    </text>
  );
}

// Reduced-motion fallback: final frame with everything lit, no scroll driving.
function PhaseTheatreStatic() {
  const finals = [
    { ...SUBGRAPHS[0], ms: "12ms" },
    { ...SUBGRAPHS[1], ms: "10ms" },
    { ...SUBGRAPHS[2], ms: "14ms" },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border shadow-2xl">
      <div className="border-cc-card-border flex items-center justify-between gap-3 border-b px-5 py-3 sm:px-6">
        <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
          distributed query plan
        </span>
        <span className="border-cc-accent/40 text-cc-accent bg-cc-accent/10 inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10.5px] tracking-wider uppercase">
          <span className="text-cc-accent" aria-hidden>
            <CheckIcon size={11} />
          </span>
          satisfied
        </span>
      </div>

      <svg
        viewBox="0 0 520 320"
        className="h-auto w-full"
        role="img"
        aria-label="The final frame of the distributed query plan, with the planner satisfied"
      >
        <defs>
          <radialGradient
            id="fusion-v7-gw-glow-static"
            cx="0.5"
            cy="0.5"
            r="0.6"
          >
            <stop offset="0%" stopColor={ACCENT} stopOpacity="0.28" />
            <stop offset="100%" stopColor={ACCENT} stopOpacity="0" />
          </radialGradient>
        </defs>
        <rect
          x={CLIENT_X - 38}
          y={CLIENT_Y - 22}
          width="76"
          height="44"
          rx="8"
          fill="rgba(245,241,234,0.04)"
          stroke="rgba(245,241,234,0.22)"
        />
        <text
          x={CLIENT_X}
          y={CLIENT_Y - 2}
          textAnchor="middle"
          fontFamily="var(--font-body)"
          fontSize="12"
          fill="#f5f0ea"
        >
          client
        </text>
        <text
          x={CLIENT_X}
          y={CLIENT_Y + 14}
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="9.5"
          fill="rgba(245,241,234,0.55)"
        >
          one operation
        </text>
        <line
          x1={CLIENT_X + 38}
          y1={CLIENT_Y}
          x2={GATEWAY_X - 56}
          y2={GATEWAY_Y}
          stroke={ACCENT}
          strokeWidth="2"
          strokeLinecap="round"
        />
        <circle
          cx={GATEWAY_X}
          cy={GATEWAY_Y}
          r="62"
          fill="url(#fusion-v7-gw-glow-static)"
        />
        <rect
          x={GATEWAY_X - 56}
          y={GATEWAY_Y - 28}
          width="112"
          height="56"
          rx="10"
          fill="rgba(12,19,34,0.85)"
          stroke="rgba(94,234,212,0.6)"
        />
        <text
          x={GATEWAY_X}
          y={GATEWAY_Y - 8}
          textAnchor="middle"
          fontFamily="var(--font-body)"
          fontSize="12"
          fill="#f5f0ea"
        >
          Fusion gateway
        </text>
        <text
          x={GATEWAY_X}
          y={GATEWAY_Y + 8}
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill={ACCENT}
        >
          /graphql
        </text>
        <text
          x={GATEWAY_X}
          y={GATEWAY_Y + 22}
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="9"
          fill="rgba(245,241,234,0.5)"
        >
          gateway.far
        </text>

        {finals.map((s, i) => {
          const wireY = GATEWAY_Y + (i - 1) * 14;
          return (
            <g key={s.key}>
              <line
                x1={GATEWAY_X + 56}
                y1={wireY}
                x2={s.x - 38}
                y2={s.y}
                stroke={ACCENT}
                strokeWidth="2"
                strokeLinecap="round"
                strokeDasharray={s.key === "reviews" ? "4 3" : undefined}
              />
              <rect
                x={s.x - 38}
                y={s.y - 18}
                width="76"
                height="36"
                rx="6"
                fill="rgba(245,241,234,0.04)"
                stroke="rgba(245,241,234,0.22)"
              />
              <text
                x={s.x}
                y={s.y + 4}
                textAnchor="middle"
                fontFamily="ui-monospace, monospace"
                fontSize="11"
                fill="rgba(245,241,234,0.78)"
              >
                {s.label}
              </text>
              <text
                x={s.x + 46}
                y={s.y + 4}
                textAnchor="start"
                fontFamily="ui-monospace, monospace"
                fontSize="10.5"
                fill={ACCENT}
              >
                {s.ms}
              </text>
            </g>
          );
        })}

        <rect
          x={GATEWAY_X - 88}
          y={GATEWAY_Y + 50}
          width="176"
          height="22"
          rx="11"
          fill="rgba(94,234,212,0.08)"
          stroke="rgba(94,234,212,0.55)"
        />
        <text
          x={GATEWAY_X}
          y={GATEWAY_Y + 65}
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill={ACCENT}
        >
          reviews(productIds: [...])
        </text>
      </svg>

      <div className="border-cc-card-border border-t px-5 py-4 sm:px-6">
        <div className="grid grid-cols-4 gap-2">
          {PHASES.map((p, i) => (
            <div
              key={p.key}
              className="bg-cc-accent/10 border-cc-accent/50 flex flex-col gap-1.5 rounded-md border px-3 py-2"
            >
              <span className="text-cc-accent font-mono text-[10.5px] tracking-widest uppercase">
                {String(i + 1).padStart(2, "0")} {p.label}
              </span>
              <span className="text-cc-ink-dim hidden text-[11px] leading-tight sm:block">
                {p.caption}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Composition pipeline strip: parse -> enrich -> validate -> merge -> satisfiability -> gateway.far
// Each node lights in sequence when the strip enters view.
// -----------------------------------------------------------------------------

function CompositionStrip() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { amount: 0.5, once: true });
  const prefersReduced = useReducedMotion();
  const nodes = [
    "parse",
    "enrich",
    "validate",
    "merge",
    "satisfiability",
    "gateway.far",
  ];
  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6 sm:p-8"
    >
      <div className="flex flex-wrap items-center gap-2 sm:gap-3">
        {nodes.map((n, i) => (
          <div key={n} className="flex items-center gap-2 sm:gap-3">
            <motion.div
              initial={prefersReduced ? false : { opacity: 0.25, scale: 0.98 }}
              animate={
                inView || prefersReduced
                  ? { opacity: 1, scale: 1 }
                  : { opacity: 0.25, scale: 0.98 }
              }
              transition={{
                delay: prefersReduced ? 0 : i * 0.18,
                duration: 0.5,
                ease: "easeOut",
              }}
              className={[
                "rounded-md border px-3 py-2 font-mono text-[11px] tracking-widest uppercase",
                i === nodes.length - 1
                  ? "border-cc-accent/55 bg-cc-accent/10 text-cc-accent"
                  : "border-cc-card-border text-cc-ink-dim bg-[rgba(245,241,234,0.03)]",
              ].join(" ")}
            >
              {n}
            </motion.div>
            {i < nodes.length - 1 && (
              <motion.span
                initial={prefersReduced ? false : { opacity: 0, scaleX: 0 }}
                animate={
                  inView || prefersReduced
                    ? { opacity: 1, scaleX: 1 }
                    : { opacity: 0, scaleX: 0 }
                }
                transition={{
                  delay: prefersReduced ? 0 : i * 0.18 + 0.12,
                  duration: 0.35,
                }}
                className="bg-cc-card-border h-px w-6 origin-left sm:w-10"
                aria-hidden
              />
            )}
          </div>
        ))}
      </div>
      <p className="text-cc-prose mt-6 max-w-3xl text-base leading-relaxed sm:text-lg">
        Composition happens in CI. The gateway loads the artifact at startup, so
        the runtime never sees raw source schemas, and a release cannot ship a
        graph the build did not prove answerable.
      </p>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Satisfiability tree: branches resolve in cascade, ending on UNSATISFIABLE chip.
// -----------------------------------------------------------------------------

function SatisfiabilityTree() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { amount: 0.4, once: true });
  const prefersReduced = useReducedMotion();
  const branches = [
    { y: 40, label: "order(id)", owner: "checkout" },
    { y: 96, label: "order.items", owner: "catalog" },
    { y: 152, label: "order.shipping", owner: "checkout" },
  ];
  return (
    <div ref={ref}>
      <svg
        viewBox="0 0 480 220"
        className="h-auto w-full"
        role="img"
        aria-label="Every reachable field has a resolver path across the composed graph"
      >
        <text
          x="12"
          y="18"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.45)"
        >
          reachability walk over Query.*
        </text>

        <rect
          x="20"
          y="92"
          width="68"
          height="32"
          rx="6"
          fill="rgba(94,234,212,0.08)"
          stroke="rgba(94,234,212,0.55)"
        />
        <text
          x="54"
          y="112"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="11"
          fill={ACCENT}
        >
          Query
        </text>

        {branches.map((n, i) => (
          <g key={n.label}>
            <motion.path
              d={`M 88 108 C 130 108, 130 ${n.y + 14}, 172 ${n.y + 14}`}
              stroke="rgba(94,234,212,0.45)"
              strokeWidth="1.4"
              fill="none"
              initial={prefersReduced ? false : { pathLength: 0, opacity: 0 }}
              animate={
                inView || prefersReduced
                  ? { pathLength: 1, opacity: 1 }
                  : { pathLength: 0, opacity: 0 }
              }
              transition={{
                delay: prefersReduced ? 0 : 0.2 + i * 0.35,
                duration: 0.55,
                ease: "easeOut",
              }}
            />
            <motion.g
              initial={prefersReduced ? false : { opacity: 0, x: -6 }}
              animate={
                inView || prefersReduced
                  ? { opacity: 1, x: 0 }
                  : { opacity: 0, x: -6 }
              }
              transition={{
                delay: prefersReduced ? 0 : 0.45 + i * 0.35,
                duration: 0.35,
              }}
            >
              <rect
                x="172"
                y={n.y}
                width="156"
                height="28"
                rx="6"
                fill="rgba(245,241,234,0.04)"
                stroke="rgba(94,234,212,0.45)"
              />
              <text
                x="184"
                y={n.y + 18}
                fontFamily="ui-monospace, monospace"
                fontSize="11"
                fill="#f5f0ea"
              >
                {n.label}
              </text>
              <text
                x="320"
                y={n.y + 18}
                textAnchor="end"
                fontFamily="ui-monospace, monospace"
                fontSize="10"
                fill="rgba(245,241,234,0.55)"
              >
                {n.owner}
              </text>
              <g transform={`translate(336 ${n.y + 6})`}>
                <rect
                  width="120"
                  height="16"
                  rx="8"
                  fill="rgba(94,234,212,0.12)"
                  stroke="rgba(94,234,212,0.55)"
                />
                <text
                  x="60"
                  y="12"
                  textAnchor="middle"
                  fontFamily="ui-monospace, monospace"
                  fontSize="9"
                  fill={ACCENT}
                >
                  path resolvable
                </text>
              </g>
            </motion.g>
          </g>
        ))}

        <motion.g
          initial={prefersReduced ? false : { opacity: 0, y: 6 }}
          animate={
            inView || prefersReduced
              ? { opacity: 1, y: 0 }
              : { opacity: 0, y: 6 }
          }
          transition={{
            delay: prefersReduced ? 0 : 1.6,
            duration: 0.4,
          }}
        >
          <text
            x="12"
            y="204"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.45)"
          >
            unresolvable shapes fail composition with UNSATISFIABLE_QUERY_PATH
          </text>
        </motion.g>
      </svg>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Federation interop: three source-schema cards converge on a single Fusion node.
// -----------------------------------------------------------------------------

function FederationInteropAnimated() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { amount: 0.4, once: true });
  const prefersReduced = useReducedMotion();
  const rows = [
    { y: 30, label: "Apollo Federation v2", sub: "@key, @requires" },
    { y: 96, label: "Hot Chocolate", sub: "@lookup, plain Query" },
    { y: 162, label: "Hot Chocolate (entities)", sub: "@lookup, @key" },
  ];
  return (
    <div ref={ref}>
      <svg
        viewBox="0 0 480 220"
        className="h-auto w-full"
        role="img"
        aria-label="Three source schemas converge into a single Fusion gateway"
      >
        {rows.map((row, i) => (
          <g key={row.label}>
            <motion.g
              initial={prefersReduced ? false : { opacity: 0, x: -10 }}
              animate={
                inView || prefersReduced
                  ? { opacity: 1, x: 0 }
                  : { opacity: 0, x: -10 }
              }
              transition={{
                delay: prefersReduced ? 0 : i * 0.18,
                duration: 0.4,
              }}
            >
              <rect
                x="12"
                y={row.y}
                width="200"
                height="40"
                rx="6"
                fill="rgba(245,241,234,0.04)"
                stroke="rgba(245,241,234,0.16)"
              />
              <text
                x="24"
                y={row.y + 18}
                fontFamily="var(--font-body)"
                fontSize="12"
                fill="#f5f0ea"
              >
                {row.label}
              </text>
              <text
                x="24"
                y={row.y + 33}
                fontFamily="ui-monospace, monospace"
                fontSize="10"
                fill="rgba(245,241,234,0.55)"
              >
                {row.sub}
              </text>
            </motion.g>
            <motion.path
              d={`M 212 ${row.y + 20} C 260 ${row.y + 20}, 280 110, 320 110`}
              stroke="rgba(94,234,212,0.55)"
              strokeWidth="1.5"
              fill="none"
              initial={prefersReduced ? false : { pathLength: 0, opacity: 0 }}
              animate={
                inView || prefersReduced
                  ? { pathLength: 1, opacity: 1 }
                  : { pathLength: 0, opacity: 0 }
              }
              transition={{
                delay: prefersReduced ? 0 : 0.6 + i * 0.18,
                duration: 0.55,
                ease: "easeOut",
              }}
            />
          </g>
        ))}
        <motion.g
          initial={prefersReduced ? false : { opacity: 0, scale: 0.92 }}
          animate={
            inView || prefersReduced
              ? { opacity: 1, scale: 1 }
              : { opacity: 0, scale: 0.92 }
          }
          transition={{
            delay: prefersReduced ? 0 : 1.2,
            duration: 0.4,
          }}
        >
          <rect
            x="320"
            y="86"
            width="148"
            height="48"
            rx="8"
            fill="rgba(94,234,212,0.08)"
            stroke="rgba(94,234,212,0.55)"
          />
          <text
            x="394"
            y="108"
            textAnchor="middle"
            fontFamily="var(--font-body)"
            fontSize="12"
            fill={ACCENT}
          >
            Fusion gateway
          </text>
          <text
            x="394"
            y="124"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9.5"
            fill="rgba(245,241,234,0.62)"
          >
            GraphQL Composite Schemas spec
          </text>
        </motion.g>
      </svg>
    </div>
  );
}

// -----------------------------------------------------------------------------
// .NET pipeline diagram: subtle pulse on the Fusion middleware block.
// -----------------------------------------------------------------------------

function DotNetGatewayAnimated() {
  const prefersReduced = useReducedMotion();
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="ASP.NET Core middleware pipeline hosting the Fusion middleware"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        Program.cs
      </text>

      {[
        { x: 12, label: "AuthN" },
        { x: 96, label: "Headers" },
        { x: 180, label: "Fusion" },
        { x: 264, label: "Cache" },
        { x: 348, label: "Telemetry" },
      ].map((m) => (
        <g key={m.label}>
          {m.label === "Fusion" ? (
            <>
              <motion.rect
                x={m.x}
                y="60"
                width="76"
                height="28"
                rx="4"
                fill="rgba(94,234,212,0.16)"
                stroke="rgba(94,234,212,0.6)"
                animate={
                  prefersReduced ? { opacity: 1 } : { opacity: [0.7, 1, 0.7] }
                }
                transition={
                  prefersReduced
                    ? undefined
                    : {
                        repeat: Infinity,
                        repeatType: "loop",
                        duration: 3,
                        ease: "easeInOut",
                      }
                }
              />
              <text
                x={m.x + 38}
                y="78"
                textAnchor="middle"
                fontFamily="ui-monospace, monospace"
                fontSize="10.5"
                fill={ACCENT}
              >
                {m.label}
              </text>
            </>
          ) : (
            <>
              <rect
                x={m.x}
                y="60"
                width="76"
                height="28"
                rx="4"
                fill="rgba(245,241,234,0.04)"
                stroke="rgba(245,241,234,0.18)"
              />
              <text
                x={m.x + 38}
                y="78"
                textAnchor="middle"
                fontFamily="ui-monospace, monospace"
                fontSize="10.5"
                fill="rgba(245,241,234,0.7)"
              >
                {m.label}
              </text>
            </>
          )}
        </g>
      ))}
      <path
        d="M 12 100 L 424 100"
        stroke="rgba(245,241,234,0.18)"
        strokeWidth="1"
        fill="none"
      />
      <text
        x="12"
        y="116"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        ASP.NET Core middleware pipeline
      </text>

      <rect
        x="12"
        y="148"
        width="412"
        height="60"
        rx="6"
        fill="rgba(94,234,212,0.06)"
        stroke="rgba(94,234,212,0.4)"
      />
      <text
        x="24"
        y="168"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill={ACCENT}
      >
        builder.Services.AddGraphQLGateway()
      </text>
      <text
        x="24"
        y="186"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill={ACCENT}
      >
        .AddFileSystemConfiguration(&quot;./gateway.far&quot;);
      </text>
      <text
        x="24"
        y="202"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.55)"
      >
        your DI, your auth, your logging
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Self-run band: animated packet loop never crosses the dashed boundary.
// -----------------------------------------------------------------------------

function SelfRunBand() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { amount: 0.4, once: true });
  const prefersReduced = useReducedMotion();
  return (
    <div ref={ref}>
      <svg
        viewBox="0 0 480 220"
        className="h-auto w-full"
        role="img"
        aria-label="A request packet stays inside your network across the gateway and subgraphs"
      >
        <rect
          x="12"
          y="22"
          width="456"
          height="176"
          rx="12"
          fill="rgba(245,241,234,0.03)"
          stroke="rgba(245,241,234,0.22)"
          strokeDasharray="4 4"
        />
        <text
          x="28"
          y="42"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.55)"
        >
          your network
        </text>

        <rect
          x="32"
          y="92"
          width="76"
          height="32"
          rx="6"
          fill="rgba(245,241,234,0.04)"
          stroke="rgba(245,241,234,0.18)"
        />
        <text
          x="70"
          y="112"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="11"
          fill="#f5f0ea"
        >
          client
        </text>

        <rect
          x="180"
          y="80"
          width="140"
          height="56"
          rx="8"
          fill="rgba(94,234,212,0.08)"
          stroke="rgba(94,234,212,0.55)"
        />
        <text
          x="250"
          y="104"
          textAnchor="middle"
          fontFamily="var(--font-body)"
          fontSize="12"
          fill={ACCENT}
        >
          Fusion gateway
        </text>
        <text
          x="250"
          y="120"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.62)"
        >
          ASP.NET Core
        </text>

        {[
          { y: 56, label: "catalog" },
          { y: 100, label: "checkout" },
          { y: 144, label: "reviews" },
        ].map((s) => (
          <g key={s.label}>
            <path
              d={`M 320 108 C 350 108, 350 ${s.y + 14}, 388 ${s.y + 14}`}
              stroke="rgba(94,234,212,0.45)"
              strokeWidth="1.2"
              fill="none"
            />
            <rect
              x="388"
              y={s.y}
              width="64"
              height="28"
              rx="4"
              fill="rgba(245,241,234,0.04)"
              stroke="rgba(245,241,234,0.16)"
            />
            <text
              x="420"
              y={s.y + 18}
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="10.5"
              fill="rgba(245,241,234,0.62)"
            >
              {s.label}
            </text>
          </g>
        ))}

        {/* Packet loop, in view only */}
        <motion.circle
          r="5"
          fill={ACCENT}
          initial={prefersReduced ? false : { opacity: 0 }}
          animate={
            inView || prefersReduced
              ? prefersReduced
                ? { opacity: 1, cx: 70, cy: 108 }
                : {
                    opacity: 1,
                    cx: [70, 250, 420, 250, 70],
                    cy: [108, 108, 70, 108, 108],
                  }
              : { opacity: 0 }
          }
          transition={
            prefersReduced
              ? undefined
              : {
                  duration: 5.5,
                  times: [0, 0.25, 0.5, 0.75, 1],
                  ease: "easeInOut",
                  repeat: Infinity,
                  repeatDelay: 0.6,
                }
          }
        />

        <text
          x="28"
          y="186"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.45)"
        >
          no hosted hop, no third party in the request path
        </text>
      </svg>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Code window primitive (used in the .NET row), matched to v1 styling.
// -----------------------------------------------------------------------------

interface ConsoleProps {
  readonly file: string;
  readonly tag: string;
  readonly children: ReactNode;
}

function ConsoleCard({ file, tag, children }: ConsoleProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span
          className="bg-cc-status-firing h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-investigating h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-healthy h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span className="text-cc-ink-dim ml-3 font-mono text-[11px]">
          {file}
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {tag}
        </span>
      </div>
      <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[12.5px] leading-6">
        {children}
      </pre>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Two-column section with copy + animated visual.
// -----------------------------------------------------------------------------

interface TwoColumnRowProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

function TwoColumnRow({
  id,
  index,
  eyebrow,
  title,
  body,
  bullets,
  visual,
  reverse = false,
}: TwoColumnRowProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <div
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex items-center gap-3">
            <IndexTag value={index} />
            <Eyebrow>{eyebrow}</Eyebrow>
          </div>
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            {title}
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            {body}
          </p>
          <ul className="mt-6 flex flex-col gap-2.5">
            {bullets.map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon size={14} />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </div>
        <div
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
        >
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 sm:p-6">
            {visual}
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Proof grid item with number tick-up on view.
// -----------------------------------------------------------------------------

interface ProofItemProps {
  readonly label: string;
  readonly value: string;
}

function ProofItem({ label, value }: ProofItemProps) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { amount: 0.5, once: true });
  const prefersReduced = useReducedMotion();
  return (
    <motion.div
      ref={ref}
      initial={prefersReduced ? false : { opacity: 0, y: 6 }}
      animate={
        inView || prefersReduced ? { opacity: 1, y: 0 } : { opacity: 0, y: 6 }
      }
      transition={{ duration: 0.45, ease: "easeOut" }}
      className="flex flex-col gap-1"
    >
      <span className="text-cc-heading font-heading text-2xl font-semibold tracking-tight">
        {value}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
        {label}
      </span>
    </motion.div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function ClientPage() {
  const prefersReduced = useReducedMotion();

  return (
    <MotionConfig reducedMotion="user">
      {/* HERO */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-6">
            <motion.div
              initial={prefersReduced ? false : { opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5, ease: "easeOut" }}
            >
              <Eyebrow>Distributed GraphQL gateway</Eyebrow>
            </motion.div>
            <motion.h1
              initial={prefersReduced ? false : { opacity: 0, y: 14 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.55, delay: 0.1, ease: "easeOut" }}
              className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl"
            >
              Compose at planning time. Execute as one distributed plan.
            </motion.h1>
            <motion.p
              initial={prefersReduced ? false : { opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5, delay: 0.25, ease: "easeOut" }}
              className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed"
            >
              Fusion is ChilliCream&apos;s distributed GraphQL gateway for .NET.
              Compose subgraphs into one composite schema in CI, ship a
              versioned plan proven answerable, then watch the gateway split a
              single client operation into parallel and batched subgraph fetches
              and return one composed response.
            </motion.p>
            <motion.div
              initial={prefersReduced ? false : { opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.45, delay: 0.4, ease: "easeOut" }}
              className="mt-8 flex flex-wrap gap-3"
            >
              <SolidButton href="/docs/fusion">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </motion.div>
            <motion.dl
              initial={prefersReduced ? false : { opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.6, delay: 0.55 }}
              className="border-cc-card-border mt-10 grid grid-cols-3 gap-6 border-t pt-6"
            >
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  License
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">MIT</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Runtime
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">ASP.NET Core</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Spec
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">Composite Schemas</dd>
              </div>
            </motion.dl>
          </div>
          <div className="lg:col-span-6">
            <HeroSnapshot />
          </div>
        </div>
      </section>

      {/* CENTERPIECE THEATRE */}
      <section
        aria-label="Distributed query plan, in motion"
        className="border-cc-card-border border-t py-16 sm:py-24"
      >
        <div className="mb-10 grid items-end gap-6 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <IndexTag value="01" />
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Plan in motion: one operation, a distributed fetch.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              The gateway takes a single client operation, plans it against your
              composite schema, and executes parallel and batched fetches across
              the subgraphs that own each field. Scroll to step through the four
              phases.
            </p>
          </div>
          <div className="lg:col-span-5 lg:text-right">
            <p className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
              phases: plan, parallel, batch, compose
            </p>
          </div>
        </div>
        <PhaseTheatre />
      </section>

      {/* COMPOSITION STRIP */}
      <section className="border-cc-card-border border-t py-20 sm:py-24">
        <div className="mb-10 grid items-end gap-6 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <IndexTag value="02" />
            <Eyebrow>Composition</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Composition runs in CI, not on a hot path.
            </h2>
          </div>
        </div>
        <CompositionStrip />
      </section>

      {/* SATISFIABILITY */}
      <TwoColumnRow
        id="satisfiability"
        index="03"
        eyebrow="Satisfiability proof"
        title="If it composes, it answers."
        body="Composition's final phase walks every reachable field from the root types and proves it can be resolved across your subgraphs given the available lookups and keys. A query that successfully validates against the gateway is one your services can actually answer. Unreachable shapes fail composition with UNSATISFIABLE_QUERY_PATH."
        bullets={[
          "Reachability analysis over the full composed graph, shipped in Fusion.Composition.Satisfiability.",
          "Catches contract drift between subgraphs before a client ever sends the query.",
          "Failures cite the exact field path, so the broken shape is the next thing you fix.",
        ]}
        visual={<SatisfiabilityTree />}
      />

      {/* FEDERATION */}
      <TwoColumnRow
        id="federation"
        index="04"
        eyebrow="Apollo Federation"
        title="Apollo Federation spec compatible, on an open standard."
        body="Fusion implements the GraphQL Composite Schemas specification under the GraphQL Foundation, and reads Apollo Federation v2 subgraphs through a dedicated connector. Bring existing @key, @requires, and @provides directives into a Fusion composition without rewriting resolvers, on a vendor-neutral spec."
        bullets={[
          "GraphQL Composite Schemas spec, vendor-neutral. Subgraph schemas stay portable.",
          "Apollo Federation v2 interop via Fusion.Connectors.ApolloFederation.",
          "Documented migration path from Federation v2 with directive-by-directive mapping.",
        ]}
        visual={<FederationInteropAnimated />}
        reverse
      />

      {/* .NET-NATIVE */}
      <TwoColumnRow
        id="dotnet"
        index="05"
        eyebrow=".NET-native gateway"
        title="The gateway is your code, on Hot Chocolate."
        body="Fusion's gateway is an ASP.NET Core app, configured with AddGraphQLGateway() and built on Hot Chocolate. Your DI container, your authentication, your middleware, your logging. No standalone binary, no YAML, no Node runtime in the request path. The same Hot Chocolate server you already ship can be a Fusion subgraph with no resolver changes."
        bullets={[
          "AddGraphQLGateway() integrates with the ASP.NET Core middleware pipeline you already operate.",
          "Auth, header propagation, and cache control land where you expect them in .NET.",
          "An existing Hot Chocolate server is already a valid subgraph, no federation library needed.",
        ]}
        visual={
          <div className="flex flex-col gap-4">
            <DotNetGatewayAnimated />
            <ConsoleCard file="Program.cs" tag="C#">
              <span style={{ color: "#c9d1d9" }}>{"var builder = "}</span>
              <span style={{ color: "#ffa657" }}>WebApplication</span>
              <span style={{ color: "#c9d1d9" }}>{"."}</span>
              <span style={{ color: "#d2a8ff" }}>CreateBuilder</span>
              <span style={{ color: "#c9d1d9" }}>{"(args);"}</span>
              {"\n\n"}
              <span style={{ color: "#c9d1d9" }}>{"builder.Services"}</span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>{"    ."}</span>
              <span style={{ color: "#d2a8ff" }}>AddGraphQLGateway</span>
              <span style={{ color: "#c9d1d9" }}>{"()"}</span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>{"    ."}</span>
              <span style={{ color: "#d2a8ff" }}>
                AddFileSystemConfiguration
              </span>
              <span style={{ color: "#c9d1d9" }}>{"("}</span>
              <span style={{ color: "#a5d6ff" }}>{'"./gateway.far"'}</span>
              <span style={{ color: "#c9d1d9" }}>{");"}</span>
              {"\n\n"}
              <span style={{ color: "#c9d1d9" }}>{"var app = builder."}</span>
              <span style={{ color: "#d2a8ff" }}>Build</span>
              <span style={{ color: "#c9d1d9" }}>{"();"}</span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>{"app."}</span>
              <span style={{ color: "#d2a8ff" }}>MapGraphQL</span>
              <span style={{ color: "#c9d1d9" }}>{"();"}</span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>{"app."}</span>
              <span style={{ color: "#d2a8ff" }}>Run</span>
              <span style={{ color: "#c9d1d9" }}>{"();"}</span>
            </ConsoleCard>
          </div>
        }
      />

      {/* SELF-RUN BAND */}
      <section className="border-cc-card-border border-t py-20 sm:py-24">
        <div className="mb-10 grid items-end gap-6 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <IndexTag value="06" />
            <Eyebrow>Self-run, always</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              The gateway is always self-run, never a hosted hop.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Fusion runs in your infrastructure, period. Every client request
              and every subgraph fetch stay inside your network boundary. You
              choose the cluster, the auth, the egress, the audit trail.
            </p>
          </div>
        </div>
        <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 sm:p-6">
          <SelfRunBand />
        </div>
      </section>

      {/* PROOF + CTA */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>MIT licensed</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Open source, on an open standard.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Fusion is part of the ChilliCream GraphQL Platform, developed in
              the open under the MIT license and built on the GraphQL Composite
              Schemas specification under the GraphQL Foundation. The codebase,
              the issue tracker, the roadmap, and the release notes all live on
              GitHub.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/fusion">Read the docs</OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Runtime" value="ASP.NET Core" />
              <ProofItem label="Spec" value="Composite Schemas" />
              <ProofItem label="Built on" value="Hot Chocolate" />
              <ProofItem label="Interop" value="Federation v2" />
              <ProofItem label="Tracing" value="OpenTelemetry" />
            </div>
          </div>
        </div>
      </section>

      {/* CLOSING CTA */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            One composite graph, proven before you ship it.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            Point Fusion at your subgraphs, compose in CI, and serve from a
            single .NET endpoint you operate yourself. The plan is built, the
            satisfiability is proven, and the runtime is the ASP.NET Core you
            already run.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/fusion">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </MotionConfig>
  );
}
