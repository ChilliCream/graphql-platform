import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";
import { collectRemoteImages } from "./remote.mjs";

const IMAGE_RE = /\.(png|jpe?g|webp)$/i;
const CONCURRENCY = 6;
const REMOTE_CONCURRENCY = 4;
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
 * @param {{ quality: number, widths: number[], formats: string[], sourceDir: string, outputDir: string, manifestPath: string, share?: { width: number, height: number, quality: number, images: string[] }, onProgress?: (p: { phase: "images" | "remote", done: number, total: number }) => void }} config
 *
 * `share` requests a dedicated share-card variant (an exact `width`x`height`
 * cover-cropped JPEG, stored as `shareSrc` in the manifest) for the listed
 * source URLs. JPEG because share-card crawlers do not reliably decode
 * WebP/AVIF.
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
  const onProgress =
    typeof config.onProgress === "function" ? config.onProgress : null;
  // Hash the encode settings so changing quality/widths/formats invalidates the cache.
  const settingsJson = JSON.stringify({ quality, widths, formats });

  // Share variants are cached independently of the srcset ladder: `shareHash`
  // on a manifest entry tracks the share encode settings, so changing them
  // re-encodes only the share JPEGs, not every variant.
  const share = config.share ?? null;
  const shareSet = new Set(share?.images ?? []);
  const shareHash = share
    ? sha256(
        JSON.stringify({
          width: share.width,
          height: share.height,
          quality: share.quality,
        })
      )
    : null;

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

      const shareOptions = {
        wanted: shareSet.has(url),
        bytes,
        relPath,
        outputDir,
        share,
        shareHash,
      };

      const existing = manifest[url];
      if (existing && existing.hash === hash && allOutputsExist(existing, outputDir)) {
        // Backfill the blur placeholder for manifests generated before blur
        // support, without re-encoding the (unchanged) full-size variants.
        const entry = existing.blurDataURL
          ? existing
          : { ...existing, ...(await makeBlur(bytes)) };
        newManifest[url] = await ensureShareVariant(entry, shareOptions);
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

      newManifest[url] = await ensureShareVariant(
        {
          hash,
          width: intrinsicW,
          height: intrinsicH,
          formats: formatsEntry,
          ...(await makeBlur(bytes)),
        },
        shareOptions
      );
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

  let localDone = 0;
  await runPool(tasks, CONCURRENCY, () => {
    localDone++;
    onProgress?.({ phase: "images", done: localDone, total: sources.length });
  });

  // Self-host + optimize external images (avatars, YouTube thumbnails).
  const remotes = await collectRemoteImages(cwd);
  const remoteDir = path.join(outputDir, "remote");
  let remoteFetched = 0;
  let remoteCached = 0;

  const remoteTasks = remotes.map(({ key, url, fallbackUrl }) => async () => {
    try {
      const sha = sha256(key);
      // URL-based cache key: the remote content is assumed stable, so we do not
      // re-download on every build.
      const hash = sha256(key + settingsJson);

      const existing = manifest[key];
      if (
        existing &&
        existing.hash === hash &&
        existing.fallbackSrc &&
        existing.blurDataURL &&
        allOutputsExist(existing, outputDir) &&
        remoteFallbackExists(existing.fallbackSrc, outputDir)
      ) {
        newManifest[key] = existing;
        remoteCached++;
        return;
      }

      let res = await fetch(url);
      if (!res.ok && fallbackUrl) {
        res = await fetch(fallbackUrl);
      }
      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }
      const bytes = Buffer.from(await res.arrayBuffer());

      const ext = extFromContentType(res.headers.get("content-type"));
      const originalRel = `${sha}.${ext}`;
      writeAtomic(path.join(remoteDir, originalRel), bytes);
      const fallbackSrc = `/_optimized/images/remote/${originalRel}`;

      const meta = await sharp(bytes).metadata();
      const intrinsicW = meta.width ?? 0;
      const intrinsicH = meta.height ?? 0;
      const targetWidths = computeTargetWidths(widths, intrinsicW);

      const formatsEntry = {};
      for (const format of formats) {
        const variants = [];
        for (const width of targetWidths) {
          const outRel = `${sha}.w${width}.${format}`;
          const buffer = await sharp(bytes)
            .resize({ width, withoutEnlargement: true })
            .toFormat(format, { quality })
            .toBuffer();
          writeAtomic(path.join(remoteDir, outRel), buffer);
          variants.push({ w: width, path: `/_optimized/images/remote/${outRel}` });
        }
        variants.sort((a, b) => a.w - b.w);
        formatsEntry[format] = variants;
      }

      const blur = await makeBlur(bytes);

      newManifest[key] = {
        hash,
        width: intrinsicW,
        height: intrinsicH,
        formats: formatsEntry,
        ...blur,
        fallbackSrc,
      };
      remoteFetched++;
    } catch (err) {
      // Never abort the build on a remote failure (e.g. offline). Skip the
      // manifest entry so the component falls back to the external URL.
      console.warn(`[image-opt] WARN remote ${key}: ${err.message}`);
    }
  });

  let remoteDone = 0;
  await runPool(remoteTasks, REMOTE_CONCURRENCY, () => {
    remoteDone++;
    onProgress?.({ phase: "remote", done: remoteDone, total: remotes.length });
  });

  // Prune entries whose source no longer exists and best-effort delete orphans.
  for (const [url, entry] of Object.entries(manifest)) {
    if (!newManifest[url]) {
      deleteOrphans(entry, outputDir);
    }
  }

  writeAtomic(manifestPath, Buffer.from(JSON.stringify(newManifest, null, 2)));

  console.log(
    `[image-opt] ${sources.length} sources, ${encoded} (re)encoded, ${cached} cached` +
      `, ${remotes.length} remote (${remoteFetched} fetched, ${remoteCached} cached)`
  );

  return newManifest;
}

