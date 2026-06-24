import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { GreenDonut } from "@/src/icons/GreenDonut";

export const metadata: Metadata = {
  title: "Green Donut: DataLoader for .NET",
  description:
    "Green Donut is the DataLoader for .NET. Kill N+1 in resolvers with batching, per-request caching, deduplication, and the [DataLoader] attribute. MIT.",
  robots: { index: false, follow: false },
  openGraph: {
    title: "Green Donut: DataLoader for .NET",
    description:
      "Kill N+1 in your .NET resolvers. Batching, per-request caching, dedup, and the [DataLoader] attribute generate the wiring for you. MIT-licensed.",
    type: "website",
  },
};

// Brand spectrum, used at most once on this page (closing CTA accent line).
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

// -----------------------------------------------------------------------------
// Step row: the structural primitive of this page. A 4rem numeral gutter on the
// left, content column on the right. The numeral sits ON the page's vertical
// hairline rule (rendered once at the page level) and is crossed by an accent
// tick dot at its baseline.
// -----------------------------------------------------------------------------

interface StepRowProps {
  readonly numeral: string;
  readonly children: ReactNode;
}

function StepRow({ numeral, children }: StepRowProps) {
  return (
    <section className="relative mt-32 grid grid-cols-[4rem_1fr] gap-6 first:mt-0 sm:gap-8">
      <div className="relative flex justify-center">
        <span
          className="text-cc-accent block font-mono text-[88px] leading-none tracking-tight tabular-nums"
          aria-hidden
        >
          {numeral}
        </span>
        <span
          className="bg-cc-accent absolute top-[78px] left-1/2 h-1.5 w-1.5 -translate-x-1/2 rounded-full"
          aria-hidden
        />
      </div>
      <div className="min-w-0 pt-3">{children}</div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Before/after SQL pane used in step 01.
// -----------------------------------------------------------------------------

function SqlBeforeAfterCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="border-cc-card-border bg-cc-bg/50 rounded-lg border p-4">
          <div className="text-cc-danger font-mono text-[11px] tracking-[0.18em] uppercase">
            Without DataLoader
          </div>
          <div className="mt-3 space-y-1.5 font-mono text-[12px]">
            <div className="text-cc-ink">SELECT * FROM orders LIMIT 20</div>
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
            <div className="text-cc-ink">SELECT * FROM orders LIMIT 20</div>
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
  );
}

// -----------------------------------------------------------------------------
// Six-keys-to-one batching diagram and the batch/cache/dedup tri-tile, used in
// step 02.
// -----------------------------------------------------------------------------

const HERO_KEYS = ["id: 7", "id: 12", "id: 3", "id: 9", "id: 21", "id: 4"];

function BatchDiagramCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6">
      <div className="flex items-center justify-between">
        <Eyebrow>Before / After</Eyebrow>
        <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
          one tick
        </span>
      </div>

      <div className="mt-5 grid grid-cols-1 gap-5 md:grid-cols-2">
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
                <linearGradient
                  id="gd-v4-batch-line"
                  x1="0"
                  y1="0"
                  x2="1"
                  y2="0"
                >
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
                    stroke="url(#gd-v4-batch-line)"
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
    </div>
  );
}

interface TriTileProps {
  readonly label: string;
  readonly value: string;
}

function TriTile({ label, value }: TriTileProps) {
  return (
    <div className="px-3 py-3">
      <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
        {label}
      </div>
      <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
        {value}
      </div>
    </div>
  );
}

function BatchCacheDedupStrip() {
  return (
    <div className="border-cc-card-border mt-5 grid grid-cols-3 divide-x divide-[var(--color-cc-card-border)] overflow-hidden rounded-lg border text-center">
      <TriTile label="Batch" value="one fetch" />
      <TriTile label="Cache" value="per request" />
      <TriTile label="Dedup" value="same key, once" />
    </div>
  );
}

// -----------------------------------------------------------------------------
// C# syntax tokens and code-line primitives, reused by steps 03 and 06.
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

interface CodeCardChromeProps {
  readonly filename: string;
  readonly children: ReactNode;
}

function CodeCardChrome({ filename, children }: CodeCardChromeProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-xl border shadow-2xl">
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
          {filename}
        </span>
      </div>
      <div className="py-4">{children}</div>
    </div>
  );
}

function DataLoaderCodeCard() {
  return (
    <CodeCardChrome filename="UserDataLoader.cs">
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
        <span style={C.type}>AppDbContext</span> <span style={C.param}>db</span>
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
    </CodeCardChrome>
  );
}

function OrderTypeCodeCard() {
  return (
    <CodeCardChrome filename="OrderType.cs">
      <CodeLine n={1}>
        <span style={C.kw}>public class</span>{" "}
        <span style={C.type}>OrderType</span> <span style={C.plain}>:</span>{" "}
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
        <span style={C.attr}>[Parent]</span> <span style={C.type}>Order</span>{" "}
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
        <span style={C.plain}>{"=>"}</span> <span style={C.kw}>await</span>{" "}
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
    </CodeCardChrome>
  );
}

