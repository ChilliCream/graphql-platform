import type { Metadata } from "next";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Mocha } from "@/src/icons/Mocha";

export const metadata: Metadata = {
  title: "Mocha: Event-driven .NET, open and modern",
  description:
    "Mocha is the source-generated mediator and cross-service message bus for .NET. CQRS, sagas, outbox and inbox in one open-source, MIT-licensed framework.",
  keywords: [
    "Mocha",
    ".NET mediator",
    ".NET message bus",
    "CQRS",
    "source generator",
    "saga",
    "outbox",
    "inbox",
    "exactly-once processing",
    "RabbitMQ",
    "Postgres",
    "Kafka",
    "Azure Service Bus",
    "OpenTelemetry",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Mocha: Event-driven .NET, open and modern",
    description:
      "One open-source framework for in-process CQRS and cross-service messaging on .NET. Source-generated dispatch, sagas validated before traffic, outbox plus inbox.",
    type: "website",
  },
};

// Brand spectrum, used exactly once on this page (on the wedge word in the hero).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// ---------------------------------------------------------------------------
// Primitives
// ---------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-[0.25em] uppercase">
      {children}
    </div>
  );
}

interface SectionHeaderProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly lead?: ReactNode;
  readonly align?: "left" | "center";
}

function SectionHeader({
  eyebrow,
  title,
  lead,
  align = "center",
}: SectionHeaderProps) {
  const alignment = align === "center" ? "text-center mx-auto" : "text-left";
  return (
    <div className={`max-w-3xl ${alignment}`}>
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-3 text-3xl tracking-tight sm:text-4xl">
        {title}
      </h2>
      {lead ? <p className="text-cc-ink-dim lead mt-4">{lead}</p> : null}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Hero
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="relative pt-12 pb-10 sm:pt-20 sm:pb-16">
      <div className="mx-auto grid max-w-6xl gap-12 px-2 lg:grid-cols-[1.25fr_1fr] lg:items-center">
        <div>
          <Eyebrow>Mediator and Bus for .NET</Eyebrow>
          <h1 className="text-cc-heading font-heading text-hero mt-5 tracking-tight">
            Event-driven .NET,{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              open and modern
            </span>
            .
          </h1>
          <p className="text-cc-ink-dim lead mt-6 max-w-xl">
            Mocha is one source-generated framework for in-process CQRS and
            cross-service messaging on .NET. Write a handler, publish a message,
            and let the platform compose the pipeline, the transport, and the
            telemetry around your code.
          </p>
          <div className="mt-9 flex flex-wrap gap-4">
            <SolidButton href="/docs/mocha">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
          <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase">
            <span>MIT licensed</span>
            <span aria-hidden className="text-cc-ink-faint">
              /
            </span>
            <span>ASP.NET Core</span>
            <span aria-hidden className="text-cc-ink-faint">
              /
            </span>
            <span>Source-generated</span>
            <span aria-hidden className="text-cc-ink-faint">
              /
            </span>
            <span>AOT-friendly</span>
          </div>
        </div>
        <HeroArtwork />
      </div>
    </section>
  );
}

// Hero artwork: the Mocha cup, with a small annotated wire diagram of a
// message flow leaving the cup as a soft brand stroke. Single-instance use of
// the spectrum is on the headline, so the wires here use the teal accent only.
function HeroArtwork() {
  return (
    <div className="relative mx-auto w-full max-w-md">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 rounded-full opacity-40 blur-3xl"
        style={{
          background:
            "radial-gradient(closest-side, rgba(94,234,212,0.18), transparent)",
        }}
      />
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 backdrop-blur-sm">
        <div className="flex items-center justify-between">
          <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.3em] uppercase">
            mocha.bus
          </span>
          <span className="text-cc-accent inline-flex items-center gap-2 font-mono text-[0.65rem] tracking-[0.3em] uppercase">
            <span className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full" />
            healthy
          </span>
        </div>
        <div className="mt-6 flex items-center justify-center">
          <Mocha className="h-44 w-auto drop-shadow-[0_20px_30px_rgba(0,0,0,0.45)]" />
        </div>
        <div className="text-cc-ink mt-6 grid grid-cols-3 gap-2 font-mono text-[0.65rem] tracking-widest uppercase">
          <span className="border-cc-card-border bg-cc-bg/60 rounded-md border px-2 py-1.5 text-center">
            publish
          </span>
          <span className="border-cc-card-border bg-cc-bg/60 rounded-md border px-2 py-1.5 text-center">
            dispatch
          </span>
          <span className="border-cc-card-border bg-cc-bg/60 rounded-md border px-2 py-1.5 text-center">
            handle
          </span>
        </div>
        <p className="text-cc-ink-dim mt-4 text-center text-xs">
          Every hop is an OpenTelemetry span.
        </p>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// What makes it different: 4 cards
// ---------------------------------------------------------------------------

interface PillarProps {
  readonly index: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

const PILLARS: readonly PillarProps[] = [
  {
    index: "01",
    title: "Mediator and bus in one",
    body: "One handler-first programming model spans in-process CQRS and cross-service messaging. Inject IMediator for local commands and queries, inject IEventBus for pub/sub, send, and request/reply across services. Same handlers, same pipelines, same observability.",
    bullets: [
      "ICommand, IQuery, INotification for in-process dispatch",
      "PublishAsync, SendAsync, RequestAsync for the bus",
      "Use either side standalone, or both together",
    ],
  },
  {
    index: "02",
    title: "Roslyn source-generated dispatch",
    body: "A Roslyn source generator discovers your handlers and sagas at compile time and emits typed registration plus pre-compiled pipeline delegates. No MakeGenericType at runtime, no service-provider lookups to resolve the pipeline, no reflection on the hot path.",
    bullets: [
      "Compile-time errors when a handler is missing",
      "Pre-compiled delegates for each pipeline stage",
      "AOT-friendly, no runtime code generation",
    ],
  },
  {
    index: "03",
    title: "Sagas you can ship safely",
    body: "Define a saga as a sealed class with explicit transitions like Draft to Checked to Published. Mocha validates that every state is reachable and every path leads to a final state before the service handles its first message, so a stuck saga never reaches production.",
    bullets: [
      "Validated at startup, before traffic is served",
      "Persists state, manages transitions, handles compensation",
      "Scheduled and delayed messages for timeouts",
    ],
  },
  {
    index: "04",
    title: "Outbox plus inbox: exactly-once processing",
    body: "A transactional outbox makes your database write and the message dispatch succeed or fail together. An idempotent inbox dedupes incoming messages by id. Together they give exactly-once processing on top of an at-least-once transport, no lost messages and no double work.",
    bullets: [
      "Transactional outbox per database, EF Core integration",
      "Idempotent inbox dedupes by message id",
      "Dead-letter, retry, redelivery, circuit breaker, concurrency limiter",
    ],
  },
];

function PillarCard({ index, title, body, bullets }: PillarProps) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-2xl border p-7 backdrop-blur-sm transition-colors">
      <div className="text-cc-accent font-mono text-xs tracking-[0.3em] uppercase">
        {index}
      </div>
      <h3 className="text-cc-heading font-heading mt-3 text-xl tracking-tight">
        {title}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">{body}</p>
      <ul className="mt-5 space-y-2">
        {bullets.map((b) => (
          <li
            key={b}
            className="text-cc-ink flex items-start gap-2 text-sm leading-snug"
          >
            <span className="text-cc-accent mt-0.5 inline-flex shrink-0">
              <CheckIcon />
            </span>
            <span>{b}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

function PillarsSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="What makes it different"
        title="Four things Mocha leans on, by design"
        lead="Mediator plus bus in one framework, source-generated dispatch, sagas validated before the service handles traffic, and reliability primitives that compose into exactly-once processing."
      />
      <div className="mt-12 grid gap-5 md:grid-cols-2">
        {PILLARS.map((p) => (
          <PillarCard key={p.index} {...p} />
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Real-feel code example: [Handler] + PublishAsync
// ---------------------------------------------------------------------------

// Color tokens kept inline so the snippet ships as static HTML, no client JS.
const TOK = {
  kw: "text-[#7c92c6]", // keyword (violet)
  type: "text-[#16b9e4]", // type (cyan)
  str: "text-[#f0786a]", // string (coral)
  com: "text-cc-ink-dim", // comment
  ident: "text-cc-heading",
  punct: "text-cc-ink",
};

function CodeExample() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The shape of code"
        title="A handler, a publish, a saga"
        lead="A command handler persists the entity and publishes a domain event. A saga picks the event up and drives the workflow. The source generator wires registration and pipelines at compile time."
      />
      <div className="mx-auto mt-10 max-w-3xl">
        <div className="border-cc-card-border bg-cc-surface/80 overflow-hidden rounded-2xl border shadow-2xl shadow-black/40 backdrop-blur-sm">
          <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
            <div className="flex items-center gap-2">
              <span className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-success/70 h-2.5 w-2.5 rounded-full" />
            </div>
            <span className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
              ReviewHandlers.cs
            </span>
          </div>
          <pre className="overflow-x-auto p-5 font-mono text-[13px] leading-relaxed">
            <code>
              <span className={TOK.com}>
                {"// Source-generated dispatch. No reflection on the hot path."}
              </span>
              {"\n"}
              <span className={TOK.kw}>using </span>
              <span className={TOK.type}>Mocha</span>
              <span className={TOK.punct}>;</span>
              {"\n\n"}
              <span className={TOK.kw}>public record </span>
              <span className={TOK.type}>CreateReview</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.type}>Guid</span>{" "}
              <span className={TOK.ident}>ProductId</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.type}>string</span>{" "}
              <span className={TOK.ident}>Body</span>
              <span className={TOK.punct}>{")"} : </span>
              <span className={TOK.type}>ICommand</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>Guid</span>
              <span className={TOK.punct}>{">"};</span>
              {"\n"}
              <span className={TOK.kw}>public record </span>
              <span className={TOK.type}>ReviewCreated</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.type}>Guid</span>{" "}
              <span className={TOK.ident}>ReviewId</span>
              <span className={TOK.punct}>{")"} : </span>
              <span className={TOK.type}>INotification</span>
              <span className={TOK.punct}>;</span>
              {"\n\n"}
              <span className={TOK.kw}>public class </span>
              <span className={TOK.type}>CreateReviewHandler</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.type}>ReviewDbContext</span>{" "}
              <span className={TOK.ident}>db</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.type}>IEventBus</span>{" "}
              <span className={TOK.ident}>bus</span>
              <span className={TOK.punct}>{")"}</span>
              {"\n  "}
              <span className={TOK.punct}>{": "}</span>
              <span className={TOK.type}>ICommandHandler</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>CreateReview</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.type}>Guid</span>
              <span className={TOK.punct}>{">"}</span>
              {"\n"}
              <span className={TOK.punct}>{"{"}</span>
              {"\n  "}
              <span className={TOK.kw}>public async </span>
              <span className={TOK.type}>ValueTask</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>Guid</span>
              <span className={TOK.punct}>{">"} </span>
              <span className={TOK.ident}>HandleAsync</span>
              <span className={TOK.punct}>{"("}</span>
              {"\n      "}
              <span className={TOK.type}>CreateReview</span>{" "}
              <span className={TOK.ident}>cmd</span>
              <span className={TOK.punct}>{","}</span>
              {"\n      "}
              <span className={TOK.type}>CancellationToken</span>{" "}
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{")"}</span>
              {"\n  "}
              <span className={TOK.punct}>{"{"}</span>
              {"\n    "}
              <span className={TOK.kw}>var </span>
              <span className={TOK.ident}>review</span>
              <span className={TOK.punct}>{" = new "}</span>
              <span className={TOK.type}>Review</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>cmd</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>ProductId</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.ident}>cmd</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Body</span>
              <span className={TOK.punct}>{");"}</span>
              {"\n    "}
              <span className={TOK.ident}>db</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Reviews</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Add</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>review</span>
              <span className={TOK.punct}>{");"}</span>
              {"\n\n    "}
              <span className={TOK.com}>
                {"// Outbox: the save and the publish are one transaction."}
              </span>
              {"\n    "}
              <span className={TOK.kw}>await </span>
              <span className={TOK.ident}>bus</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>PublishAsync</span>
              <span className={TOK.punct}>{"(new "}</span>
              <span className={TOK.type}>ReviewCreated</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>review</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Id</span>
              <span className={TOK.punct}>{"), "}</span>
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{");"}</span>
              {"\n    "}
              <span className={TOK.kw}>await </span>
              <span className={TOK.ident}>db</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>SaveChangesAsync</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>ct</span>
              <span className={TOK.punct}>{");"}</span>
              {"\n    "}
              <span className={TOK.kw}>return </span>
              <span className={TOK.ident}>review</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Id</span>
              <span className={TOK.punct}>{";"}</span>
              {"\n  "}
              <span className={TOK.punct}>{"}"}</span>
              {"\n"}
              <span className={TOK.punct}>{"}"}</span>
              {"\n\n"}
              <span className={TOK.kw}>public sealed class </span>
              <span className={TOK.type}>ReviewWorkflow</span>
              <span className={TOK.punct}>{" : "}</span>
              <span className={TOK.type}>Saga</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>ReviewState</span>
              <span className={TOK.punct}>{">"}</span>
              {"\n"}
              <span className={TOK.punct}>{"{"}</span>
              {"\n  "}
              <span className={TOK.com}>
                {"// Draft -> Checked -> Published; validated before traffic."}
              </span>
              {"\n  "}
              <span className={TOK.kw}>protected override void </span>
              <span className={TOK.ident}>Configure</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.type}>SagaBuilder</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>ReviewState</span>
              <span className={TOK.punct}>{">"} </span>
              <span className={TOK.ident}>b</span>
              <span className={TOK.punct}>{") =>"}</span>
              {"\n      "}
              <span className={TOK.ident}>b</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>On</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>ReviewCreated</span>
              <span className={TOK.punct}>{">().Goto("}</span>
              <span className={TOK.str}>{'"Checked"'}</span>
              <span className={TOK.punct}>{")"}</span>
              {"\n       "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>On</span>
              <span className={TOK.punct}>{"<"}</span>
              <span className={TOK.type}>ContentChecked</span>
              <span className={TOK.punct}>{">().Goto("}</span>
              <span className={TOK.str}>{'"Published"'}</span>
              <span className={TOK.punct}>{").Finalize();"}</span>
              {"\n"}
              <span className={TOK.punct}>{"}"}</span>
            </code>
          </pre>
        </div>
        <p className="text-cc-ink-dim mt-4 text-center text-sm">
          The generator emits the registration and the pipeline. The bus picks
          up <code className="text-cc-ink">ReviewCreated</code> from the outbox
          and the saga drives <code className="text-cc-ink">Draft</code> to{" "}
          <code className="text-cc-ink">Checked</code> to{" "}
          <code className="text-cc-ink">Published</code>.
        </p>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Per-hop trace timeline (every hop is a span in Nitro)
// ---------------------------------------------------------------------------

interface TraceSpan {
  readonly label: string;
  readonly service: string;
  readonly start: number; // percent
  readonly width: number; // percent
  readonly color: string;
}

const SPANS: readonly TraceSpan[] = [
  {
    label: "POST /reviews",
    service: "reviews.api",
    start: 0,
    width: 100,
    color: "#5eead4",
  },
  {
    label: "mediator.dispatch CreateReview",
    service: "reviews.api",
    start: 3,
    width: 24,
    color: "#16b9e4",
  },
  {
    label: "outbox.write ReviewCreated",
    service: "reviews.api",
    start: 14,
    width: 11,
    color: "#7c92c6",
  },
  {
    label: "transport.publish (RabbitMQ)",
    service: "reviews.api",
    start: 25,
    width: 9,
    color: "#7c92c6",
  },
  {
    label: "inbox.dedupe ReviewCreated",
    service: "moderation",
    start: 35,
    width: 8,
    color: "#34d399",
  },
  {
    label: "handler ContentCheck",
    service: "moderation",
    start: 43,
    width: 28,
    color: "#5eead4",
  },
  {
    label: "saga.transition Checked",
    service: "reviews.workflow",
    start: 72,
    width: 12,
    color: "#fbbf24",
  },
  {
    label: "saga.transition Published",
    service: "reviews.workflow",
    start: 85,
    width: 13,
    color: "#34d399",
  },
];

interface SpanRowProps {
  readonly span: TraceSpan;
}

function SpanRow({ span }: SpanRowProps) {
  const barStyle: CSSProperties = {
    left: `${span.start}%`,
    width: `${span.width}%`,
    backgroundColor: span.color,
  };
  return (
    <div className="grid grid-cols-[minmax(0,1fr)_3fr] items-center gap-3 py-1.5">
      <div className="min-w-0">
        <div className="text-cc-heading truncate font-mono text-xs">
          {span.label}
        </div>
        <div className="text-cc-nav-label truncate font-mono text-[0.65rem] tracking-widest uppercase">
          {span.service}
        </div>
      </div>
      <div className="bg-cc-bg/70 relative h-5 overflow-hidden rounded-sm">
        <div
          className="absolute top-0 bottom-0 rounded-sm opacity-90"
          style={barStyle}
        />
      </div>
    </div>
  );
}

function TraceSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Every hop a span"
        title="Follow a message from publish to handle, in Nitro"
        lead="Mocha emits an OpenTelemetry span for every dispatch, transport hop, inbox dedupe, handler execution, and saga transition. Correlation flows across services automatically, so the trace you see is the whole story."
      />
      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-5xl overflow-hidden rounded-xl border backdrop-blur-sm">
        <div className="border-cc-card-border flex items-center justify-between border-b px-5 py-3">
          <div className="flex items-center gap-3">
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.3em] uppercase">
              trace
            </span>
            <code className="text-cc-ink-dim text-xs">
              reviews.api -&gt; moderation -&gt; reviews.workflow
            </code>
          </div>
          <span className="text-cc-accent inline-flex items-center gap-2 font-mono text-[0.65rem] tracking-[0.3em] uppercase">
            <span className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full" />
            ok
          </span>
        </div>
        <div className="px-5 py-5">
          <div className="text-cc-nav-label grid grid-cols-[minmax(0,1fr)_3fr] gap-3 pb-2 font-mono text-[0.6rem] tracking-[0.25em] uppercase">
            <span>span</span>
            <div className="text-cc-nav-label flex justify-between">
              <span>t = start</span>
              <span>end</span>
            </div>
          </div>
          <div className="bg-cc-card-border h-px" />
          <div className="mt-2">
            {SPANS.map((s) => (
              <SpanRow key={s.label} span={s} />
            ))}
          </div>
        </div>
        <div className="border-cc-card-border text-cc-ink-dim border-t px-5 py-3 text-xs">
          The same telemetry powers the Mocha topology view in Nitro: nodes for
          services, edges for messages, drill in to see this exact span tree.
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Soft comparison band: factual deltas only
// ---------------------------------------------------------------------------

interface PeerRowProps {
  readonly name: string;
  readonly shape: string;
  readonly license: string;
  readonly delta: string;
}

const PEERS: readonly PeerRowProps[] = [
  {
    name: "MediatR",
    shape: "In-process mediator only",
    license: "Commercial (dual-license since 2025)",
    delta:
      "Mocha source-generates handler dispatch at compile time. MediatR resolves and composes the pipeline at runtime via reflection.",
  },
  {
    name: "MassTransit",
    shape: "Cross-service message bus, sagas, outbox",
    license: "v8 Apache-2.0, v9 commercial (GA Q1 2026)",
    delta:
      "Mocha is one framework for both in-process CQRS and the cross-service bus. MassTransit covers the bus side, with a separate mediator add-on.",
  },
  {
    name: "NServiceBus",
    shape: "Enterprise service bus, sagas, outbox",
    license: "Commercial, per-endpoint",
    delta:
      "Mocha is MIT-licensed open source. Programming model uses ICommandHandler / IEventHandler classes plus a Roslyn source generator instead of runtime conventions.",
  },
  {
    name: "Wolverine",
    shape: "Mediator and bus, sagas, outbox",
    license: "Open source (MIT)",
    delta:
      "Mocha source-generates handler dispatch at compile time. Wolverine generates handler glue at runtime on first use.",
  },
];

function PeerCard({ name, shape, license, delta }: PeerRowProps) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-xl border p-5 backdrop-blur-sm transition-colors">
      <div className="flex items-baseline justify-between gap-3">
        <h3 className="text-cc-heading font-heading text-lg tracking-tight">
          {name}
        </h3>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.2em] uppercase">
          peer
        </span>
      </div>
      <dl className="mt-3 space-y-2 text-sm">
        <div className="flex gap-2">
          <dt className="text-cc-nav-label w-16 shrink-0 font-mono text-[0.65rem] tracking-widest uppercase">
            shape
          </dt>
          <dd className="text-cc-ink">{shape}</dd>
        </div>
        <div className="flex gap-2">
          <dt className="text-cc-nav-label w-16 shrink-0 font-mono text-[0.65rem] tracking-widest uppercase">
            license
          </dt>
          <dd className="text-cc-ink">{license}</dd>
        </div>
      </dl>
      <div className="bg-cc-card-border my-4 h-px" />
      <p className="text-cc-ink-dim text-sm leading-relaxed">{delta}</p>
    </article>
  );
}

function ComparisonSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Soft comparison"
        title="Where Mocha sits next to the .NET messaging libraries you know"
        lead="Factual deltas, no takedowns. The .NET messaging space is full of good ideas; Mocha is the one that source-generates the mediator and the bus together, under MIT."
      />
      <div className="mt-10 grid gap-5 sm:grid-cols-2">
        {PEERS.map((p) => (
          <PeerCard key={p.name} {...p} />
        ))}
      </div>
      <p className="text-cc-ink-dim mx-auto mt-8 max-w-3xl text-center text-sm">
        Mocha is part of the wider ChilliCream platform: the topology and span
        view above light up automatically alongside Hot Chocolate and Fusion in
        the same Nitro instance.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Catalogue: denser feature grid
// ---------------------------------------------------------------------------

interface FeatureProps {
  readonly title: string;
  readonly body: string;
  readonly icon: ReactNode;
}

function FeatureIcon({ children }: { children: ReactNode }) {
  return (
    <span
      aria-hidden
      className="border-cc-card-border bg-cc-bg/60 text-cc-accent inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border"
    >
      {children}
    </span>
  );
}

function IconHandlers() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <rect x="3" y="4" width="18" height="6" rx="1.5" />
      <rect x="3" y="14" width="18" height="6" rx="1.5" />
      <path d="M7 7h2" />
      <path d="M7 17h2" />
    </svg>
  );
}

