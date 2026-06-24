import type { Metadata } from "next";
import type { ReactNode } from "react";

import { LogoCloud } from "@/src/components/home/LogoCloud";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "About ChilliCream: Open-Source GraphQL for .NET",
  description:
    "About ChilliCream: the team building the open-source GraphQL platform for .NET, including Hot Chocolate, Fusion, and Nitro, used in production by enterprises.",
  keywords: [
    "About ChilliCream",
    "ChilliCream",
    "GraphQL platform",
    "Hot Chocolate",
    "Fusion",
    "Nitro",
    "Strawberry Shake",
    "Green Donut",
    "Cookie Crumble",
    "open source GraphQL",
    ".NET GraphQL",
    "OpenTelemetry",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "About ChilliCream: Open-Source GraphQL for .NET",
    description:
      "We build the open-source GraphQL platform for .NET: Hot Chocolate, Fusion, Nitro, Strawberry Shake, Mocha, Green Donut, and Cookie Crumble. In the open, in production.",
    type: "website",
  },
};

// Brand spectrum gradient, used exactly once on this screen (the hero headline accent).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const REPO_URL = "https://github.com/ChilliCream/graphql-platform";

interface CodeChromeProps {
  readonly title: string;
  readonly children: ReactNode;
  readonly footer?: ReactNode;
  readonly className?: string;
}

/**
 * A small chrome window styled to evoke a code editor or terminal pane. Used as
 * the visual frame for the hero repo card and the product list, so the whole
 * page reads like an IDE shell rather than marketing rectangles.
 */
