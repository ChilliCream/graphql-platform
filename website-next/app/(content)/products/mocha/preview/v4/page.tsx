import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroTrace } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Mocha: Source-Generated Mediator and Message Bus for .NET",
  description:
    "Mocha messaging .NET: a source-generated mediator and bus with validated sagas and exactly-once processing for in-process CQRS and cross-service messaging.",
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
      "Mocha messaging .NET: source-generated mediator and bus, validated sagas, exactly-once processing, OpenTelemetry on every hop.",
    type: "website",
  },
};

// Brand spectrum, used exactly once on the closing CTA hairline.
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
// The Constellation: the hero motif. A labeled star-map of one handler-first
// model, with thin cc-accent strokes and small filled dots at endpoints.
// -----------------------------------------------------------------------------

interface ConstellationNode {
  readonly id: string;
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly kind: "handler" | "interface" | "transport" | "saga" | "store";
}

interface ConstellationEdge {
  readonly from: string;
  readonly to: string;
}

const HERO_NODES: readonly ConstellationNode[] = [
  { id: "cmd", x: 110, y: 130, label: "ISender", kind: "interface" },
  {
    id: "h1",
    x: 240,
    y: 90,
    label: "[Handler] CreateReview",
    kind: "handler",
  },
  { id: "pub", x: 120, y: 240, label: "IPublisher", kind: "interface" },
  { id: "ob", x: 250, y: 240, label: "outbox", kind: "store" },
  { id: "tr", x: 380, y: 240, label: "transport", kind: "transport" },
  { id: "in", x: 510, y: 240, label: "inbox", kind: "store" },
  {
    id: "h2",
    x: 620,
    y: 200,
    label: "[Handler] ReviewCreated",
    kind: "handler",
  },
  {
    id: "sg",
    x: 470,
    y: 110,
    label: "[Saga] PublishFlow",
    kind: "saga",
  },
];

const HERO_EDGES: readonly ConstellationEdge[] = [
  { from: "cmd", to: "h1" },
  { from: "h1", to: "pub" },
  { from: "h1", to: "sg" },
  { from: "pub", to: "ob" },
  { from: "ob", to: "tr" },
  { from: "tr", to: "in" },
  { from: "in", to: "h2" },
  { from: "sg", to: "tr" },
];

function nodeFill(kind: ConstellationNode["kind"]): string {
  switch (kind) {
    case "handler":
      return "rgba(94,234,212,0.10)";
    case "saga":
      return "rgba(94,234,212,0.08)";
    case "transport":
      return "rgba(94,234,212,0.08)";
    case "store":
      return "rgba(245,241,234,0.04)";
    default:
      return "rgba(245,241,234,0.04)";
  }
}

function nodeStroke(kind: ConstellationNode["kind"]): string {
  switch (kind) {
    case "handler":
      return "rgba(94,234,212,0.6)";
    case "saga":
      return "rgba(94,234,212,0.45)";
    case "transport":
      return "rgba(94,234,212,0.55)";
    default:
      return "rgba(245,241,234,0.20)";
  }
}

function nodeText(kind: ConstellationNode["kind"]): string {
  switch (kind) {
    case "handler":
    case "saga":
    case "transport":
      return "#5eead4";
    default:
      return "rgba(245,241,234,0.78)";
  }
}

