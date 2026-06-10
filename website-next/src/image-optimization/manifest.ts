import fs from "node:fs";
import path from "node:path";

export interface OptimizedVariant {
  w: number;
  path: string;
}
export interface OptimizedImage {
  width: number;
  height: number;
  formats: Partial<Record<"avif" | "webp", OptimizedVariant[]>>;
  /** Tiny base64 placeholder shown (blurred) until the full image loads. */
  blurDataURL?: string;
  /** Intrinsic dimensions of the blur placeholder image (for the blur SVG). */
  blurWidth?: number;
  blurHeight?: number;
  /** Self-hosted URL to render as the `<img>` src for remote (external) images. */
  fallbackSrc?: string;
}

let cache: Record<string, OptimizedImage> | null | undefined;

function load(): Record<string, OptimizedImage> | null {
  if (cache !== undefined) {
    return cache;
  }
  try {
    const raw = fs.readFileSync(
      path.join(process.cwd(), "public/_optimized/manifest.json"),
      "utf8"
    );
    cache = JSON.parse(raw);
  } catch {
    cache = null; // dev / unoptimized / not generated yet
  }
  return cache ?? null;
}

export function getOptimizedImage(src: string): OptimizedImage | null {
  const m = load();
  if (!m || !src) {
    return null;
  }
  // Absolute URLs match the manifest key exactly (query strings are part of the
  // key, e.g. avatar `?v=4`); local paths still strip query/hash.
  const clean = /^https?:\/\//i.test(src) ? src : src.split(/[?#]/)[0];
  return m[clean] ?? null;
}
