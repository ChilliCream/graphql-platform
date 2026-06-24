"use client";

import { MotionConfig, motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ *
 * Palette. The page runs on a single cc-accent teal rail. The brand
 * spectrum is spent exactly once, in the closing CTA phrase.
 * ------------------------------------------------------------------ */

const TEAL = "#5eead4";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";
const CYAN = "#16b9e4";

const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, ${VIOLET} 52%, ${CORAL} 100%)`;

/* ================================================================== *
 * PAGE VIEW
 * The Signal Column: everything funnels through a single narrow
 * max-w-xl reading column down the dead-center of the viewport, with
 * one vertical teal rail running floor-to-ceiling as the spine.
 * ================================================================== */

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      <main className="relative isolate overflow-hidden pb-24">
        <SignalRail />
        <div className="relative flex flex-col gap-32 pt-16">
          <Hero />
          <LiveStatStrip />
          <OperationImpact />
          <TraceMini />
          <ClientUsage />
          <CrossServiceLine />
          <HonestyNote />
          <ClosingCta />
        </div>
      </main>
    </MotionConfig>
  );
}

/* ================================================================== *
 * SIGNAL RAIL
 * A single 1px vertical accent line down the column's left edge, with
 * a soft outer glow and a teal pulse that travels top to bottom on a
 * 7s infinite loop. Reduced-motion users get a static rail.
 * ================================================================== */

function SignalRail() {
  const reduced = useReducedMotion();
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0 -z-10">
      {/* Outer soft glow band */}
      <div
        className="absolute top-0 bottom-0 left-1/2 -translate-x-[18rem] opacity-80"
        style={{
          width: "48px",
          marginLeft: "-23px",
          background: `radial-gradient(ellipse 24px 60% at center, ${TEAL}2e 0%, transparent 70%)`,
        }}
      />
      {/* Crisp 1px rail */}
      <div
        className="absolute top-0 bottom-0 left-1/2 w-px -translate-x-[18rem]"
        style={{
          backgroundColor: TEAL,
          opacity: 0.7,
          boxShadow: `0 0 8px ${TEAL}66`,
        }}
      />
      {/* Traveling pulse */}
      {!reduced && (
        <motion.div
          className="absolute left-1/2 -translate-x-[18rem]"
          style={{
            width: "1px",
            height: "80px",
            marginLeft: "-0.5px",
            background: `linear-gradient(to bottom, transparent, ${TEAL}, transparent)`,
            boxShadow: `0 0 16px ${TEAL}cc, 0 0 32px ${TEAL}66`,
            top: 0,
          }}
          animate={{ y: ["-5%", "105vh"] }}
          transition={{
            duration: 7,
            repeat: Infinity,
            ease: "linear",
          }}
        />
      )}
    </div>
  );
}

/* ================================================================== *
 * COLUMN PRIMITIVES
 * Every section uses the same mx-auto max-w-xl column. The rail sits
 * 18rem left of center; the column starts there too, so the rail is
 * the column's left edge.
 * ================================================================== */

interface ColumnProps {
  readonly children: React.ReactNode;
  readonly className?: string;
}

function Column({ children, className }: ColumnProps) {
  return (
    <div
      className={`mx-auto max-w-xl px-6 ${className ?? ""}`}
      style={{ marginLeft: "max(1.5rem, calc(50% - 18rem))" }}
    >
      {children}
    </div>
  );
}

interface FrameMarkerProps {
  readonly stamp: string;
}

function FrameMarker({ stamp }: FrameMarkerProps) {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute top-0 left-1/2 -translate-x-[18rem]"
      style={{ width: "1px" }}
    >
      <div className="relative">
        {/* tick hairline crossing the rail */}
        <span
          className="absolute top-1.5 block h-px w-[14px]"
          style={{
            left: "-6px",
            backgroundColor: "rgba(245, 241, 234, 0.32)",
          }}
        />
        {/* mono timestamp to the left of the rail */}
        <span
          className="text-cc-nav-label absolute top-0 font-mono text-[10px] tracking-wider"
          style={{ right: "12px", whiteSpace: "nowrap" }}
        >
          {stamp}
        </span>
      </div>
    </div>
  );
}

interface FrameSectionProps {
  readonly stamp: string;
  readonly children: React.ReactNode;
}

function FrameSection({ stamp, children }: FrameSectionProps) {
  return (
    <section className="relative">
      <FrameMarker stamp={stamp} />
      <Column className="pt-6">{children}</Column>
    </section>
  );
}

/* ================================================================== *
 * HERO  ·  frame 00:00
 * ================================================================== */

function Hero() {
  return (
    <FrameSection stamp="00:00">
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Nitro analytics
      </span>
      <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-6 leading-[1.05]">
        One column of signal for your .NET GraphQL API.
      </h1>
      <p className="lead text-cc-prose mt-6">
        Ranked impact, distributed traces, per-client usage, all in a single
        OpenTelemetry-native feed. Read the API top to bottom, p50 through p99,
        in one narrow column of truth.
      </p>
      <div className="mt-9 flex flex-wrap items-center gap-4">
        <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Get Started
        </SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch
        </OutlineButton>
      </div>
    </FrameSection>
  );
}

/* ================================================================== *
 * LIVE STAT STRIP  ·  frame 00:08
 * Three stacked stat rows in the column, tick-in once on view.
 * ================================================================== */

interface StatRow {
  readonly label: string;
  readonly value: string;
  readonly unit: string;
  readonly note: string;
}

const STAT_ROWS: readonly StatRow[] = [
  {
    label: "p95 latency",
    value: "42",
    unit: "ms",
    note: "checkout · 1h",
  },
  {
    label: "p99 latency",
    value: "318",
    unit: "ms",
    note: "checkout · 1h",
  },
  {
    label: "impact score",
    value: "94",
    unit: "/100",
    note: "ranked #1 today",
  },
];

function LiveStatStrip() {
  return (
    <FrameSection stamp="00:08">
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Live signal
      </span>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
        The numbers, ticked in.
      </h2>
      <p className="text-body text-cc-ink-dim mt-5">
        p95, p99, and the impact score, lifted from real OpenTelemetry spans.
        Each row is a frame in the feed, stacked, not gridded.
      </p>
      <div className="mt-8 space-y-3">
        {STAT_ROWS.map((row, i) => (
          <StatTickRow key={row.label} row={row} index={i} />
        ))}
      </div>
    </FrameSection>
  );
}

interface StatTickRowProps {
  readonly row: StatRow;
  readonly index: number;
}

function StatTickRow({ row, index }: StatTickRowProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-15% 0px" }}
      transition={{ duration: 0.5, delay: index * 0.12, ease: "easeOut" }}
      className="border-cc-card-border bg-cc-card-bg flex items-baseline gap-4 rounded-lg border px-4 py-3 backdrop-blur-md"
    >
      <span className="text-cc-nav-label flex-1 font-mono text-[11px] tracking-wide uppercase">
        {row.label}
      </span>
      <span className="font-heading text-cc-heading text-h4 tabular-nums">
        {row.value}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px]">{row.unit}</span>
      <span className="text-cc-nav-label w-32 text-right font-mono text-[11px]">
        {row.note}
      </span>
    </motion.div>
  );
}

/* ================================================================== *
 * OPERATION IMPACT  ·  frame 00:18
 * Ranked list of 5 operations, impact bars in cc-accent.
 * ================================================================== */

interface OpRow {
  readonly rank: number;
  readonly name: string;
  readonly p95: string;
  readonly impact: number;
}

const OP_ROWS: readonly OpRow[] = [
  { rank: 1, name: "checkout", p95: "42ms", impact: 94 },
  { rank: 2, name: "cartSummary", p95: "31ms", impact: 71 },
  { rank: 3, name: "productList", p95: "12ms", impact: 38 },
  { rank: 4, name: "userProfile", p95: "8ms", impact: 22 },
  { rank: 5, name: "shippingQuote", p95: "6ms", impact: 14 },
];

function OperationImpact() {
  return (
    <FrameSection stamp="00:18">
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Operation impact
      </span>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
        What to fix first, in one ranked list.
      </h2>
      <p className="text-body text-cc-ink-dim mt-5">
        The Nitro impact score folds p95, p99, throughput, and error rate into
        one number. The operation at the top is the one that owes you attention.
      </p>
      <div className="border-cc-card-border bg-cc-card-bg mt-7 overflow-hidden rounded-xl border backdrop-blur-md">
        <div className="border-cc-card-border/60 bg-cc-code-header/70 text-cc-nav-label flex items-center justify-between border-b px-4 py-2.5 font-mono text-[11px] tracking-wide uppercase">
          <span>nitro · operations</span>
          <span>sorted by impact</span>
        </div>
        <ul className="divide-cc-card-border/40 divide-y">
          {OP_ROWS.map((row, i) => (
            <OperationRow key={row.name} row={row} index={i} />
          ))}
        </ul>
      </div>
    </FrameSection>
  );
}

interface OperationRowProps {
  readonly row: OpRow;
  readonly index: number;
}

function OperationRow({ row, index }: OperationRowProps) {
  return (
    <motion.li
      initial={{ opacity: 0, x: -6 }}
      whileInView={{ opacity: 1, x: 0 }}
      viewport={{ once: true, margin: "-10% 0px" }}
      transition={{ duration: 0.4, delay: index * 0.08, ease: "easeOut" }}
      className="grid grid-cols-[28px_1fr_56px_1fr_44px] items-center gap-3 px-4 py-3 font-mono text-[12px]"
    >
      <span className="text-cc-nav-label text-[11px]">#{row.rank}</span>
      <span
        className={
          row.rank === 1
            ? "text-cc-heading truncate"
            : "text-cc-ink-dim truncate"
        }
      >
        {row.name}
      </span>
      <span className="text-cc-ink-dim text-right">{row.p95}</span>
      <span className="bg-cc-surface/80 relative inline-block h-1.5 overflow-hidden rounded-full">
        <motion.span
          className="absolute inset-y-0 left-0 rounded-full"
          style={{ backgroundColor: TEAL }}
          initial={{ width: 0 }}
          whileInView={{ width: `${row.impact}%` }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{
            duration: 0.8,
            delay: 0.2 + index * 0.08,
            ease: "easeOut",
          }}
        />
      </span>
      <span className="text-cc-ink-dim text-right tabular-nums">
        {row.impact}
      </span>
    </motion.li>
  );
}

/* ================================================================== *
 * TRACE MINI  ·  frame 00:32
 * A narrow 8-span vertical-friendly waterfall. Bars draw in once.
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
  graphql: "GQL",
  rest: "REST",
  grpc: "gRPC",
  job: "JOB",
  db: "DB",
};

const SPANS: readonly Span[] = [
  {
    id: "s0",
    label: "mutation checkout",
    kind: "graphql",
    start: 0,
    width: 100,
    ms: "318",
  },
  {
    id: "s1",
    label: "resolveCart",
    kind: "graphql",
    start: 2,
    width: 8,
    ms: "14",
  },
  {
    id: "s2",
    label: "users-svc /me",
    kind: "rest",
    start: 6,
    width: 11,
    ms: "21",
  },
  {
    id: "s3",
    label: "billing.Charge()",
    kind: "grpc",
    start: 18,
    width: 62,
    ms: "201",
    slow: true,
  },
  {
    id: "s4",
    label: "SELECT account",
    kind: "db",
    start: 22,
    width: 12,
    ms: "9",
  },
  {
    id: "s5",
    label: "SELECT ledger",
    kind: "db",
    start: 36,
    width: 14,
    ms: "11",
  },
  {
    id: "s6",
    label: "enqueue receipt",
    kind: "job",
    start: 80,
    width: 13,
    ms: "37",
  },
  {
    id: "s7",
    label: "publish event",
    kind: "job",
    start: 92,
    width: 7,
    ms: "8",
  },
];

function TraceMini() {
  return (
    <FrameSection stamp="00:32">
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Distributed trace
      </span>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
        From the spike to the slow span.
      </h2>
      <p className="text-body text-cc-ink-dim mt-5">
        Every hop is a real OpenTelemetry span: GraphQL at the root, REST and
        gRPC across services, the database read, the background job at the end.
        The slow hop is marked, not buried.
      </p>
      <div className="border-cc-card-border bg-cc-card-bg mt-7 overflow-hidden rounded-xl border backdrop-blur-md">
        <div className="border-cc-card-border/60 bg-cc-code-header/70 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-4 py-2.5">
          <span className="text-cc-nav-label font-mono text-[11px]">trace</span>
          <span className="font-mono text-[11px]" style={{ color: TEAL }}>
            4b1c8f2a9e07
          </span>
          <span className="text-cc-nav-label ml-auto inline-flex items-center gap-1.5 font-mono text-[11px]">
            duration <span className="text-cc-heading">318ms</span>
          </span>
        </div>
        <div className="space-y-1.5 px-4 py-4">
          {SPANS.map((span, i) => (
            <SpanRow key={span.id} span={span} index={i} />
          ))}
        </div>
        <div className="border-cc-card-border/60 text-cc-nav-label flex items-center justify-between border-t px-4 py-2 font-mono text-[10px]">
          <span>0ms</span>
          <span>100ms</span>
          <span>200ms</span>
          <span>318ms</span>
        </div>
      </div>
    </FrameSection>
  );
}

interface SpanRowProps {
  readonly span: Span;
  readonly index: number;
}

function SpanRow({ span, index }: SpanRowProps) {
  const isRoot = span.kind === "graphql" && span.start === 0;
  return (
    <div className="flex items-center gap-2">
      <span
        className="text-cc-nav-label w-9 shrink-0 font-mono text-[9px] tracking-wide uppercase"
        style={{ color: span.slow ? CORAL : undefined }}
      >
        {KIND_LABEL[span.kind]}
      </span>
      <span
        className={`w-32 shrink-0 truncate font-mono text-[11px] ${
          isRoot || span.slow ? "text-cc-heading" : "text-cc-ink-dim"
        }`}
      >
        {span.label}
      </span>
      <div className="bg-cc-surface/60 relative h-4 flex-1 rounded">
        <motion.div
          className="absolute top-1/2 h-3 -translate-y-1/2 rounded-[2px]"
          style={{
            left: `${span.start}%`,
            backgroundColor: span.slow ? CORAL : TEAL,
            opacity: span.slow ? 1 : 0.6,
            boxShadow: span.slow ? `0 0 12px ${CORAL}66` : undefined,
          }}
          initial={{ width: 0 }}
          whileInView={{ width: `${span.width}%` }}
          viewport={{ once: true, margin: "-10% 0px" }}
          transition={{
            duration: 0.6,
            delay: index * 0.08,
            ease: "easeOut",
          }}
        />
      </div>
      <span className="text-cc-nav-label w-10 shrink-0 text-right font-mono text-[10px] tabular-nums">
        {span.ms}ms
      </span>
    </div>
  );
}

/* ================================================================== *
 * CLIENT USAGE  ·  frame 00:46
 * Compact list of client/version rows with inline sparklines.
 * ================================================================== */

interface ClientRow {
  readonly name: string;
  readonly version: string;
  readonly rpm: string;
  readonly spark: readonly number[];
  readonly affected?: boolean;
}

const CLIENT_ROWS: readonly ClientRow[] = [
  {
    name: "Hot Chocolate IDE",
    version: "endpoint",
    rpm: "1.2k",
    spark: [3, 4, 5, 4, 6, 7, 6, 8, 7, 9, 8, 10],
    affected: true,
  },
  {
    name: "Strawberry Shake",
    version: "14.2.1",
    rpm: "5.7k",
    spark: [6, 7, 6, 8, 9, 8, 10, 11, 10, 12, 11, 13],
    affected: true,
  },
  {
    name: "web-app",
    version: "3.1.0",
    rpm: "8.4k",
    spark: [5, 6, 7, 6, 8, 7, 9, 8, 10, 9, 11, 10],
  },
];

function ClientUsage() {
  return (
    <FrameSection stamp="00:46">
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Per-client usage
      </span>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
        Published clients affected.
      </h2>
      <p className="text-body text-cc-ink-dim mt-5">
        Nitro registers clients by name and version. When a breaking change
        ships, the same telemetry tells you which clients still call the
        deprecated field, with rate, latency, and errors broken down by caller.
      </p>
      <div className="border-cc-card-border bg-cc-card-bg mt-7 overflow-hidden rounded-xl border backdrop-blur-md">
        <div className="border-cc-card-border/60 bg-cc-code-header/70 text-cc-nav-label flex items-center justify-between border-b px-4 py-2.5 font-mono text-[11px] tracking-wide uppercase">
          <span>clients · checkout · 1h</span>
          <span>nitro</span>
        </div>
        <ul className="divide-cc-card-border/40 divide-y">
          {CLIENT_ROWS.map((c) => (
            <li
              key={`${c.name}-${c.version}`}
              className="flex items-center gap-3 px-4 py-3"
            >
              <span className="min-w-0 flex-1">
                <span
                  className={`block truncate font-mono text-[12px] ${
                    c.affected ? "text-cc-heading" : "text-cc-ink-dim"
                  }`}
                >
                  {c.name}
                </span>
                <span className="text-cc-nav-label block truncate font-mono text-[10px]">
                  {c.version}
                  {c.affected ? " · affected" : ""}
                </span>
              </span>
              <Sparkline points={c.spark} affected={c.affected ?? false} />
              <span className="text-cc-ink-dim w-12 shrink-0 text-right font-mono text-[11px] tabular-nums">
                {c.rpm}
              </span>
            </li>
          ))}
        </ul>
        <p className="text-cc-nav-label border-cc-card-border/60 border-t px-4 py-2.5 font-mono text-[11px]">
          Drill into any client to see which operations and which schema
          versions it touches.
        </p>
      </div>
    </FrameSection>
  );
}

interface SparklineProps {
  readonly points: readonly number[];
  readonly affected: boolean;
}

function Sparkline({ points, affected }: SparklineProps) {
  const w = 80;
  const h = 22;
  const max = Math.max(...points);
  const min = Math.min(...points);
  const range = max - min || 1;
  const step = w / (points.length - 1);
  const path = points
    .map((p, i) => {
      const x = i * step;
      const y = h - ((p - min) / range) * h;
      return `${i === 0 ? "M" : "L"}${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(" ");
  const color = affected ? TEAL : "rgba(245, 241, 234, 0.32)";
  return (
    <svg
      aria-hidden
      width={w}
      height={h}
      viewBox={`0 0 ${w} ${h}`}
      className="shrink-0"
    >
      <path
        d={path}
        fill="none"
        stroke={color}
        strokeWidth={1.25}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/* ================================================================== *
 * CROSS-SERVICE LINE  ·  frame 01:02
 * One paragraph plus four mono pills inline.
 * ================================================================== */

function CrossServiceLine() {
  const kinds = ["GraphQL", "REST", "gRPC", "Jobs"] as const;
  return (
    <FrameSection stamp="01:02">
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Cross-service .NET
      </span>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
        One pipeline, every kind of service.
      </h2>
      <p className="text-body text-cc-ink-dim mt-5">
        The OpenTelemetry pipeline is the same across GraphQL, REST, gRPC, and
        background workers, so a single trace can carry the whole call. Nitro
        ingests it and lets you ask the operation and the service the same
        questions.
      </p>
      <div className="mt-7 flex flex-wrap items-center gap-2">
        {kinds.map((k) => (
          <span
            key={k}
            className="border-cc-card-border text-cc-ink-dim rounded-full border px-3 py-1 font-mono text-[11px]"
            style={{ backgroundColor: "rgba(94, 234, 212, 0.06)" }}
          >
            {k}
          </span>
        ))}
      </div>
      <p className="text-cc-nav-label mt-5 font-mono text-[11px]">
        Wire services through{" "}
        <code className="text-cc-ink">ChilliCream.Nitro.OpenTelemetry</code>.
      </p>
    </FrameSection>
  );
}

/* ================================================================== *
 * HONESTY NOTE  ·  frame 01:18
 * Tight bullet card. CheckIcon bullets, the truthful caveats.
 * ================================================================== */

const HONESTY: readonly string[] = [
  "Telemetry needs Nitro configuration, a deliberate step in your services, documented and explicit.",
  "OpenTelemetry-native end to end; vendor-neutral spans, no proprietary agent in the middle.",
  "The GraphQL IDE serves from your Hot Chocolate endpoint, independent of the telemetry dashboards.",
];

function HonestyNote() {
  return (
    <FrameSection stamp="01:18">
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Straight about what it is
      </span>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
        Honest about the setup.
      </h2>
      <div className="border-cc-card-border bg-cc-card-bg mt-7 rounded-xl border px-5 py-5 backdrop-blur-md">
        <ul className="space-y-3.5">
          {HONESTY.map((line) => (
            <li key={line} className="flex items-start gap-2.5">
              <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
                <CheckIcon size={14} />
              </span>
              <span className="text-caption text-cc-ink-dim">{line}</span>
            </li>
          ))}
        </ul>
      </div>
    </FrameSection>
  );
}

/* ================================================================== *
 * CLOSING CTA  ·  frame 01:34
 * One phrase rendered with the brand spectrum gradient. The rail
 * terminates with a final tick mark below the buttons.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="relative">
      <FrameMarker stamp="01:34" />
      <Column className="pt-6">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
          End of feed
        </span>
        <p
          className="font-heading text-h3 sm:text-h2 mt-5 leading-tight"
          style={{
            background: SPECTRUM,
            WebkitBackgroundClip: "text",
            backgroundClip: "text",
            color: "transparent",
          }}
        >
          See the signal end to end.
        </p>
        <p className="text-body text-cc-ink-dim mt-5">
          Point OpenTelemetry at Nitro once and every request becomes evidence:
          ranked by impact, traced end to end, sliced by client and by service.
        </p>
        <div className="mt-9 flex flex-wrap items-center gap-4">
          <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
            Get Started
          </SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
        {/* terminating tick on the rail */}
        <div
          aria-hidden
          className="relative mt-12"
          style={{ marginLeft: "-1.5rem" }}
        >
          <span
            className="absolute block h-px w-[24px]"
            style={{
              left: "0",
              backgroundColor: TEAL,
              opacity: 0.7,
              boxShadow: `0 0 8px ${TEAL}66`,
            }}
          />
        </div>
      </Column>
    </section>
  );
}
