import { BlogIndexShell } from "@/src/components/BlogIndexShell";
import { ArticleBreadcrumb } from "@/src/components/learn/ArticleLayout";
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

  return (
    <div className="cc-content-dark">
      <div className="mx-auto max-w-6xl px-5 pt-8 sm:px-12">
        <ArticleBreadcrumb items={[{ label: "Learn", href: "/learn" }, { label: "Articles" }]} />
      </div>
      <BlogIndexShell
        title="Articles"
        posts={slice?.posts ?? []}
        pagination={
          slice
            ? {
                currentPage: slice.currentPage,
                totalPages: slice.totalPages,
                hrefForPage: (p) => (p === 1 ? "/learn/articles" : `/learn/articles/page/${p}`),
              }
            : undefined
        }
      />
    </div>
  );
}
