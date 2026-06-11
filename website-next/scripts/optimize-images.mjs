#!/usr/bin/env node
// Standalone image optimization step, run as part of the release workflow (and
// the local `ci-local.sh` script). Generates AVIF/WebP variants, blur
// placeholders and self-hosted copies of remote images into
// `public/_optimized/`, then writes the manifest the app reads at build time.
// Renders a progress indicator: a live bar on a TTY, decile milestones in CI.
import fs from "node:fs";
import path from "node:path";
import process from "node:process";
import matter from "gray-matter";
import optimizeImages from "../src/image-optimization/generate.mjs";

const config = {
  quality: 90,
  widths: [640, 1080, 1920],
  formats: ["avif", "webp"],
  sourceDir: "public/images",
  outputDir: "public/_optimized/images",
  manifestPath: "public/_optimized/manifest.json",
  // Featured blog images additionally get a dedicated 1200x630 share-card
  // JPEG (og:image / twitter:image / RSS enclosure). Exact Open Graph
  // dimensions avoid crawler-side cropping; JPEG because share-card crawlers
  // do not reliably decode WebP/AVIF.
  share: {
    width: 1200,
    height: 630,
    quality: 80,
    images: listFeaturedBlogImages(),
  },
};

// Mirrors the featured-image resolution in src/helpers/blogPosts.ts and the
// post layout in src/helpers/blogPaths.ts (a post is either `stem.md(x)` or
// `stem/stem.md(x)` under content/blog). Only local /images/ paths can be
// optimized; absolute URLs are skipped.
function listFeaturedBlogImages() {
  const root = path.join(process.cwd(), "content/blog");
  let entries;
  try {
    entries = fs.readdirSync(root, { withFileTypes: true });
  } catch {
    return [];
  }

  const images = new Set();
  for (const entry of entries) {
    const post = resolveBlogPost(root, entry);
    if (!post) {
      continue;
    }
    try {
      const { data } = matter(fs.readFileSync(post.file, "utf8"));
      const raw = typeof data.featuredImage === "string" ? data.featuredImage : "";
      if (!raw) {
        continue;
      }
      const url = /^(https?:)?\/\//.test(raw)
        ? null // external URL: cannot self-optimize
        : raw.startsWith("/")
          ? raw
          : `/images/blog/${post.stem}/${raw}`;
      if (url?.startsWith("/images/")) {
        images.add(url);
      }
    } catch {
      // unreadable post: skip, the build itself will surface the error
    }
  }
  return [...images];
}

function resolveBlogPost(root, entry) {
  if (entry.isDirectory()) {
    for (const ext of ["md", "mdx"]) {
      const file = path.join(root, entry.name, `${entry.name}.${ext}`);
      if (fs.existsSync(file)) {
        return { file, stem: entry.name };
      }
    }
    return null;
  }
  if (entry.isFile()) {
    const match = entry.name.match(/^(.+)\.mdx?$/i);
    return match ? { file: path.join(root, entry.name), stem: match[1] } : null;
  }
  return null;
}

const LABELS = { images: "Images", remote: "Remote" };
const isTTY = Boolean(process.stdout.isTTY);
let currentPhase = null;
let lastPct = -1;

function render({ phase, done, total }) {
  if (total === 0) {
    return;
  }
  if (phase !== currentPhase) {
    currentPhase = phase;
    lastPct = -1;
  }
  const label = (LABELS[phase] ?? phase).padEnd(6);
  const pct = Math.floor((done / total) * 100);

  if (isTTY) {
    const width = 30;
    const filled = Math.round((done / total) * width);
    const bar = "█".repeat(filled) + "░".repeat(width - filled);
    process.stdout.write(
      `\r[image-opt] ${label} ${bar} ${done}/${total} (${pct}%)`,
    );
    if (done === total) {
      process.stdout.write("\n");
    }
    return;
  }

  // Non-TTY (CI): one line per 10% to keep the log readable.
  if (pct !== lastPct && (pct % 10 === 0 || done === total)) {
    lastPct = pct;
    console.log(`[image-opt] ${label.trim()} ${done}/${total} (${pct}%)`);
  }
}

try {
  await optimizeImages({ ...config, onProgress: render });
} catch (err) {
  console.error(`[image-opt] failed: ${err?.stack ?? err}`);
  process.exit(1);
}
