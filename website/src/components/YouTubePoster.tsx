import { Image } from "@/src/design-system/Image";
import { getOptimizedImage } from "@/src/image-optimization/manifest";
import { YOUTUBE_ID_RE, youTubePosterFallback, youTubePosterKey } from "./youTubePosterUrl";

// Re-exported for existing callers; the definitions live in
// `youTubePosterUrl.ts` so a client component can import just the URL
// builders without pulling this file's `node:fs`-based manifest lookup into
// its bundle (see that file's header comment).
export { YOUTUBE_ID_RE, youTubePosterFallback, youTubePosterKey };

const POSTER_SIZES = "(min-width: 768px) 768px, 100vw";
const POSTER_CLASS = "h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]";

/**
 * Self-hosted optimized poster URL for a video id, resolved from the same
 * manifest {@link YouTubePoster} uses. `undefined` when the build hasn't
 * generated one (development, or the id isn't in the manifest): callers fall
 * back to {@link youTubePosterFallback}. Server-only (imports the
 * `node:fs`-based manifest); do not import this from a client component.
 */
export function resolveYouTubePoster(videoId: string): string | undefined {
  return getOptimizedImage(youTubePosterKey(videoId))?.fallbackSrc ?? undefined;
}

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
  const posterUrl = youTubePosterKey(videoId);
  const opt = getOptimizedImage(posterUrl);

  // The external fallback (used only when the thumbnail wasn't self-hosted,
  // e.g. an offline build) points at hqdefault; see youTubePosterFallback.
  if (!opt) {
    return (
      // eslint-disable-next-line @next/next/no-img-element
      <img
        src={youTubePosterFallback(videoId)}
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
