import type { Metadata } from "next";
import type { ReactNode } from "react";

import { FromOurBlog } from "@/src/components/FromOurBlog";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { NitroPricing } from "@/src/components/home/NitroPricing";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroReel, NitroSchema } from "@/src/nitro";

export const metadata: Metadata = {
  title: "ChilliCream: Ship GraphQL on .NET without guesswork",
  description:
    "The end-to-end GraphQL platform for .NET. Build the graph in C#, observe production with Nitro, evolve safely, and let agents help. Open source, MIT.",
  keywords: [
    "GraphQL",
    ".NET GraphQL platform",
    "Hot Chocolate",
    "Strawberry Shake",
    "Nitro",
    "Mocha",
    "Green Donut",
    "Cookie Crumble",
    "Fusion",
    "Federation",
    "C# GraphQL",
    "GraphQL observability",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "ChilliCream: Ship GraphQL on .NET without guesswork",
    description:
      "The end-to-end GraphQL platform for .NET: build in C#, observe with Nitro, evolve without breaking consumers, and let agents help. Open source, MIT.",
    type: "website",
  },
};

// Brand spectrum gradient. Used exactly once on this screen, on the headline wedge.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// ---------------------------------------------------------------------------
// Shared primitives
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

interface StepBadgeProps {
  readonly index: string;
  readonly label: string;
}

