// Read-only editorial adapter for the /learn landing (learn-editorial.md
// section 3, learn-content-strategy.md section 3). Maps blog posts and
// catalog items onto the five editorial topics and reshapes a
// `BlogPostSummary` into the plain `BlogTeaserData` shape `BlogTeaser`
// expects. This module only reads the public helpers/types those surfaces
// already export; it does not touch `src/data/learn`, `content/blog/`, or
// any `src/components/Blog*` source file.

import type { BlogTeaserData } from "@/src/components/BlogTeaser";
import type { ProductKey } from "@/src/data/learn/facets";
import type { LearnItemSummary } from "@/src/data/learn/types";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";

export type TopicKey = "graphql" | "hot-chocolate" | "federation" | "tooling" | "ai";

export interface Topic {
  readonly key: TopicKey;
  readonly label: string;
  /**
   * Preapplied `/learn/browse` filter query (no leading "?"). `null` when no
   * catalog product axis maps cleanly onto the topic (graphql and ai are
   * cross-cutting subjects, not tied to one product), in which case the
   * topic links at the unfiltered catalog until `/learn/topics/[topic]`
   * exists (learn-editorial.md section 3.1).
   */
  readonly browseQuery: string | null;
}

export const TOPICS: readonly Topic[] = [
  { key: "graphql", label: "GraphQL fundamentals", browseQuery: null },
  { key: "hot-chocolate", label: "Hot Chocolate", browseQuery: "product=hot-chocolate" },
  { key: "federation", label: "Federation and Fusion", browseQuery: "product=fusion,mocha" },
  { key: "tooling", label: "Tooling and observability", browseQuery: "product=nitro" },
  { key: "ai", label: "AI and agents", browseQuery: null },
];

export const topicBrowseHref = (topic: Topic): string =>
  topic.browseQuery ? `/learn/browse?${topic.browseQuery}` : "/learn/browse";

// Tag -> topic, transcribed from learn-content-strategy.md section 3's
// mapping table. `products`, `workshops`, and `release` are metadata tags
// (content type / stream), not subjects, so they are intentionally absent.
const TAG_TOPIC: Record<string, TopicKey> = {
  graphql: "graphql",
  directives: "graphql",
  deprecation: "graphql",
  community: "graphql",
  graphqlconf: "graphql",
  api: "graphql",
  hotchocolate: "hot-chocolate",
  dotnet: "hot-chocolate",
  aspnetcore: "hot-chocolate",
  fusion: "federation",
  federation: "federation",
  "apollo-federation": "federation",
  "micro-services": "federation",
  subscriptions: "federation",
  "event-streams": "federation",
  nitro: "tooling",
  bananacakepop: "tooling",
  ide: "tooling",
  cloud: "tooling",
  telemetry: "tooling",
  "open-telemetry": "tooling",
  logging: "tooling",
  openapi: "tooling",
  rest: "tooling",
  ai: "ai",
  llm: "ai",
  mcp: "ai",
  agents: "ai",
  "semantic-introspection": "ai",
};

const PRODUCT_TOPIC: Record<ProductKey, TopicKey> = {
  "hot-chocolate": "hot-chocolate",
  "strawberry-shake": "hot-chocolate",
  fusion: "federation",
  mocha: "federation",
  nitro: "tooling",
};

/** Topics a blog post belongs to, derived from its tags plus the `AI` category as a fallback (strategy section 3). */
export function topicsForBlogPost(post: Pick<BlogPostSummary, "tags" | "category">): readonly TopicKey[] {
  const keys = new Set<TopicKey>();
  for (const tag of post.tags) {
    const topic = TAG_TOPIC[tag];
    if (topic) {
      keys.add(topic);
    }
  }
  if (post.category === "AI") {
    keys.add("ai");
  }
  return [...keys];
}

/** Topics a catalog item belongs to, derived from its product mix (strategy section 3). */
export function topicsForLearnItem(item: Pick<LearnItemSummary, "products">): readonly TopicKey[] {
  const keys = new Set<TopicKey>();
  for (const product of item.products) {
    const topic = PRODUCT_TOPIC[product];
    if (topic) {
      keys.add(topic);
    }
  }
  return [...keys];
}

/**
 * Read-only adapter from a `BlogPostSummary` (as returned by
 * `listBlogPostSummaries()`/`getLatestBlogPost()`) to the plain data shape
 * `BlogTeaser` renders. No blog file is read or modified here; this only
 * reshapes an already-computed summary.
 */
export function toBlogTeaserData(post: BlogPostSummary): BlogTeaserData {
  return {
    href: post.href,
    title: post.title,
    date: post.date,
    featuredImage: post.featuredImage,
    category: post.category,
    description: post.description,
    author: post.author,
    authorImageUrl: post.authorImageUrl,
  };
}
