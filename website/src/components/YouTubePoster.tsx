import { Image } from "@/src/design-system/Image";
import { getOptimizedImage } from "@/src/image-optimization/manifest";

/** 11-character YouTube video id shape (the `v` query param). */
export const YOUTUBE_ID_RE = /^[a-zA-Z0-9_-]{11}$/;

const POSTER_SIZES = "(min-width: 768px) 768px, 100vw";
const POSTER_CLASS = "h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]";

interface YouTubePosterProps {
  /** 11-character YouTube video id, already validated against {@link YOUTUBE_ID_RE}. */
  readonly videoId: string;
  readonly className?: string;
}

/**
 * Resolves and renders the poster image for a YouTube video: the self-hosted,
 * optimized `maxresdefault` (AVIF/WebP) when the build generated one, falling
 * back to the external `hqdefault` thumbnail otherwise. Shared by every
 * click-to-load YouTube facade on the site.
 */
export function YouTubePoster({ videoId, className = POSTER_CLASS }: YouTubePosterProps) {
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
        sizes={POSTER_SIZES}
        alt=""
        loading="lazy"
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
          sizes={POSTER_SIZES}
        />
      )}
      {opt.formats.webp && (
        <source
          type="image/webp"
          srcSet={opt.formats.webp.map((v) => `${v.path} ${v.w}w`).join(", ")}
          sizes={POSTER_SIZES}
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
        loading="lazy"
        decoding="async"
        className={className}
      />
    </picture>
  );
}
