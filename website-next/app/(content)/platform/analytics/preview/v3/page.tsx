import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroMonitoringReel } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro Analytics | Metric Catalogue for .NET GraphQL",
  description:
    "OpenTelemetry-native analytics for .NET GraphQL: p95/p99 + throughput, impact score, per-client usage, distributed traces, REST and gRPC service monitoring.",
  keywords: [
    "GraphQL analytics",
    "Nitro analytics",
    "OpenTelemetry .NET",
    "p95 p99 latency",
    "impact score",
    "distributed tracing",
    "per-client usage",
    "Hot Chocolate observability",
    "REST gRPC monitoring",
    "operation monitoring",
  ],
  openGraph: {
    title: "Nitro Analytics, Metric Catalogue",
    description:
      "Operation insights, impact score, per-client usage, distributed traces, and cross-service .NET monitoring. OpenTelemetry in, evidence out.",
  },
  robots: { index: false, follow: false },
};

/* ----------------------------------------------------------------------------
   Scene palette. Teal is the signature. The brand spectrum (cyan, violet,
   coral) appears exactly once, inside the trace pipeline diagram. Status
   colors are rationed and used only where they carry data.
---------------------------------------------------------------------------- */
const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const CYAN = "#16b9e4";
const VIOLET = "#7c92c6";

/* ============================================================================
   Primitives
============================================================================ */

interface EyebrowProps {
  readonly tag: string;
  readonly children: ReactNode;
  readonly color?: string;
}

function Eyebrow({ tag, children, color = TEAL }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label flex items-center gap-2 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
      <span style={{ color }}>{tag}</span>
      <span className="bg-cc-card-border h-px w-6" aria-hidden />
      {children}
    </p>
  );
}

interface CardProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly glow?: string;
}

function Card({ children, className = "", glow }: CardProps) {
  return (
    <div
      className={`border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur ${className}`}
    >
      {glow && (
        <div
          className="pointer-events-none absolute -top-20 -right-16 h-44 w-44 rounded-full opacity-40 blur-3xl"
          style={{ backgroundColor: `${glow}55` }}
          aria-hidden
        />
      )}
      {children}
    </div>
  );
}

interface TileHeaderProps {
  readonly index: string;
  readonly title: string;
  readonly accent: string;
}

function TileHeader({ index, title, accent }: TileHeaderProps) {
  return (
    <div className="flex items-baseline gap-3 px-5 pt-5">
      <span
        className="font-mono text-[0.6rem] tracking-[0.14em] tabular-nums"
        style={{ color: accent }}
      >
        {index}
      </span>
      <h3 className="font-heading text-h6 text-cc-heading">{title}</h3>
    </div>
  );
}

interface TileFootnoteProps {
  readonly children: ReactNode;
}

function TileFootnote({ children }: TileFootnoteProps) {
  return (
    <p className="text-cc-ink-dim border-cc-card-border/60 mt-auto border-t px-5 py-3 text-[0.74rem] leading-relaxed">
      {children}
    </p>
  );
}

/* ============================================================================
   HERO
============================================================================ */

interface HeroPillProps {
  readonly label: string;
  readonly accent: string;
}

function HeroPill({ label, accent }: HeroPillProps) {
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.62rem] tracking-[0.1em] uppercase"
      style={{
        color: accent,
        borderColor: `${accent}44`,
        backgroundColor: `${accent}10`,
      }}
    >
      <span
        className="h-1.5 w-1.5 rounded-full"
        style={{ backgroundColor: accent, boxShadow: `0 0 8px ${accent}aa` }}
        aria-hidden
      />
      {label}
    </span>
  );
}

