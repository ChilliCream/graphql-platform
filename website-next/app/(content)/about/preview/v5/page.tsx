import type { Metadata } from "next";
import NextLink from "next/link";
import type { ComponentType, CSSProperties, ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { CookieCrumble } from "@/src/icons/CookieCrumble";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";

export const metadata: Metadata = {
  title: "About ChilliCream",
  description:
    "About ChilliCream: the end-to-end GraphQL platform for .NET teams, from Hot Chocolate and Strawberry Shake to the Nitro control plane, open source on GitHub.",
  keywords: [
    "ChilliCream",
    "About ChilliCream",
    "GraphQL platform",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    ".NET GraphQL",
    "Fusion",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "About ChilliCream",
    description:
      "About ChilliCream: the end-to-end GraphQL platform for .NET teams, from Hot Chocolate and Strawberry Shake to the Nitro control plane, open source on GitHub.",
  },
};

interface ProductEntry {
  readonly badge: string;
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

const PRODUCTS: readonly ProductEntry[] = [
  {
    badge: "01",
    name: "Hot Chocolate",
    tagline: "GraphQL server for .NET",
    description:
      "The schema-first and code-first GraphQL server at the heart of the platform. Built on ASP.NET Core, with first-class support for queries, mutations, subscriptions, and the modern GraphQL spec.",
    href: "/products/hotchocolate",
    linkLabel: "Hot Chocolate",
    icon: HotChocolate,
  },
  {
    badge: "02",
    name: "Strawberry Shake",
    tagline: "Typed .NET client",
    description:
      "A typed GraphQL client for .NET with MSBuild codegen. Write a query, get a fully typed C# API, and ship apps that talk to any GraphQL endpoint without hand-written DTOs.",
    href: "/products/strawberryshake",
    linkLabel: "Strawberry Shake",
    icon: StrawberryShake,
  },
  {
    badge: "03",
    name: "Nitro",
    tagline: "Control plane and IDE",
    description:
      "The control plane for your GraphQL APIs: schema and client registry, CI checks, observability, and the GraphQL IDE your team already uses to explore the graph.",
    href: "https://nitro.chillicream.com",
    linkLabel: "Nitro",
    external: true,
    icon: Nitro,
  },
  {
    badge: "04",
    name: "Mocha",
    tagline: "Mediator and messaging",
    description:
      "A source-generated mediator and cross-service messaging library. No reflection on the hot path, predictable performance, and the same programming model in-process and across services.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Mocha",
    external: true,
    icon: Mocha,
  },
  {
    badge: "05",
    name: "Green Donut",
    tagline: "DataLoader for .NET",
    description:
      "The DataLoader implementation behind Hot Chocolate. Batches and caches your data access so resolvers stay simple and you avoid the N+1 problem by default.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Green Donut",
    external: true,
    icon: GreenDonut,
  },
  {
    badge: "06",
    name: "Cookie Crumble",
    tagline: "GraphQL-aware snapshot testing",
    description:
      "Snapshot testing built for GraphQL. Native support for execution results and HTTP responses, with Markdown snapshots for tests that capture multiple shapes of state.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Cookie Crumble",
    external: true,
    icon: CookieCrumble,
  },
];

interface Principle {
  readonly title: string;
  readonly body: string;
}

const PRINCIPLES: readonly Principle[] = [
  {
    title: "Open source first",
    body: "The entire platform lives on GitHub. You can read the code, file an issue, send a pull request, and ship the same bits we ship. Public roadmaps, public releases, public discussions.",
  },
  {
    title: "Honest about what we ship",
    body: "We describe what the platform does, not what it might do one day. If a capability is in preview, we say so. If a tradeoff exists, we name it. No marketing language standing in for engineering.",
  },
  {
    title: ".NET-native, end to end",
    body: "Server, client, mediator, DataLoader, and tests are designed together for .NET. OpenTelemetry-native observability, performance treated as a feature, and a developer experience that respects the language.",
  },
];

interface CommunityLink {
  readonly label: string;
  readonly href: string;
  readonly description: string;
}

const COMMUNITY: readonly CommunityLink[] = [
  {
    label: "Slack",
    href: "https://slack.chillicream.com/",
    description:
      "Ask questions, trade patterns, talk to the people who build the platform.",
  },
  {
    label: "GitHub",
    href: "https://github.com/ChilliCream/graphql-platform",
    description:
      "Read the code, file issues, send pull requests. The platform is built in the open.",
  },
  {
    label: "YouTube",
    href: "https://www.youtube.com/c/ChilliCream",
    description:
      "Talks, deep dives, and walkthroughs from the team and the wider community.",
  },
  {
    label: "Blog",
    href: "/blog",
    description:
      "Release notes, design decisions, and engineering writeups straight from the team.",
  },
  {
    label: "Nitro",
    href: "https://nitro.chillicream.com",
    description:
      "Sign in to the control plane: schema registry, CI checks, observability, IDE.",
  },
  {
    label: "X",
    href: "https://x.com/Chilli_Cream",
    description:
      "Short updates, releases, and conversation with the broader GraphQL community.",
  },
  {
    label: "LinkedIn",
    href: "https://www.linkedin.com/company/chillicream",
    description:
      "Follow the company for product news, talks, and hiring updates.",
  },
];

export default function AboutPreviewV5Page() {
  return (
    <>
      <ConstellationHero />
      <LogoBand />
      <PlatformSection />
      <PrinciplesSection />
      <CommunitySection />
      <EngageBand />
    </>
  );
}

function ConstellationHero() {
  return (
    <section className="py-16 sm:py-24">
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-10">
        <div className="order-2 lg:order-1 lg:col-span-5">
          <p className="text-cc-nav-label mb-4 font-mono text-xs tracking-[0.2em] uppercase">
            About ChilliCream
          </p>
          <h1 className="font-heading text-cc-heading text-hero tracking-tight">
            The GraphQL platform for{" "}
            <span className="text-cc-accent">.NET teams</span>.
          </h1>
          <p className="lead text-cc-ink mt-8 max-w-xl">
            Six open source products, designed together, that take a .NET team
            from a single GraphQL endpoint to a registered, observed,
            distributed graph. Built in the open on GitHub.
          </p>
          <div className="mt-10 flex flex-wrap items-center gap-3">
            <SolidButton href="https://github.com/ChilliCream/graphql-platform">
              Explore the platform on GitHub
            </SolidButton>
            <OutlineButton href="/services">Work with us</OutlineButton>
          </div>
        </div>

        <div className="order-1 lg:order-2 lg:col-span-7">
          <div className="mx-auto w-full max-w-[640px]">
            <ConstellationDiagram />
          </div>
        </div>
      </div>
    </section>
  );
}

interface ConstellationNode {
  readonly badge: string;
  readonly label: string;
  readonly cx: number;
  readonly cy: number;
}

const NODES: readonly ConstellationNode[] = [
  { badge: "01", label: "Hot Chocolate", cx: 250, cy: 60 },
  { badge: "02", label: "Strawberry Shake", cx: 450, cy: 140 },
  { badge: "03", label: "Nitro", cx: 470, cy: 360 },
  { badge: "04", label: "Mocha", cx: 250, cy: 440 },
  { badge: "05", label: "Green Donut", cx: 50, cy: 360 },
  { badge: "06", label: "Cookie Crumble", cx: 30, cy: 140 },
];

function ConstellationDiagram() {
  const hub = { cx: 250, cy: 250 };
  const hubRadius = 64;
  const nodeRadius = 38;
  const accent = "#5eead4";
  const heading = "#f1f5f9";
  const ink = "#cbd5e1";
  const surface = "#0c1322";
  const border = "rgba(94,234,212,0.35)";

  return (
    <svg
      viewBox="0 0 500 500"
      role="img"
      aria-label="ChilliCream platform constellation: six products around a central hub"
      className="block aspect-square w-full"
    >
      <defs>
        <radialGradient id="v5-hub-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor={accent} stopOpacity="0.35" />
          <stop offset="60%" stopColor={accent} stopOpacity="0.08" />
          <stop offset="100%" stopColor={accent} stopOpacity="0" />
        </radialGradient>
        <radialGradient id="v5-node-fill" cx="50%" cy="40%" r="60%">
          <stop offset="0%" stopColor="#101a2c" />
          <stop offset="100%" stopColor={surface} />
        </radialGradient>
      </defs>

      {/* faint guide ring */}
      <circle
        cx={hub.cx}
        cy={hub.cy}
        r={185}
        fill="none"
        stroke={border}
        strokeOpacity="0.18"
        strokeDasharray="2 6"
      />

      {/* connection lines hub to nodes */}
      {NODES.map((n) => (
        <line
          key={`line-${n.badge}`}
          x1={hub.cx}
          y1={hub.cy}
          x2={n.cx}
          y2={n.cy}
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
        />
      ))}

      {/* hub glow */}
      <circle cx={hub.cx} cy={hub.cy} r={140} fill="url(#v5-hub-glow)" />

      {/* hub */}
      <circle
        cx={hub.cx}
        cy={hub.cy}
        r={hubRadius}
        fill={surface}
        stroke={accent}
        strokeWidth="1.25"
      />
      <text
        x={hub.cx}
        y={hub.cy - 6}
        textAnchor="middle"
        fill={heading}
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="10"
        letterSpacing="2"
      >
        CHILLICREAM
      </text>
      <text
        x={hub.cx}
        y={hub.cy + 10}
        textAnchor="middle"
        fill={ink}
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize="9"
        letterSpacing="2"
      >
        PLATFORM
      </text>

      {/* nodes */}
      {NODES.map((n) => {
        // place numeric badge just outside the circle, leaning toward hub
        const dx = n.cx - hub.cx;
        const dy = n.cy - hub.cy;
        const len = Math.sqrt(dx * dx + dy * dy);
        const ux = dx / len;
        const uy = dy / len;
        const badgeX = n.cx - ux * (nodeRadius + 14);
        const badgeY = n.cy - uy * (nodeRadius + 14);

        // label below node
        const labelY = n.cy + nodeRadius + 18;

        return (
          <g key={`node-${n.badge}`}>
            <circle
              cx={n.cx}
              cy={n.cy}
              r={nodeRadius}
              fill="url(#v5-node-fill)"
              stroke={border}
              strokeWidth="1"
            />
            <circle cx={n.cx} cy={n.cy} r={5} fill={accent} fillOpacity="0.9" />
            <text
              x={badgeX}
              y={badgeY + 3}
              textAnchor="middle"
              fill={accent}
              fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
              fontSize="10"
              letterSpacing="2"
            >
              {n.badge}
            </text>
            <text
              x={n.cx}
              y={labelY}
              textAnchor="middle"
              fill={heading}
              fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
              fontSize="10"
              letterSpacing="1.5"
            >
              {n.label.toUpperCase()}
            </text>
          </g>
        );
      })}
    </svg>
  );
}

function LogoBand() {
  return (
    <section className="border-cc-card-border border-t py-16 sm:py-20">
      <p className="text-cc-nav-label mb-10 text-center font-mono text-xs tracking-[0.2em] uppercase">
        Teams shipping on ChilliCream
      </p>
      <LogoCloud />
    </section>
  );
}

function PlatformSection() {
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          The constellation
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          Six products. One platform.
        </h2>
        <p className="text-cc-ink mt-5 text-base sm:text-lg">
          Each numbered node in the diagram is a real, shipping open source
          project in the ChilliCream/graphql-platform repository. Pick one and
          adopt it on its own, or compose the full stack.
        </p>
      </header>

      <div className="mx-auto mt-12 grid max-w-6xl gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {PRODUCTS.map((product) => (
          <ProductCard key={product.name} {...product} />
        ))}
      </div>

      <p className="text-cc-ink-dim mx-auto mt-10 max-w-3xl text-center text-sm">
        Fusion, our distributed gateway, is built on Hot Chocolate and lives in
        the same repository. Composition runs at planning time, the gateway is
        always self-run.
      </p>
    </section>
  );
}

