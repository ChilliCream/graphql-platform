import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Nitro Analytics: GraphQL Observability for .NET",
  description:
    "GraphQL observability for .NET on OpenTelemetry. A five-step trail: point telemetry at Nitro, rank by impact, open the trace, see the clients, one pane.",
  keywords: [
    "GraphQL analytics",
    "OpenTelemetry .NET",
    "Nitro dashboard",
    "distributed tracing",
    "p95 p99 latency",
    "impact score",
    "per-client usage",
    "Hot Chocolate telemetry",
    "cross-service monitoring",
  ],
  openGraph: {
    title: "Nitro Analytics: Walk the Path From Spike to Span",
    description:
      "A five-step trail through Nitro's GraphQL observability for .NET on OpenTelemetry: operation impact, distributed tracing, per-client share, cross-service views.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ *
 * Palette. Teal is the signature accent on cc-bg. Status colours are
 * data, not decoration. The brand spectrum is spent exactly once at
 * the closing CTA, terminating the vertical trail.
 * ------------------------------------------------------------------ */

const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";
const CYAN = "#16b9e4";
const SOFT_VIOLET = "#8b9bd4";

const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, ${VIOLET} 52%, ${CORAL} 100%)`;

export default function AnalyticsPreviewV4Page() {
  return (
    <main className="pb-16">
      <div className="relative mx-auto max-w-3xl px-6">
        <Hero />
        <Trail>
          <StepOne />
          <StepTwo />
          <StepThree />
          <StepFour />
          <StepFive />
        </Trail>
        <ClosingCta />
      </div>
    </main>
  );
}

/* ================================================================== *
 * HERO (marker 00 / start)
 * Centered above where the vertical trail begins.
 * ================================================================== */

function Hero() {
  return (
    <section className="relative isolate pt-10 pb-16 text-center">
      <HeroGlow />
      <div className="text-cc-nav-label inline-flex items-center gap-3 font-mono text-[11px] tracking-[0.28em] uppercase">
        <span className="text-cc-accent">00</span>
        <span className="text-cc-ink-faint">/</span>
        <span>start</span>
      </div>
      <p className="text-cc-nav-label mt-6 font-mono text-xs tracking-[0.28em] uppercase">
        Nitro analytics
      </p>
      <h1 className="font-heading text-hero text-cc-heading mt-6">
        Walk the path from
        <br />
        spike to span.
      </h1>
      <p className="lead text-cc-prose mx-auto mt-7 max-w-2xl">
        GraphQL observability for .NET on OpenTelemetry. Five deliberate steps:
        point telemetry at Nitro, rank operations by impact, open the trace, see
        the clients, and watch every service share one pipeline.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Get Started
        </SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch
        </OutlineButton>
      </div>
      <HeroStatStrip />
    </section>
  );
}

function HeroGlow() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute -top-16 left-1/2 -z-10 h-[420px] w-[760px] -translate-x-1/2 opacity-60 blur-3xl"
      style={{
        background: `radial-gradient(50% 50% at 50% 40%, ${TEAL}1f 0%, transparent 70%), radial-gradient(40% 40% at 70% 60%, ${VIOLET}1a 0%, transparent 72%)`,
      }}
    />
  );
}

function HeroStatStrip() {
  const stats = [
    { label: "p95", value: "live" },
    { label: "p99", value: "live" },
    { label: "impact", value: "ranked" },
    { label: "clients", value: "by version" },
  ];
  return (
    <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 inline-flex flex-wrap items-center justify-center gap-x-6 gap-y-3 rounded-full border px-5 py-2.5 backdrop-blur-md">
      {stats.map((s, i) => (
        <span
          key={s.label}
          className="text-cc-nav-label flex items-center gap-2 font-mono text-[11px]"
        >
          <span className="bg-cc-accent h-1.5 w-1.5 rounded-full" />
          <span className="tracking-wide uppercase">{s.label}</span>
          <span className="text-cc-ink-dim">{s.value}</span>
          {i < stats.length - 1 && (
            <span className="text-cc-ink-faint pl-3">·</span>
          )}
        </span>
      ))}
    </div>
  );
}

/* ================================================================== *
 * TRAIL container. The vertical rule. Each Step plants a numeral that
 * sits flush-top against the rule, with a 1px teal cross-tick.
 * ================================================================== */

interface TrailProps {
  readonly children: React.ReactNode;
}

function Trail({ children }: TrailProps) {
  return (
    <div className="relative">
      <div
        aria-hidden
        className="bg-cc-card-border pointer-events-none absolute top-0 bottom-0 left-12 w-px"
      />
      {children}
    </div>
  );
}

/* ================================================================== *
 * STEP primitive. Two-column row: 96px gutter for the numeral,
 * flexible body. Tall vertical rhythm between steps.
 * ================================================================== */

interface StepProps {
  readonly number: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly lead: string;
  readonly children: React.ReactNode;
  readonly first?: boolean;
}

function Step({ number, eyebrow, title, lead, children, first }: StepProps) {
  return (
    <section
      className={`relative grid grid-cols-[96px_1fr] ${first ? "pt-4 pb-20" : "py-20"}`}
    >
      <div className="relative">
        <span
          aria-hidden
          className="bg-cc-accent absolute top-3 left-12 h-px w-4"
        />
        <span
          className="text-cc-accent text-h2 block font-mono tabular-nums"
          style={{ fontWeight: 500, lineHeight: 1 }}
        >
          {number}
        </span>
      </div>
      <div className="pt-1 pl-2">
        <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.28em] uppercase">
          {eyebrow}
        </p>
        <h2 className="font-heading text-h3 text-cc-heading mt-4">{title}</h2>
        <p className="text-lead text-cc-prose mt-5">{lead}</p>
        <div className="mt-7">{children}</div>
      </div>
    </section>
  );
}

/* ================================================================== *
 * STEP 01 - Point telemetry at Nitro
 * Honesty caveat surfaced early, plus a small mono code chip.
 * ================================================================== */

function StepOne() {
  return (
    <Step
      first
      number="01"
      eyebrow="Point telemetry at Nitro"
      title="Wire it up once, deliberately."
      lead="The dashboards do not turn on by themselves. You add the Nitro OpenTelemetry package to your .NET services and add Nitro as an exporter to the OTel pipeline you already configure. A configuration step, documented, not magic."
    >
      <p className="text-body text-cc-ink-dim">
        It is OpenTelemetry end to end. Vendor-neutral spans mean your data is
        yours, and there is no proprietary agent locking the trace in.
      </p>
      <CodeChip />
    </Step>
  );
}

function CodeChip() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg mt-7 overflow-hidden rounded-xl border backdrop-blur-md">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center gap-2 border-b px-4 py-2.5">
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-ink-faint h-2.5 w-2.5 rounded-full" />
        <span className="text-cc-nav-label ml-2 font-mono text-[11px]">
          Program.cs
        </span>
        <span className="text-cc-nav-label ml-auto font-mono text-[10px] tracking-wide uppercase">
          dotnet
        </span>
      </div>
      <div className="space-y-1.5 px-5 py-5 font-mono text-[12px] leading-relaxed">
        <div>
          <span className="text-cc-nav-label">{"// 1. add the package"}</span>
        </div>
        <div className="text-cc-ink-dim">
          <span className="text-cc-accent">dotnet add</span> package
          ChilliCream.Nitro.OpenTelemetry
        </div>
        <div className="h-2" />
        <div>
          <span className="text-cc-nav-label">{"// 2. wire the exporter"}</span>
        </div>
        <div className="text-cc-ink-dim">
          builder.Services.AddOpenTelemetry()
        </div>
        <div className="text-cc-ink-dim pl-4">
          .WithTracing(t =&gt; t.
          <span className="text-cc-accent">AddNitroExporter</span>())
        </div>
        <div className="text-cc-ink-dim pl-4">
          .WithMetrics(m =&gt; m.
          <span className="text-cc-accent">AddNitroExporter</span>());
        </div>
      </div>
    </div>
  );
}

/* ================================================================== *
 * STEP 02 - Rank by impact, not noise
 * OperationsTable artifact, full-width inside the column.
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

function StepTwo() {
  return (
    <Step
      number="02"
      eyebrow="Operation insights"
      title="Rank by impact, not noise."
      lead="The Nitro impact score folds p95, p99, throughput, and error rate into one number, so the operation at the top is the one worth opening first."
    >
      <p className="text-body text-cc-ink-dim">
        Latency distributions, not averages. Throughput and error rate as
        siblings, never separated. The row that owes you attention surfaces
        without you scanning a wall of charts.
      </p>
      <OperationsTable />
    </Step>
  );
}

function OperationsTable() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg mt-7 overflow-hidden rounded-2xl border backdrop-blur-md">
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
      <div className="text-cc-nav-label border-cc-card-border/50 grid grid-cols-[28px_1fr_44px_44px_44px_44px_56px] gap-2 border-b px-4 py-2.5 font-mono text-[10px] tracking-wide uppercase">
        <span />
        <span>operation</span>
        <span className="text-right">p95</span>
        <span className="text-right">p99</span>
        <span className="text-right">rpm</span>
        <span className="text-right">err</span>
        <span className="text-right">impact</span>
      </div>
      <div className="divide-cc-card-border/40 divide-y">
        {OP_ROWS.map((row) => (
          <OperationRow key={row.name} row={row} />
        ))}
      </div>
    </div>
  );
}

interface OperationRowProps {
  readonly row: OpRow;
}

function OperationRow({ row }: OperationRowProps) {
  const isHot = row.status === "fire";
  return (
    <div
      className={`grid grid-cols-[28px_1fr_44px_44px_44px_44px_56px] items-center gap-2 px-4 py-3 font-mono text-[11px] ${
        isHot ? "bg-cc-surface/80" : "bg-cc-surface/40"
      }`}
      style={isHot ? { boxShadow: `inset 0 0 0 1px ${CORAL}33` } : undefined}
    >
      <span className="text-cc-nav-label text-[10px]">#{row.rank}</span>
      <span className="flex items-center gap-2 truncate">
        <StatusDot
          color={STATUS_COLOR[row.status]}
          pulse={row.status === "fire"}
        />
        <span
          className={`truncate ${isHot ? "text-cc-heading" : "text-cc-ink-dim"}`}
        >
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
        <ImpactBar value={row.impact} status={row.status} />
        <span className="text-cc-ink-dim w-5 text-right text-[10px]">
          {row.impact}
        </span>
      </span>
    </div>
  );
}

interface ImpactBarProps {
  readonly value: number;
  readonly status: OpRow["status"];
}

function ImpactBar({ value, status }: ImpactBarProps) {
  return (
    <span className="bg-cc-surface/80 relative inline-block h-1.5 w-10 overflow-hidden rounded-full">
      <span
        className="absolute inset-y-0 left-0 rounded-full"
        style={{
          width: `${value}%`,
          backgroundColor: STATUS_COLOR[status],
        }}
      />
    </span>
  );
}

/* ================================================================== *
 * STEP 03 - Open the trace, see the slow span
 * TraceWaterfall artifact.
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
  job: SOFT_VIOLET,
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
    label: "users-svc · GET /me",
    kind: "rest",
    start: 4,
    width: 11,
    ms: "21ms",
  },
  {
    id: "s2",
    label: "billing · Charge()",
    kind: "grpc",
    start: 16,
    width: 64,
    ms: "201ms",
    slow: true,
  },
  {
    id: "s3",
    label: "db · SELECT account",
    kind: "db",
    start: 20,
    width: 12,
    ms: "9ms",
  },
  {
    id: "s4",
    label: "worker · enqueue receipt",
    kind: "job",
    start: 82,
    width: 13,
    ms: "37ms",
  },
];

const SPAN_DEPTH: readonly number[] = [0, 1, 1, 2, 2];

function StepThree() {
  return (
    <Step
      number="03"
      eyebrow="Distributed tracing"
      title="Open the trace, see the slow span."
      lead="From the impact row, one click opens the trace. Every hop is a real OpenTelemetry span: GraphQL at the root, REST and gRPC across services, the database read, the background job enqueued at the end."
    >
      <p className="text-body text-cc-ink-dim">
        The hop that is actually slow is highlighted in coral, not buried below
        averages. Open standard underneath, the spans are your data.
      </p>
      <TraceWaterfall />
    </Step>
  );
}

function TraceWaterfall() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg mt-7 overflow-hidden rounded-2xl border backdrop-blur-md">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3">
        <span className="text-cc-nav-label font-mono text-[11px]">trace</span>
        <span className="text-cc-accent font-mono text-[11px]">
          4b1c8f2a9e07
        </span>
        <span className="text-cc-ink-faint font-mono text-[11px]">·</span>
        <span className="text-cc-ink-dim font-mono text-[11px]">
          mutation checkout
        </span>
        <span className="text-cc-nav-label ml-auto inline-flex items-center gap-1.5 font-mono text-[11px]">
          duration <span className="text-cc-heading">318ms</span>
        </span>
      </div>
      <div className="px-5 py-5">
        <div className="space-y-2.5">
          {SPANS.map((span, i) => (
            <SpanRow key={span.id} span={span} depth={SPAN_DEPTH[i] ?? 0} />
          ))}
        </div>
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
      <div
        className="flex w-[38%] shrink-0 items-center gap-2 truncate"
        style={{ paddingLeft: depth * 12 }}
      >
        <span
          className="rounded px-1.5 py-0.5 font-mono text-[9px] font-semibold tracking-wide uppercase"
          style={{ color, backgroundColor: `${color}1a` }}
        >
          {KIND_LABEL[span.kind]}
        </span>
        <span
          className={`truncate font-mono text-[11px] ${isRoot ? "text-cc-heading" : "text-cc-ink-dim"}`}
        >
          {span.label}
        </span>
      </div>
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

/* ================================================================== *
 * STEP 04 - Know which clients are on the line
 * ClientShareTile artifact.
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

function StepFour() {
  return (
    <Step
      number="04"
      eyebrow="Per-client usage"
      title="Know which clients are on the line."
      lead="Nitro registers your clients by name and version. The same telemetry that fuels the operations table breaks down by caller, so you can see which published clients are affected before you ship a fix."
    >
      <p className="text-body text-cc-ink-dim">
        Version-aware: storefront@4.2.0 vs storefront@4.1.x, side by side. Ties
        cleanly into the schema registry for deprecation work.
      </p>
      <ClientShareTile />
    </Step>
  );
}

function ClientShareTile() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg mt-7 overflow-hidden rounded-2xl border backdrop-blur-md">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
          clients · checkout · 1h
        </span>
        <span className="text-cc-ink-faint font-mono text-[10px]">nitro</span>
      </div>
      <div className="px-5 py-5">
        <div className="flex h-2 overflow-hidden rounded-full">
          {CLIENT_ROWS.map((c) => (
            <span
              key={c.name}
              style={{
                width: `${c.share}%`,
                backgroundColor: STATUS_COLOR[c.status],
                opacity: c.status === "ok" ? 0.55 : 1,
              }}
            />
          ))}
        </div>
        <div className="mt-5 space-y-2">
          {CLIENT_ROWS.map((c) => (
            <div
              key={c.name}
              className="flex items-center gap-3 rounded-lg px-2.5 py-2"
              style={{
                backgroundColor:
                  c.status === "fire"
                    ? "rgba(240, 120, 106, 0.07)"
                    : "rgba(12, 19, 34, 0.4)",
              }}
            >
              <StatusDot color={STATUS_COLOR[c.status]} />
              <span
                className={`flex-1 truncate font-mono text-[11px] ${
                  c.status === "fire" ? "text-cc-heading" : "text-cc-ink-dim"
                }`}
              >
                {c.name}
              </span>
              <ShareBar share={c.share} status={c.status} />
              <span className="text-cc-nav-label w-10 text-right font-mono text-[10px]">
                {c.share}%
              </span>
              <span className="text-cc-ink-dim w-12 text-right font-mono text-[10px]">
                {c.rpm}
              </span>
            </div>
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
}

function ShareBar({ share, status }: ShareBarProps) {
  return (
    <span className="bg-cc-surface/80 relative inline-block h-1.5 w-16 overflow-hidden rounded-full">
      <span
        className="absolute inset-y-0 left-0 rounded-full"
        style={{
          width: `${share}%`,
          backgroundColor: STATUS_COLOR[status],
        }}
      />
    </span>
  );
}

/* ================================================================== *
 * STEP 05 - See every .NET service in one pane
 * Service-kind chip rail + a single honesty line.
 * ================================================================== */

interface ServiceKind {
  readonly key: Span["kind"];
  readonly label: string;
  readonly note: string;
}

const SERVICE_KINDS: readonly ServiceKind[] = [
  { key: "graphql", label: "GraphQL", note: "Hot Chocolate" },
  { key: "rest", label: "REST", note: "ASP.NET Core" },
  { key: "grpc", label: "gRPC", note: "service to service" },
  { key: "job", label: "Job", note: "background workers" },
];

function StepFive() {
  return (
    <Step
      number="05"
      eyebrow="Cross-service .NET monitoring"
      title="See every .NET service in one pane."
      lead="GraphQL, REST, gRPC, and background jobs share one OpenTelemetry pipeline, so a single trace can carry the whole call. Nitro ingests it and lets you ask the operation and the service the same questions."
    >
      <p className="text-body text-cc-ink-dim">
        Wire your services through{" "}
        <code className="text-cc-ink font-mono">
          ChilliCream.Nitro.OpenTelemetry
        </code>
        . Nitro reads the spans, not a sidecar agent.
      </p>
      <ServiceChipRail />
      <div
        className="border-cc-card-border/70 mt-7 flex items-start gap-2.5 rounded-xl border-l-2 bg-transparent py-2 pl-4"
        style={{ borderLeftColor: "var(--color-cc-accent)" }}
      >
        <h3 className="text-h6 text-cc-heading font-heading">
          Open standard underneath, the spans are your data.
        </h3>
      </div>
    </Step>
  );
}

function ServiceChipRail() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg mt-7 overflow-hidden rounded-2xl border backdrop-blur-md">
      <div className="border-cc-card-border/60 bg-cc-code-header/70 flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
          service kinds
        </span>
        <span className="text-cc-ink-faint font-mono text-[10px]">
          one pipeline
        </span>
      </div>
      <div className="grid grid-cols-2 gap-3 px-5 py-5 sm:grid-cols-4">
        {SERVICE_KINDS.map((s) => (
          <ServiceChip key={s.key} kind={s} />
        ))}
      </div>
    </div>
  );
}

interface ServiceChipProps {
  readonly kind: ServiceKind;
}

function ServiceChip({ kind }: ServiceChipProps) {
  const color = KIND_COLOR[kind.key];
  return (
    <div
      className="bg-cc-surface/50 flex flex-col gap-2 rounded-lg border px-3 py-3"
      style={{ borderColor: `${color}33` }}
    >
      <span
        className="self-start rounded px-1.5 py-0.5 font-mono text-[10px] font-semibold tracking-wide uppercase"
        style={{ color, backgroundColor: `${color}1a` }}
      >
        {kind.label}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px]">{kind.note}</span>
      <div className="bg-cc-surface/60 mt-1 h-1 overflow-hidden rounded-full">
        <span
          className="block h-full"
          style={{ width: "100%", backgroundColor: color, opacity: 0.55 }}
        />
      </div>
    </div>
  );
}

/* ================================================================== *
 * CLOSING CTA (marker 06 / launch)
 * The ladder terminates into the single spectrum hairline.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="relative mt-4 grid grid-cols-[96px_1fr] pt-10 pb-14">
      <div className="relative">
        <span
          aria-hidden
          className="absolute top-3 left-12 h-px w-4"
          style={{ background: SPECTRUM }}
        />
        <span
          className="text-cc-accent text-h2 block font-mono tabular-nums"
          style={{ fontWeight: 500, lineHeight: 1 }}
        >
          06
        </span>
      </div>
      <div className="pt-1 pl-2">
        <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.28em] uppercase">
          <span className="text-cc-accent">/</span> launch
        </p>
        <div
          aria-hidden
          className="mt-6 h-px w-full"
          style={{ background: SPECTRUM }}
        />
        <h2 className="font-heading text-h2 text-cc-heading mt-8">
          The trail ends in the dashboard.
        </h2>
        <p className="lead text-cc-prose mt-5">
          Point OpenTelemetry at Nitro once and every request becomes evidence,
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
        <ul className="text-caption text-cc-ink-dim mt-9 space-y-3">
          <CheckLine>
            Five steps, one OpenTelemetry pipeline, all your .NET services
          </CheckLine>
          <CheckLine>
            Ranked operations, distributed traces, per-client share
          </CheckLine>
          <CheckLine>
            Open standard underneath, the spans are your data
          </CheckLine>
        </ul>
      </div>
    </section>
  );
}

/* ================================================================== *
 * Shared primitives
 * ================================================================== */

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
      <span className="text-cc-accent mt-0.5 shrink-0">
        <CheckIcon size={14} />
      </span>
      <span>{children}</span>
    </li>
  );
}
