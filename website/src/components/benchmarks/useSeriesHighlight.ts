import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";

/** The pinned series among `seriesIds`, with functions that pin a series, remove the pin, and toggle the pin of a series. A pinned series stays pinned across scenarios that contain it. */
export function useSeriesHighlight(seriesIds: readonly string[]) {
  const { pinnedSeries, setPinnedSeries } = useBenchmarkPreferences();
  return {
    pinned:
      pinnedSeries && seriesIds.includes(pinnedSeries)
        ? pinnedSeries
        : undefined,
    pin: (id: string) => setPinnedSeries(id),
    unpin: () => setPinnedSeries(undefined),
    togglePinned: (id: string) =>
      setPinnedSeries((current) => (current === id ? undefined : id)),
  };
}
