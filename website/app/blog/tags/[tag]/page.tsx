import { notFound } from "next/navigation";
import { BlogIndexShell } from "@/src/components/BlogIndexShell";
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
    <BlogIndexShell
      title={`#${tag}`}
      subtitle={
        <p className="text-cc-ink-dim text-sm">
          {tagged.length} {tagged.length === 1 ? "post" : "posts"} tagged “{tag}
          ”.
        </p>
      }
      posts={slice.posts}
      pagination={{
        currentPage: slice.currentPage,
        totalPages: slice.totalPages,
        hrefForPage: (p) =>
          p === 1 ? `/blog/tags/${tag}` : `/blog/tags/${tag}/${p}`,
      }}
    />
  );
}
