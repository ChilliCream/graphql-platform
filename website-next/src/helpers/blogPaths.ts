import fs from "node:fs";
import path from "node:path";

export const BLOG_ROOT = path.join(process.cwd(), "blogs");
const STEM_RE = /^(\d{4})-(\d{2})-(\d{2})-(.+)$/;

export type BlogStem = {
  year: string;
  month: string;
  day: string;
  slug: string;
};

export function parseBlogStem(stem: string): BlogStem | null {
  const match = STEM_RE.exec(stem);
  if (!match) {
    return null;
  }
  const [, year, month, day, slug] = match;
  return { year, month, day, slug };
}

/**
 * Walks the top level of `blogs/`, validates every entry follows the
 * `YYYY-MM-DD-slug` convention (file or directory), and returns the list
 * of resolvable posts. Throws on the first violation so the build fails.
 */
export function listBlogPosts(): {
  stem: string;
  parsed: BlogStem;
  rel: string;
}[] {
  if (!fs.existsSync(BLOG_ROOT)) {
    return [];
  }
  const entries = fs.readdirSync(BLOG_ROOT, { withFileTypes: true });
  const posts: { stem: string; parsed: BlogStem; rel: string }[] = [];

  for (const entry of entries) {
    if (entry.isDirectory()) {
      const parsed = parseBlogStem(entry.name);
      if (!parsed) {
        // non-post directory at the root (e.g. "shared" assets) is allowed
        continue;
      }
      const candidate = ["md", "mdx"]
        .map((ext) => `${entry.name}/${entry.name}.${ext}`)
        .find((rel) => fs.existsSync(path.join(BLOG_ROOT, rel)));
      if (!candidate) {
        throw new Error(
          `[blogPaths] Blog directory "${entry.name}" is missing the matching ` +
            `${entry.name}.md(x) file inside it.`
        );
      }
      posts.push({ stem: entry.name, parsed, rel: candidate });
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
    const parsed = parseBlogStem(stem);
    if (!parsed) {
      throw new Error(
        `[blogPaths] Invalid blog file "${entry.name}". ` +
          `Expected name format YYYY-MM-DD-slug.md(x).`
      );
    }
    posts.push({ stem, parsed, rel: entry.name });
  }

  return posts;
}

/** Build the canonical URL for a blog post stem. */
export function blogUrlForStem(parsed: BlogStem): string {
  return `/blogs/${parsed.year}/${parsed.month}/${parsed.day}/${parsed.slug}`;
}

/** Reverse the catch-all slug (e.g. ['2019','06','05','hot-chocolate-9.0.0'])
 *  to a file path relative to BLOG_ROOT, or null if not found. */
export function resolveBlogFile(slug: string[]): string | null {
  if (slug.length < 4) {
    return null;
  }
  const [year, month, day, ...rest] = slug;
  const slugPart = rest.join("/");
  const stem = `${year}-${month}-${day}-${slugPart}`;
  const candidates = [
    `${stem}.md`,
    `${stem}.mdx`,
    `${stem}/${stem}.md`,
    `${stem}/${stem}.mdx`,
  ];
  return (
    candidates.find((c) => fs.existsSync(path.join(BLOG_ROOT, c))) ?? null
  );
}

/** Given a path relative to BLOG_ROOT (e.g. "2019-06-05-foo/2019-06-05-foo.md"),
 *  produce the canonical blog URL — or null if the path doesn't follow the
 *  blog convention. */
export function blogUrlFromBlogRelPath(rel: string): string | null {
  const cleanRel = rel.replace(/\.mdx?$/i, "");
  const segments = cleanRel.split("/");
  const parsed = parseBlogStem(segments[0]);
  if (!parsed) {
    return null;
  }
  return blogUrlForStem(parsed);
}
