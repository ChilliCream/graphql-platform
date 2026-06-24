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
    "About ChilliCream: we build the end-to-end GraphQL platform for .NET teams, from the Hot Chocolate server to the Nitro control plane, open source, in the open.",
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
      "We build the end-to-end GraphQL platform for .NET teams, from the Hot Chocolate server to the Nitro control plane, open source and in the open.",
  },
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

const PRODUCTS: readonly ProductCardProps[] = [
  {
    name: "Hot Chocolate",
    tagline: "GraphQL server for .NET",
    description:
      "The schema-first and code-first GraphQL server at the heart of the platform. Built on ASP.NET Core, with first-class support for queries, mutations, subscriptions, and the modern GraphQL spec.",
    href: "/products/hotchocolate",
    linkLabel: "Hot Chocolate",
    icon: HotChocolate,
  },
  {
    name: "Strawberry Shake",
    tagline: "Typed .NET client",
    description:
      "A typed GraphQL client for .NET with MSBuild codegen. Write a query, get a fully typed C# API, and ship apps that talk to any GraphQL endpoint without hand-written DTOs.",
    href: "/products/strawberryshake",
    linkLabel: "Strawberry Shake",
    icon: StrawberryShake,
  },
  {
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
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
}

const PRINCIPLES: readonly Principle[] = [
  {
    eyebrow: "01",
    title: "Open source first",
    body: "The entire platform lives on GitHub. You can read the code, file an issue, send a pull request, and ship the same bits we ship. Public roadmaps, public releases, public discussions.",
  },
  {
    eyebrow: "02",
    title: "Honest about what we ship",
    body: "We describe what the platform does, not what it might do one day. If a capability is in preview, we say so. If a tradeoff exists, we name it. No marketing language standing in for engineering.",
  },
  {
    eyebrow: "03",
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

export default function AboutPreviewV1Page() {
  return (
    <>
      <MissionHero />
      <LogoCloud />
      <PlatformSection />
      <PrinciplesSection />
      <CommunitySection />
      <EngageBand />
    </>
  );
}

function MissionHero() {
  return (
    <section className="py-16 text-center sm:py-24">
      <p className="text-cc-nav-label mb-4 font-mono text-xs tracking-[0.2em] uppercase">
        About ChilliCream
      </p>
      <h1 className="font-heading text-cc-heading text-hero mx-auto max-w-4xl tracking-tight">
        The GraphQL platform for{" "}
        <span className="text-cc-accent">.NET teams</span>.
      </h1>
      <p className="lead text-cc-ink mx-auto mt-8 max-w-2xl">
        We build an end-to-end GraphQL platform: the server, the typed client,
        the gateway, the control plane, and the tools to test and ship it. All
        open source, all designed together, all in the open on GitHub.
      </p>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="https://github.com/ChilliCream/graphql-platform">
          Explore the platform on GitHub
        </SolidButton>
        <OutlineButton href="/services">Work with us</OutlineButton>
      </div>
    </section>
  );
}

function PlatformSection() {
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          The platform we build
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          Six products. One platform.
        </h2>
        <p className="text-cc-ink mt-5 text-base sm:text-lg">
          Every piece is a real, shipping open-source project in the
          ChilliCream/graphql-platform repository. Pick one and adopt it on its
          own, or compose the full stack.
        </p>
      </header>

      <div className="mt-12 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {PRODUCTS.map((product) => (
          <ProductCard key={product.name} {...product} />
        ))}
      </div>

      <p className="text-cc-ink-dim mt-10 text-center text-sm">
        Fusion, our distributed gateway, is built on Hot Chocolate and lives in
        the same repository.
      </p>
    </section>
  );
}

function ProductCard({
  name,
  tagline,
  description,
  href,
  linkLabel,
  external,
  icon: Icon,
}: ProductCardProps) {
  const arrow = <span aria-hidden="true">&rarr;</span>;

  const linkClasses =
    "text-cc-accent mt-5 inline-flex items-center gap-1.5 text-sm font-medium hover:underline";

  return (
    <article className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group relative flex flex-col rounded-2xl border p-6 transition-colors">
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
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          How we build, every release.
        </h2>
      </header>

      <ol className="mt-12 grid gap-5 md:grid-cols-3">
        {PRINCIPLES.map((p) => (
          <li
            key={p.title}
            className="bg-cc-card-bg border-cc-card-border rounded-2xl border p-7"
          >
            <div className="text-cc-accent font-mono text-xs tracking-[0.2em]">
              {p.eyebrow}
            </div>
            <h3 className="font-heading text-cc-heading text-h5 mt-4 tracking-tight">
              {p.title}
            </h3>
            <p className="text-cc-ink mt-3 text-sm leading-relaxed">{p.body}</p>
          </li>
        ))}
      </ol>
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
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          The platform is built in public.
        </h2>
        <p className="text-cc-ink mt-5 text-base sm:text-lg">
          Follow along, ask questions, and contribute. These are the real
          channels where the work happens.
        </p>
      </header>

      <ul className="mt-12 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
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
      <div className="bg-cc-card-bg border-cc-card-border rounded-3xl border p-10 text-center sm:p-14">
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
