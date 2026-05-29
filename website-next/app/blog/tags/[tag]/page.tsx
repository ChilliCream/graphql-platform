import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { Pagination } from "@/src/design-system/Pagination";
import { listTags, paginate, postsForTag } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";

type Params = { tag: string };
type PageProps = { params: Promise<Params> };

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  const tags = listTags(listBlogPostSummaries());
  return tags.length > 0
    ? tags.map((tag) => ({ tag }))
    : [{ tag: "__empty__" }];
}

export async function generateMetadata({
  params,
}: PageProps): Promise<Metadata> {
  const { tag } = await params;
  const decoded = decodeURIComponent(tag);
  return {
    title: `Posts tagged "${decoded}"`,
    description: `Posts tagged "${decoded}".`,
  };
}

export default async function BlogTagPage({ params }: PageProps) {
  const { tag } = await params;
  const decoded = decodeURIComponent(tag);
  const all = listBlogPostSummaries();
  const tagged = postsForTag(all, tag);
  if (tagged.length === 0) {
    notFound();
  }
  const slice = paginate(tagged, 1);
  if (slice === null) {
    notFound();
  }

  return (
    <div className="cc-content-dark cc-prose-invert min-h-screen px-5 py-12 sm:py-16">
      <header className="mx-auto mb-12 max-w-3xl text-center">
        <h1 className="text-4xl font-bold tracking-tight text-slate-100 sm:text-5xl">
          Posts tagged &ldquo;{decoded}&rdquo;
        </h1>
        <p className="mt-4 text-lg text-slate-400">
          {tagged.length} {tagged.length === 1 ? "post" : "posts"}
        </p>
      </header>

      <div className="mx-auto flex max-w-6xl flex-col gap-6">
        <BlogTeaserGrid posts={slice.posts} />
        <Pagination
          currentPage={slice.currentPage}
          totalPages={slice.totalPages}
          hrefForPage={(p) =>
            p === 1 ? `/blog/tags/${tag}` : `/blog/tags/${tag}/${p}`
          }
        />
      </div>
    </div>
  );
}
