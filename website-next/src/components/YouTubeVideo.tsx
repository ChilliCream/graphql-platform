import { BrokenMedia } from "@/src/design-system/BrokenMedia";
import { Image } from "@/src/design-system/Image";
import { getOptimizedImage } from "@/src/image-optimization/manifest";
import { VideoFacade } from "./VideoFacade";

const ID_RE = /^[a-zA-Z0-9_-]{11}$/;
const POSTER_SIZES = "(min-width: 768px) 768px, 100vw";
const POSTER_CLASS =
  "h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]";

type YouTubeVideoProps = {
  /** 11-character YouTube video id. */
  videoId: string;
  /** Visually hidden label for the play button. */
  playlabel?: string;
};

/**
 * Embeds a YouTube video as a click-to-load facade. Resolves the self-hosted,
 * optimized poster (AVIF/WebP) for the id at build time, renders it here, and
 * hands it to the client <VideoFacade>, which only loads the player iframe once
 * clicked.
 */
export function YouTubeVideo({ videoId, playlabel }: YouTubeVideoProps) {
  if (!ID_RE.test(videoId)) {
    return <BrokenMedia message="This video couldn't be loaded." />;
  }

  const posterUrl = `https://i.ytimg.com/vi/${videoId}/maxresdefault.jpg`;
  const opt = getOptimizedImage(posterUrl);

  // hqdefault always exists; maxresdefault 404s for many videos, so the external
  // fallback (used only when the thumbnail wasn't self-hosted, e.g. an offline
  // build) points at hqdefault.
  const poster = opt ? (
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
        className={POSTER_CLASS}
      />
    </picture>
  ) : (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src={`https://i.ytimg.com/vi/${videoId}/hqdefault.jpg`}
      sizes={POSTER_SIZES}
      alt=""
      loading="lazy"
      decoding="async"
      className={POSTER_CLASS}
    />
  );

  return (
    <div className="my-6 overflow-hidden rounded-md ring-1 ring-cc-card-border">
      <VideoFacade videoId={videoId} playlabel={playlabel}>
        {poster}
      </VideoFacade>
    </div>
  );
}