function Hero() {
  return (
    <header className="border-cc-card-border bg-cc-surface/40 relative overflow-hidden rounded-3xl border px-6 py-14 backdrop-blur sm:px-12 sm:py-20">
      <div
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(60% 70% at 10% -10%, rgba(94,234,212,0.18), transparent 60%), radial-gradient(40% 60% at 100% 0%, rgba(124,146,198,0.10), transparent 60%)",
        }}
        aria-hidden
      />
      <div
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.05]"
        style={{
          backgroundImage:
            "linear-gradient(rgba(245,241,234,1) 1px, transparent 1px), linear-gradient(90deg, rgba(245,241,234,1) 1px, transparent 1px)",
          backgroundSize: "44px 44px",
          maskImage:
            "radial-gradient(70% 70% at 20% 20%, #000 30%, transparent 80%)",
        }}
        aria-hidden
      />

      <div className="mx-auto max-w-4xl text-center">
        <Eyebrow tag="catalogue">
          <span>nitro analytics for .NET</span>
        </Eyebrow>
        <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-6 tracking-tight">
          Every metric that matters.
          <br className="hidden sm:block" />
          <span style={{ color: TEAL }}>One catalogue.</span>
        </h1>
        <p className="lead text-cc-prose !font-body !text-lead mx-auto mt-6 max-w-2xl !font-normal">
          Operation insights, impact ranking, per-client usage, distributed
          traces, and cross-service monitoring for REST, gRPC, and background
          jobs. OpenTelemetry in, evidence out.
        </p>
        <div className="mt-9 flex flex-wrap justify-center gap-3">
          <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
            Get Started
          </SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
        <div className="mt-10 flex flex-wrap justify-center gap-2">
          <HeroPill label="p95 / p99" accent={TEAL} />
          <HeroPill label="throughput" accent={TEAL} />
          <HeroPill label="error rate" accent={AMBER} />
          <HeroPill label="impact score" accent={CORAL} />
          <HeroPill label="trace waterfall" accent={VIOLET} />
          <HeroPill label="per-client usage" accent={GREEN} />
          <HeroPill label="REST + gRPC + jobs" accent={TEAL} />
        </div>
      </div>
    </header>
  );
}

/* ============================================================================
   NITRO EMBED, the singular animated artifact.
============================================================================ */

function NitroEmbed() {
  return (
    <section className="mt-12">
      <div className="mx-auto mb-6 max-w-3xl text-center">
        <Eyebrow tag="live">
          <span>monitoring reel</span>
        </Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4">
          The same screens you ship to.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          Not a marketing render. The monitoring views loop through the
          operations dashboard, a single-operation drill-down, and the trace
          waterfall they share.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg mx-auto max-w-5xl overflow-hidden rounded-xl border backdrop-blur">
        <NitroMonitoringReel />
      </div>
    </section>
  );
}

/* ============================================================================
   BENTO CATALOGUE, six metric tiles, asymmetric grid.
============================================================================ */

function MetricCatalogue() {
  return (
    <section className="mt-12">
      <div className="mx-auto mb-8 max-w-3xl text-center">
        <Eyebrow tag="catalogue · 06">
          <span>what you can ask of it</span>
        </Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4">
          Six questions, six tiles.
        </h2>
      </div>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
        <div className="sm:col-span-3">
          <OperationInsightsTile />
        </div>
        <div className="sm:col-span-3">
          <ImpactScoreTile />
        </div>
        <div className="sm:col-span-2">
          <ClientUsageTile />
        </div>
        <div className="sm:col-span-4">
          <DistributedTraceTile />
        </div>
        <div className="sm:col-span-4">
          <ServiceMonitoringTile />
        </div>
        <div className="sm:col-span-2">
          <OpenTelemetryTile />
        </div>
      </div>
    </section>
  );
}

/* ---- 01 Operation insights ---------------------------------------------- */

const LATENCY_BARS = [
  { p: "p50", ms: "9ms", pct: 14, tone: GREEN },
  { p: "p75", ms: "16ms", pct: 24, tone: GREEN },
  { p: "p90", ms: "27ms", pct: 42, tone: TEAL },
  { p: "p95", ms: "42ms", pct: 64, tone: TEAL },
  { p: "p99", ms: "318ms", pct: 94, tone: CORAL },
];

function OperationInsightsTile() {
  return (
    <Card className="flex h-full flex-col" glow={TEAL}>
      <TileHeader index="01" title="Operation insights" accent={TEAL} />
      <p className="text-cc-ink-dim px-5 pt-2 text-[0.78rem] leading-relaxed">
        p95 and p99, throughput, and error rate per operation. The whole shape
        of the latency distribution, not a single average that hides the tail.
      </p>
      <div className="space-y-2 px-5 pt-5 pb-5">
        {LATENCY_BARS.map((row) => (
          <div
            key={row.p}
            className="grid grid-cols-[2.4rem_1fr_3rem] items-center gap-3"
          >
            <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-wide uppercase">
              {row.p}
            </span>
            <span className="bg-cc-card-border/40 h-2 overflow-hidden rounded-full">
              <span
                className="block h-full rounded-full"
                style={{ width: `${row.pct}%`, backgroundColor: row.tone }}
              />
            </span>
            <span
              className="text-right font-mono text-[0.72rem] tabular-nums"
              style={{ color: row.tone }}
            >
              {row.ms}
            </span>
          </div>
        ))}
      </div>
      <div className="mx-5 mb-5 grid grid-cols-3 gap-3">
        <MiniStat label="throughput" value="1.2k/m" tone={GREEN} />
        <MiniStat label="errors" value="0.3%" tone={AMBER} />
        <MiniStat label="window" value="last 1h" tone={TEAL} />
      </div>
      <TileFootnote>
        Illustrative values. Yours come from your own telemetry, not from us.
      </TileFootnote>
    </Card>
  );
}

