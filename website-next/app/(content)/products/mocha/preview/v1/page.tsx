import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroTrace } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Mocha: Source-Generated Mediator and Message Bus for .NET",
  description:
    "Mocha is the open-source .NET messaging framework: an in-process mediator and a cross-service message bus with source-generated dispatch and reliable delivery.",
  keywords: [
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
    title: "Mocha: Mediator and Message Bus for .NET",
    description:
      "One source-generated framework for in-process CQRS and cross-service messaging on .NET. RabbitMQ, Postgres, in-process, outbox and inbox, sagas, traces.",
    type: "website",
  },
};

// Brand spectrum, allowed at most once per screen. Used on the closing CTA rule.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface IndexTagProps {
  readonly value: string;
}

function IndexTag({ value }: IndexTagProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex h-6 items-center justify-center rounded-full border px-2 font-mono text-[11px] tabular-nums">
      {value}
    </span>
  );
}

// -----------------------------------------------------------------------------
// Hero code card. Two stacked panels:
//   top    -> in-process mediator [Handler] for CreateReview -> ReviewCreated
//   bottom -> cross-service bus PublishAsync + IEventHandler<T>
// Same generated wiring covers both. The single color event in the hero is a
// soft teal pulse anchored on the shared [Handler] attribute.
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

// Token color helpers. GitHub-dark approximations, scoped to the snippets only
// so the rest of the page stays on cc-* tokens.
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
  dim: { color: "#8b949e" },
};

interface CodePanelProps {
  readonly file: string;
  readonly tag: string;
  readonly children: ReactNode;
  readonly footer: ReactNode;
}

function CodePanel({ file, tag, children, footer }: CodePanelProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border">
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
          {file}
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {tag}
        </span>
      </div>
      <div className="relative py-4">{children}</div>
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
        {footer}
      </div>
    </div>
  );
}

