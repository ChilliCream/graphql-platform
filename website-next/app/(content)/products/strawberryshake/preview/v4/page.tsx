import type { Metadata } from "next";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Strawberry Shake: typed GraphQL client for .NET reference",
  description:
    "Reference for Strawberry Shake, the open-source typed GraphQL client for .NET. MSBuild codegen, normalized reactive store, subscriptions, Blazor and Razor.",
  keywords: [
    "Strawberry Shake",
    ".NET GraphQL client",
    "strongly-typed GraphQL",
    "MSBuild codegen",
    "Blazor GraphQL",
    "Razor GraphQL",
    "MAUI GraphQL",
    "GraphQL subscriptions",
    "persisted operations",
    "reactive store",
    "dotnet graphql",
    "Hot Chocolate",
  ],
  openGraph: {
    title: "Strawberry Shake: typed GraphQL client for .NET reference",
    description:
      "Reference for Strawberry Shake, the open-source typed GraphQL client for .NET. MSBuild codegen, normalized reactive store, subscriptions, Blazor and Razor.",
    type: "website",
  },
  robots: { index: false, follow: false },
};

// Single brand-spectrum hairline, allowed once on the page (above the closing CTA).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Section index. The sidebar TOC and the article sigils share this list, so
// the reference and the body stay in lockstep.
// -----------------------------------------------------------------------------

interface SectionEntry {
  readonly sigil: string;
  readonly id: string;
  readonly label: string;
}

const SECTIONS: readonly SectionEntry[] = [
  { sigil: "S00", id: "hero", label: "Reference" },
  { sigil: "S01", id: "overview", label: "Overview" },
  { sigil: "S02", id: "strongly-typed", label: "Strongly-typed Client" },
  { sigil: "S03", id: "reactive-store", label: "Reactive Store" },
  { sigil: "S04", id: "fetch-strategies", label: "Fetch Strategies" },
  { sigil: "S05", id: "subscriptions", label: "Subscriptions & Razor" },
  { sigil: "S06", id: "msbuild", label: "MSBuild Codegen" },
  { sigil: "S07", id: "nitro", label: "Nitro Companion" },
  { sigil: "S08", id: "open-source", label: "Open Source" },
];

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface SigilProps {
  readonly value: string;
}

function Sigil({ value }: SigilProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex h-5 items-center rounded-sm border px-1.5 font-mono text-[10px] tracking-[0.18em] uppercase tabular-nums">
      {value}
    </span>
  );
}

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
// Token color helpers for inline code panels (GitHub-dark approximations).
// -----------------------------------------------------------------------------

const T: Record<string, CSSProperties> = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  num: { color: "#79c0ff" },
  comment: { color: "#8b949e", fontStyle: "italic" },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
  dim: { color: "#8b949e" },
  gqlKw: { color: "#ff7b72" },
  gqlType: { color: "#ffa657" },
  gqlField: { color: "#7ee787" },
  gqlVar: { color: "#79c0ff" },
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
// Sidebar TOC. Sticky on lg+, horizontal scroll-chip rail on smaller screens.
// Each item gets a 2px vertical hairline; the hero entry carries the active
// cc-accent rail (declarative, server-only, no scroll-spy), the rest are dim.
// -----------------------------------------------------------------------------

