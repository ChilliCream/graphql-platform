"use client";

import type { ReactNode } from "react";
import { useEffect, useRef, useState } from "react";
import {
  AnimatePresence,
  MotionConfig,
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "motion/react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroTrace } from "@/src/nitro";

// NOTE: Next App Router does NOT honor `export const metadata` from a "use client"
// file at runtime, but it is still consumed for static analysis in this repo's
// preview routes. Robots is set to no-index so search engines skip these v* variants.

// Brand spectrum gradient, used exactly once on the page (the closing hairline).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const ACCENT = "#5eead4"; // cc-accent teal

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface FramedVisualProps {
  readonly children: ReactNode;
}

function FramedVisual({ children }: FramedVisualProps) {
  return (
    <div className="relative">
      <div
        aria-hidden="true"
        className="absolute -inset-x-6 -inset-y-4 -z-10 rounded-[2rem] opacity-40 blur-3xl"
        style={{
          background:
            "radial-gradient(60% 60% at 50% 40%, rgba(94,234,212,0.18), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
        {children}
      </div>
    </div>
  );
}

// ─── HERO CENTERPIECE: Control Plane Console ─────────────────────────────────
// A composite SVG dashboard pinned in the hero. Renders a request-rate
// sparkline that draws in and then drifts, three KPI tiles whose numbers count
// up via animate(), a five-lane trace waterfall whose spans stagger-grow into
// view, and a schema-diff strip that crossfades between three change classes.

interface KpiTileProps {
  readonly label: string;
  readonly suffix: string;
  readonly target: number;
  readonly drift: number;
  readonly inView: boolean;
  readonly reduceMotion: boolean;
  readonly format?: (value: number) => string;
}

function KpiTile({
  label,
  suffix,
  target,
  drift,
  inView,
  reduceMotion,
  format,
}: KpiTileProps) {
  const value = useMotionValue(reduceMotion ? target : 0);
  const [display, setDisplay] = useState(reduceMotion ? target : 0);

  useEffect(() => {
    const unsub = value.on("change", (v) => setDisplay(v));
    return unsub;
  }, [value]);

  useEffect(() => {
    if (!inView) {
      return;
    }
    if (reduceMotion) {
      value.set(target);
      return;
    }
    const controls = animate(value, target, {
      duration: 1.2,
      ease: "easeOut",
    });
    return () => controls.stop();
  }, [inView, reduceMotion, target, value]);

  useEffect(() => {
    if (!inView || reduceMotion || drift <= 0) {
      return;
    }
    let cancelled = false;
    const step = () => {
      if (cancelled) {
        return;
      }
      const next = target + (Math.random() * 2 - 1) * drift;
      const controls = animate(value, next, {
        duration: 1.6,
        ease: "easeInOut",
        onComplete: () => {
          step();
        },
      });
      return () => controls.stop();
    };
    const delay = window.setTimeout(step, 1300);
    return () => {
      cancelled = true;
      window.clearTimeout(delay);
    };
  }, [inView, reduceMotion, target, drift, value]);

  const rendered = format ? format(display) : display.toFixed(0);

  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-lg border px-4 py-3">
      <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
        {label}
      </div>
      <div className="text-cc-heading font-heading mt-1 flex items-baseline gap-1">
        <span className="text-2xl tabular-nums">{rendered}</span>
        <span className="text-cc-ink-dim text-xs">{suffix}</span>
      </div>
    </div>
  );
}

interface SparklineProps {
  readonly inView: boolean;
  readonly reduceMotion: boolean;
}

function Sparkline({ inView, reduceMotion }: SparklineProps) {
  // Stable baseline path, then a drifting overlay traced by a motion value.
  const basePoints = [
    [0, 28],
    [12, 22],
    [24, 26],
    [36, 18],
    [48, 24],
    [60, 14],
    [72, 20],
    [84, 12],
    [96, 18],
    [108, 10],
    [120, 16],
  ] as const;
  const d = basePoints
    .map(([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`)
    .join(" ");

  const phase = useMotionValue(0);
  useEffect(() => {
    if (!inView || reduceMotion) {
      return;
    }
    const controls = animate(phase, Math.PI * 2, {
      duration: 6,
      repeat: Infinity,
      repeatType: "loop",
      ease: "linear",
    });
    return () => controls.stop();
  }, [inView, reduceMotion, phase]);

  const tipX = useTransform(phase, (p) => 100 + Math.cos(p) * 12);
  const tipY = useTransform(phase, (p) => 14 + Math.sin(p) * 4);

  return (
    <svg
      viewBox="0 0 120 36"
      className="h-16 w-full"
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      <defs>
        <linearGradient id="cc-spark-fill" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={ACCENT} stopOpacity="0.35" />
          <stop offset="100%" stopColor={ACCENT} stopOpacity="0" />
        </linearGradient>
      </defs>
      <motion.path
        d={`${d} L120 36 L0 36 Z`}
        fill="url(#cc-spark-fill)"
        initial={{ opacity: 0 }}
        animate={inView ? { opacity: 1 } : { opacity: 0 }}
        transition={{ duration: 0.8, delay: 0.4 }}
      />
      <motion.path
        d={d}
        fill="none"
        stroke={ACCENT}
        strokeWidth={1.2}
        strokeLinecap="round"
        strokeLinejoin="round"
        initial={{ pathLength: 0 }}
        animate={inView ? { pathLength: 1 } : { pathLength: 0 }}
        transition={{ duration: 1.4, ease: "easeInOut" }}
      />
      {!reduceMotion && (
        <motion.circle r={1.8} fill={ACCENT} cx={tipX} cy={tipY} />
      )}
    </svg>
  );
}

interface WaterfallProps {
  readonly inView: boolean;
  readonly reduceMotion: boolean;
  readonly lanes?: ReadonlyArray<{
    start: number;
    width: number;
    label: string;
  }>;
  readonly height?: number;
}

function Waterfall({
  inView,
  reduceMotion,
  lanes = [
    { start: 0, width: 96, label: "gateway" },
    { start: 6, width: 64, label: "users" },
    { start: 22, width: 48, label: "orders" },
    { start: 30, width: 38, label: "billing" },
    { start: 54, width: 28, label: "ledger" },
  ],
  height = 88,
}: WaterfallProps) {
  const rowH = height / lanes.length;
  return (
    <svg
      viewBox={`0 0 100 ${height}`}
      preserveAspectRatio="none"
      className="h-full w-full"
      aria-hidden="true"
    >
      {lanes.map((lane, i) => {
        const y = i * rowH + rowH * 0.3;
        const barH = rowH * 0.4;
        return (
          <g key={lane.label}>
            <rect
              x={0}
              y={y + barH / 2 - 0.25}
              width={100}
              height={0.5}
              fill="rgba(255,255,255,0.04)"
            />
            <motion.rect
              x={lane.start}
              y={y}
              height={barH}
              rx={0.8}
              fill={ACCENT}
              fillOpacity={0.85 - i * 0.12}
              initial={{ width: 0 }}
              animate={
                inView
                  ? { width: lane.width }
                  : { width: reduceMotion ? lane.width : 0 }
              }
              transition={{
                duration: reduceMotion ? 0 : 0.9,
                delay: reduceMotion ? 0 : 0.15 + i * 0.15,
                ease: "easeOut",
              }}
            />
          </g>
        );
      })}
    </svg>
  );
}

type DiffKind = "safe" | "dangerous" | "breaking";

interface DiffEntry {
  readonly kind: DiffKind;
  readonly text: string;
}

const DIFF_PALETTE: Record<
  DiffKind,
  { fg: string; bg: string; label: string }
> = {
  safe: { fg: ACCENT, bg: "rgba(94,234,212,0.12)", label: "Safe" },
  dangerous: { fg: "#16b9e4", bg: "rgba(22,185,228,0.12)", label: "Dangerous" },
  breaking: { fg: "#f0786a", bg: "rgba(240,120,106,0.14)", label: "Breaking" },
};

const SCHEMA_DIFFS: ReadonlyArray<DiffEntry> = [
  { kind: "safe", text: "+ field User.displayName: String" },
  { kind: "dangerous", text: "~ field Order.total now nullable" },
  { kind: "breaking", text: "- field Invoice.legacyId removed" },
];

interface SchemaDiffStripProps {
  readonly inView: boolean;
  readonly reduceMotion: boolean;
}

function SchemaDiffStrip({ inView, reduceMotion }: SchemaDiffStripProps) {
  const [index, setIndex] = useState(
    reduceMotion ? SCHEMA_DIFFS.length - 1 : 0,
  );

  useEffect(() => {
    if (!inView || reduceMotion) {
      return;
    }
    const id = window.setInterval(() => {
      setIndex((i) => (i + 1) % SCHEMA_DIFFS.length);
    }, 2400);
    return () => window.clearInterval(id);
  }, [inView, reduceMotion]);

  const current = SCHEMA_DIFFS[index];
  const palette = DIFF_PALETTE[current.kind];

  return (
    <div className="border-cc-card-border bg-cc-card-bg flex items-center gap-3 rounded-lg border px-4 py-3">
      <AnimatePresence mode="wait">
        <motion.span
          key={`${index}-badge`}
          initial={{ opacity: 0, y: 4 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -4 }}
          transition={{ duration: 0.35 }}
          className="rounded px-2 py-0.5 font-mono text-[10px] tracking-[0.16em] uppercase"
          style={{ color: palette.fg, background: palette.bg }}
        >
          {palette.label}
        </motion.span>
      </AnimatePresence>
      <AnimatePresence mode="wait">
        <motion.code
          key={`${index}-text`}
          initial={{ opacity: 0, y: 4 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -4 }}
          transition={{ duration: 0.35 }}
          className="text-cc-ink truncate font-mono text-xs sm:text-sm"
        >
          {current.text}
        </motion.code>
      </AnimatePresence>
    </div>
  );
}

interface ControlPlaneConsoleProps {
  readonly reduceMotion: boolean;
}

function ControlPlaneConsole({ reduceMotion }: ControlPlaneConsoleProps) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { margin: "-10% 0px", once: true });

  return (
    <div ref={ref} className="relative mx-auto w-full max-w-5xl">
      <div
        aria-hidden="true"
        className="absolute -inset-x-10 -inset-y-8 -z-10 rounded-[2.5rem] opacity-50 blur-3xl"
        style={{
          background:
            "radial-gradient(50% 50% at 50% 30%, rgba(94,234,212,0.16), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-2xl border shadow-2xl shadow-black/50">
        <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
          <span className="bg-cc-accent inline-block h-2 w-2 rounded-full" />
          <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
            Nitro / production
          </span>
          <span className="text-cc-ink-dim ml-auto font-mono text-[10px]">
            live
          </span>
        </div>

        <div className="grid gap-4 p-4 sm:p-6 lg:grid-cols-12">
          <div className="border-cc-card-border bg-cc-card-bg rounded-lg border p-4 lg:col-span-7">
            <div className="flex items-center justify-between">
              <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
                Requests / sec
              </div>
              <div className="text-cc-ink-dim font-mono text-[10px]">
                last 15m
              </div>
            </div>
            <Sparkline inView={inView} reduceMotion={reduceMotion} />
          </div>

          <div className="grid grid-cols-3 gap-3 lg:col-span-5">
            <KpiTile
              label="p95"
              suffix="ms"
              target={142}
              drift={6}
              inView={inView}
              reduceMotion={reduceMotion}
            />
            <KpiTile
              label="Error rate"
              suffix="%"
              target={0.42}
              drift={0.08}
              inView={inView}
              reduceMotion={reduceMotion}
              format={(v) => v.toFixed(2)}
            />
            <KpiTile
              label="Throughput"
              suffix="rps"
              target={3120}
              drift={45}
              inView={inView}
              reduceMotion={reduceMotion}
              format={(v) => Math.round(v).toLocaleString()}
            />
          </div>

          <div className="border-cc-card-border bg-cc-card-bg rounded-lg border p-4 lg:col-span-12">
            <div className="flex items-center justify-between">
              <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
                Trace waterfall / checkoutOrder
              </div>
              <div className="text-cc-ink-dim font-mono text-[10px]">
                212 ms
              </div>
            </div>
            <div className="mt-3 h-24">
              <Waterfall inView={inView} reduceMotion={reduceMotion} />
            </div>
          </div>

          <div className="lg:col-span-12">
            <SchemaDiffStrip inView={inView} reduceMotion={reduceMotion} />
          </div>
        </div>
      </div>
    </div>
  );
}

// ─── LIVE SIGNALS STRIP ──────────────────────────────────────────────────────

interface LiveSignalsProps {
  readonly reduceMotion: boolean;
}

function LiveSignals({ reduceMotion }: LiveSignalsProps) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { margin: "-10% 0px", once: true });

  return (
    <section className="border-cc-card-border border-t py-16">
      <div ref={ref} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiTile
          label="Requests / sec"
          suffix="rps"
          target={3120}
          drift={40}
          inView={inView}
          reduceMotion={reduceMotion}
          format={(v) => Math.round(v).toLocaleString()}
        />
        <KpiTile
          label="p95 latency"
          suffix="ms"
          target={142}
          drift={4}
          inView={inView}
          reduceMotion={reduceMotion}
        />
        <KpiTile
          label="Error rate"
          suffix="%"
          target={0.42}
          drift={0.05}
          inView={inView}
          reduceMotion={reduceMotion}
          format={(v) => v.toFixed(2)}
        />
        <KpiTile
          label="Active clients"
          suffix=""
          target={184}
          drift={2}
          inView={inView}
          reduceMotion={reduceMotion}
        />
      </div>
    </section>
  );
}

// ─── PILLAR SECTIONS ─────────────────────────────────────────────────────────

interface PillarProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

function Pillar({
  id,
  index,
  eyebrow,
  title,
  body,
  visual,
  reverse = false,
}: PillarProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-28"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{ duration: 0.6, ease: "easeOut" }}
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex flex-col gap-5">
            <div className="flex items-center gap-3">
              <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
                {index}
              </span>
              <Eyebrow>{eyebrow}</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading text-h3 text-balance">
              {title}
            </h2>
            <p className="text-cc-ink max-w-md text-base leading-relaxed text-pretty sm:text-lg">
              {body}
            </p>
          </div>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{ duration: 0.7, ease: "easeOut", delay: 0.1 }}
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
        >
          <FramedVisual>{visual}</FramedVisual>
        </motion.div>
      </div>
    </section>
  );
}

// ─── PILLAR VISUALS ──────────────────────────────────────────────────────────

function ObserveMiniWaterfall({
  reduceMotion,
}: {
  readonly reduceMotion: boolean;
}) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { margin: "-10% 0px", once: true });
  return (
    <div ref={ref} className="p-6 sm:p-8">
      <div className="flex items-center justify-between">
        <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
          Operation / checkoutOrder
        </div>
        <div className="text-cc-ink-dim font-mono text-[10px]">p95 142 ms</div>
      </div>
      <div className="mt-4 h-32">
        <Waterfall inView={inView} reduceMotion={reduceMotion} height={120} />
      </div>
    </div>
  );
}

function DiagnoseSparkline({
  reduceMotion,
}: {
  readonly reduceMotion: boolean;
}) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { margin: "-10% 0px", once: true });

  // Curve climbing into a sharp spike.
  const d =
    "M0 60 L20 56 L40 50 L60 46 L80 40 L100 38 L120 30 L140 20 L150 8 L160 22 L180 36 L200 44 L240 50";

  return (
    <div ref={ref} className="p-6 sm:p-8">
      <div className="flex items-center justify-between">
        <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
          Errors / min
        </div>
        <div className="font-mono text-[10px]" style={{ color: "#f0786a" }}>
          spike detected
        </div>
      </div>
      <svg
        viewBox="0 0 240 72"
        preserveAspectRatio="none"
        className="mt-4 h-36 w-full"
        aria-hidden="true"
      >
        <defs>
          <linearGradient id="cc-diag-fill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#f0786a" stopOpacity="0.32" />
            <stop offset="100%" stopColor="#f0786a" stopOpacity="0" />
          </linearGradient>
        </defs>
        <motion.path
          d={`${d} L240 72 L0 72 Z`}
          fill="url(#cc-diag-fill)"
          initial={{ opacity: 0 }}
          animate={inView ? { opacity: 1 } : { opacity: 0 }}
          transition={{ duration: 0.6, delay: 0.6 }}
        />
        <motion.path
          d={d}
          fill="none"
          stroke="#f0786a"
          strokeWidth={1.4}
          strokeLinecap="round"
          strokeLinejoin="round"
          initial={{ pathLength: 0 }}
          animate={inView ? { pathLength: 1 } : { pathLength: 0 }}
          transition={{ duration: 1.6, ease: "easeInOut" }}
        />
        <motion.circle
          cx={150}
          cy={8}
          r={3}
          fill="#f0786a"
          initial={{ opacity: 0, scale: 0.4 }}
          animate={inView ? { opacity: 1, scale: 1 } : { opacity: 0 }}
          transition={{ duration: 0.3, delay: 1.5 }}
        />
        {!reduceMotion && (
          <motion.circle
            cx={150}
            cy={8}
            r={3}
            fill="none"
            stroke="#f0786a"
            strokeWidth={1}
            initial={{ opacity: 0.6, scale: 1 }}
            animate={inView ? { opacity: 0, scale: 3 } : { opacity: 0 }}
            transition={{
              duration: 1.4,
              repeat: Infinity,
              ease: "easeOut",
              delay: 1.6,
            }}
          />
        )}
      </svg>
    </div>
  );
}

function EvolveSchemaList({
  reduceMotion,
}: {
  readonly reduceMotion: boolean;
}) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { margin: "-10% 0px", once: true });
  const [index, setIndex] = useState(
    reduceMotion ? SCHEMA_DIFFS.length - 1 : 0,
  );

  useEffect(() => {
    if (!inView || reduceMotion) {
      return;
    }
    const id = window.setInterval(() => {
      setIndex((i) => (i + 1) % SCHEMA_DIFFS.length);
    }, 2600);
    return () => window.clearInterval(id);
  }, [inView, reduceMotion]);

  return (
    <div ref={ref} className="p-6 sm:p-8">
      <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
        PR #482 / schema check
      </div>
      <ul className="mt-4 space-y-2">
        {SCHEMA_DIFFS.map((diff, i) => {
          const palette = DIFF_PALETTE[diff.kind];
          const isActive = i === index;
          return (
            <li
              key={diff.text}
              className="border-cc-card-border bg-cc-card-bg flex items-center gap-3 rounded-md border px-3 py-2"
            >
              <motion.span
                animate={{
                  opacity: isActive || reduceMotion ? 1 : 0.5,
                }}
                transition={{ duration: 0.4 }}
                className="rounded px-2 py-0.5 font-mono text-[10px] tracking-[0.16em] uppercase"
                style={{ color: palette.fg, background: palette.bg }}
              >
                {palette.label}
              </motion.span>
              <motion.code
                animate={{
                  opacity: isActive || reduceMotion ? 1 : 0.55,
                }}
                transition={{ duration: 0.4 }}
                className="text-cc-ink truncate font-mono text-xs sm:text-sm"
              >
                {diff.text}
              </motion.code>
            </li>
          );
        })}
      </ul>
      <div className="text-cc-ink-dim mt-4 font-mono text-[11px]">
        Verified against published clients.
      </div>
    </div>
  );
}

function ComposeFanout({ reduceMotion }: { readonly reduceMotion: boolean }) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { margin: "-10% 0px", once: true });

  const leaves = [
    { x: 240, y: 30, label: "users" },
    { x: 240, y: 90, label: "orders" },
    { x: 240, y: 150, label: "billing" },
  ];
  const rootX = 50;
  const rootY = 90;

  return (
    <div ref={ref} className="p-6 sm:p-8">
      <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
        Distributed query plan
      </div>
      <svg
        viewBox="0 0 280 180"
        className="mt-4 h-48 w-full"
        aria-hidden="true"
      >
        {leaves.map((leaf, i) => {
          const d = `M${rootX + 16} ${rootY} C ${rootX + 90} ${rootY}, ${leaf.x - 80} ${leaf.y}, ${leaf.x - 16} ${leaf.y}`;
          return (
            <motion.path
              key={leaf.label}
              d={d}
              fill="none"
              stroke={ACCENT}
              strokeOpacity={0.7}
              strokeWidth={1.2}
              initial={{ pathLength: 0 }}
              animate={inView ? { pathLength: 1 } : { pathLength: 0 }}
              transition={{
                duration: reduceMotion ? 0 : 0.9,
                delay: reduceMotion ? 0 : 0.2 + i * 0.18,
                ease: "easeOut",
              }}
            />
          );
        })}

        <motion.g
          initial={{ opacity: 0 }}
          animate={inView ? { opacity: 1 } : { opacity: 0 }}
          transition={{ duration: 0.4 }}
        >
          <rect
            x={rootX - 16}
            y={rootY - 14}
            width={64}
            height={28}
            rx={6}
            fill="rgba(94,234,212,0.12)"
            stroke={ACCENT}
            strokeWidth={1}
          />
          <text
            x={rootX + 16}
            y={rootY + 4}
            textAnchor="middle"
            fontSize={9}
            fill={ACCENT}
            fontFamily="ui-monospace, monospace"
          >
            gateway
          </text>
        </motion.g>

        {leaves.map((leaf, i) => (
          <motion.g
            key={leaf.label}
            initial={{ opacity: 0, x: -8 }}
            animate={inView ? { opacity: 1, x: 0 } : { opacity: 0, x: -8 }}
            transition={{
              duration: reduceMotion ? 0 : 0.5,
              delay: reduceMotion ? 0 : 0.7 + i * 0.18,
            }}
          >
            <rect
              x={leaf.x - 16}
              y={leaf.y - 12}
              width={56}
              height={24}
              rx={5}
              fill="rgba(255,255,255,0.04)"
              stroke="rgba(94,234,212,0.5)"
              strokeWidth={1}
            />
            <text
              x={leaf.x + 12}
              y={leaf.y + 4}
              textAnchor="middle"
              fontSize={9}
              fill="#cdd9ee"
              fontFamily="ui-monospace, monospace"
            >
              {leaf.label}
            </text>
          </motion.g>
        ))}
      </svg>
    </div>
  );
}

// ─── CLOSING DIVIDER ─────────────────────────────────────────────────────────

function SpectrumHairline() {
  return (
    <motion.span
      aria-hidden="true"
      className="block h-px w-32 rounded-full"
      style={{ background: SPECTRUM, transformOrigin: "center" }}
      initial={{ scaleX: 0 }}
      whileInView={{ scaleX: 1 }}
      viewport={{ once: true, margin: "-10% 0px" }}
      transition={{ duration: 0.9, ease: "easeOut" }}
    />
  );
}

// ─── PAGE ────────────────────────────────────────────────────────────────────

export function ClientPage() {
  const reduceMotion = useReducedMotion() ?? false;

  return (
    <MotionConfig reducedMotion="user">
      {/* HERO */}
      <section className="pt-6 pb-12 text-center sm:pt-12">
        <div className="mx-auto flex max-w-3xl flex-col items-center gap-6">
          <Eyebrow>The Control Plane for GraphQL</Eyebrow>
          <h1 className="text-cc-heading font-heading text-h1 text-balance">
            Your API, in motion.
          </h1>
          <p className="lead text-cc-ink mx-auto max-w-2xl !text-xl !leading-relaxed">
            Nitro is the cockpit for your GraphQL and .NET backend. Author
            operations, watch them run, trace every request, and evolve your
            schema without breaking the clients you ship to.
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </div>

        <motion.div
          initial={{ opacity: 0, y: 28 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7, ease: "easeOut", delay: 0.15 }}
          className="mt-16 sm:mt-20"
        >
          <ControlPlaneConsole reduceMotion={reduceMotion} />
        </motion.div>
      </section>

      {/* LIVE SIGNALS */}
      <LiveSignals reduceMotion={reduceMotion} />

      {/* OBSERVE */}
      <Pillar
        id="observe"
        index="01"
        eyebrow="Observe"
        title="See exactly how your API behaves in production."
        body="Wire up Nitro and OpenTelemetry to watch latency, throughput, and error rate per operation, with p95 and p99, per-client usage, and an impact score that ranks what hurts the system most."
        visual={<ObserveMiniWaterfall reduceMotion={reduceMotion} />}
      />

      {/* TRACE (one NitroTrace embed) */}
      <Pillar
        id="trace"
        index="02"
        eyebrow="Trace"
        title="Follow one request across your whole backend."
        body="Distributed tracing stitches a single operation across GraphQL, REST, gRPC, and background jobs. Walk the span waterfall down to the resolver that ran slow."
        visual={<NitroTrace className="w-full" />}
        reverse
      />

      {/* DIAGNOSE */}
      <Pillar
        id="diagnose"
        index="03"
        eyebrow="Diagnose"
        title="From an error spike to the line that threw it."
        body="When errors climb, Nitro takes you from the spike to the exact failing operation and the server-side stack trace behind it, with no log spelunking required."
        visual={<DiagnoseSparkline reduceMotion={reduceMotion} />}
      />

      {/* EVOLVE */}
      <Pillar
        id="schema"
        index="04"
        eyebrow="Evolve"
        title="Change your schema without breaking your clients."
        body="The schema registry classifies every change as safe, dangerous, or breaking and checks it against published clients in CI, so you validate on a PR and publish only when it is safe to ship."
        visual={<EvolveSchemaList reduceMotion={reduceMotion} />}
        reverse
      />

      {/* COMPOSE */}
      <Pillar
        id="fusion"
        index="05"
        eyebrow="Compose"
        title="One graph, executed across every subgraph."
        body="With Fusion, Nitro shows the distributed query plan: how a single operation fans out into parallel, batched fetches across your subgraphs and folds back into one response."
        visual={<ComposeFanout reduceMotion={reduceMotion} />}
      />

      {/* CTA */}
      <section className="border-cc-card-border border-t py-24 text-center sm:py-32">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{ duration: 0.6, ease: "easeOut" }}
          className="mx-auto flex max-w-2xl flex-col items-center gap-6"
        >
          <SpectrumHairline />
          <Eyebrow>Ready when you are</Eyebrow>
          <h2 className="text-cc-heading font-heading text-h2 text-balance">
            Put your API on the control plane.
          </h2>
          <p className="text-cc-ink max-w-xl text-lg leading-relaxed">
            Start in the GraphQL IDE in seconds, then grow into observability,
            tracing, and a registry that keeps your schema and clients in sync.
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </motion.div>
      </section>
    </MotionConfig>
  );
}