function ProductCard({
  badge,
  name,
  tagline,
  description,
  href,
  linkLabel,
  external,
  icon: Icon,
}: ProductEntry) {
  const arrow = <span aria-hidden="true">&rarr;</span>;

  const linkClasses =
    "text-cc-accent mt-5 inline-flex items-center gap-1.5 text-sm font-medium hover:underline";

  return (
    <article className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group relative flex flex-col rounded-2xl border p-6 transition-colors">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-4">
          <div className="bg-cc-surface/60 border-cc-card-border flex h-16 w-16 shrink-0 items-center justify-center rounded-xl border">
            <Icon className="h-12 w-12 object-contain" />
          </div>
          <div className="min-w-0">
            <h3 className="font-heading text-cc-heading text-h5 leading-tight tracking-tight">
              {name}
            </h3>
            <p className="text-cc-nav-label mt-1 font-mono text-[11px] tracking-[0.18em] uppercase">
              {tagline}
            </p>
          </div>
        </div>
        <span
          aria-hidden="true"
          className="text-cc-accent shrink-0 font-mono text-xs tracking-[0.2em]"
        >
          {badge}
        </span>
      </div>

      <p className="text-cc-ink mt-5 text-sm leading-relaxed">{description}</p>

      {external ? (
        <a
          href={href}
          target="_blank"
          rel="noopener noreferrer"
          className={linkClasses}
        >
          Visit {linkLabel} {arrow}
        </a>
      ) : (
        <NextLink href={href} className={linkClasses}>
          Explore {linkLabel} {arrow}
        </NextLink>
      )}
    </article>
  );
}

