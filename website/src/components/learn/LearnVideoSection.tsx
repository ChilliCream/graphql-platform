import { ArrowLink } from "@/src/components/ArrowLink";
import { CardGrid } from "@/src/components/CardGrid";
import { resolveYouTubePoster } from "@/src/components/YouTubePoster";
import type { VideoItem } from "@/src/data/learn/types";
import { LearnCard } from "./LearnCard";

interface LearnVideoSectionProps {
  readonly videos: readonly VideoItem[];
}

/**
 * Video rail (learn-editorial.md section 3.7, spacing amended by
 * learn-harmonization.md D2): the latest 3 to 4 `VIDEO_ITEMS` as
 * `LearnCard`s, which render a real YouTube poster thumbnail (with a
 * duration overlay when the video has one) for entries with a `youtubeId`.
 * Cards link to the native `/learn/videos/[slug]` detail page when the video
 * has a `youtubeId`, and open YouTube in a new tab for the two legacy
 * entries without one. The rail link goes to the catalog pre-filtered to
 * videos, not out to YouTube: the section's job is to route deeper into the
 * site. Keeps its `border-t` per the divider policy (section 2.2 item 4):
 * its body is a card grid with no row dividers.
 *
 * `cols={2}` (rather than the `4` its width could otherwise fit) keeps the
 * rail's up-to-4 cards in a clean 2-per-row layout at every breakpoint from
 * `sm` up: at `cols={4}`, the `lg` step (1024-1279px) lands on 3 columns,
 * which orphans the 4th card onto its own row (website-kbx.4). This is the
 * cheaper of the two harmonization treatments for a 4-item rail; the other,
 * a dedicated `lg`-skips-to-4-at-`xl` grid step, would need a new `CardGrid`
 * variant for this one call site.
 */
export function LearnVideoSection({ videos }: LearnVideoSectionProps) {
  if (videos.length === 0) {
    return null;
  }
  return (
    <section className="border-cc-card-border border-t py-8 sm:py-10">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">Watch</h2>
        <ArrowLink href="/learn/browse?type=video">Browse videos</ArrowLink>
      </div>
      <CardGrid cols={2} step="progressive" itemsStretch>
        {videos.map((video) => (
          <LearnCard
            key={video.slug}
            item={video}
            poster={video.youtubeId ? resolveYouTubePoster(video.youtubeId) : undefined}
          />
        ))}
      </CardGrid>
    </section>
  );
}
