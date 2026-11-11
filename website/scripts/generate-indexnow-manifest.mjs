import { createHash } from "node:crypto";
import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { parse } from "node-html-parser";
import { parseSitemapUrls } from "./parse-sitemap.mjs";

const PROJECT_ROOT = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
);
const OUTPUT_ROOT = path.join(PROJECT_ROOT, "out");

export const MANIFEST_FILE_NAME = "indexnow-manifest.json";

const NON_CONTENT_TAGS = ["script", "style", "noscript", "template"];
const CHROME_TAGS = ["header", "footer"];

function normalizeText(value) {
  return value.replace(/\s+/g, " ").trim();
}

/**
 * Returns a SHA-256 hex digest of a rendered page's title, meta description,
 * canonical URL and `<main>` text (`<body>` text without header and footer when
 * there is no `<main>`). Scripts, styles, asset URLs and build ids never reach
 * the digest.
 */
export function hashPageContent(html) {
  const document = parse(html, { comment: false });

  for (const node of document.querySelectorAll(NON_CONTENT_TAGS.join(","))) {
    node.remove();
  }

  const title = normalizeText(
    document.querySelector("title")?.textContent ?? "",
  );
  const description = normalizeText(
    document
      .querySelector('meta[name="description"]')
      ?.getAttribute("content") ?? "",
  );
  const canonical = normalizeText(
    document.querySelector('link[rel="canonical"]')?.getAttribute("href") ?? "",
  );

  let root = document.querySelector("main");
  if (!root) {
    root = document.querySelector("body") ?? document;
    for (const node of root.querySelectorAll(CHROME_TAGS.join(","))) {
      node.remove();
    }
  }

  return createHash("sha256")
    .update(
      JSON.stringify([
        title,
        description,
        canonical,
        normalizeText(root.textContent),
      ]),
    )
    .digest("hex");
}

function htmlCandidates(outputRoot, url) {
  const pathname = decodeURIComponent(new URL(url).pathname);
  const relative = pathname.replace(/^\//, "");
  return pathname === "/"
    ? [path.join(outputRoot, "index.html")]
    : [
        path.join(outputRoot, `${relative}.html`),
        path.join(outputRoot, relative, "index.html"),
      ];
}

async function readPageHtml(outputRoot, url) {
  const candidates = htmlCandidates(outputRoot, url);
  for (const candidate of candidates) {
    try {
      return await fs.readFile(candidate, "utf8");
    } catch (error) {
      if (error?.code !== "ENOENT") {
        throw error;
      }
    }
  }
  throw new Error(`No rendered page found for ${url}`);
}

/** Maps every sitemap URL to its content hash, ordered by URL. */
export async function buildManifest(outputRoot) {
  const xml = await fs.readFile(path.join(outputRoot, "sitemap.xml"), "utf8");
  const urls = [...new Set(parseSitemapUrls(xml))].sort();
  if (urls.length === 0) {
    throw new Error("No URLs found in the sitemap.");
  }

  const manifest = {};
  for (const url of urls) {
    manifest[url] = hashPageContent(await readPageHtml(outputRoot, url));
  }
  return manifest;
}

export async function writeManifest(outputRoot = OUTPUT_ROOT) {
  const manifest = await buildManifest(outputRoot);
  await fs.writeFile(
    path.join(outputRoot, MANIFEST_FILE_NAME),
    `${JSON.stringify(manifest, null, 2)}\n`,
    "utf8",
  );
  return Object.keys(manifest).length;
}

if (
  process.argv[1] &&
  import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href
) {
  const count = await writeManifest();
  console.log(`[indexnow] Wrote ${MANIFEST_FILE_NAME} with ${count} URLs.`);
}
