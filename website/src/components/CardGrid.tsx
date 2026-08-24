import type { ReactNode } from "react";

interface CardGridProps {
  readonly children: ReactNode;
  /** Column count once the grid reaches `breakpoint` (or the top step, for `progressive`). `4` is only meaningful with `step="progressive"`. */
  readonly cols: 2 | 3 | 4;
  /**
   * How the column count ramps up from a single column.
   * `"single"` (default) jumps straight to `cols` at `breakpoint`, e.g.
   * `md:grid-cols-3`.
   * `"progressive"` adds a 2-column step at `sm:` before reaching 3 columns at
   * `lg:` (i.e. `sm:grid-cols-2 lg:grid-cols-3`); `cols=4` adds a further
   * `xl:grid-cols-4` step on top of that.
   */
  readonly step?: "single" | "progressive";
  /** Breakpoint at which the grid reaches `cols` columns. Defaults to `md`. Ignored when `step` is `"progressive"`. */
  readonly breakpoint?: "sm" | "md" | "lg";
  /** Grid gap. Defaults to `6`. */
  readonly gap?: 4 | 6;
  /** Stretches every row to the tallest cell (`items-stretch` at `breakpoint`). */
  readonly itemsStretch?: boolean;
  /**
   * With `step="progressive"` and `cols={4}`, skips the intermediate
   * 3-column step and jumps straight from `sm:grid-cols-2` to
   * `lg:grid-cols-4`. For a grid that always holds an exact multiple of 4
   * items, 3 columns orphans the last row; this keeps every row full at
   * every breakpoint. Ignored for other `cols`/`step` combinations.
   */
  readonly skipThreeCol?: boolean;
}

const SINGLE_COLS_CLASS: Record<"sm" | "md" | "lg", Record<2 | 3 | 4, string>> = {
  sm: { 2: "sm:grid-cols-2", 3: "sm:grid-cols-3", 4: "sm:grid-cols-4" },
  md: { 2: "md:grid-cols-2", 3: "md:grid-cols-3", 4: "md:grid-cols-4" },
  lg: { 2: "lg:grid-cols-2", 3: "lg:grid-cols-3", 4: "lg:grid-cols-4" },
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

const PROGRESSIVE_COLS_CLASS: Record<2 | 3 | 4, string> = {
  2: "sm:grid-cols-2",
  3: "sm:grid-cols-2 lg:grid-cols-3",
  4: "sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4",
};

/** `step="progressive"` with `cols={4}` and `skipThreeCol`: see `CardGridProps.skipThreeCol`. */
const PROGRESSIVE_FOUR_COLS_SKIP_THREE_CLASS = "sm:grid-cols-2 lg:grid-cols-4";

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
  skipThreeCol = false,
}: CardGridProps) {
  const colsClass =
    step === "progressive"
      ? cols === 4 && skipThreeCol
        ? PROGRESSIVE_FOUR_COLS_SKIP_THREE_CLASS
        : PROGRESSIVE_COLS_CLASS[cols]
      : SINGLE_COLS_CLASS[breakpoint][cols];

  return (
    <div className={`grid ${GAP_CLASS[gap]} ${colsClass}${itemsStretch ? ` ${ITEMS_STRETCH_CLASS[breakpoint]}` : ""}`}>
      {children}
    </div>
  );
}