function PrinciplesSection() {
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          Principles
        </p>
        <h2 className="font-heading text-cc-heading text-h3 tracking-tight">
          How we build, every release.
        </h2>
      </header>

      <ul className="mx-auto mt-12 grid max-w-6xl gap-5 md:grid-cols-3">
        {PRINCIPLES.map((p) => (
          <li
            key={p.title}
            className="bg-cc-card-bg border-cc-card-border rounded-2xl border p-7"
          >
            <h3 className="font-heading text-cc-heading text-h5 tracking-tight">
              {p.title}
            </h3>
            <p className="text-cc-ink mt-3 text-sm leading-relaxed">{p.body}</p>
          </li>
        ))}
      </ul>
    </section>
  );
}

function CommunitySection() {
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          Join the community
        </p>
        <h2 className="font-heading text-cc-heading text-h3 tracking-tight">
          Built in public.
        </h2>
        <p className="text-cc-ink mt-5 text-base sm:text-lg">
          Follow along, ask questions, and contribute. These are the real
          channels where the work happens.
        </p>
      </header>

      <ul className="mx-auto mt-12 grid max-w-6xl gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {COMMUNITY.map((item) => (
          <li key={item.label}>
            <CommunityCard {...item} />
          </li>
        ))}
      </ul>
    </section>
  );
}

