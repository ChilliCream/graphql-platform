import Link from "next/link";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { formatDate } from "@/src/helpers/formatDate";
import {
  formatEpisode,
  VIDEOS_PATH,
  type VideoSummary,
} from "@/src/helpers/videos";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import { VideoPoster } from "./VideoPoster";

interface MoreVideosProps {
  readonly videos: readonly VideoSummary[];
  readonly className?: string;
}

/** Compact list of other videos, shown next to the video being watched. */
export function MoreVideos({ videos, className = "" }: MoreVideosProps) {
  if (videos.length === 0) {
    return null;
  }

  return (
    <aside
      aria-labelledby="more-videos"
      className={`flex flex-col gap-4 print:hidden ${className}`}
    >
      <Eyebrow as="h2" id="more-videos" color="ink-dim">
        More videos
      </Eyebrow>
      <ul className="m-0 flex list-none flex-col gap-1 p-0">
        {videos.map((video) => (
          <li key={video.stem} className="m-0 p-0">
            <Link
              href={video.href}
              prefetch={false}
              className="group/video hover:bg-cc-hover -mx-2 flex items-start gap-3 rounded-xl p-2 no-underline transition-colors"
            >
              <VideoPoster
                videoId={video.youtubeId}
                sizes="160px"
                playButton="sm"
                className="ring-cc-card-border w-40 flex-none rounded-lg ring-1"
              />
              <div className="min-w-0">
                <p className="text-cc-heading decoration-cc-ink-faint m-0 line-clamp-2 text-sm leading-snug font-medium underline-offset-4 group-hover/video:underline">
                  {video.title}
                </p>
                <Eyebrow
                  size="2xs"
                  color="ink-dim"
                  className="mt-1.5 flex gap-2.5"
                >
                  <span className="text-cc-amber">
                    Ep {formatEpisode(video.episode)}
                  </span>
                  <time dateTime={video.date}>{formatDate(video.date)}</time>
                </Eyebrow>
              </div>
            </Link>
          </li>
        ))}
      </ul>
      <Link
        href={VIDEOS_PATH}
        prefetch={false}
        className="text-cc-heading decoration-cc-ink-faint inline-flex items-center gap-1.5 text-sm font-medium no-underline underline-offset-4 hover:underline"
      >
        All videos
        <ArrowRightIcon className="size-3.5" />
      </Link>
    </aside>
  );
}
