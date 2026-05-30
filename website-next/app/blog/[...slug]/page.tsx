import fs from "node:fs/promises";
import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BlogMetadata } from "@/src/components/BlogMetadata";
import { BlogTags } from "@/src/components/BlogTags";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { Pagination } from "@/src/design-system/Pagination";
import { SimilarPosts } from "@/src/components/SimilarPosts";
import { Typography } from "@/src/design-system/Typography";
import { paginate, POSTS_PER_PAGE } from "@/src/helpers/blogPaging";
import {
  BLOG_ROOT,
  listBlogPosts,
  resolveBlogFile,
} from "@/src/helpers/blogPaths";
import { findSimilarPosts, listBlogPostSummaries } from "@/src/helpers/blogPosts";
import { compileDoc } from "@/src/helpers/compileDoc";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";
import { estimateReadingTime } from "@/src/helpers/readingTime";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";

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
  const totalPages = Math.max(
    1,
    Math.ceil(summaries.length / POSTS_PER_PAGE),
  );
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

  const stem = stemForSlug(slug);
  const summary = listBlogPostSummaries().find((s) => s.stem === stem);
  const featuredImageAbs = summary?.featuredImage
    ? toAbsoluteUrl(summary.featuredImage)
    : undefined;
  const images = featuredImageAbs ? [featuredImageAbs] : undefined;

  return {
    title,
    description,
    openGraph: {
      type: "article",
      title,
      description,
      images,
    },
    twitter: {
      card: "summary_large_image",
      title,
      description,
      images,
    },
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
  const [{ content, frontmatter }, raw] = await Promise.all([
    compileDoc<BlogFrontmatter>(absPath),
    fs.readFile(absPath, "utf-8"),
  ]);
  const readingTime = estimateReadingTime(raw).text;

  const summaries = listBlogPostSummaries();
  const stem = stemForSlug(slug);
  const current = summaries.find((s) => s.stem === stem);
  const similar = current ? findSimilarPosts(current, summaries) : [];
  const featuredImage = current?.featuredImage ?? null;

  return (
    <main className="px-5 py-8 sm:px-12">
      <article className="mx-auto max-w-5xl">
        {featuredImage ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={featuredImage}
            alt=""
            loading="eager"
            decoding="async"
            className="mb-6 aspect-[16/9] w-full rounded-lg object-cover"
          />
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
  );
}

function isPaginationSlug(slug: string[]): boolean {
  return slug.length === 1 && /^\d+$/.test(slug[0]);
}

function stemForSlug(slug: string[]): string {
  return `${slug[0]}-${slug[1]}-${slug[2]}-${slug.slice(3).join("/")}`;
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
    <div className="px-5 py-8 sm:px-12">
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
