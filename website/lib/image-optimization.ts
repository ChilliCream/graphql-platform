import fs from "fs";
import path from "path";
import sharp from "sharp";
import crypto from "crypto";

/**
 * Gatsby-like image optimization system for Next.js static export.
 *
 * Features:
 * - Automatic WebP/AVIF format conversion
 * - Responsive image generation (multiple widths for srcset)
 * - Blur-up placeholder generation (tiny base64 images)
 * - Build-time caching via content-hash to avoid reprocessing unchanged images
 * - Path-keyed image map (public/optimized/image-map.json) for runtime lookups
 */

const CACHE_DIR = path.join(process.cwd(), ".next/cache/images");
const OUTPUT_DIR = path.join(process.cwd(), "public/optimized");
const IMAGE_MAP_PATH = path.join(OUTPUT_DIR, "image-map.json");
const PUBLIC_PREFIX = "/optimized";

// Default responsive widths matching common viewport breakpoints
const DEFAULT_WIDTHS = [320, 640, 960, 1280, 1920];
const DEFAULT_QUALITY = 80;
const PLACEHOLDER_WIDTH = 20;

interface ImageCache {
  [hash: string]: CachedImage;
}

interface CachedImage {
  srcSet: SrcSetEntry[];
  placeholder: string;
  width: number;
  height: number;
  aspectRatio: number;
}

interface SrcSetEntry {
  src: string;
  width: number;
  format: string;
}

export interface OptimizedImageProps {
  /** Default src (largest WebP variant) */
  src: string;
  /** WebP srcSet string for <img srcset=""> */
  srcSet: string;
  /** AVIF srcSet string for <source> in <picture> */
  srcSetAvif: string;
  /** Original format srcSet for fallback */
  srcSetOriginal: string;
  /** Tiny base64 placeholder for blur-up effect */
  placeholder: string;
  /** Original image width */
  width: number;
  /** Original image height */
  height: number;
  /** Width/Height ratio */
  aspectRatio: number;
  /** Available sizes hint */
  sizes: string;
}

/** Compact format stored in image-map.json (keyed by original path) */
export interface ImageMapEntry {
  /** Default WebP src */
  s: string;
  /** WebP srcSet */
  w: string;
  /** AVIF srcSet */
  a: string;
  /** Base64 placeholder */
  p: string;
  /** width */
  W: number;
  /** height */
  H: number;
}

let imageCache: ImageCache | null = null;
let imageMap: Record<string, ImageMapEntry> | null = null;

function getCacheFilePath(): string {
  return path.join(CACHE_DIR, "manifest.json");
}

function loadCache(): ImageCache {
  if (imageCache) return imageCache;

  const cacheFile = getCacheFilePath();

  try {
    if (fs.existsSync(cacheFile)) {
      imageCache = JSON.parse(fs.readFileSync(cacheFile, "utf-8"));
      return imageCache!;
    }
  } catch {
    // Cache corrupted, start fresh
  }

  imageCache = {};
  return imageCache;
}

function saveCache(): void {
  if (!imageCache) return;

  fs.mkdirSync(CACHE_DIR, { recursive: true });
  fs.writeFileSync(getCacheFilePath(), JSON.stringify(imageCache, null, 2));
}

function loadImageMap(): Record<string, ImageMapEntry> {
  if (imageMap) return imageMap;

  try {
    if (fs.existsSync(IMAGE_MAP_PATH)) {
      imageMap = JSON.parse(fs.readFileSync(IMAGE_MAP_PATH, "utf-8"));
      return imageMap!;
    }
  } catch {
    // Start fresh
  }

  imageMap = {};
  return imageMap;
}

function saveImageMap(): void {
  if (!imageMap) return;

  ensureOutputDir();
  fs.writeFileSync(IMAGE_MAP_PATH, JSON.stringify(imageMap));
}

function getFileHash(filePath: string): string {
  const content = fs.readFileSync(filePath);
  return crypto.createHash("md5").update(content).digest("hex").slice(0, 12);
}

