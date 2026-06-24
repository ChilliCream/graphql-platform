import type { Metadata } from "next";
import Link from "next/link";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Workflows: Mocha Mediator & Message Bus for .NET",
  description:
    "Field manual for event-driven workflows in .NET. Mocha is a source-generated mediator and message bus with validated sagas and outbox plus inbox.",
  keywords: [
    "mocha",
    "dotnet message bus",
    "in-process mediator",
    "CQRS",
    "sagas",
    "transactional outbox",
    "idempotent inbox",
    "exactly-once processing",
    "RabbitMQ",
    "Kafka",
    "Postgres transport",
    "Azure Service Bus",
    "event-driven architecture",
    "OpenTelemetry tracing",
  ],
  openGraph: {
    title: "Mocha Workflows: mediator + message bus for .NET",
    description:
      "Field manual for event-driven workflows .NET. Source-generated mediator, validated sagas, outbox plus inbox exactly-once processing.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/*  Accent: coral, applied only to active TOC marks, section          */
/*  ordinals, one trace span, and a single hairline rule.             */
/* ------------------------------------------------------------------ */

const CORAL = "#f0786a";

/* ============================  TOC entries  ======================== */

interface TocEntry {
  readonly ord: string;
  readonly id: string;
  readonly label: string;
}

const TOC: readonly TocEntry[] = [
  { ord: "§00", id: "masthead", label: "Masthead" },
  { ord: "§01", id: "premise", label: "Premise" },
  { ord: "§02", id: "dispatches", label: "Two dispatches" },
  { ord: "§03", id: "sagas", label: "Sagas" },
  { ord: "§04", id: "reliability", label: "Outbox plus inbox" },
  { ord: "§05", id: "transports", label: "Transports" },
  { ord: "§06", id: "observability", label: "Observability" },
  { ord: "§07", id: "reference", label: "Reference card" },
];

/* ============================  Sidebar  ============================ */

function SidebarToc() {
  return (
    <aside className="hidden lg:block">
      <nav
        aria-label="Section index"
        className="sticky top-24 max-h-[calc(100vh-7rem)] w-[240px] overflow-y-auto pr-4"
      >
        <div className="border-cc-card-border bg-cc-card-bg/60 mb-5 flex items-center justify-between rounded border px-3 py-2">
          <span className="text-cc-heading font-mono text-[11px] tracking-[0.18em] uppercase">
            mocha
          </span>
          <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
            v3.x
          </span>
        </div>
        <ol className="space-y-0.5">
          {TOC.map((entry) => (
            <li key={entry.id}>
              <a
                href={`#${entry.id}`}
                className="group text-cc-ink-dim hover:text-cc-heading relative flex items-center gap-3 py-1.5 pr-2 pl-3 font-mono text-[11px] tracking-[0.18em] uppercase"
              >
                <span className="w-8 shrink-0">{entry.ord}</span>
                <span className="truncate">{entry.label}</span>
              </a>
            </li>
          ))}
        </ol>
        <div className="border-cc-card-border mt-6 border-t pt-4">
          <Link
            href="/docs/mocha"
            className="text-cc-ink-dim hover:text-cc-heading font-mono text-[11px] tracking-[0.18em] uppercase"
          >
            Read the docs →
          </Link>
        </div>
      </nav>
    </aside>
  );
}

/* ===========================  Mobile TOC  ========================== */

function MobileToc() {
  return (
    <nav
      aria-label="Section index"
      className="border-cc-card-border bg-cc-bg/90 sticky top-16 z-10 -mx-4 mb-8 overflow-x-auto border-y px-4 py-2 backdrop-blur lg:hidden"
    >
      <ol className="flex snap-x snap-mandatory gap-2">
        {TOC.map((entry) => (
          <li key={entry.id} className="snap-start">
            <a
              href={`#${entry.id}`}
              className="border-cc-card-border bg-cc-card-bg/40 text-cc-ink-dim hover:text-cc-heading flex items-center gap-2 rounded border px-2.5 py-1.5 font-mono text-[10px] tracking-[0.18em] whitespace-nowrap uppercase"
            >
              <span>{entry.ord}</span>
              <span>{entry.label}</span>
            </a>
          </li>
        ))}
      </ol>
    </nav>
  );
}

/* ===========================  Section shell  ======================= */

interface SectionProps {
  readonly id: string;
  readonly ordinal: string;
  readonly title: string;
  readonly deck: string;
  readonly children: React.ReactNode;
  readonly first?: boolean;
}

function Section({ id, ordinal, title, deck, children, first }: SectionProps) {
  return (
    <section
      id={id}
      className={`relative scroll-mt-28 ${
        first ? "pt-0" : "border-cc-card-border border-t pt-12"
      } pb-12`}
    >
      <span
        aria-hidden
        className="absolute hidden font-mono text-[11px] tracking-[0.18em] uppercase lg:block"
        style={{
          color: CORAL,
          left: "-2rem",
          top: first ? "0.25rem" : "3.25rem",
        }}
      >
        <span className="relative inline-block">
          {ordinal}
          <span
            className="absolute right-0 -bottom-1 left-0 h-[2px]"
            style={{ backgroundColor: CORAL }}
          />
        </span>
      </span>
      <p
        className="mb-2 font-mono text-[11px] tracking-[0.18em] uppercase lg:hidden"
        style={{ color: CORAL }}
      >
        {ordinal}
      </p>
      <h2 className="font-heading text-h3 text-cc-heading">{title}</h2>
      <p className="lead text-cc-ink-dim mt-3 max-w-[68ch]">{deck}</p>
      <div className="mt-6 max-w-[68ch]">{children}</div>
    </section>
  );
}

/* ===========================  Mono pill  =========================== */

interface MonoPillProps {
  readonly children: React.ReactNode;
}

function MonoPill({ children }: MonoPillProps) {
  return (
    <span className="border-cc-card-border bg-cc-card-bg/40 text-cc-ink rounded border px-2.5 py-1 font-mono text-[11px] tracking-[0.18em] uppercase">
      {children}
    </span>
  );
}

/* ===========================  Step strip  ========================== */

interface StepStripProps {
  readonly steps: readonly string[];
  readonly accent?: boolean;
}

function StepStrip({ steps, accent }: StepStripProps) {
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      {steps.map((s, i) => (
        <div key={s} className="flex items-center gap-1.5">
          <span className="border-cc-card-border bg-cc-card-bg/40 text-cc-heading rounded border px-2.5 py-1.5 font-mono text-[11px]">
            {s}
          </span>
          {i < steps.length - 1 && (
            <svg viewBox="0 0 24 10" className="h-2.5 w-5" aria-hidden>
              <line
                x1="0"
                y1="5"
                x2="18"
                y2="5"
                stroke={accent ? CORAL : "rgba(245,241,234,0.3)"}
                strokeWidth="1.5"
              />
              <polygon
                points="17,1 24,5 17,9"
                fill={accent ? CORAL : "rgba(245,241,234,0.45)"}
              />
            </svg>
          )}
        </div>
      ))}
    </div>
  );
}

