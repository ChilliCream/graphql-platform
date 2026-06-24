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
  title: "ChilliCream: the .NET GraphQL platform, built outward",
  description:
    "The GraphQL platform for .NET teams: Hot Chocolate at the kernel, Strawberry Shake and Fusion around it, Nitro the outer orbit for registry and observability.",
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
    title: "ChilliCream: the .NET GraphQL platform, built outward",
    description:
      "Five concentric layers of one platform. .NET runtime at the kernel, Hot Chocolate, Strawberry Shake, Fusion, and Nitro orbiting outward.",
  },
  robots: { index: false, follow: false },
};

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

const SPECTRUM_GRADIENT =
  "linear-gradient(95deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

interface RingLayer {
  readonly key: string;
  readonly index: string;
  readonly name: string;
  readonly statement: string;
  readonly body: string;
}

const RING_LAYERS: readonly RingLayer[] = [
  {
    key: "dotnet",
    index: "01",
    name: ".NET runtime",
    statement: "The kernel is a runtime your team already runs.",
    body: "Everything on the platform compiles to standard .NET. ASP.NET Core hosts the server, MSBuild generates the client, the runtime you operate is the runtime you already trust.",
  },
  {
    key: "hot-chocolate",
    index: "02",
    name: "Hot Chocolate",
    statement: "Hot Chocolate wraps the kernel as the GraphQL server.",
    body: "Source-generated resolvers, code-first or schema-first authoring, execution plans validated at compile time, zero reflection on the hot path.",
  },
  {
    key: "strawberry-shake",
    index: "03",
    name: "Strawberry Shake",
    statement: "Strawberry Shake renders the schema as a typed C# client.",
    body: "MSBuild code generation turns each .graphql operation into a typed C# API, so apps stay in sync with the schema and breaking changes surface at build.",
  },
  {
    key: "fusion",
    index: "04",
    name: "Fusion",
    statement: "Fusion composes many subgraphs into one cohesive graph.",
    body: "Composition happens at planning time. The query plan is decided before traffic moves and the gateway is always your own ASP.NET Core app at the edge.",
  },
  {
    key: "nitro",
    index: "05",
    name: "Nitro",
    statement: "Nitro is the outer orbit your team logs into.",
    body: "Schema registry, client registry, CI checks, OpenTelemetry-native observability, and the GraphQL IDE, served from your endpoint and scaled in the cloud.",
  },
];

