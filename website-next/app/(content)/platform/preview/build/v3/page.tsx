import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Build Loop: Ship From the Code That Runs It",
  description:
    "Implementation-first GraphQL in C#. Hot Chocolate source-generates schema, resolvers, and DataLoaders from annotated classes; Strawberry Shake emits typed clients. No schema file to drift.",
  keywords: [
    "Hot Chocolate",
    "GraphQL C#",
    "source generated GraphQL",
    "implementation-first GraphQL",
    "Strawberry Shake typed client",
    "DataLoader batching",
    ".NET GraphQL server",
    "QueryType source generator",
  ],
  openGraph: {
    title: "Ship From the Code That Runs It",
    description:
      "One annotated C# class becomes your schema, resolver pipeline, DataLoaders, and a typed .NET client. Every generated artifact traces back to the code that runs it.",
  },
  robots: { index: false, follow: false },
};

// The single color event for this scene: a clipped cyan -> teal gradient.
const SCENE_GRADIENT = "linear-gradient(105deg, #16b9e4 0%, #5eead4 100%)";

export default function BuildLoopPreviewPage() {
  return (
    <main className="flex flex-col gap-28 pb-12">
      <Hero />
      <LineageSection />
      <BentoSection />
      <ComparisonSection />
      <HonestySection />
      <ClosingCta />
    </main>
  );
}

/* ------------------------------------------------------------------ *
 * Hero: spotlight in the scene accent, the headline straddles a real
 * C# editor card that is the source of everything downstream.
 * ------------------------------------------------------------------ */

function Hero() {
  return (
    <section className="relative isolate pt-10">
      <SpotlightMesh />
      <div className="relative flex flex-col items-center text-center">
        <span className="font-mono text-xs tracking-[0.28em] text-cc-nav-label uppercase">
          The build loop
        </span>
        <h1 className="mt-6 max-w-3xl font-heading text-h2 text-cc-heading sm:text-h1">
          Ship from the code{" "}
          <span className="relative whitespace-nowrap">
            that runs it
            <GradientUnderline />
          </span>
          .
        </h1>
        <p className="lead mt-7 max-w-2xl text-balance text-cc-prose">
          You write annotated C#. The schema, resolver pipeline, DataLoaders,
          and a typed .NET client are generated from it at build time. There is
          no separate schema file to drift, and no glue to hand-wire.
        </p>
        <div className="mt-9 flex flex-col items-center gap-3 sm:flex-row">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs">Read the Docs</OutlineButton>
        </div>
      </div>

      <div className="relative mt-16 flex justify-center">
        <div className="w-full max-w-3xl">
          <EditorCard />
        </div>
      </div>
    </section>
  );
}

function SpotlightMesh() {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0 -z-10">
      <div
        className="absolute top-[-6rem] left-1/2 h-[26rem] w-[42rem] -translate-x-1/2 rounded-full opacity-[0.22] blur-[90px]"
        style={{ background: SCENE_GRADIENT }}
      />
      <div
        className="absolute top-[18rem] left-1/2 h-[18rem] w-[18rem] -translate-x-[12rem] rounded-full opacity-[0.10] blur-[80px]"
        style={{ background: "#16b9e4" }}
      />
    </div>
  );
}

function GradientUnderline() {
  return (
    <span
      aria-hidden
      className="absolute -bottom-1 left-0 h-[3px] w-full rounded-full"
      style={{ background: SCENE_GRADIENT }}
    />
  );
}

/* ---- Real C# editor chrome ---- */

interface CodeLineProps {
  readonly n: number;
  readonly children: React.ReactNode;
}

function CodeLine({ n, children }: CodeLineProps) {
  return (
    <div className="flex">
      <span className="w-8 shrink-0 pr-4 text-right text-cc-ink-faint select-none">
        {n}
      </span>
      <span className="whitespace-pre">{children}</span>
    </div>
  );
}

// Token color helpers, kept local so the editor reads like a real editor.
const KW = "text-[#7c92c6]"; // keywords / modifiers
const ATTR = "text-[#16b9e4]"; // attributes, the scene accent
const TYPE = "text-[#5eead4]"; // types
const STR = "text-[#f0786a]"; // literals
const COM = "text-cc-ink-faint"; // comments
const PUNC = "text-cc-ink-dim";

