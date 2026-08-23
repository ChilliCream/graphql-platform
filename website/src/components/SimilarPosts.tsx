import { ArrowLink } from "@/src/components/ArrowLink";
import { LearnArticleRows } from "@/src/components/learn/LearnArticleRows";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";

type SimilarPostsProps = {
  posts: BlogPostSummary[];
};

/** The article reading page's related rail (learn-editorial.md section 16.3): posts render as `LearnListRow`s, matching every other /learn article surface. */
export function SimilarPosts({ posts }: SimilarPostsProps) {
  if (posts.length === 0) {
    return null;
  }

  // `LearnArticleRows` lays posts into a 2-column ramp: an odd count of 3 or
  // more leaves a lone card in an otherwise-empty row (learn-harmonization.md
  // D16's "2+1 orphan cell"). Trimming to the even count below it keeps the
  // rail full on every row; 1 post alone has no row rhythm to break, so it
  // renders as-is.
  const rows = posts.length >= 3 && posts.length % 2 === 1 ? posts.slice(0, -1) : posts;

  return (
    <section className="border-cc-card-border mt-12 border-t pt-10 print:hidden">
      <div className="mb-6 flex items-baseline justify-between gap-4">
        <h2 className="text-cc-heading m-0 text-2xl font-semibold">You might also like</h2>
        <ArrowLink href="/learn/articles">View all</ArrowLink>
      </div>
      <LearnArticleRows posts={rows} />
    </section>
  );
}
