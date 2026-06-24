import type { Metadata } from "next";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CookieCrumble } from "@/src/icons/CookieCrumble";

export const metadata: Metadata = {
  title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
  description:
    "GraphQL snapshot testing .NET, walked step by step. Cookie Crumble ships native formatters for IExecutionResult and GraphQLHttpResponse, with inline, file, and Markdown snapshots.",
  keywords: [
    "Cookie Crumble",
    "GraphQL snapshot testing .NET",
    "snapshot testing",
    ".NET testing",
    "Hot Chocolate testing",
    "IExecutionResult",
    "GraphQLHttpResponse",
    "MatchSnapshot",
    "MatchInlineSnapshot",
    "MatchMarkdownSnapshot",
    "xUnit",
    "NUnit",
    "TUnit",
    "MSTest",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
    description:
      "A step-by-step walk through Cookie Crumble: GraphQL-aware formatters, three snapshot shapes, and the __mismatch__ workflow. MIT-licensed.",
    type: "website",
  },
};

// Brand-spectrum hairline, used at most once on the closing CTA.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Shared primitives. The page is single-column so the rail is global.
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
// Step shell. Each step hangs an oversized numeral off a single vertical rule
// that runs through the entire page. The numeral sits on a small circular tick
// so the spine reads as a continuous timeline.
// -----------------------------------------------------------------------------

interface StepProps {
  readonly id?: string;
  readonly numeral: string;
  readonly children: ReactNode;
  readonly first?: boolean;
  readonly last?: boolean;
}

function Step({
  id,
  numeral,
  children,
  first = false,
  last = false,
}: StepProps) {
  return (
    <section
      id={id}
      className={[
        "relative scroll-mt-24",
        first ? "pt-12 pb-20 sm:pt-16 sm:pb-24" : "py-24 sm:py-28",
        last ? "pb-0" : "",
      ].join(" ")}
    >
      <div className="grid grid-cols-[5.5rem_1fr] gap-x-6 sm:grid-cols-[7rem_1fr] sm:gap-x-10 lg:grid-cols-[8.5rem_1fr] lg:gap-x-14">
        <div className="relative">
          {/* Tick mark on the rule */}
          <span
            aria-hidden
            className="border-cc-card-border bg-cc-bg absolute top-2 left-[-7px] block h-3.5 w-3.5 rounded-full border"
          />
          <span
            className="text-cc-accent font-heading block text-[64px] leading-none font-semibold tabular-nums sm:text-[88px] lg:text-[120px]"
            aria-hidden
          >
            {numeral}
          </span>
        </div>
        <div className="min-w-0 pt-1 sm:pt-2 lg:pt-3">{children}</div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Code primitives. Mirrors v1's card chrome and GitHub-dark token colors so
// the embedded snippets look native alongside the spine.
// -----------------------------------------------------------------------------

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

const C: Record<string, CSSProperties> = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  comment: { color: "#8b949e", fontStyle: "italic" },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
  dim: { color: "#8b949e" },
};

interface CodeCardProps {
  readonly filename: string;
  readonly lang: string;
  readonly children: ReactNode;
  readonly footerLeft?: ReactNode;
  readonly footerRight?: ReactNode;
}

function CodeCard({
  filename,
  lang,
  children,
  footerLeft,
  footerRight,
}: CodeCardProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-3">
        <span
          className="bg-cc-status-firing h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-investigating h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-healthy h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span className="text-cc-ink-dim ml-3 truncate font-mono text-[11px]">
          {filename}
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex shrink-0 items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {lang}
        </span>
      </div>
      <div className="relative py-4">{children}</div>
      {(footerLeft || footerRight) && (
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
          <span>{footerLeft}</span>
          <span className="text-cc-accent">{footerRight}</span>
        </div>
      )}
    </div>
  );
}

// Mini code block used for the inline / file / markdown stack and the two
// formatter snippets. Looks like the rest of the page's code chrome, just
// scoped down for compactness.
interface MiniCodeProps {
  readonly label: string;
  readonly api: string;
  readonly children: ReactNode;
}