/* ===========================  Dispatch card  ======================= */

interface DispatchCardProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly steps: readonly string[];
  readonly note: string;
}

function DispatchCard({ eyebrow, title, steps, note }: DispatchCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/40 rounded border p-4">
      <p className="text-cc-nav-label mb-2 font-mono text-[11px] tracking-[0.18em] uppercase">
        {eyebrow}
      </p>
      <h3 className="font-heading text-h6 text-cc-heading mb-3">{title}</h3>
      <StepStrip steps={steps} />
      <p className="text-cc-ink-dim mt-3 text-sm">{note}</p>
    </div>
  );
}

/* ===========================  Saga state pill  ===================== */

interface SagaPillProps {
  readonly label: string;
  readonly validated?: boolean;
  readonly first?: boolean;
}

function SagaPill({ label, validated, first }: SagaPillProps) {
  return (
    <div className="flex items-center gap-2 sm:gap-3">
      {!first && (
        <svg viewBox="0 0 40 12" className="h-3 w-8 sm:w-10" aria-hidden>
          <line
            x1="0"
            y1="6"
            x2="34"
            y2="6"
            stroke={validated ? CORAL : "rgba(245,241,234,0.3)"}
            strokeWidth="1.5"
          />
          <polygon
            points="33,2 40,6 33,10"
            fill={validated ? CORAL : "rgba(245,241,234,0.45)"}
          />
        </svg>
      )}
      <div className="border-cc-card-border bg-cc-card-bg/40 relative rounded-full border px-3.5 py-1.5">
        <span className="text-cc-heading font-mono text-[11px] tracking-[0.18em] uppercase">
          {label}
        </span>
        {validated && (
          <span
            aria-hidden
            className="absolute right-2 -bottom-1 left-2 h-[2px]"
            style={{ backgroundColor: CORAL }}
          />
        )}
      </div>
    </div>
  );
}

