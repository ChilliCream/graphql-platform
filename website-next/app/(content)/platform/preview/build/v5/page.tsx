import type { Metadata } from "next";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Build Loop: The Annotated Source",
  description:
    "Implementation-first GraphQL .NET as a single annotated file. Hot Chocolate source generation and Strawberry Shake MSBuild codegen, line by line, from one C# class.",
  keywords: [
    "implementation-first GraphQL .NET",
    "Hot Chocolate source generation",
    "C# GraphQL schema",
    "Strawberry Shake MSBuild codegen",
    "DataLoader batching",
    "QueryType attribute",
    "generated GraphQL SDL",
    "no schema drift",
    "typed end to end GraphQL",
    ".NET GraphQL build loop",
  ],
  openGraph: {
    title: "Build Loop: The Annotated Source",
    description:
      "Read implementation-first GraphQL .NET as one continuous annotated file: ProductApi.cs, the build output, the generated SDL, the DataLoader trace, and the typed Strawberry Shake client.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Single accent event                                                       */
/* -------------------------------------------------------------------------- */

const ACCENT = "#5eead4"; // cc-accent

/* -------------------------------------------------------------------------- */
/*  GitHub-dark syntax palette (matches v1)                                   */
/* -------------------------------------------------------------------------- */

const SYN = {
  attr: "#7ee787",
  keyword: "#ff7b72",
  type: "#79c0ff",
  member: "#d2a8ff",
  string: "#a5d6ff",
  number: "#f2cc60",
  comment: "#8b949e",
  punct: "#c9d1d9",
  ok: "#7ee787",
  warn: "#f2cc60",
} as const;

interface TokProps {
  readonly c?: string;
  readonly children: ReactNode;
}

function T({ c, children }: TokProps) {
  return <span style={c ? { color: c } : undefined}>{children}</span>;
}

/* -------------------------------------------------------------------------- */
/*  Chrome primitives                                                         */
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

interface PanelChromeProps {
  readonly filename: string;
  readonly status?: string;
  readonly statusKind?: "ok" | "warn" | "muted";
}

function PanelChrome({ filename, status, statusKind = "ok" }: PanelChromeProps) {
  const statusColor =
    statusKind === "ok"
      ? ACCENT
      : statusKind === "warn"
        ? SYN.warn
        : "rgba(245,241,234,0.5)";
  const statusBg =
    statusKind === "ok"
      ? "rgba(94,234,212,0.08)"
      : statusKind === "warn"
        ? "rgba(242,204,96,0.08)"
        : "rgba(245,241,234,0.06)";
  const statusBorder =
    statusKind === "ok"
      ? "rgba(94,234,212,0.28)"
      : statusKind === "warn"
        ? "rgba(242,204,96,0.28)"
        : "rgba(245,241,234,0.16)";
  return (
    <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-3.5 py-2.5">
      <span className="flex gap-1.5" aria-hidden>
        <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f57]/80" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#febc2e]/80" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#28c840]/80" />
      </span>
      <span className="text-cc-ink-dim ml-1.5 font-mono text-[0.7rem] tracking-tight">
        {filename}
      </span>
      {status ? (
        <span
          className="ml-auto rounded-full px-2 py-0.5 font-mono text-[0.6rem] tracking-[0.08em] uppercase"
          style={{
            color: statusColor,
            backgroundColor: statusBg,
            border: `1px solid ${statusBorder}`,
          }}
        >
          {status}
        </span>
      ) : null}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Annotated line: shared gutter + leader-line treatment                     */
/* -------------------------------------------------------------------------- */

interface LineProps {
  readonly n: number;
  readonly indent?: number;
  readonly highlight?: boolean;
  readonly anchor?: boolean; // shows the cc-accent leader tick on the left
  readonly children: ReactNode;
}

function Line({ n, indent = 0, highlight = false, anchor = false, children }: LineProps) {
  return (
    <div
      className={[
        "relative flex items-start",
        highlight ? "bg-[rgba(94,234,212,0.07)]" : "",
      ].join(" ")}
    >
      {anchor ? (
        <span
          className="pointer-events-none absolute top-0 bottom-0 left-0 w-[2px]"
          style={{ backgroundColor: ACCENT }}
          aria-hidden
        />
      ) : null}
      <span
        className="text-cc-nav-label w-10 shrink-0 pr-3 text-right font-mono text-[0.7rem] leading-6 select-none"
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

/* -------------------------------------------------------------------------- */
/*  Gutter annotation (the page's signature)                                  */
/* -------------------------------------------------------------------------- */

interface GutterNoteProps {
  readonly label: string;
  readonly title: string;
  readonly children: ReactNode;
}

function GutterNote({ label, title, children }: GutterNoteProps) {
  return (
    <aside className="relative">
      <span
        className="inline-flex items-center gap-2 rounded-full px-2.5 py-1 font-mono text-[0.62rem] tracking-[0.08em] uppercase"
        style={{
          color: ACCENT,
          backgroundColor: "rgba(94,234,212,0.06)",
          border: "1px solid rgba(94,234,212,0.28)",
        }}
      >
        <span
          className="h-px w-4"
          style={{ backgroundColor: ACCENT }}
          aria-hidden
        />
        {label}
      </span>
      <h3 className="text-cc-heading font-heading mt-3 text-[1.05rem] font-semibold tracking-tight">
        {title}
      </h3>
      <div className="text-cc-prose mt-2 text-[0.95rem] leading-relaxed">
        {children}
      </div>
    </aside>
  );
}

/* -------------------------------------------------------------------------- */
/*  Section wrapper: gutter (lg) + panel column                               */
/* -------------------------------------------------------------------------- */

interface SectionProps {
  readonly eyebrow: string;
  readonly heading: ReactNode;
  readonly notes: ReactNode;
  readonly panel: ReactNode;
}

function AnnotatedSection({ eyebrow, heading, notes, panel }: SectionProps) {
  return (
    <section className="mx-auto w-full max-w-5xl">
      <div className="mb-6 lg:pl-[300px]">
        <Eyebrow>{eyebrow}</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-3 text-h3 font-semibold tracking-tight">
          {heading}
        </h2>
      </div>
      <div className="grid gap-6 lg:grid-cols-[280px_1fr] lg:gap-10">
        <div className="flex flex-col gap-6">{notes}</div>
        <div className="min-w-0">{panel}</div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Panel 01 - annotated ProductApi.cs                                        */
/* -------------------------------------------------------------------------- */

function Panel01ProductApi() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
      <PanelChrome filename="ProductApi.cs" status="build · ok" />
      <div className="overflow-x-auto py-3">
        <Line n={1} anchor>
          <T c={SYN.attr}>[QueryType]</T>
        </Line>
        <Line n={2}>
          <T c={SYN.keyword}>public partial class</T>{" "}
          <T c={SYN.type}>ProductApi</T>
        </Line>
        <Line n={3}>{"{"}</Line>
        <Line n={4} indent={1} anchor>
          <T c={SYN.keyword}>public static</T> <T c={SYN.type}>Product</T>{" "}
          <T c={SYN.member}>GetProduct</T>(
        </Line>
        <Line n={5} indent={2}>
          <T c={SYN.type}>int</T> id,
        </Line>
        <Line n={6} indent={2}>
          <T c={SYN.type}>ProductService</T> service)
        </Line>
        <Line n={7} indent={3}>
          {"=> service."}
          <T c={SYN.member}>ById</T>
          {"(id);"}
        </Line>
        <Line n={8}>{""}</Line>
        <Line n={9} indent={1} highlight anchor>
          <T c={SYN.attr}>[DataLoader]</T>
        </Line>
        <Line n={10} indent={1} highlight>
          <T c={SYN.keyword}>internal static async</T>{" "}
          <T c={SYN.type}>Task</T>
          {"<"}
          <T c={SYN.type}>IReadOnlyDictionary</T>
          {"<"}
          <T c={SYN.type}>int</T>, <T c={SYN.type}>Product</T>
          {">>"}
        </Line>
        <Line n={11} indent={2} highlight>
          <T c={SYN.member}>GetProductsAsync</T>(
        </Line>
        <Line n={12} indent={3} highlight>
          <T c={SYN.type}>IReadOnlyList</T>
          {"<"}
          <T c={SYN.type}>int</T>
          {">"} keys,
        </Line>
        <Line n={13} indent={3} highlight>
          <T c={SYN.type}>ProductService</T> service)
        </Line>
        <Line n={14} indent={3} highlight>
          {"=> service."}
          <T c={SYN.member}>ByIds</T>
          {"(keys);"}
          <T c={SYN.comment}>{" // batches N keys, 1 fetch"}</T>
        </Line>
        <Line n={15}>{"}"}</Line>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Panel 02 - build log terminal                                             */
/* -------------------------------------------------------------------------- */

interface LogLineProps {
  readonly n: number;
  readonly anchor?: boolean;
  readonly children: ReactNode;
}

function LogLine({ n, anchor = false, children }: LogLineProps) {
  return (
    <div
      className={[
        "relative flex items-start",
        anchor ? "bg-[rgba(94,234,212,0.06)]" : "",
      ].join(" ")}
    >
      {anchor ? (
        <span
          className="pointer-events-none absolute top-0 bottom-0 left-0 w-[2px]"
          style={{ backgroundColor: ACCENT }}
          aria-hidden
        />
      ) : null}
      <span
        className="text-cc-nav-label w-10 shrink-0 pr-3 text-right font-mono text-[0.7rem] leading-6 select-none"
        aria-hidden
      >
        {n}
      </span>
      <span
        className="font-mono text-[0.78rem] leading-6"
        style={{ color: SYN.punct }}
      >
        {children}
      </span>
    </div>
  );
}

function Panel02BuildLog() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
      <PanelChrome filename="dotnet build · MSBuild" status="build · ok" />
      <div className="overflow-x-auto py-3">
        <LogLine n={1}>
          <T c={SYN.comment}>$ dotnet build ProductApi.csproj</T>
        </LogLine>
        <LogLine n={2}>
          <T c={SYN.member}>Restore</T> succeeded in{" "}
          <T c={SYN.number}>0.8s</T>
        </LogLine>
        <LogLine n={3} anchor>
          <T c={SYN.ok}>{"→ "}</T>
          HotChocolate.Types.Analyzers: <T c={SYN.attr}>[QueryType]</T>{" "}
          discovered on <T c={SYN.type}>ProductApi</T>
        </LogLine>
        <LogLine n={4} anchor>
          <T c={SYN.ok}>{"→ "}</T>
          Emitting resolver pipeline for{" "}
          <T c={SYN.member}>GetProduct(id)</T>
        </LogLine>
        <LogLine n={5} anchor>
          <T c={SYN.ok}>{"→ "}</T>
          Generating <T c={SYN.string}>schema.graphql</T> from registered types
        </LogLine>
        <LogLine n={6} anchor>
          <T c={SYN.ok}>{"→ "}</T>
          Wiring <T c={SYN.attr}>[DataLoader]</T>{" "}
          <T c={SYN.member}>GetProductsAsync</T> (batch + cache)
        </LogLine>
        <LogLine n={7} anchor>
          <T c={SYN.ok}>{"→ "}</T>
          StrawberryShake MSBuild: regenerating{" "}
          <T c={SYN.string}>ProductClient.cs</T> from operations
        </LogLine>
        <LogLine n={8}>
          <T c={SYN.member}>Build</T> succeeded in <T c={SYN.number}>2.4s</T>
        </LogLine>
        <LogLine n={9}>
          <T c={SYN.comment}>0 warning(s), 0 error(s)</T>
        </LogLine>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Panel 03 - generated SDL                                                  */
/* -------------------------------------------------------------------------- */

function Panel03Sdl() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
      <PanelChrome filename="schema.graphql · generated" status="contract" />
      <div className="overflow-x-auto py-3">
        <Line n={1}>
          <T c={SYN.comment}>{"# emitted from ProductApi.cs · do not edit"}</T>
        </Line>
        <Line n={2}>{""}</Line>
        <Line n={3} anchor>
          <T c={SYN.keyword}>type</T> <T c={SYN.type}>Query</T> {"{"}
        </Line>
        <Line n={4} indent={1}>
          <T c={SYN.member}>product</T>(<T c={SYN.string}>id</T>:{" "}
          <T c={SYN.type}>Int!</T>): <T c={SYN.type}>Product</T>
        </Line>
        <Line n={5}>{"}"}</Line>
        <Line n={6}>{""}</Line>
        <Line n={7} anchor>
          <T c={SYN.keyword}>type</T> <T c={SYN.type}>Product</T> {"{"}
        </Line>
        <Line n={8} indent={1}>
          <T c={SYN.member}>id</T>: <T c={SYN.type}>Int!</T>
        </Line>
        <Line n={9} indent={1}>
          <T c={SYN.member}>name</T>: <T c={SYN.type}>String!</T>
        </Line>
        <Line n={10} indent={1}>
          <T c={SYN.member}>price</T>: <T c={SYN.type}>Decimal!</T>
        </Line>
        <Line n={11}>{"}"}</Line>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Panel 04 - DataLoader trace                                               */
/* -------------------------------------------------------------------------- */

function Panel04DataLoader() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
      <PanelChrome filename="request.trace · DataLoader" status="1 fetch" />
      <div className="overflow-x-auto py-3">
        <LogLine n={1}>
          <T c={SYN.comment}>
            {"// request: { products(ids: [41,17,88,17,41]) { name } }"}
          </T>
        </LogLine>
        <LogLine n={2}>
          <T c={SYN.member}>GetProductsAsync</T> ← key{" "}
          <T c={SYN.number}>41</T>
        </LogLine>
        <LogLine n={3}>
          <T c={SYN.member}>GetProductsAsync</T> ← key{" "}
          <T c={SYN.number}>17</T>
        </LogLine>
        <LogLine n={4}>
          <T c={SYN.member}>GetProductsAsync</T> ← key{" "}
          <T c={SYN.number}>88</T>
        </LogLine>
        <LogLine n={5}>
          <T c={SYN.member}>GetProductsAsync</T> ← key{" "}
          <T c={SYN.number}>17</T> <T c={SYN.comment}>(deduped)</T>
        </LogLine>
        <LogLine n={6}>
          <T c={SYN.member}>GetProductsAsync</T> ← key{" "}
          <T c={SYN.number}>41</T> <T c={SYN.comment}>(deduped)</T>
        </LogLine>
        <LogLine n={7} anchor>
          <T c={SYN.ok}>{"→ "}</T>
          batch flush: <T c={SYN.member}>ByIds</T>([
          <T c={SYN.number}>41</T>,<T c={SYN.number}>17</T>,
          <T c={SYN.number}>88</T>])
        </LogLine>
        <LogLine n={8}>
          <T c={SYN.comment}>5 keys, 3 unique, 1 round trip</T>
        </LogLine>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Panel 05 - typed client call                                              */
/* -------------------------------------------------------------------------- */

function Panel05Client() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
      <PanelChrome filename="Program.cs · consumer" status="typed" />
      <div className="overflow-x-auto py-3">
        <Line n={1}>
          <T c={SYN.keyword}>using</T> Generated.Client;
        </Line>
        <Line n={2}>{""}</Line>
        <Line n={3}>
          <T c={SYN.keyword}>var</T> result ={" "}
          <T c={SYN.keyword}>await</T> client
        </Line>
        <Line n={4} indent={1}>
          .<T c={SYN.member}>GetProduct</T>
        </Line>
        <Line n={5} indent={1} highlight anchor>
          .<T c={SYN.member}>ExecuteAsync</T>(id);
          <T c={SYN.comment}>{"  // typed end to end"}</T>
        </Line>
        <Line n={6}>{""}</Line>
        <Line n={7}>
          <T c={SYN.keyword}>string</T> name = result.Data
        </Line>
        <Line n={8} indent={1}>
          .<T c={SYN.member}>Product</T>
        </Line>
        <Line n={9} indent={1}>
          .<T c={SYN.member}>Name</T>;
        </Line>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Panel 06 - comparison rendered as a // block comment                      */
/* -------------------------------------------------------------------------- */

interface CmpRow {
  readonly label: string;
  readonly handWired: string;
  readonly schemaFirst: string;
  readonly generated: string;
}

const CMP_ROWS: readonly CmpRow[] = [
  {
    label: "drift",
    handWired: "manual sync",
    schemaFirst: "DSL != code",
    generated: "one source",
  },
  {
    label: "types",
    handWired: "mostly typed",
    schemaFirst: "re-mapped",
    generated: "end to end",
  },
  {
    label: "n+1",
    handWired: "easy to miss",
    schemaFirst: "wired by hand",
    generated: "[DataLoader]",
  },
  {
    label: "client",
    handWired: "hand-rolled",
    schemaFirst: "separate gen",
    generated: "MSBuild gen",
  },
  {
    label: "feedback",
    handWired: "at runtime",
    schemaFirst: "codegen step",
    generated: "at build",
  },
];

function Panel06Comparison() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
      <PanelChrome filename="approaches.diff · block-comment" status="3 columns" />
      <div className="overflow-x-auto px-0 py-3">
        <Line n={1}>
          <T c={SYN.comment}>{"/*"}</T>
        </Line>
        <Line n={2}>
          <T c={SYN.comment}>{" *  hand-wired       schema-first DSL    source-generated"}</T>
        </Line>
        <Line n={3}>
          <T c={SYN.comment}>{" *  ---------------  -------------------  ----------------"}</T>
        </Line>
        {CMP_ROWS.map((row, i) => (
          <CmpLine key={row.label} n={4 + i} row={row} />
        ))}
        <Line n={4 + CMP_ROWS.length}>
          <T c={SYN.comment}>{" */"}</T>
        </Line>
      </div>
      <div className="border-cc-card-border text-cc-nav-label flex items-center gap-3 border-t px-3.5 py-2 font-mono text-[0.62rem] tracking-[0.04em]">
        <span
          className="inline-block h-2 w-2 rounded-full"
          style={{ backgroundColor: ACCENT }}
          aria-hidden
        />
        accent rows mark what source generation removes from the loop
      </div>
    </div>
  );
}

interface CmpLineProps {
  readonly n: number;
  readonly row: CmpRow;
}

function CmpLine({ n, row }: CmpLineProps) {
  return (
    <div className="relative flex items-start">
      <span
        className="text-cc-nav-label w-10 shrink-0 pr-3 text-right font-mono text-[0.7rem] leading-6 select-none"
        aria-hidden
      >
        {n}
      </span>
      <div
        className="grid w-full font-mono text-[0.78rem] leading-6"
        style={{
          gridTemplateColumns: "5ch 17ch 21ch 1fr",
          color: SYN.comment,
        }}
      >
        <span> * </span>
        <span>{row.handWired}</span>
        <span>{row.schemaFirst}</span>
        <span
          className="rounded-sm px-1"
          style={{
            color: ACCENT,
            backgroundColor: "rgba(94,234,212,0.08)",
          }}
        >
          {row.generated}
        </span>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Honesty strip - single-line // NOTE comment                               */
/* -------------------------------------------------------------------------- */

function HonestyStrip() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border">
      <div className="flex items-center gap-3 px-4 py-3">
        <span
          className="font-mono text-[0.78rem]"
          style={{ color: SYN.warn }}
          aria-hidden
        >
          ⚑
        </span>
        <span
          className="font-mono text-[0.78rem] leading-6"
          style={{ color: SYN.comment }}
        >
          {
            "// NOTE: generation removes drift inside this service. It does not freeze the world outside it."
          }
        </span>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing brace footer with CTA lines                                        */
/* -------------------------------------------------------------------------- */

function ClosingBracePanel() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg/95 overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
      <PanelChrome filename="ProductApi.cs · footer" status="ready" />
      <div className="py-3">
        <Line n={98}>
          {"}"} <T c={SYN.comment}>{"// end ProductApi"}</T>
        </Line>
        <Line n={99}>{""}</Line>
        <Line n={100}>
          <T c={SYN.comment}>{"// next:"}</T>
        </Line>
      </div>
      <div className="border-cc-card-border flex flex-col gap-3 border-t px-4 py-5 sm:flex-row sm:items-center">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
        <span className="text-cc-nav-label font-mono text-[0.7rem] sm:ml-auto">
          {"// scroll back to L1 to start over"}
        </span>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function BuildLoopV5Page() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      {/* ----------------------------- HERO ------------------------------ */}
      <section className="mx-auto w-full max-w-5xl">
        <div className="lg:pl-[300px]">
          <Eyebrow>Build loop · annotated source</Eyebrow>
          <h1 className="mt-5 text-hero tracking-tight">
            <span className="font-mono text-cc-heading">ProductApi</span>
            <span
              className="font-mono bg-clip-text text-transparent"
              style={{
                backgroundImage: `linear-gradient(100deg, #16b9e4, ${ACCENT}, #7c92c6)`,
              }}
            >
              .cs
            </span>
            <span className="block text-h2 font-heading font-semibold text-cc-heading mt-3">
              implementation-first GraphQL .NET, read line by line.
            </span>
          </h1>
          <p className="text-cc-prose mt-6 max-w-2xl text-lead leading-relaxed">
            One annotated C# class is the file. Hot Chocolate source generation
            emits the schema, the resolver pipeline, and DataLoaders. Strawberry
            Shake MSBuild codegen emits the typed .NET client. The rest of this
            page is that file, scrolled.
          </p>
          <div className="mt-8 flex flex-wrap items-center gap-3">
            <span
              className="font-mono text-[0.78rem]"
              style={{ color: SYN.comment }}
            >
              {"// run it:"}
            </span>
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs">Read the Docs</OutlineButton>
          </div>
        </div>
      </section>

      {/* --------------------------- PANEL 01 ---------------------------- */}
      <AnnotatedSection
        eyebrow="Panel 01 · the class"
        heading="The single annotated source the rest of the file derives from."
        notes={
          <>
            <GutterNote label="L1 · [QueryType]" title="The annotation is the binding.">
              <T c={SYN.attr}>[QueryType]</T> marks the partial class as a
              GraphQL type root. There is no separate registration call and no
              DSL to keep in sync, the attribute is how Hot Chocolate finds the
              resolvers at build time.
            </GutterNote>
            <GutterNote label="L4 · GetProduct" title="A method is a field.">
              The static method becomes the{" "}
              <code className="font-mono text-cc-ink">product(id)</code> field
              on <code className="font-mono text-cc-ink">Query</code>.
              Parameters become field arguments, return types become GraphQL
              types, services arrive via DI.
            </GutterNote>
            <GutterNote label="L9-L14 · [DataLoader]" title="Batching is generated, not hand-wired.">
              The <T c={SYN.attr}>[DataLoader]</T> attribute on a key-to-value
              method tells the source generator to emit a batched, request-scoped
              loader. The resolver pipeline calls it automatically when a field
              fans out across a list.
            </GutterNote>
          </>
        }
        panel={<Panel01ProductApi />}
      />

      {/* --------------------------- PANEL 02 ---------------------------- */}
      <AnnotatedSection
        eyebrow="Panel 02 · the build"
        heading="The loop closes at build, not at the first failing request."
        notes={
          <>
            <GutterNote label="L3 · analyzer" title="Hot Chocolate finds the class.">
              The Hot Chocolate analyzer runs in-process with the C# compiler.
              It discovers <T c={SYN.attr}>[QueryType]</T> partials and emits
              registration code into the same compilation, so the schema is
              part of what is built.
            </GutterNote>
            <GutterNote label="L5 · schema" title="The contract is a build artifact.">
              The SDL is re-derived from the types and methods that were just
              compiled. The schema file you publish cannot disagree with the
              code that answers requests, because it came from that code.
            </GutterNote>
            <GutterNote label="L7 · MSBuild codegen" title="The typed client comes from MSBuild.">
              Strawberry Shake is an MSBuild task, not a source generator. It
              reads your <code className="font-mono text-cc-ink">.graphql</code>{" "}
              operations and regenerates{" "}
              <code className="font-mono text-cc-ink">ProductClient.cs</code>{" "}
              every build, so the caller is typed against the same schema the
              service just emitted.
            </GutterNote>
          </>
        }
        panel={<Panel02BuildLog />}
      />

      {/* --------------------------- PANEL 03 ---------------------------- */}
      <AnnotatedSection
        eyebrow="Panel 03 · the contract"
        heading="The same file, scrolled further: the SDL that gets published."
        notes={
          <>
            <GutterNote label="L3 · Query" title="The published query root.">
              The <code className="font-mono text-cc-ink">Query.product</code>{" "}
              field is the public surface of the{" "}
              <code className="font-mono text-cc-ink">GetProduct</code> method
              from Panel 01, L4. The argument list and nullability come from
              the C# signature.
            </GutterNote>
            <GutterNote label="L7 · Product" title="Types follow shapes, not extra mappings.">
              The <code className="font-mono text-cc-ink">Product</code> type
              mirrors the CLR type. There is no schema file you maintain
              alongside the model and no mapping table to keep in step.
            </GutterNote>
            <GutterNote label="contract" title="One schema, traced.">
              This is the artifact your consumers see. Every field on it
              traces back to a method or property on a class in the same
              project, which is what we mean when we say the schema cannot
              drift inside the service.
            </GutterNote>
          </>
        }
        panel={<Panel03Sdl />}
      />

      {/* --------------------------- PANEL 04 ---------------------------- */}
      <AnnotatedSection
        eyebrow="Panel 04 · the trace"
        heading="The DataLoader collapses five inbound keys into one fetch."
        notes={
          <>
            <GutterNote label="L2-L6 · inbound" title="Five keys arrive in one tick.">
              A single GraphQL request asks for five products. The generated
              loader records each key without touching the database.
            </GutterNote>
            <GutterNote label="L7 · flush" title="One batched call to ByIds.">
              At the end of the request scope the loader deduplicates and
              flushes one call to <code className="font-mono text-cc-ink">ByIds([41,17,88])</code>{" "}
              The N+1 cliff was removed by the attribute, not by review.
            </GutterNote>
            <GutterNote label="guarantee" title="A property of generated code.">
              Because the loader is generated from{" "}
              <T c={SYN.attr}>[DataLoader]</T> on L9 of Panel 01, the batching
              behaviour is the same in every endpoint that uses it, not a
              convention some resolvers remember to honour.
            </GutterNote>
          </>
        }
        panel={<Panel04DataLoader />}
      />

      {/* --------------------------- PANEL 05 ---------------------------- */}
      <AnnotatedSection
        eyebrow="Panel 05 · the caller"
        heading="The round-trip closes on the consumer side, in the same language."
        notes={
          <>
            <GutterNote label="L1 · using" title="Generated.Client is yours.">
              The namespace was emitted by Strawberry Shake at build, against
              the same schema Panel 03 published. The client moves when the
              schema moves.
            </GutterNote>
            <GutterNote label="L5 · ExecuteAsync" title="Typed against the contract.">
              <code className="font-mono text-cc-ink">GetProduct.ExecuteAsync(id)</code>{" "}
              is a method, not a string. If the field is renamed in the
              schema, this line fails the next build, not the next deploy.
            </GutterNote>
            <GutterNote label="round-trip" title="From class to caller.">
              Class on L1 of Panel 01, schema on L3 of Panel 03, client call on
              L5 here. The same code shape carries the contract from server to
              consumer in one language.
            </GutterNote>
          </>
        }
        panel={<Panel05Client />}
      />

      {/* --------------------------- PANEL 06 ---------------------------- */}
      <AnnotatedSection
        eyebrow="Panel 06 · the diff"
        heading="The same loop, written three ways, as a block comment."
        notes={
          <>
            <GutterNote label="cols 1-2" title="What you maintain by hand.">
              Hand-wired and schema-first DSL stacks keep a contract artefact
              and a code artefact in step manually. The right column shows the
              same five loop properties when one source generates the rest.
            </GutterNote>
            <GutterNote label="col 3" title="What generation removes.">
              Source generation is the column that removes each row from the
              list of things you have to remember: drift, types, batching,
              client sync, and the moment you find out something broke.
            </GutterNote>
            <GutterNote label="not a benchmark" title="A property comparison.">
              This is not a performance comparison. It names which parts of the
              build loop are derived for you and which you keep wired up
              yourself.
            </GutterNote>
          </>
        }
        panel={<Panel06Comparison />}
      />

      {/* --------------------------- HONESTY ----------------------------- */}
      <section className="mx-auto w-full max-w-5xl">
        <div className="mb-5 lg:pl-[300px]">
          <Eyebrow>Honesty · the narrow guarantee</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-3 text-h3 font-semibold tracking-tight">
            What this loop does, and what it does not.
          </h2>
        </div>
        <div className="grid gap-6 lg:grid-cols-[280px_1fr] lg:gap-10">
          <div className="flex flex-col gap-4">
            <p className="text-cc-prose text-[0.95rem] leading-relaxed">
              One annotated class means the schema, resolvers, and DataLoaders
              cannot disagree, because they are derived from the same code.
              That is the guarantee we make.
            </p>
            <p className="text-cc-ink-dim text-[0.95rem] leading-relaxed">
              It is not a promise about consumers you do not control. When a
              type changes, the schema diff tells you which published clients
              are affected so you can coordinate the rollout, rather than
              discovering it in production.
            </p>
          </div>
          <HonestyStrip />
        </div>
      </section>

      {/* ----------------------------- CTA ------------------------------- */}
      <section className="mx-auto w-full max-w-5xl">
        <div className="lg:pl-[300px]">
          <ClosingBracePanel />
        </div>
      </section>
    </div>
  );
}
