import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { NitroPricing } from "@/src/components/home/NitroPricing";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroReel } from "@/src/nitro";

export const metadata: Metadata = {
  title: "ChilliCream | The GraphQL Platform for .NET Teams",
  description:
    "ChilliCream is the GraphQL platform for .NET teams. Build with Hot Chocolate, ship with Nitro, evolve with Fusion. Open source, MIT licensed core.",
  keywords: [
    "ChilliCream",
    "Hot Chocolate",
    "Strawberry Shake",
    "Nitro",
    "Mocha",
    "Green Donut",
    "Cookie Crumble",
    "Fusion",
    "GraphQL .NET",
    "GraphQL platform",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "ChilliCream | The GraphQL Platform for .NET Teams",
    description:
      "Build, observe, evolve. The end to end GraphQL platform purpose built for .NET teams.",
  },
};

/* ------------------------------------------------------------------ *
 * Inline primitives                                                   *
 * ------------------------------------------------------------------ */

interface EyebrowProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function Eyebrow({ children, className }: EyebrowProps) {
  return (
    <span
      className={`text-cc-nav-label font-mono text-[0.7rem] tracking-[0.28em] uppercase ${className ?? ""}`}
    >
      {children}
    </span>
  );
}

interface SectionHeadingProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly intro?: ReactNode;
  readonly align?: "left" | "center";
}

function SectionHeading({
  eyebrow,
  title,
  intro,
  align = "left",
}: SectionHeadingProps) {
  const alignCls = align === "center" ? "items-center text-center" : "";
  return (
    <div className={`flex flex-col gap-4 ${alignCls}`}>
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="text-cc-heading text-h4 font-heading sm:text-h3 max-w-3xl font-semibold tracking-tight">
        {title}
      </h2>
      {intro ? (
        <p className="text-cc-prose max-w-2xl text-base sm:text-lg">{intro}</p>
      ) : null}
    </div>
  );
}

