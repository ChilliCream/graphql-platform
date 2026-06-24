import type { Metadata } from "next";
import Link from "next/link";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Event-Driven Workflows for .NET",
  description:
    "Event-driven workflows for .NET with Mocha: in-process CQRS and a cross-service bus, source-generated dispatch, outbox and inbox, sagas, every hop traced.",
  keywords: [
    "event-driven workflows .NET",
    "in-process mediator and message bus",
    "source-generated mediator",
    "CQRS and messaging .NET",
    "transactional outbox idempotent inbox",
    "saga orchestration .NET",
    "OpenTelemetry message tracing",
    "pluggable transports RabbitMQ Postgres",
    "pub/sub request/reply fire-and-forget",
    "MediatR MassTransit alternative",
  ],
  openGraph: {
    title: "Event-Driven Workflows for .NET with Mocha",
    description:
      "One .NET framework for in-process CQRS and cross-service messaging: source-generated dispatch, outbox and inbox, sagas, and every hop traced as a span in Nitro.",
  },
};

/**
 * Per-stage accent, sourced from the multi-color step palette in globals.css
 * (--cc-step-1..4). Each stage of the flow gets a distinct hue so the four
 * stages read as an ordered, color-coded sequence down the spine.
 */
interface Stage {
  readonly n: number;
  readonly dot: string;
  readonly text: string;
  readonly label: string;
  readonly title: string;
  readonly detail: string;
  /** The hop drawn as a Nitro span bar: offset + width as percentages. */
  readonly span: { readonly left: number; readonly width: number };
  readonly spanLabel: string;
}

const STAGES: readonly Stage[] = [
  {
    n: 1,
    dot: "var(--cc-step-1)",
    text: "var(--cc-step-1-text)",
    label: "command dispatched",
    title: "An action becomes a command.",
    detail:
      "A CreateReview command is dispatched through the in-process mediator. Commands, queries, and notifications are plain typed messages, so the request can return while the work it started keeps moving.",
    span: { left: 0, width: 22 },
    spanLabel: "dispatch",
  },
  {
    n: 2,
    dot: "var(--cc-step-2)",
    text: "var(--cc-step-2-text)",
    label: "handler runs",
    title: "A handler picks it up.",
    detail:
      "A Roslyn source generator discovers the matching handler at compile time and emits typed registration plus a pre-compiled pipeline. Dispatch is zero-reflection and AOT-friendly, with retry, circuit breaker, and concurrency limits sitting in the pipeline as middleware.",
    span: { left: 18, width: 30 },
    spanLabel: "handle",
  },
  {
    n: 3,
    dot: "var(--cc-step-3)",
    text: "var(--cc-step-3-text)",
    label: "event published",
    title: "An event goes onto the bus.",
    detail:
      "The handler publishes ReviewCreated. The same model spans in-process notifications and the cross-service bus, so other services subscribe without coupling. A transactional outbox makes the database write and the dispatch succeed or fail together, and an idempotent inbox deduplicates for effectively exactly-once processing.",
    span: { left: 42, width: 26 },
    spanLabel: "publish",
  },
  {
    n: 4,
    dot: "var(--cc-step-4)",
    text: "var(--cc-step-4-text)",
    label: "saga advances",
    title: "A saga carries the workflow.",
    detail:
      "A saga is a C# state machine that advances across services as events arrive. Mocha validates that every state is reachable and every path reaches a final state before the service handles traffic, so a workflow cannot silently get stuck on the way to production.",
    span: { left: 60, width: 38 },
    spanLabel: "advance",
  },
];

const SAGA_STATES = [
  { name: "Draft", state: "done" },
  { name: "Checked", state: "active" },
  { name: "Published", state: "next" },
] as const;

