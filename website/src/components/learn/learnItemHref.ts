import type { LearnItemSummary } from "@/src/data/learn/types";

/**
 * The link target for a learn item: the in-house template detail route for
 * templates, a video's own URL, or a non-template item's external URL. The
 * other four content types have no detail pages yet, so an item with neither
 * a matching case nor an external URL falls back to `"#"`.
 */
export function learnItemHref(item: LearnItemSummary): string {
  switch (item.type) {
    case "template":
      return `/learn/templates/${item.slug}`;
    case "video":
      return item.url;
    default:
      return item.externalUrl ?? "#";
  }
}
