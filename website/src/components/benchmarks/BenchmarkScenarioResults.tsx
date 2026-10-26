import type { ReactNode } from "react";
import { useBenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";
import { BenchmarkResultsTable } from "@/src/components/benchmarks/BenchmarkResultsTable";
import {
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
  /** A control shown at the end of the first table's heading row. */
  readonly action: ReactNode;
}

/** The GraphQL Federation specification and Apollo Federation tables of one scenario, with single-run notices. */
export function BenchmarkScenarioResults({
  report,
  scenario,
  variants,
  action,
}: BenchmarkScenarioResultsProps) {
  const groups = scenarioGroups(scenario, variants);
  const { pinnedSeries } = useBenchmarkPreferences();
  const selected = groups.some((group) =>
    group.rows.some((row) => row.variant === pinnedSeries),
  )
    ? pinnedSeries
    : undefined;
  const { config } = scenario;

  if (groups.length === 0) {
    return (
      <div className="border-cc-card-border border-y py-10">
        <p className="text-cc-heading font-semibold">
          No results were reported.
        </p>
        <p className="mt-2 max-w-2xl">
          No throughput values from an older run have been substituted.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-12">
      {groups.map((group, index) => (
        <BenchmarkResultsTable
          key={group.id}
          id={`${scenario.id}-${group.id}`}
          title={group.name}
          caption={tableCaption(scenario, group, report)}
          group={group}
          variants={variants}
          selected={selected}
          measuredRuns={config.measuredRuns}
          gatewayThreads={config.cpuAllocation?.gateway?.hardwareThreads}
          throughputNote={throughputNote(scenario)}
          action={index === 0 ? action : undefined}
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
