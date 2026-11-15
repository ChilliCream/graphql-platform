import { authorPersonId } from "@/src/data/authors";
import type { ArticleSummary } from "@/src/helpers/contentCollection";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";
import type { JsonLdNode } from "@/src/helpers/structuredData";
import { getShareImageSrc } from "@/src/image-optimization/manifest";

/** The comparison section itself, the entity every article is part of. */
export const COMPARISON_ID = schemaId("/comparison", "collection");
export const COMPARISON_DESCRIPTION =
  "Side-by-side comparisons of GraphQL Federation and the alternatives, " +
  "with a verdict on when each is the better buy.";

export function createComparisonCollectionNode(
  description: string = COMPARISON_DESCRIPTION,
): JsonLdNode {
  return {
    "@type": "CollectionPage",
    "@id": COMPARISON_ID,
    url: toAbsoluteUrl("/comparison"),
    name: "ChilliCream Comparisons",
    description,
    publisher: schemaRef(ORGANIZATION_ID),
    inLanguage: "en",
  };
}

export function createComparisonItemListNode(
  path: string,
  name: string,
  articles: readonly ArticleSummary[],
  startPosition = 1,
): JsonLdNode {
  return {
    "@type": "ItemList",
    "@id": schemaId(path, "articles"),
    name,
    numberOfItems: articles.length,
    itemListOrder: "https://schema.org/ItemListOrderDescending",
    itemListElement: articles.map((article, index) => ({
      "@type": "ListItem",
      position: startPosition + index,
      url: toAbsoluteUrl(article.href),
      item: {
        "@type": "TechArticle",
        "@id": schemaId(article.href, "article"),
        url: toAbsoluteUrl(article.href),
        headline: article.title,
        ...(article.description ? { description: article.description } : {}),
        datePublished: article.date,
        ...(article.featuredImage
          ? { image: toAbsoluteUrl(getShareImageSrc(article.featuredImage)) }
          : {}),
        publisher: schemaRef(ORGANIZATION_ID),
        ...(article.authorProfile
          ? {
              author: {
                "@type": "Person",
                "@id": authorPersonId(article.authorProfile),
                name: article.authorProfile.name,
                url: toAbsoluteUrl(`/authors/${article.authorProfile.slug}`),
              },
            }
          : article.author
            ? {
                author: {
                  "@type": "Person",
                  name: article.author,
                  ...(article.authorUrl ? { url: article.authorUrl } : {}),
                },
              }
            : {}),
        isPartOf: schemaRef(COMPARISON_ID),
        inLanguage: "en",
        isAccessibleForFree: true,
      },
    })),
  };
}