function HeroConstellation() {
  const lookup = new Map(HERO_NODES.map((n) => [n.id, n]));
  return (
    <div className="relative">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 280px at 55% 50%, rgba(94, 234, 212, 0.14), transparent 70%), radial-gradient(360px 240px at 22% 70%, rgba(22, 185, 228, 0.10), transparent 70%)",
        }}
      />
      <svg
        viewBox="0 0 720 360"
        className="relative h-auto w-full"
        role="img"
        aria-label="One handler-first messaging constellation: ISender and IPublisher dispatch to handlers and a saga, an outbox bridges to a transport, and an inbox dedupes on the consumer side."
      >
        <defs>
          <radialGradient id="cc-star-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor="rgba(94,234,212,0.55)" />
            <stop offset="100%" stopColor="rgba(94,234,212,0)" />
          </radialGradient>
        </defs>

        {/* Faint star-field dots */}
        {[
          [40, 50],
          [90, 320],
          [200, 30],
          [330, 60],
          [420, 320],
          [560, 60],
          [660, 90],
          [690, 300],
          [60, 200],
          [380, 180],
        ].map(([cx, cy]) => (
          <circle
            key={`${cx}-${cy}`}
            cx={cx}
            cy={cy}
            r="1.2"
            fill="rgba(245,241,234,0.22)"
          />
        ))}

        {/* Edges */}
        {HERO_EDGES.map((e) => {
          const a = lookup.get(e.from);
          const b = lookup.get(e.to);
          if (!a || !b) return null;
          return (
            <g key={`${e.from}-${e.to}`}>
              <line
                x1={a.x}
                y1={a.y}
                x2={b.x}
                y2={b.y}
                stroke="rgba(94,234,212,0.45)"
                strokeWidth="1.1"
              />
              <circle cx={a.x} cy={a.y} r="2.2" fill="#5eead4" />
              <circle cx={b.x} cy={b.y} r="2.2" fill="#5eead4" />
            </g>
          );
        })}

        {/* Glow on the central [Handler] node */}
        <circle cx="240" cy="90" r="34" fill="url(#cc-star-glow)" />

        {/* Nodes */}
        {HERO_NODES.map((n) => {
          const w = n.label.length * 6.6 + 22;
          const h = 26;
          return (
            <g key={n.id}>
              <rect
                x={n.x - w / 2}
                y={n.y - h / 2}
                width={w}
                height={h}
                rx="6"
                fill={nodeFill(n.kind)}
                stroke={nodeStroke(n.kind)}
              />
              <text
                x={n.x}
                y={n.y + 4}
                textAnchor="middle"
                fontFamily="ui-monospace, monospace"
                fontSize="11"
                fill={nodeText(n.kind)}
              >
                {n.label}
              </text>
            </g>
          );
        })}

        <text
          x="20"
          y="340"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.5)"
        >
          one handler-first model, charted across mediator, bus, saga, outbox,
          inbox
        </text>
      </svg>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Card header ornament: a miniature constellation glyph that echoes the hero.
// -----------------------------------------------------------------------------

interface CardGlyphProps {
  readonly variant: 1 | 2 | 3 | 4 | 5;
}

