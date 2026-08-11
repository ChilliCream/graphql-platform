import { notFound } from "next/navigation";
import { BlogIndexShell } from "@/src/components/BlogIndexShell";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import {
  listTags,
  paginate,
  POSTS_PER_PAGE,
  postsForTag,
} from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import {
  createBlogItemListNode,
  createBlogNode,
} from "@/src/helpers/blogStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaRef } from "@/src/helpers/structuredData";

type Params = { tag: string; page: string };
type PageProps = { params: Promise<Params> };

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  const posts = listBlogPostSummaries();
  const tags = listTags(posts);
  const params: Params[] = [];
  for (const tag of tags) {
    const count = postsForTag(posts, tag).length;
    const totalPages = Math.max(1, Math.ceil(count / POSTS_PER_PAGE));
    for (let p = 2; p <= totalPages; p++) {
      params.push({ tag, page: String(p) });
    }
  }
  return params.length > 0 ? params : [{ tag: "__empty__", page: "__empty__" }];
}

export async function generateMetadata({ params }: PageProps) {
  const { tag, page } = await params;
  return pageMetadata({
    title: `#${tag} Blog Posts, Page ${page}`,
    description: `Posts tagged "${tag}", page ${page}.`,
    path: `/blog/tags/${tag}/${page}`,
  });
}

export default async function TagPageN({ params }: PageProps) {
  const { tag, page } = await params;
  const pageNum = Number(page);
  if (!Number.isInteger(pageNum) || pageNum < 2) {
    notFound();
  }
  const tagged = postsForTag(listBlogPostSummaries(), tag);
  if (tagged.length === 0) {
    notFound();
  }
  const slice = paginate(tagged, pageNum);
  if (slice === null) {
    notFound();
  }

  const path = `/blog/tags/${tag}/${pageNum}`;
  const title = `#${tag} Blog Posts, Page ${pageNum}`;
  const description = `Posts tagged "${tag}", page ${pageNum}.`;
  const postList = createBlogItemListNode(
    path,
    `ChilliCream blog posts tagged ${tag}, page ${pageNum}`,
    slice.posts,
    (pageNum - 1) * POSTS_PER_PAGE + 1,
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
          { name: `#${tag}`, path: `/blog/tags/${tag}` },
          { name: `Page ${pageNum}` },
        ]}
        mainEntity={schemaRef(postList["@id"]!)}
        about={{ "@type": "DefinedTerm", name: tag }}
        additionalNodes={[createBlogNode(), postList]}
      />
      <BlogIndexShell
        title={`#${tag}`}
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
