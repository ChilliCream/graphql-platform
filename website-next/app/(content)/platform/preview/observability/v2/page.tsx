import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Production View — See What Your GraphQL API Is Doing",
  description:
    "Nitro is OpenTelemetry-native observability for GraphQL: p95/p99 latency, throughput, error rate and an impact score, with distributed traces that span REST, gRPC and background jobs.",
  keywords: [
    "GraphQL observability",
    "OpenTelemetry",
    "distributed tracing",
    "p95 p99 latency",
    "Nitro",
    "Hot Chocolate",
    "operation monitoring",
    "impact score",
    "GraphQL metrics",
    "trace waterfall",
  ],
  openGraph: {
    title: "Production View — See What Your GraphQL API Is Doing",
    description:
      "OpenTelemetry-native observability for GraphQL. Operation, service and client views with p95/p99, throughput, error rate and an impact score — plus traces that span REST, gRPC and jobs.",
  },
  robots: { index: false, follow: false },
};

/* ---------------------------------------------------------------------------
   Scene accent: teal #5eead4 is the signature ink. Status is rationed as data
   only — green healthy, amber investigating, coral firing. The charts carry the
   chroma; the prose stays monochrome.
--------------------------------------------------------------------------- */
const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";

export default function ObservabilityPreviewV2Page() {
  return (
    <article className="text-cc-ink">
      <Hero />
      <SpecLedger />
      <ChapterOne />
      <ChapterTwo />
      <ChapterThree />
      <HonestyBeat />
      <ClosingCta />
    </article>
  );
}

/* ===========================================================================
   HERO — outcome-led, editorial. Big Josefin display, a hairline-ruled mono
   dateline, and the trace-id stitch drawn as a thin line from a dashboard spike
   into a slow span.
=========================================================================== */
function Hero() {
  return (
    <header className="relative pt-6 sm:pt-10">
      <div className="text-cc-nav-label flex items-center gap-4 font-mono text-[0.7rem] tracking-[0.22em] uppercase">
        <span>Production View</span>
        <span className="bg-cc-card-border h-px flex-1" />
        <span>/platform/observability</span>
      </div>

      <h1 className="font-heading text-cc-heading mt-10 max-w-4xl text-[clamp(2.75rem,8vw,6.5rem)] leading-[0.98] font-bold tracking-[-0.02em] text-balance">
        See what the
        <br />
        API is doing.
      </h1>

      <p className="lead text-cc-prose mt-8 max-w-2xl">
        When checkout slows down at 2&nbsp;a.m., you should not be opening a
        second dashboard project. You should be reading the evidence the API
        already emitted.
      </p>

      <p className="text-body text-cc-ink-dim mt-6 max-w-2xl">
        Nitro is OpenTelemetry-native. Every operation, service and client is
        measured the moment it runs — latency, throughput, errors and a ranked
        impact score — and each request leaves a trace you can follow from the
        GraphQL field all the way into the gRPC call that actually went slow.
      </p>

      <div className="mt-10 flex flex-wrap items-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
        </OutlineButton>
      </div>

      <div className="mt-16">
        <IncidentStitch />
      </div>
    </header>
  );
}

