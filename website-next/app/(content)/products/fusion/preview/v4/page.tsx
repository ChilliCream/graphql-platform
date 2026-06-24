import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroFusion } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Fusion: Distributed GraphQL Gateway for .NET",
  description:
    "Fusion is ChilliCream's distributed GraphQL gateway .NET teams own end to end. Compose subgraphs at planning time, prove the graph answers, ship from one endpoint.",
  keywords: [
    "Fusion",
    "distributed GraphQL gateway",
    "distributed GraphQL gateway .NET",
    "GraphQL Composite Schemas",
    "Apollo Federation",
    ".NET GraphQL gateway",
    "Hot Chocolate",
    "subgraph composition",
    "query plan",
    "satisfiability",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Fusion: Distributed GraphQL Gateway for .NET",
    description:
      "Compose subgraphs into one validated graph at planning time. Apollo Federation spec compatible. .NET-native, self-run gateway built on Hot Chocolate.",
    type: "website",
  },
};

// Brand spectrum, used at most once per page, on the closing hairline.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Primitives
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

interface ChapterMarkerProps {
  readonly numeral: string;
  readonly label: string;
}

function ChapterMarker({ numeral, label }: ChapterMarkerProps) {
  return (
    <div className="flex items-center justify-center gap-3">
      <span aria-hidden className="bg-cc-card-border h-px w-6" />
      <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
        {numeral} {label}
      </span>
      <span aria-hidden className="bg-cc-card-border h-px w-6" />
    </div>
  );
}

function SectionHairline() {
  return (
    <div className="flex justify-center py-16 sm:py-20">
      <span aria-hidden className="bg-cc-card-border h-px w-12" />
    </div>
  );
}

interface PullQuoteProps {
  readonly children: ReactNode;
  readonly cite?: string;
}

function PullQuote({ children, cite }: PullQuoteProps) {
  return (
    <figure className="mx-auto my-12 max-w-3xl">
      <blockquote className="border-cc-accent text-cc-heading font-heading text-h4 border-l-2 pl-6 italic">
        {children}
      </blockquote>
      {cite ? (
        <figcaption className="text-cc-ink-dim mt-3 pl-6 font-mono text-[11px] tracking-[0.18em] uppercase">
          {cite}
        </figcaption>
      ) : null}
    </figure>
  );
}

interface DiagramFrameProps {
  readonly caption: string;
  readonly children: ReactNode;
}

function DiagramFrame({ caption, children }: DiagramFrameProps) {
  return (
    <figure className="my-10">
      <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 sm:p-6">
        {children}
      </div>
      <figcaption className="text-cc-ink-dim mt-3 text-center font-mono text-[11px] tracking-[0.18em] uppercase">
        {caption}
      </figcaption>
    </figure>
  );
}

// -----------------------------------------------------------------------------
// Console card (reused inline at column width)
// -----------------------------------------------------------------------------

interface ConsoleCardProps {
  readonly file: string;
  readonly tag: string;
  readonly children: ReactNode;
}

