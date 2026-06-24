import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Espresso } from "@/src/icons/Espresso";

export const metadata: Metadata = {
  title: "Nitro Cold Brew Bar: GraphQL Observability for .NET",
  description:
    "GraphQL observability for .NET, served like a cold brew bar. Watch a checkout incident from p99 spike to the slow gRPC span across GraphQL, REST, gRPC, and jobs.",
  keywords: [
    "GraphQL observability",
    "GraphQL observability for .NET",
    "OpenTelemetry .NET",
    "distributed tracing",
    "Nitro telemetry",
    "p95 p99 latency",
    "operation monitoring",
    "Hot Chocolate observability",
    "trace waterfall",
  ],
  openGraph: {
    title: "Nitro Cold Brew Bar",
    description:
      "Every request is an order tracked from counter to cup. p99 spikes are pours that ran long, and the trace id is the ticket that follows the drink through the back bar.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ *
 * Scene palette. Palette stays cc-* dark; coffee lives in the COPY
 * and one Espresso icon. Status is rationed as data.
 * ------------------------------------------------------------------ */

const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";
const CYAN = "#16b9e4";

// The single spectrum event for the page lives in the closing CTA.
const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, ${VIOLET} 52%, ${CORAL} 100%)`;

// The trace id is the order ticket number running through the page.
const TRACE_ID = "4b1c8f2a9e07";
const ORDER_ID = `#${TRACE_ID}`;

export default function ObservabilityPreviewV6Page() {
  return (
    <main className="flex flex-col gap-28 pb-16">
      <Hero />
      <IncidentSection />
      <MetricsInterlude />
      <LensesSection />
      <TopologySection />
      <HonestySection />
      <ClosingCta />
    </main>
  );
}

/* ================================================================== *
 * HERO
 * Eyebrow "Today's pour"; the right-side incident tile is rendered as
 * a printed cafe order ticket with cc-* dark surfaces and mono receipt
 * styling. The order id (a real trace id) stitches into the next
 * section.
 * ================================================================== */

function Hero() {
  return (
    <section className="relative isolate pt-8">
      <HeroGlow />
      <div className="relative grid items-center gap-12 lg:grid-cols-[1.05fr_1fr]">
        <div>
          <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
            Today&apos;s pour
          </span>
          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-6">
            See every order
            <br />
            leave the bar.
          </h1>
          <p className="lead text-cc-prose mt-6 max-w-xl">
            GraphQL observability for .NET, served like a cold brew bar. The
            moment latency climbs, you already know which operation hurts, who
            it reaches, and exactly which hop ran long.
          </p>
          <p className="text-body text-cc-ink-dim mt-5 max-w-xl">
            Nitro is OpenTelemetry-native: operation, service, and client views
            with p95 / p99, throughput, error rate, and an impact score. Every
            request is a distributed trace that spans GraphQL, REST, gRPC, and
            background jobs, so debugging starts from evidence, not another
            dashboard project.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
              Read the Docs
            </OutlineButton>
          </div>
          <div className="text-cc-nav-label mt-8 flex items-center gap-3 font-mono text-[11px]">
            <StatusDot color={AMBER} pulse />
            <span className="tracking-wide uppercase">
              Order on the rail right now
            </span>
            <span className="text-cc-ink-faint">·</span>
            <span>
              ticket <span className="text-cc-ink-dim">{TRACE_ID}</span>
            </span>
          </div>
        </div>
        <OrderTicket />
      </div>
    </section>
  );
}

function HeroGlow() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute -top-24 right-0 -z-10 h-[460px] w-[620px] opacity-60 blur-3xl"
      style={{
        background: `radial-gradient(60% 60% at 70% 30%, ${AMBER}22 0%, transparent 70%), radial-gradient(50% 50% at 80% 70%, ${TEAL}1c 0%, transparent 72%)`,
      }}
    />
  );
}