function SectionDivider() {
  return (
    <div className="relative h-px w-full" aria-hidden>
      <div className="bg-cc-card-border absolute inset-0" />
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Inline SVG art                                                      *
 * ------------------------------------------------------------------ */

/** Decorative .NET wedge mark for the hero. Uses cc tokens only; the
 *  brand spectrum lives on the hero headline so this art stays on the
 *  accent token. */
function DotNetWedgeArt({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 320 320"
      width="100%"
      height="100%"
      className={className}
      role="img"
      aria-label="GraphQL plus .NET mark"
    >
      <defs>
        <radialGradient id="cc-lv2-glow" cx="50%" cy="50%" r="55%">
          <stop offset="0%" stopColor="rgba(94,234,212,0.22)" />
          <stop offset="100%" stopColor="rgba(94,234,212,0)" />
        </radialGradient>
      </defs>
      <circle cx="160" cy="160" r="150" fill="url(#cc-lv2-glow)" />
      <circle
        cx="160"
        cy="160"
        r="120"
        fill="none"
        stroke="var(--color-cc-card-border)"
        strokeWidth="1"
      />
      <circle
        cx="160"
        cy="160"
        r="80"
        fill="none"
        stroke="var(--color-cc-card-border)"
        strokeWidth="1"
      />
      {/* GraphQL-style hex frame */}
      <polygon
        points="160,40 264,100 264,220 160,280 56,220 56,100"
        fill="none"
        stroke="var(--color-cc-accent)"
        strokeWidth="1.5"
        opacity="0.85"
      />
      {/* Node dots */}
      <g fill="var(--color-cc-accent)">
        <circle cx="160" cy="40" r="5" />
        <circle cx="264" cy="100" r="5" />
        <circle cx="264" cy="220" r="5" />
        <circle cx="160" cy="280" r="5" />
        <circle cx="56" cy="220" r="5" />
        <circle cx="56" cy="100" r="5" />
      </g>
      <circle cx="160" cy="160" r="6" fill="var(--color-cc-heading)" />
      {/* .NET wordmark */}
      <text
        x="160"
        y="170"
        textAnchor="middle"
        fontFamily="var(--font-heading)"
        fontWeight={700}
        fontSize={42}
        letterSpacing="-0.02em"
        fill="var(--color-cc-heading)"
      >
        .NET
      </text>
      <text
        x="160"
        y="206"
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={11}
        letterSpacing="0.32em"
        fill="var(--color-cc-ink-dim)"
      >
        GRAPHQL NATIVE
      </text>
    </svg>
  );
}

interface ProductGlyphProps {
  readonly id: string;
}

/** Compact icon set for the product grid. Each glyph uses cc-* tokens only
 *  (no spectrum), so the gradient stays a single accent on this page. */
function ProductGlyph({ id }: ProductGlyphProps) {
  const stroke = "var(--color-cc-accent)";
  const dim = "var(--color-cc-card-border-hover)";
  switch (id) {
    case "hotchocolate":
      return (
        <svg viewBox="0 0 48 48" width="40" height="40" aria-hidden>
          <rect
            x="10"
            y="14"
            width="28"
            height="24"
            rx="6"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <path
            d="M16 14 V10 M24 14 V8 M32 14 V10"
            stroke={stroke}
            strokeWidth="1.5"
            strokeLinecap="round"
          />
          <path
            d="M38 22 H42 A4 4 0 0 1 42 30 H38"
            fill="none"
            stroke={dim}
            strokeWidth="1.5"
          />
          <path
            d="M18 24 H30 M18 30 H26"
            stroke={dim}
            strokeWidth="1.5"
            strokeLinecap="round"
          />
        </svg>
      );
    case "strawberryshake":
      return (
        <svg viewBox="0 0 48 48" width="40" height="40" aria-hidden>
          <path
            d="M14 18 L24 8 L34 18"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
            strokeLinejoin="round"
          />
          <path
            d="M12 18 H36 L30 40 H18 Z"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
            strokeLinejoin="round"
          />
          <path
            d="M18 24 H30 M20 30 H28"
            stroke={dim}
            strokeWidth="1.5"
            strokeLinecap="round"
          />
        </svg>
      );
    case "nitro":
      return (
        <svg viewBox="0 0 48 48" width="40" height="40" aria-hidden>
          <rect
            x="6"
            y="10"
            width="36"
            height="24"
            rx="3"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <path d="M6 18 H42" stroke={stroke} strokeWidth="1.5" />
          <circle cx="10" cy="14" r="1.2" fill={stroke} />
          <circle cx="14" cy="14" r="1.2" fill={stroke} />
          <path
            d="M12 24 L18 24 L20 28 L24 20 L28 28 L30 24 L36 24"
            fill="none"
            stroke={dim}
            strokeWidth="1.5"
            strokeLinejoin="round"
          />
          <path
            d="M18 40 H30"
            stroke={dim}
            strokeWidth="1.5"
            strokeLinecap="round"
          />
          <path d="M24 34 V40" stroke={dim} strokeWidth="1.5" />
        </svg>
      );
    case "mocha":
      return (
        <svg viewBox="0 0 48 48" width="40" height="40" aria-hidden>
          <circle
            cx="24"
            cy="24"
            r="14"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <path d="M14 24 H34" stroke={stroke} strokeWidth="1.5" />
          <path d="M24 10 V38" stroke={dim} strokeWidth="1.5" />
          <circle cx="24" cy="24" r="3" fill={stroke} />
        </svg>
      );
    case "greendonut":
      return (
        <svg viewBox="0 0 48 48" width="40" height="40" aria-hidden>
          <circle
            cx="24"
            cy="24"
            r="15"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <circle
            cx="24"
            cy="24"
            r="6"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <path
            d="M9 24 A15 15 0 0 1 24 9"
            fill="none"
            stroke={dim}
            strokeWidth="1.5"
            strokeLinecap="round"
          />
        </svg>
      );
    case "cookiecrumble":
      return (
        <svg viewBox="0 0 48 48" width="40" height="40" aria-hidden>
          <circle
            cx="24"
            cy="24"
            r="15"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <circle cx="18" cy="20" r="1.6" fill={stroke} />
          <circle cx="28" cy="18" r="1.6" fill={stroke} />
          <circle cx="30" cy="28" r="1.6" fill={stroke} />
          <circle cx="20" cy="30" r="1.6" fill={dim} />
          <circle cx="24" cy="24" r="1.6" fill={dim} />
        </svg>
      );
    case "fusion":
      return (
        <svg viewBox="0 0 48 48" width="40" height="40" aria-hidden>
          <circle
            cx="12"
            cy="14"
            r="3.5"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <circle
            cx="36"
            cy="14"
            r="3.5"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <circle
            cx="12"
            cy="34"
            r="3.5"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <circle
            cx="36"
            cy="34"
            r="3.5"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <rect
            x="20"
            y="20"
            width="8"
            height="8"
            rx="2"
            fill="none"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <path
            d="M15 14 L20 22 M33 14 L28 22 M15 34 L20 26 M33 34 L28 26"
            stroke={dim}
            strokeWidth="1.5"
            strokeLinecap="round"
          />
        </svg>
      );
    default:
      return null;
  }
}

/* ------------------------------------------------------------------ *
 * Section: hero                                                       *
 * ------------------------------------------------------------------ */

function Hero() {
  return (
    <section className="relative pt-10 pb-16 sm:pt-16 sm:pb-20">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage:
            "radial-gradient(ellipse 60% 70% at 70% 10%, rgba(94,234,212,0.10), transparent 65%)",
        }}
      />
      <div className="grid items-center gap-10 lg:grid-cols-[1.15fr_1fr]">
        <div className="flex flex-col gap-6">
          <Eyebrow>Built for .NET. Built in .NET.</Eyebrow>
          <h1 className="text-cc-heading font-heading text-h2 sm:text-h1 leading-[1.05] font-semibold tracking-tight">
            The GraphQL platform{" "}
            <span
              style={{
                backgroundImage:
                  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
                WebkitBackgroundClip: "text",
                backgroundClip: "text",
                color: "transparent",
              }}
            >
              for .NET teams.
            </span>
          </h1>
          <p className="text-cc-prose max-w-xl text-base sm:text-lg">
            Hot Chocolate, Strawberry Shake, Nitro, Fusion, and the rest of the
            family. One toolchain that runs alongside your existing .NET stack,
            from the first resolver to the federated graph in production.
          </p>
          <div className="flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              Launch Nitro
            </OutlineButton>
          </div>
          <ul className="text-cc-ink-dim mt-2 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm">
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon size={14} />
              </span>
              MIT open source core
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon size={14} />
              </span>
              Source generated Hot Chocolate
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon size={14} />
              </span>
              Self host or use Nitro
            </li>
          </ul>
        </div>
        <div className="relative mx-auto w-full max-w-md lg:max-w-none">
          <div className="border-cc-card-border bg-cc-card-bg aspect-square rounded-3xl border p-4 sm:p-6">
            <DotNetWedgeArt />
          </div>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Section: what .NET teams ship                                       *
 * ------------------------------------------------------------------ */

interface WedgeCard {
  readonly tag: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

const WEDGE_CARDS: readonly WedgeCard[] = [
  {
    tag: "Commerce grade graphs",
    title: "Catalog, cart, checkout. One graph.",
    body: "Catalog and checkout graphs can run on Hot Chocolate and Fusion, with source generated resolvers and Green Donut batching on hot paths.",
    bullets: [
      "Pooled buffers and zero allocation hot paths",
      "Green Donut DataLoader for N plus 1",
      "Persisted operations and request pipeline hooks",
    ],
  },
  {
    tag: "Regulated enterprise",
    title: "Audit ready, on your runtime.",
    body: "Regulated domain graphs can run on Hot Chocolate with the Fusion gateway self hosted in your VNet, your region, against your own identity provider.",
    bullets: [
      "Self host the Fusion gateway, no hosted dependency",
      "Authorization and policy at the field level",
      "Persisted queries and request governance",
    ],
  },
  {
    tag: ".NET native end to end",
    title: "From your DbContext to a typed client.",
    body: "Hot Chocolate lives inside .NET services next to EF Core, and Strawberry Shake generates a typed client at MSBuild time so the consuming app moves in lockstep with the schema.",
    bullets: [
      "EF Core projections and async streaming",
      "OpenTelemetry tracing and metrics out of the box",
      "Strawberry Shake typed client via MSBuild",
    ],
  },
];

function WedgeBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="flex flex-col gap-10">
        <SectionHeading
          eyebrow="What .NET teams ship with us"
          title="Three shapes of work, one platform."
          intro="A flavor of what .NET teams ship on the platform: commerce grade graphs, regulated enterprise workloads, and .NET native end to end stacks."
        />
        <div className="grid gap-5 lg:grid-cols-3">
          {WEDGE_CARDS.map((card, idx) => (
            <article
              key={card.tag}
              className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col gap-5 rounded-2xl border p-6 transition-colors sm:p-7"
            >
              <div className="flex items-center justify-between">
                <Eyebrow>{card.tag}</Eyebrow>
                <span className="text-cc-ink-faint font-heading text-2xl font-semibold tabular-nums">
                  0{idx + 1}
                </span>
              </div>
              <h3 className="text-cc-heading font-heading text-xl font-semibold tracking-tight sm:text-2xl">
                {card.title}
              </h3>
              <p className="text-cc-prose text-sm sm:text-base">{card.body}</p>
              <ul className="mt-1 flex flex-col gap-2">
                {card.bullets.map((b) => (
                  <li
                    key={b}
                    className="text-cc-ink flex items-start gap-2 text-sm"
                  >
                    <span className="text-cc-accent mt-0.5 shrink-0">
                      <CheckIcon size={14} />
                    </span>
                    <span>{b}</span>
                  </li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Section: platform pillars                                           *
 * ------------------------------------------------------------------ */

interface Pillar {
  readonly tag: string;
  readonly title: string;
  readonly body: string;
}

const PILLARS: readonly Pillar[] = [
  {
    tag: "Build",
    title: "Schema first or code first.",
    body: "Hot Chocolate gives you the choice. Source generation keeps resolvers and types in sync without runtime reflection on hot paths.",
  },
  {
    tag: "Observe",
    title: "Every operation, every span.",
    body: "Nitro telemetry captures operations, traces, and errors. Wire it to your existing OpenTelemetry pipeline or use Nitro hosted.",
  },
  {
    tag: "Evolve",
    title: "Compose without breaking clients.",
    body: "Fusion composes subgraphs at planning time and ships a single, governed schema. Published clients affected are surfaced before the change ships.",
  },
  {
    tag: "Agentic",
    title: "Built for tool use and MCP.",
    body: "GraphQL schemas read like contracts. Hot Chocolate exposes the introspection and execution surface agents need to call your APIs safely.",
  },
  {
    tag: "Workflows",
    title: "Long running, validated up front.",
    body: "Mocha runs validated sagas with exactly once processing. Workflows are checked before traffic, not after a partial failure.",
  },
];

function PillarsBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="flex flex-col gap-10">
        <SectionHeading
          eyebrow="The platform"
          title="Build, observe, evolve. Plus the parts most platforms skip."
          intro="ChilliCream is not a server with a dashboard bolted on. Five surfaces, one runtime, designed to live in your CI and your production cluster."
        />
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {PILLARS.map((p, idx) => (
            <div
              key={p.tag}
              className="border-cc-card-border bg-cc-card-bg flex flex-col gap-3 rounded-2xl border p-6 sm:p-7"
            >
              <div className="flex items-center gap-3">
                <span className="border-cc-card-border text-cc-accent font-heading flex h-9 w-9 items-center justify-center rounded-full border text-sm font-semibold tabular-nums">
                  {idx + 1}
                </span>
                <Eyebrow>{p.tag}</Eyebrow>
              </div>
              <h3 className="text-cc-heading font-heading text-lg font-semibold tracking-tight sm:text-xl">
                {p.title}
              </h3>
              <p className="text-cc-prose text-sm">{p.body}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Section: the product family                                         *
 * ------------------------------------------------------------------ */

interface ProductCard {
  readonly id: string;
  readonly name: string;
  readonly role: string;
  readonly tagline: string;
  readonly body: string;
  readonly bullets: readonly string[];
  readonly href: string;
  readonly cta: string;
}

const PRODUCTS: readonly ProductCard[] = [
  {
    id: "hotchocolate",
    name: "Hot Chocolate",
    role: "GraphQL server",
    tagline: "Source generated server for .NET.",
    body: "The open source GraphQL server at the heart of the platform. Schema first or code first, with source generation on hot paths.",
    bullets: [
      "Source generated resolvers and types",
      "Schema first or code first authoring",
      "Subscriptions, persisted queries, request hooks",
    ],
    href: "/products/hotchocolate",
    cta: "Explore Hot Chocolate",
  },
  {
    id: "strawberryshake",
    name: "Strawberry Shake",
    role: "Typed .NET client",
    tagline: "MSBuild generated GraphQL client.",
    body: "Strawberry Shake generates a typed .NET client at build time via MSBuild code generation. Your app moves in lockstep with the schema, with offline first stores baked in.",
    bullets: [
      "MSBuild time code generation",
      "Reactive store with cached entities",
      "Persisted operations and codegen profiles",
    ],
    href: "/products/strawberryshake",
    cta: "Explore Strawberry Shake",
  },
  {
    id: "nitro",
    name: "Nitro",
    role: "IDE and control plane",
    tagline: "The IDE, telemetry, and schema registry.",
    body: "The GraphQL IDE that serves from your endpoint plus a hosted control plane for telemetry, the schema registry, and the operation library. Configure Nitro telemetry on the server to feed it.",
    bullets: [
      "Schema aware IDE served from your endpoint",
      "Operations, traces, and error timelines",
      "Schema registry and persisted operations",
    ],
    href: "https://nitro.chillicream.com",
    cta: "Launch Nitro",
  },
  {
    id: "fusion",
    name: "Fusion",
    role: "Federation and gateway",
    tagline: "Composition at planning time.",
    body: "Fusion composes subgraphs at planning time and runs as a gateway you self host. The composition step rejects breaking changes before they hit production.",
    bullets: [
      "Composition at planning time, not at request time",
      "Self hosted gateway, your VNet, your region",
      "Published clients affected surfaced before publish",
    ],
    href: "/docs/fusion",
    cta: "Explore Fusion",
  },
  {
    id: "mocha",
    name: "Mocha",
    role: "Workflow engine",
    tagline: "Validated sagas, exactly once processing.",
    body: "Mocha runs long lived workflows and sagas. Sagas are validated before they receive traffic, and step execution is exactly once at the processing layer.",
    bullets: [
      "Sagas validated before traffic",
      "Exactly once processing semantics",
      "Durable timers and compensations",
    ],
    href: "/docs/mocha",
    cta: "See Mocha",
  },
  {
    id: "greendonut",
    name: "Green Donut",
    role: "DataLoader",
    tagline: "Batching that erases N plus 1.",
    body: "Green Donut is the DataLoader implementation behind Hot Chocolate. Group, batch, and cache field resolution so the database does the right amount of work.",
    bullets: [
      "Per request batching and caching",
      "Group by key, by tenant, by anything",
      "Composes cleanly with EF Core projections",
    ],
    href: "https://github.com/ChilliCream/graphql-platform",
    cta: "Read about Green Donut",
  },
  {
    id: "cookiecrumble",
    name: "Cookie Crumble",
    role: "Snapshot testing",
    tagline: "Snapshots for results and schemas.",
    body: "The snapshot library we use to test the platform. Native support for GraphQL execution results, HTTP responses, and markdown snapshots for multi shape state.",
    bullets: [
      "Native IExecutionResult and GraphQLHttpResponse",
      "Inline and markdown snapshot formats",
      "Diffs that read like a code review",
    ],
    href: "https://github.com/ChilliCream/graphql-platform",
    cta: "See Cookie Crumble",
  },
];

interface ProductCardCellProps {
  readonly product: ProductCard;
}

function ProductCardCell({ product }: ProductCardCellProps) {
  const internal = product.href.startsWith("/");
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex flex-col gap-4 rounded-2xl border p-6 transition-colors sm:p-7">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <span
            aria-hidden
            className="border-cc-card-border bg-cc-surface/70 flex h-12 w-12 shrink-0 items-center justify-center rounded-xl border"
          >
            <ProductGlyph id={product.id} />
          </span>
          <div className="flex flex-col">
            <span className="text-cc-heading font-heading text-lg font-semibold tracking-tight">
              {product.name}
            </span>
            <Eyebrow>{product.role}</Eyebrow>
          </div>
        </div>
      </div>
      <p className="text-cc-heading text-sm font-medium">{product.tagline}</p>
      <p className="text-cc-prose text-sm">{product.body}</p>
      <ul className="flex flex-col gap-2">
        {product.bullets.map((b) => (
          <li key={b} className="text-cc-ink flex items-start gap-2 text-sm">
            <span className="text-cc-accent mt-0.5 shrink-0">
              <CheckIcon size={14} />
            </span>
            <span>{b}</span>
          </li>
        ))}
      </ul>
      <div className="mt-auto pt-2">
        {internal ? (
          <Link
            href={product.href}
            className="text-cc-accent hover:text-cc-accent-hover inline-flex items-center gap-1.5 font-mono text-xs no-underline"
          >
            {product.cta}
            <span aria-hidden>-&gt;</span>
          </Link>
        ) : (
          <a
            href={product.href}
            target="_blank"
            rel="noopener noreferrer"
            className="text-cc-accent hover:text-cc-accent-hover inline-flex items-center gap-1.5 font-mono text-xs no-underline"
          >
            {product.cta}
            <span aria-hidden>-&gt;</span>
          </a>
        )}
      </div>
    </article>
  );
}

function ProductFamilyBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="flex flex-col gap-10">
        <SectionHeading
          eyebrow="The product family"
          title="Seven products. One platform you actually own."
          intro="Each piece is useful on its own, and the suite is more than the sum. Hot Chocolate is the engine, Nitro is the control plane, Fusion is the gateway, and the rest fill in the gaps that .NET teams hit at scale."
        />
        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {PRODUCTS.map((product) => (
            <ProductCardCell key={product.id} product={product} />
          ))}
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Section: Nitro reel                                                 *
 * ------------------------------------------------------------------ */

function NitroBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="flex flex-col gap-10">
        <SectionHeading
          eyebrow="Nitro"
          title="The control plane behind it all."
          intro="Author, observe, diagnose, govern, and federate. Nitro is where the platform shows up to the people who keep it running. Five tabs of real product, on a loop."
          align="center"
        />
        <div className="border-cc-card-border bg-cc-card-bg mx-auto w-full max-w-5xl overflow-hidden rounded-xl border">
          <NitroReel />
        </div>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="https://nitro.chillicream.com">
            Launch Nitro
          </SolidButton>
          <OutlineButton href="/platform">See the platform</OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Section: open source                                                *
 * ------------------------------------------------------------------ */

function OpenSourceBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-7 sm:p-10">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0"
          style={{
            backgroundImage:
              "radial-gradient(ellipse 80% 90% at 100% 0%, rgba(94,234,212,0.08), transparent 60%), radial-gradient(ellipse 60% 70% at 0% 100%, rgba(124,146,198,0.07), transparent 60%)",
          }}
        />
        <div className="relative grid gap-8 lg:grid-cols-[1.1fr_1fr] lg:items-center">
          <div className="flex flex-col gap-5">
            <Eyebrow>Open source, MIT</Eyebrow>
            <h2 className="text-cc-heading font-heading text-h4 sm:text-h3 font-semibold tracking-tight">
              The core is open. The community is the engine.
            </h2>
            <p className="text-cc-prose max-w-xl text-base sm:text-lg">
              Hot Chocolate, Strawberry Shake, Green Donut, Cookie Crumble, and
              the Fusion composer all ship under the MIT license on GitHub. Use
              them in commercial work, fork them, contribute back. Nitro and the
              paid Fusion gateway sit on top for teams that want a managed path.
            </p>
            <div className="flex flex-wrap items-center gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                Star on GitHub
              </SolidButton>
              <OutlineButton href="/licensing/chillicream-license">
                Read the license
              </OutlineButton>
            </div>
          </div>
          <dl className="grid grid-cols-2 gap-3">
            {[
              { k: "License", v: "MIT" },
              { k: "Runtime", v: ".NET" },
              { k: "Languages", v: "C# and TS" },
              { k: "Source", v: "GitHub" },
            ].map((fact) => (
              <div
                key={fact.k}
                className="border-cc-card-border bg-cc-surface/60 rounded-xl border p-5"
              >
                <dt>
                  <Eyebrow>{fact.k}</Eyebrow>
                </dt>
                <dd className="text-cc-heading font-heading mt-2 text-2xl font-semibold tracking-tight">
                  {fact.v}
                </dd>
              </div>
            ))}
          </dl>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Section: pricing pointer                                            *
 * ------------------------------------------------------------------ */

function PricingPointerBand() {
  return (
    <section className="py-12 sm:py-16">
      <div className="flex flex-col gap-8">
        <SectionHeading
          eyebrow="Plans"
          title="Self host for free. Pay when you want a hand."
          intro="The open source platform is yours. Add a support plan when you want SLAs, or pick Dedicated when you want Nitro hosted for your team."
        />
        <NitroPricing />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Section: closing CTA                                                *
 * ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="relative my-12 overflow-hidden rounded-3xl">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          backgroundImage:
            "radial-gradient(ellipse 70% 100% at 50% 0%, rgba(94,234,212,0.14), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-card-bg relative rounded-3xl border px-6 py-14 text-center sm:px-12 sm:py-20">
        <Eyebrow>One platform, one .NET stack</Eyebrow>
        <h2 className="text-cc-heading font-heading mx-auto mt-4 max-w-3xl text-3xl font-semibold tracking-tight sm:text-5xl">
          Start with Hot Chocolate. Grow into the full platform.
        </h2>
        <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base sm:text-lg">
          Spin up a graph in minutes, wire Nitro telemetry when you are ready,
          add Fusion when one graph becomes many. Same toolchain, same .NET
          runtime, same team behind it.
        </p>
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch Nitro
          </OutlineButton>
        </div>
        <p className="text-cc-ink-dim mt-6 font-mono text-xs tracking-wide">
          MIT licensed. No card needed. Self host or use Nitro hosted.
        </p>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Page                                                                *
 * ------------------------------------------------------------------ */

export default function LandingV2Page() {
  return (
    <>
      <Hero />
      <LogoCloud />
      <SectionDivider />
      <WedgeBand />
      <SectionDivider />
      <PillarsBand />
      <SectionDivider />
      <ProductFamilyBand />
      <SectionDivider />
      <NitroBand />
      <SectionDivider />
      <OpenSourceBand />
      <SectionDivider />
      <PricingPointerBand />
      <ClosingCta />
    </>
  );
}
