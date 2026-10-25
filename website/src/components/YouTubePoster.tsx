import { Image } from "@/src/design-system/Image";
import { getOptimizedImage } from "@/src/image-optimization/manifest";

interface YouTubePosterProps {
  /** 11-character YouTube video id. */
  readonly videoId: string;
  readonly sizes: string;
  readonly className?: string;
  /** Loads the poster eagerly with high priority, for above-the-fold posters. */
  readonly priority?: boolean;
}

/**
 * Renders the poster image of a YouTube video, using the self-hosted, optimized
 * AVIF/WebP variants when the image pipeline produced them.
 */
export function YouTubePoster({
  videoId,
  sizes,
  className,
  priority = false,
}: YouTubePosterProps) {
  const loading = priority ? "eager" : "lazy";
  const fetchPriority = priority ? "high" : undefined;
  const posterUrl = `https://i.ytimg.com/vi/${videoId}/maxresdefault.jpg`;
  const opt = getOptimizedImage(posterUrl);

  // hqdefault always exists; maxresdefault 404s for many videos, so the external
  // fallback (used only when the thumbnail wasn't self-hosted, e.g. an offline
  // build) points at hqdefault.
  if (!opt) {
    return (
      // eslint-disable-next-line @next/next/no-img-element
      <img
        src={`https://i.ytimg.com/vi/${videoId}/hqdefault.jpg`}
        sizes={sizes}
        alt=""
        loading={loading}
        fetchPriority={fetchPriority}
        decoding="async"
        className={className}
      />
    );
  }

  return (
    <picture className="contents">
      {opt.formats.avif && (
        <source
          type="image/avif"
          srcSet={opt.formats.avif.map((v) => `${v.path} ${v.w}w`).join(", ")}
          sizes={sizes}
        />
      )}
      {opt.formats.webp && (
        <source
          type="image/webp"
          srcSet={opt.formats.webp.map((v) => `${v.path} ${v.w}w`).join(", ")}
          sizes={sizes}
        />
      )}
      <Image
        src={opt.fallbackSrc ?? posterUrl}
        alt=""
        width={opt.width}
        height={opt.height}
        blurDataURL={opt.blurDataURL}
        blurWidth={opt.blurWidth}
        blurHeight={opt.blurHeight}
        loading={loading}
        fetchPriority={fetchPriority}
        decoding="async"
        className={className}
      />
    </picture>
  );
}
