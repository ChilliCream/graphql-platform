import type { Metadata } from "next";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Strawberry Shake: Strongly-typed GraphQL Client for .NET",
  description:
    "Strawberry Shake is the open-source, strongly-typed GraphQL client for .NET. MSBuild codegen, normalized reactive store, subscriptions, Blazor and Razor ready.",
  keywords: [
    "Strawberry Shake",
    ".NET GraphQL client",
    "strongly-typed GraphQL",
    "MSBuild codegen",
    "Blazor GraphQL",
    "Razor GraphQL",
    "MAUI GraphQL",
    "GraphQL subscriptions",
    "persisted operations",
    "reactive store",
    "dotnet graphql",
    "Hot Chocolate",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Strawberry Shake: Strongly-typed GraphQL Client for .NET",
    description:
      "Typed C# clients generated from your .graphql operations. Normalized reactive store, subscriptions, Blazor and Razor friendly. MIT-licensed.",
    type: "website",
  },
};

// The brand spectrum, allowed at most ONCE per screen. Used as the hairline on
// the closing CTA. The hero gets the single clipped cyan -> teal accent only.
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
// Token color helpers. GitHub-dark approximations, scoped to inline code panels
// so the rest of the page stays on cc-* tokens.
// -----------------------------------------------------------------------------

const T: Record<string, CSSProperties> = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  num: { color: "#79c0ff" },
  comment: { color: "#8b949e", fontStyle: "italic" },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
  dim: { color: "#8b949e" },
  // GraphQL operation tokens
  gqlKw: { color: "#ff7b72" },
  gqlType: { color: "#ffa657" },
  gqlField: { color: "#7ee787" },
  gqlVar: { color: "#79c0ff" },
};

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

// -----------------------------------------------------------------------------
// Hero code card. Two stacked window panels:
//   - top: a small GetProduct.graphql operation (the contract the team writes)
//   - bottom: the call site, var result = await client.GetProduct.ExecuteAsync(id);
// One clipped cyan -> teal gradient anchored in the top-left is the lone color
// event in the hero. Window chrome lives in the cc-* token surface.
// -----------------------------------------------------------------------------

