import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { kickerForBlogPost } from "./editorial";
import { LearnListRow } from "./LearnListRow";

interface LearnArticleRowsProps {
  readonly posts: readonly BlogPostSummary[];
}

/**
 * Article list body for `/learn/articles` surfaces (learn-editorial.md
 * section 16.2): posts render as `LearnListRow`s in a two-column ramp,
 * retiring the uniform `BlogTeaserGrid`.
 */
export function LearnArticleRows({ posts }: LearnArticleRowsProps) {
  if (posts.length === 0) {
    return null;
  }
  return (
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
  );
}
