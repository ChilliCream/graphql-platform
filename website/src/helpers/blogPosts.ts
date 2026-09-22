import { BLOG_COLLECTION } from "./blogPaths";
import {
  findSimilarArticles,
  listCollectionSummaries,
} from "./contentCollection";
import type { ArticleSummary } from "./contentCollection";

export type BlogPostSummary = ArticleSummary;

/**
 * Lists all blog posts with their summary metadata. Sorted newest-first by
 * the date encoded in the file/directory name (which authors keep in sync
 * with the frontmatter `date`). Posts without a real title in frontmatter
 * fall back to the slug so listings never render blank cards.
 */
export function listBlogPostSummaries(): BlogPostSummary[] {
  return listCollectionSummaries(BLOG_COLLECTION);
}

export function getLatestBlogPost(): BlogPostSummary | null {
  const posts = listBlogPostSummaries();
  return posts.find((p) => p.featuredImage) ?? posts[0] ?? null;
}

/**
 * Ranks other posts by tag overlap with the reference post, breaking ties by
 * `date` desc. Returns at most `limit` posts (default 3), excluding the
 * reference post itself and posts with zero tag overlap.
 */
export function findSimilarPosts(
  reference: BlogPostSummary,
  pool: BlogPostSummary[],
  limit = 3,
): BlogPostSummary[] {
  return findSimilarArticles(reference, pool, limit);
}
