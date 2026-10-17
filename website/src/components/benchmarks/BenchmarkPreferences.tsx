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
} | null>(null);

interface BenchmarkPreferencesProps {
  readonly children: ReactNode;
}

export function BenchmarkPreferences({ children }: BenchmarkPreferencesProps) {
  const [filters, setFilters] = useState<BenchmarkFilters>({
    technologies: [],
    showPreviews: true,
  });
  return (
    <Preferences.Provider value={{ filters, setFilters }}>
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