function CodeChrome({ title, children, footer, className }: CodeChromeProps) {
  return (
    <div
      className={[
        "bg-cc-card-bg border-cc-card-border overflow-hidden rounded-xl border backdrop-blur-sm",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-success/70 h-2.5 w-2.5 rounded-full" />
        <span className="text-cc-ink-dim ml-3 font-mono text-xs tracking-wide">
          {title}
        </span>
      </div>
      {children}
      {footer && (
        <div className="border-cc-card-border text-cc-ink-dim border-t px-4 py-2.5 font-mono text-xs">
          {footer}
        </div>
      )}
    </div>
  );
}

interface RepoMetaRowProps {
  readonly label: string;
  readonly value: string;
  readonly dotClass?: string;
}

function RepoMetaRow({ label, value, dotClass }: RepoMetaRowProps) {
  return (
    <div className="flex items-center gap-3">
      {dotClass && (
        <span
          className={["inline-block h-2.5 w-2.5 rounded-full", dotClass].join(
            " ",
          )}
          aria-hidden="true"
        />
      )}
      <span className="text-cc-ink-dim font-mono text-xs tracking-wide uppercase">
        {label}
      </span>
      <span className="text-cc-heading font-mono text-sm">{value}</span>
    </div>
  );
}

function GitHubMark({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      className={className}
      aria-hidden="true"
      fill="currentColor"
    >
      <path d="M12 .5C5.65.5.5 5.65.5 12c0 5.08 3.29 9.39 7.86 10.91.57.11.78-.25.78-.55v-1.93c-3.2.69-3.87-1.54-3.87-1.54-.52-1.33-1.27-1.69-1.27-1.69-1.04-.71.08-.7.08-.7 1.15.08 1.76 1.18 1.76 1.18 1.02 1.76 2.69 1.25 3.35.96.1-.75.4-1.25.73-1.54-2.55-.29-5.24-1.28-5.24-5.69 0-1.26.45-2.29 1.18-3.09-.12-.29-.51-1.46.11-3.04 0 0 .96-.31 3.16 1.18a10.94 10.94 0 0 1 5.75 0c2.2-1.49 3.16-1.18 3.16-1.18.62 1.58.23 2.75.11 3.04.74.8 1.18 1.83 1.18 3.09 0 4.42-2.69 5.4-5.26 5.68.41.36.78 1.07.78 2.17v3.22c0 .31.21.67.79.55A11.51 11.51 0 0 0 23.5 12C23.5 5.65 18.35.5 12 .5Z" />
    </svg>
  );
}

function SlackIcon({ className }: { readonly className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden="true">
      <path
        fill="currentColor"
        d="M5 15a2 2 0 1 1 0-4h2v2a2 2 0 0 1-2 2Zm1-4a2 2 0 0 1-2-2 2 2 0 0 1 4 0v2H6Zm3 8a2 2 0 1 1-4 0v-2h2a2 2 0 0 1 2 2Zm-2-3a2 2 0 0 1 0-4h6a2 2 0 0 1 0 4H7Zm12-4a2 2 0 1 1 0 4h-2v-2a2 2 0 0 1 2-2Zm-1 4a2 2 0 0 1 2 2 2 2 0 0 1-4 0v-2h2Zm-3-8a2 2 0 1 1 4 0v2h-2a2 2 0 0 1-2-2Zm2 3a2 2 0 0 1 0 4h-6a2 2 0 0 1 0-4h6Z"
      />
    </svg>
  );
}

function YouTubeIcon({ className }: { readonly className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden="true">
      <path
        fill="currentColor"
        d="M21.6 7.2a2.5 2.5 0 0 0-1.76-1.76C18.27 5 12 5 12 5s-6.27 0-7.84.44A2.5 2.5 0 0 0 2.4 7.2 26.2 26.2 0 0 0 2 12a26.2 26.2 0 0 0 .4 4.8 2.5 2.5 0 0 0 1.76 1.76C5.73 19 12 19 12 19s6.27 0 7.84-.44a2.5 2.5 0 0 0 1.76-1.76A26.2 26.2 0 0 0 22 12a26.2 26.2 0 0 0-.4-4.8ZM10 15V9l5.2 3L10 15Z"
      />
    </svg>
  );
}

function XIcon({ className }: { readonly className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden="true">
      <path
        fill="currentColor"
        d="M17.53 3H20.5l-6.5 7.43L21.75 21h-6.1l-4.78-6.25L5.4 21H2.42l6.95-7.94L2.25 3h6.25l4.32 5.71L17.53 3Zm-1.07 16.2h1.65L7.62 4.7H5.85l10.6 14.5Z"
      />
    </svg>
  );
}

function LinkedInIcon({ className }: { readonly className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden="true">
      <path
        fill="currentColor"
        d="M6.94 6.5a1.94 1.94 0 1 1-3.88 0 1.94 1.94 0 0 1 3.88 0ZM3.2 8.7h3.6V21H3.2V8.7Zm6 0h3.45v1.68h.05c.48-.9 1.65-1.85 3.4-1.85 3.64 0 4.3 2.4 4.3 5.52V21H16.8v-5.45c0-1.3 0-2.97-1.81-2.97-1.82 0-2.09 1.41-2.09 2.88V21H9.2V8.7Z"
      />
    </svg>
  );
}

function BlogIcon({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      className={className}
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M4 5h11a4 4 0 0 1 4 4v10H8a4 4 0 0 1-4-4V5Z" />
      <path d="M8 9h7M8 13h7M8 17h4" />
    </svg>
  );
}

function NitroSparkIcon({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      className={className}
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M12 3v4M12 17v4M3 12h4M17 12h4M5.6 5.6l2.8 2.8M15.6 15.6l2.8 2.8M5.6 18.4l2.8-2.8M15.6 8.4l2.8-2.8" />
      <circle cx="12" cy="12" r="2.4" fill="currentColor" stroke="none" />
    </svg>
  );
}

function ArrowIcon({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      className={className}
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M5 12h14M13 6l6 6-6 6" />
    </svg>
  );
}

interface Product {
  readonly name: string;
  readonly tag: string;
  readonly description: string;
  readonly href?: string;
}

const PRODUCTS: readonly Product[] = [
  {
    name: "Hot Chocolate",
    tag: "graphql-server",
    description:
      "The open-source GraphQL server for .NET. Fluent or schema-first, with first-class subscriptions, persisted operations, and OpenTelemetry.",
    href: "/products/hotchocolate",
  },
  {
    name: "Fusion",
    tag: "distributed-gateway",
    description:
      "The distributed GraphQL gateway built on Hot Chocolate. Compose subgraphs into one schema with predictable execution and traceable requests.",
  },
  {
    name: "Nitro",
    tag: "control-plane",
    description:
      "The control plane: schema and client registry, CI schema checks, observability and tracing, plus the GraphQL IDE your team already wants to use.",
    href: "/products/nitro",
  },
  {
    name: "Strawberry Shake",
    tag: "typed-client",
    description:
      "Typed GraphQL client for .NET with MSBuild codegen. Author operations next to your code, ship strongly-typed clients without ceremony.",
    href: "/products/strawberryshake",
  },
  {
    name: "Mocha",
    tag: "mediator",
    description:
      "Source-generated mediator and cross-service messaging for .NET. Zero-reflection dispatch, designed for high-throughput service boundaries.",
  },
  {
    name: "Green Donut",
    tag: "dataloader",
    description:
      "The DataLoader for .NET. Batched, cached, request-scoped fetching that fixes N+1 without leaking through your resolvers.",
  },
  {
    name: "Cookie Crumble",
    tag: "snapshot-testing",
    description:
      "GraphQL-aware snapshot testing. Lock in queries, responses, and schema shape so refactors stop breaking your contract by accident.",
  },
];

interface Principle {
  readonly index: string;
  readonly title: string;
  readonly body: string;
}

const PRINCIPLES: readonly Principle[] = [
  {
    index: "01",
    title: "Open source, open core",
    body: "Hot Chocolate, Fusion, Strawberry Shake, Mocha, Green Donut, and Cookie Crumble are open source on GitHub. The platform you read is the platform you run.",
  },
  {
    index: "02",
    title: "Decisions in the open",
    body: "Designs land as issues and PRs. Discussions happen in public on GitHub and Slack. We would rather get critique early than ship something quietly wrong.",
  },
  {
    index: "03",
    title: "Honest about scope",
    body: "We claim what is in the repo and used by customers, not what sounds good in a deck. If something is experimental, the docs say so.",
  },
  {
    index: "04",
    title: ".NET-native, end to end",
    body: "Hot Chocolate, Fusion, Strawberry Shake, Mocha, and Green Donut are written for .NET first. Idiomatic APIs that feel native to the .NET stack.",
  },
  {
    index: "05",
    title: "Performance and DX as features",
    body: "Source generators, pooled buffers, and zero-allocation hot paths sit next to first-class developer ergonomics. Both matter, both ship.",
  },
  {
    index: "06",
    title: "OpenTelemetry-native",
    body: "Tracing, metrics, and logs are first-class across the platform, and Nitro speaks the same language. Bring your own backend or use ours.",
  },
];

interface Channel {
  readonly name: string;
  readonly handle: string;
  readonly href: string;
  readonly icon: ReactNode;
}

const CHANNELS: readonly Channel[] = [
  {
    name: "Slack",
    handle: "slack.chillicream.com",
    href: "https://slack.chillicream.com/",
    icon: <SlackIcon className="h-5 w-5" />,
  },
  {
    name: "YouTube",
    handle: "/c/ChilliCream",
    href: "https://www.youtube.com/c/ChilliCream",
    icon: <YouTubeIcon className="h-5 w-5" />,
  },
  {
    name: "X",
    handle: "@Chilli_Cream",
    href: "https://x.com/Chilli_Cream",
    icon: <XIcon className="h-5 w-5" />,
  },
  {
    name: "LinkedIn",
    handle: "/company/chillicream",
    href: "https://www.linkedin.com/company/chillicream",
    icon: <LinkedInIcon className="h-5 w-5" />,
  },
  {
    name: "Blog",
    handle: "/blog",
    href: "/blog",
    icon: <BlogIcon className="h-5 w-5" />,
  },
  {
    name: "Nitro",
    handle: "nitro.chillicream.com",
    href: "https://nitro.chillicream.com",
    icon: <NitroSparkIcon className="h-5 w-5" />,
  },
];

interface BusinessModelCardProps {
  readonly index: string;
  readonly title: string;
  readonly body: string;
  readonly link: { readonly href: string; readonly label: string };
}

function BusinessModelCard({
  index,
  title,
  body,
  link,
}: BusinessModelCardProps) {
  return (
    <div className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative flex flex-col rounded-xl border p-6 backdrop-blur-sm transition-colors">
      <div className="text-cc-ink-dim font-mono text-xs tracking-[0.2em] uppercase">
        {index}
      </div>
      <h3 className="text-cc-heading font-heading mt-3 text-xl font-semibold tracking-tight">
        {title}
      </h3>
      <p className="text-cc-ink mt-3 text-sm leading-relaxed">{body}</p>
      <a
        href={link.href}
        className="text-cc-accent hover:text-cc-accent-hover mt-5 inline-flex items-center gap-2 font-mono text-xs tracking-wide uppercase"
        {...(link.href.startsWith("/")
          ? {}
          : { target: "_blank", rel: "noopener noreferrer" })}
      >
        {link.label}
        <ArrowIcon className="h-3.5 w-3.5" />
      </a>
    </div>
  );
}

export default function AboutPreviewV2Page() {
  return (
    <>
      {/* Hero */}
      <section className="relative pt-16 pb-12 sm:pt-24 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-[1.05fr_1fr] lg:gap-16">
          <div>
            <div className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
              <span className="text-cc-accent">$</span> about chillicream
              --since main
            </div>
            <h1 className="font-heading text-cc-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight sm:text-6xl lg:text-7xl">
              Built in the open.{" "}
              <span
                className="bg-clip-text text-transparent"
                style={{ backgroundImage: SPECTRUM }}
              >
                Used in production.
              </span>
            </h1>
            <p className="text-cc-ink mt-6 max-w-xl text-lg leading-relaxed">
              ChilliCream builds the open-source GraphQL platform for .NET. Hot
              Chocolate, Fusion, and Nitro power developer tools, enterprise
              APIs, and the systems behind them, with the source on GitHub and
              the roadmap in the open.
            </p>
            <div className="mt-8 flex flex-wrap items-center gap-3">
              <SolidButton href={REPO_URL}>View on GitHub</SolidButton>
              <OutlineButton href="/platform">
                Explore the platform
              </OutlineButton>
            </div>
          </div>

          {/* GitHub-style repo chrome card. Static, inline, no fetch, no image. */}
          <CodeChrome
            title="ChilliCream/graphql-platform"
            footer={
              <div className="flex items-center justify-between">
                <span>readme.md</span>
                <span className="text-cc-ink">main</span>
              </div>
            }
          >
            <div className="px-5 py-5">
              <div className="flex items-center gap-3">
                <span className="border-cc-card-border bg-cc-bg/60 text-cc-heading inline-flex h-9 w-9 items-center justify-center rounded-md border">
                  <GitHubMark className="h-5 w-5" />
                </span>
                <div className="leading-tight">
                  <div className="text-cc-ink-dim font-mono text-xs">
                    github.com
                  </div>
                  <div className="text-cc-heading font-mono text-sm">
                    ChilliCream / graphql-platform
                  </div>
                </div>
                <span className="border-cc-card-border text-cc-ink-dim ml-auto rounded-full border px-2.5 py-0.5 font-mono text-[10px] tracking-wide uppercase">
                  Public
                </span>
              </div>

              <p className="text-cc-ink mt-5 text-sm leading-relaxed">
                Welcome to the home of the ChilliCream GraphQL platform.
                Everything you need to build, evolve, and run a modern GraphQL
                API in .NET.
              </p>

              <div className="mt-5 grid gap-2.5">
                <RepoMetaRow
                  label="License"
                  value="MIT"
                  dotClass="bg-cc-success"
                />
                <RepoMetaRow
                  label="Language"
                  value="C# / .NET"
                  dotClass="bg-cc-accent"
                />
                <RepoMetaRow
                  label="Stack"
                  value="Hot Chocolate, Fusion, Nitro"
                  dotClass="bg-cc-cta"
                />
                <RepoMetaRow
                  label="Topics"
                  value="graphql · dotnet · opentelemetry"
                  dotClass="bg-cc-warning"
                />
              </div>

              <a
                href={REPO_URL}
                target="_blank"
                rel="noopener noreferrer"
                className="text-cc-accent hover:text-cc-accent-hover mt-5 inline-flex items-center gap-2 font-mono text-xs tracking-wide uppercase"
              >
                git clone graphql-platform
                <ArrowIcon className="h-3.5 w-3.5" />
              </a>
            </div>
          </CodeChrome>
        </div>
      </section>

      {/* Quiet enterprise proof. */}
      <LogoCloud />

      {/* How we work: business model + values combined into one section. */}
      <section className="py-16 sm:py-20">
        <div className="grid gap-10 lg:grid-cols-[0.9fr_1.1fr] lg:gap-16">
          <div>
            <div className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
              # how we work
            </div>
            <h2 className="font-heading text-cc-heading mt-4 text-4xl font-semibold tracking-tight sm:text-5xl">
              Open-core OSS, sold as services and Nitro.
            </h2>
            <p className="text-cc-ink mt-5 text-base leading-relaxed">
              The libraries are free and MIT-licensed. We make a living from
              hands-on services, Nitro plans, and support contracts, not from
              gating the SDK behind a tier.
            </p>
            <p className="text-cc-ink mt-4 text-base leading-relaxed">
              If the open-source platform never needs us, that is a feature.
              When teams want experts, control-plane tooling, or a phone number,
              we are here.
            </p>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <BusinessModelCard
              index="01"
              title="The platform stays open"
              body="Hot Chocolate, Fusion, Strawberry Shake, Mocha, Green Donut, and Cookie Crumble live in one public repo, MIT-licensed, with the roadmap visible."
              link={{ href: REPO_URL, label: "open the repo" }}
            />
            <BusinessModelCard
              index="02"
              title="Services pay the bills"
              body="Advisory hours, contracting engagements, and team training are how we fund the platform work. The engineers on the call are the people who wrote the code."
              link={{ href: "/services", label: "see services" }}
            />
            <BusinessModelCard
              index="03"
              title="Nitro is the product"
              body="Nitro is the commercial control plane: schema registry, CI checks, observability, and the GraphQL IDE. Paid plans fund the platform."
              link={{ href: "/products/nitro", label: "explore Nitro" }}
            />
            <BusinessModelCard
              index="04"
              title="Support, with a contract"
              body="Tiered support plans for teams that want a private channel, SLAs, and people who know the codebase because they wrote it."
              link={{ href: "/services/support", label: "see support" }}
            />
          </div>
        </div>
      </section>

      {/* Principles. */}
      <section className="py-16 sm:py-20">
        <div className="mb-12 flex flex-col items-start gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div className="max-w-xl">
            <div className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
              # principles
            </div>
            <h2 className="font-heading text-cc-heading mt-4 text-4xl font-semibold tracking-tight sm:text-5xl">
              The rules we hold ourselves to.
            </h2>
          </div>
          <p className="text-cc-ink-dim max-w-md text-sm leading-relaxed">
            Not slogans. These are the constraints we judge a design against
            before it ships.
          </p>
        </div>

        <ol className="border-cc-card-border bg-cc-card-border grid gap-px overflow-hidden rounded-xl border sm:grid-cols-2 lg:grid-cols-3">
          {PRINCIPLES.map((p) => (
            <li
              key={p.index}
              className="bg-cc-card-bg flex flex-col p-6 backdrop-blur-sm"
            >
              <div className="flex items-baseline gap-3">
                <span className="text-cc-accent font-mono text-xs tracking-[0.2em]">
                  {p.index}
                </span>
                <h3 className="text-cc-heading font-heading text-lg font-semibold tracking-tight">
                  {p.title}
                </h3>
              </div>
              <p className="text-cc-ink mt-3 text-sm leading-relaxed">
                {p.body}
              </p>
            </li>
          ))}
        </ol>
      </section>

      {/* The platform: six products in a code-listing chrome. */}
      <section className="py-16 sm:py-20">
        <div className="mb-12">
          <div className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            # the platform
          </div>
          <h2 className="font-heading text-cc-heading mt-4 max-w-3xl text-4xl font-semibold tracking-tight sm:text-5xl">
            One repo. Six libraries and one gateway that fit together.
          </h2>
          <p className="text-cc-ink mt-5 max-w-2xl text-base leading-relaxed">
            Every box below is a real package in{" "}
            <code className="text-cc-accent font-mono text-sm">
              ChilliCream/graphql-platform
            </code>
            . Pick one, pick the stack, pick whatever solves the problem in
            front of you.
          </p>
        </div>

        <CodeChrome title="src/ // graphql-platform">
          <ul className="divide-cc-card-border divide-y">
            {PRODUCTS.map((product, idx) => {
              const number = String(idx + 1).padStart(2, "0");
              const content = (
                <div className="grid grid-cols-[auto_1fr] items-start gap-4 px-5 py-5 sm:grid-cols-[auto_1fr_auto] sm:items-center sm:px-6">
                  <span className="text-cc-ink-dim w-8 font-mono text-xs tracking-wide">
                    {number}
                  </span>
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-baseline gap-3">
                      <span className="text-cc-heading font-heading text-lg font-semibold tracking-tight">
                        {product.name}
                      </span>
                      <span className="text-cc-accent font-mono text-[11px] tracking-wide">
                        {"// "}
                        {product.tag}
                      </span>
                    </div>
                    <p className="text-cc-ink mt-1.5 text-sm leading-relaxed">
                      {product.description}
                    </p>
                  </div>
                  {product.href && (
                    <span className="text-cc-ink-dim hidden font-mono text-xs tracking-wide uppercase sm:inline-flex sm:items-center sm:gap-1.5">
                      read more
                      <ArrowIcon className="h-3.5 w-3.5" />
                    </span>
                  )}
                </div>
              );
              return (
                <li key={product.name}>
                  {product.href ? (
                    <a
                      href={product.href}
                      className="hover:bg-cc-bg/40 block transition-colors"
                    >
                      {content}
                    </a>
                  ) : (
                    <div>{content}</div>
                  )}
                </li>
              );
            })}
          </ul>
        </CodeChrome>
      </section>

      {/* Community channels. */}
      <section className="py-16 sm:py-20">
        <div className="mb-12 max-w-3xl">
          <div className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            # community
          </div>
          <h2 className="font-heading text-cc-heading mt-4 text-4xl font-semibold tracking-tight sm:text-5xl">
            Show up where the work happens.
          </h2>
          <p className="text-cc-ink mt-5 text-base leading-relaxed">
            Questions, designs, bug reports, and the occasional GraphQL hot take
            all land in the same channels we use ourselves. No marketing gates.
          </p>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {CHANNELS.map((channel) => (
            <ChannelLink key={channel.name} channel={channel} />
          ))}
        </div>
      </section>

      {/* Engage / contact band. */}
      <section className="py-16 sm:py-20">
        <div className="bg-cc-card-bg border-cc-card-border relative overflow-hidden rounded-2xl border p-8 backdrop-blur-sm sm:p-12">
          <div className="grid gap-8 lg:grid-cols-[1.1fr_0.9fr] lg:items-center">
            <div>
              <div className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
                # engage
              </div>
              <h2 className="font-heading text-cc-heading mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">
                Bring us in when it counts.
              </h2>
              <p className="text-cc-ink mt-5 max-w-xl text-base leading-relaxed">
                Whether you need a second pair of eyes on an architecture, a
                team that can ship a Fusion gateway with you, or a support
                contract that puts an engineer on the other end of Slack, we can
                help.
              </p>
            </div>
            <div className="flex flex-wrap gap-3 lg:justify-end">
              <SolidButton href="/services/support/contact">
                Talk to us
              </SolidButton>
              <OutlineButton href="/services">See services</OutlineButton>
              <OutlineButton href="/pricing">Pricing</OutlineButton>
            </div>
          </div>
        </div>
      </section>
    </>
  );
}

interface ChannelLinkProps {
  readonly channel: Channel;
}

function ChannelLink({ channel }: ChannelLinkProps) {
  const isExternal = !channel.href.startsWith("/");
  return (
    <a
      href={channel.href}
      {...(isExternal ? { target: "_blank", rel: "noopener noreferrer" } : {})}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group flex items-center gap-4 rounded-xl border p-5 backdrop-blur-sm transition-colors"
    >
      <span className="border-cc-card-border bg-cc-bg/60 text-cc-heading group-hover:text-cc-accent inline-flex h-11 w-11 items-center justify-center rounded-lg border transition-colors">
        {channel.icon}
      </span>
      <div className="min-w-0 flex-1">
        <div className="text-cc-heading font-heading text-base font-semibold tracking-tight">
          {channel.name}
        </div>
        <div className="text-cc-ink-dim truncate font-mono text-xs">
          {channel.handle}
        </div>
      </div>
      <ArrowIcon className="text-cc-ink-dim group-hover:text-cc-accent h-4 w-4 transition-colors" />
    </a>
  );
}
