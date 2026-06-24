import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  NitroDiagnose,
  NitroFusion,
  NitroMonitoring,
  NitroReel,
  NitroSchema,
  NitroTrace,
} from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro: GraphQL Control Plane, Weighed Side by Side",
  description:
    "Nitro is the GraphQL control plane: weigh hand-rolled observability, tracing, diagnosis, schema evolution and Fusion federation side by side, one spine.",
  keywords: [
    "Nitro",
    "Nitro GraphQL control plane",
    "GraphQL IDE",
    "OpenTelemetry",
    "distributed tracing",
    "schema registry",
    "client registry",
    "Fusion gateway",
    "API observability",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Nitro: GraphQL Control Plane, Weighed Side by Side",
    description:
      "Weigh hand-rolled GraphQL operations against Nitro: observe, trace, diagnose, evolve and compose, judged on one persistent spine.",
    type: "website",
  },
};

// Brand spectrum gradient, used exactly once on this page (hero hairline).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const CORAL = "#f0786a";

/* -----------------------------------------------------------------------
 * Primitives
 * ---------------------------------------------------------------------*/

interface EyebrowProps {
  readonly children: ReactNode;
  readonly tone?: "accent" | "muted";
}

function Eyebrow({ children, tone = "accent" }: EyebrowProps) {
  const color = tone === "accent" ? "text-cc-accent" : "text-cc-nav-label";
  return (
    <span
      className={[
        color,
        "text-caption font-medium tracking-[0.2em] uppercase",
      ].join(" ")}
    >
      {children}
    </span>
  );
}

interface CrossIconProps {
  readonly size?: number;
}

function CrossIcon({ size = 14 }: CrossIconProps) {
  return (
    <svg viewBox="0 0 16 16" width={size} height={size} aria-hidden="true">
      <path
        d="M4 4 L12 12 M12 4 L4 12"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

/* -----------------------------------------------------------------------
 * Chips
 * ---------------------------------------------------------------------*/

interface ChipProps {
  readonly children: ReactNode;
}

function HandRolledChip({ children }: ChipProps) {
  return (
    <span
      className="text-caption inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono tracking-[0.2em] uppercase tabular-nums"
      style={{
        borderColor: "rgba(240, 120, 106, 0.45)",
        background: "rgba(240, 120, 106, 0.08)",
        color: CORAL,
      }}
    >
      <CrossIcon size={12} />
      <span className="font-medium">{children}</span>
    </span>
  );
}

function NitroChip({ children }: ChipProps) {
  return (
    <span className="border-cc-accent/40 bg-cc-accent/10 text-cc-accent text-caption inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono tracking-[0.2em] uppercase tabular-nums">
      <CheckIcon size={12} />
      <span className="font-medium">{children}</span>
    </span>
  );
}

/* -----------------------------------------------------------------------
 * Split row: the page's signature 2-column compare with a vertical spine.
 * ---------------------------------------------------------------------*/

interface SplitRowProps {
  readonly left: ReactNode;
  readonly right: ReactNode;
}

function SplitRow({ left, right }: SplitRowProps) {
  return (
    <div className="grid gap-0 lg:grid-cols-2">
      <div className="border-cc-card-border border-b py-8 pr-0 pb-10 lg:border-r lg:border-b-0 lg:py-10 lg:pr-10">
        {left}
      </div>
      <div className="py-8 pt-10 pl-0 lg:py-10 lg:pt-10 lg:pl-10">{right}</div>
    </div>
  );
}

/* -----------------------------------------------------------------------
 * Verdict section: shared eyebrow + h3 caption above a SplitRow with two
 * equal cards. Left card lists the hand-rolled liability; right card shows
 * Nitro's answer with a compact inline product still.
 * ---------------------------------------------------------------------*/

interface VerdictSectionProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly leftHeading: string;
  readonly leftBody: string;
  readonly leftPoints: readonly string[];
  readonly rightHeading: string;
  readonly rightBody: string;
  readonly rightPoints: readonly string[];
  readonly visual: ReactNode;
}

function VerdictSection({
  id,
  index,
  eyebrow,
  title,
  leftHeading,
  leftBody,
  leftPoints,
  rightHeading,
  rightBody,
  rightPoints,
  visual,
}: VerdictSectionProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-16 sm:py-20"
    >
      <div className="mx-auto flex max-w-3xl flex-col items-center gap-4 text-center">
        <div className="flex items-center gap-3">
          <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
            {index}
          </span>
          <Eyebrow>{eyebrow}</Eyebrow>
        </div>
        <h2 className="text-cc-heading font-heading text-h3 text-balance">
          {title}
        </h2>
      </div>

      <div className="mt-10">
        <SplitRow
          left={
            <VerdictCard
              side="left"
              heading={leftHeading}
              body={leftBody}
              points={leftPoints}
            />
          }
          right={
            <VerdictCard
              side="right"
              heading={rightHeading}
              body={rightBody}
              points={rightPoints}
              visual={visual}
            />
          }
        />
      </div>
    </section>
  );
}

