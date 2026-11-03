import { benchmarkTechnology } from "@/src/components/benchmarks/benchmarkFilters";
import { BenchmarkHistoryChartFrame } from "@/src/components/benchmarks/BenchmarkHistoryChartFrame";
import {
  historyTimeScale,
  type HistoryPoint,
  historyScale,
  historySeries,
  latestCompletedPoint,
  seriesChange,
  type HistorySeries,
} from "@/src/components/benchmarks/benchmarkView";
import type {
  BenchmarkCatalogEntry,
  BenchmarkHistoryScenario,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkHistoryChartProps {
  readonly id: string;
  readonly scenario: BenchmarkHistoryScenario;
  readonly variants: ReadonlyMap<string, BenchmarkCatalogEntry>;
}

/** The throughput history of a scenario with every variant of `scenario`, scaled to the released series. The frame hides the series that the filters exclude. */
export function BenchmarkHistoryChart({
  id,
  scenario,
  variants,
}: BenchmarkHistoryChartProps) {
  const series = historySeries(scenario, variants);
  if (series.length === 0) {
    return null;
  }
  const rps = (items: readonly HistorySeries[]) =>
    items.flatMap((item) => item.points.map((point) => point.rps));
  const released = series.filter((item) => !item.preview);
  const scale = historyScale(rps(released.length > 0 ? released : series));
  const time = historyTimeScale(
    series.flatMap((item) => item.points.map((point) => point.timestamp)),
  );
  const dates = time.ticks;
  const dateIndex = new Map(dates.map((date, index) => [date, index]));
  const failureNote = (point: HistoryPoint) => {
    const measuredRuns = point.measuredRuns ?? scenario.config.measuredRuns;
    return measuredRuns !== undefined && point.completedRuns < measuredRuns
      ? `Only ${point.completedRuns} of ${measuredRuns} runs completed`
      : "Failed";
  };

  return (
    <BenchmarkHistoryChartFrame
      id={id}
      series={series.map((item) => {
        const newest = latestCompletedPoint(item) ?? item.points.at(-1)!;
        const change = seriesChange(item);
        return {
          id: item.id,
          name: item.name,
          runtime: item.runtime,
          composition: item.composition,
          technology: benchmarkTechnology(variants.get(item.id)!),
          preview: item.preview,
          color: item.color,
          dashed: item.dashed,
          newestRps: newest.rps,
          ...(change !== undefined
            ? {
                change: {
                  value: change,
                  from: item.points.find((point) => !point.failed)!.version,
                  to: newest.version,
                },
              }
            : {}),
          ...(item.points.at(-1)!.failed ? { latestFailed: true } : {}),
          points: item.points.map((point) => ({
            date: dateIndex.get(point.timestamp)!,
            rps: point.rps,
            version: point.version,
            ...(point.runtime !== item.runtime
              ? { runtime: point.runtime }
              : {}),
            ...(point.failed ? { failure: failureNote(point) } : {}),
          })),
        };
      })}
      domain={[scale.ticks[0], scale.ticks.at(-1)!]}
      dates={dates}
    />
  );
}
