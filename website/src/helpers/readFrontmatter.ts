import fs from "node:fs";
import matter from "gray-matter";

export type DocFrontmatter = {
  title?: string;
  /**
   * Overrides the browser tab / SEO `<title>` (and Open Graph title) without
   * changing the on-page `<h1>`, which still uses `title`. Use for terse
   * headings that need a richer, keyword-bearing title tag.
   */
  metaTitle?: string;
  description?: string;
  [key: string]: unknown;
};

export function readFrontmatter(absPath: string): DocFrontmatter {
  const raw = fs.readFileSync(absPath, "utf-8");
  const { data } = matter(raw);
  return data as DocFrontmatter;
}
