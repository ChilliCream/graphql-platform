import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ComparisonGapOptions } from "@/src/components/compare/ComparisonGapOptions";
import { ComparisonHeader } from "@/src/components/compare/ComparisonHeader";
import { ComparisonSources } from "@/src/components/compare/ComparisonSources";
import { DotGridSurface } from "@/src/components/DotGridSurface";
import { FaqSection } from "@/src/components/FaqSection";
import { FeatureComparison } from "@/src/components/FeatureComparison";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { compileDoc } from "@/src/helpers/compileDoc";
import { COMPARISONS_PATH, listComparisons } from "@/src/helpers/comparisons";
import { getLastModifiedFromGit } from "@/src/helpers/gitMetadata";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { createFaqNode } from "@/src/helpers/structuredData";

type Params = { slug: string };
type PageProps = { params: Promise<Params> };

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  const params = listComparisons().map((comparison) => ({
    slug: comparison.slug,
  }));
  // output: export requires at least one prerendered path; the placeholder
  // renders a 404 via notFound() when there are no comparisons.
  return params.length > 0 ? params : [{ slug: "__empty__" }];
}

export async function generateMetadata({
  params,
}: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const comparison = listComparisons().find(
    (candidate) => candidate.slug === slug,
  );
  if (!comparison) {
    return {};
  }

  return pageMetadata({
    title: comparison.metaTitle,
    description: comparison.description,
    path: comparison.href,
  });
}

export default async function ComparisonPage({ params }: PageProps) {
  const { slug } = await params;
  const comparison = listComparisons().find(
    (candidate) => candidate.slug === slug,
  );
  if (!comparison) {
    notFound();
  }

  const [{ content }, lastModified] = await Promise.all([
    compileDoc(comparison.file),
    getLastModifiedFromGit(comparison.file),
  ]);
  const { product, competitor } = comparison;
  const docsPath = `/docs/${product.slug}`;

  return (
    <div className="px-5 py-8 sm:px-12">
      <PageStructuredData
        title={comparison.metaTitle}
        description={comparison.description}
        dateModified={lastModified?.toISOString()}
        path={comparison.href}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Compare", path: COMPARISONS_PATH },
          { name: comparison.title },
        ]}
        additionalNodes={
          comparison.faq.length > 0
            ? [createFaqNode(comparison.href, comparison.faq)]
            : []
        }
      />
      <article className="mx-auto max-w-5xl">
        <ComparisonHeader comparison={comparison} />
        <div className="mt-6 [&_li]:max-w-3xl [&_p]:max-w-3xl">{content}</div>
        <FeatureComparison
          id="features"
          className="mt-24"
          eyebrow="Summary"
          heading={`${product.title} and ${competitor} side by side`}
          columns={[product.title, competitor]}
          groups={comparison.features.map((group) => ({
            title: group.title,
            rows: group.rows.map((row) => ({
              label: row.label,
              cells: [row.product, row.competitor],
            })),
          }))}
        />
        <ComparisonGapOptions
          product={product.title}
          gaps={comparison.gaps}
          className="mt-24"
        />
        {comparison.faq.length > 0 ? (
          <FaqSection
            id="faq"
            className="mt-24 scroll-mt-24"
            eyebrow="FAQ"
            heading={`${product.title} and ${competitor} questions`}
            items={comparison.faq}
          />
        ) : null}
        <ComparisonSources comparison={comparison} className="mt-24" />
        <DotGridSurface className="mt-24 rounded-3xl">
          <NextStepsSection
            skin="accent"
            className="py-0"
            title={`Try ${product.title} in your project.`}
            text={`Start with the docs, or move from ${competitor} step by step with the migration guide.`}
            primaryLink={docsPath}
            primaryLinkText={`Get started with ${product.title}`}
            secondaryLink={comparison.migrationGuide ?? docsPath}
            secondaryLinkText={
              comparison.migrationGuide
                ? `Migrate from ${competitor}`
                : "Read the docs"
            }
          />
        </DotGridSurface>
      </article>
    </div>
  );
}
