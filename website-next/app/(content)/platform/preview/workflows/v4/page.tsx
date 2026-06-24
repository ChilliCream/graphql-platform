import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Mocha: Longform Dispatch for event-driven workflows .NET",
  description:
    "Mocha is a source-generated .NET mediator and cross-service message bus with validated sagas and outbox plus inbox processing for event-driven workflows.",
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
      "Hand the slow work to a message and let it keep moving after the response goes out. One source-generated framework for in-process CQRS and cross-service messaging.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/*  Coral accent. One column. One vertical thread running the page.    */
/* ------------------------------------------------------------------ */

const CORAL = "#f0786a";
const CORAL_SOFT = "rgba(240,120,106,0.55)";
const CORAL_WASH = "rgba(240,120,106,0.10)";

/* ============================  Thread primitives  ==================== */

function ThreadDot() {
  return (
    <div
      className="relative mx-auto h-3 w-3 rounded-full"
      aria-hidden
      style={{
        backgroundColor: CORAL,
        boxShadow:
          "0 0 0 4px rgba(240,120,106,0.18), 0 0 18px rgba(240,120,106,0.45)",
      }}
    />
  );
}

interface ThreadBoundaryProps {
  readonly children: React.ReactNode;
}

function ThreadBoundary({ children }: ThreadBoundaryProps) {
  return (
    <div className="relative">
      {/* Dot anchoring this section onto the page thread */}
      <div className="pointer-events-none absolute top-0 left-1/2 -translate-x-1/2">
        <ThreadDot />
      </div>
      <div className="pt-10">{children}</div>
    </div>
  );
}

interface SectionProps {
  readonly children: React.ReactNode;
  readonly first?: boolean;
}

function Section({ children, first }: SectionProps) {
  return (
    <section className={first ? "pt-8 pb-24" : "py-24 sm:py-32"}>
      <ThreadBoundary>{children}</ThreadBoundary>
    </section>
  );
}

interface EyebrowProps {
  readonly children: React.ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p
      className="mb-5 font-mono text-[11px] tracking-[0.2em] uppercase"
      style={{ color: CORAL }}
    >
      {children}
    </p>
  );
}

interface FigureStubProps {
  readonly children: React.ReactNode;
}

function FigureStub({ children }: FigureStubProps) {
  // A short stub on the left edge connecting the inline figure back to the
  // page-center thread, so the figure reads as a node on the trace.
  return (
    <figure className="relative mx-auto mt-12 max-w-3xl">
      <div
        className="pointer-events-none absolute top-1/2 -left-6 hidden h-px w-6 -translate-y-1/2 md:block"
        aria-hidden
        style={{
          background: `linear-gradient(90deg, transparent, ${CORAL_SOFT})`,
        }}
      />
      {children}
    </figure>
  );
}

interface FigureCaptionProps {
  readonly children: React.ReactNode;
}

function FigureCaption({ children }: FigureCaptionProps) {
  return (
    <figcaption className="text-cc-nav-label text-caption mt-3 text-center font-mono">
      {children}
    </figcaption>
  );
}

/* ============================  Inline code card  ===================== */