function EditorCard() {
  return (
    <figure className="overflow-hidden rounded-2xl border border-cc-card-border bg-cc-code-bg/95 shadow-[0_30px_80px_-40px_rgba(22,185,228,0.45)] backdrop-blur">
      <div className="flex items-center gap-2 border-b border-cc-card-border bg-cc-code-header px-4 py-3">
        <span className="h-3 w-3 rounded-full bg-[#f0786a]/70" />
        <span className="h-3 w-3 rounded-full bg-[#fcd34d]/70" />
        <span className="h-3 w-3 rounded-full bg-[#5eead4]/70" />
        <span className="ml-3 font-mono text-xs text-cc-ink-dim">
          ProductApi.cs
        </span>
        <span className="ml-auto font-mono text-[11px] tracking-wide text-cc-nav-label uppercase">
          source of truth
        </span>
      </div>
      <pre className="overflow-x-auto px-4 py-5 font-mono text-[13px] leading-[1.7]">
        <code className="text-cc-ink">
          <CodeLine n={1}>
            <span className={ATTR}>[QueryType]</span>
          </CodeLine>
          <CodeLine n={2}>
            <span className={KW}>public partial class</span>{" "}
            <span className={TYPE}>ProductApi</span>
          </CodeLine>
          <CodeLine n={3}>
            <span className={PUNC}>{"{"}</span>
          </CodeLine>
          <CodeLine n={4}>
            {"    "}
            <span className={KW}>public static async</span>{" "}
            <span className={TYPE}>Task</span>
            <span className={PUNC}>&lt;</span>
            <span className={TYPE}>Product</span>
            <span className={PUNC}>&gt;</span>{" "}
            <span className="text-cc-ink">GetProductAsync</span>
            <span className={PUNC}>(</span>
          </CodeLine>
          <CodeLine n={5}>
            {"        "}
            <span className={TYPE}>int</span>{" "}
            <span className="text-cc-ink">id</span>
            <span className={PUNC}>,</span>
          </CodeLine>
          <CodeLine n={6}>
            {"        "}
            <span className={TYPE}>ProductByIdDataLoader</span>{" "}
            <span className="text-cc-ink">loader</span>
            <span className={PUNC}>)</span>
          </CodeLine>
          <CodeLine n={7}>
            {"        "}
            <span className={PUNC}>=&gt;</span>{" "}
            <span className="text-cc-ink">loader</span>
            <span className={PUNC}>.</span>
            <span className="text-cc-ink">LoadAsync</span>
            <span className={PUNC}>(</span>
            <span className="text-cc-ink">id</span>
            <span className={PUNC}>);</span>
          </CodeLine>
          <CodeLine n={8}>
            <span className={PUNC}>{"}"}</span>
          </CodeLine>
          <CodeLine n={9}> </CodeLine>
          <CodeLine n={10}>
            <span className={ATTR}>[DataLoader]</span>
          </CodeLine>
          <CodeLine n={11}>
            <span className={KW}>internal static partial</span>{" "}
            <span className={TYPE}>Task</span>
            <span className={PUNC}>&lt;</span>
            <span className={TYPE}>IReadOnlyDictionary</span>
            <span className={PUNC}>&lt;</span>
            <span className={TYPE}>int</span>
            <span className={PUNC}>,</span> <span className={TYPE}>Product</span>
            <span className={PUNC}>&gt;&gt;</span>
          </CodeLine>
          <CodeLine n={12}>
            {"    "}
            <span className="text-cc-ink">GetProductsAsync</span>
            <span className={PUNC}>(</span>
            <span className={TYPE}>IReadOnlyList</span>
            <span className={PUNC}>&lt;</span>
            <span className={TYPE}>int</span>
            <span className={PUNC}>&gt;</span>{" "}
            <span className="text-cc-ink">ids</span>
            <span className={PUNC}>,</span> <span className={COM}>{"/* … */"}</span>
            <span className={PUNC}>);</span>
          </CodeLine>
        </code>
      </pre>
    </figure>
  );
}

/* ------------------------------------------------------------------ *
 * Lineage: cyan hairline connectors from the one class down to three
 * real generated-artifact mini-mocks.
 * ------------------------------------------------------------------ */

