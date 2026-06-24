import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { GreenDonut } from "@/src/icons/GreenDonut";

export const metadata: Metadata = {
  title: "Green Donut, the DataLoader for .NET",
  description:
    "Green Donut is the open source DataLoader for .NET. Kill N+1 with batching, dedup, and per-request caching. Source generated, AOT friendly, MIT licensed.",
  keywords: [
    "Green Donut",
    "DataLoader .NET",
    "N+1 problem",
    "GraphQL batching",
    "request scoped cache",
    "Hot Chocolate DataLoader",
    "EF Core batching",
    "AOT friendly",
    "ChilliCream",
    "MIT licensed",
  ],
  openGraph: {
    title: "Green Donut, the DataLoader for .NET",
    description:
      "Kill N+1 in .NET with batching, deduplication, and per request caching. Source generated DataLoader methods, AOT friendly, drops into Hot Chocolate or any service.",
  },
  robots: { index: false, follow: false },
};

// Brand spectrum used exactly once on the page, as the headline accent.
const SPECTRUM_GRADIENT =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

export default function GreenDonutStoryPage() {
  return (
    <>
      <Hero />
      <BeforeAfter />
      <UnderTheHood />
      <WhatYouGet />
      <MitBand />
      <ClosingCta />
    </>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                       */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="relative py-16 text-center sm:py-24">
      <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
        DataLoader for .NET, open source
      </div>

      <h1 className="text-cc-heading font-heading mx-auto max-w-4xl text-5xl leading-[1.05] font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        From one query per row to{" "}
        <span
          className="bg-clip-text text-transparent"
          style={{ backgroundImage: SPECTRUM_GRADIENT }}
        >
          one batched fetch
        </span>{" "}
        per tick.
      </h1>

      <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-base sm:text-lg">
        Green Donut is the DataLoader implementation that powers Hot Chocolate
        and works standalone in any .NET service. Collect keys this tick,
        deduplicate, fetch once, cache per request. The N+1 problem disappears
        without manual plumbing.
      </p>

      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/greendonut">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>

      <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 font-mono text-xs tracking-wide">
        <span className="inline-flex items-center gap-1.5">
          <span
            aria-hidden
            className="bg-cc-accent inline-block size-1.5 rounded-full"
          />
          MIT licensed
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span
            aria-hidden
            className="bg-cc-accent inline-block size-1.5 rounded-full"
          />
          AOT friendly
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span
            aria-hidden
            className="bg-cc-accent inline-block size-1.5 rounded-full"
          />
          Standalone or with Hot Chocolate
        </span>
      </div>

      <div
        className="pointer-events-none absolute top-8 right-8 hidden opacity-20 lg:block"
        aria-hidden
      >
        <GreenDonut className="h-40 w-auto" />
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Before / After                                                             */
/* -------------------------------------------------------------------------- */

function BeforeAfter() {
  return (
    <section className="py-16">
      <div className="mb-12 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Before and after
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          One small attribute, one fewer outage.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          The classic N+1 looks innocent in code review and lethal in
          production. Below, the same resolver written naively and then with a
          generated DataLoader. Same shape, very different round trip count.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Panel
          tone="warning"
          eyebrow="Before"
          title="Naive resolver, one round trip per row"
          summary="For a list of 50 books, this resolver issues 1 query for the list and 50 more, one per book, to fetch each author. That is 51 round trips for a single request."
          metric="1 + N round trips"
          metricLabel="51 queries for 50 books"
        >
          <CodeFrame language="C# / before.cs" tone="warning">
            <CsLine>
              <Attr>[QueryType]</Attr>
            </CsLine>
            <CsLine>
              <Kw>public partial class</Kw> <Type>Query</Type>
            </CsLine>
            <CsLine>{`{`}</CsLine>
            <CsLine indent={1}>
              <Kw>public</Kw> <Type>{`IQueryable<Book>`}</Type> GetBooks(
              <Type>BookDb</Type> db) {`=> db.Books;`}
            </CsLine>
            <CsLine>{`}`}</CsLine>
            <CsLine />
            <CsLine>
              <Attr>[ObjectType&lt;Book&gt;]</Attr>
            </CsLine>
            <CsLine>
              <Kw>public static partial class</Kw> <Type>BookNode</Type>
            </CsLine>
            <CsLine>{`{`}</CsLine>
            <CsLine indent={1}>
              <Cmt>{`// One database call per Book.Author, every time.`}</Cmt>
            </CsLine>
            <CsLine indent={1}>
              <Kw>public static async</Kw> <Type>{`Task<Author>`}</Type>{" "}
              GetAuthorAsync(
            </CsLine>
            <CsLine indent={2}>
              [<Type>Parent</Type>] <Type>Book</Type> book,
            </CsLine>
            <CsLine indent={2}>
              <Type>BookDb</Type> db, <Type>CancellationToken</Type> ct)
            </CsLine>
            <CsLine indent={1}>
              {`  => await db.Authors.FirstAsync(a => a.Id == book.AuthorId, ct);`}
            </CsLine>
            <CsLine>{`}`}</CsLine>
          </CodeFrame>
        </Panel>

        <Panel
          tone="success"
          eyebrow="After"
          title="A DataLoader, one batched fetch"
          summary="Mark a static method with [DataLoader]. The source generator emits the loader plus the DI wiring. Hot Chocolate batches keys collected in the same tick into a single call, deduplicates them, and caches the result per request."
          metric="2 round trips"
          metricLabel="1 list query, 1 batched author fetch"
        >
          <CodeFrame language="C# / after.cs" tone="success">
            <CsLine>
              <Kw>internal static class</Kw> <Type>AuthorLoaders</Type>
            </CsLine>
            <CsLine>{`{`}</CsLine>
            <CsLine indent={1}>
              <Attr>[DataLoader]</Attr>
            </CsLine>
            <CsLine indent={1}>
              <Kw>public static async</Kw>{" "}
              <Type>{`Task<IReadOnlyDictionary<int, Author>>`}</Type>
            </CsLine>
            <CsLine indent={2}>
              GetAuthorsByIdAsync(<Type>{`IReadOnlyList<int>`}</Type> ids,
            </CsLine>
            <CsLine indent={2}>
              <Type>BookDb</Type> db, <Type>CancellationToken</Type> ct)
            </CsLine>
            <CsLine indent={2}>{`=> await db.Authors`}</CsLine>
            <CsLine indent={3}>{`.Where(a => ids.Contains(a.Id))`}</CsLine>
            <CsLine indent={3}>{`.ToDictionaryAsync(a => a.Id, ct);`}</CsLine>
            <CsLine>{`}`}</CsLine>
            <CsLine />
            <CsLine>
              <Attr>[ObjectType&lt;Book&gt;]</Attr>
            </CsLine>
            <CsLine>
              <Kw>public static partial class</Kw> <Type>BookNode</Type>
            </CsLine>
            <CsLine>{`{`}</CsLine>
            <CsLine indent={1}>
              <Kw>public static</Kw> <Type>{`Task<Author>`}</Type>{" "}
              GetAuthorAsync(
            </CsLine>
            <CsLine indent={2}>
              [<Type>Parent</Type>] <Type>Book</Type> book,
            </CsLine>
            <CsLine indent={2}>
              <Type>IAuthorByIdDataLoader</Type> loader,{" "}
              <Type>CancellationToken</Type> ct)
            </CsLine>
            <CsLine indent={1}>
              {`  => loader.LoadAsync(book.AuthorId, ct);`}
            </CsLine>
            <CsLine>{`}`}</CsLine>
          </CodeFrame>
        </Panel>
      </div>

      <p className="text-cc-ink-dim mx-auto mt-8 max-w-3xl text-center text-sm">
        The resolver still asks for one author at a time. Green Donut quietly
        merges every key requested in the same execution tick into one batched
        call, returns each caller their own result, and remembers what it saw
        for the rest of the request.
      </p>
    </section>
  );
}

interface PanelProps {
  readonly tone: "warning" | "success";
  readonly eyebrow: string;
  readonly title: string;
  readonly summary: string;
  readonly metric: string;
  readonly metricLabel: string;
  readonly children: ReactNode;
}

function Panel({
  tone,
  eyebrow,
  title,
  summary,
  metric,
  metricLabel,
  children,
}: PanelProps) {
  const accent = tone === "warning" ? "text-cc-warning" : "text-cc-success";
  const dot = tone === "warning" ? "bg-cc-warning" : "bg-cc-success";
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col overflow-hidden rounded-2xl border backdrop-blur-sm transition-colors">
      <header className="border-cc-card-border border-b px-6 py-5">
        <div
          className={`mb-2 flex items-center gap-2 font-mono text-xs font-semibold tracking-widest uppercase ${accent}`}
        >
          <span
            aria-hidden
            className={`inline-block size-1.5 rounded-full ${dot}`}
          />
          {eyebrow}
        </div>
        <h3 className="text-cc-heading text-xl font-semibold tracking-tight">
          {title}
        </h3>
        <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
          {summary}
        </p>
      </header>
      <div className="px-6 pt-6">{children}</div>
      <footer className="border-cc-card-border mt-auto flex items-baseline justify-between gap-3 border-t px-6 py-4">
        <div className={`font-mono text-2xl font-semibold ${accent}`}>
          {metric}
        </div>
        <div className="text-cc-ink-dim text-right font-mono text-xs tracking-wide">
          {metricLabel}
        </div>
      </footer>
    </article>
  );
}

/* -------------------------------------------------------------------------- */
/*  Under the hood, 3 steps                                                    */
/* -------------------------------------------------------------------------- */

interface HoodStep {
  readonly index: string;
  readonly title: string;
  readonly body: string;
}

const HOOD_STEPS: readonly HoodStep[] = [
  {
    index: "01",
    title: "Collect keys this tick",
    body: "Every resolver that asks for an author hands its id to the DataLoader. Instead of firing immediately, Green Donut queues the keys for the current execution tick.",
  },
  {
    index: "02",
    title: "Deduplicate",
    body: "Two resolvers asking for author id 42 should not cost two queries. Duplicate keys collapse to one, and a per request promise cache returns the same value to every caller.",
  },
  {
    index: "03",
    title: "One batched fetch",
    body: "When the tick drains, your fetch method runs once with the full key list. The result is split back to each waiting caller and cached for the rest of the request.",
  },
];

function UnderTheHood() {
  return (
    <section className="py-16">
      <div className="mb-12 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          How it works
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Three moves, every request.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          A DataLoader is not magic. It is three small ideas, applied
          consistently to every resolver that takes a key.
        </p>
      </div>

      <ol className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {HOOD_STEPS.map((step) => (
          <li
            key={step.index}
            className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative overflow-hidden rounded-xl border p-6 backdrop-blur-sm transition-colors"
          >
            <div className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
              Step {step.index}
            </div>
            <h3 className="text-cc-heading mt-2 text-lg font-semibold tracking-tight">
              {step.title}
            </h3>
            <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">
              {step.body}
            </p>
            <FlowArt step={step.index} />
          </li>
        ))}
      </ol>
    </section>
  );
}

interface FlowArtProps {
  readonly step: string;
}

function FlowArt({ step }: FlowArtProps) {
  if (step === "01") {
    return (
      <svg viewBox="0 0 200 60" className="mt-5 w-full" aria-hidden>
        {[12, 32, 52, 72, 92, 112, 132, 152, 172].map((x) => (
          <circle key={x} cx={x} cy={30} r={4} fill="#5eead4" opacity={0.85} />
        ))}
        <line
          x1={4}
          x2={196}
          y1={50}
          y2={50}
          stroke="currentColor"
          className="text-cc-card-border"
          strokeWidth={1}
        />
        <text
          x={100}
          y={14}
          textAnchor="middle"
          className="fill-cc-ink-dim font-mono"
          fontSize={9}
        >
          tick
        </text>
      </svg>
    );
  }
  if (step === "02") {
    return (
      <svg viewBox="0 0 200 60" className="mt-5 w-full" aria-hidden>
        {[
          { x: 20, dup: true },
          { x: 50, dup: false },
          { x: 80, dup: true },
          { x: 110, dup: false },
          { x: 140, dup: true },
          { x: 170, dup: false },
        ].map((pt, i) => (
          <g key={i}>
            <circle
              cx={pt.x}
              cy={20}
              r={4}
              fill={pt.dup ? "#5eead4" : "#5eead4"}
              opacity={pt.dup ? 0.3 : 0.95}
            />
            {pt.dup ? (
              <line
                x1={pt.x - 4}
                y1={16}
                x2={pt.x + 4}
                y2={24}
                stroke="#f0786a"
                strokeWidth={1.4}
              />
            ) : (
              <line
                x1={pt.x}
                y1={26}
                x2={pt.x}
                y2={46}
                stroke="currentColor"
                className="text-cc-card-border"
                strokeWidth={1}
                strokeDasharray="2 2"
              />
            )}
          </g>
        ))}
        {[50, 110, 170].map((x) => (
          <circle key={x} cx={x} cy={50} r={4} fill="#5eead4" />
        ))}
      </svg>
    );
  }
  return (
    <svg viewBox="0 0 200 60" className="mt-5 w-full" aria-hidden>
      <rect
        x={10}
        y={14}
        width={70}
        height={32}
        rx={6}
        fill="none"
        stroke="#5eead4"
        strokeWidth={1.4}
      />
      {[26, 44, 62].map((x) => (
        <circle key={x} cx={x} cy={30} r={3} fill="#5eead4" />
      ))}
      <path
        d="M 86 30 L 124 30"
        stroke="currentColor"
        className="text-cc-card-border"
        strokeWidth={1.4}
        markerEnd="url(#gd-arrow)"
      />
      <defs>
        <marker
          id="gd-arrow"
          viewBox="0 0 10 10"
          refX="8"
          refY="5"
          markerWidth="6"
          markerHeight="6"
          orient="auto"
        >
          <path d="M 0 0 L 10 5 L 0 10 z" fill="#5eead4" />
        </marker>
      </defs>
      <rect
        x={128}
        y={14}
        width={62}
        height={32}
        rx={6}
        fill="#5eead4"
        opacity={0.18}
        stroke="#5eead4"
        strokeWidth={1.4}
      />
      <text
        x={159}
        y={35}
        textAnchor="middle"
        className="fill-cc-heading font-mono"
        fontSize={10}
      >
        fetch
      </text>
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/*  What you get, 4 cards                                                      */
/* -------------------------------------------------------------------------- */

interface Capability {
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

const CAPABILITIES: readonly Capability[] = [
  {
    title: "Generated wiring",
    body: "Mark a static fetch method with [DataLoader] and the source generator emits the loader interface, the implementation, and the DI registration. You write the query, nothing else.",
    bullets: ["[DataLoader] attribute", "AOT friendly, no runtime reflection"],
  },
  {
    title: "Batch, group, keyed, paged",
    body: "Pick the loader shape that fits the fetch. One value per key, many values per key, a fetched once result, or a paged slice with cursors and selection pushdown.",
    bullets: [
      "Batch, grouped, fetch once, branched",
      "Paged loads with GreenDonut.Data",
    ],
  },
  {
    title: "Per request scoped cache",
    body: "Identity stays consistent within a request and resets cleanly between requests. No accidental cross tenant or cross user leakage from a long lived cache.",
    bullets: [
      "Promise cache, scoped to the request",
      "Pluggable cache backend",
    ],
  },
  {
    title: "Standalone or with Hot Chocolate",
    body: "Hot Chocolate auto discovers your loaders and threads them through resolvers. Outside of GraphQL, inject the same loader into any .NET service that needs batched fetches.",
    bullets: ["Zero wiring inside Hot Chocolate", "Works in any .NET service"],
  },
];

function WhatYouGet() {
  return (
    <section className="py-16">
      <div className="mb-12 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          What you get
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          A DataLoader you do not have to write.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          Green Donut handles batching, deduplication, caching, and the wiring.
          You stay in your domain code.
        </p>
      </div>

      <div className="grid gap-5 sm:grid-cols-2">
        {CAPABILITIES.map((capability) => (
          <article
            key={capability.title}
            className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-xl border p-6 backdrop-blur-sm transition-colors"
          >
            <h3 className="text-cc-heading text-lg font-semibold">
              {capability.title}
            </h3>
            <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
              {capability.body}
            </p>
            <ul className="mt-4 space-y-1.5">
              {capability.bullets.map((bullet) => (
                <li
                  key={bullet}
                  className="text-cc-ink flex items-start gap-2 text-sm"
                >
                  <span className="text-cc-accent mt-1 shrink-0">
                    <CheckIcon />
                  </span>
                  <span>{bullet}</span>
                </li>
              ))}
            </ul>
          </article>
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  MIT / open source band                                                     */
/* -------------------------------------------------------------------------- */

function MitBand() {
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 text-center backdrop-blur-sm sm:p-12">
        <div className="text-cc-accent mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          MIT licensed, open source
        </div>
        <h2 className="text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
          Free for any project, commercial or otherwise.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          Green Donut is developed in the open under the MIT license as part of
          the ChilliCream graphql platform. Read the source, file issues, send
          patches, or fork it for your own platform.
        </p>
        <div className="mt-7 flex flex-wrap justify-center gap-4">
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform/blob/main/LICENSE">
            Read the license
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA                                                                */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="py-20 text-center">
      <h2 className="text-cc-heading mx-auto max-w-3xl text-3xl font-semibold tracking-tight sm:text-4xl">
        Stop paying for N+1.{" "}
        <span className="text-cc-accent">Start with one attribute.</span>
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base sm:text-lg">
        Add Green Donut, mark your fetch method with [DataLoader], and let the
        generator do the rest.
      </p>

      <div className="mx-auto mt-8 max-w-xl">
        <CodeFrame language="terminal">
          <TermLine prompt>dotnet add package GreenDonut</TermLine>
          <TermLine prompt>
            dotnet add package GreenDonut.Data.EntityFramework
          </TermLine>
          <TermLine muted>
            {`# Hot Chocolate auto discovers [DataLoader] methods.`}
          </TermLine>
        </CodeFrame>
      </div>

      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/greendonut">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Code framing helpers                                                       */
/* -------------------------------------------------------------------------- */

interface CodeFrameProps {
  readonly language: string;
  readonly children: ReactNode;
  readonly tone?: "warning" | "success" | "neutral";
}

function CodeFrame({ language, children, tone = "neutral" }: CodeFrameProps) {
  let accentDot = "bg-cc-accent";
  if (tone === "warning") {
    accentDot = "bg-cc-warning";
  } else if (tone === "success") {
    accentDot = "bg-cc-success";
  }
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <div className="border-cc-card-border bg-cc-surface/60 text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px] tracking-wide">
        <div className="flex items-center gap-2">
          <span
            aria-hidden
            className={`inline-block size-2 rounded-full opacity-70 ${accentDot}`}
          />
          <span
            aria-hidden
            className="bg-cc-card-border inline-block size-2 rounded-full opacity-70"
          />
          <span
            aria-hidden
            className="bg-cc-card-border inline-block size-2 rounded-full opacity-70"
          />
        </div>
        <span className="uppercase">{language}</span>
      </div>
      <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[13px] leading-[1.65]">
        <code>{children}</code>
      </pre>
    </div>
  );
}

interface CsLineProps {
  readonly children?: ReactNode;
  readonly indent?: number;
}

function CsLine({ children, indent = 0 }: CsLineProps) {
  const pad = "  ".repeat(indent);
  return (
    <div>
      {pad}
      {children}
      {"\n"}
    </div>
  );
}

function Kw({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-accent">{children}</span>;
}

function Type({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-warning">{children}</span>;
}

function Attr({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-cta">{children}</span>;
}

function Cmt({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-ink-dim italic">{children}</span>;
}

interface TermLineProps {
  readonly children: ReactNode;
  readonly prompt?: boolean;
  readonly muted?: boolean;
}

function TermLine({ children, prompt, muted }: TermLineProps) {
  const cls = muted ? "text-cc-ink-dim" : "text-cc-ink";
  return (
    <div className={cls}>
      {prompt ? <span className="text-cc-accent">$ </span> : null}
      {children}
      {"\n"}
    </div>
  );
}
