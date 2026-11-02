import Link from "next/link";
import { MockWindowChrome } from "@/src/components/MockWindowChrome";
import { YouTubePlayButton } from "@/src/components/YouTubePlayButton";
import { YouTubePoster } from "@/src/components/YouTubePoster";
import { authorPageUrl } from "@/src/data/authors";
import { Picture } from "@/src/design-system/Picture";
import { formatDate } from "@/src/helpers/formatDate";
import { formatEpisode, type VideoSummary } from "@/src/helpers/videos";
import { PlayGlyph } from "@/src/icons/PlayGlyph";

interface VideoStageProps {
  /** Videos, newest first. The first one is featured, the rest are up next. */
  readonly videos: readonly VideoSummary[];
  readonly upNextCount: number;
  readonly channelUrl: string;
}

/**
 * A watch page in a window, modeled on YouTube: the latest video with its
 * details, and an up-next list of the other videos beside it. Every video
 * links to its episode page.
 */
export function VideoStage({
  videos,
  upNextCount,
  channelUrl,
}: VideoStageProps) {
  const [current, ...others] = videos;
  if (!current) {
    return null;
  }
  const upNext = others.slice(0, upNextCount);

  return (
    <MockWindowChrome
      header={{
        variant: "custom",
        content: (
          <span className="text-cc-heading inline-flex items-center gap-2 text-sm font-semibold tracking-tight">
            <span className="bg-cc-youtube text-cc-white inline-flex h-4.5 w-6.5 items-center justify-center rounded-[5px]">
              <PlayGlyph className="size-3" />
            </span>
            ChilliCream TV
          </span>
        ),
      }}
      headerClassName="flex items-center px-4 py-2.5"
      shadow="soft"
    >
      <div className="grid sm:grid-cols-[minmax(0,1fr)_16rem]">
        <div className="min-w-0 p-3 pb-4 sm:pr-0">
          <Link
            href={current.href}
            prefetch={false}
            aria-label={`Watch ${current.title}`}
            className="group bg-cc-black relative block aspect-video overflow-hidden rounded-xl"
          >
            <YouTubePoster
              videoId={current.youtubeId}
              sizes="(max-width: 1023px) 100vw, 520px"
              priority
              className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]"
            />
            <YouTubePlayButton />
          </Link>

          <p className="text-cc-heading m-0 mt-3 text-[0.95rem] leading-snug font-semibold">
            {current.title}
          </p>

          <div className="mt-3 flex flex-wrap items-center justify-between gap-x-4 gap-y-3">
            {current.author ? (
              <Link
                href={authorPageUrl(current.author)}
                prefetch={false}
                className="flex items-center gap-2.5 no-underline"
              >
                <span className="bg-cc-surface size-8 flex-none overflow-hidden rounded-full">
                  <Picture
                    src={current.author.imageUrl}
                    alt=""
                    width={32}
                    height={32}
                    sizes="32px"
                    className="size-8 object-cover"
                  />
                </span>
                <span className="leading-tight">
                  <span className="text-cc-heading block text-xs font-semibold">
                    {current.author.name}
                  </span>
                  <span className="text-cc-ink-dim block text-[0.68rem]">
                    ChilliCream TV
                  </span>
                </span>
              </Link>
            ) : null}
            <a
              href={channelUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="bg-cc-heading text-cc-surface hover:bg-cc-white inline-flex items-center rounded-full px-3 py-1.5 text-xs font-medium no-underline transition-colors"
            >
              Subscribe
            </a>
          </div>

          <div className="bg-cc-white/5 mt-3 rounded-xl px-3 py-2.5 text-xs leading-relaxed">
            <p className="m-0 flex gap-3">
              <span className="text-cc-heading font-semibold">
                Episode {formatEpisode(current.episode)}
              </span>
              <time dateTime={current.date} className="text-cc-ink-dim">
                {formatDate(current.date)}
              </time>
            </p>
            <p className="text-cc-ink m-0 mt-1 line-clamp-2">
              {current.description}
            </p>
          </div>
        </div>

        {upNext.length > 0 ? (
          <div className="min-w-0 p-3">
            <p className="text-cc-heading m-0 mb-2 px-1 text-xs font-semibold">
              Up next
            </p>
            <ul className="m-0 flex list-none flex-col gap-1 p-0">
              {upNext.map((video) => (
                <li key={video.stem} className="m-0 p-0">
                  <Link
                    href={video.href}
                    prefetch={false}
                    className="group/video hover:bg-cc-hover -mx-1 flex items-start gap-2.5 rounded-lg p-1 no-underline transition-colors"
                  >
                    <div className="bg-cc-surface relative aspect-video w-20 flex-none overflow-hidden rounded-lg">
                      <YouTubePoster
                        videoId={video.youtubeId}
                        sizes="80px"
                        className="h-full w-full object-cover transition-transform duration-300 group-hover/video:scale-[1.03]"
                      />
                      <span className="bg-cc-black/80 text-cc-white absolute right-0.5 bottom-0.5 rounded-sm px-1 font-mono text-[0.5rem] leading-3 font-medium">
                        EP {formatEpisode(video.episode)}
                      </span>
                    </div>
                    <div className="min-w-0">
                      <p className="text-cc-heading m-0 line-clamp-2 text-xs leading-snug font-medium">
                        {video.title}
                      </p>
                      {video.author ? (
                        <p className="text-cc-ink-dim m-0 mt-1 text-[0.65rem]">
                          {video.author.name}
                        </p>
                      ) : null}
                      <p className="text-cc-ink-dim m-0 text-[0.65rem]">
                        <time dateTime={video.date}>
                          {formatDate(video.date)}
                        </time>
                      </p>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ) : null}
      </div>
    </MockWindowChrome>
  );
}
