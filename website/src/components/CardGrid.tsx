import type { ReactNode } from "react";

interface CardGridProps {
  readonly children: ReactNode;
  /** Column count once the grid reaches `breakpoint` (or the `sm:` step, for `progressive`). */
  readonly cols: 2 | 3;
  /**
   * How the column count ramps up from a single column.
   * `"single"` (default) jumps straight to `cols` at `breakpoint`, e.g.
   * `md:grid-cols-3`.
   * `"progressive"` adds a 2-column step at `sm:` before reaching 3 columns at
   * `lg:` (i.e. `sm:grid-cols-2 lg:grid-cols-3`). Only meaningful with `cols=3`.
   */
  readonly step?: "single" | "progressive";
  /** Breakpoint at which the grid reaches `cols` columns. Defaults to `md`. Ignored when `step` is `"progressive"`. */
  readonly breakpoint?: "sm" | "md" | "lg";
  /** Grid gap. Defaults to `6`. */
  readonly gap?: 4 | 6;
  /** Stretches every row to the tallest cell (`items-stretch` at `breakpoint`). */
  readonly itemsStretch?: boolean;
}

const SINGLE_COLS_CLASS: Record<"sm" | "md" | "lg", Record<2 | 3, string>> = {
  sm: { 2: "sm:grid-cols-2", 3: "sm:grid-cols-3" },
  md: { 2: "md:grid-cols-2", 3: "md:grid-cols-3" },
  lg: { 2: "lg:grid-cols-2", 3: "lg:grid-cols-3" },
};

const ITEMS_STRETCH_CLASS: Record<"sm" | "md" | "lg", string> = {
  sm: "sm:items-stretch",
  md: "md:items-stretch",
  lg: "lg:items-stretch",
};

const GAP_CLASS: Record<4 | 6, string> = {
  4: "gap-4",
  6: "gap-6",
};

const PROGRESSIVE_COLS_CLASS = "sm:grid-cols-2 lg:grid-cols-3";

/**
 * Lays out cards (`IconFeatureCard`, `PerkCard`, or an ad hoc card) in a
 * responsive grid. Consumers render their own cards as children; this
 * component only owns the grid's column ramp, gap, and row stretch.
 */
export function CardGrid({
  children,
  cols,
  step = "single",
  breakpoint = "md",
  gap = 6,
  itemsStretch = false,
}: CardGridProps) {
  const colsClass =
    step === "progressive"
      ? PROGRESSIVE_COLS_CLASS
      : SINGLE_COLS_CLASS[breakpoint][cols];

  return (
    <div
      className={`grid ${GAP_CLASS[gap]} ${colsClass}${
        itemsStretch ? ` ${ITEMS_STRETCH_CLASS[breakpoint]}` : ""
      }`}
    >
      {children}
    </div>
  );
}
