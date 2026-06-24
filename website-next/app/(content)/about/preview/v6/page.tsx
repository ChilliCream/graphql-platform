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
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "About ChilliCream",
  description:
    "About ChilliCream: one menu, six pours, served open source. The end-to-end GraphQL platform for .NET teams, from Hot Chocolate to the Nitro control plane.",
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
      "About ChilliCream: one menu, six pours, served open source. The end-to-end GraphQL platform for .NET teams.",
  },
};

interface ProductCardProps {
  readonly number: string;
  readonly name: string;
  readonly brewStyle: string;
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
    number: "01",
    name: "Hot Chocolate",
    brewStyle: "Single origin server",
    tagline: "GraphQL server for .NET",
    description:
      "The schema-first and code-first GraphQL server at the heart of the platform. Built on ASP.NET Core, with first-class support for queries, mutations, subscriptions, and the modern GraphQL spec.",
    href: "/products/hotchocolate",
    linkLabel: "Hot Chocolate",
    icon: HotChocolate,
  },
  {
    number: "02",
    name: "Strawberry Shake",
    brewStyle: "Typed to order",
    tagline: "Typed .NET client",
    description:
      "A typed GraphQL client for .NET with MSBuild codegen. Write a query, get a fully typed C# API, and ship apps that talk to any GraphQL endpoint without hand-written DTOs.",
    href: "/products/strawberryshake",
    linkLabel: "Strawberry Shake",
    icon: StrawberryShake,
  },
  {
    number: "03",
    name: "Nitro",
    brewStyle: "The control bar",
    tagline: "Control plane and IDE",
    description:
      "The control plane for your GraphQL APIs: schema and client registry, CI checks, observability, and the GraphQL IDE your team already uses to explore the graph.",
    href: "https://nitro.chillicream.com",
    linkLabel: "Nitro",
    external: true,
    icon: Nitro,
  },
  {
    number: "04",
    name: "Mocha",
    brewStyle: "Pulled mediator shot",
    tagline: "Mediator and messaging",
    description:
      "A source-generated mediator and cross-service messaging library. No reflection on the hot path, predictable performance, and the same programming model in-process and across services.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Mocha",
    external: true,
    icon: Mocha,
  },
  {
    number: "05",
    name: "Green Donut",
    brewStyle: "Batched and cached",
    tagline: "DataLoader for .NET",
    description:
      "The DataLoader implementation behind Hot Chocolate. Batches and caches your data access so resolvers stay simple and you avoid the N+1 problem by default.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Green Donut",
    external: true,
    icon: GreenDonut,
  },
  {
    number: "06",
    name: "Cookie Crumble",
    brewStyle: "Quality control on the bar",
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
  readonly icon: ComponentType<{
    readonly className?: string;
    readonly style?: CSSProperties;
  }>;
}

const PRINCIPLES: readonly Principle[] = [
  {
    eyebrow: "01",
    title: "Open source first",
    body: "The entire platform lives on GitHub. You can read the code, file an issue, send a pull request, and ship the same bits we ship. Public roadmaps, public releases, public discussions.",
    icon: DripBrewer,
  },
  {
    eyebrow: "02",
    title: "Honest about what we ship",
    body: "We describe what the platform does, not what it might do one day. If a capability is in preview, we say so. If a tradeoff exists, we name it. No marketing language standing in for engineering.",
    icon: PourOver,
  },
  {
    eyebrow: "03",
    title: ".NET-native, end to end",
    body: "Server, client, mediator, DataLoader, and tests are designed together for .NET. OpenTelemetry-native observability, performance treated as a feature, and a developer experience that respects the language.",
    icon: Espresso,
  },
];

interface BrewWay {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly icon: ComponentType<{
    readonly className?: string;
    readonly style?: CSSProperties;
  }>;
}

