import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroMonitoring } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Analytics. See the spike. Find the slow span. Ship the fix.",
  description:
    "Nitro analytics for .NET GraphQL: walk the loop from operation dashboard to distributed trace to impact-ranked diagnosis on top of OpenTelemetry data.",
  keywords: [
    "Nitro analytics",
    "GraphQL observability",
    "OpenTelemetry .NET",
    "distributed tracing",
    "p95 p99 latency",
    "operation monitoring",
    "client usage",
    "impact score",
    "Hot Chocolate",
    "gRPC trace",
  ],
  openGraph: {
    title: "Analytics. See the spike. Find the slow span. Ship the fix.",
    description:
      "Nitro analytics for .NET GraphQL: walk the loop from operation dashboard to distributed trace to impact-ranked diagnosis on top of OpenTelemetry data.",
  },
  robots: { index: false, follow: false },
};

/* ---------------------------------------------------------------------------
   Scene palette. Teal is the signature ink. Status is rationed as data only:
   green healthy, amber investigating, coral firing. The brand spectrum gets
   exactly one appearance, on the hero headline.
--------------------------------------------------------------------------- */
const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";
const TEAL = "#5eead4";
const GREEN = "#34d399";
const AMBER = "#fbbf24";
const CORAL = "#f0786a";
const VIOLET = "#7c92c6";

/* ===========================================================================
   PAGE
=========================================================================== */
export default function AnalyticsPreviewV2Page() {
  return (
    <article className="text-cc-ink">
      <Hero />
      <LoopMap />
      <StepWatch />
      <StepTrace />
      <StepDiagnose />
      <StepShip />
      <ClosingCta />
    </article>
  );
}

/* ===========================================================================
   HERO. Spectrum-accented three-beat headline, mono dateline, dual CTA, and a
   compact loop key (01 to 04) that previews the arc below.
=========================================================================== */
function Hero() {
  return (
    <header className="relative pt-6 sm:pt-10">
      <div className="text-cc-nav-label flex items-center gap-4 font-mono text-[0.7rem] tracking-[0.22em] uppercase">
        <span>Analytics</span>
        <span className="bg-cc-card-border h-px flex-1" />
        <span>/platform/analytics</span>
      </div>

      <h1 className="font-heading text-cc-heading mt-10 max-w-5xl text-[clamp(2.5rem,7vw,5.75rem)] leading-[0.98] font-bold tracking-[-0.02em] text-balance">
        <span className="block">See the spike.</span>
        <span className="block">Find the slow span.</span>
        <span
          className="block bg-clip-text text-transparent"
          style={{ backgroundImage: SPECTRUM }}
        >
          Ship the fix.
        </span>
      </h1>

      <p className="lead text-cc-prose mt-8 max-w-2xl">
        Nitro is the observability loop for a .NET GraphQL API. It watches every
        operation, stitches the trace into the gRPC call that actually went
        slow, and ranks the work by who and what it hurt.
      </p>

      <p className="text-body text-cc-ink-dim mt-5 max-w-2xl">
        OpenTelemetry-native, server side. One screen for service, operation and
        client. The arc on this page is the same arc you run at 2 a.m.
      </p>

      <div className="mt-10 flex flex-wrap items-center gap-4">
        <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Get Started
        </SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch
        </OutlineButton>
      </div>

      <div className="mt-14">
        <LoopKey />
      </div>
    </header>
  );
}

/* ---------------------------------------------------------------------------
   LoopKey. The page table of contents, rendered as a four-stop arc with mono
   step indices and short labels. Each stop scrolls to the matching section.
--------------------------------------------------------------------------- */
function LoopKey() {
  const stops: readonly {
    n: string;
    label: string;
    href: string;
    dot: string;
  }[] = [
    { n: "01", label: "Watch", href: "#watch", dot: GREEN },
    { n: "02", label: "Trace", href: "#trace", dot: TEAL },
    { n: "03", label: "Diagnose", href: "#diagnose", dot: VIOLET },
    { n: "04", label: "Ship", href: "#ship", dot: AMBER },
  ];

  return (
    <nav
      aria-label="Observability loop"
      className="border-cc-card-border bg-cc-card-bg relative grid grid-cols-2 gap-px overflow-hidden rounded-2xl border backdrop-blur sm:grid-cols-4"
    >
      {stops.map((s) => (
        <a
          key={s.n}
          href={s.href}
          className="group bg-cc-bg/40 hover:bg-cc-surface/60 flex flex-col gap-3 p-5 no-underline transition-colors"
        >
          <span className="flex items-center gap-2">
            <span
              aria-hidden
              className="inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: s.dot }}
            />
            <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
              {s.n}
            </span>
          </span>
          <span className="text-cc-heading font-heading text-h5 font-semibold tracking-tight">
            {s.label}
          </span>
        </a>
      ))}
    </nav>
  );
}