function CardGlyph({ variant }: CardGlyphProps) {
  const sets: Record<number, ReadonlyArray<readonly [number, number]>> = {
    1: [
      [8, 20],
      [22, 8],
      [22, 32],
      [38, 20],
    ],
    2: [
      [6, 12],
      [6, 28],
      [20, 20],
      [34, 12],
      [34, 28],
    ],
    3: [
      [6, 20],
      [20, 20],
      [34, 20],
    ],
    4: [
      [8, 14],
      [22, 26],
      [36, 14],
    ],
    5: [
      [6, 10],
      [18, 22],
      [30, 10],
      [42, 22],
    ],
  };
  const points = sets[variant];
  return (
    <svg
      width="56"
      height="40"
      viewBox="0 0 48 40"
      aria-hidden
      className="shrink-0"
    >
      {points.map((p, i) => {
        const next = points[i + 1];
        if (!next) return null;
        return (
          <line
            key={`${p[0]}-${p[1]}`}
            x1={p[0]}
            y1={p[1]}
            x2={next[0]}
            y2={next[1]}
            stroke="rgba(94,234,212,0.55)"
            strokeWidth="1"
          />
        );
      })}
      {points.map((p) => (
        <circle
          key={`d-${p[0]}-${p[1]}`}
          cx={p[0]}
          cy={p[1]}
          r="2"
          fill="#5eead4"
        />
      ))}
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Constellation card wrapper. Calm cc-surface card with the mini glyph header.
// -----------------------------------------------------------------------------

interface CardProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly glyph: 1 | 2 | 3 | 4 | 5;
  readonly children?: ReactNode;
}

function ConstellationCard({
  id,
  index,
  eyebrow,
  title,
  body,
  glyph,
  children,
}: CardProps) {
  return (
    <section id={id} className="scroll-mt-24 py-20 sm:py-24">
      <div className="border-cc-card-border bg-cc-surface rounded-2xl border p-8 sm:p-10">
        <div className="flex items-start gap-5">
          <CardGlyph variant={glyph} />
          <div className="flex-1">
            <div className="flex items-center gap-3">
              <IndexTag value={index} />
              <Eyebrow>{eyebrow}</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading text-h3 mt-4 font-semibold tracking-tight text-balance">
              {title}
            </h2>
            <p className="text-cc-prose text-lead mt-4 max-w-3xl">{body}</p>
          </div>
        </div>
        {children ? <div className="mt-8">{children}</div> : null}
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Card sub-diagrams. Each reuses the node-and-line motif.
// -----------------------------------------------------------------------------

function MediatorBusTwin() {
  return (
    <svg
      viewBox="0 0 480 140"
      className="h-auto w-full"
      role="img"
      aria-label="Mediator and bus twin nodes share the same [Handler] shape."
    >
      <rect
        x="40"
        y="44"
        width="140"
        height="52"
        rx="10"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="110"
        y="68"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        ISender
      </text>
      <text
        x="110"
        y="84"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        in-process mediator
      </text>

      <line
        x1="180"
        y1="70"
        x2="300"
        y2="70"
        stroke="rgba(94,234,212,0.45)"
        strokeWidth="1.2"
      />
      <circle cx="180" cy="70" r="2.2" fill="#5eead4" />
      <circle cx="240" cy="70" r="2.2" fill="#5eead4" />
      <text
        x="240"
        y="58"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        same [Handler] shape
      </text>
      <circle cx="300" cy="70" r="2.2" fill="#5eead4" />

      <rect
        x="300"
        y="44"
        width="140"
        height="52"
        rx="10"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="370"
        y="68"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        IPublisher
      </text>
      <text
        x="370"
        y="84"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        cross-service bus
      </text>
    </svg>
  );
}

function SourceGenMini() {
  return (
    <svg
      viewBox="0 0 480 200"
      className="h-auto w-full"
      role="img"
      aria-label="Source generator discovers handlers and emits typed registration."
    >
      {[
        { y: 20, label: "[Handler] CreateReview" },
        { y: 70, label: "[Handler] ReviewCreated" },
        { y: 120, label: "[Saga] PublishFlow" },
      ].map((row) => (
        <g key={row.label}>
          <rect
            x="20"
            y={row.y}
            width="170"
            height="32"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.45)"
          />
          <text
            x="32"
            y={row.y + 20}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#f5f0ea"
          >
            {row.label}
          </text>
          <line
            x1="190"
            y1={row.y + 16}
            x2="280"
            y2="92"
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.1"
          />
          <circle cx="190" cy={row.y + 16} r="2.2" fill="#5eead4" />
        </g>
      ))}

      <rect
        x="280"
        y="68"
        width="100"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="330"
        y="88"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        Roslyn
      </text>
      <text
        x="330"
        y="104"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        compile time
      </text>
      <circle cx="280" cy="92" r="2.2" fill="#5eead4" />
      <line
        x1="380"
        y1="92"
        x2="430"
        y2="92"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.2"
      />
      <circle cx="380" cy="92" r="2.2" fill="#5eead4" />
      <rect
        x="370"
        y="138"
        width="90"
        height="38"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.20)"
      />
      <text
        x="415"
        y="154"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.78)"
      >
        AddReviews()
      </text>
      <text
        x="415"
        y="168"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.55)"
      >
        typed registration
      </text>
      <line
        x1="430"
        y1="92"
        x2="415"
        y2="138"
        stroke="rgba(94,234,212,0.45)"
        strokeWidth="1.1"
      />
      <circle cx="430" cy="92" r="2.2" fill="#5eead4" />
    </svg>
  );
}

