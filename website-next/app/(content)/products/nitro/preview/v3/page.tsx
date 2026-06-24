import type { Metadata } from "next";
import type { ReactNode } from "react";

import {
  NitroReel,
  NitroMonitoring,
  NitroTrace,
  NitroDiagnose,
  NitroSchema,
  NitroFusion,
} from "@/src/nitro";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Nitro: The Control Plane for GraphQL & .NET",
  description:
    "Nitro is the cockpit for your GraphQL and .NET backend: a schema-aware IDE, OpenTelemetry observability, distributed tracing, a CI-gated schema registry, and the Fusion gateway.",
  keywords: [
    "Nitro",
    "GraphQL control plane",
    "GraphQL IDE",
    "OpenTelemetry observability",
    "distributed tracing",
    "schema registry",
    "breaking change detection",
    "Fusion gateway",
    "Hot Chocolate",
    ".NET APM",
  ],
  openGraph: {
    title: "Nitro: The Control Plane for GraphQL & .NET",
    description:
      "Author, observe, diagnose, evolve, and federate from one cockpit. OpenTelemetry-native, self-hostable, CI-gated. Built for the engineers who run the graph.",
  },
  robots: { index: false, follow: false },
};

/* ----------------------------------------------------------------------------
   Scene palette. teal #5eead4 is the signature; the brand spectrum
   (cyan -> violet -> coral) is rationed to a single hero gradient. Status
   hues carry data, not decoration.
---------------------------------------------------------------------------- */
const TEAL = "#5eead4";
const CYAN = "#16b9e4";
const VIOLET = "#7c92c6";
const CORAL = "#f0786a";
const GREEN = "#34d399";
const AMBER = "#fbbf24";

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

interface ScreenFrameProps {
  readonly route: string;
  readonly children: ReactNode;
  readonly className?: string;
  readonly glow?: string;
}

/**
 * Frames a vendored Nitro screen as an embedded product screenshot: a window
 * chrome bar over the Nitro app surface, sized by the caller. The glow is a
 * single rationed corner accent so the frame reads as layered depth.
 */
function ScreenFrame({
  route,
  children,
  className = "",
  glow,
}: ScreenFrameProps) {
  return (
    <div
      className={`border-cc-card-border bg-cc-surface/40 relative overflow-hidden rounded-xl border shadow-[0_30px_70px_-34px_rgba(0,0,0,0.8)] backdrop-blur ${className}`}
    >
      {glow && (
        <div
          className="pointer-events-none absolute -top-20 -right-20 -z-0 h-48 w-48 rounded-full opacity-40 blur-3xl"
          style={{ backgroundColor: `${glow}55` }}
          aria-hidden
        />
      )}
      <div className="border-cc-card-border bg-cc-code-header relative z-10 flex items-center gap-3 border-b px-4 py-2.5">
        <div className="flex gap-1.5" aria-hidden>
          <span className="h-2.5 w-2.5 rounded-full bg-[#f0786a]/70" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#fbbf24]/70" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#34d399]/70" />
        </div>
        <span className="text-cc-ink-dim truncate font-mono text-[0.66rem]">
          {route}
        </span>
      </div>
      <div className="relative z-10">{children}</div>
    </div>
  );
}

interface BandProps {
  readonly children: ReactNode;
  readonly className?: string;
}

/** A feature band: tight vertical rhythm between sections. */
function Band({ children, className = "" }: BandProps) {
  return (
    <section className={`mt-20 sm:mt-28 ${className}`}>{children}</section>
  );
}

interface BandHeadProps {
  readonly tag: string;
  readonly color?: string;
  readonly kicker: string;
  readonly title: ReactNode;
  readonly body: string;
}

function BandHead({ tag, color, kicker, title, body }: BandHeadProps) {
  return (
    <div className="max-w-2xl">
      <Eyebrow tag={tag} color={color}>
        {kicker}
      </Eyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4 tracking-tight">
        {title}
      </h2>
      <p className="text-cc-ink-dim mt-4 leading-relaxed">{body}</p>
    </div>
  );
}