function StepBadge({ index, label }: StepBadgeProps) {
  return (
    <div className="inline-flex items-center gap-3">
      <span className="border-cc-card-border bg-cc-card-bg text-cc-accent inline-flex h-8 min-w-8 items-center justify-center rounded-full border px-2 font-mono text-xs tracking-wider">
        {index}
      </span>
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.25em] uppercase">
        {label}
      </span>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Hero
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="relative isolate mx-auto max-w-5xl px-5 pt-20 pb-10 text-center sm:px-12 sm:pt-28 sm:pb-14">
      <Eyebrow>The GraphQL platform for .NET</Eyebrow>
      <h1 className="text-cc-heading font-heading text-h2 sm:text-h1 mt-6 font-semibold tracking-[-0.02em] text-balance">
        Ship GraphQL{" "}
        <span
          className="bg-clip-text text-transparent"
          style={{ backgroundImage: SPECTRUM }}
        >
          without guesswork
        </span>
        .
      </h1>
      <p className="text-cc-ink lead mx-auto mt-6 max-w-2xl text-pretty">
        From the first resolver to the federated production graph, ChilliCream
        gives .NET teams one set of tools to build, observe, and evolve GraphQL.
        Open source at the core, premium where it counts.
      </p>
      <div className="mt-9 flex flex-wrap justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch Nitro
        </OutlineButton>
      </div>
      <div className="text-cc-ink-dim mt-7 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase">
        <span>MIT licensed</span>
        <span aria-hidden className="text-cc-ink-faint">
          /
        </span>
        <span>ASP.NET Core</span>
        <span aria-hidden className="text-cc-ink-faint">
          /
        </span>
        <span>GraphQL 2025 spec</span>
        <span aria-hidden className="text-cc-ink-faint">
          /
        </span>
        <span>Fusion-ready</span>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Arc intro
// ---------------------------------------------------------------------------

function ArcIntro() {
  return (
    <section className="mx-auto max-w-5xl px-5 pt-12 pb-8 text-center sm:px-12 sm:pt-16 sm:pb-10">
      <Eyebrow>The arc</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-3 text-3xl tracking-tight sm:text-4xl">
        Four moves from a fresh project to a federated graph.
      </h2>
      <p className="text-cc-ink-dim lead mx-auto mt-4 max-w-2xl">
        Each step is its own product surface, designed to be picked up
        independently. Adopt them in order, or wherever the pain lives today.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Step 01 - Build
// ---------------------------------------------------------------------------

function StepBuild() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-12 sm:px-12 sm:py-16">
      <div className="grid gap-10 lg:grid-cols-2 lg:items-center">
        <div>
          <StepBadge index="01" label="Build the graph in C#" />
          <h2 className="text-cc-heading font-heading mt-5 text-3xl tracking-tight sm:text-4xl">
            Resolvers are methods. The schema is your code.
          </h2>
          <p className="text-cc-ink-dim lead mt-5">
            Hot Chocolate is the .NET-native GraphQL server. Annotate partial
            classes with [QueryType] and [MutationType], and a Roslyn source
            generator emits the schema, resolver pipeline, and DataLoader
            infrastructure at build time. One server speaks HTTP, WebSocket,
            SSE, REST, and MCP.
          </p>
          <ul className="mt-6 space-y-3">
            {[
              "Source-generated schema, refactor-safe with nameof",
              "EF Core, Marten, MongoDB, RavenDB out of the box",
              "Strawberry Shake adds typed C# clients via MSBuild code generation",
            ].map((item) => (
              <li
                key={item}
                className="text-cc-ink flex items-start gap-3 text-sm"
              >
                <span
                  aria-hidden
                  className="bg-cc-accent mt-2 inline-block h-1.5 w-1.5 shrink-0 rounded-full"
                />
                <span>{item}</span>
              </li>
            ))}
          </ul>
          <div className="mt-7 flex flex-wrap gap-4">
            <OutlineButton href="/products/hotchocolate">
              Hot Chocolate
            </OutlineButton>
            <OutlineButton href="/products/strawberryshake">
              Strawberry Shake
            </OutlineButton>
          </div>
        </div>

        <CodeStill />
      </div>
    </section>
  );
}

function CodeStill() {
  // Inline "code still": a styled card mimicking an editor frame. Pure SVG/HTML, no images.
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-1 shadow-2xl shadow-black/30">
      <div className="border-cc-card-border bg-cc-surface flex items-center gap-2 rounded-t-xl border-b px-4 py-2.5">
        <span
          className="h-2.5 w-2.5 rounded-full bg-[#f0786a]/70"
          aria-hidden
        />
        <span
          className="h-2.5 w-2.5 rounded-full bg-[#7c92c6]/70"
          aria-hidden
        />
        <span
          className="h-2.5 w-2.5 rounded-full bg-[#16b9e4]/70"
          aria-hidden
        />
        <span className="text-cc-ink-dim ml-3 font-mono text-xs">
          QueryType.cs
        </span>
      </div>
      <pre className="text-cc-ink overflow-x-auto rounded-b-xl px-5 py-5 font-mono text-[13px] leading-6">
        <code>
          <span className="text-cc-ink-dim">
            {"// Resolvers are just methods."}
          </span>
          {"\n"}
          <span className="text-[#7c92c6]">[QueryType]</span>
          {"\n"}
          <span className="text-[#16b9e4]">public partial class</span>{" "}
          <span className="text-cc-heading">Query</span>
          {"\n"}
          {"{"}
          {"\n"}
          {"    "}
          <span className="text-[#16b9e4]">public static</span>{" "}
          <span className="text-[#5eead4]">Task&lt;Book&gt;</span>{" "}
          <span className="text-cc-heading">GetBookAsync</span>({"\n"}
          {"        "}
          <span className="text-[#5eead4]">int</span> id,
          {"\n"}
          {"        "}
          <span className="text-[#5eead4]">BookByIdDataLoader</span> books,
          {"\n"}
          {"        "}
          <span className="text-[#5eead4]">CancellationToken</span> ct){"\n"}
          {"        "}
          =&gt; books.LoadAsync(id, ct);
          {"\n"}
          {"}"}
          {"\n\n"}
          <span className="text-[#7c92c6]">[DataLoader]</span>
          {"\n"}
          <span className="text-[#16b9e4]">internal static async</span>{" "}
          <span className="text-[#5eead4]">Task&lt;</span>
          {"\n"}
          {"    "}
          <span className="text-[#5eead4]">
            IReadOnlyDictionary&lt;int, Book&gt;&gt;
          </span>{" "}
          <span className="text-cc-heading">BookByIdAsync</span>({"\n"}
          {"        "}
          <span className="text-[#5eead4]">IReadOnlyList&lt;int&gt;</span> ids,
          {"\n"}
          {"        "}BookService svc,
          {"\n"}
          {"        "}
          <span className="text-[#5eead4]">CancellationToken</span> ct)
          {"\n"}
          {"    "}=&gt; (<span className="text-[#16b9e4]">await</span>{" "}
          svc.GetAsync(ids, ct)).ToDictionary(b =&gt; b.Id);
        </code>
      </pre>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Step 02 - Observe (with NitroReel)
// ---------------------------------------------------------------------------

function StepObserve() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-14 sm:px-12 sm:py-20">
      <div className="mx-auto max-w-3xl text-center">
        <StepBadge index="02" label="Watch what it does" />
        <h2 className="text-cc-heading font-heading mt-5 text-3xl tracking-tight sm:text-4xl">
          Nitro is the product behind it.
        </h2>
        <p className="text-cc-ink-dim lead mx-auto mt-5">
          Every request becomes a trace, every field becomes a metric, every
          deployment becomes a story. Nitro ingests Hot Chocolate telemetry the
          moment you wire it up, then turns it into operation insights, schema
          health, and slow-field forensics. One workspace for the whole graph.
        </p>
      </div>

      <figure className="mx-auto mt-10 max-w-5xl">
        <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
          <NitroReel />
        </div>
        <figcaption className="text-cc-ink-dim mt-4 text-center font-mono text-xs tracking-wider uppercase">
          Nitro: author / observe / diagnose / schema / fusion
        </figcaption>
      </figure>

      <div className="mx-auto mt-10 grid max-w-4xl gap-6 sm:grid-cols-3">
        <Stat
          value="P50/P95/P99"
          label="per operation, per field, per client"
        />
        <Stat value="OpenTelemetry" label="OTLP ingest, no proprietary agent" />
        <Stat value="Live" label="GraphQL IDE served from your endpoint" />
      </div>
    </section>
  );
}