/* The Nitro dashboard tile, rendered as a printed cafe order ticket. */
function OrderTicket() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative rounded-2xl border p-1 shadow-2xl backdrop-blur-md">
      <div className="border-cc-card-border/60 bg-cc-surface/95 overflow-hidden rounded-xl border">
        {/* ticket header: receipt-style top */}
        <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center gap-2 border-b px-4 py-2.5">
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
          <span className="text-cc-nav-label ml-2 font-mono text-[11px]">
            nitro · counter ticket
          </span>
          <span className="border-cc-card-border/70 text-cc-nav-label ml-auto inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wide uppercase">
            <StatusDot color={GREEN} />
            pouring
          </span>
        </div>

        {/* "printed" receipt header with order id */}
        <div className="border-cc-card-border/40 border-b border-dashed px-5 pt-4 pb-3">
          <div className="flex items-baseline justify-between">
            <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.22em] uppercase">
              order
            </span>
            <span className="font-mono text-[11px]" style={{ color: TEAL }}>
              {ORDER_ID}
            </span>
          </div>
          <div className="mt-2 flex items-baseline justify-between">
            <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.22em] uppercase">
              on the rail
            </span>
            <span className="text-cc-heading font-mono text-[13px]">
              checkout
            </span>
          </div>
        </div>

        {/* ticket body */}
        <div className="flex items-start justify-between px-5 pt-4">
          <div>
            <div className="text-cc-nav-label font-mono text-[11px]">
              status
            </div>
            <div className="text-cc-heading mt-1 font-mono text-sm">
              p99 spiking
            </div>
          </div>
          <span
            className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 font-mono text-[10px] font-semibold tracking-wide uppercase"
            style={{
              color: AMBER,
              backgroundColor: `${AMBER}1a`,
              boxShadow: `inset 0 0 0 1px ${AMBER}40`,
            }}
          >
            <StatusDot color={AMBER} pulse />
            Investigating
          </span>
        </div>

        {/* the p99 pour-time chart */}
        <div className="px-5 pt-4">
          <div className="flex items-baseline justify-between">
            <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
              p99 latency · 30m
            </span>
            <span className="font-mono text-[11px]" style={{ color: CORAL }}>
              ▲ 318 ms
            </span>
          </div>
          <SpikeChart />
        </div>

        {/* mono stat row */}
        <div className="divide-cc-card-border/50 border-cc-card-border/50 mt-1 grid grid-cols-4 divide-x border-t">
          <TileStat label="p95" value="42ms" tone="ink" />
          <TileStat label="p99" value="318ms" tone="coral" />
          <TileStat label="errors" value="0.3%" tone="ink" />
          <TileStat label="rpm" value="9.4k" tone="ink" />
        </div>
      </div>
    </div>
  );
}

interface TileStatProps {
  readonly label: string;
  readonly value: string;
  readonly tone: "ink" | "coral";
}

function TileStat({ label, value, tone }: TileStatProps) {
  return (
    <div className="px-3 py-3">
      <div className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
        {label}
      </div>
      <div
        className="mt-1 font-mono text-base"
        style={{ color: tone === "coral" ? CORAL : "var(--color-cc-heading)" }}
      >
        {value}
      </div>
    </div>
  );
}

// Baseline-then-spike pour curve; dashed amber marker is where the long
// pour starts, and where the order ticket stitches downward.
function SpikeChart() {
  const points = [
    18, 16, 19, 17, 20, 18, 16, 19, 21, 18, 17, 20, 19, 22, 28, 41, 58, 72, 80,
    78,
  ];
  const w = 320;
  const h = 88;
  const max = 96;
  const step = w / (points.length - 1);
  const coords = points.map((p, i) => {
    const x = i * step;
    const y = h - (p / max) * h;
    return [x, y] as const;
  });
  const line = coords.map(([x, y]) => `${x},${y}`).join(" ");
  const area = `${line} ${w},${h} 0,${h}`;
  const spikeStart = coords.findIndex((_, i) => points[i] >= 41);
  const [sx] = coords[spikeStart];
  const last = coords[coords.length - 1];

  return (
    <svg
      viewBox={`0 0 ${w} ${h}`}
      className="mt-2 h-[88px] w-full"
      preserveAspectRatio="none"
      aria-hidden
    >
      <defs>
        <linearGradient id="spikeFill" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={CORAL} stopOpacity="0.32" />
          <stop offset="100%" stopColor={CORAL} stopOpacity="0" />
        </linearGradient>
      </defs>
      {/* in-budget pour threshold */}
      <line
        x1="0"
        y1={h - (32 / max) * h}
        x2={w}
        y2={h - (32 / max) * h}
        stroke="var(--color-cc-ink-faint)"
        strokeWidth="1"
        strokeDasharray="3 4"
      />
      {/* long-pour onset marker, where the ticket stitches down */}
      <line
        x1={sx}
        y1="0"
        x2={sx}
        y2={h}
        stroke={AMBER}
        strokeWidth="1"
        strokeOpacity="0.5"
      />
      <polygon points={area} fill="url(#spikeFill)" />
      <polyline
        points={line}
        fill="none"
        stroke={CORAL}
        strokeWidth="2"
        strokeLinejoin="round"
        strokeLinecap="round"
      />
      <circle cx={last[0]} cy={last[1]} r="3" fill={CORAL} />
    </svg>
  );
}

