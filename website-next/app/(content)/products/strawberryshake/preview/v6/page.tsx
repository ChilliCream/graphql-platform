import type { Metadata } from "next";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Strawberry Shake: The House Pour",
  description:
    "Strawberry Shake is the open-source, typed GraphQL client for .NET. MSBuild codegen turns .graphql order tickets into typed C# records, store, subscriptions.",
  keywords: [
    "Strawberry Shake",
    "typed GraphQL client for .NET",
    ".NET GraphQL client",
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
  robots: { index: false, follow: false },
  openGraph: {
    title: "Strawberry Shake: The House Pour",
    description:
      "Typed C# clients poured from your .graphql operations. Normalized reactive store, subscriptions, Blazor and Razor ready. MIT-licensed.",
    type: "website",
  },
};

// The brand spectrum, allowed at most ONCE per screen. Used as the hairline on
// the closing CTA. The hero gets the single clipped cyan to teal accent only.
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

interface BrewIconProps {
  readonly icon: ReactNode;
}

/** A small drink icon sitting next to an eyebrow on each feature row. */
function BrewIcon({ icon }: BrewIconProps) {
  return (
    <span
      aria-hidden
      className="border-cc-card-border text-cc-accent inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-md border"
    >
      <span className="block h-4 w-4">{icon}</span>
    </span>
  );
}

// -----------------------------------------------------------------------------
// Token color helpers. GitHub-dark approximations, scoped to inline code panels
// so the rest of the page stays on cc-* tokens.
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
  // GraphQL operation tokens
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
// EspressoCup glyph: a tiny inline cup rendered with cc-* tokens. Steam is the
// single cyan to teal gradient hairline (the lone color event in the hero).
// -----------------------------------------------------------------------------

interface EspressoCupProps {
  readonly className?: string;
}

