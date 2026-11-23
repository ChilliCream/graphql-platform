import fs from "node:fs/promises";
import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ArticleJumpLinks } from "@/src/components/ArticleJumpLinks";
import { BlogIndexShell } from "@/src/components/BlogIndexShell";
import { BlogMetadata } from "@/src/components/BlogMetadata";
import { BlogShareBar } from "@/src/components/BlogShareBar";
import { BlogSidebar } from "@/src/components/BlogSidebar";
import { BlogTags } from "@/src/components/BlogTags";
import { ComparisonContent } from "@/src/components/ComparisonContent";
import { ComparisonCta } from "@/src/components/ComparisonCta";
import { DocsToolbar } from "@/src/components/DocsToolbar";
import { FaqSection } from "@/src/components/FaqSection";
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
  type ComparisonCallToAction,
  type ComparisonLink,
  COMPARISON_ROOT,
  findSimilarComparisons,
  listComparisons,
  listComparisonSummaries,
  readComparisonDetails,
  resolveComparisonFile,
} from "@/src/helpers/comparisonCollection";
import {
  COMPARISON_DESCRIPTION,
  COMPARISON_ID,
  createComparisonCollectionNode,
  createComparisonItemListNode,
  createComparisonSubjectNodes,
} from "@/src/helpers/comparisonStructuredData";
import { compileDoc } from "@/src/helpers/compileDoc";
import { getLastModifiedFromGit } from "@/src/helpers/gitMetadata";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";
import { estimateReadingTime } from "@/src/helpers/readingTime";
import { SITE_NAME, TWITTER_HANDLE } from "@/src/helpers/site";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  createFaqNode,
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
  type JsonLdNode,
  toStructuredDateTime,
} from "@/src/helpers/structuredData";
import { getShareImageSrc } from "@/src/image-optimization/manifest";
import { shareCardSize } from "@/src/og/pageShareCardImage";

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
  about?: unknown;
  faq?: unknown;
  cta?: unknown;
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

/** Heading and anchor of the questions rendered from the `faq` frontmatter. */
const FAQ_HEADING = "Frequently asked questions";
const FAQ_ID = "faq";

