import type { Metadata } from "next";
import NextLink from "next/link";
import type { ReactNode } from "react";

import { FromOurBlog } from "@/src/components/FromOurBlog";
import { LogoCloud } from "@/src/components/home/LogoCloud";

export const metadata: Metadata = {
  title: "ChilliCream Dispatch No. 01: GraphQL platform for .NET",
  description:
    "Dispatch No. 01 from ChilliCream. A long-form read on the end-to-end GraphQL platform for .NET: Hot Chocolate, Strawberry Shake, Nitro, Fusion, Mocha.",
  keywords: [
    "ChilliCream",
    "GraphQL platform for .NET",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    "Fusion",
    "Mocha",
    "GraphQL observability",
    "schema registry",
  ],
  openGraph: {
    title: "ChilliCream Dispatch No. 01: GraphQL platform for .NET",
    description:
      "A long-form read on the end-to-end GraphQL platform for .NET. Hot Chocolate, Strawberry Shake, Nitro, Fusion, Mocha. One open source family, MIT licensed.",
  },
  robots: { index: false, follow: false },
};

interface ChapterRuleProps {
  readonly numeral: string;
  readonly kicker: string;
}

interface MoveProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly marginalia: readonly string[];
}

interface ProductRowProps {
  readonly name: string;
  readonly tagline: string;
  readonly description: string;
  readonly href: string;
  readonly linkLabel: string;
  readonly external?: boolean;
}

interface PricingRowProps {
  readonly label: string;
  readonly price: string;
  readonly note: string;
}

const MOVES: readonly MoveProps[] = [
  {
    eyebrow: "Build",
    title: "Source-generated, end to end.",
    body: "Hot Chocolate generates resolver dispatch, type bindings, and execution plans at compile time. Strawberry Shake generates the typed C# client from your operations via MSBuild code generation. The schema you ship is the schema both sides agree on, with no reflection on the hot path.",
    marginalia: [
      "compile-time validated",
      "MSBuild codegen",
      "zero reflection",
    ],
  },
  {
    eyebrow: "Observe",
    title: "OpenTelemetry from gateway to resolver.",
    body: "Once Nitro is configured in the server, every operation carries OpenTelemetry traces, metrics, and logs. Operation insights surface p95, throughput, error rate, and impact, with per-client tracking via GraphQL-Client-Id and Version.",
    marginalia: ["p95 142ms", "3.4k ops per minute", "per-client tracking"],
  },
  {
    eyebrow: "Evolve",
    title: "Know which published clients a change affects.",
    body: "The schema registry tracks every published version. The client registry knows every operation each app actually sends. Nitro CI compares them and reports the published clients affected before you deploy, with stage promotion gates and rollback by republishing an earlier tag.",
    marginalia: ["schema registry", "client registry", "stage promotion gates"],
  },
  {
    eyebrow: "Agentic",
    title: "An MCP endpoint your agents can actually use.",
    body: "Hot Chocolate exposes an MCP server over Streamable HTTP. Curate feature collections of .graphql operations, JSON descriptions, and HTML context, then track per-tool latency, ops per minute, error rate, and impact in Nitro beside the per-operation view.",
    marginalia: ["MCP over HTTP", "feature collections", "per-tool telemetry"],
  },
  {
    eyebrow: "Workflows",
    title: "Mocha runs the work between services.",
    body: "Mocha is the source-generated mediator and messaging library. Sagas are validated before traffic moves, processing is exactly-once, and the in-process and cross-service programming model is the same. No reflection on the hot path.",
    marginalia: [
      "validated sagas",
      "exactly-once processing",
      "same model everywhere",
    ],
  },
];

