import type { Metadata } from "next";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  NitroDiagnose,
  NitroFusion,
  NitroMonitoring,
  NitroSchema,
  NitroTrace,
} from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro: GraphQL Control Plane, One Command at a Time",
  description:
    "The Nitro GraphQL control plane walked through one command at a time: observe, trace, diagnose, evolve, and compose your GraphQL and .NET API from one console.",
  keywords: [
    "Nitro",
    "Nitro GraphQL control plane",
    "GraphQL IDE",
    "OpenTelemetry",
    "distributed tracing",
    "schema registry",
    "Fusion gateway",
    "API observability",
    "ChilliCream",
    ".NET observability",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Nitro: GraphQL Control Plane, One Command at a Time",
    description:
      "The Nitro GraphQL control plane walked through one command at a time: observe, trace, diagnose, evolve, and compose your API from one console.",
    type: "website",
  },
};

interface ChromeProps {
  readonly breadcrumb: string;
  readonly stdoutLabel?: string;
  readonly children: ReactNode;
}

/**
 * Persistent macOS-style code-panel chrome. Three traffic-light dots on a
 * cc-code-header bar, a mono breadcrumb, and a thin cc-accent left edge rule
 * tying every snippet and product screen into the same continuous session.
 */
function PanelChrome({ breadcrumb, stdoutLabel, children }: ChromeProps) {
  return (
    <div className="border-cc-card-border bg-cc-code-bg relative overflow-hidden rounded-lg border shadow-2xl shadow-black/40">
      <span
        aria-hidden="true"
        className="bg-cc-accent absolute inset-y-0 left-0 w-px"
      />
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-3 border-b px-4 py-2.5">
        <span aria-hidden="true" className="flex items-center gap-1.5">
          <span className="block h-2.5 w-2.5 rounded-full bg-[#ff5f56]" />
          <span className="block h-2.5 w-2.5 rounded-full bg-[#ffbd2e]" />
          <span className="block h-2.5 w-2.5 rounded-full bg-[#27c93f]" />
        </span>
        <span className="text-cc-nav-label text-caption font-mono tracking-tight">
          {breadcrumb}
        </span>
      </div>
      {stdoutLabel ? (
        <div className="border-cc-card-border bg-cc-code-bg border-b px-4 py-2">
          <span className="text-cc-accent text-caption font-mono tracking-[0.18em] uppercase">
            {stdoutLabel}
          </span>
        </div>
      ) : null}
      {children}
    </div>
  );
}

interface CodeBlockProps {
  readonly children: ReactNode;
}

/** Mono code body inside the chrome. Uses font-mono and cc-ink on cc-code-bg. */
function CodeBlock({ children }: CodeBlockProps) {
  return (
    <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[13px] leading-[1.65] sm:text-sm">
      {children}
    </pre>
  );
}

interface ProductFrameProps {
  readonly children: ReactNode;
}

/**
 * Frames a NitroReel screen so it reads as the stdout of the snippet above:
 * same code-bg background, no extra inner padding so the product screen fills
 * the chrome edge to edge.
 */
function ProductFrame({ children }: ProductFrameProps) {
  return <div className="bg-cc-code-bg overflow-hidden">{children}</div>;
}

interface ChapterEyebrowProps {
  readonly index: string;
  readonly label: string;
}

/** Left-flush `// 0X ── EYEBROW` mono rule on cc-accent. */
function ChapterEyebrow({ index, label }: ChapterEyebrowProps) {
  return (
    <div className="text-cc-accent text-caption flex items-center gap-2 font-mono tracking-[0.2em] uppercase">
      <span aria-hidden="true">{`// ${index}`}</span>
      <span aria-hidden="true" className="text-cc-nav-label">
        ──
      </span>
      <span>{label}</span>
    </div>
  );
}

interface ChapterProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly heading: string;
  readonly body: string;
  readonly snippet: ReactNode;
  readonly snippetBreadcrumb: string;
  readonly productBreadcrumb: string;
  readonly stdoutLabel: string;
  readonly visual: ReactNode;
}

/**
 * One numbered chapter: eyebrow rule, h3, body paragraph, code panel, then
 * matching product screen framed in identical chrome (reads as its stdout).
 */
