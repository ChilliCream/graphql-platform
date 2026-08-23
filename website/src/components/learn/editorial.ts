// Read-only editorial adapter for the /learn landing (learn-editorial.md
// section 3, learn-content-strategy.md section 3). Maps blog posts onto the
// five editorial topics. This module only reads the public helpers/types
// those surfaces already export; it does not touch `src/data/learn`,
// `content/blog/`, or any `src/components/Blog*` source file.

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
  // `browseQuery` filters on `product=fusion` only; mocha is deliberately
  // excluded from this browse filter (review note from .10).
  { key: "federation", label: "Federation and Fusion", browseQuery: "product=fusion" },
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

/** Kicker text for a `LearnListRow` (learn-editorial.md section 14.1): the post's category, falling back to its primary topic label. */
export function kickerForBlogPost(post: Pick<BlogPostSummary, "category" | "tags">): string {
  if (post.category) {
    return post.category;
  }
  const topicKey = topicsForBlogPost(post)[0];
  return TOPICS.find((topic) => topic.key === topicKey)?.label ?? "Article";
}

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

/** Most frequent tags across `posts`, ranked by frequency desc then alphabetically, capped at `limit` (section 14.4's "Most popular" rail unit). */
export function popularTags(posts: readonly BlogPostSummary[], limit = 12): string[] {
  const counts = new Map<string, number>();
  for (const post of posts) {
    for (const tag of post.tags) {
      counts.set(tag, (counts.get(tag) ?? 0) + 1);
    }
  }
  return [...counts.entries()]
    .sort(([aTag, aCount], [bTag, bCount]) => (bCount !== aCount ? bCount - aCount : aTag.localeCompare(bTag)))
    .slice(0, limit)
    .map(([tag]) => tag);
}
