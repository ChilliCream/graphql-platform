import type { LearnItemSummary } from "@/src/data/learn/types";

/**
 * The link target for a learn item: every catalog type has its own in-house
 * detail route (template, video, tutorial, example, workshop) except a video
 * without a `youtubeId` (a legacy entry with no native embed), which falls
 * back to its own external `url`. Cards never link straight out to GitHub or
 * docs: the detail page carries that link (its GitHub button, or an
 * equivalent external CTA) instead.
 */
export function learnItemHref(item: LearnItemSummary): string {
  switch (item.type) {
    case "template":
      return `/learn/templates/${item.slug}`;
    case "video":
      return item.youtubeId ? `/learn/videos/${item.slug}` : item.url;
    case "tutorial":
      return `/learn/tutorials/${item.slug}`;
    case "example":
      return `/learn/examples/${item.slug}`;
    case "workshop":
      return `/learn/workshops/${item.slug}`;
  }
}
