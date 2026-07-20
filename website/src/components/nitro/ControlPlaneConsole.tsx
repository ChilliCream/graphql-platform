"use client";

import {
  animate,
  AnimatePresence,
  motion,
  useInView,
  useMotionValue,
  useTransform,
} from "motion/react";
import { useEffect, useRef, useState } from "react";

import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { MockWindowChrome } from "@/src/components/MockWindowChrome";
import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

const ACCENT = "#5eead4";

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
    const controls = animate(value, target, { duration: 1.2, ease: "easeOut" });
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
    <Card className="rounded-lg px-4 py-3">
      <Eyebrow color="ink-dim" className="text-[10px]">
        {label}
      </Eyebrow>
      <div className="text-cc-heading font-heading mt-1 flex items-baseline gap-1">
        <span className="text-2xl tabular-nums">{rendered}</span>
        <span className="text-cc-ink-dim text-xs">{suffix}</span>
      </div>
    </Card>
  );
}

interface SparklineProps {
  readonly inView: boolean;
  readonly reduceMotion: boolean;
}

function Sparkline({ inView, reduceMotion }: SparklineProps) {
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

  const travel = useMotionValue(0);
  useEffect(() => {
    if (!inView || reduceMotion) {
      return;
    }
    const controls = animate(travel, 1, {
      duration: 4,
      repeat: Infinity,
      repeatType: "loop",
      ease: "linear",
    });
    return () => controls.stop();
  }, [inView, reduceMotion, travel]);

  const lastIndex = basePoints.length - 1;
  const pointAt = (t: number, axis: 0 | 1) => {
    const scaled = t * lastIndex;
    const i = Math.min(Math.max(Math.floor(scaled), 0), lastIndex - 1);
    const f = scaled - i;
    return (
      basePoints[i][axis] + (basePoints[i + 1][axis] - basePoints[i][axis]) * f
    );
  };
  const tipX = useTransform(travel, (t) => pointAt(t, 0));
  const tipY = useTransform(travel, (t) => pointAt(t, 1));
  const pulseOpacity = useTransform(travel, [0, 0.12, 0.85, 1], [0, 1, 1, 0]);

  return (
    <svg
      viewBox="0 0 120 36"
      className="h-16 w-full"
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      <defs>
        <linearGradient id="cc-cpc-spark-fill" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={ACCENT} stopOpacity="0.35" />
          <stop offset="100%" stopColor={ACCENT} stopOpacity="0" />
        </linearGradient>
        <filter
          id="cc-cpc-spark-glow"
          x="-50%"
          y="-50%"
          width="200%"
          height="200%"
        >
          <feGaussianBlur stdDeviation="1.6" />
        </filter>
      </defs>
      <motion.path
        d={`${d} L120 36 L0 36 Z`}
        fill="url(#cc-cpc-spark-fill)"
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
        <motion.g style={{ opacity: pulseOpacity }}>
          <motion.circle
            r={3.5}
            fill={ACCENT}
            fillOpacity={0.45}
            filter="url(#cc-cpc-spark-glow)"
            cx={tipX}
            cy={tipY}
          />
          <motion.circle r={1.8} fill={ACCENT} cx={tipX} cy={tipY} />
        </motion.g>
      )}
    </svg>
  );
}

interface WaterfallProps {
  readonly inView: boolean;
  readonly reduceMotion: boolean;
}

function Waterfall({ inView, reduceMotion }: WaterfallProps) {
  const lanes = [
    { start: 0, width: 96, label: "gateway" },
    { start: 6, width: 64, label: "users" },
    { start: 22, width: 48, label: "orders" },
    { start: 30, width: 38, label: "billing" },
    { start: 54, width: 28, label: "ledger" },
  ];
  const height = 88;
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
    <Card className="flex items-center gap-3 rounded-lg px-4 py-3">
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
    </Card>
  );
}

interface ControlPlaneConsoleProps {
  readonly className?: string;
}

export function ControlPlaneConsole({ className }: ControlPlaneConsoleProps) {
  const reduceMotion = useReducedMotionPreference();
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { margin: "-10% 0px" });

  return (
    <div ref={ref} className={className}>
      <MockWindowChrome
        header={{ variant: "status-dot" }}
        label="Nitro / production"
        headerRight={
          <span className="text-cc-ink-dim font-mono text-[10px]">live</span>
        }
        glow={{
          background:
            "radial-gradient(50% 50% at 50% 30%, rgba(94,234,212,0.16), transparent 70%)",
          inset: "-inset-x-10 -inset-y-8",
          blur: "blur-3xl",
          rounded: "rounded-[2.5rem]",
        }}
      >
        <div className="grid gap-4 p-4 sm:p-6 lg:grid-cols-12">
          <div className="border-cc-card-border bg-cc-card-bg rounded-lg border p-4 lg:col-span-7">
            <div className="flex items-center justify-between">
              <div className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
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
              <div className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
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
      </MockWindowChrome>
    </div>
  );
}