function HeroCodeStack() {
  return (
    <div className="relative">
      {/* The lone color event on the hero: a soft teal pulse anchored where the
          [Handler] attributes sit on both panels. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 200px at 14% 18%, rgba(94, 234, 212, 0.18), transparent 70%), radial-gradient(360px 200px at 14% 78%, rgba(22, 185, 228, 0.14), transparent 70%)",
        }}
      />
      <div className="relative flex flex-col gap-5 shadow-2xl">
        <CodePanel
          file="Reviews/CreateReviewHandler.cs"
          tag="C# / mediator"
          footer={
            <>
              <span>in-process: ISender.Send(CreateReview)</span>
              <span className="text-cc-accent">source-generated dispatch</span>
            </>
          }
        >
          <CodeLine n={1}>
            <span style={C.kw}>using</span> <span style={C.plain}>Mocha;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={C.plain}>&nbsp;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={C.kw}>namespace</span>{" "}
            <span style={C.plain}>Reviews;</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={C.plain}>&nbsp;</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={C.kw}>public record</span>{" "}
            <span style={C.type}>CreateReview</span>
            <span style={C.punct}>(</span>
            <span style={C.type}>Guid</span>{" "}
            <span style={C.param}>ProductId</span>
            <span style={C.punct}>, </span>
            <span style={C.type}>string</span> <span style={C.param}>Text</span>
            <span style={C.punct}>)</span>
            <span style={C.plain}>{` : `}</span>
            <span style={C.type}>ICommand</span>
            <span style={C.punct}>{`<`}</span>
            <span style={C.type}>Guid</span>
            <span style={C.punct}>{`>;`}</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={C.plain}>&nbsp;</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={C.comment}>
              {`// Source-generated registration, pre-compiled pipeline.`}
            </span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={C.punct}>[</span>
            <span style={C.attr}>Handler</span>
            <span style={C.punct}>]</span>
          </CodeLine>
          <CodeLine n={9}>
            <span style={C.kw}>public static async</span>{" "}
            <span style={C.type}>Task</span>
            <span style={C.punct}>{`<`}</span>
            <span style={C.type}>Guid</span>
            <span style={C.punct}>{`>`}</span>{" "}
            <span style={C.fn}>HandleAsync</span>
            <span style={C.punct}>(</span>
          </CodeLine>
          <CodeLine n={10}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.type}>CreateReview</span>{" "}
            <span style={C.param}>command</span>
            <span style={C.punct}>,</span>
          </CodeLine>
          <CodeLine n={11}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.type}>ReviewsDbContext</span>{" "}
            <span style={C.param}>db</span>
            <span style={C.punct}>,</span>
          </CodeLine>
          <CodeLine n={12}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.type}>IPublisher</span>{" "}
            <span style={C.param}>bus</span>
            <span style={C.punct}>,</span>
          </CodeLine>
          <CodeLine n={13}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.type}>CancellationToken</span>{" "}
            <span style={C.param}>ct</span>
            <span style={C.punct}>{`)`}</span>
          </CodeLine>
          <CodeLine n={14}>
            <span style={C.punct}>{`{`}</span>
          </CodeLine>
          <CodeLine n={15}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.kw}>var</span> <span style={C.param}>review</span>{" "}
            <span style={C.punct}>=</span> <span style={C.type}>Review</span>
            <span style={C.punct}>.</span>
            <span style={C.fn}>Draft</span>
            <span style={C.punct}>(</span>
            <span style={C.param}>command</span>
            <span style={C.punct}>);</span>
          </CodeLine>
          <CodeLine n={16}>
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
          <CodeLine n={17}>
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
          <CodeLine n={18}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.kw}>return</span> <span style={C.param}>review</span>
            <span style={C.punct}>.</span>
            <span style={C.plain}>Id</span>
            <span style={C.punct}>;</span>
          </CodeLine>
          <CodeLine n={19}>
            <span style={C.punct}>{`}`}</span>
          </CodeLine>
        </CodePanel>

        <CodePanel
          file="Search/ReviewCreatedHandler.cs"
          tag="C# / bus"
          footer={
            <>
              <span>cross-service: RabbitMQ, Postgres, in-process</span>
              <span className="text-cc-accent">same handler shape</span>
            </>
          }
        >
          <CodeLine n={1}>
            <span style={C.kw}>using</span> <span style={C.plain}>Mocha;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={C.plain}>&nbsp;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={C.kw}>namespace</span>{" "}
            <span style={C.plain}>Search;</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={C.plain}>&nbsp;</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={C.comment}>
              {`// Received from the bus, processed exactly once via inbox.`}
            </span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={C.punct}>[</span>
            <span style={C.attr}>Handler</span>
            <span style={C.punct}>]</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={C.kw}>public static async</span>{" "}
            <span style={C.type}>Task</span>{" "}
            <span style={C.fn}>HandleAsync</span>
            <span style={C.punct}>(</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.type}>ReviewCreated</span>{" "}
            <span style={C.param}>evt</span>
            <span style={C.punct}>,</span>
          </CodeLine>
          <CodeLine n={9}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.type}>ISearchIndex</span>{" "}
            <span style={C.param}>index</span>
            <span style={C.punct}>,</span>
          </CodeLine>
          <CodeLine n={10}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.type}>CancellationToken</span>{" "}
            <span style={C.param}>ct</span>
            <span style={C.punct}>{`) =>`}</span>
          </CodeLine>
          <CodeLine n={11}>
            <span style={C.plain}>{`    `}</span>
            <span style={C.kw}>await</span> <span style={C.param}>index</span>
            <span style={C.punct}>.</span>
            <span style={C.fn}>UpsertAsync</span>
            <span style={C.punct}>(</span>
            <span style={C.param}>evt</span>
            <span style={C.punct}>.</span>
            <span style={C.plain}>ReviewId</span>
            <span style={C.punct}>, </span>
            <span style={C.param}>ct</span>
            <span style={C.punct}>);</span>
          </CodeLine>
        </CodePanel>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Feature row. Copy column + inline SVG/markup visual column.
// -----------------------------------------------------------------------------

interface FeatureRowProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

function FeatureRow({
  id,
  index,
  eyebrow,
  title,
  body,
  bullets,
  visual,
  reverse = false,
}: FeatureRowProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <div
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex items-center gap-3">
            <IndexTag value={index} />
            <Eyebrow>{eyebrow}</Eyebrow>
          </div>
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            {title}
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            {body}
          </p>
          <ul className="mt-6 flex flex-col gap-2.5">
            {bullets.map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon size={14} />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </div>
        <div
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
        >
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 sm:p-6">
            {visual}
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Inline diagrams. All hand-built TSX/SVG with cc-* aligned colors.
// -----------------------------------------------------------------------------

/** Mediator + bus diagram: one programming model, two dispatch boundaries. */
function MediatorAndBusDiagram() {
  return (
    <svg
      viewBox="0 0 480 240"
      className="h-auto w-full"
      role="img"
      aria-label="One handler-first model dispatches in-process via mediator and across services via the bus"
    >
      {/* Process boundary */}
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
        stroke="rgba(22,185,228,0.6)"
        strokeWidth="1.4"
        fill="none"
      />

      {/* Transport in the middle */}
      <rect
        x="232"
        y="138"
        width="68"
        height="36"
        rx="6"
        fill="rgba(22,185,228,0.08)"
        stroke="rgba(22,185,228,0.55)"
      />
      <text
        x="266"
        y="153"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10.5"
        fill="#16b9e4"
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
        stroke="rgba(22,185,228,0.55)"
        strokeWidth="1.4"
        fill="none"
      />

      {/* Process boundary B */}
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

      <text
        x="22"
        y="222"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        same handler-first model, two dispatch boundaries
      </text>
    </svg>
  );
}

/** Source generator diagram. Build step turns handlers into typed registrations. */
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
      <text
        x="12"
        y="200"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        zero reflection, zero MakeGenericType at runtime
      </text>
    </svg>
  );
}

/** Pluggable transports diagram. */
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

/** Saga state diagram: Draft -> Checked -> Published, validated before traffic. */
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

/** Outbox + inbox diagram: DB commit and dispatch atomic, dedupe on receive. */
function OutboxInboxDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Transactional outbox commits with the database, idempotent inbox deduplicates on receive"
    >
      {/* DB tx */}
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

      {/* Dispatcher */}
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
        fill="rgba(22,185,228,0.08)"
        stroke="rgba(22,185,228,0.55)"
      />
      <text
        x="274"
        y="128"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#16b9e4"
      >
        dispatcher
      </text>
      <path
        d="M 308 124 L 348 124"
        stroke="rgba(22,185,228,0.55)"
        strokeWidth="1.4"
        fill="none"
      />

      {/* Inbox */}
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
// Stat tile used in the MIT band.
// -----------------------------------------------------------------------------

interface ProofItemProps {
  readonly label: string;
  readonly value: string;
}

function ProofItem({ label, value }: ProofItemProps) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-cc-heading font-heading text-2xl font-semibold tracking-tight">
        {value}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
        {label}
      </span>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function MochaPreviewV1() {
  return (
    <>
      {/* HERO: confident split layout. Copy left, two stacked code panels right. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>Messaging framework for .NET</Eyebrow>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              In-process mediator AND cross-service bus, in one framework.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Mocha is the open-source .NET messaging framework that covers both
              in-process CQRS and inter-service messaging behind one
              handler-first model. A Roslyn source generator discovers handlers
              and sagas at compile time and emits typed registration plus
              pre-compiled pipeline delegates. Same code shape whether a message
              stays in-process or crosses a transport.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/mocha">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
            <dl className="border-cc-card-border mt-10 grid grid-cols-3 gap-6 border-t pt-6">
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  License
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">MIT</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Runtime
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">
                  .NET / ASP.NET Core
                </dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Dispatch
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">Source-generated</dd>
              </div>
            </dl>
          </div>
          <div className="lg:col-span-7">
            <HeroCodeStack />
          </div>
        </div>
      </section>

      {/* Quick "what you get" strip. */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-6">
          {[
            "Mediator + bus",
            "Source-generated dispatch",
            "Pluggable transports",
            "Validated sagas",
            "Outbox + inbox",
            "Every hop a span",
          ].map((label) => (
            <li
              key={label}
              className="text-cc-ink flex items-center gap-2 font-mono text-[11.5px] tracking-tight uppercase"
            >
              <span className="text-cc-accent" aria-hidden>
                <CheckIcon size={12} />
              </span>
              {label}
            </li>
          ))}
        </ul>
      </section>

      {/* SIX feature rows, alternating sides. */}
      <FeatureRow
        id="mediator-and-bus"
        index="01"
        eyebrow="Mediator AND bus"
        title="One handler-first model spans in-process and cross-service."
        body="Inject ISender, IPublisher, or IRequestClient and dispatch a command, event, or request. The exact same handler shape that powers an in-process mediator call also powers a cross-service consumer, so a message that starts in one service and crosses a transport to another keeps the same code shape on both sides."
        bullets={[
          "ICommand, IQuery, INotification for in-process CQRS through the mediator.",
          "PublishAsync, SendAsync, RequestAsync over the bus to other services.",
          "IBatchEventHandler for batched consumers when throughput matters more than latency.",
        ]}
        visual={<MediatorAndBusDiagram />}
      />

      <FeatureRow
        id="source-generated-dispatch"
        index="02"
        eyebrow="Source-generated dispatch"
        title="A Roslyn source generator wires every handler at compile time."
        body="The Mocha analyzer discovers handlers, consumers, and sagas across your assemblies and emits typed registration plus pre-compiled pipeline delegates. No MakeGenericType, no service-provider lookups on the hot path, no reflection at runtime. The pipeline you ship is the pipeline the compiler built."
        bullets={[
          "Typed AddReviews()-style registration emitted per assembly, no manual wiring.",
          "Middleware composed at build time into a delegate per handler.",
          "AOT-friendly: no runtime emit, no dynamic code, no MakeGenericType.",
        ]}
        visual={<SourceGenDiagram />}
        reverse
      />

      <FeatureRow
        id="transports"
        index="03"
        eyebrow="Pluggable transports"
        title="Pick the broker, swap brokers, run more than one at once."
        body="RabbitMQ, PostgreSQL, and in-process ship as first-class transports. Kafka, Azure Service Bus, and Azure Event Hub ship in source. Register a default transport, route specific message types through a different one, or run multiple transports side by side. The handlers do not change when the transport does."
        bullets={[
          "RabbitMQ for topic and queue routing, Postgres for durable + outbox in one store.",
          "In-process transport for local development and tests, same code as production.",
          "Per-message routing rules let high-volume topics use a different broker.",
        ]}
        visual={<TransportsDiagram />}
      />

      <FeatureRow
        id="sagas"
        index="04"
        eyebrow="Validated sagas"
        title="Sagas are checked before the service handles its first request."
        body="Define a state machine: states, triggers, transitions, compensations. At startup Mocha validates that every state is reachable, every path leads to a final state, and every trigger you handle is one the saga can receive. A saga that would get stuck or drop a message never gets past startup."
        bullets={[
          "Draft -> Checked -> Published, with compensation paths on failure.",
          "Persisted state across hops, scoped to a correlation key.",
          "Validated before the service handles traffic, never silently broken in prod.",
        ]}
        visual={<SagaDiagram />}
        reverse
      />

      <FeatureRow
        id="reliability"
        index="05"
        eyebrow="Outbox + inbox"
        title="Exactly-once processing, the boring way: outbox plus idempotent inbox."
        body="The transactional outbox commits your domain change and the message to dispatch in the same database transaction, so a crash never loses messages. On the receive side, an idempotent inbox records the message id and skips duplicates so each message is processed exactly once, even when the broker redelivers."
        bullets={[
          "Transactional outbox on Postgres (and EF Core), wired through your DbContext.",
          "Idempotent inbox dedupes on the consumer side without extra application code.",
          "Per-exception retry, redelivery, dead-letter, circuit breaker, concurrency limiter.",
        ]}
        visual={<OutboxInboxDiagram />}
      />

      <FeatureRow
        id="observability"
        index="06"
        eyebrow="Every hop a span"
        title="OpenTelemetry-native, every publish and consume is a real span."
        body="Mocha emits structured OpenTelemetry traces and metrics for every dispatch, send, and handler execution, with correlation propagated across service boundaries. The same trace shows the publisher, the transport hop, and the consumer. When the observer is off, the cost is a no-op check."
        bullets={[
          "Every PublishAsync, transport hop, and handler invocation is a span.",
          "Correlation ids propagate across services automatically.",
          "Configure one OTLP exporter, see the flow end to end in your existing backend.",
        ]}
        visual={
          <div className="bg-cc-surface relative overflow-hidden rounded-lg">
            <NitroTrace />
          </div>
        }
        reverse
      />

      {/* MIT / open source proof band. */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>MIT licensed</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Open source. Free to use. Built in the open.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Mocha is released under the MIT license. Use it in commercial
              work, fork it, vendor it, audit it. The codebase, the issue
              tracker, the roadmap, and the release notes all live on GitHub
              alongside the rest of the ChilliCream platform.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/mocha">Read the docs</OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Runtime" value=".NET / ASP.NET Core" />
              <ProofItem label="Dispatch" value="Source-generated" />
              <ProofItem label="Transports" value="Rabbit / PG / mem" />
              <ProofItem label="Reliability" value="Outbox + inbox" />
              <ProofItem label="Tracing" value="OpenTelemetry" />
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA. The single brand-spectrum hairline lives here. */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            Stop choosing between a mediator and a bus.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            Write a handler, attribute it, dispatch it. The source generator
            handles registration and the pipeline. The transport, the outbox,
            the inbox, the sagas, and the traces are part of the framework, not
            bolt-on packages you wire yourself.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/mocha">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