/* ================================================================== *
 * INCIDENT SECTION - "One order, one ticket"
 * The trace-id is printed at the top of the waterfall like a receipt
 * number; the full distributed-trace waterfall sits below. The stitch
 * hairline literally runs from the ticket number down to the slow
 * gRPC span. Engineering terms stay verbatim; the legend has one
 * subtle coffee aside on the slow row, and the Espresso icon sits
 * next to it as the long-pour motif.
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

function IncidentSection() {
  return (
    <section className="relative">
      <SectionEyebrow>One order, one ticket</SectionEyebrow>
      <div className="mt-5 grid gap-8 lg:grid-cols-[1fr_1.6fr] lg:items-start">
        <div>
          <h2 className="font-heading text-h4 text-cc-heading sm:text-h3">
            Follow the slow span, not the dashboard.
          </h2>
          <p className="text-body text-cc-ink-dim mt-5">
            The p99 spike on{" "}
            <code className="text-cc-ink font-mono">checkout</code> is one click
            from its trace. A single request fans out across your graph and the
            services behind it, every hop a real OpenTelemetry span. The same{" "}
            <code className="font-mono" style={{ color: TEAL }}>
              ticket {TRACE_ID}
            </code>{" "}
            that printed on the order tile is stitched straight to the span that
            ran long.
          </p>
          <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
            <LegendRow
              kind="graphql"
              text="The order arrives: the GraphQL operation, root of the trace"
            />
            <LegendRow
              kind="rest"
              text="Grind delay on users-svc: a REST hop to fetch the customer"
            />
            <LegendRow
              kind="grpc"
              text="The shot was pulled long on billing: the slow gRPC charge"
              highlight
              icon
            />
            <LegendRow kind="db" text="A fast database read" />
            <LegendRow
              kind="job"
              text="A background job enqueued for the receipt"
            />
          </ul>
        </div>
        <TraceWaterfall />
      </div>
    </section>
  );
}

interface LegendRowProps {
  readonly kind: Span["kind"];
  readonly text: string;
  readonly highlight?: boolean;
  readonly icon?: boolean;
}

function LegendRow({ kind, text, highlight, icon }: LegendRowProps) {
  return (
    <li className="flex items-center gap-3">
      <span
        className="h-2.5 w-2.5 shrink-0 rounded-[3px]"
        style={{ backgroundColor: KIND_COLOR[kind] }}
      />
      <span className={highlight ? "text-cc-prose" : undefined}>
        {text}
        {highlight && (
          <span
            className="ml-2 rounded px-1.5 py-0.5 font-mono text-[10px] tracking-wide uppercase"
            style={{ color: CORAL, backgroundColor: `${CORAL}1a` }}
          >
            201 ms · 63%
          </span>
        )}
      </span>
      {icon && (
        <Espresso
          className="ml-1 h-5 w-5 shrink-0 opacity-80"
          style={{ filter: `drop-shadow(0 0 6px ${CORAL}44)` }}
        />
      )}
    </li>
  );
}

function TraceWaterfall() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-md">
      {/* receipt-style header: trace id printed like an order number */}
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3">
        <span className="text-cc-nav-label font-mono text-[11px]">ticket</span>
        <span className="font-mono text-[11px]" style={{ color: TEAL }}>
          {ORDER_ID}
        </span>
        <span className="text-cc-ink-faint font-mono text-[11px]">·</span>
        <span className="text-cc-ink-dim font-mono text-[11px]">
          mutation checkout
        </span>
        <span className="text-cc-nav-label ml-auto inline-flex items-center gap-1.5 font-mono text-[11px]">
          pour time <span className="text-cc-heading">318ms</span>
        </span>
      </div>

      {/* the ticket stitch hairline runs from the printed receipt number
          down into the slow gRPC row, literally threading the trace id */}
      <div className="relative px-5 py-5">
        <StitchThread />
        <div className="space-y-2.5">
          {SPANS.map((span, i) => (
            <SpanRow key={span.id} span={span} depth={SPAN_DEPTH[i] ?? 0} />
          ))}
        </div>

        {/* time axis */}
        <div className="border-cc-card-border/50 text-cc-nav-label mt-5 ml-[38%] flex items-center justify-between border-t pt-2 font-mono text-[10px]">
          <span>0ms</span>
          <span>100ms</span>
          <span>200ms</span>
          <span>318ms</span>
        </div>
      </div>
    </div>
  );
}

