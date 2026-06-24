import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Observability for .NET",
  description:
    "GraphQL observability for .NET. Nitro gives OpenTelemetry-native traces, p95/p99, error rate, and impact scores across GraphQL, REST, gRPC, and background jobs.",
  keywords: [
    "GraphQL observability for .NET",
    "OpenTelemetry GraphQL tracing",
    "Nitro operation monitoring",
    "p95 p99 GraphQL latency",
    "impact score",
    "monitor any .NET service",
    "distributed tracing",
    "correlated logs",
    "per-client usage",
    "one pane of glass",
  ],
  openGraph: {
    title: "GraphQL Observability for .NET: One OpenTelemetry View",
    description:
      "Trace one GraphQL operation through REST, gRPC, and .NET background jobs. p95/p99, error rate, and an impact score that ranks what hurts the system first.",
  },
};

/**
 * One golden-signal metric tile. `figure` is the big numeral in the heading
 * voice; `highlight` swaps the tile into the accent tint for the impact callout.
 */
interface MetricProps {
  readonly figure: string;
  readonly label: string;
  readonly sub: string;
  readonly highlight?: boolean;
}

function Metric({ figure, label, sub, highlight = false }: MetricProps) {
  return (
    <div
      className={[
        "rounded-2xl border p-5 backdrop-blur-sm",
        highlight
          ? "border-cc-accent/50 bg-cc-surface ring-cc-accent/20 ring-1"
          : "border-cc-card-border bg-cc-surface",
      ].join(" ")}
    >
      <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
        {label}
      </p>
      <p
        className={[
          "font-heading text-h4 mt-3 leading-none font-semibold tabular-nums",
          highlight ? "text-cc-accent" : "text-cc-heading",
        ].join(" ")}
      >
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-2 font-mono text-[0.62rem]">{sub}</p>
    </div>
  );
}

/** A single horizontal span bar in the trace waterfall. */
interface SpanRow {
  readonly label: string;
  readonly kind: string;
  readonly left: number;
  readonly width: number;
  /** Tints the bar as the hot span (where the time went). */
  readonly hot?: boolean;
}

const TRACE_SPANS: readonly SpanRow[] = [
  { label: "api", kind: "GraphQL", left: 0, width: 100 },
  { label: "users-svc", kind: "REST", left: 8, width: 22 },
  { label: "billing", kind: "gRPC", left: 30, width: 38, hot: true },
  { label: "invoice-worker", kind: "job", left: 62, width: 24 },
  { label: "orders-db", kind: "SQL", left: 70, width: 18 },
];

/**
 * The trace-waterfall panel: cross-protocol span bars rendered as one trace.
 * Horizontal time, not a vertical step list, so the eye reads where the latency
 * accumulated across the .NET backend.
 */
function TraceWaterfall() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-3xl border p-6 backdrop-blur-sm sm:p-8">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            sample trace &middot; checkout
          </p>
          <p className="text-cc-ink-dim mt-1 text-sm">
            One operation, every hop it touched.
          </p>
        </div>
        <span className="border-cc-accent/40 text-cc-accent rounded-full border px-2.5 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
          GraphQL + REST + gRPC + jobs
        </span>
      </div>

      <div className="border-cc-card-border mt-6 space-y-2.5 border-t pt-6">
        {TRACE_SPANS.map((span) => (
          <div key={span.label} className="flex items-center gap-3">
            <span className="text-cc-ink-dim w-28 shrink-0 text-right font-mono text-[0.6rem]">
              {span.label}
            </span>
            <span className="bg-cc-surface relative h-2.5 flex-1 overflow-hidden rounded-full">
              <span
                className={[
                  "absolute top-0 h-full rounded-full",
                  span.hot ? "bg-cc-accent" : "bg-cc-accent opacity-45",
                ].join(" ")}
                style={{ left: `${span.left}%`, width: `${span.width}%` }}
              />
            </span>
            <span className="text-cc-nav-label w-14 shrink-0 font-mono text-[0.55rem] tracking-[0.06em] uppercase">
              {span.kind}
            </span>
          </div>
        ))}
      </div>

      <div className="border-cc-card-border mt-6 flex flex-wrap items-center justify-between gap-2 border-t pt-4">
        <p className="text-cc-ink-dim font-mono text-[0.62rem]">
          where the time went:{" "}
          <span className="text-cc-accent">billing (gRPC) 38ms</span>
        </p>
        <p className="text-cc-ink-faint font-mono text-[0.6rem]">
          logs correlated inside the trace
        </p>
      </div>
    </div>
  );
}

