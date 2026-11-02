import fs from "node:fs";
import path from "node:path";
import { findAuthorProfile } from "@/src/data/authors";
import { readFrontmatter } from "./readFrontmatter";
import type { AuthorProfile } from "@/src/data/authors";

/**
 * Describes one markdown article collection on disk (the blog, the comparison
 * section, ...). Everything the listing/URL/summary helpers below need to walk
 * a collection lives here, so a new section is a descriptor rather than a copy
 * of the pipeline.
 */
export type ContentCollection = {
  /** Absolute path of the directory holding the articles. */
  readonly root: string;
  /** URL prefix every article of the collection lives under, e.g. `/blog`. */
  readonly basePath: string;
  /**
   * File/directory naming convention: `dated` stems are `YYYY-MM-DD-slug`
   * (the date is part of the URL), `plain` stems are just `slug`.
   */
  readonly stem: "dated" | "plain";
  /** Root-relative directory co-located images resolve against. */
  readonly imageBasePath: string;
};

export type DatedStem = {
  year: string;
  month: string;
  day: string;
  slug: string;
};

export type CollectionEntry = {
  /** File/directory name without extension, e.g. `2019-06-05-hot-chocolate-9`. */
  stem: string;
  /** Stem without the date prefix for dated collections; the stem otherwise. */
  slug: string;
  /** Parsed date prefix, or null in a `plain` collection. */
  dated: DatedStem | null;
  /** Path of the markdown file relative to the collection root. */
  rel: string;
};

export type ArticleSummary = {
  stem: string;
  href: string;
  title: string;
  description: string | null;
  date: string;
  category: string | null;
  tags: string[];
  featuredImage: string | null;
  author: string | null;
  authorUrl: string | null;
  authorImageUrl: string | null;
  authorProfile: AuthorProfile | null;
};

const DATED_STEM_RE = /^(\d{4})-(\d{2})-(\d{2})-(.+)$/;

export function parseDatedStem(stem: string): DatedStem | null {
  const match = DATED_STEM_RE.exec(stem);
  if (!match) {
    return null;
  }
  const [, year, month, day, slug] = match;
  return { year, month, day, slug };
}

/**
 * Splits a stem according to the collection's convention, or returns null when
 * the stem does not follow it (a `dated` collection rejects undated stems; a
 * `plain` collection only rejects the empty stem).
 */
export function parseCollectionStem(
  collection: ContentCollection,
  stem: string,
): { slug: string; dated: DatedStem | null } | null {
  if (collection.stem === "dated") {
    const dated = parseDatedStem(stem);
    return dated ? { slug: dated.slug, dated } : null;
  }
  return stem.length > 0 ? { slug: stem, dated: null } : null;
}

/**
 * Walks the top level of the collection root, validates every entry follows the
 * collection's naming convention (file or directory), and returns the list of
 * resolvable articles. Throws on the first violation so the build fails.
 */
export function listCollectionEntries(
  collection: ContentCollection,
): CollectionEntry[] {
  if (!fs.existsSync(collection.root)) {
    return [];
  }
  const entries = fs.readdirSync(collection.root, { withFileTypes: true });
  const articles: CollectionEntry[] = [];

  for (const entry of entries) {
    if (entry.isDirectory()) {
      const parsed = parseCollectionStem(collection, entry.name);
      if (!parsed) {
        // non-article directory at the root (e.g. "shared" assets) is allowed
        continue;
      }
      const candidate = ["md", "mdx"]
        .map((ext) => `${entry.name}/${entry.name}.${ext}`)
        .find((rel) => fs.existsSync(path.join(collection.root, rel)));
      if (!candidate) {
        throw new Error(
          `[contentCollection] Directory "${entry.name}" under ` +
            `${collection.basePath} is missing the matching ` +
            `${entry.name}.md(x) file inside it.`,
        );
      }
      articles.push({
        stem: entry.name,
        slug: parsed.slug,
        dated: parsed.dated,
        rel: candidate,
      });
      continue;
    }

    if (!entry.isFile()) {
      continue;
    }

    const fileMatch = entry.name.match(/^(.+)\.(mdx?)$/i);
    if (!fileMatch) {
      // non-markdown file at the root (e.g. images) is allowed
      continue;
    }
    const stem = fileMatch[1];
    const parsed = parseCollectionStem(collection, stem);
    if (!parsed) {
      throw new Error(
        `[contentCollection] Invalid ${collection.basePath} file ` +
          `"${entry.name}". Expected name format ` +
          `${collection.stem === "dated" ? "YYYY-MM-DD-slug" : "slug"}.md(x).`,
      );
    }
    articles.push({
      stem,
      slug: parsed.slug,
      dated: parsed.dated,
      rel: entry.name,
    });
  }

  return articles;
}

/**
 * Canonical URL of an article. The URL slug mirrors the markdown file name, so
 * a dated stem keeps its date, e.g. /blog/2019-06-05-hot-chocolate-9.
 */
export function urlForEntry(
  collection: ContentCollection,
  stem: string,
): string {
  return `${collection.basePath}/${stem}`;
}

/**
 * Reverse the catch-all slug (e.g. ['2019-06-05-hot-chocolate-9']) to a file
 * path relative to the collection root, or null if not found.
 */
