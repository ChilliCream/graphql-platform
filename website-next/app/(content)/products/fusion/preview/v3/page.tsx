import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Fusion as FusionIcon } from "@/src/icons/Fusion";
import { NitroFusion } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Fusion: The .NET-native distributed GraphQL gateway",
  description:
    ".NET-native distributed GraphQL gateway. Compose subgraphs at planning time, prove every query is answerable, run the gateway in your ASP.NET Core app.",
  keywords: [
    "Fusion",
    "distributed GraphQL gateway",
    ".NET GraphQL gateway",
    "GraphQL composition",
    "GraphQL Composite Schemas",
    "Apollo Federation spec",
    "ASP.NET Core gateway",
    "Hot Chocolate",
    "query plan tracing",
    "Nitro",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Fusion: The .NET-native distributed GraphQL gateway",
    description:
      "Compose independent subgraphs into one validated graph. Plan-time composition, proven answerable, self-run ASP.NET Core gateway built on Hot Chocolate.",
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
        <Eyebrow>Distributed GraphQL Gateway</Eyebrow>
        <h1 className="text-cc-heading font-heading text-hero mt-5 tracking-tight">
          The{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            .NET-native
          </span>{" "}
          distributed GraphQL gateway.
        </h1>
        <p className="text-cc-ink-dim lead mx-auto mt-6 max-w-2xl">
          Fusion composes independent subgraphs into one validated graph and
          serves it from a single endpoint. Composition runs at planning time
          (not at every request), the gateway is your ASP.NET Core app, and the
          query plan is traced end to end into Nitro.
        </p>
        <div className="mt-9 flex flex-wrap justify-center gap-4">
          <SolidButton href="/docs/fusion">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>
        <div className="text-cc-ink-dim mt-6 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase">
          <span>MIT licensed</span>
          <span aria-hidden className="text-cc-ink-faint">
            /
          </span>
          <span>ASP.NET Core</span>
          <span aria-hidden className="text-cc-ink-faint">
            /
          </span>
          <span>Built on Hot Chocolate</span>
          <span aria-hidden className="text-cc-ink-faint">
            /
          </span>
          <span>Apollo Federation spec compatible</span>
        </div>
      </div>
      <div className="mt-12 flex justify-center">
        <FusionIcon
          className="h-24 w-auto opacity-90"
          style={{
            filter: "drop-shadow(0 18px 40px rgba(94, 234, 212, 0.18))",
          }}
        />
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// "What makes Fusion different" wedge cards
// ---------------------------------------------------------------------------

interface PillarCardProps {
  readonly index: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

function PillarCard({ index, title, body, bullets }: PillarCardProps) {
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

const PILLARS: readonly PillarCardProps[] = [
  {
    index: "01",
    title: "Composition validated at planning time",
    body: "The composition pipeline walks every reachable field in the merged graph and proves it can be resolved across your subgraphs. A query the gateway accepts is one your services can actually answer.",
    bullets: [
      "Type, enum, and field conflicts fail like compile errors",
      "Satisfiability proof catches unresolvable paths before deploy",
      "Composition output is a versioned, inspectable Fusion archive",
    ],
  },
  {
    index: "02",
    title: ".NET-native, built on Hot Chocolate",
    body: "Fusion is an ASP.NET Core app, not a Rust binary configured by YAML and not a Node runtime. DI, auth, header propagation, and middleware all run in .NET, end to end, on the same Hot Chocolate execution engine you already know.",
    bullets: [
      "AddGraphQLGateway() and you own the host",
      "Any Hot Chocolate server is already a valid subgraph",
      "No JVM, no Rust gateway, no JS runtime in the path",
    ],
  },
  {
    index: "03",
    title: "Always self-run, never a hosted hop",
    body: "Fusion's gateway runs where you run it. There is no required cloud control plane, no mandatory hosted hop on the request path, and no extra dependency between your clients and your subgraphs.",
    bullets: [
      "Composition runs locally or in CI, fully offline",
      "Load the Fusion archive from disk or your own pipeline",
      "Optional Nitro for managed delivery, never required",
    ],
  },
  {
    index: "04",
    title: "Query plan traced into Nitro",
    body: "Every request emits OpenTelemetry spans for ExecuteRequest, PlanOperation, and each ExecutePlanNode subgraph fetch. Nitro reads those spans and shows the distributed query plan as a graph, with timings per branch.",
    bullets: [
      "Native ActivitySource, exports to any OTel backend",
      "See parallel and batched subgraph fetches per request",
      "Find the slow subgraph in a federated query in seconds",
    ],
  },
];

function PillarsSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The wedge"
        title="What makes Fusion different"
        lead="Distributed GraphQL gateways exist. Fusion is the one a .NET architect can operate without leaving the .NET stack, while keeping composition honest and the request path under your control."
      />
      <div className="mt-12 grid gap-5 md:grid-cols-2">
        {PILLARS.map((p) => (
          <PillarCard key={p.index} {...p} />
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Soft factual comparison band (Apollo Router / Cosmo). Facts only, no swipes.
// ---------------------------------------------------------------------------

interface GatewayFactCardProps {
  readonly name: string;
  readonly accent: string;
  readonly runtime: string;
  readonly config: string;
  readonly composition: string;
  readonly path: string;
  readonly highlight?: boolean;
}

function GatewayFactCard({
  name,
  accent,
  runtime,
  config,
  composition,
  path,
  highlight,
}: GatewayFactCardProps) {
  const ring = highlight
    ? "ring-1 ring-cc-accent/40"
    : "ring-1 ring-transparent";
  return (
    <article
      className={`border-cc-card-border bg-cc-card-bg ${ring} flex h-full flex-col rounded-2xl border p-6 backdrop-blur-sm`}
    >
      <div
        className="font-mono text-[11px] tracking-[0.3em] uppercase"
        style={{ color: accent }}
      >
        {highlight ? "This page" : "Reference"}
      </div>
      <h3 className="text-cc-heading font-heading mt-2 text-lg tracking-tight">
        {name}
      </h3>
      <dl className="text-cc-ink mt-5 space-y-3 text-sm">
        <div>
          <dt className="text-cc-ink-dim text-xs tracking-wider uppercase">
            Runtime
          </dt>
          <dd className="mt-1">{runtime}</dd>
        </div>
        <div>
          <dt className="text-cc-ink-dim text-xs tracking-wider uppercase">
            Configuration
          </dt>
          <dd className="mt-1">{config}</dd>
        </div>
        <div>
          <dt className="text-cc-ink-dim text-xs tracking-wider uppercase">
            Composition model
          </dt>
          <dd className="mt-1">{composition}</dd>
        </div>
        <div>
          <dt className="text-cc-ink-dim text-xs tracking-wider uppercase">
            Request path
          </dt>
          <dd className="mt-1">{path}</dd>
        </div>
      </dl>
    </article>
  );
}

function ComparisonSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Honest deltas"
        title="How distributed GraphQL gateways line up"
        lead="Factual side by side. Pick the column that matches the stack your team operates and read across. No swipes; the differences are what they are."
      />
      <div className="mx-auto mt-12 grid max-w-5xl gap-5 md:grid-cols-3">
        <GatewayFactCard
          name="Fusion"
          accent="#5eead4"
          runtime="ASP.NET Core (.NET 8+)"
          config="C# program (AddGraphQLGateway), full DI and middleware"
          composition="Plan-time composition into a Fusion archive (.far), satisfiability proof"
          path="Self-run, no required cloud hop"
          highlight
        />
        <GatewayFactCard
          name="Apollo Router"
          accent="#7c92c6"
          runtime="Rust"
          config="Standalone binary, YAML configuration"
          composition="Composition via Rover, typically delivered through GraphOS"
          path="Self-host the binary, optional GraphOS managed delivery"
        />
        <GatewayFactCard
          name="WunderGraph Cosmo"
          accent="#f0786a"
          runtime="Go (router) and Rust (engine)"
          config="Standalone binary, YAML configuration"
          composition="Composition via the Cosmo control plane (self-hostable)"
          path="Self-host the router, control plane self-hostable or managed"
        />
      </div>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-3xl text-center text-sm">
        All three implement a federation-style composite graph. The deltas are
        about the runtime your platform team operates and where composition
        lives, not about who is faster on a given benchmark.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Real-feel code snippet (C# gateway + CLI compose)
// ---------------------------------------------------------------------------

// Color tokens. Kept inline so the snippet ships as static HTML with no client JS.
const TOK = {
  kw: "text-[#7c92c6]", // keyword (violet)
  type: "text-[#16b9e4]", // type / class (cyan)
  attr: "text-cc-accent", // attributes / directives
  str: "text-[#f0786a]", // string (coral)
  com: "text-cc-ink-dim", // comment
  ident: "text-cc-heading",
  punct: "text-cc-ink",
};

function CodeExample() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The shape of code"
        title="Compose subgraphs, run the gateway"
        lead="Compose your subgraphs with the Nitro CLI to produce a Fusion archive. Point a small ASP.NET Core program at that archive and you have a federated GraphQL endpoint."
      />
      <div className="mx-auto mt-10 grid max-w-5xl gap-5 lg:grid-cols-2">
        <div className="border-cc-card-border bg-cc-surface/80 overflow-hidden rounded-2xl border shadow-2xl shadow-black/40 backdrop-blur-sm">
          <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
            <div className="flex items-center gap-2">
              <span className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-success/70 h-2.5 w-2.5 rounded-full" />
            </div>
            <span className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
              compose.sh
            </span>
          </div>
          <pre className="overflow-x-auto p-5 font-mono text-[13px] leading-relaxed">
            <code>
              <span className={TOK.com}>
                {
                  "# Compose subgraphs at planning time. Runs offline, no cloud required."
                }
              </span>
              {"\n"}
              <span className={TOK.ident}>nitro</span>{" "}
              <span className={TOK.ident}>fusion</span>{" "}
              <span className={TOK.ident}>compose</span>{" "}
              <span className={TOK.punct}>\</span>
              {"\n  "}
              <span className={TOK.kw}>--source</span>{" "}
              <span className={TOK.str}>./subgraphs/catalog.graphql</span>{" "}
              <span className={TOK.punct}>\</span>
              {"\n  "}
              <span className={TOK.kw}>--source</span>{" "}
              <span className={TOK.str}>./subgraphs/billing.graphql</span>{" "}
              <span className={TOK.punct}>\</span>
              {"\n  "}
              <span className={TOK.kw}>--source</span>{" "}
              <span className={TOK.str}>./subgraphs/shipping.graphql</span>{" "}
              <span className={TOK.punct}>\</span>
              {"\n  "}
              <span className={TOK.kw}>--output</span>{" "}
              <span className={TOK.str}>./gateway.far</span>
              {"\n\n"}
              <span className={TOK.com}>
                {
                  "# Validate against a target stage in CI for breaking-change checks."
                }
              </span>
              {"\n"}
              <span className={TOK.ident}>nitro</span>{" "}
              <span className={TOK.ident}>fusion</span>{" "}
              <span className={TOK.ident}>validate</span>{" "}
              <span className={TOK.kw}>--stage</span>{" "}
              <span className={TOK.str}>prod</span>
              {"\n\n"}
              <span className={TOK.com}>
                {
                  "# Example diagnostic, halts before deploy if the graph is unsound:"
                }
              </span>
              {"\n"}
              <span className={TOK.com}>
                {"# error UNSATISFIABLE_QUERY_PATH: Order.shippingAddress"}
              </span>
              {"\n"}
              <span className={TOK.com}>
                {"#   no subgraph can resolve this field from Order keys"}
              </span>
            </code>
          </pre>
        </div>
        <div className="border-cc-card-border bg-cc-surface/80 overflow-hidden rounded-2xl border shadow-2xl shadow-black/40 backdrop-blur-sm">
          <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2.5">
            <div className="flex items-center gap-2">
              <span className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full" />
              <span className="bg-cc-success/70 h-2.5 w-2.5 rounded-full" />
            </div>
            <span className="text-cc-ink-dim font-mono text-xs tracking-widest uppercase">
              Program.cs
            </span>
          </div>
          <pre className="overflow-x-auto p-5 font-mono text-[13px] leading-relaxed">
            <code>
              <span className={TOK.com}>
                {"// Your gateway is an ordinary ASP.NET Core program."}
              </span>
              {"\n"}
              <span className={TOK.kw}>using </span>
              <span className={TOK.type}>HotChocolate.Fusion</span>
              <span className={TOK.punct}>;</span>
              {"\n\n"}
              <span className={TOK.kw}>var </span>
              <span className={TOK.ident}>builder</span>{" "}
              <span className={TOK.punct}>=</span>{" "}
              <span className={TOK.type}>WebApplication</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>CreateBuilder</span>
              <span className={TOK.punct}>{"(args);"}</span>
              {"\n\n"}
              <span className={TOK.ident}>builder</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Services</span>
              {"\n  "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>AddGraphQLGateway</span>
              <span className={TOK.punct}>{"()"}</span>
              {"\n  "}
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>AddFileSystemConfiguration</span>
              <span className={TOK.punct}>{"("}</span>
              <span className={TOK.str}>&quot;./gateway.far&quot;</span>
              <span className={TOK.punct}>{");"}</span>
              {"\n\n"}
              <span className={TOK.kw}>var </span>
              <span className={TOK.ident}>app</span>{" "}
              <span className={TOK.punct}>=</span>{" "}
              <span className={TOK.ident}>builder</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Build</span>
              <span className={TOK.punct}>{"();"}</span>
              {"\n\n"}
              <span className={TOK.ident}>app</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>MapGraphQL</span>
              <span className={TOK.punct}>{"();"}</span>
              {"\n"}
              <span className={TOK.ident}>app</span>
              <span className={TOK.punct}>.</span>
              <span className={TOK.ident}>Run</span>
              <span className={TOK.punct}>{"();"}</span>
              {"\n\n"}
              <span className={TOK.com}>
                {"// DI, auth, header propagation: standard ASP.NET Core."}
              </span>
              {"\n"}
              <span className={TOK.com}>
                {"// OpenTelemetry spans on every request and plan node."}
              </span>
            </code>
          </pre>
        </div>
      </div>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-3xl text-center text-sm">
        The same Hot Chocolate server can be a Fusion subgraph with no resolver
        changes. Start with one service, add subgraphs when the domain calls for
        it.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Nitro embed (single product card on this page)
// ---------------------------------------------------------------------------

function NitroEmbed() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="Trace the distributed query plan"
        title="See where time goes across subgraphs"
        lead="Fusion plans the query, executes parallel and batched subgraph fetches, and emits OpenTelemetry spans for each branch. Nitro reads those spans and renders the distributed query plan as a graph you can drill into."
      />
      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-5xl overflow-hidden rounded-xl border backdrop-blur-sm">
        <NitroFusion />
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Compact capability catalogue
// ---------------------------------------------------------------------------

interface FeatureProps {
  readonly title: string;
  readonly body: string;
  readonly icon: ReactNode;
}

function FeatureIcon({ children }: { children: ReactNode }) {
  return (
    <span
      aria-hidden
      className="border-cc-card-border bg-cc-bg/60 text-cc-accent inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border"
    >
      {children}
    </span>
  );
}

function IconCompose() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="6" cy="6" r="2.2" />
      <circle cx="18" cy="6" r="2.2" />
      <circle cx="6" cy="18" r="2.2" />
      <circle cx="18" cy="18" r="2.2" />
      <circle cx="12" cy="12" r="2.6" />
      <path d="M7.5 7.5 10 10" />
      <path d="M16.5 7.5 14 10" />
      <path d="M7.5 16.5 10 14" />
      <path d="M16.5 16.5 14 14" />
    </svg>
  );
}

function IconShield() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <path d="M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6l-8-3Z" />
      <path d="m8.5 12 2.5 2.5L15.5 10" />
    </svg>
  );
}

function IconPlan() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="5" cy="6" r="1.6" />
      <circle cx="5" cy="12" r="1.6" />
      <circle cx="5" cy="18" r="1.6" />
      <path d="M9 6h11" />
      <path d="M9 12h8" />
      <path d="M9 18h11" />
    </svg>
  );
}

function IconLive() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <circle cx="12" cy="12" r="2" />
      <path d="M7.5 7.5a6 6 0 0 0 0 9" />
      <path d="M16.5 7.5a6 6 0 0 1 0 9" />
      <path d="M4.5 4.5a10 10 0 0 0 0 15" />
      <path d="M19.5 4.5a10 10 0 0 1 0 15" />
    </svg>
  );
}

function IconCache() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <ellipse cx="12" cy="5.5" rx="7" ry="2.5" />
      <path d="M5 5.5v6c0 1.4 3.1 2.5 7 2.5s7-1.1 7-2.5v-6" />
      <path d="M5 11.5v6c0 1.4 3.1 2.5 7 2.5s7-1.1 7-2.5v-6" />
    </svg>
  );
}

