import { notFound } from "next/navigation";
import { BlogIndexShell } from "@/src/components/BlogIndexShell";
import { ArticleBreadcrumb } from "@/src/components/learn/ArticleLayout";
import { listTags, paginate, POSTS_PER_PAGE, postsForTag } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";

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
  const { tag } = await params;
  return { title: `${tag} · Articles` };
}

export default async function ArticleTagPageN({ params }: PageProps) {
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

  return (
    <div className="cc-content-dark">
      <div className="mx-auto max-w-6xl px-5 pt-8 sm:px-12">
        <ArticleBreadcrumb
          items={[
            { label: "Learn", href: "/learn" },
            { label: "Articles", href: "/learn/articles" },
            { label: `#${tag}` },
          ]}
        />
      </div>
      <BlogIndexShell
        title={`#${tag}`}
        posts={slice.posts}
        pagination={{
          currentPage: slice.currentPage,
          totalPages: slice.totalPages,
          hrefForPage: (p) => (p === 1 ? `/learn/articles/tags/${tag}` : `/learn/articles/tags/${tag}/${p}`),
        }}
      />
    </div>
  );
}
