import { LearnArticleRows } from "@/src/components/learn/LearnArticleRows";
import { ArticleBreadcrumb } from "@/src/components/learn/ArticleLayout";
import { LearnFeaturedStory } from "@/src/components/learn/LearnFeaturedStory";
import { Pagination } from "@/src/design-system/Pagination";
import { Typography } from "@/src/design-system/Typography";
import { paginate } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "Articles",
  description: "All ChilliCream articles: announcements, deep dives, and how-tos, from the /learn hub.",
  path: "/learn/articles",
});

export default function ArticlesIndex() {
  const posts = listBlogPostSummaries();
  const slice = paginate(posts, 1);
  const items = slice?.posts ?? [];
  const [featured, ...rest] = items;

  return (
    <div className="cc-content-dark">
      <ArticleBreadcrumb items={[{ label: "Learn", href: "/learn" }, { label: "Articles" }]} />
      <Typography variant="h1">Articles</Typography>
      {featured ? <LearnFeaturedStory post={featured} priority sizes="(max-width: 1663px) 100vw, 1600px" /> : null}
      <div className="mt-8 sm:mt-10">
        <LearnArticleRows posts={rest} />
      </div>
      {slice ? (
        <Pagination
          currentPage={slice.currentPage}
          totalPages={slice.totalPages}
          hrefForPage={(p) => (p === 1 ? "/learn/articles" : `/learn/articles/page/${p}`)}
        />
      ) : null}
    </div>
  );
}
