import type { Metadata } from "next";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CookieCrumble as CookieCrumbleIcon } from "@/src/icons/CookieCrumble";

export const metadata: Metadata = {
  title: "Cookie Crumble: GraphQL-aware Snapshot Testing for .NET",
  description:
    "Cookie Crumble is the GraphQL-aware snapshot testing library for .NET. Inline, file, and markdown snapshots with native formatters for IExecutionResult.",
  keywords: [
    "Cookie Crumble",
    "snapshot testing",
    ".NET snapshot testing",
    "GraphQL testing",
    "Hot Chocolate testing",
    "MatchSnapshot",
    "MatchInlineSnapshot",
    "MatchMarkdownSnapshot",
    "xUnit",
    "TUnit",
    "NUnit",
    "MSTest",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Cookie Crumble: GraphQL-aware Snapshot Testing for .NET",
    description:
      "Inline, file, and markdown snapshots with native formatters for IExecutionResult and GraphQLHttpResponse. MIT-licensed.",
    type: "website",
  },
};

// Brand spectrum, allowed at most once per screen. Used on the closing CTA rule.
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
// Code window primitives. Tokens are GitHub-dark approximations, scoped to the
// snippet blocks only so the rest of the page stays on cc-* tokens.
// -----------------------------------------------------------------------------

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
  num: { color: "#79c0ff" },
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

interface CodeWindowProps {
  readonly file: string;
  readonly lang: string;
  readonly badge?: string;
  readonly children: ReactNode;
  readonly footerLeft?: ReactNode;
  readonly footerRight?: ReactNode;
}

function CodeWindow({
  file,
  lang,
  badge,
  children,
  footerLeft,
  footerRight,
}: CodeWindowProps) {
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
          {file}
        </span>
        {badge ? (
          <span className="border-cc-accent/40 text-cc-accent ml-3 inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
            {badge}
          </span>
        ) : null}
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {lang}
        </span>
      </div>
      <div className="relative py-4">{children}</div>
      {footerLeft || footerRight ? (
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
          <span>{footerLeft}</span>
          <span className="text-cc-accent">{footerRight}</span>
        </div>
      ) : null}
    </div>
  );
}

// -----------------------------------------------------------------------------
// Hero
// -----------------------------------------------------------------------------

function HeroCodeCard() {
  return (
    <CodeWindow
      file="Catalog.Tests/ProductTests.cs"
      lang="C#"
      footerLeft="snapshot matched"
      footerRight="GraphQL-aware formatter"
    >
      <CodeLine n={1}>
        <span style={C.kw}>using</span>{" "}
        <span style={C.plain}>CookieCrumble;</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.punct}>[</span>
        <span style={C.attr}>Fact</span>
        <span style={C.punct}>]</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.kw}>public async</span> <span style={C.type}>Task</span>{" "}
        <span style={C.fn}>Product_By_Id_Returns_Expected_Result</span>
        <span style={C.punct}>()</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={C.punct}>{`{`}</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.kw}>var</span> <span style={C.plain}>result</span>{" "}
        <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
        <span style={C.plain}>executor</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>ExecuteAsync</span>
        <span style={C.punct}>(</span>
        <span style={C.str}>{`"{ product(id: 1) { id name price } }"`}</span>
        <span style={C.punct}>);</span>
      </CodeLine>
      <CodeLine n={7}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={8}>
        <span style={C.plain}>{`    `}</span>
        <span
          style={C.comment}
        >{`// IExecutionResult formatted as readable GraphQL response`}</span>
      </CodeLine>
      <CodeLine n={9}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.plain}>result</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>MatchSnapshot</span>
        <span style={C.punct}>();</span>
      </CodeLine>
      <CodeLine n={10}>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
    </CodeWindow>
  );
}

// -----------------------------------------------------------------------------
// Step arc. Each step pairs a copy column with a real-feel code snippet column.
// -----------------------------------------------------------------------------

interface StepProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
  readonly snippet: ReactNode;
  readonly reverse?: boolean;
}

