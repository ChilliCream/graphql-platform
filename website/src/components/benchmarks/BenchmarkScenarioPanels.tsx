import { Fragment, type ReactNode } from "react";
import { BenchmarkFilterButton } from "@/src/components/benchmarks/BenchmarkFilterButton";
import { BenchmarkNoscript } from "@/src/components/benchmarks/BenchmarkNoscript";
import { BenchmarkScenarioResults } from "@/src/components/benchmarks/BenchmarkScenarioResults";
import { SCENARIO_INFO } from "@/src/components/benchmarks/benchmarkView";
import type { BenchmarkControls } from "@/src/components/benchmarks/useBenchmarkControls";
import type { ScenarioId } from "@/src/helpers/benchmarkReport";

interface BenchmarkScenarioPanelsProps {
  readonly controls: BenchmarkControls;
  /** The history chart of each scenario that has recorded runs. */
  readonly charts: Partial<Record<ScenarioId, ReactNode>>;
}

/** The results tables and history chart of the selected scenario. Every scenario is rendered, and only the selected one is shown. */
export function BenchmarkScenarioPanels({
  controls,
  charts,
}: BenchmarkScenarioPanelsProps) {
  const { report, scenarios, variants } = controls;

  return (
    <>
      <BenchmarkNoscript />
      {!controls.hasResults && (
        <div
          className="border-cc-card-border mt-10 rounded-lg border px-5 py-8"
          role="status"
        >
          <p className="text-cc-heading font-semibold">
            No results match these filters
          </p>
          <p className="text-cc-ink-dim mt-2">
            Choose another technology or include preview releases.
          </p>
        </div>
      )}
      {scenarios.length === 0 && (
        <p className="text-cc-heading mt-10 font-semibold">
          This report contains no scenarios.
        </p>
      )}
      {controls.hasResults &&
        scenarios.map((scenario) => {
          const chart = charts[scenario.id];
          const name = SCENARIO_INFO[scenario.id].name;
          return (
            <section
              key={scenario.id}
              id={`benchmark-panel-${scenario.id}`}
              data-benchmark-panel
              aria-label={`${name} results`}
              className={
                controls.selected === scenario.id ? "mt-12" : "mt-12 hidden"
              }
            >
              <BenchmarkScenarioResults
                report={report}
                scenario={scenario}
                variants={variants}
                action={
                  <div data-benchmark-inline-filter>
                    <BenchmarkFilterButton
                      controls={controls}
                      className="border-cc-card-border hover:border-cc-heading/40 size-9 rounded-md border"
                    />
                  </div>
                }
              />
              {chart ? (
                <div className="mt-16">
                  <Fragment
                    key={`${controls.filters.showPreviews}:${controls.filters.technologies.join(",")}`}
                  >
                    {chart}
                  </Fragment>
                </div>
              ) : (
                <p className="text-caption text-cc-ink-dim mt-12">
                  No earlier runs of the {name.toLowerCase()} scenario are
                  recorded yet, so there is no version history to chart.
                </p>
              )}
            </section>
          );
        })}
    </>
  );
}