function LineageSection() {
  return (
    <section className="relative">
      <SectionHeading
        eyebrow="One source, three artifacts"
        title="Every generated file traces back to one class"
        text="At build time the annotated class fans out into schema, batching, and a typed call site. Nothing is authored twice, so nothing can disagree."
      />

      <div className="relative mt-14">
        <SourceNode />
        <LineageConnectors />
        <div className="mt-12 grid gap-5 md:grid-cols-3">
          <ArtifactSdlPanel />
          <ArtifactBatchPanel />
          <ArtifactClientPanel />
        </div>
      </div>
    </section>
  );
}

function SourceNode() {
  return (
    <div className="flex justify-center">
      <div className="inline-flex items-center gap-3 rounded-full border border-cc-card-border bg-cc-card-bg px-5 py-2.5 backdrop-blur">
        <span
          className="h-2.5 w-2.5 rounded-full"
          style={{ background: SCENE_GRADIENT }}
        />
        <span className="font-mono text-sm text-cc-heading">
          [QueryType] ProductApi
        </span>
        <span className="font-mono text-xs text-cc-nav-label">.cs</span>
      </div>
    </div>
  );
}

// Three hairline drops from the source node to each artifact column.
function LineageConnectors() {
  return (
    <svg
      aria-hidden
      viewBox="0 0 900 90"
      preserveAspectRatio="none"
      className="mt-3 h-12 w-full"
    >
      <defs>
        <linearGradient id="lineageStroke" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#16b9e4" stopOpacity="0.9" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.15" />
        </linearGradient>
      </defs>
      <path
        d="M450 0 L450 28 L150 28 L150 84"
        fill="none"
        stroke="url(#lineageStroke)"
        strokeWidth="1.5"
      />
      <path
        d="M450 0 L450 84"
        fill="none"
        stroke="url(#lineageStroke)"
        strokeWidth="1.5"
      />
      <path
        d="M450 0 L450 28 L750 28 L750 84"
        fill="none"
        stroke="url(#lineageStroke)"
        strokeWidth="1.5"
      />
      <circle cx="150" cy="84" r="3" fill="#16b9e4" />
      <circle cx="450" cy="84" r="3" fill="#16b9e4" />
      <circle cx="750" cy="84" r="3" fill="#16b9e4" />
    </svg>
  );
}

interface ArtifactShellProps {
  readonly tag: string;
  readonly title: string;
  readonly children: React.ReactNode;
}

function ArtifactShell({ tag, title, children }: ArtifactShellProps) {
  return (
    <div className="flex flex-col rounded-2xl border border-cc-card-border bg-cc-card-bg p-5 backdrop-blur">
      <div className="flex items-center gap-2">
        <span
          className="font-mono text-[11px] tracking-wide uppercase"
          style={{ color: "#16b9e4" }}
        >
          {tag}
        </span>
      </div>
      <h3 className="mt-1 font-heading text-h6 text-cc-heading">{title}</h3>
      <div className="mt-4 flex-1">{children}</div>
    </div>
  );
}

function ArtifactSdlPanel() {
  return (
    <ArtifactShell tag="generated · SDL" title="The schema, not a copy">
      <div className="rounded-lg border border-cc-card-border bg-cc-code-bg/90 p-4 font-mono text-[12px] leading-[1.7]">
        <div>
          <span className={KW}>type</span> <span className={TYPE}>Product</span>{" "}
          <span className={PUNC}>{"{"}</span>
        </div>
        <div className="pl-4">
          <span className="text-cc-ink">id</span>
          <span className={PUNC}>:</span> <span className={TYPE}>Int!</span>
        </div>
        <div className="pl-4">
          <span className="text-cc-ink">name</span>
          <span className={PUNC}>:</span> <span className={TYPE}>String!</span>
        </div>
        <div>
          <span className={PUNC}>{"}"}</span>
        </div>
        <div className="mt-2">
          <span className={KW}>type</span> <span className={TYPE}>Query</span>{" "}
          <span className={PUNC}>{"{"}</span>
        </div>
        <div className="pl-4">
          <span className="text-cc-ink">product</span>
          <span className={PUNC}>(</span>
          <span className="text-cc-ink">id</span>
          <span className={PUNC}>:</span> <span className={TYPE}>Int!</span>
          <span className={PUNC}>)</span>
          <span className={PUNC}>:</span> <span className={TYPE}>Product</span>
        </div>
        <div>
          <span className={PUNC}>{"}"}</span>
        </div>
      </div>
      <p className="mt-3 text-caption text-cc-ink-dim">
        Emitted from the class, never edited by hand.
      </p>
    </ArtifactShell>
  );
}