/** Small mono chip used in the diagrams and the detail strip. */
function Chip({
  children,
  accent = false,
}: {
  readonly children: React.ReactNode;
  readonly accent?: boolean;
}) {
  return (
    <span
      className={[
        "rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap",
        accent
          ? "border-cc-accent/60 text-cc-accent bg-cc-surface"
          : "border-cc-card-border text-cc-ink bg-cc-surface",
      ].join(" ")}
    >
      {children}
    </span>
  );
}

/** The thin Nitro span bar that rides under every hop on the spine. */
function SpanBar({
  left,
  width,
  label,
}: {
  readonly left: number;
  readonly width: number;
  readonly label: string;
}) {
  return (
    <div className="mt-4 flex items-center gap-2">
      <span className="text-cc-ink-dim w-14 shrink-0 text-right font-mono text-[0.55rem]">
        {label}
      </span>
      <span
        className="bg-cc-surface border-cc-card-border relative h-2 flex-1 overflow-hidden rounded-full border"
        aria-hidden="true"
      >
        <span
          className="bg-cc-accent absolute top-0 h-full rounded-full opacity-70"
          style={{ left: `${left}%`, width: `${width}%` }}
        />
      </span>
    </div>
  );
}

/** One node on the spine: a numbered, color-coded dot plus the stage copy. */
function TimelineNode({
  stage,
  children,
}: {
  readonly stage: Stage;
  readonly children?: React.ReactNode;
}) {
  return (
    <li className="relative pb-12 last:pb-0">
      {/* Numbered dot, bisected by the spine (the ol's left border). */}
      <span
        className="bg-cc-bg absolute top-0 -left-[calc(2rem+1.5px)] flex size-11 -translate-x-1/2 items-center justify-center rounded-full border font-mono text-sm font-semibold sm:size-12"
        style={{ borderColor: stage.dot, color: stage.text }}
        aria-hidden="true"
      >
        {stage.n}
      </span>

      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm sm:p-6">
        <span
          className="font-mono text-[0.6rem] tracking-[0.15em] uppercase"
          style={{ color: stage.text }}
        >
          {stage.label}
        </span>
        <h3 className="font-heading text-cc-heading text-h5 sm:text-h4 mt-2 leading-tight font-semibold text-balance">
          {stage.title}
        </h3>
        <p className="text-cc-ink mt-3 text-base/relaxed text-pretty">
          {stage.detail}
        </p>

        <SpanBar
          left={stage.span.left}
          width={stage.span.width}
          label={stage.spanLabel}
        />
        <p className="text-cc-nav-label mt-2 text-right font-mono text-[0.55rem] tracking-[0.08em] uppercase">
          every hop a span in Nitro
        </p>

        {children}
      </div>
    </li>
  );
}

/** Saga state chain hung off the final node: Draft -> Checked -> Published. */
function SagaSubFlow() {
  return (
    <div className="border-cc-card-border mt-5 border-t pt-4">
      <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.15em] uppercase">
        saga state
      </p>
      <div className="mt-3 flex flex-wrap items-center gap-1.5">
        {SAGA_STATES.map((s, i) => (
          <span key={s.name} className="flex items-center gap-1.5">
            {i > 0 && (
              <span aria-hidden="true" className="text-cc-ink-faint text-sm">
                &rarr;
              </span>
            )}
            <span
              className={[
                "rounded-md border px-2.5 py-1 font-mono text-[0.6rem]",
                s.state === "active"
                  ? "border-cc-accent/60 text-cc-accent"
                  : s.state === "done"
                    ? "border-cc-card-border text-cc-ink-dim"
                    : "border-cc-ink-faint text-cc-ink-dim border-dashed",
              ].join(" ")}
            >
              {s.name}
            </span>
          </span>
        ))}
      </div>
      <p className="text-cc-ink-dim mt-3 text-sm">
        Every path reaches a final state, validated before the service handles
        traffic.
      </p>
    </div>
  );
}

