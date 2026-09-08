import type { Metadata } from "next";

import { pageMetadata } from "@/src/helpers/pageMetadata";
import {
  type BreadcrumbItem,
  type JsonLdNode,
  LOGO_ID,
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

import { COMPARISONS, type FederationComparison } from "../comparisons";

/** The explainer page every comparison hangs off. */
export const FEDERATION_PATH = "/platform/graphql-federation";

/** Route of a comparison page, e.g. `/platform/graphql-federation/vs-bff`. */
export function comparisonPath(slug: string): string {
  return `${FEDERATION_PATH}/${slug}`;
}

/**
 * Where a {@link FederationComparison} points. While the comparison routes are
 * still in-page anchors on the explainer, the anchor is resolved against the
 * explainer path so the link works from a comparison page too; once
 * `comparisons.ts` carries real routes the href is used as it stands.
 */
export function comparisonHref(comparison: FederationComparison): string {
  return comparison.href.startsWith("#")
    ? `${FEDERATION_PATH}${comparison.href}`
    : comparison.href;
}

/** The `PageStructuredData` props a comparison route spreads. */
export interface ComparisonStructuredData {
  readonly title: string;
  readonly description: string;
  readonly path: string;
  readonly breadcrumbs: readonly BreadcrumbItem[];
  readonly mainEntity: JsonLdNode;
  readonly additionalNodes: readonly JsonLdNode[];
}

export interface ComparisonPageBuild {
  readonly comparison: FederationComparison;
  readonly path: string;
  readonly metadata: Metadata;
  readonly structuredData: ComparisonStructuredData;
}

/**
 * Builds the metadata and the structured-data graph of one comparison page from
 * its entry in {@link COMPARISONS}, so each route only has to render the layout:
 *
 * ```tsx
 * const { metadata, structuredData } = buildComparisonPage("vs-bff");
 * ```
 */
export function buildComparisonPage(slug: string): ComparisonPageBuild {
  const comparison = COMPARISONS.find((entry) => entry.slug === slug);

  if (!comparison) {
    throw new Error(`Unknown GraphQL Federation comparison: "${slug}".`);
  }

  const path = comparisonPath(slug);
  const description = comparison.metaDescription ?? comparison.summary;
  const articleId = schemaId(path, "article");

  const article: JsonLdNode = {
    "@type": "TechArticle",
    "@id": articleId,
    headline: comparison.title,
    description,
    inLanguage: "en",
    image: schemaRef(LOGO_ID),
    publisher: schemaRef(ORGANIZATION_ID),
    mainEntityOfPage: schemaRef(schemaId(path, "webpage")),
  };

  return {
    comparison,
    path,
    metadata: pageMetadata({
      title: comparison.title,
      description,
      path,
      keywords: comparison.keywords,
    }),
    structuredData: {
      title: comparison.title,
      description,
      path,
      breadcrumbs: [
        { name: "Home", path: "/" },
        { name: "Platform", path: "/platform" },
        { name: "GraphQL Federation", path: FEDERATION_PATH },
        { name: comparison.title },
      ],
      mainEntity: schemaRef(articleId),
      additionalNodes: [article],
    },
  };
}