/* ============================================================================
   HERO: the reel front and center, the brand spectrum used once.
============================================================================ */

function Hero() {
  return (
    <header className="relative pt-10 sm:pt-16">
      <div
        className="pointer-events-none absolute inset-0 -top-24 -z-10"
        style={{
          background:
            "radial-gradient(60% 60% at 18% 0%, rgba(22,185,228,0.16), transparent 60%), radial-gradient(55% 55% at 82% 8%, rgba(124,146,198,0.14), transparent 60%), radial-gradient(50% 50% at 60% 30%, rgba(240,120,106,0.10), transparent 60%)",
        }}
        aria-hidden
      />

      <div className="text-center">
        <Eyebrow tag="nitro" color={TEAL}>
          <span className="inline-flex items-center justify-center">
            control plane for graphql &amp; .net
          </span>
        </Eyebrow>
        <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mx-auto mt-6 max-w-4xl tracking-tight">
          One cockpit for the
          <span
            className="bg-clip-text text-transparent"
            style={{
              backgroundImage: `linear-gradient(100deg, ${CYAN}, ${VIOLET} 55%, ${CORAL})`,
            }}
          >
            {" "}
            whole API lifecycle
          </span>
          .
        </h1>
        <p className="text-cc-prose mx-auto mt-6 max-w-2xl text-base leading-relaxed sm:text-lg">
          Author operations against a live schema, watch p95/p99 and impact
          score in production, chase one trace across GraphQL, REST, gRPC and
          jobs, evolve the schema without breaking published clients, and
          federate it all through Fusion. OpenTelemetry-native and
          self-hostable.
        </p>
        <div className="mt-9 flex flex-wrap justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch Nitro
          </OutlineButton>
        </div>
      </div>

      {/* The reel is the centerpiece (wide). */}
      <div className="mt-14 sm:mt-16">
        <div
          className="pointer-events-none absolute left-1/2 hidden h-72 w-[58rem] -translate-x-1/2 rounded-full opacity-30 blur-3xl lg:block"
          style={{
            background: `radial-gradient(closest-side, ${TEAL}33, transparent)`,
          }}
          aria-hidden
        />
        <NitroReel className="border-cc-card-border relative mx-auto block max-w-6xl overflow-hidden rounded-2xl border shadow-[0_40px_90px_-40px_rgba(0,0,0,0.85)]" />
      </div>
    </header>
  );
}

/* ============================================================================
   STAT ROW: concrete, measured numbers as proof.
============================================================================ */

interface StatProps {
  readonly value: string;
  readonly unit?: string;
  readonly label: string;
  readonly color: string;
}

const STATS: readonly StatProps[] = [
  { value: "p95 / p99", label: "latency per operation", color: TEAL },
  {
    value: "1",
    unit: "trace",
    label: "across GraphQL · REST · gRPC · jobs",
    color: VIOLET,
  },
  {
    value: "3",
    unit: "classes",
    label: "safe · dangerous · breaking",
    color: AMBER,
  },
  { value: "0", unit: "restart", label: "hot reload on publish", color: GREEN },
];

function StatRow() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg mt-20 grid grid-cols-2 gap-px overflow-hidden rounded-2xl border backdrop-blur sm:mt-24 sm:grid-cols-4">
      {STATS.map((s) => (
        <div key={s.label} className="bg-cc-surface/30 px-6 py-7">
          <div className="font-heading text-cc-heading flex items-baseline gap-1.5 text-[2rem] leading-none tracking-tight tabular-nums sm:text-[2.4rem]">
            <span style={{ color: s.color }}>{s.value}</span>
            {s.unit && (
              <span className="text-cc-ink-dim font-mono text-sm font-normal">
                {s.unit}
              </span>
            )}
          </div>
          <p className="text-caption text-cc-ink-dim mt-3 leading-snug">
            {s.label}
          </p>
        </div>
      ))}
    </section>
  );
}

