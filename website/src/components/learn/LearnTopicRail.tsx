import { ArrowLink } from "@/src/components/ArrowLink";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { kickerForBlogPost } from "./editorial";
import { LearnListRow } from "./LearnListRow";

interface LearnTopicRailProps {
  readonly heading: string;
  readonly moreHref: string;
  /** Up to 4 posts for the rail, newest first; the caller has already applied the cross-section dedupe. */
  readonly posts: readonly BlogPostSummary[];
}

/**
 * One rail per topic with 3 or more posts remaining after the editorial
 * band's dedupe (learn-editorial.md section 15.2): a header row plus up to 4
 * `LearnListRow`s in a two-column ramp. Catalog items no longer appear inside
 * topic sections; they are reachable through the section's "More" link and
 * the collection band.
 */
export function LearnTopicRail({ heading, moreHref, posts }: LearnTopicRailProps) {
  if (posts.length === 0) {
    return null;
  }
  return (
    <section className="border-cc-card-border border-t py-14 sm:py-20">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">{heading}</h2>
        <ArrowLink href={moreHref}>More {heading}</ArrowLink>
      </div>
      <div className="grid grid-cols-1 gap-x-10 lg:grid-cols-2">
        {posts.map((post) => (
          <LearnListRow
            key={post.stem}
            href={post.href}
            title={post.title}
            kicker={kickerForBlogPost(post)}
            featuredImage={post.featuredImage}
            product={post.products[0] ?? null}
            author={post.author}
            date={post.date}
          />
        ))}
      </div>
    </section>
  );
}
