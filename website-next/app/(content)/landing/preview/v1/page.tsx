import type { Metadata } from "next";
import NextLink from "next/link";
import type { ComponentType, CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { FromOurBlog } from "@/src/components/FromOurBlog";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CookieCrumble } from "@/src/icons/CookieCrumble";
import { Fusion } from "@/src/icons/Fusion";
import { GitHubIcon } from "@/src/icons/GitHub";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";
import { NitroFusion, NitroReel } from "@/src/nitro";

export const metadata: Metadata = {
  title: "ChilliCream: end-to-end GraphQL platform for .NET",
  description:
    "ChilliCream is the end-to-end GraphQL platform for .NET teams. Build with Hot Chocolate, ship with Nitro: schema registry, CI checks, observability, Fusion.",
  keywords: [
    "ChilliCream",
    "GraphQL platform",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    "Fusion",
    "GraphQL observability",
    "schema registry",
    ".NET GraphQL",
  ],
  openGraph: {
    title: "ChilliCream: end-to-end GraphQL platform for .NET",
    description:
      "Build, observe, and evolve your GraphQL platform on .NET. Hot Chocolate server, Strawberry Shake client, Nitro control plane, Fusion composition.",
  },
  robots: { index: false, follow: false },
};

interface Capability {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
  readonly mediaSide: "left" | "right";
}

interface ProductCardProps {
  readonly name: string;
  readonly tagline: string;
  readonly description: string;
  readonly href: string;
  readonly linkLabel: string;
  readonly external?: boolean;
  readonly icon: ComponentType<{
    readonly className?: string;
    readonly style?: CSSProperties;
  }>;
}

const PRIMARY_PRODUCTS: readonly ProductCardProps[] = [
  {
    name: "Hot Chocolate",
    tagline: "GraphQL server for .NET",
    description:
      "The source-generated GraphQL server at the heart of the platform. Schema-first or code-first, built on ASP.NET Core, with the modern GraphQL spec.",
    href: "/products/hotchocolate",
    linkLabel: "Hot Chocolate",
    icon: HotChocolate,
  },
  {
    name: "Strawberry Shake",
    tagline: "Typed .NET client",
    description:
      "A typed GraphQL client for .NET. MSBuild code generation turns each query into a fully typed C# API, so apps stay in sync with the schema.",
    href: "/products/strawberryshake",
    linkLabel: "Strawberry Shake",
    icon: StrawberryShake,
  },
  {
    name: "Nitro",
    tagline: "Control plane and IDE",
    description:
      "Schema registry, client registry, CI checks, observability, and the GraphQL IDE your team already uses. Served from your endpoint, scaled in the cloud.",
    href: "https://nitro.chillicream.com",
    linkLabel: "Nitro",
    external: true,
    icon: Nitro,
  },
];

const SECONDARY_PRODUCTS: readonly ProductCardProps[] = [
  {
    name: "Mocha",
    tagline: "Mediator and workflows",
    description:
      "A source-generated mediator for in-process and cross-service messaging. Validated sagas, exactly-once processing, no reflection on the hot path.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Mocha on GitHub",
    external: true,
    icon: Mocha,
  },
  {
    name: "Fusion",
    tagline: "Composition for many subgraphs",
    description:
      "Compose multiple subgraphs at planning time, then run the gateway in your own ASP.NET Core process. The query plan is decided before traffic moves.",
    href: "/products/fusion",
    linkLabel: "Fusion",
    icon: Fusion,
  },
  {
    name: "Green Donut",
    tagline: "DataLoader for .NET",
    description:
      "The DataLoader implementation behind Hot Chocolate. Batches and caches data access so resolvers stay simple and N+1 stops being your problem.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Green Donut on GitHub",
    external: true,
    icon: GreenDonut,
  },
  {
    name: "Cookie Crumble",
    tagline: "GraphQL-aware snapshot testing",
    description:
      "Snapshot testing built for GraphQL. Native support for execution results and HTTP responses, with Markdown snapshots for multi-shape assertions.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Cookie Crumble on GitHub",
    external: true,
    icon: CookieCrumble,
  },
];