/* ---------------------------------------------------------------------------
   IncidentStitch — the signature visual. A floating Nitro dashboard tile
   mid-incident (checkout, p99 spiking, amber Investigating) with a single
   hairline "trace-id" stitching the spike down into the slow gRPC span of a
   compact waterfall. Built on a faint blueprint dot-grid.
--------------------------------------------------------------------------- */
function IncidentStitch() {
  return (
    <figure
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border px-5 py-6 backdrop-blur sm:px-8 sm:py-8"
      style={{
        backgroundImage:
          "radial-gradient(rgba(245,241,234,0.06) 1px, transparent 1px)",
        backgroundSize: "22px 22px",
      }}
    >
      <figcaption className="text-cc-nav-label mb-6 flex flex-wrap items-center justify-between gap-3 font-mono text-[0.7rem] tracking-[0.16em] uppercase">
        <span>Nitro · operation monitoring</span>
        <span className="text-cc-ink-dim flex items-center gap-2">
          trace
          <span className="bg-cc-code-bg text-cc-ink rounded px-2 py-0.5 tracking-normal lowercase normal-case">
            a3f9-c0d1-77e2
          </span>
        </span>
      </figcaption>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.15fr)]">
        {/* dashboard tile */}
        <div className="border-cc-card-border bg-cc-surface/70 rounded-xl border p-5">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-cc-nav-label font-mono text-[0.72rem] tracking-[0.14em] uppercase">
                Operation
              </p>
              <p className="text-cc-heading mt-1 font-mono">checkout</p>
            </div>
            <StatusBadge state="investigating" label="Investigating" />
          </div>

          <div className="mt-5 grid grid-cols-3 gap-3 font-mono">
            <Stat label="p95" value="42" unit="ms" tone="ok" />
            <Stat label="p99" value="318" unit="ms" tone="hot" />
            <Stat label="errors" value="0.3" unit="%" tone="warn" />
          </div>

          <div className="mt-5">
            <SparkSpike />
          </div>
        </div>

        {/* waterfall */}
        <div className="border-cc-card-border bg-cc-surface/70 rounded-xl border p-5">
          <p className="text-cc-nav-label font-mono text-[0.72rem] tracking-[0.14em] uppercase">
            Distributed trace
          </p>
          <p className="text-cc-ink-dim mt-1 mb-4 font-mono">
            mutation <span className="text-cc-heading">checkout</span>
          </p>
          <TraceWaterfall />
        </div>
      </div>

      {/* status legend: the full healthy → investigating → firing scale */}
      <div className="text-cc-nav-label mt-6 flex flex-wrap items-center gap-x-5 gap-y-2 font-mono text-[0.62rem] tracking-[0.14em] uppercase">
        <span className="tracking-[0.18em]">status</span>
        <StatusKey color={GREEN} label="Healthy" />
        <StatusKey color={AMBER} label="Investigating" />
        <StatusKey color={CORAL} label="Firing" />
      </div>

      {/* the stitch: hairline tying the spike to the slow span */}
      <svg
        className="pointer-events-none absolute inset-0 hidden h-full w-full lg:block"
        aria-hidden
        viewBox="0 0 100 100"
        preserveAspectRatio="none"
      >
        <defs>
          <linearGradient id="stitch-v2" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stopColor={AMBER} stopOpacity="0.55" />
            <stop offset="100%" stopColor={CORAL} stopOpacity="0.7" />
          </linearGradient>
        </defs>
        <path
          d="M41 64 C49 78 55 70 62 80"
          fill="none"
          stroke="url(#stitch-v2)"
          strokeWidth="1"
          strokeDasharray="3 4"
        />
      </svg>
    </figure>
  );
}

interface StatusBadgeProps {
  readonly state: "healthy" | "investigating" | "firing";
  readonly label: string;
}

function StatusBadge({ state, label }: StatusBadgeProps) {
  const color =
    state === "healthy" ? GREEN : state === "investigating" ? AMBER : CORAL;
  return (
    <span
      className="inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[0.68rem] tracking-[0.12em] uppercase"
      style={{
        borderColor: `${color}55`,
        backgroundColor: `${color}14`,
        color,
      }}
    >
      <span
        className="h-1.5 w-1.5 rounded-full"
        style={{ backgroundColor: color }}
      />
      {label}
    </span>
  );
}

interface StatusKeyProps {
  readonly color: string;
  readonly label: string;
}

function StatusKey({ color, label }: StatusKeyProps) {
  return (
    <span className="text-cc-ink-dim flex items-center gap-2">
      <span
        className="h-1.5 w-1.5 rounded-full"
        style={{ backgroundColor: color }}
      />
      {label}
    </span>
  );
}

interface StatProps {
  readonly label: string;
  readonly value: string;
  readonly unit: string;
  readonly tone: "ok" | "warn" | "hot";
}

function Stat({ label, value, unit, tone }: StatProps) {
  const color = tone === "ok" ? TEAL : tone === "warn" ? AMBER : CORAL;
  return (
    <div className="border-cc-card-border bg-cc-code-bg/60 rounded-lg border px-3 py-2.5">
      <p className="text-cc-nav-label text-[0.62rem] tracking-[0.14em] uppercase">
        {label}
      </p>
      <p className="mt-1 leading-none">
        <span className="text-xl" style={{ color }}>
          {value}
        </span>
        <span className="text-cc-ink-dim ml-0.5 text-[0.7rem]">{unit}</span>
      </p>
    </div>
  );
}

