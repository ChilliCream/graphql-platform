import type { Metadata } from "next";
import NextLink from "next/link";
import type { ComponentType, CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { FromOurBlog } from "@/src/components/FromOurBlog";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { CookieCrumble } from "@/src/icons/CookieCrumble";
import { Fusion } from "@/src/icons/Fusion";
import { GitHubIcon } from "@/src/icons/GitHub";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";
import { NitroReel } from "@/src/nitro";

export const metadata: Metadata = {
  title: "ChilliCream: the GraphQL platform for .NET, house blend",
  description:
    "ChilliCream is the end-to-end GraphQL platform for .NET. Hot Chocolate server, Strawberry Shake client, Nitro control plane, Fusion composition, MIT licensed.",
  keywords: [
    "ChilliCream",
    "GraphQL platform",
    "GraphQL platform for .NET",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    "Fusion",
  ],
  openGraph: {
    title: "ChilliCream: the GraphQL platform for .NET, house blend",
    description:
      "Build, observe, and evolve your GraphQL platform on .NET. One menu of products, one bar, MIT licensed and open source on GitHub.",
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

interface MenuItemProps {
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

const PRIMARY_PRODUCTS: readonly MenuItemProps[] = [
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

const SECONDARY_PRODUCTS: readonly MenuItemProps[] = [
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

export default function LandingPreviewV6Page() {
  return (
    <>
      <Hero />
      <ReelStage />
      <LogoBand />
      <Capabilities />
      <TodaysMenu />
      <BehindTheBar />
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
        On the menu today
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
      <p className="text-cc-nav-label mb-4 text-center font-mono text-xs tracking-[0.22em] uppercase">
        Today&apos;s pour, the Nitro control plane
      </p>
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
      <p className="text-cc-nav-label pt-8 text-center font-mono text-xs tracking-[0.22em] uppercase">
        Brewed for teams shipping on .NET
      </p>
      <LogoCloud />
    </div>
  );
}

const CAPABILITIES: readonly Capability[] = [
  {
    eyebrow: "Roast",
    title: "Build with Hot Chocolate, source generated end to end.",
    body: "Hot Chocolate generates resolver dispatch, type bindings, and execution plans at compile time. Code-first or schema-first, built on ASP.NET Core, with the modern GraphQL spec.",
    bullets: [
      "Code-first and schema-first authoring",
      "Compile-time validated execution plans",
      "Zero reflection on the hot path",
    ],
    visual: <BuildVisual />,
    mediaSide: "right",
  },
  {
    eyebrow: "Grind",
    title: "Ship Strawberry Shake, a typed client for every consumer.",
    body: "MSBuild code generation turns each .graphql operation into a fully typed C# API. Apps stay in sync with the schema, and the client registry tracks every operation each published version sends.",
    bullets: [
      "MSBuild code generation for the client",
      "Per-client identity via GraphQL-Client-Id and Version",
      "Operations tracked in the client registry",
    ],
    visual: <ClientVisual />,
    mediaSide: "left",
  },
  {
    eyebrow: "Brew",
    title: "Run on Nitro, OpenTelemetry from gateway to resolver.",
    body: "Once Nitro is configured in the server, every operation carries OpenTelemetry traces, metrics, and logs. Operation insights surface p95, throughput, error rate, and impact, with per-client tracking.",
    bullets: [
      "Per-operation p95, p99, throughput",
      "Per-client tracking and version drift",
      "Resolver-level sample traces",
    ],
    visual: <ObserveVisual />,
    mediaSide: "right",
  },
  {
    eyebrow: "Serve",
    title: "Compose with Fusion, plan before traffic moves.",
    body: "Fusion composes multiple subgraphs into one cohesive graph at planning time. The gateway is always your own ASP.NET Core app, so you keep the auth, telemetry, and runtime you already trust.",
    bullets: [
      "Composition lifecycle: begin, validate, commit, rollback",
      "Self-hosted gateway, ASP.NET Core auth at the edge",
      "Distributed tracing across every subgraph",
    ],
    visual: <ServeVisual />,
    mediaSide: "left",
  },
];

function Capabilities() {
  return (
    <section aria-labelledby="capabilities-heading" className="py-20 sm:py-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Behind the bar
        </p>
        <h2
          id="capabilities-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Build, ship, run, and compose, in one pass.
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
        </code>
      </pre>
      <div className="text-cc-ink-dim mt-4 flex flex-wrap gap-2 text-[0.7rem]">
        <Pill>compile-time validated</Pill>
        <Pill>zero reflection on hot path</Pill>
      </div>
    </div>
  );
}

function ClientVisual() {
  return (
    <div className="font-mono text-[0.78rem] leading-relaxed">
      <div className="text-cc-nav-label mb-3 flex items-center justify-between text-[0.65rem] tracking-[0.18em] uppercase">
        <span>getCart.graphql</span>
        <span className="text-cc-accent">MSBuild codegen</span>
      </div>
      <pre className="text-cc-ink overflow-x-auto">
        <code>
          <span className="text-cc-ink-dim">{"# query"}</span>
          {"\n"}
          query <span className="text-cc-accent">getCart</span>($id: ID!) {"{"}
          {"\n"}
          {"  "}cart(id: $id) {"{"} id totals {"{"} amount currency {"}"} {"}"}
          {"\n"}
          {"}"}
        </code>
      </pre>
      <div className="border-cc-card-border my-4 border-t" />
      <pre className="text-cc-ink overflow-x-auto">
        <code>
          <span className="text-cc-ink-dim">{"// generated C#"}</span>
          {"\n"}
          var result = await <span className="text-cc-accent">client</span>
          .GetCart.ExecuteAsync(id);
          {"\n"}
          var totals = result.Data?.Cart?.Totals;
        </code>
      </pre>
      <div className="text-cc-ink-dim mt-4 flex flex-wrap gap-2 text-[0.7rem]">
        <Pill>typed C# from .graphql</Pill>
        <Pill>tracked in client registry</Pill>
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

function ServeVisual() {
  return (
    <div className="font-mono text-[0.78rem]">
      <div className="text-cc-nav-label mb-3 flex items-center justify-between text-[0.65rem] tracking-[0.18em] uppercase">
        <span>fusion / compose</span>
        <span className="text-cc-success">plan committed</span>
      </div>
      <ol className="flex flex-col gap-2">
        <ComposeStep step="01" label="begin composition" state="ok" />
        <ComposeStep step="02" label="validate 7 subgraphs" state="ok" />
        <ComposeStep step="03" label="plan: 142 paths" state="ok" />
        <ComposeStep step="04" label="commit to gateway" state="run" />
      </ol>
      <div className="border-cc-card-border mt-5 border-t pt-4">
        <div className="text-cc-nav-label text-[0.65rem] tracking-[0.18em] uppercase">
          subgraphs in this graph
        </div>
        <div className="text-cc-ink mt-2 flex flex-wrap gap-2">
          <Pill>orders</Pill>
          <Pill>inventory</Pill>
          <Pill>payments</Pill>
          <Pill>shipping</Pill>
          <Pill>identity</Pill>
          <Pill>catalog</Pill>
          <Pill>pricing</Pill>
        </div>
      </div>
    </div>
  );
}

function ComposeStep({
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

function TodaysMenu() {
  return (
    <section
      aria-labelledby="menu-heading"
      className="border-cc-card-border bg-cc-card-bg/40 mt-20 rounded-3xl border p-6 sm:mt-28 sm:p-10"
    >
      <div className="flex flex-col items-center gap-3 text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
          The house blend
        </p>
        <h2
          id="menu-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
        >
          Today&apos;s Menu
        </h2>
        <p className="text-cc-ink mx-auto max-w-2xl text-base">
          One family of products. MIT licensed. Each one stands on its own, and
          each one assumes the others exist.
        </p>
        <div className="bg-cc-accent/70 mt-2 h-px w-24" aria-hidden />
      </div>

      <div className="mt-12">
        <div className="text-cc-accent mb-5 flex items-center gap-3 font-mono text-[0.7rem] tracking-[0.22em] uppercase">
          <span>On the board</span>
          <span className="bg-cc-card-border h-px flex-1" aria-hidden />
        </div>
        <ul className="flex flex-col">
          {PRIMARY_PRODUCTS.map((p) => (
            <MenuRow key={p.name} product={p} prominent />
          ))}
        </ul>
      </div>

      <div className="mt-12">
        <div className="text-cc-nav-label mb-5 flex items-center gap-3 font-mono text-[0.7rem] tracking-[0.22em] uppercase">
          <span>Below the rule</span>
          <span className="bg-cc-card-border h-px flex-1" aria-hidden />
        </div>
        <ul className="flex flex-col">
          {SECONDARY_PRODUCTS.map((p) => (
            <MenuRow key={p.name} product={p} />
          ))}
        </ul>
      </div>
    </section>
  );
}

function MenuRow({
  product,
  prominent = false,
}: {
  readonly product: MenuItemProps;
  readonly prominent?: boolean;
}) {
  const Icon = product.icon;
  return (
    <li className="border-cc-card-border group border-b last:border-b-0">
      <div className="grid grid-cols-[auto_1fr] items-start gap-4 py-6 sm:grid-cols-[auto_1fr_auto] sm:gap-6 sm:py-7">
        <div
          className={`text-cc-ink-dim group-hover:text-cc-accent flex-none transition-colors ${
            prominent
              ? "h-12 w-12 sm:h-14 sm:w-14"
              : "h-10 w-10 sm:h-12 sm:w-12"
          }`}
        >
          <Icon className="h-full w-full" />
        </div>
        <div className="min-w-0">
          <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1">
            <h3
              className={`font-heading text-cc-heading font-semibold ${
                prominent ? "text-xl sm:text-2xl" : "text-lg sm:text-xl"
              }`}
            >
              {product.name}
            </h3>
            <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.18em] uppercase">
              {product.tagline}
            </p>
          </div>
          <p className="text-cc-ink mt-3 max-w-2xl text-sm leading-relaxed sm:text-base">
            {product.description}
          </p>
          <div className="mt-4 sm:hidden">
            <ProductLink product={product} />
          </div>
        </div>
        <div className="col-span-2 hidden sm:col-span-1 sm:block sm:self-center sm:text-right">
          <ProductLink product={product} />
        </div>
      </div>
    </li>
  );
}

function ProductLink({ product }: { readonly product: MenuItemProps }) {
  const isExternal = product.external;
  const linkClass =
    "text-cc-accent hover:text-cc-accent-hover inline-flex items-center gap-1 text-sm font-medium";
  const arrow = (
    <span
      aria-hidden
      className="transition-transform group-hover:translate-x-0.5"
    >
      {"->"}
    </span>
  );
  if (isExternal) {
    return (
      <a
        href={product.href}
        target="_blank"
        rel="noopener noreferrer"
        className={`group ${linkClass}`}
      >
        {product.linkLabel} {arrow}
      </a>
    );
  }
  return (
    <NextLink href={product.href} className={`group ${linkClass}`}>
      {product.linkLabel} {arrow}
    </NextLink>
  );
}

function BehindTheBar() {
  return (
    <section aria-labelledby="oss-heading" className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/60 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Behind the bar
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
      <div className="border-cc-card-border bg-cc-card-bg/50 mx-auto flex max-w-3xl flex-col items-center gap-5 rounded-2xl border p-8 text-center sm:p-10">
        <div className="text-cc-accent inline-flex items-center gap-3">
          <CoffeeTray className="h-8 w-12" />
          <span className="font-mono text-xs tracking-[0.22em] uppercase">
            Pick your brew style at the counter
          </span>
        </div>
        <h2
          id="pricing-heading"
          className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold"
        >
          Free to start. Honest to scale.
        </h2>
        <p className="text-cc-ink max-w-xl text-base">
          Hot Chocolate is open source and free under MIT. Nitro Cloud has a
          free Shared Instance and a $400 Dedicated Instance. Support plans
          start at $450 Startup and $1,300 Business, with Custom Enterprise.
        </p>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/pricing">See pricing</SolidButton>
          <OutlineButton href="/services/support/contact">
            Talk to us
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

function BlogStrip() {
  return (
    <section className="mt-20 sm:mt-28">
      <p className="text-cc-nav-label mb-6 text-center font-mono text-xs tracking-[0.22em] uppercase">
        Fresh grounds
      </p>
      <FromOurBlog />
    </section>
  );
}

function ClosingCta() {
  return (
    <section className="mt-20 mb-10 text-center sm:mt-28">
      <EspressoMark />
      <p className="text-cc-nav-label mt-5 font-mono text-xs tracking-[0.22em] uppercase">
        Last call
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
        Start with a single shot.
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
        Free tier on Nitro Cloud, or self-host the whole platform under MIT.
        Start with Hot Chocolate, wire up Strawberry Shake, plug in Nitro for
        the registry and telemetry.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to us
        </OutlineButton>
      </div>
    </section>
  );
}

function EspressoMark() {
  return (
    <svg
      viewBox="0 0 48 56"
      aria-hidden="true"
      className="text-cc-accent mx-auto h-12 w-10 opacity-60"
      fill="none"
      stroke="currentColor"
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth="1.5"
    >
      <path d="M22 4 C19 9, 25 11, 22 16" opacity="0.7" />
      <path d="M8 26 h26 v8 a10 10 0 0 1 -10 10 h-6 a10 10 0 0 1 -10 -10 z" />
      <path d="M34 28 h5 a4 4 0 0 1 4 4 v2 a4 4 0 0 1 -4 4 h-5" />
      <path d="M6 50 h32" />
    </svg>
  );
}