function Chapter({
  id,
  index,
  eyebrow,
  heading,
  body,
  snippet,
  snippetBreadcrumb,
  productBreadcrumb,
  stdoutLabel,
  visual,
}: ChapterProps) {
  return (
    <section id={id} className="flex scroll-mt-24 flex-col gap-6">
      <ChapterEyebrow index={index} label={eyebrow} />
      <h2 className="text-cc-heading font-heading text-h3 max-w-2xl text-balance">
        {heading}
      </h2>
      <p className="text-cc-ink text-body max-w-2xl leading-relaxed">{body}</p>
      <PanelChrome breadcrumb={snippetBreadcrumb}>
        <CodeBlock>{snippet}</CodeBlock>
      </PanelChrome>
      <PanelChrome breadcrumb={productBreadcrumb} stdoutLabel={stdoutLabel}>
        <ProductFrame>{visual}</ProductFrame>
      </PanelChrome>
    </section>
  );
}

// ---- Inline code token helpers ---------------------------------------------

interface TokenProps {
  readonly children: ReactNode;
}

function Comment({ children }: TokenProps) {
  return <span className="text-cc-nav-label">{children}</span>;
}

function Accent({ children }: TokenProps) {
  return <span className="text-cc-accent">{children}</span>;
}

function Key({ children }: TokenProps) {
  return <span className="text-cc-heading">{children}</span>;
}

function Prompt() {
  return <span className="text-cc-accent">$ </span>;
}

// ---- Page ------------------------------------------------------------------

