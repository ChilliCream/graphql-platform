// Per-content-type presentation for /learn: singular badge label, CTA copy,
// and the accent tint recipe (StatusChip pattern: bg/10-15, ring/30, hover
// border/60) shared by ContentTypeBadge, the facet-bar pills, and LearnCard's
// hover border. One record, so a type's color is defined in exactly one
// place. contentTypeLabel in ../../data/learn/facets returns the PLURAL form
// used by facet options; this record supplies the singular badge form.

import type { LearnContentType } from "@/src/data/learn/facets";

export interface ContentTypeMeta {
  /** Singular label, e.g. "Template" (facets.contentTypeLabel returns the plural "Templates"). */
  readonly label: string;
  /** Trailing card affordance, e.g. "View template". */
  readonly ctaLabel: string;
  readonly text: string;
  readonly bg: string;
  readonly ring: string;
  readonly hoverBorder: string;
  /** Classes for the facet bar's active content-type pill. */
  readonly activePill: string;
}

export const CONTENT_TYPE_META: Record<LearnContentType, ContentTypeMeta> = {
  template: {
    label: "Template",
    ctaLabel: "View template",
    text: "text-cc-accent",
    bg: "bg-cc-accent/10",
    ring: "ring-cc-accent/30",
    hoverBorder: "hover:border-cc-accent/60",
    activePill: "bg-cc-accent/15 text-cc-accent border-cc-accent/40",
  },
  video: {
    label: "Video",
    ctaLabel: "Watch video",
    text: "text-cc-danger",
    bg: "bg-cc-danger/10",
    ring: "ring-cc-danger/30",
    hoverBorder: "hover:border-cc-danger/60",
    activePill: "bg-cc-danger/15 text-cc-danger border-cc-danger/40",
  },
  tutorial: {
    label: "Tutorial",
    ctaLabel: "Start tutorial",
    text: "text-cc-success",
    bg: "bg-cc-success/10",
    ring: "ring-cc-success/30",
    hoverBorder: "hover:border-cc-success/60",
    activePill: "bg-cc-success/15 text-cc-success border-cc-success/40",
  },
  example: {
    label: "Example",
    ctaLabel: "View example",
    text: "text-cc-info",
    bg: "bg-cc-info/10",
    ring: "ring-cc-info/30",
    hoverBorder: "hover:border-cc-info/60",
    activePill: "bg-cc-info/15 text-cc-info border-cc-info/40",
  },
  workshop: {
    label: "Workshop",
    ctaLabel: "View workshop",
    text: "text-cc-warning",
    bg: "bg-cc-warning/10",
    ring: "ring-cc-warning/30",
    hoverBorder: "hover:border-cc-warning/60",
    activePill: "bg-cc-warning/15 text-cc-warning border-cc-warning/40",
  },
  // Editorial reading types (learn-editorial.md section 6.1). Not wired into
  // CONTENT_TYPE_OPTIONS, so hoverBorder/activePill are unused by any catalog
  // surface today; they're filled in for interface completeness and so a
  // future LearnCard for these types needs no further decisions.
  comparison: {
    label: "Comparison",
    ctaLabel: "Read comparison",
    text: "text-cc-tip",
    bg: "bg-cc-tip/10",
    ring: "ring-cc-tip/30",
    hoverBorder: "hover:border-cc-tip/60",
    activePill: "bg-cc-tip/15 text-cc-tip border-cc-tip/40",
  },
  explainer: {
    label: "Explainer",
    ctaLabel: "Read explainer",
    text: "text-cc-note",
    bg: "bg-cc-note/10",
    ring: "ring-cc-note/30",
    hoverBorder: "hover:border-cc-note/60",
    activePill: "bg-cc-note/15 text-cc-note border-cc-note/40",
  },
};
