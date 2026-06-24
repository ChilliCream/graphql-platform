import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Build Loop: The Ledger",
  description:
    "Implementation-first GraphQL .NET side by side with hand-rolled schema-first. One annotated C# class generates the schema, DataLoaders, and a typed client.",
  keywords: [
    "implementation-first GraphQL",
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
    title: "The Ledger: Implementation-first vs hand-rolled GraphQL .NET",
    description:
      "One annotated C# class generates the schema, DataLoaders, and a typed Strawberry Shake client. Compared side by side with the hand-rolled schema-first loop.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Accent: single cyan event for the whole page.                              */
/* -------------------------------------------------------------------------- */

const ACCENT = "#16b9e4";
const ACCENT_SOFT = "rgba(22, 185, 228, 0.08)";
const ACCENT_RIM = "rgba(22, 185, 228, 0.32)";

/* -------------------------------------------------------------------------- */
/*  Syntax palette (matches v1 exactly).                                       */
/* -------------------------------------------------------------------------- */

const SYN = {
  attr: "#7ee787",
  keyword: "#ff7b72",
  type: "#79c0ff",
  member: "#d2a8ff",
  string: "#a5d6ff",
  comment: "#8b949e",
  punct: "#c9d1d9",
};

/* -------------------------------------------------------------------------- */
/*  Primitives                                                                 */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function Eyebrow({ children, className }: EyebrowProps) {
  return (
    <p
      className={[
        "text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase",
        className ?? "",
      ].join(" ")}
    >
      {children}
    </p>
  );
}

interface TokenProps {
  readonly c?: string;
  readonly children: ReactNode;
}

function T({ c, children }: TokenProps) {
  return <span style={c ? { color: c } : undefined}>{children}</span>;
}

interface WindowDotsProps {
  readonly title: string;
  readonly meta?: string;
  readonly dimmed?: boolean;
}

function WindowDots({ title, meta, dimmed = false }: WindowDotsProps) {
  return (
    <div
      className={[
        "bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-3.5 py-2.5",
        dimmed ? "opacity-70" : "",
      ].join(" ")}
    >
      <span className="flex gap-1.5" aria-hidden>
        <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f57]/70" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#febc2e]/70" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#28c840]/70" />
      </span>
      <span className="text-cc-ink-dim ml-1.5 font-mono text-[0.7rem] tracking-tight">
        {title}
      </span>
      {meta ? (
        <span className="text-cc-nav-label ml-auto font-mono text-[0.65rem] tracking-tight">
          {meta}
        </span>
      ) : null}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Status chips                                                               */
/* -------------------------------------------------------------------------- */

interface ChipProps {
  readonly children: ReactNode;
}

function CrossChip({ children }: ChipProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex items-center gap-1.5 rounded-full border bg-cc-surface/60 px-2 py-0.5 font-mono text-[0.58rem] tracking-[0.12em] uppercase">
      <svg
        viewBox="0 0 12 12"
        className="h-2.5 w-2.5"
        aria-hidden
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
      >
        <path d="M2 2 L10 10 M10 2 L2 10" />
      </svg>
      {children}
    </span>
  );
}

function CheckChip({ children }: ChipProps) {
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 font-mono text-[0.58rem] tracking-[0.12em] uppercase"
      style={{
        color: ACCENT,
        backgroundColor: ACCENT_SOFT,
        border: `1px solid ${ACCENT_RIM}`,
      }}
    >
      <CheckIcon size={9} />
      {children}
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/*  Ledger shell: a section card with a floor-to-ceiling vertical rule         */
/*  separating the Hand-rolled column from the Platform column.                */
/* -------------------------------------------------------------------------- */

interface LedgerShellProps {
  readonly eyebrow: string;
  readonly leftLabel: string;
  readonly rightLabel: string;
  readonly left: ReactNode;
  readonly right: ReactNode;
}

function LedgerShell({
  eyebrow,
  leftLabel,
  rightLabel,
  left,
  right,
}: LedgerShellProps) {
  return (
    <section className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border backdrop-blur-sm">
      {/* heading strap: left label | center eyebrow | right label */}
      <div className="border-cc-card-border grid grid-cols-1 border-b md:grid-cols-[1fr_auto_1fr]">
        <div className="border-cc-card-border border-b px-5 py-3 md:border-r md:border-b-0">
          <span className="text-cc-ink-dim font-mono text-[0.72rem] tracking-tight">
            {leftLabel}
          </span>
        </div>
        <div className="border-cc-card-border border-b px-5 py-3 text-center md:border-r md:border-b-0">
          <Eyebrow>{eyebrow}</Eyebrow>
        </div>
        <div className="px-5 py-3 md:text-right">
          <span
            className="font-mono text-[0.72rem] tracking-tight"
            style={{ color: ACCENT }}
          >
            {rightLabel}
          </span>
        </div>
      </div>

      {/* body: 1fr | 1fr divided by a 1px cc-card-border rule */}
      <div className="grid grid-cols-1 md:grid-cols-[1fr_1px_1fr]">
        <div className="p-6 sm:p-7">{left}</div>
        <div className="bg-cc-card-border hidden w-px md:block" aria-hidden />
        <div className="border-cc-card-border border-t p-6 sm:p-7 md:border-t-0">
          {right}
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Code lines                                                                 */
/* -------------------------------------------------------------------------- */

interface CodeLineProps {
  readonly n: number;
  readonly indent?: number;
  readonly highlight?: boolean;
  readonly dim?: boolean;
  readonly children: ReactNode;
}

function CodeLine({
  n,
  indent = 0,
  highlight = false,
  dim = false,
  children,
}: CodeLineProps) {
  return (
    <div
      className={[
        "flex items-start",
        highlight ? "bg-[#16b9e4]/8" : "",
        dim ? "opacity-60" : "",
      ].join(" ")}
    >
      <span
        className="text-cc-nav-label w-9 shrink-0 pr-3 text-right font-mono text-[0.7rem] leading-6 select-none"
        aria-hidden
      >
        {n}
      </span>
      <code
        className="font-mono text-[0.76rem] leading-6"
        style={{ paddingLeft: `${indent * 0.9}rem`, color: SYN.punct }}
      >
        {children}
      </code>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  HERO                                                                       */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur-sm">
      <div
        className="pointer-events-none absolute inset-y-0 right-0 -z-10 w-1/2"
        style={{
          background: `radial-gradient(60% 80% at 80% 30%, ${ACCENT}22, transparent 70%)`,
        }}
        aria-hidden
      />

      {/* heading strap */}
      <div className="border-cc-card-border grid grid-cols-1 border-b md:grid-cols-[1fr_auto_1fr]">
        <div className="border-cc-card-border border-b px-6 py-3 md:border-r md:border-b-0">
          <CrossChip>Hand-rolled</CrossChip>
        </div>
        <div className="border-cc-card-border border-b px-6 py-3 text-center md:border-r md:border-b-0">
          <Eyebrow>Build loop, the ledger</Eyebrow>
        </div>
        <div className="px-6 py-3 md:text-right">
          <CheckChip>Platform</CheckChip>
        </div>
      </div>

      {/* split headline + body: one h1 contains both clauses, visually split
          across the divider. The "without / with" labels read as eyebrows,
          not as decorative hero text. */}
      <div className="relative grid grid-cols-1 md:grid-cols-[1fr_1px_1fr]">
        {/* visually-hidden single-string heading for screen readers */}
        <h1 className="sr-only">
          From hand-rolled schema-first to implementation-first GraphQL .NET,
          without the drift.
        </h1>

        <div className="px-6 pt-10 pb-28 sm:px-10 sm:pt-14 sm:pb-32">
          <p className="text-cc-nav-label mb-4 font-mono text-[0.62rem] tracking-[0.22em] uppercase">
            without source generation
          </p>
          <p
            className="font-heading text-hero text-cc-ink-dim font-semibold tracking-tight"
            aria-hidden
          >
            Keep the schema in sync, by hand.
          </p>
          <p className="text-cc-ink-dim mt-6 max-w-md text-[1.05rem] leading-relaxed">
            A DSL file, a resolver map, and a separately generated client
            schema, all kept in step by hand. Drift is the default state.
          </p>

          <div className="mt-7 flex flex-col gap-2">
            {["schema.graphql", "Resolvers.cs", "client.schema"].map((f) => (
              <span
                key={f}
                className="text-cc-ink-dim border-cc-card-border max-w-xs rounded-md border border-dashed bg-cc-surface/40 px-3 py-1.5 font-mono text-[0.7rem]"
              >
                {f}
              </span>
            ))}
          </div>
        </div>

        <div className="bg-cc-card-border hidden w-px md:block" aria-hidden />

        <div className="border-cc-card-border border-t px-6 pt-10 pb-28 sm:px-10 sm:pt-14 sm:pb-32 md:border-t-0 md:text-right">
          <p
            className="mb-4 font-mono text-[0.62rem] tracking-[0.22em] uppercase"
            style={{ color: ACCENT }}
          >
            with source generation
          </p>
          <p
            className="font-heading text-hero font-semibold tracking-tight"
            style={{ color: ACCENT }}
            aria-hidden
          >
            Implementation-first GraphQL .NET, without the drift.
          </p>
          <p className="text-cc-prose mt-6 ml-auto max-w-md text-[1.05rem] leading-relaxed">
            One annotated C# partial class. Hot Chocolate generates the schema,
            the resolver pipeline, and DataLoaders. Strawberry Shake codegen
            emits a typed .NET client.
          </p>

          <div className="mt-7 flex justify-end">
            <span
              className="rounded-md border px-3 py-1.5 font-mono text-[0.7rem]"
              style={{
                color: ACCENT,
                borderColor: ACCENT_RIM,
                backgroundColor: ACCENT_SOFT,
              }}
            >
              [QueryType] ProductApi.cs
            </span>
          </div>
        </div>

        {/* CTA straddling the vertical rule: a centered pill anchored to the
            bottom center of the split grid, so the buttons bridge both halves
            instead of sitting in a separate footer strip. */}
        <div className="pointer-events-none absolute inset-x-0 bottom-6 hidden justify-center md:flex">
          <div
            className="bg-cc-card-bg pointer-events-auto flex items-center gap-3 rounded-full border px-3 py-2 backdrop-blur-sm"
            style={{ borderColor: ACCENT_RIM }}
          >
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs">Read the Docs</OutlineButton>
          </div>
        </div>
      </div>

      {/* mobile CTA: stacked under the split because there is no vertical
          rule to straddle at narrow widths. */}
      <div className="border-cc-card-border flex flex-col items-center gap-3 border-t px-6 py-6 sm:flex-row sm:justify-center md:hidden">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  SOURCE: Resolvers.cs + schema.graphql  vs  one ProductApi.cs               */
/* -------------------------------------------------------------------------- */

function SourceLeft() {
  return (
    <div className="flex flex-col gap-3">
      <CrossChip>two files, drifts</CrossChip>

      <div className="border-cc-card-border bg-cc-code-bg/60 overflow-hidden rounded-lg border border-dashed">
        <WindowDots title="schema.graphql" meta="hand-edited" dimmed />
        <div className="py-2">
          <CodeLine n={1} dim>
            <T c={SYN.keyword}>type</T> <T c={SYN.type}>Query</T> {"{"}
          </CodeLine>
          <CodeLine n={2} indent={1} dim>
            <T c={SYN.member}>product</T>(<T c={"#ffa657"}>id</T>:{" "}
            <T c={SYN.type}>Int!</T>): <T c={SYN.type}>Product</T>
          </CodeLine>
          <CodeLine n={3} dim>
            {"}"}
          </CodeLine>
          <CodeLine n={4} dim>
            {""}
          </CodeLine>
          <CodeLine n={5} dim>
            <T c={SYN.keyword}>type</T> <T c={SYN.type}>Product</T> {"{"}
          </CodeLine>
          <CodeLine n={6} indent={1} dim>
            <T c={SYN.member}>id</T>: <T c={SYN.type}>Int!</T>
          </CodeLine>
          <CodeLine n={7} indent={1} dim>
            <T c={SYN.member}>name</T>: <T c={SYN.type}>String!</T>
          </CodeLine>
          <CodeLine n={8} dim>
            {"}"}
          </CodeLine>
        </div>
      </div>

      <div className="border-cc-card-border bg-cc-code-bg/60 overflow-hidden rounded-lg border border-dashed">
        <WindowDots title="Resolvers.cs" meta="wire by hand" dimmed />
        <div className="py-2">
          <CodeLine n={1} dim>
            <T c={SYN.keyword}>public class</T>{" "}
            <T c={SYN.type}>QueryResolvers</T>
          </CodeLine>
          <CodeLine n={2} dim>
            {"{"}
          </CodeLine>
          <CodeLine n={3} indent={1} dim>
            <T c={SYN.keyword}>public</T> <T c={SYN.type}>Product</T>{" "}
            <T c={SYN.member}>GetProduct</T>(
          </CodeLine>
          <CodeLine n={4} indent={2} dim>
            <T c={SYN.type}>int</T> id,
          </CodeLine>
          <CodeLine n={5} indent={2} dim>
            <T c={SYN.type}>ProductService</T> service)
          </CodeLine>
          <CodeLine n={6} indent={3} dim>
            {"=> service."}
            <T c={SYN.member}>ById</T>
            {"(id);"}
          </CodeLine>
          <CodeLine n={7} dim>
            {"}"}
            <T c={SYN.comment}>{"  // no batching"}</T>
          </CodeLine>
        </div>
      </div>
    </div>
  );
}

function SourceRight() {
  return (
    <div className="flex flex-col gap-3">
      <CheckChip>one class, generated</CheckChip>

      <div
        className="overflow-hidden rounded-lg border"
        style={{
          borderColor: ACCENT_RIM,
          backgroundColor: "rgba(12, 19, 34, 0.75)",
        }}
      >
        <WindowDots title="ProductApi.cs" meta="C#  ·  saved" />
        <div className="py-2">
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
            <T c={SYN.type}>int</T> id, <T c={SYN.type}>ProductService</T> s)
          </CodeLine>
          <CodeLine n={6} indent={3}>
            {"=> s."}
            <T c={SYN.member}>ById</T>
            {"(id);"}
          </CodeLine>
          <CodeLine n={7}>{""}</CodeLine>
          <CodeLine n={8} indent={1} highlight>
            <T c={SYN.attr}>[DataLoader]</T>
          </CodeLine>
          <CodeLine n={9} indent={1} highlight>
            <T c={SYN.keyword}>internal static async</T>{" "}
            <T c={SYN.type}>Task</T>
            {"<"}
            <T c={SYN.type}>IReadOnlyDictionary</T>
            {"<"}
            <T c={SYN.type}>int</T>, <T c={SYN.type}>Product</T>
            {">>"}
          </CodeLine>
          <CodeLine n={10} indent={2} highlight>
            <T c={SYN.member}>GetProductsAsync</T>(
          </CodeLine>
          <CodeLine n={11} indent={3} highlight>
            <T c={SYN.type}>IReadOnlyList</T>
            {"<"}
            <T c={SYN.type}>int</T>
            {">"} keys, <T c={SYN.type}>ProductService</T> s)
          </CodeLine>
          <CodeLine n={12} indent={3} highlight>
            {"=> s."}
            <T c={SYN.member}>ByIds</T>
            {"(keys);"}
          </CodeLine>
          <CodeLine n={13}>{"}"}</CodeLine>
          <CodeLine n={14}>
            <T c={SYN.comment}>{"// schema + DataLoader generated"}</T>
          </CodeLine>
          <CodeLine n={15}>
            <T c={SYN.comment}>{"// typed client emitted at build"}</T>
          </CodeLine>
        </div>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  ARTIFACTS                                                                  */
/* -------------------------------------------------------------------------- */

interface ArtifactItem {
  readonly name: string;
  readonly tag: string;
}

function ArtifactsLeft() {
  const files: readonly ArtifactItem[] = [
    { name: "schema.graphql", tag: "DSL, hand-edited" },
    { name: "ResolverMap.cs", tag: "field to method, manual" },
    { name: "client.schema.graphql", tag: "copy, can drift" },
  ];
  return (
    <div className="flex flex-col gap-4">
      <CrossChip>three things to maintain</CrossChip>
      <div className="flex flex-col gap-3">
        {files.map((f) => (
          <div
            key={f.name}
            className="border-cc-card-border bg-cc-surface/40 rounded-lg border border-dashed p-4"
          >
            <p className="text-cc-ink font-mono text-[0.75rem]">{f.name}</p>
            <p className="text-cc-nav-label mt-1 font-mono text-[0.62rem] tracking-tight">
              {f.tag}
            </p>
          </div>
        ))}
      </div>
      <p className="text-cc-ink-dim text-[0.9rem] leading-snug">
        Three artifacts, three update cadences. Anything you forget to edit is a
        runtime surprise.
      </p>
    </div>
  );
}

function ArtifactsRight() {
  const items: readonly ArtifactItem[] = [
    { name: "schema.graphql", tag: "generated SDL" },
    { name: "DataLoader batch", tag: "N keys to 1 fetch" },
    { name: "ProductClient.cs", tag: "Strawberry Shake MSBuild" },
  ];
  return (
    <div className="flex flex-col gap-4">
      <CheckChip>one lineage, generated</CheckChip>

      <div
        className="overflow-hidden rounded-lg border"
        style={{ borderColor: ACCENT_RIM, backgroundColor: ACCENT_SOFT }}
      >
        <div
          className="flex items-center justify-between border-b px-4 py-2.5"
          style={{ borderColor: ACCENT_RIM }}
        >
          <span className="font-mono text-[0.7rem]" style={{ color: ACCENT }}>
            [QueryType] ProductApi
          </span>
          <span className="text-cc-nav-label font-mono text-[0.6rem]">
            generated
          </span>
        </div>
        <ul>
          {items.map((it, i) => (
            <li
              key={it.name}
              className={[
                "flex items-center justify-between px-4 py-3",
                i < items.length - 1 ? "border-b" : "",
              ].join(" ")}
              style={{ borderColor: ACCENT_RIM }}
            >
              <span className="text-cc-heading font-mono text-[0.74rem]">
                {it.name}
              </span>
              <span
                className="font-mono text-[0.62rem] tracking-tight"
                style={{ color: ACCENT }}
              >
                {it.tag}
              </span>
            </li>
          ))}
        </ul>
      </div>

      <p className="text-cc-prose text-[0.9rem] leading-snug">
        One annotated class fans out into the three artifacts above at build
        time. Nothing in between to fall out of date.
      </p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  LOOP                                                                       */
/* -------------------------------------------------------------------------- */

interface StepProps {
  readonly index: number;
  readonly label: string;
  readonly note: string;
  readonly tone: "left" | "right";
}

function Step({ index, label, note, tone }: StepProps) {
  const isRight = tone === "right";
  return (
    <li className="flex gap-4">
      <span
        className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full font-mono text-[0.7rem] font-semibold"
        style={
          isRight
            ? { color: "#0b0f1a", backgroundColor: ACCENT }
            : {
                color: "#cbd5e1",
                backgroundColor: "rgba(148, 163, 184, 0.12)",
                border: "1px solid rgba(148, 163, 184, 0.3)",
              }
        }
      >
        {index}
      </span>
      <div className="flex-1">
        <p
          className="font-mono text-[0.74rem] tracking-tight"
          style={isRight ? { color: ACCENT } : undefined}
        >
          {isRight ? label : <span className="text-cc-heading">{label}</span>}
        </p>
        <p
          className={[
            "mt-1 text-[0.85rem] leading-snug",
            isRight ? "text-cc-prose" : "text-cc-ink-dim",
          ].join(" ")}
        >
          {note}
        </p>
      </div>
    </li>
  );
}

function LoopLeft() {
  return (
    <div className="flex flex-col gap-5">
      <CrossChip>manual loop</CrossChip>
      <ol className="flex flex-col gap-5">
        <Step
          index={1}
          label="edit DSL"
          note="Open schema.graphql, add the new field, save."
          tone="left"
        />
        <Step
          index={2}
          label="re-map resolvers"
          note="Wire the field to a method in ResolverMap.cs by hand."
          tone="left"
        />
        <Step
          index={3}
          label="regen client"
          note="Run the client schema export, copy it across, rerun codegen."
          tone="left"
        />
        <Step
          index={4}
          label="hope it builds"
          note="Find the gaps at runtime, in tests, or in production."
          tone="left"
        />
      </ol>
    </div>
  );
}

function LoopRight() {
  return (
    <div className="flex flex-col gap-5">
      <CheckChip>MSBuild loop</CheckChip>
      <ol className="flex flex-col gap-5">
        <Step
          index={1}
          label="annotate"
          note="Add [QueryType] or [DataLoader] to a partial class."
          tone="right"
        />
        <Step
          index={2}
          label="generate"
          note="Hot Chocolate source generators emit the schema and resolver pipeline."
          tone="right"
        />
        <Step
          index={3}
          label="codegen"
          note="Strawberry Shake builds a typed .NET client from your operations."
          tone="right"
        />
        <Step
          index={4}
          label="ship"
          note="The contract you publish is the code that just compiled."
          tone="right"
        />
      </ol>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  N+1                                                                        */
/* -------------------------------------------------------------------------- */

function NPlusOneLeft() {
  const keys = [41, 17, 88, 17, 41];
  return (
    <div className="flex flex-col gap-4">
      <CrossChip>5 keys, 5 queries</CrossChip>

      <div className="border-cc-card-border bg-cc-surface/40 rounded-lg border border-dashed p-5">
        <div className="flex items-center justify-between gap-3">
          <div className="flex flex-col gap-1.5">
            {keys.map((k, i) => (
              <span
                key={`${k}-${i}`}
                className="text-cc-ink-dim border-cc-card-border rounded-md border bg-cc-surface px-2 py-1 text-center font-mono text-[0.65rem] tabular-nums"
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
            {[12, 33, 54, 75, 96].map((y, i) => (
              <path
                key={i}
                d={`M0 ${y} L 60 ${y}`}
                stroke="#ef4444"
                strokeWidth="1.2"
                strokeDasharray="3 3"
                opacity={0.75}
              />
            ))}
          </svg>

          <div className="flex flex-col gap-1.5">
            {keys.map((_, i) => (
              <span
                key={i}
                className="text-cc-ink-dim border-cc-card-border rounded-md border bg-cc-surface px-2 py-1 text-center font-mono text-[0.6rem]"
              >
                ById
              </span>
            ))}
          </div>
        </div>
      </div>

      <p className="text-cc-ink-dim text-[0.88rem] leading-snug">
        Each key fires its own database call. Easy to miss in review, expensive
        under fan-out.
      </p>
    </div>
  );
}

function NPlusOneRight() {
  const keys = [41, 17, 88, 17, 41];
  return (
    <div className="flex flex-col gap-4">
      <CheckChip>5 keys, 1 batched fetch</CheckChip>

      <div
        className="rounded-lg border p-5"
        style={{ borderColor: ACCENT_RIM, backgroundColor: ACCENT_SOFT }}
      >
        <div className="flex items-center justify-between gap-3">
          <div className="flex flex-col gap-1.5">
            {keys.map((k, i) => (
              <span
                key={`${k}-${i}`}
                className="text-cc-ink-dim border-cc-card-border rounded-md border bg-cc-surface px-2 py-1 text-center font-mono text-[0.65rem] tabular-nums"
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
              <linearGradient id="v4-batch-wire" x1="0" y1="0" x2="64" y2="0">
                <stop offset="0" stopColor={ACCENT} stopOpacity="0.45" />
                <stop offset="1" stopColor={ACCENT} />
              </linearGradient>
            </defs>
            {[12, 33, 54, 75, 96].map((y, i) => (
              <path
                key={i}
                d={`M0 ${y} C 26 ${y}, 26 60, 60 60`}
                stroke="url(#v4-batch-wire)"
                strokeWidth="1.5"
                opacity={0.9}
              />
            ))}
            <circle cx="61" cy="60" r="3" fill={ACCENT} />
          </svg>

          <div className="flex flex-col items-center justify-center">
            <span
              className="rounded-md px-2.5 py-1.5 text-center font-mono text-[0.62rem] leading-tight"
              style={{
                color: ACCENT,
                border: `1px solid ${ACCENT_RIM}`,
                backgroundColor: "rgba(22, 185, 228, 0.06)",
              }}
            >
              ByIds(
              <br />
              [41,17,88])
            </span>
            <span className="text-cc-nav-label mt-1.5 font-mono text-[0.55rem] tracking-tight">
              1 query
            </span>
          </div>
        </div>
      </div>

      <p className="text-cc-prose text-[0.88rem] leading-snug">
        [DataLoader] dedupes the 5 keys to 3 ids and runs a single ByIds fetch.
        Generated, not hand-wired.
      </p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  SCOREBOARD                                                                 */
/* -------------------------------------------------------------------------- */

type Cell = "good" | "warn" | "bad";

interface ScoreRow {
  readonly label: string;
  readonly leftKind: Cell;
  readonly leftText: string;
  readonly rightKind: Cell;
  readonly rightText: string;
}

const SCORE: readonly ScoreRow[] = [
  {
    label: "Schema drift",
    leftKind: "bad",
    leftText: "DSL not equal code",
    rightKind: "good",
    rightText: "One source",
  },
  {
    label: "Type safety",
    leftKind: "warn",
    leftText: "Re-mapped by hand",
    rightKind: "good",
    rightText: "Typed end to end",
  },
  {
    label: "N+1 fetches",
    leftKind: "bad",
    leftText: "Easy to miss",
    rightKind: "good",
    rightText: "[DataLoader]",
  },
  {
    label: "Client sync",
    leftKind: "bad",
    leftText: "Hand-rolled",
    rightKind: "good",
    rightText: "MSBuild codegen",
  },
  {
    label: "Build feedback",
    leftKind: "warn",
    leftText: "At runtime",
    rightKind: "good",
    rightText: "At build",
  },
];

interface CellMarkProps {
  readonly kind: Cell;
  readonly text: string;
  readonly tone: "left" | "right";
}

function CellMark({ kind, text, tone }: CellMarkProps) {
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
      <span
        className={tone === "right" ? "text-cc-prose" : "text-cc-ink-dim"}
        style={
          tone === "right" && kind === "good" ? { color: ACCENT } : undefined
        }
      >
        {text}
      </span>
    </span>
  );
}

function ScoreboardLeft() {
  return (
    <div className="flex flex-col gap-3">
      <CrossChip>hand-rolled</CrossChip>
      <ul className="flex flex-col">
        {SCORE.map((row, i) => (
          <li
            key={row.label}
            className={[
              "py-3",
              i < SCORE.length - 1 ? "border-b border-cc-card-border" : "",
            ].join(" ")}
          >
            <p className="text-cc-heading mb-1.5 text-[0.85rem] font-medium">
              {row.label}
            </p>
            <CellMark kind={row.leftKind} text={row.leftText} tone="left" />
          </li>
        ))}
      </ul>
    </div>
  );
}

function ScoreboardRight() {
  return (
    <div className="flex flex-col gap-3">
      <CheckChip>platform</CheckChip>
      <ul className="flex flex-col">
        {SCORE.map((row, i) => (
          <li
            key={row.label}
            className={[
              "py-3",
              i < SCORE.length - 1 ? "border-b border-cc-card-border" : "",
            ].join(" ")}
          >
            <p className="text-cc-heading mb-1.5 text-[0.85rem] font-medium">
              {row.label}
            </p>
            <CellMark kind={row.rightKind} text={row.rightText} tone="right" />
          </li>
        ))}
      </ul>
      <p className="text-cc-nav-label mt-2 font-mono text-[0.62rem] tracking-tight">
        ● strong · ◐ partial · ○ weak
      </p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  HONESTY                                                                    */
/* -------------------------------------------------------------------------- */

function HonestyLeft() {
  return (
    <div className="flex flex-col gap-4">
      <CrossChip>what it does not do</CrossChip>
      <h3 className="font-heading text-h5 text-cc-heading font-semibold tracking-tight">
        It does not freeze the world outside the service.
      </h3>
      <p className="text-cc-ink-dim text-[0.95rem] leading-relaxed">
        Generation only governs what your service emits. Consumers you do not
        control can keep an older version of the schema, and you cannot reach
        across the network to update them.
      </p>
      <p className="text-cc-ink-dim text-[0.95rem] leading-relaxed">
        When a type changes, the schema diff tells you which published clients
        are affected so you can coordinate the rollout, rather than discovering
        it in production.
      </p>
    </div>
  );
}

function HonestyRight() {
  return (
    <div className="flex flex-col gap-4">
      <CheckChip>what it does do</CheckChip>
      <h3
        className="font-heading text-h5 font-semibold tracking-tight"
        style={{ color: ACCENT }}
      >
        It makes the contract derive from the code that runs.
      </h3>
      <p className="text-cc-prose text-[0.95rem] leading-relaxed">
        One annotated class means the schema, resolvers, and DataLoaders cannot
        disagree, because they are derived from the same code. That is a real,
        narrow guarantee, and it is the one we make.
      </p>
      <p className="text-cc-prose text-[0.95rem] leading-relaxed">
        Build-time closure: if the C# compiles, the schema is whole, the
        DataLoaders exist, and the typed Strawberry Shake client matches the
        operations you wrote against it.
      </p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  PAGE                                                                       */
/* -------------------------------------------------------------------------- */

export default function BuildLoopV4Page() {
  return (
    <div className="flex flex-col gap-10 py-6">
      <Hero />

      <LedgerShell
        eyebrow="Source"
        leftLabel="Hand-rolled · schema.graphql + Resolvers.cs"
        rightLabel="Platform · [QueryType] ProductApi.cs"
        left={<SourceLeft />}
        right={<SourceRight />}
      />

      <LedgerShell
        eyebrow="Artifacts"
        leftLabel="Hand-rolled · three files to maintain"
        rightLabel="Platform · one lineage, generated"
        left={<ArtifactsLeft />}
        right={<ArtifactsRight />}
      />

      <LedgerShell
        eyebrow="The loop"
        leftLabel="Hand-rolled · edit, map, regen, hope"
        rightLabel="Platform · annotate, generate, codegen, ship"
        left={<LoopLeft />}
        right={<LoopRight />}
      />

      <LedgerShell
        eyebrow="N+1 fetches"
        leftLabel="Hand-rolled · 5 keys to 5 queries"
        rightLabel="Platform · 5 keys to 1 batched fetch"
        left={<NPlusOneLeft />}
        right={<NPlusOneRight />}
      />

      <LedgerShell
        eyebrow="Scoreboard"
        leftLabel="Hand-rolled"
        rightLabel="Platform"
        left={<ScoreboardLeft />}
        right={<ScoreboardRight />}
      />

      <LedgerShell
        eyebrow="Honesty"
        leftLabel="What it does not do"
        rightLabel="What it does do"
        left={<HonestyLeft />}
        right={<HonestyRight />}
      />

      <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border px-6 py-12 text-center backdrop-blur-sm sm:px-10 sm:py-14">
        <Eyebrow>Pick a side</Eyebrow>
        <h2 className="font-heading text-h2 text-cc-heading mx-auto mt-4 max-w-3xl font-semibold tracking-tight">
          Pick the side that ships the contract.
        </h2>
        <p className="text-cc-prose mx-auto mt-5 max-w-xl text-[1.05rem] leading-relaxed">
          Start writing your GraphQL API as annotated C# and watch the schema,
          DataLoaders, and a typed .NET client appear from the code you already
          wrote.
        </p>
        <div className="mt-7 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs">Read the Docs</OutlineButton>
        </div>
      </section>
    </div>
  );
}
