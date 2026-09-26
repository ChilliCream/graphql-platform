import type { BlogTeaserData } from "@/src/components/BlogTeaser";
import { blogUrlForStem } from "@/src/helpers/blogPaths";

/**
 * The copy for the Fusion product page: the hero, the body sections, the
 * feature grid, the featured content teasers, and the closing Nitro band.
 */

export interface CopyLink {
  readonly label: string;
  readonly href: string;
}

/**
 * A titled block of the page copy, where a paragraph's linked phrase appears
 * in `links` with its href and verbatim in one of the paragraphs.
 * `inPractice` holds the links of the trailing "In practice: ..." line,
 * rendered as the labels joined by " and " and closed with a period. A
 * section with `bullets` renders them with the shared `CheckList` instead of
 * a description paragraph; `paragraphs` is empty for those sections.
 */
export interface CopySection {
  readonly id: string;
  readonly title: string;
  readonly paragraphs: readonly string[];
  readonly bullets?: readonly string[];
  readonly links: readonly CopyLink[];
  readonly inPractice: readonly CopyLink[];
}

export const HERO = {
  title: "Fusion",
  teaser:
    "Fusion is a high-performance API gateway for connecting GraphQL, REST, and gRPC APIs at scale. It brings APIs from across your organization together into one coherent graph and executes every request across the services behind it. Fusion natively supports the GraphQL Federation specification and Apollo Federation, so you can federate existing APIs without locking yourself into a single ecosystem.",
  buttons: [
    { label: "Get Started", href: "/docs/fusion/getting-started" },
    {
      label: "Contact an Expert",
      href: "/services/support/contact?subject=Sales&context=Fusion",
    },
  ],
} as const satisfies {
  readonly title: string;
  readonly teaser: string;
  readonly buttons: readonly CopyLink[];
};

export const SECTIONS: readonly CopySection[] = [
  {
    id: "what-is-fusion",
    title: "What is Fusion?",
    paragraphs: [
      "Fusion sits between your clients and the APIs owned by your teams. Each team keeps its own service, technology stack, and schema, while Fusion brings those APIs together into a single coherent graph.",
      "When a client sends a query, Fusion determines which services are needed, coordinates the requests across them, and combines the results into a single response. Those services can expose GraphQL, REST through OpenAPI, or gRPC. They do not need to use the same language, framework, or API technology.",
      "Fusion composes your graph at build time, so schema conflicts and incompatible changes are caught before they reach production. For GraphQL services, Fusion supports both the GraphQL Federation specification and Apollo Federation. If federation is new to you, start with the GraphQL Federation page.",
    ],
    links: [
      {
        label: "the GraphQL Federation page",
        href: "/platform/graphql-federation",
      },
    ],
    inPractice: [],
  },
  {
    id: "performance",
    title: "Performance by design",
    paragraphs: [],
    bullets: [
      "Run your graph on a high-performance router built for low latency and high throughput.",
      "Cache query plans so repeated operations can execute without rebuilding the execution plan.",
      "Deduplicate downstream requests to avoid fetching the same data multiple times during a single operation.",
      "Stream results incrementally with @defer and @stream so clients can receive useful data sooner.",
      "Precompute operation plans at build time so there is no planning overhead at runtime.",
    ],
    links: [],
    inPractice: [],
  },
  {
    id: "security",
    title: "Centralized API security",
    paragraphs: [],
    bullets: [
      "Enforce policies centrally at the gateway before requests reach your services.",
      "Safelist trusted GraphQL operations to reduce unexpected workloads and make API behavior more predictable.",
      "Integrate directly with Open Policy Agent (OPA) for centralized, policy-based authorization.",
      "Plug in custom policy providers when your organization needs its own authorization or governance logic.",
      "Generate an audit trail for every request and persist it in Nitro for security reviews, compliance, and debugging.",
    ],
    links: [],
    inPractice: [],
  },
  {
    id: "insights",
    title: "Deep operational insights",
    paragraphs: [],
    bullets: [
      "Trace every request with full OpenTelemetry support and see how it executes across your graph.",
      "Combine telemetry with Fusion operation plans to understand where time is spent and which services drive latency.",
      "Score request cost using the GraphQL Cost Specification and apply cost-based rate limiting.",
      "Collect cost and usage data in Nitro to understand API consumption and support usage-based billing.",
    ],
    links: [],
    inPractice: [],
  },
  {
    id: "client-safety",
    title: "Protect your clients",
    paragraphs: [
      "Composition ensures your subgraphs work together, but a valid graph can still break a client that depends on a field being changed or removed.",
      "Nitro closes that gap by comparing every schema change against the operations used by your clients. Teams can see whether a change is safe, risky, or breaking before it reaches production.",
    ],
    links: [],
    inPractice: [
      { label: "schema governance in Nitro", href: "/products/nitro#schema" },
    ],
  },
];

export const FEATURED_CONTENT: readonly BlogTeaserData[] = [
  {
    href: "/docs/fusion/getting-started",
    title: "Get started with Fusion",
    description: "Build your first federated graph and run it locally.",
    category: "Tutorial",
    featuredImage: null,
  },
  {
    href: "/docs/fusion/connectors/apollofederation",
    title: "Switch your gateway, keep your graph",
    description:
      "Move from Apollo Router, Hive Gateway, or Cosmo to Fusion while keeping the subgraphs and schemas you already have.",
    category: "Guide",
    featuredImage: null,
  },
  {
    href: blogUrlForStem({
      year: "2026",
      month: "07",
      day: "12",
      slug: "fusion-16-5",
    }),
    title: "The Gateway for Everyone",
    description:
      "Learn why Fusion supports both GraphQL Federation and Apollo Federation, and how it brings GraphQL, REST, and gRPC into one gateway.",
    category: "Blog",
    featuredImage: null,
  },
];

export const CLOSING_BAND = {
  id: "get-started",
  title: "Bring your APIs together with Fusion",
  description:
    "Connect GraphQL, REST, and gRPC APIs behind one high-performance gateway and evolve your graph without locking your teams into a single federation ecosystem.",
  buttons: HERO.buttons,
} as const satisfies {
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly buttons: readonly CopyLink[];
};

export interface Feature {
  readonly title: string;
  readonly description: string;
}

export const FEATURES: readonly Feature[] = [
  {
    title: "Connect every API",
    description:
      "Bring GraphQL, REST, and gRPC services into the same graph and validate their contracts together during composition.",
  },
  {
    title: "Keep your existing stack",
    description:
      "Use the languages, frameworks, and servers your teams already know. Fusion does not require a distributed runtime or vendor-specific package inside your services.",
  },
  {
    title: "Adopt without disruption",
    description:
      "Run GraphQL Federation and Apollo Federation side by side, and migrate one subgraph at a time without requiring a coordinated cutover across teams.",
  },
  {
    title: "Performance by design",
    description:
      "Precompute operation plans, cache execution plans, deduplicate downstream requests, and stream results incrementally with @defer and @stream.",
  },
  {
    title: "Centralize API security",
    description:
      "Enforce policies at the gateway, integrate with Open Policy Agent or custom policy providers, and keep unauthorized traffic away from your services.",
  },
  {
    title: "See what happens in production",
    description:
      "Trace requests across your graph with OpenTelemetry, understand execution through operation plans, and use Nitro to analyze cost, usage, and performance.",
  },
];