const HEADLINE_LEAD = "Your GraphQL platform,";
const HEADLINE_ACCENT = "running on .NET.";
const SPECTRUM_GRADIENT =
  "linear-gradient(95deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

export default function LandingPreviewV1Page() {
  return (
    <>
      <Hero />
      <ReelStage />
      <LogoBand />
      <Capabilities />
      <FusionDeepCut />
      <ProductFamily />
      <OpenSourceBand />
      <PricingPointer />
      <BlogStrip />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section className="pt-12 pb-10 text-center sm:pt-20 sm:pb-14">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
        The ChilliCream GraphQL platform
      </p>
      <h1 className="font-heading text-cc-heading sm:text-h2 lg:text-h1 mx-auto mt-6 max-w-4xl text-4xl leading-[1.05] font-semibold tracking-[-0.02em] text-balance">
        {HEADLINE_LEAD}
        <span
          className="block bg-clip-text pb-[0.12em] leading-[1.12] text-transparent"
          style={{ backgroundImage: SPECTRUM_GRADIENT }}
        >
          {HEADLINE_ACCENT}
        </span>
      </h1>
      <p className="text-cc-ink mx-auto mt-7 max-w-2xl text-base text-pretty sm:text-lg">
        Hot Chocolate ships the server. Strawberry Shake ships the typed client.
        Nitro ships the control plane, registry, and observability. One
        platform, designed together, open source on GitHub.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch Nitro
        </OutlineButton>
      </div>
      <p className="text-cc-nav-label mt-5 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
        MIT licensed. Self-host or run on Nitro Cloud.
      </p>
    </section>
  );
}

function ReelStage() {
  return (
    <section aria-labelledby="reel-heading" className="pb-16 sm:pb-24">
      <h2 id="reel-heading" className="sr-only">
        The Nitro control plane
      </h2>
      <div className="border-cc-card-border bg-cc-card-bg/70 mx-auto max-w-5xl overflow-hidden rounded-xl border shadow-[0_30px_80px_-40px_rgba(94,234,212,0.35)]">
        <NitroReel />
      </div>
      <p className="text-cc-ink-dim mx-auto mt-5 max-w-2xl text-center text-sm">
        Author, observe, diagnose, evolve, and federate. The Nitro reel is a
        live render of the same control plane your team logs into.
      </p>
    </section>
  );
}

function LogoBand() {
  return (
    <div className="border-cc-card-border border-y">
      <LogoCloud />
    </div>
  );
}

const CAPABILITIES: readonly Capability[] = [
  {
    eyebrow: "Build",
    title: "Source-generated GraphQL, end to end.",
    body: "Hot Chocolate generates resolver dispatch, type bindings, and execution plans at compile time. Strawberry Shake generates the typed C# client from your operations via MSBuild. The schema you ship is the schema both sides agree on.",
    bullets: [
      "Code-first and schema-first authoring",
      "Compile-time validated execution plans",
      "MSBuild code generation for the client",
    ],
    visual: <BuildVisual />,
    mediaSide: "right",
  },
  {
    eyebrow: "Observe",
    title: "OpenTelemetry-native, from gateway to resolver.",
    body: "Once Nitro is configured in the server, every operation carries OpenTelemetry traces, metrics, and logs. Operation insights surface p95, throughput, error rate, and impact, with per-client tracking via GraphQL-Client-Id and Version.",
    bullets: [
      "Per-operation p95, p99, throughput",
      "Per-client tracking and version drift",
      "Resolver-level sample traces",
    ],
    visual: <ObserveVisual />,
    mediaSide: "left",
  },
  {
    eyebrow: "Evolve",
    title: "Know which published clients a change affects.",
    body: "The schema registry tracks every published version. The client registry knows every operation each app actually sends. Nitro CI compares them and tells you which published clients are affected before you deploy.",
    bullets: [
      "Breaking change classification, safe to breaking",
      "Stage promotion with approval gates",
      "Rollback by republishing an earlier tag",
    ],
    visual: <EvolveVisual />,
    mediaSide: "right",
  },
  {
    eyebrow: "Agentic",
    title: "An MCP endpoint your agents can actually use.",
    body: "Hot Chocolate exposes an MCP server over Streamable HTTP. Curate feature collections of .graphql operations, JSON descriptions, and HTML context, then track per-tool latency, ops per minute, error rate, and impact in Nitro.",
    bullets: [
      "MCP over Streamable HTTP, from the server",
      "Feature collections curate operations",
      "Per-tool telemetry beside per-operation",
    ],
    visual: <AgenticVisual />,
    mediaSide: "left",
  },
  {
    eyebrow: "Workflows",
    title: "Mocha runs the work between services.",
    body: "Mocha is the source-generated mediator and messaging library. Sagas are validated before traffic, processing is exactly-once, and the in-process and cross-service programming model is the same. No reflection on the hot path.",
    bullets: [
      "Validated sagas before traffic moves",
      "Exactly-once processing semantics",
      "Same model in-process and across services",
    ],
    visual: <WorkflowVisual />,
    mediaSide: "right",
  },
];