interface MiniStatProps {
  readonly label: string;
  readonly value: string;
  readonly tone: string;
}

function MiniStat({ label, value, tone }: MiniStatProps) {
  return (
    <div className="border-cc-card-border/60 rounded-lg border bg-black/20 px-3 py-2">
      <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.1em] uppercase">
        {label}
      </p>
      <p
        className="mt-1 font-mono text-sm font-semibold tabular-nums"
        style={{ color: tone }}
      >
        {value}
      </p>
    </div>
  );
}

/* ---- 02 Impact score ---------------------------------------------------- */

interface ImpactRow {
  readonly op: string;
  readonly type: "query" | "mutation";
  readonly impact: number;
  readonly p95: string;
  readonly tone: string;
}

const IMPACT_ROWS: readonly ImpactRow[] = [
  { op: "checkout", type: "mutation", impact: 98, p95: "42ms", tone: CORAL },
  { op: "productPage", type: "query", impact: 71, p95: "11ms", tone: TEAL },
  { op: "cartSummary", type: "query", impact: 54, p95: "9ms", tone: TEAL },
  { op: "applyCoupon", type: "mutation", impact: 38, p95: "16ms", tone: AMBER },
  { op: "search", type: "query", impact: 33, p95: "28ms", tone: TEAL },
];

function ImpactScoreTile() {
  return (
    <Card className="flex h-full flex-col" glow={CORAL}>
      <TileHeader index="02" title="Impact score" accent={CORAL} />
      <p className="text-cc-ink-dim px-5 pt-2 text-[0.78rem] leading-relaxed">
        Rank operations by how much they actually hurt: traffic times slowness
        times error rate. The operation that&rsquo;s slow for everyone wins, not
        the one that&rsquo;s merely loud.
      </p>
      <div className="text-cc-nav-label grid grid-cols-[1fr_3rem_5rem] items-center gap-3 px-5 pt-5 pb-2 font-mono text-[0.55rem] tracking-[0.12em] uppercase">
        <span>operation</span>
        <span className="text-right">p95</span>
        <span className="text-right">impact</span>
      </div>
      <div className="divide-cc-card-border/60 flex-1 divide-y">
        {IMPACT_ROWS.map((row) => (
          <div
            key={row.op}
            className="grid grid-cols-[1fr_3rem_5rem] items-center gap-3 px-5 py-2"
          >
            <span className="flex items-baseline gap-2 truncate">
              <span className="text-cc-nav-label font-mono text-[0.58rem]">
                {row.type}
              </span>
              <span className="text-cc-heading truncate font-mono text-[0.78rem]">
                {row.op}
              </span>
            </span>
            <span className="text-cc-ink-dim text-right font-mono text-[0.7rem] tabular-nums">
              {row.p95}
            </span>
            <span className="flex items-center justify-end gap-2">
              <span className="bg-cc-card-border/40 h-1 w-10 overflow-hidden rounded-full">
                <span
                  className="block h-full rounded-full"
                  style={{
                    width: `${row.impact}%`,
                    backgroundColor: row.tone,
                  }}
                />
              </span>
              <span
                className="w-6 text-right font-mono text-[0.72rem] font-semibold tabular-nums"
                style={{ color: row.tone }}
              >
                {row.impact}
              </span>
            </span>
          </div>
        ))}
      </div>
      <TileFootnote>
        Sort the list. Read it top to bottom. That is your roadmap.
      </TileFootnote>
    </Card>
  );
}

/* ---- 03 Per-client usage ------------------------------------------------ */

interface ClientUsage {
  readonly name: string;
  readonly share: number;
  readonly version: string;
}

