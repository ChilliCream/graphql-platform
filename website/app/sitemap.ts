import path from "node:path";
import type { MetadataRoute } from "next";
import {
  BLOG_ROOT,
  blogUrlForStem,
  listBlogPosts,
} from "@/src/helpers/blogPaths";
import { getLastModifiedFromGit } from "@/src/helpers/gitMetadata";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";
import { SITE_URL } from "@/src/helpers/siteUrl";

export const dynamic = "force-static";

const fs = process.getBuiltinModule("node:fs");

// Marketing / legal / product pages live in the `(content)` route group, so
// their on-disk folder names map 1:1 to URL paths (the group itself is elided).
const CONTENT_PAGES_ROOT = path.join(process.cwd(), "app", "(content)");
const DOCS_CONTENT_ROOT = path.join(process.cwd(), "content", "docs");

// Pages that exist for a user flow but should not be indexed.
const EXCLUDED_PATHS = new Set(["/services/support/thank-you"]);

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  return [
    ...(await rootPages()),
    ...(await staticPages()),
    ...(await docsPages()),
    ...(await blogPosts()),
  ];
}

// Pages that live outside the `(content)` route group: the homepage and the
// docs/blog hub pages. These are the highest-value URLs on the site and must
// be listed explicitly since `staticPages()` only walks `(content)`.
async function rootPages(): Promise<MetadataRoute.Sitemap> {
  const pages = [
    { file: path.join(process.cwd(), "app", "page.tsx"), urlPath: "/" },
    {
      file: path.join(process.cwd(), "app", "docs", "page.tsx"),
      urlPath: "/docs",
    },
    {
      file: path.join(process.cwd(), "app", "blog", "page.tsx"),
      urlPath: "/blog",
    },
  ];
  return Promise.all(
    pages.map(async ({ file, urlPath }) => ({
      url: urlPath === "/" ? `${SITE_URL}/` : `${SITE_URL}${urlPath}`,
      lastModified:
        (await getLastModifiedFromGit(file)) ?? fs.statSync(file).mtime,
      changeFrequency: "weekly" as const,
      priority: urlPath === "/" ? 1 : 0.8,
    })),
  );
}

async function staticPages(): Promise<MetadataRoute.Sitemap> {
  return Promise.all(
    walk(CONTENT_PAGES_ROOT)
      .filter((file) => path.basename(file) === "page.tsx")
      .map((file) => {
        const rel = path.relative(CONTENT_PAGES_ROOT, path.dirname(file));
        const urlPath = rel === "" ? "/" : `/${rel.split(path.sep).join("/")}`;
        return { file, urlPath };
      })
      .filter(({ urlPath }) => !EXCLUDED_PATHS.has(urlPath))
      .map(async ({ file, urlPath }) => ({
        url: `${SITE_URL}${urlPath}`,
        lastModified:
          (await getLastModifiedFromGit(file)) ?? fs.statSync(file).mtime,
        changeFrequency: "monthly" as const,
        priority: urlPath === "/" ? 1 : 0.7,
      })),
  );
}

async function docsPages(): Promise<MetadataRoute.Sitemap> {
  const files = walk(DOCS_CONTENT_ROOT).filter((f) => /\.mdx?$/.test(f));
  return Promise.all(
    files
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
      .map(async ({ file, slug }) => ({
        url: `${SITE_URL}/docs/${slug.join("/")}`,
        lastModified:
          (await getLastModifiedFromGit(file)) ?? fs.statSync(file).mtime,
        changeFrequency: "weekly" as const,
        priority: 0.5,
      })),
  );
}

async function blogPosts(): Promise<MetadataRoute.Sitemap> {
  return Promise.all(
    listBlogPosts().map(async ({ parsed, rel }) => {
      const file = path.join(BLOG_ROOT, rel);
      const fm = readFrontmatter(file) as Record<string, unknown>;
      // An explicit `updated` frontmatter field wins; otherwise the last git
      // commit touching the post, with file mtime as the no-git fallback.
      const updated =
        typeof fm.updated === "string" && fm.updated.length > 0
          ? new Date(fm.updated)
          : null;
      return {
        url: `${SITE_URL}${blogUrlForStem(parsed)}`,
        lastModified:
          updated ??
          (await getLastModifiedFromGit(file)) ??
          fs.statSync(file).mtime,
        changeFrequency: "yearly" as const,
        priority: 0.5,
      };
    }),
  );
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