/* ===========================================================================
   LoopMap. A thin hairline-ruled summary of the four-step arc. Helps the eye
   anchor before the long-form chapters begin.
=========================================================================== */
function LoopMap() {
  return (
    <section className="mt-24 sm:mt-32">
      <Eyebrow>The loop</Eyebrow>
      <h2 className="font-heading text-cc-heading mt-4 max-w-3xl text-[clamp(1.75rem,3.6vw,2.65rem)] leading-tight font-semibold tracking-tight text-balance">
        One screen, four moves. Watch, trace, diagnose, ship.
      </h2>
      <p className="text-body text-cc-ink-dim mt-5 max-w-2xl">
        Each step has a question, a panel, and a next action. You never leave
        Nitro to find the answer.
      </p>
    </section>
  );
}

/* ===========================================================================
   STEP 01. Watch. The operation monitoring dashboard, embedded as the live
   NitroMonitoring animation. This is the only Nitro embed on the page.
=========================================================================== */
function StepWatch() {
  return (
    <StepShell
      anchor="watch"
      step="01"
      label="Watch"
      dot={GREEN}
      title="The dashboard tells you something moved."
      lede="p95 and p99 by operation, throughput, error rate, and a ranked impact score. The first signal is a sparkline turning amber on checkoutCart."
    >
      <figure className="border-cc-card-border bg-cc-card-bg mx-auto mt-2 max-w-5xl overflow-hidden rounded-xl border">
        <figcaption className="border-cc-card-border text-cc-nav-label flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3 font-mono text-[0.68rem] tracking-[0.16em] uppercase">
          <span>Nitro · operation monitoring</span>
          <span className="flex items-center gap-2">
            <span
              aria-hidden
              className="inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: AMBER }}
            />
            <span style={{ color: AMBER }}>Investigating</span>
          </span>
        </figcaption>
        <NitroMonitoring />
      </figure>

      <ReadingNotes
        items={[
          {
            label: "p95 / p99",
            note: "Tail latency by operation, not by route. A checkout query owns its own histogram.",
          },
          {
            label: "Throughput",
            note: "Requests per minute against the same window so a spike reads as a spike.",
          },
          {
            label: "Error rate",
            note: "GraphQL errors are first class, separated from transport errors.",
          },
          {
            label: "Impact score",
            note: "Latency, error rate and traffic combined, so the top of the table is the operation worth opening.",
          },
        ]}
      />
    </StepShell>
  );
}

/* ===========================================================================
   STEP 02. Trace. Distributed trace waterfall with the slow gRPC hop tinted.
   Drawn entirely in inline SVG so the visual matches the page tokens exactly.
=========================================================================== */
function StepTrace() {
  return (
    <StepShell
      anchor="trace"
      step="02"
      label="Trace"
      dot={TEAL}
      title="Click the spike. The trace is already attached."
      lede="The dashboard is OpenTelemetry under the hood, so every operation row has its slow exemplars one click away. The waterfall shows the GraphQL field, the resolver, the downstream services, and the call that owns the wait."
    >
      <TraceWaterfallVisual />

      <ReadingNotes
        items={[
          {
            label: "GraphQL field span",
            note: "Operation name, variables, and the field that owns this trace, not a generic /graphql route.",
          },
          {
            label: "Cross-service hops",
            note: "REST, gRPC and background jobs are spans next to GraphQL fields, with their own attributes.",
          },
          {
            label: "Slow span tint",
            note: "The hop that owns most of the wall time is highlighted, with its share of the total in the gutter.",
          },
          {
            label: "Attribute drilldown",
            note: "Status, peer, db.statement and custom tags travel with the span. No second tool.",
          },
        ]}
      />
    </StepShell>
  );
}

/* ---------------------------------------------------------------------------
   TraceWaterfallVisual. A compact, honest inline trace built with SVG. The
   widths are illustrative, the labels are realistic.
--------------------------------------------------------------------------- */
interface SpanRow {
  readonly name: string;
  readonly kind: string;
  readonly start: number;
  readonly width: number;
  readonly tone: "ok" | "slow";
  readonly ms: string;
}

