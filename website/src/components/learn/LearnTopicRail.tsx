import Link from "next/link";
import { ArrowLink } from "@/src/components/ArrowLink";
import { hubKickerForPost } from "@/src/data/learn/hubs";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { Picture } from "@/src/design-system/Picture";
import { formatDate } from "@/src/helpers/formatDate";
import { LearnListRow } from "./LearnListRow";

/**
 * Row kicker inside a headed section: category, falling back to the date,
 * never the section's own topic name (learn-harmonization.md D19). The
 * hub-label fallback in `hubKickerForPost` is reserved for the Latest
 * rail, which has no section heading of its own.
 */
const rowKicker = (post: BlogPostSummary): string =>
  post.category ?? formatDate(post.date, { month: "short", day: "numeric", year: "numeric" });

interface LearnTopicRailProps {
  readonly heading: string;
  readonly moreHref: string;
  /** Up to 4 posts for the rail, newest first; the caller has already applied the cross-section dedupe. */
  readonly posts: readonly BlogPostSummary[];
  /** Which side the lead-story slot renders on; rails alternate this A-B-A across the page (learn-harmonization.md section 2.5.2, D7). */
  readonly leadSide?: "left" | "right";
  /** Overrides the default "More {heading}" copy in the rail's ArrowLink. */
  readonly moreLabel?: string;
}

/**
 * The rail's first post, rendered larger than its `LearnListRow` siblings
 * (learn-harmonization.md D7). The image sits in a single grid column
 * (website-kbx.28) at a fixed 16:9 box, so it can never grow wider than one
 * column or crop artwork that is itself authored at 16:9.
 */
function RailFeature({ post }: { readonly post: BlogPostSummary }) {
  return (
    <Link href={post.href} className="group/rail flex flex-col no-underline">
      {post.featuredImage ? (
        <div className="aspect-video overflow-hidden rounded-2xl">
          <Picture
            src={post.featuredImage}
            alt=""
            sizes="(max-width: 1023px) 100vw, 33vw"
            className="h-full w-full object-cover"
          />
        </div>
      ) : null}
      <span
        className={`font-heading text-cc-heading text-h5 group-hover/rail:text-cc-accent font-semibold text-balance transition-colors ${post.featuredImage ? "mt-5" : ""}`}
      >
        {post.title}
      </span>
      {post.description ? <p className="text-cc-ink-dim mt-2 line-clamp-1 text-sm">{post.description}</p> : null}
    </Link>
  );
}

/**
 * One rail per topic with 3 or more posts remaining after the editorial
 * band's dedupe (learn-editorial.md section 15.2, amended by
 * learn-harmonization.md section 2.5.2/D7, restructured to 3 columns by
 * website-kbx.28): a header row, one lead-story feature occupying one of 3
 * grid columns, and up to 3 compact `LearnListRow`s stacked in the
 * remaining 2 columns. Catalog items no longer appear inside topic
 * sections; they are reachable through the section's "More" link and the
 * collection band.
 */
export function LearnTopicRail({ heading, moreHref, posts, leadSide = "left", moreLabel }: LearnTopicRailProps) {
  if (posts.length === 0) {
    return null;
  }
  const [lead, ...rest] = posts;
  const leadRight = leadSide === "right";

  return (
    <section className="py-8 sm:py-10">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">{heading}</h2>
        <ArrowLink href={moreHref}>{moreLabel ?? `More ${heading}`}</ArrowLink>
      </div>
      <div className="grid grid-cols-1 gap-x-10 gap-y-8 lg:grid-cols-3">
        <div className={leadRight ? "lg:order-2" : undefined}>
          <RailFeature post={lead} />
        </div>
        <div className={`flex flex-col lg:col-span-2 lg:self-start ${leadRight ? "lg:order-1" : ""}`}>
          {rest.map((post) => (
            <LearnListRow
              key={post.stem}
              density="compact"
              href={post.href}
              title={post.title}
              kicker={rowKicker(post)}
              // Only link the kicker when it shows the post's category: the
              // date fallback (no category) must never read as a link.
              kickerHref={post.category ? hubKickerForPost(post).href : undefined}
              featuredImage={post.featuredImage}
              product={post.products[0] ?? null}
              author={post.author}
              date={post.date}
            />
          ))}
        </div>
      </div>
    </section>
  );
}