const BREW_WAYS: readonly BrewWay[] = [
  {
    eyebrow: "Pick one pour",
    title: "Adopt a single product",
    body: "Drop Hot Chocolate into an existing service, or wire Strawberry Shake into one client. Every product stands on its own.",
    icon: CoffeeTray,
  },
  {
    eyebrow: "Compose the menu",
    title: "Run the full stack",
    body: "Pair the server, the typed client, the gateway, and the control plane. Designed together, shipped together, observable together.",
    icon: FrenchPress,
  },
  {
    eyebrow: "Roast your own",
    title: "Contribute on GitHub",
    body: "Read the code, file an issue, send a pull request. The same repository we ship from is the one you can open today.",
    icon: DripBrewer,
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

export default function AboutPreviewV6Page() {
  return (
    <>
      <MenuHero />
      <TrustedAtBar />
      <OnTheMenu />
      <BehindTheBar />
      <BrewYourWay />
      <OpenCounter />
      <LastCallCTA />
    </>
  );
}

interface DecorativeSvgProps {
  readonly className?: string;
}

function SteamLine({ className }: DecorativeSvgProps) {
  return (
    <svg
      viewBox="0 0 60 80"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.4}
      strokeLinecap="round"
      aria-hidden="true"
      className={className}
    >
      <path
        d="M 18 70 C 10 56, 26 48, 18 34 C 10 20, 26 12, 20 2"
        opacity={0.85}
      />
      <path
        d="M 32 70 C 24 56, 40 48, 32 34 C 24 20, 40 12, 34 2"
        opacity={0.55}
      />
      <path
        d="M 46 70 C 38 56, 54 48, 46 34 C 38 20, 54 12, 48 2"
        opacity={0.3}
      />
    </svg>
  );
}

function EspressoCupGlyph({ className }: DecorativeSvgProps) {
  return (
    <svg
      viewBox="0 0 80 60"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M 12 18 L 56 18 L 52 46 Q 50 52, 44 52 L 24 52 Q 18 52, 16 46 Z" />
      <path d="M 56 24 Q 70 24, 70 34 Q 70 44, 56 44" />
      <line x1="14" y1="58" x2="62" y2="58" opacity={0.5} />
      <line x1="22" y1="26" x2="46" y2="26" opacity={0.4} />
    </svg>
  );
}

function MenuHero() {
  return (
    <section className="py-16 sm:py-24">
      <div className="bg-cc-card-bg border-cc-card-border relative mx-auto max-w-5xl overflow-hidden rounded-3xl border px-6 py-14 sm:px-12 sm:py-20">
        <div
          aria-hidden="true"
          className="border-cc-card-border pointer-events-none absolute inset-3 rounded-2xl border opacity-40"
        />
        <div className="relative flex flex-col items-center text-center">
          <div className="text-cc-accent mb-6 flex items-end justify-center gap-3">
            <EspressoCupGlyph className="h-10 w-14" />
            <SteamLine className="h-12 w-8 -translate-y-2" />
          </div>
          <p className="text-cc-nav-label mb-5 font-mono text-xs tracking-[0.22em] uppercase">
            Today&rsquo;s pour / House blend since day one
          </p>
          <h1 className="font-heading text-cc-heading text-hero mx-auto max-w-4xl tracking-tight">
            About <span className="text-cc-accent">ChilliCream</span>.
          </h1>
          <p className="lead text-cc-ink mx-auto mt-7 max-w-2xl">
            One menu, six pours, served open source. We build the end-to-end
            GraphQL platform for .NET teams: the server, the typed client, the
            gateway, the control plane, and the tools to test and ship it.
          </p>
          <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
            <SolidButton href="https://github.com/ChilliCream/graphql-platform">
              Explore the platform on GitHub
            </SolidButton>
            <OutlineButton href="/services">Work with us</OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

function TrustedAtBar() {
  return (
    <section className="py-12 sm:py-16">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.22em] uppercase">
          Served at
        </p>
        <p className="text-cc-ink-dim text-sm">
          Teams who drink the house blend daily.
        </p>
      </header>
      <div className="mt-8">
        <LogoCloud />
      </div>
    </section>
  );
}

function OnTheMenu() {
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.22em] uppercase">
          On the menu
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          Six pours. One bar.
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
  number,
  name,
  brewStyle,
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
      <div className="border-cc-card-border mb-5 flex items-center justify-between border-b pb-4">
        <span className="text-cc-accent font-mono text-xs tracking-[0.22em]">
          {number}
        </span>
        <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.22em] uppercase">
          {brewStyle}
        </span>
      </div>

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

function BehindTheBar() {
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.22em] uppercase">
          How every cup is pulled
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          Behind the bar.
        </h2>
      </header>

      <ol className="mt-12 grid gap-5 md:grid-cols-3">
        {PRINCIPLES.map(({ eyebrow, title, body, icon: Icon }) => (
          <li
            key={title}
            className="bg-cc-card-bg border-cc-card-border rounded-2xl border p-7"
          >
            <Icon className="text-cc-accent mb-4 h-5 w-5" />
            <div className="text-cc-accent font-mono text-xs tracking-[0.2em]">
              {eyebrow}
            </div>
            <h3 className="font-heading text-cc-heading text-h5 mt-4 tracking-tight">
              {title}
            </h3>
            <p className="text-cc-ink mt-3 text-sm leading-relaxed">{body}</p>
          </li>
        ))}
      </ol>
    </section>
  );
}

function BrewYourWay() {
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.22em] uppercase">
          Brew your way
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          Three ways to adopt the platform.
        </h2>
      </header>

      <div className="mt-12 grid gap-5 md:grid-cols-3">
        {BREW_WAYS.map(({ eyebrow, title, body, icon: Icon }) => (
          <article
            key={title}
            className="bg-cc-card-bg border-cc-card-border flex flex-col rounded-2xl border p-7"
          >
            <Icon className="text-cc-accent mb-5 h-10 w-10" />
            <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.22em] uppercase">
              {eyebrow}
            </p>
            <h3 className="font-heading text-cc-heading text-h5 mt-3 tracking-tight">
              {title}
            </h3>
            <p className="text-cc-ink mt-3 text-sm leading-relaxed">{body}</p>
          </article>
        ))}
      </div>
    </section>
  );
}

function OpenCounter() {
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.22em] uppercase">
          Where the conversation happens
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          At the open counter.
        </h2>
        <p className="text-cc-ink mt-5 text-base sm:text-lg">
          Pull up a stool. The platform is built in public.
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

function LastCallCTA() {
  return (
    <section className="py-16 sm:py-20">
      <div className="bg-cc-card-bg border-cc-card-border rounded-3xl border p-10 text-center sm:p-14">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.22em] uppercase">
          Last call
        </p>
        <h2 className="font-heading text-cc-heading text-h2 mx-auto max-w-3xl tracking-tight">
          Need a barista on your side?
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