export function resolveCollectionFile(
  collection: ContentCollection,
  slug: string[],
): string | null {
  if (slug.length !== 1) {
    return null;
  }
  const stem = slug[0];
  if (!parseCollectionStem(collection, stem)) {
    return null;
  }
  const candidates = [
    `${stem}.md`,
    `${stem}.mdx`,
    `${stem}/${stem}.md`,
    `${stem}/${stem}.mdx`,
  ];
  return (
    candidates.find((c) => fs.existsSync(path.join(collection.root, c))) ?? null
  );
}

/**
 * Given a path relative to the collection root (e.g.
 * "2019-06-05-foo/2019-06-05-foo.md"), produce the canonical URL — or null if
 * the path doesn't follow the collection's convention.
 */
export function urlFromCollectionRelPath(
  collection: ContentCollection,
  rel: string,
): string | null {
  const cleanRel = rel.replace(/\.mdx?$/i, "");
  const segments = cleanRel.split("/");
  if (!parseCollectionStem(collection, segments[0])) {
    return null;
  }
  return urlForEntry(collection, segments[0]);
}

/**
 * Lists all articles of a collection with their summary metadata. Sorted
 * newest-first by `date`. Articles without a real title in frontmatter fall
 * back to the slug so listings never render blank cards.
 */
export function listCollectionSummaries(
  collection: ContentCollection,
): ArticleSummary[] {
  const articles = listCollectionEntries(collection)
    .filter(({ slug }) => slug !== "__empty__")
    .map(({ stem, slug, dated, rel }) => {
      const fm = readFrontmatter(path.join(collection.root, rel)) as Record<
        string,
        unknown
      >;
      const tags = Array.isArray(fm.tags)
        ? (fm.tags as unknown[]).filter(
            (t): t is string => typeof t === "string" && t.length > 0,
          )
        : [];
      const featuredImageRaw =
        typeof fm.featuredImage === "string" ? fm.featuredImage : null;
      return {
        stem,
        href: urlForEntry(collection, stem),
        title:
          typeof fm.title === "string" && fm.title.length > 0 ? fm.title : slug,
        description:
          typeof fm.description === "string" && fm.description.length > 0
            ? fm.description
            : null,
        date: resolveDate(collection, stem, dated, fm.date),
        category:
          typeof fm.category === "string" && fm.category.length > 0
            ? fm.category
            : null,
        tags,
        featuredImage: resolveFeaturedImage(collection, stem, featuredImageRaw),
        author: typeof fm.author === "string" ? fm.author : null,
        authorUrl: typeof fm.authorUrl === "string" ? fm.authorUrl : null,
        authorImageUrl:
          typeof fm.authorImageUrl === "string" ? fm.authorImageUrl : null,
        authorProfile: findAuthorProfile(
          typeof fm.author === "string" ? fm.author : null,
          typeof fm.authorUrl === "string" ? fm.authorUrl : null,
        ),
      };
    });

  articles.sort((a, b) => (a.date < b.date ? 1 : a.date > b.date ? -1 : 0));
  return articles;
}

/**
 * The frontmatter `date` wins; a dated stem provides the fallback. Undated
 * collections have no fallback, so a missing `date` fails the build the same
 * way a malformed dated stem does.
 */
function resolveDate(
  collection: ContentCollection,
  stem: string,
  dated: DatedStem | null,
  raw: unknown,
): string {
  if (typeof raw === "string" && raw.length > 0) {
    return raw;
  }
  if (dated) {
    return `${dated.year}-${dated.month}-${dated.day}`;
  }
  throw new Error(
    `[contentCollection] Article "${stem}" under ${collection.basePath} is ` +
      `missing the frontmatter "date". Undated stems carry no date, so the ` +
      `frontmatter must provide one (YYYY-MM-DD).`,
  );
}

function resolveFeaturedImage(
  collection: ContentCollection,
  stem: string,
  raw: string | null,
): string | null {
  if (!raw) {
    return null;
  }
  // Absolute or root-relative URL: trust as-is.
  if (/^(https?:)?\/\//.test(raw) || raw.startsWith("/")) {
    return raw;
  }
  // Co-located image: images live under /public{imageBasePath}/{stem}/.
  return `${collection.imageBasePath}/${stem}/${raw}`;
}

/**
 * Ranks other articles by tag overlap with the reference article, breaking ties
 * by `date` desc. Returns at most `limit` articles (default 3), excluding the
 * reference article itself and articles with zero tag overlap.
 */
export function findSimilarArticles(
  reference: ArticleSummary,
  pool: ArticleSummary[],
  limit = 3,
): ArticleSummary[] {
  const referenceTags = new Set(reference.tags);
  if (referenceTags.size === 0) {
    return [];
  }

  const scored = pool
    .filter((p) => p.stem !== reference.stem)
    .map((p) => {
      let overlap = 0;
      for (const tag of p.tags) {
        if (referenceTags.has(tag)) {
          overlap++;
        }
      }
      return { article: p, overlap };
    })
    .filter((s) => s.overlap > 0);

  scored.sort((a, b) => {
    if (b.overlap !== a.overlap) {
      return b.overlap - a.overlap;
    }
    return a.article.date < b.article.date
      ? 1
      : a.article.date > b.article.date
        ? -1
        : 0;
  });

  return scored.slice(0, limit).map((s) => s.article);
}