function IconAspire() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
    >
      <rect x="3" y="4" width="7" height="7" rx="1.5" />
      <rect x="14" y="4" width="7" height="7" rx="1.5" />
      <rect x="3" y="14" width="7" height="7" rx="1.5" />
      <rect x="14" y="14" width="7" height="7" rx="1.5" />
      <path d="M10 7.5h4" />
      <path d="M10 17.5h4" />
      <path d="M6.5 11v3" />
      <path d="M17.5 11v3" />
    </svg>
  );
}

const FEATURES: readonly FeatureProps[] = [
  {
    title: "Plan-time composition",
    body: "Fusion plans subgraph composition before traffic ever arrives. The runtime gateway reads a versioned archive, not a recomputed schema per request.",
    icon: <IconCompose />,
  },
  {
    title: "Apollo Federation spec compatible",
    body: "Bring existing Apollo Federation v2 subgraphs into Fusion. Composition rewrites the directives so subgraphs stay where they are during a migration.",
    icon: <IconShield />,
  },
  {
    title: "Distributed query planner",
    body: "Per request, the planner picks parallel and batched subgraph fetches, deduplicates identical in-flight queries, and enforces a concurrency gate.",
    icon: <IconPlan />,
  },
  {
    title: "Subscriptions over SSE and WebSocket",
    body: "Real-time updates flow through the gateway via Server-Sent Events or WebSocket, with subgraph providers for Redis, NATS, RabbitMQ, and Postgres.",
    icon: <IconLive />,
  },
  {
    title: "Cache control, end to end",
    body: "Subgraph @cacheControl policies are merged conservatively into one safe HTTP Cache-Control and Vary policy. Persisted operations keep cache keys CDN-friendly.",
    icon: <IconCache />,
  },
  {
    title: ".NET Aspire integration",
    body: "An Aspire AppHost can compose live subgraphs at build, write the archive, and start the gateway. Tight inner-loop dev, no manual export and restart.",
    icon: <IconAspire />,
  },
];