function Step({
  id,
  index,
  eyebrow,
  title,
  body,
  bullets,
  snippet,
  reverse = false,
}: StepProps) {
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
          {snippet}
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Step 01: Inline snapshot
// -----------------------------------------------------------------------------

function InlineSnippet() {
  return (
    <CodeWindow
      file="Cart.Tests/PricingTests.cs"
      lang="C#"
      badge="01 inline"
      footerLeft="expected value lives beside the assertion"
      footerRight="MatchInlineSnapshot"
    >
      <CodeLine n={1}>
        <span style={C.punct}>[</span>
        <span style={C.attr}>Fact</span>
        <span style={C.punct}>]</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.kw}>public void</span>{" "}
        <span style={C.fn}>Discount_Applies_When_Over_Threshold</span>
        <span style={C.punct}>()</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.punct}>{`{`}</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.kw}>var</span> <span style={C.plain}>cart</span>{" "}
        <span style={C.punct}>=</span> <span style={C.kw}>new</span>{" "}
        <span style={C.type}>Cart</span>
        <span style={C.punct}>();</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.plain}>cart</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>Add</span>
        <span style={C.punct}>(</span>
        <span style={C.str}>{`"SKU-001"`}</span>
        <span style={C.punct}>, qty: </span>
        <span style={C.num}>3</span>
        <span style={C.punct}>);</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={7}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.plain}>cart</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>Summarize</span>
        <span style={C.punct}>()</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>MatchInlineSnapshot</span>
        <span style={C.punct}>(</span>
      </CodeLine>
      <CodeLine n={8}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.str}>{`"""`}</span>
      </CodeLine>
      <CodeLine n={9}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.str}>{`{`}</span>
      </CodeLine>
      <CodeLine n={10}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.str}>{`  "subtotal": 30.00,`}</span>
      </CodeLine>
      <CodeLine n={11}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.str}>{`  "discount": 3.00,`}</span>
      </CodeLine>
      <CodeLine n={12}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.str}>{`  "total": 27.00`}</span>
      </CodeLine>
      <CodeLine n={13}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.str}>{`}`}</span>
      </CodeLine>
      <CodeLine n={14}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.str}>{`"""`}</span>
        <span style={C.punct}>);</span>
      </CodeLine>
      <CodeLine n={15}>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
    </CodeWindow>
  );
}

// -----------------------------------------------------------------------------
// Step 02: File snapshot
// -----------------------------------------------------------------------------

function FileSnippet() {
  return (
    <div className="grid gap-4">
      <CodeWindow
        file="Catalog.Tests/SearchTests.cs"
        lang="C#"
        badge="02 file"
        footerLeft="Catalog.Tests/__snapshots__/Search_Returns_Top_Results.snap"
        footerRight="MatchSnapshot"
      >
        <CodeLine n={1}>
          <span style={C.punct}>[</span>
          <span style={C.attr}>Fact</span>
          <span style={C.punct}>]</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.kw}>public async</span>{" "}
          <span style={C.type}>Task</span>{" "}
          <span style={C.fn}>Search_Returns_Top_Results</span>
          <span style={C.punct}>()</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.punct}>{`{`}</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.kw}>var</span> <span style={C.plain}>response</span>{" "}
          <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
          <span style={C.plain}>client</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>PostAsync</span>
          <span style={C.punct}>(</span>
          <span style={C.str}>{`"/graphql"`}</span>
          <span style={C.punct}>, </span>
          <span style={C.plain}>query</span>
          <span style={C.punct}>);</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={C.plain}>{`    `}</span>
          <span
            style={C.comment}
          >{`// GraphQLHttpResponse formatter pins headers + body shape.`}</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.kw}>await</span> <span style={C.plain}>response</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchSnapshotAsync</span>
          <span style={C.punct}>();</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
      </CodeWindow>
      <CodeWindow
        file="Search_Returns_Top_Results.snap"
        lang="snap"
        footerLeft="committed alongside the test, reviewed in diff"
      >
        <CodeLine n={1}>
          <span style={C.comment}>{`# Headers`}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>Status:</span> <span style={C.num}>200</span>{" "}
          <span style={C.plain}>OK</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>
            Content-Type: application/graphql-response+json
          </span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.comment}>{`# Body`}</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={C.punct}>{`{`}</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={C.plain}>{`  `}</span>
          <span style={C.str}>{`"data"`}</span>
          <span style={C.punct}>: {`{`}</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.str}>{`"search"`}</span>
          <span style={C.punct}>: [</span>
        </CodeLine>
        <CodeLine n={9}>
          <span style={C.plain}>{`      `}</span>
          <span style={C.punct}>{`{ `}</span>
          <span style={C.str}>{`"id"`}</span>
          <span style={C.punct}>: </span>
          <span style={C.str}>{`"p_001"`}</span>
          <span style={C.punct}>, </span>
          <span style={C.str}>{`"name"`}</span>
          <span style={C.punct}>: </span>
          <span style={C.str}>{`"Espresso Beans"`}</span>
          <span style={C.punct}>{` },`}</span>
        </CodeLine>
        <CodeLine n={10}>
          <span style={C.plain}>{`      `}</span>
          <span style={C.punct}>{`{ `}</span>
          <span style={C.str}>{`"id"`}</span>
          <span style={C.punct}>: </span>
          <span style={C.str}>{`"p_017"`}</span>
          <span style={C.punct}>, </span>
          <span style={C.str}>{`"name"`}</span>
          <span style={C.punct}>: </span>
          <span style={C.str}>{`"French Press"`}</span>
          <span style={C.punct}>{` }`}</span>
        </CodeLine>
        <CodeLine n={11}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>]</span>
        </CodeLine>
        <CodeLine n={12}>
          <span style={C.plain}>{`  `}</span>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
        <CodeLine n={13}>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
      </CodeWindow>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Step 03: Markdown snapshot
