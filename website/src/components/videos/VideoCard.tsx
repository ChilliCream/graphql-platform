import Link from "next/link";
import { CARD_WASH_CLASS, CARD_WASH_STYLE } from "@/src/components/cardWash";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { formatDate } from "@/src/helpers/formatDate";
import { formatEpisode, type VideoSummary } from "@/src/helpers/videos";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import { VideoPoster } from "./VideoPoster";

interface VideoCardProps {
  readonly video: VideoSummary;
}

/** Teaser card linking to a video page. */
export function VideoCard({ video }: VideoCardProps) {
  return (
    <article className="group/video h-full w-full">
      <Link
        href={video.href}
        prefetch={false}
        className={`border-cc-ink-faint ${CARD_WASH_CLASS} hover:border-cc-card-border-hover flex h-full flex-col overflow-hidden rounded-2xl border no-underline transition-[background-color,border-color] duration-150`}
        style={CARD_WASH_STYLE}
      >
        <VideoPoster
          videoId={video.youtubeId}
          sizes="(max-width: 639px) 100vw, (max-width: 1023px) 50vw, 400px"
          className="border-cc-ink-faint w-full border-b"
        />
        <div className="flex flex-1 flex-col px-6 pt-5 pb-6">
          <Eyebrow
            as="div"
            size="2xs"
            color="ink-dim"
            className="flex items-center gap-3"
          >
            <span className="text-cc-amber">
              Ep {formatEpisode(video.episode)}
            </span>
            <time dateTime={video.date}>{formatDate(video.date)}</time>
            {video.example ? (
              <span className="border-cc-ink-faint text-cc-ink ml-auto rounded-md border py-1 pr-[calc(0.4rem-0.18em)] pl-1.5 leading-none">
                Source
              </span>
            ) : null}
          </Eyebrow>
          <h3 className="text-cc-heading m-0 mt-4 mb-3 text-xl leading-tight font-medium tracking-[-0.015em]">
            {video.title}
          </h3>
          <p className="text-cc-ink-dim m-0 mb-6 line-clamp-3 text-sm leading-[1.55]">
            {video.description}
          </p>
          <span className="text-cc-ink group-hover/video:text-cc-amber mt-auto inline-flex items-center gap-1.5 text-xs tracking-[0.18em] uppercase transition-colors">
            Watch
            <ArrowRightIcon className="size-3.5" />
          </span>
        </div>
      </Link>
    </article>
  );
}
