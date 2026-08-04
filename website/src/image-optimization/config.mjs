// Single source of truth for the image-optimization settings. `base` applies
// to all images (local and remote); `profiles` layer use-case-specific
// overrides on top for images that need something other than the defaults.

export const base = {
  quality: 90,
  // 828 fills the 640->1080 gap: content columns top out around 1024px CSS,
  // so ~700-828px slots (tablet 1x, phone 2x) would otherwise be forced up to
  // the 1080 variant.
  widths: [640, 828, 1080, 1920],
  formats: ["avif", "webp"],
  sourceDir: "public/images",
  outputDir: "public/_optimized/images",
  manifestPath: "public/_optimized/manifest.json",
};

export const profiles = {
  // Author avatars render at 30px (see BlogMetadata), so the srcset ladder
  // only needs 1x/2x/3x DPR variants instead of the content-image ladder.
  // `fetchSize` asks GitHub for a pre-scaled avatar (largest ladder width at
  // 2x) instead of downloading the full-size original.
  avatars: {
    widths: [30, 60, 90],
    fetchSize: 180,
  },
  // Featured blog images additionally get a dedicated share-card variant
  // (og:image / twitter:image / RSS enclosure). Exact Open Graph dimensions
  // avoid crawler-side cropping; JPEG because share-card crawlers do not
  // reliably decode WebP/AVIF.
  shareCards: {
    width: 1200,
    height: 630,
    quality: 80,
  },
  // Featured, body, and teaser images use `base` unchanged; add a profile
  // here only when a use case needs different settings.
};
