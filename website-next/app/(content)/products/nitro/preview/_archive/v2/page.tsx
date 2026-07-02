import type { Metadata } from "next";
import type { ReactNode } from "react";

import {
  NitroCompose,
  NitroDiagnose,
  NitroFusion,
  NitroMonitoring,
  NitroReel,
  NitroSchema,
  NitroTrace,
} from "@/src/nitro";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Nitro: The Control Plane for GraphQL APIs",
  description:
    "Author, observe, and evolve your GraphQL APIs from one cockpit. OpenTelemetry insights, distributed tracing, schema registry, and the Fusion gateway in Nitro.",
  keywords: [
    "GraphQL control plane",
    "GraphQL API cockpit",
    "GraphQL IDE",
    "OpenTelemetry observability",
    "distributed tracing",
    "GraphQL schema registry",
    "safe schema evolution",
    "Fusion gateway",
    "p95 p99 latency",
    "ChilliCream Nitro",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Nitro: The Control Plane for Your GraphQL APIs",
    description:
      "One cockpit to author, observe, diagnose, and evolve GraphQL APIs. OpenTelemetry insights, distributed tracing, a schema registry, and the Fusion gateway.",
  },
};

/**
 * Immersive breakout wrapper. The (content) layout caps content at max-w-5xl;
 * this escapes that cap so a band can read full-bleed on the dark starfield.
 * The negative margins cancel the layout's horizontal padding (px-5 / sm:px-12)
 * and the max-w-5xl centering, then the inner band re-pads itself.
 */
function FullBleed({
  children,
  className = "",
}: {
  readonly children: ReactNode;
  readonly className?: string;
}) {
  return (
    <div
      className={`relative left-1/2 w-screen -translate-x-1/2 px-5 sm:px-12 ${className}`}
    >
      {children}
    </div>
  );
}

/**
 * Frames a vendored Nitro screen like an embedded product screenshot: a sized,
 * bordered, rounded container with a soft brand glow so the GitHub-dark app UI
 * sits as a surface on the marketing page.
 */
function ScreenFrame({
  children,
  maxWidth = "max-w-5xl",
  glow = "accent",
}: {
  readonly children: ReactNode;
  readonly maxWidth?: string;
  readonly glow?: "accent" | "spectrum" | "none";
}) {
  const glowClass =
    glow === "spectrum"
      ? "before:bg-[radial-gradient(60%_60%_at_50%_0%,rgba(22,185,228,0.18),rgba(124,146,198,0.10)_45%,transparent_75%)]"
      : glow === "accent"
        ? "before:bg-[radial-gradient(55%_55%_at_50%_0%,rgba(94,234,212,0.14),transparent_70%)]"
        : "before:hidden";

  return (
    <div
      className={`relative mx-auto w-full ${maxWidth} before:pointer-events-none before:absolute before:-inset-x-8 before:-top-12 before:-bottom-8 before:-z-10 before:blur-2xl ${glowClass}`}
    >
      <div className="border-cc-card-border bg-cc-surface/40 overflow-hidden rounded-2xl border shadow-2xl shadow-black/40 backdrop-blur-sm">
        {children}
      </div>
    </div>
  );
}

/** Section eyebrow: small mono label that introduces each immersive band. */
function Eyebrow({ children }: { readonly children: ReactNode }) {
  return (
    <span className="text-cc-accent/90 font-mono text-xs font-semibold tracking-[0.18em] uppercase">
      {children}
    </span>
  );
}

/** A compact stat tile used in the bento monitoring band. */
function StatTile({
  metric,
  label,
  detail,
}: {
  readonly metric: string;
  readonly label: string;
  readonly detail: string;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 flex flex-col rounded-2xl border p-6 backdrop-blur-sm">
      <span className="font-heading text-cc-heading text-h4 leading-none font-semibold">
        {metric}
      </span>
      <span className="text-cc-accent mt-3 font-mono text-[0.62rem] tracking-[0.12em] uppercase">
        {label}
      </span>
      <span className="text-cc-ink-dim mt-2 text-sm/relaxed">{detail}</span>
    </div>
  );
}

/**
 * One immersive feature band: leads with a large animated screen, with a punchy
 * one-line benefit headline and a tight paragraph beneath. The visual leads
 * (above the copy) so the product carries the section.
 */
function FeatureBand({
  eyebrow,
  heading,
  body,
  visual,
}: {
  readonly eyebrow: string;
  readonly heading: ReactNode;
  readonly body: ReactNode;
  readonly visual: ReactNode;
}) {
  return (
    <section className="py-20 sm:py-28">
      <div className="mx-auto max-w-3xl text-center">
        <Eyebrow>{eyebrow}</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.08] font-semibold text-balance">
          {heading}
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-5 max-w-2xl text-base/relaxed text-pretty sm:text-lg/relaxed">
          {body}
        </p>
      </div>
      <div className="mt-14">{visual}</div>
    </section>
  );
}