function DispatchCodeCard() {
  return (
    <div
      className="bg-cc-code-bg overflow-hidden rounded-xl border"
      style={{ borderColor: "color-mix(in srgb, #f0786a 38%, transparent)" }}
    >
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2">
        <span className="text-cc-nav-label font-mono text-[11px]">
          ReviewHandlers.cs
        </span>
        <span className="font-mono text-[10px]" style={{ color: CORAL }}>
          one definition
        </span>
      </div>
      <pre className="overflow-x-auto px-5 py-4 font-mono text-[12px] leading-relaxed">
        <code>
          <span className="text-cc-nav-label">{"[Handler]"}</span>
          {"\n"}
          <span className="text-cc-ink-dim">public async </span>
          <span style={{ color: CORAL }}>Task</span>
          <span className="text-cc-ink-dim"> Handle(</span>
          {"\n"}
          {"  "}
          <span className="text-cc-heading">CreateReview</span>
          <span className="text-cc-ink-dim"> command,</span>
          {"\n"}
          {"  "}
          <span className="text-cc-heading">IMessageBus</span>
          <span className="text-cc-ink-dim"> bus)</span>
          {"\n"}
          <span className="text-cc-ink-dim">{"{"}</span>
          {"\n"}
          {"  "}
          <span className="text-cc-ink-dim">var review = </span>
          <span className="text-cc-heading">Review</span>
          <span className="text-cc-ink-dim">.Draft(command);</span>
          {"\n\n"}
          {"  "}
          <span className="text-cc-nav-label">
            {"// committed with the DB write, sent once"}
          </span>
          {"\n"}
          {"  "}
          <span className="text-cc-ink-dim">await bus.</span>
          <span style={{ color: CORAL }}>PublishAsync</span>
          <span className="text-cc-ink-dim">(</span>
          {"\n"}
          {"    "}
          <span className="text-cc-ink-dim">new </span>
          <span className="text-cc-heading">ReviewCreated</span>
          <span className="text-cc-ink-dim">(review.Id));</span>
          {"\n"}
          <span className="text-cc-ink-dim">{"}"}</span>
        </code>
      </pre>
    </div>
  );
}

/* ============================  Saga rail  ============================ */

interface SagaPillProps {
  readonly label: string;
  readonly state: "done" | "live" | "pending";
}

function SagaPill({ label, state }: SagaPillProps) {
  const isLive = state === "live";
  const isDone = state === "done";
  const dot = isLive
    ? CORAL
    : isDone
      ? "var(--color-cc-accent)"
      : "rgba(245,241,234,0.3)";
  const border = isLive
    ? "color-mix(in srgb, #f0786a 55%, transparent)"
    : isDone
      ? "color-mix(in srgb, #5eead4 45%, transparent)"
      : "var(--color-cc-card-border)";
  const bg = isLive
    ? CORAL_WASH
    : isDone
      ? "color-mix(in srgb, #5eead4 8%, transparent)"
      : "transparent";
  return (
    <div
      className="flex items-center gap-2 rounded-full border px-4 py-2"
      style={{ borderColor: border, backgroundColor: bg }}
    >
      <span
        className="size-2 rounded-full"
        style={{
          backgroundColor: dot,
          boxShadow: isLive ? "0 0 0 4px rgba(240,120,106,0.18)" : "none",
        }}
      />
      <span className="text-cc-heading font-mono text-[12px]">{label}</span>
    </div>
  );
}

interface SagaConnectorProps {
  readonly state: "done" | "pending";
}

function SagaConnector({ state }: SagaConnectorProps) {
  const done = state === "done";
  return (
    <svg
      viewBox="0 0 56 12"
      className="h-3 w-10 shrink-0 sm:w-14"
      aria-hidden
      preserveAspectRatio="none"
    >
      <line
        x1="0"
        y1="6"
        x2="48"
        y2="6"
        stroke={done ? CORAL_SOFT : "rgba(245,241,234,0.2)"}
        strokeWidth="1.5"
        strokeDasharray={done ? "0" : "4 3"}
      />
      <polygon
        points="47,2 56,6 47,10"
        fill={done ? CORAL_SOFT : "rgba(245,241,234,0.3)"}
      />
    </svg>
  );
}

function SagaRail() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border px-5 py-6 backdrop-blur-sm sm:px-8">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
          ReviewSaga
        </span>
        <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[11px]">
          <span style={{ color: CORAL }}>
            <CheckIcon size={12} />
          </span>
          <span style={{ color: CORAL }}>all paths terminal</span>
        </span>
      </div>
      <div className="flex flex-wrap items-center justify-center gap-y-3">
        <SagaPill label="Draft" state="done" />
        <SagaConnector state="done" />
        <SagaPill label="Checked" state="live" />
        <SagaConnector state="pending" />
        <SagaPill label="Published" state="pending" />
      </div>
    </div>
  );
}

