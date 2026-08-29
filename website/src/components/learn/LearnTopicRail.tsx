import { ArrowLink } from "@/src/components/ArrowLink";
import { CardGrid } from "@/src/components/CardGrid";
import { hubKickerForPost } from "@/src/data/learn/hubs";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { LearnArticleCard } from "./LearnArticleCard";

/**
 * Card kicker inside a headed section: category, falling back to the
 * generic "Article" content-type label, never the date (the card's meta
 * line already carries `author · date`, so a date-fallback kicker duplicated
 * it, website-vlm) and never the section's own topic name
 * (learn-harmonization.md D19). The hub-label fallback in `hubKickerForPost`
 * is reserved for the Latest rail, which has no section heading of its own.
 */
const cardKicker = (post: BlogPostSummary): string => post.category ?? "Article";

/**
 * Only link the kicker when it shows the post's category: the "Article"
 * fallback (no category) must never read as a link.
 */
const cardKickerHref = (post: BlogPostSummary): string | undefined =>
  post.category ? hubKickerForPost(post).href : undefined;

interface LearnTopicRailProps {
  readonly heading: string;
  readonly moreHref: string;
  /** Up to 4 posts for the rail, newest first; the caller has already applied the cross-section dedupe. */
  readonly posts: readonly BlogPostSummary[];
}

/**
 * One rail per topic with 3 or more posts remaining after the editorial
 * band's dedupe (learn-editorial.md section 15.2, amended by
 * learn-harmonization.md section 2.5.2/D7, rebuilt on cards by
 * website-446): a header row, the newest post as a full-width
 * `LearnArticleCard layout="split"` lead, and the remaining posts as equal
 * cards in a 3-column grid (2 from `sm`, 1 on mobile). The previous
 * composition put the lead in one of three columns and stacked compact rows
 * across the other two, which at 1536px and wider stretched each row into a
 * 900px+ line holding a 48px thumbnail and a short title; cards keep every
 * secondary item at one column width no matter how wide the viewport gets.
 * Catalog items do not appear inside topic sections; they are reachable
 * through the section's "More" link and the collection band.
 */
export function LearnTopicRail({ heading, moreHref, posts }: LearnTopicRailProps) {
  if (posts.length === 0) {
    return null;
  }
  const [lead, ...rest] = posts;

  return (
    <section className="py-8 sm:py-10">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">{heading}</h2>
        <ArrowLink href={moreHref}>{`More ${heading}`}</ArrowLink>
      </div>
      <LearnArticleCard
        post={lead}
        layout="split"
        kicker={cardKicker(lead)}
        kickerHref={cardKickerHref(lead)}
        sizes="(max-width: 1023px) 100vw, (max-width: 1279px) 45vw, min(40vw, 640px)"
      />
      {rest.length > 0 ? (
        <div className="mt-10">
          <CardGrid cols={3} step="progressive">
            {rest.map((post) => (
              <LearnArticleCard
                key={post.stem}
                post={post}
                kicker={cardKicker(post)}
                kickerHref={cardKickerHref(post)}
                sizes="(max-width: 639px) 100vw, (max-width: 1023px) 50vw, min(30vw, 520px)"
              />
            ))}
          </CardGrid>
        </div>
      ) : null}
    </section>
  );
}
