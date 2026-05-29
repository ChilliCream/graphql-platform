import type { Metadata } from "next";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { Pagination } from "@/src/design-system/Pagination";
import { paginate } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";

export const metadata: Metadata = {
  title: "Blog",
  description: "The ChilliCream blog: announcements, deep dives, and how-tos.",
};

export default function BlogListPage() {
  const posts = listBlogPostSummaries();
  const slice = paginate(posts, 1);

  return (
    <div className="cc-content-dark cc-prose-invert min-h-screen px-5 py-12 sm:py-16">
      <header className="mx-auto mb-12 max-w-3xl text-center">
        <h1 className="text-4xl font-bold tracking-tight text-slate-100 sm:text-5xl">
          Blog
        </h1>
        <p className="mt-4 text-lg text-slate-400">
          The latest news about ChilliCream and our products
        </p>
      </header>

      <div className="mx-auto flex max-w-6xl flex-col gap-6">
        <BlogTeaserGrid posts={slice?.posts ?? []} />
        {slice ? (
          <Pagination
            currentPage={slice.currentPage}
            totalPages={slice.totalPages}
            hrefForPage={(p) => (p === 1 ? "/blog" : `/blog/${p}`)}
          />
        ) : null}
      </div>
    </div>
  );
}
