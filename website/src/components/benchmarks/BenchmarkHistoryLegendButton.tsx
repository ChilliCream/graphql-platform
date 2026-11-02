"use client";

import type { ReactNode } from "react";
import { useHistoryChartPin } from "@/src/components/benchmarks/BenchmarkHistoryChartFrame";

interface BenchmarkHistoryLegendButtonProps {
  readonly seriesId: string;
  readonly children: ReactNode;
}

/** A legend entry that highlights its series on hover and focus, and pins it on click. */
export function BenchmarkHistoryLegendButton({
  seriesId,
  children,
}: BenchmarkHistoryLegendButtonProps) {
  const { pinned, toggle } = useHistoryChartPin();
  const isPinned = seriesId === pinned;

  return (
    <li data-series={seriesId} className="w-60 max-w-full">
      <button
        type="button"
        aria-pressed={isPinned}
        onClick={() => toggle(seriesId)}
        className={`focus-visible:outline-cc-accent flex w-full cursor-pointer items-center gap-2.5 rounded-md px-2 py-1.5 text-left text-sm transition-colors focus-visible:outline-2 ${
          isPinned
            ? "bg-cc-white/5 rounded-l-none shadow-[inset_3px_0_0_0_var(--color-cc-accent)]"
            : ""
        }`}
      >
        {children}
      </button>
    </li>
  );
}
