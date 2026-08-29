// Per-content-type label for /learn (learn-harmonization.md section 2.3,
// D4/D12/D13: the per-type accent recipe is retired in favor of one neutral
// chip recipe defined by ContentTypeBadge and Tag directly). One record, so
// a type's display label is defined in exactly one place. contentTypeLabel
// in ../../data/learn/facets returns the PLURAL form used by facet options;
// this record supplies the singular badge form.

import { ARTICLE_LABEL } from "@/src/data/learn/hubs";
import type { LearnContentType } from "@/src/data/learn/facets";

export interface ContentTypeMeta {
  /** Singular label, e.g. "Template" (facets.contentTypeLabel returns the plural "Templates"). */
  readonly label: string;
}

export const CONTENT_TYPE_META: Record<LearnContentType, ContentTypeMeta> = {
  template: { label: "Template" },
  video: { label: "Video" },
  tutorial: { label: "Tutorial" },
  example: { label: "Example" },
  workshop: { label: "Workshop" },
  // Editorial reading types (learn-editorial.md section 6.1).
  comparison: { label: "Comparison" },
  explainer: { label: "Explainer" },
  article: { label: ARTICLE_LABEL },
};
