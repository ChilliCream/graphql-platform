"use client";

import type { ReactNode } from "react";
import { useHistoryChartHighlight } from "@/src/components/benchmarks/BenchmarkHistoryChartFrame";

interface BenchmarkHistoryLegendButtonProps {
  readonly seriesId: string;
  readonly children: ReactNode;
}

/** A legend entry that highlights its series on hover and focus, and pins it on click. */
export function BenchmarkHistoryLegendButton({
  seriesId,
  children,
}: BenchmarkHistoryLegendButtonProps) {
  const { active, pinned, highlight, endHover, focus, toggle } =
    useHistoryChartHighlight();
  const isActive = seriesId === active;
  const isPinned = seriesId === pinned;

  return (
    <li data-series={seriesId} className="w-60 max-w-full">
      <button
        type="button"
        aria-pressed={isPinned}
        onMouseEnter={() => highlight(seriesId)}
        onMouseLeave={endHover}
        onFocus={(event) => {
          if (event.currentTarget.matches(":focus-visible")) {
            focus(seriesId);
          }
        }}
        onBlur={() => focus(undefined)}
        onClick={() => toggle(seriesId)}
        className={`focus-visible:outline-cc-accent flex w-full cursor-pointer items-center gap-2.5 rounded-md px-2 py-1.5 text-left text-sm transition-colors focus-visible:outline-2 ${
          isPinned
            ? "bg-cc-white/5 rounded-l-none shadow-[inset_3px_0_0_0_var(--color-cc-accent)]"
            : isActive
              ? "bg-cc-hover"
              : "hover:bg-cc-hover"
        } ${active && !isActive ? "opacity-60" : ""}`}
      >
        {children}
      </button>
    </li>
  );
}
