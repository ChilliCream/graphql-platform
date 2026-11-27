"use client";

import {
  createContext,
  useContext,
  useState,
  type Dispatch,
  type SetStateAction,
  type ReactNode,
} from "react";
import type { BenchmarkFilters } from "@/src/components/benchmarks/benchmarkFilters";
import type { HistoryMetric } from "@/src/components/benchmarks/benchmarkView";

const Preferences = createContext<{
  readonly filters: BenchmarkFilters;
  readonly setFilters: Dispatch<SetStateAction<BenchmarkFilters>>;
  readonly pinnedSeries: readonly string[];
  readonly setPinnedSeries: Dispatch<SetStateAction<readonly string[]>>;
  /** The metric that the history charts plot. */
  readonly historyMetric: HistoryMetric;
  readonly setHistoryMetric: Dispatch<SetStateAction<HistoryMetric>>;
  /** Whether the sticky scenario bar is shown, and whether it shows its filter button. */
  readonly scenarioBar: { readonly visible: boolean; readonly filter: boolean };
  readonly setScenarioBar: Dispatch<
    SetStateAction<{ readonly visible: boolean; readonly filter: boolean }>
  >;
} | null>(null);

interface BenchmarkPreferencesProps {
  readonly children: ReactNode;
}

export function BenchmarkPreferences({ children }: BenchmarkPreferencesProps) {
  const [filters, setFilters] = useState<BenchmarkFilters>({
    technologies: [],
    showPreviews: false,
  });
  const [pinnedSeries, setPinnedSeries] = useState<readonly string[]>([]);
  const [historyMetric, setHistoryMetric] = useState<HistoryMetric>("rps");
  const [scenarioBar, setScenarioBar] = useState({
    visible: false,
    filter: false,
  });
  return (
    <Preferences.Provider
      value={{
        filters,
        setFilters,
        pinnedSeries,
        setPinnedSeries,
        historyMetric,
        setHistoryMetric,
        scenarioBar,
        setScenarioBar,
      }}
    >
      {children}
    </Preferences.Provider>
  );
}

export function useBenchmarkPreferences() {
  const preferences = useContext(Preferences);
  if (!preferences) {
    throw new Error(
      "Benchmark preferences require a BenchmarkPreferences provider.",
    );
  }
  return preferences;
}