export default function NitroPreviewV5Page() {
  return (
    <div className="mx-auto w-full max-w-4xl px-4 pt-10 pb-24 sm:px-6 sm:pt-16">
      {/* HERO ---------------------------------------------------------- */}
      <section className="flex flex-col gap-6">
        <ChapterEyebrow index="nitro --help" label="The Operator's Console" />
        <h1 className="text-cc-heading font-heading text-hero text-balance">
          The Nitro GraphQL control plane, one command at a time.
        </h1>
        <p className="text-cc-ink text-lead max-w-2xl leading-tight text-pretty">
          Nitro is the cockpit for your GraphQL and .NET backend. Read it like a
          runbook: five chapters, five commands, each one a real piece of the
          control plane wired to its output on the page below.
        </p>
        <div className="mt-2 flex flex-wrap items-center gap-4">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch Nitro
          </OutlineButton>
        </div>

        <div className="mt-8">
          <PanelChrome breadcrumb="nitro › --help">
            <CodeBlock>
              <Prompt />
              nitro <Accent>--help</Accent>
              {"\n"}
              <Comment>
                {"# The GraphQL control plane, one command at a time."}
              </Comment>
              {"\n\n"}
              <Key>commands:</Key>
              {"\n"}
              {"  "}
              <Accent>login</Accent>
              {"        sign in to your Nitro workspace"}
              {"\n"}
              {"  "}
              <Accent>api</Accent>
              {"          manage APIs and their stages"}
              {"\n"}
              {"  "}
              <Accent>schema</Accent>
              {"       publish, validate, and download schemas"}
              {"\n"}
              {"  "}
              <Accent>client</Accent>
              {"       register clients and check changes"}
              {"\n"}
              {"  "}
              <Accent>fusion</Accent>
              {"       upload and publish Fusion source schemas"}
              {"\n\n"}
              <Comment>
                {
                  "# Five capabilities follow: observe, trace, diagnose, evolve, compose."
                }
              </Comment>
              {"\n"}
              <Comment>
                {"# Scroll on; the rest of this page is the man page."}
              </Comment>
            </CodeBlock>
          </PanelChrome>
        </div>
      </section>

      {/* CHAPTERS ------------------------------------------------------ */}
      <div className="mt-24 flex flex-col gap-24">
        <Chapter
          id="observe"
          index="01"
          eyebrow="Observe"
          heading="Wire up telemetry, watch operations as they run."
          body="Nitro talks OpenTelemetry. Add the instrumentation in your GraphQL server, point it at Nitro, and every operation shows up with latency, throughput, error rate, p95 and p99, per-client usage, and an impact score that ranks what hurts the system most."
          snippetBreadcrumb="nitro › observe › Program.cs"
          snippet={
            <>
              <Comment>{"// Program.cs"}</Comment>
              {"\n"}
              <Key>builder</Key>.Services
              {"\n"}
              {"  ."}
              <Accent>AddGraphQLServer</Accent>
              {"()"}
              {"\n"}
              {"  ."}
              <Accent>AddInstrumentation</Accent>
              {"()"}
              {"\n"}
              {"  ."}
              <Accent>AddQueryType</Accent>
              {"<Query>();"}
              {"\n\n"}
              <Key>builder</Key>.Services
              {"\n"}
              {"  ."}
              <Accent>AddNitro</Accent>
              {"()"}
              {"\n"}
              {"  ."}
              <Accent>AddOpenTelemetry</Accent>
              {"();"}
              {"\n\n"}
              <Comment>
                {"# Telemetry needs Nitro configuration; the line above is it."}
              </Comment>
            </>
          }
          productBreadcrumb="nitro › observe › live operations"
          stdoutLabel="# stdout ── live operations"
          visual={<NitroMonitoring className="w-full" />}
        />

        <Chapter
          id="trace"
          index="02"
          eyebrow="Trace"
          heading="Follow one request across your whole backend."
          body="One operation, one ID, one waterfall. Distributed tracing stitches the request across GraphQL, REST, gRPC, and background jobs, so you can walk the spans down to the resolver that ran slow."
          snippetBreadcrumb="nitro › trace › 8f3d2a"
          snippet={
            <>
              <Comment>
                {
                  "# trace 8f3d2a, fetched from Nitro for operation GetCart (last 5m)."
                }
              </Comment>
              {"\n\n"}
              <Key>trace 8f3d2a · 142ms · OK</Key>
              {"\n"}
              {"  POST /graphql                        142ms"}
              {"\n"}
              {"  ├─ parse + validate                    4ms"}
              {"\n"}
              {"  ├─ plan                                3ms"}
              {"\n"}
              {"  ├─ users.byId                         18ms"}
              {"\n"}
              {"  ├─ orders.recent                      "}
              <Accent>62ms</Accent>
              {"\n"}
              {"  │   └─ sql.select * from orders       58ms"}
              {"\n"}
              {"  ├─ shipments.batch                    34ms"}
              {"\n"}
              {"  └─ serialize                          11ms"}
              {"\n\n"}
              <Comment>
                {"# open the waterfall below to drill into any span →"}
              </Comment>
            </>
          }
          productBreadcrumb="nitro › trace › 8f3d2a"
          stdoutLabel="# stdout ── span waterfall"
          visual={<NitroTrace className="w-full" />}
        />

        <Chapter
          id="diagnose"
          index="03"
          eyebrow="Diagnose"
          heading="From an error spike to the line that threw it."
          body="When errors climb, Nitro takes you from the spike to the failing operation and the server-side stack trace behind it. No log spelunking, no theories: open the alert, read the frame, fix the line."
          snippetBreadcrumb="nitro › diagnose › alert"
          snippet={
            <>
              <Comment>{"# alert payload, posted by Nitro"}</Comment>
              {"\n"}
              <Key>level</Key>: <Accent>error</Accent>
              {"\n"}
              <Key>kind</Key>: SpikeDetected
              {"\n"}
              <Key>operation</Key>: <Accent>CheckoutMutation</Accent>
              {"\n"}
              <Key>window</Key>: last 2m
              {"\n"}
              <Key>rate</Key>: 12.3 err/min (baseline 0.4)
              {"\n\n"}
              <Comment>{"# server-side stack frame (most recent)"}</Comment>
              {"\n"}
              {"  at CheckoutHandler.CommitAsync"}
              {"\n"}
              {"     in Checkout/CheckoutHandler.cs:line "}
              <Accent>87</Accent>
              {"\n"}
              {"  at CheckoutResolvers.Checkout"}
              {"\n"}
              {"     in Resolvers/CheckoutResolvers.cs:line 42"}
              {"\n\n"}
              <Comment>{"# rendered diagnosis below ↓"}</Comment>
            </>
          }
          productBreadcrumb="nitro › diagnose › CheckoutMutation"
          stdoutLabel="# stdout ── rendered diagnosis"
          visual={<NitroDiagnose className="w-full" />}
        />

        <Chapter
          id="evolve"
          index="04"
          eyebrow="Evolve"
          heading="Change the schema without breaking your clients."
          body="Every change is classified as safe, dangerous, or breaking and checked against published clients in CI. You validate on a PR and publish only when it is safe to ship; published clients affected are listed before anything reaches production."
          snippetBreadcrumb="nitro › evolve › schema.graphql"
          snippet={
            <>
              <Comment>{"# schema.graphql · pull request #482"}</Comment>
              {"\n\n"}
              {"  type "}
              <Accent>Order</Accent>
              {" {"}
              {"\n"}
              {"    id: ID!"}
              {"\n"}
              <span className="text-cc-accent">{"  + "}</span>
              {"  trackingNumber: String   "}
              <Comment>{"# SAFE: additive field"}</Comment>
              {"\n"}
              <span className="text-[#f0786a]">{"  - "}</span>
              {"  status: String           "}
              <Comment>{"# BREAKING: removed field"}</Comment>
              {"\n"}
              <span className="text-cc-accent">{"  + "}</span>
              {"  state: OrderState!       "}
              <Comment>{"# SAFE: new non-null on new field"}</Comment>
              {"\n"}
              {"  }"}
              {"\n\n"}
              <Prompt />
              nitro schema <Accent>validate</Accent> \{"\n"}
              {"  "}
              <Accent>--api-id</Accent> 0xCART <Accent>--stage</Accent>{" "}
              production \{"\n"}
              {"  "}
              <Accent>--schema-file</Accent> ./schema.graphql
              {"\n"}
              <Comment>
                {"# validating against published clients… 1 breaking change."}
              </Comment>
              {"\n"}
              <Comment>
                {"# verdict and affected clients render below ↓"}
              </Comment>
            </>
          }
          productBreadcrumb="nitro › evolve › registry verdict"
          stdoutLabel="# stdout ── registry verdict"
          visual={<NitroSchema className="w-full" />}
        />

        <Chapter
          id="compose"
          index="05"
          eyebrow="Compose"
          heading="One graph, executed across every subgraph."
          body="Fusion composes your subgraphs at planning time, then runs them through a gateway you operate yourself. Nitro shows the distributed query plan: how one operation fans out into parallel, batched fetches across subgraphs and folds back into one response."
          snippetBreadcrumb="nitro › fusion › publish"
          snippet={
            <>
              <Comment>{"# Fusion: composition at planning time."}</Comment>
              {"\n"}
              <Prompt />
              nitro fusion <Accent>publish</Accent> \{"\n"}
              {"  "}
              <Accent>--api-id</Accent> 0xCART <Accent>--stage</Accent>{" "}
              production \{"\n"}
              {"  "}
              <Accent>--source-schema</Accent> users \{"\n"}
              {"  "}
              <Accent>--source-schema</Accent> orders \{"\n"}
              {"  "}
              <Accent>--source-schema</Accent> shipments
              {"\n"}
              <Comment>
                {"# composed 3 source schemas into one Fusion graph."}
              </Comment>
              {"\n\n"}
              <Comment>{"# query plan (illustrative)"}</Comment>
              {"\n"}
              {"  CartOverview"}
              {"\n"}
              {"  ├─ users.viewer            (1 fetch)"}
              {"\n"}
              {"  ├─ orders.recent           (1 fetch, batched x 24)"}
              {"\n"}
              {"  │   └─ shipments.byOrderId (1 fetch, batched x 24)"}
              {"\n"}
              {"  └─ join + serialize"}
              {"\n\n"}
              <Comment>
                {"# gateway runs on your infrastructure; executed plan below ↓"}
              </Comment>
            </>
          }
          productBreadcrumb="nitro › fusion › executed plan"
          stdoutLabel="# stdout ── executed query plan"
          visual={<NitroFusion className="w-full" />}
        />
      </div>

      {/* OUTRO CTA ----------------------------------------------------- */}
      <section className="mt-24 flex flex-col gap-6">
        <ChapterEyebrow index="end" label="Ready when you are" />
        <h2 className="text-cc-heading font-heading text-h2 max-w-2xl text-balance">
          Put your API on the control plane.
        </h2>
        <p className="text-cc-ink text-body max-w-2xl leading-relaxed">
          Start in the GraphQL IDE in seconds, then grow into observability,
          tracing, and a registry that keeps your schema and your published
          clients in sync.
        </p>

        <PanelChrome breadcrumb="nitro › ready when you are">
          <CodeBlock>
            <Comment>{"# ready when you are"}</Comment>
            {"\n"}
            <Prompt />
            nitro <Accent>login</Accent>
            {"\n"}
            <Prompt />
            open <Accent>https://nitro.chillicream.com</Accent>
            {"\n"}
          </CodeBlock>
          <div className="border-cc-card-border bg-cc-code-header border-t px-5 py-2">
            <span className="text-cc-accent text-caption font-mono tracking-[0.18em] uppercase">
              # stdout ── start here
            </span>
          </div>
          <div className="bg-cc-code-bg flex flex-wrap items-center gap-4 px-5 py-6">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
        </PanelChrome>
      </section>
    </div>
  );
}
