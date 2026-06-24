import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Fusion as FusionIcon } from "@/src/icons/Fusion";
import { NitroFusion } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Fusion, the distributed GraphQL gateway for .NET",
  description:
    "Fusion is ChilliCream's distributed GraphQL gateway. Compose subgraphs into one validated graph, served from a single .NET endpoint, with every plan traced.",
  keywords: [
    "Fusion",
    "distributed GraphQL gateway",
    "GraphQL composition",
    "GraphQL Composite Schemas",
    "Apollo Federation spec",
    "subgraph",
    "query plan",
    "Hot Chocolate",
    "ASP.NET Core gateway",
    "ChilliCream",
  ],
  openGraph: {
    title: "Fusion, the distributed GraphQL gateway for .NET",
    description:
      "Compose independent subgraphs into one validated graph. Plan, fetch in parallel, trace every step in Nitro. Built on Hot Chocolate, self-run on ASP.NET Core.",
  },
  robots: { index: false, follow: false },
};

// Brand spectrum used exactly once on the page, as the hero accent.
const SPECTRUM_GRADIENT =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

export default function FusionStoryPage() {
  return (
    <>
      <Hero />
      <NarrativeArc />
      <MitBand />
      <ClosingCta />
    </>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                       */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="relative py-16 text-center sm:py-24">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-8 mx-auto flex max-w-md justify-center opacity-60"
      >
        <FusionIcon className="h-24 w-auto" />
      </div>

      <div className="relative">
        <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
          Distributed GraphQL gateway, built on Hot Chocolate
        </div>

        <h1 className="text-cc-heading font-heading mx-auto max-w-4xl text-5xl leading-[1.05] font-semibold tracking-tight sm:text-6xl lg:text-7xl">
          See your query{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM_GRADIENT }}
          >
            become a plan
          </span>
          .
        </h1>

        <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-base sm:text-lg">
          Fusion composes independent subgraphs into one validated graph at
          planning time, then serves it from a single .NET endpoint you operate
          yourself. Every request becomes a distributed plan, executed in
          parallel across your services, traced span by span in Nitro.
        </p>

        <div className="mt-8 flex flex-wrap justify-center gap-4">
          <SolidButton href="/docs/fusion">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>

        <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 font-mono text-xs tracking-wide">
          <HeroBullet>MIT licensed</HeroBullet>
          <HeroBullet>Apollo Federation spec compatible</HeroBullet>
          <HeroBullet>Self-run on ASP.NET Core</HeroBullet>
        </div>
      </div>
    </section>
  );
}

