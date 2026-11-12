import productGroups from "@/src/data/productGroups.json";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";

export type JsonLdScalar = string | number | boolean | null;

export type JsonLdValue = JsonLdScalar | JsonLdNode | readonly JsonLdValue[];

export interface JsonLdNode {
  readonly "@id"?: string;
  readonly "@type"?: string | readonly string[];
  readonly [property: string]: JsonLdValue | undefined;
}

export interface JsonLdGraph {
  readonly "@context": "https://schema.org";
  readonly "@graph": readonly JsonLdNode[];
}

export type JsonLdDocument = JsonLdNode | JsonLdGraph;

export const ORGANIZATION_ID = toAbsoluteUrl("/#organization");
export const WEBSITE_ID = toAbsoluteUrl("/#website");
export const LOGO_ID = toAbsoluteUrl("/#logo");

export interface ProductGroup {
  readonly id: string;
  readonly name: string;
  readonly memberIds: readonly string[];
}

const SOFTWARE_NODE_FRAGMENTS: Readonly<Record<string, string>> = {
  fusion: "software",
  nitro: "product",
  hotchocolate: "software",
  strawberryshake: "software",
  mocha: "software",
};

function toProductGroup(slug: string): ProductGroup {
  const group = productGroups.find((candidate) => candidate.slug === slug);
  if (!group) {
    throw new Error(`Unknown product group: ${slug}`);
  }

  return {
    id: toAbsoluteUrl(`/#product-group-${group.slug}`),
    name: group.name,
    memberIds: group.products.map((product) =>
      toAbsoluteUrl(`/products/${product}#${SOFTWARE_NODE_FRAGMENTS[product]}`),
    ),
  };
}

export const FEDERATION_PLATFORM_GROUP = toProductGroup(
  "graphql-federation-platform",
);

export const FRAMEWORKS_GROUP = toProductGroup("graphql-frameworks");

export const MESSAGING_GROUP = toProductGroup("messaging-framework");

export const PRODUCT_GROUPS: readonly ProductGroup[] = productGroups.map(
  (group) => toProductGroup(group.slug),
);

export interface BreadcrumbItem {
  readonly name: string;
  /** Omit only for the current page at the end of the trail. */
  readonly path?: string;
}

interface PageStructuredDataInput {
  readonly title: string;
  readonly description?: string;
  readonly dateModified?: string;
  readonly path: string;
  readonly pageType?: string | readonly string[];
  readonly breadcrumbs?: readonly BreadcrumbItem[];
  readonly mainEntity?: JsonLdNode;
  readonly about?: JsonLdNode | readonly JsonLdNode[];
  readonly additionalNodes?: readonly JsonLdNode[];
}

interface FaqItem {
  readonly question: string;
  readonly answer: string;
}

interface ItemListEntry {
  readonly name: string;
  readonly url: string;
  readonly description?: string;
  readonly itemType?: string;
}

interface ItemListOptions {
  readonly order?:
    | "https://schema.org/ItemListOrderAscending"
    | "https://schema.org/ItemListOrderDescending"
    | "https://schema.org/ItemListUnordered";
  readonly startPosition?: number;
  readonly idFragment?: string;
}

export function schemaId(path: string, fragment: string): string {
  return `${toAbsoluteUrl(path)}#${fragment}`;
}

export function schemaRef(id: string): JsonLdNode {
  return { "@id": id };
}

export function createProductGroupNodes(): readonly JsonLdNode[] {
  return PRODUCT_GROUPS.map((group) => ({
    "@type": "Collection",
    "@id": group.id,
    name: group.name,
    publisher: schemaRef(ORGANIZATION_ID),
    hasPart: group.memberIds.map(schemaRef),
  }));
}

export function createPageGraph({
  title,
  description,
  dateModified,
  path,
  pageType = "WebPage",
  breadcrumbs,
  mainEntity,
  about,
  additionalNodes = [],
}: PageStructuredDataInput): JsonLdGraph {
  const url = toAbsoluteUrl(path);
  const pageId = schemaId(path, "webpage");
  const breadcrumb =
    breadcrumbs && breadcrumbs.length >= 1
      ? createBreadcrumbNode(path, breadcrumbs)
      : null;

  const page: JsonLdNode = {
    "@type": pageType,
    "@id": pageId,
    url,
    name: title,
    ...(description ? { description } : {}),
    ...(dateModified ? { dateModified } : {}),
    isPartOf: schemaRef(WEBSITE_ID),
    publisher: schemaRef(ORGANIZATION_ID),
    inLanguage: "en",
    ...(breadcrumb ? { breadcrumb: schemaRef(breadcrumb["@id"]!) } : {}),
    ...(mainEntity ? { mainEntity } : {}),
    ...(about ? { about } : {}),
  };

  return {
    "@context": "https://schema.org",
    "@graph": [page, ...(breadcrumb ? [breadcrumb] : []), ...additionalNodes],
  };
}

export function createBreadcrumbNode(
  path: string,
  items: readonly BreadcrumbItem[],
): JsonLdNode {
  return {
    "@type": "BreadcrumbList",
    "@id": schemaId(path, "breadcrumb"),
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      ...(item.path ? { item: toAbsoluteUrl(item.path) } : {}),
    })),
  };
}

export function createFaqNode(
  path: string,
  items: readonly FaqItem[],
): JsonLdNode {
  return {
    "@type": "FAQPage",
    "@id": schemaId(path, "faq"),
    isPartOf: schemaRef(schemaId(path, "webpage")),
    mainEntity: items.map((item) => ({
      "@type": "Question",
      name: item.question,
      acceptedAnswer: {
        "@type": "Answer",
        text: item.answer,
      },
    })),
  };
}

export function createItemListNode(
  path: string,
  name: string,
  items: readonly ItemListEntry[],
  {
    order = "https://schema.org/ItemListOrderAscending",
    startPosition = 1,
    idFragment = "item-list",
  }: ItemListOptions = {},
): JsonLdNode {
  return {
    "@type": "ItemList",
    "@id": schemaId(path, idFragment),
    name,
    numberOfItems: items.length,
    itemListOrder: order,
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: startPosition + index,
      url: toAbsoluteUrl(item.url),
      item: {
        "@type": item.itemType ?? "Thing",
        name: item.name,
        url: toAbsoluteUrl(item.url),
        ...(item.description ? { description: item.description } : {}),
      },
    })),
  };
}