function Capabilities() {
  return (
    <section aria-labelledby="capabilities-heading" className="py-20 sm:py-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          The platform, in five moves
        </p>
        <h2
          id="capabilities-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Build, observe, evolve, ship to agents, run the work.
        </h2>
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
          Each capability is its own surface in Nitro, and each one was designed
          to work with the others. Pull one up below.
        </p>
      </div>

      <div className="mt-16 flex flex-col gap-20 sm:gap-28">
        {CAPABILITIES.map((cap) => (
          <CapabilityRow key={cap.eyebrow} cap={cap} />
        ))}
      </div>
    </section>
  );
}

function CapabilityRow({ cap }: { readonly cap: Capability }) {
  const mediaFirst = cap.mediaSide === "left";
  return (
    <article className="grid items-center gap-10 md:grid-cols-2 md:gap-14">
      <div className={mediaFirst ? "md:order-1" : "md:order-2"}>
        <div className="border-cc-card-border bg-cc-card-bg/60 overflow-hidden rounded-2xl border p-5 sm:p-7">
          {cap.visual}
        </div>
      </div>
      <div className={mediaFirst ? "md:order-2" : "md:order-1"}>
        <p className="text-cc-accent font-mono text-xs tracking-[0.22em] uppercase">
          {cap.eyebrow}
        </p>
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold">
          {cap.title}
        </h3>
        <p className="text-cc-ink mt-5 text-base leading-relaxed">{cap.body}</p>
        <ul className="mt-6 flex flex-col gap-3">
          {cap.bullets.map((bullet) => (
            <li key={bullet} className="flex items-start gap-3">
              <span className="text-cc-accent mt-[5px] flex-none">
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm">{bullet}</span>
            </li>
          ))}
        </ul>
      </div>
    </article>
  );
}

function BuildVisual() {
  return (
    <div className="font-mono text-[0.78rem] leading-relaxed">
      <div className="text-cc-nav-label mb-3 flex items-center justify-between text-[0.65rem] tracking-[0.18em] uppercase">
        <span>Program.cs</span>
        <span className="text-cc-accent">source generated</span>
      </div>
      <pre className="text-cc-ink overflow-x-auto">
        <code>
          <span className="text-cc-ink-dim">{"// Hot Chocolate server"}</span>
          {"\n"}
          <span className="text-cc-accent">builder</span>.Services
          {"\n"}
          {"  "}.AddGraphQLServer()
          {"\n"}
          {"  "}.AddQueryType&lt;<span className="text-cc-accent">Query</span>
          &gt;()
          {"\n"}
          {"  "}.AddMutationType&lt;
          <span className="text-cc-accent">Mutation</span>
          &gt;()
          {"\n"}
          {"  "}.AddInstrumentation()
          {"\n"}
          {"  "}.AddNitroExporter();
          {"\n\n"}
          <span className="text-cc-ink-dim">
            {"// Strawberry Shake client"}
          </span>
          {"\n"}
          dotnet graphql init{" "}
          <span className="text-cc-accent">https://api/graphql</span>
        </code>
      </pre>
      <div className="text-cc-ink-dim mt-4 flex flex-wrap gap-2 text-[0.7rem]">
        <Pill>compile-time validated</Pill>
        <Pill>zero reflection on hot path</Pill>
      </div>
    </div>
  );
}