function HeroCodePanel() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      {/* Clipped cyan -> teal gradient. Single color event for the hero. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 180px at 14% 18%, rgba(94, 234, 212, 0.18), transparent 70%), radial-gradient(280px 140px at 8% 12%, rgba(22, 185, 228, 0.18), transparent 70%)",
        }}
      />

      {/* Window chrome */}
      <div className="bg-cc-code-header border-cc-card-border relative flex items-center gap-2 border-b px-4 py-3">
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
          Catalog/GetProduct.graphql
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          GraphQL
        </span>
      </div>

      {/* GraphQL operation */}
      <div className="relative py-3">
        <CodeLine n={1}>
          <span style={T.gqlKw}>query</span>{" "}
          <span style={T.gqlType}>GetProduct</span>
          <span style={T.punct}>(</span>
          <span style={T.gqlVar}>$id</span>
          <span style={T.punct}>: </span>
          <span style={T.gqlType}>ID!</span>
          <span style={T.punct}>) {`{`}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={T.plain}>{`  `}</span>
          <span style={T.gqlField}>productById</span>
          <span style={T.punct}>(</span>
          <span style={T.gqlField}>id</span>
          <span style={T.punct}>: </span>
          <span style={T.gqlVar}>$id</span>
          <span style={T.punct}>) {`{`}</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={T.plain}>{`    `}</span>
          <span style={T.gqlField}>id</span>
          <span style={T.plain}> </span>
          <span style={T.gqlField}>name</span>
          <span style={T.plain}> </span>
          <span style={T.gqlField}>priceCents</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={T.plain}>{`    `}</span>
          <span style={T.gqlField}>variants</span>
          <span style={T.punct}> {`{`} </span>
          <span style={T.gqlField}>sku</span>
          <span style={T.plain}> </span>
          <span style={T.gqlField}>inStock</span>
          <span style={T.punct}> {`}`}</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={T.plain}>{`  `}</span>
          <span style={T.punct}>{`}`}</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={T.punct}>{`}`}</span>
        </CodeLine>
      </div>

      {/* Bridge bar: what MSBuild does between the two panels. */}
      <div className="border-cc-card-border bg-cc-code-header text-cc-ink-dim flex items-center justify-between gap-3 border-y px-4 py-2 font-mono text-[10.5px] tracking-tight">
        <span className="inline-flex items-center gap-2">
          <span
            className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
            aria-hidden
          />
          dotnet build
        </span>
        <span className="text-cc-accent">
          MSBuild codegen emits typed client
        </span>
        <span>Catalog.Client.cs</span>
      </div>

      {/* C# call site against the generated client */}
      <div className="relative py-3">
        <CodeLine n={1}>
          <span style={T.kw}>using</span>{" "}
          <span style={T.plain}>Catalog.Client;</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={3}>
          <span
            style={T.comment}
          >{`// Resolve the generated, strongly-typed client from DI.`}</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={T.kw}>var</span> <span style={T.param}>client</span>{" "}
          <span style={T.punct}>=</span> <span style={T.param}>services</span>
          <span style={T.punct}>.</span>
          <span style={T.fn}>GetRequiredService</span>
          <span style={T.punct}>{`<`}</span>
          <span style={T.type}>ICatalogClient</span>
          <span style={T.punct}>{`>`}</span>
          <span style={T.punct}>();</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={T.kw}>var</span> <span style={T.param}>result</span>{" "}
          <span style={T.punct}>=</span> <span style={T.kw}>await</span>{" "}
          <span style={T.param}>client</span>
          <span style={T.punct}>.</span>
          <span style={T.plain}>GetProduct</span>
          <span style={T.punct}>.</span>
          <span style={T.fn}>ExecuteAsync</span>
          <span style={T.punct}>(</span>
          <span style={T.param}>id</span>
          <span style={T.punct}>);</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={8}>
          <span
            style={T.comment}
          >{`// result.Data.ProductById is a typed record. Errors are aggregated.`}</span>
        </CodeLine>
        <CodeLine n={9}>
          <span style={T.kw}>if</span> <span style={T.punct}>(</span>
          <span style={T.param}>result</span>
          <span style={T.punct}>.</span>
          <span style={T.plain}>IsErrorResult</span>
          <span style={T.punct}>())</span>
        </CodeLine>
        <CodeLine n={10}>
          <span style={T.plain}>{`    `}</span>
          <span style={T.kw}>throw new</span>{" "}
          <span style={T.type}>GraphQLClientException</span>
          <span style={T.punct}>(</span>
          <span style={T.param}>result</span>
          <span style={T.punct}>.</span>
          <span style={T.plain}>Errors</span>
          <span style={T.punct}>);</span>
        </CodeLine>
        <CodeLine n={11}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={12}>
          <span style={T.type}>Product</span>{" "}
          <span style={T.param}>product</span> <span style={T.punct}>=</span>{" "}
          <span style={T.param}>result</span>
          <span style={T.punct}>.</span>
          <span style={T.plain}>Data</span>
          <span style={T.punct}>!.</span>
          <span style={T.plain}>ProductById</span>
          <span style={T.punct}>;</span>
        </CodeLine>
      </div>

      {/* Footer caption */}
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
        <span>build: typed client + records + DI registration emitted</span>
        <span className="text-cc-accent">dotnet graphql, MSBuild codegen</span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Alternating feature row.
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
// Inline code panel used inside feature rows. Smaller than the hero card.
// -----------------------------------------------------------------------------

interface InlineCodePanelProps {
  readonly file: string;
  readonly tag: string;
  readonly lines: ReactNode;
  readonly footer?: ReactNode;
}

function InlineCodePanel({ file, tag, lines, footer }: InlineCodePanelProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[11px]">{file}</span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {tag}
        </span>
      </div>
      <div className="py-3">{lines}</div>
      {footer ? (
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-3 border-t px-4 py-2 font-mono text-[10.5px]">
          {footer}
        </div>
      ) : null}
    </div>
  );
}

// -----------------------------------------------------------------------------
// Per-row code snippets and inline SVG diagrams.
// -----------------------------------------------------------------------------

