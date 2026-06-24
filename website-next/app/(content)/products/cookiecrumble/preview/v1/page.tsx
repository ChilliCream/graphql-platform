import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CookieCrumble } from "@/src/icons/CookieCrumble";

export const metadata: Metadata = {
  title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
  description:
    "Cookie Crumble is the open-source snapshot testing library for .NET with native formatters for Hot Chocolate IExecutionResult and GraphQLHttpResponse.",
  keywords: [
    "Cookie Crumble",
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
    title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
    description:
      "Snapshot testing with native formatters for IExecutionResult and GraphQLHttpResponse. Inline, file, and Markdown snapshots. MIT-licensed.",
    type: "website",
  },
};

// Brand spectrum hairline, used at most once per screen, on the closing CTA.
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
// Code primitives. The hero pairs a real-feel C# test snippet on the left with
// the snapshot output it produces on the right. GitHub-dark token colors are
// scoped to these blocks; the rest of the page stays on cc-* tokens.
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

// The hero left card: an xUnit test using Cookie Crumble to snapshot a
// Hot Chocolate IExecutionResult. This is the one place we name the actual
// API tokens (Snapshot.Create, MatchSnapshot) the page is trying to teach.
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

// The hero right card: the snapshot the test above produces. Plain GraphQL
// response shape, formatted by Cookie Crumble's IExecutionResult formatter.
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
// Alternating feature row, same shape as the rest of the product pages.
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
// Inline visuals for the feature rows. Each one is a compact code card or SVG
// that shows the relevant API in action.
// -----------------------------------------------------------------------------

// Row 01: GraphQL-aware formatters. Two side-by-side mini snippets, one for
// IExecutionResult, one for GraphQLHttpResponse, both calling MatchSnapshot.
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
        Both types ship with native formatters, so the snapshot reads like the
        GraphQL response itself, not like a serialized object graph.
      </p>
    </div>
  );
}

// Row 02: Three snapshot flavors stacked.
function SnapshotFlavorsVisual() {
  return (
    <div className="flex flex-col gap-3">
      <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
          <span>Inline</span>
          <span className="text-cc-accent">MatchInlineSnapshot</span>
        </div>
        <div className="py-3">
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
        </div>
      </div>
      <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
          <span>File</span>
          <span className="text-cc-accent">MatchSnapshot</span>
        </div>
        <div className="py-3">
          <CodeLine n={1}>
            <span style={C.plain}>result</span>
            <span style={C.punct}>.</span>
            <span style={C.fn}>MatchSnapshot</span>
            <span style={C.punct}>();</span>{" "}
            <span style={C.comment}>{`// __snapshots__/<test>.snap`}</span>
          </CodeLine>
        </div>
      </div>
      <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
          <span>Markdown</span>
          <span className="text-cc-accent">MatchMarkdownSnapshot</span>
        </div>
        <div className="py-3">
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
        </div>
      </div>
    </div>
  );
}

// Row 03: The __mismatch__ update workflow, as a small file tree plus a
// labelled flow arrow.
function MismatchWorkflowVisual() {
  return (
    <svg
      viewBox="0 0 480 240"
      className="h-auto w-full"
      role="img"
      aria-label="Failing tests write to a __mismatch__ folder, diff against the committed snapshot, then move into place when accepted"
    >
      <defs>
        <linearGradient id="cc-mismatch-line" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.1" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
        </linearGradient>
      </defs>
      {/* Failing test box */}
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
      {/* Mismatch folder */}
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
      {/* Arrow from failing test into mismatch */}
      <path
        d="M 92 88 L 92 132"
        stroke="rgba(240,120,106,0.55)"
        strokeWidth="1.5"
        fill="none"
      />
      <polygon points="88,128 92,140 96,128" fill="rgba(240,120,106,0.7)" />
      {/* Accept step */}
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
      {/* Connector */}
      <path
        d="M 212 170 C 250 170, 250 120, 276 120"
        stroke="url(#cc-mismatch-line)"
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
  );
}

