import { hubKickerForPost } from "@/src/data/learn/hubs";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";
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
      {posts.map((post) => {
        const kicker = hubKickerForPost(post);
        return (
          <LearnListRow
            key={post.stem}
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
  );
}