interface VerdictCardProps {
  readonly side: "left" | "right";
  readonly heading: string;
  readonly body: string;
  readonly points: readonly string[];
  readonly visual?: ReactNode;
}

function VerdictCard({
  side,
  heading,
  body,
  points,
  visual,
}: VerdictCardProps) {
  const isLeft = side === "left";
  return (
    <article className="border-cc-card-border bg-cc-card-bg flex h-full flex-col gap-5 rounded-xl border p-6 sm:p-7">
      <div className="flex items-center justify-between gap-3">
        {isLeft ? (
          <HandRolledChip>Hand-rolled</HandRolledChip>
        ) : (
          <NitroChip>With Nitro</NitroChip>
        )}
        <span className="text-cc-nav-label text-caption font-mono tracking-[0.2em] uppercase tabular-nums">
          {isLeft ? "Liability" : "Asset"}
        </span>
      </div>
      <h3 className="text-cc-heading font-heading text-h6 text-balance">
        {heading}
      </h3>
      <p className="text-cc-ink text-body max-w-md leading-relaxed text-pretty">
        {body}
      </p>
      <ul className="mt-1 flex flex-col gap-3">
        {points.map((point) => (
          <li key={point} className="flex items-start gap-3">
            <span
              aria-hidden="true"
              className="mt-[6px] inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full"
              style={
                isLeft
                  ? {
                      color: CORAL,
                      background: "rgba(240, 120, 106, 0.12)",
                      border: "1px solid rgba(240, 120, 106, 0.35)",
                    }
                  : undefined
              }
            >
              {isLeft ? (
                <CrossIcon size={10} />
              ) : (
                <span className="text-cc-accent bg-cc-accent/10 border-cc-accent/35 inline-flex h-4 w-4 items-center justify-center rounded-full border">
                  <CheckIcon size={10} />
                </span>
              )}
            </span>
            <span className="text-cc-ink-dim text-body leading-relaxed">
              {point}
            </span>
          </li>
        ))}
      </ul>
      {visual ? (
        <div className="border-cc-card-border bg-cc-surface mt-3 overflow-hidden rounded-lg border">
          {visual}
        </div>
      ) : null}
    </article>
  );
}

/* -----------------------------------------------------------------------
 * Page
 * ---------------------------------------------------------------------*/