// Row 04: Test framework matrix.
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
          The assertion API is the same in every framework. Add the Cookie
          Crumble package, call MatchSnapshot, and the failure message points at
          the diff your runner of choice already knows how to surface.
        </p>
      </div>
    </div>
  );
}

// Row 05: Dogfooded by the platform: three pill rows pointing at the products
// that use Cookie Crumble for their own tests, with the Cookie Crumble drink
// glyph as the source.
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
          Every product on the ChilliCream platform writes its assertions with
          Cookie Crumble. The library evolves under real production pressure.
        </li>
      </ul>
    </div>
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
// Page
// -----------------------------------------------------------------------------

export default function CookieCrumblePreviewV1() {
  return (
    <>
      {/* HERO: copy left, code+snapshot pair right. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>Snapshot testing for .NET</Eyebrow>
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

      {/* Capabilities strip. */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
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

      {/* Feature rows. */}
      <FeatureRow
        id="formatters"
        index="01"
        eyebrow="GraphQL-aware formatters"
        title="The snapshot reads like the GraphQL response, not a dump."
        body="Cookie Crumble ships first-class formatters for Hot Chocolate's IExecutionResult and for GraphQLHttpResponse. Pass either type to MatchSnapshot and the snapshot file comes out as the request and the response, in a shape your reviewers can read. No custom serializers, no opt-in attributes."
        bullets={[
          "Native formatter for IExecutionResult covers data, errors, and extensions.",
          "Native formatter for GraphQLHttpResponse keeps status, headers, and body together.",
          "Falls back to a structural formatter for any other .NET object you assert on.",
        ]}
        visual={<FormattersVisual />}
      />

      <FeatureRow
        id="flavors"
        index="02"
        eyebrow="Inline, file, Markdown"
        title="Three snapshot shapes, one assertion API."
        body="Small assertions go inline so the expected output sits beside the test. Larger payloads land in a snapshot file next to the test. When a single test exercises several layers (request, response, projected events, audit log), MatchMarkdownSnapshot composes them into a single readable document instead of a bag of unrelated assertions."
        bullets={[
          "MatchInlineSnapshot keeps tiny assertions self-contained.",
          "MatchSnapshot writes to a snapshot file next to your test.",
          "MatchMarkdownSnapshot captures several shapes of state in one document.",
        ]}
        visual={<SnapshotFlavorsVisual />}
        reverse
      />

      <FeatureRow
        id="mismatch"
        index="03"
        eyebrow="Update workflow"
        title="A __mismatch__ folder turns failing snapshots into a code review."
        body="When a snapshot test fails, Cookie Crumble writes the actual output into a __mismatch__/ folder next to the test. The folder is gitignored, so the failing artefact never sneaks into a commit by accident. Diff it against the committed snapshot, decide whether the change is intentional, and move it into place when you accept it."
        bullets={[
          "Failing snapshots land in __mismatch__/, never on top of the committed file.",
          "The folder is meant to be gitignored, so nothing accidental gets checked in.",
          "Updates become a deliberate review step, not a silent overwrite.",
        ]}
        visual={<MismatchWorkflowVisual />}
      />

      <FeatureRow
        id="frameworks"
        index="04"
        eyebrow="Test framework"
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

      <FeatureRow
        id="dogfood"
        index="05"
        eyebrow="Dogfooded by the platform"
        title="Built so the team can test Hot Chocolate, Fusion, and Mocha."
        body="Cookie Crumble exists because the ChilliCream platform needed snapshot assertions that understand GraphQL. It backs the test suites for Hot Chocolate, Fusion, and Mocha, so every commit through those products exercises Cookie Crumble itself. Pick it up for your own .NET tests and you inherit that pressure."
        bullets={[
          "Used end-to-end across the ChilliCream platform's own test suites.",
          "Every Hot Chocolate, Fusion, and Mocha commit re-exercises Cookie Crumble.",
          "Equally useful for any .NET test that benefits from snapshots.",
        ]}
        visual={<DogfoodedVisual />}
      />

      {/* MIT / open source band. */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>MIT licensed</Eyebrow>
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
          <Eyebrow>Get started</Eyebrow>
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