const PRODUCTS: readonly ProductRowProps[] = [
  {
    name: "Hot Chocolate",
    tagline: "GraphQL server for .NET",
    description:
      "The source-generated GraphQL server at the heart of the platform. Schema-first or code-first, built on ASP.NET Core, with the modern GraphQL spec.",
    href: "/products/hotchocolate",
    linkLabel: "Read about Hot Chocolate",
  },
  {
    name: "Strawberry Shake",
    tagline: "Typed .NET client",
    description:
      "A typed GraphQL client for .NET. MSBuild code generation turns each query into a fully typed C# API, so apps stay in sync with the schema.",
    href: "/products/strawberryshake",
    linkLabel: "Read about Strawberry Shake",
  },
  {
    name: "Nitro",
    tagline: "Control plane and IDE",
    description:
      "Schema registry, client registry, CI checks, observability, and the GraphQL IDE your team already uses. Served from your endpoint, scaled in the cloud.",
    href: "https://nitro.chillicream.com",
    linkLabel: "Launch Nitro",
    external: true,
  },
  {
    name: "Mocha",
    tagline: "Mediator and workflows",
    description:
      "A source-generated mediator for in-process and cross-service messaging. Validated sagas, exactly-once processing, no reflection on the hot path.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Mocha on GitHub",
    external: true,
  },
  {
    name: "Fusion",
    tagline: "Composition for many subgraphs",
    description:
      "Compose multiple subgraphs at planning time, then run the gateway in your own ASP.NET Core process. The query plan is decided before traffic moves.",
    href: "/products/fusion",
    linkLabel: "Read about Fusion",
  },
  {
    name: "Green Donut",
    tagline: "DataLoader for .NET",
    description:
      "The DataLoader implementation behind Hot Chocolate. Batches and caches data access so resolvers stay simple and N+1 stops being your problem.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Green Donut on GitHub",
    external: true,
  },
  {
    name: "Cookie Crumble",
    tagline: "GraphQL-aware snapshot testing",
    description:
      "Snapshot testing built for GraphQL. Native support for execution results and HTTP responses, with Markdown snapshots for multi-shape assertions.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Cookie Crumble on GitHub",
    external: true,
  },
];

const PRICING_ROWS: readonly PricingRowProps[] = [
  { label: "Shared Instance", price: "Free", note: "pay as you go" },
  { label: "Dedicated Instance", price: "$400", note: "per month" },
  { label: "Startup support", price: "$450", note: "per month" },
  { label: "Business support", price: "$1,300", note: "per month" },
];

export default function LandingPreviewV4Page() {
  return (
    <article className="mx-auto max-w-[640px] pt-10 pb-16 sm:pt-16 lg:max-w-none">
      <Masthead />

      <ChapterRule numeral="I." kicker="The Lede" />
      <ColumnWithMarginalia
        marginalia={[
          "MIT licensed",
          "self-host or Nitro Cloud",
          "one monorepo",
        ]}
      >
        <p className="text-cc-ink text-lead first-letter:font-heading first-letter:text-cc-accent first-letter:float-left first-letter:mt-1 first-letter:mr-3 first-letter:text-[5.5rem] first-letter:leading-[0.85]">
          One platform, designed together. Hot Chocolate ships the server.
          Strawberry Shake ships the typed client. Nitro ships the control
          plane, registry, and observability. The whole family lives in one
          monorepo on GitHub, MIT licensed across every package, free to
          self-host and ready to run on Nitro Cloud when you want it managed.
        </p>
      </ColumnWithMarginalia>

      <LogoBand />

      <ChapterRule numeral="II." kicker="The Five Moves" />
      <ColumnWithMarginalia>
        <p className="text-cc-ink text-body leading-relaxed">
          The platform is five moves, designed to compose. Each surfaces in
          Nitro as its own view, and each one assumes the others exist.
        </p>
      </ColumnWithMarginalia>

      <div className="mt-10 flex flex-col gap-10">
        {MOVES.map((move, index) => (
          <Move key={move.eyebrow} move={move} isFirst={index === 0} />
        ))}
      </div>

      <ChapterRule numeral="III." kicker="Fusion, in detail" />
      <ColumnWithMarginalia
        marginalia={[
          "composition at planning time",
          "self-run ASP.NET Core gateway",
          "distributed tracing across subgraphs",
        ]}
      >
        <h2 className="font-heading text-cc-heading text-h4 font-semibold">
          Compose many subgraphs at planning time.
        </h2>
        <p className="text-cc-ink text-body mt-5 leading-relaxed">
          Fusion is composition for many subgraphs into one cohesive graph. The
          query plan is decided before traffic moves, so the work that would
          otherwise happen on the hot path happens once, in advance, inside the
          composition lifecycle of begin, validate, commit, and rollback.
        </p>
        <p className="text-cc-ink text-body mt-5 leading-relaxed">
          The gateway is always your own ASP.NET Core app. You keep the auth,
          telemetry, and runtime you already trust at the edge, and distributed
          tracing follows the request across every subgraph.
        </p>
      </ColumnWithMarginalia>

      <ChapterRule numeral="IV." kicker="The Family" />
      <ColumnWithMarginalia>
        <p className="text-cc-ink text-body leading-relaxed">
          A coherent set of libraries for .NET. Each one stands on its own, and
          each one assumes the others exist.
        </p>
      </ColumnWithMarginalia>

      <div className="mx-auto mt-8 max-w-[640px]">
        <div className="border-cc-card-border border-t">
          {PRODUCTS.map((product) => (
            <ProductRow key={product.name} product={product} />
          ))}
        </div>
      </div>

      <ChapterRule numeral="V." kicker="Open source, in the open" />
      <ColumnWithMarginalia>
        <p className="text-cc-ink text-body leading-relaxed">
          The whole platform lives in one monorepo. Read the code, open an
          issue, send a pull request, and ship the same bits we ship. Releases,
          roadmaps, and discussions are public. MIT licensed across every
          package, with community Slack and GitHub triage.
        </p>
        <p className="mt-6">
          <a
            href="https://github.com/ChilliCream/graphql-platform"
            target="_blank"
            rel="noopener noreferrer"
            className="border-cc-card-border hover:border-cc-card-border-hover text-cc-ink hover:text-cc-heading inline-flex items-center gap-2 border px-3 py-1.5 font-mono text-[0.7rem] tracking-[0.16em] uppercase transition-colors"
          >
            <span>ChilliCream/graphql-platform</span>
            <span aria-hidden>-&gt;</span>
          </a>
        </p>
      </ColumnWithMarginalia>

      <ChapterRule numeral="VI." kicker="Pricing, plainly" />
      <ColumnWithMarginalia>
        <p className="text-cc-ink text-body leading-relaxed">
          The Hot Chocolate platform itself is open source and free under MIT.
          Nitro Cloud has a free Shared Instance and a $400 Dedicated Instance.
          Support plans start at $450 Startup and $1,300 Business, with Custom
          Enterprise for the rest.
        </p>
        <dl className="border-cc-card-border mt-8 grid grid-cols-2 gap-y-5 border-t pt-6 sm:grid-cols-4 sm:gap-x-6">
          {PRICING_ROWS.map((row) => (
            <PricingCell key={row.label} row={row} />
          ))}
        </dl>
        <p className="mt-8">
          <NextLink
            href="/pricing"
            className="text-cc-accent hover:text-cc-accent-hover text-lead font-medium underline underline-offset-4"
          >
            See full pricing -&gt;
          </NextLink>
        </p>
      </ColumnWithMarginalia>

      <ChapterRule numeral="VII." kicker="Recent dispatches" />
      <div className="mx-auto mt-10 max-w-[640px] lg:max-w-none lg:pr-[272px]">
        <BorderlessBlog />
      </div>

      <Colophon />
    </article>
  );
}

