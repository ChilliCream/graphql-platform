"use client";

import { BenchmarkHistoryChart } from "@/src/components/benchmarks/BenchmarkHistoryChart";
import { BenchmarkLoadShape } from "@/src/components/benchmarks/BenchmarkLoadShape";
import { BenchmarkNoscript } from "@/src/components/benchmarks/BenchmarkNoscript";
import { BenchmarkScenarioResults } from "@/src/components/benchmarks/BenchmarkScenarioResults";
import { BenchmarkVerdictSummary } from "@/src/components/benchmarks/BenchmarkVerdict";
import {
  SCENARIO_INFO,
  SCENARIO_LOAD,
} from "@/src/components/benchmarks/benchmarkView";
import { useBenchmarkControls } from "@/src/components/benchmarks/useBenchmarkControls";
import type {
  BenchmarkHistory,
  BenchmarkReport,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkExplorerProps {
  readonly report: BenchmarkReport;
  readonly history: BenchmarkHistory;
}

/** The version filter, the results summary, the scenario tiles with their load shapes, and the selected scenario's tables and history chart. */
export function BenchmarkExplorer({ report, history }: BenchmarkExplorerProps) {
  const controls = useBenchmarkControls(report, history);
  const { scenarios, variants, historyVariants, stableOnly } = controls;

  return (
    <>
      <BenchmarkVerdictSummary
        id="benchmark-results"
        report={report}
        stableOnly={stableOnly}
        controls={
          <label
            data-benchmark-controls
            className="text-cc-heading flex shrink-0 cursor-pointer items-center gap-2 text-sm"
          >
            <input
              type="checkbox"
              name="benchmark-previews"
              checked={controls.showPreviews}
              onChange={(event) =>
                controls.setShowPreviews(event.target.checked)
              }
              className="accent-cc-accent focus-visible:outline-cc-accent size-4 cursor-pointer rounded focus-visible:outline-2"
            />
            Include previews
          </label>
        }
      />
      <fieldset
        className="border-cc-card-border mt-10 grid border-y sm:mt-14 md:grid-cols-3"
        data-benchmark-controls
      >
        <legend className="sr-only">Scenario</legend>
        {scenarios.map((scenario) => {
          const info = SCENARIO_INFO[scenario.id];
          return (
            <button
              key={scenario.id}
              type="button"
              aria-label={info.name}
              aria-pressed={controls.selected === scenario.id}
              aria-controls={`benchmark-panel-${scenario.id}`}
              onClick={() => controls.setSelected(scenario.id)}
              className="border-cc-card-border hover:bg-cc-white/2 focus-visible:bg-cc-white/5 focus-visible:outline-cc-accent flex cursor-pointer border-b text-left last:border-b-0 focus-visible:outline-2 focus-visible:-outline-offset-2 md:border-r md:border-b-0 md:last:border-r-0"
            >
              <span
                className={`text-cc-ink-dim flex w-full flex-col px-5 py-5 transition-colors sm:py-6 ${controls.selected === scenario.id ? "bg-cc-white/5 text-cc-heading shadow-[inset_4px_0_0_0_var(--color-cc-heading)] md:shadow-[inset_0_-4px_0_0_var(--color-cc-heading)]" : ""}`}
              >
                <span className="font-heading text-h5 sm:text-h4 text-cc-heading font-semibold">
                  {info.name}
                </span>
                <BenchmarkLoadShape scenario={scenario} className="mt-4" />
                <span className="text-caption text-cc-ink-dim mt-3 block">
                  {SCENARIO_LOAD[scenario.id]}
                </span>
              </span>
            </button>
          );
        })}
      </fieldset>
      <BenchmarkNoscript />
      {scenarios.length === 0 && (
        <p className="text-cc-heading mt-10 font-semibold">
          This report contains no scenarios.
        </p>
      )}
      {scenarios.map((scenario) => {
        const scenarioHistory = controls.historyOf(scenario.id);
        const name = SCENARIO_INFO[scenario.id].name;
        return (
          <section
            key={scenario.id}
            id={`benchmark-panel-${scenario.id}`}
            data-benchmark-panel
            aria-labelledby={`benchmark-heading-${scenario.id}`}
            className={
              controls.selected === scenario.id ? "mt-12" : "mt-12 hidden"
            }
          >
            <h2 id={`benchmark-heading-${scenario.id}`} className="sr-only">
              {name} results
            </h2>
            <BenchmarkScenarioResults
              report={report}
              scenario={scenario}
              variants={variants}
              stableOnly={stableOnly}
            />
            {scenarioHistory && historyVariants ? (
              <div className="mt-16">
                <BenchmarkHistoryChart
                  id={`benchmark-${scenario.id}`}
                  scenario={scenarioHistory}
                  variants={historyVariants}
                  stableOnly={stableOnly}
                />
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
