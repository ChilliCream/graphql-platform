import type { ReactNode } from "react";

interface OfferingGridProps {
  /** Column utilities, e.g. "md:grid-cols-2 lg:grid-cols-3". */
  readonly columns: string;
  readonly children: ReactNode;
}

/**
 * Lays out `Offering` cards in a grid whose row tracks the cards share via
 * `grid-template-rows: subgrid`. Sharing tracks makes every section (title,
 * description, price, body) the same height across a row, so the cards line up
 * no matter how many lines the description wraps to. The grid gap is the gutter
 * between cards; each card resets its own inner gap to zero.
 */
export function OfferingGrid({ columns, children }: OfferingGridProps) {
  return <div className={`grid gap-6 ${columns}`}>{children}</div>;
}