function TraceWaterfallVisual() {
  const rows: readonly SpanRow[] = [
    {
      name: "query checkoutCart",
      kind: "graphql",
      start: 0,
      width: 100,
      tone: "ok",
      ms: "486 ms",
    },
    {
      name: "Query.cart",
      kind: "resolver",
      start: 1,
      width: 96,
      tone: "ok",
      ms: "478 ms",
    },
    {
      name: "CartService.Load",
      kind: "rest",
      start: 3,
      width: 14,
      tone: "ok",
      ms: "62 ms",
    },
    {
      name: "PricingService.Quote",
      kind: "grpc",
      start: 18,
      width: 70,
      tone: "slow",
      ms: "338 ms",
    },
    {
      name: "InventoryService.Reserve",
      kind: "grpc",
      start: 88,
      width: 8,
      tone: "ok",
      ms: "38 ms",
    },
    {
      name: "Outbox.Publish",
      kind: "job",
      start: 92,
      width: 6,
      tone: "ok",
      ms: "28 ms",
    },
  ];

  const KIND_COLOR: Record<string, string> = {
    graphql: TEAL,
    resolver: TEAL,
    rest: VIOLET,
    grpc: VIOLET,
    job: GREEN,
  };

  return (
    <figure
      className="border-cc-card-border bg-cc-card-bg mx-auto mt-2 max-w-5xl overflow-hidden rounded-xl border"
      style={{
        backgroundImage:
          "radial-gradient(rgba(245,241,234,0.05) 1px, transparent 1px)",
        backgroundSize: "22px 22px",
      }}
    >
      <figcaption className="border-cc-card-border text-cc-nav-label flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3 font-mono text-[0.68rem] tracking-[0.16em] uppercase">
        <span>Trace · 4e7a · 486 ms</span>
        <span>checkoutCart · prod-eu</span>
      </figcaption>

      <div className="px-4 py-6 sm:px-6 sm:py-7">
        {/* Time ruler */}
        <div className="text-cc-nav-label mb-3 flex justify-between font-mono text-[0.62rem] tracking-tight">
          <span>0 ms</span>
          <span>120</span>
          <span>240</span>
          <span>360</span>
          <span>486 ms</span>
        </div>

        <ul className="flex flex-col gap-2">
          {rows.map((r) => (
            <li
              key={r.name}
              className="grid grid-cols-12 items-center gap-3 text-[0.78rem]"
            >
              <span className="text-cc-ink-dim col-span-4 truncate font-mono">
                <span
                  aria-hidden
                  className="mr-2 inline-block h-1.5 w-1.5 rounded-full align-middle"
                  style={{ background: KIND_COLOR[r.kind] }}
                />
                {r.name}
              </span>

              <div className="relative col-span-6 h-3 overflow-hidden rounded-sm bg-white/[0.03]">
                <span
                  className="absolute top-0 bottom-0 rounded-sm"
                  style={{
                    left: `${r.start}%`,
                    width: `${r.width}%`,
                    background:
                      r.tone === "slow" ? CORAL : "rgba(94, 234, 212, 0.55)",
                    boxShadow:
                      r.tone === "slow"
                        ? `0 0 0 1px ${CORAL}`
                        : "0 0 0 1px rgba(94,234,212,0.45)",
                  }}
                />
              </div>

              <span
                className="col-span-2 text-right font-mono tabular-nums"
                style={{
                  color: r.tone === "slow" ? CORAL : "var(--color-cc-ink-dim)",
                }}
              >
                {r.ms}
              </span>
            </li>
          ))}
        </ul>

        <p className="text-cc-ink-dim mt-6 max-w-2xl font-mono text-[0.7rem] tracking-tight">
          <span style={{ color: CORAL }}>PricingService.Quote</span> owns 69% of
          the wall time. Two upstream calls, both gRPC, both healthy.
        </p>
      </div>
    </figure>
  );
}

