import { BrokenMedia } from "@/src/design-system/BrokenMedia";
import { VideoFacade } from "./VideoFacade";
import { YouTubePoster } from "./YouTubePoster";

const ID_RE = /^[a-zA-Z0-9_-]{11}$/;
const POSTER_SIZES = "(min-width: 768px) 768px, 100vw";
const POSTER_CLASS =
  "h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]";

type YouTubeVideoProps = {
  /** 11-character YouTube video id. */
  videoId: string;
  /** Visually hidden label for the play button. */
  playlabel?: string;
  /**
   * Loads the player without a click once the visitor has given marketing
   * consent. For pages whose main content is the video.
   */
  autoload?: boolean;
  /** Poster `sizes` hint; defaults to a content-column width. */
  sizes?: string;
  /** Spacing and corner-radius classes for the outer frame. */
  className?: string;
};

/**
 * Embeds a YouTube video as a click-to-load facade. Resolves the self-hosted,
 * optimized poster (AVIF/WebP) for the id at build time, renders it here, and
 * hands it to the client <VideoFacade>, which only loads the player iframe once
 * clicked (or, with `autoload`, once marketing consent is given).
 */
export function YouTubeVideo({
  videoId,
  playlabel,
  autoload = false,
  sizes = POSTER_SIZES,
  className = "my-6 rounded-md",
}: YouTubeVideoProps) {
  if (!ID_RE.test(videoId)) {
    return <BrokenMedia message="This video couldn't be loaded." />;
  }

  return (
    <div className={`ring-cc-card-border overflow-hidden ring-1 ${className}`}>
      <VideoFacade videoId={videoId} playlabel={playlabel} autoload={autoload}>
        <YouTubePoster
          videoId={videoId}
          sizes={sizes}
          className={POSTER_CLASS}
        />
      </VideoFacade>
    </div>
  );
}