function ObserveVisual() {
  return (
    <div>
      <div className="text-cc-nav-label mb-3 flex items-center justify-between font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        <span>checkout.graphql / getCart</span>
        <span className="text-cc-accent">live</span>
      </div>
      <div className="grid grid-cols-3 gap-3">
        <Stat label="p95" value="142ms" trend="-12ms" trendOk />
        <Stat label="rate" value="3.4k/m" trend="+8%" trendOk />
        <Stat label="errors" value="0.21%" trend="+0.04" trendOk={false} />
      </div>
      <Sparkbars />
      <div className="text-cc-ink-dim mt-3 font-mono text-[0.7rem]">
        Per-client: <span className="text-cc-ink">web@2.4.0</span>,{" "}
        <span className="text-cc-ink">ios@1.9.2</span>,{" "}
        <span className="text-cc-ink">android@1.8.7</span>
      </div>
    </div>
  );
}

function Stat({
  label,
  value,
  trend,
  trendOk,
}: {
  readonly label: string;
  readonly value: string;
  readonly trend: string;
  readonly trendOk: boolean;
}) {
  return (
    <div className="border-cc-card-border bg-cc-bg/50 rounded-xl border p-3">
      <div className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
        {label}
      </div>
      <div className="font-heading text-cc-heading mt-1 text-xl font-semibold">
        {value}
      </div>
      <div
        className={`mt-1 font-mono text-[0.65rem] ${
          trendOk ? "text-cc-success" : "text-cc-warning"
        }`}
      >
        {trend}
      </div>
    </div>
  );
}

function Sparkbars() {
  const heights = [28, 36, 30, 44, 38, 52, 46, 60, 48, 56, 64, 58];
  return (
    <div className="border-cc-card-border bg-cc-bg/50 mt-4 flex h-20 items-end gap-1 rounded-xl border p-3">
      {heights.map((h, i) => (
        <span
          key={i}
          className="bg-cc-accent/70 flex-1 rounded-t"
          style={{ height: `${h}%` }}
        />
      ))}
    </div>
  );
}

function EvolveVisual() {
  return (
    <div className="font-mono text-[0.75rem]">
      <div className="text-cc-nav-label mb-3 flex items-center justify-between text-[0.65rem] tracking-[0.18em] uppercase">
        <span>schema check / pr #2417</span>
        <span className="text-cc-warning">3 affected</span>
      </div>
      <div className="flex flex-col gap-2">
        <DiffRow tag="+" tone="add">
          type Cart {"{"} totals: Money! {"}"}
        </DiffRow>
        <DiffRow tag="-" tone="remove">
          type Cart {"{"} total: Float! {"}"}
        </DiffRow>
        <DiffRow tag="!" tone="warn">
          BREAKING: Cart.total removed
        </DiffRow>
      </div>
      <div className="border-cc-card-border mt-5 border-t pt-4">
        <div className="text-cc-nav-label text-[0.65rem] tracking-[0.18em] uppercase">
          published clients affected
        </div>
        <ul className="text-cc-ink mt-2 space-y-1">
          <li className="flex justify-between">
            <span>web@2.3.x</span>
            <span className="text-cc-warning">getCart</span>
          </li>
          <li className="flex justify-between">
            <span>ios@1.9.x</span>
            <span className="text-cc-warning">getCart</span>
          </li>
          <li className="flex justify-between">
            <span>android@1.8.x</span>
            <span className="text-cc-warning">getCart</span>
          </li>
        </ul>
      </div>
    </div>
  );
}