function ArtifactBatchPanel() {
  return (
    <ArtifactShell tag="generated · DataLoader" title="N keys collapse to one">
      <BatchDiagram />
      <p className="mt-3 text-caption text-cc-ink-dim">
        Five field resolutions, one fetch. The N+1 is gone by construction.
      </p>
    </ArtifactShell>
  );
}

function BatchDiagram() {
  const keys = [11, 12, 13, 14, 15];
  return (
    <div className="flex items-center gap-3">
      <div className="flex flex-col gap-1.5">
        {keys.map((k) => (
          <span
            key={k}
            className="rounded border border-cc-card-border bg-cc-surface px-2 py-1 text-center font-mono text-[11px] text-cc-ink"
          >
            id {k}
          </span>
        ))}
      </div>
      <svg
        aria-hidden
        viewBox="0 0 70 120"
        preserveAspectRatio="none"
        className="h-[120px] w-[70px]"
      >
        <defs>
          <linearGradient id="batchStroke" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor="#16b9e4" stopOpacity="0.7" />
            <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
          </linearGradient>
        </defs>
        {[8, 32, 56, 80, 104].map((y) => (
          <path
            key={y}
            d={`M0 ${y} C 35 ${y}, 35 60, 68 60`}
            fill="none"
            stroke="url(#batchStroke)"
            strokeWidth="1.5"
          />
        ))}
        <circle cx="68" cy="60" r="3.5" fill="#5eead4" />
      </svg>
      <div
        className="rounded-lg px-3 py-2 text-center font-mono text-[11px] text-cc-surface"
        style={{ background: SCENE_GRADIENT }}
      >
        1 fetch
        <span className="block text-[10px] opacity-80">WHERE id IN (…)</span>
      </div>
    </div>
  );
}

function ArtifactClientPanel() {
  return (
    <ArtifactShell tag="generated · client" title="Typed call site in .NET">
      <div className="rounded-lg border border-cc-card-border bg-cc-code-bg/90 p-4 font-mono text-[12px] leading-[1.7]">
        <div>
          <span className={KW}>var</span>{" "}
          <span className="text-cc-ink">result</span>{" "}
          <span className={PUNC}>=</span> <span className={KW}>await</span>
        </div>
        <div className="pl-4">
          <span className="text-cc-ink">client</span>
          <span className={PUNC}>.</span>
          <span className={TYPE}>GetProduct</span>
          <span className={PUNC}>.</span>
          <span className="text-cc-ink">ExecuteAsync</span>
          <span className={PUNC}>(</span>
          <span className={STR}>id</span>
          <span className={PUNC}>:</span> <span className={STR}>11</span>
          <span className={PUNC}>);</span>
        </div>
        <div className="mt-2">
          <span className="text-cc-ink">result</span>
          <span className={PUNC}>.</span>
          <span className="text-cc-ink">Data</span>
          <span className={PUNC}>.</span>
          <span className="text-cc-ink">Product</span>
          <span className={PUNC}>.</span>
          <span className="text-cc-ink">Name</span>
          <span className={COM}>{" // string"}</span>
        </div>
      </div>
      <p className="mt-3 text-caption text-cc-ink-dim">
        Strawberry Shake generates this from your operation via MSBuild.
      </p>
    </ArtifactShell>
  );
}

/* ------------------------------------------------------------------ *
 * Bento: asymmetric grid of differently-sized tiles.
 * ------------------------------------------------------------------ */

function BentoSection() {
  return (
    <section>
      <SectionHeading
        eyebrow="Why the loop is tighter"
        title="Fewer moving parts between you and a running API"
        text="Implementation-first means the layers you used to wire by hand are derived. The result is a build loop that catches drift before it ships."
      />
      <div className="mt-14 grid auto-rows-[minmax(0,1fr)] grid-cols-1 gap-5 md:grid-cols-6">
        <BuildRailTile />
        <StatTile
          value="1"
          label="annotated class"
          caption="becomes schema, resolvers, batching, and client"
        />
        <StatTile
          value="0"
          label="schema files to sync"
          caption="the SDL is generated, never authored twice"
        />
        <PullQuoteTile />
        <GlueCollapseTile />
        <TimelineTile />
      </div>
    </section>
  );
}