function HeroBullet({ children }: { readonly children: ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span
        aria-hidden
        className="bg-cc-accent inline-block size-1.5 rounded-full"
      />
      {children}
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/*  The five-step narrative arc                                                */
/* -------------------------------------------------------------------------- */

function NarrativeArc() {
  return (
    <section className="py-16">
      <div className="mb-12 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          From schema to span, in five steps
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          One request. Five moves. Every one of them observable.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          A distributed graph stops being a black box when you can see each
          stage. Subgraphs publish, composition proves, the gateway plans, the
          plan executes in parallel, Nitro traces the result. That is Fusion.
        </p>
      </div>

      <ol className="space-y-12">
        <Step
          index="01"
          eyebrow="Publish"
          title="Subgraphs publish their schemas."
          blurb="Every Hot Chocolate server is already a valid Fusion subgraph, no resolver changes required. Each team owns its service and its schema; Fusion only needs to know where to find them."
        >
          <CodeFrame language="C# / Checkout subgraph, Program.cs">
            <CsLine>
              <Kw>var</Kw> builder = WebApplication.CreateBuilder(args);
            </CsLine>
            <CsLine />
            <CsLine>builder.Services</CsLine>
            <CsLine indent={1}>.AddGraphQLServer()</CsLine>
            <CsLine indent={1}>
              .AddTypes()<Cmt>{` // a normal Hot Chocolate server`}</Cmt>
            </CsLine>
            <CsLine indent={1}>
              .RegisterDbContext{`<`}
              <Type>CheckoutDb</Type>
              {`>`}();
            </CsLine>
            <CsLine />
            <CsLine>
              <Kw>var</Kw> app = builder.Build();
            </CsLine>
            <CsLine>
              app.MapGraphQL();{" "}
              <Cmt>{`// publishes /graphql, ready for Fusion`}</Cmt>
            </CsLine>
            <CsLine>app.Run();</CsLine>
          </CodeFrame>
        </Step>

        <Step
          index="02"
          eyebrow="Compose"
          title="Composition validates and proves the graph."
          blurb="Composition runs at build or CI time. It merges every subgraph schema, checks them against each other, and proves satisfiability: every reachable field can actually be resolved across your fleet. If it composes, it answers."
        >
          <CodeFrame language="terminal">
            <TermLine prompt>
              nitro fusion compose --output ./gateway.far
            </TermLine>
            <TermLine muted>
              {`  loading source schemas (Catalog, Checkout, Users)`}
            </TermLine>
            <TermLine muted>{`  validating pre-merge invariants`}</TermLine>
            <TermLine muted>{`  merging composite schema`}</TermLine>
            <TermLine muted>
              {`  validating satisfiability (every reachable field is resolvable)`}
            </TermLine>
            <TermLine ok>composition succeeded, wrote gateway.far</TermLine>
          </CodeFrame>
          <Aside>
            A query that succeeds against the gateway is one your services can
            actually answer, because composition has already proven it.
          </Aside>
        </Step>

        <Step
          index="03"
          eyebrow="Receive"
          title="A query arrives at your gateway."
          blurb="The gateway is your ASP.NET Core app, never a hosted hop. Your DI, your auth, your middleware. Fusion loads the composed archive, exposes one endpoint, and accepts the query through the same pipeline you already operate."
        >
          <div className="grid gap-4 lg:grid-cols-2">
            <CodeFrame language="C# / Gateway, Program.cs">
              <CsLine>builder.Services</CsLine>
              <CsLine indent={1}>.AddGraphQLGateway()</CsLine>
              <CsLine indent={1}>
                .AddFileSystemConfiguration(<Str>{`"./gateway.far"`}</Str>);
              </CsLine>
              <CsLine />
              <CsLine>
                <Kw>var</Kw> app = builder.Build();
              </CsLine>
              <CsLine>
                app.UseAuthentication(); <Cmt>{`// your auth, in process`}</Cmt>
              </CsLine>
              <CsLine>app.MapGraphQL();</CsLine>
              <CsLine>app.Run();</CsLine>
            </CodeFrame>

            <CodeFrame language="GraphQL / incoming query">
              <CsLine>
                <Kw>query</Kw> <Type>Checkout</Type>(
                <span className="text-cc-cta">$id</span>: <Type>ID!</Type>){" "}
                {`{`}
              </CsLine>
              <CsLine indent={1}>
                cart(id: <span className="text-cc-cta">$id</span>) {`{`}
              </CsLine>
              <CsLine indent={2}>total</CsLine>
              <CsLine indent={2}>items {`{`}</CsLine>
              <CsLine indent={3}>quantity</CsLine>
              <CsLine indent={3}>
                product {`{`} name price {`}`}
              </CsLine>
              <CsLine indent={2}>{`}`}</CsLine>
              <CsLine indent={2}>
                customer {`{`} email tier {`}`}
              </CsLine>
              <CsLine indent={1}>{`}`}</CsLine>
              <CsLine>{`}`}</CsLine>
            </CodeFrame>
          </div>
        </Step>

        <Step
          index="04"
          eyebrow="Plan"
          title="Fusion plans parallel, batched fetches."
          blurb="The planner reads the composite schema, splits the query across the subgraphs that own each field, and orders the fetches: parallel where possible, batched per key, sequenced only where the next call depends on the last. The plan is data, not magic, and you can inspect it."
        >
          <PlanDiagram />
          <Aside>
            Cart and customer fan out in parallel, then product details are
            batched into one fetch keyed by the product ids returned from the
            cart. Three services, one round of parallel work, one merged
            response.
          </Aside>
        </Step>

        <Step
          index="05"
          eyebrow="Trace"
          title="The plan is traced into Nitro."
          blurb="Fusion emits OpenTelemetry spans for the gateway request, the planning step, and every plan node. Nitro picks them up as a live query-plan visualisation so you can see which subgraph took how long, where data was batched, and where time is actually being spent."
        >
          <div className="border-cc-card-border bg-cc-card-bg mx-auto max-w-5xl overflow-hidden rounded-xl border backdrop-blur-sm">
            <NitroFusion />
          </div>
          <p className="text-cc-ink-dim mt-3 text-center font-mono text-xs tracking-wide">
            Nitro Fusion view, every subgraph call hung off the plan that
            produced it.
          </p>
        </Step>
      </ol>
    </section>
  );
}

interface StepProps {
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly blurb: string;
  readonly children: ReactNode;
}

function Step({ index, eyebrow, title, blurb, children }: StepProps) {
  return (
    <li className="grid gap-6 lg:grid-cols-[14rem_1fr] lg:gap-10">
      <div className="lg:pt-2">
        <div className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
          {index} {eyebrow}
        </div>
        <h3 className="text-cc-heading mt-2 text-2xl font-semibold tracking-tight">
          {title}
        </h3>
        <p className="text-cc-ink-dim mt-3 text-sm sm:text-base">{blurb}</p>
      </div>
      <div>{children}</div>
    </li>
  );
}

function Aside({ children }: { readonly children: ReactNode }) {
  return (
    <p className="text-cc-ink-dim border-cc-accent/40 mt-4 border-l-2 pl-4 text-sm italic sm:text-base">
      {children}
    </p>
  );
}

/* -------------------------------------------------------------------------- */
/*  Step 04, inline plan diagram                                               */
/* -------------------------------------------------------------------------- */

function PlanDiagram() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <div className="border-cc-card-border bg-cc-surface/60 text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px] tracking-wide">
        <span className="uppercase">Query plan</span>
        <span className="uppercase">3 nodes, 2 levels</span>
      </div>
      <div className="px-5 py-6">
        <svg
          viewBox="0 0 640 240"
          className="h-auto w-full"
          role="img"
          aria-label="Query plan diagram, parallel cart and customer fetch followed by a batched product fetch."
        >
          <defs>
            <linearGradient id="fusion-plan-edge" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0" stopColor="#5eead4" stopOpacity="0.9" />
              <stop offset="1" stopColor="#5eead4" stopOpacity="0.2" />
            </linearGradient>
          </defs>

          {/* root */}
          <g>
            <circle cx="60" cy="120" r="10" fill="#5eead4" />
            <text
              x="60"
              y="156"
              textAnchor="middle"
              fill="#a1a3af"
              fontFamily="ui-monospace, monospace"
              fontSize="11"
            >
              gateway
            </text>
          </g>

          {/* edges to parallel nodes */}
          <path
            d="M 70 120 C 140 120, 160 60, 240 60"
            stroke="url(#fusion-plan-edge)"
            strokeWidth="2"
            fill="none"
          />
          <path
            d="M 70 120 C 140 120, 160 190, 240 190"
            stroke="url(#fusion-plan-edge)"
            strokeWidth="2"
            fill="none"
          />

          {/* parallel: cart */}
          <PlanNode
            x={240}
            y={60}
            label="Fetch (parallel)"
            subgraph="Checkout"
            fields="cart, items"
          />

          {/* parallel: customer */}
          <PlanNode
            x={240}
            y={190}
            label="Fetch (parallel)"
            subgraph="Users"
            fields="customer"
          />

          {/* edge from cart to batched product */}
          <path
            d="M 380 60 C 460 60, 480 120, 540 120"
            stroke="url(#fusion-plan-edge)"
            strokeWidth="2"
            fill="none"
            strokeDasharray="4 3"
          />

          {/* batched product fetch */}
          <PlanNode
            x={540}
            y={120}
            label="Fetch (batched)"
            subgraph="Catalog"
            fields="product[id]"
          />

          {/* level labels */}
          <text
            x="240"
            y="22"
            textAnchor="middle"
            fill="#62748e"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            letterSpacing="1.5"
          >
            LEVEL 1, PARALLEL
          </text>
          <text
            x="540"
            y="22"
            textAnchor="middle"
            fill="#62748e"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            letterSpacing="1.5"
          >
            LEVEL 2, BATCHED
          </text>
        </svg>
      </div>
    </div>
  );
}