function MiniCode({ label, api, children }: MiniCodeProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
        <span>{label}</span>
        <span className="text-cc-accent">{api}</span>
      </div>
      <div className="py-3">{children}</div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Hero test card and its produced snapshot, stacked in step 01.
// -----------------------------------------------------------------------------

function HeroTestCard() {
  return (
    <CodeCard
      filename="Catalog.Tests/ProductQueryTests.cs"
      lang="C#"
      footerLeft="xUnit + Cookie Crumble"
      footerRight="MatchSnapshot()"
    >
      <CodeLine n={1}>
        <span style={C.kw}>using</span>{" "}
        <span style={C.plain}>CookieCrumble;</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.kw}>using</span>{" "}
        <span style={C.plain}>HotChocolate.Execution;</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.kw}>public class</span>{" "}
        <span style={C.type}>ProductQueryTests</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={C.punct}>{`{`}</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.punct}>[</span>
        <span style={C.attr}>Fact</span>
        <span style={C.punct}>]</span>
      </CodeLine>
      <CodeLine n={7}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.kw}>public async</span> <span style={C.type}>Task</span>{" "}
        <span style={C.fn}>Product_By_Id_Returns_Catalog_Shape</span>
        <span style={C.punct}>()</span>
      </CodeLine>
      <CodeLine n={8}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.punct}>{`{`}</span>
      </CodeLine>
      <CodeLine n={9}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.comment}>{`// arrange`}</span>
      </CodeLine>
      <CodeLine n={10}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.kw}>await using var</span>{" "}
        <span style={C.plain}>server </span>
        <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
        <span style={C.type}>TestServer</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>CreateAsync</span>
        <span style={C.punct}>();</span>
      </CodeLine>
      <CodeLine n={11}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={12}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.comment}>{`// act`}</span>
      </CodeLine>
      <CodeLine n={13}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.type}>IExecutionResult</span>{" "}
        <span style={C.plain}>result </span>
        <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
        <span style={C.plain}>server</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>ExecuteAsync</span>
        <span style={C.punct}>(</span>
      </CodeLine>
      <CodeLine n={14}>
        <span style={C.plain}>{`            `}</span>
        <span style={C.str}>{`"""`}</span>
      </CodeLine>
      <CodeLine n={15}>
        <span style={C.plain}>{`            `}</span>
        <span
          style={C.str}
        >{`query { productById(id: "p_42") { id name price } }`}</span>
      </CodeLine>
      <CodeLine n={16}>
        <span style={C.plain}>{`            `}</span>
        <span style={C.str}>{`"""`}</span>
        <span style={C.punct}>);</span>
      </CodeLine>
      <CodeLine n={17}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={18}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.comment}>{`// assert`}</span>
      </CodeLine>
      <CodeLine n={19}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.plain}>result</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>MatchSnapshot</span>
        <span style={C.punct}>();</span>
      </CodeLine>
      <CodeLine n={20}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
      <CodeLine n={21}>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
    </CodeCard>
  );
}

function HeroSnapshotCard() {
  return (
    <CodeCard
      filename="__snapshots__/ProductQueryTests.Product_By_Id_Returns_Catalog_Shape.snap"
      lang="snapshot"
      footerLeft="GraphQL-aware formatter"
      footerRight="IExecutionResult"
    >
      <CodeLine n={1}>
        <span style={C.punct}>{`{`}</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.plain}>{`  `}</span>
        <span style={C.str}>{`"data"`}</span>
        <span style={C.punct}>: {`{`}</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.str}>{`"productById"`}</span>
        <span style={C.punct}>: {`{`}</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.plain}>{`      `}</span>
        <span style={C.str}>{`"id"`}</span>
        <span style={C.punct}>: </span>
        <span style={C.str}>{`"p_42"`}</span>
        <span style={C.punct}>,</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={C.plain}>{`      `}</span>
        <span style={C.str}>{`"name"`}</span>
        <span style={C.punct}>: </span>
        <span style={C.str}>{`"Cookie Crumble Tee"`}</span>
        <span style={C.punct}>,</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={C.plain}>{`      `}</span>
        <span style={C.str}>{`"price"`}</span>
        <span style={C.punct}>: </span>
        <span style={C.plain}>{`24.0`}</span>
      </CodeLine>
      <CodeLine n={7}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
      <CodeLine n={8}>
        <span style={C.plain}>{`  `}</span>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
      <CodeLine n={9}>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
      <CodeLine n={10}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={11}>
        <span style={C.comment}>{`# Committed alongside the test.`}</span>
      </CodeLine>
      <CodeLine n={12}>
        <span
          style={C.comment}
        >{`# Diffs in PRs read like the API contract.`}</span>
      </CodeLine>
    </CodeCard>
  );
}