interface TileProps {
  readonly className?: string;
  readonly children: React.ReactNode;
}

function Tile({ className = "", children }: TileProps) {
  return (
    <div
      className={`rounded-2xl border border-cc-card-border bg-cc-card-bg p-6 backdrop-blur transition-colors hover:border-cc-card-border-hover ${className}`}
    >
      {children}
    </div>
  );
}

// Wide tile: save -> generate -> typecheck -> run rail that lights each stage.
function BuildRailTile() {
  const stages = [
    { label: "save .cs", tone: "#16b9e4" },
    { label: "generate", tone: "#2dccdb" },
    { label: "type-check", tone: "#45dbd2" },
    { label: "run", tone: "#5eead4" },
  ];
  return (
    <Tile className="md:col-span-4">
      <span className="font-mono text-[11px] tracking-wide text-cc-nav-label uppercase">
        The save-to-run rail
      </span>
      <h3 className="mt-2 font-heading text-h5 text-cc-heading">
        One keystroke lights the whole pipeline
      </h3>
      <div className="mt-7 flex items-center">
        {stages.map((s, i) => (
          <div key={s.label} className="flex flex-1 items-center">
            <div className="flex flex-1 flex-col items-center gap-2">
              <span
                className="h-3.5 w-3.5 rounded-full"
                style={{
                  background: s.tone,
                  boxShadow: `0 0 14px ${s.tone}`,
                }}
              />
              <span className="text-center font-mono text-[11px] text-cc-ink">
                {s.label}
              </span>
            </div>
            {i < stages.length - 1 && (
              <span
                className="h-px w-full flex-1"
                style={{
                  background: `linear-gradient(90deg, ${s.tone}, ${stages[i + 1].tone})`,
                }}
              />
            )}
          </div>
        ))}
      </div>
      <p className="mt-7 text-body text-cc-ink-dim">
        A change to the annotated class regenerates the schema and client and
        re-runs the type-checker. Drift surfaces as a build error, in the editor,
        before anything reaches a published client.
      </p>
    </Tile>
  );
}

interface StatTileProps {
  readonly value: string;
  readonly label: string;
  readonly caption: string;
}

function StatTile({ value, label, caption }: StatTileProps) {
  return (
    <Tile className="md:col-span-2 md:row-span-1 flex flex-col justify-between">
      <span
        className="font-heading text-[3.25rem] leading-none"
        style={{
          background: SCENE_GRADIENT,
          WebkitBackgroundClip: "text",
          backgroundClip: "text",
          color: "transparent",
        }}
      >
        {value}
      </span>
      <div className="mt-4">
        <p className="font-mono text-sm text-cc-heading">{label}</p>
        <p className="mt-1 text-caption text-cc-ink-dim">{caption}</p>
      </div>
    </Tile>
  );
}

function PullQuoteTile() {
  return (
    <Tile className="md:col-span-2 flex flex-col justify-center">
      <p className="font-heading text-h6 text-cc-heading">
        “If the resolver compiles against the type, the schema already agrees
        with it.”
      </p>
      <p className="mt-4 font-mono text-[11px] tracking-wide text-cc-nav-label uppercase">
        implementation-first, by design
      </p>
    </Tile>
  );
}

// Before/after: a tangle of glue boxes collapsing into one annotated class.
function GlueCollapseTile() {
  return (
    <Tile className="md:col-span-2">
      <span className="font-mono text-[11px] tracking-wide text-cc-nav-label uppercase">
        Before / after
      </span>
      <h3 className="mt-2 font-heading text-h6 text-cc-heading">
        Glue tangle, collapsed
      </h3>
      <div className="mt-5 grid grid-cols-[1fr_auto_1fr] items-center gap-3">
        <div className="flex flex-col gap-1.5">
          {["schema.graphql", "resolvers", "mappers", "client.ts"].map((b) => (
            <span
              key={b}
              className="truncate rounded border border-dashed border-cc-card-border px-2 py-1 font-mono text-[10px] text-cc-ink-dim"
            >
              {b}
            </span>
          ))}
        </div>
        <svg aria-hidden viewBox="0 0 24 24" className="h-5 w-5 text-cc-ink-dim">
          <path
            d="M4 12 H18 M13 7 L18 12 L13 17"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.6"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
        <span
          className="rounded-lg px-2 py-3 text-center font-mono text-[11px] text-cc-surface"
          style={{ background: SCENE_GRADIENT }}
        >
          ProductApi.cs
        </span>
      </div>
    </Tile>
  );
}

