import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ComparisonHeader } from "@/src/components/compare/ComparisonHeader";
import { ComparisonHighlights } from "@/src/components/compare/ComparisonHighlights";
import { ComparisonSources } from "@/src/components/compare/ComparisonSources";
import { FeatureComparison } from "@/src/components/FeatureComparison";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { Card } from "@/src/design-system/Card";
import { compileDoc } from "@/src/helpers/compileDoc";
import { COMPARISONS_PATH, listComparisons } from "@/src/helpers/comparisons";
import { getLastModifiedFromGit } from "@/src/helpers/gitMetadata";
import { pageMetadata } from "@/src/helpers/pageMetadata";

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
      />
      <article className="mx-auto max-w-5xl">
        <ComparisonHeader comparison={comparison} />
        <ComparisonHighlights
          highlights={comparison.highlights}
          className="mt-16"
        />
        <FeatureComparison
          id="features"
          className="mt-20"
          eyebrow="Side by side"
          heading={`${product.title} and ${competitor} compared`}
          columns={[product.title, competitor]}
          groups={comparison.features.map((group) => ({
            title: group.title,
            rows: group.rows.map((row) => ({
              label: row.label,
              cells: [row.product, row.competitor],
            })),
          }))}
        />
        <Card className="mt-20 px-6 py-2 sm:px-10">{content}</Card>
        <ComparisonSources comparison={comparison} className="mt-16" />
      </article>
    </div>
  );
}