function GraphqlrcSnippet() {
  return (
    <InlineCodePanel
      file=".graphqlrc.json"
      tag="JSON"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>{`{`}</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.str}>&quot;schema&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;schema.graphql&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.str}>&quot;documents&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;**/*.graphql&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.str}>&quot;extensions&quot;</span>
            <span style={T.punct}>: {`{`}</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.str}>&quot;strawberryShake&quot;</span>
            <span style={T.punct}>: {`{`}</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.str}>&quot;name&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;CatalogClient&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.str}>&quot;namespace&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;Catalog.Client&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.str}>&quot;url&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>
              &quot;https://api.example.com/graphql&quot;
            </span>
          </CodeLine>
          <CodeLine n={9}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
          <CodeLine n={10}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
          <CodeLine n={11}>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>dotnet graphql init &amp; dotnet graphql update</span>
          <span className="text-cc-accent">CLI</span>
        </>
      }
    />
  );
}

/** Normalized store diagram: two queries, one entity row in the cache. */
function ReactiveStoreDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Two queries denormalized into one entity row, components subscribe to changes"
    >
      <defs>
        <linearGradient id="ss-store-line" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.1" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
        </linearGradient>
      </defs>
      {[
        { y: 24, label: "GetProduct(id: 42)" },
        { y: 84, label: "ListProducts(first: 10)" },
      ].map((q) => (
        <g key={q.label}>
          <rect
            x="12"
            y={q.y}
            width="170"
            height="34"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="22"
            y={q.y + 21}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#a1a3af"
          >
            {q.label}
          </text>
          <path
            d={`M 182 ${q.y + 17} C 230 ${q.y + 17}, 230 110, 270 110`}
            stroke="url(#ss-store-line)"
            strokeWidth="1.5"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="270"
        y="86"
        width="130"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="335"
        y="106"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        EntityStore
      </text>
      <text
        x="335"
        y="122"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        Product#42 (one row)
      </text>
      {[156, 188].map((y, i) => (
        <g key={y}>
          <path
            d={`M 400 110 C 432 110, 432 ${y}, 462 ${y}`}
            stroke="rgba(94,234,212,0.45)"
            strokeWidth="1.2"
            fill="none"
          />
          <text
            x="462"
            y={y + 3}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {i === 0 ? "Watch()" : "UseQuery"}
          </text>
        </g>
      ))}
      <text
        x="12"
        y="180"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        normalized, deduplicated, reactive
      </text>
    </svg>
  );
}

/** Fetch strategies diagram. */
function FetchStrategiesDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Three fetch strategies: cache-first, network-only, cache-and-network"
    >
      {[
        {
          y: 16,
          name: "CacheFirst",
          desc: "store hit returns first, no request",
        },
        {
          y: 84,
          name: "NetworkOnly",
          desc: "always fetch, then update the store",
        },
        {
          y: 152,
          name: "CacheAndNetwork",
          desc: "yield cache, refresh in the background",
        },
      ].map((s) => (
        <g key={s.name}>
          <rect
            x="12"
            y={s.y}
            width="180"
            height="48"
            rx="8"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.5)"
          />
          <text
            x="24"
            y={s.y + 20}
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {s.name}
          </text>
          <text
            x="24"
            y={s.y + 38}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {s.desc}
          </text>
          <path
            d={`M 192 ${s.y + 24} L 290 ${s.y + 24}`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
          <polygon
            points={`286,${s.y + 20} 298,${s.y + 24} 286,${s.y + 28}`}
            fill="rgba(94,234,212,0.55)"
          />
        </g>
      ))}
      <rect
        x="298"
        y="50"
        width="160"
        height="120"
        rx="10"
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="378"
        y="76"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#f5f0ea"
      >
        client.GetProduct
      </text>
      <text
        x="378"
        y="96"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        .Watch(strategy)
      </text>
      <text
        x="378"
        y="120"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        per call,
      </text>
      <text
        x="378"
        y="134"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        or set globally
      </text>
      <text
        x="378"
        y="156"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#5eead4"
      >
        IObservable&lt;Result&gt;
      </text>
    </svg>
  );
}