const CLIENT_USAGE: readonly ClientUsage[] = [
  { name: "web", share: 62, version: "v4.12" },
  { name: "ios", share: 24, version: "v3.8" },
  { name: "android", share: 14, version: "v3.7" },
];

function ClientUsageTile() {
  return (
    <Card className="flex h-full flex-col p-5" glow={GREEN}>
      <TileHeader index="03" title="Per-client usage" accent={GREEN} />
      <p className="text-cc-ink-dim px-0 pt-2 text-[0.78rem] leading-relaxed">
        Which named client calls which operation, at which version. The
        breakdown you need before you deprecate anything.
      </p>
      <div className="mt-4 space-y-3">
        {CLIENT_USAGE.map((c) => (
          <div key={c.name} className="space-y-1">
            <div className="flex items-baseline justify-between">
              <span className="text-cc-heading font-mono text-[0.74rem]">
                {c.name}
                <span className="text-cc-nav-label ml-2 text-[0.6rem]">
                  {c.version}
                </span>
              </span>
              <span className="text-cc-ink-dim font-mono text-[0.66rem] tabular-nums">
                {c.share}%
              </span>
            </div>
            <span className="bg-cc-card-border/40 block h-1.5 overflow-hidden rounded-full">
              <span
                className="block h-full rounded-full"
                style={{
                  width: `${c.share}%`,
                  backgroundColor: GREEN,
                }}
              />
            </span>
          </div>
        ))}
      </div>
      <p className="text-cc-ink-dim mt-auto pt-5 text-[0.72rem] leading-relaxed">
        Operation hash, client name, client version. Published clients affected,
        not guessed.
      </p>
    </Card>
  );
}

/* ---- 04 Distributed trace ----------------------------------------------- */

interface SpanRow {
  readonly service: string;
  readonly kind: "graphql" | "rest" | "grpc" | "job" | "db";
  readonly offset: number;
  readonly width: number;
  readonly ms: string;
  readonly hot?: boolean;
}

const KIND_LABEL: Record<SpanRow["kind"], string> = {
  graphql: "GraphQL",
  rest: "REST",
  grpc: "gRPC",
  job: "job",
  db: "DB",
};

const KIND_TINT: Record<SpanRow["kind"], string> = {
  graphql: TEAL,
  rest: VIOLET,
  grpc: CORAL,
  job: VIOLET,
  db: VIOLET,
};

const TRACE_SPANS: readonly SpanRow[] = [
  {
    service: "api · checkout",
    kind: "graphql",
    offset: 0,
    width: 1,
    ms: "318ms",
  },
  {
    service: "users-svc · GET /me",
    kind: "rest",
    offset: 0.05,
    width: 0.13,
    ms: "44ms",
  },
  {
    service: "billing · Charge",
    kind: "grpc",
    offset: 0.21,
    width: 0.58,
    ms: "204ms",
    hot: true,
  },
  {
    service: "worker · receipt",
    kind: "job",
    offset: 0.5,
    width: 0.18,
    ms: "58ms",
  },
  {
    service: "orders.db · INSERT",
    kind: "db",
    offset: 0.84,
    width: 0.12,
    ms: "38ms",
  },
];

function DistributedTraceTile() {
  return (
    <Card className="flex h-full flex-col" glow={VIOLET}>
      <TileHeader index="04" title="Distributed trace" accent={VIOLET} />
      <p className="text-cc-ink-dim px-5 pt-2 text-[0.78rem] leading-relaxed">
        Every hop a span, every span on the same trace. Follow the slow request
        from the GraphQL edge into the gRPC service that ate the budget.
      </p>
      <div className="space-y-2 px-5 py-5">
        {TRACE_SPANS.map((span) => (
          <SpanBar key={span.service} span={span} />
        ))}
      </div>
      <TileFootnote>
        billing · Charge held the request{" "}
        <span style={{ color: CORAL }}>204ms</span>, 64% of the trace.
      </TileFootnote>
    </Card>
  );
}

interface SpanBarProps {
  readonly span: SpanRow;
}