function CatalogueSection() {
  return (
    <section className="py-16 sm:py-20">
      <SectionHeader
        eyebrow="The catalogue"
        title="Everything you expect from a serious GraphQL gateway"
        lead="Beyond the wedge, Fusion ships the boring necessities a federated platform needs to run in production."
      />
      <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {FEATURES.map((f) => (
          <div
            key={f.title}
            className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full gap-4 rounded-xl border p-5 backdrop-blur-sm transition-colors"
          >
            <FeatureIcon>{f.icon}</FeatureIcon>
            <div>
              <h3 className="text-cc-heading font-heading text-base tracking-tight">
                {f.title}
              </h3>
              <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
                {f.body}
              </p>
            </div>
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
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px opacity-70"
          style={{ background: SPECTRUM }}
        />
        <div className="flex flex-col items-start gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="max-w-2xl">
            <Eyebrow>Open source</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-2xl tracking-tight sm:text-3xl">
              MIT licensed. Run it where you want.
            </h2>
            <p className="text-cc-ink-dim mt-3 text-sm sm:text-base">
              Fusion is released under the MIT license. Run the gateway in your
              cluster, in a container, in Azure Functions, on bare metal. Read
              the source, file an issue, send a PR.
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
      <Eyebrow>Compose the graph</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-4 text-3xl tracking-tight sm:text-4xl">
        Run a .NET-native gateway over your subgraphs.
      </h2>
      <p className="text-cc-ink-dim lead mx-auto mt-5 max-w-2xl">
        Compose your first Fusion archive locally, point a small ASP.NET Core
        program at it, and ship a federated graph from a single endpoint.
      </p>
      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/fusion">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>
      <p className="text-cc-ink-dim mt-6 font-mono text-xs tracking-widest uppercase">
        nitro fusion compose --source ./subgraphs --output gateway.far
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function FusionDotNetGatewayPage() {
  return (
    <>
      <Hero />
      <PillarsSection />
      <ComparisonSection />
      <CodeExample />
      <NitroEmbed />
      <CatalogueSection />
      <MitBand />
      <ClosingCta />
    </>
  );
}