// -----------------------------------------------------------------------------

function MarkdownSnippet() {
  return (
    <div className="grid gap-4">
      <CodeWindow
        file="Catalog.Tests/CheckoutFlowTests.cs"
        lang="C#"
        badge="03 markdown"
        footerLeft="one document, several shapes of state"
        footerRight="MatchMarkdownSnapshot"
      >
        <CodeLine n={1}>
          <span style={C.punct}>[</span>
          <span style={C.attr}>Fact</span>
          <span style={C.punct}>]</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.kw}>public async</span>{" "}
          <span style={C.type}>Task</span>{" "}
          <span style={C.fn}>Checkout_Records_Order_And_Publishes_Event</span>
          <span style={C.punct}>()</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.punct}>{`{`}</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.kw}>var</span> <span style={C.plain}>result</span>{" "}
          <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
          <span style={C.plain}>executor</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>ExecuteAsync</span>
          <span style={C.punct}>(</span>
          <span style={C.plain}>checkoutMutation</span>
          <span style={C.punct}>);</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.type}>Snapshot</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Create</span>
          <span style={C.punct}>()</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(</span>
          <span style={C.plain}>result</span>
          <span style={C.punct}>, </span>
          <span style={C.str}>{`"GraphQL Result"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(</span>
          <span style={C.plain}>db</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Orders</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Single</span>
          <span style={C.punct}>(), </span>
          <span style={C.str}>{`"Order Row"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={9}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(</span>
          <span style={C.plain}>bus</span>
          <span style={C.punct}>.</span>
          <span style={C.plain}>Published</span>
          <span style={C.punct}>, </span>
          <span style={C.str}>{`"Published Events"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={10}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchMarkdownSnapshot</span>
          <span style={C.punct}>();</span>
        </CodeLine>
        <CodeLine n={11}>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
      </CodeWindow>
      <CodeWindow
        file="Checkout_Records_Order_And_Publishes_Event.md"
        lang="md"
        footerLeft="rendered next to the test, scannable at a glance"
      >
        <CodeLine n={1}>
          <span style={C.kw}># GraphQL Result</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.comment}>{`\`\`\`json`}</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.punct}>{`{ `}</span>
          <span style={C.str}>{`"data"`}</span>
          <span style={C.punct}>: {`{ `}</span>
          <span style={C.str}>{`"checkout"`}</span>
          <span style={C.punct}>: {`{ `}</span>
          <span style={C.str}>{`"orderId"`}</span>
          <span style={C.punct}>: </span>
          <span style={C.str}>{`"o_42"`}</span>
          <span style={C.punct}>{` } } }`}</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.comment}>{`\`\`\``}</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={C.kw}># Order Row</span>
        </CodeLine>
        <CodeLine n={8}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={9}>
          <span style={C.plain}>{`| Field    | Value      |`}</span>
        </CodeLine>
        <CodeLine n={10}>
          <span style={C.plain}>{`| -------- | ---------- |`}</span>
        </CodeLine>
        <CodeLine n={11}>
          <span style={C.plain}>{`| Id       | o_42       |`}</span>
        </CodeLine>
        <CodeLine n={12}>
          <span style={C.plain}>{`| Total    | 27.00      |`}</span>
        </CodeLine>
        <CodeLine n={13}>
          <span style={C.plain}>{`| Status   | Confirmed  |`}</span>
        </CodeLine>
        <CodeLine n={14}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={15}>
          <span style={C.kw}># Published Events</span>
        </CodeLine>
        <CodeLine n={16}>
          <span style={C.plain}>&nbsp;</span>
        </CodeLine>
        <CodeLine n={17}>
          <span style={C.plain}>{`- OrderConfirmed { orderId: "o_42" }`}</span>
        </CodeLine>
      </CodeWindow>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Update workflow diagram
// -----------------------------------------------------------------------------

interface UpdateCardProps {
  readonly step: string;
  readonly title: string;
  readonly body: string;
  readonly chrome: ReactNode;
}

function UpdateCard({ step, title, body, chrome }: UpdateCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex flex-col rounded-xl border p-6">
      <div className="flex items-center gap-3">
        <IndexTag value={step} />
        <Eyebrow>Update</Eyebrow>
      </div>
      <h3 className="text-cc-heading font-heading mt-4 text-xl font-semibold tracking-tight">
        {title}
      </h3>
      <p className="text-cc-prose mt-3 text-sm leading-relaxed">{body}</p>
      <div className="mt-5">{chrome}</div>
    </div>
  );
}

