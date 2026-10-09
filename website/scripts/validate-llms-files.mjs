import { createHash } from "node:crypto";
import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  generateLlmsFiles,
  isLlmsEligibleUrl,
} from "./generate-llms-files.mjs";
import { parseSitemapUrls } from "./parse-sitemap.mjs";

const PROJECT_ROOT = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
);
const OUTPUT_ROOT = path.join(PROJECT_ROOT, "out");
const EXCLUDED_PATHS = [
  "/platform/continuous-integration",
  "/services/support/thank-you",
  "/404",
  "/blog/tags/",
  "/docs/skillz",
];
const BLOG_POST_PATH = /^\/blog\/\d{4}-\d{2}-\d{2}-/;
const FORBIDDEN_BUILD_MARKERS = [
  "self.__next_f",
  "data-nextjs-scroll-focus-boundary",
  "<style data-precedence",
  "data:image/",
];

async function sitemapUrls() {
  const xml = await fs.readFile(path.join(OUTPUT_ROOT, "sitemap.xml"), "utf8");
  return parseSitemapUrls(xml);
}

function markdownRelativePath(url) {
  const pathname = decodeURIComponent(new URL(url).pathname);
  return pathname === "/" ? "index.md" : `${pathname.slice(1)}.md`;
}

function markdownUrl(url) {
  const parsed = new URL(url);
  parsed.pathname =
    parsed.pathname === "/" ? "/index.md" : `${parsed.pathname}.md`;
  return parsed.href;
}

function countExactLine(text, line) {
  return text.split("\n").filter((candidate) => candidate.trimEnd() === line)
    .length;
}

function validateCatalog(relativePath, content) {
  const lines = content.replace(/^\uFEFF/, "").split("\n");
  if (!/^#\s+\S/.test(lines[0])) {
    throw new Error(`${relativePath} must start with an H1.`);
  }

  const firstContentAfterTitle = lines
    .slice(1)
    .find((line) => line.trim() !== "");
  if (!firstContentAfterTitle?.startsWith("> ")) {
    throw new Error(`${relativePath} must put its summary after the H1.`);
  }

  const sections = [...content.matchAll(/^##\s+(.+)$/gm)];
  if (sections.length === 0) {
    throw new Error(`${relativePath} does not contain any H2 file sections.`);
  }

  for (let index = 0; index < sections.length; index += 1) {
    const start = sections[index].index + sections[index][0].length;
    const end = sections[index + 1]?.index ?? content.length;
    if (!/^- \[[^\]]+\]\([^)]+\)(?:: .+)?$/m.test(content.slice(start, end))) {
      throw new Error(
        `${relativePath} section "${sections[index][1]}" has no valid file-list entry.`,
      );
    }
  }

  const links = [...content.matchAll(/\]\((https?:\/\/[^)]+)\)/g)].map(
    (match) => match[1],
  );
  if (new Set(links).size !== links.length) {
    throw new Error(`${relativePath} contains duplicate links.`);
  }

  return links;
}

async function listFiles(directory, predicate) {
  const entries = await fs.readdir(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const absolute = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...(await listFiles(absolute, predicate)));
    } else if (predicate(entry.name)) {
      files.push(absolute);
    }
  }

  return files;
}

