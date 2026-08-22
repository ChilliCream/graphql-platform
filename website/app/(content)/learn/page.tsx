import { Suspense } from "react";
import Link from "next/link";
import { CardGrid } from "@/src/components/CardGrid";
import { ContentTypeBadge } from "@/src/components/learn/ContentTypeBadge";
import { LearnCardSkeleton } from "@/src/components/learn/LearnCardSkeleton";
import { LearnCatalog } from "@/src/components/learn/LearnCatalog";
import { LearnClosing } from "@/src/components/learn/LearnClosing";
import { learnItemHref } from "@/src/components/learn/learnItemHref";
import { PageHero } from "@/src/components/PageHero";
import { CONTENT_TYPE_OPTIONS } from "@/src/data/learn/facets";
import { LEARN_ITEMS, LEARN_SUMMARIES } from "@/src/data/learn/content";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { listArticleSummaries, type ArticleSummary } from "@/src/helpers/articles";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL, toAbsoluteUrl } from "@/src/helpers/siteUrl";

export const metadata = pageMetadata({
  title: "Learn",
  description:
    "Templates, videos, tutorials, examples, and workshops for building with Hot Chocolate, Fusion, and the rest of the platform.",
  path: "/learn",
  keywords: ["GraphQL templates", "Hot Chocolate", "Fusion", "GraphQL tutorials", ".NET"],
});

// Default catalog order: templates flagged `featured` first, then by content
// type in CONTENT_TYPE_OPTIONS order, then title. `featured` lives only on
// the full TemplateItem (LEARN_ITEMS), not on the TemplateSummary shape the
// hub renders (LEARN_SUMMARIES); resolved by reading the flag off the full
// items and applying the resulting order to the summaries, rather than
// widening the summary type. See website-5yo.3 review notes.
const FEATURED_TEMPLATE_SLUGS = new Set(
  LEARN_ITEMS.filter((item) => item.type === "template" && item.featured).map((item) => item.slug),
);

function defaultOrder(a: LearnItemSummary, b: LearnItemSummary): number {
  const aFeatured = FEATURED_TEMPLATE_SLUGS.has(a.slug) ? 0 : 1;
  const bFeatured = FEATURED_TEMPLATE_SLUGS.has(b.slug) ? 0 : 1;
  if (aFeatured !== bFeatured) {
    return aFeatured - bFeatured;
  }
  const aTypeIndex = CONTENT_TYPE_OPTIONS.findIndex((option) => option.key === a.type);
  const bTypeIndex = CONTENT_TYPE_OPTIONS.findIndex((option) => option.key === b.type);
  if (aTypeIndex !== bTypeIndex) {
    return aTypeIndex - bTypeIndex;
  }
  return a.title.localeCompare(b.title);
}

const ORDERED_SUMMARIES: readonly LearnItemSummary[] = [...LEARN_SUMMARIES].sort(defaultOrder);

const ARTICLE_SUMMARIES = listArticleSummaries();

const STRUCTURED_DATA = {
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: "Home", item: `${SITE_URL}/` },
        { "@type": "ListItem", position: 2, name: "Learn" },
      ],
    },
    {
      "@type": "ItemList",
      name: "ChilliCream Learn Catalog",
      numberOfItems: ORDERED_SUMMARIES.length,
      itemListElement: ORDERED_SUMMARIES.map((item, index) => ({
        "@type": "ListItem",
        position: index + 1,
        name: item.title,
        url: toAbsoluteUrl(learnItemHref(item)),
      })),
    },
  ],
};

export default function LearnPage() {
  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(STRUCTURED_DATA) }} />
      <PageHero
        eyebrow="Learn"
        title="Learn ChilliCream"
        teaser="Templates, videos, tutorials, examples, and workshops for building with Hot Chocolate, Fusion, and the rest of the platform."
      />
      <Suspense fallback={<CatalogFallback />}>
        <LearnCatalog items={ORDERED_SUMMARIES} />
      </Suspense>
      <ArticlesRail articles={ARTICLE_SUMMARIES} />
      <LearnClosing />
    </>
  );
}

/** Explainers/comparisons rail: articles live in content/learn/articles/, outside the LearnItem catalog, so they get their own section rather than a catalog facet (see facets.ts doc comment). */
function ArticlesRail({ articles }: { readonly articles: readonly ArticleSummary[] }) {
  if (articles.length === 0) {
    return null;
  }
  return (
    <section className="border-cc-card-border border-t py-14 sm:py-20">
      <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">Explainers & comparisons</h2>
      <p className="text-cc-ink-dim mt-2 mb-8 max-w-2xl">
        Answer-first reading on how ChilliCream&rsquo;s platform works and how it compares.
      </p>
      <CardGrid cols={3} step="progressive" itemsStretch>
        {articles.map((article) => (
          <Link
            key={article.slug}
            href={article.href}
            className="border-cc-card-border bg-cc-card hover:border-cc-accent/60 flex flex-col gap-3 rounded-lg border p-5 transition-colors"
          >
            <ContentTypeBadge type={article.kind} />
            <span className="font-heading text-cc-heading text-lg font-semibold">{article.title}</span>
            {article.description ? <span className="text-cc-ink-dim text-sm">{article.description}</span> : null}
          </Link>
        ))}
      </CardGrid>
    </section>
  );
}

function CatalogFallback() {
  return (
    <div className="py-14 sm:py-20" aria-hidden="true">
      <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-wrap gap-2.5">
          {Array.from({ length: 6 }).map((_, index) => (
            <span key={index} className="bg-cc-hover h-8 w-24 animate-pulse rounded-full" />
          ))}
        </div>
        <span className="bg-cc-hover h-10 w-full animate-pulse rounded-lg lg:w-72" />
      </div>
      <div className="mb-8 flex flex-wrap gap-2.5">
        {Array.from({ length: 5 }).map((_, index) => (
          <span key={index} className="bg-cc-hover h-8 w-28 animate-pulse rounded-full" />
        ))}
      </div>
      <div className="bg-cc-hover mb-4 h-4 w-20 animate-pulse rounded" />
      <CardGrid cols={3} step="progressive" itemsStretch>
        {Array.from({ length: 6 }).map((_, index) => (
          <LearnCardSkeleton key={index} />
        ))}
      </CardGrid>
    </div>
  );
}