/** MSBuild codegen flow: .graphql + schema in, .cs out, at build time. */
function CodegenFlowDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="dotnet build runs MSBuild codegen across .graphql operations and schema.graphql to emit typed C# clients"
    >
      {[
        { y: 24, label: "schema.graphql", sub: "downloaded by CLI" },
        { y: 88, label: "GetProduct.graphql", sub: "your operations" },
        { y: 152, label: ".graphqlrc.json", sub: "name + namespace" },
      ].map((n) => (
        <g key={n.label}>
          <rect
            x="12"
            y={n.y}
            width="170"
            height="42"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="22"
            y={n.y + 18}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#f5f0ea"
          >
            {n.label}
          </text>
          <text
            x="22"
            y={n.y + 32}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.55)"
          >
            {n.sub}
          </text>
          <path
            d={`M 182 ${n.y + 21} L 270 110`}
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="270"
        y="86"
        width="110"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="325"
        y="106"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        MSBuild
      </text>
      <text
        x="325"
        y="122"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        codegen task
      </text>
      <path
        d="M 380 110 L 432 110"
        stroke="rgba(94,234,212,0.7)"
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="432,106 444,110 432,114" fill="rgba(94,234,212,0.7)" />
      <text
        x="468"
        y="100"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#f5f0ea"
      >
        CatalogClient.cs
      </text>
      <text
        x="468"
        y="116"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        records, DI, store
      </text>
    </svg>
  );
}

function SubscriptionCodeSnippet() {
  return (
    <InlineCodePanel
      file="PriceTicker.razor"
      tag="Razor"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>UseSubscription</span>{" "}
            <span style={T.param}>TResult</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;OnPriceChangedResult&quot;</span>{" "}
            <span style={T.param}>Subscribe</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>
              &quot;@(c =&gt; c.OnPriceChanged.Watch(sku))&quot;
            </span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>@</span>
            <span style={T.kw}>if</span>{" "}
            <span style={T.punct}>(context.Data is {} d)</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>{`{`}</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>span</span>
            <span style={T.punct}>&gt;@d.PriceChanged.PriceCents&lt;/</span>
            <span style={T.type}>span</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>UseSubscription</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>WebSocket transport, store-backed re-render</span>
          <span className="text-cc-accent">@OnPriceChanged</span>
        </>
      }
    />
  );
}

function CsprojCodeSnippet() {
  return (
    <InlineCodePanel
      file="Catalog.Client.csproj"
      tag="MSBuild"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>Project</span> <span style={T.attr}>Sdk</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;Microsoft.NET.Sdk&quot;</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>PropertyGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>TargetFramework</span>
            <span style={T.punct}>&gt;</span>
            <span style={T.plain}>net9.0</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>TargetFramework</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>PropertyGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ItemGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>PackageReference</span>{" "}
            <span style={T.attr}>Include</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;StrawberryShake.Server&quot;</span>{" "}
            <span style={T.punct}>/&gt;</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>PackageReference</span>{" "}
            <span style={T.attr}>Include</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;StrawberryShake.Tools&quot;</span>{" "}
            <span style={T.attr}>PrivateAssets</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;all&quot;</span>{" "}
            <span style={T.punct}>/&gt;</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ItemGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={9}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>Project</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>dotnet tool install StrawberryShake.Tools</span>
          <span className="text-cc-accent">build-time only</span>
        </>
      }
    />
  );
}

function BlazorRazorSnippet() {
  return (
    <InlineCodePanel
      file="ProductCard.razor"
      tag="Blazor"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>UseQuery</span>{" "}
            <span style={T.param}>TResult</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;GetProductResult&quot;</span>{" "}
            <span style={T.param}>Operation</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>
              &quot;@(c =&gt; c.GetProduct.Watch(Id))&quot;
            </span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>Pending</span>
            <span style={T.punct}>&gt;Loading...&lt;/</span>
            <span style={T.type}>Pending</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>Error</span>{" "}
            <span style={T.param}>Context</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;errors&quot;</span>
            <span style={T.punct}>&gt;@errors[0].Message&lt;/</span>
            <span style={T.type}>Error</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ChildContent</span>{" "}
            <span style={T.param}>Context</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;result&quot;</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>h2</span>
            <span style={T.punct}>&gt;@result.Data!.ProductById.Name&lt;/</span>
            <span style={T.type}>h2</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>UseQuery</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>UseQuery, UseSubscription, UseFragment</span>
          <span className="text-cc-accent">StrawberryShake.Razor</span>
        </>
      }
    />
  );
}

