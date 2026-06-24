import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Mocha as MochaIcon } from "@/src/icons/Mocha";
import { NitroTrace } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Mocha, Source-generated mediator and message bus for .NET",
  description:
    "Mocha is the open source, source-generated mediator and cross-service message bus for .NET. CQRS, sagas, outbox, RabbitMQ, Postgres, every hop a span.",
  keywords: [
    "Mocha",
    ".NET mediator",
    ".NET message bus",
    "CQRS",
    "saga orchestration",
    "transactional outbox",
    "idempotent inbox",
    "RabbitMQ .NET",
    "Postgres messaging",
    "source generator",
    "OpenTelemetry messaging",
    "ChilliCream",
  ],
  openGraph: {
    title: "Mocha, Source-generated mediator and message bus for .NET",
    description:
      "An open source, source-generated mediator and cross-service message bus for .NET. CQRS, sagas, outbox, pluggable transports, every hop a span in Nitro.",
  },
  robots: { index: false, follow: false },
};

// Brand spectrum used exactly once on the page, as the headline accent.
const SPECTRUM_GRADIENT =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

export default function MochaTopologyStoryPage() {
  return (
    <>
      <Hero />
      <TopologyArc />
      <TransportsBand />
      <MitBand />
      <ClosingCta />
    </>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="relative py-16 text-center sm:py-24">
      <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
        Source-generated mediator and message bus for .NET
      </div>

      <h1 className="text-cc-heading font-heading mx-auto max-w-4xl text-5xl leading-[1.05] font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        Let your services{" "}
        <span
          className="bg-clip-text text-transparent"
          style={{ backgroundImage: SPECTRUM_GRADIENT }}
        >
          react, not wait
        </span>
        .
      </h1>

      <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-base sm:text-lg">
        Mocha is one framework for in-process CQRS and cross-service messaging.
        A Roslyn source generator discovers your handlers and sagas at compile
        time and emits typed registration plus pre-compiled pipeline delegates,
        so dispatch is zero reflection on every hop.
      </p>

      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/mocha">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>

      <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 font-mono text-xs tracking-wide">
        <span className="inline-flex items-center gap-1.5">
          <span
            aria-hidden
            className="bg-cc-accent inline-block size-1.5 rounded-full"
          />
          MIT licensed
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span
            aria-hidden
            className="bg-cc-accent inline-block size-1.5 rounded-full"
          />
          Zero-reflection dispatch
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span
            aria-hidden
            className="bg-cc-accent inline-block size-1.5 rounded-full"
          />
          AOT friendly
        </span>
      </div>

      <div className="pointer-events-none mt-10 flex items-center justify-center">
        <MochaIcon className="text-cc-accent h-24 w-auto opacity-80" />
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Topology arc, 5 numbered steps                                            */
/* -------------------------------------------------------------------------- */

function TopologyArc() {
  return (
    <section className="py-16">
      <div className="mb-12 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          One message, five stations
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Follow a CreateReview command across the topology.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          Publish, handle, react, advance, trace. The same handler-first model
          covers in-process mediation and cross-service messaging, with the
          outbox and inbox making each hop reliable.
        </p>
      </div>

      <ol className="space-y-12">
        <Step
          index="01"
          title="Publish"
          tagline="The message takes flight."
          blurb="Hand a message to the bus. PublishAsync is one call for events, SendAsync for fire-and-forget, RequestAsync for typed request reply. The transactional outbox commits the database write and the message together, so nothing is lost between the two."
        >
          <CodeFrame language="C# / ReviewsApi.cs">
            <CsLine>
              <Kw>public</Kw> <Type>async Task</Type> SubmitAsync(
              <Type>CreateReview</Type> cmd, <Type>IMessageBus</Type> bus,
            </CsLine>
            <CsLine indent={2}>
              <Type>CancellationToken</Type> ct)
            </CsLine>
            <CsLine>{`{`}</CsLine>
            <CsLine indent={1}>
              <Kw>await</Kw> bus.<Method>PublishAsync</Method>(<Kw>new</Kw>{" "}
              <Type>ReviewCreated</Type>(cmd.OrderId, cmd.Stars), ct);
            </CsLine>
            <CsLine>{`}`}</CsLine>
            <CsLine />
            <CsLine>
              <Cmt>{`// Outbox + idempotent inbox = exactly-once processing.`}</Cmt>
            </CsLine>
          </CodeFrame>
          <FlowDiagram
            label="Publish ReviewCreated"
            stages={[
              { label: "ReviewsApi", tone: "ink" },
              { label: "Outbox", tone: "accent" },
              { label: "Transport", tone: "ink" },
            ]}
          />
        </Step>

        <Step
          index="02"
          title="Handle"
          tagline="The handler discovered at compile time runs."
          blurb="Implement IEventHandler<T> on a regular class with constructor injection. Mocha's Roslyn source generator scans your assembly at build time and emits typed registration plus pre-compiled pipeline delegates. No MakeGenericType, no service provider lookups to resolve the pipeline, no reflection at runtime."
        >
          <CodeFrame language="C# / ReviewCreatedHandler.cs">
            <CsLine>
              <Kw>public sealed class</Kw> <Type>ReviewCreatedHandler</Type>(
              <Type>ReviewStore</Type> store)
            </CsLine>
            <CsLine indent={1}>
              : <Type>IEventHandler</Type>&lt;<Type>ReviewCreated</Type>&gt;
            </CsLine>
            <CsLine>{`{`}</CsLine>
            <CsLine indent={1}>
              <Kw>public</Kw> <Type>ValueTask</Type>{" "}
              <Method>HandleAsync</Method>(
            </CsLine>
            <CsLine indent={2}>
              <Type>ReviewCreated</Type> evt, <Type>CancellationToken</Type> ct)
            </CsLine>
            <CsLine indent={2}>=&gt; store.AppendAsync(evt, ct);</CsLine>
            <CsLine>{`}`}</CsLine>
            <CsLine />
            <CsLine>
              <Cmt>{`// Program.cs`}</Cmt>
            </CsLine>
            <CsLine>
              builder.Services.AddMessageBus().AddReviewsModule();
            </CsLine>
          </CodeFrame>
          <p className="text-cc-ink-dim mt-3 text-sm">
            The AddReviewsModule() call is itself generated. The compiler sees
            every IEventHandler in your assembly and wires it without a runtime
            scan.
          </p>
        </Step>

        <Step
          index="03"
          title="React"
          tagline="Downstream consumers wake up."
          blurb="Other services subscribe to the same event. Notifications, billing, search indexing, each gets a copy through the transport you chose. Resilience controls (retry, dead-letter, circuit breaker, concurrency limiter) run as pipeline middleware around every consumer."
        >
          <CodeFrame language="C# / SendThanksHandler.cs">
            <CsLine>
              <Kw>public sealed class</Kw> <Type>SendThanksHandler</Type>(
              <Type>IEmailSender</Type> mail)
            </CsLine>
            <CsLine indent={1}>
              : <Type>IEventHandler</Type>&lt;<Type>ReviewCreated</Type>&gt;
            </CsLine>
            <CsLine>{`{`}</CsLine>
            <CsLine indent={1}>
              <Kw>public</Kw> <Type>ValueTask</Type>{" "}
              <Method>HandleAsync</Method>(
            </CsLine>
            <CsLine indent={2}>
              <Type>ReviewCreated</Type> evt, <Type>CancellationToken</Type> ct)
            </CsLine>
            <CsLine indent={2}>
              =&gt; mail.SendThanksAsync(evt.OrderId, ct);
            </CsLine>
            <CsLine>{`}`}</CsLine>
          </CodeFrame>
          <FanOutDiagram
            label="Fan out"
            source="ReviewCreated"
            targets={["Notifications", "Billing", "Search"]}
          />
        </Step>

        <Step
          index="04"
          title="Saga"
          tagline="Draft to Checked to Published, on its own."
          blurb="A saga owns a piece of long-running state. Define the states, the transitions, and the compensations; Mocha persists the state and routes messages to the right instance. Sagas are validated before the service handles traffic, so a saga that cannot reach a final state never goes live."
        >
          <CodeFrame language="C# / ReviewSaga.cs">
            <CsLine>
              <Kw>public sealed class</Kw> <Type>ReviewSaga</Type> :{" "}
              <Type>Saga</Type>&lt;<Type>ReviewSagaState</Type>&gt;
            </CsLine>
            <CsLine>{`{`}</CsLine>
            <CsLine indent={1}>
              <Kw>private const string</Kw> Checked = <Kw>nameof</Kw>(Checked);
            </CsLine>
            <CsLine indent={1}>
              <Kw>private const string</Kw> Published = <Kw>nameof</Kw>
              (Published);
            </CsLine>
            <CsLine />
            <CsLine indent={1}>
              <Kw>protected override void</Kw> <Method>Configure</Method>(
            </CsLine>
            <CsLine indent={2}>
              <Type>ISagaDescriptor</Type>&lt;<Type>ReviewSagaState</Type>&gt;
              descriptor)
            </CsLine>
            <CsLine indent={1}>{`{`}</CsLine>
            <CsLine indent={2}>descriptor.Initially()</CsLine>
            <CsLine indent={3}>
              .OnEvent&lt;<Type>ReviewCreated</Type>&gt;()
            </CsLine>
            <CsLine indent={3}>
              .StateFactory(<Type>ReviewSagaState</Type>.From)
            </CsLine>
            <CsLine indent={3}>.TransitionTo(Checked);</CsLine>
            <CsLine />
            <CsLine indent={2}>descriptor.During(Checked)</CsLine>
            <CsLine indent={3}>
              .OnEvent&lt;<Type>ModerationPassed</Type>&gt;()
            </CsLine>
            <CsLine indent={3}>.TransitionTo(Published);</CsLine>
            <CsLine />
            <CsLine indent={2}>descriptor.Finally(Published);</CsLine>
            <CsLine indent={1}>{`}`}</CsLine>
            <CsLine>{`}`}</CsLine>
          </CodeFrame>
          <SagaDiagram />
        </Step>

        <Step
          index="05"
          title="Trace"
          tagline="Every hop is a span in Nitro."
          blurb="Every dispatch, receive, and handler execution emits a structured OpenTelemetry span. Correlation propagates across service boundaries automatically, so a single message flow renders as one waterfall in Nitro from publish to consumer."
        >
          <div className="border-cc-card-border bg-cc-card-bg mx-auto max-w-5xl overflow-hidden rounded-xl border backdrop-blur-sm">
            <NitroTrace />
          </div>
          <p className="text-cc-ink-dim mt-3 text-center font-mono text-xs tracking-wide">
            Per-hop spans rendered live in Nitro, the platform&apos;s self-run
            control plane.
          </p>
        </Step>
      </ol>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Step layout                                                               */
/* -------------------------------------------------------------------------- */

interface StepProps {
  readonly index: string;
  readonly title: string;
  readonly tagline: string;
  readonly blurb: string;
  readonly children: ReactNode;
}

function Step({ index, title, tagline, blurb, children }: StepProps) {
  return (
    <li className="grid gap-6 lg:grid-cols-[16rem_1fr] lg:gap-10">
      <div className="lg:pt-2">
        <div className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
          Step {index}
        </div>
        <h3 className="text-cc-heading mt-2 text-2xl font-semibold tracking-tight">
          {title}
        </h3>
        <p className="text-cc-ink mt-2 text-sm font-medium">{tagline}</p>
        <p className="text-cc-ink-dim mt-3 text-sm sm:text-base">{blurb}</p>
      </div>
      <div>{children}</div>
    </li>
  );
}

/* -------------------------------------------------------------------------- */
/*  Inline flow diagram (svg-free, semantic markup)                           */
/* -------------------------------------------------------------------------- */

interface FlowStage {
  readonly label: string;
  readonly tone: "ink" | "accent";
}

interface FlowDiagramProps {
  readonly label: string;
  readonly stages: readonly FlowStage[];
}

function FlowDiagram({ label, stages }: FlowDiagramProps) {
  return (
    <figure className="border-cc-card-border bg-cc-surface/40 mt-4 overflow-hidden rounded-xl border p-5 backdrop-blur-sm">
      <figcaption className="text-cc-ink-dim mb-4 font-mono text-[11px] tracking-widest uppercase">
        {label}
      </figcaption>
      <div className="flex flex-wrap items-center gap-3">
        {stages.map((stage, i) => {
          const isLast = i === stages.length - 1;
          const toneClass =
            stage.tone === "accent"
              ? "border-cc-accent/60 text-cc-accent"
              : "border-cc-card-border text-cc-ink";
          return (
            <div key={stage.label} className="flex items-center gap-3">
              <span
                className={`inline-flex items-center rounded-full border px-3 py-1.5 font-mono text-xs ${toneClass}`}
              >
                {stage.label}
              </span>
              {!isLast && (
                <span aria-hidden className="text-cc-ink-dim font-mono text-sm">
                  {"→"}
                </span>
              )}
            </div>
          );
        })}
      </div>
    </figure>
  );
}

interface FanOutDiagramProps {
  readonly label: string;
  readonly source: string;
  readonly targets: readonly string[];
}

function FanOutDiagram({ label, source, targets }: FanOutDiagramProps) {
  return (
    <figure className="border-cc-card-border bg-cc-surface/40 mt-4 overflow-hidden rounded-xl border p-5 backdrop-blur-sm">
      <figcaption className="text-cc-ink-dim mb-4 font-mono text-[11px] tracking-widest uppercase">
        {label}
      </figcaption>
      <div className="flex items-stretch gap-4">
        <div className="flex items-center">
          <span className="border-cc-accent/60 text-cc-accent inline-flex items-center rounded-full border px-3 py-1.5 font-mono text-xs">
            {source}
          </span>
        </div>
        <svg
          aria-hidden
          viewBox="0 0 80 80"
          className="text-cc-ink-dim h-16 w-16 shrink-0"
          preserveAspectRatio="none"
        >
          <line
            x1="0"
            y1="40"
            x2="78"
            y2="10"
            stroke="currentColor"
            strokeWidth="1"
          />
          <line
            x1="0"
            y1="40"
            x2="78"
            y2="40"
            stroke="currentColor"
            strokeWidth="1"
          />
          <line
            x1="0"
            y1="40"
            x2="78"
            y2="70"
            stroke="currentColor"
            strokeWidth="1"
          />
        </svg>
        <ul className="flex flex-col justify-between gap-2">
          {targets.map((target) => (
            <li
              key={target}
              className="border-cc-card-border text-cc-ink inline-flex items-center rounded-full border px-3 py-1.5 font-mono text-xs"
            >
              {target}
            </li>
          ))}
        </ul>
      </div>
    </figure>
  );
}

/* -------------------------------------------------------------------------- */
/*  Saga diagram                                                              */
/* -------------------------------------------------------------------------- */

function SagaDiagram() {
  const states = ["Draft", "Checked", "Published"] as const;
  return (
    <figure className="border-cc-card-border bg-cc-surface/40 mt-4 overflow-hidden rounded-xl border p-5 backdrop-blur-sm">
      <figcaption className="text-cc-ink-dim mb-4 font-mono text-[11px] tracking-widest uppercase">
        ReviewSaga, validated before traffic
      </figcaption>
      <div className="flex flex-wrap items-center gap-3">
        {states.map((state, i) => {
          const isLast = i === states.length - 1;
          const isFinal = i === states.length - 1;
          return (
            <div key={state} className="flex items-center gap-3">
              <span
                className={`inline-flex items-center gap-2 rounded-full border px-3 py-1.5 font-mono text-xs ${
                  isFinal
                    ? "border-cc-success/60 text-cc-success"
                    : "border-cc-card-border text-cc-ink"
                }`}
              >
                <span
                  aria-hidden
                  className={`inline-block size-1.5 rounded-full ${
                    isFinal ? "bg-cc-success" : "bg-cc-accent"
                  }`}
                />
                {state}
              </span>
              {!isLast && (
                <span aria-hidden className="text-cc-ink-dim font-mono text-sm">
                  {"→"}
                </span>
              )}
            </div>
          );
        })}
      </div>
      <p className="text-cc-ink-dim mt-4 text-xs">
        Every state is reachable and every path lands in a final state. Mocha
        runs that check at startup, before the service accepts traffic.
      </p>
    </figure>
  );
}

/* -------------------------------------------------------------------------- */
/*  Code framing helpers                                                      */
/* -------------------------------------------------------------------------- */

interface CodeFrameProps {
  readonly language: string;
  readonly children: ReactNode;
}

function CodeFrame({ language, children }: CodeFrameProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <div className="border-cc-card-border bg-cc-surface/60 text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px] tracking-wide">
        <div className="flex items-center gap-2">
          <span
            aria-hidden
            className="bg-cc-status-firing inline-block size-2 rounded-full opacity-70"
          />
          <span
            aria-hidden
            className="bg-cc-status-investigating inline-block size-2 rounded-full opacity-70"
          />
          <span
            aria-hidden
            className="bg-cc-status-healthy inline-block size-2 rounded-full opacity-70"
          />
        </div>
        <span className="uppercase">{language}</span>
      </div>
      <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[13px] leading-[1.65]">
        <code>{children}</code>
      </pre>
    </div>
  );
}

interface CsLineProps {
  readonly children?: ReactNode;
  readonly indent?: number;
}

function CsLine({ children, indent = 0 }: CsLineProps) {
  const pad = "  ".repeat(indent);
  return (
    <div>
      {pad}
      {children}
      {"\n"}
    </div>
  );
}

interface SyntaxTokenProps {
  readonly children: ReactNode;
}

function Kw({ children }: SyntaxTokenProps) {
  return <span className="text-cc-accent">{children}</span>;
}

function Type({ children }: SyntaxTokenProps) {
  return <span className="text-cc-warning">{children}</span>;
}

function Method({ children }: SyntaxTokenProps) {
  return <span className="text-cc-heading">{children}</span>;
}

function Cmt({ children }: SyntaxTokenProps) {
  return <span className="text-cc-ink-dim italic">{children}</span>;
}

/* -------------------------------------------------------------------------- */
/*  Transports band                                                           */
/* -------------------------------------------------------------------------- */

interface Transport {
  readonly name: string;
  readonly note: string;
}

const PRIMARY_TRANSPORTS: readonly Transport[] = [
  {
    name: "RabbitMQ",
    note: "The default broker. Topology, retries, and dead-letter routing are configured for you.",
  },
  {
    name: "Postgres",
    note: "Use the database you already operate as a durable transport. Outbox lives next to your data.",
  },
  {
    name: "In-process",
    note: "Zero-cost dispatch for local handlers and tests. Same API, no broker required.",
  },
];

const SECONDARY_TRANSPORTS: readonly string[] = [
  "Kafka",
  "Azure Service Bus",
  "Azure Event Hubs",
];

function TransportsBand() {
  return (
    <section className="py-16">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Pluggable transports
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Swap the broker, keep your handlers.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          Register a default transport, route specific messages through a
          different one, run multiple transports at once. Your handler code does
          not change when the broker does.
        </p>
      </div>

      <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {PRIMARY_TRANSPORTS.map((transport) => (
          <article
            key={transport.name}
            className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-xl border p-6 backdrop-blur-sm transition-colors"
          >
            <div className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
              Primary
            </div>
            <h3 className="text-cc-heading mt-2 text-lg font-semibold">
              {transport.name}
            </h3>
            <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
              {transport.note}
            </p>
            <ul className="mt-4 space-y-1.5">
              <li className="text-cc-ink flex items-start gap-2 text-sm">
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon />
                </span>
                <span>Outbox and inbox supported</span>
              </li>
              <li className="text-cc-ink flex items-start gap-2 text-sm">
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon />
                </span>
                <span>Retry, dead-letter, scheduling</span>
              </li>
            </ul>
          </article>
        ))}
      </div>

      <div className="border-cc-card-border bg-cc-surface/40 mt-6 flex flex-wrap items-center justify-between gap-4 rounded-xl border px-6 py-5 backdrop-blur-sm">
        <div>
          <div className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
            Also in source
          </div>
          <p className="text-cc-ink mt-1 text-sm">
            Reach further when you need to, on the same handler model.
          </p>
        </div>
        <ul className="flex flex-wrap items-center gap-2">
          {SECONDARY_TRANSPORTS.map((name) => (
            <li
              key={name}
              className="border-cc-card-border text-cc-ink rounded-full border px-3 py-1.5 font-mono text-xs"
            >
              {name}
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  MIT / open source band                                                    */
/* -------------------------------------------------------------------------- */

function MitBand() {
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 text-center backdrop-blur-sm sm:p-12">
        <div className="text-cc-accent mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          MIT licensed, open source
        </div>
        <h2 className="text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
          Open in the open. Commercial or otherwise, no strings attached.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          Mocha is open source under the MIT license and developed in the open
          on GitHub. Read the source, file issues, send patches, or fork it for
          your own platform.
        </p>
        <div className="mt-7 flex flex-wrap justify-center gap-4">
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform/blob/main/LICENSE">
            Read the license
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA                                                               */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="py-20 text-center">
      <h2 className="text-cc-heading mx-auto max-w-3xl text-3xl font-semibold tracking-tight sm:text-4xl">
        Your next message is a{" "}
        <span className="text-cc-accent">PublishAsync</span> away.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base sm:text-lg">
        Add the package, implement IEventHandler, and the source generator does
        the rest. The topology you just walked through is one AddMessageBus()
        call away.
      </p>

      <div className="mx-auto mt-8 max-w-xl">
        <CodeFrame language="terminal">
          <TermLine prompt>dotnet add package Mocha</TermLine>
          <TermLine prompt>
            dotnet add package Mocha.Transport.RabbitMQ
          </TermLine>
          <TermLine prompt>dotnet run</TermLine>
        </CodeFrame>
      </div>

      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/mocha">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
    </section>
  );
}

interface TermLineProps {
  readonly children: ReactNode;
  readonly prompt?: boolean;
}

function TermLine({ children, prompt }: TermLineProps) {
  return (
    <div className="text-cc-ink">
      {prompt ? <span className="text-cc-accent">$ </span> : null}
      {children}
      {"\n"}
    </div>
  );
}
