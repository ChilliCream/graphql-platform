"use client";

import Link from "next/link";
import { BENCHMARK_SCENARIO_PATHS } from "@/src/components/benchmarks/benchmarkRoutes";

import { BenchmarkFilterBar } from "@/src/components/benchmarks/BenchmarkFilterBar";
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
  ScenarioId,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkExplorerProps {
  readonly report: BenchmarkReport;
  readonly history: BenchmarkHistory;
  readonly initialScenario: ScenarioId;
}

/** The version filter, the results summary, the scenario tiles with their load shapes, and the selected scenario's tables and history chart. */
export function BenchmarkExplorer({
  report,
  history,
  initialScenario,
}: BenchmarkExplorerProps) {
  const controls = useBenchmarkControls(report, history, initialScenario);
  const { scenarios, variants, historyVariants, stableOnly } = controls;

  return (
    <div className="relative [overflow-anchor:none]">
      <div className="pointer-events-none sticky top-20 z-30 flex h-0 justify-end">
        <BenchmarkFilterBar
          technologies={controls.technologies}
          filters={controls.filters}
          onChange={controls.setFilters}
        />
      </div>
      <h2
        id="benchmark-results-heading"
        className="text-h6 text-cc-heading pr-14 font-semibold"
      >
        Results at a glance
      </h2>
      <BenchmarkVerdictSummary
        id="benchmark-results"
        report={report}
        stableOnly={stableOnly}
      />
      <fieldset
        className="border-cc-card-border mt-10 grid border-y sm:mt-14 md:grid-cols-3"
        data-benchmark-controls
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
              aria-current={
                controls.selected === scenario.id ? "page" : undefined
              }
              aria-controls={`benchmark-panel-${scenario.id}`}
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
            </Link>
          );
        })}
      </fieldset>
      <BenchmarkNoscript />
      {!controls.hasResults && (
        <div
          className="border-cc-card-border rounded-lg border px-5 py-8"
          role="status"
        >
          <h2 className="text-h6 text-cc-heading font-semibold">
            No results match these filters
          </h2>
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
                report={controls.report}
                scenario={scenario}
                variants={variants}
                stableOnly={stableOnly}
              />
              {scenarioHistory && historyVariants ? (
                <div className="mt-16">
                  <BenchmarkHistoryChart
                    key={`${stableOnly}:${controls.filters.technologies.join(",")}`}
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
    </div>
  );
}
