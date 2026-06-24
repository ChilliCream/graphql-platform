import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

/* -------------------------------------------------------------------------- */
/*  Inline cup glyph (currentColor) used where the foreign-palette Espresso   */
/*  icon would clash with the cc-* dark palette.                              */
/* -------------------------------------------------------------------------- */

interface CupGlyphProps {
  readonly className?: string;
}

function CupGlyph({ className }: CupGlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden
      className={className}
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M5 9h11v5a4 4 0 0 1-4 4H9a4 4 0 0 1-4-4V9Z" />
      <path d="M16 10h2a2 2 0 0 1 0 4h-2" />
      <path d="M8 3.5c0 1 1 1.5 1 2.5S8 7.5 8 8.5" />
      <path d="M12 3.5c0 1 1 1.5 1 2.5s-1 1.5-1 2.5" />
    </svg>
  );
}

export const metadata: Metadata = {
  title: "Build Loop: House Roast",
  description:
    "Implementation-first GraphQL .NET. One annotated C# class generates the schema, batching DataLoaders, and a typed Strawberry Shake client.",
  keywords: [
    "implementation-first GraphQL .NET",
    "Hot Chocolate source generation",
    "C# GraphQL schema",
    "Strawberry Shake typed client",
    "DataLoader batching",
    "QueryType attribute",
    "generated GraphQL SDL",
    "no schema drift",
    "typed end to end GraphQL",
    ".NET GraphQL build loop",
  ],
  openGraph: {
    title: "Ship From the Code That Runs It",
    description:
      "One annotated C# class generates the schema, resolver pipeline, DataLoaders, and a typed .NET client. Every artifact traces back to the code that runs in production.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Scene accent                                                              */
/*  Single color event: a clipped cyan (#16b9e4) -> teal (#5eead4) gradient.  */
/* -------------------------------------------------------------------------- */

const SCENE_FROM = "#16b9e4";
const SCENE_TO = "#5eead4";

/* -------------------------------------------------------------------------- */
/*  Small shared chrome primitives                                            */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface WindowDotsProps {
  readonly title: string;
  readonly meta?: ReactNode;
}

/** macOS-style traffic-light header used by every product-mock card. */
function WindowDots({ title, meta }: WindowDotsProps) {
  return (
    <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-3.5 py-2.5">
      <span className="flex gap-1.5" aria-hidden>
        <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f57]/80" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#febc2e]/80" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#28c840]/80" />
      </span>
      <span className="text-cc-ink-dim ml-1.5 font-mono text-[0.7rem] tracking-tight">
        {title}
      </span>
      {meta ? (
        <span className="text-cc-nav-label ml-auto flex items-center gap-1.5 font-mono text-[0.65rem] tracking-tight">
          {meta}
        </span>
      ) : null}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero editor mock                                                           */
/* -------------------------------------------------------------------------- */

interface CodeTokenProps {
  readonly c?: string;
  readonly children: ReactNode;
}

function T({ c, children }: CodeTokenProps) {
  return <span style={c ? { color: c } : undefined}>{children}</span>;
}

const SYN = {
  attr: "#7ee787",
  keyword: "#ff7b72",
  type: "#79c0ff",
  member: "#d2a8ff",
  string: "#a5d6ff",
  comment: "#8b949e",
  punct: "#c9d1d9",
};

interface CodeLineProps {
  readonly n: number;
  readonly indent?: number;
  readonly highlight?: boolean;
  readonly children: ReactNode;
}

function CodeLine({ n, indent = 0, highlight = false, children }: CodeLineProps) {
  return (
    <div
      className={[
        "flex items-start",
        highlight ? "bg-[#16b9e4]/8" : "",
      ].join(" ")}
    >
      <span
        className="text-cc-nav-label w-9 shrink-0 pr-3 text-right font-mono text-[0.7rem] leading-6 select-none"
        aria-hidden
      >
        {n}
      </span>
      <code
        className="font-mono text-[0.78rem] leading-6"
        style={{ paddingLeft: `${indent * 0.9}rem`, color: SYN.punct }}
      >
        {children}
      </code>
    </div>
  );
}

function HeroEditor() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40 backdrop-blur-md">
      <WindowDots title="ProductApi.cs" meta={<span>C#  ·  saved</span>} />
      <div className="overflow-x-auto py-3">
        <CodeLine n={1}>
          <T c={SYN.attr}>[QueryType]</T>
        </CodeLine>
        <CodeLine n={2}>
          <T c={SYN.keyword}>public partial class</T>{" "}
          <T c={SYN.type}>ProductApi</T>
        </CodeLine>
        <CodeLine n={3}>{"{"}</CodeLine>
        <CodeLine n={4} indent={1}>
          <T c={SYN.keyword}>public static</T> <T c={SYN.type}>Product</T>{" "}
          <T c={SYN.member}>GetProduct</T>(
        </CodeLine>
        <CodeLine n={5} indent={2}>
          <T c={SYN.type}>int</T> id,
        </CodeLine>
        <CodeLine n={6} indent={2}>
          <T c={SYN.type}>ProductService</T> service)
        </CodeLine>
        <CodeLine n={7} indent={3}>
          {"=> service."}
          <T c={SYN.member}>ById</T>
          {"(id);"}
        </CodeLine>
        <CodeLine n={8}>{""}</CodeLine>
        <CodeLine n={9} indent={1} highlight>
          <T c={SYN.attr}>[DataLoader]</T>
        </CodeLine>
        <CodeLine n={10} indent={1} highlight>
          <T c={SYN.keyword}>internal static async</T>{" "}
          <T c={SYN.type}>Task</T>
          {"<"}
          <T c={SYN.type}>IReadOnlyDictionary</T>
          {"<"}
          <T c={SYN.type}>int</T>, <T c={SYN.type}>Product</T>
          {">>"}
        </CodeLine>
        <CodeLine n={11} indent={2} highlight>
          <T c={SYN.member}>GetProductsAsync</T>(
        </CodeLine>
        <CodeLine n={12} indent={3} highlight>
          <T c={SYN.type}>IReadOnlyList</T>
          {"<"}
          <T c={SYN.type}>int</T>
          {">"} keys,
        </CodeLine>
        <CodeLine n={13} indent={3} highlight>
          <T c={SYN.type}>ProductService</T> service)
        </CodeLine>
        <CodeLine n={14} indent={3} highlight>
          {"=> service."}
          <T c={SYN.member}>ByIds</T>
          {"(keys);"}
          <T c={SYN.comment}>{" // batches N keys → 1 fetch"}</T>
        </CodeLine>
        <CodeLine n={15}>{"}"}</CodeLine>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Lineage: one bean, three cups                                              */
/* -------------------------------------------------------------------------- */

interface ArtifactCardProps {
  readonly tag: string;
  readonly title: string;
  readonly tagIcon?: ReactNode;
  readonly children: ReactNode;
}

function ArtifactCard({ tag, title, tagIcon, children }: ArtifactCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex flex-col overflow-hidden rounded-xl border backdrop-blur-sm">
      <div className="border-cc-card-border flex items-center justify-between border-b px-3.5 py-2.5">
        <span className="text-cc-heading font-mono text-[0.7rem]">{title}</span>
        <span
          className="flex items-center gap-1.5 rounded-full px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase"
          style={{
            color: SCENE_TO,
            backgroundColor: "rgba(94, 234, 212, 0.08)",
            border: "1px solid rgba(94, 234, 212, 0.22)",
          }}
        >
          {tagIcon ? (
            <span
              aria-hidden
              className="flex items-center"
              style={{ color: SCENE_TO, opacity: 0.85 }}
            >
              {tagIcon}
            </span>
          ) : null}
          {tag}
        </span>
      </div>
      <div className="flex grow flex-col p-3.5">{children}</div>
    </div>
  );
}

function SdlArtifact() {
  return (
    <ArtifactCard
      tag="roast"
      title="schema.graphql"
      tagIcon={<PourOver className="h-3 w-3" />}
    >
      <pre className="font-mono text-[0.72rem] leading-5">
        <code>
          <span style={{ color: SYN.keyword }}>type</span>{" "}
          <span style={{ color: SYN.type }}>Query</span> {"{"}
          {"\n  "}
          <span style={{ color: SYN.member }}>product</span>(
          <span style={{ color: "#ffa657" }}>id</span>:{" "}
          <span style={{ color: SYN.type }}>Int!</span>):{" "}
          <span style={{ color: SYN.type }}>Product</span>
          {"\n"}
          {"}"}
          {"\n\n"}
          <span style={{ color: SYN.keyword }}>type</span>{" "}
          <span style={{ color: SYN.type }}>Product</span> {"{"}
          {"\n  "}
          <span style={{ color: SYN.member }}>id</span>:{" "}
          <span style={{ color: SYN.type }}>Int!</span>
          {"\n  "}
          <span style={{ color: SYN.member }}>name</span>:{" "}
          <span style={{ color: SYN.type }}>String!</span>
          {"\n"}
          {"}"}
        </code>
      </pre>
    </ArtifactCard>
  );
}

function BatchArtifact() {
  const keys = [41, 17, 88, 17, 41];
  return (
    <ArtifactCard
      tag="brew"
      title="DataLoader"
      tagIcon={<FrenchPress className="h-3 w-3" />}
    >
      <div className="flex grow items-center justify-between gap-2">
        <div className="flex flex-col gap-1.5">
          {keys.map((k, i) => (
            <span
              key={`${k}-${i}`}
              className="text-cc-ink-dim border-cc-card-border bg-cc-surface rounded-md border px-2 py-1 text-center font-mono text-[0.65rem] tabular-nums"
            >
              {k}
            </span>
          ))}
        </div>

        <svg
          viewBox="0 0 64 120"
          className="h-28 w-16 shrink-0"
          aria-hidden
          fill="none"
        >
          <defs>
            <linearGradient id="batch-wire-v6" x1="0" y1="0" x2="64" y2="0">
              <stop offset="0" stopColor={SCENE_FROM} />
              <stop offset="1" stopColor={SCENE_TO} />
            </linearGradient>
          </defs>
          {[12, 33, 54, 75, 96].map((y, i) => (
            <path
              key={i}
              d={`M0 ${y} C 26 ${y}, 26 60, 60 60`}
              stroke="url(#batch-wire-v6)"
              strokeWidth="1.5"
              opacity={0.85}
            />
          ))}
          <circle cx="61" cy="60" r="3" fill={SCENE_TO} />
        </svg>

        <div className="flex flex-col items-center gap-1">
          <span
            className="rounded-md px-2.5 py-1.5 text-center font-mono text-[0.62rem] leading-tight"
            style={{
              color: SCENE_TO,
              border: "1px solid rgba(94, 234, 212, 0.3)",
              backgroundColor: "rgba(94, 234, 212, 0.06)",
            }}
          >
            ByIds(
            <br />
            [41,17,88])
          </span>
          <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-tight">
            1 query
          </span>
        </div>
      </div>
      <p className="text-cc-nav-label border-cc-card-border mt-3 border-t pt-2.5 font-mono text-[0.6rem] tracking-tight">
        5 keys · deduped → 3 ids → 1 fetch
      </p>
    </ArtifactCard>
  );
}

function ClientArtifact() {
  return (
    <ArtifactCard tag="pour" title="ProductClient.cs">
      <pre className="font-mono text-[0.72rem] leading-5">
        <code>
          <span style={{ color: SYN.keyword }}>var</span> result ={"\n  "}
          <span style={{ color: SYN.keyword }}>await</span> client
          {"\n    ."}
          <span style={{ color: SYN.member }}>GetProduct</span>
          {"\n    ."}
          <span style={{ color: SYN.member }}>ExecuteAsync</span>(
          <span style={{ color: SYN.string }}>id</span>);
          {"\n\n"}
          <span style={{ color: SYN.keyword }}>string</span> name ={"\n  "}
          result.Data
          {"\n    ."}
          <span style={{ color: SYN.member }}>Product</span>
          {"\n    ."}
          <span style={{ color: SYN.member }}>Name</span>;
          <span style={{ color: SYN.comment }}>{" // typed"}</span>
        </code>
      </pre>
    </ArtifactCard>
  );
}

/* -------------------------------------------------------------------------- */
/*  Build ribbon: Behind the bar (Roast / Grind / Brew / Pour)                 */
/* -------------------------------------------------------------------------- */

interface BarStageProps {
  readonly index: number;
  readonly label: string;
  readonly note: string;
  readonly icon: ReactNode;
  readonly last?: boolean;
}

function BarStage({ index, label, note, icon, last = false }: BarStageProps) {
  return (
    <li className="relative flex flex-1 flex-col">
      <div className="flex items-center">
        <span
          className="relative z-10 flex h-7 w-7 shrink-0 items-center justify-center rounded-full font-mono text-[0.7rem] font-semibold"
          style={{
            color: "#0b0f1a",
            background: `linear-gradient(135deg, ${SCENE_FROM}, ${SCENE_TO})`,
          }}
        >
          {index}
        </span>
        {!last ? (
          <span
            className="h-px flex-1"
            style={{
              background:
                "linear-gradient(90deg, rgba(94,234,212,0.5), rgba(94,234,212,0.12))",
            }}
            aria-hidden
          />
        ) : null}
      </div>
      <div className="mt-3 flex items-center gap-2">
        <span
          aria-hidden
          className="flex h-5 w-5 items-center justify-center"
          style={{ color: SCENE_TO, opacity: 0.55 }}
        >
          {icon}
        </span>
        <p className="text-cc-heading font-mono text-[0.72rem] tracking-tight">
          {label}
        </p>
      </div>
      <p className="text-cc-ink-dim mt-1 pr-4 text-[0.78rem] leading-snug">
        {note}
      </p>
    </li>
  );
}

/* -------------------------------------------------------------------------- */
/*  Comparison table                                                           */
/* -------------------------------------------------------------------------- */

type Cell = "good" | "warn" | "bad";

interface RowProps {
  readonly label: string;
  readonly handWired: Cell;
  readonly schemaFirst: Cell;
  readonly generated: Cell;
  readonly handWiredText: string;
  readonly schemaFirstText: string;
  readonly generatedText: string;
}

function CellMark({ kind, text }: { readonly kind: Cell; readonly text: string }) {
  const styles: Record<Cell, string> = {
    good: "text-cc-success",
    warn: "text-cc-warning",
    bad: "text-cc-danger",
  };
  const glyph: Record<Cell, string> = {
    good: "●",
    warn: "◐",
    bad: "○",
  };
  return (
    <span className="flex items-center gap-2 text-[0.82rem]">
      <span className={`${styles[kind]} text-[0.7rem]`} aria-hidden>
        {glyph[kind]}
      </span>
      <span className="text-cc-ink-dim">{text}</span>
    </span>
  );
}

const COMPARISON: readonly RowProps[] = [
  {
    label: "Schema drift",
    handWired: "warn",
    handWiredText: "Manual sync",
    schemaFirst: "bad",
    schemaFirstText: "DSL ≠ code",
    generated: "good",
    generatedText: "One source",
  },
  {
    label: "Type safety",
    handWired: "warn",
    handWiredText: "Mostly typed",
    schemaFirst: "warn",
    schemaFirstText: "Re-mapped",
    generated: "good",
    generatedText: "End to end",
  },
  {
    label: "N+1 fetches",
    handWired: "bad",
    handWiredText: "Easy to miss",
    schemaFirst: "warn",
    schemaFirstText: "Wired by hand",
    generated: "good",
    generatedText: "[DataLoader]",
  },
  {
    label: "Client sync",
    handWired: "bad",
    handWiredText: "Hand-rolled",
    schemaFirst: "warn",
    schemaFirstText: "Separate gen",
    generated: "good",
    generatedText: "MSBuild gen",
  },
  {
    label: "Build feedback",
    handWired: "warn",
    handWiredText: "At runtime",
    schemaFirst: "warn",
    schemaFirstText: "Codegen step",
    generated: "good",
    generatedText: "At build",
  },
];

function ComparisonTable() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-sm">
      <div className="border-cc-card-border grid grid-cols-[1.1fr_1fr_1fr_1.1fr] border-b">
        <div className="px-4 py-3.5">
          <Eyebrow>approach</Eyebrow>
        </div>
        <div className="border-cc-card-border border-l px-4 py-3.5">
          <span className="text-cc-ink font-mono text-[0.72rem]">
            Hand-wired
          </span>
        </div>
        <div className="border-cc-card-border border-l px-4 py-3.5">
          <span className="text-cc-ink font-mono text-[0.72rem]">
            Schema-first DSL
          </span>
        </div>
        <div
          className="border-l px-4 py-3.5"
          style={{ borderColor: "rgba(94,234,212,0.3)" }}
        >
          <span className="font-mono text-[0.72rem]" style={{ color: SCENE_TO }}>
            Source-generated
          </span>
        </div>
      </div>

      {COMPARISON.map((row, i) => (
        <div
          key={row.label}
          className={[
            "grid grid-cols-[1.1fr_1fr_1fr_1.1fr] items-center",
            i % 2 === 1 ? "bg-cc-surface/40" : "",
          ].join(" ")}
        >
          <div className="px-4 py-3.5">
            <span className="text-cc-heading text-[0.85rem] font-medium">
              {row.label}
            </span>
          </div>
          <div className="border-cc-card-border border-l px-4 py-3.5">
            <CellMark kind={row.handWired} text={row.handWiredText} />
          </div>
          <div className="border-cc-card-border border-l px-4 py-3.5">
            <CellMark kind={row.schemaFirst} text={row.schemaFirstText} />
          </div>
          <div
            className="border-l px-4 py-3.5"
            style={{
              borderColor: "rgba(94,234,212,0.3)",
              backgroundColor: "rgba(94,234,212,0.04)",
            }}
          >
            <CellMark kind={row.generated} text={row.generatedText} />
          </div>
        </div>
      ))}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Section heading helper                                                     */
/* -------------------------------------------------------------------------- */

interface SectionHeadProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function SectionHead({ eyebrow, title, children }: SectionHeadProps) {
  return (
    <div className="max-w-2xl">
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-3 font-semibold tracking-tight">
        {title}
      </h2>
      {children ? (
        <p className="text-cc-prose mt-4 text-[1.05rem] leading-relaxed">
          {children}
        </p>
      ) : null}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export default function BuildLoopV6Page() {
  return (
    <div className="flex flex-col gap-28 py-6 sm:gap-36">
      {/* ----------------------------- HERO ----------------------------- */}
      <section className="grid items-center gap-12 lg:grid-cols-[0.9fr_1.1fr]">
        <div>
          <Eyebrow>On the menu · implementation-first</Eyebrow>
          <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
            Ship from the{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{
                backgroundImage: `linear-gradient(100deg, ${SCENE_FROM}, ${SCENE_TO})`,
              }}
            >
              code that runs it.
            </span>
          </h1>
          <p className="text-cc-prose mt-6 max-w-xl text-[1.15rem] leading-relaxed">
            Write your GraphQL API as annotated C#. Hot Chocolate generates the
            schema, the resolver pipeline, and DataLoaders straight from those
            classes, so the contract you publish is the implementation that
            answers the request.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs">Read the Docs</OutlineButton>
          </div>
          <ul className="text-cc-ink-dim mt-9 flex flex-wrap gap-x-6 gap-y-2 text-[0.85rem]">
            {[
              "No separate schema file to drift",
              "Typed end to end in one language",
              "DataLoaders generated, not hand-wired",
            ].map((item) => (
              <li key={item} className="flex items-center gap-2">
                <span style={{ color: SCENE_TO }}>
                  <CheckIcon size={13} />
                </span>
                {item}
              </li>
            ))}
          </ul>
        </div>

        <div className="relative">
          {/* faint scene-accent glow under the editor */}
          <div
            className="pointer-events-none absolute -inset-6 -z-10 rounded-3xl opacity-40 blur-2xl"
            style={{
              background: `radial-gradient(60% 60% at 30% 20%, ${SCENE_FROM}33, transparent 70%)`,
            }}
            aria-hidden
          />
          {/* chalkline label above the editor */}
          <div className="mb-3 flex items-center gap-2 pl-1">
            <span
              aria-hidden
              className="h-px w-6"
              style={{
                background:
                  "linear-gradient(90deg, rgba(94,234,212,0.45), rgba(94,234,212,0))",
              }}
            />
            <span
              className="font-mono text-[0.65rem] tracking-[0.18em] uppercase"
              style={{ color: SCENE_TO, opacity: 0.85 }}
            >
              Today&apos;s pour · single origin
            </span>
          </div>
          <HeroEditor />
        </div>
      </section>

      {/* --------------------------- LINEAGE ---------------------------- */}
      <section>
        <SectionHead
          eyebrow="House blend · one bean, three cups"
          title="Every cup traces back to one annotated class."
        >
          The <code className="font-mono text-cc-ink">ProductApi</code> class
          above is the only thing you maintain. At build time it fans out into
          the schema you serve, the batching that protects your database, and a
          typed client your callers consume.
        </SectionHead>

        {/* lineage connector rail */}
        <div className="mt-12">
          <div className="mb-6 flex justify-center">
            <span
              className="inline-flex items-center gap-2 rounded-lg border px-4 py-2 font-mono text-[0.78rem]"
              style={{
                color: SCENE_TO,
                borderColor: "rgba(94,234,212,0.3)",
                backgroundColor: "rgba(94,234,212,0.05)",
              }}
            >
              <CupGlyph className="h-4 w-4 opacity-80" />
              [QueryType] ProductApi
            </span>
          </div>
          <svg
            viewBox="0 0 600 44"
            className="mx-auto block h-11 w-full max-w-3xl"
            preserveAspectRatio="none"
            aria-hidden
            fill="none"
          >
            <defs>
              <linearGradient id="lineage-v6" x1="300" y1="0" x2="300" y2="44">
                <stop offset="0" stopColor={SCENE_FROM} />
                <stop offset="1" stopColor={SCENE_TO} />
              </linearGradient>
            </defs>
            <path
              d="M300 0 L300 14 M300 14 C 300 30, 100 14, 100 44 M300 14 L300 44 M300 14 C 300 30, 500 14, 500 44"
              stroke="url(#lineage-v6)"
              strokeWidth="1.5"
            />
          </svg>

          <div className="mt-3 grid gap-5 md:grid-cols-3">
            <SdlArtifact />
            <BatchArtifact />
            <ClientArtifact />
          </div>
        </div>
      </section>

      {/* ------------------------- BUILD RIBBON ------------------------- */}
      <section>
        <SectionHead
          eyebrow="Behind the bar · MSBuild codegen"
          title="The loop closes at the bar, not at the first failing order."
        >
          A save triggers the same generation your CI runs. The schema is
          re-derived, DataLoaders are rebuilt, and Strawberry Shake regenerates
          the typed client from your operations, all before anything hits a
          runtime path.
        </SectionHead>

        <div className="border-cc-card-border bg-cc-card-bg mt-10 rounded-2xl border p-6 backdrop-blur-sm sm:p-8">
          <ol className="flex flex-col gap-8 sm:flex-row sm:gap-0">
            <BarStage
              index={1}
              label="roast · annotate"
              note="Add [QueryType] / [DataLoader] to a partial class."
              icon={<PourOver className="h-5 w-5" />}
            />
            <BarStage
              index={2}
              label="grind · generate"
              note="Source generators emit the schema and resolver pipeline."
              icon={<DripBrewer className="h-5 w-5" />}
            />
            <BarStage
              index={3}
              label="brew · batch"
              note="DataLoader batches N keys into one fetch per request."
              icon={<FrenchPress className="h-5 w-5" />}
            />
            <BarStage
              index={4}
              label="pour · typed client"
              note="Strawberry Shake MSBuild codegen builds a typed .NET client."
              icon={<CupGlyph className="h-5 w-5" />}
              last
            />
          </ol>
        </div>
      </section>

      {/* --------------------- BARISTA'S BATCH (N+1) -------------------- */}
      <section>
        <SectionHead
          eyebrow="One pull, many cups"
          title="Five orders, one trip to the grinder."
        >
          The same DataLoader the build ribbon generated. Five inbound keys
          collapse to a single ByIds call, so a list of products costs one
          query, not one per row.
        </SectionHead>

        <div className="border-cc-card-border bg-cc-card-bg mt-8 flex items-center gap-5 rounded-2xl border p-6 backdrop-blur-sm sm:gap-7 sm:p-8">
          <span
            aria-hidden
            className="hidden h-12 w-12 shrink-0 items-center justify-center sm:flex"
            style={{ color: SCENE_TO, opacity: 0.55 }}
          >
            <FrenchPress className="h-12 w-12" />
          </span>
          <div className="flex grow flex-col gap-3">
            <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
              ticket
            </p>
            <ol className="text-cc-ink-dim flex flex-col gap-1.5 font-mono text-[0.78rem] leading-snug tabular-nums">
              <li>order in · 5 keys (41, 17, 88, 17, 41)</li>
              <li>dedupe · 3 unique ids</li>
              <li>
                <span style={{ color: SCENE_TO }}>pull · 1 ByIds([41,17,88])</span>
              </li>
            </ol>
          </div>
        </div>
      </section>

      {/* -------------------------- COLLAPSE ---------------------------- */}
      <section className="grid items-center gap-12 lg:grid-cols-[1fr_1fr]">
        <div>
          <SectionHead
            eyebrow="Fewer hands on the bar"
            title="Collapse the glue tangle into the class itself."
          >
            Schema-first stacks ask you to keep a DSL, a resolver map, and a
            client schema in step by hand. Implementation-first removes those
            seams: the annotation is the binding, so there is nothing in between
            to fall out of date.
          </SectionHead>
          <ul className="mt-6 flex flex-col gap-3">
            {[
              "No DSL file mirroring your types",
              "No resolver-to-field wiring table",
              "No separately maintained client schema",
            ].map((item) => (
              <li
                key={item}
                className="text-cc-ink-dim flex items-center gap-3 text-[0.95rem]"
              >
                <span style={{ color: SCENE_TO }}>
                  <CheckIcon size={14} />
                </span>
                {item}
              </li>
            ))}
          </ul>
        </div>

        {/* before -> after glue collapse diagram */}
        <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-sm">
          <WindowDots title="before  →  after" />
          <div className="grid grid-cols-[1fr_auto_0.8fr] items-center gap-3 p-5">
            <div className="flex flex-col gap-2">
              <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.18em] uppercase">
                pre-ground, pre-mixed, pre-bottled
              </span>
              {["schema.graphql", "Resolvers.cs", "client.schema", "mappings"].map(
                (l) => (
                  <span
                    key={l}
                    className="text-cc-ink-dim border-cc-card-border bg-cc-surface/60 rounded-md border border-dashed px-2.5 py-1.5 text-center font-mono text-[0.66rem]"
                  >
                    {l}
                  </span>
                ),
              )}
            </div>

            <svg
              viewBox="0 0 56 120"
              className="h-32 w-14"
              aria-hidden
              fill="none"
            >
              <defs>
                <linearGradient
                  id="collapse-v6"
                  x1="0"
                  y1="0"
                  x2="56"
                  y2="0"
                >
                  <stop offset="0" stopColor={SCENE_FROM} />
                  <stop offset="1" stopColor={SCENE_TO} />
                </linearGradient>
              </defs>
              {[18, 46, 74, 102].map((y, i) => (
                <path
                  key={i}
                  d={`M0 ${y} C 28 ${y}, 28 60, 54 60`}
                  stroke="url(#collapse-v6)"
                  strokeWidth="1.5"
                  opacity={0.8}
                />
              ))}
            </svg>

            <div className="flex flex-col gap-2">
              <span
                className="font-mono text-[0.55rem] tracking-[0.18em] uppercase"
                style={{ color: SCENE_TO, opacity: 0.8 }}
              >
                whole bean
              </span>
              <span
                className="rounded-lg border px-2.5 py-3 text-center font-mono text-[0.66rem] leading-tight"
                style={{
                  color: SCENE_TO,
                  borderColor: "rgba(94,234,212,0.32)",
                  backgroundColor: "rgba(94,234,212,0.06)",
                }}
              >
                ProductApi
                <br />
                .cs
              </span>
            </div>
          </div>
        </div>
      </section>

      {/* ------------------------- COMPARISON --------------------------- */}
      <section>
        <SectionHead
          eyebrow="Quality control on the bar"
          title="Three ways to build a GraphQL API. One keeps the contract honest."
        />
        <div className="mt-10">
          <ComparisonTable />
        </div>
        <p className="text-cc-nav-label mt-4 font-mono text-[0.66rem] tracking-tight">
          ● strong · ◐ partial · ○ weak, based on what each approach maintains
          by hand.
        </p>
      </section>

      {/* -------------------------- HONESTY ----------------------------- */}
      <section className="border-cc-card-border bg-cc-surface/50 rounded-2xl border p-8 backdrop-blur-sm sm:p-10">
        <Eyebrow>House policy</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-3 max-w-3xl font-semibold tracking-tight">
          Generation removes drift inside your service. It does not freeze the
          world outside it.
        </h2>
        <div className="mt-7 grid gap-6 sm:grid-cols-2">
          <p className="text-cc-prose text-[1rem] leading-relaxed">
            One annotated class means the schema, resolvers, and DataLoaders
            cannot disagree, because they are derived from the same code. That
            is a real, narrow guarantee, and it is the one we make.
          </p>
          <p className="text-cc-ink-dim text-[1rem] leading-relaxed">
            It is not a promise about consumers you do not control. When a type
            changes, the schema diff tells you which published clients are
            affected so you can coordinate the rollout, rather than discovering
            it in production.
          </p>
        </div>
      </section>

      {/* ---------------------------- CTA ------------------------------- */}
      <section className="flex flex-col items-center gap-7 py-6 text-center">
        <span
          aria-hidden
          className="flex h-10 w-10 items-center justify-center"
          style={{ color: SCENE_TO, opacity: 0.4 }}
        >
          <CupGlyph className="h-10 w-10" />
        </span>
        <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
          Start with the bean. Ship the cup.
        </h2>
        <p className="text-cc-prose max-w-xl text-[1.1rem] leading-relaxed">
          Build your first implementation-first GraphQL API in C# and watch the
          schema, batching, and a typed client appear from the code you already
          wrote.
        </p>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs">Read the Docs</OutlineButton>
        </div>
      </section>
    </div>
  );
}
