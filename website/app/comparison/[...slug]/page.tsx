import fs from "node:fs/promises";
import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BlogIndexShell } from "@/src/components/BlogIndexShell";
import { BlogMetadata } from "@/src/components/BlogMetadata";
import { BlogShareBar } from "@/src/components/BlogShareBar";
import { BlogSidebar } from "@/src/components/BlogSidebar";
import { BlogTags } from "@/src/components/BlogTags";
import { DocsToolbar } from "@/src/components/DocsToolbar";
import { NotFoundContent } from "@/src/components/NotFoundContent";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { SidebarDrawer } from "@/src/components/SidebarDrawer";
import { SimilarPosts } from "@/src/components/SimilarPosts";
import { TableOfContents } from "@/src/components/TableOfContents";
import { YouTubeVideo } from "@/src/components/YouTubeVideo";
import {
  authorPageUrl,
  authorPersonId,
  createAuthorPersonNode,
} from "@/src/data/authors";
import { Picture } from "@/src/design-system/Picture";
import { Typography } from "@/src/design-system/Typography";
import { paginate, POSTS_PER_PAGE } from "@/src/helpers/blogPaging";
import {
  COMPARISON_ROOT,
  findSimilarComparisons,
  listComparisons,
  listComparisonSummaries,
  resolveComparisonFile,
} from "@/src/helpers/comparisonCollection";
import {
  COMPARISON_DESCRIPTION,
  COMPARISON_ID,
  createComparisonCollectionNode,
  createComparisonItemListNode,
} from "@/src/helpers/comparisonStructuredData";
import { compileDoc } from "@/src/helpers/compileDoc";
import { getLastModifiedFromGit } from "@/src/helpers/gitMetadata";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";
import { estimateReadingTime } from "@/src/helpers/readingTime";
import { SITE_NAME, TWITTER_HANDLE } from "@/src/helpers/site";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
  type JsonLdNode,
} from "@/src/helpers/structuredData";
import { getShareImageSrc } from "@/src/image-optimization/manifest";

type ComparisonFrontmatter = {
  title?: string;
  description?: string;
  author?: string;
  authorUrl?: string;
  authorImageUrl?: string;
  date?: string;
  updated?: string;
  tags?: string[];
  featuredImage?: string;
  featuredVideoId?: string;
};

type Params = { slug: string[] };
type PageProps = { params: Promise<Params> };

export const dynamicParams = false;

/**
 * Synthetic slug that prerenders the static comparison 404 page
 * (`/comparison/404`). nginx serves it for unmatched comparison URLs so the
 * "Browse comparisons" link is in the HTML.
 */
const NOT_FOUND_SEGMENT = "404";

export function generateStaticParams(): Params[] {
  const articleParams = listComparisons().map<Params>(({ stem }) => ({
    slug: [stem],
  }));

  const summaries = listComparisonSummaries();
  const totalPages = Math.max(1, Math.ceil(summaries.length / POSTS_PER_PAGE));
  const pageParams: Params[] = [];
  for (let p = 2; p <= totalPages; p++) {
    pageParams.push({ slug: [String(p)] });
  }

  const params = [
    ...articleParams,
    ...pageParams,
    { slug: [NOT_FOUND_SEGMENT] },
  ];
  // output: export requires at least one prerendered path; placeholder
  // renders 404 via notFound() when no content is present.
  return params.length > 0 ? params : [{ slug: ["__empty__"] }];
}

