import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { GreenDonut } from "@/src/icons/GreenDonut";

export const metadata: Metadata = {
  title: "Green Donut: DataLoader for .NET",
  description:
    "Green Donut is the open-source DataLoader for .NET. Kill N+1 in resolvers with batching, per-request caching, and source-generated wiring. MIT licensed.",
  keywords: [
    "Green Donut",
    "DataLoader",
    ".NET DataLoader",
    "N+1 problem",
    "GraphQL batching",
    "request scoped cache",
    "Hot Chocolate",
    "C# resolvers",
    "AOT-friendly",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Green Donut: DataLoader for .NET",
    description:
      "Kill N+1 in your .NET resolvers. Batching, per-request caching, dedup, and the [DataLoader] attribute generate the wiring for you. MIT-licensed.",
    type: "website",
  },
};

// Brand spectrum, used at most once on this page (closing CTA rule).
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
// Hero: before/after diagram. Six key requests collapse into one batched fetch.
// Pure inline SVG, sized with viewBox so it scales with the column.
// -----------------------------------------------------------------------------

const HERO_KEYS = ["id: 7", "id: 12", "id: 3", "id: 9", "id: 21", "id: 4"];

function BeforeAfterDiagram() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-6 shadow-2xl">
      <div className="flex items-center justify-between">
        <Eyebrow>Before / After</Eyebrow>
        <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
          one tick
        </span>
      </div>

      <div className="mt-5 grid grid-cols-1 gap-5 md:grid-cols-2">
        {/* Before column */}
        <div>
          <div className="text-cc-ink-dim mb-3 font-mono text-[11px] tracking-[0.18em] uppercase">
            Before
          </div>
          <div className="border-cc-card-border bg-cc-bg/60 rounded-lg border p-4">
            <ul className="space-y-2">
              {HERO_KEYS.map((k) => (
                <li
                  key={k}
                  className="flex items-center justify-between font-mono text-[12px]"
                >
                  <span className="text-cc-ink">GetUser({k})</span>
                  <span className="text-cc-danger/90 font-mono text-[10.5px] tracking-[0.16em] uppercase">
                    SELECT
                  </span>
                </li>
              ))}
            </ul>
            <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
              <span className="text-cc-ink-dim font-mono text-[11px]">
                round trips
              </span>
              <span className="text-cc-danger font-mono text-[13px] font-semibold tabular-nums">
                6
              </span>
            </div>
          </div>
        </div>

        {/* After column */}
        <div>
          <div className="text-cc-ink-dim mb-3 font-mono text-[11px] tracking-[0.18em] uppercase">
            After
          </div>
          <div className="border-cc-card-border bg-cc-bg/60 rounded-lg border p-4">
            <div className="mb-3 font-mono text-[12px]">
              <span className="text-cc-ink">LoadAsync(</span>
              <span className="text-cc-accent">[7, 12, 3, 9, 21, 4]</span>
              <span className="text-cc-ink">)</span>
            </div>
            <svg
              viewBox="0 0 220 90"
              className="w-full"
              role="img"
              aria-label="Six keys collapse into one batched fetch"
            >
              <defs>
                <linearGradient id="gd-batch-line" x1="0" y1="0" x2="1" y2="0">
                  <stop offset="0%" stopColor="#5eead4" stopOpacity="0.25" />
                  <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
                </linearGradient>
              </defs>
              {[10, 24, 38, 52, 66, 80].map((y, i) => (
                <g key={y}>
                  <circle
                    cx="14"
                    cy={y}
                    r="3"
                    fill="#5eead4"
                    fillOpacity="0.85"
                  />
                  <path
                    d={`M18,${y} C 90,${y} 130,45 188,45`}
                    fill="none"
                    stroke="url(#gd-batch-line)"
                    strokeWidth="1.25"
                    strokeLinecap="round"
                    opacity={0.55 + i * 0.06}
                  />
                </g>
              ))}
              <rect
                x="184"
                y="36"
                width="30"
                height="18"
                rx="4"
                fill="#5eead4"
                fillOpacity="0.18"
                stroke="#5eead4"
                strokeOpacity="0.65"
              />
              <text
                x="199"
                y="48"
                textAnchor="middle"
                fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
                fontSize="9"
                fill="#5eead4"
              >
                IN (...)
              </text>
            </svg>
            <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
              <span className="text-cc-ink-dim font-mono text-[11px]">
                round trips
              </span>
              <span className="text-cc-accent font-mono text-[13px] font-semibold tabular-nums">
                1
              </span>
            </div>
          </div>
        </div>
      </div>

      <div className="border-cc-card-border mt-5 grid grid-cols-3 divide-x divide-[var(--color-cc-card-border)] overflow-hidden rounded-lg border text-center">
        <div className="px-3 py-3">
          <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
            Batch
          </div>
          <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
            one fetch
          </div>
        </div>
        <div className="px-3 py-3">
          <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
            Cache
          </div>
          <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
            per request
          </div>
        </div>
        <div className="px-3 py-3">
          <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
            Dedup
          </div>
          <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
            same key, once
          </div>
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Inline C# code snippet for the [DataLoader] attribute. Tokens are scoped to
// this snippet only, mimicking GitHub-dark, so the rest of the page stays on
// cc-* tokens.
// -----------------------------------------------------------------------------

