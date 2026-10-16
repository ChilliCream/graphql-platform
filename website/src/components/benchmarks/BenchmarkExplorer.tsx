"use client";

import Link from "next/link";
import { useState } from "react";
import {
  benchmarkVariants,
  formatRuntimeTag,
  gatewayLink,
  variantName,
  type BenchmarkCatalogEntry,
  type BenchmarkReport,
  type BenchmarkScenario,
  type BenchmarkVariantResult,
  type Composition,
  type ScenarioId,
  type ScenarioStatus,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkExplorerProps {
  readonly report: BenchmarkReport;
}

const SCENARIOS = {
  "constant-latency": {
    name: "Real-world",
    hint: "With downstream I/O",
  },
  burst: {
    name: "Burst traffic",
    hint: "Rising concurrency",
  },
  constant: {
    name: "Synthetic",
    hint: "Without added I/O",
  },
} as const;

const SCENARIO_ORDER = Object.keys(SCENARIOS);

const COMPOSITION_GROUPS = [
  { id: "composite-schema", name: "Composite Schema" },
  { id: "apollo-federation", name: "Apollo Federation" },
] as const satisfies readonly { id: Composition; name: string }[];

const STATUSES: Readonly<
  Record<ScenarioStatus, { readonly label: string; readonly empty: string }>
> = {
  pending: { label: "Pending", empty: "Results are not available yet." },
  "in-progress": {
    label: "In progress",
    empty: "This scenario is still running.",
  },
  complete: { label: "Complete", empty: "No results were reported." },
  skipped: {
    label: "Skipped",
    empty: "This scenario was skipped in this run.",
  },
  failed: { label: "Failed", empty: "This scenario failed in this run." },
};

const plural = (count: number, noun: string) =>
  `${count} ${noun}${count === 1 ? "" : "s"}`;

const describeConditions = (scenario: BenchmarkScenario) => {
  const { config } = scenario;
  const allocation = config.cpuAllocation;
  return [
    config.virtualUsers !== undefined &&
      `${config.virtualUsers} ${scenario.id === "burst" ? "peak virtual users" : "virtual users"}`,
    config.durationSeconds !== undefined && `${config.durationSeconds}s`,
    `${config.subgraphDelayMs}ms added subgraph I/O`,
    allocation?.gateway && `${allocation.gateway.cores} cores for the gateway`,
    allocation?.subgraphs &&
      `${allocation.subgraphs.cores} cores for subgraphs`,
    allocation?.k6 && `${allocation.k6.cores} cores for k6`,
    config.measuredRuns !== undefined &&
      plural(config.measuredRuns, "measured run"),
    plural(config.warmupRuns, "discarded warmup run"),
  ]
    .filter(Boolean)
    .join(" · ");
};

const formatNumber = (value: number) =>
  new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(value);

const formatMemory = (kibibytes: number) =>
  new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 0,
  }).format(kibibytes / 1024);

const formatLatency = (milliseconds: number) =>
  new Intl.NumberFormat("en-US", {
    minimumFractionDigits: milliseconds < 100 ? 1 : 0,
    maximumFractionDigits: milliseconds < 100 ? 1 : 0,
  }).format(milliseconds);

const formatReliability = (successful: number, total: number) => {
  if (total === 0) {
    return "—";
  }
  if (successful === total) {
    return "100%";
  }
  return `${(Math.floor((successful / total) * 10_000) / 100).toFixed(2)}%`;
};

const NOT_REPORTED = (
  <span className="text-cc-ink-dim font-sans" title="Not reported">
    —
  </span>
);

const TAG =
  "border-cc-card-border text-cc-ink-dim rounded border px-1.5 font-mono text-[11px] leading-5 whitespace-nowrap";

const throughputOf = (row: BenchmarkVariantResult) => row.throughput.medianRps;

const isReliable = (row: BenchmarkVariantResult) =>
  row.requests.total > 0 && row.requests.successful === row.requests.total;

const winningThroughputOf = (row: BenchmarkVariantResult) =>
  isReliable(row) ? row.throughput.medianRps : -1;