/** A switcher tab; static, it conveys the three Nitro views without faking a UI. */
function ViewTab({
  name,
  active = false,
}: {
  readonly name: string;
  readonly active?: boolean;
}) {
  return (
    <span
      className={[
        "rounded-lg border px-3 py-1.5 font-mono text-[0.65rem]",
        active
          ? "border-cc-accent/60 text-cc-accent bg-cc-surface"
          : "border-cc-card-border text-cc-ink-dim",
      ].join(" ")}
    >
      {name}
    </span>
  );
}

/** operation / service / client view switcher with the impact-score callout. */
function ViewSwitcher() {
  const rows = [
    { op: "checkout", impact: "#1", p95: "210ms", err: "1.8%" },
    { op: "productPage", impact: "#2", p95: "84ms", err: "0.2%" },
    { op: "search", impact: "#3", p95: "61ms", err: "0.0%" },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-3xl border p-6 backdrop-blur-sm sm:p-8">
      <div className="flex items-center gap-2">
        <ViewTab name="operation" active />
        <ViewTab name="service" />
        <ViewTab name="client" />
      </div>

      <div className="border-cc-card-border mt-6 border-t pt-5">
        <div className="text-cc-nav-label grid grid-cols-[1fr_auto_auto] gap-3 font-mono text-[0.55rem] tracking-[0.1em] uppercase sm:grid-cols-[1fr_auto_auto_auto]">
          <span>operation</span>
          <span className="hidden text-right sm:block">p95</span>
          <span className="text-right">error</span>
          <span className="text-right">impact</span>
        </div>
        <div className="mt-3 space-y-2">
          {rows.map((row, index) => (
            <div
              key={row.op}
              className={[
                "grid grid-cols-[1fr_auto_auto] items-center gap-3 rounded-lg border px-3 py-2.5 sm:grid-cols-[1fr_auto_auto_auto]",
                index === 0
                  ? "border-cc-accent/40 bg-cc-surface"
                  : "border-cc-card-border bg-cc-surface",
              ].join(" ")}
            >
              <span className="text-cc-ink font-mono text-xs">{row.op}</span>
              <span className="text-cc-ink-dim hidden text-right font-mono text-xs tabular-nums sm:block">
                {row.p95}
              </span>
              <span className="text-cc-ink-dim text-right font-mono text-xs tabular-nums">
                {row.err}
              </span>
              <span
                className={[
                  "text-right font-mono text-xs font-semibold",
                  index === 0 ? "text-cc-accent" : "text-cc-ink-dim",
                ].join(" ")}
              >
                {row.impact}
              </span>
            </div>
          ))}
        </div>
      </div>

      <div className="border-cc-card-border mt-5 border-t pt-4">
        <p className="text-cc-ink-dim text-xs/relaxed">
          The impact score ranks operations by how much they hurt the system, so
          you fix the work that matters first instead of chasing the loudest
          error.
        </p>
      </div>
    </div>
  );
}

/** A value section: copy column beside an inline visual, optionally flipped. */
function ValueSection({
  eyebrow,
  heading,
  children,
  visual,
  flip = false,
}: {
  readonly eyebrow: string;
  readonly heading: string;
  readonly children: ReactNode;
  readonly visual: ReactNode;
  readonly flip?: boolean;
}) {
  return (
    <div className="grid items-center gap-8 lg:grid-cols-2 lg:gap-12">
      <div className={flip ? "lg:order-2" : "lg:order-1"}>
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.15em] uppercase">
          {eyebrow}
        </span>
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.1] font-semibold text-balance">
          {heading}
        </h2>
        <div className="text-cc-ink mt-5 space-y-4 text-base/relaxed text-pretty">
          {children}
        </div>
      </div>
      <div className={flip ? "lg:order-1" : "lg:order-2"}>{visual}</div>
    </div>
  );
}

