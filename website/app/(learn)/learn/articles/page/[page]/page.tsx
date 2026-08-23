import { notFound } from "next/navigation";
import { LearnArticleRows } from "@/src/components/learn/LearnArticleRows";
import { ArticleBreadcrumb } from "@/src/components/learn/ArticleLayout";
import { Pagination } from "@/src/design-system/Pagination";
import { Typography } from "@/src/design-system/Typography";
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
      <ArticleBreadcrumb items={[{ label: "Learn", href: "/learn" }, { label: "Articles" }]} />
      <Typography variant="h1">Articles</Typography>
      <div className="mt-8 sm:mt-10">
        <LearnArticleRows posts={slice.posts} />
      </div>
      <Pagination
        currentPage={slice.currentPage}
        totalPages={slice.totalPages}
        hrefForPage={(p) => (p === 1 ? "/learn/articles" : `/learn/articles/page/${p}`)}
      />
    </div>
  );
}
