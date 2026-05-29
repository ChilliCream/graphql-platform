import { notFound } from "next/navigation";
import { Pagination } from "@/src/design-system/Pagination";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { Typography } from "@/src/design-system/Typography";
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

export async function generateMetadata({ params }: PageProps) {
  const { tag } = await params;
  return {
    title: `${tag} · Blog`,
    description: `Posts tagged "${tag}".`,
  };
}

export default async function TagIndex({ params }: PageProps) {
  const { tag } = await params;
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
    <div className="px-5 py-8 sm:px-12">
      <div className="mx-auto flex max-w-6xl flex-col gap-6">
        <header className="flex flex-col gap-1">
          <Typography variant="h1">#{tag}</Typography>
          <p className="text-sm text-slate-500">
            {tagged.length} {tagged.length === 1 ? "post" : "posts"} tagged
            “{tag}”.
          </p>
        </header>
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
