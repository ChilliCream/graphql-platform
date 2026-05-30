import path from "node:path";
import { BLOG_ROOT, blogUrlForStem, listBlogPosts } from "./blogPaths";
import { readFrontmatter } from "./readFrontmatter";

export type BlogPostSummary = {
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
};

/**
 * Lists all blog posts with their summary metadata. Sorted newest-first by
 * the date encoded in the file/directory name (which authors keep in sync
 * with the frontmatter `date`). Posts without a real title in frontmatter
 * fall back to the slug so listings never render blank cards.
 */
export function listBlogPostSummaries(): BlogPostSummary[] {
  const posts = listBlogPosts()
    .filter(({ parsed }) => parsed.slug !== "__empty__")
    .map(({ stem, parsed, rel }) => {
      const fm = readFrontmatter(path.join(BLOG_ROOT, rel)) as Record<
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
        href: blogUrlForStem(parsed),
        title:
          typeof fm.title === "string" && fm.title.length > 0
            ? fm.title
            : parsed.slug,
        description:
          typeof fm.description === "string" && fm.description.length > 0
            ? fm.description
            : null,
        date:
          typeof fm.date === "string" && fm.date.length > 0
            ? fm.date
            : `${parsed.year}-${parsed.month}-${parsed.day}`,
        category:
          typeof fm.category === "string" && fm.category.length > 0
            ? fm.category
            : null,
        tags,
        featuredImage: resolveFeaturedImage(stem, featuredImageRaw),
        author: typeof fm.author === "string" ? fm.author : null,
        authorUrl: typeof fm.authorUrl === "string" ? fm.authorUrl : null,
        authorImageUrl:
          typeof fm.authorImageUrl === "string" ? fm.authorImageUrl : null,
      };
    });

  posts.sort((a, b) => (a.date < b.date ? 1 : a.date > b.date ? -1 : 0));
  return posts;
}

export function getLatestBlogPost(): BlogPostSummary | null {
  const posts = listBlogPostSummaries();
  return posts.find((p) => p.featuredImage) ?? posts[0] ?? null;
}

function resolveFeaturedImage(stem: string, raw: string | null): string | null {
  if (!raw) {
    return null;
  }
  // Absolute or root-relative URL: trust as-is.
  if (/^(https?:)?\/\//.test(raw) || raw.startsWith("/")) {
    return raw;
  }
  // Co-located image: served from /public/blog/{stem}/ — the asset-copy
  // step at content-setup time mirrors images out of /content/blog/{stem}/.
  return `/blog/${stem}/${raw}`;
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
