import { notFound } from "next/navigation";
import { BlogIndexShell } from "@/src/components/BlogIndexShell";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { listTags, paginate, postsForTag } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import {
  createBlogItemListNode,
  createBlogNode,
} from "@/src/helpers/blogStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaRef } from "@/src/helpers/structuredData";

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
    ...pageMetadata({
      title: `#${tag} Blog Posts`,
      description: `Posts tagged "${tag}".`,
      path: `/blog/tags/${tag}`,
    }),
    robots: { index: false, follow: true },
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

  const path = `/blog/tags/${tag}`;
  const title = `#${tag} Blog Posts`;
  const description = `Posts tagged "${tag}".`;
  const postList = createBlogItemListNode(
    path,
    `ChilliCream blog posts tagged ${tag}`,
    slice.posts,
  );

  return (
    <>
      <PageStructuredData
        title={title}
        description={description}
        path={path}
        pageType="CollectionPage"
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Blog", path: "/blog" },
          { name: `#${tag}` },
        ]}
        mainEntity={schemaRef(postList["@id"]!)}
        about={{ "@type": "DefinedTerm", name: tag }}
        additionalNodes={[createBlogNode(), postList]}
      />
      <BlogIndexShell
        title={`#${tag}`}
        subtitle={
          <p className="text-cc-ink-dim text-sm">
            {tagged.length} {tagged.length === 1 ? "post" : "posts"} tagged “
            {tag}”.
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
    </>
  );
}
