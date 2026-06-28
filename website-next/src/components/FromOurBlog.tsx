import Link from "next/link";
import { BlogTeaserGrid } from "./BlogTeaserGrid";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";

type FromOurBlogProps = {
  /** How many posts to show. Defaults to 3. */
  limit?: number;
  className?: string;
};

export function FromOurBlog({ limit = 3, className }: FromOurBlogProps) {
  const posts = listBlogPostSummaries().slice(0, limit);
  if (posts.length === 0) {
    return null;
  }

  return (
    <section className={className}>
      <div className="mb-6 flex items-baseline justify-between gap-4">
        <h2 className="text-cc-ink m-0 text-2xl font-semibold">
          From our blog
        </h2>
        <Link
          href="/blog"
          className="text-cc-accent hover:text-cc-accent-hover flex-none text-sm font-medium no-underline"
        >
          View all →
        </Link>
      </div>
      <BlogTeaserGrid posts={posts} />
    </section>
  );
}