const C = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  comment: { color: "#8b949e", fontStyle: "italic" as const },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  plain: { color: "#c9d1d9" },
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

function DataLoaderCodeCard() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-3">
        <span
          className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span
          className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span
          className="bg-cc-success/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span className="ml-3 font-mono text-[11px] text-[#8b949e]">
          UserDataLoader.cs
        </span>
      </div>

      <div className="py-4">
        <CodeLine n={1}>
          <span style={C.comment}>{"// One method. Wiring is generated."}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.kw}>public static class</span>{" "}
          <span style={C.type}>UserDataLoader</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{"{"}</span>
        </CodeLine>
        <CodeLine n={4}>
          {"    "}
          <span style={C.attr}>[DataLoader]</span>
        </CodeLine>
        <CodeLine n={5}>
          {"    "}
          <span style={C.kw}>public static async</span>{" "}
          <span style={C.type}>{"Task<IReadOnlyDictionary<int, User>>"}</span>{" "}
          <span style={C.fn}>GetUsersAsync</span>
          <span style={C.plain}>(</span>
        </CodeLine>
        <CodeLine n={6}>
          {"        "}
          <span style={C.type}>{"IReadOnlyList<int>"}</span>{" "}
          <span style={C.param}>ids</span>
          <span style={C.plain}>,</span>
        </CodeLine>
        <CodeLine n={7}>
          {"        "}
          <span style={C.type}>AppDbContext</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.plain}>,</span>
        </CodeLine>
        <CodeLine n={8}>
          {"        "}
          <span style={C.type}>CancellationToken</span>{" "}
          <span style={C.param}>ct</span>
          <span style={C.plain}>{")"}</span>
        </CodeLine>
        <CodeLine n={9}>
          {"        "}
          <span style={C.plain}>{"=>"}</span> <span style={C.kw}>await</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.plain}>.Users</span>
        </CodeLine>
        <CodeLine n={10}>
          {"            "}
          <span style={C.plain}>.</span>
          <span style={C.fn}>Where</span>
          <span style={C.plain}>(u {"=>"} </span>
          <span style={C.param}>ids</span>
          <span style={C.plain}>.</span>
          <span style={C.fn}>Contains</span>
          <span style={C.plain}>(u.Id))</span>
        </CodeLine>
        <CodeLine n={11}>
          {"            "}
          <span style={C.plain}>.</span>
          <span style={C.fn}>ToDictionaryAsync</span>
          <span style={C.plain}>(u {"=>"} u.Id, </span>
          <span style={C.param}>ct</span>
          <span style={C.plain}>);</span>
        </CodeLine>
        <CodeLine n={12}>
          <span style={C.plain}>{"}"}</span>
        </CodeLine>
        <CodeLine n={13}> </CodeLine>
        <CodeLine n={14}>
          <span style={C.comment}>
            {"// Inject IUserDataLoader anywhere. Six keys arrive,"}
          </span>
        </CodeLine>
        <CodeLine n={15}>
          <span style={C.comment}>
            {"// one batched call goes out. Same key in the same"}
          </span>
        </CodeLine>
        <CodeLine n={16}>
          <span style={C.comment}>
            {"// request is served from the cache."}
          </span>
        </CodeLine>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Pattern card: a labelled tile used in the pillar grid.