/** Three messaging patterns, each answering one question. */
function PatternTriad() {
  const rows = [
    {
      pattern: "Event",
      question: "who needs to know?",
      method: "PublishAsync",
    },
    { pattern: "Send", question: "who should act?", method: "SendAsync" },
    {
      pattern: "Request-Reply",
      question: "what is the result?",
      method: "RequestAsync",
    },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6 backdrop-blur-sm sm:p-8">
      <h3 className="font-heading text-cc-heading text-h5 font-semibold">
        Three patterns, one model
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm">
        The same handler-first API covers pub/sub, fire-and-forget, and
        request/reply, used independently or together.
      </p>
      <div className="mt-5 space-y-2">
        {rows.map((row) => (
          <div
            key={row.pattern}
            className="border-cc-card-border bg-cc-surface flex flex-wrap items-center gap-x-3 gap-y-1.5 rounded-lg border px-3 py-2.5"
          >
            <span className="text-cc-heading w-28 shrink-0 text-sm font-medium">
              {row.pattern}
            </span>
            <span className="text-cc-ink-dim flex-1 text-sm">
              {row.question}
            </span>
            <Chip accent>{row.method}</Chip>
          </div>
        ))}
      </div>
    </div>
  );
}

/** Reliability beat: outbox + inbox, the honesty about "effectively". */
function ReliabilityPanel() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6 backdrop-blur-sm sm:p-8">
      <h3 className="font-heading text-cc-heading text-h5 font-semibold">
        Reliability built in
      </h3>
      <div className="mt-5 grid gap-3">
        <div className="border-cc-card-border bg-cc-surface rounded-lg border px-4 py-3">
          <p className="text-cc-heading text-sm font-medium">
            Transactional outbox
          </p>
          <p className="text-cc-ink-dim mt-1 text-sm">
            The database write and the message dispatch succeed or fail
            together.
          </p>
        </div>
        <div className="border-cc-card-border bg-cc-surface rounded-lg border px-4 py-3">
          <p className="text-cc-heading text-sm font-medium">
            Idempotent inbox
          </p>
          <p className="text-cc-ink-dim mt-1 text-sm">
            Duplicate messages are deduplicated so each is processed once.
          </p>
        </div>
      </div>
      <p className="text-cc-ink mt-4 text-sm/relaxed">
        Together they give{" "}
        <span className="text-cc-accent font-medium">
          effectively exactly-once
        </span>{" "}
        processing. Per-exception retry and redelivery, dead-letter routing,
        circuit breaker, concurrency limiter, and scheduled or delayed delivery
        all ship as pipeline middleware.
      </p>
    </div>
  );
}

