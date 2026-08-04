import type { CSSProperties, ReactNode } from "react";

import { PopularBadge } from "@/src/components/PopularBadge";
import { popularBorderStyle } from "@/src/components/popularRing";
import { Card } from "@/src/design-system/Card";

interface HighlightCardProps {
  readonly children: ReactNode;
  readonly className?: string;
  /** Renders as `<article>` (`PerkCard`) instead of the default `<div>` (`Offering`, `TierGrid`). */
  readonly as?: "div" | "article";
  /** Rainbow gradient border plus a `PopularBadge`, instead of the plain bordered surface. */
  readonly highlight?: boolean;
  /** Overrides the `PopularBadge` label (defaults to "Most Popular"). */
  readonly badgeLabel?: string;
  /**
   * Number of in-flow subgrid rows `children` occupies, applied as
   * `gridRow: span N`. Pass this and `subgrid` together when the card is a row
   * item inside a `grid-template-rows: subgrid` parent (`PerkCard`,
   * `Offering`); omit both for a plain flex column (`TierGrid`'s `TierCard`).
   */
  readonly rowCount?: number;
  /** Lays `children` out as `grid grid-rows-subgrid` instead of `flex flex-col`. */
  readonly subgrid?: boolean;
  /** Gap between the in-flow children: Tailwind `gap-*` class. Defaults to `gap-5`. */
  readonly gap?: string;
  /** Padding: Tailwind `p-*` class(es). Defaults to `p-7 sm:p-9` (`TierGrid`'s value). */
  readonly padding?: string;
}

/**
 * The highlight-vs-plain card shell shared by `PerkCard`, `Offering`, and
 * `TierGrid`'s `TierCard`: a plain `cc-card-border`/`cc-card-bg` surface, or,
 * when `highlight` is set, a rainbow gradient border with a `PopularBadge`
 * tab. Owns only the border and badge treatment; each caller supplies its own
 * header/price/perk-list content as `children`.
 */
export function HighlightCard({
  children,
  className,
  as: Tag = "div",
  highlight = false,
  badgeLabel,
  rowCount,
  subgrid = false,
  gap = "gap-5",
  padding = "p-7 sm:p-9",
}: HighlightCardProps) {
  const style: CSSProperties = {
    ...(rowCount !== undefined ? { gridRow: `span ${rowCount}` } : {}),
    ...(highlight ? popularBorderStyle : {}),
  };

  const layoutCls = [
    "h-full rounded-3xl",
    subgrid ? "grid grid-rows-subgrid" : "flex flex-col",
    gap,
    padding,
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  if (!highlight) {
    return (
      <Card
        as={Tag}
        variant="plain"
        className={`bg-cc-card-bg/60 ${layoutCls}`}
        style={style}
      >
        {children}
      </Card>
    );
  }

  const cls = ["relative", layoutCls].filter(Boolean).join(" ");

  return (
    <Tag className={cls} style={style}>
      {highlight && <PopularBadge label={badgeLabel} />}
      {children}
    </Tag>
  );
}
