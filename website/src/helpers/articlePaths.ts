import fs from "node:fs";
import path from "node:path";

export const ARTICLES_ROOT = path.join(process.cwd(), "content/learn/articles");

/**
 * Lists the top level of `content/learn/articles/`: every `<slug>.md(x)` file
 * or `<slug>/<slug>.md(x)` directory (the same co-located-assets shape
 * `blogPaths.ts` uses for the blog). Unlike blog stems, article slugs carry
 * no date prefix, since comparisons and explainers are evergreen and updated
 * in place rather than dated.
 */
export function listArticleSlugs(): { slug: string; rel: string }[] {
  if (!fs.existsSync(ARTICLES_ROOT)) {
    return [];
  }
  const entries = fs.readdirSync(ARTICLES_ROOT, { withFileTypes: true });
  const articles: { slug: string; rel: string }[] = [];

  for (const entry of entries) {
    if (entry.isDirectory()) {
      const candidate = ["md", "mdx"]
        .map((ext) => `${entry.name}/${entry.name}.${ext}`)
        .find((rel) => fs.existsSync(path.join(ARTICLES_ROOT, rel)));
      if (!candidate) {
        throw new Error(
          `[articlePaths] Article directory "${entry.name}" is missing the matching ` +
            `${entry.name}.md(x) file inside it.`,
        );
      }
      articles.push({ slug: entry.name, rel: candidate });
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
    articles.push({ slug: fileMatch[1], rel: entry.name });
  }

  return articles;
}

/** Resolve a `/learn/articles/[slug]` URL slug to a file path relative to {@link ARTICLES_ROOT}, or null if not found. */
export function resolveArticleFile(slug: string): string | null {
  const candidates = [`${slug}.md`, `${slug}.mdx`, `${slug}/${slug}.md`, `${slug}/${slug}.mdx`];
  return candidates.find((c) => fs.existsSync(path.join(ARTICLES_ROOT, c))) ?? null;
}
