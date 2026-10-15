"use client";

import { useState } from "react";
import {
  BENCHMARK_REPOSITORY,
  GATEWAYS,
  benchmarkSourceUrl,
  type BenchmarkReport,
  type ScenarioId,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkExplorerProps {
  readonly report: BenchmarkReport;
}

const SCENARIOS = {
  "constant-latency": {
    name: "Real-world",
    hint: "With downstream I/O",
    file: "RESULTS_CONSTANT_LATENCY.md",
  },
  burst: {
    name: "Burst traffic",
    hint: "Rising concurrency",
    file: "RESULTS_BURST.md",
  },
  constant: {
    name: "Synthetic",
    hint: "Without added I/O",
    file: "RESULTS_CONSTANT.md",
  },
} as const;

const STATUS_LABELS = {
  ok: "Measured",
  failed: "Run failed",
  incompatible: "Incompatible response",
  "not-run": "Not run",
} as const;

const formatNumber = (value: number) =>
  new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(value);

const formatMemory = (kibibytes: number) =>
  new Intl.NumberFormat("en-US", {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  }).format(kibibytes / 1024);

export function BenchmarkExplorer({ report }: BenchmarkExplorerProps) {
  const [selected, setSelected] = useState<ScenarioId>("constant-latency");
  const [includePreviews, setIncludePreviews] = useState(true);

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
        {report.scenarios.map((scenario, index) => (
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
              {scenario.status === "pending" && (
                <span className="text-cc-warning ml-auto text-xs">Pending</span>
              )}
            </span>
          </label>
        ))}
      </fieldset>
      <div className="mb-5 text-sm" data-benchmark-controls>
        <label className="flex cursor-pointer items-center gap-2">
          <input
            type="checkbox"
            checked={includePreviews}
            onChange={(event) => setIncludePreviews(event.target.checked)}
            className="accent-cc-accent size-4"
          />
          Include preview builds
        </label>
      </div>
      <noscript>
        <style>{`[data-benchmark-panel] { display: block; margin-top: 2rem; } [data-benchmark-controls] { display: none; }`}</style>
      </noscript>
      {report.scenarios.map((scenario) => {
        const info = SCENARIOS[scenario.id];
        const rows = scenario.results
          .filter((row) => includePreviews || !GATEWAYS[row.gateway].preview)
          .toSorted(
            (a, b) =>
              (b.medianRps ?? -1) - (a.medianRps ?? -1) ||
              a.gateway.localeCompare(b.gateway),
          );
        const maxRps = Math.max(0, ...rows.map((row) => row.medianRps ?? 0));
        return (
          <section
            key={scenario.id}
            id={`panel-${scenario.id}`}
            data-benchmark-panel
            className={selected === scenario.id ? undefined : "hidden"}
            aria-labelledby={`heading-${scenario.id}`}
          >
            <div className="border-cc-card-border bg-cc-surface/60 overflow-hidden rounded-2xl border">
              <div className="border-cc-card-border flex flex-wrap items-center justify-between gap-3 border-b px-5 py-4 sm:px-6">
                <h3
                  id={`heading-${scenario.id}`}
                  className="text-cc-heading font-semibold"
                >
                  {info.name} benchmark
                </h3>
                <p className="text-caption text-cc-ink-dim font-mono">
                  {scenario.virtualUsers}{" "}
                  {scenario.id === "burst" ? "peak VUs" : "VUs"}{" "}
                  <span aria-hidden="true">/</span> {scenario.durationSeconds}s{" "}
                  <span aria-hidden="true">/</span> {scenario.subgraphDelayMs}ms
                  added I/O
                </p>
              </div>
              {scenario.status === "pending" ? (
                <div className="p-8 sm:p-12">
                  <p className="text-cc-heading font-semibold">
                    Results not yet published in this snapshot.
                  </p>
                  <p className="mt-2 max-w-2xl">
                    The burst test starts at 50 virtual users, ramps to 500, and
                    returns to 50. No throughput values from an older run have
                    been substituted.
                  </p>
                </div>
              ) : (
                <div
                  className="overflow-x-auto"
                  role="region"
                  aria-label={`${info.name} benchmark results, scroll horizontally for all columns`}
                  tabIndex={0}
                >
                  <table className="w-full min-w-235 text-left text-sm">
                    <caption className="sr-only">
                      {info.name} GraphQL federation benchmark, run{" "}
                      {report.run.number}. Median requests per second across{" "}
                      {scenario.measuredRuns} measured run(s), with best and
                      worst throughput, peak gateway resident memory in MiB, and
                      coefficient of variation.
                    </caption>
                    <thead className="text-cc-ink-dim bg-cc-white/2 text-xs">
                      <tr>
                        <th scope="col" className="px-6 py-4 font-medium">
                          Router / gateway
                        </th>
                        <th scope="col" className="px-5 py-4 font-medium">
                          Median RPS ↑
                        </th>
                        <th
                          scope="col"
                          className="px-5 py-4 text-right font-medium"
                        >
                          <span className="block">Peak memory ↓</span>
                          <span className="mt-1 block font-normal">
                            MiB · RSS
                          </span>
                        </th>
                        <th
                          scope="col"
                          className="px-5 py-4 text-right font-medium"
                        >
                          Worst → best RPS
                        </th>
                        <th
                          scope="col"
                          className="px-6 py-4 text-right font-medium"
                        >
                          <abbr
                            title="Coefficient of variation across measured runs"
                            className="underline-offset-4"
                          >
                            CV
                          </abbr>
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows.map((row) => {
                        const gateway = GATEWAYS[row.gateway];
                        const fastest =
                          row.status === "ok" && row.medianRps === maxRps;
                        return (
                          <tr
                            key={row.gateway}
                            className={`border-cc-card-border border-t ${fastest ? "bg-cc-accent/5" : "hover:bg-cc-white/2"}`}
                          >
                            <th scope="row" className="px-6 py-4 font-normal">
                              <div className="text-cc-heading flex flex-wrap items-center gap-2 font-semibold">
                                {gateway.name}
                                {gateway.preview && (
                                  <span className="border-cc-card-border text-cc-ink-dim rounded border px-1.5 py-0.5 text-[10px] font-normal tracking-wide uppercase">
                                    Preview
                                  </span>
                                )}
                              </div>
                              <div className="text-cc-ink-dim mt-1 text-xs">
                                {gateway.runtime} · {gateway.family}
                              </div>
                              <div className="text-cc-ink-dim mt-1 font-mono text-xs">
                                {row.version ?? "Version unavailable"}
                              </div>
                            </th>
                            <td className="w-[28%] min-w-56 px-5 py-4">
                              {row.status === "ok" && row.medianRps !== null ? (
                                <>
                                  <div
                                    className={`mb-2 font-mono font-semibold tabular-nums ${fastest ? "text-cc-accent" : "text-cc-heading"}`}
                                  >
                                    {formatNumber(row.medianRps)}
                                    {fastest && (
                                      <span className="ml-2 font-sans text-xs font-normal">
                                        Highest reported
                                      </span>
                                    )}
                                  </div>
                                  <div
                                    className="bg-cc-white/5 h-1.5 overflow-hidden rounded-full"
                                    aria-hidden="true"
                                  >
                                    <div
                                      className={`h-full rounded-full ${fastest ? "bg-cc-accent" : "bg-cc-ink-dim/45"}`}
                                      style={{
                                        width: `${maxRps === 0 ? 0 : (row.medianRps / maxRps) * 100}%`,
                                      }}
                                    />
                                  </div>
                                </>
                              ) : (
                                <span className="text-cc-warning">
                                  {STATUS_LABELS[row.status]}
                                </span>
                              )}
                            </td>
                            <td className="px-5 py-4 text-right font-mono text-xs whitespace-nowrap tabular-nums">
                              {row.memory !== null ? (
                                <a
                                  className="text-cc-heading decoration-cc-ink-dim/50 hover:text-cc-accent underline decoration-dotted underline-offset-4"
                                  href={`${BENCHMARK_REPOSITORY}/actions/runs/${report.run.id}/artifacts/${row.memory.artifactId}`}
                                  aria-label={`${gateway.name} ${gateway.runtime}: ${formatMemory(row.memory.peakRssKiB)} MiB peak memory, open source artifact`}
                                >
                                  {formatMemory(row.memory.peakRssKiB)}
                                </a>
                              ) : (
                                <span className="text-cc-ink-dim font-sans">
                                  Not reported
                                </span>
                              )}
                            </td>
                            <td className="px-5 py-4 text-right font-mono text-xs whitespace-nowrap tabular-nums">
                              {row.worstRps !== null && row.bestRps !== null
                                ? `${formatNumber(row.worstRps)} → ${formatNumber(row.bestRps)}`
                                : "—"}
                            </td>
                            <td className="px-6 py-4 text-right font-mono text-xs tabular-nums">
                              {scenario.measuredRuns > 1 &&
                              row.cvPercent !== null ? (
                                `${row.cvPercent.toFixed(1)}%`
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
              )}
              <div className="border-cc-card-border text-caption text-cc-ink-dim flex flex-wrap justify-between gap-3 border-t px-6 py-4">
                <span>
                  {scenario.status === "pending"
                    ? "Awaiting measurements"
                    : `${scenario.measuredRuns} measured run${scenario.measuredRuns === 1 ? "" : "s"} · ${scenario.warmupRuns} discarded warmup run${scenario.warmupRuns === 1 ? "" : "s"}`}
                </span>
                {scenario.status === "complete" && (
                  <a
                    className="text-cc-accent underline-offset-4 hover:underline"
                    href={benchmarkSourceUrl(report, info.file)}
                  >
                    Original result table ↗
                  </a>
                )}
              </div>
            </div>
            {scenario.measuredRuns === 1 && scenario.status === "complete" && (
              <p className="text-caption text-cc-warning mt-4">
                Only one measured run is available. The median, best, and worst
                are the same observation; run-to-run variation cannot be
                estimated. Small differences do not establish a reliable
                ranking.
              </p>
            )}
          </section>
        );
      })}
      <p className="text-caption text-cc-ink-dim mt-5">
        Bars start at zero and scale to the highest displayed throughput in each
        scenario. Preview builds are labeled separately. An incompatible
        response or a failed run is never ranked as a successful measurement.
      </p>
      <p className="text-caption text-cc-ink-dim mt-3">
        Memory is the peak sampled resident set size (RSS) of the gateway
        process tree during the measured run that supplies the median RPS. It
        excludes the load generator and subgraphs. One MiB is 1,024 KiB; lower
        memory is better. Each memory value links to its source artifact.
      </p>
    </section>
  );
}