interface SpanRowProps {
  readonly span: Span;
  readonly depth: number;
}

function SpanRow({ span, depth }: SpanRowProps) {
  const color = KIND_COLOR[span.kind];
  const isRoot = span.kind === "graphql";
  return (
    <div className="flex items-center gap-3">
      {/* label column */}
      <div
        className="flex w-[38%] shrink-0 items-center gap-2 truncate"
        style={{ paddingLeft: depth * 14 }}
      >
        <span
          className="rounded px-1.5 py-0.5 font-mono text-[9px] font-semibold tracking-wide uppercase"
          style={{ color, backgroundColor: `${color}1a` }}
        >
          {KIND_LABEL[span.kind]}
        </span>
        <span
          className={`truncate font-mono text-[12px] ${isRoot ? "text-cc-heading" : "text-cc-ink-dim"}`}
        >
          {span.label}
        </span>
      </div>

      {/* track */}
      <div className="bg-cc-surface/60 relative h-6 flex-1 rounded">
        <div
          className="absolute top-1/2 flex h-4 -translate-y-1/2 items-center rounded-[3px]"
          style={{
            left: `${span.start}%`,
            width: `${span.width}%`,
            backgroundColor: span.slow ? CORAL : color,
            opacity: span.slow ? 1 : 0.78,
            boxShadow: span.slow ? `0 0 16px ${CORAL}66` : undefined,
          }}
        >
          {span.slow && (
            <span className="text-cc-surface ml-2 font-mono text-[10px] font-semibold">
              billing.Charge()
            </span>
          )}
        </div>
        <span
          className="text-cc-nav-label absolute top-1/2 -translate-y-1/2 font-mono text-[10px]"
          style={{ left: `calc(${span.start + span.width}% + 8px)` }}
        >
          {span.ms}
        </span>
      </div>
    </div>
  );
}

// The ticket stitch: vertical hairline from the printed receipt number
// down to the slow gRPC row, with nodes at each end.
function StitchThread() {
  return (
    <svg
      aria-hidden
      className="pointer-events-none absolute inset-0 h-full w-full"
      preserveAspectRatio="none"
    >
      <defs>
        <linearGradient id="stitch" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={AMBER} stopOpacity="0.7" />
          <stop offset="100%" stopColor={CORAL} stopOpacity="0.9" />
        </linearGradient>
      </defs>
      {/* the gRPC row (index 2) sits ~94px down; the stitch lands on it */}
      <line
        x1="54%"
        y1="0"
        x2="54%"
        y2="96"
        stroke="url(#stitch)"
        strokeWidth="1.5"
        strokeDasharray="2 3"
      />
      <circle cx="54%" cy="2" r="2.5" fill={AMBER} />
      <circle cx="54%" cy="94" r="3" fill={CORAL} />
    </svg>
  );
}

/* ================================================================== *
 * METRICS INTERLUDE - "On the menu"
 * Four big-number tiles for p95 / p99 / error rate / throughput laid
 * out like a chalkboard menu row, but no chalk textures. The metaphor
 * lives in the eyebrow and the thin top divider only.
 * ================================================================== */

