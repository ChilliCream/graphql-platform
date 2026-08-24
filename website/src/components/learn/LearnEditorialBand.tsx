import { ArrowLink } from "@/src/components/ArrowLink";
import { hubKickerForPost } from "@/src/data/learn/hubs";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { LearnFeaturedStory } from "./LearnFeaturedStory";
import { LearnLatestVideos, type LatestVideoRailItem } from "./LearnLatestVideos";
import { LearnListRow } from "./LearnListRow";
import { LearnTagCloud } from "./LearnTagCloud";

interface LearnEditorialBandProps {
  /** Up to 5 posts for the Latest column, newest first, excluding `featuredPost`. Empty omits the column entirely rather than rendering an empty "Latest" shell. */
  readonly latestPosts: readonly BlogPostSummary[];
  /** `null` omits the Featured column entirely (website-kbx.6): a near-empty topic hub has no post to feature. */
  readonly featuredPost: BlogPostSummary | null;
  /** Rail's "Latest videos" rows, newest first, up to 2. */
  readonly latestVideos: readonly LatestVideoRailItem[];
  readonly tags: readonly string[];
  /** "All articles" link target for the Latest column; defaults to the sitewide index. A topic hub page passes its own scoped browse link. */
  readonly allArticlesHref?: string;
}

/**
 * Editorial hero band (learn-editorial.md section 15.1, amended by
 * learn-harmonization.md section 2.5.1 and website-kbx.6): the three-column
 * Latest / Featured / rail grid, shared verbatim by the landing page and
 * every topic hub page, parameterized by whatever posts/videos/tags the
 * caller has already scoped to its topic. `xl` and up locks the featured
 * story to its 600px floor via `minmax(37.5rem,1fr)`, compressing the side
 * columns first; `2xl` grows both side columns further instead of giving all
 * extra width to the center; `lg` drops to a two-column Featured/rail row
 * with Latest as a full-width two-column list below; below `lg` everything
 * stacks, Featured first.
 *
 * Each column renders only when it has content, and the whole band renders
 * nothing when all three are empty (a near-empty hub, e.g. Messaging, has no
 * featured post, no articles, and no videos): there is no empty "Latest"
 * heading with zero rows, no blank Featured slot, and no bare rail column.
 */
export function LearnEditorialBand({
  latestPosts,
  featuredPost,
  latestVideos,
  tags,
  allArticlesHref = "/learn/articles",
}: LearnEditorialBandProps) {
  const showLatest = latestPosts.length > 0;
  const showRail = latestVideos.length > 0 || tags.length > 0;

  if (!showLatest && !featuredPost && !showRail) {
    return null;
  }

  return (
    <div>
      <div className="grid grid-cols-1 gap-y-12 lg:grid-cols-[minmax(0,1fr)_19rem] lg:gap-x-8 xl:grid-cols-[minmax(14rem,19rem)_minmax(37.5rem,1fr)_minmax(14rem,19rem)] xl:gap-x-0 xl:gap-y-0 2xl:grid-cols-[minmax(16rem,24rem)_minmax(37.5rem,1fr)_minmax(16rem,24rem)]">
        {showLatest ? (
          <div className="order-2 lg:order-3 lg:col-span-2 xl:order-1 xl:col-span-1 xl:flex xl:flex-col xl:pr-8">
            <h2 className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">Latest</h2>
            <div className="mt-2 grid grid-cols-1 gap-x-10 sm:grid-cols-2 xl:grid-cols-1">
              {latestPosts.map((post) => {
                const kicker = hubKickerForPost(post);
                return (
                  <LearnListRow
                    key={post.stem}
                    density="compact"
                    href={post.href}
                    title={post.title}
                    kicker={kicker.text}
                    kickerHref={kicker.href}
                    featuredImage={post.featuredImage}
                    product={post.products[0] ?? null}
                    author={post.author}
                    date={post.date}
                  />
                );
              })}
            </div>
            <ArrowLink href={allArticlesHref} className="mt-6 xl:mt-auto xl:pt-6">
              All articles
            </ArrowLink>
          </div>
        ) : null}

        {featuredPost ? (
          <div className="xl:border-cc-card-border order-1 xl:order-2 xl:border-l xl:px-8">
            <LearnFeaturedStory post={featuredPost} priority />
          </div>
        ) : null}

        {showRail ? (
          <div className="xl:border-cc-card-border order-3 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:order-2 lg:grid-cols-1 xl:order-3 xl:flex xl:flex-col xl:border-l xl:px-8">
            {latestVideos.length > 0 ? (
              <div className="sm:col-span-2 lg:col-span-1">
                <LearnLatestVideos videos={latestVideos} />
              </div>
            ) : null}
            <div className="sm:col-span-2 lg:col-span-1">
              <LearnTagCloud tags={tags} />
            </div>
            {latestVideos.length > 0 ? (
              <ArrowLink href="/learn/browse?type=video" className="mt-6 xl:mt-auto xl:pt-6">
                All videos
              </ArrowLink>
            ) : null}
          </div>
        ) : null}
      </div>
    </div>
  );
}
