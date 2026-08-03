import { BlogIndexShell } from "@/src/components/BlogIndexShell";
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
    return <BlogIndexShell title="Blog" posts={[]} />;
  }

  return (
    <BlogIndexShell
      title="Blog"
      posts={slice.posts}
      pagination={{
        currentPage: slice.currentPage,
        totalPages: slice.totalPages,
        hrefForPage: (p) => (p === 1 ? "/blog" : `/blog/${p}`),
      }}
    />
  );
}