function MetricsInterlude() {
  const metrics = [
    {
      label: "p95 latency",
      value: "42",
      unit: "ms",
      tone: GREEN,
      note: "within budget",
    },
    {
      label: "p99 latency",
      value: "318",
      unit: "ms",
      tone: CORAL,
      note: "spiking",
    },
    {
      label: "error rate",
      value: "0.3",
      unit: "%",
      tone: AMBER,
      note: "5xx on billing",
    },
    {
      label: "throughput",
      value: "9.4",
      unit: "k rpm",
      tone: TEAL,
      note: "steady",
    },
  ];
  return (
    <section>
      <div className="flex items-baseline justify-between gap-4">
        <SectionEyebrow>On the menu</SectionEyebrow>
        <span className="text-cc-nav-label font-mono text-[11px]">
          the four signals
        </span>
      </div>
      <div
        aria-hidden
        className="mt-3 h-px w-full"
        style={{
          background:
            "linear-gradient(90deg, transparent 0%, var(--color-cc-card-border) 18%, var(--color-cc-card-border) 82%, transparent 100%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-card-border mt-6 grid gap-px overflow-hidden rounded-2xl border sm:grid-cols-2 lg:grid-cols-4">
        {metrics.map((m) => (
          <div key={m.label} className="bg-cc-surface/85 px-6 py-7">
            <div className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
              {m.label}
            </div>
            <div className="mt-3 flex items-baseline gap-1">
              <span
                className="font-heading text-h3 leading-none"
                style={{ color: m.tone }}
              >
                {m.value}
              </span>
              <span className="text-cc-ink-dim font-mono text-sm">
                {m.unit}
              </span>
            </div>
            <div className="mt-3 flex items-center gap-2">
              <StatusDot color={m.tone} />
              <span className="text-cc-nav-label font-mono text-[11px]">
                {m.note}
              </span>
            </div>
          </div>
        ))}
      </div>
      <p className="text-caption text-cc-nav-label mt-4">
        The same numbers behind the order tile, held side by side. Impact score
        names the rest: <span className="text-cc-ink-dim">#1 checkout</span>.
      </p>
    </section>
  );
}

/* ================================================================== *
 * LENSES SECTION - "Behind the bar"
 * Three real-looking Nitro cards (operation / service / client). The
 * eyebrow carries the metaphor; the cards stay technical.
 * ================================================================== */

function LensesSection() {
  return (
    <section>
      <SectionEyebrow>Behind the bar</SectionEyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5 max-w-2xl">
        Same telemetry, three lenses.
      </h2>
      <p className="text-body text-cc-ink-dim mt-5 max-w-2xl">
        Rank by impact to see what hurts, drop into the degraded service, then
        see which published clients tasted it before you ship a fix.
      </p>

      <div className="mt-10 grid gap-5 lg:grid-cols-3">
        <OperationLens />
        <ServiceLens />
        <ClientLens />
      </div>
    </section>
  );
}

interface LensCardProps {
  readonly title: string;
  readonly tab: string;
  readonly children: React.ReactNode;
}

function LensCard({ title, tab, children }: LensCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-md">
      <div className="border-cc-card-border/60 bg-cc-code-header/60 flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
          {tab}
        </span>
        <span className="text-cc-ink-faint font-mono text-[10px]">nitro</span>
      </div>
      <div className="px-4 py-4">
        <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
        <div className="mt-3">{children}</div>
      </div>
    </div>
  );
}

interface OpRow {
  readonly name: string;
  readonly impact: number;
  readonly p95: string;
  readonly status: "ok" | "warn" | "fire";
}

const OP_ROWS: readonly OpRow[] = [
  { name: "checkout", impact: 1, p95: "42ms", status: "fire" },
  { name: "cartSummary", impact: 2, p95: "31ms", status: "warn" },
  { name: "productList", impact: 3, p95: "12ms", status: "ok" },
  { name: "userProfile", impact: 4, p95: "8ms", status: "ok" },
];

const STATUS_COLOR: Record<OpRow["status"], string> = {
  ok: GREEN,
  warn: AMBER,
  fire: CORAL,
};

function OperationLens() {
  return (
    <LensCard title="Ranked by impact" tab="operations">
      <div className="space-y-1.5">
        {OP_ROWS.map((row) => (
          <div
            key={row.name}
            className={`flex items-center gap-3 rounded-lg px-2.5 py-2 ${
              row.status === "fire" ? "bg-cc-surface/80" : "bg-cc-surface/40"
            }`}
            style={
              row.status === "fire"
                ? { boxShadow: `inset 0 0 0 1px ${CORAL}33` }
                : undefined
            }
          >
            <span className="text-cc-nav-label w-5 font-mono text-[11px]">
              #{row.impact}
            </span>
            <StatusDot
              color={STATUS_COLOR[row.status]}
              pulse={row.status === "fire"}
            />
            <span
              className={`flex-1 font-mono text-[12px] ${
                row.status === "fire" ? "text-cc-heading" : "text-cc-ink-dim"
              }`}
            >
              {row.name}
            </span>
            <span className="text-cc-nav-label font-mono text-[11px]">
              {row.p95}
            </span>
          </div>
        ))}
      </div>
      <p className="text-cc-nav-label mt-3 text-[11px]">
        Impact score ranks by what hurts the system, not raw call count.
      </p>
    </LensCard>
  );
}

function ServiceLens() {
  return (
    <LensCard title="billing · degraded" tab="services">
      <div className="grid grid-cols-2 gap-2">
        <MiniStat label="p95" value="42ms" />
        <MiniStat label="p99" value="318ms" tone={CORAL} />
        <MiniStat label="errors" value="0.3%" />
        <MiniStat label="rpm" value="9.4k" />
      </div>
      <div className="bg-cc-surface/50 mt-3 rounded-lg px-3 py-2.5">
        <div className="text-cc-nav-label mb-1.5 font-mono text-[10px] tracking-wide uppercase">
          status codes · 5m
        </div>
        <StatusBar />
        <div className="mt-2 flex items-center gap-4 font-mono text-[10px]">
          <span style={{ color: GREEN }}>2xx 96.4%</span>
          <span style={{ color: AMBER }}>4xx 3.3%</span>
          <span style={{ color: CORAL }}>5xx 0.3%</span>
        </div>
      </div>
    </LensCard>
  );
}

function ClientLens() {
  const clients = [
    { name: "web-storefront@4.2.0", share: "61%", status: "fire" as const },
    { name: "ios-app@3.8.1", share: "27%", status: "warn" as const },
    { name: "partner-api@1.0", share: "12%", status: "ok" as const },
  ];
  return (
    <LensCard title="Published clients affected" tab="clients">
      <div className="space-y-2">
        {clients.map((c) => (
          <div key={c.name} className="flex items-center gap-2.5">
            <StatusDot color={STATUS_COLOR[c.status]} />
            <span className="text-cc-ink-dim flex-1 truncate font-mono text-[12px]">
              {c.name}
            </span>
            <span className="text-cc-nav-label font-mono text-[11px]">
              {c.share}
            </span>
          </div>
        ))}
      </div>
      <p className="text-cc-nav-label mt-4 text-[11px]">
        See which published clients are affected before you ship the fix.
      </p>
    </LensCard>
  );
}

interface MiniStatProps {
  readonly label: string;
  readonly value: string;
  readonly tone?: string;
}

function MiniStat({ label, value, tone }: MiniStatProps) {
  return (
    <div className="bg-cc-surface/50 rounded-lg px-3 py-2.5">
      <div className="text-cc-nav-label font-mono text-[10px] tracking-wide uppercase">
        {label}
      </div>
      <div
        className="mt-1 font-mono text-sm"
        style={{ color: tone ?? "var(--color-cc-heading)" }}
      >
        {value}
      </div>
    </div>
  );
}

function StatusBar() {
  return (
    <div className="flex h-2 overflow-hidden rounded-full">
      <span style={{ width: "96.4%", backgroundColor: GREEN }} />
      <span style={{ width: "3.3%", backgroundColor: AMBER }} />
      <span style={{ width: "0.3%", backgroundColor: CORAL }} />
    </div>
  );
}

/* ================================================================== *
 * TOPOLOGY SECTION - "All the way down the bar"
 * A converging service-topology node graph. GraphQL fans down to REST,
 * gRPC, job, and DB; the hot gRPC hop glows coral. The graph stays
 * technical; the metaphor lives in the eyebrow and headline only.
 * ================================================================== */

function TopologySection() {
  return (
    <section className="grid gap-10 lg:grid-cols-[1fr_1.2fr] lg:items-center">
      <div>
        <SectionEyebrow>All the way down the bar</SectionEyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-5">
          The graph is the counter. The trace goes back to the grinder.
        </h2>
        <p className="text-body text-cc-ink-dim mt-5">
          A distributed trace does not stop at the GraphQL boundary. Nitro
          monitors REST APIs, gRPC services, and background jobs through{" "}
          <code className="text-cc-ink font-mono">
            ChilliCream.Nitro.OpenTelemetry
          </code>
          , so the same trace that opens on{" "}
          <code className="font-mono" style={{ color: TEAL }}>
            checkout
          </code>{" "}
          follows the call down to the hop that ran long.
        </p>
        <ul className="text-caption text-cc-ink-dim mt-7 space-y-3">
          <CheckLine>
            Operation, service, and client views over one OpenTelemetry stream
          </CheckLine>
          <CheckLine>
            Vendor-neutral OTel: no proprietary agent to wire up
          </CheckLine>
          <CheckLine>
            The hot hop glows, so the eye lands on cause, not noise
          </CheckLine>
        </ul>
      </div>
      <TopologyGraph />
    </section>
  );
}

function TopologyGraph() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 backdrop-blur-md">
      <svg
        viewBox="0 0 360 300"
        className="h-auto w-full"
        role="img"
        aria-label="Service topology: GraphQL fans out to REST, gRPC, job, and database hops, with the slow gRPC hop to billing highlighted."
      >
        <defs>
          <linearGradient id="hotEdge" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={TEAL} stopOpacity="0.5" />
            <stop offset="100%" stopColor={CORAL} stopOpacity="0.9" />
          </linearGradient>
          <filter id="nodeGlow" x="-50%" y="-50%" width="200%" height="200%">
            <feGaussianBlur stdDeviation="4" result="b" />
            <feMerge>
              <feMergeNode in="b" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>

        {/* edges */}
        <Edge x1={180} y1={48} x2={84} y2={132} />
        <Edge x1={180} y1={48} x2={180} y2={132} hot />
        <Edge x1={180} y1={48} x2={276} y2={132} />
        <Edge x1={180} y1={132} x2={132} y2={228} />
        <Edge x1={180} y1={132} x2={228} y2={228} />

        {/* nodes */}
        <Node x={180} y={48} kind="graphql" label="api" sub="GraphQL" />
        <Node x={84} y={132} kind="rest" label="users-svc" sub="REST" />
        <Node x={180} y={132} kind="grpc" label="billing" sub="gRPC" hot />
        <Node x={276} y={132} kind="job" label="worker" sub="Job" />
        <Node x={132} y={228} kind="db" label="accounts" sub="DB" />
        <Node x={228} y={228} kind="db" label="ledger" sub="DB" />
      </svg>
      <div className="text-cc-nav-label mt-2 flex flex-wrap items-center justify-center gap-x-4 gap-y-1 font-mono text-[10px]">
        <LegendChip kind="graphql" />
        <LegendChip kind="rest" />
        <LegendChip kind="grpc" />
        <LegendChip kind="job" />
        <LegendChip kind="db" />
      </div>
    </div>
  );
}