interface StatProps {
  readonly value: string;
  readonly label: string;
}

function Stat({ value, label }: StatProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5">
      <div className="text-cc-heading font-heading text-xl">{value}</div>
      <div className="text-cc-ink-dim mt-1 text-sm leading-snug">{label}</div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Step 03 - Evolve (with NitroSchema)
// ---------------------------------------------------------------------------

function StepEvolve() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-14 sm:px-12 sm:py-20">
      <div className="grid gap-12 lg:grid-cols-12 lg:items-center">
        <div className="lg:col-span-5">
          <StepBadge index="03" label="Evolve without breaking consumers" />
          <h2 className="text-cc-heading font-heading mt-5 text-3xl tracking-tight sm:text-4xl">
            Know which fields matter before you touch them.
          </h2>
          <p className="text-cc-ink-dim lead mt-5">
            Schema registry tracks every published version, attributes every
            field to the operations and clients that read it, and warns you when
            a change would affect published clients. Deprecate, monitor usage
            decay, and retire with confidence.
          </p>
          <ul className="mt-6 space-y-3">
            {[
              "Schema versioning with diff and approval flows",
              "Field-level usage from live Nitro traffic",
              "Fusion composes subgraphs at planning time, no runtime stitching",
            ].map((item) => (
              <li
                key={item}
                className="text-cc-ink flex items-start gap-3 text-sm"
              >
                <span
                  aria-hidden
                  className="bg-cc-accent mt-2 inline-block h-1.5 w-1.5 shrink-0 rounded-full"
                />
                <span>{item}</span>
              </li>
            ))}
          </ul>
          <div className="mt-7 flex flex-wrap gap-4">
            <OutlineButton href="/products/fusion">Fusion</OutlineButton>
            <OutlineButton href="/products/nitro">
              Schema in Nitro
            </OutlineButton>
          </div>
        </div>

        <div className="lg:col-span-7">
          <figure className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
            <NitroSchema />
          </figure>
          <p className="text-cc-ink-dim mt-3 text-center font-mono text-xs tracking-wider uppercase">
            Deprecated-field usage, drilled into the operations still calling it
          </p>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Step 04 - Agents
// ---------------------------------------------------------------------------

function StepAgents() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-14 sm:px-12 sm:py-20">
      <div className="grid gap-10 lg:grid-cols-12 lg:items-center">
        <div className="lg:col-span-7">
          <AgentVisual />
        </div>
        <div className="lg:col-span-5">
          <StepBadge index="04" label="Let agents help" />
          <h2 className="text-cc-heading font-heading mt-5 text-3xl tracking-tight sm:text-4xl">
            Your graph, exposed safely to AI.
          </h2>
          <p className="text-cc-ink-dim lead mt-5">
            Call AddMcp() and MapGraphQLMcp() and Hot Chocolate exposes your
            tools and prompts to MCP-aware agents at /graphql/mcp. Mocha keeps
            agent-triggered workflows honest: long-running sagas are validated
            before traffic, with exactly-once processing for the steps that
            cannot replay.
          </p>
          <ul className="mt-6 space-y-3">
            {[
              "MCP server surface served from the same ASP.NET Core app",
              "Mocha sagas validated before traffic, exactly-once processing",
              "Green Donut DataLoaders keep agent fan-out cheap",
            ].map((item) => (
              <li
                key={item}
                className="text-cc-ink flex items-start gap-3 text-sm"
              >
                <span
                  aria-hidden
                  className="bg-cc-accent mt-2 inline-block h-1.5 w-1.5 shrink-0 rounded-full"
                />
                <span>{item}</span>
              </li>
            ))}
          </ul>
          <div className="mt-7 flex flex-wrap gap-4">
            <OutlineButton href="/products/mocha">Mocha</OutlineButton>
            <OutlineButton href="/products/greendonut">
              Green Donut
            </OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

function AgentVisual() {
  // Pure inline SVG: an MCP/agent on the left, the graph on the right, three tool
  // arrows in cyan / violet / coral (the brand spectrum, already used in the hero
  // wordmark; kept subtle here, no fills, so the single-spectrum rule still holds).
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-6 shadow-2xl shadow-black/30">
      <svg
        viewBox="0 0 640 360"
        className="h-auto w-full"
        role="img"
        aria-label="An MCP agent calling three tools on the GraphQL graph"
      >
        <defs>
          <radialGradient id="cc-agent-graph" cx="50%" cy="50%" r="55%">
            <stop offset="0%" stopColor="#5eead4" stopOpacity="0.35" />
            <stop offset="60%" stopColor="#5eead4" stopOpacity="0.05" />
            <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
          </radialGradient>
          <pattern
            id="cc-agent-grid"
            width="32"
            height="32"
            patternUnits="userSpaceOnUse"
          >
            <path
              d="M32 0H0V32"
              fill="none"
              stroke="rgba(255,255,255,0.04)"
              strokeWidth="1"
            />
          </pattern>
        </defs>

        <rect width="640" height="360" fill="url(#cc-agent-grid)" />

        {/* Agent block */}
        <g transform="translate(40 110)">
          <rect
            width="160"
            height="140"
            rx="14"
            fill="rgba(12,19,34,0.9)"
            stroke="rgba(255,255,255,0.12)"
          />
          <text
            x="20"
            y="34"
            fill="#5eead4"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="11"
            letterSpacing="2"
          >
            AGENT
          </text>
          <text
            x="20"
            y="64"
            fill="#f5f7fb"
            fontFamily="ui-sans-serif, system-ui, sans-serif"
            fontSize="14"
            fontWeight="600"
          >
            mcp://your-graph
          </text>
          <text
            x="20"
            y="92"
            fill="rgba(245,247,251,0.55)"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="11"
          >
            tools.list()
          </text>
          <text
            x="20"
            y="112"
            fill="rgba(245,247,251,0.55)"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="11"
          >
            tools.call()
          </text>
        </g>

        {/* Graph blob */}
        <circle cx="490" cy="180" r="120" fill="url(#cc-agent-graph)" />
        <g transform="translate(490 180)">
          <circle r="6" fill="#5eead4" />
          {[
            { x: -70, y: -50 },
            { x: 70, y: -60 },
            { x: -80, y: 40 },
            { x: 60, y: 60 },
            { x: 0, y: -90 },
            { x: 0, y: 90 },
          ].map((p) => (
            <g key={`${p.x}:${p.y}`}>
              <line
                x1="0"
                y1="0"
                x2={p.x}
                y2={p.y}
                stroke="rgba(94,234,212,0.45)"
                strokeWidth="1"
              />
              <circle cx={p.x} cy={p.y} r="4" fill="#5eead4" opacity="0.85" />
            </g>
          ))}
        </g>

        {/* Three tool arrows */}
        {[
          { y: 120, color: "#16b9e4", label: "getOrders" },
          { y: 180, color: "#7c92c6", label: "shipQuote" },
          { y: 240, color: "#f0786a", label: "refund" },
        ].map((tool) => (
          <g key={tool.label}>
            <line
              x1="200"
              y1={tool.y}
              x2="370"
              y2={tool.y}
              stroke={tool.color}
              strokeWidth="1.5"
              strokeDasharray="4 6"
              opacity="0.75"
            />
            <polygon
              points={`370,${tool.y} 360,${tool.y - 5} 360,${tool.y + 5}`}
              fill={tool.color}
              opacity="0.85"
            />
            <text
              x="210"
              y={tool.y - 8}
              fill="rgba(245,247,251,0.7)"
              fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
              fontSize="11"
            >
              {tool.label}()
            </text>
          </g>
        ))}
      </svg>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Product family rail
// ---------------------------------------------------------------------------

interface ProductChipProps {
  readonly name: string;
  readonly tag: string;
  readonly href: string;
}

const PRODUCTS: readonly ProductChipProps[] = [
  {
    name: "Hot Chocolate",
    tag: "GraphQL server",
    href: "/products/hotchocolate",
  },
  {
    name: "Strawberry Shake",
    tag: "Typed C# client",
    href: "/products/strawberryshake",
  },
  { name: "Nitro", tag: "Observability", href: "/products/nitro" },
  { name: "Mocha", tag: "Sagas, jobs, queues", href: "/products/mocha" },
  { name: "Green Donut", tag: "DataLoader", href: "/products/greendonut" },
  {
    name: "Cookie Crumble",
    tag: "Snapshot testing",
    href: "/products/cookiecrumble",
  },
  { name: "Fusion", tag: "Federation", href: "/products/fusion" },
];

function ProductChip({ name, tag, href }: ProductChipProps) {
  return (
    <a
      href={href}
      className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover group flex items-center justify-between gap-4 rounded-xl border px-5 py-4 no-underline transition-colors"
    >
      <div>
        <div className="text-cc-heading font-heading text-base tracking-tight">
          {name}
        </div>
        <div className="text-cc-ink-dim mt-0.5 font-mono text-xs tracking-wider uppercase">
          {tag}
        </div>
      </div>
      <span
        aria-hidden
        className="text-cc-ink-dim group-hover:text-cc-accent text-lg leading-none transition-colors"
      >
        &rarr;
      </span>
    </a>
  );
}

function ProductRail() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-12 sm:px-12 sm:py-16">
      <div className="max-w-3xl">
        <Eyebrow>The family</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-3 text-3xl tracking-tight sm:text-4xl">
          Seven products. One platform you can adopt piece by piece.
        </h2>
        <p className="text-cc-ink-dim lead mt-4">
          Each one stands on its own, and they were designed to compose. Start
          where the pain is.
        </p>
      </div>
      <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {PRODUCTS.map((p) => (
          <ProductChip key={p.name} {...p} />
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Open source band
// ---------------------------------------------------------------------------

function OpenSource() {
  return (
    <section className="mx-auto max-w-5xl px-5 py-12 sm:px-12 sm:py-16">
      <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 sm:p-10">
        <div className="grid gap-8 lg:grid-cols-12 lg:items-center">
          <div className="lg:col-span-8">
            <Eyebrow>Open source at the core</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-2xl tracking-tight sm:text-3xl">
              MIT licensed. Built in the open. Used by Galaxus, Swiss Life, and
              teams inside Microsoft.
            </h2>
            <p className="text-cc-ink-dim mt-4 text-sm leading-relaxed">
              The whole platform lives on GitHub. File an issue, send a PR, fork
              the gateway, vendor the runtime: the rules are MIT and they do not
              change for paying customers. The commercial tier funds the work
              and adds the operator surface around it.
            </p>
          </div>
          <div className="flex flex-wrap gap-3 lg:col-span-4 lg:justify-end">
            <SolidButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </SolidButton>
            <OutlineButton href="/docs">Read the docs</OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Pricing pointer
// ---------------------------------------------------------------------------

function PricingPointer() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-12 sm:px-12 sm:py-16">
      <div className="max-w-3xl">
        <Eyebrow>Pricing</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-3 text-3xl tracking-tight sm:text-4xl">
          Free where it should be, paid where the operator surface lives.
        </h2>
        <p className="text-cc-ink-dim lead mt-4">
          The runtime, clients, sagas, and federation are MIT. Nitro adds the
          managed schema registry, telemetry workspace, and operator features
          your platform team needs.
        </p>
      </div>
      <NitroPricing />
      <div className="mt-6 flex flex-wrap gap-3">
        <OutlineButton href="/pricing">See full pricing</OutlineButton>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Closing CTA
// ---------------------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="mx-auto max-w-4xl px-5 py-20 text-center sm:px-12 sm:py-28">
      <Eyebrow>Your next move</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-4 text-3xl tracking-tight sm:text-5xl">
        Build the graph. Watch it run. Ship the next change with proof.
      </h2>
      <p className="text-cc-ink-dim lead mx-auto mt-6 max-w-2xl">
        Start free, wire up Nitro when you are ready, and call us when the graph
        becomes critical. The platform is the same the whole way.
      </p>
      <div className="mt-9 flex flex-wrap justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch Nitro
        </OutlineButton>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function LandingV3Preview() {
  return (
    <>
      <Hero />
      <LogoCloud />
      <ArcIntro />
      <StepBuild />
      <StepObserve />
      <StepEvolve />
      <StepAgents />
      <ProductRail />
      <OpenSource />
      <PricingPointer />
      <ClosingCta />
      <div className="px-5 py-8 sm:px-12">
        <div className="mx-auto flex max-w-6xl flex-col gap-12">
          <FromOurBlog />
        </div>
      </div>
    </>
  );
}
