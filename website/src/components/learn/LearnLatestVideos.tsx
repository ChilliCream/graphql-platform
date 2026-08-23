import Link from "next/link";
import { ArrowLink } from "@/src/components/ArrowLink";
import { youTubePosterFallback } from "@/src/components/youTubePosterUrl";
import type { ProductKey } from "@/src/data/learn/facets";
import { findHub, hubHref, primaryHubForLearnItem, type HubKey } from "@/src/data/learn/hubs";
import { topicLabelForProduct } from "./editorial";

export interface LatestVideoRailItem {
  readonly slug: string;
  readonly title: string;
  /** Present because callers only pass videos with a native embed, i.e. an internal /learn/videos/<slug> page. */
  readonly youtubeId: string;
  readonly duration?: string;
  readonly products: readonly ProductKey[];
  /** Explicit hub override, when set on the source `VideoItem` (`src/data/learn/hubs.ts`). */
  readonly hubs?: readonly HubKey[];
}

interface LearnLatestVideosProps {
  /** Up to 4 rows, newest first; the caller has already filtered to videos with a youtubeId. */
  readonly videos: readonly LatestVideoRailItem[];
}

/**
 * Right-rail "Latest videos" block (learn-harmonization.md rail treatments,
 * website-kbx.2), replacing the rejected image promo tile. Each row is a
 * 16:9 YouTube poster thumbnail (a duration badge overlays it only when the
 * video has one) plus a kicker and 2-line title, linking to the video's
 * internal /learn/videos/<slug> page. Uses the pure `youTubePosterUrl.ts`
 * builders rather than the `YouTubePoster` component, matching `LearnCard`'s
 * `VideoThumb`: a plain fallback poster keeps this row free of the
 * optimized-image manifest's `node:fs` import.
 */
export function LearnLatestVideos({ videos }: LearnLatestVideosProps) {
  if (videos.length === 0) {
    return null;
  }
  return (
    <div>
      <h2 className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">Latest videos</h2>
      <div className="mt-2 flex flex-col">
        {videos.map((video) => {
          const hub = primaryHubForLearnItem(video);
          const hubLabel = hub ? findHub(hub)?.label : undefined;
          return (
            <div
              key={video.slug}
              className="group/row border-cc-card-border relative flex flex-col gap-3 border-b py-5 sm:flex-row sm:items-center sm:gap-4 lg:flex-col lg:items-stretch lg:gap-3"
            >
              <div className="bg-cc-white/4 relative aspect-video overflow-hidden rounded-lg sm:w-44 sm:shrink-0 lg:w-full">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={youTubePosterFallback(video.youtubeId)}
                  alt=""
                  loading="lazy"
                  decoding="async"
                  className="h-full w-full object-cover transition-transform duration-300 group-hover/row:scale-[1.02]"
                />
                {video.duration ? (
                  <span className="bg-cc-surface/90 text-cc-ink absolute right-2 bottom-2 rounded-full px-2 py-0.5 font-mono text-[0.6875rem] tracking-wider">
                    {video.duration}
                  </span>
                ) : null}
              </div>
              <span className="flex flex-col gap-1.5">
                {hub ? (
                  <Link
                    href={hubHref(hub)}
                    className="text-cc-ink-dim hover:text-cc-accent relative z-10 w-fit font-mono text-xs tracking-wider uppercase no-underline transition-colors"
                  >
                    {hubLabel}
                  </Link>
                ) : (
                  <span className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">
                    {topicLabelForProduct(video.products)}
                  </span>
                )}
                <Link
                  href={`/learn/videos/${video.slug}`}
                  className="font-heading text-cc-heading text-h6 group-hover/row:text-cc-accent static line-clamp-2 font-semibold no-underline transition-colors"
                >
                  {video.title}
                  <span className="absolute inset-0" aria-hidden="true" />
                </Link>
              </span>
            </div>
          );
        })}
      </div>
      <ArrowLink href="/learn/browse?type=video" className="mt-6">
        All videos
      </ArrowLink>
    </div>
  );
}
