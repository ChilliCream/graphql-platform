import { ArrowLink } from "@/src/components/ArrowLink";
import { BlogTeaserGrid } from "./BlogTeaserGrid";
import type { ArticleSummary } from "@/src/helpers/contentCollection";

interface SimilarPostsProps {
  readonly posts: ArticleSummary[];
  /** Target of the "View all" link. Defaults to the blog index. */
  readonly viewAllHref?: string;
}

export function SimilarPosts({
  posts,
  viewAllHref = "/blog",
}: SimilarPostsProps) {
  if (posts.length === 0) {
    return null;
  }

  return (
    <section className="border-cc-card-border mt-12 border-t pt-10 print:hidden">
      <div className="mb-6 flex items-baseline justify-between gap-4">
        <h2 className="text-cc-heading m-0 text-2xl font-semibold">
          You might also like
        </h2>
        <ArrowLink href={viewAllHref}>View all</ArrowLink>
      </div>
      <BlogTeaserGrid posts={posts} />
    </section>
  );
}