function EspressoCup({ className }: EspressoCupProps) {
  return (
    <svg viewBox="0 0 64 64" className={className} aria-hidden fill="none">
      <defs>
        <linearGradient id="ss-v6-steam" x1="0" x2="0" y1="1" y2="0">
          <stop offset="0%" stopColor="#16b9e4" stopOpacity="0.0" />
          <stop offset="50%" stopColor="#16b9e4" stopOpacity="0.9" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.95" />
        </linearGradient>
      </defs>
      {/* steam, single hairline */}
      <path
        d="M 28 6 C 30 12, 26 16, 28 22"
        stroke="url(#ss-v6-steam)"
        strokeWidth="1.2"
        strokeLinecap="round"
        fill="none"
      />
      <path
        d="M 36 4 C 38 10, 34 14, 36 20"
        stroke="url(#ss-v6-steam)"
        strokeWidth="1.2"
        strokeLinecap="round"
        fill="none"
        opacity="0.7"
      />
      {/* cup body */}
      <path
        d="M 14 28 L 46 28 L 44 50 Q 44 56 38 56 L 22 56 Q 16 56 16 50 Z"
        stroke="#5eead4"
        strokeWidth="1.4"
        fill="rgba(94,234,212,0.06)"
      />
      {/* coffee surface */}
      <ellipse
        cx="30"
        cy="30"
        rx="14"
        ry="2.5"
        fill="rgba(94,234,212,0.18)"
        stroke="rgba(94,234,212,0.6)"
        strokeWidth="1"
      />
      {/* handle */}
      <path
        d="M 46 32 Q 56 32 56 40 Q 56 48 46 48"
        stroke="#5eead4"
        strokeWidth="1.4"
        fill="none"
      />
      {/* saucer */}
      <path
        d="M 10 58 L 50 58"
        stroke="rgba(245,241,234,0.32)"
        strokeWidth="1"
        strokeLinecap="round"
      />
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Hero "order ticket to counter" motif. Order ticket card on the left, dotted
// "dotnet build" arrow into the espresso cup, then the call-site panel.
// -----------------------------------------------------------------------------

function OrderTicketCard() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      {/* Clipped cyan to teal gradient. Single color event for the hero. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 180px at 14% 18%, rgba(94, 234, 212, 0.18), transparent 70%), radial-gradient(280px 140px at 8% 12%, rgba(22, 185, 228, 0.18), transparent 70%)",
        }}
      />

      {/* Ticket header */}
      <div className="bg-cc-code-header border-cc-card-border relative flex items-center gap-3 border-b px-4 py-3">
        <span className="text-cc-accent font-mono text-[10.5px] tracking-[0.22em] uppercase">
          Order ticket
        </span>
        <span className="text-cc-ink-dim font-mono text-[11px]">
          Catalog/GetProduct.graphql
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          GraphQL
        </span>
      </div>

      {/* Customer name row */}
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-3 border-b px-5 py-2 font-mono text-[10.5px]">
        <span className="tracking-wider uppercase">For</span>
        <span className="text-cc-ink">Catalog.Client</span>
      </div>

      {/* GraphQL operation */}
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

      {/* dotnet build arrow into espresso cup */}
      <div className="border-cc-card-border bg-cc-code-header relative flex items-center gap-4 border-y px-4 py-3">
        <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-wider uppercase">
          dotnet build
        </span>
        <span
          aria-hidden
          className="from-cc-accent/60 relative flex-1 bg-gradient-to-r to-transparent"
          style={{ height: 1 }}
        />
        <span className="text-cc-accent" aria-hidden>
          <svg
            width="14"
            height="10"
            viewBox="0 0 14 10"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.4"
          >
            <path d="M 1 5 L 12 5 M 8 1 L 12 5 L 8 9" strokeLinecap="round" />
          </svg>
        </span>
        <EspressoCup className="h-9 w-9" />
        <span className="text-cc-accent font-mono text-[10.5px] tracking-wider uppercase">
          Today&apos;s pour
        </span>
      </div>

      {/* C# call site, framed as what lands at the counter */}
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-3 border-b px-5 py-2 font-mono text-[10.5px]">
        <span className="tracking-wider uppercase">At the counter</span>
        <span className="text-cc-ink">Catalog.Client.cs</span>
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
          >{`// Resolve the generated, typed client from DI.`}</span>
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
// Alternating feature row, with a small drink icon sitting next to the eyebrow.
// -----------------------------------------------------------------------------

interface FeatureRowProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly icon: ReactNode;
  readonly title: string;
  readonly body: ReactNode;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

function FeatureRow({
  id,
  index,
  eyebrow,
  icon,
  title,
  body,
  bullets,
  visual,
  reverse = false,
}: FeatureRowProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <div
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex items-center gap-3">
            <BrewIcon icon={icon} />
            <IndexTag value={index} />
            <Eyebrow>{eyebrow}</Eyebrow>
          </div>
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            {title}
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            {body}
          </p>
          <ul className="mt-6 flex flex-col gap-2.5">
            {bullets.map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon size={14} />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </div>
        <div
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
        >
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 sm:p-6">
            {visual}
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Inline code panel used inside feature rows. Smaller than the hero card.
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
// Per-row code snippets and inline SVG diagrams.
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
          <span>house recipe</span>
          <span className="text-cc-accent">.graphqlrc.json</span>
        </>
      }
    />
  );
}

/** Normalized store diagram: two queries, one entity row in the cache. */
function ReactiveStoreDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Two queries denormalized into one entity row, components subscribe to changes"
    >
      <defs>
        <linearGradient id="ss-v6-store-line" x1="0" x2="1" y1="0" y2="0">
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
            stroke="url(#ss-v6-store-line)"
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
        Product#42 (one row, many cups)
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

/** Fetch strategies diagram, with small mono service tags on each row. */
function FetchStrategiesDiagram() {
  return (
    <svg
      viewBox="0 0 480 240"
      className="h-auto w-full"
      role="img"
      aria-label="Three fetch strategies: CacheFirst, NetworkOnly, CacheAndNetwork"
    >
      {[
        {
          y: 16,
          name: "CacheFirst",
          desc: "store hit returns first, no request",
          tag: "warm",
        },
        {
          y: 92,
          name: "NetworkOnly",
          desc: "always fetch, then update the store",
          tag: "fresh",
        },
        {
          y: 168,
          name: "CacheAndNetwork",
          desc: "yield cache, refresh in the background",
          tag: "both",
        },
      ].map((s) => (
        <g key={s.name}>
          <rect
            x="12"
            y={s.y}
            width="200"
            height="56"
            rx="8"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.5)"
          />
          <text
            x="24"
            y={s.y + 22}
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {s.name}
          </text>
          <text
            x="24"
            y={s.y + 42}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {s.desc}
          </text>
          <rect
            x="158"
            y={s.y + 8}
            width="46"
            height="16"
            rx="3"
            fill="rgba(94,234,212,0.1)"
            stroke="rgba(94,234,212,0.45)"
          />
          <text
            x="181"
            y={s.y + 19}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9"
            fill="#5eead4"
          >
            {s.tag}
          </text>
          <path
            d={`M 212 ${s.y + 28} L 298 ${s.y + 28}`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
          <polygon
            points={`294,${s.y + 24} 306,${s.y + 28} 294,${s.y + 32}`}
            fill="rgba(94,234,212,0.55)"
          />
        </g>
      ))}
      <rect
        x="306"
        y="62"
        width="160"
        height="128"
        rx="10"
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="386"
        y="88"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#f5f0ea"
      >
        client.GetProduct
      </text>
      <text
        x="386"
        y="108"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        .Watch(strategy)
      </text>
      <text
        x="386"
        y="134"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        per call,
      </text>
      <text
        x="386"
        y="148"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        or set globally
      </text>
      <text
        x="386"
        y="172"
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

/** MSBuild codegen flow with a small espresso cup glyph echoing the hero. */
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
        d="M 380 110 L 412 110"
        stroke="rgba(94,234,212,0.7)"
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="412,106 424,110 412,114" fill="rgba(94,234,212,0.7)" />
      {/* small inline espresso cup, echoing the hero motif */}
      <g transform="translate(424, 86)">
        <path
          d="M 6 14 L 30 14 L 28 36 Q 28 40 24 40 L 12 40 Q 8 40 8 36 Z"
          stroke="#5eead4"
          strokeWidth="1.2"
          fill="rgba(94,234,212,0.06)"
        />
        <path
          d="M 30 18 Q 38 18 38 24 Q 38 30 30 30"
          stroke="#5eead4"
          strokeWidth="1.2"
          fill="none"
        />
        <ellipse
          cx="18"
          cy="16"
          rx="10"
          ry="1.8"
          fill="rgba(94,234,212,0.18)"
          stroke="rgba(94,234,212,0.5)"
          strokeWidth="0.8"
        />
        <path
          d="M 14 4 C 16 8, 12 10, 14 14"
          stroke="rgba(94,234,212,0.6)"
          strokeWidth="0.8"
          strokeLinecap="round"
          fill="none"
        />
      </g>
      <text
        x="468"
        y="146"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        CatalogClient.cs
      </text>
      <text
        x="468"
        y="160"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        records, DI, store
      </text>
    </svg>
  );
}

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
// Open source proof items.
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

export default function StrawberryShakePreviewV6() {
  return (
    <>
      {/* HERO: order-ticket-to-counter motif. One color event total. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <div className="flex items-center gap-3">
              <BrewIcon icon={<DripBrewer className="h-full w-full" />} />
              <Eyebrow>On the menu // House Pour</Eyebrow>
            </div>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Your .graphql files are the order ticket.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Strawberry Shake is the open-source, typed GraphQL client for
              .NET. Drop your operations into .graphql files, run
              <code className="text-cc-accent mx-1 font-mono text-base">
                dotnet build
              </code>
              , and MSBuild codegen emits a typed client, immutable records, a
              normalized reactive store, and the DI registration. The call site
              is what lands at the counter, named and made to spec.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/strawberryshake">
                Get Started
              </SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
            <dl className="border-cc-card-border mt-10 grid grid-cols-3 gap-6 border-t pt-6">
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  License
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">MIT</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Runtimes
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">.NET, Blazor, MAUI</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Codegen
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">
                  MSBuild, build time
                </dd>
              </div>
            </dl>
          </div>
          <div className="lg:col-span-7">
            <OrderTicketCard />
          </div>
        </div>
      </section>

      {/* Brew menu strip: capability chips in a tiny menu list */}
      <section
        aria-label="Today's brew menu"
        className="border-cc-card-border border-y py-6"
      >
        <div className="mb-4 flex items-center gap-3">
          <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
            Today&apos;s brew menu
          </span>
          <span
            aria-hidden
            className="from-cc-card-border h-px flex-1 bg-gradient-to-r to-transparent"
          />
        </div>
        <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-6">
          {[
            "MSBuild codegen",
            "Normalized store",
            "Three fetch strategies",
            "WebSocket subscriptions",
            "Persisted operations",
            "Blazor / Razor ready",
          ].map((label) => (
            <li
              key={label}
              className="text-cc-ink flex items-center gap-2 font-mono text-[11.5px] tracking-tight uppercase"
            >
              <span className="text-cc-accent" aria-hidden>
                <CheckIcon size={12} />
              </span>
              {label}
            </li>
          ))}
        </ul>
      </section>

      {/* FEATURE ROWS: each carries a small drink icon as the row index. */}
      <FeatureRow
        id="strongly-typed"
        index="01"
        eyebrow="The order ticket // Typed client"
        icon={<DripBrewer className="h-full w-full" />}
        title="Operations in .graphql, typed C# at the counter."
        body="Queries, mutations, and subscriptions live in plain .graphql files next to the code that uses them. The schema lives in a schema.graphql file pulled from any spec-compliant server. The CLI reads the .graphqlrc.json config, MSBuild emits the typed client class, the result records, the fragments, and the DI registration, and the call sites are ordinary async C# with IntelliSense and refactor support."
        bullets={[
          "Operations are valid GraphQL documents you can hand to any tool.",
          "Generated records are nullable-aware, immutable, and deconstructible.",
          "Compatible with any GraphQL spec server, not only Hot Chocolate.",
        ]}
        visual={<GraphqlrcSnippet />}
      />

      <FeatureRow
        id="reactive-store"
        index="02"
        eyebrow="The pass // Reactive store"
        icon={<PourOver className="h-full w-full" />}
        title="One bean ground once, every drink poured from it."
        body="Strawberry Shake denormalizes every GraphQL result into an entity store keyed by type and id: one row per entity, every Watch re-renders on change. The same model Relay and Apollo made the standard for client caches. A query that returns the same product as a list and as a detail shares one row. Watch a query and your component re-renders when that row changes, no matter which operation produced the update."
        bullets={[
          "IObservable<Result> on every Watch(), Razor and Blazor wrappers wire it for you.",
          "Mutations write back into the store, related queries refresh automatically.",
          "Persist the store to SQLite or LiteDB and rehydrate it on next launch.",
        ]}
        visual={<ReactiveStoreDiagram />}
        reverse
      />

      <FeatureRow
        id="fetch-strategies"
        index="03"
        eyebrow="Serve styles // Fetch strategies"
        icon={<Espresso className="h-full w-full" />}
        title="CacheFirst, NetworkOnly, CacheAndNetwork. Pick the pour per call."
        body="Every operation supports three execution strategies. CacheFirst returns the store entry without a request when it has one. NetworkOnly always fetches and writes the result through the store. CacheAndNetwork yields the cached entry immediately and refreshes in the background, which is the strategy that powers fast launches and snappy detail pages."
        bullets={[
          "Strategy is a per-Watch override on top of a per-client default.",
          "Combine with persisted state for instant cache hits on cold start.",
          "Result stream emits both cache and network values, in order.",
        ]}
        visual={<FetchStrategiesDiagram />}
      />

      <FeatureRow
        id="subscriptions"
        index="04"
        eyebrow="Live from the bar // Subscriptions"
        icon={<FrenchPress className="h-full w-full" />}
        title="Realtime over WebSocket, into the same store."
        body="Subscription operations look like queries: declare them in a .graphql file, get a typed Watch on the generated client. The WebSocket transport carries a connection_init payload for auth and reconnect handling. Pushed values write through the same entity store, so any open query, fragment, or Razor component sees the update."
        bullets={[
          "Token refresh and reconnect are part of the transport, not your code.",
          "UseSubscription Razor component lifts updates straight into Blazor markup.",
          "Same Watch surface as queries, no separate event-handler pipeline.",
        ]}
        visual={<SubscriptionCodeSnippet />}
        reverse
      />

      <FeatureRow
        id="msbuild-codegen"
        index="05"
        eyebrow="Built to order // MSBuild codegen"
        icon={<CoffeeTray className="h-full w-full" />}
        title="Codegen runs at dotnet build, not at runtime."
        body="Strawberry Shake generates code through MSBuild tasks driven by the dotnet graphql CLI, not through runtime IL weaving and not through reflection. Add the StrawberryShake.Tools NuGet, point the .graphqlrc.json at your schema, and dotnet build emits .NET source for the client, operations, fragments, records, and the DI extension method. Stale generated code is a build error, not a runtime surprise."
        bullets={[
          "dotnet graphql init scaffolds the config and downloads the schema in one step.",
          "dotnet graphql update keeps the local schema in sync with the server.",
          "MSBuild codegen runs in CI on every build, the same way your project compiles.",
        ]}
        visual={<CsprojCodeSnippet />}
      />

      <FeatureRow
        id="blazor-razor"
        index="06"
        eyebrow="At the counter // Blazor and Razor"
        icon={<DripBrewer className="h-full w-full" />}
        title="Razor components served from the reactive store."
        body="StrawberryShake.Razor ships UseQuery, UseSubscription, UseFragment, and a DataComponent base that bind generated operations to Blazor markup. Pending, Error, and ChildContent slots cover the common UI states, and every component reacts to the entity store, so a mutation in one corner of the app re-renders dependent components everywhere."
        bullets={[
          "Server, WebAssembly, and hybrid Blazor projects all use the same client.",
          "Fragments map to typed sub-records you can reuse across components.",
          "Works inside .NET MAUI for typed GraphQL on iOS, Android, and desktop.",
        ]}
        visual={<BlazorRazorSnippet />}
        reverse
      />

      <FeatureRow
        id="codegen-flow"
        index="07"
        eyebrow="Behind the bar // Build pipeline"
        icon={<PourOver className="h-full w-full" />}
        title="One pipeline, from .graphql to typed records."
        body="The full picture: the CLI keeps your schema.graphql current, .graphqlrc.json names the client, your operations live in .graphql files, MSBuild runs the codegen task during dotnet build, and the project compiles against the freshly emitted .cs. Nothing happens at runtime that you did not write or that the build did not emit."
        bullets={[
          "No reflection at request time, no surprise startup work to scan documents.",
          "Persisted operations: ship operation hashes, lock production to known docs.",
          "Codegen output is checked source, diffable in code review.",
        ]}
        visual={<CodegenFlowDiagram />}
      />

      {/* The lone embedded Nitro product card: the GraphQL IDE the client pairs with. */}
      <section className="border-cc-card-border border-t py-20 sm:py-24">
        <div className="mb-10 grid items-end gap-6 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <IndexTag value="08" />
            <div className="mt-3">
              <Eyebrow>Drafting station</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Draft the operation, then check in the .graphql file.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Nitro is the GraphQL IDE that ships with the Hot Chocolate server,
              and it is the same surface most teams use to draft operations
              before saving them as .graphql files in the client project. Browse
              the schema, run a query against the live server, and copy the
              document into the codegen pipeline.
            </p>
          </div>
          <div className="lg:col-span-5 lg:text-right">
            <p className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
              live at /graphql
            </p>
          </div>
        </div>
        <div className="border-cc-card-border bg-cc-surface mx-auto max-w-5xl overflow-hidden rounded-xl border">
          <NitroCompose />
        </div>
      </section>

      {/* MIT / open source proof band */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>
              House blend, free to brew{" "}
              <span className="text-cc-ink-dim">(MIT licensed)</span>
            </Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Open source, in production, and free to use.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Strawberry Shake is part of the ChilliCream GraphQL platform and
              is released under the MIT license. Use it in commercial work, fork
              it, vendor it, audit it. The codebase, issues, and release notes
              all live on GitHub next to Hot Chocolate.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/strawberryshake">
                Read the docs
              </OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Codegen" value="MSBuild" />
              <ProofItem label="Store" value="Normalized" />
              <ProofItem label="Transports" value="HTTP / WebSocket" />
              <ProofItem label="UI" value="Blazor / Razor" />
              <ProofItem label="Server" value="Hot Chocolate" />
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA with the single brand-spectrum hairline */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Order up</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            A typed GraphQL client your .NET team can actually own.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            A few .graphql files, a .graphqlrc.json, and a NuGet reference. The
            client, the records, the store, and the DI wiring are emitted for
            you at build time, and the runtime is the .NET you already ship.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
