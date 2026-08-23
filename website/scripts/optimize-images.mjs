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
import { base, profiles } from "../src/image-optimization/config.mjs";
import optimizeImages from "../src/image-optimization/generate.mjs";

const config = {
  ...base,
  share: {
    ...profiles.shareCards,
    images: listFeaturedArticleImages(),
  },
};

// Mirrors the featured-image resolution in resolveFeaturedImage in
// src/helpers/articles.ts (articles are flat `slug.md(x)` files under
// content/learn/articles). Only local /images/ paths can be optimized;
// absolute URLs are skipped.
function listFeaturedArticleImages() {
  const root = path.join(process.cwd(), "content/learn/articles");
  let entries;
  try {
    entries = fs.readdirSync(root, { withFileTypes: true });
  } catch {
    return [];
  }

  const images = new Set();
  for (const entry of entries) {
    if (!entry.isFile()) {
      continue;
    }
    const match = entry.name.match(/^(.+)\.mdx?$/i);
    if (!match) {
      continue;
    }
    const slug = match[1];
    const file = path.join(root, entry.name);
    try {
      const { data } = matter(fs.readFileSync(file, "utf8"));
      const raw = typeof data.featuredImage === "string" ? data.featuredImage : "";
      if (!raw) {
        continue;
      }
      const url = /^(https?:)?\/\//.test(raw)
        ? null // external URL: cannot self-optimize
        : raw.startsWith("/")
          ? raw
          : `/images/learn-articles/${slug}/${raw}`;
      if (url?.startsWith("/images/")) {
        images.add(url);
      }
    } catch {
      // unreadable article: skip, the build itself will surface the error
    }
  }
  return [...images];
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
    process.stdout.write(`\r[image-opt] ${label} ${bar} ${done}/${total} (${pct}%)`);
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
