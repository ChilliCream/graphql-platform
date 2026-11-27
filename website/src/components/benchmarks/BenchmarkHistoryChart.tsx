import { benchmarkTechnology } from "@/src/components/benchmarks/benchmarkFilters";
import { BenchmarkHistoryChartFrame } from "@/src/components/benchmarks/BenchmarkHistoryChartFrame";
import {
  HISTORY_METRICS,
  historyTimeScale,
  type HistoryMetric,
  type HistoryPoint,
  type HistoryValues,
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

const METRICS = Object.keys(HISTORY_METRICS) as HistoryMetric[];

/** Returns the metric values of a point without the metrics it did not record. */
const pointValues = ({ rps, memory, cpu }: HistoryValues): HistoryValues => ({
  rps,
  ...(memory !== undefined ? { memory } : {}),
  ...(cpu !== undefined ? { cpu } : {}),
});

/** The metric history of a scenario with every variant of `scenario`, each metric scaled to the released series. The frame hides the series that the filters exclude. */
export function BenchmarkHistoryChart({
  id,
  scenario,
  variants,
}: BenchmarkHistoryChartProps) {
  const series = historySeries(scenario, variants);
  if (series.length === 0) {
    return null;
  }
  const released = series.filter((item) => !item.preview);
  const values = (items: readonly HistorySeries[], metric: HistoryMetric) =>
    items.flatMap((item) =>
      item.points.flatMap((point) => point[metric] ?? []),
    );
  const domains = Object.fromEntries(
    METRICS.flatMap((metric) => {
      const recorded = values(series, metric);
      if (recorded.length === 0) {
        return [];
      }
      const releasedValues = values(released, metric);
      const { ticks } = historyScale(
        releasedValues.length > 0 ? releasedValues : recorded,
      );
      return [[metric, [ticks[0], ticks.at(-1)!] as const]];
    }),
  );
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
      series={series.map((item) => ({
        id: item.id,
        name: item.name,
        runtime: item.runtime,
        composition: item.composition,
        technology: benchmarkTechnology(variants.get(item.id)!),
        preview: item.preview,
        color: item.color,
        dashed: item.dashed,
        newest: pointValues(latestCompletedPoint(item) ?? item.points.at(-1)!),
        changes: Object.fromEntries(
          METRICS.flatMap((metric) => {
            const change = seriesChange(item, metric);
            return change ? [[metric, change]] : [];
          }),
        ),
        ...(item.points.at(-1)!.failed ? { latestFailed: true } : {}),
        points: item.points.map((point) => ({
          date: dateIndex.get(point.timestamp)!,
          ...pointValues(point),
          version: point.version,
          ...(point.runtime !== item.runtime ? { runtime: point.runtime } : {}),
          ...(point.failed ? { failure: failureNote(point) } : {}),
        })),
      }))}
      domains={domains}
      dates={dates}
    />
  );
}