function SidebarToc() {
  return (
    <>
      {/* Mobile: horizontal chip rail */}
      <nav
        aria-label="On this page"
        className="border-cc-card-border -mx-4 mb-4 overflow-x-auto border-b lg:hidden"
      >
        <ul className="flex min-w-max items-center gap-2 px-4 pb-4">
          {SECTIONS.map((s) => (
            <li key={s.id}>
              <a
                href={`#${s.id}`}
                className="border-cc-card-border text-cc-ink-dim hover:border-cc-card-border-hover hover:text-cc-ink inline-flex items-center gap-2 rounded-full border px-3 py-1.5 font-mono text-[10.5px] tracking-[0.18em] uppercase"
              >
                <span className="tabular-nums">{s.sigil}</span>
                <span className="tracking-tight normal-case">{s.label}</span>
              </a>
            </li>
          ))}
        </ul>
      </nav>

      {/* Desktop: sticky left TOC */}
      <aside
        aria-label="On this page"
        className="hidden lg:sticky lg:top-24 lg:block lg:max-h-[calc(100vh-6rem)] lg:overflow-auto"
      >
        <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.22em] uppercase">
          On this page
        </div>
        <ul className="mt-4 flex flex-col">
          {SECTIONS.map((s, i) => {
            const isActive = i === 0;
            return (
              <li key={s.id} className="relative">
                <a
                  href={`#${s.id}`}
                  className="group flex items-start gap-3 py-2 pl-4"
                >
                  <span
                    aria-hidden
                    className={[
                      "absolute top-1.5 bottom-1.5 left-0 w-[2px] rounded-full transition-colors",
                      isActive
                        ? "bg-cc-accent"
                        : "bg-cc-card-border group-hover:bg-cc-card-border-hover",
                    ].join(" ")}
                  />
                  <span
                    className={[
                      "w-9 shrink-0 font-mono text-[10.5px] tracking-[0.18em] uppercase tabular-nums",
                      isActive ? "text-cc-accent" : "text-cc-ink-dim",
                    ].join(" ")}
                  >
                    {s.sigil}
                  </span>
                  <span
                    className={[
                      "font-mono text-[11.5px] tracking-tight",
                      isActive
                        ? "text-cc-heading"
                        : "text-cc-ink group-hover:text-cc-heading",
                    ].join(" ")}
                  >
                    {s.label}
                  </span>
                </a>
              </li>
            );
          })}
        </ul>
        <div className="border-cc-card-border mt-6 border-t pt-4">
          <div className="text-cc-ink-dim font-mono text-[10px] tracking-[0.22em] uppercase">
            Target framework
          </div>
          <div className="text-cc-ink mt-1 font-mono text-[11px]">net9.0</div>
        </div>
      </aside>
    </>
  );
}

// -----------------------------------------------------------------------------
// Article-level heading: in-flow companion to the sidebar entry.
// -----------------------------------------------------------------------------

interface SectionHeadingProps {
  readonly sigil: string;
  readonly children: ReactNode;
  readonly level?: "h2" | "h3";
}

function SectionHeading({
  sigil,
  children,
  level = "h3",
}: SectionHeadingProps) {
  const cls =
    level === "h2"
      ? "text-cc-heading font-heading text-h2 font-semibold tracking-tight text-balance"
      : "text-cc-heading font-heading text-h3 font-semibold tracking-tight text-balance";
  return (
    <div className="flex items-baseline gap-3">
      <Sigil value={sigil} />
      {level === "h2" ? (
        <h2 className={cls}>{children}</h2>
      ) : (
        <h3 className={cls}>{children}</h3>
      )}
    </div>
  );
}

// -----------------------------------------------------------------------------
// Article shell. Hairline divider on every section after the first, generous
// scroll-margin so the hash links land below the site header.
// -----------------------------------------------------------------------------

interface ArticleSectionProps {
  readonly id: string;
  readonly children: ReactNode;
  readonly first?: boolean;
}

