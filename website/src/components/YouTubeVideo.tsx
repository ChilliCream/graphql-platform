import { BrokenMedia } from "@/src/design-system/BrokenMedia";
import { VideoFacade } from "./VideoFacade";
import { YOUTUBE_ID_RE, YouTubePoster } from "./YouTubePoster";

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
  if (!YOUTUBE_ID_RE.test(videoId)) {
    return <BrokenMedia message="This video couldn't be loaded." />;
  }

  return (
    <div className="ring-cc-card-border my-6 overflow-hidden rounded-md ring-1">
      <VideoFacade videoId={videoId} playlabel={playlabel} location="article">
        <YouTubePoster videoId={videoId} />
      </VideoFacade>
    </div>
  );
}
