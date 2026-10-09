import { BlogIndexShell } from "@/src/components/BlogIndexShell";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { paginate } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import {
  BLOG_DESCRIPTION,
  BLOG_ID,
  createBlogItemListNode,
  createBlogNode,
} from "@/src/helpers/blogStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaRef } from "@/src/helpers/structuredData";

const PAGE = {
  title: "Blog",
  description: BLOG_DESCRIPTION,
  path: "/blog",
} as const;

export const metadata = pageMetadata(PAGE);

export default function BlogsIndex() {
  const posts = listBlogPostSummaries();
  const slice = paginate(posts, 1);
  if (slice === null) {
    return <BlogIndexShell title="Blog" posts={[]} />;
  }

  const postList = createBlogItemListNode(
    PAGE.path,
    "Latest ChilliCream blog posts",
    slice.posts,
  );

  return (
    <>
      <PageStructuredData
        {...PAGE}
        pageType="CollectionPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Blog" }]}
        mainEntity={schemaRef(postList["@id"]!)}
        about={schemaRef(BLOG_ID)}
        additionalNodes={[createBlogNode(), postList]}
      />
      <BlogIndexShell
        title="Blog"
        posts={slice.posts}
        pagination={{
          currentPage: slice.currentPage,
          totalPages: slice.totalPages,
          hrefForPage: (p) => (p === 1 ? "/blog" : `/blog/${p}`),
        }}
      />
    </>
  );
}