interface PlanNodeProps {
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly subgraph: string;
  readonly fields: string;
}

function PlanNode({ x, y, label, subgraph, fields }: PlanNodeProps) {
  const w = 140;
  const h = 60;
  return (
    <g transform={`translate(${x - w / 2}, ${y - h / 2})`}>
      <rect
        x="0"
        y="0"
        width={w}
        height={h}
        rx="8"
        ry="8"
        fill="#0c1322"
        stroke="rgba(245, 241, 234, 0.16)"
        strokeWidth="1"
      />
      <text
        x="12"
        y="20"
        fill="#5eead4"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        letterSpacing="1"
      >
        {label.toUpperCase()}
      </text>
      <text
        x="12"
        y="38"
        fill="#f5f0ea"
        fontFamily="ui-sans-serif, system-ui"
        fontSize="13"
        fontWeight="600"
      >
        {subgraph}
      </text>
      <text
        x="12"
        y="52"
        fill="#a1a3af"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
      >
        {fields}
      </text>
    </g>
  );
}

/* -------------------------------------------------------------------------- */
/*  Code framing helpers                                                       */
/* -------------------------------------------------------------------------- */

interface CodeFrameProps {
  readonly language: string;
  readonly children: ReactNode;
  readonly flush?: boolean;
}

