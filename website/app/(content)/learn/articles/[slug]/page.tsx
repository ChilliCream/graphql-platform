import fs from "node:fs/promises";
import path from "node:path";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { CardGrid } from "@/src/components/CardGrid";
import { ArticleLayout } from "@/src/components/learn/ArticleLayout";
import { LearnCard } from "@/src/components/learn/LearnCard";
import { ARTICLES_ROOT, listArticleSlugs, resolveArticleFile } from "@/src/helpers/articlePaths";
import { findArticleSummary } from "@/src/helpers/articles";
import { compileDoc } from "@/src/helpers/compileDoc";
import { estimateReadingTime } from "@/src/helpers/readingTime";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL, toAbsoluteUrl } from "@/src/helpers/siteUrl";
import { LEARN_SUMMARIES, TEMPLATE_SUMMARIES } from "@/src/data/learn/content";
import type { ProductKey } from "@/src/data/learn/facets";
import type { LearnItemSummary } from "@/src/data/learn/types";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

const MAX_RELATED = 3;

export const dynamicParams = false;

export function generateStaticParams(): { slug: string }[] {
  return listArticleSlugs().map(({ slug }) => ({ slug }));
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const article = findArticleSummary(slug);
  if (!article) {
    return {};
  }
  return pageMetadata({
    title: article.title,
    description: article.description ?? article.title,
    path: article.href,
    keywords: [article.kind, ...article.tags],
  });
}

/** Related items for an article: templates and other catalog items sharing a product, capped at {@link MAX_RELATED}. */
function findRelated(products: readonly ProductKey[]): readonly LearnItemSummary[] {
  if (products.length === 0) {
    return [];
  }
  const overlap = (item: LearnItemSummary) => item.products.some((p) => products.includes(p));
  const templates = TEMPLATE_SUMMARIES.filter(overlap).slice(0, MAX_RELATED);
  if (templates.length >= MAX_RELATED) {
    return templates;
  }
  const usedSlugs = new Set(templates.map((t) => t.slug));
  const others = LEARN_SUMMARIES.filter(
    (item) => item.type !== "template" && !usedSlugs.has(item.slug) && overlap(item),
  );
  return [...templates, ...others].slice(0, MAX_RELATED);
}

export default async function ArticlePage({ params }: PageProps) {
  const { slug } = await params;
  const article = findArticleSummary(slug);
  const rel = resolveArticleFile(slug);
  if (!article || rel === null) {
    notFound();
  }

  const absPath = path.join(ARTICLES_ROOT, rel);
  const [{ content, toc }, raw] = await Promise.all([compileDoc(absPath), fs.readFile(absPath, "utf-8")]);
  const readingTime = estimateReadingTime(raw).text;
  const related = findRelated(article.products);
  const shareUrl = toAbsoluteUrl(article.href);

  const jsonLd = {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "Article",
        headline: article.title,
        ...(article.description ? { description: article.description } : {}),
        datePublished: toSchemaDate(article.date),
        ...(article.updated ? { dateModified: toSchemaDate(article.updated) } : {}),
        ...(article.featuredImage ? { image: toAbsoluteUrl(article.featuredImage) } : {}),
        ...(article.author
          ? {
              author: {
                "@type": "Person",
                name: article.author,
                ...(article.authorUrl ? { url: article.authorUrl } : {}),
              },
            }
          : {}),
        ...(article.tags.length > 0 ? { keywords: article.tags } : {}),
        publisher: { "@id": `${SITE_URL}/#organization` },
        mainEntityOfPage: shareUrl,
      },
      {
        "@type": "BreadcrumbList",
        itemListElement: [
          { "@type": "ListItem", position: 1, name: "Learn", item: `${SITE_URL}/learn` },
          { "@type": "ListItem", position: 2, name: article.title },
        ],
      },
    ],
  };

  return (
    <div className="cc-content-dark">
      <script
        type="application/ld+json"
        // Escape `<` so content text can never close the script tag (XSS).
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c") }}
      />
      <ArticleLayout
        breadcrumb={[{ label: "Learn", href: "/learn" }, { label: "Articles" }]}
        kind={article.kind}
        title={article.title}
        standfirst={article.description ?? undefined}
        meta={{
          author: article.author ?? undefined,
          authorUrl: article.authorUrl ?? undefined,
          authorImageUrl: article.authorImageUrl ?? undefined,
          publishedDate: article.date,
          updatedDate: article.updated ?? undefined,
          readingTime,
        }}
        heroImageSrc={article.featuredImage}
        shareUrl={shareUrl}
        tags={article.tags}
        toc={toc}
        related={
          related.length > 0 ? (
            <section className="border-cc-card-border mt-12 border-t pt-10 print:hidden">
              <h2 className="text-cc-heading m-0 mb-6 text-2xl font-semibold">Related</h2>
              <CardGrid cols={3} itemsStretch>
                {related.map((item) => (
                  <LearnCard key={item.slug} item={item} />
                ))}
              </CardGrid>
            </section>
          ) : null
        }
      >
        {content}
      </ArticleLayout>
    </div>
  );
}

function toSchemaDate(date: string): string {
  return /^\d{4}-\d{2}-\d{2}$/.test(date) ? `${date}T00:00:00.000Z` : date;
}
