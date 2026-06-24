import type { Metadata } from "next";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroTrace } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Mocha: The Dispatch Essay",
  description:
    "Mocha messaging .NET, a source-generated mediator and cross-service bus. Sagas validated before traffic. Exactly-once processing via outbox and idempotent inbox.",
  keywords: [
    "Mocha messaging .NET",
    "Mocha",
    ".NET messaging",
    "in-process mediator",
    "message bus",
    "CQRS",
    "Roslyn source generator",
    "RabbitMQ",
    "Postgres outbox",
    "transactional outbox",
    "idempotent inbox",
    "saga orchestration",
    "OpenTelemetry",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Mocha messaging .NET, told as an essay",
    description:
      "A source-generated mediator and cross-service bus for .NET. Sagas validated before traffic. Exactly-once processing via outbox and idempotent inbox.",
    type: "website",
  },
};

// The brand-spectrum gradient. Used exactly once on the page: as the closing
// hairline above the final CTA, where the left-edge thread terminates.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Code syntax tokens, GitHub-dark approximations, scoped to the inline snippet.
// -----------------------------------------------------------------------------

const C = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  comment: { color: "#8b949e", fontStyle: "italic" as const },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
};

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface ChapterMarkerProps {
  readonly index: string;
  readonly label: string;
}

/**
 * Anchors each movement. The mono numerals double as the badge for the
 * continuous left-edge thread that runs from hero to CTA.
 */
function ChapterMarker({ index, label }: ChapterMarkerProps) {
  return (
    <div className="flex items-center gap-3">
      <span
        aria-hidden
        className="border-cc-accent/70 bg-cc-bg text-cc-accent inline-flex h-7 w-7 items-center justify-center rounded-full border font-mono text-[11px] tabular-nums"
      >
        {index}
      </span>
      <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
        {label}
      </span>
    </div>
  );
}

interface VisualFrameProps {
  readonly caption: ReactNode;
  readonly children: ReactNode;
}

function VisualFrame({ caption, children }: VisualFrameProps) {
  return (
    <figure className="my-2">
      <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border p-5 sm:p-6">
        {children}
      </div>
      <figcaption className="text-cc-ink-dim text-caption mt-3 font-mono tracking-[0.16em] italic">
        {caption}
      </figcaption>
    </figure>
  );
}

interface PullQuoteProps {
  readonly children: ReactNode;
}

function PullQuote({ children }: PullQuoteProps) {
  return (
    <blockquote className="text-cc-heading font-heading text-h4 my-4 italic">
      &ldquo;{children}&rdquo;
    </blockquote>
  );
}

// -----------------------------------------------------------------------------
// Movement wrapper. Owns the left-edge continuous hairline, the chapter badge
// pinned to it, and the inner column rhythm.
// -----------------------------------------------------------------------------

interface MovementProps {
  readonly id: string;
  readonly index: string;
  readonly label: string;
  readonly title?: string;
  readonly children: ReactNode;
}

