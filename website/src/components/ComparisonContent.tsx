import type { ReactNode } from "react";

interface ComparisonContentProps {
  /** The compiled article body. */
  readonly children: ReactNode;
}

/**
 * The body of a comparison article. Lays out the tables after the "Feature
 * comparison" heading (`#feature-comparison`) on one grid: the feature name
 * takes the same share in every table, the product columns split the rest
 * evenly, and the second column (our product) is tinted. Other tables, and
 * articles without that heading, keep the default table layout.
 */
export function ComparisonContent({ children }: ComparisonContentProps) {
  return (
    <div
      className={[
        // The first row fixes the column widths, so every table lines up.
        "[&_#feature-comparison~div>table]:table-fixed",
        "[&_#feature-comparison~div>table_th:first-child]:w-[36%]",
        // Wrap long values inside the column instead of widening the table.
        "[&_#feature-comparison~div>table_:is(th,td)]:break-words",
        "max-sm:[&_#feature-comparison~div>table_:is(th,td)]:px-2",
        // Our product's column.
        "[&_#feature-comparison~div>table_:is(th,td):nth-child(2)]:bg-cc-accent/5",
        "[&_#feature-comparison~div>table_th:nth-child(2)]:text-cc-accent",
      ].join(" ")}
    >
      {children}
    </div>
  );
}
