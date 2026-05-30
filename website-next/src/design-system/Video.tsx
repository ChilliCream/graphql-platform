import { BrokenMedia } from "./BrokenMedia";
import { VideoFacade } from "./VideoFacade";
import { extractYouTubeId } from "@/src/helpers/extractYouTubeId";

type VideoProps = {
  /** YouTube video ID or any common YouTube URL form. */
  src: string;
  /** Visually hidden label for the play button. */
  playlabel?: string;
};

export function Video({ src, playlabel }: VideoProps) {
  const id = extractYouTubeId(src);
  if (!id) {
    return <BrokenMedia message="This video couldn't be loaded." />;
  }

  return (
    <div className="my-6 overflow-hidden rounded-md ring-1 ring-cc-card-border">
      <VideoFacade videoId={id} playlabel={playlabel} />
    </div>
  );
}