// MSBuild codegen timeline ribbon: .graphql -> typed .NET client.
function TimelineTile() {
  const steps = [".graphql", "MSBuild codegen", "typed .NET client"];
  return (
    <Tile className="md:col-span-2">
      <span className="font-mono text-[11px] tracking-wide text-cc-nav-label uppercase">
        Client codegen
      </span>
      <h3 className="mt-2 font-heading text-h6 text-cc-heading">
        MSBuild ribbon, end to end
      </h3>
      <ol className="mt-5 flex flex-col gap-3">
        {steps.map((s, i) => (
          <li key={s} className="flex items-center gap-3">
            <span
              className="grid h-6 w-6 shrink-0 place-items-center rounded-full font-mono text-[11px] text-cc-surface"
              style={{ background: "#16b9e4" }}
            >
              {i + 1}
            </span>
            <span className="font-mono text-[12px] text-cc-ink">{s}</span>
          </li>
        ))}
      </ol>
      <p className="mt-4 text-caption text-cc-ink-dim">
        Strawberry Shake runs as an MSBuild step, so the client is regenerated
        on every build.
      </p>
    </Tile>
  );
}

/* ------------------------------------------------------------------ *
 * Comparison: hand-wired vs schema-first DSL vs source-generated.
 * ------------------------------------------------------------------ */

interface ComparisonRow {
  readonly dimension: string;
  readonly handWired: string;
  readonly schemaFirst: string;
  readonly generated: string;
  readonly generatedWins: boolean;
}

const COMPARISON_ROWS: readonly ComparisonRow[] = [
  {
    dimension: "Schema drift",
    handWired: "Manual, easy to skew",
    schemaFirst: "Separate file can lag code",
    generated: "Generated from the code",
    generatedWins: true,
  },
  {
    dimension: "Type safety",
    handWired: "Stringly-typed plumbing",
    schemaFirst: "Bridged across DSL boundary",
    generated: "Typed end to end in C#",
    generatedWins: true,
  },
  {
    dimension: "N+1 fetches",
    handWired: "Hand-batched, error-prone",
    schemaFirst: "Wired per resolver",
    generated: "[DataLoader] batches by key",
    generatedWins: true,
  },
  {
    dimension: "Client sync",
    handWired: "Updated by hand",
    schemaFirst: "Re-run codegen manually",
    generated: "MSBuild regenerates on build",
    generatedWins: true,
  },
  {
    dimension: "Build feedback",
    handWired: "Found at runtime",
    schemaFirst: "Partly at codegen time",
    generated: "Compiler error in the editor",
    generatedWins: true,
  },
];

