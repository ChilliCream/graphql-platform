import Link from "next/link";
import { authorPageUrl } from "@/src/data/authors";
import { Picture } from "@/src/design-system/Picture";
import { formatDate } from "@/src/helpers/formatDate";
import type { VideoSummary } from "@/src/helpers/videos";

interface VideoBylineProps {
  readonly video: VideoSummary;
}

/** The presenter and publish date of a video. */
export function VideoByline({ video }: VideoBylineProps) {
  const date = (
    <time dateTime={video.date} className="text-cc-ink-dim block text-xs">
      {formatDate(video.date)}
    </time>
  );

  if (!video.author) {
    return date;
  }

  return (
    <div className="flex items-center gap-3">
      <Picture
        src={video.author.imageUrl}
        alt=""
        width={36}
        height={36}
        sizes="36px"
        className="size-9 flex-none rounded-full object-cover"
      />
      <div className="leading-tight">
        <Link
          href={authorPageUrl(video.author)}
          prefetch={false}
          className="text-cc-heading hover:text-cc-white block text-sm font-medium no-underline transition-colors"
        >
          {video.author.name}
        </Link>
        {date}
      </div>
    </div>
  );
}