/* A latency sparkline with a coral spike and a teal under-fill, mid-incident. */
function SparkSpike() {
  const pts = "0,30 18,28 36,31 54,26 72,29 90,22 108,9 126,16 144,13 162,11";
  return (
    <div>
      <div className="text-cc-nav-label mb-2 flex items-center justify-between font-mono text-[0.62rem] tracking-[0.12em] uppercase">
        <span>p99 latency · last 30m</span>
        <span style={{ color: CORAL }}>+274ms</span>
      </div>
      <svg viewBox="0 0 162 40" className="h-16 w-full" aria-hidden>
        <defs>
          <linearGradient id="spark-fill-v2" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={TEAL} stopOpacity="0.3" />
            <stop offset="100%" stopColor={TEAL} stopOpacity="0" />
          </linearGradient>
        </defs>
        <polygon points={`${pts} 162,40 0,40`} fill="url(#spark-fill-v2)" />
        <polyline
          points={pts}
          fill="none"
          stroke={TEAL}
          strokeWidth="1.5"
          strokeLinejoin="round"
          strokeLinecap="round"
        />
        <circle cx="108" cy="9" r="2.4" fill={CORAL} />
      </svg>
    </div>
  );
}

interface SpanRow {
  readonly label: string;
  readonly kind: string;
  readonly offset: number;
  readonly width: number;
  readonly hot?: boolean;
  readonly ms: string;
}