function DiffRow({
  tag,
  tone,
  children,
}: {
  readonly tag: string;
  readonly tone: "add" | "remove" | "warn";
  readonly children: ReactNode;
}) {
  const toneClass =
    tone === "add"
      ? "text-cc-success"
      : tone === "remove"
        ? "text-cc-danger"
        : "text-cc-warning";
  return (
    <div className="border-cc-card-border bg-cc-bg/50 flex items-start gap-3 rounded-md border px-3 py-2">
      <span className={`font-bold ${toneClass}`}>{tag}</span>
      <span className="text-cc-ink">{children}</span>
    </div>
  );
}

function AgenticVisual() {
  return (
    <div>
      <div className="text-cc-nav-label mb-3 flex items-center justify-between font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        <span>mcp / featureset: orders</span>
        <span className="text-cc-accent">4 tools</span>
      </div>
      <ul className="flex flex-col gap-2 font-mono text-[0.75rem]">
        <McpTool name="placeOrder" ops="142/m" p95="186ms" />
        <McpTool name="cancelOrder" ops="9/m" p95="94ms" />
        <McpTool name="lookupOrder" ops="612/m" p95="42ms" />
        <McpTool name="refundOrder" ops="3/m" p95="220ms" />
      </ul>
      <div className="text-cc-ink-dim mt-4 font-mono text-[0.7rem]">
        Streamable HTTP, served from your endpoint. Per-tool telemetry beside
        per-operation in Nitro.
      </div>
    </div>
  );
}

function McpTool({
  name,
  ops,
  p95,
}: {
  readonly name: string;
  readonly ops: string;
  readonly p95: string;
}) {
  return (
    <li className="border-cc-card-border bg-cc-bg/50 flex items-center justify-between rounded-md border px-3 py-2">
      <span className="text-cc-heading">{name}</span>
      <span className="text-cc-ink-dim flex gap-4">
        <span>{ops}</span>
        <span className="text-cc-accent">{p95}</span>
      </span>
    </li>
  );
}

function WorkflowVisual() {
  return (
    <div>
      <div className="text-cc-nav-label mb-4 flex items-center justify-between font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        <span>mocha / placeOrderSaga</span>
        <span className="text-cc-success">validated</span>
      </div>
      <ol className="flex flex-col gap-3">
        <SagaStep step="01" label="ReserveInventory" state="ok" />
        <SagaStep step="02" label="ChargePayment" state="ok" />
        <SagaStep step="03" label="DispatchShipment" state="run" />
        <SagaStep step="04" label="NotifyCustomer" state="idle" />
      </ol>
      <div className="text-cc-ink-dim mt-4 font-mono text-[0.7rem]">
        Exactly-once processing. Source-generated handlers. No reflection on the
        hot path.
      </div>
    </div>
  );
}

function SagaStep({
  step,
  label,
  state,
}: {
  readonly step: string;
  readonly label: string;
  readonly state: "ok" | "run" | "idle";
}) {
  const dot =
    state === "ok"
      ? "bg-cc-success"
      : state === "run"
        ? "bg-cc-accent animate-pulse"
        : "bg-cc-ink-faint";
  return (
    <li className="border-cc-card-border bg-cc-bg/50 flex items-center gap-3 rounded-md border px-3 py-2">
      <span className="text-cc-nav-label font-mono text-[0.65rem]">{step}</span>
      <span className={`h-2 w-2 rounded-full ${dot}`} aria-hidden />
      <span className="text-cc-ink font-mono text-[0.78rem]">{label}</span>
    </li>
  );
}

function Pill({ children }: { readonly children: ReactNode }) {
  return (
    <span className="border-cc-card-border bg-cc-bg/60 text-cc-ink-dim rounded-full border px-2.5 py-0.5">
      {children}
    </span>
  );
}