/** Visual: the three OpenTelemetry signals converging into one Nitro pane. */
function SignalLanes() {
  const lanes = [
    { name: "traces", note: "every span, full attribute sidebar" },
    { name: "metrics", note: "p95 / p99, ops/min, error rate" },
    { name: "logs", note: "correlated inside the trace" },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-md rounded-2xl border p-5 backdrop-blur-sm">
      <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
        OpenTelemetry signals
      </p>
      <div className="mt-4 space-y-2.5">
        {lanes.map((lane) => (
          <div
            key={lane.name}
            className="border-cc-card-border bg-cc-surface flex items-center gap-3 rounded-lg border px-3 py-2.5"
          >
            <span className="text-cc-accent w-16 shrink-0 font-mono text-xs font-medium">
              {lane.name}
            </span>
            <span className="text-cc-ink-dim flex-1 font-mono text-[0.62rem]">
              {lane.note}
            </span>
          </div>
        ))}
      </div>
      <p className="text-cc-ink-faint mt-4 text-center font-mono text-[0.58rem]">
        open standard &middot; no proprietary agent
      </p>
    </div>
  );
}

/** Visual: a latency distribution with a brushed band over the slow tail. */
function LatencyDistribution() {
  // Bar heights as percentages; the tail (last three) is "brushed".
  const bars = [
    { h: 26, sel: false },
    { h: 52, sel: false },
    { h: 88, sel: false },
    { h: 64, sel: false },
    { h: 40, sel: false },
    { h: 30, sel: true },
    { h: 22, sel: true },
    { h: 16, sel: true },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-md rounded-2xl border p-5 backdrop-blur-sm">
      <div className="flex items-center justify-between">
        <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          latency distribution
        </p>
        <span className="border-cc-accent/40 text-cc-accent rounded-full border px-2 py-0.5 font-mono text-[0.55rem]">
          isolated 3 slow traces
        </span>
      </div>

      <div className="mt-5 flex h-28 items-end gap-1.5">
        {bars.map((bar, index) => (
          <span
            key={index}
            className={[
              "flex-1 rounded-t-sm",
              bar.sel ? "bg-cc-accent" : "bg-cc-accent opacity-30",
            ].join(" ")}
            style={{ height: `${bar.h}%` }}
          />
        ))}
      </div>

      <div className="text-cc-nav-label mt-2 flex justify-between font-mono text-[0.55rem]">
        <span>fast</span>
        <span className="text-cc-accent">brushed: slow tail</span>
        <span>p99</span>
      </div>
    </div>
  );
}

/** Visual: two client rows by GraphQL-Client-Id, one feeling the slowdown. */
function ClientPanel() {
  const clients = [
    { id: "checkout-ios", share: "58% of latency", flagged: true },
    { id: "admin-web", share: "11% of latency", flagged: false },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-md rounded-2xl border p-5 backdrop-blur-sm">
      <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
        per-client usage &middot; GraphQL-Client-Id
      </p>
      <div className="mt-4 space-y-2.5">
        {clients.map((client) => (
          <div
            key={client.id}
            className={[
              "flex items-center gap-3 rounded-lg border px-3 py-2.5",
              client.flagged
                ? "border-cc-accent/40 bg-cc-surface"
                : "border-cc-card-border bg-cc-surface",
            ].join(" ")}
          >
            <span className="text-cc-ink font-mono text-xs">{client.id}</span>
            <span className="text-cc-ink-dim flex-1 text-right font-mono text-[0.62rem]">
              {client.share}
            </span>
            {client.flagged && (
              <span className="text-cc-accent font-mono text-[0.55rem] tracking-[0.08em] uppercase">
                felt it
              </span>
            )}
          </div>
        ))}
      </div>
      <p className="text-cc-ink-faint mt-4 text-center font-mono text-[0.58rem]">
        which client drove load, which one felt the slowdown
      </p>
    </div>
  );
}

