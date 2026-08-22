import type { LearnContentType } from "@/src/data/learn/facets";
import { CONTENT_TYPE_META } from "./contentTypeMeta";

interface ContentTypeBadgeProps {
  readonly type: LearnContentType;
  readonly className?: string;
}

/** Tinted mono chip naming a learn item's content type, in the StatusChip shape (no dot). */
export function ContentTypeBadge({ type, className = "" }: ContentTypeBadgeProps) {
  const meta = CONTENT_TYPE_META[type];
  return (
    <span
      className={`inline-flex items-center rounded-[5px] px-1.5 py-0.5 font-mono text-[0.6rem] font-semibold tracking-[0.14em] uppercase ring-1 ring-inset ${meta.bg} ${meta.text} ${meta.ring} ${className}`}
    >
      {meta.label}
    </span>
  );
}