function TraceWaterfall() {
  const spans: SpanRow[] = [
    { label: "api", kind: "graphql", offset: 0, width: 100, ms: "318ms" },
    { label: "users-svc", kind: "rest", offset: 4, width: 18, ms: "31ms" },
    {
      label: "billing",
      kind: "grpc",
      offset: 23,
      width: 58,
      hot: true,
      ms: "262ms",
    },
    { label: "settle-job", kind: "job", offset: 70, width: 22, ms: "44ms" },
    { label: "orders-db", kind: "db", offset: 83, width: 14, ms: "19ms" },
  ];
  return (
    <div className="space-y-2.5">
      {spans.map((s) => (
        <div
          key={s.label}
          className="grid grid-cols-[5.5rem_1fr] items-center gap-3"
        >
          <div className="text-cc-ink font-mono text-[0.72rem] leading-tight">
            {s.label}
            <span className="text-cc-nav-label block text-[0.58rem] tracking-[0.1em] uppercase">
              {s.kind}
            </span>
          </div>
          <div className="bg-cc-code-bg/70 relative h-5 rounded">
            <div
              className="absolute top-0 flex h-full items-center justify-end rounded px-1.5 font-mono text-[0.6rem]"
              style={{
                left: `${s.offset}%`,
                width: `${s.width}%`,
                backgroundColor: s.hot ? `${CORAL}28` : `${TEAL}1f`,
                boxShadow: s.hot ? `inset 0 0 0 1px ${CORAL}88` : "none",
                color: s.hot ? CORAL : "var(--color-cc-ink-dim)",
              }}
            >
              {s.ms}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

/* ===========================================================================
   SPEC LEDGER — a thin, full-width mono band of the believable numbers. Reads
   like a whitepaper figure caption, not a product screenshot.
=========================================================================== */
function SpecLedger() {
  const cells = [
    { k: "p95 latency", v: "42 ms", tone: GREEN },
    { k: "p99 latency", v: "318 ms", tone: AMBER },
    { k: "error rate", v: "0.3 %", tone: AMBER },
    { k: "#1 impact", v: "checkout", tone: CORAL },
  ];
  return (
    <section className="border-cc-card-border mt-24 border-y py-8">
      <div className="grid grid-cols-2 gap-y-8 sm:grid-cols-4">
        {cells.map((c, i) => (
          <div
            key={c.k}
            className={
              i < cells.length - 1
                ? "sm:border-cc-card-border sm:border-r sm:pr-6"
                : ""
            }
          >
            <p className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.18em] uppercase">
              {c.k}
            </p>
            <p
              className="font-heading mt-2 text-3xl font-semibold"
              style={{ color: c.tone }}
            >
              {c.v}
            </p>
          </div>
        ))}
      </div>
    </section>
  );
}

/* ===========================================================================
   NUMBERED NARRATIVE — 01 / 02 / 03. Alternating two-column prose + diagram.
=========================================================================== */
interface ChapterFrameProps {
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly children: ReactNode;
  readonly diagram: ReactNode;
  readonly reverse?: boolean;
}

function ChapterFrame({
  index,
  eyebrow,
  title,
  children,
  diagram,
  reverse,
}: ChapterFrameProps) {
  return (
    <section className="border-cc-card-border mt-28 border-t pt-12">
      <div className="grid gap-12 lg:grid-cols-2 lg:gap-16">
        <div className={reverse ? "lg:order-2" : ""}>
          <div className="flex items-baseline gap-4">
            <span
              className="font-heading text-5xl leading-none font-bold"
              style={{ color: TEAL }}
            >
              {index}
            </span>
            <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.2em] uppercase">
              {eyebrow}
            </span>
          </div>
          <h2 className="font-heading text-cc-heading mt-6 text-[clamp(1.9rem,4vw,2.75rem)] leading-[1.1] font-semibold">
            {title}
          </h2>
          <div className="text-body text-cc-ink-dim mt-6 space-y-4">
            {children}
          </div>
        </div>
        <div className={reverse ? "lg:order-1" : ""}>{diagram}</div>
      </div>
    </section>
  );
}

function ChapterOne() {
  return (
    <ChapterFrame
      index="01"
      eyebrow="Measure"
      title="Every operation, ranked by what it costs you."
      diagram={<ImpactTable />}
    >
      <p>
        Averages hide the operation that is quietly burning your error budget.
        Nitro ranks operations by an{" "}
        <strong className="text-cc-heading">impact score</strong> that folds
        latency, throughput and failure rate into one number, so the thing
        hurting production rises to the top on its own.
      </p>
      <p>
        Split the same numbers by service or by client to see who is applying
        the pressure. The metrics are standard OpenTelemetry — p95, p99,
        throughput and error rate — collected per operation with no proprietary
        agent in the path.
      </p>
    </ChapterFrame>
  );
}

interface ImpactRow {
  readonly op: string;
  readonly p95: string;
  readonly err: string;
  readonly impact: number;
  readonly status: "ok" | "warn" | "err";
}

function ImpactTable() {
  const rows: ImpactRow[] = [
    { op: "checkout", p95: "42ms", err: "0.3%", impact: 98, status: "err" },
    { op: "cartAddItem", p95: "11ms", err: "0.0%", impact: 61, status: "warn" },
    { op: "productById", p95: "6ms", err: "0.0%", impact: 38, status: "ok" },
    { op: "searchCatalog", p95: "19ms", err: "0.1%", impact: 24, status: "ok" },
    { op: "viewerProfile", p95: "4ms", err: "0.0%", impact: 9, status: "ok" },
  ];
  const dot = (s: ImpactRow["status"]) =>
    s === "ok" ? GREEN : s === "warn" ? AMBER : CORAL;
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur"
      style={{
        backgroundImage:
          "radial-gradient(rgba(245,241,234,0.05) 1px, transparent 1px)",
        backgroundSize: "20px 20px",
      }}
    >
      <div className="border-cc-card-border text-cc-nav-label grid grid-cols-[1.4fr_0.7fr_0.7fr_1fr] gap-2 border-b pb-3 font-mono text-[0.62rem] tracking-[0.14em] uppercase">
        <span>operation</span>
        <span className="text-right">p95</span>
        <span className="text-right">err</span>
        <span className="text-right">impact</span>
      </div>
      <div className="divide-cc-card-border divide-y">
        {rows.map((r) => (
          <div
            key={r.op}
            className="grid grid-cols-[1.4fr_0.7fr_0.7fr_1fr] items-center gap-2 py-3 font-mono text-[0.8rem]"
          >
            <span className="text-cc-heading flex items-center gap-2">
              <span
                className="h-1.5 w-1.5 shrink-0 rounded-full"
                style={{ backgroundColor: dot(r.status) }}
              />
              {r.op}
            </span>
            <span className="text-cc-ink-dim text-right">{r.p95}</span>
            <span className="text-cc-ink-dim text-right">{r.err}</span>
            <span className="flex items-center justify-end gap-2">
              <span className="bg-cc-code-bg h-1 w-12 overflow-hidden rounded-full">
                <span
                  className="block h-full rounded-full"
                  style={{
                    width: `${r.impact}%`,
                    backgroundColor: dot(r.status),
                  }}
                />
              </span>
              <span className="text-cc-ink w-6 text-right">{r.impact}</span>
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

function ChapterTwo() {
  return (
    <ChapterFrame
      index="02"
      eyebrow="Follow"
      title="One trace, from the GraphQL field to the slow hop."
      reverse
      diagram={<TopologyDiagram />}
    >
      <p>
        A GraphQL request rarely stops at GraphQL. The{" "}
        <code className="bg-cc-code-bg text-cc-heading rounded px-1.5 py-0.5 font-mono text-[0.85em]">
          checkout
        </code>{" "}
        mutation fans out into a REST lookup, a gRPC billing call, a background
        settlement job and a database write — and any one of them can be the
        problem.
      </p>
      <p>
        Nitro&apos;s distributed traces span all of it. The hop that is actually
        slow lights up in the topology, so debugging starts from evidence
        instead of a guess about which service to blame.
      </p>
    </ChapterFrame>
  );
}

/* A converging service-topology graph. GraphQL at the top fans down into REST /
   gRPC / job / DB; the hot gRPC hop glows. Downward, blueprint-line aesthetic. */
function TopologyDiagram() {
  return (
    <div
      className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 backdrop-blur"
      style={{
        backgroundImage:
          "radial-gradient(rgba(245,241,234,0.05) 1px, transparent 1px)",
        backgroundSize: "20px 20px",
      }}
    >
      <p className="text-cc-nav-label mb-4 font-mono text-[0.62rem] tracking-[0.16em] uppercase">
        service topology · checkout
      </p>
      <svg viewBox="0 0 320 260" className="w-full" aria-hidden>
        <defs>
          <filter id="glow-v2" x="-50%" y="-50%" width="200%" height="200%">
            <feGaussianBlur stdDeviation="3.5" result="b" />
            <feMerge>
              <feMergeNode in="b" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>

        {/* edges */}
        <g
          fill="none"
          stroke="rgba(245,241,234,0.18)"
          strokeWidth="1"
          strokeDasharray="3 3"
        >
          <path d="M160 42 L70 110" />
          <path d="M160 42 L250 110" />
          <path d="M70 110 L70 184" />
          <path d="M250 110 L250 184" />
          <path d="M70 184 L160 226" />
          <path d="M250 184 L160 226" />
        </g>
        {/* hot edge */}
        <path
          d="M160 42 L250 110"
          fill="none"
          stroke={CORAL}
          strokeWidth="1.4"
          filter="url(#glow-v2)"
        />

        <TopoNode x={160} y={42} label="api" kind="graphql" />
        <TopoNode x={70} y={110} label="users-svc" kind="rest" />
        <TopoNode x={250} y={110} label="billing" kind="gRPC" hot />
        <TopoNode x={70} y={184} label="settle-job" kind="job" />
        <TopoNode x={250} y={184} label="ledger" kind="grpc" />
        <TopoNode x={160} y={226} label="orders-db" kind="db" />
      </svg>
      <div className="text-cc-ink-dim mt-4 flex items-center justify-center gap-2 font-mono text-[0.62rem]">
        <span
          className="h-1.5 w-1.5 rounded-full"
          style={{ backgroundColor: CORAL }}
        />
        hot hop · billing gRPC · 262ms
      </div>
    </div>
  );
}

interface TopoNodeProps {
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly kind: string;
  readonly hot?: boolean;
}

function TopoNode({ x, y, label, kind, hot }: TopoNodeProps) {
  const stroke = hot ? CORAL : "rgba(94,234,212,0.5)";
  const fill = hot ? `${CORAL}1a` : "rgba(94,234,212,0.08)";
  return (
    <g filter={hot ? "url(#glow-v2)" : undefined}>
      <rect
        x={x - 42}
        y={y - 16}
        width={84}
        height={32}
        rx={7}
        fill={fill}
        stroke={stroke}
        strokeWidth="1"
      />
      <text
        x={x}
        y={y - 1}
        textAnchor="middle"
        fontFamily="var(--font-mono, monospace)"
        fontSize="9.5"
        fill="var(--color-cc-heading)"
      >
        {label}
      </text>
      <text
        x={x}
        y={y + 9}
        textAnchor="middle"
        fontFamily="var(--font-mono, monospace)"
        fontSize="6.5"
        letterSpacing="0.12em"
        fill={hot ? CORAL : "var(--color-cc-nav-label)"}
        style={{ textTransform: "uppercase" }}
      >
        {kind}
      </text>
    </g>
  );
}

function ChapterThree() {
  return (
    <ChapterFrame
      index="03"
      eyebrow="Wire"
      title="It is OpenTelemetry, not a new vendor."
      diagram={<WireSpec />}
    >
      <p>
        Telemetry leaves your service through the standard OTLP exporter. Point
        it at Nitro and the operation, service and client views light up — the
        same traces and metrics also work with anything else that speaks
        OpenTelemetry, so you are never locked to one screen.
      </p>
      <p>
        The GraphQL IDE can be served straight from your Hot Chocolate endpoint
        for local exploration. The telemetry dashboards are a separate, opt-in
        step you configure in Nitro — two distinct things, kept honest and kept
        apart.
      </p>
    </ChapterFrame>
  );
}

/* A code-or-spec side panel: OTLP wiring as terse, accurate config. */
function WireSpec() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg overflow-hidden rounded-2xl border">
      <div className="border-cc-card-border bg-cc-code-header text-cc-nav-label flex items-center justify-between border-b px-4 py-2.5 font-mono text-[0.66rem] tracking-[0.12em] uppercase">
        <span>Program.cs</span>
        <span className="text-cc-ink-dim flex items-center gap-1.5">
          <span
            className="h-1.5 w-1.5 rounded-full"
            style={{ backgroundColor: GREEN }}
          />
          exporting
        </span>
      </div>
      <pre className="text-cc-ink-dim overflow-x-auto px-4 py-4 font-mono text-[0.78rem] leading-relaxed">
        <code>
          <span className="text-cc-nav-label">
            {"// OpenTelemetry-native, OTLP out"}
          </span>
          {"\n"}
          builder.Services{"\n"}
          {"  "}.<span style={{ color: TEAL }}>AddOpenTelemetry</span>(){"\n"}
          {"  "}.<span style={{ color: TEAL }}>WithTracing</span>(t {"=> "}t
          {"\n"}
          {"    "}.
          <span style={{ color: TEAL }}>AddHotChocolateInstrumentation</span>()
          {"\n"}
          {"    "}.<span style={{ color: TEAL }}>AddOtlpExporter</span>());
          {"\n"}
          {"\n"}
          <span className="text-cc-nav-label">
            {"// dashboards: configured in Nitro,"}
          </span>
          {"\n"}
          <span className="text-cc-nav-label">
            {"// the IDE is served from the endpoint."}
          </span>
        </code>
      </pre>
    </div>
  );
}

/* ===========================================================================
   HONESTY BEAT — credibility, framed as a typographic pull-quote with three
   precise commitments. Monochrome, accent rationed as the check ink.
=========================================================================== */
function HonestyBeat() {
  const points = [
    "Standard OpenTelemetry traces and metrics — no proprietary agent in the request path.",
    "Distributed traces span GraphQL, REST, gRPC and background jobs in a single timeline.",
    "The GraphQL IDE and the telemetry dashboards are configured separately, on purpose.",
  ];
  return (
    <section className="border-cc-card-border mt-28 border-t pt-12">
      <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.2em] uppercase">
        What we will not overclaim
      </p>
      <blockquote className="font-heading text-cc-heading mt-6 max-w-3xl text-[clamp(1.5rem,3.2vw,2.25rem)] leading-[1.2] font-semibold">
        Observability earns trust by being{" "}
        <span style={{ color: TEAL }}>accurate</span>, not by being loud.
      </blockquote>
      <ul className="border-cc-card-border bg-cc-card-border mt-10 grid gap-px overflow-hidden rounded-2xl border sm:grid-cols-3">
        {points.map((p) => (
          <li
            key={p}
            className="bg-cc-surface/80 text-body text-cc-ink-dim flex flex-col gap-3 p-6"
          >
            <span style={{ color: TEAL }}>
              <CheckIcon size={16} />
            </span>
            {p}
          </li>
        ))}
      </ul>
    </section>
  );
}

/* ===========================================================================
   CLOSING CTA — shared pair.
=========================================================================== */
function ClosingCta() {
  return (
    <section className="border-cc-card-border mt-28 mb-8 border-t pt-16 text-center">
      <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.2em] uppercase">
        Production View
      </p>
      <h2 className="font-heading text-cc-heading mx-auto mt-6 max-w-3xl text-[clamp(2rem,5vw,3.625rem)] leading-[1.08] font-semibold">
        Stop guessing. Read the evidence the API already gave you.
      </h2>
      <p className="text-body text-cc-ink-dim mx-auto mt-6 max-w-xl">
        Wire up OpenTelemetry, point it at Nitro, and watch the next incident
        explain itself.
      </p>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
        </OutlineButton>
      </div>
    </section>
  );
}