interface EdgeProps {
  readonly x1: number;
  readonly y1: number;
  readonly x2: number;
  readonly y2: number;
  readonly hot?: boolean;
}

function Edge({ x1, y1, x2, y2, hot }: EdgeProps) {
  return (
    <line
      x1={x1}
      y1={y1}
      x2={x2}
      y2={y2}
      stroke={hot ? "url(#hotEdge)" : "var(--color-cc-card-border)"}
      strokeWidth={hot ? 2 : 1.25}
    />
  );
}

interface NodeProps {
  readonly x: number;
  readonly y: number;
  readonly kind: Span["kind"];
  readonly label: string;
  readonly sub: string;
  readonly hot?: boolean;
}

function Node({ x, y, kind, label, sub, hot }: NodeProps) {
  const color = hot ? CORAL : KIND_COLOR[kind];
  return (
    <g filter={hot ? "url(#nodeGlow)" : undefined}>
      <rect
        x={x - 46}
        y={y - 18}
        width={92}
        height={36}
        rx={8}
        fill="var(--color-cc-surface)"
        stroke={color}
        strokeWidth={hot ? 1.5 : 1}
        strokeOpacity={hot ? 1 : 0.6}
      />
      <circle cx={x - 34} cy={y} r={3} fill={color} />
      <text
        x={x - 24}
        y={y - 1}
        fill="var(--color-cc-heading)"
        fontSize="10"
        fontFamily="monospace"
      >
        {label}
      </text>
      <text
        x={x - 24}
        y={y + 10}
        fill="var(--color-cc-nav-label)"
        fontSize="7.5"
        fontFamily="monospace"
        letterSpacing="0.08em"
      >
        {sub.toUpperCase()}
      </text>
    </g>
  );
}