function SpanBar({ span }: SpanBarProps) {
  const tint = span.hot ? CORAL : KIND_TINT[span.kind];
  return (
    <div className="grid grid-cols-[9rem_1fr] items-center gap-3">
      <div className="flex items-center gap-2 overflow-hidden">
        <span
          className="shrink-0 rounded px-1 py-0.5 font-mono text-[0.5rem] tracking-[0.04em] uppercase"
          style={{ color: tint, backgroundColor: `${tint}1f` }}
        >
          {KIND_LABEL[span.kind]}
        </span>
        <span className="text-cc-ink-dim truncate font-mono text-[0.62rem]">
          {span.service}
        </span>
      </div>
      <div className="relative h-5">
        <div className="bg-cc-card-border/40 absolute inset-y-0 left-0 w-full rounded" />
        <div
          className="absolute inset-y-0 flex items-center justify-end rounded pr-1.5"
          style={{
            left: `${span.offset * 100}%`,
            width: `${span.width * 100}%`,
            backgroundColor: span.hot ? `${tint}33` : `${tint}22`,
            boxShadow: span.hot
              ? `inset 0 0 0 1px ${tint}aa, 0 0 16px ${tint}55`
              : `inset 0 0 0 1px ${tint}44`,
          }}
        >
          <span
            className="font-mono text-[0.55rem] tabular-nums"
            style={{ color: span.hot ? tint : "#c8d2e6" }}
          >
            {span.ms}
          </span>
        </div>
      </div>
    </div>
  );
}

/* ---- 05 Service monitoring (REST + gRPC + jobs) ------------------------- */

interface ServiceRow {
  readonly name: string;
  readonly kind: "REST" | "gRPC" | "job";
  readonly p95: string;
  readonly throughput: string;
  readonly errors: string;
  readonly status: "ok" | "warn" | "hot";
}

const SERVICE_ROWS: readonly ServiceRow[] = [
  {
    name: "users-svc",
    kind: "REST",
    p95: "44ms",
    throughput: "820/m",
    errors: "0.1%",
    status: "ok",
  },
  {
    name: "billing",
    kind: "gRPC",
    p95: "204ms",
    throughput: "612/m",
    errors: "0.4%",
    status: "hot",
  },
  {
    name: "catalog-svc",
    kind: "REST",
    p95: "31ms",
    throughput: "1.4k/m",
    errors: "0.0%",
    status: "ok",
  },
  {
    name: "receipt.worker",
    kind: "job",
    p95: "58ms",
    throughput: "240/m",
    errors: "0.2%",
    status: "warn",
  },
];

const SERVICE_STATUS_COLOR: Record<ServiceRow["status"], string> = {
  ok: GREEN,
  warn: AMBER,
  hot: CORAL,
};

function ServiceMonitoringTile() {
  return (
    <Card className="flex h-full flex-col" glow={TEAL}>
      <TileHeader index="05" title="Service monitoring" accent={TEAL} />
      <p className="text-cc-ink-dim px-5 pt-2 text-[0.78rem] leading-relaxed">
        Same metric catalogue applied to every .NET service behind the API: REST
        endpoints, gRPC methods, and background jobs all reported next to the
        operation they belong to.
      </p>
      <div className="text-cc-nav-label grid grid-cols-[1rem_1fr_2.6rem_3.4rem_3.4rem_3.4rem] items-center gap-3 px-5 pt-5 pb-2 font-mono text-[0.55rem] tracking-[0.12em] uppercase">
        <span />
        <span>service</span>
        <span>kind</span>
        <span className="text-right">p95</span>
        <span className="text-right">rate</span>
        <span className="text-right">errors</span>
      </div>
      <div className="divide-cc-card-border/60 flex-1 divide-y">
        {SERVICE_ROWS.map((row) => {
          const tone = SERVICE_STATUS_COLOR[row.status];
          return (
            <div
              key={row.name}
              className="grid grid-cols-[1rem_1fr_2.6rem_3.4rem_3.4rem_3.4rem] items-center gap-3 px-5 py-2.5"
            >
              <span
                className="h-2 w-2 rounded-full"
                style={{
                  backgroundColor: tone,
                  boxShadow:
                    row.status === "ok" ? undefined : `0 0 8px ${tone}aa`,
                }}
                aria-hidden
              />
              <span className="text-cc-heading truncate font-mono text-[0.78rem]">
                {row.name}
              </span>
              <span
                className="rounded px-1 py-0.5 text-center font-mono text-[0.5rem] tracking-[0.04em] uppercase"
                style={{
                  color:
                    row.kind === "gRPC"
                      ? CORAL
                      : row.kind === "job"
                        ? VIOLET
                        : VIOLET,
                  backgroundColor: `${row.kind === "gRPC" ? CORAL : VIOLET}1f`,
                }}
              >
                {row.kind}
              </span>
              <span
                className="text-right font-mono text-[0.72rem] tabular-nums"
                style={{ color: row.status === "hot" ? CORAL : undefined }}
              >
                {row.p95}
              </span>
              <span className="text-cc-ink-dim text-right font-mono text-[0.72rem] tabular-nums">
                {row.throughput}
              </span>
              <span
                className="text-right font-mono text-[0.72rem] tabular-nums"
                style={{ color: row.status === "warn" ? AMBER : undefined }}
              >
                {row.errors}
              </span>
            </div>
          );
        })}
      </div>
      <TileFootnote>
        One pane for the GraphQL edge and the .NET services behind it.
      </TileFootnote>
    </Card>
  );
}