export default function NitroPreviewPage() {
  return (
    <>
      {/* Hero: spectrum-accented headline over the 5-tab product reel. */}
      <section className="pt-16 pb-10 text-center sm:pt-24">
        <span className="text-cc-ink-dim font-mono text-xs font-semibold tracking-[0.2em] uppercase">
          Nitro / The GraphQL Control Plane
        </span>
        <h1 className="font-heading text-cc-heading mx-auto mt-6 max-w-4xl text-5xl leading-[1.04] font-semibold tracking-tight text-balance sm:text-6xl lg:text-7xl">
          Your whole API,{" "}
          <span className="bg-gradient-to-r from-[#16b9e4] via-[#7c92c6] to-[#f0786a] bg-clip-text text-transparent">
            in motion.
          </span>
        </h1>
        <p className="text-cc-ink-dim mx-auto mt-7 max-w-2xl text-lg/relaxed text-pretty sm:text-xl/relaxed">
          Nitro is the cockpit for your GraphQL APIs and your whole .NET
          backend. Author operations, watch them run in production, trace the
          slow span, and evolve the schema, all from one place.
        </p>
        <div className="mt-9 flex flex-wrap justify-center gap-4">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch Nitro
          </OutlineButton>
        </div>
      </section>

      {/* The reel is the hero centerpiece. Break it out wider than the column. */}
      <FullBleed className="pb-6">
        <ScreenFrame maxWidth="max-w-6xl" glow="spectrum">
          <NitroReel />
        </ScreenFrame>
      </FullBleed>

      {/* Author: the GraphQL IDE. Distributed trace screen leads the band. */}
      <FeatureBand
        eyebrow="Author"
        heading="A schema-aware IDE that runs the real thing."
        body="Explore any GraphQL schema, write operations with full type awareness, and run them against your endpoint as federated data streams back. The IDE serves straight from the endpoint, so you are testing the API you ship."
        visual={
          <ScreenFrame maxWidth="max-w-4xl">
            <NitroCompose />
          </ScreenFrame>
        }
      />

      {/* Observe: bento band pairing the full monitoring dashboard with stats. */}
      <FullBleed>
        <section className="py-20 sm:py-28">
          <div className="mx-auto max-w-5xl">
            <div className="max-w-3xl">
              <Eyebrow>Observe</Eyebrow>
              <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.08] font-semibold text-balance">
                See every operation, ranked by what it costs you.
              </h2>
              <p className="text-cc-ink-dim mt-5 max-w-2xl text-base/relaxed text-pretty sm:text-lg/relaxed">
                Wire up OpenTelemetry and Nitro turns the firehose into a
                dashboard: latency with p95 and p99, throughput, error rate, and
                an impact score that sorts operations by how much they hurt the
                system. Not just GraphQL, any .NET service you point at it.
              </p>
            </div>

            <div className="mt-12 grid gap-5 lg:grid-cols-3">
              <div className="lg:col-span-2">
                <ScreenFrame maxWidth="max-w-none" glow="none">
                  <NitroMonitoring />
                </ScreenFrame>
              </div>
              <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-1">
                <StatTile
                  metric="p95 / p99"
                  label="latency"
                  detail="Tail latency surfaced next to the average, so outliers do not hide behind the mean."
                />
                <StatTile
                  metric="impact"
                  label="ranked insights"
                  detail="Operation and resolver insights sorted by system impact, not just raw call count."
                />
              </div>
            </div>
          </div>
        </section>
      </FullBleed>

      {/* Diagnose: error spike to failing operation to stack trace. */}
      <FeatureBand
        eyebrow="Diagnose"
        heading="From an error spike to the exact failing operation."
        body="When errors climb, follow the spike straight to the operation behind it and the server stack trace that explains it. No grepping logs across services to reconstruct what went wrong."
        visual={
          <ScreenFrame maxWidth="max-w-4xl" glow="spectrum">
            <NitroDiagnose />
          </ScreenFrame>
        }
      />

      {/* Trace: one request across GraphQL + REST/gRPC/jobs, full bleed wide. */}
      <FullBleed>
        <section className="py-20 sm:py-28">
          <div className="mx-auto max-w-5xl text-center">
            <Eyebrow>Distributed tracing</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.08] font-semibold text-balance">
              One request, every hop, down to the slow span.
            </h2>
            <p className="text-cc-ink-dim mx-auto mt-5 max-w-2xl text-base/relaxed text-pretty sm:text-lg/relaxed">
              Follow a single request as a waterfall across GraphQL, REST, gRPC,
              and background jobs. Spans and correlated logs sit together, so
              the hop that cost you the latency is obvious at a glance.
            </p>
          </div>
          <div className="mt-14">
            <ScreenFrame maxWidth="max-w-5xl">
              <NitroTrace />
            </ScreenFrame>
          </div>
        </section>
      </FullBleed>

      {/* Schema: registry + safe evolution. */}
      <FeatureBand
        eyebrow="Evolve"
        heading="Change the schema without breaking published clients."
        body="The schema registry classifies every change as safe, dangerous, or breaking and checks it against the operations published clients depend on. Validate in your PR build, publish per stage, and roll back to an earlier tagged version when you need to."
        visual={
          <ScreenFrame maxWidth="max-w-4xl">
            <NitroSchema />
          </ScreenFrame>
        }
      />

      {/* Fusion: distributed query plan across subgraphs. */}
      <FeatureBand
        eyebrow="Fusion"
        heading="One query, a plan that fans out across subgraphs."
        body="The Fusion gateway composes your subgraphs into one graph and executes each operation as a distributed query plan, fetching in parallel and batching across services. Watch the plan and the per-subgraph telemetry in the same cockpit."
        visual={
          <ScreenFrame maxWidth="max-w-4xl" glow="spectrum">
            <NitroFusion />
          </ScreenFrame>
        }
      />

      {/* Closing CTA. */}
      <section className="py-24 text-center sm:py-32">
        <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto max-w-3xl rounded-3xl border p-10 backdrop-blur-sm sm:p-14">
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 leading-[1.05] font-semibold text-balance">
            Take command of your graph.
          </h2>
          <p className="text-cc-ink-dim mx-auto mt-6 max-w-xl text-base/relaxed text-pretty sm:text-lg/relaxed">
            Author, observe, diagnose, and evolve your GraphQL APIs from one
            control plane. Start free, or launch Nitro and explore right now.
          </p>
          <div className="mt-9 flex flex-wrap justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
