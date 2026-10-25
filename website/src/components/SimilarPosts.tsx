import { ArrowLink } from "@/src/components/ArrowLink";
import { BlogTeaserGrid } from "./BlogTeaserGrid";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";

type SimilarPostsProps = {
  posts: BlogPostSummary[];
};

export function SimilarPosts({ posts }: SimilarPostsProps) {
  if (posts.length === 0) {
    return null;
  }

  return (
    <section className="border-cc-card-border mt-12 border-t pt-10 print:hidden">
      <div className="mb-6 flex items-baseline justify-between gap-4">
        <h2 className="text-cc-heading m-0 text-2xl font-semibold">
          You might also like
        </h2>
        <ArrowLink href="/blog">View all</ArrowLink>
      </div>
      <BlogTeaserGrid posts={posts} />
    </section>
  );
}
