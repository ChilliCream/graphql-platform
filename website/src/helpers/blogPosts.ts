import type { ProductKey } from "@/src/data/learn/facets";
import { listArticleSummaries, type ArticleSummary } from "./articles";

export type BlogPostSummary = {
  stem: string;
  href: string;
  title: string;
  description: string | null;
  date: string;
  category: string | null;
  tags: string[];
  products: ProductKey[];
  featuredImage: string | null;
  author: string | null;
  authorUrl: string | null;
  authorImageUrl: string | null;
};

function toBlogPostSummary(article: ArticleSummary): BlogPostSummary {
  return {
    stem: article.slug,
    href: article.href,
    title: article.title,
    description: article.description,
    date: article.date,
    category: article.category,
    tags: article.tags,
    products: article.products,
    featuredImage: article.featuredImage,
    author: article.author,
    authorUrl: article.authorUrl,
    authorImageUrl: article.authorImageUrl,
  };
}

/**
 * Lists all blog posts with their summary metadata. Sorted newest-first by
 * `date`. Posts live under `content/learn/articles/` as `kind: article`
 * entries (website-5yo.11 migration) and are read through the shared article
 * pipeline (`src/helpers/articles.ts`), reshaped into the `BlogPostSummary`
 * shape every existing caller (header nav, the /learn landing rails, RSS)
 * already expects.
 */
export function listBlogPostSummaries(): BlogPostSummary[] {
  return listArticleSummaries()
    .filter((a) => a.kind === "article")
    .map(toBlogPostSummary);
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
export function findSimilarPosts(reference: BlogPostSummary, pool: BlogPostSummary[], limit = 3): BlogPostSummary[] {
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
      return { post: p, overlap };
    })
    .filter((s) => s.overlap > 0);

  scored.sort((a, b) => {
    if (b.overlap !== a.overlap) {
      return b.overlap - a.overlap;
    }
    return a.post.date < b.post.date ? 1 : a.post.date > b.post.date ? -1 : 0;
  });

  return scored.slice(0, limit).map((s) => s.post);
}