function outputPathForPublicUrl(url) {
  const parsed = new URL(url);
  const relative = decodeURIComponent(parsed.pathname).replace(/^\//, "");
  if (relative === "") {
    return path.join(OUTPUT_ROOT, "index.html");
  }
  if (/\.(?:md|txt|xml)$/.test(relative)) {
    return path.join(OUTPUT_ROOT, relative);
  }
  return path.join(OUTPUT_ROOT, `${relative}.html`);
}

async function sha256(file) {
  const content = await fs.readFile(file);
  return createHash("sha256").update(content).digest("hex");
}

async function generatedHashes(markdownFiles) {
  const llmsFiles = await listFiles(
    OUTPUT_ROOT,
    (name) => name === "llms.txt" || name === "llms-full.txt",
  );
  const files = [...new Set([...llmsFiles, ...markdownFiles])].sort();
  return new Map(
    await Promise.all(
      files.map(async (file) => [
        path.relative(OUTPUT_ROOT, file),
        await sha256(file),
      ]),
    ),
  );
}

const allSitemapUrls = await sitemapUrls();
if (
  allSitemapUrls.length === 0 ||
  new Set(allSitemapUrls).size !== allSitemapUrls.length
) {
  throw new Error("The sitemap must contain a nonempty set of unique URLs.");
}
if (
  allSitemapUrls.some((url) =>
    new URL(url).pathname.match(/^\/docs\/skillz(?:\/|$)/),
  )
) {
  throw new Error("The sitemap contains legacy /docs/skillz URLs.");
}

const urls = allSitemapUrls.filter(isLlmsEligibleUrl);
for (const url of allSitemapUrls) {
  const pathname = new URL(url).pathname;
  if (BLOG_POST_PATH.test(pathname) && !isLlmsEligibleUrl(url)) {
    throw new Error(`Public blog post was excluded from LLM export: ${url}`);
  }
}

const perPageMarkdownFiles = urls.map((url) =>
  path.join(OUTPUT_ROOT, markdownRelativePath(url)),
);
for (let index = 0; index < urls.length; index += 1) {
  const canonicalUrl = urls[index];
  const file = perPageMarkdownFiles[index];
  const content = await fs.readFile(file, "utf8");
  if (content.length < 100) {
    throw new Error(
      `${path.relative(OUTPUT_ROOT, file)} is unexpectedly short.`,
    );
  }
  if (countExactLine(content, `Canonical source: ${canonicalUrl}`) !== 1) {
    throw new Error(
      `${path.relative(OUTPUT_ROOT, file)} does not have exactly one canonical source marker.`,
    );
  }

  const pathname = decodeURIComponent(new URL(canonicalUrl).pathname);
  const htmlPath = path.join(
    OUTPUT_ROOT,
    pathname === "/" ? "index.html" : `${pathname.slice(1)}.html`,
  );
  const html = await fs.readFile(htmlPath, "utf8");
  const expectedAlternate = `<link rel="alternate" type="text/markdown" href="${markdownUrl(canonicalUrl)}" data-llms-generated="true"/>`;
  if (html.split(expectedAlternate).length !== 2) {
    throw new Error(
      `${path.relative(OUTPUT_ROOT, htmlPath)} does not advertise exactly one Markdown alternative.`,
    );
  }
}

for (const url of allSitemapUrls.filter(
  (candidate) => !isLlmsEligibleUrl(candidate),
)) {
  const pathname = decodeURIComponent(new URL(url).pathname);
  const htmlPath = path.join(OUTPUT_ROOT, `${pathname.slice(1)}.html`);
  const html = await fs.readFile(htmlPath, "utf8");
  if (html.includes('data-llms-generated="true"')) {
    throw new Error(
      `${path.relative(OUTPUT_ROOT, htmlPath)} advertises redundant archive Markdown.`,
    );
  }
}

const rootCatalog = await fs.readFile(
  path.join(OUTPUT_ROOT, "llms.txt"),
  "utf8",
);
const fullCorpus = await fs.readFile(
  path.join(OUTPUT_ROOT, "llms-full.txt"),
  "utf8",
);
const catalogFiles = await listFiles(
  OUTPUT_ROOT,
  (name) => name === "llms.txt",
);
const discoveredMarkdownUrls = new Set();

for (const file of catalogFiles) {
  const relative = path.relative(OUTPUT_ROOT, file);
  if (/^docs[\\/]skillz(?:[\\/]|$)/.test(relative)) {
    throw new Error(`Generated legacy Skills catalog ${relative}`);
  }
}

for (const file of catalogFiles) {
  const relative = path.relative(OUTPUT_ROOT, file);
  const content = await fs.readFile(file, "utf8");
  const links = validateCatalog(relative, content);

  for (const link of links) {
    const parsed = new URL(link);
    if (parsed.origin !== new URL(urls[0]).origin) {
      continue;
    }
    const target = outputPathForPublicUrl(link);
    try {
      await fs.access(target);
    } catch {
      throw new Error(`${relative} links to missing exported file ${link}`);
    }
    if (parsed.pathname.endsWith(".md")) {
      discoveredMarkdownUrls.add(link);
    }
  }
}

for (const url of urls) {
  const pageMarkdownUrl = markdownUrl(url);
  if (!discoveredMarkdownUrls.has(pageMarkdownUrl)) {
    throw new Error(`No llms.txt catalog links to ${pageMarkdownUrl}`);
  }
  if (countExactLine(fullCorpus, `Canonical source: ${url}`) !== 1) {
    throw new Error(`llms-full.txt does not include ${url} exactly once.`);
  }
}

const fullCorpusSources = fullCorpus
  .split("\n")
  .filter((line) => line.startsWith("Canonical source: "))
  .map((line) => new URL(line.slice("Canonical source: ".length)).pathname);
for (const excluded of EXCLUDED_PATHS) {
  const excludedSource = fullCorpusSources.some((pathname) =>
    excluded.endsWith("/")
      ? pathname.startsWith(excluded)
      : pathname === excluded || pathname.startsWith(`${excluded}/`),
  );
  if (rootCatalog.includes(excluded) || excludedSource) {
    throw new Error(`Generated root files include excluded source ${excluded}`);
  }
}

for (const marker of FORBIDDEN_BUILD_MARKERS) {
  if (fullCorpus.includes(marker)) {
    throw new Error(`llms-full.txt contains build-only marker ${marker}`);
  }
}

const before = await generatedHashes(perPageMarkdownFiles);
await generateLlmsFiles();
const after = await generatedHashes(perPageMarkdownFiles);
if (
  before.size !== after.size ||
  [...before].some(([file, hash]) => after.get(file) !== hash)
) {
  throw new Error("LLM content generation is not deterministic.");
}

const totalMarkdownBytes = (
  await Promise.all(perPageMarkdownFiles.map((file) => fs.stat(file)))
).reduce((sum, stat) => sum + stat.size, 0);

console.log(
  `[llms] Validated ${allSitemapUrls.length} sitemap URLs, ${urls.length} substantive pages, ${catalogFiles.length} catalogs, ${perPageMarkdownFiles.length} Markdown pages, and ${(totalMarkdownBytes / 1024 / 1024).toFixed(2)} MiB of page content.`,
);