interface FileRowProps {
  readonly name: string;
  readonly status?: "new" | "updated" | "kept";
  readonly indent?: number;
  readonly muted?: boolean;
}

function FileRow({ name, status, indent = 0, muted = false }: FileRowProps) {
  const statusStyle: Record<
    NonNullable<FileRowProps["status"]>,
    CSSProperties
  > = {
    new: { color: "#f0786a" },
    updated: { color: "#5eead4" },
    kept: { color: "#8b949e" },
  };
  const tagText: Record<NonNullable<FileRowProps["status"]>, string> = {
    new: "new",
    updated: "promoted",
    kept: "kept",
  };
  return (
    <div className="flex items-center justify-between gap-3 font-mono text-[12px]">
      <span
        className={muted ? "text-cc-ink-dim" : "text-cc-ink"}
        style={{ paddingLeft: `${indent * 14}px` }}
      >
        {name}
      </span>
      {status ? (
        <span
          style={statusStyle[status]}
          className="text-[10.5px] tracking-widest uppercase"
        >
          {tagText[status]}
        </span>
      ) : null}
    </div>
  );
}

function MismatchTree() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg rounded-lg border p-4">
      <FileRow name="Catalog.Tests/" />
      <FileRow name="__snapshots__/" indent={1} muted />
      <FileRow
        name="Search_Returns_Top_Results.snap"
        indent={2}
        status="kept"
      />
      <FileRow name="__mismatch__/" indent={1} />
      <FileRow name="Search_Returns_Top_Results.snap" indent={2} status="new" />
      <FileRow name="SearchTests.cs" indent={1} muted />
    </div>
  );
}

function PromotedTree() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg rounded-lg border p-4">
      <FileRow name="Catalog.Tests/" />
      <FileRow name="__snapshots__/" indent={1} muted />
      <FileRow
        name="Search_Returns_Top_Results.snap"
        indent={2}
        status="updated"
      />
      <FileRow name="SearchTests.cs" indent={1} muted />
      <FileRow name="(no __mismatch__ folder, all clean)" indent={1} muted />
    </div>
  );
}

// -----------------------------------------------------------------------------
// Stat band for the MIT / open source proof.
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