// -----------------------------------------------------------------------------
// Bullet list used inside steps. Same check icon treatment as v1.
// -----------------------------------------------------------------------------

interface BulletListProps {
  readonly items: readonly string[];
}

function BulletList({ items }: BulletListProps) {
  return (
    <ul className="mt-6 flex flex-col gap-2.5">
      {items.map((b) => (
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
  );
}

// -----------------------------------------------------------------------------
// Step 02: two stacked mini formatter snippets.
// -----------------------------------------------------------------------------

function FormattersBlock() {
  return (
    <div className="mt-8 flex flex-col gap-3">
      <MiniCode label="IExecutionResult" api="in-process execution">
        <CodeLine n={1}>
          <span style={C.type}>IExecutionResult</span>{" "}
          <span style={C.plain}>result </span>
          <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
          <span style={C.plain}>server</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>ExecuteAsync</span>
          <span style={C.punct}>(query);</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>result</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchSnapshot</span>
          <span style={C.punct}>();</span>
        </CodeLine>
      </MiniCode>
      <MiniCode label="GraphQLHttpResponse" api="over HTTP">
        <CodeLine n={1}>
          <span style={C.type}>GraphQLHttpResponse</span>{" "}
          <span style={C.plain}>response </span>
          <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
          <span style={C.plain}>client</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>PostAsync</span>
          <span style={C.punct}>(request);</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>response</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchSnapshot</span>
          <span style={C.punct}>();</span>
        </CodeLine>
      </MiniCode>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Step 03: inline / file / markdown snippets stacked along the spine.
// -----------------------------------------------------------------------------

function ShapesBlock() {
  return (
    <div className="mt-8 flex flex-col gap-3">
      <MiniCode label="Inline" api="MatchInlineSnapshot">
        <CodeLine n={1}>
          <span style={C.plain}>result</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchInlineSnapshot</span>
          <span style={C.punct}>(</span>
          <span style={C.str}>{`"""`}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.str}>{`{ "data": { "ping": "pong" } }`}</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.str}>{`"""`}</span>
          <span style={C.punct}>);</span>
        </CodeLine>
      </MiniCode>
      <MiniCode label="File" api="MatchSnapshot">
        <CodeLine n={1}>
          <span style={C.plain}>result</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchSnapshot</span>
          <span style={C.punct}>();</span>{" "}
          <span style={C.comment}>{`// __snapshots__/<test>.snap`}</span>
        </CodeLine>
      </MiniCode>
      <MiniCode label="Markdown" api="MatchMarkdownSnapshot">
        <CodeLine n={1}>
          <span style={C.type}>Snapshot</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Create</span>
          <span style={C.punct}>()</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(request, </span>
          <span style={C.str}>{`"Request"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(result, </span>
          <span style={C.str}>{`"Result"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(events, </span>
          <span style={C.str}>{`"Audit"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchMarkdownSnapshot</span>
          <span style={C.punct}>();</span>
        </CodeLine>
      </MiniCode>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Step 04: the __mismatch__ flow as a compact inline SVG.
// -----------------------------------------------------------------------------

function MismatchFlow() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg mt-8 rounded-xl border p-5 sm:p-6">
      <svg
        viewBox="0 0 480 240"
        className="h-auto w-full"
        role="img"
        aria-label="Failing tests write to a __mismatch__ folder, the diff is reviewed, then the snapshot moves into __snapshots__"
      >
        <defs>
          <linearGradient id="cc-v5-flow" x1="0" x2="1" y1="0" y2="0">
            <stop offset="0%" stopColor="#5eead4" stopOpacity="0.1" />
            <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
          </linearGradient>
        </defs>
        <rect
          x="12"
          y="24"
          width="160"
          height="64"
          rx="8"
          fill="rgba(240,120,106,0.08)"
          stroke="rgba(240,120,106,0.55)"
        />
        <text
          x="92"
          y="48"
          textAnchor="middle"
          fontFamily="var(--font-body)"
          fontSize="12"
          fill="#f5f0ea"
        >
          Test run differs
        </text>
        <text
          x="92"
          y="66"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.62)"
        >
          snapshot mismatch
        </text>
        <rect
          x="12"
          y="132"
          width="200"
          height="80"
          rx="8"
          fill="rgba(245,241,234,0.04)"
          stroke="rgba(245,241,234,0.16)"
        />
        <text
          x="22"
          y="152"
          fontFamily="ui-monospace, monospace"
          fontSize="11"
          fill="#a1a3af"
        >
          __mismatch__/
        </text>
        <text
          x="34"
          y="172"
          fontFamily="ui-monospace, monospace"
          fontSize="10.5"
          fill="rgba(245,241,234,0.62)"
        >
          ProductQueryTests.snap
        </text>
        <text
          x="34"
          y="190"
          fontFamily="ui-monospace, monospace"
          fontSize="10.5"
          fill="rgba(245,241,234,0.62)"
        >
          BasketServiceTests.snap
        </text>
        <text
          x="34"
          y="204"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.45)"
        >
          gitignored, never committed
        </text>
        <path
          d="M 92 88 L 92 132"
          stroke="rgba(240,120,106,0.55)"
          strokeWidth="1.5"
          fill="none"
        />
        <polygon points="88,128 92,140 96,128" fill="rgba(240,120,106,0.7)" />
        <rect
          x="276"
          y="92"
          width="180"
          height="56"
          rx="8"
          fill="rgba(94,234,212,0.08)"
          stroke="rgba(94,234,212,0.55)"
        />
        <text
          x="366"
          y="116"
          textAnchor="middle"
          fontFamily="var(--font-body)"
          fontSize="12"
          fill="#5eead4"
        >
          Move into __snapshots__/
        </text>
        <text
          x="366"
          y="132"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.62)"
        >
          review the diff, then accept
        </text>
        <path
          d="M 212 170 C 250 170, 250 120, 276 120"
          stroke="url(#cc-v5-flow)"
          strokeWidth="1.5"
          fill="none"
        />
        <polygon points="272,116 280,120 272,124" fill="rgba(94,234,212,0.7)" />
        <text
          x="234"
          y="158"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.55)"
        >
          review diff
        </text>
      </svg>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Step 05: 2x2 framework grid plus single-row caption.
// -----------------------------------------------------------------------------

function FrameworkGrid() {
  const frameworks: readonly {
    readonly name: string;
    readonly note: string;
  }[] = [
    { name: "xUnit", note: "[Fact] / [Theory]" },
    { name: "NUnit", note: "[Test]" },
    { name: "TUnit", note: "[Test]" },
    { name: "MSTest", note: "[TestMethod]" },
  ];
  return (
    <div className="mt-8 grid grid-cols-2 gap-3">
      {frameworks.map((f) => (
        <div
          key={f.name}
          className="border-cc-card-border bg-cc-surface/40 flex flex-col gap-1 rounded-lg border px-4 py-4"
        >
          <div className="flex items-center justify-between">
            <span className="text-cc-heading font-heading text-base font-semibold">
              {f.name}
            </span>
            <span className="text-cc-accent" aria-hidden>
              <CheckIcon size={14} />
            </span>
          </div>
          <span className="text-cc-ink-dim font-mono text-[11px]">
            {f.note}
          </span>
        </div>
      ))}
      <div className="border-cc-card-border bg-cc-card-bg col-span-2 rounded-lg border px-4 py-3">
        <p className="text-cc-ink text-[12.5px] leading-relaxed">
          The assertion API is the same in every framework. Add the Cookie
          Crumble package, call MatchSnapshot, and the failure message points at
          the diff your runner of choice already knows how to surface.
        </p>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Step 06: inline product list with the CookieCrumble glyph anchored to the
// spine on the left.
// -----------------------------------------------------------------------------

function DogfoodList() {
  const products: readonly { readonly name: string; readonly role: string }[] =
    [
      { name: "Hot Chocolate", role: "GraphQL server" },
      { name: "Fusion", role: "Federation gateway" },
      { name: "Mocha", role: "Distributed messaging" },
    ];
  return (
    <div className="mt-8 grid items-start gap-6 sm:grid-cols-[auto_1fr]">
      <div className="bg-cc-surface/40 border-cc-card-border flex h-28 w-28 items-center justify-center rounded-xl border">
        <CookieCrumble className="h-20 w-auto" />
      </div>
      <ul className="flex flex-col gap-2.5">
        {products.map((p) => (
          <li
            key={p.name}
            className="border-cc-card-border bg-cc-surface/40 flex items-center justify-between rounded-lg border px-4 py-3"
          >
            <span className="text-cc-heading font-heading text-base font-semibold">
              {p.name}
            </span>
            <span className="text-cc-ink-dim font-mono text-[11px] tracking-wider uppercase">
              {p.role}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function CookieCrumblePreviewV5() {
  return (
    <div className="relative mx-auto max-w-[64rem]">
      {/* The single continuous spine. Sits behind every step, anchored to the
          left edge of the numeral column. */}
      <div
        aria-hidden
        className="bg-cc-card-border pointer-events-none absolute top-0 bottom-0 left-[calc(5.5rem-1px)] w-px sm:left-[calc(7rem-1px)] lg:left-[calc(8.5rem-1px)]"
      />

      {/* Step 00 - Hero */}
      <Step numeral="00" first>
        <Eyebrow>Snapshot testing for .NET</Eyebrow>
        <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
          Snapshot testing that understands GraphQL.
        </h1>
        <p className="text-cc-prose mt-6 max-w-2xl text-lg leading-relaxed">
          Cookie Crumble is the open-source snapshot library the ChilliCream
          team writes its own tests with. It ships native formatters for Hot
          Chocolate IExecutionResult and GraphQLHttpResponse, so the snapshot
          file reads like the GraphQL response itself. Inline, file, or
          Markdown. xUnit, NUnit, TUnit, or MSTest. MIT-licensed.
        </p>
        <div className="mt-8 flex flex-wrap gap-3">
          <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>
        <dl className="border-cc-card-border mt-10 grid max-w-2xl grid-cols-3 gap-6 border-t pt-6">
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
            <dd className="text-cc-ink mt-1 text-sm">.NET 8 and later</dd>
          </div>
          <div>
            <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
              Frameworks
            </dt>
            <dd className="text-cc-ink mt-1 text-sm">
              xUnit, NUnit, TUnit, MSTest
            </dd>
          </div>
        </dl>
      </Step>

      {/* Step 01 - The two artefacts */}
      <Step id="artefacts" numeral="01">
        <Eyebrow>The two artefacts</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          Read the test, read the snapshot.
        </h2>
        <p className="text-cc-prose mt-4 max-w-2xl text-lg leading-relaxed">
          A Cookie Crumble test is a normal .NET test that asserts a result with
          MatchSnapshot. The snapshot it produces sits next to the test on disk.
          Both files live in the repo, both files show up in code review. Cause
          and effect, top to bottom.
        </p>
        <div className="mt-8 flex flex-col gap-4">
          <HeroTestCard />
          <HeroSnapshotCard />
        </div>
      </Step>

      {/* Step 02 - GraphQL-aware formatters */}
      <Step id="formatters" numeral="02">
        <Eyebrow>GraphQL-aware formatters</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          The snapshot reads like the GraphQL response, not a dump.
        </h2>
        <p className="text-cc-prose mt-4 max-w-2xl text-lg leading-relaxed">
          Cookie Crumble ships first-class formatters for Hot Chocolate&apos;s
          IExecutionResult and for GraphQLHttpResponse. Pass either type to
          MatchSnapshot and the snapshot file comes out as the request and the
          response, in a shape your reviewers can read. No custom serializers,
          no opt-in attributes.
        </p>
        <BulletList
          items={[
            "Native formatter for IExecutionResult covers data, errors, and extensions.",
            "Native formatter for GraphQLHttpResponse keeps status, headers, and body together.",
            "Falls back to a structural formatter for any other .NET object you assert on.",
          ]}
        />
        <FormattersBlock />
      </Step>

      {/* Step 03 - Three shapes, one API */}
      <Step id="shapes" numeral="03">
        <Eyebrow>Inline, file, Markdown</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          Three snapshot shapes, one assertion API.
        </h2>
        <p className="text-cc-prose mt-4 max-w-2xl text-lg leading-relaxed">
          Small assertions go inline so the expected output sits beside the
          test. Larger payloads land in a snapshot file next to the test. When a
          single test exercises several layers (request, response, projected
          events, audit log), MatchMarkdownSnapshot composes them into a single
          readable document instead of a bag of unrelated assertions.
        </p>
        <BulletList
          items={[
            "MatchInlineSnapshot keeps tiny assertions self-contained.",
            "MatchSnapshot writes to a snapshot file next to your test.",
            "MatchMarkdownSnapshot captures several shapes of state in one document.",
          ]}
        />
        <ShapesBlock />
      </Step>

      {/* Step 04 - The __mismatch__ workflow */}
      <Step id="mismatch" numeral="04">
        <Eyebrow>Update workflow</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          A __mismatch__ folder turns failing snapshots into a code review.
        </h2>
        <p className="text-cc-prose mt-4 max-w-2xl text-lg leading-relaxed">
          When a snapshot test fails, Cookie Crumble writes the actual output
          into a __mismatch__/ folder next to the test. The folder is
          gitignored, so the failing artefact never sneaks into a commit by
          accident. Diff it against the committed snapshot, decide whether the
          change is intentional, and move it into place when you accept it.
        </p>
        <BulletList
          items={[
            "Failing snapshots land in __mismatch__/, never on top of the committed file.",
            "The folder is meant to be gitignored, so nothing accidental gets checked in.",
            "Updates become a deliberate review step, not a silent overwrite.",
          ]}
        />
        <MismatchFlow />
      </Step>

      {/* Step 05 - Runs in every .NET test runner */}
      <Step id="frameworks" numeral="05">
        <Eyebrow>Test framework</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          Drops into the .NET test runner you already use.
        </h2>
        <p className="text-cc-prose mt-4 max-w-2xl text-lg leading-relaxed">
          The same MatchSnapshot, MatchInlineSnapshot, and MatchMarkdownSnapshot
          APIs work on top of xUnit, NUnit, TUnit, and MSTest. Cookie Crumble
          figures out the current test&apos;s name and namespace from the
          runner, names the snapshot file accordingly, and surfaces failures
          through the runner&apos;s normal channel.
        </p>
        <FrameworkGrid />
      </Step>

      {/* Step 06 - Dogfooded by the platform */}
      <Step id="dogfood" numeral="06">
        <Eyebrow>Dogfooded by the platform</Eyebrow>
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          Built so the team can test Hot Chocolate, Fusion, and Mocha.
        </h2>
        <p className="text-cc-prose mt-4 max-w-2xl text-lg leading-relaxed">
          Cookie Crumble exists because the ChilliCream platform needed snapshot
          assertions that understand GraphQL. It backs the test suites for Hot
          Chocolate, Fusion, and Mocha, so every commit through those products
          exercises Cookie Crumble itself. Pick it up for your own .NET tests
          and you inherit that pressure.
        </p>
        <DogfoodList />
      </Step>

      {/* Closing CTA. Spine still visible, one brand-spectrum hairline. */}
      <section className="relative pt-20 pb-24 sm:pt-24 sm:pb-32">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="grid grid-cols-[5.5rem_1fr] gap-x-6 sm:grid-cols-[7rem_1fr] sm:gap-x-10 lg:grid-cols-[8.5rem_1fr] lg:gap-x-14">
          <div className="relative">
            <span
              aria-hidden
              className="border-cc-card-border bg-cc-bg absolute top-2 left-[-7px] block h-3.5 w-3.5 rounded-full border"
            />
            <span
              className="text-cc-accent font-heading block text-[64px] leading-none font-semibold tabular-nums sm:text-[88px] lg:text-[120px]"
              aria-hidden
            >
              07
            </span>
          </div>
          <div className="min-w-0 pt-1 sm:pt-2 lg:pt-3">
            <Eyebrow>Get started</Eyebrow>
            <p className="text-cc-heading font-heading mt-5 max-w-2xl text-4xl leading-[1.1] font-semibold tracking-tight text-balance sm:text-5xl">
              Write the assertion. Read the GraphQL.
            </p>
            <p className="text-cc-prose mt-5 max-w-2xl text-lg leading-relaxed">
              Add the Cookie Crumble package to your test project, call
              MatchSnapshot on an IExecutionResult or a GraphQLHttpResponse, and
              the next pull request diff reads like the API contract instead of
              a wall of property assertions.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
