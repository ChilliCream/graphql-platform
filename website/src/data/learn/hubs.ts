// Canonical topic-hub taxonomy for /learn/topics/[slug] (website-kbx.3).
//
// Four hubs, ruled by the user 2026-08-23 to replace the Hot Chocolate /
// Fusion / Nitro product links in the promoted subnav: GraphQL & Federation,
// Messaging, Tooling & Observability, Agents. Products stay a browse facet
// (PRODUCT_OPTIONS in ./facets); hubs are the broader editorial axis every
// catalog item, article, and video is mapped onto here.
//
// `PRODUCT_HUB` is the primary mapping: hot-chocolate, fusion, and
// strawberry-shake are all GraphQL-server/client technology, so they fold
// into one hub rather than the three separate product links the old subnav
// gave them. mocha maps to Messaging, nitro to Tooling & Observability. This
// merge is also what fixes the Green Donut/DataLoader-performance mapping
// gap hnm.2's review flagged: those videos carry only the `hot-chocolate`
// product (no dedicated ProductKey exists for Green Donut, a Hot Chocolate
// sub-library, and none should: it is not a top-level product per
// src/data/products.ts). Under the old 5-topic split (graphql vs.
// hot-chocolate vs. federation) that one product tag was ambiguous; folded
// into a single GraphQL & Federation hub, it maps unambiguously.
//
// `LearnItemBase.hubs` (see ./types) is the escape hatch for content whose
// real subject a product tag alone cannot express, e.g. an OpenTelemetry
// video tagged only `hot-chocolate` that is really Tooling & Observability
// content. Set it explicitly there rather than guessing from title text.

import type { ProductKey, UseCaseKey } from "./facets";

export type HubKey = "graphql-federation" | "messaging" | "tooling-observability" | "agents";

export interface Hub {
  readonly key: HubKey;
  readonly label: string;
  readonly tagline: string;
  readonly description: string;
  /** `/learn/browse` link pre-filtered to this hub's primary product facet. */
  readonly browseHref: string;
}

export const HUBS: readonly Hub[] = [
  {
    key: "graphql-federation",
    label: "GraphQL & Federation",
    tagline: "Build and compose GraphQL APIs",
    description:
      "Server fundamentals with Hot Chocolate, composite schemas and gateways with Fusion, and GraphQL clients: schema design, resolvers, DataLoader and performance, subscriptions, and federation.",
    browseHref: "/learn/browse?product=hot-chocolate",
  },
  {
    key: "messaging",
    label: "Messaging",
    tagline: "Message-driven services with Mocha",
    description: "Message buses, sagas, and the transactional outbox pattern for services that talk asynchronously.",
    browseHref: "/learn/browse?product=mocha",
  },
  {
    key: "tooling-observability",
    label: "Tooling & Observability",
    tagline: "Ship, monitor, and govern your APIs",
    description:
      "Nitro's IDE and cloud tooling, OpenTelemetry and logging, and the OpenAPI/REST adapters that put a GraphQL API in front of every client.",
    browseHref: "/learn/browse?product=nitro",
  },
  {
    key: "agents",
    label: "Agents",
    tagline: "GraphQL APIs for LLMs and agent tooling",
    description: "MCP, semantic introspection, and building agent-ready APIs that LLMs and autonomous tools can use.",
    browseHref: "/learn/browse?type=template&use=llm-mcp",
  },
];

export const findHub = (key: string): Hub | undefined => HUBS.find((hub) => hub.key === key);

export const hubHref = (key: HubKey): string => `/learn/topics/${key}`;

/** Product -> hub, the primary signal for catalog items (templates, videos, tutorials, examples, workshops). */
const PRODUCT_HUB: Partial<Record<ProductKey, HubKey>> = {
  "hot-chocolate": "graphql-federation",
  fusion: "graphql-federation",
  "strawberry-shake": "graphql-federation",
  mocha: "messaging",
  nitro: "tooling-observability",
};

/** Minimal shape `hubsForLearnItem` needs: every `LearnItem`/`LearnItemSummary` variant satisfies it. */
interface HubbableLearnItem {
  readonly products: readonly ProductKey[];
  readonly hubs?: readonly HubKey[];
  readonly type?: string;
  readonly useCases?: readonly UseCaseKey[];
}

