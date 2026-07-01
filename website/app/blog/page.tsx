import { Pagination } from "@/src/design-system/Pagination";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { Typography } from "@/src/design-system/Typography";
import { paginate } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "Blog",
  description: "The ChilliCream blog: announcements, deep dives, and how-tos.",
  path: "/blog",
});

export default function BlogsIndex() {
  const posts = listBlogPostSummaries();
  const slice = paginate(posts, 1);
  if (slice === null) {
    return (
      <div className="px-5 py-8 sm:px-12">
        <div className="mx-auto flex max-w-6xl flex-col gap-6">
          <Typography variant="h1">Blog</Typography>
          <BlogTeaserGrid posts={[]} />
        </div>
      </div>
    );
  }

  return (
    <div className="px-5 py-8 sm:px-12">
      <div className="mx-auto flex max-w-6xl flex-col gap-6">
        <Typography variant="h1">Blog</Typography>
        <BlogTeaserGrid posts={slice.posts} />
        <Pagination
          currentPage={slice.currentPage}
          totalPages={slice.totalPages}
          hrefForPage={(p) => (p === 1 ? "/blog" : `/blog/${p}`)}
        />
      </div>
    </div>
  );
}
