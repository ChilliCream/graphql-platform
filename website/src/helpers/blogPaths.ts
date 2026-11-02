import path from "node:path";
import {
  listCollectionEntries,
  parseDatedStem,
  resolveCollectionFile,
  urlForEntry,
  urlFromCollectionRelPath,
} from "./contentCollection";
import type { ContentCollection, DatedStem } from "./contentCollection";

export const BLOG_ROOT = path.join(process.cwd(), "content/blog");

/** The blog as a content collection: dated stems under /blog. */
export const BLOG_COLLECTION: ContentCollection = {
  root: BLOG_ROOT,
  basePath: "/blog",
  stem: "dated",
  imageBasePath: "/images/blog",
};

export type BlogStem = DatedStem;

export function parseBlogStem(stem: string): BlogStem | null {
  return parseDatedStem(stem);
}

/**
 * Walks the top level of `content/blog/`, validates every entry follows the
 * `YYYY-MM-DD-slug` convention (file or directory), and returns the list
 * of resolvable posts. Throws on the first violation so the build fails.
 */
export function listBlogPosts(): {
  stem: string;
  parsed: BlogStem;
  rel: string;
}[] {
  return listCollectionEntries(BLOG_COLLECTION).flatMap(
    ({ stem, dated, rel }) => (dated ? [{ stem, parsed: dated, rel }] : []),
  );
}

/** Build the canonical URL for a blog post stem. The URL slug mirrors the
 *  markdown file name, e.g. /blog/2019-06-05-hot-chocolate-9. */
export function blogUrlForStem(parsed: BlogStem): string {
  return urlForEntry(
    BLOG_COLLECTION,
    `${parsed.year}-${parsed.month}-${parsed.day}-${parsed.slug}`,
  );
}

/** Reverse the catch-all slug (e.g. ['2019-06-05-hot-chocolate-9']) to a file
 *  path relative to BLOG_ROOT, or null if not found. */
export function resolveBlogFile(slug: string[]): string | null {
  return resolveCollectionFile(BLOG_COLLECTION, slug);
}

/** Given a path relative to BLOG_ROOT (e.g. "2019-06-05-foo/2019-06-05-foo.md"),
 *  produce the canonical blog URL — or null if the path doesn't follow the
 *  blog convention. */
export function blogUrlFromBlogRelPath(rel: string): string | null {
  return urlFromCollectionRelPath(BLOG_COLLECTION, rel);
}
