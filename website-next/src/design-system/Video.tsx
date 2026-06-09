import { BrokenMedia } from "./BrokenMedia";
import { VideoFacade } from "./VideoFacade";
import { extractYouTubeId } from "@/src/helpers/extractYouTubeId";
import { getOptimizedImage } from "@/src/image-optimization/manifest";

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

  const posterUrl = `https://i.ytimg.com/vi/${id}/maxresdefault.jpg`;
  const opt = getOptimizedImage(posterUrl);
  const poster = opt
    ? {
        fallbackSrc: opt.fallbackSrc ?? posterUrl,
        avifSrcSet: opt.formats.avif?.map((v) => `${v.path} ${v.w}w`).join(", "),
        webpSrcSet: opt.formats.webp?.map((v) => `${v.path} ${v.w}w`).join(", "),
        blurDataURL: opt.blurDataURL,
        blurWidth: opt.blurWidth,
        blurHeight: opt.blurHeight,
        width: opt.width,
        height: opt.height,
      }
    : undefined;

  return (
    <div className="my-6 overflow-hidden rounded-md ring-1 ring-cc-card-border">
      <VideoFacade videoId={id} playlabel={playlabel} poster={poster} />
    </div>
  );
}