/* ===========================================================================
   STEP 03. Diagnose. An impact-ranked operation table with a per-client drill
   in the side panel. Honest illustrative numbers, no client logos.
=========================================================================== */
function StepDiagnose() {
  return (
    <StepShell
      anchor="diagnose"
      step="03"
      label="Diagnose"
      dot={VIOLET}
      title="Rank the work. Open the client that owns the noise."
      lede="The impact score sorts operations by who and what they hurt. The side panel breaks the noisy one down by client, so you know whether to call the mobile team, fix a bad query, or roll the pricing service."
    >
      <DiagnoseVisual />

      <ReadingNotes
        items={[
          {
            label: "Impact rank",
            note: "Combines latency, error rate and traffic. Sorts the queue, doesn't replace your judgment.",
          },
          {
            label: "Per-client usage",
            note: "Same operation, broken down by client name and version, so a regression is attributable.",
          },
          {
            label: ".NET service breakdown",
            note: "GraphQL gateway, REST microservice, gRPC service or background job. Same telemetry shape.",
          },
          {
            label: "Drill stays in Nitro",
            note: "From dashboard to trace to client, all on the same screen. No tool switch.",
          },
        ]}
      />
    </StepShell>
  );
}

interface ImpactRow {
  readonly op: string;
  readonly score: number;
  readonly p99: string;
  readonly rpm: string;
  readonly errors: string;
  readonly tone: "high" | "mid" | "low";
}

