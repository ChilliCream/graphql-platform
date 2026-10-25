"use client";

import { BenchmarkGlance } from "@/src/components/benchmarks/BenchmarkGlance";
import { SECTION_HEADING } from "@/src/components/benchmarks/benchmarkHeadings";
import { BenchmarkScenarioPanels } from "@/src/components/benchmarks/BenchmarkScenarioPanels";
import { BenchmarkScenarioTiles } from "@/src/components/benchmarks/BenchmarkScenarioTiles";
import { BenchmarkStickyScenarioBar } from "@/src/components/benchmarks/BenchmarkStickyScenarioBar";
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

/** The results summary per composition, the scenario tiles with their sticky bar, and the selected scenario's tables and history chart. */
export function BenchmarkExplorer({
  report,
  history,
  initialScenario,
}: BenchmarkExplorerProps) {
  const controls = useBenchmarkControls(report, history, initialScenario);

  return (
    <div className="relative [overflow-anchor:none]" data-benchmark-explorer>
      <BenchmarkStickyScenarioBar controls={controls} />
      <BenchmarkGlance
        report={controls.summaryReport}
        variants={controls.variants}
        visibleRows={controls.visibleRows}
      />
      <section
        aria-labelledby="benchmark-scenarios-heading"
        className="mt-16 sm:mt-20"
      >
        <h2 id="benchmark-scenarios-heading" className={SECTION_HEADING}>
          Results by scenario
        </h2>
        <BenchmarkScenarioTiles controls={controls} className="mt-6" />
        <BenchmarkScenarioPanels controls={controls} />
      </section>
    </div>
  );
}