function Movement({ id, index, label, title, children }: MovementProps) {
  return (
    <section
      id={id}
      aria-labelledby={`${id}-title`}
      className="relative scroll-mt-24 py-24 sm:py-28 lg:py-32"
    >
      <ChapterMarker index={index} label={label} />
      {title ? (
        <h2
          id={`${id}-title`}
          className="text-cc-heading font-heading text-h3 mt-6 font-semibold tracking-tight text-balance"
        >
          {title}
        </h2>
      ) : null}
      <div className="text-cc-ink-dim text-lead mt-8 space-y-8">{children}</div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Inline snippet for movement 02. Condensed CreateReviewHandler.cs core.
// -----------------------------------------------------------------------------

interface CodeLineProps {
  readonly n: number;
  readonly children: ReactNode;
}

function CodeLine({ n, children }: CodeLineProps) {
  return (
    <div className="flex gap-4 px-5">
      <span
        className="w-6 shrink-0 text-right font-mono text-[11px] text-[#484f58] tabular-nums select-none"
        aria-hidden
      >
        {n}
      </span>
      <span className="font-mono text-[12.5px] leading-6 whitespace-pre">
        {children}
      </span>
    </div>
  );
}

function CreateReviewHandlerSnippet() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-xl border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-3">
        <span
          className="bg-cc-status-firing h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-investigating h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-healthy h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span className="text-cc-ink-dim ml-3 font-mono text-[11px]">
          Reviews/CreateReviewHandler.cs
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          C# / mediator
        </span>
      </div>
      <div className="py-4">
        <CodeLine n={1}>
          <span style={C.comment}>{`// Source-generated registration.`}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.punct}>[</span>
          <span style={C.attr}>Handler</span>
          <span style={C.punct}>]</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.kw}>public static async</span>{" "}
          <span style={C.type}>Task</span>
          <span style={C.punct}>{`<`}</span>
          <span style={C.type}>Guid</span>
          <span style={C.punct}>{`>`}</span>{" "}
          <span style={C.fn}>HandleAsync</span>
          <span style={C.punct}>(</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.type}>CreateReview</span>{" "}
          <span style={C.param}>command</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.type}>ReviewsDbContext</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.type}>IPublisher</span>{" "}
          <span style={C.param}>bus</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.type}>CancellationToken</span>{" "}
          <span style={C.param}>ct</span>
          <span style={C.punct}>{`)`}</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={C.punct}>{`{`}</span>
        </CodeLine>
        <CodeLine n={9}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.kw}>var</span> <span style={C.param}>review</span>{" "}
          <span style={C.punct}>=</span> <span style={C.type}>Review</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Draft</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>command</span>
          <span style={C.punct}>);</span>
        </CodeLine>
        <CodeLine n={10}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.param}>db</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Reviews</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>review</span>
          <span style={C.punct}>);</span>
        </CodeLine>
        <CodeLine n={11}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.kw}>await</span> <span style={C.param}>bus</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>PublishAsync</span>
          <span style={C.punct}>(</span>
          <span style={C.kw}>new</span>{" "}
          <span style={C.type}>ReviewCreated</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>review</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Id</span>
          <span style={C.punct}>), </span>
          <span style={C.param}>ct</span>
          <span style={C.punct}>);</span>
        </CodeLine>
        <CodeLine n={12}>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Inline diagrams (carried over from v1, unchanged framing logic).
// -----------------------------------------------------------------------------

function MediatorAndBusDiagram() {
  return (
    <svg
      viewBox="0 0 480 240"
      className="h-auto w-full"
      role="img"
      aria-label="One handler-first model dispatches in-process via mediator and across services via the bus"
    >
      <rect
        x="12"
        y="32"
        width="220"
        height="176"
        rx="10"
        fill="rgba(245,241,234,0.03)"
        stroke="rgba(245,241,234,0.16)"
        strokeDasharray="4 4"
      />
      <text
        x="22"
        y="50"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        process A
      </text>
      <rect
        x="32"
        y="70"
        width="80"
        height="32"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="72"
        y="90"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#a1a3af"
      >
        ISender
      </text>
      <path
        d="M 112 86 L 152 86"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.4"
        fill="none"
      />
      <rect
        x="152"
        y="68"
        width="70"
        height="36"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="187"
        y="83"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10.5"
        fill="#5eead4"
      >
        [Handler]
      </text>
      <text
        x="187"
        y="97"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.62)"
      >
        CreateReview
      </text>

      <rect
        x="32"
        y="140"
        width="80"
        height="32"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="72"
        y="160"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#a1a3af"
      >
        IPublisher
      </text>
      <path
        d="M 112 156 L 232 156"
        stroke="rgba(94,234,212,0.5)"
        strokeWidth="1.4"
        fill="none"
      />

      <rect
        x="232"
        y="138"
        width="68"
        height="36"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="266"
        y="153"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10.5"
        fill="#5eead4"
      >
        transport
      </text>
      <text
        x="266"
        y="167"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.62)"
      >
        rabbit / pg
      </text>
      <path
        d="M 300 156 L 340 156"
        stroke="rgba(94,234,212,0.5)"
        strokeWidth="1.4"
        fill="none"
      />

      <rect
        x="340"
        y="32"
        width="128"
        height="176"
        rx="10"
        fill="rgba(245,241,234,0.03)"
        stroke="rgba(245,241,234,0.16)"
        strokeDasharray="4 4"
      />
      <text
        x="350"
        y="50"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        process B
      </text>
      <rect
        x="356"
        y="138"
        width="100"
        height="36"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="406"
        y="153"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10.5"
        fill="#5eead4"
      >
        [Handler]
      </text>
      <text
        x="406"
        y="167"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.62)"
      >
        ReviewCreated
      </text>
    </svg>
  );
}

function SourceGenDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="A Roslyn source generator discovers handlers and emits typed registration plus pre-compiled pipelines"
    >
      {[
        {
          y: 24,
          label: "[Handler] CreateReview",
          tone: "rgba(94,234,212,0.5)",
        },
        {
          y: 70,
          label: "[Handler] ReviewCreated",
          tone: "rgba(94,234,212,0.4)",
        },
        { y: 116, label: "[Saga] PublishFlow", tone: "rgba(94,234,212,0.35)" },
      ].map((row) => (
        <g key={row.label}>
          <rect
            x="12"
            y={row.y}
            width="180"
            height="34"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke={row.tone}
          />
          <text
            x="24"
            y={row.y + 21}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#f5f0ea"
          >
            {row.label}
          </text>
          <path
            d={`M 192 ${row.y + 17} L 280 110`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="280"
        y="86"
        width="100"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="330"
        y="106"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        Roslyn
      </text>
      <text
        x="330"
        y="122"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        compile time
      </text>
      <path
        d="M 380 110 L 412 110"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="412,106 424,110 412,114" fill="rgba(94,234,212,0.7)" />
      <rect
        x="380"
        y="158"
        width="88"
        height="40"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="424"
        y="174"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        AddReviews()
      </text>
      <text
        x="424"
        y="188"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.55)"
      >
        pre-compiled pipeline
      </text>
    </svg>
  );
}

function TransportsDiagram() {
  const rows = [
    { y: 26, name: "RabbitMQ", note: "topic + queue routing" },
    { y: 64, name: "PostgreSQL", note: "durable + outbox" },
    { y: 102, name: "in-process", note: "same handler shape" },
    { y: 140, name: "Kafka", note: "partitioned log" },
    { y: 178, name: "Azure Service Bus / Event Hub", note: "managed Azure" },
  ];
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Swap transports without changing handlers"
    >
      <rect
        x="320"
        y="84"
        width="144"
        height="52"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="392"
        y="104"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        IEventBus
      </text>
      <text
        x="392"
        y="120"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        PublishAsync, SendAsync, RequestAsync
      </text>
      {rows.map((r) => (
        <g key={r.name}>
          <rect
            x="12"
            y={r.y}
            width="200"
            height="26"
            rx="5"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="24"
            y={r.y + 16}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="#f5f0ea"
          >
            {r.name}
          </text>
          <text
            x="208"
            y={r.y + 16}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="9.5"
            fill="rgba(245,241,234,0.55)"
          >
            {r.note}
          </text>
          <path
            d={`M 212 ${r.y + 13} C 270 ${r.y + 13}, 270 110, 320 110`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.1"
            fill="none"
          />
        </g>
      ))}
    </svg>
  );
}

function SagaDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Saga states Draft, Checked, Published validated before the service handles traffic"
    >
      {[
        { x: 28, label: "Draft" },
        { x: 192, label: "Checked" },
        { x: 356, label: "Published" },
      ].map((s, i) => (
        <g key={s.label}>
          <rect
            x={s.x}
            y="86"
            width="100"
            height="48"
            rx="10"
            fill={i === 1 ? "rgba(94,234,212,0.08)" : "rgba(245,241,234,0.04)"}
            stroke={
              i === 1 ? "rgba(94,234,212,0.55)" : "rgba(245,241,234,0.22)"
            }
          />
          <text
            x={s.x + 50}
            y="116"
            textAnchor="middle"
            fontFamily="var(--font-body)"
            fontSize="13"
            fill="#f5f0ea"
          >
            {s.label}
          </text>
          {i < 2 && (
            <g>
              <path
                d={`M ${s.x + 100} 110 L ${s.x + 188} 110`}
                stroke="rgba(94,234,212,0.55)"
                strokeWidth="1.4"
                fill="none"
              />
              <polygon
                points={`${s.x + 188},106 ${s.x + 196},110 ${s.x + 188},114`}
                fill="rgba(94,234,212,0.7)"
              />
            </g>
          )}
        </g>
      ))}
      <text
        x="78"
        y="78"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        ReviewCreated
      </text>
      <text
        x="242"
        y="78"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        ContentChecked
      </text>
      <rect
        x="28"
        y="172"
        width="428"
        height="32"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="44"
        y="192"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        validated at startup, before the service handles traffic
      </text>
    </svg>
  );
}

function OutboxInboxDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Transactional outbox commits with the database, idempotent inbox deduplicates on receive"
    >
      <rect
        x="12"
        y="24"
        width="180"
        height="172"
        rx="10"
        fill="rgba(245,241,234,0.03)"
        stroke="rgba(245,241,234,0.16)"
        strokeDasharray="4 4"
      />
      <text
        x="22"
        y="42"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        one DB transaction
      </text>
      <rect
        x="28"
        y="58"
        width="148"
        height="36"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="40"
        y="80"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#f5f0ea"
      >
        Reviews row
      </text>
      <rect
        x="28"
        y="108"
        width="148"
        height="36"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="40"
        y="130"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        outbox: ReviewCreated
      </text>
      <text
        x="28"
        y="170"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        commit together, or not at all
      </text>

      <path
        d="M 192 124 L 240 124"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.4"
        fill="none"
      />
      <rect
        x="240"
        y="106"
        width="68"
        height="36"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="274"
        y="128"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        dispatcher
      </text>
      <path
        d="M 308 124 L 348 124"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.4"
        fill="none"
      />

      <rect
        x="348"
        y="24"
        width="120"
        height="172"
        rx="10"
        fill="rgba(245,241,234,0.03)"
        stroke="rgba(245,241,234,0.16)"
        strokeDasharray="4 4"
      />
      <text
        x="358"
        y="42"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        consumer
      </text>
      <rect
        x="360"
        y="58"
        width="96"
        height="36"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="408"
        y="80"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        inbox dedupe
      </text>
      <rect
        x="360"
        y="108"
        width="96"
        height="36"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="408"
        y="130"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#f5f0ea"
      >
        handler
      </text>
      <text
        x="358"
        y="170"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        exactly-once processing
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Inline reusable bits for the epilogue dt/dd grid.
// -----------------------------------------------------------------------------

interface FactProps {
  readonly term: string;
  readonly value: string;
}