function ConsoleCard({ file, tag, children }: ConsoleCardProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border my-10 overflow-hidden rounded-lg border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
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
      <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[12.5px] leading-6">
        {children}
      </pre>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Inline diagrams. Each constrained to the centered column width.
// -----------------------------------------------------------------------------

function CompositionPipelineDiagram() {
  const phases = ["parse", "enrich", "validate", "merge", "satisfiability"];
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Composition phases run in CI and emit a versioned Fusion archive"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        nitro fusion compose
      </text>
      {[
        { y: 36, label: "catalog.graphql" },
        { y: 76, label: "checkout.graphql" },
        { y: 116, label: "reviews.graphql" },
      ].map((s) => (
        <g key={s.label}>
          <rect
            x="12"
            y={s.y}
            width="120"
            height="26"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="22"
            y={s.y + 17}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            {s.label}
          </text>
          <path
            d={`M 132 ${s.y + 13} C 160 ${s.y + 13}, 160 80, 188 80`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      {phases.map((p, i) => (
        <g key={p}>
          <rect
            x={188 + i * 52}
            y="68"
            width="44"
            height="24"
            rx="4"
            fill={
              i === phases.length - 1
                ? "rgba(94,234,212,0.16)"
                : "rgba(245,241,234,0.04)"
            }
            stroke={
              i === phases.length - 1
                ? "rgba(94,234,212,0.55)"
                : "rgba(245,241,234,0.16)"
            }
          />
          <text
            x={188 + i * 52 + 22}
            y="83"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9"
            fill={
              i === phases.length - 1 ? "#5eead4" : "rgba(245,241,234,0.62)"
            }
          >
            {p}
          </text>
          {i < phases.length - 1 && (
            <path
              d={`M ${188 + i * 52 + 44} 80 L ${188 + (i + 1) * 52} 80`}
              stroke="rgba(245,241,234,0.25)"
              strokeWidth="1"
              fill="none"
            />
          )}
        </g>
      ))}
      <path
        d="M 410 92 L 410 140"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.2"
        fill="none"
      />
      <polygon points="406,140 410,150 414,140" fill="rgba(94,234,212,0.7)" />
      <rect
        x="332"
        y="152"
        width="156"
        height="40"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="410"
        y="170"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        gateway.far
      </text>
      <text
        x="410"
        y="184"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.62)"
      >
        versioned, inspectable
      </text>
      <text
        x="12"
        y="170"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        fails like a compile error if a phase
      </text>
      <text
        x="12"
        y="184"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        emits a diagnostic, before deploy
      </text>
    </svg>
  );
}

function SatisfiabilityDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Every reachable field has a resolver path across the composed graph"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        reachability walk over Query.*
      </text>
      <rect
        x="20"
        y="92"
        width="68"
        height="32"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="54"
        y="112"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        Query
      </text>
      {[
        { y: 40, label: "order(id)", owner: "checkout" },
        { y: 96, label: "order.items", owner: "catalog" },
        { y: 152, label: "order.shipping", owner: "checkout" },
      ].map((n) => (
        <g key={n.label}>
          <path
            d={`M 88 108 C 130 108, 130 ${n.y + 14}, 172 ${n.y + 14}`}
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="172"
            y={n.y}
            width="156"
            height="28"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.45)"
          />
          <text
            x="184"
            y={n.y + 18}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#f5f0ea"
          >
            {n.label}
          </text>
          <text
            x="320"
            y={n.y + 18}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.55)"
          >
            {n.owner}
          </text>
          <g transform={`translate(336 ${n.y + 6})`}>
            <rect
              width="120"
              height="16"
              rx="8"
              fill="rgba(94,234,212,0.12)"
              stroke="rgba(94,234,212,0.55)"
            />
            <text
              x="60"
              y="12"
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="9"
              fill="#5eead4"
            >
              path resolvable
            </text>
          </g>
        </g>
      ))}
      <text
        x="12"
        y="204"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        unresolvable shapes fail composition with UNSATISFIABLE_QUERY_PATH
      </text>
    </svg>
  );
}

function FederationInteropDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Apollo Federation v2 subgraphs are valid Fusion subgraphs"
    >
      {[
        { y: 30, label: "Apollo Federation v2", sub: "@key, @requires" },
        { y: 96, label: "Hot Chocolate", sub: "@lookup, plain Query" },
        { y: 162, label: "Hot Chocolate (entities)", sub: "@lookup, @key" },
      ].map((row) => (
        <g key={row.label}>
          <rect
            x="12"
            y={row.y}
            width="200"
            height="40"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="24"
            y={row.y + 18}
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {row.label}
          </text>
          <text
            x="24"
            y={row.y + 33}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.55)"
          >
            {row.sub}
          </text>
          <path
            d={`M 212 ${row.y + 20} C 260 ${row.y + 20}, 280 110, 320 110`}
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="320"
        y="86"
        width="148"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="394"
        y="108"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#5eead4"
      >
        Fusion gateway
      </text>
      <text
        x="394"
        y="124"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.62)"
      >
        GraphQL Composite Schemas spec
      </text>
    </svg>
  );
}

function QueryPlanDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Distributed query plan with parallel and batched subgraph fetches"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        query plan
      </text>
      <rect
        x="12"
        y="32"
        width="100"
        height="28"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="62"
        y="51"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        client op
      </text>
      {[
        { y: 76, label: "fetch catalog", ms: "12ms" },
        { y: 110, label: "fetch checkout", ms: "10ms" },
      ].map((n) => (
        <g key={n.label}>
          <path
            d={`M 112 46 C 150 46, 150 ${n.y + 12}, 180 ${n.y + 12}`}
            stroke="rgba(94,234,212,0.45)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="180"
            y={n.y}
            width="160"
            height="24"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="192"
            y={n.y + 16}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            {n.label}
          </text>
          <text
            x="332"
            y={n.y + 16}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(94,234,212,0.85)"
          >
            {n.ms}
          </text>
        </g>
      ))}
      <path
        d="M 340 88 C 372 88, 372 156, 180 156"
        stroke="rgba(94,234,212,0.45)"
        strokeWidth="1.2"
        fill="none"
        strokeDasharray="3 3"
      />
      <rect
        x="180"
        y="144"
        width="220"
        height="36"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(94,234,212,0.45)"
      />
      <text
        x="192"
        y="162"
        fontFamily="ui-monospace, monospace"
        fontSize="10.5"
        fill="#f5f0ea"
      >
        batch reviews(productIds: [...])
      </text>
      <text
        x="192"
        y="174"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.55)"
      >
        one HTTP/2 call, no N+1 across the graph
      </text>
      <text
        x="12"
        y="200"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        parallel where independent, sequenced where required
      </text>
    </svg>
  );
}

function DotNetGatewayDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Fusion gateway is an ASP.NET Core app with DI, auth, and middleware"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        Program.cs
      </text>
      {[
        { x: 12, label: "AuthN" },
        { x: 96, label: "Headers" },
        { x: 180, label: "Fusion" },
        { x: 264, label: "Cache" },
        { x: 348, label: "Telemetry" },
      ].map((m) => (
        <g key={m.label}>
          <rect
            x={m.x}
            y="60"
            width="76"
            height="28"
            rx="4"
            fill={
              m.label === "Fusion"
                ? "rgba(94,234,212,0.16)"
                : "rgba(245,241,234,0.04)"
            }
            stroke={
              m.label === "Fusion"
                ? "rgba(94,234,212,0.6)"
                : "rgba(245,241,234,0.18)"
            }
          />
          <text
            x={m.x + 38}
            y="78"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill={m.label === "Fusion" ? "#5eead4" : "rgba(245,241,234,0.7)"}
          >
            {m.label}
          </text>
        </g>
      ))}
      <path
        d="M 12 100 L 424 100"
        stroke="rgba(245,241,234,0.18)"
        strokeWidth="1"
        fill="none"
      />
      <text
        x="12"
        y="116"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        ASP.NET Core middleware pipeline
      </text>
      <text
        x="12"
        y="150"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        no separate Rust binary, no YAML config, no Node runtime
      </text>
      <rect
        x="12"
        y="166"
        width="412"
        height="48"
        rx="6"
        fill="rgba(94,234,212,0.06)"
        stroke="rgba(94,234,212,0.4)"
      />
      <text
        x="24"
        y="184"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        builder.Services.AddGraphQLGateway()
      </text>
      <text
        x="24"
        y="202"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        .AddFileSystemConfiguration(&quot;./gateway.far&quot;);
      </text>
    </svg>
  );
}

function SelfRunDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="The gateway runs in your own infrastructure, never a hosted hop"
    >
      <rect
        x="12"
        y="22"
        width="456"
        height="176"
        rx="12"
        fill="rgba(245,241,234,0.03)"
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="4 4"
      />
      <text
        x="28"
        y="42"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        your network
      </text>
      <rect
        x="32"
        y="92"
        width="76"
        height="32"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.18)"
      />
      <text
        x="70"
        y="112"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#f5f0ea"
      >
        client
      </text>
      <path
        d="M 108 108 L 168 108"
        stroke="rgba(94,234,212,0.5)"
        strokeWidth="1.2"
        fill="none"
      />
      <polygon points="168,104 180,108 168,112" fill="rgba(94,234,212,0.7)" />
      <rect
        x="180"
        y="80"
        width="140"
        height="56"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="250"
        y="104"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#5eead4"
      >
        Fusion gateway
      </text>
      <text
        x="250"
        y="120"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        ASP.NET Core
      </text>
      {[
        { y: 56, label: "catalog" },
        { y: 100, label: "checkout" },
        { y: 144, label: "reviews" },
      ].map((s) => (
        <g key={s.label}>
          <path
            d={`M 320 108 C 350 108, 350 ${s.y + 14}, 388 ${s.y + 14}`}
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="388"
            y={s.y}
            width="64"
            height="28"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="420"
            y={s.y + 18}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            {s.label}
          </text>
        </g>
      ))}
      <text
        x="28"
        y="186"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        no hosted hop, no third party in the request path
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Facts list (single-column, hairline separated)
// -----------------------------------------------------------------------------

