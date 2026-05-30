import Link from "next/link";
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
    <section className="mt-12 border-t border-cc-card-border pt-10">
      <div className="mb-6 flex items-baseline justify-between gap-4">
        <h2 className="m-0 text-2xl font-semibold text-cc-ink">
          You might also like
        </h2>
        <Link
          href="/blog"
          className="flex-none text-sm font-medium text-cc-accent no-underline hover:text-cc-accent-hover"
        >
          View all →
        </Link>
      </div>
      <BlogTeaserGrid posts={posts} />
    </section>
  );
}