export default function NitroVerdictLedgerPage() {
  // The (content) layout caps children at max-w-5xl with px-5 sm:px-12. To
  // honor the spec's max-w-6xl single column, escape that cap with a
  // FullBleed-style relative left-1/2 w-screen -translate-x-1/2 wrapper and
  // re-pad inside, then re-center at max-w-6xl.
  return (
    <div className="relative left-1/2 w-screen -translate-x-1/2 px-5 sm:px-12">
      <div className="mx-auto w-full max-w-6xl">
        {/* HERO: single centered block, no split. ─────────────────── */}
        <section className="pt-6 pb-14 text-center sm:pt-12">
          <div className="mx-auto flex max-w-3xl flex-col items-center gap-6">
            <div className="flex flex-col items-center gap-4">
              <span
                aria-hidden="true"
                className="h-px w-24 rounded-full"
                style={{ background: SPECTRUM }}
              />
              <Eyebrow>The Control Plane for GraphQL</Eyebrow>
            </div>
            <h1 className="text-cc-heading font-heading text-hero text-balance">
              Weigh it for yourself.
            </h1>
            <p className="text-cc-ink text-lead mx-auto max-w-2xl leading-relaxed">
              Nitro is the GraphQL control plane for your .NET backend. On the
              left, the hand-rolled status quo. On the right, Nitro. One spine,
              five verdicts, no hand-waving.
            </p>
            <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="https://nitro.chillicream.com">
                Launch Nitro
              </OutlineButton>
            </div>
          </div>
        </section>

        {/* LEDGER HEADER BAR ─────────────────────────────────────── */}
        <section
          aria-label="The ledger"
          className="border-cc-card-border bg-cc-card-bg sticky top-18 z-10 mt-6 rounded-xl border backdrop-blur-md"
        >
          <SplitRow
            left={
              <div className="flex items-center justify-between gap-4 px-2 py-1 sm:px-4">
                <div className="flex items-center gap-3">
                  <HandRolledChip>Hand-rolled</HandRolledChip>
                  <span className="text-cc-heading font-heading text-h6 hidden sm:inline">
                    The status quo
                  </span>
                </div>
                <span className="text-cc-nav-label text-caption font-mono tracking-[0.2em] uppercase tabular-nums">
                  LHS
                </span>
              </div>
            }
            right={
              <div className="flex items-center justify-between gap-4 px-2 py-1 sm:px-4">
                <div className="flex items-center gap-3">
                  <NitroChip>With Nitro</NitroChip>
                  <span className="text-cc-heading font-heading text-h6 hidden sm:inline">
                    The control plane
                  </span>
                </div>
                <span className="text-cc-nav-label text-caption font-mono tracking-[0.2em] uppercase tabular-nums">
                  RHS
                </span>
              </div>
            }
          />
        </section>

        {/* EVIDENCE REEL ─────────────────────────────────────────── */}
        <section
          id="evidence"
          className="border-cc-card-border border-t py-16 sm:py-20"
        >
          <div className="mx-auto flex max-w-3xl flex-col items-center gap-4 text-center">
            <div className="flex items-center gap-3">
              <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
                00
              </span>
              <Eyebrow>Exhibit A</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading text-h2 text-balance">
              Exhibit A: your API in motion.
            </h2>
            <p className="text-cc-ink text-body max-w-2xl leading-relaxed text-pretty">
              The hand-rolled column lists what running production GraphQL
              without a control plane actually costs. The Nitro column rests its
              case on the live, five-tab product reel.
            </p>
          </div>

          <div className="mt-10">
            <SplitRow
              left={
                <article className="border-cc-card-border bg-cc-card-bg flex h-full flex-col gap-5 rounded-xl border p-6 sm:p-7">
                  <div className="flex items-center justify-between gap-3">
                    <HandRolledChip>Hand-rolled</HandRolledChip>
                    <span className="text-cc-nav-label text-caption font-mono tracking-[0.2em] uppercase tabular-nums">
                      4 liabilities
                    </span>
                  </div>
                  <h3 className="text-cc-heading font-heading text-h6 text-balance">
                    What the status quo charges you.
                  </h3>
                  <ul className="flex flex-col gap-4">
                    {[
                      "Bespoke dashboards stitched together per service, drifting out of sync with the schema.",
                      "Tracing that stops at the edge: GraphQL says ok, the slow span hides three hops down.",
                      "Error spikes investigated by grepping logs, asking the user to repro, and guessing.",
                      "Schema changes shipped on hope, with deprecation comments and apology threads.",
                    ].map((point, idx) => (
                      <li key={point} className="flex items-start gap-3">
                        <span
                          aria-hidden="true"
                          className="mt-[2px] inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full"
                          style={{
                            color: CORAL,
                            background: "rgba(240, 120, 106, 0.12)",
                            border: "1px solid rgba(240, 120, 106, 0.35)",
                          }}
                        >
                          <CrossIcon size={11} />
                        </span>
                        <span className="text-cc-ink text-body leading-relaxed">
                          <span className="text-cc-nav-label text-caption mr-2 font-mono tabular-nums">
                            {String(idx + 1).padStart(2, "0")}
                          </span>
                          {point}
                        </span>
                      </li>
                    ))}
                  </ul>
                </article>
              }
              right={
                <article className="border-cc-card-border bg-cc-card-bg flex h-full flex-col gap-5 rounded-xl border p-6 sm:p-7">
                  <div className="flex items-center justify-between gap-3">
                    <NitroChip>With Nitro</NitroChip>
                    <span className="text-cc-nav-label text-caption font-mono tracking-[0.2em] uppercase tabular-nums">
                      Live exhibit
                    </span>
                  </div>
                  <h3 className="text-cc-heading font-heading text-h6 text-balance">
                    Five tabs. One cockpit. The same telemetry, owned.
                  </h3>
                  <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-lg border shadow-2xl shadow-black/40">
                    <NitroReel />
                  </div>
                  <p className="text-cc-ink-dim text-body leading-relaxed">
                    Observe, Trace, Diagnose, Evolve, Compose: each tab is one
                    verdict you will weigh below.
                  </p>
                </article>
              }
            />
          </div>
        </section>

        {/* VERDICTS ──────────────────────────────────────────────── */}
        <VerdictSection
          id="observe"
          index="01"
          eyebrow="Verdict 01 / Observe"
          title="See what production is actually doing."
          leftHeading="Dashboards you cobble together."
          leftBody="A patchwork of metrics adapters, hand-built panels, and p95 figures you mostly guess at, because the metric was never tied to the operation."
          leftPoints={[
            "Per-operation latency, throughput, and error rate are not first-class.",
            "Impact is judged by whoever shouts loudest, not by usage data.",
          ]}
          rightHeading="OpenTelemetry, per operation."
          rightBody="Wire up Nitro and OpenTelemetry to watch latency, throughput, and error rate per operation, with p95 and p99, per-client usage, and an impact score that ranks what hurts the system most."
          rightPoints={[
            "p95 and p99 per operation and per client, not per process.",
            "Impact score ranks operations by real production weight.",
          ]}
          visual={<NitroMonitoring className="w-full" />}
        />

        <VerdictSection
          id="trace"
          index="02"
          eyebrow="Verdict 02 / Trace"
          title="Follow one request end to end."
          leftHeading="Log greps across services."
          leftBody="A correlation id, three log viewers, and a guess about which hop went slow. The slow span lives behind a service boundary you cannot see across."
          leftPoints={[
            "GraphQL responds ok while a downstream span quietly burns seconds.",
            "Background jobs are out of band, so the trace ends at the edge.",
          ]}
          rightHeading="Spans across the whole backend."
          rightBody="Distributed tracing stitches a single operation across GraphQL, REST, gRPC, and background jobs. Walk the span waterfall down to the resolver that ran slow."
          rightPoints={[
            "One waterfall from the operation down to the resolver and the call beyond.",
            "GraphQL, REST, gRPC, and jobs sit on the same trace, not separate ones.",
          ]}
          visual={<NitroTrace className="w-full" />}
        />

        <VerdictSection
          id="diagnose"
          index="03"
          eyebrow="Verdict 03 / Diagnose"
          title="From a spike to the line that threw it."
          leftHeading="Log spelunking and repro guesses."
          leftBody="Errors climb, the on-call opens five tabs, and the diagnosis is whatever the most senior person remembers seeing last time. Customers wait."
          leftPoints={[
            "No path from a chart spike to the failing operation, only to a service.",
            "Server-side stack traces live in a log store, not next to the operation.",
          ]}
          rightHeading="Spike, operation, stack trace."
          rightBody="When errors climb, Nitro takes you from the spike to the exact failing operation and the server-side stack trace behind it, with no log spelunking required."
          rightPoints={[
            "Click the spike, get the operation, get the stack trace.",
            "The same view shows variables and the trace that produced the error.",
          ]}
          visual={<NitroDiagnose className="w-full" />}
        />

        <VerdictSection
          id="schema"
          index="04"
          eyebrow="Verdict 04 / Evolve"
          title="Ship schema changes without breaking clients."
          leftHeading="Hope, deprecation, apologies."
          leftBody="A field is dropped. A nullability flips. Three weeks later, a client team that nobody told learns about it from a customer ticket. Slack threads follow."
          leftPoints={[
            "Breaking changes are classified, if at all, by whoever wrote the PR.",
            "No record of which client versions still call the field you removed.",
          ]}
          rightHeading="Safe, dangerous, breaking, in CI."
          rightBody="The schema registry classifies every change as safe, dangerous, or breaking and checks it against published clients in CI, so you validate on a PR and publish only when it is safe to ship."
          rightPoints={[
            "Every PR carries a verdict: safe, dangerous, or breaking.",
            "Breaking changes name the published clients affected before merge.",
          ]}
          visual={<NitroSchema className="w-full" />}
        />

        <VerdictSection
          id="fusion"
          index="05"
          eyebrow="Verdict 05 / Compose"
          title="One graph across every subgraph."
          leftHeading="Hand-stitched federation."
          leftBody="A bespoke gateway nobody owns, opaque fan-out across subgraphs, and a fetch waterfall that nobody can read once a request becomes interesting."
          leftPoints={[
            "Composition logic lives in the gateway at runtime, drifting between deploys.",
            "Subgraph fan-out is invisible until something is slow in production.",
          ]}
          rightHeading="Fusion: composed at planning time."
          rightBody="With Fusion, Nitro shows the distributed query plan: how a single operation fans out into parallel, batched fetches across your subgraphs and folds back into one response."
          rightPoints={[
            "Composition happens at planning time; the gateway you run executes the plan.",
            "Per-operation query plan is inspectable, not a black box.",
          ]}
          visual={<NitroFusion className="w-full" />}
        />

        {/* FINAL VERDICT CTA ─────────────────────────────────────── */}
        <section className="border-cc-card-border border-t py-24 text-center sm:py-28">
          <div className="mx-auto flex max-w-2xl flex-col items-center gap-6">
            <Eyebrow>The ledger closes</Eyebrow>
            <h2 className="text-cc-heading font-heading text-h2 text-balance">
              Put your API on the control plane.
            </h2>
            <p className="text-cc-ink text-body max-w-xl leading-relaxed">
              Five verdicts, one column on each side of the rule. Start in the
              GraphQL IDE in seconds, then grow into observability, tracing, and
              a registry that keeps your schema and clients in sync.
            </p>
            <div className="mt-2 flex flex-wrap items-center justify-center gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="https://nitro.chillicream.com">
                Launch Nitro
              </OutlineButton>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}
