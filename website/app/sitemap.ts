import { execSync } from "child_process";
import fs from "fs";
import path from "path";
import type { MetadataRoute } from "next";

import { getAllBlogPosts, getAllTags, getPostsPerPage } from "@/lib/blog";
import {
  getContentDir,
  getFilesRecursively,
  readMarkdownFile,
} from "@/lib/content";
import { getAllDocPages } from "@/lib/docs";
import { siteMetadata } from "@/lib/site-config";

export const dynamic = "force-static";

const BASE_URL = siteMetadata.siteUrl;
const WEBSITE_DIR = path.resolve(process.cwd());

const STATIC_ROUTES: { route: string; pageFile: string }[] = [
  { route: "/", pageFile: "app/page.tsx" },
  { route: "/blog/", pageFile: "app/blog/page.tsx" },
  { route: "/docs/", pageFile: "app/docs/page.tsx" },
  { route: "/help/", pageFile: "app/help/page.tsx" },
  { route: "/pricing/", pageFile: "app/pricing/page.tsx" },
  {
    route: "/platform/analytics/",
    pageFile: "app/platform/analytics/page.tsx",
  },
  {
    route: "/platform/continuous-integration/",
    pageFile: "app/platform/continuous-integration/page.tsx",
  },
  {
    route: "/platform/ecosystem/",
    pageFile: "app/platform/ecosystem/page.tsx",
  },
  {
    route: "/products/hotchocolate/",
    pageFile: "app/products/hotchocolate/page.tsx",
  },
  { route: "/products/nitro/", pageFile: "app/products/nitro/page.tsx" },
  {
    route: "/products/strawberryshake/",
    pageFile: "app/products/strawberryshake/page.tsx",
  },
  { route: "/services/advisory/", pageFile: "app/services/advisory/page.tsx" },
  { route: "/services/support/", pageFile: "app/services/support/page.tsx" },
  {
    route: "/services/support/contact/",
    pageFile: "app/services/support/contact/page.tsx",
  },
  {
    route: "/services/support/thank-you/",
    pageFile: "app/services/support/thank-you/page.tsx",
  },
  { route: "/services/training/", pageFile: "app/services/training/page.tsx" },
];

const DOCS_DISALLOWED = [
  /^\/docs\/hotchocolate\/v10(\/|$)/,
  /^\/docs\/hotchocolate\/v11(\/|$)/,
];

const mtimeCache = new Map<string, Date | undefined>();

function gitMTime(filePath: string): Date | undefined {
  if (process.env.NODE_ENV === "development") return undefined;
  if (mtimeCache.has(filePath)) return mtimeCache.get(filePath);

  let result: Date | undefined;
  try {
    const out = execSync(`git log -1 --format=%aI -- "${filePath}"`, {
      encoding: "utf-8",
      timeout: 5000,
    }).trim();
    if (out) {
      const d = new Date(out);
      if (!Number.isNaN(d.getTime())) result = d;
    }
  } catch {
    // git unavailable, leave undefined
  }
  mtimeCache.set(filePath, result);
  return result;
}

function readBasicSlugs(subdir: string): string[] {
  const dir = getContentDir("basic", subdir);
  if (!fs.existsSync(dir)) return [];
  return fs
    .readdirSync(dir)
    .filter((f) => f.endsWith(".md"))
    .map((f) => f.replace(/\.md$/, ""));
}

function getBlogPostFilePaths(): Map<string, string> {
  const map = new Map<string, string>();
  const blogDir = getContentDir("blog");
  for (const file of getFilesRecursively(blogDir, ".md")) {
    const { frontmatter } = readMarkdownFile(file);
    if (
      typeof frontmatter.path === "string" &&
      frontmatter.path.startsWith("/blog/")
    ) {
      map.set(frontmatter.path, file);
    }
  }
  return map;
}

function parseIso(value: string | undefined): Date | undefined {
  if (!value) return undefined;
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? undefined : d;
}

export default function sitemap(): MetadataRoute.Sitemap {
  const fallback = new Date();
  const entries: MetadataRoute.Sitemap = [];

  for (const { route, pageFile } of STATIC_ROUTES) {
    entries.push({
      url: `${BASE_URL}${route}`,
      lastModified: gitMTime(path.join(WEBSITE_DIR, pageFile)) ?? fallback,
    });
  }

  for (const slug of readBasicSlugs("legal")) {
    entries.push({
      url: `${BASE_URL}/legal/${slug}/`,
      lastModified:
        gitMTime(path.join(getContentDir("basic", "legal"), `${slug}.md`)) ??
        fallback,
    });
  }
  for (const slug of readBasicSlugs("licensing")) {
    entries.push({
      url: `${BASE_URL}/licensing/${slug}/`,
      lastModified:
        gitMTime(
          path.join(getContentDir("basic", "licensing"), `${slug}.md`)
        ) ?? fallback,
    });
  }

  const posts = getAllBlogPosts();
  const blogFilePaths = getBlogPostFilePaths();
  const postMTimes = new Map<string, Date>();
  let latestPostDate: Date | undefined;
  for (const post of posts) {
    const filePath = blogFilePaths.get(post.slug);
    const mtime =
      (filePath ? gitMTime(filePath) : undefined) ??
      parseIso(post.date) ??
      fallback;
    postMTimes.set(post.slug, mtime);
    if (!latestPostDate || mtime > latestPostDate) latestPostDate = mtime;
  }

  // /blog/ index lastModified reflects the newest post
  const blogIndex = entries.find((e) => e.url === `${BASE_URL}/blog/`);
  if (blogIndex && latestPostDate) blogIndex.lastModified = latestPostDate;

  const postsPerPage = getPostsPerPage();
  const totalPages = Math.max(1, Math.ceil(posts.length / postsPerPage));
  for (let page = 2; page <= totalPages; page++) {
    entries.push({
      url: `${BASE_URL}/blog/${page}/`,
      lastModified: latestPostDate ?? fallback,
    });
  }

  for (const post of posts) {
    const postPath = post.slug.endsWith("/") ? post.slug : `${post.slug}/`;
    entries.push({
      url: `${BASE_URL}${postPath}`,
      lastModified: postMTimes.get(post.slug) ?? fallback,
    });
  }

  for (const tag of getAllTags()) {
    let tagLatest: Date | undefined;
    for (const post of posts) {
      if (!post.tags.includes(tag)) continue;
      const mtime = postMTimes.get(post.slug);
      if (mtime && (!tagLatest || mtime > tagLatest)) tagLatest = mtime;
    }
    entries.push({
      url: `${BASE_URL}/blog/tags/${tag}/`,
      lastModified: tagLatest ?? fallback,
    });
  }

  const docPages = getAllDocPages();
  for (const page of docPages) {
    const docPath = page.slug.endsWith("/") ? page.slug : `${page.slug}/`;
    if (DOCS_DISALLOWED.some((re) => re.test(docPath))) continue;

    entries.push({
      url: `${BASE_URL}${docPath}`,
      lastModified: parseIso(page.lastUpdatedIso) ?? fallback,
    });
  }

  return entries;
}
