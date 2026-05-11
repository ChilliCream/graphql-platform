import fs from "node:fs";
import path from "node:path";
import type { MetadataRoute } from "next";
import {
  BLOG_ROOT,
  blogUrlForStem,
  listBlogPosts,
} from "@/src/helpers/blogPaths";
import { SITE_URL } from "@/src/helpers/siteUrl";

export const dynamic = "force-static";

// Marketing / legal / product pages live in the `(content)` route group, so
// their on-disk folder names map 1:1 to URL paths (the group itself is elided).
const CONTENT_PAGES_ROOT = path.join(process.cwd(), "app", "(content)");
const DOCS_CONTENT_ROOT = path.join(process.cwd(), "content", "docs");

// Pages that exist for a user flow but should not be indexed.
const EXCLUDED_PATHS = new Set(["/services/support/thank-you"]);

export default function sitemap(): MetadataRoute.Sitemap {
  return [...staticPages(), ...docsPages(), ...blogPosts()];
}

function staticPages(): MetadataRoute.Sitemap {
  return walk(CONTENT_PAGES_ROOT)
    .filter((file) => path.basename(file) === "page.tsx")
    .map((file) => {
      const rel = path.relative(CONTENT_PAGES_ROOT, path.dirname(file));
      const urlPath = rel === "" ? "/" : `/${rel.split(path.sep).join("/")}`;
      return { file, urlPath };
    })
    .filter(({ urlPath }) => !EXCLUDED_PATHS.has(urlPath))
    .map(({ file, urlPath }) => ({
      url: `${SITE_URL}${urlPath}`,
      lastModified: fs.statSync(file).mtime,
      changeFrequency: "monthly",
      priority: urlPath === "/" ? 1 : 0.7,
    }));
}

function docsPages(): MetadataRoute.Sitemap {
  return walk(DOCS_CONTENT_ROOT)
    .filter((file) => /\.mdx?$/.test(file))
    .map((file) => {
      const parts = path
        .relative(DOCS_CONTENT_ROOT, file)
        .replace(/\.mdx?$/, "")
        .split(path.sep);
      const slug =
        parts[parts.length - 1] === "index" ? parts.slice(0, -1) : parts;
      return { file, slug };
    })
    .filter(({ slug }) => slug.length > 0)
    .map(({ file, slug }) => ({
      url: `${SITE_URL}/docs/${slug.join("/")}`,
      lastModified: fs.statSync(file).mtime,
      changeFrequency: "weekly",
      priority: 0.5,
    }));
}

function blogPosts(): MetadataRoute.Sitemap {
  return listBlogPosts().map(({ parsed, rel }) => ({
    url: `${SITE_URL}${blogUrlForStem(parsed)}`,
    lastModified: fs.statSync(path.join(BLOG_ROOT, rel)).mtime,
    changeFrequency: "yearly",
    priority: 0.5,
  }));
}

function walk(dir: string): string[] {
  if (!fs.existsSync(dir)) {
    return [];
  }
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    return entry.isDirectory() ? walk(full) : [full];
  });
}