export default function WorkflowsPage() {
  const transports = ["RabbitMQ", "Postgres", "in-process"];
  const patterns = [
    "transactional outbox",
    "idempotent inbox",
    "mediator AND bus in one",
  ];

  return (
    <>
      {/* Hero: top entry point of the spine. */}
      <section className="pt-16 pb-10 text-center sm:pt-24">
        <p className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
          Event-driven workflows for .NET
        </p>
        <h1 className="font-heading text-cc-heading mx-auto mt-4 max-w-3xl text-5xl leading-tight font-semibold tracking-tight text-balance sm:text-6xl">
          Let work continue after the request.
        </h1>
        <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
          Mocha turns backend behavior into commands, events, handlers, and
          sagas, then keeps every publish, dispatch, receive, and consume a real
          span you can follow in Nitro. Event-driven work for .NET that stays
          visible end to end.
        </p>
        <div className="mt-9 flex flex-wrap justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
        </div>
      </section>

      {/* The spine: four ordered, color-coded nodes descending in time. */}
      <section className="py-8">
        <h2 className="sr-only">How a workflow moves through Mocha</h2>
        <ol className="border-cc-card-border relative mx-auto max-w-2xl border-l pl-8">
          {STAGES.map((stage) => (
            <TimelineNode key={stage.n} stage={stage}>
              {stage.n === 4 ? <SagaSubFlow /> : null}
            </TimelineNode>
          ))}
        </ol>
      </section>

      {/* Differentiated value: patterns + reliability side by side. */}
      <section className="py-12">
        <div className="mx-auto mb-10 max-w-2xl text-center">
          <h2 className="font-heading text-cc-heading text-h3 font-semibold tracking-tight text-balance">
            One framework for in-process and cross-service work.
          </h2>
          <p className="text-cc-ink-dim mt-4 text-base sm:text-lg">
            A source-generated, zero-reflection in-process mediator for CQRS and
            a cross-service message bus, in a single typed model. It covers both
            jobs without leaving you to stitch two libraries together.
          </p>
        </div>
        <div className="grid gap-6 lg:grid-cols-2">
          <PatternTriad />
          <ReliabilityPanel />
        </div>
      </section>

      {/* Honesty / credibility beat. */}
      <section className="py-12">
        <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto max-w-3xl rounded-2xl border p-7 backdrop-blur-sm sm:p-9">
          <h2 className="font-heading text-cc-heading text-h4 font-semibold">
            Decoupled, but not a black box.
          </h2>
          <p className="text-cc-ink mt-4 text-base/relaxed text-pretty">
            Most messaging frameworks hand you decoupling and then lose the
            thread: the request returns, work fans out, and you cannot say where
            it went. Mocha is OpenTelemetry-native. Every dispatch, receive, and
            handler execution emits structured traces and metrics, correlation
            IDs propagate across service boundaries, and you can follow a
            message from publish to consume as real spans, with a visual
            topology, once telemetry is configured to flow into Nitro.
          </p>
          <ul className="mt-6 space-y-2.5">
            {[
              "Sagas are validated before the service handles traffic, not as a compile-time check, so a stuck workflow does not reach production.",
              "Pluggable transports: lead with RabbitMQ, Postgres, and in-process, and swap them without touching your handlers.",
              "Handler-first and explicit. You declare IEventHandler<T>, ICommand<T>, and IQuery<TResponse>; Mocha builds the endpoints, pipelines, and consumers around them.",
            ].map((item) => (
              <li key={item} className="flex items-start gap-3">
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon />
                </span>
                <span className="text-cc-ink-dim text-sm/relaxed">{item}</span>
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* Transports + patterns detail strip. */}
      <section className="py-10">
        <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto max-w-3xl rounded-2xl border p-6 text-center backdrop-blur-sm sm:p-8">
          <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.15em] uppercase">
            transports and patterns
          </p>
          <div className="mt-4 flex flex-wrap items-center justify-center gap-2">
            {transports.map((t) => (
              <Chip key={t} accent>
                {t}
              </Chip>
            ))}
            {patterns.map((p) => (
              <Chip key={p}>{p}</Chip>
            ))}
          </div>
          <p className="text-cc-ink-dim mt-5 text-sm">
            Open and modern, built on the Enterprise Integration Patterns
            catalog. Kafka, Azure Service Bus, and Event Hub also exist in
            source.{" "}
            <Link
              href="/docs/mocha"
              className="text-cc-accent hover:text-cc-accent-hover font-medium"
            >
              Learn more about Mocha
            </Link>
            .
          </p>
        </div>
      </section>

      {/* CTA. */}
      <section className="py-16 text-center">
        <h2 className="font-heading text-cc-heading text-h3 font-semibold tracking-tight text-balance">
          Keep the workflow moving without losing the thread.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          Ship event-driven work that continues after the request and stays
          visible end to end. See where every message went in Nitro, and explore
          how Mocha fits the wider ChilliCream platform.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-4">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/platform/analytics">
            See it in Nitro
          </OutlineButton>
        </div>
        <p className="text-cc-ink-faint mt-6 text-sm">
          <Link
            href="/platform"
            className="text-cc-ink-dim hover:text-cc-accent"
          >
            Back to the platform
          </Link>
        </p>
      </section>
    </>
  );
}
