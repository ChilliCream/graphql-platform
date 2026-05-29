import fs from "node:fs";
import path from "node:path";

import { readFrontmatter } from "./readFrontmatter";

export interface LatestBlogPost {
  href: string;
  title: string;
  date: string;
  description?: string;
  featuredImage?: string;
}

const BLOGS_DIR = path.join(process.cwd(), "content", "blogs");

// Walks content/blogs, parses frontmatter, returns the post with the most
// recent `date`. Used by the header nav dropdowns to render the "Latest
// Blog Post" panel that ships with /website's design.
export function getLatestBlogPost(): LatestBlogPost | null {
  const entries = listBlogFiles();
  let best: { date: Date; entry: BlogEntry } | null = null;

  for (const entry of entries) {
    const fm = readFrontmatter(entry.absPath);
    const dateStr = typeof fm.date === "string" ? fm.date : null;
    // Only consider posts that ship a featured image — the dropdown panel
    // needs the hero artwork to look right; a text-only card looks empty.
    if (!dateStr || typeof fm.featuredImage !== "string" || !fm.featuredImage) {
      continue;
    }
    const d = new Date(dateStr);
    if (Number.isNaN(d.getTime())) continue;
    if (!best || d > best.date) {
      best = { date: d, entry: { ...entry, fm, dateStr } };
    }
  }

  if (!best) return null;
  const { entry } = best;
  const fm = entry.fm;
  const slug = entry.slug;
  // Featured image lives next to the .md inside the blog folder. The
  // /website-next public/ tree doesn't carry the per-post image folders so
  // we resolve the asset against chillicream.com — same approach the
  // landing Outro uses for blog cards. (next/image works for external URLs
  // when `images.unoptimized: true`, which our static export already sets.)
  const featured = typeof fm.featuredImage === "string" ? fm.featuredImage : "";
  const featuredImage = featured
    ? `https://chillicream.com/images/blog/${slug}/${featured}`
    : undefined;

  return {
    href: `/blog/${dateToPathSegments(entry.dateStr)}/${slugTrailing(slug, entry.dateStr)}`,
    title: typeof fm.title === "string" ? fm.title : slug,
    date: formatDate(entry.dateStr),
    description: typeof fm.description === "string" ? fm.description : undefined,
    featuredImage,
  };
}

interface BlogEntry {
  absPath: string;
  slug: string;
  fm?: ReturnType<typeof readFrontmatter>;
  dateStr?: string;
}

function listBlogFiles(): BlogEntry[] {
  const results: BlogEntry[] = [];
  if (!fs.existsSync(BLOGS_DIR)) return results;
  for (const item of fs.readdirSync(BLOGS_DIR, { withFileTypes: true })) {
    if (item.isFile() && /\.(md|mdx)$/.test(item.name)) {
      results.push({
        absPath: path.join(BLOGS_DIR, item.name),
        slug: item.name.replace(/\.(md|mdx)$/, ""),
      });
    } else if (item.isDirectory()) {
      // Per-post folder: pick the .md/.mdx with the same name as the folder.
      const folder = path.join(BLOGS_DIR, item.name);
      const candidate = [`${item.name}.md`, `${item.name}.mdx`]
        .map((f) => path.join(folder, f))
        .find((p) => fs.existsSync(p));
      if (candidate) {
        results.push({ absPath: candidate, slug: item.name });
      }
    }
  }
  return results;
}

function dateToPathSegments(dateStr: string): string {
  // Blog file names start with YYYY-MM-DD, the route uses /YYYY/MM/DD.
  const [y, m, d] = dateStr.split("-");
  return `${y}/${m}/${d}`;
}

function slugTrailing(folderOrFile: string, dateStr: string): string {
  // Strip leading YYYY-MM-DD- from folder/file name.
  const datePrefix = dateStr.replace(/-/g, "-");
  return folderOrFile.startsWith(datePrefix + "-")
    ? folderOrFile.slice(datePrefix.length + 1)
    : folderOrFile;
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}