// -----------------------------------------------------------------------------
// Loader shapes table for step 04. Flat rows, no card chrome; the page rule
// continues through it.
// -----------------------------------------------------------------------------

interface LoaderShapeRowProps {
  readonly name: string;
  readonly shape: string;
  readonly use: string;
}

function LoaderShapeRow({ name, shape, use }: LoaderShapeRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-12 gap-3 border-b py-4 last:border-b-0">
      <div className="col-span-12 sm:col-span-3">
        <span className="text-cc-heading font-mono text-[12.5px] font-semibold">
          {name}
        </span>
      </div>
      <div className="col-span-12 sm:col-span-5">
        <span className="text-cc-ink font-mono text-[12px]">{shape}</span>
      </div>
      <div className="col-span-12 sm:col-span-4">
        <span className="text-cc-ink-dim text-sm">{use}</span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Small check-bullet primitive reused across the prose steps.
// -----------------------------------------------------------------------------

interface CheckBulletProps {
  readonly children: ReactNode;
}

function CheckBullet({ children }: CheckBulletProps) {
  return (
    <li className="text-cc-ink-dim flex items-start gap-3 text-sm">
      <span className="text-cc-accent mt-[3px]">
        <CheckIcon />
      </span>
      <span>{children}</span>
    </li>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function GreenDonutV4Page() {
  return (
    <main className="relative mx-auto w-full max-w-3xl px-6 pt-16 pb-24 sm:px-8">
      {/* The continuous left-aligned hairline rule. Sits inside the 4rem
          numeral gutter, centered on it. Anchors every step. */}
      <div
        className="bg-cc-card-border pointer-events-none absolute top-16 bottom-24 left-[calc(1.5rem+2rem)] w-px sm:left-[calc(2rem+2rem)]"
        aria-hidden
      />

      {/* STEP 00 / HERO */}
      <StepRow numeral="00">
        <div className="flex items-center gap-3">
          <GreenDonut className="h-9 w-9" />
          <Eyebrow>DataLoader for .NET</Eyebrow>
        </div>

        <h1 className="font-heading text-cc-heading text-h1 mt-6">
          Kill N+1 in your .NET resolvers.
        </h1>

        <p className="lead text-cc-ink mt-5">
          Green Donut is the DataLoader for .NET. It collapses many key requests
          from the same tick into one batched fetch, caches each result inside
          the request, and deduplicates repeat keys. Source generated, AOT
          friendly, MIT licensed.
        </p>

        <div className="mt-8 flex flex-wrap items-center gap-3">
          <SolidButton href="/docs/greendonut">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>

        <ul className="text-cc-ink-dim mt-8 grid grid-cols-1 gap-x-6 gap-y-2 text-sm sm:grid-cols-2">
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
      </StepRow>

      {/* STEP 01 / THE N+1 PROBLEM */}
      <StepRow numeral="01">
        <Eyebrow>The N+1 problem</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mt-3">
          One query for the list. N more for every row.
        </h2>
        <p className="text-cc-ink mt-4">
          Resolvers run per field, per node. A list of orders loads in one
          query. Then each order asks for its customer, its line items, its
          shipping address. The database does the same point lookup, again and
          again, for a single request. Latency climbs linearly with the page
          size and the connection pool starts to choke.
        </p>
        <p className="text-cc-ink-dim mt-4">
          DataLoader fixes this without rewriting your resolvers. Each resolver
          still asks for the keys it needs. Green Donut collects the keys that
          arrive on the same tick, sends a single batched fetch, and hands each
          resolver its result.
        </p>

        <div className="mt-6">
          <SqlBeforeAfterCard />
        </div>
      </StepRow>

      {/* STEP 02 / THE FIX IN ONE TICK */}
      <StepRow numeral="02">
        <Eyebrow>The fix in one tick</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mt-3">
          Six round trips collapse into one batched fetch.
        </h2>
        <p className="text-cc-ink mt-4">
          On the same event-loop tick, Green Donut collects every key any
          resolver asks for, sends one IN(...) query, and routes each result
          back to the resolver that asked for it. Same code path, same call
          site, one trip instead of six.
        </p>

        <div className="mt-6">
          <BatchDiagramCard />
          <BatchCacheDedupStrip />
        </div>
      </StepRow>

      {/* STEP 03 / THE [DataLoader] ATTRIBUTE */}
      <StepRow numeral="03">
        <Eyebrow>The [DataLoader] attribute</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mt-3">
          Write the method. Skip the plumbing.
        </h2>
        <p className="text-cc-ink mt-4">
          Mark a static method with{" "}
          <span className="text-cc-heading font-mono text-[12.5px]">
            [DataLoader]
          </span>
          . Take an{" "}
          <span className="text-cc-heading font-mono text-[12.5px]">
            IReadOnlyList&lt;TKey&gt;
          </span>{" "}
          and return an{" "}
          <span className="text-cc-heading font-mono text-[12.5px]">
            IReadOnlyDictionary&lt;TKey, TValue&gt;
          </span>
          . The source generator emits the loader type, the interface, and the
          DI registration. Inject the generated interface and call{" "}
          <span className="text-cc-heading font-mono text-[12.5px]">
            LoadAsync
          </span>{" "}
          from your resolver.
        </p>

        <div className="mt-6">
          <DataLoaderCodeCard />
        </div>
      </StepRow>

      {/* STEP 04 / LOADER SHAPES */}
      <StepRow numeral="04">
        <Eyebrow>Loader shapes</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mt-3">
          One attribute, three signatures.
        </h2>
        <p className="text-cc-ink mt-4">
          The signature picks the loader. Same attribute, same generated wiring,
          the right shape for the relationship you are loading.
        </p>

        <div className="mt-6">
          <div className="border-cc-card-border text-cc-ink-dim grid grid-cols-12 gap-3 border-b py-3 font-mono text-[11px] tracking-[0.18em] uppercase">
            <div className="col-span-12 sm:col-span-3">Loader</div>
            <div className="col-span-12 sm:col-span-5">Return shape</div>
            <div className="col-span-12 sm:col-span-4">
              When to reach for it
            </div>
          </div>
          <LoaderShapeRow
            name="Keyed"
            shape="IReadOnlyDictionary<TKey, TValue>"
            use="One result per key. The default for foreign key lookups."
          />
          <LoaderShapeRow
            name="Grouped"
            shape="ILookup<TKey, TValue>"
            use="Many results per key. One to many relationships."
          />
          <LoaderShapeRow
            name="Pagination"
            shape="Page<TKey, TValue>"
            use="A cursor window per key. Paged connections per node."
          />
        </div>
      </StepRow>

      {/* STEP 05 / PER-REQUEST SCOPE */}
      <StepRow numeral="05">
        <Eyebrow>Per request scope</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mt-3">
          Each request gets its own cache.
        </h2>
        <p className="text-cc-ink mt-4">
          The default cache scope is the request, so concurrent requests never
          see each other&apos;s data. The cache itself is pluggable, so you can
          swap in a shared store when you actually want results to live longer
          than a single request.
        </p>
        <p className="text-cc-ink-dim mt-4">
          Negative caching, where a missing key is remembered as a miss, is opt
          in. Keep it off and a later resolver in the same request can retry the
          lookup. Turn it on and the loader returns the cached miss immediately.
        </p>

        <ul className="mt-6 space-y-3">
          <CheckBullet>
            Request scoped by default, with isolation between concurrent
            requests.
          </CheckBullet>
          <CheckBullet>
            Pluggable cache abstraction, swap in a global or shared store when
            you want it.
          </CheckBullet>
          <CheckBullet>
            Negative caching is opt in, so missing keys do not silently stay
            missing.
          </CheckBullet>
        </ul>
      </StepRow>

      {/* STEP 06 / HOT CHOCOLATE OR STANDALONE */}
      <StepRow numeral="06">
        <Eyebrow>Hot Chocolate or standalone</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mt-3">
          Same loader. Different host.
        </h2>
        <p className="text-cc-ink mt-4">
          Green Donut is the engine Hot Chocolate uses to batch resolver work
          and eliminate N+1, and you do not need a GraphQL server to use it.
          Drop a loader in a background worker, a REST controller, or a CLI.
          Same{" "}
          <span className="text-cc-heading font-mono text-[12.5px]">
            LoadAsync
          </span>{" "}
          entry point, same batching, same per request scope.
        </p>

        <div className="mt-6">
          <OrderTypeCodeCard />
        </div>

        <ul className="mt-6 space-y-3">
          <CheckBullet>
            Auto discovered by Hot Chocolate, opt in everywhere else.
          </CheckBullet>
          <CheckBullet>
            Scope a batch to the HTTP request, the message, or any custom unit
            of work.
          </CheckBullet>
          <CheckBullet>
            Pluggable cache means you can share results across batches when that
            is what you actually want.
          </CheckBullet>
        </ul>
      </StepRow>

      {/* STEP 07 / OPEN SOURCE CLOSE */}
      <StepRow numeral="07">
        <Eyebrow>Open source</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mt-3">
          MIT licensed. Maintained in the open.
        </h2>
        <p className="text-cc-ink mt-4">
          Green Donut ships in the same repository as Hot Chocolate, Fusion,
          Strawberry Shake, and Cookie Crumble. No per request fee, no per seat
          fee, no commercial fork. One library, four siblings, one license.
        </p>
      </StepRow>

      {/* CLOSING CTA, breaks the rule, centers full-width. */}
      <section className="relative mt-32 text-center">
        <div
          className="mx-auto mb-10 h-px w-40"
          style={{ background: SPECTRUM }}
          aria-hidden
        />
        <Eyebrow>Stop paying for N+1</Eyebrow>
        <h2 className="font-heading text-cc-heading text-h2 mx-auto mt-4 max-w-3xl">
          Six resolver round trips. One batched fetch. Same code path.
        </h2>
        <p className="lead text-cc-ink mx-auto mt-4 max-w-2xl">
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