const SETUP_FACTS: readonly string[] = [
  "ChilliCream.Nitro.OpenTelemetry exports traces, metrics, and logs over the OpenTelemetry standard, so signals stay vendor-neutral.",
  "Instrument REST APIs, gRPC services, and background jobs, not just the GraphQL server, for one trace across the whole .NET backend.",
  "Telemetry flows to Nitro for the operation, service, and client dashboards once Nitro is configured.",
  "The GraphQL IDE can be served straight from your Hot Chocolate endpoint for local exploration.",
];

export default function ObservabilityPage() {
  return (
    <>
      {/* Dashboard hero: the data view IS the illustration. */}
      <section className="py-12 sm:py-16">
        <div className="max-w-2xl">
          <p className="text-cc-ink-dim font-mono text-xs font-semibold tracking-widest uppercase">
            GraphQL observability for .NET
          </p>
          <h1 className="font-heading text-cc-heading mt-4 text-4xl leading-[1.05] font-semibold tracking-tight sm:text-5xl lg:text-6xl">
            See what the API is doing.
          </h1>
          <p className="text-cc-ink-dim mt-6 text-base sm:text-lg">
            GraphQL observability for .NET that goes beyond the graph. When an
            operation fans out through REST, gRPC, and background jobs, Nitro
            shows where the time went, which errors matter, and which clients
            felt it, in one OpenTelemetry view.
          </p>
        </div>

        {/* 4-up golden-signals metric strip, one tile carrying the impact callout. */}
        <div className="mt-10 grid grid-cols-2 gap-4 lg:grid-cols-4">
          <Metric figure="42ms" label="p95 latency" sub="avg 18ms · p99 96ms" />
          <Metric figure="96ms" label="p99 latency" sub="tail under SLO" />
          <Metric figure="3.4k" label="throughput" sub="operations / min" />
          <Metric
            figure="#1"
            label="impact: checkout"
            sub="error rate 1.8%"
            highlight
          />
        </div>
      </section>

      {/* Trace-waterfall panel: cross-protocol spans as one trace. */}
      <section className="py-10">
        <div className="mb-6 max-w-3xl">
          <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold">
            Follow one request through every service that touched it.
          </h2>
          <p className="text-cc-ink mt-4 text-base/relaxed">
            A GraphQL operation rarely ends at the resolver. Nitro brings the
            REST fetch, the gRPC call, the worker, and the database query into
            one distributed trace, so a slow operation has an address instead of
            a guess. Every span carries a full attribute sidebar, and logs are
            correlated inside the trace.
          </p>
        </div>
        <TraceWaterfall />
      </section>

      {/* operation / service / client switcher + impact-score callout. */}
      <section className="py-10">
        <div className="mb-6 max-w-3xl">
          <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold">
            Operation, service, and client, from the same data.
          </h2>
          <p className="text-cc-ink mt-4 text-base/relaxed">
            Switch the view without leaving the page: latency with average, p95,
            and p99; throughput in operations per minute; error rate and
            failed-operation counts. The impact score ranks what hurts the
            system most, so triage starts with the work that matters.
          </p>
        </div>
        <ViewSwitcher />
      </section>

      {/* Value section: beyond the graph (any .NET service). */}
      <section className="py-12">
        <ValueSection
          eyebrow="Beyond the graph"
          heading="The whole .NET backend, not just the graph."
          visual={<SignalLanes />}
        >
          <p>
            Most GraphQL control planes watch the graph and stop there. Nitro is
            OpenTelemetry-native end to end, so it watches any .NET service the
            operation reaches: REST APIs, gRPC services, and background jobs via{" "}
            <code className="text-cc-accent">
              ChilliCream.Nitro.OpenTelemetry
            </code>
            .
          </p>
          <p>
            Because it rides an open standard, your traces stay vendor-neutral
            with no proprietary agent to install. One pane of glass replaces a
            per-service dashboard project.
          </p>
        </ValueSection>
      </section>

      {/* Value section: outlier hunting via the latency distribution. */}
      <section className="py-12">
        <ValueSection
          eyebrow="Find the slow tail"
          heading="Brush-select a latency band and isolate the outliers."
          flip
          visual={<LatencyDistribution />}
        >
          <p>
            Toggle operation insights and resolver insights to see where time
            accrues. Then brush-select a latency band on the distribution to
            pull out just the slow traces and inspect what they had in common.
          </p>
          <p>
            Averages hide the tail. The distribution makes the p99 outliers a
            thing you can grab, open, and read span by span, rather than a
            number you argue about.
          </p>
        </ValueSection>
      </section>

      {/* Value section: per-client usage. */}
      <section className="py-12">
        <ValueSection
          eyebrow="Who felt it"
          heading="See which client is driving load or feeling the slowdown."
          visual={<ClientPanel />}
        >
          <p>
            Per-client usage via the{" "}
            <code className="text-cc-accent">GraphQL-Client-Id</code> header
            attributes latency and errors to the client behind them, so
            &ldquo;is it everyone or just one app?&rdquo; is a lookup, not a
            debate.
          </p>
          <p>
            That turns a vague incident into a scoped one: the client driving
            the load, and the client feeling the slowdown, are named in the same
            view.
          </p>
        </ValueSection>
      </section>

      {/* Honesty / credibility beat. */}
      <section className="py-12">
        <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto max-w-3xl rounded-3xl border p-8 backdrop-blur-sm sm:p-10">
          <span className="text-cc-nav-label font-mono text-xs tracking-[0.15em] uppercase">
            Honest scoping
          </span>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-tight font-semibold text-balance">
            Debug from evidence, not another dashboard.
          </h2>
          <p className="text-cc-ink mt-5 text-base/relaxed">
            Two facts that stay separate, because they are separate: the GraphQL
            IDE can be served from your Hot Chocolate endpoint, and the
            operation, service, and client dashboards live in Nitro once it is
            configured. We frame OpenTelemetry as the credibility lever, an open
            standard with no agent lock-in, not a claim that visibility is free.
            An impact score ranks what hurts the system; it is a triage aid, not
            an SLA or an auto-fix.
          </p>
        </div>
      </section>

      {/* "One pane of glass" caption + checklist + CTA. */}
      <section className="py-12">
        <div className="grid items-start gap-8 lg:grid-cols-2 lg:gap-12">
          <div className="border-cc-card-border bg-cc-card-bg/60 rounded-3xl border p-8 backdrop-blur-sm">
            <h2 className="font-heading text-cc-heading text-h4 leading-tight font-semibold">
              One pane of glass for the whole .NET backend.
            </h2>
            <ul className="mt-6 space-y-4">
              {SETUP_FACTS.map((fact) => (
                <li key={fact} className="flex items-start gap-3">
                  <span className="text-cc-accent mt-0.5 shrink-0">
                    <CheckIcon />
                  </span>
                  <span className="text-cc-ink text-sm/relaxed">{fact}</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="flex flex-col justify-center">
            <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold text-balance">
              Watch production with evidence.
            </h2>
            <p className="text-cc-ink mt-5 text-base/relaxed">
              Point your Hot Chocolate API and the .NET services around it at
              Nitro, and read latency, errors, and impact from one OpenTelemetry
              view.
            </p>
            <div className="mt-8 flex flex-wrap gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
                Read the Docs
              </OutlineButton>
            </div>
            <p className="text-cc-ink-dim mt-6 text-sm">
              Learn more about{" "}
              <Link
                href="/platform/analytics"
                className="text-cc-accent hover:text-cc-accent-hover transition-colors"
              >
                analytics
              </Link>
              ,{" "}
              <Link
                href="/platform/continuous-integration"
                className="text-cc-accent hover:text-cc-accent-hover transition-colors"
              >
                continuous integration
              </Link>
              , or the wider{" "}
              <Link
                href="/platform"
                className="text-cc-accent hover:text-cc-accent-hover transition-colors"
              >
                platform
              </Link>
              .
            </p>
          </div>
        </div>
      </section>
    </>
  );
}
