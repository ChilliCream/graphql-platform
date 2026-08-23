import path from "node:path";
import type { ProductKey } from "@/src/data/learn/facets";
import { ARTICLES_ROOT, listArticleSlugs } from "./articlePaths";
import { readFrontmatter } from "./readFrontmatter";

/**
 * The editorial article genres (strategy doc section 2); the frontmatter
 * field is `kind`, not `type`, to stay distinct from the catalog's
 * `LearnItem.type` discriminant. `article` is the migrated blog corpus
 * (website-5yo.11); `comparison` and `explainer` are first-party evergreen
 * genres.
 */
export type ArticleKind = "comparison" | "explainer" | "article";

export type ArticleSummary = {
  slug: string;
  href: string;
  kind: ArticleKind;
  title: string;
  description: string | null;
  date: string;
  updated: string | null;
  /** Blog category (Release, Newsletter, AI, ...), `article` kind only; null for comparisons/explainers. */
  category: string | null;
  /**
   * Editorial topic keys from the strategy doc's taxonomy (section 3:
   * `graphql`, `hot-chocolate`, `federation`, `tooling`, `ai`). Carried
   * through frontmatter for the topic rails website-5yo.10 builds; not yet
   * validated against a closed union since that module doesn't exist.
   */
  topics: string[];
  products: ProductKey[];
  tags: string[];
  featuredImage: string | null;
  author: string | null;
  authorUrl: string | null;
  authorImageUrl: string | null;
};

/**
 * Lists all articles under `content/learn/articles/` with their summary
 * metadata. Sorted newest-first by `updated` (falling back to `date`).
 * Throws if an article is missing `kind` or carries an unrecognized value,
 * since a mis-typed `kind` would otherwise render with no top-matter chip.
 */
export function listArticleSummaries(): ArticleSummary[] {
  const articles = listArticleSlugs().map(({ slug, rel }) => {
    const fm = readFrontmatter(path.join(ARTICLES_ROOT, rel)) as Record<string, unknown>;

    if (fm.kind !== "comparison" && fm.kind !== "explainer" && fm.kind !== "article") {
      throw new Error(
        `[articles] "${slug}" has frontmatter kind "${String(fm.kind)}"; expected "comparison", "explainer", or "article".`,
      );
    }
    if (typeof fm.title !== "string" || fm.title.length === 0) {
      throw new Error(`[articles] "${slug}" is missing a frontmatter "title".`);
    }
    if (typeof fm.date !== "string" || fm.date.length === 0) {
      throw new Error(`[articles] "${slug}" is missing a frontmatter "date".`);
    }

    const stringArray = (value: unknown): string[] =>
      Array.isArray(value) ? value.filter((v): v is string => typeof v === "string" && v.length > 0) : [];

    const featuredImageRaw = typeof fm.featuredImage === "string" ? fm.featuredImage : null;

    return {
      slug,
      href: `/learn/articles/${slug}`,
      kind: fm.kind as ArticleKind,
      title: fm.title,
      description: typeof fm.description === "string" && fm.description.length > 0 ? fm.description : null,
      date: fm.date,
      updated: typeof fm.updated === "string" && fm.updated.length > 0 ? fm.updated : null,
      category: typeof fm.category === "string" && fm.category.length > 0 ? fm.category : null,
      topics: stringArray(fm.topics),
      products: stringArray(fm.products) as ProductKey[],
      tags: stringArray(fm.tags),
      featuredImage: resolveFeaturedImage(slug, featuredImageRaw),
      author: typeof fm.author === "string" ? fm.author : null,
      authorUrl: typeof fm.authorUrl === "string" ? fm.authorUrl : null,
      authorImageUrl: typeof fm.authorImageUrl === "string" ? fm.authorImageUrl : null,
    };
  });

  articles.sort((a, b) => {
    const aDate = a.updated ?? a.date;
    const bDate = b.updated ?? b.date;
    return aDate < bDate ? 1 : aDate > bDate ? -1 : 0;
  });
  return articles;
}

export function findArticleSummary(slug: string): ArticleSummary | undefined {
  return listArticleSummaries().find((a) => a.slug === slug);
}

/** Articles of a given kind, e.g. the explainer rail on the `/learn` landing (website-5yo.10). */
export function listArticlesByKind(kind: ArticleKind): ArticleSummary[] {
  return listArticleSummaries().filter((a) => a.kind === kind);
}

function resolveFeaturedImage(slug: string, raw: string | null): string | null {
  if (!raw) {
    return null;
  }
  if (/^(https?:)?\/\//.test(raw) || raw.startsWith("/")) {
    return raw;
  }
  // Co-located image: article images live under /public/images/learn-articles/{slug}/.
  return `/images/learn-articles/${slug}/${raw}`;
}