function CommunityCard({ label, href, description }: CommunityLink) {
  const inner: ReactNode = (
    <>
      <div className="flex items-center justify-between">
        <span className="font-heading text-cc-heading text-h6 tracking-tight">
          {label}
        </span>
        <span
          aria-hidden="true"
          className="text-cc-ink-dim group-hover:text-cc-accent text-base transition-colors"
        >
          &rarr;
        </span>
      </div>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{description}</p>
    </>
  );

  const classes =
    "bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group block h-full rounded-2xl border p-5 transition-colors";

  if (href.startsWith("/")) {
    return (
      <NextLink href={href} className={classes}>
        {inner}
      </NextLink>
    );
  }

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={classes}
    >
      {inner}
    </a>
  );
}

function EngageBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="bg-cc-card-bg border-cc-card-border mx-auto max-w-6xl rounded-3xl border p-10 text-center sm:p-14">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          Work with ChilliCream
        </p>
        <h2 className="font-heading text-cc-heading text-h2 mx-auto max-w-3xl tracking-tight">
          Need GraphQL experts on your side?
        </h2>
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base sm:text-lg">
          From hourly advisory and architecture reviews to enterprise support
          with SLAs, we engage at the level your team needs.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/services/support/contact">Contact us</SolidButton>
          <OutlineButton href="/services">Browse services</OutlineButton>
          <OutlineButton href="/pricing">See pricing</OutlineButton>
        </div>
      </div>
    </section>
  );
}