/* ===========================  Reliability panel  =================== */

interface ReliabilityPanelProps {
  readonly tag: string;
  readonly title: string;
  readonly body: string;
}

function ReliabilityPanel({ tag, title, body }: ReliabilityPanelProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/40 rounded border p-4">
      <div className="mb-2 flex items-center gap-2">
        <span
          className="size-1.5 rounded-full"
          style={{ backgroundColor: CORAL }}
        />
        <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
          {tag}
        </span>
      </div>
      <h3 className="font-heading text-h4 text-cc-heading mb-2">{title}</h3>
      <p className="text-cc-ink-dim text-sm">{body}</p>
    </div>
  );
}

/* ===========================  Trace ribbon  ======================== */

interface TraceSpanProps {
  readonly label: string;
  readonly widthPct: number;
  readonly offsetPct: number;
  readonly live?: boolean;
}

function TraceSpan({ label, widthPct, offsetPct, live }: TraceSpanProps) {
  return (
    <div className="flex items-center gap-3">
      <span className="text-cc-ink w-32 shrink-0 truncate font-mono text-[11px]">
        {label}
      </span>
      <div className="bg-cc-surface/50 relative h-4 flex-1 rounded-sm">
        <div
          className="absolute top-0 h-4 rounded-sm"
          style={{
            left: `${offsetPct}%`,
            width: `${widthPct}%`,
            backgroundColor: live ? CORAL : "rgba(94,234,212,0.35)",
          }}
        />
      </div>
    </div>
  );
}

/* ===========================  Page  ================================ */

