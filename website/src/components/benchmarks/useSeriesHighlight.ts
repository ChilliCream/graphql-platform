import { useMemo } from "react";
import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";

/** Returns `pinned` without `id` when it contains `id`, and with `id` appended otherwise. */
export const togglePin = (pinned: readonly string[], id: string) =>
  pinned.includes(id) ? pinned.filter((item) => item !== id) : [...pinned, id];

/** The pinned series among `seriesIds`, with functions that pin a series, remove every pin, and toggle the pin of a series. Pinned series stay pinned across scenarios that contain them. */
export function useSeriesHighlight(seriesIds: readonly string[]) {
  const { pinnedSeries, setPinnedSeries } = useBenchmarkPreferences();
  const pinned = useMemo(
    () => pinnedSeries.filter((id) => seriesIds.includes(id)),
    [pinnedSeries, seriesIds],
  );
  return {
    pinned,
    pin: (id: string) =>
      setPinnedSeries((current) =>
        current.includes(id) ? current : [...current, id],
      ),
    unpin: () => setPinnedSeries([]),
    togglePinned: (id: string) =>
      setPinnedSeries((current) => togglePin(current, id)),
  };
}
