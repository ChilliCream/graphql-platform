import path from "node:path";
import {
  findSimilarArticles,
  listCollectionEntries,
  listCollectionSummaries,
  resolveCollectionFile,
} from "./contentCollection";
import type {
  ArticleSummary,
  CollectionEntry,
  ContentCollection,
} from "./contentCollection";

export const COMPARISON_ROOT = path.join(process.cwd(), "content/comparison");

/** The comparison section as a content collection: undated stems under /comparison. */
export const COMPARISON_COLLECTION: ContentCollection = {
  root: COMPARISON_ROOT,
  basePath: "/comparison",
  stem: "plain",
  imageBasePath: "/images/comparison",
};

/**
 * Walks the top level of `content/comparison/`, validates every entry resolves
 * to a markdown file, and returns the list of articles. Throws on the first
 * violation so the build fails.
 */
export function listComparisons(): CollectionEntry[] {
  return listCollectionEntries(COMPARISON_COLLECTION);
}

/** Reverse the catch-all slug (e.g. ['vs-apollo-federation']) to a file path
 *  relative to COMPARISON_ROOT, or null if not found. */
export function resolveComparisonFile(slug: string[]): string | null {
  return resolveCollectionFile(COMPARISON_COLLECTION, slug);
}

/**
 * Lists all comparison articles with their summary metadata, newest-first by
 * the frontmatter `date` (undated stems carry no date of their own).
 */
export function listComparisonSummaries(): ArticleSummary[] {
  return listCollectionSummaries(COMPARISON_COLLECTION);
}

/**
 * Ranks other comparisons by tag overlap with the reference article, breaking
 * ties by `date` desc. Returns at most `limit` articles (default 3).
 */
export function findSimilarComparisons(
  reference: ArticleSummary,
  pool: ArticleSummary[],
  limit = 3,
): ArticleSummary[] {
  return findSimilarArticles(reference, pool, limit);
}
