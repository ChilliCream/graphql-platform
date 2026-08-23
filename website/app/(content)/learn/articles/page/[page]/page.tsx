import { notFound } from "next/navigation";
import { BlogIndexShell } from "@/src/components/BlogIndexShell";
import { ArticleBreadcrumb } from "@/src/components/learn/ArticleLayout";
import { paginate } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";

type Params = { page: string };
type PageProps = { params: Promise<Params> };

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  const slice = paginate(listBlogPostSummaries(), 1);
  const totalPages = slice?.totalPages ?? 1;
  const params: Params[] = [];
  for (let p = 2; p <= totalPages; p++) {
    params.push({ page: String(p) });
  }
  return params.length > 0 ? params : [{ page: "__empty__" }];
}

export function generateMetadata() {
  return { title: "Articles" };
}

export default async function ArticlesPageN({ params }: PageProps) {
  const { page } = await params;
  const pageNum = Number(page);
  if (!Number.isInteger(pageNum) || pageNum < 2) {
    notFound();
  }
  const slice = paginate(listBlogPostSummaries(), pageNum);
  if (slice === null) {
    notFound();
  }

  return (
    <div className="cc-content-dark">
      <div className="mx-auto max-w-6xl px-5 pt-8 sm:px-12">
        <ArticleBreadcrumb items={[{ label: "Learn", href: "/learn" }, { label: "Articles" }]} />
      </div>
      <BlogIndexShell
        title="Articles"
        posts={slice.posts}
        pagination={{
          currentPage: slice.currentPage,
          totalPages: slice.totalPages,
          hrefForPage: (p) => (p === 1 ? "/learn/articles" : `/learn/articles/page/${p}`),
        }}
      />
    </div>
  );
}
