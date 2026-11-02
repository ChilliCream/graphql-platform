import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";

/** The pinned series among `seriesIds`, with functions that pin a series and toggle its pin. A pinned series stays pinned across scenarios that contain it. */
export function useSeriesHighlight(seriesIds: readonly string[]) {
  const { pinnedSeries, setPinnedSeries } = useBenchmarkPreferences();
  return {
    pinned:
      pinnedSeries && seriesIds.includes(pinnedSeries)
        ? pinnedSeries
        : undefined,
    pin: (id: string) => setPinnedSeries(id),
    togglePinned: (id: string) =>
      setPinnedSeries((current) => (current === id ? undefined : id)),
  };
}