/* ============================================================================
   OBSERVE + TRACE: two product surfaces side by side in one dense band.
============================================================================ */

function ObserveBand() {
  return (
    <Band>
      <BandHead
        tag="observe"
        kicker="opentelemetry-native"
        title={<>From the impact-ranked board straight to the slow span.</>}
        body="Nitro ingests OpenTelemetry over OTLP, so the same traces your services already emit drive every chart. Rank operations by impact score, then follow one trace-id from the spike down to the gRPC hop that actually held the request, across GraphQL, REST, and background jobs, not just the graph edge."
      />

      <div className="mt-9 grid items-start gap-6 lg:grid-cols-[1.25fr_1fr]">
        <ScreenFrame
          route="nitro › monitoring › operations"
          glow={TEAL}
          className="lg:sticky lg:top-24"
        >
          <NitroMonitoring className="block w-full" />
        </ScreenFrame>

        <div className="space-y-6">
          <ScreenFrame route="nitro › trace › checkout" glow={VIOLET}>
            <NitroTrace className="block w-full" />
          </ScreenFrame>
          <ul className="space-y-3">
            <MiniFeature color={TEAL}>
              Latency (avg / p95 / p99), throughput in ops/min, error rate, and
              a sortable impact column.
            </MiniFeature>
            <MiniFeature color={VIOLET}>
              Distributed waterfall with every span; logs correlated inside the
              trace they belong to.
            </MiniFeature>
            <MiniFeature color={AMBER}>
              Per-client usage via the GraphQL-Client-Id header, so you see
              which client leans hardest on the API.
            </MiniFeature>
          </ul>
        </div>
      </div>
    </Band>
  );
}

interface MiniFeatureProps {
  readonly color: string;
  readonly children: ReactNode;
}

function MiniFeature({ color, children }: MiniFeatureProps) {
  return (
    <li className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed">
      <span className="mt-0.5 shrink-0" style={{ color }}>
        <CheckIcon size={15} />
      </span>
      <span>{children}</span>
    </li>
  );
}

/* ============================================================================
   DIAGNOSE: error spike to the failing operation + server stack trace.
============================================================================ */

function DiagnoseBand() {
  return (
    <Band>
      <div className="grid items-center gap-10 lg:grid-cols-[1fr_1.15fr]">
        <BandHead
          tag="diagnose"
          color={CORAL}
          kicker="spike to stack trace"
          title="An error spike resolves to the exact failing operation."
          body="When the error rate climbs, you don't grep logs across services. Nitro takes the spike, pins the operation that's throwing, and surfaces the server stack trace and the affected response classes, so the path from symptom to the line of code is one drill-down, not an afternoon."
        />
        <ScreenFrame route="nitro › errors › applyCoupon" glow={CORAL}>
          <NitroDiagnose className="block w-full" />
        </ScreenFrame>
      </div>
    </Band>
  );
}

/* ============================================================================
   EVOLVE: schema registry + CI lifecycle accent.
============================================================================ */

const CLI_LINES: readonly {
  readonly text: string;
  readonly comment?: string;
}[] = [
  {
    text: "$ nitro schema validate",
    comment: "# PR build · catch breaking changes",
  },
  {
    text: "$ nitro schema upload --tag $SHA",
    comment: "# release build · tagged version",
  },
  {
    text: "$ nitro schema publish --stage prod",
    comment: "# deploy · make it active",
  },
];