function FusionDeepCut() {
  return (
    <section
      aria-labelledby="fusion-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
    >
      <div className="grid gap-10 lg:grid-cols-[1fr_1.25fr] lg:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Fusion
          </p>
          <h2
            id="fusion-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
          >
            Compose many subgraphs at planning time.
          </h2>
          <p className="text-cc-ink mt-5 text-base">
            Fusion is composition for many subgraphs into one cohesive graph.
            The query plan is decided before traffic moves, and the gateway is
            always your own ASP.NET Core app, so you keep the auth, telemetry,
            and runtime you already trust.
          </p>
          <ul className="mt-6 flex flex-col gap-3">
            <li className="flex items-start gap-3">
              <span className="text-cc-accent mt-[5px] flex-none">
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm">
                Composition lifecycle: begin, validate, commit, rollback
              </span>
            </li>
            <li className="flex items-start gap-3">
              <span className="text-cc-accent mt-[5px] flex-none">
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm">
                Self-hosted gateway, ASP.NET Core auth at the edge
              </span>
            </li>
            <li className="flex items-start gap-3">
              <span className="text-cc-accent mt-[5px] flex-none">
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm">
                Distributed tracing across every subgraph
              </span>
            </li>
          </ul>
          <div className="mt-7 flex flex-wrap gap-3">
            <OutlineButton href="/products/fusion">
              Read about Fusion
            </OutlineButton>
          </div>
        </div>
        <div className="border-cc-card-border bg-cc-bg/40 overflow-hidden rounded-2xl border">
          <NitroFusion />
        </div>
      </div>
    </section>
  );
}

function ProductFamily() {
  return (
    <section aria-labelledby="family-heading" className="mt-20 py-4 sm:mt-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          The open source platform behind it
        </p>
        <h2
          id="family-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          One family of products. MIT licensed.
        </h2>
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
          The ChilliCream platform is a coherent set of libraries, designed
          together for .NET. Each one stands on its own, and each one assumes
          the others exist.
        </p>
      </div>

      <div className="mt-12">
        <div className="text-cc-nav-label mb-4 font-mono text-[0.65rem] tracking-[0.2em] uppercase">
          Primary
        </div>
        <div className="grid gap-5 md:grid-cols-3">
          {PRIMARY_PRODUCTS.map((p) => (
            <ProductCard key={p.name} product={p} prominent />
          ))}
        </div>
      </div>

      <div className="mt-12">
        <div className="text-cc-nav-label mb-4 font-mono text-[0.65rem] tracking-[0.2em] uppercase">
          The rest of the family
        </div>
        <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-4">
          {SECONDARY_PRODUCTS.map((p) => (
            <ProductCard key={p.name} product={p} />
          ))}
        </div>
      </div>
    </section>
  );
}

function ProductCard({
  product,
  prominent = false,
}: {
  readonly product: ProductCardProps;
  readonly prominent?: boolean;
}) {
  const Icon = product.icon;
  const isExternal = product.external;
  const linkClass =
    "text-cc-accent hover:text-cc-accent-hover mt-5 inline-flex items-center gap-1 text-sm font-medium";
  const arrow = (
    <span
      aria-hidden
      className="transition-transform group-hover:translate-x-0.5"
    >
      {isExternal ? "->" : "->"}
    </span>
  );
  const linkLabel = isExternal ? `${product.linkLabel}` : product.linkLabel;

  return (
    <div
      className={`border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-2xl border p-6 transition-colors ${
        prominent ? "bg-cc-card-bg/80" : "bg-cc-card-bg/40"
      }`}
    >
      <Icon className={prominent ? "h-12 w-12" : "h-10 w-10"} />
      <h3 className="font-heading text-cc-heading mt-5 text-lg font-semibold">
        {product.name}
      </h3>
      <p className="text-cc-nav-label mt-1 font-mono text-[0.7rem] tracking-[0.16em] uppercase">
        {product.tagline}
      </p>
      <p className="text-cc-ink mt-4 flex-1 text-sm leading-relaxed">
        {product.description}
      </p>
      {isExternal ? (
        <a
          href={product.href}
          target="_blank"
          rel="noopener noreferrer"
          className={`group ${linkClass}`}
        >
          {linkLabel} {arrow}
        </a>
      ) : (
        <NextLink href={product.href} className={`group ${linkClass}`}>
          {linkLabel} {arrow}
        </NextLink>
      )}
    </div>
  );
}

