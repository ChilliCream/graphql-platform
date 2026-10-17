import { BenchmarkResultsTable } from "@/src/components/benchmarks/BenchmarkResultsTable";
import {
  SCENARIO_STATUSES,
  scenarioGroups,
  throughputNote,
  tableCaption,
} from "@/src/components/benchmarks/benchmarkView";
import type {
  BenchmarkCatalogEntry,
  BenchmarkReport,
  BenchmarkScenario,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkScenarioResultsProps {
  readonly report: BenchmarkReport;
  readonly scenario: BenchmarkScenario;
  readonly variants: ReadonlyMap<string, BenchmarkCatalogEntry>;
  readonly stableOnly: boolean;
}

/** The Composite Schema and Apollo Federation tables of one scenario, with its status and single-run notices. */
export function BenchmarkScenarioResults({
  report,
  scenario,
  variants,
  stableOnly,
}: BenchmarkScenarioResultsProps) {
  const groups = scenarioGroups(scenario, variants, stableOnly);
  const status = SCENARIO_STATUSES[scenario.status];
  const { config } = scenario;

  if (groups.length === 0) {
    return (
      <div className="border-cc-card-border border-y py-10">
        <p className="text-cc-heading font-semibold">{status.empty}</p>
        <p className="mt-2 max-w-2xl">
          No throughput values from an older run have been substituted.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-12">
      {scenario.status !== "complete" && (
        <p className="text-caption text-cc-warning">
          This scenario is marked {status.label.toLowerCase()} in this run. The
          results below may be incomplete.
        </p>
      )}
      {groups.map((group) => (
        <BenchmarkResultsTable
          key={group.id}
          id={`${scenario.id}-${group.id}`}
          title={group.name}
          caption={tableCaption(scenario, group, report)}
          group={group}
          variants={variants}
          measuredRuns={config.measuredRuns}
          gatewayCores={config.cpuAllocation?.gateway?.cores}
          throughputNote={throughputNote(scenario)}
        />
      ))}
      {config.measuredRuns === 1 && (
        <p className="text-caption text-cc-warning">
          Only one measured run is available. Run-to-run variation cannot be
          estimated. Small differences do not establish a reliable ranking.
        </p>
      )}
    </div>
  );
}
