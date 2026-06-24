import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { CookieCrumble } from "@/src/icons/CookieCrumble";
import { Espresso } from "@/src/icons/Espresso";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "Cookie Crumble: GraphQL snapshot testing for .NET",
  description:
    "Cookie Crumble is the open-source GraphQL snapshot testing .NET library with native formatters for IExecutionResult and GraphQLHttpResponse. MIT-licensed.",
  keywords: [
    "Cookie Crumble",
    "GraphQL snapshot testing .NET",
    "snapshot testing",
    ".NET testing",
    "GraphQL testing",
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
    title: "Cookie Crumble: GraphQL snapshot testing for .NET",
    description:
      "GraphQL snapshot testing .NET with native formatters for IExecutionResult and GraphQLHttpResponse. Inline, file, and Markdown snapshots. MIT-licensed.",
    type: "website",
  },
};

// The brand-spectrum hairline is used at most once per page, on the closing CTA.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Small primitives shared across the page.
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
// Code primitives (shared with v1, so the canonical hero pair reads identically).
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

const C = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  comment: { color: "#8b949e", fontStyle: "italic" as const },
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
  readonly accent?: boolean;
}

function CodeCard({
  filename,
  lang,
  children,
  footerLeft,
  footerRight,
  accent = false,
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
        <span className="text-cc-ink-dim ml-3 font-mono text-[11px]">
          {filename}
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {lang}
        </span>
      </div>
      {accent ? (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-70"
          style={{
            background:
              "radial-gradient(420px 180px at 14% 22%, rgba(94, 234, 212, 0.18), transparent 70%), radial-gradient(280px 140px at 8% 16%, rgba(22, 185, 228, 0.16), transparent 70%)",
          }}
        />
      ) : null}
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

// Hero left card: xUnit test using Cookie Crumble to snapshot an IExecutionResult.
function HeroTestCard() {
  return (
    <CodeCard
      filename="Catalog.Tests/ProductQueryTests.cs"
      lang="C#"
      footerLeft="xUnit + Cookie Crumble"
      footerRight="MatchSnapshot()"
      accent
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

// Hero right card: the snapshot the test above produces.
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
// Formatters row (01): two stacked mini code cards. No coffee chrome here.
// -----------------------------------------------------------------------------

function FormattersVisual() {
  return (
    <div className="flex flex-col gap-4">
      <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
          <span>IExecutionResult</span>
          <span className="text-cc-accent">in-process execution</span>
        </div>
        <div className="py-3">
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
        </div>
      </div>
      <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
          <span>GraphQLHttpResponse</span>
          <span className="text-cc-accent">over HTTP</span>
        </div>
        <div className="py-3">
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
        </div>
      </div>
      <p className="text-cc-ink-dim text-[12px] leading-relaxed">
        Grind matched to the brew. IExecutionResult and GraphQLHttpResponse each
        get their own formatter, so the output reads the way you would actually
        serve it.
      </p>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Tasting Notes centerpiece (02): three cups on a tray. The single section
// where coffee chrome lives. cc-accent for icon glyphs, cc-card-border tiles.
// -----------------------------------------------------------------------------

interface TastingCupProps {
  readonly brew: string;
  readonly flavor: string;
  readonly api: string;
  readonly note: string;
  readonly icon: ReactNode;
  readonly snippet: ReactNode;
}

function TastingCup({
  brew,
  flavor,
  api,
  note,
  icon,
  snippet,
}: TastingCupProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-xl border p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="flex flex-col">
          <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.18em] uppercase">
            {brew}
          </span>
          <span className="text-cc-heading font-heading mt-1 text-lg font-semibold tracking-tight">
            {flavor}
          </span>
        </div>
        <span className="text-cc-accent shrink-0" aria-hidden>
          {icon}
        </span>
      </div>
      <div className="bg-cc-code-bg border-cc-card-border mt-4 overflow-hidden rounded-lg border">
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-3 py-1.5 font-mono text-[10.5px]">
          <span>Tasting card</span>
          <span className="text-cc-accent">{api}</span>
        </div>
        <div className="px-3 py-3">{snippet}</div>
      </div>
      <p className="text-cc-ink-dim mt-4 text-[12.5px] leading-relaxed">
        {note}
      </p>
    </div>
  );
}

function TastingFlightVisual() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 sm:p-6">
      <div className="border-cc-card-border flex items-center gap-3 border-b pb-4">
        <span className="text-cc-accent" aria-hidden>
          <CoffeeTray className="h-7 w-auto" />
        </span>
        <div className="flex flex-col">
          <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.18em] uppercase">
            Today&apos;s tray
          </span>
          <span className="text-cc-heading font-heading text-base font-semibold">
            Three cups, one assertion API.
          </span>
        </div>
      </div>
      <div className="mt-6 grid gap-4 md:grid-cols-3">
        <TastingCup
          brew="Espresso shot"
          flavor="Inline"
          api="MatchInlineSnapshot"
          icon={<Espresso className="h-8 w-auto" />}
          note="The expected output sits next to the test. Reach for it when the assertion is tiny and you want it in one read."
          snippet={
            <>
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
            </>
          }
        />
        <TastingCup
          brew="Pour-over"
          flavor="File"
          api="MatchSnapshot"
          icon={<PourOver className="h-8 w-auto" />}
          note="A snapshot file lands next to the test. Reach for it when the response is bigger than a glance and you want the diff in source control."
          snippet={
            <>
              <CodeLine n={1}>
                <span style={C.plain}>result</span>
                <span style={C.punct}>.</span>
                <span style={C.fn}>MatchSnapshot</span>
                <span style={C.punct}>();</span>
              </CodeLine>
              <CodeLine n={2}>
                <span style={C.comment}>{`// __snapshots__/<test>.snap`}</span>
              </CodeLine>
            </>
          }
        />
        <TastingCup
          brew="Cupping flight"
          flavor="Markdown"
          api="MatchMarkdownSnapshot"
          icon={<CoffeeTray className="h-8 w-auto" />}
          note="Several shapes of state captured side by side in one document. Reach for it when one test exercises request, response, and audit log together."
          snippet={
            <>
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
                <span style={C.fn}>MatchMarkdownSnapshot</span>
                <span style={C.punct}>();</span>
              </CodeLine>
            </>
          }
        />
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Quality control row (03): the __mismatch__ workflow SVG, lightly reworded.
// -----------------------------------------------------------------------------

function MismatchWorkflowVisual() {
  return (
    <svg
      viewBox="0 0 480 240"
      className="h-auto w-full"
      role="img"
      aria-label="Failing snapshots write to __mismatch__, are reviewed as a diff, then move into __snapshots__ when accepted"
    >
      <defs>
        <linearGradient id="cc-mismatch-line-v6" x1="0" x2="1" y1="0" y2="0">
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
        Cup tastes off
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
        width="220"
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
        __mismatch__/ taste-test tray
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
        Pour it into the canon
      </text>
      <text
        x="366"
        y="132"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        move into __snapshots__/
      </text>
      <path
        d="M 232 170 C 256 170, 256 120, 276 120"
        stroke="url(#cc-mismatch-line-v6)"
        strokeWidth="1.5"
        fill="none"
      />
      <polygon points="272,116 280,120 272,124" fill="rgba(94,234,212,0.7)" />
      <text
        x="244"
        y="158"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        review diff
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Framework matrix (04): same as v1, the copy carries any coffee voice.
// -----------------------------------------------------------------------------

function FrameworkMatrixVisual() {
  const frameworks = [
    { name: "xUnit", note: "[Fact] / [Theory]" },
    { name: "NUnit", note: "[Test]" },
    { name: "TUnit", note: "[Test]" },
    { name: "MSTest", note: "[TestMethod]" },
  ];
  return (
    <div className="grid grid-cols-2 gap-3">
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
          Same MatchSnapshot, same failure surface, in whichever runner your CI
          already speaks.
        </p>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// House blend / dogfood (05).
// -----------------------------------------------------------------------------

function DogfoodedVisual() {
  const products = [
    { name: "Hot Chocolate", role: "GraphQL server" },
    { name: "Fusion", role: "Federation gateway" },
    { name: "Mocha", role: "Distributed messaging" },
  ];
  return (
    <div className="grid items-center gap-6 sm:grid-cols-[auto_1fr]">
      <div className="bg-cc-surface/40 border-cc-card-border flex h-32 w-32 items-center justify-center rounded-xl border">
        <CookieCrumble className="h-24 w-auto" />
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
        <li className="text-cc-ink-dim text-[12.5px] leading-relaxed">
          Every commit through those products is another pull against Cookie
          Crumble itself.
        </li>
      </ul>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Feature row layout.
// -----------------------------------------------------------------------------

interface FeatureRowProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

function FeatureRow({
  id,
  index,
  eyebrow,
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
// Proof tile for the MIT band.
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
// Page.
// -----------------------------------------------------------------------------

export default function CookieCrumblePreviewV6() {
  return (
    <>
      {/* HERO: copy left, code+snapshot pair right. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>On the menu / Snapshot testing for .NET</Eyebrow>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Snapshot testing that understands GraphQL.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Cookie Crumble is the open-source snapshot library the ChilliCream
              team writes its own tests with. It ships native formatters for Hot
              Chocolate IExecutionResult and GraphQLHttpResponse, so the
              snapshot file reads like the GraphQL response itself. Inline,
              file, or Markdown. xUnit, NUnit, TUnit, or MSTest. MIT-licensed.
            </p>
            <p className="text-cc-ink-dim mt-4 max-w-xl text-base leading-relaxed">
              We taste every cup against the notes we already approved.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
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
          </div>
          <div className="lg:col-span-7">
            <div className="grid gap-4 lg:grid-cols-2">
              <HeroTestCard />
              <HeroSnapshotCard />
            </div>
          </div>
        </div>
      </section>

      {/* Capabilities strip: house specials. */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <div className="mb-3">
          <Eyebrow>House specials</Eyebrow>
        </div>
        <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-5">
          {[
            "GraphQL-aware formatters",
            "Inline + file + Markdown",
            "__mismatch__ workflow",
            "xUnit, NUnit, TUnit, MSTest",
            "Dogfooded by the platform",
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

      {/* 01 Formatters: "The grind". */}
      <FeatureRow
        id="formatters"
        index="01"
        eyebrow="The grind"
        title="The snapshot reads like the GraphQL response, not a dump."
        body="Cookie Crumble ships first-class formatters for Hot Chocolate's IExecutionResult and for GraphQLHttpResponse. Pass either type to MatchSnapshot and the snapshot file comes out as the request and the response, in a shape your reviewers can read. No custom serializers, no opt-in attributes."
        bullets={[
          "Native formatter for IExecutionResult covers data, errors, and extensions.",
          "Native formatter for GraphQLHttpResponse keeps status, headers, and body together.",
          "Falls back to a structural formatter for any other .NET object you assert on.",
        ]}
        visual={<FormattersVisual />}
      />

      {/* 02 Tasting Notes centerpiece. */}
      <section
        id="tasting-notes"
        className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
      >
        <div className="grid items-end gap-8 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <div className="flex items-center gap-3">
              <IndexTag value="02" />
              <Eyebrow>A tasting flight</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Three snapshot shapes, one assertion API.
            </h2>
          </div>
          <div className="lg:col-span-5">
            <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
              A snapshot is the tasting notes for a GraphQL response. You write
              them once, then every brew (test run) is checked against the cup
              you approved. Three styles, one MatchSnapshot family.
            </p>
          </div>
        </div>
        <div className="mt-10">
          <TastingFlightVisual />
        </div>
      </section>

      {/* 03 Quality control: __mismatch__. */}
      <FeatureRow
        id="mismatch"
        index="03"
        eyebrow="Quality control on the bar"
        title="A __mismatch__ folder turns failing snapshots into a code review."
        body="When a snapshot test fails, Cookie Crumble writes the actual output into a __mismatch__/ folder next to the test. The folder is gitignored, so the failing artefact never sneaks into a commit by accident. Diff it against the committed snapshot, decide whether the change is intentional, and move it into place when you accept it."
        bullets={[
          "Failing snapshots land in __mismatch__/, never on top of the committed file.",
          "The folder is meant to be gitignored, so nothing accidental gets checked in.",
          "Updates become a deliberate review step, not a silent overwrite.",
        ]}
        visual={<MismatchWorkflowVisual />}
      />

      {/* 04 Frameworks: any bar setup. */}
      <FeatureRow
        id="frameworks"
        index="04"
        eyebrow="Any bar setup"
        title="Drops into the .NET test runner you already use."
        body="The same MatchSnapshot, MatchInlineSnapshot, and MatchMarkdownSnapshot APIs work on top of xUnit, NUnit, TUnit, and MSTest. Cookie Crumble figures out the current test's name and namespace from the runner, names the snapshot file accordingly, and surfaces failures through the runner's normal channel."
        bullets={[
          "Same assertion API across xUnit, NUnit, TUnit, and MSTest.",
          "Snapshot file names are derived from the test method and class.",
          "Failures show up as ordinary test failures in your runner, IDE, and CI logs.",
        ]}
        visual={<FrameworkMatrixVisual />}
        reverse
      />

      {/* 05 House blend / dogfood. */}
      <FeatureRow
        id="dogfood"
        index="05"
        eyebrow="House blend"
        title="Built so the team can test Hot Chocolate, Fusion, and Mocha."
        body="Cookie Crumble exists because the ChilliCream platform needed snapshot assertions that understand GraphQL. It backs the test suites for Hot Chocolate, Fusion, and Mocha, so every commit through those products exercises Cookie Crumble itself. Pick it up for your own .NET tests and you inherit that pressure."
        bullets={[
          "Used end-to-end across the ChilliCream platform's own test suites.",
          "Every Hot Chocolate, Fusion, and Mocha commit re-exercises Cookie Crumble.",
          "Equally useful for any .NET test that benefits from snapshots.",
        ]}
        visual={<DogfoodedVisual />}
      />

      {/* MIT / open source band. No coffee phrasing here. */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>On the house</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Open source, dogfooded, free to use.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Cookie Crumble is released under the MIT license and developed in
              the open alongside the rest of the ChilliCream platform. Use it in
              commercial work, fork it, vendor it, audit it. The package, the
              issue tracker, and the release notes all live on GitHub.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/cookiecrumble">
                Read the docs
              </OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Package" value="CookieCrumble" />
              <ProofItem label="Runtimes" value=".NET 8 and later" />
              <ProofItem
                label="Frameworks"
                value="xUnit + NUnit + TUnit + MSTest"
              />
              <ProofItem label="Formatters" value="GraphQL-aware" />
              <ProofItem label="Workflow" value="__mismatch__/" />
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA. The single brand-spectrum hairline lives here. */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Pull a shot</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            Write the assertion. Read the GraphQL.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            Add the Cookie Crumble package to your test project, call
            MatchSnapshot on an IExecutionResult or a GraphQLHttpResponse, and
            the next pull request diff reads like the API contract instead of a
            wall of property assertions.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