/** True when the slug is the synthetic comparison 404 page. */
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
    const pageNum = Number(slug[0]);
    return pageMetadata({
      title: `Comparison, Page ${pageNum}`,
      description: `${COMPARISON_DESCRIPTION} Page ${pageNum}.`,
      path: `/comparison/${pageNum}`,
    });
  }
  const rel = resolveComparisonFile(slug);
  if (rel === null) {
    return {};
  }
  const { title, description } = readFrontmatter(
    path.join(COMPARISON_ROOT, rel),
  );

  const stem = stemForSlug(slug);
  const summary = listComparisonSummaries().find((s) => s.stem === stem);
  const featuredImageAbs = summary?.featuredImage
    ? toAbsoluteUrl(getShareImageSrc(summary.featuredImage))
    : undefined;
  const images = featuredImageAbs ? [featuredImageAbs] : undefined;

  return {
    title,
    description,
    ...(summary?.author
      ? {
          authors: [
            {
              name: summary.author,
              url: summary.authorProfile
                ? toAbsoluteUrl(authorPageUrl(summary.authorProfile))
                : (summary.authorUrl ?? undefined),
            },
          ],
        }
      : {}),
    alternates: {
      canonical: summary?.href,
    },
    openGraph: {
      type: "article",
      siteName: SITE_NAME,
      title,
      description,
      images,
      url: summary?.href,
      publishedTime: summary?.date,
      authors:
        summary?.authorProfile || summary?.authorUrl
          ? [
              summary.authorProfile
                ? toAbsoluteUrl(authorPageUrl(summary.authorProfile))
                : summary.authorUrl!,
            ]
          : summary?.author
            ? [summary.author]
            : undefined,
      tags: summary && summary.tags.length > 0 ? summary.tags : undefined,
    },
    twitter: {
      card: "summary_large_image",
      site: TWITTER_HANDLE,
      title,
      description,
      images,
    },
  };
}

