import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CookieCrumble } from "@/src/icons/CookieCrumble";

export const metadata: Metadata = {
  title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
  description:
    "Cookie Crumble is the GraphQL-aware snapshot testing library for .NET. Inline, file, and Markdown snapshots for IExecutionResult and GraphQLHttpResponse.",
  keywords: [
    "Cookie Crumble",
    ".NET snapshot testing",
    "GraphQL testing",
    "Hot Chocolate testing",
    "MatchSnapshot",
    "MatchInlineSnapshot",
    "MatchMarkdownSnapshot",
    "xUnit",
    "NUnit",
    "TUnit",
    "MSTest",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
    description:
      "Snapshot testing built so the ChilliCream team could test Hot Chocolate, Fusion, and Mocha. Inline, file, and Markdown snapshots with GraphQL-aware formatters.",
    type: "website",
  },
};

// Brand spectrum gradient. Used exactly once on this screen, on the wedge word.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-[0.25em] uppercase">
      {children}
    </div>
  );
}

interface SectionHeaderProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly lead?: ReactNode;
  readonly align?: "left" | "center";
}

function SectionHeader({
  eyebrow,
  title,
  lead,
  align = "center",
}: SectionHeaderProps) {
  const alignment = align === "center" ? "text-center mx-auto" : "text-left";
  return (
    <div className={`max-w-3xl ${alignment}`}>
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-3 text-3xl tracking-tight sm:text-4xl">
        {title}
      </h2>
      {lead ? <p className="text-cc-ink-dim lead mt-4">{lead}</p> : null}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Hero
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="relative pt-12 pb-10 sm:pt-20 sm:pb-16">
      <div className="mx-auto max-w-4xl text-center">
        <div className="mb-6 flex justify-center">
          <span
            aria-hidden
            className="border-cc-card-border bg-cc-surface/70 inline-flex h-14 w-14 items-center justify-center rounded-2xl border backdrop-blur-sm"
          >
            <CookieCrumble className="text-cc-accent h-8 w-8" />
          </span>
        </div>
        <Eyebrow>Snapshot Testing for .NET</Eyebrow>
        <h1 className="text-cc-heading font-heading text-hero mt-5 tracking-tight">
          Built so we could test{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Hot Chocolate
          </span>
          . Made for any .NET test.
        </h1>
        <p className="text-cc-ink-dim lead mx-auto mt-6 max-w-2xl">
          Cookie Crumble is the snapshot testing library the ChilliCream team
          uses to test Hot Chocolate, Fusion, and Mocha. It speaks GraphQL
          natively, formats execution results and HTTP responses for you, and
          stays out of the way for everything else.
        </p>
        <div className="mt-9 flex flex-wrap justify-center gap-4">
          <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>
        <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase">
          <span>MIT licensed</span>
          <span aria-hidden className="text-cc-ink-faint">
            /
          </span>
          <span>xUnit, NUnit, TUnit, MSTest</span>
          <span aria-hidden className="text-cc-ink-faint">
            /
          </span>
          <span>Inline, file, Markdown</span>
          <span aria-hidden className="text-cc-ink-faint">
            /
          </span>
          <span>Dogfooded by the platform</span>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// "Why we built it" honesty band
// ---------------------------------------------------------------------------

function WhyWeBuiltIt() {
  return (
    <section className="py-12 sm:py-16">
      <div className="border-cc-card-border bg-cc-surface/70 relative overflow-hidden rounded-2xl border p-8 sm:p-10">
        <div className="grid gap-8 md:grid-cols-[1fr_2fr] md:items-start md:gap-12">
          <div>
            <Eyebrow>Why we built it</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-2xl tracking-tight sm:text-3xl">
              The .NET snapshot tools we loved did not know what GraphQL was.
            </h2>
          </div>
          <div className="text-cc-ink-dim space-y-4 text-base leading-relaxed">
            <p>
              The popular .NET snapshot libraries predate the GraphQL execution
              types we work with every day. They will happily serialize an{" "}
              <code className="text-cc-ink">IExecutionResult</code> or a{" "}
              <code className="text-cc-ink">GraphQLHttpResponse</code>, but you
              get a generic object dump, not the GraphQL document a reviewer
              actually wants to read.
            </p>
            <p>
              So we wrote Cookie Crumble for ourselves. It ships with
              first-class formatters for{" "}
              <code className="text-cc-ink">IExecutionResult</code>,{" "}
              <code className="text-cc-ink">GraphQLHttpResponse</code>,{" "}
              <code className="text-cc-ink">JsonDocument</code>, exceptions,
              HTTP responses, and plain text. The Hot Chocolate, Fusion, and
              Mocha test suites run through it. If it survives the platform, it
              will survive your test suite.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// "Use it for" 4-card grid
// ---------------------------------------------------------------------------

interface UseCardProps {
  readonly index: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

function UseCard({ index, title, body, bullets }: UseCardProps) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-2xl border p-7 backdrop-blur-sm transition-colors">
      <div className="text-cc-accent font-mono text-xs tracking-[0.3em] uppercase">
        {index}
      </div>
      <h3 className="text-cc-heading font-heading mt-3 text-xl tracking-tight">
        {title}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">{body}</p>
      <ul className="mt-5 space-y-2">
        {bullets.map((b) => (
          <li
            key={b}
            className="text-cc-ink flex items-start gap-2 text-sm leading-snug"
          >
            <span className="text-cc-accent mt-0.5 inline-flex shrink-0">
              <CheckIcon />
            </span>
            <span>{b}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

const USE_CARDS: readonly UseCardProps[] = [
  {
    index: "01",
    title: "GraphQL response snapshots",
    body: "Native formatters for IExecutionResult and GraphQLHttpResponse render a stable, deterministic shape of the response. The diff in code review reads like a GraphQL document, not a serialized object graph.",
    bullets: [
      "IExecutionResult is formatted natively",
      "GraphQLHttpResponse rendering for end-to-end tests",
      "Deterministic ordering so diffs stay reviewable",
    ],
  },
  {
    index: "02",
    title: "Markdown snapshots for multi-shape state",
    body: "Use MatchMarkdownSnapshot when one test captures several shapes of state: a request, the schema, the result, the database rows, the OpenTelemetry spans. One file, several labelled blocks, one assertion.",
    bullets: [
      "One snapshot file per scenario, not per shape",
      "Mix GraphQL, JSON, SQL, and prose in one document",
      "Self-documenting when you open the file six months later",
    ],
  },
  {
    index: "03",
    title: "Inline snapshots for one-liners",
    body: "MatchInlineSnapshot keeps the expected value in the test method, next to the assertion. Great for small results that are easier to read in the test than in a sibling file.",
    bullets: [
      "Expected value lives next to the act",
      "First run writes the value back into your source",
      "Switch to file or Markdown when the value grows",
    ],
  },
  {
    index: "04",
    title: "Any .NET test that benefits from snapshots",
    body: "Cookie Crumble is not GraphQL-only. The same MatchSnapshot pipeline handles plain objects, JSON, exception messages, and HTTP responses, so you can keep one snapshot tool across your whole .NET test base.",
    bullets: [
      "Plain object and JSON formatters in the box",
      "Exception and HttpResponseMessage formatters",
      "xUnit, NUnit, TUnit, and MSTest integrations",
    ],
  },
];

function UseSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Use it for"
        title="Four shapes of test, one snapshot pipeline"
        lead="Pick the snapshot style that matches the test. The MatchSnapshot pipeline, the formatters, and the update workflow stay the same across all four."
      />
      <div className="mt-12 grid gap-5 md:grid-cols-2">
        {USE_CARDS.map((c) => (
          <UseCard key={c.index} {...c} />
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Real-feel C# code example
// ---------------------------------------------------------------------------

// Color tokens. Kept inline so the snippet ships as static HTML with no client JS.
const TOK = {
  kw: "text-[#7c92c6]", // keyword (violet)
  type: "text-[#16b9e4]", // type / class (cyan)
  attr: "text-cc-accent", // [Fact], [Test]
  str: "text-[#f0786a]", // string (coral)
  com: "text-cc-ink-dim", // comment
  ident: "text-cc-heading",
  punct: "text-cc-ink",
};

function CodeExample() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The shape of a test"
        title="One Markdown snapshot, every shape that matters"
        lead="A single MatchMarkdownSnapshot captures the request, the schema, and the GraphQL execution result in one reviewable document. The formatters know what each block is."
      />
      <div className="mx-auto mt-10 max-w-3xl">
        <div className="border-cc-card-border bg-cc-surface/80 overflow-hidden rounded-2xl border shadow-2xl shadow-black/40 backdrop-blur-sm">
          <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
            <div className="flex items-center gap-2">
              <span className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-success/70 h-2.5 w-2.5 rounded-full" />
            </div>
            <span className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
              ProductQueriesTests.cs
            </span>
          </div>
          <pre className="overflow-x-auto p-5 font-mono text-[13px] leading-relaxed">
            <code>
              <span className={TOK.kw}>using </span>
              <span className={TOK.type}>CookieCrumble</span>
              <span className={TOK.punct}>;</span>
              {"\n"}
              <span className={TOK.kw}>using </span>
              <span className={TOK.type}>HotChocolate.Execution</span>
              <span className={TOK.punct}>;</span>
              {"\n\n"}
              <span className={TOK.kw}>public class </span>
              <span className={TOK.type}>ProductQueriesTests</span>
              {"\n"}
              <span className={TOK.punct}>{"{"}</span>
              {"\n  "}
              <span className={TOK.attr}>{"[Fact]"}</span>
              {"\n  "}
              <span className={TOK.kw}>public async </span>
              <span className={TOK.type}>Task</span>{" "}
              <span className={TOK.ident}>Query_Product_By_Id</span>
              <span className={TOK.punct}>{"()"}</span>
              {"\n  "}
              <span className={TOK.punct}>{"{"}</span>
              {"\n    "}
              <span className={TOK.com}>{"// arrange"}</span>
              {"\n    "}
              <span className={TOK.kw}>var </span>
              <span className={TOK.ident}>executor</span>{" "}
              <span className={TOK.punct}>= </span>
              <span className={TOK.kw}>await </span>
              <span className={TOK.ident}>BuildExecutorAsync</span>
              <span className={TOK.punct}>{"();"}</span>
              {"\n    "}
              <span className={TOK.kw}>const string </span>
              <span className={TOK.ident}>request</span>{" "}
              <span className={TOK.punct}>= </span>
              <span className={TOK.str}>
                {
                  '"""\n      query { product(id: 1) { id name price } }\n    """'
                }
              </span>
              <span className={TOK.punct}>;</span>
              {"\n\n    "}
              <span className={TOK.com}>{"// act"}</span>
              {"\n    "}
              <span className={TOK.type}>IExecutionResult</span>{" "}
              <span className={TOK.ident}>result</span>{" "}
              <span className={TOK.punct}>= </span>
              <span className={TOK.kw}>await </span>
              <span className={TOK.ident}>executor</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>ExecuteAsync</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>request</span>
              <span className={TOK.punct}>{");"}</span>
              {"\n\n    "}
              <span className={TOK.com}>{"// assert"}</span>
              {"\n    "}
              <span className={TOK.com}>
                {
                  "// One Markdown snapshot captures request, schema, and result."
                }
              </span>
              {"\n    "}
              <span className={TOK.ident}>Snapshot</span>
              {"\n      "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Create</span>
              <span className={TOK.punct}>{"()"}</span>
              {"\n      "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Add</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>request</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.str}>{'"Request"'}</span>
              <span className={TOK.punct}>{")"}</span>
              {"\n      "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Add</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>executor</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Schema</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.str}>{'"Schema"'}</span>
              <span className={TOK.punct}>{")"}</span>
              {"\n      "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Add</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.ident}>result</span>
              <span className={TOK.punct}>{", "}</span>
              <span className={TOK.str}>{'"Result"'}</span>
              <span className={TOK.punct}>{")"}</span>
              {"\n      "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>MatchMarkdownSnapshot</span>
              <span className={TOK.punct}>{"();"}</span>
              {"\n  "}
              <span className={TOK.punct}>{"}"}</span>
              {"\n"}
              <span className={TOK.punct}>{"}"}</span>
            </code>
          </pre>
        </div>
        <p className="text-cc-ink-dim mt-4 text-center text-sm">
          For single-shape assertions use{" "}
          <code className="text-cc-ink">result.MatchSnapshot()</code> for a file
          snapshot or{" "}
          <code className="text-cc-ink">result.MatchInlineSnapshot(...)</code>{" "}
          to keep the expected value next to the test.
        </p>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Update workflow via __mismatch__
// ---------------------------------------------------------------------------

interface WorkflowStepProps {
  readonly step: string;
  readonly title: string;
  readonly body: string;
}

function WorkflowStep({ step, title, body }: WorkflowStepProps) {
  return (
    <li className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-xl border p-6 backdrop-blur-sm">
      <div className="text-cc-accent font-mono text-xs tracking-[0.3em] uppercase">
        Step {step}
      </div>
      <h3 className="text-cc-heading font-heading mt-2 text-lg tracking-tight">
        {title}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">{body}</p>
    </li>
  );
}

function WorkflowSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The update workflow"
        title="Mismatches land on disk, not in a modal"
        lead="When a snapshot does not match, Cookie Crumble writes the new value into __mismatch__/ next to the existing snapshot. Diff it, accept it, or reject it with the tooling you already use."
      />
      <ol className="mt-12 grid gap-4 md:grid-cols-3">
        <WorkflowStep
          step="01"
          title="Run the test"
          body="Cookie Crumble compares the captured value against the committed snapshot. On match, the test passes silently. On mismatch, the actual value is written to a __mismatch__/ folder next to the snapshot file."
        />
        <WorkflowStep
          step="02"
          title="Diff the mismatch"
          body="Open your usual diff tool (Rider, Visual Studio, git, VS Code) against the existing snapshot and the new __mismatch__/ file. Review the change like any other code review, in the language the formatter emits."
        />
        <WorkflowStep
          step="03"
          title="Accept or reject"
          body="If the new value is correct, move the file from __mismatch__/ over the existing snapshot and commit. If it is wrong, fix the code, rerun, and let the mismatch evaporate. No magic, no IDE plugin required."
        />
      </ol>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Soft factual comparison with Verify
// ---------------------------------------------------------------------------

function ComparisonSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Honest comparison"
        title="Cookie Crumble and Verify, side by side"
        lead="Verify is the broad, general-purpose .NET snapshot library and it is great at that job. Cookie Crumble is the GraphQL-aware one, tuned for the platform we built. Pick the one that fits your tests."
      />
      <div className="mx-auto mt-10 grid max-w-5xl gap-5 md:grid-cols-2">
        <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-7 backdrop-blur-sm">
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="border-cc-card-border bg-cc-surface/70 text-cc-accent inline-flex h-9 w-9 items-center justify-center rounded-lg border"
            >
              <CookieCrumble className="h-5 w-5" />
            </span>
            <h3 className="text-cc-heading font-heading text-xl tracking-tight">
              Cookie Crumble
            </h3>
          </div>
          <p className="text-cc-ink-dim mt-4 text-sm leading-relaxed">
            Purpose-built for the ChilliCream platform. GraphQL-aware
            formatters, Markdown snapshots for multi-shape state, and the same
            tool the Hot Chocolate, Fusion, and Mocha repos use every day.
          </p>
          <ul className="mt-5 space-y-2.5">
            {[
              "Native IExecutionResult and GraphQLHttpResponse formatters",
              "Markdown snapshots for multi-shape state",
              "Inline, file, and Markdown styles in one library",
              "xUnit, NUnit, TUnit, and MSTest integrations",
              "Dogfooded by the Hot Chocolate, Fusion, and Mocha suites",
            ].map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-2 text-sm leading-snug"
              >
                <span className="text-cc-accent mt-0.5 inline-flex shrink-0">
                  <CheckIcon />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </div>
        <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-7 backdrop-blur-sm">
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="border-cc-card-border bg-cc-surface/70 text-cc-ink-dim inline-flex h-9 w-9 items-center justify-center rounded-lg border font-mono text-sm font-semibold"
            >
              V
            </span>
            <h3 className="text-cc-heading font-heading text-xl tracking-tight">
              Verify
            </h3>
          </div>
          <p className="text-cc-ink-dim mt-4 text-sm leading-relaxed">
            The popular general-purpose .NET snapshot library by Simon Cropp.
            Broad ecosystem of converters, a mature accept/reject workflow, and
            integration with every major .NET test framework.
          </p>
          <ul className="mt-5 space-y-2.5">
            {[
              "Broad coverage across the .NET ecosystem",
              "Large catalogue of converters and extensions",
              "Mature received/verified diff workflow",
              "First-party IDE integration via tooling",
              "A great fit when your tests are not GraphQL-shaped",
            ].map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-2 text-sm leading-snug"
              >
                <span className="text-cc-accent mt-0.5 inline-flex shrink-0">
                  <CheckIcon />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
      <p className="text-cc-ink-dim mx-auto mt-8 max-w-3xl text-center text-sm">
        If most of your snapshots are GraphQL execution results or multi-shape
        scenarios, Cookie Crumble was built for that. If you need a general
        snapshot tool with the widest converter ecosystem, Verify is a fine
        answer. They do not have to be exclusive.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Dogfooding band
// ---------------------------------------------------------------------------

function DogfoodingSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Dogfooded"
        title="The same tool the platform tests itself with"
        lead="Cookie Crumble is the snapshot library called out in the ChilliCream contributor guide. The Hot Chocolate, Fusion, and Mocha test suites run through MatchSnapshot, MatchInlineSnapshot, and MatchMarkdownSnapshot."
      />
      <div className="mx-auto mt-10 grid max-w-5xl gap-4 sm:grid-cols-3">
        {[
          {
            label: "Hot Chocolate",
            body: "The GraphQL server. Execution result snapshots cover schema, query plans, and error shapes across every supported transport.",
          },
          {
            label: "Fusion",
            body: "The composition gateway. Markdown snapshots capture composed schemas, query plans, and gateway responses for review.",
          },
          {
            label: "Mocha",
            body: "The messaging framework. Snapshots cover saga state machines, OpenTelemetry traces, and message envelopes end to end.",
          },
        ].map((p) => (
          <div
            key={p.label}
            className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-xl border p-6 backdrop-blur-sm"
          >
            <div className="text-cc-nav-label font-mono text-xs tracking-[0.25em] uppercase">
              {p.label}
            </div>
            <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">
              {p.body}
            </p>
          </div>
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// MIT band
// ---------------------------------------------------------------------------

function MitBand() {
  return (
    <section className="py-12 sm:py-16">
      <div className="border-cc-card-border bg-cc-surface/70 relative overflow-hidden rounded-2xl border p-8 sm:p-10">
        <div className="flex flex-col items-start gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="max-w-2xl">
            <Eyebrow>Open source</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-2xl tracking-tight sm:text-3xl">
              MIT licensed. Use it anywhere.
            </h2>
            <p className="text-cc-ink-dim mt-3 text-sm sm:text-base">
              Cookie Crumble is released under the MIT license, like the rest of
              the ChilliCream platform. Drop it into a commercial product, an
              internal API, or a side project. Read the source, file an issue,
              send a PR.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform/blob/main/LICENSE">
              Read the license
            </OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Closing CTA
// ---------------------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="pt-12 pb-20 text-center sm:pt-16">
      <Eyebrow>Add the package, write a test</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-4 text-3xl tracking-tight sm:text-4xl">
        Write the test you actually want to read in review.
      </h2>
      <p className="text-cc-ink-dim lead mx-auto mt-5 max-w-2xl">
        Add Cookie Crumble to your test project, swap a hand-written assertion
        for a MatchSnapshot call, and let the formatter do the formatting.
      </p>
      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
      <p className="text-cc-ink-dim mt-6 font-mono text-xs tracking-widest uppercase">
        dotnet add package CookieCrumble
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function CookieCrumblePositioningPage() {
  return (
    <>
      <Hero />
      <WhyWeBuiltIt />
      <UseSection />
      <CodeExample />
      <WorkflowSection />
      <ComparisonSection />
      <DogfoodingSection />
      <MitBand />
      <ClosingCta />
    </>
  );
}
