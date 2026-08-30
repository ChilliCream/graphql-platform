import { notFound } from "next/navigation";
import { LearnArticleRows } from "@/src/components/learn/LearnArticleRows";
import { Pagination } from "@/src/design-system/Pagination";
import { Typography } from "@/src/design-system/Typography";
import { listTags, paginate, POSTS_PER_PAGE, postsForTag } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import { breadcrumbList } from "@/src/helpers/structuredData";

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

  const structuredData = {
    "@context": "https://schema.org",
    ...breadcrumbList([
      { name: "Learn", path: "/learn" },
      { name: "Articles", path: "/learn/articles" },
      { name: `#${tag}` },
    ]),
  };

  return (
    <div className="cc-content-dark">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData) }} />
      <Typography variant="h1">#{tag}</Typography>
      <div className="mt-8 sm:mt-10">
        <LearnArticleRows posts={slice.posts} />
      </div>
      <Pagination
        currentPage={slice.currentPage}
        totalPages={slice.totalPages}
        hrefForPage={(p) => (p === 1 ? `/learn/articles/tags/${tag}` : `/learn/articles/tags/${tag}/${p}`)}
      />
    </div>
  );
}
