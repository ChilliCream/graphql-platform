import { ArrowLink } from "@/src/components/ArrowLink";
import { CardGrid } from "@/src/components/CardGrid";
import type { VideoItem } from "@/src/data/learn/types";
import { LearnCard } from "./LearnCard";

interface LearnVideoSectionProps {
  readonly videos: readonly VideoItem[];
}

/**
 * Video rail (learn-editorial.md section 3.7, spacing amended by
 * learn-harmonization.md D2): the seeded `VIDEO_ITEMS` as `LearnCard`s.
 * Cards link to the native `/learn/videos/[slug]` detail page when the video
 * has a `youtubeId`, and open YouTube in a new tab for the two legacy
 * entries without one. No inline player on the landing: every tile stays a
 * uniform card. Keeps its `border-t` per the divider policy (section 2.2
 * item 4): its body is a card grid with no row dividers.
 */
export function LearnVideoSection({ videos }: LearnVideoSectionProps) {
  if (videos.length === 0) {
    return null;
  }
  return (
    <section className="border-cc-card-border border-t py-10 sm:py-12">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">Watch</h2>
        <ArrowLink href="https://www.youtube.com/c/ChilliCream" target="_blank" rel="noopener noreferrer">
          YouTube channel
        </ArrowLink>
      </div>
      <CardGrid cols={3} step="progressive" itemsStretch>
        {videos.map((video) => (
          <LearnCard key={video.slug} item={video} />
        ))}
      </CardGrid>
    </section>
  );
}