function Masthead() {
  return (
    <header className="mx-auto max-w-[640px] lg:max-w-none">
      <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
        ChilliCream <span className="text-cc-ink-faint">/</span> Dispatch no. 01{" "}
        <span className="text-cc-ink-faint">/</span> GraphQL platform for .NET
      </p>
      <h1 className="font-heading text-cc-heading mt-8 text-4xl leading-[1.05] font-semibold tracking-[-0.02em] sm:text-6xl lg:text-[5rem] lg:leading-[1.02]">
        Your GraphQL platform,
        <span className="text-cc-ink-dim block">running on .NET.</span>
      </h1>
      <p className="text-cc-ink mt-10 font-mono text-sm">
        <NextLink
          href="/get-started"
          className="text-cc-accent hover:text-cc-accent-hover font-medium underline underline-offset-4"
        >
          Start for free -&gt;
        </NextLink>
        <span className="text-cc-ink-faint mx-3">|</span>
        <a
          href="https://nitro.chillicream.com"
          target="_blank"
          rel="noopener noreferrer"
          className="text-cc-accent hover:text-cc-accent-hover font-medium underline underline-offset-4"
        >
          Launch Nitro -&gt;
        </a>
      </p>
    </header>
  );
}

function ChapterRule({ numeral, kicker }: ChapterRuleProps) {
  return (
    <div className="mx-auto mt-20 max-w-[640px] lg:max-w-none">
      <div className="border-cc-card-border flex items-end justify-between border-t pt-3">
        <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
          <span className="text-cc-accent">{numeral}</span>{" "}
          <span className="text-cc-ink">{kicker}</span>
        </p>
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.2em] uppercase">
          ChilliCream <span className="text-cc-ink-faint">/</span> dispatch no.
          01
        </p>
      </div>
    </div>
  );
}

function ColumnWithMarginalia({
  children,
  marginalia,
}: {
  readonly children: ReactNode;
  readonly marginalia?: readonly string[];
}) {
  return (
    <div className="mx-auto mt-8 grid max-w-[640px] gap-8 lg:max-w-none lg:grid-cols-[1fr_240px] lg:gap-x-12">
      <div className="min-w-0">{children}</div>
      {marginalia && marginalia.length > 0 ? (
        <aside className="text-cc-nav-label hidden font-mono text-[0.7rem] tracking-[0.16em] uppercase lg:block lg:pt-2">
          <ul className="flex flex-col gap-2">
            {marginalia.map((note) => (
              <li key={note}>{note}</li>
            ))}
          </ul>
        </aside>
      ) : (
        <div aria-hidden className="hidden lg:block" />
      )}
    </div>
  );
}

