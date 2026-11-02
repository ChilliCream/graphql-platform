import { ArrowLink } from "@/src/components/ArrowLink";
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
        <h2 className="text-cc-heading m-0 text-2xl font-semibold">
          From our blog
        </h2>
        <ArrowLink href="/blog">View all</ArrowLink>
      </div>
      <BlogTeaserGrid posts={posts} />
    </section>
  );
}