export function BenchmarkExplorer({ report }: BenchmarkExplorerProps) {
  const variants = benchmarkVariants(report);
  const scenarios = report.scenarios.toSorted(
    (a, b) => SCENARIO_ORDER.indexOf(a.id) - SCENARIO_ORDER.indexOf(b.id),
  );
  const [selected, setSelected] = useState<ScenarioId | undefined>(
    () =>
      scenarios.find((scenario) => scenario.id === "constant-latency")?.id ??
      scenarios[0]?.id,
  );

  return (
    <section
      id="results"
      className="scroll-mt-28 pb-16"
      aria-labelledby="results-heading"
    >
      <div className="mb-7">
        <h2
          id="results-heading"
          className="font-heading text-h4 text-cc-heading font-semibold"
        >
          Compare the results.
        </h2>
        <p className="mt-2">
          Median throughput. Higher is better. Versions are the builds measured
          in this run.
        </p>
      </div>
      <fieldset
        className="mb-6 grid gap-2 sm:grid-cols-3"
        data-benchmark-controls
      >
        <legend className="sr-only">Benchmark scenario</legend>
        {scenarios.map((scenario, index) => (
          <label key={scenario.id} className="cursor-pointer">
            <input
              className="peer sr-only"
              type="radio"
              name="benchmark-scenario"
              value={scenario.id}
              checked={selected === scenario.id}
              onChange={() => setSelected(scenario.id)}
              aria-controls={`panel-${scenario.id}`}
            />
            <span className="border-cc-card-border bg-cc-surface/50 peer-checked:border-cc-accent peer-checked:bg-cc-accent/8 peer-focus-visible:outline-cc-accent flex h-full items-center gap-4 rounded-xl border p-4 transition-colors peer-focus-visible:outline-2 peer-focus-visible:outline-offset-4">
              <span className="text-cc-accent font-mono text-xs">
                0{index + 1}
              </span>
              <span>
                <span className="text-cc-heading block font-semibold">
                  {SCENARIOS[scenario.id].name}
                </span>
                <span className="text-caption text-cc-ink-dim">
                  {SCENARIOS[scenario.id].hint}
                </span>
              </span>
              {scenario.status !== "complete" && (
                <span className="text-cc-warning ml-auto text-xs">
                  {STATUSES[scenario.status].label}
                </span>
              )}
            </span>
          </label>
        ))}
      </fieldset>
      {scenarios.length === 0 && (
        <p className="border-cc-card-border bg-cc-surface/60 text-cc-heading rounded-2xl border p-8 font-semibold sm:p-12">
          This report contains no scenarios.
        </p>
      )}
      <noscript>
        <style>{`[data-benchmark-panel] { display: block; margin-top: 2rem; } [data-benchmark-controls] { display: none; }`}</style>
      </noscript>
      {scenarios.map((scenario) => {
        const info = SCENARIOS[scenario.id];
        const { config } = scenario;
        const rows = scenario.results
          .flatMap((result) => result.variants)
          .toSorted(
            (a, b) =>
              throughputOf(b) - throughputOf(a) ||
              a.variant.localeCompare(b.variant),
          );
        return (
          <section
            key={scenario.id}
            id={`panel-${scenario.id}`}
            data-benchmark-panel
            className={selected === scenario.id ? undefined : "hidden"}
            aria-labelledby={`heading-${scenario.id}`}
          >
            <h3 id={`heading-${scenario.id}`} className="sr-only">
              {info.name} benchmark
            </h3>
            {rows.length === 0 ? (
              <div className="border-cc-card-border bg-cc-surface/60 rounded-2xl border p-8 sm:p-12">
                <p className="text-cc-heading font-semibold">
                  {STATUSES[scenario.status].empty}
                </p>
                <p className="mt-2 max-w-2xl">
                  No throughput values from an older run have been substituted.
                </p>
              </div>
            ) : (
              <>
                {scenario.status !== "complete" && (
                  <p className="text-caption text-cc-warning mb-4">
                    This scenario is marked{" "}
                    {STATUSES[scenario.status].label.toLowerCase()} in this run.
                    The results below may be incomplete.
                  </p>
                )}
                <div className="space-y-6">
                  {COMPOSITION_GROUPS.map((group) => {
                    const groupRows = rows.filter(
                      (row) =>
                        variants.get(row.variant)!.variant.composition ===
                        group.id,
                    );
                    if (groupRows.length === 0) {
                      return null;
                    }
                    return (
                      <ResultsTable
                        key={group.id}
                        id={`${scenario.id}-${group.id}`}
                        title={group.name}
                        caption={`${info.name} benchmark, ${group.name} composition, run ${report.run.number}. Median requests per second across measured runs, with 95th and 99.9th percentile latency, peak gateway memory in MiB, average gateway CPU cores, the share of requests that succeeded, and run-to-run throughput variation.`}
                        rows={groupRows}
                        variants={variants}
                        maxRps={Math.max(0, ...groupRows.map(throughputOf))}
                        winnerRps={Math.max(
                          0,
                          ...groupRows.map(winningThroughputOf),
                        )}
                        measuredRuns={config.measuredRuns}
                        gatewayCores={config.cpuAllocation?.gateway?.cores}
                      />
                    );
                  })}
                </div>
              </>
            )}
            <p className="text-caption text-cc-ink-dim mt-4">
              {describeConditions(scenario)}
            </p>
            {config.measuredRuns === 1 && rows.length > 0 && (
              <p className="text-caption text-cc-warning mt-4">
                Only one measured run is available. Run-to-run variation cannot
                be estimated. Small differences do not establish a reliable
                ranking.
              </p>
            )}
          </section>
        );
      })}
      <p className="text-caption text-cc-ink-dim mt-5">
        Bars start at zero and scale to the highest throughput in each table.
        The highest throughput without failed requests is highlighted. Rows with
        failed requests show their reliability in amber.
      </p>
      <p className="text-caption text-cc-ink-dim mt-3">
        Memory is the peak sampled resident set size (RSS) of the gateway
        process tree. CPU is the average number of cores the gateway used, out
        of the cores allocated to it. Both exclude the load generator and
        subgraphs. Reliability is the share of HTTP requests that did not fail.
        Run variance is the coefficient of variation of throughput across
        measured runs; lower means more consistent results.
      </p>
    </section>
  );
}

