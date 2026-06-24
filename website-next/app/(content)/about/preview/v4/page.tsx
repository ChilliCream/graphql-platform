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
    "About ChilliCream as a numbered story: the mission, the teams shipping on it, the six products, how we ship, the craft, the community, and how to engage.",
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
      "The story of ChilliCream as seven steps: mission, production users, the six products, how we ship, the craft, the community, and how to engage.",
  },
};

interface ProductRow {
  readonly index: string;
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

const PRODUCTS: readonly ProductRow[] = [
  {
    index: "3.1",
    name: "Hot Chocolate",
    tagline: "GraphQL server for .NET",
    description:
      "The schema-first and code-first GraphQL server at the heart of the platform. Built on ASP.NET Core, with first-class support for queries, mutations, subscriptions, and the modern GraphQL spec.",
    href: "/products/hotchocolate",
    linkLabel: "Hot Chocolate",
    icon: HotChocolate,
  },
  {
    index: "3.2",
    name: "Strawberry Shake",
    tagline: "Typed .NET client",
    description:
      "A typed GraphQL client for .NET with MSBuild codegen. Write a query, get a fully typed C# API, and ship apps that talk to any GraphQL endpoint without hand-written DTOs.",
    href: "/products/strawberryshake",
    linkLabel: "Strawberry Shake",
    icon: StrawberryShake,
  },
  {
    index: "3.3",
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
    index: "3.4",
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
    index: "3.5",
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
    index: "3.6",
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

export default function AboutPreviewV4Page() {
  // The continuous left rule and oversized accent numerals together form the
  // narrative spine that distinguishes this variation. Seven numbered steps,
  // each anchored on the spine, read as a single threaded story.
  return (
    <div className="mx-auto w-full max-w-5xl px-4 sm:px-6 lg:px-8">
      <div className="relative mx-auto max-w-3xl">
        <Spine />

        <StepShell numeral="01" eyebrow="STEP 01 / MISSION" variant="hero">
          <h1 className="font-heading text-cc-heading text-hero tracking-tight">
            The GraphQL platform for{" "}
            <span className="text-cc-accent">.NET teams</span>.
          </h1>
          <p className="lead text-cc-ink mt-8 max-w-2xl">
            We build an end-to-end GraphQL platform: the server, the typed
            client, the gateway, the control plane, and the tools to test and
            ship it. All open source, all designed together, all in the open on
            GitHub.
          </p>
          <div className="mt-10 flex flex-wrap items-center gap-3">
            <SolidButton href="https://github.com/ChilliCream/graphql-platform">
              Explore the platform on GitHub
            </SolidButton>
            <OutlineButton href="/services">Work with us</OutlineButton>
          </div>
        </StepShell>

        <Step02Production />
        <Step03Platform />
        <Step04OpenSource />
        <Step05DotNetNative />
        <Step06Community />

        <StepShell numeral="07" eyebrow="STEP 07 / ENGAGE" variant="last">
          <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
            Need GraphQL experts on your side?
          </h2>
          <p className="lead text-cc-ink mt-6 max-w-2xl">
            From hourly advisory and architecture reviews to enterprise support
            with SLAs, we engage at the level your team needs.
          </p>
          <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
            <SolidButton href="/services/support/contact">
              Contact us
            </SolidButton>
            <OutlineButton href="/services">Browse services</OutlineButton>
            <OutlineButton href="/pricing">See pricing</OutlineButton>
          </div>
        </StepShell>
      </div>
    </div>
  );
}

function Spine() {
  // A single 1px vertical hairline that runs the full height of the column,
  // tying every step together visually. Sits in the left gutter.
  return (
    <div
      aria-hidden="true"
      className="bg-cc-card-border pointer-events-none absolute top-0 bottom-0 left-4 w-px sm:left-10"
    />
  );
}

type StepVariant = "default" | "hero" | "last";

interface StepShellProps {
  readonly numeral: string;
  readonly eyebrow: string;
  readonly children: ReactNode;
  readonly variant?: StepVariant;
}

function StepShell({
  numeral,
  eyebrow,
  children,
  variant = "default",
}: StepShellProps) {
  // Step 07 ("last") recenters its content to mark the terminus of the spine;
  // every other step stays flush-left in the spine column. The hero step uses
  // a slightly heavier top padding so the first numeral sits comfortably below
  // the site header.
  const padding =
    variant === "hero" ? "pt-24 pb-20 sm:pt-32 sm:pb-28" : "py-20 sm:py-28";
  const gutter = variant === "last" ? "pl-14 sm:pl-28" : "pl-14 sm:pl-28";

  return (
    <section className={`relative ${padding} ${gutter}`}>
      <Numeral value={numeral} />
      <p className="text-cc-nav-label mb-5 font-mono text-xs tracking-[0.2em] uppercase">
        {eyebrow}
      </p>
      {children}
    </section>
  );
}

interface NumeralProps {
  readonly value: string;
}

function Numeral({ value }: NumeralProps) {
  // The numeral sits in the left gutter, overlapping the spine so the column
  // reads as a single threaded narrative. Uses the existing text-h1/text-hero
  // utilities, no ad-hoc sizes. Positioned so its cap height aligns roughly
  // with the eyebrow baseline across every step.
  return (
    <span
      aria-hidden="true"
      className="text-cc-accent text-h1 md:text-hero absolute top-20 left-0 font-mono leading-none tracking-tight opacity-85 select-none sm:top-24 sm:left-2"
    >
      {value}
    </span>
  );
}

function Step02Production() {
  return (
    <StepShell numeral="02" eyebrow="STEP 02 / IN PRODUCTION">
      <h2 className="font-heading text-cc-heading text-h3 tracking-tight">
        Teams ship on it today.
      </h2>
      <p className="lead text-cc-ink mt-6 max-w-2xl">
        Real teams ship the ChilliCream stack in production today, from
        independent platforms to large enterprises. A selection of public users
        is below.
      </p>
      <div className="mt-10">
        <LogoCloud />
      </div>
    </StepShell>
  );
}

function Step03Platform() {
  return (
    <StepShell numeral="03" eyebrow="STEP 03 / THE PLATFORM">
      <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
        Six products. One platform.
      </h2>
      <p className="lead text-cc-ink mt-6 max-w-2xl">
        Every piece is a real, shipping open-source project in the
        ChilliCream/graphql-platform repository. Pick one and adopt it on its
        own, or compose the full stack.
      </p>

      <ol className="border-cc-card-border divide-cc-card-border mt-12 divide-y border-y">
        {PRODUCTS.map((product) => (
          <li key={product.name}>
            <ProductSubRow {...product} />
          </li>
        ))}
      </ol>

      <p className="text-cc-ink-dim mt-10 max-w-2xl text-sm">
        Fusion, our distributed gateway, is built on Hot Chocolate and lives in
        the same repository.
      </p>
    </StepShell>
  );
}

function ProductSubRow({
  index,
  name,
  tagline,
  description,
  href,
  linkLabel,
  external,
  icon: Icon,
}: ProductRow) {
  const arrow = <span aria-hidden="true">&rarr;</span>;

  const linkClasses =
    "text-cc-accent mt-4 inline-flex items-center gap-1.5 text-sm font-medium hover:underline";

  return (
    <div className="flex gap-5 py-7 sm:gap-7 sm:py-9">
      <div className="hidden w-16 shrink-0 justify-start sm:flex">
        <span
          aria-hidden="true"
          className="text-cc-accent text-h6 font-mono leading-none tracking-tight"
        >
          {index}
        </span>
      </div>
      <div className="bg-cc-surface/60 border-cc-card-border flex h-12 w-12 shrink-0 items-center justify-center rounded-xl border sm:h-14 sm:w-14">
        <Icon className="h-10 w-10 object-contain" />
      </div>
      <div className="min-w-0 flex-1">
        <h3 className="font-heading text-cc-heading text-h5 leading-tight tracking-tight">
          <span
            aria-hidden="true"
            className="text-cc-accent mr-2 font-mono text-sm sm:hidden"
          >
            {index}
          </span>
          {name}
        </h3>
        <p className="text-cc-nav-label mt-1 font-mono text-[11px] tracking-[0.18em] uppercase">
          {tagline}
        </p>
        <p className="text-cc-ink mt-3 max-w-2xl text-sm leading-relaxed">
          {description}
        </p>
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
      </div>
    </div>
  );
}

function Step04OpenSource() {
  return (
    <StepShell numeral="04" eyebrow="STEP 04 / HOW WE SHIP">
      <h2 className="font-heading text-cc-heading text-h3 tracking-tight">
        Open source, public roadmap, public releases.
      </h2>
      <p className="lead text-cc-ink mt-6 max-w-2xl">
        The entire platform lives on GitHub. You can read the code, file an
        issue, send a pull request, and ship the same bits we ship. Public
        roadmaps, public releases, public discussions.
      </p>
      <p className="text-cc-ink-dim mt-5 max-w-2xl text-base">
        We describe what the platform does, not what it might do one day. If a
        capability is in preview, we say so. If a tradeoff exists, we name it.
        No marketing language standing in for engineering.
      </p>
    </StepShell>
  );
}

function Step05DotNetNative() {
  return (
    <StepShell numeral="05" eyebrow="STEP 05 / CRAFT">
      <h2 className="font-heading text-cc-heading text-h3 tracking-tight">
        Designed together for .NET.
      </h2>
      <p className="lead text-cc-ink mt-6 max-w-2xl">
        Server, client, mediator, DataLoader, and tests are designed together
        for .NET. OpenTelemetry-native observability, performance treated as a
        feature, and a developer experience that respects the language.
      </p>
    </StepShell>
  );
}

function Step06Community() {
  return (
    <StepShell numeral="06" eyebrow="STEP 06 / COMMUNITY">
      <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
        Built in public. Join in.
      </h2>
      <p className="lead text-cc-ink mt-6 max-w-2xl">
        Follow along, ask questions, and contribute. These are the real channels
        where the work happens.
      </p>

      <ul className="border-cc-card-border divide-cc-card-border mt-10 divide-y border-y">
        {COMMUNITY.map((item) => (
          <li key={item.label}>
            <CommunityRow {...item} />
          </li>
        ))}
      </ul>
    </StepShell>
  );
}

function CommunityRow({ label, href, description }: CommunityLink) {
  const inner: ReactNode = (
    <div className="hover:border-cc-card-border-hover group flex items-center justify-between gap-6 border border-transparent py-5 pr-2 pl-4 transition-colors">
      <div className="min-w-0">
        <div className="font-heading text-cc-heading text-h6 tracking-tight">
          {label}
        </div>
        <p className="text-cc-ink mt-1 text-sm leading-relaxed">
          {description}
        </p>
      </div>
      <span
        aria-hidden="true"
        className="text-cc-ink-dim group-hover:text-cc-accent text-base transition-colors"
      >
        &rarr;
      </span>
    </div>
  );

  if (href.startsWith("/")) {
    return (
      <NextLink href={href} className="block">
        {inner}
      </NextLink>
    );
  }

  return (
    <a href={href} target="_blank" rel="noopener noreferrer" className="block">
      {inner}
    </a>
  );
}
