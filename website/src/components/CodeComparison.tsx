import type { ReactNode } from "react";

interface CodeComparisonProps {
  /** Two fenced code blocks, each labelled with `filename="..."`. */
  readonly children: ReactNode;
}

/**
 * Two code blocks next to each other when the content column is wide enough
 * and stacked otherwise, for showing the same task written with two libraries.
 */
export function CodeComparison({ children }: CodeComparisonProps) {
  return (
    <div className="@container my-8">
      <div className="grid gap-4 @min-[62rem]:grid-cols-2 [&>figure]:my-0 [&>figure]:min-w-0">
        {children}
      </div>
    </div>
  );
}