interface LegendChipProps {
  readonly kind: Span["kind"];
}

function LegendChip({ kind }: LegendChipProps) {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span
        className="h-2 w-2 rounded-[2px]"
        style={{ backgroundColor: KIND_COLOR[kind] }}
      />
      {KIND_LABEL[kind]}
    </span>
  );
}

/* ================================================================== *
 * HONESTY SECTION - "House rules"
 * Credibility beat. Stays straight; no coffee voice inside the cards.
 * ================================================================== */

function HonestySection() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg rounded-2xl border px-6 py-9 backdrop-blur-md sm:px-10">
      <SectionEyebrow>House rules</SectionEyebrow>
      <h2 className="font-heading text-h4 text-cc-heading mt-5 max-w-2xl">
        Honest about the setup, precise about the payoff.
      </h2>
      <div className="mt-8 grid gap-6 md:grid-cols-3">
        <HonestyCard title="Telemetry is configured, not magic">
          The dashboards above come from telemetry you point at Nitro. It is a
          configuration step, deliberate and documented, not something that
          turns on by itself.
        </HonestyCard>
        <HonestyCard title="The IDE is a separate thing">
          The GraphQL IDE can be served from your Hot Chocolate endpoint. That
          is independent of the telemetry dashboards here. Two facts, kept
          apart.
        </HonestyCard>
        <HonestyCard title="An open standard underneath">
          It is OpenTelemetry end to end. Vendor-neutral spans mean your data is
          yours, and there is no proprietary agent locking the trace in.
        </HonestyCard>
      </div>
    </section>
  );
}