function IconBus() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="5" cy="12" r="2" />
      <circle cx="19" cy="6" r="2" />
      <circle cx="19" cy="18" r="2" />
      <path d="M7 12 17 6" />
      <path d="M7 12 17 18" />
    </svg>
  );
}

function IconTransport() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <rect x="3" y="6" width="18" height="12" rx="2" />
      <path d="M3 10h18" />
      <path d="M8 14h2" />
      <path d="M14 14h2" />
    </svg>
  );
}

function IconReliability() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <path d="M12 3 4 6v6c0 4.5 3.4 8.3 8 9 4.6-.7 8-4.5 8-9V6l-8-3Z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  );
}

function IconSchedule() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </svg>
  );
}

function IconTelemetry() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <path d="M3 17l5-6 4 4 5-7 4 5" />
      <circle cx="8" cy="11" r="1.2" />
      <circle cx="12" cy="15" r="1.2" />
      <circle cx="17" cy="8" r="1.2" />
    </svg>
  );
}

const FEATURES: readonly FeatureProps[] = [
  {
    title: "Three messaging patterns",
    body: "PublishAsync for pub/sub, SendAsync for one-to-one fire-and-forget, RequestAsync for typed request/reply. Plus IBatchEventHandler<T> when you want to consume in batches.",
    icon: <IconBus />,
  },
  {
    title: "Handler-first programming",
    body: "Implement ICommandHandler, IQueryHandler, or IEventHandler with a HandleAsync method. Mocha generates the registration and the pre-compiled pipeline. Inject services via the constructor.",
    icon: <IconHandlers />,
  },
  {
    title: "Pluggable transports",
    body: "Default transport plus per-message overrides. Ships RabbitMQ, Postgres, in-process; Kafka, Azure Service Bus, and Azure Event Hub live in source.",
    icon: <IconTransport />,
  },
  {
    title: "Reliability primitives",
    body: "Outbox plus idempotent inbox for exactly-once processing. Dead-letter routing, per-exception retry and redelivery, circuit breaker, concurrency limiter.",
    icon: <IconReliability />,
  },
  {
    title: "Scheduled and delayed delivery",
    body: "Hand a message to the bus with an absolute DateTimeOffset or a relative delay. Cancel before dispatch. Durable via Postgres, in-memory in dev.",
    icon: <IconSchedule />,
  },
  {
    title: "OpenTelemetry native",
    body: "Structured spans and metrics for dispatch, receive, and handle. Correlation IDs propagate across services. No-op observer when telemetry is off.",
    icon: <IconTelemetry />,
  },
];

function CatalogueSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The catalogue"
        title="Everything else you expect from a serious messaging framework"
        lead="The shortlist below ships in the box. ASP.NET Core DI, .NET Aspire, EF Core integration, and the Mocha topology view in Nitro come with it."
      />
      <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {FEATURES.map((f) => (
          <div
            key={f.title}
            className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full gap-4 rounded-xl border p-5 backdrop-blur-sm transition-colors"
          >
            <FeatureIcon>{f.icon}</FeatureIcon>
            <div>
              <h3 className="text-cc-heading font-heading text-base tracking-tight">
                {f.title}
              </h3>
              <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
                {f.body}
              </p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// MIT band
// ---------------------------------------------------------------------------

function MitBand() {
  return (
    <section className="py-12 sm:py-16">
      <div className="border-cc-card-border bg-cc-surface/70 relative overflow-hidden rounded-2xl border p-8 sm:p-10">
        <div
          aria-hidden
          className="bg-cc-accent/40 pointer-events-none absolute inset-x-0 top-0 h-px"
        />
        <div className="flex flex-col items-start gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="max-w-2xl">
            <Eyebrow>Open source</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-2xl tracking-tight sm:text-3xl">
              MIT licensed. Open and modern, on purpose.
            </h2>
            <p className="text-cc-ink-dim mt-3 text-sm sm:text-base">
              Mocha is released under the MIT license, with the rest of the
              ChilliCream platform. Drop it into a commercial product, an
              internal API, or a side project. Read the source, file an issue,
              send a PR.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform/blob/main/LICENSE">
              Read the license
            </OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Closing CTA
// ---------------------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="pt-12 pb-20 text-center sm:pt-16">
      <Eyebrow>One handler, one publish</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-4 text-3xl tracking-tight sm:text-4xl">
        Wire one handler. Publish one message. See the span tree.
      </h2>
      <p className="text-cc-ink-dim lead mx-auto mt-5 max-w-2xl">
        Install the templates, scaffold a service, point Mocha at RabbitMQ or
        Postgres, and watch the topology light up in Nitro. Add a saga the day
        you need one.
      </p>
      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/mocha">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
      <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 font-mono text-xs tracking-widest uppercase">
        <span>dotnet add package Mocha</span>
        <span aria-hidden className="text-cc-ink-faint">
          /
        </span>
        <span>dotnet add package Mocha.Transport.RabbitMQ</span>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function MochaOpenModernPreviewPage() {
  return (
    <>
      <Hero />
      <PillarsSection />
      <CodeExample />
      <TraceSection />
      <ComparisonSection />
      <CatalogueSection />
      <MitBand />
      <ClosingCta />
    </>
  );
}