export default function CookieCrumblePreviewV2() {
  return (
    <>
      {/* HERO */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <div className="flex items-center gap-3">
              <CookieCrumbleIcon className="h-10 w-auto" />
              <Eyebrow>Snapshot testing for .NET</Eyebrow>
            </div>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Three snapshot styles, one assertion.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Cookie Crumble is the GraphQL-aware snapshot testing library the
              ChilliCream team built to test Hot Chocolate, Fusion, and Mocha,
              and equally happy on any .NET test that benefits from snapshots.
              Native formatters understand IExecutionResult and
              GraphQLHttpResponse, so what you assert reads like the response,
              not a stringified blob.
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
                  Runtime
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">.NET</dd>
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
            <HeroCodeCard />
          </div>
        </div>
      </section>

      {/* Capabilities strip */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-5">
          {[
            "Inline snapshots",
            "File snapshots",
            "Markdown snapshots",
            "GraphQL-aware formatters",
            "Any .NET test framework",
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

      {/* Tutorial intro */}
      <section className="py-16 sm:py-20">
        <div className="max-w-3xl">
          <Eyebrow>The walkthrough</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            Pick the snapshot that fits the shape of what you are asserting.
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            Cookie Crumble ships three snapshot styles. They are not three
            different libraries, they are three calls on the same Snapshot API,
            tuned for three review experiences. The arc below walks each one
            with a real-feel test, so you can decide where every assertion lives
            before you write it.
          </p>
        </div>
      </section>

      {/* THREE STEPS */}
      <Step
        id="inline"
        index="01"
        eyebrow="Inline"
        title="One expression. Ship the expected value next to the call."
        body="MatchInlineSnapshot pins a small, hand-readable shape right in the test file. The first run writes the expected literal in place. Reviewers see input, action, and expectation in one frame, and the test fails loudly when the literal drifts from the value the code produces."
        bullets={[
          "Ideal for small values: a totals object, a formatted string, a structured exception.",
          "No __snapshots__ folder to maintain for tiny assertions.",
          "Code review reads top-to-bottom, no jumping between files.",
        ]}
        snippet={<InlineSnippet />}
      />

      <Step
        id="file"
        index="02"
        eyebrow="File"
        title="A larger shape, pinned to a snapshot file and reviewed in diff."
        body="MatchSnapshot writes the formatted result to a .snap file next to the test. The GraphQLHttpResponse formatter pins status, headers, and body in one canonical shape, so a regression shows up as a clean diff in pull request review rather than as a wall of inline Assert calls."
        bullets={[
          "GraphQL-aware formatters render IExecutionResult and GraphQLHttpResponse as the API actually returns them.",
          "Snapshot files live with the test and travel with the branch.",
          "Diffs are tiny when nothing changed, surgical when something did.",
        ]}
        snippet={<FileSnippet />}
        reverse
      />

      <Step
        id="markdown"
        index="03"
        eyebrow="Markdown"
        title="Multi-shape state, captured in one expressive document."
        body="MatchMarkdownSnapshot composes several values into one snapshot document with section headers. One file shows the GraphQL response, the row written to the database, and the events the bus published, in the order they happened. Section headers act as labels, so a reviewer scans the test the same way they would read a bug report."
        bullets={[
          "Add labelled blocks via Snapshot.Create().Add(value, name).",
          "Mix JSON, tables, and bullet lists in the same snapshot.",
          "Built for end-to-end tests where one assertion would never cover the surface.",
        ]}
        snippet={<MarkdownSnippet />}
      />

      {/* How to update */}
      <section
        id="update"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="mb-10 max-w-3xl">
          <Eyebrow>How to update</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            Two steps when behavior changes on purpose.
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            When a snapshot test fails because the new output is the correct
            output, the fix is two moves. No accept-all tooling, no global
            overwrite, nothing leaves the test folder.
          </p>
        </div>
        <div className="grid gap-6 lg:grid-cols-2">
          <UpdateCard
            step="01"
            title="Run the failing test. The new snapshot lands in __mismatch__."
            body="On a mismatch, Cookie Crumble writes the new candidate into a __mismatch__/ directory next to the existing snapshot. The original file is untouched, the candidate sits beside it, and a normal file diff tells you exactly what moved."
            chrome={<MismatchTree />}
          />
          <UpdateCard
            step="02"
            title="Promote the candidate. Commit the diff."
            body="Move the file from __mismatch__/ into the snapshot folder when the change is intended. The __mismatch__ directory empties out, the snapshot reflects the new contract, and the commit ships a small reviewable diff."
            chrome={<PromotedTree />}
          />
        </div>
      </section>

      {/* Dogfooded band */}
      <section className="border-cc-card-border border-t py-20 sm:py-24">
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>Dogfooded</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              The library that tests the platform tests your platform too.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Cookie Crumble is the snapshot tool the ChilliCream team uses on
              Hot Chocolate, Fusion, and Mocha. Every release of the platform is
              pinned by these snapshots, so the GraphQL-aware formatters, the
              markdown layout, and the __mismatch__ workflow are tuned for the
              exact tests you are about to write. Integrates with xUnit, NUnit,
              TUnit, and MSTest, and works on any .NET test that benefits from
              pinning a shape.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Runtime" value=".NET" />
              <ProofItem label="Inline" value="MatchInline" />
              <ProofItem label="File" value="MatchSnapshot" />
              <ProofItem label="Markdown" value="MatchMarkdown" />
              <ProofItem
                label="Frameworks"
                value="xUnit / NUnit / TUnit / MSTest"
              />
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA. Single brand-spectrum hairline. */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            Snapshot tests that read like the system they cover.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            Inline for small assertions, file for canonical shapes, markdown for
            the multi-shape ones. One library, one mental model, one
            __mismatch__ folder when something moves.
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
