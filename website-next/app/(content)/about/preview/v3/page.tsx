import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "About ChilliCream | The GraphQL Platform for .NET",
  description:
    "About ChilliCream, the team behind the end-to-end open-source GraphQL platform for .NET teams: Hot Chocolate, Fusion, Nitro, Mocha, and Strawberry Shake.",
  keywords: [
    "About ChilliCream",
    "ChilliCream",
    "GraphQL platform",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    "Fusion",
    ".NET GraphQL",
  ],
  openGraph: {
    title: "About ChilliCream | The GraphQL Platform for .NET",
    description:
      "About ChilliCream, the team behind the end-to-end open-source GraphQL platform for .NET teams: Hot Chocolate, Fusion, Nitro, Mocha, and Strawberry Shake.",
  },
  robots: {
    index: false,
    follow: false,
  },
};

// ---------- Outcome cards: what teams ship with the platform ----------

interface OutcomeCardProps {
  readonly index: string;
  readonly tagline: string;
  readonly title: string;
  readonly description: string;
  readonly points: ReadonlyArray<string>;
}

function OutcomeCard({
  index,
  tagline,
  title,
  description,
  points,
}: OutcomeCardProps) {
  return (
    <article className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative flex h-full flex-col gap-5 rounded-2xl border p-7 transition-colors">
      <header className="flex items-center justify-between">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          {index}
        </span>
        <span className="text-cc-accent font-mono text-xs tracking-[0.2em] uppercase">
          {tagline}
        </span>
      </header>
      <h3 className="text-cc-heading font-heading text-2xl leading-tight font-semibold tracking-tight">
        {title}
      </h3>
      <p className="text-cc-prose text-base leading-relaxed">{description}</p>
      <ul className="text-cc-ink mt-auto flex flex-col gap-2 text-sm">
        {points.map((point) => (
          <li key={point} className="flex items-start gap-3">
            <span className="text-cc-accent mt-1 inline-flex">
              <CheckIcon />
            </span>
            <span>{point}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

const OUTCOMES: ReadonlyArray<OutcomeCardProps> = [
  {
    index: "01",
    tagline: "High-traffic commerce",
    title: "A storefront graph that holds up at peak",
    description:
      "Federate catalog, inventory, pricing, and checkout into a single typed graph that ships fast queries to mobile and web without falling over on launch days.",
    points: [
      "One graph, many backends",
      "Predictable latency under load",
      "Schema evolution without breakage",
    ],
  },
  {
    index: "02",
    tagline: "Regulated enterprise",
    title: "A compliance-grade graph teams can trust",
    description:
      "Run a governed graph with persistent operations, schema and client registries, and CI checks that block breaking changes before they ever reach production.",
    points: [
      "Schema and client registry",
      "Breaking-change checks in CI",
      "End-to-end OpenTelemetry traces",
    ],
  },
  {
    index: "03",
    tagline: ".NET-native",
    title: "A first-class graph for .NET teams",
    description:
      "Build server and client in the same language and toolchain. Typed all the way down: source-generated clients, source-generated handlers, idiomatic C#.",
    points: [
      "Source generators end to end",
      "Strong typing across the wire",
      "Familiar .NET tooling and DI",
    ],
  },
];

// ---------- Products catalogue: the platform behind the outcomes ----------

interface ProductRowProps {
  readonly name: string;
  readonly tagline: string;
  readonly description: string;
}

const PRODUCTS: ReadonlyArray<ProductRowProps> = [
  {
    name: "Hot Chocolate",
    tagline: "GraphQL server for .NET",
    description:
      "The core GraphQL server. Code-first or schema-first, fast execution, batteries-included for production .NET workloads.",
  },
  {
    name: "Fusion",
    tagline: "Distributed gateway",
    description:
      "The distributed gateway built on Hot Chocolate. Compose multiple subgraphs into one graph with a single typed query plan.",
  },
  {
    name: "Nitro",
    tagline: "Control plane and IDE",
    description:
      "Schema and client registry, CI checks, observability, and the GraphQL IDE in one place. The operational center of the platform.",
  },
  {
    name: "Strawberry Shake",
    tagline: "Typed .NET client",
    description:
      "A typed GraphQL client for .NET with MSBuild code generation. Write a query, get strongly typed C# types and a client to call it.",
  },
  {
    name: "Mocha",
    tagline: "Mediator and messaging",
    description:
      "Source-generated mediator and cross-service messaging. The in-process and cross-service backbone for .NET applications.",
  },
  {
    name: "Green Donut",
    tagline: "DataLoader for .NET",
    description:
      "DataLoader for .NET. Batched and cached fetches that keep N+1 problems out of your resolvers.",
  },
  {
    name: "Cookie Crumble",
    tagline: "Snapshot testing",
    description:
      "GraphQL-aware snapshot testing. Pin down schemas, results, and IExecutionResult shapes with readable, diff-friendly snapshots.",
  },
];

function ProductRow({ name, tagline, description }: ProductRowProps) {
  return (
    <li className="border-cc-card-border hover:border-cc-card-border-hover group grid grid-cols-1 gap-3 border-t py-7 transition-colors md:grid-cols-12 md:items-baseline md:gap-8">
      <div className="md:col-span-3">
        <h3 className="text-cc-heading font-heading text-xl font-semibold tracking-tight">
          {name}
        </h3>
        <p className="text-cc-nav-label mt-1 font-mono text-xs tracking-[0.18em] uppercase">
          {tagline}
        </p>
      </div>
      <p className="text-cc-prose text-base leading-relaxed md:col-span-9">
        {description}
      </p>
    </li>
  );
}

// ---------- Principles ----------

interface PrincipleProps {
  readonly title: string;
  readonly description: string;
}

const PRINCIPLES: ReadonlyArray<PrincipleProps> = [
  {
    title: "Open source first",
    description:
      "The platform lives on GitHub. Issues, pull requests, and roadmap conversations happen in the open.",
  },
  {
    title: "Honest about scope",
    description:
      "We say what the platform does and what it does not. No magic, no over-claim, no roadmap fiction.",
  },
  {
    title: "Performance and DX",
    description:
      "We measure both. A fast graph that no one wants to use is a failure, and so is a friendly graph that buckles at peak.",
  },
  {
    title: ".NET-native",
    description:
      "We are a .NET shop. Idiomatic C#, source generators, and first-class integration with the platform you already run.",
  },
  {
    title: "OpenTelemetry-native",
    description:
      "Traces, metrics, and logs through OpenTelemetry. Your existing observability stack is the observability stack.",
  },
  {
    title: "Built in the open",
    description:
      "Designs, RFCs, and releases are public. The platform you depend on is one you can read end to end.",
  },
];

function Principle({ title, description }: PrincipleProps) {
  return (
    <div className="border-cc-card-border flex flex-col gap-2 border-l py-2 pl-5">
      <h3 className="text-cc-heading font-heading text-lg font-semibold tracking-tight">
        {title}
      </h3>
      <p className="text-cc-prose text-sm leading-relaxed">{description}</p>
    </div>
  );
}

// ---------- Community channels ----------

interface ChannelProps {
  readonly label: string;
  readonly handle: string;
  readonly href: string;
}

const CHANNELS: ReadonlyArray<ChannelProps> = [
  {
    label: "GitHub",
    handle: "ChilliCream/graphql-platform",
    href: "https://github.com/ChilliCream/graphql-platform",
  },
  {
    label: "Slack",
    handle: "slack.chillicream.com",
    href: "https://slack.chillicream.com/",
  },
  {
    label: "Nitro",
    handle: "nitro.chillicream.com",
    href: "https://nitro.chillicream.com",
  },
  {
    label: "YouTube",
    handle: "youtube.com/c/ChilliCream",
    href: "https://www.youtube.com/c/ChilliCream",
  },
  {
    label: "LinkedIn",
    handle: "linkedin.com/company/chillicream",
    href: "https://www.linkedin.com/company/chillicream",
  },
  {
    label: "X",
    handle: "@Chilli_Cream",
    href: "https://x.com/Chilli_Cream",
  },
  {
    label: "Blog",
    handle: "chillicream.com/blog",
    href: "/blog",
  },
];

function Channel({ label, handle, href }: ChannelProps) {
  const isInternal = href.startsWith("/");
  const linkProps = isInternal
    ? {}
    : { target: "_blank", rel: "noopener noreferrer" };

  return (
    <a
      href={href}
      {...linkProps}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group flex items-center justify-between gap-4 rounded-xl border p-5 no-underline transition-colors"
    >
      <div className="flex flex-col">
        <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.2em] uppercase">
          {label}
        </span>
        <span className="text-cc-heading mt-1 font-mono text-sm">{handle}</span>
      </div>
      <span
        aria-hidden="true"
        className="text-cc-ink-dim group-hover:text-cc-accent text-lg transition-colors"
      >
        &rarr;
      </span>
    </a>
  );
}

// ---------- Page ----------

export default function AboutPreviewV3Page() {
  return (
    <>
      {/* Hero: outcome-led positioning */}
      <section className="py-20 text-center sm:py-28">
        <div className="text-cc-nav-label mb-5 font-mono text-xs tracking-[0.25em] uppercase">
          About ChilliCream
        </div>
        <h1 className="text-cc-heading font-heading mx-auto max-w-4xl text-5xl leading-[1.05] font-semibold tracking-tight sm:text-6xl lg:text-7xl">
          What teams ship with us
          <span className="block bg-gradient-to-r from-[#16b9e4] via-[#7c92c6] to-[#f0786a] bg-clip-text text-transparent">
            is the only proof that counts.
          </span>
        </h1>
        <p className="text-cc-prose mx-auto mt-7 max-w-2xl text-lg leading-relaxed sm:text-xl">
          We build the open-source GraphQL platform for .NET. Storefronts,
          compliance-grade graphs, and developer tooling, all running on the
          same end-to-end stack: Hot Chocolate, Fusion, Nitro, and the rest.
        </p>
        <div className="mt-10 flex flex-wrap justify-center gap-4">
          <SolidButton href="https://github.com/ChilliCream/graphql-platform">
            Browse the platform
          </SolidButton>
          <OutlineButton href="/services/support/contact">
            Talk to us
          </OutlineButton>
        </div>
      </section>

      {/* Prominent customer logo band, right under the hero */}
      <div className="border-cc-card-border my-4 rounded-2xl border-y">
        <LogoCloud />
      </div>

      {/* What teams ship with the platform */}
      <section className="py-20" aria-labelledby="outcomes-heading">
        <header className="mb-12 max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Outcomes / 01
          </p>
          <h2
            id="outcomes-heading"
            className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight sm:text-4xl"
          >
            What teams ship with the platform
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            Three patterns we see again and again. Different industries, the
            same end-to-end platform underneath.
          </p>
        </header>
        <div className="grid gap-6 md:grid-cols-3">
          {OUTCOMES.map((outcome) => (
            <OutcomeCard key={outcome.index} {...outcome} />
          ))}
        </div>
      </section>

      {/* The platform behind it */}
      <section className="py-20" aria-labelledby="platform-heading">
        <header className="mb-10 grid gap-6 md:grid-cols-12 md:items-end">
          <div className="md:col-span-7">
            <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
              Platform / 02
            </p>
            <h2
              id="platform-heading"
              className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight sm:text-4xl"
            >
              The platform behind it
            </h2>
          </div>
          <p className="text-cc-prose text-base leading-relaxed md:col-span-5">
            Every outcome above runs on the same open-source stack. Six
            products, one platform, all on GitHub.
          </p>
        </header>
        <ul className="border-cc-card-border border-b" role="list">
          {PRODUCTS.map((product) => (
            <ProductRow key={product.name} {...product} />
          ))}
        </ul>
        <div className="mt-10 flex flex-wrap gap-4">
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            See the source
          </OutlineButton>
          <OutlineButton href="/products">Explore products</OutlineButton>
        </div>
      </section>

      {/* Principles / values */}
      <section className="py-20" aria-labelledby="principles-heading">
        <header className="mb-12 max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Principles / 03
          </p>
          <h2
            id="principles-heading"
            className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight sm:text-4xl"
          >
            How we work on the platform
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            We hold a few things firm. They show up in every release and every
            pull request.
          </p>
        </header>
        <div className="grid gap-x-10 gap-y-8 md:grid-cols-2 lg:grid-cols-3">
          {PRINCIPLES.map((principle) => (
            <Principle key={principle.title} {...principle} />
          ))}
        </div>
      </section>

      {/* Who we are */}
      <section
        className="border-cc-card-border my-8 rounded-2xl border py-16"
        aria-labelledby="who-heading"
      >
        <div className="mx-auto max-w-3xl px-6 text-center">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Who we are / 04
          </p>
          <h2
            id="who-heading"
            className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight sm:text-4xl"
          >
            The team behind the platform
          </h2>
          <p className="text-cc-prose mt-6 text-base leading-relaxed sm:text-lg">
            ChilliCream is the team that builds and supports the open-source
            GraphQL platform for .NET. We answer the issues on GitHub, we ship
            the products listed above, and we sit alongside the teams that run
            them in production. That is what we do, and it is the only thing
            this page is going to claim.
          </p>
        </div>
      </section>

      {/* Community */}
      <section className="py-20" aria-labelledby="community-heading">
        <header className="mb-12 max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Community / 05
          </p>
          <h2
            id="community-heading"
            className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight sm:text-4xl"
          >
            Where the conversation happens
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            Pick the channel that fits the question. We are on all of them.
          </p>
        </header>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {CHANNELS.map((channel) => (
            <Channel key={channel.label} {...channel} />
          ))}
        </div>
      </section>

      {/* Engage / contact band */}
      <NextStepsSection
        title="Ready to ship a graph?"
        text="Start with the open-source platform, then bring us in for advisory, support, or training when you need the team behind the code."
        primaryLink="/services/support/contact"
        primaryLinkText="Talk to ChilliCream"
        secondaryLink="/pricing"
        secondaryLinkText="See pricing"
      />
    </>
  );
}
