"use client";

import {
  MotionConfig,
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "motion/react";
import { useEffect, useRef } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ *
 * Palette. Teal is the signature accent, coral is reserved for the
 * slow span and #1 impact row. The spectrum is spent exactly once,
 * on the closing CTA hairline.
 * ------------------------------------------------------------------ */

const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";
const CYAN = "#16b9e4";

const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, ${VIOLET} 52%, ${CORAL} 100%)`;

/* ================================================================== *
 * PAGE VIEW (client tree)
 * ================================================================== */

export function AnalyticsPreviewV7View() {
  return (
    <MotionConfig reducedMotion="user">
      <main className="flex flex-col gap-28 pb-16">
        <Hero />
        <KpiStrip />
        <CenterpieceSection />
        <ImpactSection />
        <ClientUsageSection />
        <CrossServiceSection />
        <HonestySection />
        <ClosingCta />
      </main>
    </MotionConfig>
  );
}

/* ================================================================== *
 * HERO
 * ================================================================== */

function Hero() {
  return (
    <section className="relative isolate pt-8">
      <HeroGlow />
      <div className="relative mx-auto max-w-3xl text-center">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
          Nitro analytics
        </span>
        <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-6">
          The dashboard your
          <br />
          .NET API earns.
        </h1>
        <p className="lead text-cc-prose mx-auto mt-6 max-w-2xl">
          Operation, service, and per-client monitoring on top of OpenTelemetry.
          Ranked by impact, traced end to end, ready the moment you point
          telemetry at Nitro.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
          <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
            Get Started
          </SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
        <HeroSparkline />
      </div>
    </section>
  );
}

function HeroGlow() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute -top-24 left-1/2 -z-10 h-[460px] w-[820px] -translate-x-1/2 opacity-60 blur-3xl"
      style={{
        background: `radial-gradient(50% 50% at 50% 40%, ${TEAL}1f 0%, transparent 70%), radial-gradient(40% 40% at 70% 60%, ${VIOLET}1a 0%, transparent 72%)`,
      }}
    />
  );
}

function HeroSparkline() {
  // Tiny rpm sparkline that draws in on scroll-into-view.
  const points =
    "0,28 12,24 24,26 36,18 48,22 60,12 72,16 84,8 96,14 108,6 120,10 132,4 144,9 156,3";
  return (
    <div className="mx-auto mt-10 flex max-w-md items-center justify-center gap-3">
      <span className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
        rpm · last 60m
      </span>
      <svg
        width="160"
        height="32"
        viewBox="0 0 160 32"
        className="overflow-visible"
        aria-hidden
      >
        <motion.polyline
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          strokeLinecap="round"
          strokeLinejoin="round"
          points={points}
          initial={{ pathLength: 0, opacity: 0 }}
          whileInView={{ pathLength: 1, opacity: 1 }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{ duration: 1.2, ease: [0.22, 1, 0.36, 1] }}
        />
      </svg>
      <span className="text-cc-ink-dim font-mono text-[10px]">live</span>
    </div>
  );
}

/* ================================================================== *
 * KPI STRIP
 * Four tiles whose numbers tick up from 0 on view, with inline
 * sparklines that draw their paths.
 * ================================================================== */

interface KpiTile {
  readonly label: string;
  readonly target: number;
  readonly suffix: string;
  readonly format: "int" | "decimal";
  readonly spark: string;
  readonly tone: string;
}

const KPI_TILES: readonly KpiTile[] = [
  {
    label: "p95 latency",
    target: 42,
    suffix: "ms",
    format: "int",
    spark: "0,18 14,16 28,20 42,14 56,17 70,12 84,15 98,10 112,13 126,9",
    tone: TEAL,
  },
  {
    label: "p99 latency",
    target: 318,
    suffix: "ms",
    format: "int",
    spark: "0,22 14,18 28,24 42,12 56,20 70,8 84,16 98,6 112,14 126,4",
    tone: CORAL,
  },
  {
    label: "throughput",
    target: 9.4,
    suffix: "k rpm",
    format: "decimal",
    spark: "0,20 14,16 28,18 42,12 56,14 70,10 84,12 98,8 112,10 126,6",
    tone: TEAL,
  },
  {
    label: "error rate",
    target: 0.3,
    suffix: "%",
    format: "decimal",
    spark: "0,16 14,18 28,14 42,16 56,12 70,14 84,10 98,12 112,8 126,10",
    tone: AMBER,
  },
];

function KpiStrip() {
  return (
    <section>
      <div className="mx-auto max-w-2xl text-center">
        <SectionEyebrow>Live KPIs</SectionEyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
          Four numbers, drawn from real spans.
        </h2>
        <p className="text-body text-cc-ink-dim mt-5">
          Each tile reads the same OpenTelemetry pipeline that feeds the
          operations table and the traces below. No agents in the middle.
        </p>
      </div>
      <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {KPI_TILES.map((tile) => (
          <KpiCard key={tile.label} tile={tile} />
        ))}
      </div>
    </section>
  );
}

interface KpiCardProps {
  readonly tile: KpiTile;
}

function KpiCard({ tile }: KpiCardProps) {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { amount: 0.5, once: true });
  const reduce = useReducedMotion();
  const mv = useMotionValue(reduce ? tile.target : 0);
  const display = useTransform(mv, (v) =>
    tile.format === "decimal" ? v.toFixed(1) : Math.round(v).toString(),
  );

  useEffect(() => {
    if (reduce) {
      mv.set(tile.target);
      return;
    }
    if (inView) {
      const controls = animate(mv, tile.target, {
        duration: 1.1,
        ease: [0.22, 1, 0.36, 1],
      });
      return () => controls.stop();
    }
  }, [inView, mv, reduce, tile.target]);

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg rounded-xl border px-5 py-5 backdrop-blur-md"
    >
      <div className="flex items-center justify-between">
        <span className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
          {tile.label}
        </span>
        <span
          className="h-1.5 w-1.5 rounded-full"
          style={{ backgroundColor: tile.tone }}
        />
      </div>
      <div className="mt-3 flex items-baseline gap-1.5">
        <motion.span
          className="font-heading text-h4 text-cc-heading"
          style={{ color: tile.tone }}
        >
          {display}
        </motion.span>
        <span className="text-cc-ink-dim font-mono text-[12px]">
          {tile.suffix}
        </span>
      </div>
      <svg
        width="100%"
        height="22"
        viewBox="0 0 126 26"
        className="mt-3 block"
        preserveAspectRatio="none"
        aria-hidden
      >
        <motion.polyline
          fill="none"
          stroke={tile.tone}
          strokeOpacity={0.85}
          strokeWidth={1.25}
          strokeLinecap="round"
          strokeLinejoin="round"
          points={tile.spark}
          initial={{ pathLength: reduce ? 1 : 0, opacity: reduce ? 1 : 0 }}
          animate={
            inView || reduce
              ? { pathLength: 1, opacity: 1 }
              : { pathLength: 0, opacity: 0 }
          }
          transition={{ duration: 1.1, ease: [0.22, 1, 0.36, 1] }}
        />
      </svg>
    </div>
  );
}

/* ================================================================== *
 * CENTERPIECE: ANIMATED TRACE WATERFALL
 * Spans stagger-grow left-to-right; slow gRPC pulses coral; the
 * duration counter races to 318ms; axis ticks fade in along the way.
 * ================================================================== */

interface Span {
  readonly id: string;
  readonly label: string;
  readonly kind: "graphql" | "rest" | "grpc" | "job" | "db";
  readonly start: number;
  readonly width: number;
  readonly ms: string;
  readonly slow?: boolean;
}

const KIND_LABEL: Record<Span["kind"], string> = {
  graphql: "GraphQL",
  rest: "REST",
  grpc: "gRPC",
  job: "Job",
  db: "DB",
};

const KIND_COLOR: Record<Span["kind"], string> = {
  graphql: TEAL,
  rest: VIOLET,
  grpc: CORAL,
  job: "#8b9bd4",
  db: "#7dd3fc",
};

const SPANS: readonly Span[] = [
  {
    id: "s0",
    label: "mutation checkout",
    kind: "graphql",
    start: 0,
    width: 100,
    ms: "318ms",
  },
  {
    id: "s1",
    label: "api → users-svc · GET /me",
    kind: "rest",
    start: 4,
    width: 11,
    ms: "21ms",
  },
  {
    id: "s2",
    label: "users-svc → billing · Charge()",
    kind: "grpc",
    start: 16,
    width: 64,
    ms: "201ms",
    slow: true,
  },
  {
    id: "s3",
    label: "billing → db · SELECT account",
    kind: "db",
    start: 20,
    width: 12,
    ms: "9ms",
  },
  {
    id: "s4",
    label: "billing → worker · enqueue receipt",
    kind: "job",
    start: 82,
    width: 13,
    ms: "37ms",
  },
];

const SPAN_DEPTH: readonly number[] = [0, 1, 1, 2, 2];

function CenterpieceSection() {
  return (
    <section>
      <div className="mx-auto max-w-2xl text-center">
        <SectionEyebrow>One trace, end to end</SectionEyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
          Watch a single request thread the whole stack.
        </h2>
        <p className="text-body text-cc-ink-dim mt-5">
          The waterfall draws itself span by span. GraphQL at the root, then the
          REST and gRPC hops across your services, the database read, and the
          background job enqueued at the end. The slow hop is the one that
          explains the p99.
        </p>
      </div>
      <TraceWaterfall />
      <TraceLegend />
    </section>
  );
}

function TraceWaterfall() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { amount: 0.4, once: true });
  const reduce = useReducedMotion();

  const duration = useMotionValue(reduce ? 318 : 0);
  const durationLabel = useTransform(duration, (v) => `${Math.round(v)}ms`);

  useEffect(() => {
    if (reduce) {
      duration.set(318);
      return;
    }
    if (inView) {
      const controls = animate(duration, 318, {
        duration: 1.2,
        ease: [0.22, 1, 0.36, 1],
      });
      return () => controls.stop();
    }
  }, [inView, duration, reduce]);

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-5xl overflow-hidden rounded-2xl border backdrop-blur-md"
    >
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3">
        <span className="text-cc-nav-label font-mono text-[11px]">trace</span>
        <span className="font-mono text-[11px]" style={{ color: TEAL }}>
          4b1c8f2a9e07
        </span>
        <span className="text-cc-ink-faint font-mono text-[11px]">·</span>
        <span className="text-cc-ink-dim font-mono text-[11px]">
          mutation checkout
        </span>
        <span className="text-cc-nav-label ml-auto inline-flex items-center gap-1.5 font-mono text-[11px]">
          duration{" "}
          <motion.span className="text-cc-heading">{durationLabel}</motion.span>
        </span>
      </div>
      <div className="px-5 py-5">
        <div className="space-y-2.5">
          {SPANS.map((span, i) => (
            <SpanRow
              key={span.id}
              span={span}
              depth={SPAN_DEPTH[i] ?? 0}
              index={i}
              inView={inView}
              reduce={reduce ?? false}
            />
          ))}
        </div>
        <AxisTicks inView={inView} reduce={reduce ?? false} />
      </div>
    </div>
  );
}

interface SpanRowProps {
  readonly span: Span;
  readonly depth: number;
  readonly index: number;
  readonly inView: boolean;
  readonly reduce: boolean;
}

function SpanRow({ span, depth, index, inView, reduce }: SpanRowProps) {
  const color = KIND_COLOR[span.kind];
  const isRoot = span.kind === "graphql";
  const delay = reduce ? 0 : index * 0.18;
  const duration = reduce ? 0 : 0.7;
  const shown = inView || reduce;

  return (
    <div className="flex items-center gap-3">
      <div
        className="flex w-[38%] shrink-0 items-center gap-2 truncate"
        style={{ paddingLeft: depth * 14 }}
      >
        <motion.span
          className="rounded px-1.5 py-0.5 font-mono text-[9px] font-semibold tracking-wide uppercase"
          style={{ color, backgroundColor: `${color}1a` }}
          initial={{ opacity: reduce ? 1 : 0 }}
          animate={{ opacity: shown ? 1 : 0 }}
          transition={{ delay, duration: reduce ? 0 : 0.35 }}
        >
          {KIND_LABEL[span.kind]}
        </motion.span>
        <motion.span
          className={`truncate font-mono text-[12px] ${isRoot ? "text-cc-heading" : "text-cc-ink-dim"}`}
          initial={{ opacity: reduce ? 1 : 0, x: reduce ? 0 : -6 }}
          animate={{
            opacity: shown ? 1 : 0,
            x: shown ? 0 : -6,
          }}
          transition={{ delay, duration: reduce ? 0 : 0.45 }}
        >
          {span.label}
        </motion.span>
      </div>
      <div className="bg-cc-surface/60 relative h-6 flex-1 rounded">
        <motion.div
          className="absolute top-1/2 flex h-4 -translate-y-1/2 items-center overflow-hidden rounded-[3px]"
          style={{
            left: `${span.start}%`,
            backgroundColor: span.slow ? CORAL : color,
            opacity: span.slow ? 1 : 0.78,
          }}
          initial={{ width: reduce ? `${span.width}%` : 0 }}
          animate={{
            width: shown ? `${span.width}%` : 0,
          }}
          transition={{
            delay,
            duration,
            ease: [0.22, 1, 0.36, 1],
          }}
        >
          {span.slow && (
            <motion.div
              aria-hidden
              className="absolute inset-0 rounded-[3px]"
              animate={
                reduce
                  ? { boxShadow: `0 0 16px ${CORAL}66` }
                  : {
                      boxShadow: [
                        `0 0 0px ${CORAL}00`,
                        `0 0 22px ${CORAL}99`,
                        `0 0 14px ${CORAL}66`,
                      ],
                    }
              }
              transition={
                reduce
                  ? { duration: 0 }
                  : {
                      delay: delay + duration,
                      duration: 1.6,
                      repeat: Infinity,
                      repeatType: "reverse",
                    }
              }
            />
          )}
          {span.slow && (
            <motion.span
              className="text-cc-surface relative ml-2 font-mono text-[10px] font-semibold whitespace-nowrap"
              initial={{ opacity: reduce ? 1 : 0 }}
              animate={{ opacity: shown ? 1 : 0 }}
              transition={{
                delay: delay + duration + 0.1,
                duration: reduce ? 0 : 0.4,
              }}
            >
              billing.Charge() 201ms
            </motion.span>
          )}
        </motion.div>
        <motion.span
          className="text-cc-nav-label absolute top-1/2 -translate-y-1/2 font-mono text-[10px]"
          style={{ left: `calc(${span.start + span.width}% + 8px)` }}
          initial={{ opacity: reduce ? 1 : 0 }}
          animate={{ opacity: shown ? 1 : 0 }}
          transition={{
            delay: delay + duration * 0.6,
            duration: reduce ? 0 : 0.4,
          }}
        >
          {span.ms}
        </motion.span>
      </div>
    </div>
  );
}

interface AxisTicksProps {
  readonly inView: boolean;
  readonly reduce: boolean;
}

function AxisTicks({ inView, reduce }: AxisTicksProps) {
  const ticks = [
    { label: "0ms", delay: 0.0 },
    { label: "100ms", delay: 0.35 },
    { label: "200ms", delay: 0.75 },
    { label: "318ms", delay: 1.05 },
  ];
  return (
    <div className="border-cc-card-border/50 text-cc-nav-label mt-5 ml-[38%] flex items-center justify-between border-t pt-2 font-mono text-[10px]">
      {ticks.map((t) => (
        <motion.span
          key={t.label}
          initial={{ opacity: reduce ? 1 : 0 }}
          animate={{ opacity: inView || reduce ? 1 : 0 }}
          transition={{ delay: reduce ? 0 : t.delay, duration: 0.3 }}
        >
          {t.label}
        </motion.span>
      ))}
    </div>
  );
}

function TraceLegend() {
  const items: readonly Span["kind"][] = [
    "graphql",
    "rest",
    "grpc",
    "db",
    "job",
  ];
  return (
    <div className="mt-6 flex flex-wrap items-center justify-center gap-3">
      {items.map((kind, i) => {
        const color = KIND_COLOR[kind];
        return (
          <motion.span
            key={kind}
            className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 font-mono text-[10px] tracking-wide uppercase"
            style={{ color, backgroundColor: `${color}14` }}
            initial={{ opacity: 0, y: 6 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{ delay: i * 0.08, duration: 0.4 }}
          >
            <span
              className="h-1.5 w-1.5 rounded-full"
              style={{ backgroundColor: color }}
            />
            {KIND_LABEL[kind]}
          </motion.span>
        );
      })}
    </div>
  );
}

/* ================================================================== *
 * IMPACT SECTION
 * Ranked operations. Rows slide in from the right; impact bars
 * animate width from 0 to value; the #1 coral row glows on completion.
 * ================================================================== */

interface OpRow {
  readonly rank: number;
  readonly name: string;
  readonly p95: string;
  readonly p99: string;
  readonly rpm: string;
  readonly errRate: string;
  readonly impact: number;
  readonly status: "ok" | "warn" | "fire";
}

const OP_ROWS: readonly OpRow[] = [
  {
    rank: 1,
    name: "checkout",
    p95: "42ms",
    p99: "318ms",
    rpm: "9.4k",
    errRate: "0.3%",
    impact: 94,
    status: "fire",
  },
  {
    rank: 2,
    name: "cartSummary",
    p95: "31ms",
    p99: "88ms",
    rpm: "12.1k",
    errRate: "0.1%",
    impact: 71,
    status: "warn",
  },
  {
    rank: 3,
    name: "productList",
    p95: "12ms",
    p99: "27ms",
    rpm: "18.6k",
    errRate: "0.0%",
    impact: 38,
    status: "ok",
  },
  {
    rank: 4,
    name: "userProfile",
    p95: "8ms",
    p99: "19ms",
    rpm: "5.2k",
    errRate: "0.0%",
    impact: 22,
    status: "ok",
  },
];

const STATUS_COLOR: Record<OpRow["status"], string> = {
  ok: GREEN,
  warn: AMBER,
  fire: CORAL,
};

function ImpactSection() {
  return (
    <section className="grid gap-10 lg:grid-cols-[1fr_1.4fr] lg:items-center">
      <div>
        <SectionEyebrow>Operation insights</SectionEyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
          Ranked by what hurts, not what is loud.
        </h2>
        <p className="text-body text-cc-ink-dim mt-5">
          The Nitro impact score combines p95, p99, throughput, and error rate
          into one number, so the operation at the top of the table is the one
          worth opening first. Then drill: p95 and p99 distributions,
          throughput, and 5xx share, side by side.
        </p>
        <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
          <CheckLine>
            p95 and p99 latency with real distributions, not averages
          </CheckLine>
          <CheckLine>
            Throughput and error rate as siblings, never separated
          </CheckLine>
          <CheckLine>
            Impact score surfaces the operation that owes you attention
          </CheckLine>
        </ul>
      </div>
      <OperationsTable />
    </section>
  );
}

function OperationsTable() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { amount: 0.3, once: true });
  const reduce = useReducedMotion();

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md"
    >
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center gap-2 border-b px-4 py-2.5">
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="text-cc-nav-label ml-2 font-mono text-[11px]">
          nitro · operations
        </span>
        <span className="text-cc-nav-label ml-auto font-mono text-[10px] tracking-wide uppercase">
          sorted by impact
        </span>
      </div>
      <div className="text-cc-nav-label border-cc-card-border/50 grid grid-cols-[28px_1fr_56px_56px_56px_56px_60px] gap-3 border-b px-4 py-2.5 font-mono text-[10px] tracking-wide uppercase">
        <span />
        <span>operation</span>
        <span className="text-right">p95</span>
        <span className="text-right">p99</span>
        <span className="text-right">rpm</span>
        <span className="text-right">err</span>
        <span className="text-right">impact</span>
      </div>
      <div className="divide-cc-card-border/40 divide-y">
        {OP_ROWS.map((row, i) => (
          <OperationRow
            key={row.name}
            row={row}
            index={i}
            inView={inView}
            reduce={reduce ?? false}
          />
        ))}
      </div>
    </div>
  );
}

interface OperationRowProps {
  readonly row: OpRow;
  readonly index: number;
  readonly inView: boolean;
  readonly reduce: boolean;
}

function OperationRow({ row, index, inView, reduce }: OperationRowProps) {
  const isHot = row.status === "fire";
  const delay = reduce ? 0 : index * 0.12;
  const shown = inView || reduce;

  return (
    <motion.div
      className={`grid grid-cols-[28px_1fr_56px_56px_56px_56px_60px] items-center gap-3 px-4 py-3 font-mono text-[12px] ${
        isHot ? "bg-cc-surface/80" : "bg-cc-surface/40"
      }`}
      initial={{ opacity: reduce ? 1 : 0, x: reduce ? 0 : 24 }}
      animate={{
        opacity: shown ? 1 : 0,
        x: shown ? 0 : 24,
        boxShadow:
          shown && isHot
            ? `inset 0 0 0 1px ${CORAL}55`
            : `inset 0 0 0 0px ${CORAL}00`,
      }}
      transition={{ delay, duration: reduce ? 0 : 0.55 }}
    >
      <span className="text-cc-nav-label text-[11px]">#{row.rank}</span>
      <span className="flex items-center gap-2">
        <StatusDot
          color={STATUS_COLOR[row.status]}
          pulse={row.status === "fire"}
        />
        <span className={isHot ? "text-cc-heading" : "text-cc-ink-dim"}>
          {row.name}
        </span>
      </span>
      <span className="text-cc-ink-dim text-right">{row.p95}</span>
      <span
        className="text-right"
        style={{ color: isHot ? CORAL : "var(--color-cc-ink-dim)" }}
      >
        {row.p99}
      </span>
      <span className="text-cc-ink-dim text-right">{row.rpm}</span>
      <span
        className="text-right"
        style={{
          color:
            row.status === "fire"
              ? CORAL
              : row.status === "warn"
                ? AMBER
                : "var(--color-cc-ink-dim)",
        }}
      >
        {row.errRate}
      </span>
      <span className="flex items-center justify-end gap-1.5">
        <ImpactBar
          value={row.impact}
          status={row.status}
          inView={shown}
          delay={delay + (reduce ? 0 : 0.2)}
          reduce={reduce}
        />
        <span className="text-cc-ink-dim w-6 text-right text-[11px]">
          {row.impact}
        </span>
      </span>
    </motion.div>
  );
}

interface ImpactBarProps {
  readonly value: number;
  readonly status: OpRow["status"];
  readonly inView: boolean;
  readonly delay: number;
  readonly reduce: boolean;
}

function ImpactBar({ value, status, inView, delay, reduce }: ImpactBarProps) {
  return (
    <span className="bg-cc-surface/80 relative inline-block h-1.5 w-12 overflow-hidden rounded-full">
      <motion.span
        className="absolute inset-y-0 left-0 rounded-full"
        style={{ backgroundColor: STATUS_COLOR[status] }}
        initial={{ width: reduce ? `${value}%` : 0 }}
        animate={{ width: inView ? `${value}%` : 0 }}
        transition={{
          delay,
          duration: reduce ? 0 : 0.7,
          ease: [0.22, 1, 0.36, 1],
        }}
      />
    </span>
  );
}

/* ================================================================== *
 * CLIENT USAGE SECTION
 * Horizontal stacked share bar segments grow in sequence, then per-row
 * bars fill in turn.
 * ================================================================== */

interface ClientRow {
  readonly name: string;
  readonly share: number;
  readonly rpm: string;
  readonly status: "ok" | "warn" | "fire";
}

const CLIENT_ROWS: readonly ClientRow[] = [
  { name: "web-storefront@4.2.0", share: 61, rpm: "5.7k", status: "fire" },
  { name: "ios-app@3.8.1", share: 27, rpm: "2.5k", status: "warn" },
  { name: "android-app@3.5.0", share: 9, rpm: "0.8k", status: "ok" },
  { name: "partner-api@1.0", share: 3, rpm: "0.4k", status: "ok" },
];

function ClientUsageSection() {
  return (
    <section className="grid gap-10 lg:grid-cols-[1fr_1.4fr] lg:items-start">
      <div>
        <SectionEyebrow>Per-client usage</SectionEyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
          Which published clients call this, and how often.
        </h2>
        <p className="text-body text-cc-ink-dim mt-5">
          Nitro registers your clients by name and version. The same telemetry
          that fuels the operations table breaks down by caller, so you can see
          published clients affected before you ship a fix, and which versions
          are still out there hitting the deprecated field.
        </p>
        <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
          <CheckLine>
            Per-client share of traffic, latency, and errors
          </CheckLine>
          <CheckLine>
            Version-aware: storefront@4.2.0 vs storefront@4.1.x, side by side
          </CheckLine>
          <CheckLine>
            Ties cleanly into the schema registry for deprecation work
          </CheckLine>
        </ul>
      </div>
      <ClientShareTile />
    </section>
  );
}

function ClientShareTile() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { amount: 0.3, once: true });
  const reduce = useReducedMotion();

  return (
    <div
      ref={ref}
      className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md"
    >
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
          clients · checkout · 1h
        </span>
        <span className="text-cc-ink-faint font-mono text-[10px]">nitro</span>
      </div>
      <div className="px-5 py-5">
        <div className="flex h-2 overflow-hidden rounded-full">
          {CLIENT_ROWS.map((c, i) => (
            <motion.span
              key={c.name}
              style={{
                backgroundColor: STATUS_COLOR[c.status],
                opacity: c.status === "ok" ? 0.55 : 1,
              }}
              initial={{ width: reduce ? `${c.share}%` : 0 }}
              animate={{
                width: inView || reduce ? `${c.share}%` : 0,
              }}
              transition={{
                delay: reduce ? 0 : 0.1 + i * 0.18,
                duration: reduce ? 0 : 0.55,
                ease: [0.22, 1, 0.36, 1],
              }}
            />
          ))}
        </div>
        <div className="mt-5 space-y-2">
          {CLIENT_ROWS.map((c, i) => (
            <motion.div
              key={c.name}
              className="flex items-center gap-3 rounded-lg px-2.5 py-2"
              style={{
                backgroundColor:
                  c.status === "fire"
                    ? "rgba(240, 120, 106, 0.07)"
                    : "rgba(12, 19, 34, 0.4)",
              }}
              initial={{ opacity: reduce ? 1 : 0, y: reduce ? 0 : 6 }}
              animate={{
                opacity: inView || reduce ? 1 : 0,
                y: inView || reduce ? 0 : 6,
              }}
              transition={{
                delay: reduce ? 0 : 0.4 + i * 0.12,
                duration: reduce ? 0 : 0.4,
              }}
            >
              <StatusDot color={STATUS_COLOR[c.status]} />
              <span
                className={`flex-1 truncate font-mono text-[12px] ${
                  c.status === "fire" ? "text-cc-heading" : "text-cc-ink-dim"
                }`}
              >
                {c.name}
              </span>
              <ShareBar
                share={c.share}
                status={c.status}
                inView={inView || reduce === true}
                delay={reduce ? 0 : 0.55 + i * 0.12}
                reduce={reduce ?? false}
              />
              <span className="text-cc-nav-label w-12 text-right font-mono text-[11px]">
                {c.share}%
              </span>
              <span className="text-cc-ink-dim w-14 text-right font-mono text-[11px]">
                {c.rpm}
              </span>
            </motion.div>
          ))}
        </div>
        <p className="text-cc-nav-label mt-4 font-mono text-[11px]">
          Drill into any client to see which operations and which versions of
          the schema it touches.
        </p>
      </div>
    </div>
  );
}

interface ShareBarProps {
  readonly share: number;
  readonly status: ClientRow["status"];
  readonly inView: boolean;
  readonly delay: number;
  readonly reduce: boolean;
}

function ShareBar({ share, status, inView, delay, reduce }: ShareBarProps) {
  return (
    <span className="bg-cc-surface/80 relative inline-block h-1.5 w-20 overflow-hidden rounded-full">
      <motion.span
        className="absolute inset-y-0 left-0 rounded-full"
        style={{ backgroundColor: STATUS_COLOR[status] }}
        initial={{ width: reduce ? `${share}%` : 0 }}
        animate={{ width: inView ? `${share}%` : 0 }}
        transition={{
          delay,
          duration: reduce ? 0 : 0.6,
          ease: [0.22, 1, 0.36, 1],
        }}
      />
    </span>
  );
}

/* ================================================================== *
 * CROSS-SERVICE SECTION
 * Four service-kind tiles fade-and-rise on scroll. A 1px progress
 * bar inside each tile draws to full width.
 * ================================================================== */

interface ServiceKind {
  readonly key: Span["kind"];
  readonly label: string;
  readonly note: string;
  readonly count: string;
}

const SERVICE_KINDS: readonly ServiceKind[] = [
  {
    key: "graphql",
    label: "GraphQL",
    note: "Hot Chocolate, source-generated",
    count: "1 gateway",
  },
  {
    key: "rest",
    label: "REST APIs",
    note: "ASP.NET Core endpoints",
    count: "6 services",
  },
  {
    key: "grpc",
    label: "gRPC",
    note: "service-to-service calls",
    count: "4 services",
  },
  {
    key: "job",
    label: "Background jobs",
    note: "queued work, scheduled tasks",
    count: "3 workers",
  },
];

function CrossServiceSection() {
  return (
    <section>
      <SectionEyebrow>Cross-service .NET monitoring</SectionEyebrow>
      <div className="mt-5 grid gap-10 lg:grid-cols-[1fr_1.2fr] lg:items-start">
        <div>
          <h2 className="font-heading text-h4 text-cc-heading sm:text-h3">
            One pane for every .NET service the trace touches.
          </h2>
          <p className="text-body text-cc-ink-dim mt-5">
            The OpenTelemetry pipeline is the same across GraphQL, REST, gRPC,
            and your background workers, so a single trace can carry the whole
            call. Nitro ingests it, ranks it, and lets you ask the operation and
            the service the same questions.
          </p>
          <p className="text-body text-cc-ink-dim mt-4">
            Wire your services through{" "}
            <code className="text-cc-ink font-mono">
              ChilliCream.Nitro.OpenTelemetry
            </code>
            . The dashboards above show every kind side by side, no proprietary
            agent in the middle.
          </p>
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          {SERVICE_KINDS.map((s, i) => (
            <ServiceKindTile key={s.key} kind={s} index={i} />
          ))}
        </div>
      </div>
    </section>
  );
}

interface ServiceKindTileProps {
  readonly kind: ServiceKind;
  readonly index: number;
}

function ServiceKindTile({ kind, index }: ServiceKindTileProps) {
  const color = KIND_COLOR[kind.key];
  return (
    <motion.div
      className="border-cc-card-border bg-cc-card-bg rounded-xl border px-4 py-4 backdrop-blur-md"
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10% 0px" }}
      transition={{ delay: index * 0.1, duration: 0.5 }}
    >
      <div className="flex items-center justify-between">
        <span
          className="rounded px-1.5 py-0.5 font-mono text-[10px] font-semibold tracking-wide uppercase"
          style={{ color, backgroundColor: `${color}1a` }}
        >
          {kind.label}
        </span>
        <span className="text-cc-nav-label font-mono text-[10px]">
          {kind.count}
        </span>
      </div>
      <p className="text-cc-ink-dim mt-3 text-[13px]">{kind.note}</p>
      <div className="bg-cc-surface/60 mt-3 h-[1px] overflow-hidden rounded-full">
        <motion.span
          className="block h-full"
          style={{ backgroundColor: color, opacity: 0.7 }}
          initial={{ width: 0 }}
          whileInView={{ width: "100%" }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{
            delay: index * 0.1 + 0.25,
            duration: 0.8,
            ease: [0.22, 1, 0.36, 1],
          }}
        />
      </div>
    </motion.div>
  );
}

/* ================================================================== *
 * HONESTY BAND
 * Three plain cards with a subtle opacity/translate reveal.
 * ================================================================== */

function HonestySection() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg rounded-2xl border px-6 py-9 backdrop-blur-md sm:px-10">
      <SectionEyebrow>Straight about what it is</SectionEyebrow>
      <h2 className="font-heading text-h4 text-cc-heading mt-5 max-w-2xl">
        Honest about the setup, precise about the payoff.
      </h2>
      <div className="mt-8 grid gap-6 md:grid-cols-3">
        <HonestyCard index={0} title="Telemetry needs Nitro configuration">
          The dashboards above come from telemetry you point at Nitro. It is a
          configuration step in your services, deliberate and documented, not
          something that turns on by itself.
        </HonestyCard>
        <HonestyCard index={1} title="An open standard underneath">
          It is OpenTelemetry end to end. Vendor-neutral spans mean your data is
          yours, and there is no proprietary agent locking the trace in.
        </HonestyCard>
        <HonestyCard index={2} title="The IDE is a separate thing">
          The GraphQL IDE can be served from your Hot Chocolate endpoint. That
          is independent of the telemetry dashboards here. Two facts, kept
          apart.
        </HonestyCard>
      </div>
    </section>
  );
}

interface HonestyCardProps {
  readonly title: string;
  readonly index: number;
  readonly children: React.ReactNode;
}

function HonestyCard({ title, index, children }: HonestyCardProps) {
  return (
    <motion.div
      className="border-cc-card-border/70 bg-cc-surface/50 rounded-xl border px-5 py-5"
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10% 0px" }}
      transition={{ delay: index * 0.1, duration: 0.5 }}
    >
      <div className="flex items-center gap-2">
        <span style={{ color: TEAL }}>
          <CheckIcon size={15} />
        </span>
        <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
      </div>
      <p className="text-caption text-cc-ink-dim mt-3">{children}</p>
    </motion.div>
  );
}

/* ================================================================== *
 * CLOSING CTA
 * The single spectrum event for the page: a hairline at the top of
 * the card draws left-to-right via scaleX on view.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-surface/80 relative overflow-hidden rounded-2xl border px-6 py-14 text-center backdrop-blur-md sm:px-12">
      <motion.div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-px origin-left"
        style={{ background: SPECTRUM }}
        initial={{ scaleX: 0 }}
        whileInView={{ scaleX: 1 }}
        viewport={{ once: true, margin: "-10% 0px" }}
        transition={{ duration: 1.2, ease: [0.22, 1, 0.36, 1] }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute -bottom-24 left-1/2 h-64 w-[680px] -translate-x-1/2 opacity-25 blur-3xl"
        style={{ background: SPECTRUM }}
      />
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Nitro analytics
      </span>
      <h2 className="font-heading text-h3 text-cc-heading sm:text-h2 mt-5">
        The dashboard is the answer.
      </h2>
      <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-xl">
        Point OpenTelemetry at Nitro once and every request becomes evidence:
        ranked by impact, traced end to end, sliced by client and by service.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Get Started
        </SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch
        </OutlineButton>
      </div>
    </section>
  );
}

/* ================================================================== *
 * Shared primitives
 * ================================================================== */

interface SectionEyebrowProps {
  readonly children: React.ReactNode;
}

function SectionEyebrow({ children }: SectionEyebrowProps) {
  return (
    <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
      {children}
    </span>
  );
}

interface StatusDotProps {
  readonly color: string;
  readonly pulse?: boolean;
}

function StatusDot({ color, pulse }: StatusDotProps) {
  return (
    <span className="relative inline-flex h-2 w-2 shrink-0">
      {pulse && (
        <span
          className="absolute inline-flex h-full w-full rounded-full opacity-60 motion-safe:animate-ping"
          style={{ backgroundColor: color }}
        />
      )}
      <span
        className="relative inline-flex h-2 w-2 rounded-full"
        style={{ backgroundColor: color }}
      />
    </span>
  );
}

interface CheckLineProps {
  readonly children: React.ReactNode;
}

function CheckLine({ children }: CheckLineProps) {
  return (
    <li className="flex items-start gap-2.5">
      <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
        <CheckIcon size={14} />
      </span>
      <span>{children}</span>
    </li>
  );
}