function DiagnoseVisual() {
  const rows: readonly ImpactRow[] = [
    {
      op: "checkoutCart",
      score: 92,
      p99: "780 ms",
      rpm: "1.2k",
      errors: "0.4%",
      tone: "high",
    },
    {
      op: "productPricing",
      score: 71,
      p99: "612 ms",
      rpm: "3.4k",
      errors: "0.1%",
      tone: "mid",
    },
    {
      op: "viewerProfile",
      score: 38,
      p99: "204 ms",
      rpm: "6.1k",
      errors: "0.0%",
      tone: "low",
    },
    {
      op: "searchCatalog",
      score: 24,
      p99: "188 ms",
      rpm: "4.7k",
      errors: "0.0%",
      tone: "low",
    },
  ];

  const TONE: Record<ImpactRow["tone"], { bar: string; ink: string }> = {
    high: { bar: CORAL, ink: CORAL },
    mid: { bar: AMBER, ink: AMBER },
    low: { bar: TEAL, ink: TEAL },
  };

  const clients: readonly {
    name: string;
    version: string;
    share: number;
    p99: string;
    tone: "high" | "mid" | "low";
  }[] = [
    {
      name: "store-web",
      version: "4.18.2",
      share: 64,
      p99: "812 ms",
      tone: "high",
    },
    {
      name: "store-ios",
      version: "9.2.0",
      share: 22,
      p99: "604 ms",
      tone: "mid",
    },
    {
      name: "store-android",
      version: "9.1.7",
      share: 11,
      p99: "498 ms",
      tone: "low",
    },
    { name: "ops-cli", version: "0.4.1", share: 3, p99: "210 ms", tone: "low" },
  ];

  return (
    <div className="mx-auto mt-2 grid max-w-5xl gap-5 lg:grid-cols-5">
      {/* Impact-ranked operation table */}
      <figure className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border lg:col-span-3">
        <figcaption className="border-cc-card-border text-cc-nav-label flex items-center justify-between border-b px-4 py-3 font-mono text-[0.68rem] tracking-[0.16em] uppercase">
          <span>Operations · ranked by impact</span>
          <span>last 1h</span>
        </figcaption>

        <table className="w-full text-[0.82rem]">
          <thead>
            <tr className="text-cc-nav-label border-cc-card-border border-b font-mono text-[0.66rem] tracking-[0.16em] uppercase">
              <th className="px-4 py-3 text-left font-normal">Operation</th>
              <th className="px-4 py-3 text-right font-normal">Impact</th>
              <th className="px-4 py-3 text-right font-normal">p99</th>
              <th className="px-4 py-3 text-right font-normal">rpm</th>
              <th className="px-4 py-3 text-right font-normal">err</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r, i) => {
              const tone = TONE[r.tone];
              const selected = i === 0;
              return (
                <tr
                  key={r.op}
                  className={`border-cc-card-border border-b last:border-0 ${
                    selected ? "bg-white/[0.025]" : ""
                  }`}
                >
                  <td className="text-cc-heading px-4 py-3 font-mono">
                    <span className="flex items-center gap-2">
                      <span
                        aria-hidden
                        className="inline-block h-1.5 w-1.5 rounded-full"
                        style={{ background: tone.bar }}
                      />
                      {r.op}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <span className="inline-flex items-center justify-end gap-2">
                      <span
                        className="block h-1.5 w-16 overflow-hidden rounded-sm bg-white/[0.04]"
                        aria-hidden
                      >
                        <span
                          className="block h-full rounded-sm"
                          style={{
                            width: `${r.score}%`,
                            background: tone.bar,
                          }}
                        />
                      </span>
                      <span
                        className="font-mono tabular-nums"
                        style={{ color: tone.ink }}
                      >
                        {r.score}
                      </span>
                    </span>
                  </td>
                  <td className="text-cc-ink-dim px-4 py-3 text-right font-mono tabular-nums">
                    {r.p99}
                  </td>
                  <td className="text-cc-ink-dim px-4 py-3 text-right font-mono tabular-nums">
                    {r.rpm}
                  </td>
                  <td className="text-cc-ink-dim px-4 py-3 text-right font-mono tabular-nums">
                    {r.errors}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </figure>

      {/* Per-client drill */}
      <figure className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border lg:col-span-2">
        <figcaption className="border-cc-card-border text-cc-nav-label flex items-center justify-between border-b px-4 py-3 font-mono text-[0.68rem] tracking-[0.16em] uppercase">
          <span>checkoutCart · by client</span>
          <span style={{ color: CORAL }}>focus</span>
        </figcaption>

        <ul className="flex flex-col">
          {clients.map((c) => {
            const tone = TONE[c.tone];
            return (
              <li
                key={c.name}
                className="border-cc-card-border flex flex-col gap-2 border-b px-4 py-3 last:border-0"
              >
                <span className="flex items-center justify-between gap-3">
                  <span className="text-cc-heading font-mono text-[0.78rem]">
                    {c.name}
                    <span className="text-cc-nav-label ml-2">{c.version}</span>
                  </span>
                  <span
                    className="font-mono text-[0.78rem] tabular-nums"
                    style={{ color: tone.ink }}
                  >
                    p99 {c.p99}
                  </span>
                </span>
                <span className="flex items-center gap-3">
                  <span
                    className="block h-1.5 flex-1 overflow-hidden rounded-sm bg-white/[0.04]"
                    aria-hidden
                  >
                    <span
                      className="block h-full rounded-sm"
                      style={{ width: `${c.share}%`, background: tone.bar }}
                    />
                  </span>
                  <span className="text-cc-ink-dim font-mono text-[0.72rem] tabular-nums">
                    {c.share}%
                  </span>
                </span>
              </li>
            );
          })}
        </ul>
      </figure>
    </div>
  );
}

/* ===========================================================================
   STEP 04. Ship. Honest configuration beat: telemetry needs Nitro
   configuration. A minimal C# snippet shows the .AddInstrumentation hook and
   a checklist of what is in scope.
=========================================================================== */
function StepShip() {
  return (
    <StepShell
      anchor="ship"
      step="04"
      label="Ship"
      dot={AMBER}
      title="Wire it in once. Telemetry needs Nitro configuration."
      lede="Operation, service and client telemetry is opt in. You add it to the Hot Chocolate schema configuration in Program.cs, point it at your Nitro environment, and every operation downstream becomes a row in the dashboard above."
    >
      <div className="mx-auto mt-2 grid max-w-5xl gap-5 lg:grid-cols-5">
        <figure className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border lg:col-span-3">
          <figcaption className="border-cc-card-border text-cc-nav-label flex items-center justify-between border-b px-4 py-3 font-mono text-[0.68rem] tracking-[0.16em] uppercase">
            <span>Program.cs · Hot Chocolate</span>
            <span>C#</span>
          </figcaption>
          <pre className="text-cc-heading m-0 overflow-x-auto bg-transparent px-5 py-5 font-mono text-[0.78rem] leading-relaxed">
            <code>{CSHARP_SNIPPET}</code>
          </pre>
        </figure>

        <ul className="border-cc-card-border bg-cc-card-bg flex flex-col gap-4 rounded-xl border p-5 lg:col-span-2">
          <li className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.22em] uppercase">
            What gets measured
          </li>
          <ChecklistItem>
            Every GraphQL operation, by name, version and client.
          </ChecklistItem>
          <ChecklistItem>
            REST and gRPC services on .NET, as OpenTelemetry spans next to
            GraphQL fields.
          </ChecklistItem>
          <ChecklistItem>
            Background jobs and outbox publishers, when instrumented.
          </ChecklistItem>
          <ChecklistItem>
            Errors classified, with the exception type and the field path.
          </ChecklistItem>
          <li
            className="mt-2 flex items-start gap-3 rounded-md px-3 py-2"
            style={{
              background: "rgba(251,191,36,0.07)",
              border: "1px solid rgba(251,191,36,0.25)",
            }}
          >
            <span
              aria-hidden
              className="mt-1 inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: AMBER }}
            />
            <span className="text-cc-ink text-[0.78rem] leading-relaxed">
              Honest beat. Telemetry is not on by default. You add{" "}
              <code className="text-cc-heading font-mono text-[0.78rem]">
                .AddInstrumentation()
              </code>{" "}
              and point it at a Nitro environment. Out of the box, the schema
              still runs.
            </span>
          </li>
        </ul>
      </div>
    </StepShell>
  );
}

