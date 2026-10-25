import Link from "next/link";
import type { ReactNode } from "react";
import { BenchmarkLoadShape } from "@/src/components/benchmarks/BenchmarkLoadShape";
import { BENCHMARK_SCENARIO_PATHS } from "@/src/components/benchmarks/benchmarkRoutes";
import {
  SCENARIO_INFO,
  SCENARIO_LOAD,
} from "@/src/components/benchmarks/benchmarkView";
import type { BenchmarkControls } from "@/src/components/benchmarks/useBenchmarkControls";
import type { BenchmarkScenario } from "@/src/helpers/benchmarkReport";

interface BenchmarkScenarioTilesProps {
  readonly controls: BenchmarkControls;
  readonly className?: string;
  /** The content below each load shape, the load description when undefined. */
  readonly detail?: (scenario: BenchmarkScenario) => ReactNode;
}

/** The scenario links with their load shapes. The sticky scenario bar replaces them once they scroll out of view. */
export function BenchmarkScenarioTiles({
  controls,
  className = "",
  detail,
}: BenchmarkScenarioTilesProps) {
  const { scenarios, selected } = controls;
  return (
    <fieldset
      className={`border-cc-card-border grid border-y md:grid-cols-3 ${className}`}
      data-benchmark-controls
      data-benchmark-scenario-tiles
    >
      <legend className="sr-only">Scenario</legend>
      {scenarios.map((scenario) => {
        const info = SCENARIO_INFO[scenario.id];
        return (
          <Link
            key={scenario.id}
            href={BENCHMARK_SCENARIO_PATHS[scenario.id]}
            scroll={false}
            aria-label={info.name}
            aria-current={selected === scenario.id ? "page" : undefined}
            aria-controls={`benchmark-panel-${scenario.id}`}
            className="border-cc-card-border hover:bg-cc-white/2 focus-visible:bg-cc-white/5 focus-visible:outline-cc-accent flex cursor-pointer border-b text-left last:border-b-0 focus-visible:outline-2 focus-visible:-outline-offset-2 md:border-r md:border-b-0 md:last:border-r-0"
          >
            <span
              className={`text-cc-ink-dim flex w-full flex-col px-5 py-5 transition-colors sm:py-6 ${selected === scenario.id ? "bg-cc-white/5 text-cc-heading shadow-[inset_4px_0_0_0_var(--color-cc-heading)] md:shadow-[inset_0_-4px_0_0_var(--color-cc-heading)]" : ""}`}
            >
              <span className="font-heading text-h5 text-cc-heading font-semibold">
                {info.name}
              </span>
              <BenchmarkLoadShape scenario={scenario} className="mt-4" />
              {detail ? (
                detail(scenario)
              ) : (
                <span className="text-caption text-cc-ink-dim mt-3 block">
                  {SCENARIO_LOAD[scenario.id]}
                </span>
              )}
            </span>
          </Link>
        );
      })}
    </fieldset>
  );
}
