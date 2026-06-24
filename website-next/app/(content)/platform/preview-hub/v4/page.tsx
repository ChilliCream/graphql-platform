import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "The ChilliCream Platform: Annotated Source",
  description:
    "Read the ChilliCream GraphQL platform as one annotated source file: eight capability listings under Build, Run, Evolve, plus the Nitro control plane.",
  keywords: [
    "GraphQL platform",
    "ChilliCream platform",
    "GraphQL build pipeline",
    "GraphQL observability",
    "GraphQL workflows",
    "GraphQL release safety",
    "GraphQL analytics",
    "continuous integration",
    "GraphQL ecosystem",
    "Nitro control plane",
  ],
  openGraph: {
    title: "The ChilliCream Platform: Annotated Source",
    description:
      "The ChilliCream GraphQL platform read as one literate source file: eight capability listings plus the Nitro control plane.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: used once on the hero phrase "one platform".              */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Shared chrome: a code-editor panel with a header strip                    */
/*  (filename, traffic lights, step counter) and a gutter of line numbers.    */
/* -------------------------------------------------------------------------- */

interface CodeLine {
  readonly text: ReactNode;
  /** Treated as comment when true: rendered dim. */
  readonly comment?: boolean;
  /** Renders the line tinted with the brand accent. */
  readonly accent?: boolean;
  /** Renders the line tinted coral (used for removed diff rows). */
  readonly coral?: boolean;
}

interface CodePanelProps {
  readonly filename: string;
  readonly step: string;
  readonly lines: readonly CodeLine[];
}

function CodePanel({ filename, step, lines }: CodePanelProps) {
  return (
    <div className="border-cc-card-border bg-cc-code-bg overflow-hidden rounded-xl border">
      <div className="border-cc-card-border bg-cc-code-header flex items-center gap-3 border-b px-4 py-2.5">
        <span className="flex items-center gap-1.5" aria-hidden>
          <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f56] opacity-80" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#ffbd2e] opacity-80" />
          <span className="h-2.5 w-2.5 rounded-full bg-[#27c93f] opacity-80" />
        </span>
        <span className="text-cc-ink font-mono text-[0.78rem] tracking-tight">
          {filename}
        </span>
        <span className="text-cc-nav-label ml-auto font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          {step}
        </span>
      </div>
      <pre
        className="text-cc-ink overflow-x-auto px-0 py-4 font-mono text-[0.78rem] leading-relaxed"
        aria-hidden={false}
      >
        <code className="grid">
          {lines.map((line, i) => {
            const colorClass = line.coral
              ? "text-[#f0786a]"
              : line.accent
                ? "text-cc-accent"
                : line.comment
                  ? "text-cc-ink-dim"
                  : "text-cc-ink";
            return (
              <span key={i} className="grid grid-cols-[3rem_1fr] items-start">
                <span className="text-cc-nav-label/80 pr-3 text-right font-mono text-[0.7rem] leading-relaxed select-none">
                  {String(i + 1).padStart(2, "0")}
                </span>
                <span className={`${colorClass} pr-4 whitespace-pre`}>
                  {line.text}
                </span>
              </span>
            );
          })}
        </code>
      </pre>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Eyebrow: mono uppercase tag                                               */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

/* -------------------------------------------------------------------------- */
/*  Bucket banner: a full-width mono comment-rule like `// BUILD ////`        */
/* -------------------------------------------------------------------------- */

interface BucketBannerProps {
  readonly label: string;
  readonly intent: string;
}

function BucketBanner({ label, intent }: BucketBannerProps) {
  return (
    <section className="flex flex-col gap-3">
      <div className="flex items-center gap-4">
        <span className="text-cc-ink-dim font-mono text-[0.78rem] tracking-tight">
          {"//"}
        </span>
        <h2 className="text-h6 text-cc-heading font-mono tracking-[0.22em] uppercase">
          {label}
        </h2>
        <span
          aria-hidden
          className="bg-cc-card-border ml-2 h-px flex-1"
          style={{ minWidth: "2rem" }}
        />
        <span className="text-cc-ink-dim hidden font-mono text-[0.78rem] tracking-tight sm:inline">
          {"////"}
        </span>
      </div>
      <p className="text-cc-ink-dim font-mono text-[0.82rem] leading-snug tracking-tight">
        {intent}
      </p>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Listing row: code panel + commentary, alternating sides at md+            */
/* -------------------------------------------------------------------------- */

interface ListingProps {
  readonly number: string;
  readonly title: string;
  readonly outcome: string;
  readonly description: string;
  readonly proofs: readonly string[];
  readonly href: string;
  readonly panel: ReactNode;
  /** "left" places panel-left on md+, commentary right; "right" mirrors. */
  readonly side: "left" | "right";
}

function Listing({
  number,
  title,
  outcome,
  description,
  proofs,
  href,
  panel,
  side,
}: ListingProps) {
  const panelOrder = side === "left" ? "md:order-1" : "md:order-2";
  const proseOrder = side === "left" ? "md:order-2" : "md:order-1";
  return (
    <section className="grid items-start gap-8 md:grid-cols-2 md:gap-10">
      <div className={`flex flex-col gap-4 ${panelOrder}`}>{panel}</div>
      <div className={`flex flex-col gap-4 ${proseOrder}`}>
        <Eyebrow>
          {"// "}
          {number}
        </Eyebrow>
        <h3 className="font-heading text-h4 text-cc-heading font-semibold tracking-tight">
          {title}
        </h3>
        <p className="text-cc-ink lead">{outcome}</p>
        <p className="text-cc-ink-dim text-body leading-relaxed">
          {description}
        </p>
        <ul className="mt-1 flex flex-col gap-1.5">
          {proofs.map((proof) => (
            <li
              key={proof}
              className="text-cc-ink-dim flex items-start gap-2 text-[0.88rem] leading-snug"
            >
              <span className="text-cc-accent mt-1 flex h-3 w-3 shrink-0 items-center justify-center">
                <CheckIcon size={12} />
              </span>
              <span>{proof}</span>
            </li>
          ))}
        </ul>
        <Link
          href={href}
          className="text-cc-accent hover:text-cc-accent-hover mt-2 font-mono text-[0.82rem] tracking-tight no-underline transition-colors"
        >
          Open {title} →
        </Link>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/* -------------------------------------------------------------------------- */

const HERO_LINES: readonly CodeLine[] = [
  { text: "// platform.graphql", comment: true },
  { text: "// the chillicream platform, as one file.", comment: true },
  { text: "", comment: true },
  { text: "type Platform {" },
  { text: "  // 01 Build: ship from the code that runs it.", comment: true },
  { text: "  build: Build!", accent: true },
  {
    text: "  // 02 Agentic Coding: give agents a feedback loop.",
    comment: true,
  },
  { text: "  agenticCoding: AgenticCoding!", accent: true },
  { text: "  // 03 Observability: see what the API is doing.", comment: true },
  { text: "  observability: Observability!", accent: true },
  {
    text: "  // 04 Workflows: continue work after the request.",
    comment: true,
  },
  { text: "  workflows: Workflows!", accent: true },
  {
    text: "  // 05 Analytics: know which fields earn their keep.",
    comment: true,
  },
  { text: "  analytics: Analytics!", accent: true },
  { text: "  // 06 Ecosystem: clients, IDE, loaders.", comment: true },
  { text: "  ecosystem: Ecosystem!", accent: true },
  { text: "  // 07 Release Safety: change with a net.", comment: true },
  { text: "  releaseSafety: ReleaseSafety!", accent: true },
  {
    text: "  // 08 Continuous Integration: confidence at merge.",
    comment: true,
  },
  { text: "  continuousIntegration: CI!", accent: true },
  { text: "}" },
];

function Hero() {
  return (
    <header className="flex flex-col gap-7">
      <Eyebrow>the chillicream platform</Eyebrow>
      <h1 className="font-heading text-hero text-cc-heading max-w-4xl font-semibold tracking-tight">
        Eight capabilities,{" "}
        <span
          className="bg-clip-text text-transparent"
          style={{ backgroundImage: SPECTRUM }}
        >
          one platform
        </span>{" "}
        for every API.
      </h1>
      <p className="text-cc-ink lead max-w-2xl">
        The ChilliCream platform covers the full life of a GraphQL API, from the
        first resolver to the next breaking change. Read it like a source file:
        eight listings, three buckets, one Nitro control plane.
      </p>
      <div className="flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
      <div className="mt-4">
        <CodePanel
          filename="platform.graphql"
          step="LISTING 00 / 09"
          lines={HERO_LINES}
        />
      </div>
    </header>
  );
}

/* -------------------------------------------------------------------------- */
/*  Listing panels (each capability)                                          */
/* -------------------------------------------------------------------------- */

const BUILD_LINES: readonly CodeLine[] = [
  { text: "// 01_build.cs", comment: true },
  { text: "namespace Catalog;", comment: true },
  { text: "" },
  { text: "[QueryType]", accent: true },
  { text: "public static class ProductQueries" },
  { text: "{" },
  { text: "    public static Product GetProduct(" },
  { text: "        int id," },
  { text: "        CatalogContext db) =>" },
  { text: "            db.Products.Find(id);" },
  { text: "}" },
  { text: "" },
  { text: "// schema.graphql (generated)", comment: true },
  { text: "type Query {" },
  { text: "  product(id: Int!): Product", accent: true },
  { text: "}" },
];

const AGENTIC_LINES: readonly CodeLine[] = [
  { text: "$ nitro agent propose 'add product.price'" },
  { text: "" },
  { text: "agent > reading schema.graphql", comment: true },
  { text: "agent > drafting change", comment: true },
  { text: "" },
  { text: "+ field price: Money!", accent: true },
  { text: "" },
  { text: "lint  : naming ok, nullability ok", comment: true },
  { text: "check : 12 published clients", comment: true },
  { text: "diff  : 0 breaking, 1 additive", comment: true },
  { text: "" },
  { text: "verdict: safe to merge", accent: true },
];

const TRACE_LINES: readonly CodeLine[] = [
  { text: "// 03_trace.json (otel span excerpt)", comment: true },
  { text: "{" },
  { text: '  "name": "Query.product",' },
  { text: '  "kind": "server",' },
  { text: '  "duration_ms": 41,' },
  { text: '  "p95_ms": 78,', accent: true },
  { text: '  "attributes": {' },
  { text: '    "graphql.operation": "GetProduct",' },
  { text: '    "graphql.field.path": "product.price",', accent: true },
  { text: '    "graphql.resolver": "ProductQueries.GetProduct"' },
  { text: "  }," },
  { text: '  "events": [' },
  { text: '    { "name": "dataloader.batch", "items": 12 }' },
  { text: "  ]" },
  { text: "}" },
];

const WORKFLOWS_LINES: readonly CodeLine[] = [
  { text: "// 04_workflow.cs", comment: true },
  { text: "[Workflow]", accent: true },
  { text: "public class CheckoutFlow" },
  { text: "{" },
  { text: "    [Step] public Task Charge();" },
  { text: "    [Step] public Task Notify();" },
  { text: "    [Step] public Task Fulfill();" },
  { text: "    [Step] public Task Settle();" },
  { text: "" },
  { text: "    // durable, resumable on cold start,", comment: true },
  { text: "    // each step retried independently.", comment: true },
  { text: "}" },
];

const ANALYTICS_LINES: readonly CodeLine[] = [
  { text: "// 05_usage.graphql", comment: true },
  { text: "query FieldUsage {" },
  { text: '  schema(name: "catalog") {' },
  { text: '    field(path: "Product.legacyId") {' },
  { text: "      callsLast30Days", accent: true },
  { text: "      clients { name version }" },
  { text: "    }" },
  { text: "  }" },
  { text: "}" },
  { text: "" },
  { text: "# response", comment: true },
  { text: "# callsLast30Days: 0", comment: true },
  { text: "# clients: []  (dead field)", comment: true },
];

const ECOSYSTEM_LINES: readonly CodeLine[] = [
  { text: "// 06_ecosystem.ts", comment: true },
  { text: "// Strawberry Shake (MSBuild codegen)", comment: true },
  { text: "import { useGetProductQuery }" },
  { text: '  from "./generated";', accent: true },
  { text: "" },
  { text: "const { data } = useGetProductQuery({" },
  { text: "  variables: { id: 42 }," },
  { text: "});" },
  { text: "" },
  { text: "// open the same schema in", comment: true },
  { text: "// Banana Cake Pop to iterate.", comment: true },
];

const RELEASE_LINES: readonly CodeLine[] = [
  { text: "// 07_schema.diff", comment: true },
  { text: "diff --git schema.prev schema.next", comment: true },
  { text: "" },
  { text: "+ field discount: Money", accent: true },
  { text: "~ rename total > subtotal" },
  { text: "- field legacyId: String", coral: true },
  { text: "" },
  { text: "// 7 published clients affected, 0 blocking", comment: true },
  { text: "// rule: legacyId removal allowed (unused 30d)", comment: true },
];

const CI_LINES: readonly CodeLine[] = [
  { text: "# 08_ci.yml", comment: true },
  { text: "name: schema" },
  { text: "on: [pull_request]" },
  { text: "jobs:" },
  { text: "  validate:" },
  { text: "    steps:" },
  { text: "      - run: nitro schema check", accent: true },
  { text: "      - run: nitro schema compose", accent: true },
  { text: "      - run: nitro schema ship", accent: true },
  { text: "    # annotated diff posted to the PR", comment: true },
];

/* -------------------------------------------------------------------------- */
/*  Nitro panel                                                               */
/* -------------------------------------------------------------------------- */

const NITRO_LINES: readonly CodeLine[] = [
  { text: "$ nitro login" },
  { text: "ok > authenticated as you@org", comment: true },
  { text: "" },
  { text: "$ nitro schema publish ./schema.graphql \\" },
  { text: "    --service catalog --env production" },
  { text: "published > catalog@7f3a1d2", accent: true },
  { text: "" },
  { text: "$ nitro watch traces --service catalog" },
  { text: "trace > Query.product  p95 78ms  ok", comment: true },
  { text: "trace > Query.search   p95 142ms ok", comment: true },
  { text: "" },
  { text: "# the file that imports the other eight.", comment: true },
];

function NitroSection() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg flex flex-col gap-6 rounded-xl border p-6 md:p-10">
      <Eyebrow>{"// nitro"}</Eyebrow>
      <div className="flex flex-col gap-6">
        <CodePanel
          filename="nitro.sh"
          step="LISTING 09 / 09"
          lines={NITRO_LINES}
        />
        <div className="flex flex-col gap-4">
          <h2 className="font-heading text-h3 text-cc-heading font-semibold tracking-tight">
            The control plane that powers the platform.
          </h2>
          <p className="text-cc-ink lead max-w-3xl">
            Nitro is the file that imports the other eight. Schema registry,
            release checks, analytics, and traces share one home. Connect a
            service, ship a change, and Nitro keeps the rest of the platform in
            sync.
          </p>
          <ul className="text-cc-ink-dim flex flex-col gap-1.5">
            {[
              "Schema registry for every environment",
              "Release checks against published clients",
              "Field usage and traces in one timeline",
            ].map((line) => (
              <li
                key={line}
                className="flex items-start gap-2 text-[0.88rem] leading-snug"
              >
                <span className="text-cc-accent mt-1 flex h-3 w-3 shrink-0 items-center justify-center">
                  <CheckIcon size={12} />
                </span>
                <span>{line}</span>
              </li>
            ))}
          </ul>
          <div className="mt-2 flex flex-wrap items-center gap-3">
            <SolidButton href="https://nitro.chillicream.com">
              Open Nitro
            </SolidButton>
            <OutlineButton href="/products/nitro">About Nitro</OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA                                                               */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="flex flex-col items-center gap-6 py-6 text-center">
      <p className="text-cc-nav-label font-mono text-[0.78rem] tracking-tight">
        {"// fin"}
      </p>
      <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
        Start with the surface closest to today&apos;s problem.
      </h2>
      <p className="text-cc-ink lead max-w-2xl">
        Every listing above is a real page. Open the one that maps to the work
        in front of you, or start a project and let the platform fold in as you
        need it.
      </p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function PlatformAnnotatedSourcePage() {
  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-20 py-6">
      <Hero />

      <BucketBanner
        label="BUILD"
        intent="Author the API and let agents help."
      />

      <Listing
        number="01 Build"
        title="Build"
        outcome="Ship from the code that runs it."
        description="Hot Chocolate is implementation-first: write a resolver class, get a schema, types, and a typed client out of the same source. The code that runs in production is the same code reviewers read."
        proofs={[
          "Implementation-first GraphQL in C#",
          "Schema, resolvers, DataLoaders from one class",
          "Typed .NET clients out of the same source",
        ]}
        href="/platform/build"
        panel={
          <CodePanel
            filename="01_build.cs"
            step="LISTING 01 / 09"
            lines={BUILD_LINES}
          />
        }
        side="left"
      />

      <Listing
        number="02 Agentic Coding"
        title="Agentic Coding"
        outcome="Give coding agents a feedback loop."
        description="Typed contracts, schema diffs, and lint signals turn a coding agent's guesses into reviewed proposals. The same loop a senior reviewer would run, executed before the PR is opened."
        proofs={[
          "Typed contracts agents can read",
          "Diff and lint signal on every change",
          "Same loop a senior reviewer would run",
        ]}
        href="/platform/agentic-coding"
        panel={
          <CodePanel
            filename="02_agentic.sh"
            step="LISTING 02 / 09"
            lines={AGENTIC_LINES}
          />
        }
        side="right"
      />

      <BucketBanner
        label="RUN"
        intent="Operate it in production with eyes on every call."
      />

      <Listing
        number="03 Observability"
        title="Observability"
        outcome="See what the API is doing, right now."
        description="Operation-level traces, field hot paths, N+1 detection, and OpenTelemetry export to the stack you already run. Telemetry needs Nitro configuration, then it flows to your collector."
        proofs={[
          "Operation-level traces and timings",
          "Field hot paths and N+1 detection",
          "OpenTelemetry export to your stack",
        ]}
        href="/platform/observability"
        panel={
          <CodePanel
            filename="03_trace.json"
            step="LISTING 03 / 09"
            lines={TRACE_LINES}
          />
        }
        side="left"
      />

      <Listing
        number="04 Workflows"
        title="Workflows"
        outcome="Let work continue after the request."
        description="Mocha runs durable steps with retries, resumable on cold start. Sagas are validated before traffic so you find composition errors during build, not in production. Exactly-once processing for steps that touch the world."
        proofs={[
          "Durable steps with retries",
          "Background jobs in the same model",
          "Resumable on cold start",
        ]}
        href="/platform/workflows"
        panel={
          <CodePanel
            filename="04_workflow.cs"
            step="LISTING 04 / 09"
            lines={WORKFLOWS_LINES}
          />
        }
        side="right"
      />

      <Listing
        number="05 Analytics"
        title="Analytics"
        outcome="Know which fields earn their keep."
        description="Field-level usage over time, per-client adoption per type, dead-field detection before you cut. The usage telemetry rides the same Nitro pipeline as your traces."
        proofs={[
          "Field-level usage over time",
          "Per-client adoption per type",
          "Spot dead fields before you cut",
        ]}
        href="/platform/analytics"
        panel={
          <CodePanel
            filename="05_usage.graphql"
            step="LISTING 05 / 09"
            lines={ANALYTICS_LINES}
          />
        }
        side="left"
      />

      <Listing
        number="06 Ecosystem"
        title="Ecosystem"
        outcome="An ecosystem you can trust and reuse."
        description="Banana Cake Pop for query exploration, Strawberry Shake MSBuild codegen for typed clients, Green Donut for DataLoaders. Each tool stands alone, but they cohere when used together."
        proofs={[
          "Banana Cake Pop IDE",
          "Strawberry Shake typed clients",
          "Green Donut DataLoaders",
        ]}
        href="/platform/ecosystem"
        panel={
          <CodePanel
            filename="06_ecosystem.ts"
            step="LISTING 06 / 09"
            lines={ECOSYSTEM_LINES}
          />
        }
        side="right"
      />

      <BucketBanner
        label="EVOLVE"
        intent="Ship change without breaking published clients."
      />

      <Listing
        number="07 Release Safety"
        title="Release Safety"
        outcome="Change contracts with a safety net."
        description="Every diff is checked against the clients that published against your schema. Breaking changes are flagged before merge. Block, warn, or allow per rule, with the published clients affected listed inline."
        proofs={[
          "Schema diff against published clients",
          "Breaking change flagged before merge",
          "Block, warn, or allow per rule",
        ]}
        href="/platform/release-safety"
        panel={
          <CodePanel
            filename="07_schema.diff"
            step="LISTING 07 / 09"
            lines={RELEASE_LINES}
          />
        }
        side="left"
      />

      <Listing
        number="08 Continuous Integration"
        title="Continuous Integration"
        outcome="Innovate with confidence at merge time."
        description="Schema check on every pull request, composition validation across services, annotated diffs in code review. The same Nitro commands run locally, in CI, and in production."
        proofs={[
          "Schema check on every pull request",
          "Composition validation across services",
          "Annotated diffs in code review",
        ]}
        href="/platform/continuous-integration"
        panel={
          <CodePanel
            filename="08_ci.yml"
            step="LISTING 08 / 09"
            lines={CI_LINES}
          />
        }
        side="right"
      />

      <NitroSection />
      <ClosingCta />
    </div>
  );
}
