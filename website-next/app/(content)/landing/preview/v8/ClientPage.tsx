"use client";

import { motion, useReducedMotion } from "motion/react";
import NextLink from "next/link";
import type {
  ComponentType,
  CSSProperties,
  PointerEvent,
  ReactNode,
} from "react";
import { useCallback, useRef, useState } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Fusion } from "@/src/icons/Fusion";
import { GitHubIcon } from "@/src/icons/GitHub";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";

/* -------------------------------------------------------------------------- */
/*  Constants                                                                  */
/* -------------------------------------------------------------------------- */

const SPECTRUM_GRADIENT =
  "linear-gradient(95deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const DOT_BG =
  "radial-gradient(circle, rgba(245,241,234,0.12) 1px, transparent 1.2px)";

const HALO_BG =
  "radial-gradient(circle, rgba(94,234,212,0.7) 1px, transparent 1.2px)";

interface Pillar {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
}

const PILLARS: readonly Pillar[] = [
  {
    eyebrow: "Build",
    title: "Source-generated server, typed client.",
    body: "Hot Chocolate is source-generated on the server. Strawberry Shake uses MSBuild code generation to turn each query into a typed C# API. The schema you ship is the one both sides agree on.",
  },
  {
    eyebrow: "Ship",
    title: "Schema registry, CI checks, safe rollouts.",
    body: "The schema registry tracks every published version. Nitro CI compares schema changes against the operations your published clients actually send, and surfaces which clients are affected before deploy.",
  },
  {
    eyebrow: "Operate",
    title: "OpenTelemetry traces, per-client tracking.",
    body: "Once Nitro is configured in the server, every operation carries OpenTelemetry traces, metrics, and logs. Per-client tracking via GraphQL-Client-Id and Version makes drift visible.",
  },
];

interface PlatformNode {
  readonly name: string;
  readonly tagline: string;
  readonly description: string;
  readonly href: string;
  readonly external?: boolean;
  readonly icon: ComponentType<{
    readonly className?: string;
    readonly style?: CSSProperties;
  }>;
  readonly col: number;
  readonly row: number;
}

const PLATFORM_NODES: readonly PlatformNode[] = [
  {
    name: "Hot Chocolate",
    tagline: "Server",
    description: "Source-generated GraphQL server on ASP.NET Core.",
    href: "/products/hotchocolate",
    icon: HotChocolate,
    col: 1,
    row: 1,
  },
  {
    name: "Strawberry Shake",
    tagline: "Client",
    description: "Typed .NET client via MSBuild code generation.",
    href: "/products/strawberryshake",
    icon: StrawberryShake,
    col: 2,
    row: 1,
  },
  {
    name: "Nitro",
    tagline: "Control plane",
    description: "Registry, CI checks, observability, GraphQL IDE.",
    href: "https://nitro.chillicream.com",
    external: true,
    icon: Nitro,
    col: 3,
    row: 1,
  },
  {
    name: "Fusion",
    tagline: "Composition",
    description:
      "Compose many subgraphs at planning time, run the gateway yourself.",
    href: "/products/fusion",
    icon: Fusion,
    col: 1,
    row: 2,
  },
  {
    name: "Mocha",
    tagline: "Workflows",
    description:
      "Source-generated mediator. Validated sagas, exactly-once processing.",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: Mocha,
    col: 2,
    row: 2,
  },
  {
    name: "Green Donut",
    tagline: "DataLoader",
    description: "Batches and caches resolver fetches so N+1 stops mattering.",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: GreenDonut,
    col: 3,
    row: 2,
  },
];

