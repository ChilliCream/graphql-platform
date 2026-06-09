#!/usr/bin/env node
// Standalone image optimization step, run as part of the release workflow (and
// the local `ci-local.sh` script). Generates AVIF/WebP variants, blur
// placeholders and self-hosted copies of remote images into
// `public/_optimized/`, then writes the manifest the app reads at build time.
// Renders a progress indicator: a live bar on a TTY, decile milestones in CI.
import process from "node:process";
import optimizeImages from "../src/image-optimization/generate.mjs";

const config = {
  quality: 90,
  widths: [640, 1080, 1920],
  formats: ["avif", "webp"],
  sourceDir: "public/images",
  outputDir: "public/_optimized/images",
  manifestPath: "public/_optimized/manifest.json",
};

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
