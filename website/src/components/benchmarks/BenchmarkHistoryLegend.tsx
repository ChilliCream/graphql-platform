import { memo } from "react";
import type { HistoryChartSeries } from "@/src/components/benchmarks/BenchmarkHistoryChartFrame";
import { BenchmarkHistoryLegendButton } from "@/src/components/benchmarks/BenchmarkHistoryLegendButton";
import {
  COMPOSITION_GROUPS,
  formatChange,
  formatNumber,
} from "@/src/components/benchmarks/benchmarkView";
import { BenchmarkPreviewBadge } from "@/src/components/benchmarks/BenchmarkPreviewBadge";
import { WarningIcon } from "@/src/icons/Warning";

interface BenchmarkHistoryLegendProps {
  readonly id: string;
  readonly series: readonly HistoryChartSeries[];
}

/** The legend of a history chart: the series of each composition, highest newest throughput first. */
export const BenchmarkHistoryLegend = memo(function BenchmarkHistoryLegend({
  id,
  series,
}: BenchmarkHistoryLegendProps) {
  return COMPOSITION_GROUPS.map((group) => {
    const members = series
      .filter((item) => item.composition === group.id)
      .toSorted((a, b) => b.points.at(-1)!.rps - a.points.at(-1)!.rps);
    if (members.length === 0) {
      return null;
    }
    return (
      <div key={group.id} data-legend-group={group.id}>
        <p
          id={`${id}-legend-${group.id}`}
          className="text-cc-ink-dim mb-1.5 text-xs font-medium"
        >
          {group.name}
        </p>
        <ul
          aria-labelledby={`${id}-legend-${group.id}`}
          className="-mx-2 flex flex-wrap gap-x-1 gap-y-0.5"
        >
          {members.map((item) => (
            <BenchmarkHistoryLegendButton key={item.id} seriesId={item.id}>
              <LegendEntry series={item} />
            </BenchmarkHistoryLegendButton>
          ))}
        </ul>
      </div>
    );
  });
});

interface LegendEntryProps {
  readonly series: HistoryChartSeries;
}

function LegendEntry({ series }: LegendEntryProps) {
  const { change } = series;

  return (
    <>
      <svg
        className="h-3 w-6 shrink-0 overflow-visible"
        viewBox="0 0 24 12"
        aria-hidden="true"
      >
        <line
          x1="1"
          y1="6"
          x2="23"
          y2="6"
          stroke={series.color}
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeDasharray={series.dashed ? "3 3" : undefined}
        />
        <circle
          cx="12"
          cy="6"
          r="4"
          stroke={series.color}
          strokeWidth="2"
          fill={series.dashed ? "var(--color-cc-bg)" : series.color}
        />
      </svg>
      <span
        className="min-w-0 flex-1"
        data-nosnippet={series.preview ? "" : undefined}
      >
        <span className="flex min-w-0 items-center gap-1">
          {series.preview && <BenchmarkPreviewBadge />}
          <span
            className="text-cc-heading min-w-0 truncate"
            title={series.name}
          >
            {series.name}
          </span>
          <span
            className="border-cc-card-border text-cc-ink-dim max-w-1/2 min-w-0 shrink-0 truncate rounded border px-1.5 font-mono text-[11px] leading-5"
            title={series.runtime}
          >
            {series.runtime}
          </span>
        </span>
        <span className="mt-0.5 flex items-baseline gap-2 text-xs tabular-nums">
          <span className="text-cc-heading font-semibold">
            {formatNumber(series.newestRps)}
          </span>
          <span className="text-cc-ink-dim">requests/s</span>
          {change && (
            <span
              className={
                change.value >= 0 ? "text-cc-success" : "text-cc-danger"
              }
              title={`Change from ${change.from} to ${change.to}`}
            >
              <span aria-hidden="true">{change.value >= 0 ? "▲" : "▼"}</span>{" "}
              <span className="sr-only">
                {change.value >= 0 ? "up" : "down"}
              </span>
              {formatChange(change.value)}
            </span>
          )}
          {series.latestFailed && (
            <span
              className="text-cc-danger self-center"
              title="Latest run failed"
            >
              <WarningIcon className="size-3.5" />
              <span className="sr-only">Latest run failed</span>
            </span>
          )}
        </span>
      </span>
    </>
  );
}