// -----------------------------------------------------------------------------
// Open source proof items.
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

export default function StrawberryShakePreviewV1() {
  return (
    <>
      {/* HERO: copy left, dual code panel right. One color event total. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>GraphQL client for .NET</Eyebrow>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Your .graphql files are the contract.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Strawberry Shake is the open-source, strongly-typed GraphQL client
              for .NET. Drop your operations into .graphql files, run
              <code className="text-cc-accent mx-1 font-mono text-base">
                dotnet build
              </code>
              , and MSBuild codegen emits a typed client, immutable records, a
              normalized reactive store, and the DI registration. Call sites
              read like ordinary async C# against IDE-completed methods.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/strawberryshake">
                Get Started
              </SolidButton>
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
                  Runtimes
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">.NET, Blazor, MAUI</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Codegen
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">
                  MSBuild, build time
                </dd>
              </div>
            </dl>
          </div>
          <div className="lg:col-span-7">
            <HeroCodePanel />
          </div>
        </div>
      </section>

      {/* Quick capability strip */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-6">
          {[
            "MSBuild code generation",
            "Normalized entity store",
            "Three fetch strategies",
            "WebSocket subscriptions",
            "Persisted operations",
            "Blazor and Razor ready",
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

      {/* FIVE feature rows, alternating sides, each with a concrete code or diagram. */}
      <FeatureRow
        id="strongly-typed"
        index="01"
        eyebrow="Strongly-typed Client"
        title="Operations in .graphql, typed C# at the call site."
        body="Queries, mutations, and subscriptions live in plain .graphql files next to the code that uses them. The schema lives in a schema.graphql file pulled from any spec-compliant server. The CLI reads the .graphqlrc.json config, MSBuild emits the typed client class, the result records, the fragments, and the DI registration, and the call sites are ordinary async C# with IntelliSense and refactor support."
        bullets={[
          "Operations are valid GraphQL documents you can hand to any tool.",
          "Generated records are nullable-aware, immutable, and deconstructible.",
          "Compatible with any GraphQL spec server, not only Hot Chocolate.",
        ]}
        visual={<GraphqlrcSnippet />}
      />

      <FeatureRow
        id="reactive-store"
        index="02"
        eyebrow="Reactive Store"
        title="A normalized entity store, with Relay and Apollo vocabulary."
        body="Strawberry Shake denormalizes every GraphQL result into an entity store keyed by type and id, the same model Relay and Apollo made the standard for client caches. A query that returns the same product as a list and as a detail shares one row. Watch a query and your component re-renders when that row changes, no matter which operation produced the update."
        bullets={[
          "IObservable<Result> on every Watch(), Razor and Blazor wrappers wire it for you.",
          "Mutations write back into the store, related queries refresh automatically.",
          "Persist the store to SQLite or LiteDB and rehydrate it on next launch.",
        ]}
        visual={<ReactiveStoreDiagram />}
        reverse
      />

      <FeatureRow
        id="fetch-strategies"
        index="03"
        eyebrow="Fetch Strategies"
        title="CacheFirst, NetworkOnly, CacheAndNetwork. Set globally, override per call."
        body="Every operation supports three execution strategies. CacheFirst returns the store entry without a request when it has one. NetworkOnly always fetches and writes the result through the store. CacheAndNetwork yields the cached entry immediately and refreshes in the background, which is the strategy that powers fast launches and snappy detail pages."
        bullets={[
          "Strategy is a per-Watch override on top of a per-client default.",
          "Combine with persisted state for instant cache hits on cold start.",
          "Result stream emits both cache and network values, in order.",
        ]}
        visual={<FetchStrategiesDiagram />}
      />

      <FeatureRow
        id="subscriptions"
        index="04"
        eyebrow="Subscriptions"
        title="Realtime over WebSocket, into the same store."
        body="Subscription operations look like queries: declare them in a .graphql file, get a typed Watch on the generated client. The WebSocket transport carries a connection_init payload for auth and reconnect handling. Pushed values write through the same entity store, so any open query, fragment, or Razor component sees the update."
        bullets={[
          "Token refresh and reconnect are part of the transport, not your code.",
          "UseSubscription Razor component lifts updates straight into Blazor markup.",
          "Same Watch surface as queries, no separate event-handler pipeline.",
        ]}
        visual={<SubscriptionCodeSnippet />}
        reverse
      />

      <FeatureRow
        id="msbuild-codegen"
        index="05"
        eyebrow="MSBuild Code Generation"
        title="Code generation that runs with dotnet build, not at runtime."
        body="Strawberry Shake generates code through MSBuild tasks driven by the dotnet graphql CLI, not through runtime IL weaving and not through reflection. Add the StrawberryShake.Tools NuGet, point the .graphqlrc.json at your schema, and dotnet build emits .NET source for the client, operations, fragments, records, and the DI extension method. Stale generated code is a build error, not a runtime surprise."
        bullets={[
          "dotnet graphql init scaffolds the config and downloads the schema in one step.",
          "dotnet graphql update keeps the local schema in sync with the server.",
          "MSBuild codegen runs in CI on every build, the same way your project compiles.",
        ]}
        visual={<CsprojCodeSnippet />}
      />

      <FeatureRow
        id="blazor-razor"
        index="06"
        eyebrow="Blazor and Razor"
        title="Razor components built on the reactive store."
        body="StrawberryShake.Razor ships UseQuery, UseSubscription, UseFragment, and a DataComponent base that bind generated operations to Blazor markup. Pending, Error, and ChildContent slots cover the common UI states, and every component reacts to the entity store, so a mutation in one corner of the app re-renders dependent components everywhere."
        bullets={[
          "Server, WebAssembly, and hybrid Blazor projects all use the same client.",
          "Fragments map to typed sub-records you can reuse across components.",
          "Works inside .NET MAUI for typed GraphQL on iOS, Android, and desktop.",
        ]}
        visual={<BlazorRazorSnippet />}
        reverse
      />

      <FeatureRow
        id="codegen-flow"
        index="07"
        eyebrow="Build pipeline"
        title="One pipeline, from .graphql to typed records."
        body="The full picture: the CLI keeps your schema.graphql current, .graphqlrc.json names the client, your operations live in .graphql files, MSBuild runs the codegen task during dotnet build, and the project compiles against the freshly emitted .cs. Nothing happens at runtime that you did not write or that the build did not emit."
        bullets={[
          "No reflection at request time, no surprise startup work to scan documents.",
          "Persisted operations: ship operation hashes, lock production to known docs.",
          "Codegen output is checked source, diffable in code review.",
        ]}
        visual={<CodegenFlowDiagram />}
      />

      {/* The lone embedded Nitro product card: the GraphQL IDE the client pairs with. */}
      <section className="border-cc-card-border border-t py-20 sm:py-24">
        <div className="mb-10 grid items-end gap-6 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <IndexTag value="08" />
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Draft the operation, then check in the .graphql file.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Nitro is the GraphQL IDE that ships with the Hot Chocolate server,
              and it is the same surface most teams use to draft operations
              before saving them as .graphql files in the client project. Browse
              the schema, run a query against the live server, and copy the
              document into the codegen pipeline.
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

      {/* MIT / open source proof band */}
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
              Strawberry Shake is part of the ChilliCream GraphQL platform and
              is released under the MIT license. Use it in commercial work, fork
              it, vendor it, audit it. The codebase, issues, and release notes
              all live on GitHub next to Hot Chocolate.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/strawberryshake">
                Read the docs
              </OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Codegen" value="MSBuild" />
              <ProofItem label="Store" value="Normalized" />
              <ProofItem label="Transports" value="HTTP / WebSocket" />
              <ProofItem label="UI" value="Blazor / Razor" />
              <ProofItem label="Server" value="Hot Chocolate" />
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA with the single brand-spectrum hairline */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            A typed GraphQL client your .NET team can actually own.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            A few .graphql files, a .graphqlrc.json, and a NuGet reference. The
            client, the records, the store, and the DI wiring are emitted for
            you at build time, and the runtime is the .NET you already ship.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
