import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { GreenDonut } from "@/src/icons/GreenDonut";

export const metadata: Metadata = {
  title: "Green Donut: DataLoader for .NET",
  description:
    "Green Donut is the open-source DataLoader for .NET. Coalesce resolver calls into one batched fetch with per-request caching and source-generated wiring.",
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
      "Kill N+1 in your .NET resolvers. Batching, per-request caching, dedup, and the [DataLoader] attribute generate the wiring for you. MIT licensed.",
    type: "website",
  },
};

// Brand spectrum, used at most once on this page (open source MIT band).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
  readonly withGlyph?: boolean;
}

function Eyebrow({ children, withGlyph = true }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption inline-flex items-center gap-2 font-mono font-medium tracking-[0.2em] uppercase">
      {withGlyph ? <CoalescerGlyph /> : null}
      <span>{children}</span>
    </span>
  );
}

// The mini "Coalescer" glyph: three lines converge into one beam. Tied
// visually to the hero diagram. Used on every section eyebrow.
function CoalescerGlyph() {
  return (
    <svg
      width="16"
      height="10"
      viewBox="0 0 16 10"
      fill="none"
      aria-hidden
      className="shrink-0"
    >
      <path
        d="M0.5 1.2 C 5 1.2, 7 5, 11 5"
        stroke="currentColor"
        strokeWidth="0.9"
        strokeOpacity="0.55"
        strokeLinecap="round"
      />
      <path
        d="M0.5 5 C 5 5, 7 5, 11 5"
        stroke="currentColor"
        strokeWidth="0.9"
        strokeOpacity="0.85"
        strokeLinecap="round"
      />
      <path
        d="M0.5 8.8 C 5 8.8, 7 5, 11 5"
        stroke="currentColor"
        strokeWidth="0.9"
        strokeOpacity="0.55"
        strokeLinecap="round"
      />
      <path
        d="M11 5 L 15.5 5"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
    </svg>
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
// The Coalescer: the single giant hero visual. Six labelled keys fan into a
// glowing accent funnel, exit as one labelled batch pill, and land in a
// stylised database cylinder. Chromeless, bleeds onto cc-bg.
// -----------------------------------------------------------------------------

const COALESCER_KEYS = [
  { y: 60, label: "id: 7" },
  { y: 130, label: "id: 12" },
  { y: 200, label: "id: 3" },
  { y: 290, label: "id: 9" },
  { y: 360, label: "id: 21" },
  { y: 430, label: "id: 4" },
];

function CoalescerHeroSvg() {
  const center = { x: 410, y: 245 };
  return (
    <svg
      viewBox="0 0 720 520"
      className="h-auto w-full"
      role="img"
      aria-label="Six keys converge through a coalescer funnel into one batched fetch to the database"
    >
      <defs>
        <linearGradient id="gd-beam" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.18" />
          <stop offset="55%" stopColor="#5eead4" stopOpacity="0.55" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.95" />
        </linearGradient>
        <radialGradient id="gd-lens" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.55" />
          <stop offset="55%" stopColor="#5eead4" stopOpacity="0.18" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
        </radialGradient>
        <linearGradient id="gd-out" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.95" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.55" />
        </linearGradient>
      </defs>

      {/* Faint dotted tick baseline through the funnel. */}
      <line
        x1="20"
        y1={center.y}
        x2="700"
        y2={center.y}
        stroke="#5eead4"
        strokeOpacity="0.18"
        strokeWidth="0.75"
        strokeDasharray="2 6"
      />

      {/* Lens halo. */}
      <circle cx={center.x} cy={center.y} r="78" fill="url(#gd-lens)" />

      {/* Key nodes on the left, each with label and converging beam. */}
      {COALESCER_KEYS.map((k, i) => {
        const strokeWidth = 0.9 + i * 0.18;
        return (
          <g key={k.label}>
            <rect
              x="22"
              y={k.y - 14}
              width="92"
              height="28"
              rx="6"
              fill="#0c1322"
              stroke="#1f2a44"
              strokeWidth="1"
            />
            <circle cx="38" cy={k.y} r="3" fill="#5eead4" fillOpacity="0.9" />
            <text
              x="52"
              y={k.y + 4}
              fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
              fontSize="11.5"
              fill="#cbd5e1"
            >
              {k.label}
            </text>
            <path
              d={`M114,${k.y} C 230,${k.y} 320,${center.y} ${center.x - 30},${center.y}`}
              fill="none"
              stroke="url(#gd-beam)"
              strokeWidth={strokeWidth}
              strokeLinecap="round"
              opacity={0.55 + i * 0.07}
            />
          </g>
        );
      })}

      {/* Funnel: two converging strokes drawing the lens edges. */}
      <path
        d={`M ${center.x - 40} ${center.y - 70} L ${center.x + 10} ${center.y} L ${center.x - 40} ${center.y + 70}`}
        fill="none"
        stroke="#5eead4"
        strokeOpacity="0.75"
        strokeWidth="1.4"
        strokeLinejoin="round"
      />
      <path
        d={`M ${center.x + 10} ${center.y} L ${center.x + 60} ${center.y - 28} M ${center.x + 10} ${center.y} L ${center.x + 60} ${center.y + 28}`}
        fill="none"
        stroke="#5eead4"
        strokeOpacity="0.55"
        strokeWidth="1.1"
      />
      <circle
        cx={center.x + 10}
        cy={center.y}
        r="5"
        fill="#5eead4"
        fillOpacity="0.9"
      />

      {/* Single converged beam exiting right. */}
      <path
        d={`M ${center.x + 10} ${center.y} L 580 ${center.y}`}
        stroke="url(#gd-out)"
        strokeWidth="3"
        strokeLinecap="round"
      />

      {/* Batch pill. */}
      <g>
        <rect
          x="492"
          y={center.y - 18}
          width="118"
          height="36"
          rx="18"
          fill="#5eead4"
          fillOpacity="0.14"
          stroke="#5eead4"
          strokeOpacity="0.8"
          strokeWidth="1.1"
        />
        <text
          x="551"
          y={center.y + 4}
          textAnchor="middle"
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="11.5"
          fill="#5eead4"
          letterSpacing="0.16em"
        >
          BATCH IN (...)
        </text>
      </g>

      {/* Connector to database. */}
      <path
        d={`M 610 ${center.y} L 654 ${center.y}`}
        stroke="#5eead4"
        strokeOpacity="0.7"
        strokeWidth="1.4"
        strokeLinecap="round"
      />

      {/* Database cylinder. */}
      <g transform={`translate(654, ${center.y - 36})`}>
        <ellipse
          cx="28"
          cy="6"
          rx="28"
          ry="6"
          fill="#0c1322"
          stroke="#5eead4"
          strokeOpacity="0.75"
          strokeWidth="1.2"
        />
        <path
          d="M 0 6 L 0 66"
          stroke="#5eead4"
          strokeOpacity="0.75"
          strokeWidth="1.2"
        />
        <path
          d="M 56 6 L 56 66"
          stroke="#5eead4"
          strokeOpacity="0.75"
          strokeWidth="1.2"
        />
        <ellipse
          cx="28"
          cy="66"
          rx="28"
          ry="6"
          fill="#0c1322"
          stroke="#5eead4"
          strokeOpacity="0.75"
          strokeWidth="1.2"
        />
        <ellipse
          cx="28"
          cy="22"
          rx="28"
          ry="6"
          fill="none"
          stroke="#5eead4"
          strokeOpacity="0.35"
          strokeWidth="1"
        />
        <ellipse
          cx="28"
          cy="40"
          rx="28"
          ry="6"
          fill="none"
          stroke="#5eead4"
          strokeOpacity="0.25"
          strokeWidth="1"
        />
      </g>

      {/* Axis labels. */}
      <text
        x="22"
        y="40"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill="#94a3b8"
        letterSpacing="0.2em"
      >
        KEYS
      </text>
      <text
        x={center.x - 22}
        y={center.y - 92}
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill="#94a3b8"
        letterSpacing="0.2em"
      >
        COALESCE
      </text>
      <text
        x="540"
        y={center.y - 32}
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill="#94a3b8"
        letterSpacing="0.2em"
        textAnchor="middle"
      >
        ONE FETCH
      </text>
      <text
        x="682"
        y={center.y + 56}
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        fill="#94a3b8"
        letterSpacing="0.2em"
        textAnchor="middle"
      >
        DB
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Inline C# code snippet styles (GitHub-dark, snippet-scoped).
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

// -----------------------------------------------------------------------------
// Behaviors strip tile
// -----------------------------------------------------------------------------

interface BehaviorTileProps {
  readonly title: string;
  readonly body: string;
  readonly fact: string;
}

function BehaviorTile({ title, body, fact }: BehaviorTileProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-xl border p-6">
      <div className="text-cc-accent">
        <CoalescerGlyph />
      </div>
      <h3 className="text-cc-heading font-heading text-h5 mt-4">{title}</h3>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{body}</p>
      <div className="border-cc-card-border mt-5 border-t pt-3">
        <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.16em] uppercase">
          {fact}
        </span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Loader catalogue row
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
// Annotated code card. Left rail uses IndexTag markers; right rail is the code.
// -----------------------------------------------------------------------------

interface AnnotationRowProps {
  readonly value: string;
  readonly label: string;
  readonly note: string;
}

function AnnotationRow({ value, label, note }: AnnotationRowProps) {
  return (
    <li className="flex items-start gap-3">
      <IndexTag value={value} />
      <div>
        <div className="text-cc-heading font-mono text-[12px]">{label}</div>
        <div className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
          {note}
        </div>
      </div>
    </li>
  );
}

function DataLoaderAnnotatedCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border">
      <div className="grid grid-cols-1 lg:grid-cols-12">
        {/* Annotation rail */}
        <div className="border-cc-card-border lg:col-span-4 lg:border-r">
          <div className="p-6">
            <Eyebrow>The [DataLoader] attribute</Eyebrow>
            <h3 className="text-cc-heading font-heading text-h5 mt-3">
              Write the method. Skip the plumbing.
            </h3>
            <ul className="mt-6 space-y-5">
              <AnnotationRow
                value="01"
                label="[DataLoader]"
                note="One attribute on a plain static method. The source generator emits the interface, the typed accessor, and the DI registration."
              />
              <AnnotationRow
                value="02"
                label="IReadOnlyList<TKey>"
                note="Take the keys that arrived in this tick. Return an IReadOnlyDictionary keyed by them. Missing keys yield null at the call site."
              />
              <AnnotationRow
                value="03"
                label="Generated wiring"
                note="Inject the generated IUserDataLoader from anywhere. AOT friendly, zero reflection, no base class."
              />
            </ul>
          </div>
        </div>

        {/* Code */}
        <div className="lg:col-span-8">
          <div className="bg-cc-code-bg h-full">
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
                <span style={C.comment}>
                  {"// One method. Wiring is generated."}
                </span>
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
                <span style={C.type}>
                  {"Task<IReadOnlyDictionary<int, User>>"}
                </span>{" "}
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
                <span style={C.plain}>{"=>"}</span>{" "}
                <span style={C.kw}>await</span> <span style={C.param}>db</span>
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
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function GreenDonutV5Page() {
  return (
    <main className="mx-auto w-full max-w-7xl px-6 pt-16 pb-24 sm:px-8 lg:px-10">
      {/* HERO: sparse copy left, giant chromeless Coalescer SVG right. */}
      <section className="grid grid-cols-1 items-center gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-5">
          <div className="flex items-center gap-3">
            <GreenDonut className="h-9 w-9" />
            <Eyebrow withGlyph={false}>
              Green Donut / DataLoader for .NET
            </Eyebrow>
          </div>

          <h1 className="font-heading text-cc-heading text-hero mt-6">
            Six round trips become one.
          </h1>

          <p className="lead text-cc-ink mt-5 max-w-lg">
            Green Donut coalesces sibling resolver calls into one batched fetch,
            caches the result inside the request, and skips repeat keys. Source
            generated, AOT friendly, MIT licensed.
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-3">
            <SolidButton href="/docs/greendonut">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>

          <div className="text-cc-ink-dim mt-8 flex flex-wrap items-center gap-x-5 gap-y-2 font-mono text-[11px] tracking-[0.16em] uppercase">
            <span>Batch</span>
            <span className="text-cc-card-border" aria-hidden>
              /
            </span>
            <span>Cache</span>
            <span className="text-cc-card-border" aria-hidden>
              /
            </span>
            <span>Dedup</span>
            <span className="text-cc-card-border" aria-hidden>
              /
            </span>
            <span>DataLoader attribute</span>
          </div>
        </div>

        <div className="lg:col-span-7">
          <CoalescerHeroSvg />
        </div>
      </section>

      {/* BEHAVIORS STRIP */}
      <section className="mt-24">
        <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
          <BehaviorTile
            title="Batch"
            body="Keys that arrive on the same tick are coalesced into one fetch. Resolvers stay shaped per field; the database sees one round trip."
            fact="one tick, one fetch"
          />
          <BehaviorTile
            title="Cache"
            body="Each key is cached for the rest of the request. Concurrent requests never see each other's data. Swap in a global cache when you actually want one."
            fact="per request scope"
          />
          <BehaviorTile
            title="Dedup"
            body="The same key in the same request returns the same task. Repeat lookups never become repeat queries."
            fact="same key, once"
          />
        </div>
      </section>

      {/* PROBLEM PANEL: two side-by-side query lists */}
      <section className="mt-24">
        <div className="max-w-3xl">
          <Eyebrow>The N+1 problem</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2 mt-4">
            One query for the list. N more for every row.
          </h2>
          <p className="text-cc-ink mt-4">
            Resolvers run per field, per node. A list of orders loads in one
            query. Then each order asks for its customer, again and again, for a
            single request. Latency climbs with the page size and the connection
            pool starts to choke. DataLoader fixes this without rewriting your
            resolvers.
          </p>
        </div>

        <div className="mt-10 grid grid-cols-1 gap-5 md:grid-cols-2">
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6">
            <div className="text-cc-danger font-mono text-[11px] tracking-[0.18em] uppercase">
              Without DataLoader
            </div>
            <div className="mt-4 space-y-1.5 font-mono text-[12px]">
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
            <div className="border-cc-card-border mt-5 flex items-center justify-between border-t pt-3">
              <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.16em] uppercase">
                round trips
              </span>
              <span className="text-cc-danger font-mono text-[15px] font-semibold tabular-nums">
                21
              </span>
            </div>
          </div>

          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6">
            <div className="text-cc-accent font-mono text-[11px] tracking-[0.18em] uppercase">
              With Green Donut
            </div>
            <div className="mt-4 space-y-1.5 font-mono text-[12px]">
              <div className="text-cc-ink">SELECT * FROM orders LIMIT 20</div>
              <div className="text-cc-ink">SELECT * FROM customers</div>
              <div className="text-cc-ink">
                {"    "}WHERE id IN (1, 2, ..., 20)
              </div>
              <div className="text-cc-ink-dim">{"    "}</div>
              <div className="text-cc-ink-dim">{"    "}</div>
            </div>
            <div className="border-cc-card-border mt-5 flex items-center justify-between border-t pt-3">
              <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.16em] uppercase">
                round trips
              </span>
              <span className="text-cc-accent font-mono text-[15px] font-semibold tabular-nums">
                2
              </span>
            </div>
          </div>
        </div>
      </section>

      {/* ATTRIBUTE CARD with annotation rail */}
      <section className="mt-24">
        <div className="mx-auto max-w-3xl text-center">
          <Eyebrow>The attribute does the wiring</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2 mt-4">
            Write the method. The generator writes the rest.
          </h2>
          <p className="text-cc-ink mt-4">
            Mark a static method with{" "}
            <span className="text-cc-heading font-mono text-[13px]">
              [DataLoader]
            </span>
            . Take the keys, return a dictionary. The source generator emits the
            loader type, the interface, and the DI registration.
          </p>
        </div>
        <div className="mt-10">
          <DataLoaderAnnotatedCard />
        </div>
      </section>

      {/* LOADER SHAPES TABLE */}
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
            <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-xl border">
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
                  OrderType.cs
                </span>
              </div>
              <div className="py-4">
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
            </div>
          </div>

          <div className="lg:col-span-5">
            <Eyebrow>Inside Hot Chocolate, standalone outside</Eyebrow>
            <h3 className="font-heading text-cc-heading text-h3 mt-4">
              Same loader. Different host.
            </h3>
            <p className="text-cc-ink mt-4">
              Green Donut is the engine Hot Chocolate uses to batch resolver
              work and eliminate N+1, and you do not need a GraphQL server to
              use it. Drop a loader in a background worker, a REST controller,
              or a CLI.
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

      {/* MIT BAND (single spectrum use) */}
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
                fee, no commercial fork.
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
        <div className="flex justify-center">
          <Eyebrow>Next step</Eyebrow>
        </div>
        <h2 className="font-heading text-cc-heading text-h2 mx-auto mt-4 max-w-3xl">
          Stop paying for N+1.
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
