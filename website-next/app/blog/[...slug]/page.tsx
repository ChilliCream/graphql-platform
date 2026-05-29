import fs from "node:fs/promises";
import path from "node:path";
import type { Metadata } from "next";
import Image from "next/image";
import { notFound } from "next/navigation";
import { BlogMetadata } from "@/src/components/BlogMetadata";
import { BlogTags } from "@/src/components/BlogTags";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { SimilarPosts } from "@/src/components/SimilarPosts";
import { LatestBlogPosts } from "@/src/design-system/LatestBlogPosts";
import { Pagination } from "@/src/design-system/Pagination";
import { TableOfContents } from "@/src/design-system/TableOfContents";
import { Typography } from "@/src/design-system/Typography";
import { paginate, POSTS_PER_PAGE } from "@/src/helpers/blogPaging";
import {
  BLOG_ROOT,
  listBlogPosts,
  resolveBlogFile,
} from "@/src/helpers/blogPaths";
import {
  findSimilarPosts,
  listBlogPostSummaries,
} from "@/src/helpers/blogPosts";
import { compileDoc } from "@/src/helpers/compileDoc";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";
import { estimateReadingTime } from "@/src/helpers/readingTime";

// How many recent posts to surface in the left "Latest Blog Posts" rail.
const LATEST_COUNT = 8;

type BlogFrontmatter = {
  title?: string;
  description?: string;
  author?: string;
  authorUrl?: string;
  authorImageUrl?: string;
  date?: string;
  tags?: string[];
};

type Params = { slug: string[] };
type PageProps = { params: Promise<Params> };

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  const postParams = listBlogPosts().map<Params>(({ parsed }) => ({
    slug: [parsed.year, parsed.month, parsed.day, parsed.slug],
  }));

  const summaries = listBlogPostSummaries();
  const totalPages = Math.max(1, Math.ceil(summaries.length / POSTS_PER_PAGE));
  const pageParams: Params[] = [];
  for (let p = 2; p <= totalPages; p++) {
    pageParams.push({ slug: [String(p)] });
  }

  const params = [...postParams, ...pageParams];
  // output: export requires at least one prerendered path; placeholder
  // renders 404 via notFound() when no content is present.
  return params.length > 0 ? params : [{ slug: ["__empty__"] }];
}

export async function generateMetadata({
  params,
}: PageProps): Promise<Metadata> {
  const { slug } = await params;
  if (isPaginationSlug(slug)) {
    return { title: "Blog" };
  }
  const rel = resolveBlogFile(slug);
  if (rel === null) {
    return {};
  }
  const { title, description } = readFrontmatter(path.join(BLOG_ROOT, rel));
  return {
    title,
    description,
  };
}

export default async function BlogSlugPage({ params }: PageProps) {
  const { slug } = await params;

  if (isPaginationSlug(slug)) {
    return renderPagination(Number(slug[0]));
  }

  const rel = resolveBlogFile(slug);
  if (rel === null) {
    notFound();
  }

  const absPath = path.join(BLOG_ROOT, rel);
  const [{ content, frontmatter, toc }, raw] = await Promise.all([
    compileDoc<BlogFrontmatter>(absPath),
    fs.readFile(absPath, "utf-8"),
  ]);
  const readingTime = estimateReadingTime(raw).text;

  // listBlogPostSummaries() resolves featured-image URLs and sorts newest
  // first, so we reuse it for the left "Latest Blog Posts" rail, the header
  // hero image, and the "similar posts" ranking rather than re-deriving any
  // of it here.
  const summaries = listBlogPostSummaries();
  const stem = `${slug[0]}-${slug[1]}-${slug[2]}-${slug.slice(3).join("/")}`;
  const current = summaries.find((s) => s.stem === stem);
  const similar = current ? findSimilarPosts(current, summaries) : [];
  const featuredImage = current?.featuredImage ?? null;
  const currentHref = current?.href;
  const latestPosts = summaries.slice(0, LATEST_COUNT).map((p) => ({
    href: p.href,
    title: p.title,
  }));

  return (
    <div className="cc-content-dark cc-prose-invert grid min-h-screen grid-cols-1 xl:grid-cols-[16rem_minmax(0,1fr)_20rem]">
      <aside className="hidden xl:block sticky top-[72px] self-start h-[calc(100vh-72px)] overflow-y-auto">
        <LatestBlogPosts posts={latestPosts} currentHref={currentHref} />
      </aside>
      <main className="min-w-0 px-5 py-8 sm:px-12">
        <article className="mx-auto max-w-5xl">
          {featuredImage ? (
            <div className="not-prose mb-8 aspect-[16/9] w-full overflow-hidden rounded-xl bg-[#0c1322]">
              <Image
                src={featuredImage}
                alt={frontmatter.title ?? ""}
                width={1280}
                height={720}
                priority
                className="h-full w-full object-cover"
              />
            </div>
          ) : null}
          {frontmatter.title ? (
            <Typography variant="h1">{frontmatter.title}</Typography>
          ) : null}
          <BlogMetadata
            author={frontmatter.author}
            authorUrl={frontmatter.authorUrl}
            authorImageUrl={frontmatter.authorImageUrl}
            date={frontmatter.date}
            readingTime={readingTime}
          />
          <BlogTags tags={frontmatter.tags} />
          {content}
          <SimilarPosts posts={similar} />
        </article>
      </main>
      <aside className="hidden xl:block sticky top-[72px] self-start h-[calc(100vh-72px)] overflow-y-auto">
        <TableOfContents items={toc} label="In this Article" />
      </aside>
    </div>
  );
}

function isPaginationSlug(slug: string[]): boolean {
  return slug.length === 1 && /^\d+$/.test(slug[0]);
}

function renderPagination(pageNum: number) {
  if (!Number.isInteger(pageNum) || pageNum < 2) {
    notFound();
  }
  const slice = paginate(listBlogPostSummaries(), pageNum);
  if (slice === null) {
    notFound();
  }

  return (
    <div className="cc-content-dark cc-prose-invert px-5 py-8 sm:px-12">
      <div className="mx-auto flex max-w-6xl flex-col gap-6">
        <Typography variant="h1">Blog</Typography>
        <BlogTeaserGrid posts={slice.posts} />
        <Pagination
          currentPage={slice.currentPage}
          totalPages={slice.totalPages}
          hrefForPage={(p) => (p === 1 ? "/blog" : `/blog/${p}`)}
        />
      </div>
    </div>
  );
}
