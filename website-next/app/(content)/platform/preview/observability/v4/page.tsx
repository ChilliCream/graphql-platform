import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Observability for .NET: The Reference",
  description:
    "GraphQL observability for .NET as a reference: OpenTelemetry signals, distributed traces, operation, service, and client lenses, one checkout incident.",
  keywords: [
    "GraphQL observability",
    "OpenTelemetry .NET",
    "distributed tracing",
    "Nitro telemetry",
    "p95 p99 latency",
    "operation monitoring",
    "Hot Chocolate observability",
    "trace waterfall",
  ],
  openGraph: {
    title: "GraphQL Observability for .NET: The Reference",
    description:
      "Reference-manual view of Nitro observability: four OTel signals, one trace id, three lenses, one topology, one checkout incident.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ *
 * Variation: The Reference Manual.
 * Family: sidebar-toc. A sticky left TOC indexes the article column.
 * Accent: cyan (#16b9e4), used only for active state, the cyan ticks
 * along the page margin rail, and the single spectrum hairline at the
 * closing CTA. Everything else is cc-* tokens.
 * ------------------------------------------------------------------ */

const CYAN = "#16b9e4";
const CORAL = "#f0786a";

// The single allowed spectrum event, reserved for the closing CTA top rule.
// Brand spectrum: cyan -> violet -> coral.
const SPECTRUM = `linear-gradient(100deg, ${CYAN} 0%, #7c92c6 52%, ${CORAL} 100%)`;

const TRACE_ID = "4b1c8f2a9e07";
const OPERATION = "checkout";
const INCIDENT_STATUS = "Investigating";

interface TocEntry {
  readonly num: string;
  readonly id: string;
  readonly title: string;
}

const TOC: readonly TocEntry[] = [
  { num: "01", id: "signals", title: "Signals" },
  { num: "02", id: "trace", title: "Trace" },
  { num: "03", id: "lenses", title: "Lenses" },
  { num: "04", id: "topology", title: "Topology" },
  { num: "05", id: "standard", title: "Standard" },
  { num: "06", id: "honesty", title: "Honesty" },
];

export default function ObservabilityReferencePage() {
  return (
    <main className="pb-20">
      <div className="mx-auto lg:max-w-[1180px]">
        <DocumentHeader />
        <MobileTocStrip />
        <div className="mt-16 grid gap-12 lg:grid-cols-[260px_1fr] lg:items-start lg:gap-16">
          <SidebarToc />
          <article className="border-cc-card-border/60 relative border-l pl-8 sm:pl-10">
            <Section01Signals />
            <SectionDivider />
            <Section02Trace />
            <SectionDivider />
            <Section03Lenses />
            <SectionDivider />
            <Section04Topology />
            <SectionDivider />
            <Section05Standard />
            <SectionDivider />
            <Section06Honesty />
            <ClosingCta />
          </article>
        </div>
      </div>
    </main>
  );
}

/* ================================================================== *
 * DOCUMENT HEADER
 * Eyebrow / h1 / lead / dual CTA / mono meta strip.
 * ================================================================== */

function DocumentHeader() {
  return (
    <header className="pt-10">
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Reference / Observability
      </span>
      <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-6 max-w-3xl">
        GraphQL observability for .NET
      </h1>
      <p className="lead text-cc-prose mt-6 max-w-2xl">
        A reference for what production looks like when telemetry is wired
        through the .NET graph.
      </p>
      <p className="text-body text-cc-ink-dim mt-5 max-w-2xl">
        Nitro is OpenTelemetry-native: operation, service, and client views with
        p95 / p99, throughput, error rate, and an impact score. Every request is
        a distributed trace that spans GraphQL, REST, gRPC, and background jobs,
        so debugging starts from evidence.
      </p>
      <div className="mt-9 flex flex-wrap items-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
        </OutlineButton>
      </div>
      <MetaStrip />
    </header>
  );
}

function MetaStrip() {
  return (
    <dl className="border-cc-card-border/60 mt-10 grid gap-x-10 gap-y-2 border-t border-b py-4 font-mono text-[11px] sm:grid-cols-3">
      <MetaRow label="trace.id" value={TRACE_ID} />
      <MetaRow label="operation" value={OPERATION} />
      <MetaRow label="status" value={INCIDENT_STATUS} accent />
    </dl>
  );
}

interface MetaRowProps {
  readonly label: string;
  readonly value: string;
  readonly accent?: boolean;
}

function MetaRow({ label, value, accent }: MetaRowProps) {
  return (
    <div className="flex items-baseline gap-3">
      <dt className="text-cc-nav-label tracking-wide uppercase">{label}</dt>
      <dd
        className={accent ? "" : "text-cc-ink"}
        style={accent ? { color: CYAN } : undefined}
      >
        {value}
      </dd>
    </div>
  );
}

/* ================================================================== *
 * SIDEBAR TOC + MOBILE STRIP
 * The sticky sidebar at lg, the horizontal chip strip on mobile.
 * Below the index, a fixed page-meta block lists trace id, operation,
 * and incident status as mono key/value rows.
 * ================================================================== */

function SidebarToc() {
  return (
    <aside className="hidden lg:block">
      <nav
        aria-label="Section index"
        className="sticky top-24 flex flex-col gap-10"
      >
        <ol className="flex flex-col">
          {TOC.map((entry) => (
            <TocItem key={entry.id} entry={entry} />
          ))}
        </ol>
        <PageMetaBlock />
      </nav>
    </aside>
  );
}

interface TocItemProps {
  readonly entry: TocEntry;
}

function TocItem({ entry }: TocItemProps) {
  return (
    <li className="relative">
      <a
        href={`#${entry.id}`}
        className="flex items-baseline gap-3 py-2 pl-3 font-mono text-[11px] tracking-wide"
      >
        <span className="text-cc-nav-label">{entry.num}</span>
        <span className="text-cc-ink-faint">/</span>
        <span className="text-cc-ink-dim">{entry.title}</span>
      </a>
    </li>
  );
}

function PageMetaBlock() {
  return (
    <div className="border-cc-card-border/60 border-t pt-6">
      <div className="text-cc-nav-label font-mono text-[10px] tracking-[0.28em] uppercase">
        Page meta
      </div>
      <dl className="mt-3 space-y-2 font-mono text-[11px]">
        <MetaKv label="trace.id" value={TRACE_ID} />
        <MetaKv label="operation" value={OPERATION} />
        <MetaKv label="status" value={INCIDENT_STATUS} accent />
        <MetaKv label="standard" value="opentelemetry" />
      </dl>
    </div>
  );
}

interface MetaKvProps {
  readonly label: string;
  readonly value: string;
  readonly accent?: boolean;
}

function MetaKv({ label, value, accent }: MetaKvProps) {
  return (
    <div className="flex items-baseline justify-between gap-3">
      <dt className="text-cc-nav-label">{label}</dt>
      <dd
        className={accent ? "" : "text-cc-ink"}
        style={accent ? { color: CYAN } : undefined}
      >
        {value}
      </dd>
    </div>
  );
}

function MobileTocStrip() {
  return (
    <nav
      aria-label="Section index"
      className="border-cc-card-border/60 -mx-4 mt-10 overflow-x-auto border-y px-4 lg:hidden"
    >
      <ol className="flex min-w-max items-center gap-5 py-3 font-mono text-[11px]">
        {TOC.map((entry) => (
          <li key={entry.id}>
            <a
              href={`#${entry.id}`}
              className="inline-flex items-baseline gap-2 tracking-wide"
            >
              <span className="text-cc-nav-label">{entry.num}</span>
              <span className="text-cc-ink-dim">{entry.title}</span>
            </a>
          </li>
        ))}
      </ol>
    </nav>
  );
}

/* ================================================================== *
 * SECTION MARKER + DIVIDER
 * The "§ NN" marker is the cyan tick that breaks the rail. The
 * divider is a full-width hairline between sections.
 * ================================================================== */

interface SectionMarkerProps {
  readonly num: string;
  readonly id: string;
}

function SectionMarker({ num, id }: SectionMarkerProps) {
  return (
    <div className="relative" id={id}>
      {/* cyan tick that breaks the article-left rail */}
      <span
        aria-hidden
        className="absolute top-3 -left-10 hidden h-px w-6 sm:block"
        style={{ backgroundColor: CYAN }}
      />
      <span
        className="font-mono text-xs tracking-[0.28em] uppercase"
        style={{ color: CYAN }}
      >
        § {num}
      </span>
    </div>
  );
}

function SectionDivider() {
  return <hr className="border-cc-card-border/60 my-16" />;
}

/* ================================================================== *
 * 01 SIGNALS
 * The four OTel signals as a definition list: mono label, big number
 * in text-h3 status-coded, one-line note.
 * ================================================================== */

interface SignalRow {
  readonly label: string;
  readonly value: string;
  readonly unit: string;
  readonly status: "ok" | "warn" | "fire" | "steady";
  readonly note: string;
}

const SIGNALS: readonly SignalRow[] = [
  {
    label: "p95 latency",
    value: "42",
    unit: "ms",
    status: "ok",
    note: "within budget for the checkout operation.",
  },
  {
    label: "p99 latency",
    value: "318",
    unit: "ms",
    status: "fire",
    note: "spiking, traced to billing.Charge over gRPC.",
  },
  {
    label: "error rate",
    value: "0.3",
    unit: "%",
    status: "warn",
    note: "5xx surfacing on the billing service.",
  },
  {
    label: "throughput",
    value: "9.4",
    unit: "k rpm",
    status: "steady",
    note: "load is unchanged, latency is the variable.",
  },
];

const STATUS_TOKEN: Record<SignalRow["status"], string> = {
  ok: "#34d399",
  warn: "#fbbf24",
  fire: CORAL,
  steady: "var(--color-cc-heading)",
};

const STATUS_TAG: Record<SignalRow["status"], string> = {
  ok: "healthy",
  warn: "warning",
  fire: "firing",
  steady: "steady",
};

function Section01Signals() {
  return (
    <section>
      <SectionMarker num="01" id="signals" />
      <h2 className="font-heading text-h3 text-cc-heading mt-4">
        The four OTel signals
      </h2>
      <p className="lead text-cc-prose mt-4 max-w-2xl">
        Operation health collapses to four numbers. Each is sampled from the
        same OpenTelemetry stream the rest of this reference draws from.
      </p>
      <p className="text-body text-cc-ink-dim mt-4 max-w-2xl">
        Status colour is rationed as data: it tells you which of the four is the
        variable in this incident, not which one is the loudest. The impact
        score in the lenses section ranks operations by what hurts the system,
        not by raw call count.
      </p>

      <dl className="mt-10 flex flex-col">
        {SIGNALS.map((s, i) => (
          <SignalDefRow
            key={s.label}
            signal={s}
            isLast={i === SIGNALS.length - 1}
          />
        ))}
      </dl>
    </section>
  );
}

interface SignalDefRowProps {
  readonly signal: SignalRow;
  readonly isLast: boolean;
}

function SignalDefRow({ signal, isLast }: SignalDefRowProps) {
  return (
    <div
      className={`grid grid-cols-[140px_1fr] gap-x-8 py-5 sm:grid-cols-[180px_1fr_auto] ${
        isLast ? "" : "border-cc-card-border/60 border-b"
      }`}
    >
      <dt className="text-cc-nav-label font-mono text-[11px] tracking-wide uppercase">
        {signal.label}
      </dt>
      <dd className="flex items-baseline gap-1.5">
        <span
          className="font-heading text-h3 leading-none"
          style={{ color: STATUS_TOKEN[signal.status] }}
        >
          {signal.value}
        </span>
        <span className="text-cc-ink-dim font-mono text-sm">{signal.unit}</span>
      </dd>
      <dd className="text-cc-ink-dim text-caption col-span-2 mt-3 sm:col-span-1 sm:mt-0 sm:max-w-sm sm:self-center sm:text-right">
        <span
          className="mr-2 font-mono text-[10px] tracking-wide uppercase"
          style={{ color: STATUS_TOKEN[signal.status] }}
        >
          {STATUS_TAG[signal.status]}
        </span>
        {signal.note}
      </dd>
    </div>
  );
}

/* ================================================================== *
 * 02 TRACE
 * Distributed trace anatomy as a compact left-aligned waterfall.
 * Hairline-bordered table, mono kind tags, ms columns, slow gRPC row
 * highlighted with a cyan left-border tick.
 * ================================================================== */

interface Span {
  readonly label: string;
  readonly kind: "graphql" | "rest" | "grpc" | "db" | "job";
  readonly start: number;
  readonly width: number;
  readonly ms: string;
  readonly slow?: boolean;
}

const SPANS: readonly Span[] = [
  {
    label: "mutation checkout",
    kind: "graphql",
    start: 0,
    width: 100,
    ms: "318ms",
  },
  {
    label: "api → users-svc · GET /me",
    kind: "rest",
    start: 4,
    width: 11,
    ms: "21ms",
  },
  {
    label: "users-svc → billing · Charge()",
    kind: "grpc",
    start: 16,
    width: 64,
    ms: "201ms",
    slow: true,
  },
  {
    label: "billing → db · SELECT account",
    kind: "db",
    start: 20,
    width: 12,
    ms: "9ms",
  },
  {
    label: "billing → worker · enqueue receipt",
    kind: "job",
    start: 82,
    width: 13,
    ms: "37ms",
  },
];

const KIND_LABEL: Record<Span["kind"], string> = {
  graphql: "GRAPHQL",
  rest: "REST",
  grpc: "GRPC",
  db: "DB",
  job: "JOB",
};

function Section02Trace() {
  return (
    <section>
      <SectionMarker num="02" id="trace" />
      <h2 className="font-heading text-h3 text-cc-heading mt-4">
        Anatomy of one trace
      </h2>
      <p className="lead text-cc-prose mt-4 max-w-2xl">
        A single request fans out across the graph and the services behind it.
        Every hop is an OpenTelemetry span on the same trace id.
      </p>
      <p className="text-body text-cc-ink-dim mt-4 max-w-2xl">
        The p99 spike on <code className="text-cc-ink font-mono">checkout</code>{" "}
        is one click from its trace. The waterfall reads top to bottom from the
        GraphQL root, through the REST hop, into the slow gRPC call to{" "}
        <code className="text-cc-ink font-mono">billing.Charge()</code>, then
        the database read and the job enqueue.
      </p>

      <TraceTable />
    </section>
  );
}

function TraceTable() {
  return (
    <div className="border-cc-card-border/60 mt-10 overflow-hidden rounded border">
      <div className="border-cc-card-border/60 flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3 font-mono text-[11px]">
        <span className="text-cc-nav-label tracking-wide uppercase">trace</span>
        <span style={{ color: CYAN }}>{TRACE_ID}</span>
        <span className="text-cc-ink-faint">·</span>
        <span className="text-cc-ink-dim">mutation checkout</span>
        <span className="text-cc-nav-label ml-auto">
          duration <span className="text-cc-heading">318ms</span>
        </span>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full font-mono text-[11px]">
          <thead>
            <tr className="border-cc-card-border/60 text-cc-nav-label border-b">
              <th className="px-4 py-2 text-left font-normal tracking-wide uppercase">
                kind
              </th>
              <th className="px-2 py-2 text-left font-normal tracking-wide uppercase">
                span
              </th>
              <th className="px-2 py-2 text-left font-normal tracking-wide uppercase">
                waterfall
              </th>
              <th className="px-4 py-2 text-right font-normal tracking-wide uppercase">
                ms
              </th>
            </tr>
          </thead>
          <tbody>
            {SPANS.map((span) => (
              <SpanRow key={span.label} span={span} />
            ))}
          </tbody>
        </table>
      </div>
      <div className="border-cc-card-border/60 text-cc-nav-label flex items-center justify-between border-t px-5 py-2 font-mono text-[10px]">
        <span>0ms</span>
        <span>100ms</span>
        <span>200ms</span>
        <span>318ms</span>
      </div>
    </div>
  );
}

interface SpanRowProps {
  readonly span: Span;
}

function SpanRow({ span }: SpanRowProps) {
  return (
    <tr
      className="border-cc-card-border/40 border-b last:border-b-0"
      style={span.slow ? { boxShadow: `inset 2px 0 0 0 ${CYAN}` } : undefined}
    >
      <td className="px-4 py-2.5 align-middle">
        <span className="text-cc-nav-label tracking-wide">
          {KIND_LABEL[span.kind]}
        </span>
      </td>
      <td className="text-cc-ink-dim px-2 py-2.5 align-middle whitespace-nowrap">
        <span className={span.slow ? "text-cc-heading" : undefined}>
          {span.label}
        </span>
      </td>
      <td className="px-2 py-2.5 align-middle">
        <div className="bg-cc-surface/40 relative h-2 w-full min-w-[160px] rounded">
          <div
            className="absolute top-0 h-2 rounded"
            style={{
              left: `${span.start}%`,
              width: `${span.width}%`,
              backgroundColor: span.slow
                ? CYAN
                : "var(--color-cc-card-border-hover)",
            }}
          />
        </div>
      </td>
      <td className="text-cc-ink px-4 py-2.5 text-right align-middle">
        {span.ms}
      </td>
    </tr>
  );
}

/* ================================================================== *
 * 03 LENSES
 * Three stacked article subsections (operation / service / client).
 * Each: h3, mono table, one-paragraph rationale, hairline top rule.
 * ================================================================== */

function Section03Lenses() {
  return (
    <section>
      <SectionMarker num="03" id="lenses" />
      <h2 className="font-heading text-h3 text-cc-heading mt-4">
        Three lenses on one stream
      </h2>
      <p className="lead text-cc-prose mt-4 max-w-2xl">
        Telemetry is the same stream sliced three ways. Each lens reframes the
        same checkout incident with different aggregation.
      </p>
      <p className="text-body text-cc-ink-dim mt-4 max-w-2xl">
        Rank operations by impact to find what hurts most, drop into the service
        that is degraded, or check which published clients are affected before
        you ship the fix.
      </p>

      <div className="mt-10 flex flex-col gap-12">
        <LensOperation />
        <LensService />
        <LensClient />
      </div>
    </section>
  );
}

interface LensRow {
  readonly rank: string;
  readonly name: string;
  readonly metric: string;
  readonly status: "ok" | "warn" | "fire";
}

function LensOperation() {
  const rows: readonly LensRow[] = [
    { rank: "#1", name: "checkout", metric: "42ms", status: "fire" },
    { rank: "#2", name: "cartSummary", metric: "31ms", status: "warn" },
    { rank: "#3", name: "productList", metric: "12ms", status: "ok" },
    { rank: "#4", name: "userProfile", metric: "8ms", status: "ok" },
  ];
  return (
    <LensSubsection
      title="Operation lens, ranked by impact"
      head="operation"
      metricHead="p95"
      rows={rows}
      rationale="Impact score weights the latency budget burned by each operation. The list answers what hurts the system most right now without being misled by call count."
    />
  );
}

function LensService() {
  // Per-service "observed span ms" values map 1:1 to the spans in section 02
  // (rest 21ms, gRPC 201ms, db 9ms, job 37ms).
  const rows: readonly LensRow[] = [
    { rank: "01", name: "billing (Charge)", metric: "201ms", status: "fire" },
    { rank: "02", name: "worker (enqueue)", metric: "37ms", status: "warn" },
    { rank: "03", name: "users-svc (GET /me)", metric: "21ms", status: "ok" },
    { rank: "04", name: "accounts-db (SELECT)", metric: "9ms", status: "ok" },
  ];
  return (
    <LensSubsection
      title="Service lens, ranked by burnt budget"
      head="service"
      metricHead="observed span ms"
      rows={rows}
      rationale="Aggregate the same trace by service to see which downstream is the bottleneck. The values are the observed span durations from section 02, so billing.Charge over gRPC is the row to open first."
    />
  );
}

function LensClient() {
  const rows: readonly LensRow[] = [
    {
      rank: "01",
      name: "web-storefront@4.2.0",
      metric: "61%",
      status: "fire",
    },
    {
      rank: "02",
      name: "ios-app@3.8.1",
      metric: "27%",
      status: "warn",
    },
    {
      rank: "03",
      name: "partner-api@1.0",
      metric: "12%",
      status: "ok",
    },
  ];
  return (
    <LensSubsection
      title="Client lens, published clients affected"
      head="client"
      metricHead="share"
      rows={rows}
      rationale="See which published clients are affected before you ship the fix. The same trace stream attributes every span to the client that produced it, so the rollout decision is evidence, not guesswork."
    />
  );
}

interface LensSubsectionProps {
  readonly title: string;
  readonly head: string;
  readonly metricHead: string;
  readonly rows: readonly LensRow[];
  readonly rationale: string;
}

function LensSubsection({
  title,
  head,
  metricHead,
  rows,
  rationale,
}: LensSubsectionProps) {
  return (
    <div className="border-cc-card-border/60 border-t pt-6">
      <h3 className="font-heading text-h5 text-cc-heading">{title}</h3>
      <p className="text-body text-cc-ink-dim mt-3 max-w-2xl">{rationale}</p>
      <div className="mt-5 overflow-x-auto">
        <table className="w-full font-mono text-[11px]">
          <thead>
            <tr className="border-cc-card-border/60 text-cc-nav-label border-b">
              <Th>rank</Th>
              <Th>{head}</Th>
              <Th align="right">{metricHead}</Th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr
                key={r.name}
                className="border-cc-card-border/30 border-b last:border-b-0"
                style={
                  r.status === "fire"
                    ? { boxShadow: `inset 2px 0 0 0 ${CYAN}` }
                    : undefined
                }
              >
                <Td>{r.rank}</Td>
                <Td>
                  <span
                    className={`whitespace-nowrap ${
                      r.status === "fire"
                        ? "text-cc-heading"
                        : "text-cc-ink-dim"
                    }`}
                  >
                    {r.name}
                  </span>
                </Td>
                <Td align="right">{r.metric}</Td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

interface ThProps {
  readonly children: React.ReactNode;
  readonly align?: "left" | "right";
}

function Th({ children, align = "left" }: ThProps) {
  return (
    <th
      className={`px-3 py-2 font-normal tracking-wide uppercase ${
        align === "right" ? "text-right" : "text-left"
      }`}
    >
      {children}
    </th>
  );
}

interface TdProps {
  readonly children: React.ReactNode;
  readonly align?: "left" | "right";
}

function Td({ children, align = "left" }: TdProps) {
  return (
    <td
      className={`text-cc-ink-dim px-3 py-2.5 ${
        align === "right" ? "text-right" : "text-left"
      }`}
    >
      {children}
    </td>
  );
}

/* ================================================================== *
 * 04 TOPOLOGY
 * Compact SVG service graph rendered as labeled rectangles connected
 * by hairlines, with the hot gRPC edge in cyan, plus three OTel facts.
 * ================================================================== */

function Section04Topology() {
  return (
    <section>
      <SectionMarker num="04" id="topology" />
      <h2 className="font-heading text-h3 text-cc-heading mt-4">
        The topology behind one trace
      </h2>
      <p className="lead text-cc-prose mt-4 max-w-2xl">
        A distributed trace does not stop at the GraphQL boundary. The graph is
        the entry, the spans run all the way down.
      </p>
      <p className="text-body text-cc-ink-dim mt-4 max-w-2xl">
        Nitro monitors REST APIs, gRPC services, and background jobs through{" "}
        <code className="text-cc-ink font-mono">
          ChilliCream.Nitro.OpenTelemetry
        </code>
        , so the same trace that opens on{" "}
        <code className="text-cc-ink font-mono">checkout</code> follows the call
        down to the hop that is actually slow.
      </p>

      <div className="mt-10 grid gap-10 lg:grid-cols-[1.2fr_1fr] lg:items-center">
        <TopologyGraph />
        <ul className="text-cc-ink-dim text-caption space-y-4">
          <CheckLine>
            Operation, service, and client views over one OpenTelemetry stream.
          </CheckLine>
          <CheckLine>
            Vendor-neutral OTel: no proprietary agent to wire up.
          </CheckLine>
          <CheckLine>
            The hot hop highlights, so the eye lands on cause, not noise.
          </CheckLine>
        </ul>
      </div>
    </section>
  );
}

function TopologyGraph() {
  return (
    <div className="border-cc-card-border/60 rounded border p-6">
      <svg
        viewBox="0 0 360 300"
        className="h-auto w-full"
        role="img"
        aria-label="Service topology: GraphQL fans out to REST, gRPC, job, and database hops, with the slow gRPC hop to billing highlighted."
      >
        {/* edges */}
        <TopEdge x1={180} y1={48} x2={84} y2={132} />
        <TopEdge x1={180} y1={48} x2={180} y2={132} hot />
        <TopEdge x1={180} y1={48} x2={276} y2={132} />
        <TopEdge x1={180} y1={132} x2={132} y2={228} />
        <TopEdge x1={180} y1={132} x2={228} y2={228} />

        {/* nodes */}
        <TopNode x={180} y={48} label="api" sub="GRAPHQL" />
        <TopNode x={84} y={132} label="users-svc" sub="REST" />
        <TopNode x={180} y={132} label="billing" sub="GRPC" hot />
        <TopNode x={276} y={132} label="worker" sub="JOB" />
        <TopNode x={132} y={228} label="accounts" sub="DB" />
        <TopNode x={228} y={228} label="ledger" sub="DB" />
      </svg>
    </div>
  );
}

interface TopEdgeProps {
  readonly x1: number;
  readonly y1: number;
  readonly x2: number;
  readonly y2: number;
  readonly hot?: boolean;
}

function TopEdge({ x1, y1, x2, y2, hot }: TopEdgeProps) {
  return (
    <line
      x1={x1}
      y1={y1}
      x2={x2}
      y2={y2}
      stroke={hot ? CYAN : "var(--color-cc-card-border)"}
      strokeWidth={hot ? 1.5 : 1}
    />
  );
}

interface TopNodeProps {
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly sub: string;
  readonly hot?: boolean;
}

function TopNode({ x, y, label, sub, hot }: TopNodeProps) {
  return (
    <g>
      <rect
        x={x - 50}
        y={y - 18}
        width={100}
        height={36}
        rx={2}
        fill="var(--color-cc-surface)"
        stroke={hot ? CYAN : "var(--color-cc-card-border)"}
        strokeWidth={hot ? 1.25 : 1}
      />
      <text
        x={x}
        y={y - 2}
        fill="var(--color-cc-heading)"
        fontSize="10"
        fontFamily="monospace"
        textAnchor="middle"
      >
        {label}
      </text>
      <text
        x={x}
        y={y + 10}
        fill={hot ? CYAN : "var(--color-cc-nav-label)"}
        fontSize="7.5"
        fontFamily="monospace"
        letterSpacing="0.18em"
        textAnchor="middle"
      >
        {sub}
      </text>
    </g>
  );
}

/* ================================================================== *
 * 05 STANDARD
 * ChilliCream.Nitro.OpenTelemetry. Three short paragraphs framed as
 * What it is / What it isn't / Why it matters.
 * ================================================================== */

function Section05Standard() {
  return (
    <section>
      <SectionMarker num="05" id="standard" />
      <h2 className="font-heading text-h3 text-cc-heading mt-4">
        OpenTelemetry-native, by design
      </h2>
      <p className="lead text-cc-prose mt-4 max-w-2xl">
        The pipeline that carries every signal on this page is{" "}
        <code className="text-cc-ink font-mono">
          ChilliCream.Nitro.OpenTelemetry
        </code>
        , a thin .NET adapter that emits the same OTel spans your other services
        already produce.
      </p>

      <div className="mt-10 flex flex-col gap-8">
        <StandardItem
          label="What it is"
          body="A .NET library that emits OpenTelemetry traces, metrics, and logs from Hot Chocolate and the services around it. The spans flow into Nitro through the standard OTLP path, and into whatever vendor-neutral backend you already operate."
        />
        <StandardItem
          label="What it isn't"
          body="It is not a proprietary agent, not a sidecar, and not a fork of OpenTelemetry. It does not replace your existing observability stack and it does not lock the trace data behind a vendor format."
        />
        <StandardItem
          label="Why it matters"
          body="Because the trace id you see in Nitro is the same id every other service in your topology can correlate against. The standard underneath is the reason the lenses above can be three slices of one stream rather than three disconnected dashboards."
        />
      </div>
    </section>
  );
}

interface StandardItemProps {
  readonly label: string;
  readonly body: string;
}

function StandardItem({ label, body }: StandardItemProps) {
  return (
    <div className="grid gap-2 sm:grid-cols-[180px_1fr] sm:gap-8">
      <div className="text-cc-nav-label font-mono text-[11px] tracking-[0.28em] uppercase">
        {label}
      </div>
      <p className="text-body text-cc-ink-dim max-w-2xl">{body}</p>
    </div>
  );
}

/* ================================================================== *
 * 06 HONESTY
 * 06.a / 06.b / 06.c numbered definition entries. No cards, hairlines
 * only.
 * ================================================================== */

interface HonestyItem {
  readonly num: string;
  readonly title: string;
  readonly body: string;
}

const HONESTY: readonly HonestyItem[] = [
  {
    num: "06.a",
    title: "Telemetry is configured, not magic",
    body: "The dashboards on this page come from telemetry you point at Nitro. It is a configuration step, deliberate and documented, not something that turns on by itself.",
  },
  {
    num: "06.b",
    title: "The IDE is a separate thing",
    body: "The GraphQL IDE can be served from your Hot Chocolate endpoint. That is independent of the telemetry dashboards in this reference. Two facts, kept apart.",
  },
  {
    num: "06.c",
    title: "An open standard underneath",
    body: "It is OpenTelemetry end to end. Vendor-neutral spans mean the data is yours, and there is no proprietary agent locking the trace in.",
  },
];

function Section06Honesty() {
  return (
    <section>
      <SectionMarker num="06" id="honesty" />
      <h2 className="font-heading text-h3 text-cc-heading mt-4">
        Honest about the setup
      </h2>
      <p className="lead text-cc-prose mt-4 max-w-2xl">
        Three notes to keep the reference precise about what is and is not
        included.
      </p>

      <dl className="mt-10 flex flex-col">
        {HONESTY.map((h, i) => (
          <HonestyRow key={h.num} item={h} isLast={i === HONESTY.length - 1} />
        ))}
      </dl>
    </section>
  );
}

interface HonestyRowProps {
  readonly item: HonestyItem;
  readonly isLast: boolean;
}

function HonestyRow({ item, isLast }: HonestyRowProps) {
  return (
    <div
      className={`grid gap-2 py-6 sm:grid-cols-[180px_1fr] sm:gap-8 ${
        isLast ? "" : "border-cc-card-border/60 border-b"
      }`}
    >
      <div>
        <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.28em] uppercase">
          {item.num}
        </span>
        <h3 className="font-heading text-h6 text-cc-heading mt-2">
          {item.title}
        </h3>
      </div>
      <p className="text-body text-cc-ink-dim max-w-2xl self-start">
        {item.body}
      </p>
    </div>
  );
}

/* ================================================================== *
 * CLOSING CTA
 * Hairline-bordered footer block, single cyan top rule as the page's
 * one spectrum event, mono colophon line beneath.
 * ================================================================== */

function ClosingCta() {
  return (
    <section className="border-cc-card-border/60 relative mt-16 border px-6 py-12 sm:px-10">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-px"
        style={{ background: SPECTRUM }}
      />
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.28em] uppercase">
        Closing / Start for Free
      </span>
      <h2 className="font-heading text-h3 text-cc-heading mt-4 max-w-2xl">
        Wire the telemetry once. Read the system from then on.
      </h2>
      <p className="text-body text-cc-ink-dim mt-4 max-w-2xl">
        Point your services at OpenTelemetry, open Nitro, and every request
        becomes evidence: ranked by impact, traced end to end, slow span already
        highlighted.
      </p>
      <div className="mt-8 flex flex-wrap items-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read the Docs
        </OutlineButton>
      </div>
      <p className="text-cc-nav-label mt-8 font-mono text-[11px] tracking-wide">
        colophon · chillicream.nitro.opentelemetry · reference manual · trace{" "}
        {TRACE_ID}
      </p>
    </section>
  );
}

/* ================================================================== *
 * Shared primitives
 * ================================================================== */

interface CheckLineProps {
  readonly children: React.ReactNode;
}

function CheckLine({ children }: CheckLineProps) {
  return (
    <li className="flex items-start gap-2.5">
      <span className="mt-0.5 shrink-0" style={{ color: CYAN }}>
        <CheckIcon size={14} />
      </span>
      <span>{children}</span>
    </li>
  );
}