function SagaStateMini() {
  const states = [
    { x: 40, label: "Draft", active: false },
    { x: 200, label: "Checked", active: true },
    { x: 360, label: "Published", active: false },
  ];
  return (
    <svg
      viewBox="0 0 480 160"
      className="h-auto w-full"
      role="img"
      aria-label="Saga states Draft, Checked, Published validated before traffic."
    >
      {states.map((s, i) => (
        <g key={s.label}>
          <rect
            x={s.x}
            y="50"
            width="100"
            height="44"
            rx="10"
            fill={s.active ? "rgba(94,234,212,0.10)" : "rgba(245,241,234,0.04)"}
            stroke={
              s.active ? "rgba(94,234,212,0.6)" : "rgba(245,241,234,0.22)"
            }
          />
          <text
            x={s.x + 50}
            y="76"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="12"
            fill={s.active ? "#5eead4" : "#f5f0ea"}
          >
            {s.label}
          </text>
          {i < states.length - 1 ? (
            <g>
              <line
                x1={s.x + 100}
                y1="72"
                x2={s.x + 160}
                y2="72"
                stroke="rgba(94,234,212,0.5)"
                strokeWidth="1.2"
              />
              <circle cx={s.x + 100} cy="72" r="2.2" fill="#5eead4" />
              <circle cx={s.x + 160} cy="72" r="2.2" fill="#5eead4" />
            </g>
          ) : null}
        </g>
      ))}
      <text
        x="40"
        y="130"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        validated at startup, before the service handles traffic
      </text>
    </svg>
  );
}

