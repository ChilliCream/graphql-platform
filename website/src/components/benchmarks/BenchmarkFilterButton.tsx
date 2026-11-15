"use client";

import { BenchmarkFilterBar } from "@/src/components/benchmarks/BenchmarkFilterBar";
import type { BenchmarkControls } from "@/src/components/benchmarks/useBenchmarkControls";
import { FilterIcon } from "@/src/icons/FilterIcon";

interface BenchmarkFilterButtonProps {
  readonly controls: BenchmarkControls;
  /** The classes of the button, which set its size and border. */
  readonly className: string;
}

/** An icon button that opens the filters of the results tables, highlighted while a filter is applied. */
export function BenchmarkFilterButton({
  controls,
  className,
}: BenchmarkFilterButtonProps) {
  const { technologies, showPreviews } = controls.filters;
  const active = technologies.length > 0 || showPreviews;

  return (
    <BenchmarkFilterBar
      technologies={controls.technologies}
      filters={controls.filters}
      onChange={controls.setFilters}
      className="h-full"
      triggerClassName={`focus-visible:outline-cc-accent flex items-center justify-center transition-colors focus-visible:outline-2 focus-visible:-outline-offset-2 ${active ? "text-cc-accent" : "text-cc-ink-dim hover:text-cc-heading"} ${className}`}
    >
      <FilterIcon className="size-4 shrink-0" />
      <span className="sr-only">
        {active ? "Filter results, filters applied" : "Filter results"}
      </span>
    </BenchmarkFilterBar>
  );
}
