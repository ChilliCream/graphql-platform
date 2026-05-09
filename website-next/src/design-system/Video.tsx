import { YouTubeEmbed } from "@next/third-parties/google";
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
    return null;
  }

  return (
    <div className="my-6 overflow-hidden rounded-lg ring-2 ring-amber-300">
      <YouTubeEmbed
        videoid={id}
        playlabel={playlabel}
        style="max-width: 100%;"
      />
    </div>
  );
}
