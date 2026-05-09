import path from "node:path";
import type { TreeNode } from "./buildContentTree";
import {
  BLOG_ROOT,
  blogUrlForStem,
  listBlogPosts,
} from "./blogPaths";
import { readFrontmatter } from "./readFrontmatter";

/**
 * Builds the blog sidebar tree as a flat list ordered newest first.
 * Title comes from frontmatter; falls back to the slug portion.
 */
export function buildBlogTree(): TreeNode[] {
  const posts = listBlogPosts();
  posts.sort((a, b) => (a.stem < b.stem ? 1 : a.stem > b.stem ? -1 : 0));

  return posts.map(({ parsed, rel }) => {
    const fm = readFrontmatter(path.join(BLOG_ROOT, rel));
    const title =
      typeof fm.title === "string" && fm.title.length > 0
        ? fm.title
        : parsed.slug;
    return {
      title,
      href: blogUrlForStem(parsed),
      children: [],
    };
  });
}
