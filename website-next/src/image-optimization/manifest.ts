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
  const clean = src.split(/[?#]/)[0];
  return m[clean] ?? null;
}