interface HonestyCardProps {
  readonly title: string;
  readonly children: React.ReactNode;
}

function HonestyCard({ title, children }: HonestyCardProps) {
  return (
    <div className="border-cc-card-border/70 bg-cc-surface/50 rounded-xl border px-5 py-5">
      <div className="flex items-center gap-2">
        <span style={{ color: TEAL }}>
          <CheckIcon size={15} />
        </span>
        <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
      </div>
      <p className="text-caption text-cc-ink-dim mt-3">{children}</p>
    </div>
  );
}

/* ================================================================== *
 * CLOSING CTA - the single spectrum event for the page.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-surface/80 relative overflow-hidden rounded-2xl border px-6 py-14 text-center backdrop-blur-md sm:px-12">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-px"
        style={{ background: SPECTRUM }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute -bottom-24 left-1/2 h-64 w-[680px] -translate-x-1/2 opacity-25 blur-3xl"
        style={{ background: SPECTRUM }}
      />
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Today&apos;s pour
      </span>
      <h2 className="font-heading text-h3 text-cc-heading sm:text-h2 mt-5">
        Pour a fresh build.
      </h2>
      <p className="text-body text-cc-ink-dim mx-auto mt-5 max-w-xl">
        Wire your services to OpenTelemetry once and every request becomes
        evidence: ranked by impact, traced end to end, slow span already
        highlighted.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
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
