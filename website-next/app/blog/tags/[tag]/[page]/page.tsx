import { notFound } from "next/navigation";
import { Pagination } from "@/src/design-system/Pagination";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { Typography } from "@/src/design-system/Typography";
import {
  listTags,
  paginate,
  POSTS_PER_PAGE,
  postsForTag,
} from "@/src/helpers/blogPaging";
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
  return { title: `${tag} · Blog` };
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

  return (
    <div className="px-5 py-8 sm:px-12">
      <div className="mx-auto flex max-w-6xl flex-col gap-6">
        <Typography variant="h1">#{tag}</Typography>
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