function ComparisonSection() {
  return (
    <section>
      <SectionHeading
        eyebrow="Three ways to build a GraphQL API"
        title="What the implementation-first loop changes"
        text="Hand-wired and schema-first DSL both keep the contract in a place the compiler cannot see. Source generation pulls it back into the code that runs."
      />
      <div className="mt-12 overflow-hidden rounded-2xl border border-cc-card-border bg-cc-card-bg backdrop-blur">
        <div className="grid grid-cols-[1.1fr_1fr_1fr_1.25fr] border-b border-cc-card-border">
          <HeaderCell label="" />
          <HeaderCell label="Hand-wired" muted />
          <HeaderCell label="Schema-first DSL" muted />
          <HeaderCell label="Source-generated" accent />
        </div>
        {COMPARISON_ROWS.map((row, i) => (
          <div
            key={row.dimension}
            className={`grid grid-cols-[1.1fr_1fr_1fr_1.25fr] ${
              i < COMPARISON_ROWS.length - 1
                ? "border-b border-cc-card-border"
                : ""
            }`}
          >
            <div className="px-4 py-4 font-mono text-[12px] tracking-wide text-cc-heading">
              {row.dimension}
            </div>
            <div className="px-4 py-4 text-caption text-cc-ink-dim">
              {row.handWired}
            </div>
            <div className="px-4 py-4 text-caption text-cc-ink-dim">
              {row.schemaFirst}
            </div>
            <div className="flex items-start gap-2 bg-[#16b9e4]/[0.06] px-4 py-4 text-caption text-cc-heading">
              {row.generatedWins && (
                <span className="mt-[3px] text-cc-accent">
                  <CheckIcon size={13} />
                </span>
              )}
              <span>{row.generated}</span>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

interface HeaderCellProps {
  readonly label: string;
  readonly muted?: boolean;
  readonly accent?: boolean;
}

function HeaderCell({ label, muted, accent }: HeaderCellProps) {
  return (
    <div
      className={`px-4 py-4 font-mono text-[11px] tracking-[0.12em] uppercase ${
        accent
          ? "text-cc-heading"
          : muted
            ? "text-cc-nav-label"
            : "text-cc-nav-label"
      }`}
      style={accent ? { background: "#16b9e4" + "12" } : undefined}
    >
      <span className="flex items-center gap-2">
        {accent && (
          <span
            className="h-2 w-2 rounded-full"
            style={{ background: SCENE_GRADIENT }}
          />
        )}
        {label}
      </span>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Honesty beat: what source generation does and does not promise.
 * ------------------------------------------------------------------ */

function HonestySection() {
  const points: readonly { readonly title: string; readonly body: string }[] = [
    {
      title: "What it guarantees",
      body: "The generated schema and typed client come from the same annotated class, so the contract you ship matches the code that serves it.",
    },
    {
      title: "What it does not",
      body: "Generation cannot read your intent. A breaking change still breaks; the loop just surfaces it as a build error so you see which published clients are affected before release.",
    },
    {
      title: "Where the line is",
      body: "Hot Chocolate generates server artifacts at build time. Strawberry Shake emits the .NET client through MSBuild codegen. Both run on every build, neither hides runtime cost.",
    },
  ];
  return (
    <section className="rounded-3xl border border-cc-card-border bg-cc-surface/60 p-8 backdrop-blur sm:p-12">
      <span className="font-mono text-[11px] tracking-[0.2em] text-cc-nav-label uppercase">
        Straight about the trade
      </span>
      <h2 className="mt-4 max-w-2xl font-heading text-h4 text-cc-heading">
        Generation removes glue, not judgement
      </h2>
      <div className="mt-9 grid gap-6 md:grid-cols-3">
        {points.map((p) => (
          <div
            key={p.title}
            className="border-t border-cc-card-border pt-5"
            style={{ borderTopColor: "rgba(22,185,228,0.35)" }}
          >
            <h3 className="font-heading text-h6 text-cc-heading">{p.title}</h3>
            <p className="mt-3 text-body text-cc-ink-dim">{p.body}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Closing CTA pair.
 * ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="relative isolate overflow-hidden rounded-3xl border border-cc-card-border bg-cc-card-bg px-8 py-16 text-center backdrop-blur sm:px-12">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-40 opacity-[0.18] blur-[70px]"
        style={{ background: SCENE_GRADIENT }}
      />
      <h2 className="mx-auto max-w-2xl font-heading text-h3 text-cc-heading">
        Build the API from the code that runs it
      </h2>
      <p className="lead mx-auto mt-5 max-w-xl text-cc-prose">
        Annotate a class, build, and ship a schema and typed client that cannot
        drift apart.
      </p>
      <div className="mt-9 flex flex-col items-center justify-center gap-3 sm:flex-row">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Shared section heading.
 * ------------------------------------------------------------------ */

interface SectionHeadingProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly text: string;
}

function SectionHeading({ eyebrow, title, text }: SectionHeadingProps) {
  return (
    <div className="mx-auto max-w-2xl text-center">
      <span className="font-mono text-[11px] tracking-[0.2em] text-cc-nav-label uppercase">
        {eyebrow}
      </span>
      <h2 className="mt-4 font-heading text-h4 text-cc-heading">{title}</h2>
      <p className="mt-4 text-body text-cc-ink-dim">{text}</p>
    </div>
  );
}