function EvolveBand() {
  return (
    <Band>
      <div className="grid items-start gap-10 lg:grid-cols-[1.15fr_1fr]">
        <div>
          <BandHead
            tag="evolve"
            color={AMBER}
            kicker="schema registry · ci-gated"
            title="Change fields without breaking published clients."
            body="The registry classifies every diff as safe, dangerous, or breaking, and validates the schema against the client registry, the contract between your API and the clients that depend on it. Breaking changes are caught in the PR build, not in a customer incident."
          />
          <ScreenFrame
            route="nitro › schema › reference"
            glow={AMBER}
            className="mt-8"
          >
            <NitroSchema className="block w-full" />
          </ScreenFrame>
        </div>

        {/* CLI / lifecycle accent: validate -> upload -> publish */}
        <div className="lg:sticky lg:top-24">
          <div className="border-cc-card-border bg-cc-code-bg overflow-hidden rounded-xl border shadow-[0_30px_70px_-40px_rgba(0,0,0,0.8)]">
            <div className="border-cc-card-border bg-cc-code-header flex items-center gap-2 border-b px-4 py-2.5">
              <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                .github/workflows/api.yml
              </span>
            </div>
            <pre className="overflow-x-auto px-5 py-5 font-mono text-[0.74rem] leading-relaxed">
              <code>
                {CLI_LINES.map((line, i) => (
                  <span key={line.text} className="block">
                    <span className="text-cc-heading">{line.text}</span>
                    {line.comment && (
                      <span className="text-cc-nav-label">
                        {"  "}
                        {line.comment}
                      </span>
                    )}
                    {i === 0 && (
                      <span className="text-cc-ink-dim block">
                        {"  "}✓ 0 breaking · 1 dangerous · 4 safe
                      </span>
                    )}
                  </span>
                ))}
              </code>
            </pre>
          </div>
          <ul className="mt-6 space-y-3">
            <MiniFeature color={GREEN}>
              <span className="font-mono text-[0.82em]">
                validate → upload → publish
              </span>{" "}
              maps to PR → release → deploy.
            </MiniFeature>
            <MiniFeature color={AMBER}>
              Version history with rollback, plus a deployment audit log of
              every publish.
            </MiniFeature>
            <MiniFeature color={CORAL}>
              <span className="font-mono text-[0.82em]">
                --wait-for-approval
              </span>{" "}
              gates risky changes behind a human review.
            </MiniFeature>
          </ul>
        </div>
      </div>
    </Band>
  );
}

/* ============================================================================
   FEDERATE: Fusion distributed query plan.
============================================================================ */

function FederateBand() {
  return (
    <Band>
      <div className="grid items-center gap-10 lg:grid-cols-[1fr_1.15fr]">
        <BandHead
          tag="federate"
          color={CYAN}
          kicker="fusion gateway"
          title="One query, a distributed execution plan across subgraphs."
          body="Fusion composes your subgraphs into a single gateway. The query plan shows how one operation fans out into parallel, batched fetches across services, and Nitro monitors the gateway and every subgraph in one place: topology, per-subgraph latency, throughput, and errors."
        />
        <ScreenFrame route="nitro › fusion › query-plan" glow={CYAN}>
          <NitroFusion className="block w-full" />
        </ScreenFrame>
      </div>
    </Band>
  );
}

/* ============================================================================
   CAPABILITY GRID: dense surface of what else is in the cockpit.
============================================================================ */

interface CapabilityProps {
  readonly title: string;
  readonly body: string;
  readonly color: string;
}

const CAPABILITIES: readonly CapabilityProps[] = [
  {
    title: "Schema-aware IDE",
    body: "Autocomplete, error highlighting, and run operations against any GraphQL API. Web PWA or desktop.",
    color: TEAL,
  },
  {
    title: "Client registry",
    body: "Persisted operations double as a security lock; version clients independently per stage.",
    color: VIOLET,
  },
  {
    title: "Monitor any .NET service",
    body: "Not just the graph: REST APIs, gRPC services, and background jobs through the same OTel pipeline.",
    color: GREEN,
  },
  {
    title: "Stages & deployments",
    body: "Model dev / QA / prod, each with its own active schema, client versions, and telemetry.",
    color: AMBER,
  },
  {
    title: "Self-hostable",
    body: "Serve the GraphQL IDE straight from your Hot Chocolate endpoint; no proprietary agent to adopt.",
    color: CYAN,
  },
  {
    title: "Org & workspaces",
    body: "Shared documents and environments that sync across devices, with Owner / Admin / Collaborator roles.",
    color: CORAL,
  },
  {
    title: "MCP tools for agents",
    body: "Expose GraphQL operations as MCP tools, versioned and observed per-tool with the same telemetry engine.",
    color: VIOLET,
  },
  {
    title: "CLI for everything",
    body: "APIs, schemas, clients, stages, Fusion, and MCP: the full control-plane verb surface, scriptable.",
    color: TEAL,
  },
];

