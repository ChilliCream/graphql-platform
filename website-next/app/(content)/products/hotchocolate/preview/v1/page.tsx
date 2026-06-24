import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Hot Chocolate: GraphQL Server for .NET",
  description:
    "Hot Chocolate is the open-source GraphQL server for .NET. C# resolvers, source-generated schema and DataLoaders, subscriptions, OpenTelemetry, and Fusion-ready.",
  keywords: [
    "Hot Chocolate",
    "GraphQL server",
    ".NET GraphQL",
    "C# GraphQL",
    "ASP.NET Core",
    "DataLoader",
    "Green Donut",
    "GraphQL subscriptions",
    "OpenTelemetry",
    "Apollo Federation",
    "Fusion",
    "Strawberry Shake",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Hot Chocolate: GraphQL Server for .NET",
    description:
      "C# is the schema. Source-generated resolvers, batched DataLoaders, subscriptions, OpenTelemetry, and Fusion compatibility. MIT-licensed.",
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
// Hero code card: a real-feel GitHub-dark C# snippet with line numbers and a
// single clipped cyan -> teal gradient overlay clipped to the [QueryType] token,
// which is the only color event in the hero (the accent rule on this page).
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

// Token color helpers. Colors are GitHub-dark approximations, scoped to this
// snippet only so the rest of the page stays on cc-* tokens.
const C = {
  kw: { color: "#ff7b72" }, // keyword
  type: { color: "#ffa657" }, // type names
  str: { color: "#a5d6ff" }, // strings
  comment: { color: "#8b949e", fontStyle: "italic" as const }, // comments
  attr: { color: "#d2a8ff" }, // attributes
  fn: { color: "#d2a8ff" }, // method names
  param: { color: "#79c0ff" }, // params
  punct: { color: "#c9d1d9" }, // punctuation / default
  plain: { color: "#c9d1d9" },
  dim: { color: "#8b949e" },
};

function HeroCodeCard() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      {/* Window chrome */}
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
          Catalog/Query.cs
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          C#
        </span>
      </div>

      {/* The clipped cyan -> teal gradient: a single soft pulse anchored in the
          top-left where the [QueryType] attribute sits. This is the lone color
          event in the hero. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 180px at 14% 22%, rgba(94, 234, 212, 0.18), transparent 70%), radial-gradient(280px 140px at 8% 16%, rgba(22, 185, 228, 0.18), transparent 70%)",
        }}
      />

      <div className="relative py-4">
        <CodeLine n={1}>
          <span style={C.kw}>using</span>{" "}
          <span style={C.plain}>HotChocolate.Types;</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.kw}>namespace</span>{" "}
          <span style={C.plain}>Catalog;</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.comment}>{`// The C# is the schema.`}</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={C.punct}>[</span>
          <span style={C.attr}>QueryType</span>
          <span style={C.punct}>]</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={C.kw}>public partial class</span>{" "}
          <span style={C.type}>Query</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={C.punct}>{`{`}</span>
        </CodeLine>
        <CodeLine n={9}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.kw}>public static async</span>{" "}
          <span style={C.type}>Task</span>
          <span style={C.punct}>{`<`}</span>
          <span style={C.type}>Product</span>
          <span style={C.punct}>{`?>`}</span>{" "}
          <span style={C.fn}>GetProductByIdAsync</span>
          <span style={C.punct}>(</span>
        </CodeLine>
        <CodeLine n={10}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>Guid</span> <span style={C.param}>id</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={11}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>IProductByIdDataLoader</span>{" "}
          <span style={C.param}>productById</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={12}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>CancellationToken</span>{" "}
          <span style={C.param}>ct</span>
          <span style={C.punct}>{`) =>`}</span>
        </CodeLine>
        <CodeLine n={13}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.kw}>await</span>{" "}
          <span style={C.param}>productById</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>LoadAsync</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>id</span>
          <span style={C.punct}>, </span>
          <span style={C.param}>ct</span>
          <span style={C.punct}>);</span>
        </CodeLine>
        <CodeLine n={14}>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
        <CodeLine n={15}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={16}>
          <span
            style={C.comment}
          >{`// Source-generated: batches Guids per request, no N+1.`}</span>
        </CodeLine>
        <CodeLine n={17}>
          <span style={C.punct}>[</span>
          <span style={C.attr}>DataLoader</span>
          <span style={C.punct}>]</span>
        </CodeLine>
        <CodeLine n={18}>
          <span style={C.kw}>internal static async</span>{" "}
          <span style={C.type}>Task</span>
          <span style={C.punct}>{`<`}</span>
          <span style={C.type}>IReadOnlyDictionary</span>
          <span style={C.punct}>{`<`}</span>
          <span style={C.type}>Guid</span>
          <span style={C.punct}>, </span>
          <span style={C.type}>Product</span>
          <span style={C.punct}>{`>>`}</span>
        </CodeLine>
        <CodeLine n={19}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.fn}>GetProductsByIdAsync</span>
          <span style={C.punct}>(</span>
        </CodeLine>
        <CodeLine n={20}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>IReadOnlyList</span>
          <span style={C.punct}>{`<`}</span>
          <span style={C.type}>Guid</span>
          <span style={C.punct}>{`>`}</span> <span style={C.param}>ids</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={21}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>CatalogDbContext</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.punct}>,</span>
        </CodeLine>
        <CodeLine n={22}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>CancellationToken</span>{" "}
          <span style={C.param}>ct</span>
          <span style={C.punct}>{`) =>`}</span>
        </CodeLine>
        <CodeLine n={23}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.kw}>await</span> <span style={C.param}>db</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Products</span>
        </CodeLine>
        <CodeLine n={24}>
          <span style={C.plain}>{`            `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Where</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>p</span>
          <span style={C.punct}>{` => `}</span>
          <span style={C.param}>ids</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Contains</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>p</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Id</span>
          <span style={C.punct}>))</span>
        </CodeLine>
        <CodeLine n={25}>
          <span style={C.plain}>{`            `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>ToDictionaryAsync</span>
          <span style={C.punct}>(</span>
          <span style={C.param}>p</span>
          <span style={C.punct}>{` => `}</span>
          <span style={C.param}>p</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Id</span>
          <span style={C.punct}>, </span>
          <span style={C.param}>ct</span>
          <span style={C.punct}>);</span>
        </CodeLine>
      </div>

      {/* Footer caption explains what the snippet proves. */}
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
        <span>build: schema + resolvers + DataLoader emitted</span>
        <span className="text-cc-accent">Roslyn source generator</span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Alternating feature row. Each row pairs a copy column with a small inline
// SVG/markup diagram column. No external images, no rasters.
// -----------------------------------------------------------------------------

interface FeatureRowProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
  /** When true, copy renders on the right and the visual on the left. */
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
// Inline diagrams. Each is hand-built TSX/SVG using cc-* tokens, framed inside
// a feature card. No external assets.
// -----------------------------------------------------------------------------

/** Composition diagram: subgraphs feed a Fusion gateway plan at build time. */
function CompositionDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Subgraph schemas composed at build time into a Fusion plan"
    >
      <defs>
        <linearGradient id="hc-fx-line" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.1" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
        </linearGradient>
      </defs>
      {/* Subgraph nodes */}
      {[
        { y: 24, label: "catalog.graphql" },
        { y: 92, label: "checkout.graphql" },
        { y: 160, label: "reviews.graphql" },
      ].map((n) => (
        <g key={n.label}>
          <rect
            x="12"
            y={n.y}
            width="148"
            height="36"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="86"
            y={n.y + 22}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#a1a3af"
          >
            {n.label}
          </text>
          <path
            d={`M 160 ${n.y + 18} L 280 110`}
            stroke="url(#hc-fx-line)"
            strokeWidth="1.5"
            fill="none"
          />
        </g>
      ))}
      {/* Composer */}
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
        fusion compose
      </text>
      <text
        x="330"
        y="122"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        build time
      </text>
      <path
        d="M 380 110 L 432 110"
        stroke="rgba(94,234,212,0.7)"
        strokeWidth="1.5"
        fill="none"
      />
      <polygon points="432,106 444,110 432,114" fill="rgba(94,234,212,0.7)" />
      <text
        x="438"
        y="98"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#a1a3af"
      >
        plan
      </text>
    </svg>
  );
}

