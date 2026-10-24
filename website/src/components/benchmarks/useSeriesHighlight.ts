import { useEffect, useRef, useState } from "react";
import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";

/** A pinned series takes precedence over pointer and keyboard highlights and stays pinned across scenarios that contain it. */
export function useSeriesHighlight(seriesIds: readonly string[]) {
  const [hovered, setHovered] = useState<string>();
  const [focused, setFocused] = useState<string>();
  const { pinnedSeries, setPinnedSeries: setPinned } =
    useBenchmarkPreferences();
  const pinned =
    pinnedSeries && seriesIds.includes(pinnedSeries) ? pinnedSeries : undefined;
  const hoverTimeout = useRef<ReturnType<typeof setTimeout>>(undefined);
  useEffect(() => () => clearTimeout(hoverTimeout.current), []);
  return {
    active: pinned ?? hovered ?? focused,
    pinned,
    setHovered: (id: string | undefined) => {
      clearTimeout(hoverTimeout.current);
      if (!pinned) {
        setHovered(id);
      }
    },
    setFocused,
    clearHoverSoon: () => {
      clearTimeout(hoverTimeout.current);
      hoverTimeout.current = setTimeout(() => setHovered(undefined), 100);
    },
    clearPointerHighlight: () => {
      clearTimeout(hoverTimeout.current);
      setHovered(undefined);
    },
    togglePinned: (id: string) => {
      clearTimeout(hoverTimeout.current);
      setHovered(undefined);
      setFocused(undefined);
      setPinned((current) => (current === id ? undefined : id));
    },
  };
}