interface Stripe {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

const STRIPES: readonly Stripe[] = [
  {
    eyebrow: "Registry + CI",
    title: "Know which published clients a change affects.",
    body: "Nitro CI compares a proposed schema against every operation each app actually sends, and reports breaking changes before traffic moves.",
    bullets: [
      "Breaking change classification, safe to breaking",
      "Stage promotion with approval gates",
      "Rollback by republishing an earlier tag",
    ],
  },
  {
    eyebrow: "Observability + Fusion",
    title: "OpenTelemetry traces and self-run gateway.",
    body: "Operation insights surface p95, throughput, error rate, and impact. Fusion composes subgraphs at planning time, then runs the gateway in your own ASP.NET Core process.",
    bullets: [
      "Per-operation p95, p99, throughput",
      "Per-client tracking and version drift",
      "Self-hosted gateway, ASP.NET Core auth at the edge",
    ],
  },
];

interface Proof {
  readonly quote: string;
  readonly label: string;
}

const PROOF: readonly Proof[] = [
  {
    quote: "Source-generated, end to end.",
    label: "Hot Chocolate server + Strawberry Shake client",
  },
  {
    quote: "Composed before traffic moves.",
    label: "Fusion query plan at composition time",
  },
  {
    quote: "Published clients you can name.",
    label: "Nitro CI surfaces affected clients",
  },
];

/* -------------------------------------------------------------------------- */
/*  Dot-grid wrapper with hover halo                                           */
/* -------------------------------------------------------------------------- */

interface DotGridSurfaceProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly id?: string;
}

function DotGridSurface({ children, className = "", id }: DotGridSurfaceProps) {
  const reduced = useReducedMotion();
  const wrapperRef = useRef<HTMLDivElement | null>(null);
  const [active, setActive] = useState(false);

  const onMove = useCallback(
    (event: PointerEvent<HTMLDivElement>) => {
      if (reduced) {
        return;
      }
      const el = wrapperRef.current;
      if (!el) {
        return;
      }
      const rect = el.getBoundingClientRect();
      el.style.setProperty("--x", `${event.clientX - rect.left}px`);
      el.style.setProperty("--y", `${event.clientY - rect.top}px`);
      if (!active) {
        setActive(true);
      }
    },
    [active, reduced],
  );

  const onLeave = useCallback(() => {
    setActive(false);
  }, []);

  return (
    <div
      id={id}
      ref={wrapperRef}
      onPointerMove={onMove}
      onPointerLeave={onLeave}
      className={`relative isolate overflow-hidden ${className}`}
      style={
        {
          "--x": "50%",
          "--y": "50%",
        } as CSSProperties
      }
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          backgroundImage: DOT_BG,
          backgroundSize: "24px 24px",
          backgroundPosition: "0 0",
        }}
      />
      {!reduced ? (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 transition-opacity duration-300"
          style={{
            backgroundImage: HALO_BG,
            backgroundSize: "24px 24px",
            backgroundPosition: "0 0",
            opacity: active ? 1 : 0,
            WebkitMaskImage:
              "radial-gradient(circle 180px at var(--x) var(--y), #000 0%, rgba(0,0,0,0.6) 40%, transparent 75%)",
            maskImage:
              "radial-gradient(circle 180px at var(--x) var(--y), #000 0%, rgba(0,0,0,0.6) 40%, transparent 75%)",
          }}
        />
      ) : null}
      <div className="relative z-10">{children}</div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export function ClientPage({ blogSlot }: { readonly blogSlot?: ReactNode }) {
  return (
    <>
      <Hero />
      <LogoBand />
      <PillarsRow />
      <ProductMap />
      <CapabilityStripes />
      <ProofStrip />
      {blogSlot}
      <FinalCta />
    </>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                       */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <DotGridSurface className="-mx-[max(0px,calc((100vw-72rem)/2))] px-[max(0px,calc((100vw-72rem)/2))]">
      <section className="grid items-center gap-10 py-16 sm:py-24 md:grid-cols-3 md:gap-12">
        <div className="md:col-span-2">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
            The ChilliCream GraphQL platform
          </p>
          <h1 className="font-heading text-cc-heading sm:text-h2 lg:text-h1 mt-6 max-w-3xl text-4xl leading-[1.05] font-semibold tracking-[-0.02em] text-balance">
            The GraphQL platform for{" "}
            <span className="text-cc-accent">.NET</span>.
          </h1>
          <p className="text-cc-ink mt-6 max-w-2xl text-base text-pretty sm:text-lg">
            Hot Chocolate ships the server. Strawberry Shake ships the typed
            client. Nitro ships the control plane, registry, and observability.
            One platform, designed together, open source on GitHub.
          </p>
          <div className="mt-8 flex flex-wrap items-center gap-3">
            <SolidButton href="/get-started">
              Start with Hot Chocolate
            </SolidButton>
            <OutlineButton href="/services/support/contact">
              Talk to us
            </OutlineButton>
          </div>
          <p className="text-cc-nav-label mt-5 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
            MIT licensed. Self-host or run on Nitro Cloud.
          </p>
        </div>
        <div className="md:col-span-1">
          <SchemaGridCard />
        </div>
      </section>
    </DotGridSurface>
  );
}

function SchemaGridCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/80 relative overflow-hidden rounded-2xl border p-5 backdrop-blur-sm">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-50"
        style={{
          backgroundImage: DOT_BG,
          backgroundSize: "24px 24px",
        }}
      />
      <div className="relative">
        <div className="text-cc-nav-label mb-3 flex items-center justify-between font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          <span>schema.graphql</span>
          <span className="text-cc-accent">live</span>
        </div>
        <pre className="text-cc-ink font-mono text-[0.72rem] leading-relaxed">
          <code>
            <span className="text-cc-nav-label">type</span>{" "}
            <span className="text-cc-accent">Query</span> {"{"}
            {"\n"}
            {"  "}cart(id: ID!): Cart
            {"\n"}
            {"  "}me: Viewer!
            {"\n"}
            {"}"}
            {"\n\n"}
            <span className="text-cc-nav-label">type</span>{" "}
            <span className="text-cc-accent">Cart</span> {"{"}
            {"\n"}
            {"  "}totals: Money!
            {"\n"}
            {"}"}
          </code>
        </pre>
        <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3 font-mono text-[0.65rem]">
          <span className="text-cc-ink-dim">composed</span>
          <span className="text-cc-accent">3 subgraphs</span>
        </div>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Logo band                                                                  */
/* -------------------------------------------------------------------------- */

