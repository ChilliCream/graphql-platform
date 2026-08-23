import type { LearnContentType } from "@/src/data/learn/facets";
import { CONTENT_TYPE_META } from "./contentTypeMeta";

interface ContentTypeBadgeProps {
  readonly type: LearnContentType;
  readonly className?: string;
}

/**
 * Neutral mono chip naming a learn item's content type (learn-harmonization.md
 * section 2.3, D4): one recipe for every type, the label text is the only
 * differentiator. `self-start` keeps the chip's own width even inside a
 * `flex flex-col` parent, where `align-items: stretch` would otherwise
 * blockify it to the column's full width (D1).
 */
export function ContentTypeBadge({ type, className = "" }: ContentTypeBadgeProps) {
  const meta = CONTENT_TYPE_META[type];
  return (
    <span
      className={`text-cc-ink-dim bg-cc-hover ring-cc-card-border inline-flex shrink-0 items-center self-start rounded-full px-2 py-0.5 font-mono text-[0.6875rem] font-semibold tracking-[0.14em] uppercase ring-1 ring-inset ${className}`}
    >
      {meta.label}
    </span>
  );
}
