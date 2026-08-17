import path from "node:path";
import type { MetadataRoute } from "next";
import {
  BLOG_ROOT,
  blogUrlForStem,
  listBlogPosts,
} from "@/src/helpers/blogPaths";
import { POSTS_PER_PAGE } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
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
const EXCLUDED_PATHS = new Set([
  "/platform/continuous-integration",
  "/services/support/thank-you",
]);

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const entries = [
    ...rootPages(),
    ...staticPages(),
    ...(await docsPages()),
    ...blogArchivePages(),
    ...(await blogPosts()),
  ];

  const urls = entries.map((entry) => entry.url);
  if (new Set(urls).size !== urls.length) {
    throw new Error("The sitemap contains duplicate canonical URLs.");
  }

  return entries.sort((left, right) => left.url.localeCompare(right.url));
}

// Pages that live outside the `(content)` route group: the homepage and the
// docs hub page. Blog indexes are generated with the other blog archives.
// These files are not part of the generated git manifest, so emitting their
// checkout mtime would falsely claim they changed on every deployment. An
// omitted `<lastmod>` is more useful than an unverifiable timestamp.
function rootPages(): MetadataRoute.Sitemap {
  return [sitemapEntry("/"), sitemapEntry("/docs")];
}

function staticPages(): MetadataRoute.Sitemap {
  return (
    walk(CONTENT_PAGES_ROOT)
      .filter((file) => path.basename(file) === "page.tsx")
      .map((file) => {
        const rel = path.relative(CONTENT_PAGES_ROOT, path.dirname(file));
        return rel === "" ? "/" : `/${rel.split(path.sep).join("/")}`;
      })
      .filter((urlPath) => !EXCLUDED_PATHS.has(urlPath))
      // Visible content commonly lives in imported components, so page.tsx's
      // commit date alone is not an accurate modification date for these routes.
      .map((urlPath) => sitemapEntry(urlPath))
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
      .map(async ({ file, slug }) =>
        sitemapEntry(
          `/docs/${slug.join("/")}`,
          await getLastModifiedFromGit(file),
        ),
      ),
  );
}

/** Every indexable, self-canonical blog listing page. */
function blogArchivePages(): MetadataRoute.Sitemap {
  const posts = listBlogPostSummaries();
  const entries = [sitemapEntry("/blog")];
  const pageCount = Math.ceil(posts.length / POSTS_PER_PAGE);

  for (let page = 2; page <= pageCount; page++) {
    entries.push(sitemapEntry(`/blog/${page}`));
  }

  return entries;
}

async function blogPosts(): Promise<MetadataRoute.Sitemap> {
  return Promise.all(
    listBlogPosts().map(async ({ parsed, rel }) => {
      const file = path.join(BLOG_ROOT, rel);
      const fm = readFrontmatter(file) as Record<string, unknown>;
      // An explicit `updated` frontmatter field wins; otherwise the last git
      // commit touching the post. Filesystem mtimes are deliberately not used:
      // checkout and container-copy times change without the article changing.
      const updated =
        typeof fm.updated === "string" && fm.updated.length > 0
          ? validDate(fm.updated)
          : undefined;
      return sitemapEntry(
        blogUrlForStem(parsed),
        updated ?? (await getLastModifiedFromGit(file)),
      );
    }),
  );
}

function sitemapEntry(
  urlPath: string,
  lastModified?: Date,
): MetadataRoute.Sitemap[number] {
  return {
    url: urlPath === "/" ? `${SITE_URL}/` : `${SITE_URL}${urlPath}`,
    ...(lastModified ? { lastModified } : {}),
  };
}

function validDate(value: string): Date | undefined {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date;
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
