import { ArrowLink } from "@/src/components/ArrowLink";
import type { BlogTeaserData } from "@/src/components/BlogTeaser";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";

interface LearnLatestSectionProps {
  readonly posts: readonly BlogTeaserData[];
}

/**
 * Latest section (learn-editorial.md section 3.3): the newest posts as a
 * `BlogTeaserGrid`. Section 3.3 also designs a headline list for posts 4-6
 * on `lg`; nesting `BlogTeaserGrid`'s own 3-column ramp inside that
 * secondary column squeezes its cards below their intended width, so this
 * takes the section's own sanctioned simplification: `BlogTeaserGrid` with
 * up to 6 posts and nothing else.
 */
export function LearnLatestSection({ posts }: LearnLatestSectionProps) {
  if (posts.length === 0) {
    return null;
  }
  return (
    <section className="border-cc-card-border border-t py-14 sm:py-20">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">Latest</h2>
        <ArrowLink href="/blog">All articles</ArrowLink>
      </div>
      <BlogTeaserGrid posts={posts.slice(0, 6)} />
    </section>
  );
}
