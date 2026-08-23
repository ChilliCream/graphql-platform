import { ArrowLink } from "@/src/components/ArrowLink";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { TOPICS, topicsForBlogPost } from "./editorial";
import { LearnFeaturedStory } from "./LearnFeaturedStory";
import { LearnListRow } from "./LearnListRow";
import { LearnPromoTile } from "./LearnPromoTile";
import { LearnTagCloud } from "./LearnTagCloud";

interface RailPromo {
  readonly href: string;
  readonly image: string;
  readonly kicker: string;
  readonly title: string;
  readonly author?: string;
}

interface LearnEditorialBandProps {
  /** Up to 5 posts for the Latest column, newest first, excluding `featuredPost`. */
  readonly latestPosts: readonly BlogPostSummary[];
  readonly featuredPost: BlogPostSummary;
  /** Curated image promo tile; omitted when no eligible post remains after dedupe. */
  readonly railPromo: RailPromo | null;
  readonly tags: readonly string[];
}

/** Kicker text for a `LearnListRow`: the post's category, falling back to its primary topic label. */
function kickerFor(post: BlogPostSummary): string {
  if (post.category) {
    return post.category;
  }
  const topicKey = topicsForBlogPost(post)[0];
  return TOPICS.find((topic) => topic.key === topicKey)?.label ?? "Article";
}

/**
 * Landing editorial band (learn-editorial.md section 15.1): the three-column
 * Latest / Featured / rail grid, replacing the shipped `LearnFeatureHero` +
 * `LearnLatestSection` pair. `xl` and up locks the featured story to its
 * 600px floor via `minmax(37.5rem,1fr)`, compressing the side columns first;
 * `lg` drops to a two-column Featured/rail row with Latest as a full-width
 * two-column list below; below `lg` everything stacks, Featured first.
 */
export function LearnEditorialBand({ latestPosts, featuredPost, railPromo, tags }: LearnEditorialBandProps) {
  return (
    <div className="pt-8 sm:pt-10">
      <div className="grid grid-cols-1 gap-y-12 lg:grid-cols-[minmax(0,1fr)_19rem] lg:gap-x-8 xl:grid-cols-[minmax(14rem,19rem)_minmax(37.5rem,1fr)_minmax(14rem,19rem)] xl:gap-x-0 xl:gap-y-0">
        <div className="order-2 lg:order-3 lg:col-span-2 xl:order-1 xl:col-span-1 xl:pr-8">
          <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">Latest</h2>
          <div className="mt-6 grid grid-cols-1 gap-x-10 sm:grid-cols-2 xl:grid-cols-1">
            {latestPosts.map((post) => (
              <LearnListRow
                key={post.stem}
                href={post.href}
                title={post.title}
                kicker={kickerFor(post)}
                featuredImage={post.featuredImage}
                product={post.products[0] ?? null}
                author={post.author}
                date={post.date}
              />
            ))}
          </div>
          <ArrowLink href="/learn/articles" className="mt-6">
            All articles
          </ArrowLink>
        </div>

        <div className="xl:border-cc-card-border order-1 xl:order-2 xl:border-l xl:px-8">
          <LearnFeaturedStory post={featuredPost} priority />
        </div>

        <div className="xl:border-cc-card-border order-3 flex flex-col gap-6 lg:order-2 xl:order-3 xl:border-l xl:px-8">
          {railPromo ? (
            <LearnPromoTile
              variant="image"
              href={railPromo.href}
              image={railPromo.image}
              kicker={railPromo.kicker}
              title={railPromo.title}
              author={railPromo.author}
            />
          ) : null}
          <LearnPromoTile variant="cta" href="/learn#subscribe" kicker="Subscribe" title="Never miss a release" />
          <LearnTagCloud tags={tags} />
        </div>
      </div>
    </div>
  );
}
