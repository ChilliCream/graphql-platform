import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const IMAGE_RE = /\.(png|jpe?g|webp)$/i;
const CONCURRENCY = 6;
// Longest edge (px) of the tiny placeholder encoded into the blurDataURL.
const BLUR_SIZE = 8;

// Produce a tiny base64 placeholder (like next/image's blurDataURL): a small
// webp downscale that the component renders blurred until the image loads.
async function makeBlur(input) {
  const buffer = await sharp(input)
    .resize(BLUR_SIZE, BLUR_SIZE, { fit: "inside", withoutEnlargement: true })
    .webp({ quality: 50 })
    .toBuffer();
  const meta = await sharp(buffer).metadata();
  return {
    blurDataURL: `data:image/webp;base64,${buffer.toString("base64")}`,
    blurWidth: meta.width ?? BLUR_SIZE,
    blurHeight: meta.height ?? BLUR_SIZE,
  };
}

// Module-scope singleton guard: if optimizeImages runs twice in the same
// process (e.g. the Next config is evaluated more than once), do the work once.
let inFlight;

/**
 * @param {{ quality: number, widths: number[], formats: string[], sourceDir: string, outputDir: string, manifestPath: string }} config
 */
export default async function optimizeImages(config) {
  if (inFlight) {
    return inFlight;
  }
  inFlight = run(config);
  return inFlight;
}

async function run(config) {
  const cwd = process.cwd();
  const sourceDir = path.resolve(cwd, config.sourceDir);
  const outputDir = path.resolve(cwd, config.outputDir);
  const manifestPath = path.resolve(cwd, config.manifestPath);

  const { quality, widths, formats } = config;
  // Hash the encode settings so changing quality/widths/formats invalidates the cache.
  const settingsJson = JSON.stringify({ quality, widths, formats });

  const manifest = loadManifest(manifestPath);

  const sources = walk(sourceDir).filter((f) => IMAGE_RE.test(f));

  const newManifest = {};
  let encoded = 0;
  let cached = 0;

  const tasks = sources.map((file) => async () => {
    const relPath = path.relative(sourceDir, file).split(path.sep).join("/");
    const url = `/images/${relPath}`;
    try {
      const bytes = fs.readFileSync(file);
      const hash = crypto
        .createHash("sha256")
        .update(bytes)
        .update(settingsJson)
        .digest("hex");

      const existing = manifest[url];
      if (existing && existing.hash === hash && allOutputsExist(existing, outputDir)) {
        // Backfill the blur placeholder for manifests generated before blur
        // support, without re-encoding the (unchanged) full-size variants.
        newManifest[url] = existing.blurDataURL
          ? existing
          : { ...existing, ...(await makeBlur(bytes)) };
        cached++;
        return;
      }

      const meta = await sharp(file).metadata();
      const intrinsicW = meta.width ?? 0;
      const intrinsicH = meta.height ?? 0;

      const targetWidths = computeTargetWidths(widths, intrinsicW);

      const formatsEntry = {};
      for (const format of formats) {
        const variants = [];
        for (const width of targetWidths) {
          const outRel = `${relPath}.w${width}.${format}`;
          const outFile = path.join(outputDir, outRel);
          const buffer = await sharp(bytes)
            .resize({ width, withoutEnlargement: true })
            .toFormat(format, { quality })
            .toBuffer();
          writeAtomic(outFile, buffer);
          variants.push({ w: width, path: `/_optimized/images/${outRel}` });
        }
        variants.sort((a, b) => a.w - b.w);
        formatsEntry[format] = variants;
      }

      newManifest[url] = {
        hash,
        width: intrinsicW,
        height: intrinsicH,
        formats: formatsEntry,
        ...(await makeBlur(bytes)),
      };
      encoded++;
    } catch (err) {
      console.warn(`[image-opt] WARN failed to optimize ${url}: ${err.message}`);
      // Reuse the previous manifest entry if we had one, so the page can still
      // fall back gracefully on the next build.
      if (manifest[url]) {
        newManifest[url] = manifest[url];
      }
    }
  });

  await runPool(tasks, CONCURRENCY);

  // Prune entries whose source no longer exists and best-effort delete orphans.
  for (const [url, entry] of Object.entries(manifest)) {
    if (!newManifest[url]) {
      deleteOrphans(entry, outputDir, sourceDir);
    }
  }

  writeAtomic(manifestPath, Buffer.from(JSON.stringify(newManifest, null, 2)));

  console.log(
    `[image-opt] ${sources.length} sources, ${encoded} (re)encoded, ${cached} cached`
  );

  return newManifest;
}

function computeTargetWidths(ladder, intrinsicW) {
  if (!intrinsicW) {
    return [...ladder].sort((a, b) => a - b);
  }
  const smallest = Math.min(...ladder);
  if (intrinsicW < smallest) {
    return [intrinsicW];
  }
  const widths = ladder.filter((w) => w <= intrinsicW);
  // Add the intrinsic width if it is larger than all ladder widths, so the
  // largest variant equals the original size.
  if (intrinsicW > Math.max(...widths)) {
    widths.push(intrinsicW);
  }
  return [...new Set(widths)].sort((a, b) => a - b);
}

function allOutputsExist(entry, outputDir) {
  for (const variants of Object.values(entry.formats ?? {})) {
    for (const variant of variants) {
      const file = urlToOutputFile(variant.path, outputDir);
      if (!file || !fs.existsSync(file)) {
        return false;
      }
    }
  }
  return true;
}

function deleteOrphans(entry, outputDir) {
  for (const variants of Object.values(entry.formats ?? {})) {
    for (const variant of variants) {
      const file = urlToOutputFile(variant.path, outputDir);
      try {
        if (file && fs.existsSync(file)) {
          fs.unlinkSync(file);
        }
      } catch {
        // best-effort
      }
    }
  }
}

function urlToOutputFile(url, outputDir) {
  const prefix = "/_optimized/images/";
  if (!url || !url.startsWith(prefix)) {
    return null;
  }
  const rel = url.slice(prefix.length);
  return path.join(outputDir, rel.split("/").join(path.sep));
}

function loadManifest(manifestPath) {
  try {
    return JSON.parse(fs.readFileSync(manifestPath, "utf8"));
  } catch {
    return {};
  }
}

function walk(dir) {
  const out = [];
  let entries;
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return out;
  }
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      out.push(...walk(full));
    } else if (entry.isFile()) {
      out.push(full);
    }
  }
  return out;
}

function writeAtomic(file, buffer) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const tmp = `${file}.tmp-${process.pid}-${Math.random().toString(36).slice(2)}`;
  fs.writeFileSync(tmp, buffer);
  fs.renameSync(tmp, file);
}

async function runPool(tasks, concurrency) {
  let index = 0;
  const workers = Array.from({ length: Math.min(concurrency, tasks.length) }, async () => {
    while (index < tasks.length) {
      const current = index++;
      await tasks[current]();
    }
  });
  await Promise.all(workers);
}