interface CapabilityOrbit {
  readonly eyebrow: string;
  readonly ringIndex: 0 | 1 | 2 | 3 | 4;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

const CAPABILITY_ORBITS: readonly CapabilityOrbit[] = [
  {
    eyebrow: "Build",
    ringIndex: 1,
    title: "Source-generated GraphQL, end to end.",
    body: "Hot Chocolate generates resolver dispatch, type bindings, and execution plans at compile time. Strawberry Shake generates the typed C# client from your operations via MSBuild. The schema you ship is the schema both sides agree on.",
    bullets: [
      "Code-first and schema-first authoring",
      "Compile-time validated execution plans",
      "MSBuild code generation for the client",
    ],
  },
  {
    eyebrow: "Observe",
    ringIndex: 4,
    title: "OpenTelemetry-native, from gateway to resolver.",
    body: "Once Nitro is configured in the server, every operation carries OpenTelemetry traces, metrics, and logs. Operation insights surface p95, throughput, error rate, and impact, with per-client tracking via GraphQL-Client-Id and Version.",
    bullets: [
      "Per-operation p95, p99, throughput",
      "Per-client tracking and version drift",
      "Resolver-level sample traces",
    ],
  },
  {
    eyebrow: "Evolve",
    ringIndex: 4,
    title: "Know which published clients a change affects.",
    body: "The schema registry tracks every published version. The client registry knows every operation each app actually sends. Nitro CI compares them and tells you which published clients are affected before you deploy.",
    bullets: [
      "Breaking change classification, safe to breaking",
      "Stage promotion with approval gates",
      "Rollback by republishing an earlier tag",
    ],
  },
  {
    eyebrow: "Agentic",
    ringIndex: 1,
    title: "An MCP endpoint your agents can actually use.",
    body: "Hot Chocolate exposes an MCP server over Streamable HTTP. Curate feature collections of .graphql operations, JSON descriptions, and HTML context, then track per-tool latency, ops per minute, error rate, and impact in Nitro.",
    bullets: [
      "MCP over Streamable HTTP, from the server",
      "Feature collections curate operations",
      "Per-tool telemetry beside per-operation",
    ],
  },
  {
    eyebrow: "Workflows",
    ringIndex: 0,
    title: "Mocha runs the work between services.",
    body: "Mocha is the source-generated mediator and messaging library. Sagas are validated before traffic, processing is exactly-once, and the in-process and cross-service programming model is the same. No reflection on the hot path.",
    bullets: [
      "Validated sagas before traffic moves",
      "Exactly-once processing semantics",
      "Same model in-process and across services",
    ],
  },
];

export default function LandingPreviewV9Page() {
  return (
    <>
      <Hero />
      <PlatformAsRings />
      <ReelStage />
      <LogoBand />
      <CapabilitiesAsOrbits />
      <FusionDeepCut />
      <ProductFamilyAsOrbits />
      <OpenSourcePricingBand />
      <BlogStrip />
      <ClosingCta />
    </>
  );
}

/* ---------- Hero with concentric rings centerpiece ---------- */

function Hero() {
  return (
    <section className="pt-10 pb-14 text-center sm:pt-16 sm:pb-20">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
        The ChilliCream GraphQL platform
      </p>
      <h1 className="font-heading text-cc-heading sm:text-h2 lg:text-h1 mx-auto mt-6 max-w-3xl text-4xl leading-[1.05] font-semibold tracking-[-0.02em] text-balance">
        One platform, drawn from the kernel outward.
      </h1>

      <div className="relative mx-auto mt-10 aspect-square w-full max-w-[640px]">
        <ConcentricRingsHero />
      </div>

      <p className="text-cc-ink mx-auto mt-10 max-w-2xl text-base text-pretty sm:text-lg">
        .NET at the center. Hot Chocolate wraps it as the server. Strawberry
        Shake and Fusion ring outward. Nitro is the outermost orbit, observing
        everything inside.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
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

function ConcentricRingsHero() {
  // Five hairline rings (r = 80, 160, 240, 320, 400) plus an inner glow at r=40.
  // Outer 4 rings rotate -8deg to 0deg once on mount.
  // Inner glow fades from 0 to 1. Tick labels stagger in. CSS-only.
  const css = `
    @keyframes ccv9-rotate-in {
      from { transform: rotate(-8deg); }
      to   { transform: rotate(0deg); }
    }
    @keyframes ccv9-fade-in {
      from { opacity: 0; }
      to   { opacity: 1; }
    }
    .ccv9-rings-rotor {
      transform-origin: 50% 50%;
      transform-box: fill-box;
      animation: ccv9-rotate-in 2.4s cubic-bezier(0.2, 0.7, 0.2, 1) 1 forwards;
    }
    .ccv9-glow {
      opacity: 0;
      animation: ccv9-fade-in 1.2s ease-out 0.6s 1 forwards;
    }
    .ccv9-tick {
      opacity: 0;
      animation: ccv9-fade-in 0.6s ease-out 1 forwards;
    }
    .ccv9-tick-1 { animation-delay: 0.9s; }
    .ccv9-tick-2 { animation-delay: 1.1s; }
    .ccv9-tick-3 { animation-delay: 1.3s; }
    .ccv9-tick-4 { animation-delay: 1.5s; }
    .ccv9-tick-5 { animation-delay: 1.7s; }
    @media (prefers-reduced-motion: reduce) {
      .ccv9-rings-rotor { animation: none; transform: none; }
      .ccv9-glow { opacity: 1; animation: none; }
      .ccv9-tick { opacity: 1; animation: none; }
    }
  `;
  return (
    <svg
      viewBox="-440 -440 880 880"
      className="absolute inset-0 h-full w-full"
      role="img"
      aria-label="Concentric rings of the ChilliCream platform: .NET runtime at the center, Hot Chocolate, Strawberry Shake, Fusion, and Nitro outward."
    >
      <style>{css}</style>
      <defs>
        <radialGradient
          id="ccv9-fade"
          cx="0"
          cy="0"
          r="440"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0%" stopColor="#0b0f1a" stopOpacity="0" />
          <stop offset="70%" stopColor="#0b0f1a" stopOpacity="0" />
          <stop offset="100%" stopColor="#0b0f1a" stopOpacity="1" />
        </radialGradient>
        <filter id="ccv9-glow" x="-100" y="-100" width="200" height="200">
          <feGaussianBlur stdDeviation="6" />
        </filter>
      </defs>

      {/* Inner accent glow ring at r=40 */}
      <g className="ccv9-glow">
        <circle
          cx="0"
          cy="0"
          r="40"
          fill="none"
          stroke="var(--color-cc-accent)"
          strokeWidth="1.5"
          filter="url(#ccv9-glow)"
          opacity="0.9"
        />
        <circle
          cx="0"
          cy="0"
          r="40"
          fill="none"
          stroke="var(--color-cc-accent)"
          strokeWidth="1"
        />
      </g>

      {/* Outer four rings rotate once on mount */}
      <g className="ccv9-rings-rotor">
        {[80, 160, 240, 320, 400].map((r) => (
          <circle
            key={r}
            cx="0"
            cy="0"
            r={r}
            fill="none"
            stroke="var(--color-cc-card-border)"
            strokeWidth="1"
          />
        ))}

        {/* Tick marks on each ring (north position) and labels */}
        <RingTick r={80} label=".NET runtime" delayClass="ccv9-tick-1" />
        <RingTick r={160} label="Hot Chocolate" delayClass="ccv9-tick-2" />
        <RingTick r={240} label="Strawberry Shake" delayClass="ccv9-tick-3" />
        <RingTick r={320} label="Fusion" delayClass="ccv9-tick-4" />
        <RingTick r={400} label="Nitro" delayClass="ccv9-tick-5" />
      </g>

      {/* Radial mask: fade outermost ring into bg */}
      <rect
        x="-440"
        y="-440"
        width="880"
        height="880"
        fill="url(#ccv9-fade)"
        pointerEvents="none"
      />
    </svg>
  );
}

function RingTick({
  r,
  label,
  delayClass,
}: {
  readonly r: number;
  readonly label: string;
  readonly delayClass: string;
}) {
  // Tick: short radial line just outside the ring on the north axis.
  const tickInner = -r;
  const tickOuter = -(r + 12);
  const labelY = -(r + 22);
  return (
    <g className={`ccv9-tick ${delayClass}`}>
      <line
        x1="0"
        y1={tickInner}
        x2="0"
        y2={tickOuter}
        stroke="var(--color-cc-card-border-hover)"
        strokeWidth="1"
      />
      <text
        x="0"
        y={labelY}
        textAnchor="middle"
        fill="var(--color-cc-ink-dim, #94a3b8)"
        fontFamily="var(--font-mono)"
        fontSize="13"
        letterSpacing="2"
        style={{ textTransform: "uppercase" }}
      >
        {label}
      </text>
    </g>
  );
}

/* ---------- Ring-stamp glyph (small motif) ---------- */

function RingStamp({
  highlight,
  className = "h-12 w-12",
}: {
  readonly highlight: 0 | 1 | 2 | 3 | 4;
  readonly className?: string;
}) {
  // Compact 3-visible-ring stamp around the highlighted layer index.
  // We render 5 rings at small radii and pick a stroke per ring.
  const radii = [6, 12, 18, 24, 30];
  return (
    <svg
      viewBox="-36 -36 72 72"
      className={className}
      role="img"
      aria-label={`Ring ${highlight + 1} of 5 highlighted`}
    >
      {radii.map((r, i) => (
        <circle
          key={r}
          cx="0"
          cy="0"
          r={r}
          fill="none"
          stroke={
            i === highlight
              ? "var(--color-cc-accent)"
              : "var(--color-cc-card-border)"
          }
          strokeWidth={i === highlight ? 1.5 : 1}
        />
      ))}
    </svg>
  );
}

/* ---------- Platform as rings explainer ---------- */

function PlatformAsRings() {
  return (
    <section
      aria-labelledby="rings-heading"
      className="border-cc-card-border mt-8 border-t pt-16 sm:pt-24"
    >
      <div className="flex items-start gap-4">
        <RingStamp highlight={2} className="h-10 w-10 flex-none" />
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            The platform in five rings
          </p>
          <h2
            id="rings-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
          >
            Five layers, from kernel outward.
          </h2>
        </div>
      </div>

      <ol className="mt-12 flex flex-col gap-10">
        {RING_LAYERS.map((layer, i) => (
          <li
            key={layer.key}
            className="grid items-start gap-6 sm:grid-cols-[auto_1fr] sm:gap-8"
          >
            <div className="flex items-center gap-4 sm:flex-col sm:items-start sm:gap-3">
              <span className="text-cc-accent font-mono text-xs tracking-[0.22em] uppercase">
                {layer.index}
              </span>
              <RingStamp
                highlight={i as 0 | 1 | 2 | 3 | 4}
                className="h-12 w-12"
              />
            </div>
            <div>
              <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.18em] uppercase">
                {layer.name}
              </p>
              <h3 className="font-heading text-cc-heading mt-2 text-xl font-semibold sm:text-2xl">
                {layer.statement}
              </h3>
              <p className="text-cc-ink mt-3 text-base leading-relaxed">
                {layer.body}
              </p>
            </div>
          </li>
        ))}
      </ol>
    </section>
  );
}

/* ---------- Nitro reel stage ---------- */

function ReelStage() {
  return (
    <section aria-labelledby="reel-heading" className="mt-20 pb-4 sm:mt-28">
      <div className="mb-6 flex items-start gap-4">
        <RingStamp highlight={4} className="h-10 w-10 flex-none" />
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Outer orbit
          </p>
          <h2
            id="reel-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-2 font-semibold"
          >
            The control plane your team logs into.
          </h2>
        </div>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg/70 overflow-hidden rounded-xl border shadow-[0_30px_80px_-40px_rgba(94,234,212,0.35)]">
        <NitroReel />
      </div>
      <p className="text-cc-ink-dim mt-5 text-center text-sm">
        Author, observe, diagnose, evolve, and federate. The Nitro reel is a
        live render of the same control plane your team logs into.
      </p>
    </section>
  );
}

/* ---------- Logo band ---------- */

function LogoBand() {
  return (
    <div className="border-cc-card-border mt-20 border-y sm:mt-28">
      <LogoCloud />
    </div>
  );
}

/* ---------- Capabilities as orbital pulls ---------- */

function CapabilitiesAsOrbits() {
  return (
    <section aria-labelledby="capabilities-heading" className="mt-20 sm:mt-28">
      <div className="flex items-start gap-4">
        <RingStamp highlight={2} className="h-10 w-10 flex-none" />
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Orbital pulls
          </p>
          <h2
            id="capabilities-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-2 font-semibold"
          >
            What each ring earns you.
          </h2>
          <p className="text-cc-ink mt-4 max-w-2xl text-base">
            Each capability touches a specific layer of the platform. The stamp
            on the left shows which ring it lives on.
          </p>
        </div>
      </div>

      <div className="mt-14 flex flex-col gap-14">
        {CAPABILITY_ORBITS.map((cap) => (
          <CapabilityRow key={cap.eyebrow} cap={cap} />
        ))}
      </div>
    </section>
  );
}

function CapabilityRow({ cap }: { readonly cap: CapabilityOrbit }) {
  return (
    <article className="grid items-start gap-8 md:grid-cols-[auto_1fr] md:gap-10">
      <div className="flex flex-row items-center gap-4 md:flex-col md:items-start md:gap-3">
        <RingStamp highlight={cap.ringIndex} className="h-16 w-16" />
        <p className="text-cc-accent font-mono text-xs tracking-[0.22em] uppercase">
          {cap.eyebrow}
        </p>
      </div>
      <div>
        <h3 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">
          {cap.title}
        </h3>
        <p className="text-cc-ink mt-4 text-base leading-relaxed">{cap.body}</p>
        <ul className="mt-5 flex flex-col gap-3">
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

/* ---------- Fusion deep cut ---------- */

function FusionDeepCut() {
  return (
    <section
      aria-labelledby="fusion-heading"
      className="border-cc-card-border bg-cc-card-bg/40 mt-20 rounded-3xl border p-6 sm:mt-28 sm:p-10"
    >
      <div className="grid gap-10 lg:grid-cols-[1fr_1.25fr] lg:items-center">
        <div>
          <div className="flex items-center gap-4">
            <RingStamp highlight={3} className="h-12 w-12 flex-none" />
            <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
              Fusion ring
            </p>
          </div>
          <h2
            id="fusion-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold"
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

/* ---------- Product family as orbits ---------- */

function ProductFamilyAsOrbits() {
  return (
    <section aria-labelledby="family-heading" className="mt-20 sm:mt-28">
      <div className="flex items-start gap-4">
        <RingStamp highlight={2} className="h-10 w-10 flex-none" />
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            One family, on three primary orbits
          </p>
          <h2
            id="family-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-2 font-semibold"
          >
            The open source platform behind it.
          </h2>
          <p className="text-cc-ink mt-4 max-w-2xl text-base">
            Each library stands on its own, and each one assumes the others
            exist. MIT licensed, designed together for .NET.
          </p>
        </div>
      </div>

      <ol className="mt-12 flex flex-col">
        {PRIMARY_PRODUCTS.map((p, i) => (
          <PrimaryOrbitRow key={p.name} product={p} index={i + 1} />
        ))}
      </ol>

      <div className="mt-16">
        <div className="text-cc-nav-label mb-4 font-mono text-[0.65rem] tracking-[0.2em] uppercase">
          Companion orbit
        </div>
        <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-4">
          {SECONDARY_PRODUCTS.map((p) => (
            <CompanionCard key={p.name} product={p} />
          ))}
        </div>
      </div>
    </section>
  );
}

function PrimaryOrbitRow({
  product,
  index,
}: {
  readonly product: ProductCardProps;
  readonly index: number;
}) {
  const Icon = product.icon;
  const isExternal = product.external;
  const linkClass =
    "text-cc-accent hover:text-cc-accent-hover mt-4 inline-flex items-center gap-1 text-sm font-medium";
  return (
    <li className="border-cc-card-border grid items-start gap-6 border-b py-8 sm:grid-cols-[auto_1fr_auto] sm:gap-10 sm:py-10">
      <div className="flex items-center gap-4">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
          {String(index).padStart(2, "0")}
        </span>
        <Icon className="h-12 w-12" />
      </div>
      <div>
        <h3 className="font-heading text-cc-heading text-xl font-semibold sm:text-2xl">
          {product.name}
        </h3>
        <p className="text-cc-nav-label mt-1 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
          {product.tagline}
        </p>
        <p className="text-cc-ink mt-4 max-w-2xl text-base leading-relaxed">
          {product.description}
        </p>
        {isExternal ? (
          <a
            href={product.href}
            target="_blank"
            rel="noopener noreferrer"
            className={`group ${linkClass}`}
          >
            {product.linkLabel}{" "}
            <span
              aria-hidden
              className="transition-transform group-hover:translate-x-0.5"
            >
              {"->"}
            </span>
          </a>
        ) : (
          <NextLink href={product.href} className={`group ${linkClass}`}>
            {product.linkLabel}{" "}
            <span
              aria-hidden
              className="transition-transform group-hover:translate-x-0.5"
            >
              {"->"}
            </span>
          </NextLink>
        )}
      </div>
      <div className="hidden sm:block">
        <RingStamp
          highlight={index as 0 | 1 | 2 | 3 | 4}
          className="h-16 w-16"
        />
      </div>
    </li>
  );
}

function CompanionCard({ product }: { readonly product: ProductCardProps }) {
  const Icon = product.icon;
  const isExternal = product.external;
  const linkClass =
    "text-cc-accent hover:text-cc-accent-hover mt-4 inline-flex items-center gap-1 text-sm font-medium";
  return (
    <div className="border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg/40 flex h-full flex-col rounded-2xl border p-6 transition-colors">
      <Icon className="h-10 w-10" />
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
          {product.linkLabel}{" "}
          <span
            aria-hidden
            className="transition-transform group-hover:translate-x-0.5"
          >
            {"->"}
          </span>
        </a>
      ) : (
        <NextLink href={product.href} className={`group ${linkClass}`}>
          {product.linkLabel}{" "}
          <span
            aria-hidden
            className="transition-transform group-hover:translate-x-0.5"
          >
            {"->"}
          </span>
        </NextLink>
      )}
    </div>
  );
}

/* ---------- OSS + pricing pointer combined ---------- */

function OpenSourcePricingBand() {
  return (
    <section
      aria-labelledby="oss-pricing-heading"
      className="border-cc-card-border bg-cc-card-bg/50 mt-20 rounded-3xl border p-8 sm:mt-28 sm:p-10"
    >
      <div className="grid gap-10 md:grid-cols-[1.1fr_1fr]">
        <div>
          <div className="flex items-center gap-4">
            <RingStamp highlight={0} className="h-10 w-10 flex-none" />
            <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
              MIT, on GitHub
            </p>
          </div>
          <h2
            id="oss-pricing-heading"
            className="font-heading text-cc-heading text-h4 mt-4 font-semibold"
          >
            Open source at the core. Honest pricing on top.
          </h2>
          <p className="text-cc-ink mt-4 text-base">
            The Hot Chocolate platform itself is open source and free under MIT.
            Nitro Cloud has a free Shared Instance and a $400 Dedicated
            Instance. Support plans start at $450 Startup and $1,300 Business,
            with Custom Enterprise for the rest.
          </p>
          <ul className="text-cc-ink mt-6 grid gap-3 text-sm sm:grid-cols-2">
            <OssCheck>MIT licensed across every package</OssCheck>
            <OssCheck>Public releases and changelogs</OssCheck>
            <OssCheck>Public roadmap and discussions</OssCheck>
            <OssCheck>Community Slack and GitHub triage</OssCheck>
          </ul>
          <div className="mt-7 flex flex-wrap items-center gap-3">
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
            <SolidButton href="/pricing">See full pricing</SolidButton>
          </div>
        </div>

        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Price peek
          </p>
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            <PricePeek
              label="Shared Instance"
              price="Free"
              note="pay as you go"
            />
            <PricePeek
              label="Dedicated Instance"
              price="$400"
              note="per month"
              highlight
            />
            <PricePeek label="Startup support" price="$450" note="per month" />
            <PricePeek
              label="Business support"
              price="$1,300"
              note="per month"
            />
          </div>
          <div className="mt-5">
            <OutlineButton href="/services/support/contact">
              Talk to us about Enterprise
            </OutlineButton>
          </div>
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
          : "border-cc-card-border bg-cc-card-bg/60"
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

/* ---------- Blog strip ---------- */

function BlogStrip() {
  return (
    <section className="mt-20 sm:mt-28">
      <FromOurBlog />
    </section>
  );
}

/* ---------- Closing CTA with the one allowed brand-spectrum moment ---------- */

function ClosingCta() {
  return (
    <section className="mt-20 mb-10 text-center sm:mt-28">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
        Ready when you are
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mx-auto mt-4 max-w-3xl font-semibold">
        Ship your GraphQL platform,{" "}
        <span
          className="bg-clip-text pb-[0.08em] text-transparent"
          style={{ backgroundImage: SPECTRUM_GRADIENT }}
        >
          built outward from .NET
        </span>
        .
      </h2>
      <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
        Start with Hot Chocolate at the kernel. Wire up Strawberry Shake. Plug
        in Nitro for the registry, CI, and telemetry. Free to start, MIT to
        keep.
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