/* ---- 06 OpenTelemetry (vendor-neutral) ---------------------------------- */

function OpenTelemetryTile() {
  return (
    <Card className="flex h-full flex-col p-5" glow={TEAL}>
      <TileHeader index="06" title="OpenTelemetry" accent={TEAL} />
      <p className="text-cc-ink-dim pt-2 text-[0.78rem] leading-relaxed">
        Plain OTLP, no proprietary agent. The traces, metrics, and logs Nitro
        reads are the ones your services already emit.
      </p>
      <div className="border-cc-card-border/60 mt-4 rounded-lg border bg-black/30 p-3 font-mono text-[0.66rem] leading-snug">
        <p className="text-cc-nav-label text-[0.56rem] tracking-[0.12em] uppercase">
          appsettings.json
        </p>
        <pre className="text-cc-ink mt-2 overflow-x-auto whitespace-pre">
          <code>{`{
  "Otlp": {
    "Endpoint": "https://nitro/otlp",
    "Protocol": "grpc"
  }
}`}</code>
        </pre>
      </div>
      <ul className="mt-4 space-y-2 text-[0.74rem]">
        <li className="text-cc-ink flex items-start gap-2">
          <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
            <CheckIcon size={13} />
          </span>
          <span>Vendor-neutral OTLP in</span>
        </li>
        <li className="text-cc-ink flex items-start gap-2">
          <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
            <CheckIcon size={13} />
          </span>
          <span>Hot Chocolate auto-instrumented</span>
        </li>
        <li className="text-cc-ink flex items-start gap-2">
          <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
            <CheckIcon size={13} />
          </span>
          <span>Fan out to any OTel backend</span>
        </li>
      </ul>
    </Card>
  );
}

/* ============================================================================
   HOW IT WORKS, hairline trace pipeline diagram.
   This is the ONE place the brand spectrum (cyan, violet, coral) appears.
============================================================================ */

interface PipelineNode {
  readonly id: string;
  readonly label: string;
  readonly sub: string;
  readonly color: string;
}

const PIPELINE: readonly PipelineNode[] = [
  {
    id: "svc",
    label: ".NET services",
    sub: "Hot Chocolate · REST · gRPC · jobs",
    color: CYAN,
  },
  {
    id: "otlp",
    label: "OpenTelemetry",
    sub: "OTLP export · vendor-neutral",
    color: VIOLET,
  },
  {
    id: "nitro",
    label: "Nitro",
    sub: "ingest · roll-up · index",
    color: TEAL,
  },
  {
    id: "view",
    label: "Dashboards",
    sub: "operations · clients · traces",
    color: CORAL,
  },
];