interface FactRowProps {
  readonly label: string;
  readonly value: string;
}

function FactRow({ label, value }: FactRowProps) {
  return (
    <div className="border-cc-card-border flex items-baseline justify-between border-b py-4 last:border-b-0">
      <dt className="text-cc-ink-dim font-mono text-[11px] tracking-[0.2em] uppercase">
        {label}
      </dt>
      <dd className="text-cc-heading font-heading text-base">{value}</dd>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function FusionPreviewV4() {
  return (
    <article className="mx-auto max-w-2xl px-4 sm:px-6">
      {/* PROLOGUE / HERO */}
      <section className="pt-20 pb-8 text-center sm:pt-28">
        <Eyebrow>Distributed GraphQL gateway .NET</Eyebrow>
        <h1 className="text-cc-heading font-heading text-hero mt-6 font-semibold tracking-tight text-balance">
          Compose your graph at planning time, not runtime.
        </h1>
        <p className="text-cc-prose text-lead mt-8 leading-relaxed">
          Fusion is ChilliCream&apos;s distributed GraphQL gateway. Point it at
          independent subgraphs, compose them into one composite schema in CI,
          and ship a versioned plan that is proven answerable before a client
          ever sends a query. Built on Hot Chocolate, run as your own ASP.NET
          Core app.
        </p>
        <div className="mt-10 flex flex-wrap justify-center gap-3">
          <SolidButton href="/docs/fusion">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>
      </section>

      <SectionHairline />

      {/* I. COMPOSITION */}
      <section id="composition" className="scroll-mt-24 py-12 sm:py-16">
        <ChapterMarker numeral="I." label="Composition" />
        <h2 className="text-cc-heading font-heading text-h2 mt-6 text-center font-semibold tracking-tight text-balance">
          Composition runs in CI, not on a hot path.
        </h2>
        <p className="text-cc-prose text-lead mt-8 leading-relaxed">
          A composition pipeline reads each subgraph SDL, validates it against
          the others, and emits a Fusion archive your gateway loads at startup.
          Type, enum, and field conflicts surface as diagnostics with stable
          codes, on the build server, before deploy. The gateway never sees raw
          source schemas.
        </p>
        <DiagramFrame caption="nitro fusion compose, five phases, one .far artifact">
          <CompositionPipelineDiagram />
        </DiagramFrame>
        <PullQuote cite="Composition pipeline, halt-on-diagnostic">
          If a phase emits a diagnostic, the build fails, not production.
        </PullQuote>
        <p className="text-cc-prose text-body mt-6 leading-relaxed">
          The whole pipeline runs offline as a build step or in CI. Nitro cloud
          is optional for delivery, never in the request path. Each release
          produces a versioned, inspectable .far artifact you can diff between
          deploys and roll back like any other binary.
        </p>
        <ConsoleCard file="ci/compose.sh" tag="shell">
          <span style={{ color: "#8b949e" }}>
            {"# Compose subgraphs and fail the build on any conflict.\n"}
          </span>
          <span style={{ color: "#ff7b72" }}>nitro</span>
          <span style={{ color: "#c9d1d9" }}> fusion compose \\</span>
          {"\n"}
          <span style={{ color: "#c9d1d9" }}>
            {"  --subgraph catalog=./catalog.graphql \\"}
          </span>
          {"\n"}
          <span style={{ color: "#c9d1d9" }}>
            {"  --subgraph checkout=./checkout.graphql \\"}
          </span>
          {"\n"}
          <span style={{ color: "#c9d1d9" }}>
            {"  --subgraph reviews=./reviews.graphql \\"}
          </span>
          {"\n"}
          <span style={{ color: "#c9d1d9" }}>{"  --output ./gateway.far"}</span>
          {"\n\n"}
          <span style={{ color: "#5eead4" }}>{"OK"}</span>
          <span style={{ color: "#c9d1d9" }}>
            {" composed 3 subgraphs, 0 errors, "}
          </span>
          <span style={{ color: "#a5d6ff" }}>gateway.far</span>
          <span style={{ color: "#c9d1d9" }}>{" written"}</span>
        </ConsoleCard>
      </section>

      <SectionHairline />

      {/* II. SATISFIABILITY */}
      <section id="satisfiability" className="scroll-mt-24 py-12 sm:py-16">
        <ChapterMarker numeral="II." label="Satisfiability" />
        <h2 className="text-cc-heading font-heading text-h2 mt-6 text-center font-semibold tracking-tight text-balance">
          If it composes, it answers.
        </h2>
        <p className="text-cc-prose text-lead mt-8 leading-relaxed">
          Composition&apos;s final phase walks every reachable field from the
          root types and proves it can be resolved across your subgraphs given
          the available lookups and keys. A query that successfully validates
          against the gateway is one your services can actually answer.
        </p>
        <DiagramFrame caption="reachability walk over Query.*, every field proved resolvable">
          <SatisfiabilityDiagram />
        </DiagramFrame>
        <PullQuote cite="Fusion.Composition.Satisfiability">
          Unresolvable shapes fail composition with UNSATISFIABLE_QUERY_PATH.
        </PullQuote>
        <p className="text-cc-prose text-body mt-6 leading-relaxed">
          Reachability analysis catches contract drift between subgraphs before
          a client ever sends the query. Failures cite the exact field path, so
          the broken shape is the next thing you fix, not a 3am incident dressed
          up as a 500.
        </p>
      </section>

      <SectionHairline />

      {/* III. FEDERATION INTEROP */}
      <section id="federation" className="scroll-mt-24 py-12 sm:py-16">
        <ChapterMarker numeral="III." label="Federation interop" />
        <h2 className="text-cc-heading font-heading text-h2 mt-6 text-center font-semibold tracking-tight text-balance">
          Apollo Federation spec compatible, on an open standard.
        </h2>
        <p className="text-cc-prose text-lead mt-8 leading-relaxed">
          Fusion implements the GraphQL Composite Schemas specification under
          the GraphQL Foundation, and reads Apollo Federation v2 subgraphs
          through a dedicated connector. Bring existing @key, @requires, and
          @provides directives into a Fusion composition without rewriting
          resolvers, on a vendor-neutral spec.
        </p>
        <DiagramFrame caption="Federation v2 and Hot Chocolate subgraphs, one composed gateway">
          <FederationInteropDiagram />
        </DiagramFrame>
        <PullQuote cite="GraphQL Composite Schemas, GraphQL Foundation">
          Subgraph schemas stay portable. The spec belongs to the foundation,
          not the gateway vendor.
        </PullQuote>
        <p className="text-cc-prose text-body mt-6 leading-relaxed">
          Fusion.Connectors.ApolloFederation reads existing Federation v2
          subgraphs through a directive-by-directive mapping, so you can move to
          a vendor-neutral spec without rewriting resolvers or freezing your
          existing federation delivery.
        </p>
      </section>

      <SectionHairline />

      {/* IV. DISTRIBUTED QUERY PLAN */}
      <section id="plan" className="scroll-mt-24 py-12 sm:py-16">
        <ChapterMarker numeral="IV." label="Distributed query plan" />
        <h2 className="text-cc-heading font-heading text-h2 mt-6 text-center font-semibold tracking-tight text-balance">
          One client request, a planned distributed fetch.
        </h2>
        <p className="text-cc-prose text-lead mt-8 leading-relaxed">
          The gateway compiles each incoming operation into a query plan over
          your subgraphs. Independent fetches run in parallel, dependent fetches
          sequence behind them, and shared entity keys are batched into single
          HTTP/2 calls. The result is one response, assembled from the minimum
          work your fleet needs to do.
        </p>
        <DiagramFrame caption="parallel fan-out, sequenced dependents, batched entity keys">
          <QueryPlanDiagram />
        </DiagramFrame>
        <PullQuote cite="Distributed query plan, execution stage">
          Parallel where independent, sequenced where required.
        </PullQuote>
        <p className="text-cc-prose text-body mt-6 leading-relaxed">
          Entity keys are collected across the plan and batched, so a single
          HTTP/2 call replaces what would otherwise be a chatty N+1 across
          services. Persisted operations and conservative cache control are
          merged from the subgraph policies you already publish.
        </p>
      </section>

      <SectionHairline />

      {/* V. .NET-NATIVE GATEWAY */}
      <section id="dotnet" className="scroll-mt-24 py-12 sm:py-16">
        <ChapterMarker numeral="V." label=".NET-native gateway" />
        <h2 className="text-cc-heading font-heading text-h2 mt-6 text-center font-semibold tracking-tight text-balance">
          The gateway is your code, on Hot Chocolate.
        </h2>
        <p className="text-cc-prose text-lead mt-8 leading-relaxed">
          Fusion&apos;s gateway is an ASP.NET Core app, configured with
          AddGraphQLGateway() and built on Hot Chocolate. No standalone binary,
          no YAML, no Node runtime in the request path. The same Hot Chocolate
          server you already ship can be a Fusion subgraph with no resolver
          changes.
        </p>
        <DiagramFrame caption="ASP.NET Core middleware pipeline, Fusion is one stage">
          <DotNetGatewayDiagram />
        </DiagramFrame>
        <ConsoleCard file="Program.cs" tag="C#">
          <span style={{ color: "#c9d1d9" }}>{"var builder = "}</span>
          <span style={{ color: "#ffa657" }}>WebApplication</span>
          <span style={{ color: "#c9d1d9" }}>{"."}</span>
          <span style={{ color: "#d2a8ff" }}>CreateBuilder</span>
          <span style={{ color: "#c9d1d9" }}>{"(args);"}</span>
          {"\n\n"}
          <span style={{ color: "#c9d1d9" }}>{"builder.Services"}</span>
          {"\n"}
          <span style={{ color: "#c9d1d9" }}>{"    ."}</span>
          <span style={{ color: "#d2a8ff" }}>AddGraphQLGateway</span>
          <span style={{ color: "#c9d1d9" }}>{"()"}</span>
          {"\n"}
          <span style={{ color: "#c9d1d9" }}>{"    ."}</span>
          <span style={{ color: "#d2a8ff" }}>AddFileSystemConfiguration</span>
          <span style={{ color: "#c9d1d9" }}>{"("}</span>
          <span style={{ color: "#a5d6ff" }}>{'"./gateway.far"'}</span>
          <span style={{ color: "#c9d1d9" }}>{");"}</span>
          {"\n\n"}
          <span style={{ color: "#c9d1d9" }}>{"var app = builder."}</span>
          <span style={{ color: "#d2a8ff" }}>Build</span>
          <span style={{ color: "#c9d1d9" }}>{"();"}</span>
          {"\n"}
          <span style={{ color: "#c9d1d9" }}>{"app."}</span>
          <span style={{ color: "#d2a8ff" }}>MapGraphQL</span>
          <span style={{ color: "#c9d1d9" }}>{"();"}</span>
          {"\n"}
          <span style={{ color: "#c9d1d9" }}>{"app."}</span>
          <span style={{ color: "#d2a8ff" }}>Run</span>
          <span style={{ color: "#c9d1d9" }}>{"();"}</span>
        </ConsoleCard>
        <PullQuote cite="ASP.NET Core, your application host">
          Your DI, your auth, your middleware.
        </PullQuote>
        <p className="text-cc-prose text-body mt-6 leading-relaxed">
          Auth, header propagation, and cache control land where you expect them
          in .NET. An existing Hot Chocolate server is already a valid subgraph,
          no federation library needed.
        </p>
      </section>

      <SectionHairline />

      {/* VI. SELF-RUN, ALWAYS */}
      <section id="self-run" className="scroll-mt-24 py-12 sm:py-16">
        <ChapterMarker numeral="VI." label="Self-run, always" />
        <h2 className="text-cc-heading font-heading text-h2 mt-6 text-center font-semibold tracking-tight text-balance">
          The gateway is always self-run.
        </h2>
        <p className="text-cc-prose text-lead mt-8 leading-relaxed">
          Fusion runs in your infrastructure, period. Every client request and
          every subgraph fetch stay inside your network boundary. You choose the
          cluster, the auth, the egress, the audit trail. Nitro cloud is
          available for managed composition delivery, never as a hop in the
          request path.
        </p>
        <DiagramFrame caption="your network, your gateway, your subgraphs">
          <SelfRunDiagram />
        </DiagramFrame>
        <PullQuote cite="Request path, zero third parties">
          Never a hosted hop.
        </PullQuote>
        <p className="text-cc-prose text-body mt-6 leading-relaxed">
          Standard ASP.NET Core auth (JWT, cookie, OIDC, mTLS) and header
          propagation to subgraphs work the way your platform team already
          operates them. Fusion emits OpenTelemetry spans for each request, the
          planning step, and every subgraph fetch. Nitro renders the plan as a
          navigable trace, so when a single subgraph slows down you see which
          step in the plan, which subgraph, and which keys were in the batch.
        </p>
        <figure className="my-10">
          <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border">
            <NitroFusion />
          </div>
          <figcaption className="text-cc-ink-dim mt-3 text-center font-mono text-[11px] tracking-[0.18em] uppercase">
            <span>ExecuteRequest</span>
            <span aria-hidden> {"·"} </span>
            <span>PlanOperation</span>
            <span aria-hidden> {"·"} </span>
            <span>ExecutePlanNode</span>
          </figcaption>
        </figure>
      </section>

      <SectionHairline />

      {/* CODA: OPEN SOURCE */}
      <section aria-label="Open source" className="scroll-mt-24 py-12 sm:py-16">
        <div className="text-center">
          <Eyebrow>MIT licensed</Eyebrow>
          <h2 className="text-cc-heading font-heading text-h2 mt-6 font-semibold tracking-tight text-balance">
            Open source, on an open standard.
          </h2>
        </div>
        <p className="text-cc-prose text-lead mt-8 leading-relaxed">
          Fusion is part of the ChilliCream GraphQL Platform, developed in the
          open under the MIT license and built on the GraphQL Composite Schemas
          specification under the GraphQL Foundation. The codebase, the issue
          tracker, the roadmap, and the release notes all live on GitHub.
        </p>
        <dl className="mt-10">
          <FactRow label="License" value="MIT" />
          <FactRow label="Runtime" value="ASP.NET Core" />
          <FactRow label="Spec" value="Composite Schemas" />
          <FactRow label="Built on" value="Hot Chocolate" />
          <FactRow label="Interop" value="Federation v2" />
          <FactRow label="Tracing" value="OpenTelemetry" />
        </dl>
        <ul className="mt-10 flex flex-col gap-2.5">
          {[
            "Codebase, issues, and roadmap on GitHub under the MIT license.",
            "GraphQL Composite Schemas spec under the GraphQL Foundation.",
            "Released alongside Hot Chocolate, Strawberry Shake, Nitro, and Mocha.",
          ].map((line) => (
            <li
              key={line}
              className="text-cc-ink text-body flex items-start gap-3 leading-relaxed"
            >
              <span className="text-cc-accent mt-1 shrink-0" aria-hidden>
                <CheckIcon size={14} />
              </span>
              <span>{line}</span>
            </li>
          ))}
        </ul>
      </section>

      {/* CLOSING CTA: the single SPECTRUM use, then a centered button stack. */}
      <section className="relative pt-20 pb-24 sm:pt-24 sm:pb-32">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="mx-auto max-w-xl text-center">
          <Eyebrow>Get started</Eyebrow>
          <p className="text-cc-heading font-heading text-h1 mt-6 font-semibold tracking-tight text-balance">
            One composite graph, proven before you ship it.
          </p>
          <p className="text-cc-prose text-lead mt-6 leading-relaxed">
            Point Fusion at your subgraphs, compose in CI, and serve from a
            single .NET endpoint you operate yourself. The plan is built, the
            satisfiability is proven, and the runtime is the ASP.NET Core you
            already run.
          </p>
          <div className="mt-10 flex flex-col items-center gap-3">
            <SolidButton href="/docs/fusion">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </article>
  );
}
