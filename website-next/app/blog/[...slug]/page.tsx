import fs from "node:fs/promises";
import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BlogMetadata } from "@/src/components/BlogMetadata";
import { BlogShareBar } from "@/src/components/BlogShareBar";
import { BlogSidebar } from "@/src/components/BlogSidebar";
import { BlogTags } from "@/src/components/BlogTags";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { DocsToolbar } from "@/src/components/DocsToolbar";
import { NotFoundContent } from "@/src/components/NotFoundContent";
import { SidebarDrawer } from "@/src/components/SidebarDrawer";
import { TableOfContents } from "@/src/components/TableOfContents";
import { Pagination } from "@/src/design-system/Pagination";
import { Picture } from "@/src/design-system/Picture";
import { SimilarPosts } from "@/src/components/SimilarPosts";
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
import { getShareImageSrc } from "@/src/image-optimization/manifest";
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

/**
 * Synthetic slug that prerenders the static blog 404 page (`/blog/404`). nginx
 * serves it for unmatched blog URLs so the "browse the blog" link is in the HTML.
 */
const NOT_FOUND_SEGMENT = "404";

export function generateStaticParams(): Params[] {
  const postParams = listBlogPosts().map<Params>(({ stem }) => ({
    slug: [stem],
  }));

  const summaries = listBlogPostSummaries();
  const totalPages = Math.max(1, Math.ceil(summaries.length / POSTS_PER_PAGE));
  const pageParams: Params[] = [];
  for (let p = 2; p <= totalPages; p++) {
    pageParams.push({ slug: [String(p)] });
  }

  const params = [...postParams, ...pageParams, { slug: [NOT_FOUND_SEGMENT] }];
  // output: export requires at least one prerendered path; placeholder
  // renders 404 via notFound() when no content is present.
  return params.length > 0 ? params : [{ slug: ["__empty__"] }];
}

/** True when the slug is the synthetic blog 404 page. */
function isNotFoundSlug(slug: string[]): boolean {
  return slug.length === 1 && slug[0] === NOT_FOUND_SEGMENT;
}

export async function generateMetadata({
  params,
}: PageProps): Promise<Metadata> {
  const { slug } = await params;
  if (isNotFoundSlug(slug)) {
    return { title: "Page not found", robots: { index: false, follow: false } };
  }
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
    ? toAbsoluteUrl(getShareImageSrc(summary.featuredImage))
    : undefined;
  const images = featuredImageAbs ? [featuredImageAbs] : undefined;

  return {
    title,
    description,
    ...(summary?.author
      ? { authors: [{ name: summary.author, url: summary.authorUrl ?? undefined }] }
      : {}),
    alternates: {
      canonical: summary?.href,
    },
    openGraph: {
      type: "article",
      title,
      description,
      images,
      url: summary?.href,
      publishedTime: summary?.date,
      authors: summary?.authorUrl
        ? [summary.authorUrl]
        : summary?.author
          ? [summary.author]
          : undefined,
      tags: summary && summary.tags.length > 0 ? summary.tags : undefined,
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

  if (isNotFoundSlug(slug)) {
    return (
      <NotFoundContent
        secondary={{ href: "/blog", label: "Browse the blog" }}
      />
    );
  }

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

  const summaries = listBlogPostSummaries();
  const stem = stemForSlug(slug);
  const current = summaries.find((s) => s.stem === stem);
  const similar = current ? findSimilarPosts(current, summaries) : [];
  const featuredImage = current?.featuredImage ?? null;

  const sidebarPosts = summaries.slice(0, 10);
  const currentHref = current?.href ?? `/blog/${stem}`;

  const jsonLd = current
    ? {
        "@context": "https://schema.org",
        "@type": "BlogPosting",
        headline: current.title,
        ...(current.description ? { description: current.description } : {}),
        datePublished: current.date,
        ...(current.featuredImage
          ? { image: toAbsoluteUrl(current.featuredImage) }
          : {}),
        ...(current.author
          ? {
              author: {
                "@type": "Person",
                name: current.author,
                ...(current.authorUrl ? { url: current.authorUrl } : {}),
              },
            }
          : {}),
        mainEntityOfPage: toAbsoluteUrl(current.href),
      }
    : null;

  return (
    <div
      data-docs-layout
      className="cc-content-dark grid grid-cols-1 lg:grid-cols-[20rem_1fr]"
    >
      <SidebarDrawer closeLabel="Close latest posts">
        <BlogSidebar posts={sidebarPosts} currentHref={currentHref} />
      </SidebarDrawer>
      <div className="min-w-0">
        <DocsToolbar menuLabel="Open latest posts" menuPillLabel="Latest posts" />
        <div className="grid grid-cols-1 2xl:grid-cols-[1fr_20rem]">
          <main className="min-w-0 px-5 pb-8 pt-16 sm:px-12 2xl:pt-8">
            {jsonLd ? (
              <script
                type="application/ld+json"
                // Escape `<` so content text can never close the script tag (XSS).
                dangerouslySetInnerHTML={{
                  __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c"),
                }}
              />
            ) : null}
            <article className="mx-auto max-w-5xl">
              {featuredImage ? (
                <Picture
                  src={featuredImage}
                  alt=""
                  priority
                  // Mirrors the layout: a max-w-5xl (1024px) column inside px-5
                  // (sm:px-12) page padding, so the browser picks the smallest
                  // sufficient variant instead of rounding the slot up to 100vw.
                  sizes="(max-width: 639px) calc(100vw - 2.5rem), (max-width: 1119px) calc(100vw - 6rem), 1024px"
                  className="mb-6 aspect-video w-full rounded-lg object-cover"
                />
              ) : null}
              {frontmatter.title ? (
                <Typography variant="h1">{frontmatter.title}</Typography>
              ) : null}
              <div className="flex flex-wrap items-center justify-between gap-4">
                <BlogMetadata
                  author={frontmatter.author}
                  authorUrl={frontmatter.authorUrl}
                  authorImageUrl={frontmatter.authorImageUrl}
                  date={frontmatter.date}
                  readingTime={readingTime}
                />
                <BlogShareBar
                  url={toAbsoluteUrl(current?.href ?? `/blog/${stem}`)}
                  title={frontmatter.title ?? ""}
                />
              </div>
              <BlogTags tags={frontmatter.tags} />
              {content}
              <SimilarPosts posts={similar} />
            </article>
          </main>
          <TableOfContents items={toc} />
        </div>
      </div>
    </div>
  );
}

function isPaginationSlug(slug: string[]): boolean {
  return slug.length === 1 && /^\d+$/.test(slug[0]);
}

function stemForSlug(slug: string[]): string {
  return slug[0];
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