// -----------------------------------------------------------------------------

interface PillarCardProps {
  readonly index: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

function PillarCard({ index, title, body, bullets }: PillarCardProps) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover group relative flex h-full flex-col rounded-xl border p-6 transition-colors">
      <div className="flex items-center justify-between">
        <IndexTag value={index} />
        <span
          className="bg-cc-accent/70 h-1.5 w-1.5 rounded-full"
          aria-hidden
        />
      </div>
      <h3 className="text-cc-heading font-heading text-h5 mt-4">{title}</h3>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{body}</p>
      <ul className="mt-4 space-y-2">
        {bullets.map((b) => (
          <li
            key={b}
            className="text-cc-ink-dim flex items-start gap-2 text-sm"
          >
            <span className="text-cc-accent mt-[3px]">
              <CheckIcon />
            </span>
            <span>{b}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

// -----------------------------------------------------------------------------
// Loader catalogue row: short table of the loader flavors.
// -----------------------------------------------------------------------------

interface LoaderRowProps {
  readonly name: string;
  readonly shape: string;
  readonly use: string;
}

function LoaderRow({ name, shape, use }: LoaderRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-12 gap-3 border-b px-5 py-4 last:border-b-0">
      <div className="col-span-12 sm:col-span-3">
        <span className="text-cc-heading font-mono text-[12.5px] font-semibold">
          {name}
        </span>
      </div>
      <div className="col-span-12 sm:col-span-4">
        <span className="text-cc-ink font-mono text-[12px]">{shape}</span>
      </div>
      <div className="col-span-12 sm:col-span-5">
        <span className="text-cc-ink-dim text-sm">{use}</span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function GreenDonutV1Page() {
  return (
    <main className="mx-auto w-full max-w-7xl px-6 pt-16 pb-24 sm:px-8 lg:px-10">
      {/* HERO */}
      <section className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-6">
          <div className="flex items-center gap-3">
            <GreenDonut className="h-9 w-9" />
            <Eyebrow>Green Donut / DataLoader for .NET</Eyebrow>
          </div>

          <h1 className="font-heading text-cc-heading text-h1 mt-6">
            Kill N+1 in your .NET resolvers.
          </h1>

          <p className="lead text-cc-ink mt-5 max-w-xl">
            Green Donut is the DataLoader for .NET. It collapses many key
            requests from the same tick into one batched fetch, caches each
            result inside the request, and deduplicates repeat keys. Source
            generated, AOT friendly, MIT licensed.
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-3">
            <SolidButton href="/docs/greendonut">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>

          <ul className="text-cc-ink-dim mt-8 grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
              Batching, caching, dedup
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
              [DataLoader] attribute
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
              Per request scoped cache
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
              Auto discovered by Hot Chocolate
            </li>
          </ul>
        </div>

        <div className="lg:col-span-6">
          <BeforeAfterDiagram />
        </div>
      </section>

      {/* PROBLEM */}
      <section className="mt-24">
        <div className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>The N+1 problem</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h2 mt-4">
              One query for the list. N more for every row.
            </h2>
            <p className="text-cc-ink mt-4">
              Resolvers run per field, per node. A list of orders loads in one
              query. Then each order asks for its customer, its line items, its
              shipping address. The database does the same point lookup, again
              and again, for a single request. Latency climbs linearly with the
              page size and the connection pool starts to choke.
            </p>
            <p className="text-cc-ink-dim mt-4">
              DataLoader fixes this without rewriting your resolvers. Each
              resolver still asks for the keys it needs. Green Donut collects
              the keys that arrive on the same tick, sends a single batched
              fetch, and hands each resolver its result.
            </p>
          </div>

          <div className="lg:col-span-7">
            <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="border-cc-card-border bg-cc-bg/50 rounded-lg border p-4">
                  <div className="text-cc-danger font-mono text-[11px] tracking-[0.18em] uppercase">
                    Without DataLoader
                  </div>
                  <div className="mt-3 space-y-1.5 font-mono text-[12px]">
                    <div className="text-cc-ink">
                      SELECT * FROM orders LIMIT 20
                    </div>
                    <div className="text-cc-ink-dim">
                      SELECT * FROM customers WHERE id = 1
                    </div>
                    <div className="text-cc-ink-dim">
                      SELECT * FROM customers WHERE id = 2
                    </div>
                    <div className="text-cc-ink-dim">
                      SELECT * FROM customers WHERE id = 3
                    </div>
                    <div className="text-cc-ink-dim">. . . 17 more</div>
                  </div>
                  <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
                    <span className="text-cc-ink-dim font-mono text-[11px]">
                      queries
                    </span>
                    <span className="text-cc-danger font-mono text-[13px] font-semibold tabular-nums">
                      21
                    </span>
                  </div>
                </div>

                <div className="border-cc-card-border bg-cc-bg/50 rounded-lg border p-4">
                  <div className="text-cc-accent font-mono text-[11px] tracking-[0.18em] uppercase">
                    With Green Donut
                  </div>
                  <div className="mt-3 space-y-1.5 font-mono text-[12px]">
                    <div className="text-cc-ink">
                      SELECT * FROM orders LIMIT 20
                    </div>
                    <div className="text-cc-ink">SELECT * FROM customers</div>
                    <div className="text-cc-ink">
                      {"    "}WHERE id IN (1, 2, ..., 20)
                    </div>
                    <div className="text-cc-ink-dim">{"    "}</div>
                    <div className="text-cc-ink-dim">{"    "}</div>
                  </div>
                  <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
                    <span className="text-cc-ink-dim font-mono text-[11px]">
                      queries
                    </span>
                    <span className="text-cc-accent font-mono text-[13px] font-semibold tabular-nums">
                      2
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* THE CATALOGUE */}
      <section className="mt-24">
        <div className="flex flex-col items-start gap-4">
          <Eyebrow>The N+1 killer catalogue</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2 max-w-3xl">
            Six pillars. One library. Every shape of batched load you need.
          </h2>
          <p className="text-cc-ink max-w-3xl">
            Green Donut is the DataLoader implementation that powers Hot
            Chocolate, and it runs standalone in any .NET service. Each pillar
            below maps to a concrete capability, not a marketing promise.
          </p>
        </div>

        <div className="mt-10 grid grid-cols-1 gap-5 md:grid-cols-2 lg:grid-cols-3">
          <PillarCard
            index="01"
            title="The DataLoader pattern"
            body="Resolvers ask for keys. Green Donut collects the keys that arrive on the same tick, sends one batched fetch, and hands each resolver its result."
            bullets={[
              "Coalesces sibling resolver calls",
              "Returns results in the original key order",
              "Works with any async data source",
            ]}
          />
          <PillarCard
            index="02"
            title="Batching, caching, dedup"
            body="Three behaviors, one loader. Batch many keys into one fetch. Cache each key for the rest of the request. Skip repeat keys entirely."
            bullets={[
              "Configurable max batch size",
              "Same key in one request returns the same task",
              "Negative caching is opt in",
            ]}
          />
          <PillarCard
            index="03"
            title="[DataLoader] attribute"
            body="Author a loader as a plain static method. The source generator writes the interface, the registration, and the typed accessor for you."
            bullets={[
              "No base class, no ceremony",
              "Generated wiring at build time",
              "AOT friendly, zero reflection",
            ]}
          />
          <PillarCard
            index="04"
            title="Per request, pluggable cache"
            body="The default cache scope is the request, so concurrent requests never see each other's data. Swap in a global or shared cache when you want it."
            bullets={[
              "Request scoped by default",
              "Pluggable cache abstraction",
              "Hand keyed entries in or out",
            ]}
          />
          <PillarCard
            index="05"
            title="Keyed, grouped, pagination"
            body="Pick the loader shape that matches the data. One result per key, many results per key, or a paged window per key. Same attribute, different signature."
            bullets={[
              "Batch loaders for single results",
              "Grouped loaders for one to many",
              "Pagination loaders for cursor windows",
            ]}
          />
          <PillarCard
            index="06"
            title="Hot Chocolate or standalone"
            body="Drop the loader in a Hot Chocolate server and it gets auto discovered. Use it in a Worker, an MVC controller, or a console app with the same API."
            bullets={[
              "Auto discovered by Hot Chocolate",
              "Works in any .NET service",
              "MIT licensed, no per request cost",
            ]}
          />
        </div>
      </section>

      {/* CODE + GENERATED WIRING */}
      <section className="mt-24">
        <div className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>The [DataLoader] attribute</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h2 mt-4">
              Write the method. Skip the plumbing.
            </h2>
            <p className="text-cc-ink mt-4">
              Mark a static method with{" "}
              <span className="text-cc-heading font-mono text-[13px]">
                [DataLoader]
              </span>
              . Take an{" "}
              <span className="text-cc-heading font-mono text-[13px]">
                IReadOnlyList&lt;TKey&gt;
              </span>{" "}
              and return an{" "}
              <span className="text-cc-heading font-mono text-[13px]">
                IReadOnlyDictionary&lt;TKey, TValue&gt;
              </span>
              . The source generator emits the loader type, the interface, and
              the DI registration. Inject the generated interface and call{" "}
              <span className="text-cc-heading font-mono text-[13px]">
                LoadAsync
              </span>{" "}
              from your resolver.
            </p>

            <ul className="mt-6 space-y-3">
              <li className="text-cc-ink-dim flex items-start gap-3 text-sm">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <span>
                  Dependencies after the keys are resolved from DI per batch.
                </span>
              </li>
              <li className="text-cc-ink-dim flex items-start gap-3 text-sm">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <span>
                  Returns are looked up by key. Missing keys yield null.
                </span>
              </li>
              <li className="text-cc-ink-dim flex items-start gap-3 text-sm">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <span>
                  Cancellation flows through to the database driver, not just
                  the loader.
                </span>
              </li>
            </ul>
          </div>

          <div className="lg:col-span-7">
            <DataLoaderCodeCard />
          </div>
        </div>
      </section>

      {/* LOADER CATALOGUE */}
      <section className="mt-24">
        <div className="flex flex-col items-start gap-3">
          <Eyebrow>Loader shapes</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2">
            One attribute, three signatures.
          </h2>
          <p className="text-cc-ink max-w-3xl">
            The signature picks the loader. Same attribute, same generated
            wiring, the right shape for the relationship you are loading.
          </p>
        </div>

        <div className="border-cc-card-border bg-cc-card-bg mt-8 overflow-hidden rounded-xl border">
          <div className="border-cc-card-border bg-cc-surface/60 text-cc-ink-dim grid grid-cols-12 gap-3 border-b px-5 py-3 font-mono text-[11px] tracking-[0.18em] uppercase">
            <div className="col-span-12 sm:col-span-3">Loader</div>
            <div className="col-span-12 sm:col-span-4">Return shape</div>
            <div className="col-span-12 sm:col-span-5">
              When to reach for it
            </div>
          </div>
          <LoaderRow
            name="Keyed"
            shape="IReadOnlyDictionary<TKey, TValue>"
            use="One result per key. The default for foreign key lookups."
          />
          <LoaderRow
            name="Grouped"
            shape="ILookup<TKey, TValue>"
            use="Many results per key. One to many relationships, like an order to its line items."
          />
          <LoaderRow
            name="Pagination"
            shape="Page<TKey, TValue>"
            use="A cursor window per key. Connections on a parent type that need paging per node."
          />
        </div>
      </section>

      {/* HOT CHOCOLATE INTEGRATION */}
      <section className="mt-24">
        <div className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-7">
            <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6">
              <div className="flex items-center justify-between">
                <Eyebrow>In a Hot Chocolate resolver</Eyebrow>
                <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
                  auto discovered
                </span>
              </div>

              <div className="bg-cc-code-bg border-cc-card-border mt-4 overflow-hidden rounded-lg border py-4">
                <CodeLine n={1}>
                  <span style={C.kw}>public class</span>{" "}
                  <span style={C.type}>OrderType</span>{" "}
                  <span style={C.plain}>:</span>{" "}
                  <span style={C.type}>{"ObjectType<Order>"}</span>
                </CodeLine>
                <CodeLine n={2}>
                  <span style={C.plain}>{"{"}</span>
                </CodeLine>
                <CodeLine n={3}>
                  {"    "}
                  <span style={C.kw}>public static async</span>{" "}
                  <span style={C.type}>{"Task<User?>"}</span>{" "}
                  <span style={C.fn}>GetCustomerAsync</span>
                  <span style={C.plain}>(</span>
                </CodeLine>
                <CodeLine n={4}>
                  {"        "}
                  <span style={C.attr}>[Parent]</span>{" "}
                  <span style={C.type}>Order</span>{" "}
                  <span style={C.param}>order</span>
                  <span style={C.plain}>,</span>
                </CodeLine>
                <CodeLine n={5}>
                  {"        "}
                  <span style={C.type}>IUserDataLoader</span>{" "}
                  <span style={C.param}>users</span>
                  <span style={C.plain}>,</span>
                </CodeLine>
                <CodeLine n={6}>
                  {"        "}
                  <span style={C.type}>CancellationToken</span>{" "}
                  <span style={C.param}>ct</span>
                  <span style={C.plain}>{")"}</span>
                </CodeLine>
                <CodeLine n={7}>
                  {"        "}
                  <span style={C.plain}>{"=>"}</span>{" "}
                  <span style={C.kw}>await</span>{" "}
                  <span style={C.param}>users</span>
                  <span style={C.plain}>.</span>
                  <span style={C.fn}>LoadAsync</span>
                  <span style={C.plain}>(</span>
                  <span style={C.param}>order</span>
                  <span style={C.plain}>.CustomerId, </span>
                  <span style={C.param}>ct</span>
                  <span style={C.plain}>);</span>
                </CodeLine>
                <CodeLine n={8}>
                  <span style={C.plain}>{"}"}</span>
                </CodeLine>
              </div>

              <p className="text-cc-ink-dim mt-4 text-sm">
                Hot Chocolate discovers loaders marked with{" "}
                <span className="text-cc-heading font-mono text-[12.5px]">
                  [DataLoader]
                </span>{" "}
                during startup and registers them in DI. Resolvers just inject
                the generated interface.
              </p>
            </div>
          </div>

          <div className="lg:col-span-5">
            <Eyebrow>Inside Hot Chocolate, standalone outside</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h2 mt-4">
              Same loader. Different host.
            </h2>
            <p className="text-cc-ink mt-4">
              Green Donut is the engine Hot Chocolate uses to batch resolver
              work and eliminate N+1, and you do not need a GraphQL server to
              use it. Drop a loader in a background worker, a REST controller,
              or a CLI. Same{" "}
              <span className="text-cc-heading font-mono text-[12.5px]">
                LoadAsync
              </span>{" "}
              entry point, same batching, same per request scope.
            </p>

            <ul className="text-cc-ink-dim mt-6 space-y-3 text-sm">
              <li className="flex items-start gap-3">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <span>
                  Auto discovered by Hot Chocolate, opt in everywhere else.
                </span>
              </li>
              <li className="flex items-start gap-3">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <span>
                  Scope a batch to the HTTP request, the message, or any custom
                  unit of work.
                </span>
              </li>
              <li className="flex items-start gap-3">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <span>
                  Pluggable cache means you can share results across batches
                  when that is what you actually want.
                </span>
              </li>
            </ul>
          </div>
        </div>
      </section>

      {/* MIT BAND */}
      <section className="mt-24">
        <div className="rounded-xl p-[1px]" style={{ background: SPECTRUM }}>
          <div className="bg-cc-surface flex flex-col items-start justify-between gap-6 rounded-[11px] px-8 py-8 sm:flex-row sm:items-center">
            <div>
              <Eyebrow>Open source</Eyebrow>
              <div className="text-cc-heading font-heading text-h4 mt-2">
                MIT licensed. Maintained in the open.
              </div>
              <p className="text-cc-ink-dim mt-2 max-w-2xl text-sm">
                Green Donut ships in the same repository as Hot Chocolate,
                Fusion, Strawberry Shake, and Cookie Crumble. No per request
                fee, no per seat fee, no commercial fork.
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
              <SolidButton href="/docs/greendonut">Read the docs</SolidButton>
            </div>
          </div>
        </div>
      </section>

      {/* CLOSING CTA */}
      <section className="mt-24 text-center">
        <Eyebrow>Stop paying for N+1</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mx-auto mt-4 max-w-3xl">
          Six resolver round trips. One batched fetch. Same code path.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl">
          Add Green Donut to your service, mark a method with{" "}
          <span className="text-cc-heading font-mono text-[13px]">
            [DataLoader]
          </span>
          , and inject the generated interface. The N+1 disappears on the next
          request.
        </p>
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/docs/greendonut">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>
      </section>
    </main>
  );
}
