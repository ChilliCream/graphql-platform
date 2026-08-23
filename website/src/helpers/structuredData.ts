import { SITE_URL } from "@/src/helpers/siteUrl";

/** `@id` of the site-wide `Organization` node emitted once in the root layout (see `StructuredData`). */
export const ORGANIZATION_ID = `${SITE_URL}/#organization`;

/** `@id` of the site-wide `WebSite` node emitted once in the root layout (see `StructuredData`). */
export const WEBSITE_ID = `${SITE_URL}/#website`;

export interface BreadcrumbItem {
  readonly name: string;
  /** Root-relative path for this crumb. Omitted for the current page, which schema.org leaves without a URL. */
  readonly path?: string;
}

/**
 * Builds a schema.org `BreadcrumbList` node from an ordered list of crumbs,
 * starting at position 1.
 */
export function breadcrumbList(items: readonly BreadcrumbItem[]) {
  return {
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      ...(item.path ? { item: `${SITE_URL}${item.path}` } : {}),
    })),
  };
}