function Fact({ term, value }: FactProps) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="text-cc-ink-dim text-caption font-mono tracking-[0.2em] uppercase">
        {term}
      </dt>
      <dd className="text-cc-heading font-heading text-lead">{value}</dd>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function MochaPreviewV5() {
  return (
    <div className="relative mx-auto w-full max-w-2xl px-6 lg:max-w-3xl">
      {/* Continuous left-edge thread: 1px hairline running unbroken from hero
          to the closing CTA. The CTA section overlays the SPECTRUM band on
          top of this thread, which is the page's single chromatic event. */}
      <div
        aria-hidden
        className="border-cc-card-border pointer-events-none absolute top-0 bottom-0 left-0 border-l"
      />

      {/* PROLOGUE / hero. Same column, made taller, opens on whitespace and
          drops the title low. The CTAs sit inline at the end of the prose,
          not as a separate hero block. */}
      <section
        aria-labelledby="prologue-title"
        className="flex min-h-[80vh] scroll-mt-24 flex-col justify-end py-24 sm:py-28"
      >
        <ChapterMarker index="00" label="Prologue / Mocha messaging .NET" />
        <h1
          id="prologue-title"
          className="text-cc-heading font-heading text-hero mt-6 font-semibold tracking-tight text-balance"
        >
          Why are mediator and bus two different libraries?
        </h1>
        <div className="text-cc-ink-dim text-lead mt-8 space-y-8">
          <p>
            Most .NET teams reach for two separate libraries the moment a
            workload outgrows a single process. A mediator for the in-process
            CQRS path inside one service, a bus for everything that has to cross
            the wire to another. Two abstractions, two configuration stories,
            two ways to register a handler, two ways to write a test.
          </p>
          <p>
            Mocha is one source-generated framework that covers both. A Roslyn
            source generator discovers handlers and sagas at compile time and
            emits typed registration plus pre-compiled pipeline delegates, so
            the same handler shape carries a message whether it stays in-process
            or crosses a transport. The rest of this page is the argument, laid
            out movement by movement.
          </p>
          <div className="flex flex-wrap gap-3 pt-2">
            <SolidButton href="/docs/mocha">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>

      {/* MOVEMENT 01 / The two worlds */}
      <Movement
        id="movement-01"
        index="01"
        label="01 / The two worlds"
        title="One handler-first model spans both."
      >
        <p>
          The in-process mediator and the cross-service bus are usually framed
          as different problems. The mediator routes a command, query, or
          notification through a pipeline inside one process. The bus moves a
          message between processes through RabbitMQ, Postgres, Kafka, Azure
          Service Bus, or Event Hub. Different code shapes, different mental
          models, different tests.
        </p>
        <p>
          Mocha takes both at face value but unifies the surface. You inject
          ISender, IPublisher, or IRequestClient, and dispatch. The handler is
          the same shape on both sides of the transport. The framework decides
          where the message goes; one handler-first model spans both.
        </p>
        <VisualFrame caption="same handler shape, two dispatch boundaries">
          <MediatorAndBusDiagram />
        </VisualFrame>
      </Movement>

      {/* MOVEMENT 02 / Compile, don't reflect */}
      <Movement
        id="movement-02"
        index="02"
        label="02 / Compile, don't reflect"
        title="Compile, don't reflect."
      >
        <p>
          Reflection is the default tax most .NET messaging libraries pay at
          startup, sometimes on every dispatch. Mocha avoids that tax. A Roslyn
          source generator discovers every handler, consumer, and saga across
          your assemblies and emits typed registration plus pre-compiled
          pipeline delegates. No MakeGenericType, no service-provider lookups on
          the hot path, no runtime emit.
        </p>
        <CreateReviewHandlerSnippet />
        <PullQuote>
          The pipeline you ship is the pipeline the compiler built.
        </PullQuote>
        <p>
          The generator works per assembly and emits a typed AddReviews()-style
          registration call, so wiring is just one line in your composition
          root. Middleware is composed at build time into a delegate per
          handler. AOT-friendly by construction.
        </p>
        <VisualFrame caption="discovered at compile time, emitted as typed registration">
          <SourceGenDiagram />
        </VisualFrame>
      </Movement>

      {/* MOVEMENT 03 / Pick a broker, swap it later */}
      <Movement
        id="movement-03"
        index="03"
        label="03 / Pick a broker, swap it later"
        title="Pick a broker, swap it later."
      >
        <p>
          The transport is a choice you should be able to defer and revisit.
          Mocha ships RabbitMQ, PostgreSQL, and in-process as first-class
          transports, with Kafka, Azure Service Bus, and Azure Event Hub in
          source. Register a default transport, route specific message types
          through a different one, or run several side by side. The handlers do
          not change when the transport does.
        </p>
        <VisualFrame caption="one bus surface, many wires">
          <TransportsDiagram />
        </VisualFrame>
        <p className="text-cc-ink-dim text-caption font-mono tracking-[0.2em] uppercase">
          rabbit / postgres / in-process / kafka / azure sb / event hub
        </p>
      </Movement>

      {/* MOVEMENT 04 / Sagas that won't compile if broken */}
      <Movement
        id="movement-04"
        index="04"
        label="04 / Sagas validated before traffic"
        title="A saga that would get stuck never gets past startup."
      >
        <p>
          Sagas are where messaging systems quietly accumulate dead ends. Mocha
          asks you to declare the state machine, states, triggers, transitions,
          compensations, and then validates it at startup, before the service
          handles its first request. Every state has to be reachable, every path
          has to lead to a final state, every trigger you handle must be one the
          saga can actually receive.
        </p>
        <VisualFrame caption="Draft, Checked, Published, with compensation paths">
          <SagaDiagram />
        </VisualFrame>
        <PullQuote>
          A saga that would get stuck never gets past startup.
        </PullQuote>
      </Movement>

      {/* MOVEMENT 05 / Exactly-once, the boring way */}
      <Movement
        id="movement-05"
        index="05"
        label="05 / Exactly-once, the boring way"
        title="Exactly-once processing, via outbox and idempotent inbox."
      >
        <p>
          Exactly-once delivery is a fairy tale. Exactly-once processing is not.
          The transactional outbox writes your domain change and the message to
          dispatch in the same database transaction, so a crash between the two
          cannot happen. On the receive side, an idempotent inbox records the
          message id and skips duplicates, so a redelivery is harmless. Each
          message is processed exactly once.
        </p>
        <VisualFrame caption="outbox commits with the row, inbox dedupes on receive">
          <OutboxInboxDiagram />
        </VisualFrame>
        <p className="text-cc-heading font-heading text-h3 font-semibold tracking-tight">
          Commit together, or not at all.
        </p>
      </Movement>

      {/* MOVEMENT 06 / Every hop a span */}
      <Movement
        id="movement-06"
        index="06"
        label="06 / Every hop a span"
        title="OpenTelemetry-native, every publish and consume is a real span."
      >
        <p>
          Mocha emits structured OpenTelemetry traces and metrics for every
          dispatch, send, and handler execution, with correlation propagated
          across service boundaries. The same trace shows the publisher, the
          transport hop, and the consumer. Configure Nitro and you see the flow
          end to end without writing trace code yourself.
        </p>
        <VisualFrame caption="one OTLP exporter, publisher and consumer in the same trace">
          <div className="bg-cc-surface overflow-hidden rounded-lg">
            <NitroTrace />
          </div>
        </VisualFrame>
      </Movement>

      {/* EPILOGUE / MIT, in the open */}
      <Movement
        id="epilogue"
        index="07"
        label="07 / MIT, in the open"
        title="MIT, in the open."
      >
        <p>
          Mocha is released under the MIT license. Use it in commercial work,
          fork it, vendor it, audit it. The codebase, the issue tracker, the
          roadmap, and the release notes all live on GitHub alongside the rest
          of the ChilliCream platform.
        </p>
        <p>
          No telemetry-by-default, no key servers to phone home, no separate
          enterprise SKU hiding the features that matter. The framework on
          GitHub is the framework you ship.
        </p>
        <dl className="border-cc-card-border mt-2 grid grid-cols-1 gap-x-10 gap-y-6 border-t pt-8 sm:grid-cols-2">
          <Fact term="License" value="MIT" />
          <Fact term="Runtime" value=".NET / ASP.NET Core" />
          <Fact term="Dispatch" value="Source-generated" />
          <Fact term="Transports" value="Rabbit / PG / mem" />
          <Fact term="Reliability" value="Outbox + inbox" />
          <Fact term="Tracing" value="OpenTelemetry" />
        </dl>
      </Movement>

      {/* CLOSING CTA. The left-edge thread terminates into a SPECTRUM
          hairline, the single chromatic event of the page. */}
      <section
        aria-labelledby="cta-title"
        className="relative scroll-mt-24 py-28 sm:py-32"
      >
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
            Get started
          </span>
          <h2
            id="cta-title"
            className="text-cc-heading font-heading text-h2 mt-6 font-semibold tracking-tight text-balance"
          >
            <span className="block">Write a handler.</span>
            <span className="block">Attribute it.</span>
            <span className="block">Dispatch it.</span>
          </h2>
          <p className="text-cc-ink-dim text-lead mx-auto mt-8">
            The source generator handles registration and the pipeline; the
            transport, the outbox, the inbox, the sagas, and the traces are part
            of the framework, not bolt-on packages you wire yourself.
          </p>
          <div className="mt-10 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/mocha">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </div>
  );
}
