import { notFound } from "next/navigation";
import { LearnArticleRows } from "@/src/components/learn/LearnArticleRows";
import { ArticleBreadcrumb } from "@/src/components/learn/ArticleLayout";
import { Pagination } from "@/src/design-system/Pagination";
import { Typography } from "@/src/design-system/Typography";
import { listTags, paginate, postsForTag } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";

type Params = { tag: string };
type PageProps = { params: Promise<Params> };

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  const tags = listTags(listBlogPostSummaries());
  return tags.length > 0 ? tags.map((tag) => ({ tag })) : [{ tag: "__empty__" }];
}

export async function generateMetadata({ params }: PageProps) {
  const { tag } = await params;
  return {
    title: `${tag} · Articles`,
    description: `Articles tagged "${tag}".`,
  };
}

export default async function ArticleTagIndex({ params }: PageProps) {
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
    <div className="cc-content-dark">
      <ArticleBreadcrumb
        items={[
          { label: "Learn", href: "/learn" },
          { label: "Articles", href: "/learn/articles" },
          { label: `#${tag}` },
        ]}
      />
      <header className="flex flex-col gap-1">
        <Typography variant="h1">#{tag}</Typography>
        <p className="text-cc-ink-dim text-sm">
          {tagged.length} {tagged.length === 1 ? "post" : "posts"} tagged “{tag}”.
        </p>
      </header>
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
