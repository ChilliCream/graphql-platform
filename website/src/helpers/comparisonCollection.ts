import fs from "node:fs";
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
const DOCS_ROOT = path.join(process.cwd(), "content/docs");

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

/** One of the things a comparison compares, e.g. a product or a pattern. */
export type ComparisonSubject = {
  readonly name: string;
  /** Root-relative path or https:// URL of the subject, or null. */
  readonly url: string | null;
};

export type ComparisonFaqItem = {
  readonly question: string;
  readonly answer: string;
};

export type ComparisonLink = {
  readonly label: string;
  /** Root-relative path or https:// URL. */
  readonly href: string;
};

/** The closing call to action of a comparison. */
export type ComparisonCallToAction = {
  readonly title: string;
  readonly text: string;
  readonly primary: ComparisonLink;
  readonly secondary: ComparisonLink;
  /** Label of the link to the contact page, e.g. "Talk to us about your migration". */
  readonly contact: string;
};

/**
 * Reads the optional `about`, `faq`, and `cta` frontmatter of a comparison.
 * Throws on the first invalid entry so the build fails.
 */
export function readComparisonDetails(
  frontmatter: Record<string, unknown>,
  stem: string,
): {
  about: ComparisonSubject[];
  faq: ComparisonFaqItem[];
  cta: ComparisonCallToAction | null;
} {
  return {
    cta: readCallToAction(frontmatter, stem),
    about: readList(frontmatter, "about", stem, (item, at) => {
      const url = optionalString(item, "url", stem, at);
      if (url !== null && !url.startsWith("/") && !url.startsWith("https://")) {
        invalid(
          stem,
          `"${at}url" must be a root-relative path or https:// URL.`,
        );
      }
      return { name: requiredString(item, "name", stem, at), url };
    }),
    faq: readList(frontmatter, "faq", stem, (item, at) => ({
      question: requiredString(item, "question", stem, at),
      answer: requiredString(item, "answer", stem, at),
    })),
  };
}

function readCallToAction(
  frontmatter: Record<string, unknown>,
  stem: string,
): ComparisonCallToAction | null {
  const value = frontmatter.cta;
  if (value === undefined || value === null) {
    return null;
  }
  if (!isRecord(value)) {
    return invalid(stem, `"cta" must be an object.`);
  }
  return {
    title: requiredString(value, "title", stem, "cta."),
    text: requiredString(value, "text", stem, "cta."),
    primary: readLink(value, "primary", stem),
    secondary: readLink(value, "secondary", stem),
    contact: requiredString(value, "contact", stem, "cta."),
  };
}

function readLink(
  source: Record<string, unknown>,
  key: string,
  stem: string,
): ComparisonLink {
  const value = source[key];
  const at = `cta.${key}.`;
  if (!isRecord(value)) {
    return invalid(stem, `"cta.${key}" must be an object.`);
  }
  const href = requiredString(value, "href", stem, at);
  if (!href.startsWith("/") && !href.startsWith("https://")) {
    invalid(stem, `"${at}href" must be a root-relative path or https:// URL.`);
  }
  if (href.startsWith("/docs/") && !docsPageExists(href)) {
    invalid(stem, `"${at}href" points to a missing docs page: ${href}.`);
  }
  return { label: requiredString(value, "label", stem, at), href };
}

function docsPageExists(href: string): boolean {
  const rel = href.slice("/docs/".length).split(/[#?]/, 1)[0];
  return [`${rel}.md`, `${rel}/index.md`].some((candidate) =>
    fs.existsSync(path.join(DOCS_ROOT, candidate)),
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function readList<T>(
  source: Record<string, unknown>,
  key: string,
  stem: string,
  read: (item: Record<string, unknown>, at: string) => T,
): T[] {
  const value = source[key];
  if (value === undefined || value === null) {
    return [];
  }
  if (!Array.isArray(value)) {
    return invalid(stem, `"${key}" must be a list.`);
  }
  return value.map((item: unknown, index) => {
    if (!isRecord(item)) {
      return invalid(stem, `"${key}[${index}]" must be an object.`);
    }
    return read(item, `${key}[${index}].`);
  });
}

function requiredString(
  source: Record<string, unknown>,
  key: string,
  stem: string,
  at: string,
): string {
  return (
    optionalString(source, key, stem, at) ??
    invalid(stem, `"${at}${key}" is required.`)
  );
}

function optionalString(
  source: Record<string, unknown>,
  key: string,
  stem: string,
  at: string,
): string | null {
  const value = source[key];
  if (value === undefined || value === null) {
    return null;
  }
  if (typeof value !== "string" || value.trim().length === 0) {
    return invalid(stem, `"${at}${key}" must be a non-empty string.`);
  }
  return value.trim();
}

function invalid(stem: string, message: string): never {
  throw new Error(
    `[comparison] Invalid frontmatter in "content/comparison/${stem}": ${message}`,
  );
}