/** Authoring-style diagram: two paths, one schema. */
function AuthoringDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Implementation-first and code-first both compile to one schema"
    >
      {[
        {
          y: 24,
          title: "Implementation-first",
          sub: "[QueryType] partial class",
          tone: "rgba(94,234,212,0.5)",
        },
        {
          y: 132,
          title: "Code-first",
          sub: "ObjectType<T> + descriptor",
          tone: "rgba(94,234,212,0.3)",
        },
      ].map((row) => (
        <g key={row.title}>
          <rect
            x="12"
            y={row.y}
            width="180"
            height="64"
            rx="8"
            fill="rgba(245,241,234,0.04)"
            stroke={row.tone}
          />
          <text
            x="24"
            y={row.y + 26}
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {row.title}
          </text>
          <text
            x="24"
            y={row.y + 46}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            {row.sub}
          </text>
          <path
            d={`M 192 ${row.y + 32} L 300 110`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="300"
        y="78"
        width="160"
        height="64"
        rx="10"
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="380"
        y="106"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="13"
        fill="#f5f0ea"
      >
        One GraphQL schema
      </text>
      <text
        x="380"
        y="126"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10.5"
        fill="rgba(245,241,234,0.62)"
      >
        spec-compliant SDL
      </text>
    </svg>
  );
}

/** DataLoader batching diagram: N requests collapse into one batched load. */
function DataLoaderDiagram() {
  const requests = [16, 38, 60, 82, 104];
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Per-field requests deduplicated and batched into one DataLoader call"
    >
      {requests.map((y, i) => (
        <g key={y}>
          <rect
            x="12"
            y={y}
            width="120"
            height="22"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="22"
            y={y + 15}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            product(id: {i + 1})
          </text>
          <path
            d={`M 132 ${y + 11} C 200 ${y + 11}, 200 110, 268 110`}
            stroke="rgba(94,234,212,0.45)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="268"
        y="92"
        width="120"
        height="40"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="328"
        y="110"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        LoadAsync(ids)
      </text>
      <text
        x="328"
        y="124"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        1 batched call
      </text>
      <path
        d="M 388 112 L 432 112"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.2"
        fill="none"
      />
      <polygon points="432,108 444,112 432,116" fill="rgba(94,234,212,0.7)" />
      <text
        x="438"
        y="100"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#a1a3af"
      >
        db
      </text>
    </svg>
  );
}

/** Subscription transports diagram. */
function SubscriptionsDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Subscription fan-out over WebSocket and Server-Sent Events"
    >
      <rect
        x="12"
        y="88"
        width="130"
        height="48"
        rx="8"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(94,234,212,0.45)"
      />
      <text
        x="77"
        y="108"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#f5f0ea"
      >
        ITopicEventSender
      </text>
      <text
        x="77"
        y="124"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        Redis / NATS / PG
      </text>
      <path
        d="M 142 112 L 220 112"
        stroke="rgba(94,234,212,0.5)"
        strokeWidth="1.2"
        fill="none"
      />
      <rect
        x="220"
        y="88"
        width="120"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="280"
        y="108"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#5eead4"
      >
        [SubscriptionType]
      </text>
      <text
        x="280"
        y="124"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        dynamic topics
      </text>
      {[64, 112, 160].map((y, i) => (
        <g key={y}>
          <path
            d={`M 340 112 C 380 112, 380 ${y}, 420 ${y}`}
            stroke="rgba(245,241,234,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="420"
            y={y - 12}
            width="48"
            height="24"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="444"
            y={y + 4}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {i === 0 ? "ws" : i === 1 ? "sse" : "ws"}
          </text>
        </g>
      ))}
    </svg>
  );
}

/** OpenTelemetry diagram: server, execution, dataloader spans into one trace. */
function OtelDiagram() {
  const spans = [
    { y: 28, w: 380, label: "graphql.request", tone: "rgba(94,234,212,0.55)" },
    {
      y: 56,
      x: 24,
      w: 320,
      label: "graphql.execute",
      tone: "rgba(94,234,212,0.4)",
    },
    {
      y: 84,
      x: 40,
      w: 200,
      label: "graphql.parse + validate",
      tone: "rgba(94,234,212,0.3)",
    },
    {
      y: 112,
      x: 250,
      w: 90,
      label: "resolve product",
      tone: "rgba(94,234,212,0.3)",
    },
    {
      y: 140,
      x: 270,
      w: 60,
      label: "dataloader.batch",
      tone: "rgba(94,234,212,0.25)",
    },
    { y: 168, x: 340, w: 28, label: "db", tone: "rgba(245,241,234,0.25)" },
  ];
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Trace waterfall with server, execution and dataloader spans"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        trace_id 7f8a...
      </text>
      {spans.map((s) => (
        <g key={s.label}>
          <rect
            x={s.x ?? 12}
            y={s.y}
            width={s.w}
            height="14"
            rx="3"
            fill={s.tone}
          />
          <text
            x={(s.x ?? 12) + s.w + 6}
            y={s.y + 11}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {s.label}
          </text>
        </g>
      ))}
      <line
        x1="12"
        y1="192"
        x2="468"
        y2="192"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="12"
        y="206"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        0 ms
      </text>
      <text
        x="468"
        y="206"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        42 ms
      </text>
    </svg>
  );
}

/** Federation diagram: Hot Chocolate as Fusion subgraph and as Apollo subgraph. */
function FederationDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Same Hot Chocolate server runs as Fusion subgraph or Apollo Federation subgraph"
    >
      <rect
        x="12"
        y="86"
        width="160"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="92"
        y="106"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#f5f0ea"
      >
        Hot Chocolate server
      </text>
      <text
        x="92"
        y="122"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        same resolvers
      </text>
      {[
        { y: 36, label: "Fusion gateway", sub: "compile-time plan" },
        { y: 132, label: "Apollo Federation", sub: "spec subgraph" },
      ].map((g) => (
        <g key={g.label}>
          <rect
            x="300"
            y={g.y}
            width="170"
            height="52"
            rx="8"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="385"
            y={g.y + 22}
            textAnchor="middle"
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {g.label}
          </text>
          <text
            x="385"
            y={g.y + 40}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {g.sub}
          </text>
          <path
            d={`M 172 110 C 220 110, 240 ${g.y + 26}, 300 ${g.y + 26}`}
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Stat band for the MIT / open source proof. Pure markup, no claims about
// counts beyond a plain "open" framing.
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

export default function HotChocolatePreviewV1() {
  return (
    <>
      {/* HERO: split layout, copy left, code card right. One color event total. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>GraphQL server for .NET</Eyebrow>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Your C# is the schema.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Hot Chocolate is the open-source GraphQL server for .NET. Annotate
              a partial class, write idiomatic C# resolvers, and a Roslyn source
              generator emits the schema, the resolver pipeline, and DataLoader
              infrastructure at build time. One server speaks HTTP, WebSocket,
              and Server-Sent Events, and the same code can run standalone or as
              a Fusion subgraph later.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
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
                <dd className="text-cc-ink mt-1 text-sm">ASP.NET Core</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Spec
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">GraphQL 2025</dd>
              </div>
            </dl>
          </div>
          <div className="lg:col-span-7">
            <HeroCodeCard />
          </div>
        </div>
      </section>

      {/* Quick "what you get" strip: terse, scannable, all six promises named. */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-6">
          {[
            "Compile-time composition",
            "Code-first or schema-first",
            "DataLoader batching",
            "Realtime subscriptions",
            "OpenTelemetry built in",
            "Federation-ready",
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

      {/* SIX feature rows, alternating sides. Each row: copy + inline SVG diagram. */}
      <FeatureRow
        id="composition"
        index="01"
        eyebrow="Composition"
        title="Compose subgraphs at build time, not at runtime."
        body="Fusion plans composition once, in CI, against the source SDLs. The gateway loads a finished query plan and stays cheap to run at the edge. Schema changes show up as planning errors before they show up as production incidents."
        bullets={[
          "Compose any mix of Hot Chocolate subgraphs into a single planned gateway schema.",
          "Resolver paths stay typed end-to-end across the gateway.",
          "Standalone today, Fusion subgraph tomorrow, no resolver rewrites.",
        ]}
        visual={<CompositionDiagram />}
      />

      <FeatureRow
        id="authoring"
        index="02"
        eyebrow="Authoring"
        title="Implementation-first, or code-first. Pick the style that fits."
        body="Implementation-first is the default: annotate a partial class with [QueryType] and the Roslyn source generator emits the schema and resolver pipelines from your C#, similar to how Meta built its GraphQL server. When you need the schema to diverge from the model, drop into the fluent ObjectType<T> descriptor and mix both in the same project."
        bullets={[
          "Resolvers are plain C# methods with DI-injected services and CancellationToken.",
          "XML doc comments become GraphQL descriptions, refactors stay safe with nameof.",
          "dotnet new graphql gets a running server in minutes.",
        ]}
        visual={<AuthoringDiagram />}
        reverse
      />

      <FeatureRow
        id="dataloader"
        index="03"
        eyebrow="DataLoader"
        title="N+1 disappears at the field level."
        body="Green Donut is built into Hot Chocolate. Annotate a static method with [DataLoader] and the generator emits the loader class, the interface, and the DI registration. Per-request keys are deduplicated, the execution engine resolves fields in waves, and every batch dispatches together."
        bullets={[
          "Batch (one-to-one) and group (one-to-many) loaders, per-request caching.",
          "Works against Entity Framework Core, MongoDB, Marten, Raven, or any IQueryable.",
          "Projections push the selection set down to native database queries.",
        ]}
        visual={<DataLoaderDiagram />}
      />

      <FeatureRow
        id="subscriptions"
        index="04"
        eyebrow="Realtime"
        title="Subscriptions over WebSocket and Server-Sent Events."
        body="[SubscriptionType] with [Topic] placeholders gives you dynamic per-resource streams. Pick a transport: modern graphql-ws or graphql-sse for HTTP/2 and proxy-friendly delivery. Pick a pub/sub provider: in-memory for dev, Redis, NATS, Postgres LISTEN/NOTIFY, or RabbitMQ for production."
        bullets={[
          "Dynamic topics derived from arguments via [Topic], or your own subscribe resolver.",
          "Provider-agnostic publishing via ITopicEventSender from any service.",
          "@defer and @stream stream partial responses on the same connection.",
        ]}
        visual={<SubscriptionsDiagram />}
        reverse
      />

      <FeatureRow
        id="otel"
        index="05"
        eyebrow="Observability"
        title="OpenTelemetry, native and vendor-neutral."
        body="AddInstrumentation() + AddHotChocolateInstrumentation() wires Hot Chocolate into the proposed GraphQL OTel semantic conventions. Spans carry operation type, document hash, trusted document id, per-field selection, and DataLoader batch size. Configure an OTLP exporter and the traces land in whatever backend you already run."
        bullets={[
          "Three diagnostic layers: server transport, execution pipeline, DataLoader.",
          "Low-cardinality root span names by design, ActivityEnricher for custom data.",
          "Works with Jaeger, Tempo, Datadog, Honeycomb, or any OTLP endpoint.",
        ]}
        visual={<OtelDiagram />}
      />

      <FeatureRow
        id="federation"
        index="06"
        eyebrow="Federation"
        title="Fusion-ready, and Apollo Federation spec compatible."
        body="The same Hot Chocolate server runs three ways. As a single API. As a Fusion subgraph composed at build time into a planned gateway schema. As an Apollo Federation subgraph for teams already in that ecosystem. The resolvers do not change. The choice is operational, not architectural."
        bullets={[
          "Start with one server, add Fusion only when you actually need to.",
          "Apollo Federation spec implemented via the ApolloFederation package.",
          "Cost analysis (@cost, @listSize) and trusted operations apply at every tier.",
        ]}
        visual={<FederationDiagram />}
        reverse
      />

      {/* The lone embedded Nitro product card: GraphQL IDE running against a HC server. */}
      <section className="border-cc-card-border border-t py-20 sm:py-24">
        <div className="mb-10 grid items-end gap-6 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <IndexTag value="07" />
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              A GraphQL IDE ships with every server.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Run your server and the Nitro GraphQL IDE is served from the
              endpoint. Browse the schema, draft operations against your live
              resolvers, inspect responses, and share documents with the rest of
              the team.
            </p>
          </div>
          <div className="lg:col-span-5 lg:text-right">
            <p className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
              live at /graphql
            </p>
          </div>
        </div>
        <div className="border-cc-card-border bg-cc-surface mx-auto max-w-5xl overflow-hidden rounded-xl border">
          <NitroCompose />
        </div>
      </section>

      {/* MIT / open source proof band. */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>MIT licensed</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Open source, in production, and free to use.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Hot Chocolate has been developed in the open for years and is
              released under the MIT license. Use it in commercial work, fork
              it, vendor it, audit it. The codebase, the issue tracker, the
              roadmap, and the release notes all live on GitHub.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/hotchocolate">
                Read the docs
              </OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Runtime" value=".NET / ASP.NET Core" />
              <ProofItem label="Spec" value="GraphQL 2025" />
              <ProofItem label="Transports" value="HTTP / WS / SSE" />
              <ProofItem label="Federation" value="Fusion + Apollo" />
              <ProofItem label="Client" value="Strawberry Shake" />
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA. This is where the single brand-spectrum hairline appears. */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            Ship a GraphQL API your .NET team can actually own.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            A C# project, a partial class, a few attributes. The schema, the
            DataLoaders, and the resolver pipeline are generated for you at
            build time, and the runtime is the ASP.NET Core you already run.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
