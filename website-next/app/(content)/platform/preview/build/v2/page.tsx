import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Ship From the Code That Runs It | Build Loop",
  description:
    "One annotated C# class, generated end to end. The schema, resolver pipeline, DataLoaders, and typed .NET clients all trace back to the code that runs in production.",
  keywords: [
    "implementation-first GraphQL",
    "C# GraphQL server",
    "Hot Chocolate source generator",
    "source-generated schema",
    "DataLoader batching",
    "Strawberry Shake typed client",
    "MSBuild code generation",
    "no schema drift",
    "QueryType attribute",
    "typed end to end",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Ship From the Code That Runs It",
    description:
      "Implementation-first GraphQL in C#: schema, resolver pipeline, DataLoaders, and typed .NET clients all generated from one annotated class. Nothing to drift.",
  },
};

/* ------------------------------------------------------------------ *
 * Shared editorial primitives                                         *
 * ------------------------------------------------------------------ */

/** Faint blueprint dot-grid backdrop for a diagram region. */
interface BlueprintFieldProps {
  readonly children: React.ReactNode;
  readonly className?: string;
}

function BlueprintField({ children, className }: BlueprintFieldProps) {
  return (
    <div className={`relative ${className ?? ""}`}>
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 rounded-2xl opacity-[0.5]"
        style={{
          backgroundImage:
            "radial-gradient(rgba(245,241,234,0.10) 1px, transparent 1px)",
          backgroundSize: "22px 22px",
          maskImage:
            "radial-gradient(ellipse 80% 80% at 50% 50%, #000 55%, transparent 100%)",
          WebkitMaskImage:
            "radial-gradient(ellipse 80% 80% at 50% 50%, #000 55%, transparent 100%)",
        }}
      />
      <div className="relative">{children}</div>
    </div>
  );
}

/** Mono eyebrow label with an optional ordinal. */
interface EyebrowProps {
  readonly children: React.ReactNode;
  readonly className?: string;
}

function Eyebrow({ children, className }: EyebrowProps) {
  return (
    <span
      className={`font-mono text-[0.7rem] tracking-[0.28em] text-cc-nav-label uppercase ${className ?? ""}`}
    >
      {children}
    </span>
  );
}

/** Large chapter ordinal in the left rail of a narrative section. */
interface OrdinalProps {
  readonly value: string;
  readonly label: string;
}