const CSHARP_SNIPPET = `var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddInstrumentation(o =>
    {
        o.IncludeDocument = true;
        // Safe for bounded operation domains (e.g. persisted operations),
        // otherwise high-cardinality span names can blow up your backend.
        o.IncludeOperationNameInSpanName = true;
    });

builder.Services
    .AddOpenTelemetry()
    .WithTracing(t => t
        .AddHotChocolateInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();
app.MapGraphQL();
app.Run();`;

/* ===========================================================================
   CLOSING CTA
=========================================================================== */
function ClosingCta() {
  return (
    <section className="mt-28 sm:mt-36">
      <div className="border-cc-card-border bg-cc-card-bg flex flex-col items-start gap-6 rounded-2xl border p-8 backdrop-blur sm:p-12">
        <Eyebrow>Close the loop</Eyebrow>
        <h2 className="font-heading text-cc-heading max-w-3xl text-[clamp(1.85rem,4vw,2.85rem)] leading-tight font-semibold tracking-tight text-balance">
          Watch, trace, diagnose, ship. Then start again, smarter.
        </h2>
        <p className="text-body text-cc-ink-dim max-w-2xl">
          Wire Nitro into one schema today. Tomorrow, the dashboard already
          knows where to look.
        </p>
        <div className="mt-2 flex flex-wrap items-center gap-3">
          <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
            Get Started
          </SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* ===========================================================================
   Small section primitives.
=========================================================================== */
interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.66rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface StepShellProps {
  readonly anchor: string;
  readonly step: string;
  readonly label: string;
  readonly dot: string;
  readonly title: string;
  readonly lede: string;
  readonly children: ReactNode;
}

function StepShell({
  anchor,
  step,
  label,
  dot,
  title,
  lede,
  children,
}: StepShellProps) {
  return (
    <section id={anchor} className="mt-24 scroll-mt-24 sm:mt-32">
      <div className="grid gap-8 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-4">
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="inline-flex h-8 w-8 items-center justify-center rounded-full font-mono text-[0.72rem] font-semibold tabular-nums"
              style={{
                background: "rgba(245,241,234,0.04)",
                border: `1px solid ${dot}`,
                color: dot,
              }}
            >
              {step}
            </span>
            <Eyebrow>{label}</Eyebrow>
          </div>
          <h2 className="font-heading text-cc-heading mt-5 text-[clamp(1.65rem,3.2vw,2.4rem)] leading-tight font-semibold tracking-tight text-balance">
            {title}
          </h2>
          <p className="text-body text-cc-ink-dim mt-5">{lede}</p>
        </div>
        <div className="lg:col-span-8">{children}</div>
      </div>
    </section>
  );
}

interface ReadingNote {
  readonly label: string;
  readonly note: string;
}

interface ReadingNotesProps {
  readonly items: readonly ReadingNote[];
}

function ReadingNotes({ items }: ReadingNotesProps) {
  return (
    <dl className="mt-7 grid gap-x-8 gap-y-5 sm:grid-cols-2">
      {items.map((it) => (
        <div key={it.label} className="flex flex-col gap-1.5">
          <dt className="text-cc-heading font-mono text-[0.72rem] tracking-tight">
            {it.label}
          </dt>
          <dd className="text-cc-ink-dim text-[0.86rem] leading-relaxed">
            {it.note}
          </dd>
        </div>
      ))}
    </dl>
  );
}

interface ChecklistItemProps {
  readonly children: ReactNode;
}

function ChecklistItem({ children }: ChecklistItemProps) {
  return (
    <li className="text-cc-ink flex items-start gap-3 text-[0.84rem] leading-relaxed">
      <span
        aria-hidden
        className="text-cc-accent mt-0.5 inline-flex h-4 w-4 shrink-0 items-center justify-center"
      >
        <CheckIcon size={14} />
      </span>
      <span>{children}</span>
    </li>
  );
}