function ArticleSection({ id, children, first = false }: ArticleSectionProps) {
  return (
    <section
      id={id}
      className={[
        "scroll-mt-24",
        first
          ? "pt-2 pb-14 sm:pb-16"
          : "border-cc-card-border border-t py-14 sm:py-16",
      ].join(" ")}
    >
      <div className="max-w-[68ch]">{children}</div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Inline code panel reused across sections.
// -----------------------------------------------------------------------------

interface InlineCodePanelProps {
  readonly file: string;
  readonly tag: string;
  readonly lines: ReactNode;
  readonly footer?: ReactNode;
}

function InlineCodePanel({ file, tag, lines, footer }: InlineCodePanelProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[11px]">{file}</span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {tag}
        </span>
      </div>
      <div className="py-3">{lines}</div>
      {footer ? (
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-3 border-t px-4 py-2 font-mono text-[10.5px]">
          {footer}
        </div>
      ) : null}
    </div>
  );
}

// -----------------------------------------------------------------------------
// Hero code panel. GraphQL operation, build bridge, C# call site.
// -----------------------------------------------------------------------------

function HeroCodePanel() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 180px at 14% 18%, rgba(94, 234, 212, 0.16), transparent 70%), radial-gradient(280px 140px at 8% 12%, rgba(22, 185, 228, 0.12), transparent 70%)",
        }}
      />

      <div className="bg-cc-code-header border-cc-card-border relative flex items-center gap-2 border-b px-4 py-3">
        <span className="text-cc-ink-dim font-mono text-[11px]">
          Catalog/GetProduct.graphql
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          GraphQL
        </span>
      </div>

      <div className="relative py-3">
        <CodeLine n={1}>
          <span style={T.gqlKw}>query</span>{" "}
          <span style={T.gqlType}>GetProduct</span>
          <span style={T.punct}>(</span>
          <span style={T.gqlVar}>$id</span>
          <span style={T.punct}>: </span>
          <span style={T.gqlType}>ID!</span>
          <span style={T.punct}>) {`{`}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={T.plain}>{`  `}</span>
          <span style={T.gqlField}>productById</span>
          <span style={T.punct}>(</span>
          <span style={T.gqlField}>id</span>
          <span style={T.punct}>: </span>
          <span style={T.gqlVar}>$id</span>
          <span style={T.punct}>) {`{`}</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={T.plain}>{`    `}</span>
          <span style={T.gqlField}>id</span>
          <span style={T.plain}> </span>
          <span style={T.gqlField}>name</span>
          <span style={T.plain}> </span>
          <span style={T.gqlField}>priceCents</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={T.plain}>{`    `}</span>
          <span style={T.gqlField}>variants</span>
          <span style={T.punct}> {`{`} </span>
          <span style={T.gqlField}>sku</span>
          <span style={T.plain}> </span>
          <span style={T.gqlField}>inStock</span>
          <span style={T.punct}> {`}`}</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={T.plain}>{`  `}</span>
          <span style={T.punct}>{`}`}</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={T.punct}>{`}`}</span>
        </CodeLine>
      </div>

      <div className="border-cc-card-border bg-cc-code-header text-cc-ink-dim flex items-center justify-between gap-3 border-y px-4 py-2 font-mono text-[10.5px] tracking-tight">
        <span className="inline-flex items-center gap-2">
          <span
            className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
            aria-hidden
          />
          dotnet build
        </span>
        <span className="text-cc-accent">
          MSBuild codegen emits typed client
        </span>
        <span>Catalog.Client.cs</span>
      </div>

      <div className="relative py-3">
        <CodeLine n={1}>
          <span style={T.kw}>using</span>{" "}
          <span style={T.plain}>Catalog.Client;</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={3}>
          <span
            style={T.comment}
          >{`// Resolve the generated, strongly-typed client from DI.`}</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={T.kw}>var</span> <span style={T.param}>client</span>{" "}
          <span style={T.punct}>=</span> <span style={T.param}>services</span>
          <span style={T.punct}>.</span>
          <span style={T.fn}>GetRequiredService</span>
          <span style={T.punct}>{`<`}</span>
          <span style={T.type}>ICatalogClient</span>
          <span style={T.punct}>{`>`}</span>
          <span style={T.punct}>();</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={T.kw}>var</span> <span style={T.param}>result</span>{" "}
          <span style={T.punct}>=</span> <span style={T.kw}>await</span>{" "}
          <span style={T.param}>client</span>
          <span style={T.punct}>.</span>
          <span style={T.plain}>GetProduct</span>
          <span style={T.punct}>.</span>
          <span style={T.fn}>ExecuteAsync</span>
          <span style={T.punct}>(</span>
          <span style={T.param}>id</span>
          <span style={T.punct}>);</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={T.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={T.type}>Product</span>{" "}
          <span style={T.param}>product</span> <span style={T.punct}>=</span>{" "}
          <span style={T.param}>result</span>
          <span style={T.punct}>.</span>
          <span style={T.plain}>Data</span>
          <span style={T.punct}>!.</span>
          <span style={T.plain}>ProductById</span>
          <span style={T.punct}>;</span>
        </CodeLine>
      </div>

      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
        <span>build: typed client + records + DI registration emitted</span>
        <span className="text-cc-accent">dotnet graphql, MSBuild codegen</span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// S02 inline snippet
// -----------------------------------------------------------------------------

function GraphqlrcSnippet() {
  return (
    <InlineCodePanel
      file=".graphqlrc.json"
      tag="JSON"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>{`{`}</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.str}>&quot;schema&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;schema.graphql&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.str}>&quot;documents&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;**/*.graphql&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.str}>&quot;extensions&quot;</span>
            <span style={T.punct}>: {`{`}</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.str}>&quot;strawberryShake&quot;</span>
            <span style={T.punct}>: {`{`}</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.str}>&quot;name&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;CatalogClient&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.str}>&quot;namespace&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;Catalog.Client&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.str}>&quot;url&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>
              &quot;https://api.example.com/graphql&quot;
            </span>
          </CodeLine>
          <CodeLine n={9}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
          <CodeLine n={10}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
          <CodeLine n={11}>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>dotnet graphql init &amp; dotnet graphql update</span>
          <span className="text-cc-accent">CLI</span>
        </>
      }
    />
  );
}

// -----------------------------------------------------------------------------
// S03 store diagram
// -----------------------------------------------------------------------------

function ReactiveStoreDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Two queries denormalized into one entity row, components subscribe to changes"
    >
      <defs>
        <linearGradient id="ss-v4-store-line" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.1" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
        </linearGradient>
      </defs>
      {[
        { y: 24, label: "GetProduct(id: 42)" },
        { y: 84, label: "ListProducts(first: 10)" },
      ].map((q) => (
        <g key={q.label}>
          <rect
            x="12"
            y={q.y}
            width="170"
            height="34"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="22"
            y={q.y + 21}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#a1a3af"
          >
            {q.label}
          </text>
          <path
            d={`M 182 ${q.y + 17} C 230 ${q.y + 17}, 230 110, 270 110`}
            stroke="url(#ss-v4-store-line)"
            strokeWidth="1.5"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="270"
        y="86"
        width="130"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="335"
        y="106"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        EntityStore
      </text>
      <text
        x="335"
        y="122"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        Product#42 (one row)
      </text>
      {[156, 188].map((y, i) => (
        <g key={y}>
          <path
            d={`M 400 110 C 432 110, 432 ${y}, 462 ${y}`}
            stroke="rgba(94,234,212,0.45)"
            strokeWidth="1.2"
            fill="none"
          />
          <text
            x="462"
            y={y + 3}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {i === 0 ? "Watch()" : "UseQuery"}
          </text>
        </g>
      ))}
      <text
        x="12"
        y="180"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        normalized, deduplicated, reactive
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// S04 fetch strategies diagram
// -----------------------------------------------------------------------------

function FetchStrategiesDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Three fetch strategies: cache-first, network-only, cache-and-network"
    >
      {[
        {
          y: 16,
          name: "CacheFirst",
          desc: "store hit returns first, no request",
        },
        {
          y: 84,
          name: "NetworkOnly",
          desc: "always fetch, then update the store",
        },
        {
          y: 152,
          name: "CacheAndNetwork",
          desc: "yield cache, refresh in the background",
        },
      ].map((s) => (
        <g key={s.name}>
          <rect
            x="12"
            y={s.y}
            width="180"
            height="48"
            rx="8"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.5)"
          />
          <text
            x="24"
            y={s.y + 20}
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {s.name}
          </text>
          <text
            x="24"
            y={s.y + 38}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {s.desc}
          </text>
          <path
            d={`M 192 ${s.y + 24} L 290 ${s.y + 24}`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
          <polygon
            points={`286,${s.y + 20} 298,${s.y + 24} 286,${s.y + 28}`}
            fill="rgba(94,234,212,0.55)"
          />
        </g>
      ))}
      <rect
        x="298"
        y="50"
        width="160"
        height="120"
        rx="10"
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="378"
        y="76"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#f5f0ea"
      >
        client.GetProduct
      </text>
      <text
        x="378"
        y="96"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        .Watch(strategy)
      </text>
      <text
        x="378"
        y="120"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        per call,
      </text>
      <text
        x="378"
        y="134"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        or set globally
      </text>
      <text
        x="378"
        y="156"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#5eead4"
      >
        IObservable&lt;Result&gt;
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// S06 MSBuild codegen flow diagram
// -----------------------------------------------------------------------------

function CodegenFlowDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="dotnet build runs MSBuild codegen across .graphql operations and schema.graphql to emit typed C# clients"
    >
      {[
        { y: 24, label: "schema.graphql", sub: "downloaded by CLI" },
        { y: 88, label: "GetProduct.graphql", sub: "your operations" },
        { y: 152, label: ".graphqlrc.json", sub: "name + namespace" },
      ].map((n) => (
        <g key={n.label}>
          <rect
            x="12"
            y={n.y}
            width="170"
            height="42"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="22"
            y={n.y + 18}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#f5f0ea"
          >
            {n.label}
          </text>
          <text
            x="22"
            y={n.y + 32}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.55)"
          >
            {n.sub}
          </text>
          <path
            d={`M 182 ${n.y + 21} L 270 110`}
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="270"
        y="86"
        width="110"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="325"
        y="106"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        MSBuild
      </text>
      <text
        x="325"
        y="122"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        codegen task
      </text>
      <path
        d="M 380 110 L 432 110"
        stroke="rgba(94,234,212,0.7)"
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="432,106 444,110 432,114" fill="rgba(94,234,212,0.7)" />
      <text
        x="468"
        y="100"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#f5f0ea"
      >
        CatalogClient.cs
      </text>
      <text
        x="468"
        y="116"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        records, DI, store
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// S05 subscription + Blazor snippets
// -----------------------------------------------------------------------------

function SubscriptionCodeSnippet() {
  return (
    <InlineCodePanel
      file="PriceTicker.razor"
      tag="Razor"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>UseSubscription</span>{" "}
            <span style={T.param}>TResult</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;OnPriceChangedResult&quot;</span>{" "}
            <span style={T.param}>Subscribe</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>
              &quot;@(c =&gt; c.OnPriceChanged.Watch(sku))&quot;
            </span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>@</span>
            <span style={T.kw}>if</span>{" "}
            <span style={T.punct}>(context.Data is {} d)</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>{`{`}</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>span</span>
            <span style={T.punct}>&gt;@d.PriceChanged.PriceCents&lt;/</span>
            <span style={T.type}>span</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>UseSubscription</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>WebSocket transport, store-backed re-render</span>
          <span className="text-cc-accent">@OnPriceChanged</span>
        </>
      }
    />
  );
}

function BlazorRazorSnippet() {
  return (
    <InlineCodePanel
      file="ProductCard.razor"
      tag="Blazor"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>UseQuery</span>{" "}
            <span style={T.param}>TResult</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;GetProductResult&quot;</span>{" "}
            <span style={T.param}>Operation</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>
              &quot;@(c =&gt; c.GetProduct.Watch(Id))&quot;
            </span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>Pending</span>
            <span style={T.punct}>&gt;Loading...&lt;/</span>
            <span style={T.type}>Pending</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>Error</span>{" "}
            <span style={T.param}>Context</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;errors&quot;</span>
            <span style={T.punct}>&gt;@errors[0].Message&lt;/</span>
            <span style={T.type}>Error</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ChildContent</span>{" "}
            <span style={T.param}>Context</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;result&quot;</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>h2</span>
            <span style={T.punct}>&gt;@result.Data!.ProductById.Name&lt;/</span>
            <span style={T.type}>h2</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>UseQuery</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>UseQuery, UseSubscription, UseFragment</span>
          <span className="text-cc-accent">StrawberryShake.Razor</span>
        </>
      }
    />
  );
}

// -----------------------------------------------------------------------------
// S06 .csproj snippet
// -----------------------------------------------------------------------------

function CsprojCodeSnippet() {
  return (
    <InlineCodePanel
      file="Catalog.Client.csproj"
      tag="MSBuild"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>Project</span> <span style={T.attr}>Sdk</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;Microsoft.NET.Sdk&quot;</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>PropertyGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>TargetFramework</span>
            <span style={T.punct}>&gt;</span>
            <span style={T.plain}>net9.0</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>TargetFramework</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>PropertyGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ItemGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>PackageReference</span>{" "}
            <span style={T.attr}>Include</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;StrawberryShake.Server&quot;</span>{" "}
            <span style={T.punct}>/&gt;</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>PackageReference</span>{" "}
            <span style={T.attr}>Include</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;StrawberryShake.Tools&quot;</span>{" "}
            <span style={T.attr}>PrivateAssets</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;all&quot;</span>{" "}
            <span style={T.punct}>/&gt;</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ItemGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={9}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>Project</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>dotnet tool install StrawberryShake.Tools</span>
          <span className="text-cc-accent">build-time only</span>
        </>
      }
    />
  );
}

// -----------------------------------------------------------------------------
// Definition list helper
// -----------------------------------------------------------------------------

interface DefRowProps {
  readonly term: string;
  readonly desc: string;
}

function DefRow({ term, desc }: DefRowProps) {
  return (
    <div className="border-cc-card-border grid grid-cols-1 gap-1 border-t py-3 sm:grid-cols-[10rem_minmax(0,1fr)] sm:gap-6">
      <dt className="text-cc-ink-dim font-mono text-[11px] tracking-[0.16em] uppercase">
        {term}
      </dt>
      <dd className="text-cc-ink text-body">{desc}</dd>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Bullet helper
// -----------------------------------------------------------------------------

interface BulletListProps {
  readonly items: readonly string[];
}

function BulletList({ items }: BulletListProps) {
  return (
    <ul className="mt-5 flex flex-col gap-2.5">
      {items.map((b) => (
        <li key={b} className="text-cc-ink text-body flex items-start gap-3">
          <span className="text-cc-accent mt-1 shrink-0">
            <CheckIcon size={14} />
          </span>
          <span>{b}</span>
        </li>
      ))}
    </ul>
  );
}

// -----------------------------------------------------------------------------
// Proof items grid for S08
// -----------------------------------------------------------------------------

interface ProofItemProps {
  readonly label: string;
  readonly value: string;
}

function ProofItem({ label, value }: ProofItemProps) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-cc-heading font-heading text-2xl font-semibold tracking-tight">
        {value}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
        {label}
      </span>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function StrawberryShakePreviewV4() {
  return (
    <div className="pt-10 pb-16 sm:pt-16">
      <div className="grid gap-10 lg:grid-cols-[16rem_minmax(0,1fr)] lg:gap-16">
        <SidebarToc />

        <main className="min-w-0">
          {/* S00 Hero */}
          <ArticleSection id="hero" first>
            <div className="flex items-center gap-3">
              <Sigil value="S00" />
              <Eyebrow>GraphQL client for .NET / Reference</Eyebrow>
            </div>
            <h1 className="text-cc-heading font-heading text-hero mt-5 font-semibold tracking-tight text-balance">
              Your .graphql files are the contract.
            </h1>
            <p className="text-cc-prose text-lead mt-6 max-w-[68ch]">
              Strawberry Shake is the open-source typed GraphQL client for .NET.
              Drop your operations into .graphql files, run{" "}
              <code className="text-cc-accent font-mono text-base">
                dotnet build
              </code>
              , and MSBuild codegen emits a typed client, immutable records, a
              normalized reactive store, and the DI registration. Call sites
              read like ordinary async C# against IDE-completed methods.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/strawberryshake">
                Get Started
              </SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
            <div className="mt-10">
              <HeroCodePanel />
            </div>
          </ArticleSection>

          {/* S01 Overview */}
          <ArticleSection id="overview">
            <SectionHeading sigil="S01" level="h2">
              Strawberry Shake at a glance
            </SectionHeading>
            <p className="text-cc-prose text-body mt-4">
              Strawberry Shake is the typed GraphQL client for .NET in the
              ChilliCream platform. It treats your .graphql files as the source
              of truth: a CLI keeps the schema in sync, MSBuild runs the codegen
              task during build, and the runtime is plain C# against an
              IObservable-backed entity store. The reference below walks each
              moving part in the order you meet it.
            </p>
            <dl className="mt-6">
              <DefRow
                term="License"
                desc="MIT, open source, free for commercial use."
              />
              <DefRow
                term="Runtimes"
                desc=".NET, Blazor Server, Blazor WebAssembly, hybrid Blazor, and .NET MAUI."
              />
              <DefRow
                term="Codegen"
                desc="MSBuild task that runs with dotnet build. Not source generators, not runtime IL."
              />
              <DefRow
                term="Server"
                desc="Any spec-compliant GraphQL server. Pairs cleanly with Hot Chocolate but does not require it."
              />
              <DefRow
                term="Transports"
                desc="HTTP for queries and mutations, WebSocket for subscriptions with reconnect handling."
              />
              <DefRow
                term="Store"
                desc="Normalized entity store keyed by type and id, with Watch() returning IObservable<Result>."
              />
            </dl>
          </ArticleSection>

          {/* S02 Strongly-typed */}
          <ArticleSection id="strongly-typed">
            <SectionHeading sigil="S02">Strongly-typed Client</SectionHeading>
            <p className="text-cc-prose text-body mt-4">
              Queries, mutations, and subscriptions live in plain .graphql files
              next to the code that uses them. The schema lives in a
              schema.graphql file pulled from any spec-compliant server. The CLI
              reads .graphqlrc.json, MSBuild emits the typed client class, the
              result records, the fragments, and the DI registration. Call sites
              are ordinary async C# with IntelliSense and refactor support.
            </p>
            <div className="mt-6">
              <GraphqlrcSnippet />
            </div>
            <BulletList
              items={[
                "Operations are valid GraphQL documents you can hand to any tool.",
                "Generated records are nullable-aware, immutable, and deconstructible.",
                "Compatible with any GraphQL spec server, not only Hot Chocolate.",
              ]}
            />
          </ArticleSection>

          {/* S03 Reactive Store */}
          <ArticleSection id="reactive-store">
            <SectionHeading sigil="S03">Reactive Store</SectionHeading>
            <p className="text-cc-prose text-body mt-4">
              Strawberry Shake denormalizes every GraphQL result into an entity
              store keyed by type and id, the same model Relay and Apollo made
              the standard for client caches. A query that returns the same
              product as a list and as a detail shares one row. Watch a query
              and your component re-renders when that row changes, no matter
              which operation produced the update.
            </p>
            <div className="border-cc-card-border mt-6 rounded-lg border p-4">
              <ReactiveStoreDiagram />
            </div>
            <BulletList
              items={[
                "IObservable<Result> on every Watch(), Razor and Blazor wrappers wire it for you.",
                "Mutations write back into the store, related queries refresh automatically.",
                "Persist the store to SQLite or LiteDB and rehydrate it on next launch.",
              ]}
            />
          </ArticleSection>

          {/* S04 Fetch strategies */}
          <ArticleSection id="fetch-strategies">
            <SectionHeading sigil="S04">Fetch Strategies</SectionHeading>
            <p className="text-cc-prose text-body mt-4">
              Every operation supports three execution strategies. Set them
              globally on the client builder, or pass a per-Watch override on
              the call site. The result stream emits both cache and network
              values in order, so a CacheAndNetwork operation yields a UI frame
              immediately and a refreshed frame moments later.
            </p>
            <dl className="mt-6">
              <DefRow
                term="CacheFirst"
                desc="Returns the store entry without a request when it has one, otherwise fetches and writes the result through the store."
              />
              <DefRow
                term="NetworkOnly"
                desc="Always fetches and writes the result through the store. Used when freshness matters more than latency."
              />
              <DefRow
                term="CacheAndNetwork"
                desc="Yields the cached entry immediately and refreshes in the background. The strategy that powers fast launches and snappy detail pages."
              />
            </dl>
            <div className="border-cc-card-border mt-6 rounded-lg border p-4">
              <FetchStrategiesDiagram />
            </div>
          </ArticleSection>

          {/* S05 Subscriptions & Blazor */}
          <ArticleSection id="subscriptions">
            <SectionHeading sigil="S05">
              Subscriptions, Blazor and Razor
            </SectionHeading>
            <p className="text-cc-prose text-body mt-4">
              Subscription operations look like queries: declare them in a
              .graphql file, get a typed Watch on the generated client. The
              WebSocket transport carries a connection_init payload for auth and
              reconnect handling. Pushed values write through the same entity
              store, so any open query, fragment, or Razor component sees the
              update. StrawberryShake.Razor ships UseQuery, UseSubscription, and
              UseFragment that bind generated operations to Blazor markup with
              Pending, Error, and ChildContent slots.
            </p>
            <div className="mt-6 flex flex-col gap-4">
              <SubscriptionCodeSnippet />
              <BlazorRazorSnippet />
            </div>
            <BulletList
              items={[
                "Token refresh and reconnect are part of the transport, not your code.",
                "Server, WebAssembly, and hybrid Blazor projects all use the same client.",
                "Fragments map to typed sub-records you can reuse across components.",
              ]}
            />
          </ArticleSection>

          {/* S06 MSBuild codegen */}
          <ArticleSection id="msbuild">
            <SectionHeading sigil="S06">MSBuild Code Generation</SectionHeading>
            <p className="text-cc-prose text-body mt-4">
              Strawberry Shake generates code through MSBuild tasks driven by
              the dotnet graphql CLI. It runs at dotnet build, not at runtime,
              and it is not a Roslyn source generator. Add the
              StrawberryShake.Tools NuGet, point .graphqlrc.json at your schema,
              and dotnet build emits .NET source for the client, operations,
              fragments, records, and the DI extension method. Stale generated
              code is a build error, not a runtime surprise.
            </p>
            <div className="mt-6">
              <CsprojCodeSnippet />
            </div>
            <div className="border-cc-card-border mt-6 rounded-lg border p-4">
              <CodegenFlowDiagram />
            </div>
            <BulletList
              items={[
                "dotnet graphql init scaffolds the config and downloads the schema in one step.",
                "dotnet graphql update keeps the local schema in sync with the server.",
                "Codegen runs in CI on every build, the same way your project compiles.",
              ]}
            />
          </ArticleSection>

          {/* S07 Nitro companion */}
          <ArticleSection id="nitro">
            <SectionHeading sigil="S07">
              Draft operations in Nitro, then commit the .graphql file.
            </SectionHeading>
            <p className="text-cc-prose text-body mt-4">
              Nitro is the GraphQL IDE that ships with the Hot Chocolate server,
              and it is the same surface most teams use to draft operations
              before saving them as .graphql files in the client project. Browse
              the schema, run a query against the live server, and copy the
              document into the codegen pipeline.
            </p>
            <div className="border-cc-card-border mt-6 overflow-hidden rounded-lg border">
              <NitroCompose />
            </div>
            <p className="text-cc-ink-dim mt-4 font-mono text-[11px] tracking-[0.18em] uppercase">
              live at /graphql
            </p>
          </ArticleSection>

          {/* S08 Open source */}
          <ArticleSection id="open-source">
            <SectionHeading sigil="S08" level="h2">
              Open source &amp; get started
            </SectionHeading>
            <p className="text-cc-prose text-body mt-4">
              Strawberry Shake is part of the ChilliCream GraphQL platform and
              is released under the MIT license. Use it in commercial work, fork
              it, vendor it, audit it. The codebase, issues, and release notes
              all live on GitHub next to Hot Chocolate.
            </p>
            <div className="border-cc-card-border mt-6 grid grid-cols-2 gap-6 rounded-lg border p-6 sm:grid-cols-3">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Codegen" value="MSBuild" />
              <ProofItem label="Store" value="Normalized" />
              <ProofItem label="Transports" value="HTTP / WS" />
              <ProofItem label="UI" value="Blazor / Razor" />
              <ProofItem label="Server" value="Hot Chocolate" />
            </div>

            <div className="relative mt-12 pt-10">
              <div
                aria-hidden
                className="pointer-events-none absolute inset-x-0 top-0 h-px"
                style={{ background: SPECTRUM }}
              />
              <div className="flex flex-wrap items-center gap-3">
                <SolidButton href="/docs/strawberryshake">
                  Get Started
                </SolidButton>
                <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                  View on GitHub
                </OutlineButton>
              </div>
            </div>
          </ArticleSection>
        </main>
      </div>
    </div>
  );
}
