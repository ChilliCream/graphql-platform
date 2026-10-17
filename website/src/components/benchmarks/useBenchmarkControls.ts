import { useState } from "react";
import {
  defaultScenarioId,
  orderedScenarios,
} from "@/src/components/benchmarks/benchmarkView";
import {
  benchmarkVariants,
  type BenchmarkHistory,
  type BenchmarkReport,
  type ScenarioId,
} from "@/src/helpers/benchmarkReport";

/** The scenario selection shared by the benchmark designs. Results show stable versions only. */
export function useBenchmarkControls(
  report: BenchmarkReport,
  history: BenchmarkHistory | undefined,
) {
  const scenarios = orderedScenarios(report);
  const [selected, setSelected] = useState<ScenarioId | undefined>(() =>
    defaultScenarioId(scenarios),
  );
  return {
    scenarios,
    variants: benchmarkVariants(report),
    historyVariants: history && benchmarkVariants(history),
    historyOf: (id: ScenarioId) =>
      history?.scenarios.find((scenario) => scenario.id === id),
    selected,
    setSelected,
    stableOnly: true,
  };
}