function OutboxInboxMini() {
  return (
    <svg
      viewBox="0 0 480 160"
      className="h-auto w-full"
      role="img"
      aria-label="Reviews row, outbox and inbox dedupe form a three-node reliability path."
    >
      <rect
        x="30"
        y="56"
        width="120"
        height="40"
        rx="8"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.22)"
      />
      <text
        x="90"
        y="80"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#f5f0ea"
      >
        Reviews row
      </text>

      <line
        x1="150"
        y1="76"
        x2="200"
        y2="76"
        stroke="rgba(94,234,212,0.5)"
        strokeWidth="1.2"
      />
      <circle cx="150" cy="76" r="2.2" fill="#5eead4" />
      <circle cx="200" cy="76" r="2.2" fill="#5eead4" />

      <rect
        x="200"
        y="56"
        width="120"
        height="40"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="260"
        y="80"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        outbox
      </text>

      <line
        x1="320"
        y1="76"
        x2="370"
        y2="76"
        stroke="rgba(94,234,212,0.5)"
        strokeWidth="1.2"
      />
      <circle cx="320" cy="76" r="2.2" fill="#5eead4" />
      <circle cx="370" cy="76" r="2.2" fill="#5eead4" />

      <rect
        x="370"
        y="56"
        width="80"
        height="40"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="410"
        y="80"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        inbox
      </text>
      <text
        x="30"
        y="130"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        commit together, dedupe on receive, processed exactly once
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Bullet list with cc-accent checks.
// -----------------------------------------------------------------------------

interface BulletsProps {
  readonly items: readonly string[];
}

function Bullets({ items }: BulletsProps) {
  return (
    <ul className="mt-6 flex flex-col gap-2.5">
      {items.map((b) => (
        <li
          key={b}
          className="text-cc-ink text-body flex items-start gap-3 leading-relaxed"
        >
          <span className="text-cc-accent mt-1 shrink-0">
            <CheckIcon size={14} />
          </span>
          <span>{b}</span>
        </li>
      ))}
    </ul>
  );
}

// -----------------------------------------------------------------------------
// Proof tile used in the open-source band.
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
// Capability ribbon with the tiny constellation glyph prefix.
// -----------------------------------------------------------------------------

function RibbonGlyph() {
  return (
    <svg
      width="22"
      height="14"
      viewBox="0 0 22 14"
      aria-hidden
      className="shrink-0"
    >
      <line
        x1="2"
        y1="7"
        x2="11"
        y2="2"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1"
      />
      <line
        x1="11"
        y1="2"
        x2="20"
        y2="12"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1"
      />
      <circle cx="2" cy="7" r="1.6" fill="#5eead4" />
      <circle cx="11" cy="2" r="1.6" fill="#5eead4" />
      <circle cx="20" cy="12" r="1.6" fill="#5eead4" />
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function MochaPreviewV4() {
  return (
    <>
      {/* HERO: full-width centered copy stacked above a wide constellation banner. */}
      <section className="pt-16 pb-8 sm:pt-24 sm:pb-12">
        <div className="mx-auto max-w-3xl text-center">
          <Eyebrow>Messaging framework for .NET</Eyebrow>
          <h1 className="text-cc-heading font-heading text-hero mt-5 font-semibold tracking-tight text-balance">
            One model. Two boundaries.
          </h1>
          <p className="text-cc-prose text-lead mx-auto mt-6 max-w-2xl">
            Mocha is the open-source .NET messaging framework: a
            source-generated mediator and bus behind one handler-first model.
            Sagas validated before traffic, exactly-once processing on the
            consumer.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/mocha">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
        <div className="mt-14 sm:mt-20">
          <HeroConstellation />
        </div>
      </section>

      {/* Capability ribbon. */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <ul className="grid grid-cols-2 gap-x-6 gap-y-3 sm:grid-cols-3 lg:grid-cols-6">
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
              className="text-cc-ink text-caption flex items-center gap-2 font-mono tracking-widest uppercase"
            >
              <RibbonGlyph />
              {label}
            </li>
          ))}
        </ul>
      </section>

      {/* Centered single-column constellation cards. */}
      <div className="mx-auto max-w-5xl">
        <ConstellationCard
          id="mediator-and-bus"
          index="01"
          eyebrow="Same shape, two boundaries"
          title="One handler-first model spans in-process and cross-service."
          body="Inject ISender, IPublisher, or IRequestClient and dispatch a command, event, or request. The exact same handler shape that powers an in-process mediator call also powers a cross-service consumer, so a message that starts in one service and crosses a transport to another keeps the same code shape on both sides."
          glyph={1}
        >
          <Bullets
            items={[
              "ICommand, IQuery, INotification for in-process CQRS through the mediator.",
              "PublishAsync, SendAsync, RequestAsync over the bus to other services.",
              "IBatchEventHandler for batched consumers when throughput matters more than latency.",
            ]}
          />
          <div className="border-cc-card-border bg-cc-card-bg mt-8 rounded-xl border p-5 sm:p-6">
            <MediatorBusTwin />
          </div>
        </ConstellationCard>

        <ConstellationCard
          id="source-generated-dispatch"
          index="02"
          eyebrow="Compile-time wiring"
          title="A Roslyn source generator wires every handler at compile time."
          body="Every handler is a star plotted on the same chart at compile time. The Mocha analyzer walks your assemblies, places each handler, consumer, and saga on the map, and emits typed registration plus pre-compiled pipeline delegates from that fixed star chart. The pipeline you ship is the pipeline the compiler drew, no reflection redraws it at runtime."
          glyph={2}
        >
          <Bullets
            items={[
              "Typed AddReviews()-style registration emitted per assembly, no manual wiring.",
              "Middleware composed at build time into a delegate per handler.",
              "AOT-friendly: no runtime emit, no dynamic code, no MakeGenericType.",
            ]}
          />
          <div className="border-cc-card-border bg-cc-card-bg mt-8 rounded-xl border p-5 sm:p-6">
            <SourceGenMini />
          </div>
        </ConstellationCard>

        <ConstellationCard
          id="sagas"
          index="03"
          eyebrow="Validated sagas"
          title="Sagas are checked before the service handles its first request."
          body="Each saga is a sub-constellation: states, triggers, transitions, and compensations as nodes wired into the larger map. At startup Mocha traces every line of that sub-chart and confirms every state is reachable, every path lands on a final state, and every trigger you handle is one the saga can receive. Validated before traffic, never silently broken in prod."
          glyph={3}
        >
          <Bullets
            items={[
              "Draft -> Checked -> Published, with compensation paths on failure.",
              "Persisted state across hops, scoped to a correlation key.",
              "Validated before the service handles traffic, never silently broken in prod.",
            ]}
          />
          <div className="border-cc-card-border bg-cc-card-bg mt-8 rounded-xl border p-5 sm:p-6">
            <SagaStateMini />
          </div>
        </ConstellationCard>

        <ConstellationCard
          id="reliability"
          index="04"
          eyebrow="Exactly-once processing"
          title="Outbox plus idempotent inbox, the boring way."
          body="On the constellation, the outbox and inbox are two small stars on either side of the transport, anchoring the line that crosses a process boundary. The outbox commits your domain change and the message to dispatch in the same database transaction, so a crash never loses messages. On the far side, the inbox records each message id and skips duplicates so every hop along that line is processed exactly once, even when the broker redelivers."
          glyph={4}
        >
          <Bullets
            items={[
              "Transactional outbox on Postgres (and EF Core), wired through your DbContext.",
              "Idempotent inbox dedupes on the consumer side without extra application code.",
              "Per-exception retry, redelivery, dead-letter, circuit breaker, concurrency limiter.",
            ]}
          />
          <div className="border-cc-card-border bg-cc-card-bg mt-8 rounded-xl border p-5 sm:p-6">
            <OutboxInboxMini />
          </div>
        </ConstellationCard>

        <ConstellationCard
          id="transports-and-traces"
          index="05"
          eyebrow="Transports and traces"
          title="Pick the broker, see every hop as a real span."
          body="RabbitMQ, PostgreSQL, and in-process ship as first-class transports. Kafka, Azure Service Bus, and Azure Event Hub ship in source. Every PublishAsync, transport hop, and handler invocation is an OpenTelemetry span, with correlation propagated across service boundaries."
          glyph={5}
        >
          <div className="grid gap-8 lg:grid-cols-12 lg:gap-10">
            <div className="lg:col-span-5">
              <ul className="flex flex-col gap-2.5">
                {[
                  { name: "RabbitMQ", note: "topic + queue routing" },
                  { name: "PostgreSQL", note: "durable + outbox" },
                  { name: "in-process", note: "same handler shape" },
                  { name: "Kafka", note: "partitioned log" },
                  {
                    name: "Azure Service Bus / Event Hub",
                    note: "managed Azure",
                  },
                ].map((t) => (
                  <li
                    key={t.name}
                    className="border-cc-card-border bg-cc-card-bg flex items-center justify-between gap-3 rounded-md border px-3 py-2"
                  >
                    <span className="text-cc-ink font-mono text-[11.5px]">
                      {t.name}
                    </span>
                    <span className="text-cc-ink-dim font-mono text-[10.5px]">
                      {t.note}
                    </span>
                  </li>
                ))}
              </ul>
              <p className="text-cc-ink-dim text-caption mt-5 font-mono tracking-widest uppercase">
                Every hop a span. Correlation propagated end to end.
              </p>
            </div>
            <div className="lg:col-span-7">
              <div className="bg-cc-surface border-cc-card-border relative overflow-hidden rounded-lg border">
                <NitroTrace />
              </div>
            </div>
          </div>
        </ConstellationCard>
      </div>

      {/* Open-source proof band. */}
      <section
        aria-label="Open source"
        className="border-cc-card-border mx-auto max-w-5xl border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>MIT licensed</Eyebrow>
            <h2 className="text-cc-heading font-heading text-h2 mt-4 font-semibold tracking-tight text-balance">
              Open source. Free to use. Built in the open.
            </h2>
            <p className="text-cc-prose text-lead mt-4 max-w-2xl">
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
      <section className="relative py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading text-h1 mx-auto mt-5 max-w-3xl font-semibold tracking-tight text-balance">
            Write a handler. Dispatch it.
          </h2>
          <p className="text-cc-prose text-lead mx-auto mt-5 max-w-2xl">
            Attribute it, dispatch it. The source generator handles registration
            and the pipeline. The transport, the outbox, the inbox, the sagas,
            and the traces are part of the framework, not bolt-on packages you
            wire yourself.
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