/**
 * Every hub a catalog item belongs to, most-relevant first: an explicit
 * `hubs` override, then the Agents override for LLM/MCP templates (no
 * `ProductKey` represents "agent tooling"), then one hub per matching
 * `products` entry. Always non-empty for the current catalog: every seeded
 * item carries at least one of hot-chocolate/fusion/strawberry-shake/mocha/nitro.
 */
export function hubsForLearnItem(item: HubbableLearnItem): readonly HubKey[] {
  const keys = new Set<HubKey>();
  for (const hub of item.hubs ?? []) {
    keys.add(hub);
  }
  if (item.type === "template" && item.useCases?.includes("llm-mcp")) {
    keys.add("agents");
  }
  for (const product of item.products) {
    const hub = PRODUCT_HUB[product];
    if (hub) {
      keys.add(hub);
    }
  }
  return [...keys];
}

/** The single hub a catalog item is chiefly about: `hubsForLearnItem(item)[0]`. */
export function primaryHubForLearnItem(item: HubbableLearnItem): HubKey | undefined {
  return hubsForLearnItem(item)[0];
}

// Blog/article tag -> hub. Every tag from docs/design/learn-content-strategy.md
// section 1.1's frequency table is covered; `products`, `workshops`, and
// `release` are metadata tags (content type / stream), not subjects, and are
// intentionally absent, matching src/components/learn/editorial.ts's
// TAG_TOPIC precedent. No tag maps to `messaging`: Mocha content is
// currently catalog-only (no article has shipped yet), so the hub's Latest
// rail legitimately has nothing to show and is omitted rather than
// populated with an unrelated tag.
const TAG_HUB: Record<string, HubKey> = {
  graphql: "graphql-federation",
  directives: "graphql-federation",
  deprecation: "graphql-federation",
  community: "graphql-federation",
  graphqlconf: "graphql-federation",
  api: "graphql-federation",
  hotchocolate: "graphql-federation",
  dotnet: "graphql-federation",
  aspnetcore: "graphql-federation",
  fusion: "graphql-federation",
  federation: "graphql-federation",
  "apollo-federation": "graphql-federation",
  "micro-services": "graphql-federation",
  subscriptions: "graphql-federation",
  "event-streams": "graphql-federation",
  nitro: "tooling-observability",
  bananacakepop: "tooling-observability",
  ide: "tooling-observability",
  cloud: "tooling-observability",
  telemetry: "tooling-observability",
  "open-telemetry": "tooling-observability",
  logging: "tooling-observability",
  openapi: "tooling-observability",
  rest: "tooling-observability",
  ai: "agents",
  llm: "agents",
  mcp: "agents",
  agents: "agents",
  "semantic-introspection": "agents",
};

interface HubbablePost {
  readonly tags: readonly string[];
  readonly category: string | null;
}

/** Every hub a blog/article post belongs to, derived from its tags plus the `AI` category as a fallback. */
export function hubsForPost(post: HubbablePost): readonly HubKey[] {
  const keys = new Set<HubKey>();
  for (const tag of post.tags) {
    const hub = TAG_HUB[tag];
    if (hub) {
      keys.add(hub);
    }
  }
  if (post.category === "AI") {
    keys.add("agents");
  }
  return [...keys];
}

/**
 * Link target for a post's kicker (learn-editorial.md section 14.1):
 * mirrors `kickerForBlogPost`'s own precedence exactly (category verbatim,
 * falling back to the tag-derived hub label) so the link always matches the
 * text it sits under. `Release`/`Newsletter` and any other non-`AI`
 * category name no hub, so those kickers render as plain text; `AI` is the
 * one category that is also a hub name. `undefined` when no hub applies, so
 * the caller renders the kicker as plain text instead of a link.
 */
export function hubHrefForPost(post: HubbablePost): string | undefined {
  if (post.category) {
    return post.category === "AI" ? hubHref("agents") : undefined;
  }
  const hub = hubsForPost(post)[0];
  return hub ? hubHref(hub) : undefined;
}