function Ordinal({ value, label }: OrdinalProps) {
  return (
    <div className="flex items-baseline gap-3">
      <span
        className="font-heading text-h3 leading-none font-bold"
        style={{
          background: "linear-gradient(180deg, #16b9e4 0%, #5eead4 100%)",
          WebkitBackgroundClip: "text",
          backgroundClip: "text",
          color: "transparent",
        }}
      >
        {value}
      </span>
      <span className="h-px w-10 bg-cc-card-border" aria-hidden />
      <Eyebrow>{label}</Eyebrow>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Code rendering (token-colored, no external highlighter)             *
 * ------------------------------------------------------------------ */

/** A single colored token. Colors are literal hex so they survive Tailwind purge. */
function T({ c, children }: { c: string; children: React.ReactNode }) {
  return <span style={{ color: c }}>{children}</span>;
}

const SYN = {
  attr: "#16b9e4",
  keyword: "#7c92c6",
  type: "#5eead4",
  string: "#f0786a",
  comment: "#62748e",
  ink: "#cad5e2",
  punct: "#8a93a6",
} as const;

/** Editor chrome wrapper with header, traffic dots, filename and line numbers. */
interface EditorCardProps {
  readonly filename: string;
  readonly badge?: string;
  readonly lines: readonly React.ReactNode[];
  readonly className?: string;
}

function EditorCard({ filename, badge, lines, className }: EditorCardProps) {
  return (
    <figure
      className={`overflow-hidden rounded-xl border border-cc-card-border bg-cc-code-bg shadow-[0_24px_60px_-24px_rgba(0,0,0,0.7)] ${className ?? ""}`}
    >
      <figcaption className="flex items-center gap-3 border-b border-cc-card-border bg-cc-code-header px-4 py-2.5">
        <span className="flex gap-1.5" aria-hidden>
          <span className="h-2.5 w-2.5 rounded-full bg-[#f0786a]/70" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#7c92c6]/70" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#5eead4]/70" />
        </span>
        <span className="font-mono text-[0.72rem] text-cc-ink-dim">
          {filename}
        </span>
        {badge ? (
          <span className="ml-auto rounded-full border border-cc-card-border px-2 py-0.5 font-mono text-[0.62rem] tracking-wider text-cc-nav-label uppercase">
            {badge}
          </span>
        ) : null}
      </figcaption>
      <pre className="overflow-x-auto px-4 py-4 font-mono text-[0.78rem] leading-[1.7]">
        <code>
          {lines.map((ln, i) => (
            <div key={i} className="grid grid-cols-[1.6rem_1fr] gap-3">
              <span className="text-right text-cc-nav-label/60 select-none">
                {i + 1}
              </span>
              <span className="whitespace-pre">{ln}</span>
            </div>
          ))}
        </code>
      </pre>
    </figure>
  );
}

/* ------------------------------------------------------------------ *
 * Hero                                                                *
 * ------------------------------------------------------------------ */

const HERO_SOURCE: readonly React.ReactNode[] = [
  <>
    <T c={SYN.attr}>[QueryType]</T>
  </>,
  <>
    <T c={SYN.keyword}>public partial class</T> <T c={SYN.type}>ProductApi</T>
  </>,
  <>
    <T c={SYN.punct}>{"{"}</T>
  </>,
  <>
    {"  "}
    <T c={SYN.keyword}>public static</T> <T c={SYN.type}>Product</T>{" "}
    <T c={SYN.ink}>GetProduct</T>
    <T c={SYN.punct}>(</T>
    <T c={SYN.type}>int</T> <T c={SYN.ink}>id</T>
    <T c={SYN.punct}>,</T>
  </>,
  <>
    {"      "}
    <T c={SYN.type}>IProductByIdDataLoader</T> <T c={SYN.ink}>loader</T>
    <T c={SYN.punct}>)</T>
  </>,
  <>
    {"    "}
    <T c={SYN.punct}>{"=>"}</T> <T c={SYN.ink}>loader</T>
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>LoadAsync</T>
    <T c={SYN.punct}>(</T>
    <T c={SYN.ink}>id</T>
    <T c={SYN.punct}>);</T>
  </>,
  <>
    <T c={SYN.punct}>{"}"}</T>
  </>,
];

function Hero() {
  return (
    <header className="pt-6 pb-4 sm:pt-12">
      <Eyebrow>Build loop / Hot Chocolate</Eyebrow>
      <h1 className="hero mt-6 max-w-3xl text-cc-heading">
        Ship from the code that runs it.
      </h1>
      <p className="lead mt-6 max-w-2xl text-cc-prose">
        Write one annotated C# class. The schema, the resolver pipeline, the
        DataLoaders, and your typed .NET clients are{" "}
        <span className="text-cc-heading">generated from it</span> — so there is
        nothing on the side to keep in sync.
      </p>

      <div className="mt-9 flex flex-wrap items-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>

      <BlueprintField className="mt-14">
        <div className="grid items-center gap-8 lg:grid-cols-[minmax(0,1fr)_auto_minmax(0,0.9fr)]">
          <EditorCard
            filename="ProductApi.cs"
            badge="you write this"
            lines={HERO_SOURCE}
          />

          {/* Single color event: the cyan->teal generation arrow */}
          <div className="hidden flex-col items-center gap-2 lg:flex">
            <GenerationArrow />
            <Eyebrow className="text-cc-nav-label">build</Eyebrow>
          </div>

          <ul className="space-y-3">
            {[
              "Schema (SDL)",
              "Resolver pipeline",
              "DataLoaders + batching",
              "Typed .NET client",
            ].map((label, i) => (
              <li
                key={label}
                className="flex items-center gap-3 rounded-lg border border-cc-card-border bg-cc-card-bg px-4 py-3 backdrop-blur"
              >
                <span className="font-mono text-[0.62rem] text-cc-nav-label">
                  {String(i + 1).padStart(2, "0")}
                </span>
                <span className="text-sm text-cc-ink">{label}</span>
                <span className="ml-auto font-mono text-[0.6rem] tracking-wider text-cc-nav-label uppercase">
                  generated
                </span>
              </li>
            ))}
          </ul>
        </div>
      </BlueprintField>
    </header>
  );
}

/** Horizontal cyan->teal generation arrow (the page's one gradient). */
function GenerationArrow() {
  return (
    <svg
      width="120"
      height="24"
      viewBox="0 0 120 24"
      fill="none"
      aria-hidden
      className="motion-safe:[--dash:8] motion-reduce:[--dash:0]"
    >
      <defs>
        <linearGradient id="hero-gen" x1="0" y1="0" x2="120" y2="0">
          <stop offset="0%" stopColor="#16b9e4" />
          <stop offset="100%" stopColor="#5eead4" />
        </linearGradient>
      </defs>
      <line
        x1="2"
        y1="12"
        x2="108"
        y2="12"
        stroke="url(#hero-gen)"
        strokeWidth="2"
      />
      <path d="M104 6 L116 12 L104 18" stroke="url(#hero-gen)" strokeWidth="2" />
    </svg>
  );
}

/* ------------------------------------------------------------------ *
 * Section frame: numbered, two-column prose + diagram                 *
 * ------------------------------------------------------------------ */

interface ChapterProps {
  readonly ordinal: string;
  readonly kicker: string;
  readonly heading: React.ReactNode;
  readonly body: React.ReactNode;
  readonly aside?: React.ReactNode;
  readonly diagram: React.ReactNode;
  readonly flip?: boolean;
}

function Chapter({
  ordinal,
  kicker,
  heading,
  body,
  aside,
  diagram,
  flip,
}: ChapterProps) {
  return (
    <section className="border-t border-cc-card-border py-16 sm:py-20">
      <div
        className={`grid items-start gap-10 lg:grid-cols-2 lg:gap-16 ${
          flip ? "lg:[&>*:first-child]:order-2" : ""
        }`}
      >
        <div>
          <Ordinal value={ordinal} label={kicker} />
          <h2 className="mt-6 max-w-md text-h4 font-heading font-semibold text-balance text-cc-heading">
            {heading}
          </h2>
          <div className="mt-5 max-w-md space-y-4 text-body text-cc-prose">
            {body}
          </div>
          {aside ? <div className="mt-7 max-w-md">{aside}</div> : null}
        </div>
        <BlueprintField className="lg:pt-2">{diagram}</BlueprintField>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * 01 — Lineage tree: one class -> three artifacts                     *
 * ------------------------------------------------------------------ */

function LineageDiagram() {
  return (
    <div className="rounded-2xl border border-cc-card-border bg-cc-card-bg p-6 backdrop-blur">
      <div className="mb-6 inline-flex items-center gap-2 rounded-md border border-cc-card-border bg-cc-code-header px-3 py-1.5">
        <span
          className="h-2 w-2 rounded-full"
          style={{ background: "#16b9e4" }}
          aria-hidden
        />
        <span className="font-mono text-[0.72rem] text-cc-ink">
          ProductApi.cs
        </span>
        <span className="font-mono text-[0.6rem] tracking-wider text-cc-nav-label uppercase">
          source of truth
        </span>
      </div>

      {/* Lineage connectors */}
      <svg
        viewBox="0 0 320 56"
        className="h-14 w-full"
        fill="none"
        aria-hidden
      >
        <defs>
          <linearGradient id="lineage" x1="160" y1="0" x2="160" y2="56">
            <stop offset="0%" stopColor="#16b9e4" />
            <stop offset="100%" stopColor="#5eead4" />
          </linearGradient>
        </defs>
        <path
          d="M160 0 L160 16 M160 16 C160 30 53 26 53 44 M160 16 C160 30 160 26 160 44 M160 16 C160 30 267 26 267 44 M53 44 L53 56 M160 44 L160 56 M267 44 L267 56"
          stroke="url(#lineage)"
          strokeWidth="1.5"
        />
      </svg>

      <div className="grid grid-cols-3 gap-3">
        {[
          { tag: "SDL", title: "Schema", sub: "type Product { … }" },
          { tag: "Pipeline", title: "Resolvers", sub: "field execution" },
          { tag: "Client", title: "Typed calls", sub: "GetProduct(id)" },
        ].map((a) => (
          <div
            key={a.tag}
            className="rounded-lg border border-cc-card-border bg-cc-surface/60 px-3 py-3"
          >
            <span className="font-mono text-[0.58rem] tracking-wider text-cc-nav-label uppercase">
              {a.tag}
            </span>
            <p className="mt-1.5 text-sm font-medium text-cc-heading">
              {a.title}
            </p>
            <p className="mt-0.5 font-mono text-[0.66rem] text-cc-ink-dim">
              {a.sub}
            </p>
          </div>
        ))}
      </div>

      <p className="mt-5 border-t border-cc-card-border pt-4 font-mono text-[0.68rem] text-cc-nav-label">
        every artifact traces back to one annotated class
      </p>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * 02 — DataLoader batching diagram (N keys -> one fetch)              *
 * ------------------------------------------------------------------ */

function BatchingDiagram() {
  const keys = ["#41", "#7", "#88", "#41", "#12"];
  return (
    <div className="rounded-2xl border border-cc-card-border bg-cc-card-bg p-6 backdrop-blur">
      <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-4">
        {/* incoming keys */}
        <ul className="space-y-2">
          {keys.map((k, i) => (
            <li
              key={i}
              className="flex items-center justify-center rounded-md border border-cc-card-border bg-cc-surface/60 py-1.5 font-mono text-[0.7rem] text-cc-ink"
            >
              load({k})
            </li>
          ))}
        </ul>

        {/* collapse fan */}
        <svg
          viewBox="0 0 80 150"
          className="h-[150px] w-20"
          fill="none"
          aria-hidden
        >
          <defs>
            <linearGradient id="batch" x1="0" y1="75" x2="80" y2="75">
              <stop offset="0%" stopColor="#16b9e4" />
              <stop offset="100%" stopColor="#5eead4" />
            </linearGradient>
          </defs>
          {[12, 42, 75, 108, 138].map((y, i) => (
            <path
              key={i}
              d={`M2 ${y} C40 ${y} 40 75 78 75`}
              stroke="url(#batch)"
              strokeWidth="1.5"
              opacity={0.85}
            />
          ))}
          <circle cx="78" cy="75" r="3" fill="#5eead4" />
        </svg>

        {/* one fetch */}
        <div className="rounded-lg border border-cc-card-border bg-cc-surface/60 p-3">
          <span className="font-mono text-[0.58rem] tracking-wider text-cc-nav-label uppercase">
            one fetch
          </span>
          <p className="mt-1.5 font-mono text-[0.72rem] text-cc-heading">
            WHERE id IN
          </p>
          <p className="font-mono text-[0.72rem] text-[#5eead4]">
            (41, 7, 88, 12)
          </p>
          <p className="mt-2 font-mono text-[0.6rem] text-cc-nav-label">
            deduped · batched
          </p>
        </div>
      </div>

      <div className="mt-6 grid grid-cols-3 gap-3 border-t border-cc-card-border pt-4">
        {[
          ["5", "keys requested"],
          ["1", "round trip"],
          ["4", "unique ids"],
        ].map(([n, l]) => (
          <div key={l}>
            <p className="font-heading text-h5 font-semibold text-cc-heading">
              {n}
            </p>
            <p className="font-mono text-[0.6rem] tracking-wider text-cc-nav-label uppercase">
              {l}
            </p>
          </div>
        ))}
      </div>
    </div>
  );
}

const DATALOADER_SOURCE: readonly React.ReactNode[] = [
  <>
    <T c={SYN.attr}>[DataLoader]</T>
  </>,
  <>
    <T c={SYN.keyword}>internal static async</T> <T c={SYN.type}>Task</T>
    <T c={SYN.punct}>{"<"}</T>
    <T c={SYN.type}>IReadOnlyDictionary</T>
    <T c={SYN.punct}>{"<"}</T>
    <T c={SYN.type}>int</T>
    <T c={SYN.punct}>,</T> <T c={SYN.type}>Product</T>
    <T c={SYN.punct}>{">>"}</T>
  </>,
  <>
    {"  "}
    <T c={SYN.ink}>GetProductByIdAsync</T>
    <T c={SYN.punct}>(</T>
    <T c={SYN.type}>IReadOnlyList</T>
    <T c={SYN.punct}>{"<"}</T>
    <T c={SYN.type}>int</T>
    <T c={SYN.punct}>{">"}</T> <T c={SYN.ink}>ids</T>
    <T c={SYN.punct}>,</T>
  </>,
  <>
    {"      "}
    <T c={SYN.type}>CatalogContext</T> <T c={SYN.ink}>db</T>
    <T c={SYN.punct}>)</T>
  </>,
  <>
    {"  "}
    <T c={SYN.punct}>{"=>"}</T> <T c={SYN.keyword}>await</T> <T c={SYN.ink}>db</T>
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>Products</T>
  </>,
  <>
    {"      "}
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>Where</T>
    <T c={SYN.punct}>(</T>
    <T c={SYN.ink}>p</T> <T c={SYN.punct}>{"=>"}</T> <T c={SYN.ink}>ids</T>
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>Contains</T>
    <T c={SYN.punct}>(</T>
    <T c={SYN.ink}>p</T>
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>Id</T>
    <T c={SYN.punct}>))</T>
  </>,
  <>
    {"      "}
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>ToDictionaryAsync</T>
    <T c={SYN.punct}>(</T>
    <T c={SYN.ink}>p</T> <T c={SYN.punct}>{"=>"}</T> <T c={SYN.ink}>p</T>
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>Id</T>
    <T c={SYN.punct}>);</T>
  </>,
];

/* ------------------------------------------------------------------ *
 * 03 — MSBuild codegen timeline ribbon                                *
 * ------------------------------------------------------------------ */

function CodegenRibbon() {
  const stages = [
    { t: "schema.graphql", k: "input" },
    { t: "MSBuild codegen", k: "build" },
    { t: "operations", k: "queries" },
    { t: "typed .NET client", k: "output" },
  ];
  return (
    <div className="rounded-2xl border border-cc-card-border bg-cc-card-bg p-6 backdrop-blur">
      <ol className="relative space-y-0">
        {stages.map((s, i) => {
          const last = i === stages.length - 1;
          return (
            <li key={s.t} className="relative grid grid-cols-[auto_1fr] gap-4">
              <div className="flex flex-col items-center">
                <span
                  className="z-10 flex h-7 w-7 items-center justify-center rounded-full border font-mono text-[0.62rem] text-cc-heading"
                  style={{
                    borderColor: last ? "#5eead4" : "#16b9e4",
                    background: "rgba(12,19,34,0.8)",
                  }}
                >
                  {String(i + 1).padStart(2, "0")}
                </span>
                {!last ? (
                  <span
                    className="w-px flex-1"
                    style={{
                      minHeight: "32px",
                      background:
                        "linear-gradient(180deg, #16b9e4 0%, #5eead4 100%)",
                    }}
                    aria-hidden
                  />
                ) : null}
              </div>
              <div className={last ? "pb-0" : "pb-6"}>
                <p className="font-mono text-[0.8rem] text-cc-heading">{s.t}</p>
                <p className="font-mono text-[0.6rem] tracking-wider text-cc-nav-label uppercase">
                  {s.k}
                </p>
              </div>
            </li>
          );
        })}
      </ol>
      <p className="mt-4 border-t border-cc-card-border pt-4 font-mono text-[0.66rem] text-cc-nav-label">
        Strawberry Shake · runs in your build, not at startup
      </p>
    </div>
  );
}

const CLIENT_SOURCE: readonly React.ReactNode[] = [
  <>
    <T c={SYN.comment}>{"// generated from your .graphql operations"}</T>
  </>,
  <>
    <T c={SYN.keyword}>var</T> <T c={SYN.ink}>result</T>{" "}
    <T c={SYN.punct}>=</T> <T c={SYN.keyword}>await</T> <T c={SYN.ink}>client</T>
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>GetProduct</T>
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>ExecuteAsync</T>
    <T c={SYN.punct}>(</T>
    <T c={SYN.ink}>id</T>
    <T c={SYN.punct}>);</T>
  </>,
  <>{" "}</>,
  <>
    <T c={SYN.type}>Product</T>
    <T c={SYN.punct}>?</T> <T c={SYN.ink}>product</T>{" "}
    <T c={SYN.punct}>=</T> <T c={SYN.ink}>result</T>
    <T c={SYN.punct}>.</T>
    <T c={SYN.ink}>Data</T>
    <T c={SYN.punct}>?.</T>
    <T c={SYN.ink}>Product</T>
    <T c={SYN.punct}>;</T>
  </>,
  <>
    <T c={SYN.comment}>{"// product.Name is a string. Checked by the"}</T>
  </>,
  <>
    <T c={SYN.comment}>{"// compiler, not by you at runtime."}</T>
  </>,
];

/* ------------------------------------------------------------------ *
 * Comparison table climax                                             *
 * ------------------------------------------------------------------ */

type Cell = { readonly tone: "good" | "warn" | "bad"; readonly text: string };

interface ComparisonRow {
  readonly dim: string;
  readonly handWired: Cell;
  readonly schemaFirst: Cell;
  readonly sourceGen: Cell;
}

const COMPARISON: readonly ComparisonRow[] = [
  {
    dim: "Schema drift",
    handWired: { tone: "warn", text: "Manual sync" },
    schemaFirst: { tone: "bad", text: "Separate file drifts" },
    sourceGen: { tone: "good", text: "No separate file" },
  },
  {
    dim: "Type safety",
    handWired: { tone: "warn", text: "Partial" },
    schemaFirst: { tone: "warn", text: "Glue can mismatch" },
    sourceGen: { tone: "good", text: "Typed end to end" },
  },
  {
    dim: "N+1 fetching",
    handWired: { tone: "bad", text: "Hand-rolled" },
    schemaFirst: { tone: "warn", text: "Opt-in wiring" },
    sourceGen: { tone: "good", text: "[DataLoader] generated" },
  },
  {
    dim: "Client sync",
    handWired: { tone: "bad", text: "Manual contracts" },
    schemaFirst: { tone: "warn", text: "Re-export schema" },
    sourceGen: { tone: "good", text: "Codegen from ops" },
  },
  {
    dim: "Build feedback",
    handWired: { tone: "warn", text: "At runtime" },
    schemaFirst: { tone: "warn", text: "On startup" },
    sourceGen: { tone: "good", text: "At build time" },
  },
];

function ToneMark({ tone }: { tone: Cell["tone"] }) {
  if (tone === "good") {
    return (
      <span className="text-[#5eead4]" aria-label="strong">
        <CheckIcon size={13} />
      </span>
    );
  }
  if (tone === "warn") {
    return (
      <span
        className="inline-block h-[10px] w-[10px] rounded-full border-2 border-cc-nav-label"
        aria-label="partial"
      />
    );
  }
  return (
    <span
      className="inline-block h-[2px] w-[12px] rounded bg-[#f0786a]/80"
      aria-label="weak"
    />
  );
}

function ComparisonSection() {
  const cols = ["Hand-wired", "Schema-first DSL", "Source-generated"] as const;
  return (
    <section className="border-t border-cc-card-border py-16 sm:py-20">
      <Ordinal value="04" label="The honest comparison" />
      <h2 className="mt-6 max-w-xl text-h4 font-heading font-semibold text-balance text-cc-heading">
        Three ways to wire a GraphQL server. One removes the side files.
      </h2>
      <p className="mt-5 max-w-xl text-body text-cc-prose">
        Implementation-first does not make the hard problems disappear, it
        removes the surfaces where they hide. Here is the trade, stated plainly.
      </p>

      <div className="mt-10 overflow-x-auto">
        <table className="w-full min-w-[640px] border-collapse text-left">
          <thead>
            <tr className="border-b border-cc-card-border">
              <th className="py-3 pr-4 font-mono text-[0.62rem] tracking-[0.22em] text-cc-nav-label uppercase">
                Dimension
              </th>
              {cols.map((c, i) => (
                <th
                  key={c}
                  className={`py-3 pr-4 font-heading text-h6 font-semibold ${
                    i === 2 ? "text-cc-heading" : "text-cc-ink-dim"
                  }`}
                >
                  {c}
                  {i === 2 ? (
                    <span
                      className="mt-1 block h-[2px] w-12 rounded"
                      style={{
                        background:
                          "linear-gradient(90deg, #16b9e4 0%, #5eead4 100%)",
                      }}
                      aria-hidden
                    />
                  ) : null}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {COMPARISON.map((row) => (
              <tr
                key={row.dim}
                className="border-b border-cc-card-border/60 last:border-b-0"
              >
                <th
                  scope="row"
                  className="py-4 pr-4 align-top text-sm font-medium text-cc-heading"
                >
                  {row.dim}
                </th>
                {[row.handWired, row.schemaFirst, row.sourceGen].map(
                  (cell, i) => (
                    <td
                      key={i}
                      className={`py-4 pr-4 align-top ${
                        i === 2
                          ? "bg-[#16b9e4]/[0.04] rounded-sm"
                          : ""
                      }`}
                    >
                      <span className="flex items-center gap-2.5">
                        <ToneMark tone={cell.tone} />
                        <span
                          className={`text-sm ${
                            i === 2 ? "text-cc-ink" : "text-cc-ink-dim"
                          }`}
                        >
                          {cell.text}
                        </span>
                      </span>
                    </td>
                  ),
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Credibility / honesty beat                                          *
 * ------------------------------------------------------------------ */

function HonestyBeat() {
  const points: readonly string[] = [
    "Strawberry Shake clients are generated by MSBuild code generation, not a source generator. Different tool, same idea: the build produces them.",
    "Generation happens in your build, so you get compile-time feedback instead of discovering a mismatch when the server starts.",
    "When the schema changes, published clients affected by the change are the ones you regenerate. The lineage tells you where to look.",
  ];
  return (
    <section className="border-t border-cc-card-border py-16 sm:py-20">
      <div className="grid gap-10 lg:grid-cols-[0.8fr_1.2fr] lg:gap-16">
        <div>
          <Eyebrow>What we will not oversell</Eyebrow>
          <h2 className="mt-5 text-h4 font-heading font-semibold text-balance text-cc-heading">
            The precise version, because the precision is the point.
          </h2>
        </div>
        <ul className="space-y-5">
          {points.map((p, i) => (
            <li
              key={i}
              className="flex gap-4 rounded-xl border border-cc-card-border bg-cc-card-bg p-5 backdrop-blur"
            >
              <span
                className="mt-0.5 font-mono text-[0.7rem]"
                style={{ color: "#16b9e4" }}
              >
                {String(i + 1).padStart(2, "0")}
              </span>
              <p className="text-body text-cc-prose">{p}</p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Closing CTA                                                         *
 * ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="border-t border-cc-card-border py-20 text-center">
      <Eyebrow>Build loop</Eyebrow>
      <h2 className="mx-auto mt-6 max-w-2xl text-h3 font-heading font-bold text-balance text-cc-heading">
        The schema you ship is the code you wrote.
      </h2>
      <p className="mx-auto mt-5 max-w-xl text-body text-cc-prose">
        Start with one C# class. Generate the server, the DataLoaders, and the
        typed clients from it, and keep them in step with every build.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Page                                                                *
 * ------------------------------------------------------------------ */

export default function BuildLoopPreviewPage() {
  return (
    <article className="pb-8">
      <Hero />

      <Chapter
        ordinal="01"
        kicker="One source of truth"
        heading="Annotate a partial class. The schema is a consequence."
        body={
          <>
            <p>
              Put <code className="font-mono text-[0.85em] text-cc-ink">[QueryType]</code>{" "}
              on a partial class and a Roslyn source generator reads it at build
              time. The SDL, the resolver pipeline, and the DataLoader
              infrastructure are emitted from that one place.
            </p>
            <p>
              There is no <code className="font-mono text-[0.85em] text-cc-ink">schema.graphql</code>{" "}
              sitting beside the code to drift out of step. The contract and the
              implementation are the same artifact.
            </p>
          </>
        }
        aside={
          <ul className="space-y-2">
            {[
              "Schema, resolvers and DI registration generated together",
              "Refactor-safe: rename in C#, the schema follows",
              "nameof keeps projections and topics honest",
            ].map((t) => (
              <li
                key={t}
                className="flex items-start gap-2.5 text-sm text-cc-ink-dim"
              >
                <span className="mt-1 text-[#5eead4]">
                  <CheckIcon size={12} />
                </span>
                {t}
              </li>
            ))}
          </ul>
        }
        diagram={<LineageDiagram />}
      />

      <Chapter
        ordinal="02"
        kicker="The N+1 problem, generated away"
        heading="One [DataLoader] method. Many keys collapse to one fetch."
        flip
        body={
          <>
            <p>
              Mark a static method with{" "}
              <code className="font-mono text-[0.85em] text-cc-ink">[DataLoader]</code>{" "}
              and the generator emits the loader class and interface. The
              execution engine resolves fields in waves and dispatches the
              pending batch together.
            </p>
            <p>
              Five field resolutions asking for five ids become a single{" "}
              <code className="font-mono text-[0.85em] text-cc-ink">WHERE id IN (…)</code>{" "}
              query, with keys deduplicated and cached per request.
            </p>
          </>
        }
        aside={
          <EditorCard
            filename="ProductDataLoader.cs"
            badge="you write this"
            lines={DATALOADER_SOURCE}
          />
        }
        diagram={<BatchingDiagram />}
      />

      <Chapter
        ordinal="03"
        kicker="Typed, all the way to the client"
        heading="Your .NET client is generated from the operations you run."
        body={
          <>
            <p>
              Strawberry Shake turns your{" "}
              <code className="font-mono text-[0.85em] text-cc-ink">.graphql</code>{" "}
              operations into typed .NET clients through MSBuild code generation.
              The call site, the result shape, and the field types are all known
              to the compiler.
            </p>
            <p>
              Because it runs in your build, a mismatched field is a build error,
              not a surprise in production. One language from resolver to call
              site.
            </p>
          </>
        }
        aside={
          <EditorCard
            filename="Program.cs"
            badge="you write this"
            lines={CLIENT_SOURCE}
          />
        }
        diagram={<CodegenRibbon />}
      />

      <ComparisonSection />
      <HonestyBeat />
      <ClosingCta />
    </article>
  );
}