function LogoBand() {
  return (
    <div className="border-cc-card-border border-y">
      <LogoCloud />
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Pillars row                                                                */
/* -------------------------------------------------------------------------- */

function PillarsRow() {
  return (
    <section aria-labelledby="pillars-heading" className="py-20 sm:py-24">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
          The platform, in three moves
        </p>
        <h2
          id="pillars-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Build, ship, operate. One coherent fabric.
        </h2>
      </div>
      <div className="mt-12 grid gap-5 md:grid-cols-3">
        {PILLARS.map((pillar, idx) => (
          <PillarCard key={pillar.eyebrow} pillar={pillar} index={idx} />
        ))}
      </div>
    </section>
  );
}

function PillarCard({
  pillar,
  index,
}: {
  readonly pillar: Pillar;
  readonly index: number;
}) {
  return (
    <motion.article
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{ duration: 0.2, delay: index * 0.05 }}
      className="border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg/60 group relative flex flex-col rounded-2xl border p-6 transition-colors"
    >
      <span
        aria-hidden
        className="bg-cc-accent absolute -top-1 left-6 h-1.5 w-1.5 rounded-full opacity-70 transition-opacity duration-200 group-hover:opacity-100"
        style={{ boxShadow: "0 0 12px rgba(94,234,212,0.7)" }}
      />
      <p className="text-cc-accent font-mono text-[0.7rem] tracking-[0.22em] uppercase">
        {pillar.eyebrow}
      </p>
      <h3 className="font-heading text-cc-heading mt-3 text-lg font-semibold">
        {pillar.title}
      </h3>
      <p className="text-cc-ink mt-4 text-sm leading-relaxed">{pillar.body}</p>
    </motion.article>
  );
}

/* -------------------------------------------------------------------------- */
/*  Product map (lattice)                                                      */
/* -------------------------------------------------------------------------- */

function ProductMap() {
  return (
    <DotGridSurface className="-mx-[max(0px,calc((100vw-72rem)/2))] px-[max(0px,calc((100vw-72rem)/2))]">
      <section aria-labelledby="map-heading" className="py-20 sm:py-24">
        <div className="text-center">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
            The platform map
          </p>
          <h2
            id="map-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
          >
            Six nodes on one lattice.
          </h2>
          <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
            Each product is a node on the same grid. Hover any node to light up
            the surrounding lattice. One query plan, one schema, one platform.
          </p>
        </div>
        <div className="mt-12 grid gap-5 md:grid-cols-3">
          {PLATFORM_NODES.map((node, idx) => (
            <PlatformNodeCard key={node.name} node={node} index={idx} />
          ))}
        </div>
      </section>
    </DotGridSurface>
  );
}

function PlatformNodeCard({
  node,
  index,
}: {
  readonly node: PlatformNode;
  readonly index: number;
}) {
  const Icon = node.icon;
  const linkClass =
    "text-cc-accent hover:text-cc-accent-hover mt-4 inline-flex items-center gap-1 text-sm font-medium";

  const inner = (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{ duration: 0.2, delay: index * 0.04 }}
      whileHover={{ scale: 1.02 }}
      className="border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg/80 group relative flex h-full flex-col rounded-2xl border p-6 backdrop-blur-sm transition-colors"
    >
      <span
        aria-hidden
        className="bg-cc-card-border group-hover:bg-cc-accent absolute -top-1 -left-1 h-2 w-2 rounded-full transition-all duration-200"
        style={{
          boxShadow: "0 0 0 rgba(94,234,212,0)",
        }}
      />
      <span
        aria-hidden
        className="bg-cc-card-border group-hover:bg-cc-accent absolute -top-1 -right-1 h-2 w-2 rounded-full transition-all duration-200"
      />
      <span
        aria-hidden
        className="bg-cc-card-border group-hover:bg-cc-accent absolute -bottom-1 -left-1 h-2 w-2 rounded-full transition-all duration-200"
      />
      <span
        aria-hidden
        className="bg-cc-card-border group-hover:bg-cc-accent absolute -right-1 -bottom-1 h-2 w-2 rounded-full transition-all duration-200"
      />
      <Icon className="h-10 w-10" />
      <h3 className="font-heading text-cc-heading mt-4 text-lg font-semibold">
        {node.name}
      </h3>
      <p className="text-cc-nav-label mt-1 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
        {node.tagline}
      </p>
      <p className="text-cc-ink mt-3 flex-1 text-sm leading-relaxed">
        {node.description}
      </p>
      <span className={linkClass}>
        Learn more{" "}
        <span
          aria-hidden
          className="transition-transform group-hover:translate-x-0.5"
        >
          {"->"}
        </span>
      </span>
    </motion.div>
  );

  if (node.external) {
    return (
      <a
        key={node.name}
        href={node.href}
        target="_blank"
        rel="noopener noreferrer"
        className="no-underline"
      >
        {inner}
      </a>
    );
  }

  return (
    <NextLink key={node.name} href={node.href} className="no-underline">
      {inner}
    </NextLink>
  );
}

/* -------------------------------------------------------------------------- */
/*  Capability stripes                                                         */
/* -------------------------------------------------------------------------- */

function CapabilityStripes() {
  return (
    <section aria-labelledby="stripes-heading" className="py-20 sm:py-24">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
          Capability deep-dive
        </p>
        <h2
          id="stripes-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          The platform, surface by surface.
        </h2>
      </div>
      <div className="mt-12 flex flex-col gap-6">
        {STRIPES.map((stripe, idx) => (
          <StripeRow key={stripe.eyebrow} stripe={stripe} index={idx} />
        ))}
      </div>
    </section>
  );
}

