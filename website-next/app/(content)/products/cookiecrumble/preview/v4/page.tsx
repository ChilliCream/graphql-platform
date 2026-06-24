import type { Metadata } from "next";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CookieCrumble } from "@/src/icons/CookieCrumble";

export const metadata: Metadata = {
  title: "Cookie Crumble: GraphQL snapshot testing for .NET",
  description:
    "GraphQL snapshot testing .NET library. Cookie Crumble ships native formatters for Hot Chocolate IExecutionResult and GraphQLHttpResponse, MIT licensed.",
  keywords: [
    "GraphQL snapshot testing .NET",
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
    title: "Cookie Crumble: GraphQL snapshot testing for .NET",
    description:
      "Field notes on GraphQL-aware snapshot testing for .NET. Inline, file, and Markdown snapshots for IExecutionResult and GraphQLHttpResponse.",
    type: "website",
  },
};

// Single brand spectrum hairline, used once on the closing CTA bottom edge.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Editorial primitives.
// -----------------------------------------------------------------------------

interface DatelineProps {
  readonly children: ReactNode;
}

function Dateline({ children }: DatelineProps) {
  return (
    <p className="text-cc-ink-dim text-caption font-mono tracking-[0.2em] uppercase">
      {children}
    </p>
  );
}

interface SectionRuleProps {
  readonly numeral: string;
  readonly title: string;
  readonly id: string;
}

// A thin hairline that spans the column width, with a hanging Roman numeral
// in the left margin on lg screens. The numeral collapses inline below lg.
function SectionRule({ numeral, title, id }: SectionRuleProps) {
  return (
    <header id={id} className="relative scroll-mt-24 pt-16 sm:pt-20">
      <div className="border-cc-card-border border-t pt-6">
        <span
          aria-hidden
          className="text-cc-ink-dim text-caption font-mono tracking-[0.2em] uppercase lg:absolute lg:top-[1.65rem] lg:-left-24"
        >
          {numeral}.
        </span>
        <h2 className="text-cc-heading font-heading text-h2 font-semibold tracking-tight text-balance">
          {title}
        </h2>
      </div>
    </header>
  );
}

interface ProseProps {
  readonly children: ReactNode;
}

function Prose({ children }: ProseProps) {
  return (
    <p className="text-cc-prose text-body mt-6 leading-[1.75]">{children}</p>
  );
}

interface SubheadProps {
  readonly children: ReactNode;
}

function Subhead({ children }: SubheadProps) {
  return (
    <h3 className="text-cc-heading font-heading text-h4 border-cc-card-border mt-10 border-t pt-5 font-semibold tracking-tight">
      {children}
    </h3>
  );
}

interface CaptionProps {
  readonly children: ReactNode;
}

function Caption({ children }: CaptionProps) {
  return (
    <p className="text-cc-ink-dim text-caption mt-3 font-mono tracking-[0.2em] uppercase">
      {children}
    </p>
  );
}

interface PullQuoteProps {
  readonly children: ReactNode;
}

function PullQuote({ children }: PullQuoteProps) {
  return (
    <blockquote className="border-cc-accent text-cc-ink font-heading text-h3 mt-10 border-l-2 pl-6 leading-snug italic">
      {children}
    </blockquote>
  );
}

// -----------------------------------------------------------------------------
// Code primitives. GitHub-dark token palette, scoped to code blocks only.
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
}