function ensureOutputDir(): void {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

/**
 * Generate a tiny base64 placeholder image for blur-up effect.
 */
async function generatePlaceholder(inputPath: string): Promise<string> {
  const buffer = await sharp(inputPath)
    .resize(PLACEHOLDER_WIDTH)
    .blur(2)
    .webp({ quality: 20 })
    .toBuffer();

  return `data:image/webp;base64,${buffer.toString("base64")}`;
}

/**
 * Process a single image at a specific width and format.
 */
async function processVariant(
  inputPath: string,
  outputName: string,
  width: number,
  format: "webp" | "avif" | "original",
  quality: number
): Promise<SrcSetEntry> {
  ensureOutputDir();

  const image = sharp(inputPath);
  const metadata = await image.metadata();

  // Don't upscale images
  const targetWidth = Math.min(width, metadata.width || width);

  let pipeline = sharp(inputPath).resize(targetWidth);

  let ext: string;
  switch (format) {
    case "webp":
      pipeline = pipeline.webp({ quality });
      ext = "webp";
      break;
    case "avif":
      pipeline = pipeline.avif({ quality: Math.round(quality * 0.8) });
      ext = "avif";
      break;
    case "original":
      ext = path.extname(inputPath).slice(1) || "png";
      if (ext === "jpg" || ext === "jpeg") {
        pipeline = pipeline.jpeg({ quality });
      } else if (ext === "png") {
        pipeline = pipeline.png({ quality });
      }
      break;
    default:
      ext = "webp";
      pipeline = pipeline.webp({ quality });
  }

  const outputFile = `${outputName}-${targetWidth}w.${ext}`;
  const outputPath = path.join(OUTPUT_DIR, outputFile);

  if (!fs.existsSync(outputPath)) {
    await pipeline.toFile(outputPath);
  }

  return {
    src: `${PUBLIC_PREFIX}/${outputFile}`,
    width: targetWidth,
    format: ext,
  };
}

/**
 * Optimize an image for web delivery.
 *
 * @param imagePath - Path to the source image relative to public/ or absolute
 * @param options - Configuration options
 * @returns Optimized image props for use with OptimizedImage component
 */
export async function getOptimizedImageProps(
  imagePath: string,
  options: {
    widths?: number[];
    quality?: number;
    sizes?: string;
  } = {}
): Promise<OptimizedImageProps> {
  const {
    widths = DEFAULT_WIDTHS,
    quality = DEFAULT_QUALITY,
    sizes = "(max-width: 640px) 100vw, (max-width: 1024px) 75vw, 50vw",
  } = options;

  // Resolve to absolute file path
  const absolutePath = imagePath.startsWith("/")
    ? path.join(process.cwd(), "public", imagePath)
    : imagePath;

  if (!fs.existsSync(absolutePath)) {
    // Return fallback for missing images
    return {
      src: imagePath,
      srcSet: "",
      srcSetAvif: "",
      srcSetOriginal: "",
      placeholder: "",
      width: 0,
      height: 0,
      aspectRatio: 0,
      sizes,
    };
  }

  const hash = getFileHash(absolutePath);
  const cache = loadCache();
  const map = loadImageMap();

  // Check cache
  if (cache[hash]) {
    const cached = cache[hash];
    const result = buildPropsFromCached(cached, imagePath, sizes);

    // Always update the path-keyed image map
    map[imagePath] = {
      s: result.src,
      w: result.srcSet,
      a: result.srcSetAvif,
      p: cached.placeholder,
      W: cached.width,
      H: cached.height,
    };

    return result;
  }

  // Get original image metadata
  const metadata = await sharp(absolutePath).metadata();
  const originalWidth = metadata.width || 800;
  const originalHeight = metadata.height || 600;
  const aspectRatio = originalWidth / originalHeight;

  // Generate output name based on original path and hash
  const baseName = path.basename(imagePath, path.extname(imagePath));
  const outputName = `${baseName}-${hash}`;

  // Generate all variants
  const allEntries: SrcSetEntry[] = [];

  for (const width of widths) {
    if (width > originalWidth * 1.1) continue; // Skip if much larger than original

    // WebP
    const webpEntry = await processVariant(
      absolutePath,
      outputName,
      width,
      "webp",
      quality
    );
    allEntries.push(webpEntry);

    // AVIF
    const avifEntry = await processVariant(
      absolutePath,
      outputName,
      width,
      "avif",
      quality
    );
    allEntries.push(avifEntry);

    // Original format
    const origEntry = await processVariant(
      absolutePath,
      outputName,
      width,
      "original",
      quality
    );
    allEntries.push(origEntry);
  }

  // Generate placeholder
  const placeholder = await generatePlaceholder(absolutePath);

  // Cache the results
  cache[hash] = {
    srcSet: allEntries,
    placeholder,
    width: originalWidth,
    height: originalHeight,
    aspectRatio,
  };
  saveCache();

  const result = buildPropsFromCached(cache[hash], imagePath, sizes);

  // Update the path-keyed image map
  map[imagePath] = {
    s: result.src,
    w: result.srcSet,
    a: result.srcSetAvif,
    p: placeholder,
    W: originalWidth,
    H: originalHeight,
  };

  return result;
}

function buildPropsFromCached(
  cached: CachedImage,
  imagePath: string,
  sizes: string
): OptimizedImageProps {
  const webpEntries = cached.srcSet.filter((e) => e.format === "webp");
  const avifEntries = cached.srcSet.filter((e) => e.format === "avif");
  const origEntries = cached.srcSet.filter(
    (e) => e.format !== "webp" && e.format !== "avif"
  );

  return {
    src: webpEntries[webpEntries.length - 1]?.src || imagePath,
    srcSet: webpEntries.map((e) => `${e.src} ${e.width}w`).join(", "),
    srcSetAvif: avifEntries.map((e) => `${e.src} ${e.width}w`).join(", "),
    srcSetOriginal: origEntries.map((e) => `${e.src} ${e.width}w`).join(", "),
    placeholder: cached.placeholder,
    width: cached.width,
    height: cached.height,
    aspectRatio: cached.aspectRatio,
    sizes,
  };
}

/**
 * Batch optimize all images in one or more directories.
 * Writes a path-keyed image-map.json for runtime lookups.
 */
export async function optimizeDirectory(
  dir: string | string[],
  options: { widths?: number[]; quality?: number } = {}
): Promise<void> {
  const dirs = Array.isArray(dir) ? dir : [dir];
  const extensions = [".png", ".jpg", ".jpeg", ".webp"];

  for (const d of dirs) {
    if (!fs.existsSync(d)) {
      console.log(`Skipping ${d} (not found)`);
      continue;
    }

    const files = fs.readdirSync(d, { recursive: true }) as string[];

    for (const file of files) {
      const ext = path.extname(file).toLowerCase();
      if (!extensions.includes(ext)) continue;

      const fullPath = path.join(d, file);
      const stat = fs.statSync(fullPath);
      if (!stat.isFile()) continue;

      const relativePath =
        "/" + path.relative(path.join(process.cwd(), "public"), fullPath);
      console.log(`Optimizing: ${relativePath}`);

      try {
        await getOptimizedImageProps(relativePath, options);
      } catch (err) {
        console.error(`Failed to optimize ${relativePath}:`, err);
      }
    }
  }

  // Write the path-keyed image map for runtime use
  saveImageMap();
  console.log("Image optimization complete.");
  console.log(`Image map written to ${IMAGE_MAP_PATH}`);
}