function StripeRow({
  stripe,
  index,
}: {
  readonly stripe: Stripe;
  readonly index: number;
}) {
  return (
    <motion.article
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.25 }}
      transition={{ duration: 0.2, delay: index * 0.05 }}
      className="border-cc-card-border bg-cc-card-bg/50 grid gap-8 rounded-2xl border p-6 sm:p-8 md:grid-cols-[1.1fr_1fr] md:items-center md:gap-10"
    >
      <div>
        <p className="text-cc-accent font-mono text-xs tracking-[0.22em] uppercase">
          {stripe.eyebrow}
        </p>
        <h3 className="font-heading text-cc-heading text-h5 sm:text-h4 mt-3 font-semibold">
          {stripe.title}
        </h3>
        <p className="text-cc-ink mt-4 text-base leading-relaxed">
          {stripe.body}
        </p>
      </div>
      <ul className="flex flex-col gap-3">
        {stripe.bullets.map((bullet) => (
          <li key={bullet} className="flex items-start gap-3">
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{bullet}</span>
          </li>
        ))}
      </ul>
    </motion.article>
  );
}

/* -------------------------------------------------------------------------- */
/*  Proof strip                                                                */
/* -------------------------------------------------------------------------- */

function ProofStrip() {
  return (
    <section className="py-16 sm:py-20">
      <div className="grid gap-5 md:grid-cols-3">
        {PROOF.map((item, idx) => (
          <ProofCard key={item.quote} item={item} index={idx} />
        ))}
      </div>
    </section>
  );
}

function ProofCard({
  item,
  index,
}: {
  readonly item: Proof;
  readonly index: number;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{ duration: 0.2, delay: index * 0.05 }}
      className="border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg/60 group flex flex-col rounded-2xl border p-6 transition-colors"
    >
      <p className="font-heading text-cc-heading text-base leading-snug font-medium">
        {item.quote}
      </p>
      <span
        aria-hidden
        className="bg-cc-accent mt-5 h-px w-8 origin-left scale-x-0 transition-transform duration-300 group-hover:scale-x-100"
      />
      <p className="text-cc-nav-label mt-3 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
        {item.label}
      </p>
    </motion.div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Blog strip                                                                 */
/* -------------------------------------------------------------------------- */

/* Blog strip is rendered by the server page.tsx and passed in via `blogSlot`,
   because FromOurBlog is a server-only module (it reads the filesystem) and
   cannot be pulled into this client component's bundle. */

/* -------------------------------------------------------------------------- */
/*  Final CTA                                                                  */
/* -------------------------------------------------------------------------- */

function FinalCta() {
  return (
    <>
      <DotGridSurface className="-mx-[max(0px,calc((100vw-72rem)/2))] px-[max(0px,calc((100vw-72rem)/2))]">
        <section className="py-20 text-center sm:py-24">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
            Ready when you are
          </p>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
            Ship your GraphQL platform on .NET.
          </h2>
          <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base">
            Start with Hot Chocolate, wire up Strawberry Shake, plug in Nitro
            for the registry and telemetry. Free to start, MIT to keep.
          </p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <SolidButton href="/get-started">Get started</SolidButton>
            <a
              href="https://github.com/ChilliCream/graphql-platform"
              target="_blank"
              rel="noopener noreferrer"
              className="border-cc-card-border hover:border-cc-card-border-hover bg-cc-bg/40 inline-flex items-center gap-3 rounded-full border px-5 py-3 transition-colors"
            >
              <GitHubIcon className="text-cc-heading h-5 w-5" />
              <span className="text-cc-heading text-sm font-medium">
                ChilliCream/graphql-platform
              </span>
            </a>
          </div>
        </section>
      </DotGridSurface>
      <div
        aria-hidden
        className="mt-10 h-px w-full"
        style={{ backgroundImage: SPECTRUM_GRADIENT }}
      />
    </>
  );
}