function CapabilityGrid() {
  return (
    <Band>
      <BandHead
        tag="cockpit"
        kicker="batteries included"
        title="The rest of the control plane."
        body="The reel shows the five flows you live in. Underneath sit the registry, the org model, the CLI, and the surfaces that make Nitro a cockpit and not just a dashboard."
      />
      <div className="mt-9 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {CAPABILITIES.map((c) => (
          <div
            key={c.title}
            className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-5 backdrop-blur"
          >
            <span
              className="block h-1 w-7 rounded-full"
              style={{ backgroundColor: c.color }}
              aria-hidden
            />
            <h3 className="font-heading text-h6 text-cc-heading mt-4">
              {c.title}
            </h3>
            <p className="text-caption text-cc-ink-dim mt-2 leading-relaxed">
              {c.body}
            </p>
          </div>
        ))}
      </div>
    </Band>
  );
}

/* ============================================================================
   HONESTY BEAT: keep the IDE and the dashboards two distinct facts.
============================================================================ */

function HonestyBand() {
  const points: readonly string[] = [
    "Telemetry is plain OpenTelemetry: it flows to Nitro and to any OTel backend you already run.",
    "The GraphQL IDE serves straight from your Hot Chocolate endpoint: one switch, no cloud round-trip.",
    "The telemetry dashboards are a separate step: they need Nitro configuration before the charts light up.",
    "The registry tells you which published clients an operation affects, then gates the change in CI.",
  ];
  return (
    <Band>
      <div className="border-cc-card-border bg-cc-card-bg grid gap-8 rounded-3xl border p-6 backdrop-blur sm:grid-cols-[0.8fr_1.2fr] sm:p-10">
        <div>
          <Eyebrow tag="scope" color={GREEN}>
            what is true
          </Eyebrow>
          <h2 className="font-heading text-h5 text-cc-heading sm:text-h4 mt-4">
            Honest about where the lines are.
          </h2>
          <p className="text-cc-ink-dim mt-3 leading-relaxed">
            Serving the IDE is one switch; wiring telemetry into Nitro is a
            deliberate configuration step. Two facts, kept apart on purpose.
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
      </div>
    </Band>
  );
}

/* ============================================================================
   CLOSING CTA
============================================================================ */

function ClosingCta() {
  return (
    <Band>
      <section className="border-cc-card-border bg-cc-surface/40 relative overflow-hidden rounded-3xl border px-6 py-14 text-center backdrop-blur sm:px-12 sm:py-20">
        <div
          className="pointer-events-none absolute inset-0 -z-10"
          style={{
            background:
              "radial-gradient(55% 90% at 50% -10%, rgba(94,234,212,0.16), transparent 60%)",
          }}
          aria-hidden
        />
        <Eyebrow tag="ship it" color={GREEN}>
          eyes open
        </Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h2 mx-auto mt-5 max-w-2xl">
          Run the graph from one cockpit.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-5 max-w-xl leading-relaxed">
          Author, observe, diagnose, evolve, and federate. OpenTelemetry-native,
          CI-gated, and self-hostable. The IDE is free to start.
        </p>
        <div className="mt-9 flex flex-wrap justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch Nitro
          </OutlineButton>
        </div>
      </section>
    </Band>
  );
}

/* ============================================================================
   PAGE
============================================================================ */

export default function NitroPreviewV3Page() {
  return (
    <main>
      <Hero />
      <StatRow />
      <ObserveBand />
      <DiagnoseBand />
      <EvolveBand />
      <FederateBand />
      <CapabilityGrid />
      <HonestyBand />
      <ClosingCta />
    </main>
  );
}