function CodeCard({
  filename,
  lang,
  children,
  footerLeft,
  footerRight,
}: CodeCardProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-xl border">
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

// The hero left card: the canonical xUnit test that produces the snapshot.
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

// The snapshot the test above produces.
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

// Small inline code blocks for the three snapshot shapes.
function InlineSnapshotBlock() {
  return (
    <CodeCard filename="PingTests.cs" lang="C#">
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
    </CodeCard>
  );
}

function FileSnapshotBlock() {
  return (
    <CodeCard filename="ProductTests.cs" lang="C#">
      <CodeLine n={1}>
        <span style={C.plain}>result</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>MatchSnapshot</span>
        <span style={C.punct}>();</span>{" "}
        <span style={C.comment}>{`// __snapshots__/<test>.snap`}</span>
      </CodeLine>
    </CodeCard>
  );
}

function MarkdownSnapshotBlock() {
  return (
    <CodeCard filename="OrderFlowTests.cs" lang="C#">
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
    </CodeCard>
  );
}

// Hairline-only restyle of the v1 mismatch diagram. No filled rect bodies,
// only thin cc-card-border strokes and a single cc-accent connector.
function MismatchDiagram() {
  return (
    <svg
      viewBox="0 0 380 180"
      className="mx-auto h-auto w-full max-w-[380px]"
      role="img"
      aria-label="A failing test writes to a __mismatch__ folder, you review the diff, then move it into __snapshots__"
    >
      <g
        fill="none"
        stroke="rgba(245,241,234,0.32)"
        strokeWidth="1"
        strokeLinecap="square"
      >
        <rect x="10" y="22" width="120" height="44" rx="2" />
        <rect x="130" y="68" width="120" height="44" rx="2" />
        <rect x="250" y="22" width="120" height="44" rx="2" />
      </g>
      <text
        x="70"
        y="40"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#f5f0ea"
      >
        TEST RUN
      </text>
      <text
        x="70"
        y="56"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill="rgba(245,241,234,0.55)"
      >
        actual differs
      </text>
      <text
        x="190"
        y="86"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#f5f0ea"
      >
        __mismatch__/
      </text>
      <text
        x="190"
        y="102"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill="rgba(245,241,234,0.55)"
      >
        gitignored
      </text>
      <text
        x="310"
        y="40"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#f5f0ea"
      >
        __snapshots__/
      </text>
      <text
        x="310"
        y="56"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill="rgba(245,241,234,0.55)"
      >
        committed
      </text>
      <g fill="none" stroke="#5eead4" strokeWidth="1">
        <path d="M 70 66 L 70 90 L 130 90" />
        <path d="M 250 90 L 310 90 L 310 66" />
      </g>
      <polygon points="128,86 136,90 128,94" fill="#5eead4" />
      <polygon points="306,70 310,62 314,70" fill="#5eead4" />
      <text
        x="100"
        y="84"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill="rgba(245,241,234,0.55)"
      >
        write
      </text>
      <text
        x="260"
        y="84"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill="rgba(245,241,234,0.55)"
      >
        accept
      </text>
      <text
        x="190"
        y="140"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill="rgba(245,241,234,0.55)"
      >
        diff is reviewed before it lands
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Page.
// -----------------------------------------------------------------------------

interface RunnerRowProps {
  readonly name: string;
  readonly attribute: string;
}

function RunnerRow({ name, attribute }: RunnerRowProps) {
  return (
    <li className="border-cc-card-border flex items-baseline justify-between border-t py-3 last:border-b">
      <span className="text-cc-heading font-heading text-base font-semibold">
        {name}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11.5px] tracking-[0.18em] uppercase">
        {attribute}
      </span>
    </li>
  );
}

export default function CookieCrumblePreviewV4() {
  return (
    <article className="mx-auto w-full max-w-[680px] px-5 pt-[28vh] pb-24 sm:pt-[30vh]">
      {/* HERO MASTHEAD: single oversized headline, dateline, two buttons. */}
      <header>
        <p className="text-cc-accent text-caption font-mono tracking-[0.22em] uppercase">
          Cookie Crumble . Field Notes vol. 1
        </p>
        <h1 className="text-cc-heading font-heading text-hero mt-6 font-semibold tracking-tight text-balance">
          Snapshot testing that understands GraphQL.
        </h1>
        <div className="mt-10">
          <Dateline>MIT . .NET 8+ . xUnit, NUnit, TUnit, MSTest</Dateline>
        </div>
        <div className="mt-6 flex flex-wrap gap-3">
          <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>
      </header>

      {/* I. Lead essay with drop cap. */}
      <SectionRule
        id="lede"
        numeral="I"
        title="The diff should read like the API contract."
      />
      <p className="text-cc-prose text-lead mt-8 leading-[1.7]">
        <span
          className="text-cc-accent font-heading float-left mr-3 leading-[0.85] font-semibold"
          style={{ fontSize: "5.5rem" }}
        >
          G
        </span>
        raphQL responses are not ordinary .NET object graphs, and the assertions
        that prove them right should not pretend otherwise. Cookie Crumble is
        the open-source snapshot library the ChilliCream team writes its own
        tests with. It ships native formatters for Hot Chocolate
        IExecutionResult and GraphQLHttpResponse, so the snapshot file lands on
        disk as the request and the response in a shape your reviewers can read.
        No custom serializers, no opt-in attributes, no bag of property
        comparisons. The point is not that the assertion passes. The point is
        that a year from now, somebody scanning the diff in a pull request can
        tell, at a glance, exactly which field of the API changed.
      </p>
      <PullQuote>The diff should read like the API contract.</PullQuote>

      {/* II. The formatters, with a column breakout. */}
      <SectionRule
        id="formatters"
        numeral="II"
        title="GraphQL-aware formatters."
      />
      <Prose>
        Pass an IExecutionResult or a GraphQLHttpResponse to MatchSnapshot and
        Cookie Crumble routes the value through a formatter that knows what it
        is looking at. The result is not a serialized object graph. It is the
        GraphQL response itself, with data, errors, and extensions laid out the
        way the API speaks them on the wire.
      </Prose>
      <Prose>
        For tests that exercise the server in-process, IExecutionResult carries
        everything the engine produced. For tests that go over HTTP, the
        GraphQLHttpResponse formatter keeps the status, headers, and body
        together as a single artefact. Any other .NET object falls back to a
        structural formatter, so the rest of your assertions stay legible too.
      </Prose>

      {/* Breakout to max-w-[880px] for the paired code cards. */}
      <div className="not-prose relative mx-auto mt-10 w-full max-w-[880px] lg:-mx-[100px] lg:w-[calc(100%+200px)] lg:max-w-none">
        <div className="flex flex-col gap-6">
          <div>
            <HeroTestCard />
            <Caption>
              Fig. 1 . An xUnit test that asserts on an IExecutionResult.
            </Caption>
          </div>
          <div>
            <HeroSnapshotCard />
            <Caption>
              Fig. 2 . The snapshot that test produces, committed to git.
            </Caption>
          </div>
        </div>
      </div>

      {/* III. Three shapes, one API. */}
      <SectionRule id="shapes" numeral="III" title="Inline, file, Markdown." />
      <Prose>
        Three snapshot shapes, one assertion API. Use the smallest one that
        still tells the truth. Inline keeps tiny expectations next to the code
        that produced them. File takes payloads that are too large for the test
        body and parks them next to the test on disk. Markdown takes a single
        scenario that touches several layers and stitches them into one readable
        document.
      </Prose>
      <Prose>
        The choice is editorial, not technical. The runner does not care which
        you pick, and the API surface is the same set of method calls. You are
        deciding what future readers should see when they open the file.
      </Prose>

      <Subhead>Inline</Subhead>
      <p className="text-cc-prose text-body mt-4 leading-[1.75]">
        For one-line expectations that stay legible at a glance. The expected
        value lives in the test source.
      </p>
      <div className="mt-5">
        <InlineSnapshotBlock />
      </div>

      <Subhead>File</Subhead>
      <p className="text-cc-prose text-body mt-4 leading-[1.75]">
        For payloads that grow beyond a few lines. The snapshot file sits next
        to the test, named after the test method.
      </p>
      <div className="mt-5">
        <FileSnapshotBlock />
      </div>

      <Subhead>Markdown</Subhead>
      <p className="text-cc-prose text-body mt-4 leading-[1.75]">
        For scenarios that touch the request, the response, and a projection in
        the same act. Each section is labelled, fenced, and reviewable as a
        single document.
      </p>
      <div className="mt-5">
        <MarkdownSnapshotBlock />
      </div>

      {/* IV. The __mismatch__ workflow. */}
      <SectionRule
        id="mismatch"
        numeral="IV"
        title="The __mismatch__ workflow."
      />
      <Prose>
        When a snapshot test fails, Cookie Crumble does not overwrite the
        committed file. It writes the actual output into a sibling folder called
        __mismatch__. The folder is meant to be gitignored, so the failing
        artefact never sneaks into a commit by accident. You diff it against the
        committed snapshot, decide whether the change is intended, and only then
        move it into place.
      </Prose>
      <Prose>
        The point of the indirection is that an updated snapshot is a code
        review decision, not a side effect of running the suite. The act of
        accepting a snapshot is the same kind of act as merging a pull request.
        You looked at the diff. You agreed with it. You moved it in.
      </Prose>
      <div className="mt-10">
        <MismatchDiagram />
        <Caption>
          Fig. 3 . The deliberate path from a failing run to a committed
          snapshot.
        </Caption>
      </div>

      {/* V. Runner-agnostic. */}
      <SectionRule
        id="runners"
        numeral="V"
        title="It drops into your runner."
      />
      <Prose>
        The same MatchSnapshot, MatchInlineSnapshot, and MatchMarkdownSnapshot
        APIs work on top of xUnit, NUnit, TUnit, and MSTest. Cookie Crumble
        reads the current test&apos;s name and namespace from the runner, names
        the snapshot file accordingly, and surfaces failures through the channel
        your runner already uses. There is no separate report to wire up, no
        per-runner adapter to pick.
      </Prose>
      <ul className="mt-8">
        <RunnerRow name="xUnit" attribute="[Fact] / [Theory]" />
        <RunnerRow name="NUnit" attribute="[Test]" />
        <RunnerRow name="TUnit" attribute="[Test]" />
        <RunnerRow name="MSTest" attribute="[TestMethod]" />
      </ul>

      {/* VI. Dogfooded. */}
      <SectionRule
        id="dogfood"
        numeral="VI"
        title="Dogfooded by Hot Chocolate, Fusion, and Mocha."
      />
      <Prose>
        Cookie Crumble exists because the ChilliCream platform needed snapshot
        assertions that understand GraphQL. It backs the test suites for{" "}
        <span className="text-cc-heading font-heading inline-flex items-baseline gap-1.5 font-semibold">
          <CookieCrumble
            className="inline-block h-4 w-auto translate-y-[2px]"
            aria-hidden
          />
          Hot Chocolate
        </span>
        ,{" "}
        <span className="text-cc-heading font-heading font-semibold">
          Fusion
        </span>
        , and{" "}
        <span className="text-cc-heading font-heading font-semibold">
          Mocha
        </span>
        , so every commit through those products re-exercises Cookie Crumble
        itself. Pick it up for your own .NET tests and you inherit that
        pressure. The library evolves under the same load that runs the
        platform.
      </Prose>

      {/* Colophon dateline. */}
      <section
        aria-label="Colophon"
        className="border-cc-card-border mt-20 border-t pt-6"
      >
        <Dateline>
          License . MIT . Package . CookieCrumble . Runtimes . .NET 8+
        </Dateline>
        <div className="mt-2">
          <Dateline>
            Formatters . GraphQL-aware . Workflow . __mismatch__/
          </Dateline>
        </div>
      </section>

      {/* Closing CTA. The single brand-spectrum hairline lives here. */}
      <section
        aria-label="Get started"
        className="border-cc-card-border relative mt-20 border-t pt-16 pb-12 text-center"
      >
        <h2 className="text-cc-heading font-heading text-h3 mx-auto max-w-[28ch] font-semibold tracking-tight text-balance">
          Write the assertion. Read the GraphQL.
        </h2>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 bottom-0 h-px"
          style={{ background: SPECTRUM }}
        />
      </section>
    </article>
  );
}
