import { VideoFacade } from "@/src/components/VideoFacade";
import { YOUTUBE_ID_RE, YouTubePoster } from "@/src/components/YouTubePoster";
import { BrokenMedia } from "@/src/design-system/BrokenMedia";

interface LearnVideoPlayerProps {
  /** 11-character YouTube video id. */
  readonly videoId: string;
  /** Video title, used to build the play button's accessible label. */
  readonly title: string;
}

/**
 * Click-to-load YouTube embed for the video detail page, framed with the
 * learn imagery treatment (`rounded-2xl` border, `border-cc-card-border`)
 * instead of `YouTubeVideo`'s article-chrome frame. Reuses `VideoFacade` for
 * the click-to-load behavior and `YouTubePoster` for poster resolution.
 */
export function LearnVideoPlayer({ videoId, title }: LearnVideoPlayerProps) {
  if (!YOUTUBE_ID_RE.test(videoId)) {
    return <BrokenMedia message="This video couldn't be loaded." className="aspect-video w-full rounded-2xl" />;
  }

  return (
    <div className="border-cc-card-border overflow-hidden rounded-2xl border">
      <VideoFacade videoId={videoId} playlabel={`Play ${title}`}>
        <YouTubePoster videoId={videoId} />
      </VideoFacade>
    </div>
  );
}
