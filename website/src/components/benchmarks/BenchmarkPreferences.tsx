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

const Preferences = createContext<{
  readonly filters: BenchmarkFilters;
  readonly setFilters: Dispatch<SetStateAction<BenchmarkFilters>>;
  readonly pinnedSeries: string | undefined;
  readonly setPinnedSeries: Dispatch<SetStateAction<string | undefined>>;
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
  const [pinnedSeries, setPinnedSeries] = useState<string>();
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