export default function WorkflowsPreviewV5() {
  return (
    <div className="py-6">
      <div className="lg:grid lg:grid-cols-[240px_minmax(0,1fr)] lg:gap-12">
        <SidebarToc />

        <main className="min-w-0">
          <MobileToc />

          {/* ============================  Masthead  ===================== */}
          <section id="masthead" className="relative scroll-mt-28 pb-10">
            <p className="text-cc-nav-label mb-5 font-mono text-[11px] tracking-[0.18em] uppercase">
              mocha / event-driven workflows .NET
            </p>
            <h1 className="font-heading text-h1 text-cc-heading lg:text-hero max-w-[18ch]">
              Let work continue after the request.
            </h1>
            <p className="lead text-cc-ink-dim mt-6 max-w-[68ch]">
              A source-generated mediator and cross-service bus for .NET. Sagas
              validated before traffic, outbox plus inbox for exactly-once
              processing, pluggable transports.
            </p>
            <div className="mt-8 flex flex-wrap items-center gap-3">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
            </div>
            <div
              aria-hidden
              className="mt-10 h-[2px] w-32"
              style={{ backgroundColor: CORAL }}
            />
          </section>

          {/* ============================  §01 Premise  =================== */}
          <Section
            id="premise"
            ordinal="§01"
            title="Request-bound work hurts. Move past it."
            deck="A request handler that has to send three emails, refresh a search index, and notify two services either blocks the response or fakes async with a fire-and-forget Task. Both paths lose work the moment something fails."
          >
            <p className="text-cc-prose">
              The discipline is simple. Return the response the instant the
              caller needs it, then keep moving the rest of the work on its own
              schedule, with handlers that can crash, restart, and pick up where
              they left off. Mocha is the framework for that discipline, one
              source-generated programming model for both the command you
              dispatch inside a process and the event you publish across
              services.
            </p>
            <p className="text-cc-prose mt-4">
              Three primitives carry the model.
            </p>
            <div className="mt-4 flex flex-wrap gap-2">
              <MonoPill>mediator</MonoPill>
              <MonoPill>bus</MonoPill>
              <MonoPill>sagas</MonoPill>
            </div>
          </Section>

          {/* ============================  §02 Dispatches  ================ */}
          <Section
            id="dispatches"
            ordinal="§02"
            title="Two dispatches, one model."
            deck="Inside one process the mediator runs commands and queries through a pre-compiled pipeline. Across services the same publish crosses a transport and fans out to its consumers. You change the verb, not the mental model."
          >
            <div className="grid gap-3 sm:grid-cols-2">
              <DispatchCard
                eyebrow="mediator · in-process"
                title="Dispatch and reply"
                steps={["CreateReview", "[Handler]"]}
                note="Commands and queries resolve through a source-generated pipeline. No reflection on the hot path."
              />
              <DispatchCard
                eyebrow="bus · cross-service"
                title="Publish and fan out"
                steps={["ReviewCreated", "consumers"]}
                note="One event reaches every interested service through a pluggable transport, each consumer processing it exactly once."
              />
            </div>
            <p className="text-cc-ink-dim mt-4 text-sm">
              Both lanes share the same dispatch table, generated at build by
              the Mocha source generator. The runtime never reflects to find a
              handler.
            </p>
          </Section>

          {/* ============================  §03 Sagas  ===================== */}
          <Section
            id="sagas"
            ordinal="§03"
            title="Sagas validated before traffic."
            deck="A review moves Draft to Checked to Published across several messages and minutes. Define the state machine once and Mocha proves every state is reachable and every path reaches a final state before the service takes traffic."
          >
            <div className="border-cc-card-border bg-cc-card-bg/40 rounded border p-5">
              <div className="mb-4 flex items-center justify-between">
                <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
                  ReviewSaga
                </span>
                <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[11px] tracking-[0.18em] uppercase">
                  <span style={{ color: CORAL }}>
                    <CheckIcon size={12} />
                  </span>
                  validated
                </span>
              </div>
              <div className="flex flex-wrap items-center gap-y-3">
                <SagaPill label="Draft" validated first />
                <SagaPill label="Checked" validated />
                <SagaPill label="Published" validated />
              </div>
              <p className="text-cc-ink-dim mt-5 text-sm">
                The reachability check runs at host start, before the service
                accepts traffic, not at compile time. A saga that can dead-end
                fails the boot, not production.
              </p>
            </div>
          </Section>

          {/* ============================  §04 Reliability  =============== */}
          <Section
            id="reliability"
            ordinal="§04"
            title="Outbox plus inbox = exactly-once processing."
            deck="Reliable messaging is two ledgers cooperating. The outbox writes the message to the database in the same transaction as your data. The inbox dedupes on receive, so a redelivered message is observed but not re-applied."
          >
            <div className="space-y-3">
              <ReliabilityPanel
                tag="outbox"
                title="Commits with the DB write"
                body="The publish is staged in a table inside your transaction. A relay drains the outbox to the transport. If your DB write commits, the message will be sent. If it rolls back, nothing leaks."
              />
              <ReliabilityPanel
                tag="inbox"
                title="Dedupes on receive"
                body="Each consumer records the message id it has processed. A redelivery is acknowledged but not re-handled, so handlers can run as if a message arrives once."
              />
            </div>
            <div className="border-cc-card-border bg-cc-card-bg/40 mt-4 rounded border p-4">
              <p
                className="mb-1 font-mono text-[11px] tracking-[0.18em] uppercase"
                style={{ color: CORAL }}
              >
                stated honestly
              </p>
              <h3 className="font-heading text-h4 text-cc-heading mb-2">
                Exactly-once processing, not delivery
              </h3>
              <p className="text-cc-ink-dim text-sm">
                No transport can promise exactly-once delivery. Mocha promises
                exactly-once processing through the outbox and inbox pair, the
                guarantee your handlers actually need.
              </p>
            </div>
          </Section>

          {/* ============================  §05 Transports  ================ */}
          <Section
            id="transports"
            ordinal="§05"
            title="Transports are a registration detail."
            deck="Start in-process, move to Postgres or RabbitMQ in production, stream high-throughput topics through Kafka, run several at once. Swap the registration, keep the handlers."
          >
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
              {[
                "RabbitMQ",
                "Postgres",
                "Kafka",
                "Azure Service Bus",
                "In-process",
                "Azure Event Hub",
              ].map((t) => (
                <div
                  key={t}
                  className="border-cc-card-border bg-cc-card-bg/40 rounded border px-3 py-2"
                >
                  <span className="text-cc-heading font-mono text-[12px] tracking-[0.08em]">
                    {t}
                  </span>
                </div>
              ))}
            </div>
            <p className="text-cc-ink-dim mt-4 text-sm">
              Swap the registration, keep the handlers. The contract a handler
              sees is the message type, not the broker.
            </p>
          </Section>

          {/* ============================  §06 Observability  ============= */}
          <Section
            id="observability"
            ordinal="§06"
            title="Every hop a span."
            deck="Publish, dispatch, receive, and consume each emit a real OpenTelemetry span, with the correlation id propagating across every service boundary. The same trace opens in Nitro once configured."
          >
            <div className="border-cc-card-border bg-cc-card-bg/40 rounded border p-4">
              <div className="border-cc-card-border mb-3 flex items-center justify-between border-b pb-2">
                <h3 className="font-heading text-h6 text-cc-heading">
                  trace · review.created
                </h3>
                <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
                  5 spans
                </span>
              </div>
              <div className="space-y-2">
                <TraceSpan label="POST /reviews" widthPct={100} offsetPct={0} />
                <TraceSpan label="CreateReview" widthPct={10} offsetPct={2} />
                <TraceSpan label="outbox.commit" widthPct={14} offsetPct={12} />
                <TraceSpan
                  label="publish→rabbitmq"
                  widthPct={34}
                  offsetPct={26}
                  live
                />
                <TraceSpan label="SearchIndexer" widthPct={32} offsetPct={62} />
              </div>
            </div>
            <ul className="mt-5 space-y-2">
              {[
                "Correlation id carried across services",
                "Spans for dispatch, transport, and handler",
                "Zero overhead when the observer is off",
              ].map((t) => (
                <li
                  key={t}
                  className="text-cc-ink-dim flex items-center gap-2 text-sm"
                >
                  <span style={{ color: CORAL }}>
                    <CheckIcon />
                  </span>
                  {t}
                </li>
              ))}
            </ul>
          </Section>

          {/* ============================  §07 Reference  ================= */}
          <Section
            id="reference"
            ordinal="§07"
            title="Reference card."
            deck="The vocabulary that recurs through the rest of the docs, gathered in one place so the reading order does not depend on remembering which page introduced what."
          >
            <h3 className="font-heading text-h4 text-cc-heading mb-4">Terms</h3>
            <dl className="border-cc-card-border divide-cc-card-border divide-y border-y">
              {[
                {
                  term: "Handler",
                  def: "A method attributed [Handler] that consumes a command, query, or event.",
                },
                {
                  term: "ISender",
                  def: "The in-process dispatcher that routes a command or query to its handler.",
                },
                {
                  term: "IMessageBus",
                  def: "The cross-service publisher that hands an event to a transport.",
                },
                {
                  term: "Saga",
                  def: "A long-running state machine that advances as messages arrive, validated for reachability.",
                },
                {
                  term: "Outbox",
                  def: "A table that stores outgoing messages inside your DB transaction, drained to the transport.",
                },
                {
                  term: "Inbox",
                  def: "A table that records processed message ids on receive, so redeliveries do not re-apply.",
                },
                {
                  term: "Transport",
                  def: "The underlying broker or stream that moves messages between services.",
                },
              ].map((row) => (
                <div
                  key={row.term}
                  className="grid grid-cols-[140px_minmax(0,1fr)] gap-4 py-3"
                >
                  <dt className="text-cc-heading font-mono text-[11px] tracking-[0.18em] uppercase">
                    {row.term}
                  </dt>
                  <dd className="text-cc-ink-dim text-sm">{row.def}</dd>
                </div>
              ))}
            </dl>
          </Section>

          {/* ============================  Colophon  ==================== */}
          <section className="border-cc-card-border mt-4 border-t pt-10 pb-4">
            <div className="flex flex-wrap items-center gap-4">
              <span className="border-cc-card-border bg-cc-card-bg/40 text-cc-ink rounded border px-2.5 py-1 font-mono text-[11px] tracking-[0.18em] uppercase">
                mocha / v3.x
              </span>
              <div className="ml-auto flex flex-wrap gap-3">
                <SolidButton href="/get-started">Start for Free</SolidButton>
                <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
              </div>
            </div>
            <div
              aria-hidden
              className="mx-auto mt-10 h-px w-32"
              style={{ backgroundColor: CORAL }}
            />
          </section>
        </main>
      </div>
    </div>
  );
}
