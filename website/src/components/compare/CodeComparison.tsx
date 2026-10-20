import type { ReactNode } from "react";

interface CodeComparisonProps {
  /** Two fenced code blocks, each labelled with `filename="..."`. */
  readonly children: ReactNode;
}

/**
 * Two code blocks next to each other on wide screens and stacked on narrow
 * ones, for showing the same task written with two libraries.
 */
export function CodeComparison({ children }: CodeComparisonProps) {
  return (
    <div className="my-8 grid gap-4 lg:grid-cols-2 [&>figure]:my-0 [&>figure]:min-w-0">
      {children}
    </div>
  );
}