/* ============================  Transport chips strip  ================ */

interface TransportChipProps {
  readonly name: string;
  readonly highlight?: boolean;
}

function TransportChip({ name, highlight }: TransportChipProps) {
  return (
    <span
      className="bg-cc-surface/60 text-cc-heading inline-flex items-center rounded-full border px-3.5 py-1.5 font-mono text-[12px]"
      style={{
        borderColor: highlight
          ? "color-mix(in srgb, #f0786a 50%, transparent)"
          : "var(--color-cc-card-border)",
        boxShadow: highlight ? "0 0 18px rgba(240,120,106,0.18)" : undefined,
      }}
    >
      {name}
    </span>
  );
}

function TransportStrip() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border px-5 py-6 backdrop-blur-sm sm:px-8">
      <div className="flex flex-wrap items-center justify-center gap-2.5">
        <TransportChip name="RabbitMQ" highlight />
        <TransportChip name="Postgres" />
        <TransportChip name="Kafka" />
        <TransportChip name="Azure Service Bus" />
        <TransportChip name="in-process" />
      </div>
    </div>
  );
}

/* ============================  Trace ribbon  ========================= */

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
      <div className="bg-cc-surface/50 relative h-5 flex-1 rounded">
        <div
          className="absolute top-0 h-5 rounded"
          style={{
            left: `${offsetPct}%`,
            width: `${widthPct}%`,
            background: live
              ? `linear-gradient(90deg, ${CORAL}, rgba(240,120,106,0.6))`
              : "rgba(94,234,212,0.32)",
            boxShadow: live ? "0 0 14px rgba(240,120,106,0.4)" : "none",
          }}
        />
      </div>
    </div>
  );
}

function TraceRibbon() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5 backdrop-blur-sm sm:p-6">
      <div className="border-cc-card-border mb-4 flex items-center justify-between border-b pb-3">
        <span className="text-cc-heading font-mono text-[11px]">
          trace · review.created
        </span>
        <span className="text-cc-nav-label font-mono text-[10px]">
          42.6ms · 5 spans
        </span>
      </div>
      <div className="space-y-2.5">
        <TraceSpan label="POST /reviews" widthPct={100} offsetPct={0} />
        <TraceSpan label="CreateReview" widthPct={9} offsetPct={2} />
        <TraceSpan label="outbox.commit" widthPct={14} offsetPct={11} />
        <TraceSpan label="publish→rabbitmq" widthPct={34} offsetPct={26} live />
        <TraceSpan label="SearchIndexer" widthPct={32} offsetPct={62} />
      </div>
    </div>
  );
}

/* ============================  Inline prose helpers  ================= */

interface MonoProps {
  readonly children: React.ReactNode;
}

function Mono({ children }: MonoProps) {
  return <span className="text-cc-ink font-mono">{children}</span>;
}

/* ==============================  Page  =============================== */