function sha256(input) {
  return crypto.createHash("sha256").update(input).digest("hex");
}

// Largest box with the share aspect ratio that fits inside the source image,
// capped at the configured share dimensions.
function shareTargetBox(share, intrinsicW, intrinsicH) {
  if (!intrinsicW || !intrinsicH) {
    return { width: share.width, height: share.height };
  }
  const scale = Math.min(intrinsicW / share.width, intrinsicH / share.height, 1);
  return {
    width: Math.max(1, Math.floor(share.width * scale)),
    height: Math.max(1, Math.floor(share.height * scale)),
  };
}

// Adds (or removes) the dedicated share-card JPEG for a manifest entry. The
// source-content hash on the entry already covers byte changes; `shareHash`
// covers share encode-settings changes, so an up-to-date variant is reused
// without re-encoding.
async function ensureShareVariant(
  entry,
  { wanted, bytes, relPath, outputDir, share, shareHash }
) {
  if (!wanted || !share) {
    if (!entry.shareSrc) {
      return entry;
    }
    // No longer a share image (e.g. frontmatter changed): drop the variant.
    const file = urlToOutputFile(entry.shareSrc, outputDir);
    try {
      if (file && fs.existsSync(file)) {
        fs.unlinkSync(file);
      }
    } catch {
      // best-effort
    }
    const { shareSrc, shareHash: _stale, ...rest } = entry;
    return rest;
  }

  const outRel = `${relPath}.share.jpg`;
  const outFile = path.join(outputDir, outRel);
  if (entry.shareSrc && entry.shareHash === shareHash && fs.existsSync(outFile)) {
    return entry;
  }

  // Cover-crop to the exact share aspect ratio; never upscale. A source
  // smaller than the target gets a proportionally scaled-down target box
  // (same aspect ratio) instead, since `withoutEnlargement` alone would skip
  // the aspect crop entirely.
  const box = shareTargetBox(share, entry.width, entry.height);
  const buffer = await sharp(bytes)
    .resize(box.width, box.height, { fit: "cover" })
    .jpeg({ quality: share.quality })
    .toBuffer();
  writeAtomic(outFile, buffer);
  return { ...entry, shareSrc: `/_optimized/images/${outRel}`, shareHash };
}

function extFromContentType(contentType) {
  const type = (contentType ?? "").toLowerCase();
  if (type.includes("image/png")) {
    return "png";
  }
  if (type.includes("image/webp")) {
    return "webp";
  }
  if (type.includes("image/jpeg")) {
    return "jpg";
  }
  return "jpg";
}

function remoteFallbackExists(fallbackSrc, outputDir) {
  const file = urlToOutputFile(fallbackSrc, outputDir);
  return Boolean(file) && fs.existsSync(file);
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
  const paths = Object.values(entry.formats ?? {})
    .flat()
    .map((variant) => variant.path);
  // Remote entries also keep the self-hosted original download on disk; prune
  // it too, otherwise orphaned originals accumulate under remote/.
  if (entry.fallbackSrc) {
    paths.push(entry.fallbackSrc);
  }
  if (entry.shareSrc) {
    paths.push(entry.shareSrc);
  }
  for (const url of paths) {
    const file = urlToOutputFile(url, outputDir);
    try {
      if (file && fs.existsSync(file)) {
        fs.unlinkSync(file);
      }
    } catch {
      // best-effort
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

async function runPool(tasks, concurrency, onTick) {
  let index = 0;
  const workers = Array.from({ length: Math.min(concurrency, tasks.length) }, async () => {
    while (index < tasks.length) {
      const current = index++;
      await tasks[current]();
      onTick?.();
    }
  });
  await Promise.all(workers);
}