/** Anchor of the "Feature comparison" section that closes Mocha comparisons. */
const FEATURE_COMPARISON_ID = "feature-comparison";
/** Where the Mocha migration guides live; a `cta.secondary` here is one. */
const MIGRATION_GUIDE_PREFIX = "/docs/mocha/migration/";

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
    const metadata = pageMetadata({
      title: `Comparison, Page ${pageNum}`,
      description: `${COMPARISON_DESCRIPTION} Page ${pageNum}.`,
      path: `/comparison/${pageNum}`,
    });
    const images = [
      {
        url: toAbsoluteUrl("/comparison/opengraph-image"),
        ...shareCardSize,
        alt: "ChilliCream comparisons",
      },
    ];
    return {
      ...metadata,
      openGraph: { ...metadata.openGraph, images },
      twitter: { ...metadata.twitter, images },
    };
  }
  const rel = resolveComparisonFile(slug);
  if (rel === null) {
    return {};
  }
  const absPath = path.join(COMPARISON_ROOT, rel);
  const { title, metaTitle, description, updated } = readFrontmatter(absPath);
  const modifiedTime =
    normalizeStructuredDate(
      typeof updated === "string" ? updated : undefined,
    ) ?? (await getLastModifiedFromGit(absPath))?.toISOString();

  const stem = stemForSlug(slug);
  const summary = listComparisonSummaries().find((s) => s.stem === stem);
  const featuredImageAbs = summary?.featuredImage
    ? toAbsoluteUrl(getShareImageSrc(summary.featuredImage))
    : undefined;
  const images = featuredImageAbs ? [featuredImageAbs] : undefined;

  return {
    title: metaTitle ? { absolute: metaTitle } : title,
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
      title: metaTitle ?? title,
      description,
      images,
      url: summary?.href,
      publishedTime: summary?.date,
      modifiedTime,
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
      title: metaTitle ?? title,
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
  const { about, faq, cta } = readComparisonDetails(frontmatter, stem);
  const featuredImage = current.featuredImage;

  const sidebarArticles = summaries.slice(0, 10);
  const currentHref = current.href;
  const dateModified =
    toStructuredDateTime(frontmatter.updated) ?? lastModified?.toISOString();
  const articleId = schemaId(currentHref, "article");
  const authorId = current.authorProfile
    ? authorPersonId(current.authorProfile)
    : schemaId(currentHref, "author");
  const imageId = schemaId(currentHref, "primary-image");
  const article: JsonLdNode = {
    "@type": ["TechArticle", "Article"],
    "@id": articleId,
    url: toAbsoluteUrl(currentHref),
    headline: current.title,
    ...(current.description ? { description: current.description } : {}),
    datePublished: toStructuredDateTime(current.date),
    ...(dateModified ? { dateModified } : {}),
    ...(featuredImage ? { image: schemaRef(imageId) } : {}),
    ...(current.author ? { author: schemaRef(authorId) } : {}),
    publisher: schemaRef(ORGANIZATION_ID),
    mainEntityOfPage: schemaRef(schemaId(currentHref, "webpage")),
    isPartOf: schemaRef(COMPARISON_ID),
    articleSection: "Comparison",
    ...(about.length > 0 ? { about: createComparisonSubjectNodes(about) } : {}),
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
        ...shareCardSize,
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
  if (faq.length > 0) {
    additionalNodes.push(createFaqNode(currentHref, faq));
  }
  const tocItems =
    faq.length > 0
      ? [...toc, { id: FAQ_ID, text: FAQ_HEADING, depth: 2 as const }]
      : toc;
  const jumpLinks = createJumpLinks(
    toc.some((item) => item.id === FEATURE_COMPARISON_ID),
    cta,
  );
  // The "Latest comparisons" rail only shows from 110rem, where it still
  // leaves room for side-by-side code next to the table of contents
  // (CodeComparison needs 62rem). Below that it opens from the toolbar pill.
  return (
    <div
      data-docs-layout
      className="cc-content-dark grid h-full grid-cols-1 min-[110rem]:grid-cols-[20rem_1fr]"
    >
      <SidebarDrawer
        closeLabel="Close latest comparisons"
        railBreakpoint="min-[110rem]"
      >
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
          menuPillClassName="min-[110rem]:hidden"
        />
        <div className="grid grid-cols-1 2xl:grid-cols-[1fr_20rem]">
          <main className="min-w-0 px-5 pt-16 pb-8 sm:px-12 min-[110rem]:pt-8">
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
              <ArticleJumpLinks links={jumpLinks} className="-mt-2 mb-6" />
              <ComparisonContent>{content}</ComparisonContent>
              {faq.length > 0 ? (
                <FaqSection
                  id={FAQ_ID}
                  className="mt-16 scroll-mt-24"
                  eyebrow="FAQ"
                  heading={FAQ_HEADING}
                  items={faq}
                />
              ) : null}
              {cta ? <ComparisonCta cta={cta} className="mt-16" /> : null}
              <SimilarPosts posts={similar} viewAllHref="/comparison" />
            </article>
          </main>
          <TableOfContents items={tocItems} />
        </div>
      </div>
    </div>
  );
}

/**
 * Links to the feature tables and the migration guide, for readers who want
 * the summary before the walkthrough.
 */
function createJumpLinks(
  hasFeatureComparison: boolean,
  cta: ComparisonCallToAction | null,
): ComparisonLink[] {
  const links: ComparisonLink[] = [];
  if (hasFeatureComparison) {
    links.push({
      href: `#${FEATURE_COMPARISON_ID}`,
      label: "Feature comparison",
    });
  }
  if (cta?.secondary.href.startsWith(MIGRATION_GUIDE_PREFIX)) {
    links.push(cta.secondary);
  }
  return links;
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