function Move({
  move,
  isFirst,
}: {
  readonly move: MoveProps;
  readonly isFirst: boolean;
}) {
  return (
    <div className="mx-auto grid max-w-[640px] gap-8 lg:max-w-none lg:grid-cols-[1fr_240px] lg:gap-x-12">
      <div
        className={`min-w-0 ${isFirst ? "" : "border-cc-card-border border-t pt-8"}`}
      >
        <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
          {move.eyebrow}
        </p>
        <h3 className="font-heading text-cc-heading text-h5 mt-3 font-semibold">
          {move.title}
        </h3>
        <p className="text-cc-ink text-body mt-4 leading-relaxed">
          {move.body}
        </p>
      </div>
      <aside
        className={`text-cc-nav-label hidden font-mono text-[0.7rem] tracking-[0.16em] uppercase lg:block ${
          isFirst ? "" : "border-cc-card-border border-t pt-8"
        }`}
      >
        <ul className="flex flex-col gap-2">
          {move.marginalia.map((note) => (
            <li key={note}>{note}</li>
          ))}
        </ul>
      </aside>
    </div>
  );
}

function ProductRow({ product }: { readonly product: ProductRowProps }) {
  const arrow = <span aria-hidden>-&gt;</span>;
  const link = product.external ? (
    <a
      href={product.href}
      target="_blank"
      rel="noopener noreferrer"
      className="text-cc-accent hover:text-cc-accent-hover font-medium whitespace-nowrap"
    >
      {product.linkLabel} {arrow}
    </a>
  ) : (
    <NextLink
      href={product.href}
      className="text-cc-accent hover:text-cc-accent-hover font-medium whitespace-nowrap"
    >
      {product.linkLabel} {arrow}
    </NextLink>
  );

  return (
    <div className="border-cc-card-border grid gap-2 border-b py-6 sm:grid-cols-[180px_1fr] sm:gap-x-6">
      <div>
        <p className="font-heading text-cc-heading text-h6 font-semibold">
          {product.name}
        </p>
        <p className="text-cc-nav-label mt-1 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {product.tagline}
        </p>
      </div>
      <p className="text-cc-ink text-body leading-relaxed">
        {product.description} <span className="text-cc-ink-faint">/</span>{" "}
        {link}
      </p>
    </div>
  );
}

function PricingCell({ row }: { readonly row: PricingRowProps }) {
  return (
    <div>
      <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {row.label}
      </dt>
      <dd className="font-heading text-cc-heading mt-2 text-2xl font-semibold">
        {row.price}
      </dd>
      <p className="text-cc-ink-dim mt-1 font-mono text-[0.7rem]">{row.note}</p>
    </div>
  );
}

function LogoBand() {
  return (
    <div className="mx-auto mt-16 max-w-[640px] lg:max-w-none">
      <div className="border-cc-card-border border-t pt-6 text-center">
        <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
          As deployed in production at
        </p>
      </div>
      <div className="border-cc-card-border border-b">
        <LogoCloud />
      </div>
    </div>
  );
}

function BorderlessBlog() {
  return (
    <div className="[&_h2]:font-heading [&_h2]:text-cc-heading [&_h2]:text-h5 [&_a]:no-underline [&_h2]:font-semibold [&_section]:m-0">
      <FromOurBlog />
    </div>
  );
}

function Colophon() {
  return (
    <footer className="mx-auto mt-20 max-w-[640px] lg:max-w-none">
      <div className="border-cc-card-border border-t pt-8 text-right">
        <p className="text-cc-ink text-lead leading-snug">
          Ship your GraphQL platform on .NET.{" "}
          <NextLink
            href="/get-started"
            className="text-cc-accent hover:text-cc-accent-hover font-medium underline underline-offset-4"
          >
            Start for free -&gt;
          </NextLink>{" "}
          <span className="text-cc-ink-faint">/</span>{" "}
          <a
            href="https://nitro.chillicream.com"
            target="_blank"
            rel="noopener noreferrer"
            className="text-cc-accent hover:text-cc-accent-hover font-medium underline underline-offset-4"
          >
            Launch Nitro -&gt;
          </a>
        </p>
      </div>
      <div className="border-cc-card-border mt-10 border-t pt-3">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
          ChilliCream <span className="text-cc-ink-faint">/</span> dispatch ends
        </p>
      </div>
    </footer>
  );
}