function CodeFrame({ language, children, flush = false }: CodeFrameProps) {
  const outer = flush
    ? "overflow-hidden"
    : "border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm";
  return (
    <div className={outer}>
      <div className="border-cc-card-border bg-cc-surface/60 text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px] tracking-wide">
        <div className="flex items-center gap-2">
          <span
            aria-hidden
            className="bg-cc-status-firing inline-block size-2 rounded-full opacity-70"
          />
          <span
            aria-hidden
            className="bg-cc-status-investigating inline-block size-2 rounded-full opacity-70"
          />
          <span
            aria-hidden
            className="bg-cc-status-healthy inline-block size-2 rounded-full opacity-70"
          />
        </div>
        <span className="uppercase">{language}</span>
      </div>
      <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[13px] leading-[1.65]">
        <code>{children}</code>
      </pre>
    </div>
  );
}

interface CsLineProps {
  readonly children?: ReactNode;
  readonly indent?: number;
}

function CsLine({ children, indent = 0 }: CsLineProps) {
  const pad = "  ".repeat(indent);
  return (
    <div>
      {pad}
      {children}
      {"\n"}
    </div>
  );
}

function Kw({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-accent">{children}</span>;
}

function Type({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-warning">{children}</span>;
}

function Str({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-success">{children}</span>;
}

function Cmt({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-ink-dim italic">{children}</span>;
}

interface TermLineProps {
  readonly children: ReactNode;
  readonly prompt?: boolean;
  readonly muted?: boolean;
  readonly ok?: boolean;
}

function TermLine({ children, prompt, muted, ok }: TermLineProps) {
  let cls = "text-cc-ink";
  if (muted) cls = "text-cc-ink-dim";
  if (ok) cls = "text-cc-success";
  return (
    <div className={cls}>
      {prompt ? <span className="text-cc-accent">$ </span> : null}
      {children}
      {"\n"}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  MIT / open-source band                                                     */
/* -------------------------------------------------------------------------- */

function MitBand() {
  return (
    <section className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 text-center backdrop-blur-sm sm:p-12">
        <div className="text-cc-accent mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          MIT licensed, open source
        </div>
        <h2 className="text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
          Your gateway, your binary. No hosted hop in the path.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
          Fusion is open source under the MIT license and developed in the open
          on GitHub. The gateway is always your ASP.NET Core process; the
          composed archive is yours to inspect, diff, version, and ship.
        </p>
        <ul className="text-cc-ink mx-auto mt-6 grid max-w-2xl gap-2 text-left text-sm sm:grid-cols-2">
          <PromiseLine>
            Composition is a build step, not a cloud call
          </PromiseLine>
          <PromiseLine>
            Compatible with the Apollo Federation spec for subgraph interop
          </PromiseLine>
          <PromiseLine>
            Distributed tracing on the request, plan, and each subgraph fetch
          </PromiseLine>
          <PromiseLine>
            Same Hot Chocolate server can serve as a Fusion subgraph
          </PromiseLine>
        </ul>
        <div className="mt-7 flex flex-wrap justify-center gap-4">
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform/blob/main/LICENSE">
            Read the license
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

function PromiseLine({ children }: { readonly children: ReactNode }) {
  return (
    <li className="flex items-start gap-2">
      <span className="text-cc-accent mt-1 shrink-0">
        <CheckIcon />
      </span>
      <span>{children}</span>
    </li>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA                                                                */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="py-20 text-center">
      <h2 className="text-cc-heading mx-auto max-w-3xl text-3xl font-semibold tracking-tight sm:text-4xl">
        Compose the graph,{" "}
        <span className="text-cc-accent">run the gateway</span>, watch the plan.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base sm:text-lg">
        Point Fusion at your first Hot Chocolate server, compose the archive,
        and serve it from your own ASP.NET Core app. Add subgraphs when you are
        ready.
      </p>

      <div className="mx-auto mt-8 max-w-xl">
        <CodeFrame language="terminal">
          <TermLine prompt>
            dotnet tool install -g ChilliCream.Nitro.CLI
          </TermLine>
          <TermLine prompt>
            nitro fusion compose --output ./gateway.far
          </TermLine>
          <TermLine prompt>dotnet run --project ./gateway</TermLine>
        </CodeFrame>
      </div>

      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/fusion">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
    </section>
  );
}