export default async function ComparisonSlugPage({ params }: PageProps) {
  const { slug } = await params;

  if (isNotFoundSlug(slug)) {
    return (
      <NotFoundContent
        secondary={{ href: "/comparison", label: "Browse comparisons" }}
      />
    );
  }

  if (isPaginationSlug(slug)) {
    return renderPagination(Number(slug[0]));
  }

  const rel = resolveComparisonFile(slug);
  if (rel === null) {
    notFound();
  }

  const absPath = path.join(COMPARISON_ROOT, rel);
  const [{ content, frontmatter, toc }, raw, lastModified] = await Promise.all([
    compileDoc<ComparisonFrontmatter>(absPath),
    fs.readFile(absPath, "utf-8"),
    getLastModifiedFromGit(absPath),
  ]);
  const readingTime = estimateReadingTime(raw);

  const summaries = listComparisonSummaries();
  const stem = stemForSlug(slug);
  const current = summaries.find((s) => s.stem === stem);
  if (!current) {
    notFound();
  }
  const similar = findSimilarComparisons(current, summaries);
  const featuredImage = current.featuredImage;

  const sidebarArticles = summaries.slice(0, 10);
  const currentHref = current.href;
  const dateModified =
    normalizeStructuredDate(frontmatter.updated) ?? lastModified?.toISOString();
  const articleId = schemaId(currentHref, "article");
  const authorId = current.authorProfile
    ? authorPersonId(current.authorProfile)
    : schemaId(currentHref, "author");
  const imageId = schemaId(currentHref, "primary-image");
  const article: JsonLdNode = {
    "@type": "TechArticle",
    "@id": articleId,
    url: toAbsoluteUrl(currentHref),
    headline: current.title,
    ...(current.description ? { description: current.description } : {}),
    datePublished: current.date,
    ...(dateModified ? { dateModified } : {}),
    ...(featuredImage ? { image: schemaRef(imageId) } : {}),
    ...(current.author ? { author: schemaRef(authorId) } : {}),
    publisher: schemaRef(ORGANIZATION_ID),
    mainEntityOfPage: schemaRef(schemaId(currentHref, "webpage")),
    isPartOf: schemaRef(COMPARISON_ID),
    ...(current.tags.length > 0 ? { keywords: current.tags } : {}),
    wordCount: readingTime.words,
    timeRequired: `PT${readingTime.minutes}M`,
    inLanguage: "en",
    isAccessibleForFree: true,
  };
  const author: JsonLdNode | null = current.authorProfile
    ? createAuthorPersonNode(current.authorProfile)
    : current.author
      ? {
          "@type": "Person",
          "@id": authorId,
          name: current.author,
          ...(current.authorUrl
            ? { url: current.authorUrl, sameAs: [current.authorUrl] }
            : {}),
          ...(current.authorImageUrl
            ? { image: toAbsoluteUrl(current.authorImageUrl) }
            : {}),
        }
      : null;
  const image: JsonLdNode | null = featuredImage
    ? {
        "@type": "ImageObject",
        "@id": imageId,
        url: toAbsoluteUrl(getShareImageSrc(featuredImage)),
        contentUrl: toAbsoluteUrl(getShareImageSrc(featuredImage)),
        caption: current.title,
      }
    : null;
  const additionalNodes: JsonLdNode[] = [
    article,
    createComparisonCollectionNode(),
  ];
  if (author) {
    additionalNodes.push(author);
  }
  if (image) {
    additionalNodes.push(image);
  }
  return (
    <div
      data-docs-layout
      className="cc-content-dark grid h-full grid-cols-1 lg:grid-cols-[20rem_1fr]"
    >
      <SidebarDrawer closeLabel="Close latest comparisons">
        <BlogSidebar
          posts={sidebarArticles}
          currentHref={currentHref}
          label="Latest comparisons"
        />
      </SidebarDrawer>
      <div className="min-w-0">
        <DocsToolbar
          menuLabel="Open latest comparisons"
          menuPillLabel="Latest comparisons"
        />
        <div className="grid grid-cols-1 2xl:grid-cols-[1fr_20rem]">
          <main className="min-w-0 px-5 pt-16 pb-8 sm:px-12 2xl:pt-8">
            <PageStructuredData
              title={current.title}
              description={current.description ?? undefined}
              dateModified={dateModified}
              path={currentHref}
              pageType="ItemPage"
              breadcrumbs={[
                { name: "Home", path: "/" },
                { name: "Comparison", path: "/comparison" },
                { name: current.title },
              ]}
              mainEntity={schemaRef(articleId)}
              about={schemaRef(COMPARISON_ID)}
              additionalNodes={additionalNodes}
            />
            <article className="mx-auto max-w-5xl">
              {frontmatter.featuredVideoId ? (
                <YouTubeVideo
                  videoId={frontmatter.featuredVideoId}
                  playlabel={`Play ${current.title}`}
                />
              ) : featuredImage ? (
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
                  author={current.author ?? undefined}
                  authorUrl={current.authorUrl ?? undefined}
                  authorProfileHref={
                    current.authorProfile
                      ? authorPageUrl(current.authorProfile)
                      : undefined
                  }
                  authorImageUrl={current.authorImageUrl ?? undefined}
                  date={frontmatter.date}
                  readingTime={readingTime.text}
                />
                <BlogShareBar
                  url={toAbsoluteUrl(current.href)}
                  title={frontmatter.title ?? ""}
                />
              </div>
              <BlogTags tags={frontmatter.tags} hrefForTag={null} />
              {content}
              <SimilarPosts posts={similar} viewAllHref="/comparison" />
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

function normalizeStructuredDate(value: string | undefined) {
  if (!value) {
    return undefined;
  }
  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return value;
  }
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
}

function renderPagination(pageNum: number) {
  if (!Number.isInteger(pageNum) || pageNum < 2) {
    notFound();
  }
  const slice = paginate(listComparisonSummaries(), pageNum);
  if (slice === null) {
    notFound();
  }

  const path = `/comparison/${pageNum}`;
  const title = `Comparison, Page ${pageNum}`;
  const description = `${COMPARISON_DESCRIPTION} Page ${pageNum}.`;
  const articleList = createComparisonItemListNode(
    path,
    `ChilliCream comparisons, page ${pageNum}`,
    slice.posts,
    (pageNum - 1) * POSTS_PER_PAGE + 1,
  );

  return (
    <>
      <PageStructuredData
        title={title}
        description={description}
        path={path}
        pageType="CollectionPage"
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Comparison", path: "/comparison" },
          { name: `Page ${pageNum}` },
        ]}
        mainEntity={schemaRef(articleList["@id"]!)}
        about={schemaRef(COMPARISON_ID)}
        additionalNodes={[createComparisonCollectionNode(), articleList]}
      />
      <BlogIndexShell
        title="Comparison"
        posts={slice.posts}
        pagination={{
          currentPage: slice.currentPage,
          totalPages: slice.totalPages,
          hrefForPage: (p) => (p === 1 ? "/comparison" : `/comparison/${p}`),
        }}
      />
    </>
  );
}
