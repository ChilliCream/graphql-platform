import { ArrowLink } from "@/src/components/ArrowLink";
import { CardGrid } from "@/src/components/CardGrid";
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
 * `cols={4}` with `skipThreeCol` (website-kbx.9) renders `sm:grid-cols-2
 * lg:grid-cols-4`: 1 column at mobile, 2 from `sm`, 4 from `lg`, with no
 * 3-column step in between. The rail always holds up to 4 cards, so 3
 * columns would orphan the 4th onto its own row (website-kbx.4); jumping
 * straight from 2 to 4 keeps every row full at every breakpoint. At `lg`
 * (1024px) each card is ~214px wide, verified by inspection to still fit a
 * thumbnail, badge, kicker, title, and the dek without clipping.
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
      <CardGrid cols={4} step="progressive" skipThreeCol itemsStretch>
        {videos.map((video) => (
          <LearnCard key={video.slug} item={video} />
        ))}
      </CardGrid>
    </section>
  );
}
