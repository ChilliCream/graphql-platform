import type { LearnItemSummary } from "@/src/data/learn/types";

/**
 * The link target for a learn item: the in-house template detail route for
 * templates, the in-house video detail route for a video with a `youtubeId`,
 * or a non-template item's external URL. A video without a `youtubeId` (a
 * legacy entry with no native embed) falls back to its own external `url`.
 * Tutorials, examples, and workshops have no detail pages yet, so an item
 * with neither a matching case nor an external URL falls back to `"#"`.
 */
export function learnItemHref(item: LearnItemSummary): string {
  switch (item.type) {
    case "template":
      return `/learn/templates/${item.slug}`;
    case "video":
      return item.youtubeId ? `/learn/videos/${item.slug}` : item.url;
    default:
      return item.externalUrl ?? "#";
  }
}