interface ResultsTableProps {
  readonly id: string;
  readonly title: string;
  readonly caption: string;
  readonly rows: readonly BenchmarkVariantResult[];
  readonly variants: ReadonlyMap<string, BenchmarkCatalogEntry>;
  readonly maxRps: number;
  readonly winnerRps: number;
  readonly measuredRuns: number | undefined;
  readonly gatewayCores: number | undefined;
}

function ResultsTable({
  id,
  title,
  caption,
  rows,
  variants,
  maxRps,
  winnerRps,
  measuredRuns,
  gatewayCores,
}: ResultsTableProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface/60 overflow-hidden rounded-2xl border">
      <h4
        id={`group-${id}`}
        className="text-cc-heading border-cc-card-border border-b px-5 py-3.5 font-semibold sm:px-6"
      >
        {title}
      </h4>
      <div
        className="overflow-x-auto"
        role="region"
        aria-labelledby={`group-${id}`}
        tabIndex={0}
      >
        <table className="w-full min-w-224 table-fixed text-left text-sm">
          <caption className="sr-only">{caption}</caption>
          <colgroup>
            <col className="w-[26%]" />
            <col className="w-[16%]" />
            <col className="w-[8%]" />
            <col className="w-[9%]" />
            <col className="w-[10%]" />
            <col className="w-[11%]" />
            <col className="w-[9%]" />
            <col className="w-[11%]" />
          </colgroup>
          <thead className="text-cc-ink-dim bg-cc-white/2 text-xs xl:whitespace-nowrap">
            <tr>
              <th scope="col" className="px-5 py-2.5 font-medium sm:px-6">
                Router / gateway
              </th>
              <th scope="col" className="px-3 py-2.5 font-medium">
                <abbr
                  title="Median HTTP requests per second across measured runs"
                  className="no-underline"
                >
                  Median requests/s
                </abbr>
              </th>
              <th scope="col" className="px-3 py-2.5 text-right font-medium">
                <abbr
                  title="95th percentile HTTP request duration"
                  className="no-underline"
                >
                  p95 latency
                </abbr>
              </th>
              <th scope="col" className="px-3 py-2.5 text-right font-medium">
                <abbr
                  title="99.9th percentile HTTP request duration"
                  className="no-underline"
                >
                  p99.9 latency
                </abbr>
              </th>
              <th scope="col" className="px-3 py-2.5 text-right font-medium">
                <abbr
                  title="Peak resident memory (RSS) of the gateway process tree"
                  className="no-underline"
                >
                  Peak memory
                </abbr>
              </th>
              <th scope="col" className="px-3 py-2.5 text-right font-medium">
                <abbr
                  title="Average CPU cores used by the gateway, out of the cores allocated to it in this scenario"
                  className="no-underline"
                >
                  Average CPU
                </abbr>
              </th>
              <th scope="col" className="px-3 py-2.5 text-right font-medium">
                <abbr
                  title="Share of HTTP requests that did not fail"
                  className="no-underline"
                >
                  Reliability
                </abbr>
              </th>
              <th
                scope="col"
                className="py-2.5 pr-5 pl-3 text-right font-medium sm:pr-6"
              >
                <abbr
                  title="Coefficient of variation of throughput across measured runs"
                  className="no-underline"
                >
                  Run variance
                </abbr>
              </th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => {
              const entry = variants.get(row.variant)!;
              const name = variantName(entry);
              const link = gatewayLink(entry.gateway);
              const runs = measuredRuns ?? row.throughput.runsRps?.length;
              const fastest =
                isReliable(row) && row.throughput.medianRps === winnerRps;
              return (
                <tr
                  key={row.variant}
                  className={`border-cc-card-border border-t ${fastest ? "bg-cc-accent/5" : "hover:bg-cc-white/2"}`}
                >
                  <th scope="row" className="px-5 py-2.5 font-normal sm:px-6">
                    <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                      {link ? (
                        <Link
                          href={link}
                          className="text-cc-heading hover:text-cc-accent font-semibold underline-offset-4 hover:underline"
                        >
                          {name}
                        </Link>
                      ) : (
                        <span className="text-cc-heading font-semibold">
                          {name}
                        </span>
                      )}
                      {entry.variant.version !== null && (
                        <span className={TAG}>{entry.variant.version}</span>
                      )}
                      <span className={TAG}>
                        {formatRuntimeTag(entry.variant.platform)}
                      </span>
                    </div>
                  </th>
                  <td className="px-3 py-2.5">
                    <div className="flex items-center gap-3">
                      <span
                        className={`w-12 shrink-0 text-right font-mono font-semibold tabular-nums ${fastest ? "text-cc-accent" : "text-cc-heading"}`}
                      >
                        {formatNumber(row.throughput.medianRps)}
                        {fastest && (
                          <span className="sr-only">, highest reported</span>
                        )}
                      </span>
                      <div
                        className="bg-cc-white/5 h-1 flex-1 overflow-hidden rounded-full"
                        aria-hidden="true"
                      >
                        <div
                          className={`h-full rounded-full ${fastest ? "bg-cc-accent" : "bg-cc-ink-dim/45"}`}
                          style={{
                            width: `${maxRps === 0 ? 0 : (row.throughput.medianRps / maxRps) * 100}%`,
                          }}
                        />
                      </div>
                    </div>
                  </td>
                  <td className="px-3 py-2.5 text-right font-mono text-xs whitespace-nowrap tabular-nums">
                    {row.latencyMs ? (
                      <>
                        {formatLatency(row.latencyMs.p95)}{" "}
                        <span className="text-cc-ink-dim">ms</span>
                      </>
                    ) : (
                      NOT_REPORTED
                    )}
                  </td>
                  <td className="px-3 py-2.5 text-right font-mono text-xs whitespace-nowrap tabular-nums">
                    {row.latencyMs ? (
                      <>
                        {formatLatency(row.latencyMs.p99_9)}{" "}
                        <span className="text-cc-ink-dim">ms</span>
                      </>
                    ) : (
                      NOT_REPORTED
                    )}
                  </td>
                  <td className="px-3 py-2.5 text-right font-mono text-xs whitespace-nowrap tabular-nums">
                    {row.memory ? (
                      <>
                        {formatMemory(row.memory.peakRssKiB)}{" "}
                        <span className="text-cc-ink-dim">MiB</span>
                      </>
                    ) : (
                      NOT_REPORTED
                    )}
                  </td>
                  <td className="px-3 py-2.5 text-right font-mono text-xs whitespace-nowrap tabular-nums">
                    {row.cpu ? (
                      <>
                        {row.cpu.avgCores.toFixed(2)}
                        {gatewayCores !== undefined && (
                          <>
                            <span className="text-cc-ink-dim">/</span>
                            {gatewayCores}
                          </>
                        )}{" "}
                        <span className="text-cc-ink-dim">cores</span>
                      </>
                    ) : (
                      NOT_REPORTED
                    )}
                  </td>
                  <td
                    className={`px-3 py-2.5 text-right font-mono text-xs tabular-nums ${isReliable(row) ? "" : "text-cc-warning"}`}
                  >
                    {formatReliability(
                      row.requests.successful,
                      row.requests.total,
                    )}
                  </td>
                  <td className="py-2.5 pr-5 pl-3 text-right font-mono text-xs tabular-nums sm:pr-6">
                    {runs !== 1 ? (
                      `±${row.throughput.cvPercent.toFixed(1)}%`
                    ) : (
                      <span title="Variation cannot be estimated from one measured run">
                        —
                      </span>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
