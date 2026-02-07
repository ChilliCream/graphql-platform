import { execSync } from "child_process";
import path from "path";

import { getContentDir, getFilesRecursively, readMarkdownFile } from "./content";
import docsConfig from "../src/docs/docs.json";

export interface DocPage {
  slug: string;
  originPath: string;
  content: string;
  frontmatter: Record<string, any>;
  product?: string;
  version?: string;
  lastUpdated?: string;
  lastAuthorName?: string;
}

export interface DocsProduct {
  path: string;
  title: string;
  description: string;
  metaDescription?: string;
  latestStableVersion: string;
  versions: DocsVersion[];
}

export interface DocsVersion {
  path: string;
  title: string;
  items: DocsNavItem[];
}

export interface DocsNavItem {
  path: string;
  title: string;
  items?: DocsNavItem[];
}

const DOCS_DIR = getContentDir("docs");

let _cachedDocPages: DocPage[] | null = null;

function getGitMetadata(filePath: string): {
  lastUpdated: string;
  lastAuthorName: string;
} {
  try {
    const result = execSync(
      `git log -1 --format="%ai||%an" -- "${filePath}"`,
      { encoding: "utf-8", timeout: 5000 }
    ).trim();

    if (result) {
      const [dateStr, authorName] = result.split("||");
      const date = new Date(dateStr);
      const lastUpdated = date.toLocaleDateString("en-US", {
        year: "numeric",
        month: "long",
        day: "2-digit",
      });
      return { lastUpdated, lastAuthorName: authorName || "" };
    }
  } catch {
    // Git metadata not available
  }
  return { lastUpdated: "", lastAuthorName: "" };
}

export function getDocsConfig(): DocsProduct[] {
  return docsConfig as unknown as DocsProduct[];
}

export function getAllDocPages(): DocPage[] {
  if (_cachedDocPages) return _cachedDocPages;

  const files = getFilesRecursively(DOCS_DIR, ".md");
  const pages: DocPage[] = [];

  for (const file of files) {
    const { frontmatter, content } = readMarkdownFile(file);
    const relative = path.relative(DOCS_DIR, file);
    const slug =
      "/docs/" +
      relative
        .replace(/\.mdx?$/, "")
        .replace(/\/index$/, "")
        .split(path.sep)
        .join("/");

    const originPath = relative;

    // Extract product and version from path
    const parts = relative.split(path.sep);
    const product = parts[0] || undefined;
    let version: string | undefined;
    if (parts.length > 1 && /^v\d+/.test(parts[1])) {
      version = parts[1];
    }

    const gitMeta = getGitMetadata(file);

    pages.push({
      slug,
      originPath,
      content,
      frontmatter,
      product,
      version,
      lastUpdated: gitMeta.lastUpdated,
      lastAuthorName: gitMeta.lastAuthorName,
    });
  }

  _cachedDocPages = pages;
  return pages;
}

export function getDocPageBySlug(slug: string): DocPage | undefined {
  const normalizedSlug = slug.replace(/\/$/, "");
  return getAllDocPages().find((p) => p.slug === normalizedSlug);
}

export function getProductInfo(productPath: string): DocsProduct | undefined {
  return getDocsConfig().find((p) => p.path === productPath);
}

export function getVersionNav(
  productPath: string,
  versionPath: string
): DocsVersion | undefined {
  const product = getProductInfo(productPath);
  if (!product) return undefined;
  return product.versions.find((v) => v.path === versionPath);
}

export function getProductRedirectPath(productPath: string): string {
  const product = getProductInfo(productPath);
  if (!product) return `/docs/${productPath}`;
  const version = product.latestStableVersion;
  return `/docs/${productPath}${version ? `/${version}` : ""}`;
}