export default function WorkflowsPreviewV4() {
  return (
    <div className="relative">
      {/* The page-spanning vertical thread. A 1px coral-to-transparent gradient
          bar sits at the horizontal center of the column, turning the whole
          page into one continuous trace of the message's life. */}
      <div
        className="pointer-events-none absolute top-0 bottom-0 left-1/2 w-px -translate-x-1/2"
        aria-hidden
        style={{
          background:
            "linear-gradient(to bottom, transparent 0%, rgba(240,120,106,0.55) 6%, rgba(240,120,106,0.35) 50%, rgba(240,120,106,0.55) 94%, transparent 100%)",
        }}
      />

      <div className="relative mx-auto max-w-2xl px-4">
        {/* ----------------------- HERO OPENER ---------------------- */}
        <Section first>
          <div className="text-center">
            <Eyebrow>Mocha · longform dispatch</Eyebrow>
            <h1 className="font-heading text-cc-heading text-hero tracking-tight">
              Let work continue after the request.
            </h1>
            <p className="lead text-cc-ink-dim mx-auto mt-8 max-w-2xl">
              Return the response the instant the user needs it. Hand the slow,
              fan-out, cross-service work to a message and let it keep moving on
              its own.
            </p>
            <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
            </div>
          </div>
        </Section>

        {/* ----------------------- PREMISE ------------------------- */}
        <Section>
          <Eyebrow>the premise</Eyebrow>
          <h2 className="font-heading text-h2 text-cc-heading">
            You return 201. The work isn&apos;t done.
          </h2>
          <p className="lead text-cc-ink-dim mt-6">
            A user hits <Mono>POST /reviews</Mono> and waits for one thing: a
            confirmation. Everything else, the search index, the notification
            fan-out, the score recalculation, can keep moving long after the
            response goes out.
          </p>
          <p className="text-cc-prose text-body mt-6">
            That asynchronous-after-response shape is where most .NET services
            quietly accumulate plumbing. A broker connection here, a retry loop
            there, a half-finished outbox wired into a transaction nobody wants
            to touch again. Mocha collapses that drift into one .NET
            event-driven model you actually write down.
          </p>
        </Section>

        {/* --------------- DISPATCH COUPLET (figure 1) ------------- */}
        <Section>
          <Eyebrow>the dispatch couplet</Eyebrow>
          <h2 className="font-heading text-h2 text-cc-heading">
            One handler. One publish.
          </h2>
          <p className="lead text-cc-ink-dim mt-6">
            Mocha is a source-generated mediator and message bus. The handler
            you would have written by hand and the publish you would have
            wrapped in a service become a single definition the generator wires
            together.
          </p>
          <p className="text-cc-prose text-body mt-6">
            No reflection on the hot path, no service-locator lookup, no
            broker-specific glue inside the handler. The tangle of connection
            setup, dedup tables, retries and dead-letter routing moves out of
            your code and into the framework.
          </p>

          <FigureStub>
            <DispatchCodeCard />
            <FigureCaption>
              one <Mono>[Handler]</Mono>, one <Mono>PublishAsync</Mono>, the
              rest is generated
            </FigureCaption>
          </FigureStub>
        </Section>

        {/* ----------------- MEDIATOR / BUS DUALITY ---------------- */}
        <Section>
          <Eyebrow>two verbs, one model</Eyebrow>
          <h2 className="font-heading text-h2 text-cc-heading">
            In-process when it&apos;s near. On the bus when it&apos;s far.
          </h2>
          <p className="lead text-cc-ink-dim mt-6">
            Inside one process, the mediator dispatches commands and queries
            straight to a <Mono>[Handler]</Mono> through a pre-compiled
            pipeline. You call <Mono>ISender</Mono>, the handler answers, and
            the response is on its way back before the request thread has
            unwound.
          </p>
          <p className="text-cc-prose text-body mt-6">
            When the work belongs to another service, the same publish crosses a
            transport and fans out to its consumers. You change the verb from a
            send to a <Mono>PublishAsync</Mono>, not the mental model: the
            message, the handler, and the trace look the same on both sides of
            the boundary.
          </p>
        </Section>

        {/* ------------------ SAGA INTERLUDE ----------------------- */}
        <Section>
          <Eyebrow>sagas · stateful workflows</Eyebrow>
          <h2 className="font-heading text-h2 text-cc-heading">
            A workflow that can&apos;t get stuck.
          </h2>
          <p className="lead text-cc-ink-dim mt-6">
            A review moves <Mono>Draft → Checked → Published</Mono> across
            several messages and minutes. You define that state machine once.
            Mocha then checks that every state is reachable and every path
            reaches a final state, so a saga that can dead-end never makes it
            into production.
          </p>

          <FigureStub>
            <SagaRail />
            <FigureCaption>validated before traffic</FigureCaption>
          </FigureStub>
        </Section>

        {/* ----------------- RELIABILITY + TRANSPORTS -------------- */}
        <Section>
          <Eyebrow>reliability</Eyebrow>
          <h2 className="font-heading text-h2 text-cc-heading">
            Outbox plus inbox, no surprises.
          </h2>
          <p className="lead text-cc-ink-dim mt-6">
            The outbox commits the outgoing message together with your database
            write, so a published event never goes out without the row that
            justifies it. On the receiving side, the inbox deduplicates on the
            message id, so a redelivery never runs a handler twice. Together
            they give exactly-once processing.
          </p>
          <p className="text-cc-prose text-body mt-6">
            The transport itself is a registration detail. Start in-process, run
            on Postgres in production, route streams through Kafka, or sit on
            top of the broker your operations team already runs.
          </p>

          <FigureStub>
            <TransportStrip />
            <FigureCaption>swap the broker, keep the handlers</FigureCaption>
          </FigureStub>
        </Section>

        {/* --------------- OBSERVABILITY TRACE RIBBON -------------- */}
        <Section>
          <Eyebrow>observability · every hop a span</Eyebrow>
          <h2 className="font-heading text-h2 text-cc-heading">
            Follow one message, hop by hop.
          </h2>
          <p className="lead text-cc-ink-dim mt-6">
            Publish, dispatch, receive, and consume each emit a real
            OpenTelemetry span, with the correlation id propagating across every
            service boundary. The same trace opens in Nitro when you wire up
            telemetry, so you can watch the in-flight message advance the way
            you reason about it.
          </p>

          <FigureStub>
            <TraceRibbon />
            <FigureCaption>
              the coral span is the hop in flight right now
            </FigureCaption>
          </FigureStub>
        </Section>

        {/* ----------------------- HONESTY BEAT -------------------- */}
        <Section>
          <Eyebrow>what we mean precisely</Eyebrow>
          <h2 className="font-heading text-h2 text-cc-heading">
            Reliability claims, stated honestly.
          </h2>
          <p className="text-cc-prose text-body mt-6">
            <strong className="text-cc-heading font-mono">
              Exactly-once processing.
            </strong>{" "}
            The outbox commits the message with your database write, and the
            inbox deduplicates on receive. That gives exactly-once processing,
            not exactly-once delivery, which no transport can promise.{" "}
            <strong className="text-cc-heading font-mono">
              Sagas validated before traffic.
            </strong>{" "}
            The state-machine check runs before the service handles traffic, not
            at compile time, and proves your saga can always reach a final
            state.{" "}
            <strong className="text-cc-heading font-mono">
              Published clients aren&apos;t surprised.
            </strong>{" "}
            Because dispatch is source-generated and contracts are explicit, a
            changed message shows up at build time, so you can see which
            published clients are affected before the change reaches them.
          </p>
        </Section>

        {/* --------------------------- CLOSE ----------------------- */}
        <section className="py-24 sm:py-32">
          <div className="relative">
            <div className="pointer-events-none absolute top-0 left-1/2 -translate-x-1/2">
              <ThreadDot />
            </div>
            <div className="pt-12 text-center">
              <div
                className="mx-auto mb-10 h-px w-48"
                aria-hidden
                style={{
                  background: `linear-gradient(90deg, transparent, ${CORAL}, transparent)`,
                }}
              />
              <h2 className="font-heading text-h3 text-cc-heading mx-auto max-w-2xl">
                Ship the response. Keep the work moving.
              </h2>
              <p className="lead text-cc-ink-dim mx-auto mt-6 max-w-2xl">
                One framework for the command you dispatch in-process and the
                event you publish across services, with reliability and traces
                built in.
              </p>
              <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
                <SolidButton href="/get-started">Start for Free</SolidButton>
                <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}
