import { useLayoutEffect, useRef } from "react";
import {
  defaultScenarioId,
  isShownVariant,
  orderedScenarios,
  scenarioGroups,
} from "@/src/components/benchmarks/benchmarkView";
import {
  benchmarkTechnology,
  matchesBenchmarkTechnology,
  type BenchmarkFilters,
} from "@/src/components/benchmarks/benchmarkFilters";
import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";
import {
  benchmarkVariants,
  isPreview,
  type BenchmarkCatalogEntry,
  type BenchmarkHistory,
  type BenchmarkReport,
  type ScenarioId,
} from "@/src/helpers/benchmarkReport";

/** In-memory filters and the scenario selected by the static route. */
export function useBenchmarkControls(
  report: BenchmarkReport,
  history: BenchmarkHistory | undefined,
  initialScenario: ScenarioId,
) {
  const { filters, setFilters: updateFilters } = useBenchmarkPreferences();
  const scrollPosition = useRef<{
    readonly y: number;
    readonly anchors: readonly { readonly key: string; readonly top: number }[];
  }>(undefined);
  useLayoutEffect(() => {
    const position = scrollPosition.current;
    if (!position) {
      return;
    }
    scrollPosition.current = undefined;
    for (const anchor of position.anchors) {
      const elements = document.querySelectorAll<HTMLElement>(
        "[data-benchmark-scroll-anchor]",
      );
      const element = [...elements].find(
        (element) =>
          element.dataset.benchmarkScrollAnchor === anchor.key &&
          element.getBoundingClientRect().width > 0,
      );
      if (element) {
        window.scrollBy({
          top: element.getBoundingClientRect().top - anchor.top,
          behavior: "instant",
        });
        return;
      }
    }
    window.scrollTo({ top: position.y, behavior: "instant" });
  }, [filters]);
  const variants = benchmarkVariants(report);
  const historyVariants = history && benchmarkVariants(history);
  const technologies = [
    ...new Set(
      [...variants.values(), ...(historyVariants?.values() ?? [])].map(
        benchmarkTechnology,
      ),
    ),
  ].sort();
  const shown = (keep: (entry: BenchmarkCatalogEntry) => boolean) => ({
    ...report,
    scenarios: report.scenarios.map((scenario) => ({
      ...scenario,
      results: scenario.results.map((result) => ({
        ...result,
        variants: result.variants.filter((row) =>
          keep(variants.get(row.variant)!),
        ),
      })),
    })),
  });
  const released = (entry: BenchmarkCatalogEntry) =>
    isShownVariant(entry) &&
    (filters.showPreviews || !isPreview(entry.variant));
  const summaryReport = shown(released);
  const filteredReport = shown(
    (entry) =>
      released(entry) &&
      matchesBenchmarkTechnology(entry, filters.technologies),
  );
  const scenarios = orderedScenarios(filteredReport);
  const selected =
    scenarios.find((scenario) => scenario.id === initialScenario)?.id ??
    defaultScenarioId(scenarios);
  const setFilters = (next: BenchmarkFilters) => {
    const anchors = [
      ...document.querySelectorAll<HTMLElement>(
        "[data-benchmark-scroll-anchor]",
      ),
    ]
      .flatMap((element) => {
        const rect = element.getBoundingClientRect();
        return rect.width > 0 &&
          rect.bottom > 80 &&
          rect.top < window.innerHeight
          ? [{ key: element.dataset.benchmarkScrollAnchor!, top: rect.top }]
          : [];
      })
      .sort((a, b) => Math.abs(a.top - 80) - Math.abs(b.top - 80));
    scrollPosition.current = { y: window.scrollY, anchors };
    updateFilters(next);
  };
  return {
    scenarios,
    /** The report behind the results summary, which follows the preview filter but not the technology filter. */
    summaryReport,
    report: filteredReport,
    variants,
    historyVariants,
    technologies,
    filters,
    setFilters,
    historyOf: (id: ScenarioId) => {
      const scenario = history?.scenarios.find(
        (scenario) => scenario.id === id,
      );
      return (
        scenario && {
          ...scenario,
          variants: scenario.variants.filter((item) => {
            const entry = historyVariants?.get(item.variant);
            return (
              entry &&
              matchesBenchmarkTechnology(entry, filters.technologies) &&
              released(entry)
            );
          }),
        }
      );
    },
    selected,
    /** The variants shown in the tables of each scenario. */
    visibleRows: new Map(
      scenarios.map((scenario) => [
        scenario.id,
        new Set(
          scenarioGroups(scenario, variants).flatMap((group) =>
            group.rows.map((row) => row.variant),
          ),
        ),
      ]),
    ),
    hasResults: scenarios.some(
      (scenario) => scenarioGroups(scenario, variants).length > 0,
    ),
  };
}

export type BenchmarkControls = ReturnType<typeof useBenchmarkControls>;