function HowItWorks() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg mt-12 rounded-3xl border p-6 backdrop-blur sm:p-10">
      <div className="max-w-2xl">
        <Eyebrow tag="pipeline">
          <span>how it works</span>
        </Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4">
          OpenTelemetry export, straight through.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          Your services emit OTLP. Nitro ingests, rolls up, and indexes. The
          dashboards read what your traces already say. No proprietary agent, no
          second source of truth.
        </p>
      </div>

      <div className="relative mt-10">
        <svg
          viewBox="0 0 1000 160"
          className="h-auto w-full"
          role="img"
          aria-label="Telemetry pipeline: .NET services export OpenTelemetry to Nitro, which feeds the analytics dashboards."
        >
          <defs>
            <linearGradient id="trace-flow" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0%" stopColor={CYAN} stopOpacity="0.85" />
              <stop offset="50%" stopColor={VIOLET} stopOpacity="0.85" />
              <stop offset="100%" stopColor={CORAL} stopOpacity="0.85" />
            </linearGradient>
            <linearGradient id="trace-flow-faint" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0%" stopColor={CYAN} stopOpacity="0.2" />
              <stop offset="50%" stopColor={VIOLET} stopOpacity="0.2" />
              <stop offset="100%" stopColor={CORAL} stopOpacity="0.2" />
            </linearGradient>
          </defs>

          {/* glow rail */}
          <path
            d="M 70 80 L 930 80"
            stroke="url(#trace-flow-faint)"
            strokeWidth="14"
            strokeLinecap="round"
            fill="none"
          />
          {/* hairline rail */}
          <path
            d="M 70 80 L 930 80"
            stroke="url(#trace-flow)"
            strokeWidth="1.5"
            strokeLinecap="round"
            fill="none"
          />

          {PIPELINE.map((node, i) => {
            const x = 70 + (860 / (PIPELINE.length - 1)) * i;
            return (
              <g key={node.id}>
                <circle
                  cx={x}
                  cy={80}
                  r="22"
                  fill="rgba(11,15,26,0.95)"
                  stroke={node.color}
                  strokeWidth="1.5"
                  style={{ filter: `drop-shadow(0 0 12px ${node.color}55)` }}
                />
                <circle cx={x} cy={80} r="4" fill={node.color} />
                <text
                  x={x}
                  y={30}
                  textAnchor="middle"
                  fontFamily="ui-monospace, monospace"
                  fontSize="11"
                  fill={node.color}
                >
                  {node.label}
                </text>
                <text
                  x={x}
                  y={132}
                  textAnchor="middle"
                  fontFamily="ui-monospace, monospace"
                  fontSize="9"
                  fill="rgba(245,241,234,0.55)"
                >
                  {node.sub}
                </text>
              </g>
            );
          })}
        </svg>
      </div>
    </section>
  );
}

/* ============================================================================
   HONESTY BAND
============================================================================ */

function HonestyBand() {
  const points: readonly string[] = [
    "Telemetry needs Nitro configuration: dashboards do not light up until your services export OTLP to a Nitro project.",
    "Plain OpenTelemetry, so the same export can fan out to any other OTel backend you already run.",
    "Numbers shown here are illustrative dashboard values. Your numbers come from your own telemetry.",
    "Hot Chocolate is source-generated and ships with the OpenTelemetry instrumentation that powers these views.",
  ];
  return (
    <section className="border-cc-card-border bg-cc-card-bg mt-12 grid gap-8 rounded-3xl border p-6 backdrop-blur sm:grid-cols-[0.8fr_1.2fr] sm:p-10">
      <div>
        <Eyebrow tag="scope" color={GREEN}>
          <span>what is true</span>
        </Eyebrow>
        <h2 className="font-heading text-h5 text-cc-heading sm:text-h4 mt-4">
          Honest about what you have to wire up.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          The catalogue is real. Reading from it is a configuration step we will
          not pretend away.
        </p>
      </div>
      <ul className="space-y-4">
        {points.map((point) => (
          <li
            key={point}
            className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
          >
            <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
              <CheckIcon size={15} />
            </span>
            <span>{point}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

/* ============================================================================
   CLOSING CTA
============================================================================ */

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-surface/40 relative mt-12 overflow-hidden rounded-3xl border px-6 py-14 text-center backdrop-blur sm:px-12 sm:py-20">
      <div
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(55% 90% at 50% -10%, rgba(94,234,212,0.16), transparent 60%)",
        }}
        aria-hidden
      />
      <Eyebrow tag="ship it">
        <span>start the catalogue</span>
      </Eyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h2 mx-auto mt-5 max-w-2xl">
        Every metric that matters, in one place.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-5 max-w-xl leading-relaxed">
        Wire up OpenTelemetry, point the OTLP exporter at Nitro, and read the
        same six tiles for every operation, client, and service.
      </p>
      <div className="mt-9 flex flex-wrap justify-center gap-3">
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

/* ============================================================================
   PAGE
============================================================================ */

export default function AnalyticsPreviewV3Page() {
  return (
    <main>
      <Hero />
      <NitroEmbed />
      <MetricCatalogue />
      <HowItWorks />
      <HonestyBand />
      <ClosingCta />
    </main>
  );
}
