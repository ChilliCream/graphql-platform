"use client";

import { useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";

/**
 * Sticky code-and-prose split for the Build loop page. A single tall C# code
 * surface is pinned on the left; the right column scrolls four artifact steps
 * past it, and the step in view highlights its line range in the code with a
 * `cc-accent` active band. Implementation-first walkthrough, docs-style.
 */

interface CodeLine {
  readonly text: string;
  readonly kind?: "comment" | "attr";
}

// One contrived-but-real ProductApi: a [QueryType] partial, a resolver method,
// and a [DataLoader] batch method. Line numbers below drive the step ranges.
const CODE_LINES: readonly CodeLine[] = [
  { text: "[QueryType]", kind: "attr" }, // 1
  { text: "public static partial class ProductApi" }, // 2
  { text: "{" }, // 3
  { text: "    public static async Task<Product?> GetProductById(" }, // 4
  { text: "        int id," }, // 5
  { text: "        IProductByIdDataLoader productById)" }, // 6
  { text: "        => await productById.LoadAsync(id);" }, // 7
  { text: "}" }, // 8
  { text: "" }, // 9
  { text: "public static class ProductDataLoaders" }, // 10
  { text: "{" }, // 11
  { text: "    [DataLoader]", kind: "attr" }, // 12
  { text: "    public static async Task<IReadOnlyDictionary<int, Product>>" }, // 13
  { text: "        GetProductsByIdAsync(" }, // 14
  { text: "            IReadOnlyList<int> ids," }, // 15
  { text: "            ProductDbContext db," }, // 16
  { text: "            CancellationToken ct)" }, // 17
  { text: "        => await db.Products" }, // 18
  { text: "            .Where(p => ids.Contains(p.Id))" }, // 19
  { text: "            .ToDictionaryAsync(p => p.Id, ct);" }, // 20
  { text: "}" }, // 21
];

interface Step {
  readonly key: string;
  readonly label: string;
  readonly heading: string;
  readonly body: ReactNode;
  /** 1-based inclusive line range that lights up while this step is in view. */
  readonly range: readonly [number, number];
  /** The `source -> generated` lineage shown beside the step. */
  readonly lineage: readonly [string, string];
}

const STEPS: readonly Step[] = [
  {
    key: "schema",
    label: "01 / Schema",
    heading: "The implementation is the schema.",
    body: (
      <>
        Annotate a partial class with <Mono>[QueryType]</Mono> and a Roslyn
        source generator emits the schema from it at build time. The C# you run
        is the single source of truth, so there is no separate schema file to
        keep in sync by hand.
      </>
    ),
    range: [1, 3],
    lineage: ["[QueryType] ProductApi", "GraphQL schema"],
  },
  {
    key: "resolvers",
    label: "02 / Resolvers",
    heading: "Resolvers are plain methods.",
    body: (
      <>
        Arguments are parameters and services inject from DI, so the generated
        resolver pipeline wraps idiomatic C# instead of a DSL you have to learn
        and re-sync. <Mono>GetProductById</Mono> becomes the field resolver,
        wiring included.
      </>
    ),
    range: [4, 7],
    lineage: ["GetProductById(...)", "resolver pipeline"],
  },
  {
    key: "dataloaders",
    label: "03 / DataLoaders",
    heading: "N+1 solved by default.",
    body: (
      <>
        A <Mono>[DataLoader]</Mono> method is source-generated into a Green
        Donut DataLoader that batches and deduplicates the keys for you. The
        fan-out is fast by default, not a hand-wired add-on you remember to bolt
        on later.
      </>
    ),
    range: [12, 21],
    lineage: ["[DataLoader] method", "batched DataLoader"],
  },
  {
    key: "client",
    label: "04 / Typed client",
    heading: "Typed .NET clients from the same contract.",
    body: (
      <>
        Strawberry Shake generates strongly-typed .NET clients from your{" "}
        <Mono>.graphql</Mono> operations via MSBuild code generation. A contract
        change surfaces as build feedback before the app ships, so consumers
        move with the graph instead of after it.
      </>
    ),
    range: [0, 0],
    lineage: [".graphql operations", "typed .NET client"],
  },
];

/** Inline monospace token in prose, in the calm accent voice. */
function Mono({ children }: { readonly children: ReactNode }) {
  return (
    <code className="text-cc-accent font-mono text-[0.85em]">{children}</code>
  );
}

function Arrow() {
  return (
    <span aria-hidden="true" className="text-cc-ink-faint px-1 text-sm">
      &rarr;
    </span>
  );
}

/** The `source -> generated` lineage chip pair shown under each step. */
function Lineage({ from, to }: { readonly from: string; readonly to: string }) {
  return (
    <div className="mt-5 flex flex-wrap items-center gap-1">
      <span className="border-cc-card-border text-cc-ink bg-cc-surface rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap">
        {from}
      </span>
      <Arrow />
      <span className="border-cc-accent/60 text-cc-accent bg-cc-surface rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap">
        {to}
      </span>
    </div>
  );
}

/** The pinned code surface, rendered with cc-code-bg / cc-code-header chrome. */
function CodeRail({
  activeRange,
}: {
  readonly activeRange: readonly [number, number];
}) {
  const [from, to] = activeRange;

  return (
    <figure className="ring-cc-card-border bg-cc-code-bg overflow-hidden rounded-lg shadow-md ring-1">
      <figcaption className="border-cc-card-border bg-cc-code-header flex items-center gap-3 border-b px-4 py-2 text-xs">
        <span
          className="rounded px-2 py-0.5 font-mono font-semibold tracking-wider uppercase"
          style={{ color: "#9179f7", backgroundColor: "#9179f71f" }}
        >
          C#
        </span>
        <span className="text-cc-ink-dim font-mono">ProductApi.cs</span>
      </figcaption>
      <div className="overflow-x-auto py-3 font-mono text-[0.78rem] leading-6">
        {CODE_LINES.map((line, i) => {
          const lineNo = i + 1;
          const active = lineNo >= from && lineNo <= to;
          return (
            <div
              key={lineNo}
              className={[
                "flex border-l-2 pr-4 pl-3 transition-colors duration-300",
                active
                  ? "border-cc-accent bg-cc-accent/10"
                  : "border-transparent",
              ].join(" ")}
            >
              <span className="text-cc-nav-label w-7 shrink-0 text-right select-none">
                {lineNo}
              </span>
              <code
                className={[
                  "ml-4 whitespace-pre transition-colors duration-300",
                  active
                    ? "text-cc-heading"
                    : line.kind === "comment"
                      ? "text-cc-ink-dim"
                      : line.kind === "attr"
                        ? "text-cc-accent/80"
                        : "text-cc-ink",
                ].join(" ")}
              >
                {line.text === "" ? " " : line.text}
              </code>
            </div>
          );
        })}
      </div>
    </figure>
  );
}

export function BuildLoopWalkthrough() {
  const [activeIndex, setActiveIndex] = useState(0);
  const stepRefs = useRef<(HTMLElement | null)[]>([]);

  useEffect(() => {
    const nodes = stepRefs.current.filter((n): n is HTMLElement => n !== null);
    if (nodes.length === 0 || typeof IntersectionObserver === "undefined") {
      return;
    }

    const ratios = new Map<Element, number>();
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          ratios.set(entry.target, entry.intersectionRatio);
        }
        let best: Element | null = null;
        let bestRatio = 0;
        for (const [el, ratio] of ratios) {
          if (ratio > bestRatio) {
            bestRatio = ratio;
            best = el;
          }
        }
        if (best !== null) {
          const idx = nodes.indexOf(best as HTMLElement);
          if (idx !== -1) {
            setActiveIndex(idx);
          }
        }
      },
      { rootMargin: "-30% 0px -45% 0px", threshold: [0.1, 0.5, 1] },
    );

    for (const node of nodes) {
      observer.observe(node);
    }
    return () => observer.disconnect();
  }, []);

  const activeRange = STEPS[activeIndex].range;

  return (
    <div className="grid grid-cols-1 gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)] lg:gap-12">
      {/* LEFT: sticky code rail. */}
      <div className="lg:sticky lg:top-24 lg:self-start">
        <CodeRail activeRange={activeRange} />
        <p className="text-cc-nav-label mt-3 font-mono text-[0.62rem] tracking-[0.12em] uppercase">
          one artifact, one contract
        </p>
      </div>

      {/* RIGHT: scrolling artifact steps. */}
      <ol className="flex flex-col gap-6 sm:gap-8">
        {STEPS.map((step, i) => {
          const active = i === activeIndex;
          return (
            <li
              key={step.key}
              ref={(node) => {
                stepRefs.current[i] = node;
              }}
              className={[
                "rounded-2xl border p-6 backdrop-blur-sm transition-colors duration-300 sm:p-7",
                active
                  ? "border-cc-accent/50 bg-cc-card-bg"
                  : "border-cc-card-border bg-cc-card-bg/60",
              ].join(" ")}
            >
              <span className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.18em] uppercase">
                {step.label}
              </span>
              <h3 className="font-heading text-cc-heading text-h4 mt-3 leading-tight font-semibold text-balance">
                {step.heading}
              </h3>
              <p className="text-cc-ink mt-4 text-base/relaxed text-pretty">
                {step.body}
              </p>
              <Lineage from={step.lineage[0]} to={step.lineage[1]} />
            </li>
          );
        })}
      </ol>
    </div>
  );
}