function OpenSourceBand() {
  return (
    <section aria-labelledby="oss-heading" className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/60 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Open source
          </p>
          <h2
            id="oss-heading"
            className="font-heading text-cc-heading text-h4 mt-3 font-semibold"
          >
            MIT, on GitHub, built in the open.
          </h2>
          <p className="text-cc-ink mt-4 text-base">
            The whole platform lives in one monorepo. Read the code, open an
            issue, send a pull request, and ship the same bits we ship.
            Releases, roadmaps, and discussions are public.
          </p>
          <ul className="text-cc-ink mt-6 grid gap-3 text-sm sm:grid-cols-2">
            <OssCheck>MIT licensed across every package</OssCheck>
            <OssCheck>Public releases and changelogs</OssCheck>
            <OssCheck>Public roadmap and discussions</OssCheck>
            <OssCheck>Community Slack and GitHub triage</OssCheck>
          </ul>
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <a
            href="https://github.com/ChilliCream/graphql-platform"
            target="_blank"
            rel="noopener noreferrer"
            className="border-cc-card-border hover:border-cc-card-border-hover bg-cc-bg/50 inline-flex items-center gap-3 rounded-full border px-5 py-3 transition-colors"
          >
            <GitHubIcon className="text-cc-heading h-5 w-5" />
            <span className="text-cc-heading text-sm font-medium">
              ChilliCream/graphql-platform
            </span>
          </a>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            Star on GitHub
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

function OssCheck({ children }: { readonly children: ReactNode }) {
  return (
    <li className="flex items-start gap-3">
      <span className="text-cc-accent mt-[5px] flex-none">
        <CheckIcon />
      </span>
      <span>{children}</span>
    </li>
  );
}

function PricingPointer() {
  return (
    <section aria-labelledby="pricing-heading" className="mt-20 sm:mt-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Pricing
        </p>
        <h2
          id="pricing-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Free to start. Honest to scale.
        </h2>
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
          The Hot Chocolate platform itself is open source and free under MIT.
          Nitro Cloud has a free Shared Instance and a $400 Dedicated Instance.
          Support plans start at $450 Startup and $1,300 Business, with Custom
          Enterprise for the rest.
        </p>
      </div>
      <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <PricePeek label="Shared Instance" price="Free" note="pay as you go" />
        <PricePeek
          label="Dedicated Instance"
          price="$400"
          note="per month"
          highlight
        />
        <PricePeek label="Startup support" price="$450" note="per month" />
        <PricePeek label="Business support" price="$1,300" note="per month" />
      </div>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/pricing">See full pricing</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to us about Enterprise
        </OutlineButton>
      </div>
    </section>
  );
}

function PricePeek({
  label,
  price,
  note,
  highlight = false,
}: {
  readonly label: string;
  readonly price: string;
  readonly note: string;
  readonly highlight?: boolean;
}) {
  return (
    <div
      className={`rounded-2xl border p-5 ${
        highlight
          ? "border-cc-accent/50 bg-cc-accent/5"
          : "border-cc-card-border bg-cc-card-bg/50"
      }`}
    >
      <div className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {label}
      </div>
      <div className="font-heading text-cc-heading mt-3 text-2xl font-semibold">
        {price}
      </div>
      <div className="text-cc-ink-dim mt-1 font-mono text-[0.7rem]">{note}</div>
    </div>
  );
}

function BlogStrip() {
  return (
    <section className="mt-20 sm:mt-28">
      <FromOurBlog />
    </section>
  );
}

function ClosingCta() {
  return (
    <section className="mt-20 mb-10 text-center sm:mt-28">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
        Ready when you are
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
        Ship your GraphQL platform on .NET.
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
        Start with Hot Chocolate, wire up Strawberry Shake, plug in Nitro for
        the registry and telemetry. Free to start, MIT to keep.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch Nitro
        </OutlineButton>
      </div>
    </section>
  );
}
