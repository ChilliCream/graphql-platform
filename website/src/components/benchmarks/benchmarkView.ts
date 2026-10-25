import {
  BENCHMARK_REPOSITORY,
  BENCHMARK_FUSION_DOCS_URL,
  benchmarkRunId,
  benchmarkVariants,
  formatPlatform,
  decisiveFeatures,
  formatPlatformTag,
  isPreview,
  variantName,
  type BenchmarkCatalogEntry,
  type BenchmarkHistoryScenario,
  type BenchmarkHistoryVersion,
  type BenchmarkReport,
  type BenchmarkScenario,
  type BenchmarkVariantResult,
  type BenchmarkCompletedVariantResult,
  type Composition,
  type ScenarioId,
} from "@/src/helpers/benchmarkReport";

/** Display names for each scenario, in page order. */
export const SCENARIO_INFO: Readonly<
  Record<ScenarioId, { readonly name: string }>
> = {
  "constant-latency": { name: "Real-world" },
  constant: { name: "Synthetic" },
  burst: { name: "Burst traffic" },
};

const SCENARIO_ORDER = Object.keys(SCENARIO_INFO);

/** The fixed load profile of each scenario that the report does not record. */
export const SCENARIO_LOAD: Readonly<Record<ScenarioId, string>> = {
  "constant-latency":
    "50 virtual users run the GraphQL operation for 120 seconds. Subgraph requests have 4 ms of simulated downstream I/O.",
  constant:
    "50 virtual users run the GraphQL operation for 120 seconds. Subgraph requests have no simulated I/O, so the result leans on router overhead.",
  burst:
    "Load holds at 50 virtual users for 10 seconds, ramps to 500 over 40 seconds, then drops back to 50 over 10 seconds. Subgraph requests have no simulated I/O.",
};

/** Returns how requests per second is measured in a scenario when it differs from a steady load, or undefined. */
export const throughputNote = (scenario: BenchmarkScenario) =>
  scenario.id === "burst"
    ? `In this scenario it is the average over the whole run, not the rate at ${scenario.config.virtualUsers ?? "peak"} virtual users.`
    : undefined;

/** The composition families, in table order, with a one-sentence description each. */
export const COMPOSITION_GROUPS = [
  {
    id: "composite-schema",
    name: "GraphQL Federation specification",
    shortName: "GraphQL Federation",
    description:
      "An open standard for GraphQL federation, developed in the open under the GraphQL Foundation, with engineers from Apollo, ChilliCream, Netflix, The Guild, WunderGraph and others.",
  },
  {
    id: "apollo-federation",
    name: "Apollo Federation",
    shortName: "Apollo Federation",
    description:
      "A GraphQL federation approach popularized by Apollo and since adopted by other vendors.",
  },
] as const satisfies readonly {
  id: Composition;
  name: string;
  /** The name used inside sentences, such as "the fastest GraphQL Federation router". */
  shortName: string;
  description: string;
}[];

/** Returns the report scenarios in page order. */
export function orderedScenarios(
  report: BenchmarkReport,
): readonly BenchmarkScenario[] {
  return report.scenarios.toSorted(
    (a, b) => SCENARIO_ORDER.indexOf(a.id) - SCENARIO_ORDER.indexOf(b.id),
  );
}

/** Returns the scenario that is selected first: the real-world scenario, or the first one in page order. */
export function defaultScenarioId(
  scenarios: readonly BenchmarkScenario[],
): ScenarioId | undefined {
  return (
    scenarios.find((scenario) => scenario.id === "constant-latency")?.id ??
    scenarios[0]?.id
  );
}

/** The default line colors of the history chart, in catalog order. */
export const SERIES_COLORS = [
  "#5ba8fc",
  "#82fa2a",
  "#ce8703",
  "#26f4fd",
  "#b274a3",
  "#fcae93",
  "#b64efd",
  "#5a9e6e",
  "#c1b531",
  "#86cdff",
  "#8f79dc",
  "#ebd88a",
  "#3bfbb8",
  "#c796d2",
  "#27d271",
  "#6ad6b0",
  "#abd72e",
  "#fca2df",
  "#fc6efb",
  "#e83ccb",
  "#05b047",
  "#44b2aa",
  "#83b478",
  "#68ec6c",
  "#5dcbd1",
  "#81e7d3",
  "#d080e2",
  "#b570fd",
  "#ecb166",
  "#db56e8",
] as const;

/** One benchmark run in the history chart. */
export interface HistoryPoint {
  readonly timestamp: number;
  readonly runNumber: number;
  readonly runtime: string;
  readonly version: string;
  readonly rps: number;
  /** Whether the run's execution failed. */
  readonly failed: boolean;
  /** The number of measured runs that completed. */
  readonly completedRuns: number;
}

/** One variant's line in the history chart. */
export interface HistorySeries {
  readonly id: string;
  readonly name: string;
  readonly runtime: string;
  readonly composition: Composition;
  /** Whether the newest version is a preview. */
  readonly preview: boolean;
  readonly color: string;
  /** Whether the line is dashed because the palette ran out of colors. */
  readonly dashed: boolean;
  /** Sampled runs, oldest first: the runs on dates sampled across every series, which include each series' first and latest run, and every failed run. */
  readonly points: readonly HistoryPoint[];
}

/** Returns one series per variant with its dated runs. Series that share a name and runtime are told apart by their newest major version. */
export function historySeries(
  scenario: BenchmarkHistoryScenario,
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
  colors: readonly string[] = SERIES_COLORS,
): readonly HistorySeries[] {
  const catalogOrder = [...variants.keys()];
  const decisive = decisiveFeatures(variants);
  const runDate = (version: BenchmarkHistoryVersion) =>
    Date.parse(version.run.completedAt ?? version.run.startedAt!);
  const histories = scenario.variants.map((history) => ({
    history,
    versions: history.results.toSorted((a, b) => runDate(a) - runDate(b)),
  }));
  const dates = [
    ...new Set(histories.flatMap(({ versions }) => versions.map(runDate))),
  ].toSorted((a, b) => a - b);
  const stride = Math.max(1, Math.ceil((dates.length - 1) / 20));
  const count = Math.ceil((dates.length - 1) / stride) + 1;
  const sampledDates = new Set([
    ...Array.from(
      { length: count },
      (_, sample) =>
        dates[Math.round((sample * (dates.length - 1)) / (count - 1 || 1))],
    ),
    ...histories.flatMap(({ versions }) =>
      versions.length > 0
        ? [runDate(versions[0]), runDate(versions.at(-1)!)]
        : [],
    ),
  ]);
  const series = histories.flatMap(({ history, versions }) => {
    const entry = variants.get(history.variant)!;
    const features = decisive.get(history.variant)!;
    if (versions.length === 0) {
      return [];
    }
    const sampled = versions.filter(
      (version) =>
        version.status === "failed" || sampledDates.has(runDate(version)),
    );
    const colorIndex = catalogOrder.indexOf(history.variant);
    return {
      id: history.variant,
      name: variantName(entry),
      runtime: formatPlatformTag(versions.at(-1)!.platform, features),
      composition: entry.variant.composition,
      preview: isPreview(entry.variant),
      color: colors[colorIndex % colors.length],
      dashed: colorIndex >= colors.length,
      points: sampled.map((version) => ({
        timestamp: Date.parse(
          version.run.completedAt ?? version.run.startedAt!,
        ),
        runNumber: version.run.number,
        runtime: formatPlatformTag(
          version.platform,
          features.filter((feature) =>
            version.platform.features?.some((own) => own.id === feature.id),
          ),
        ),
        version: version.version,
        rps: version.throughput.medianRps,
        failed: version.status === "failed",
        completedRuns: version.throughput.runsRps!.length,
      })),
    };
  });
  return series.map((item) =>
    series.some(
      (other) =>
        other !== item &&
        other.composition === item.composition &&
        other.name === item.name &&
        other.runtime === item.runtime,
    )
      ? {
          ...item,
          name: `${item.name} ${item.points.at(-1)!.version.split(".")[0]}.x`,
        }
      : item,
  );
}

/** Returns equally spaced positions for recorded runs. A single run is centered. */
export function historyTimeScale(values: readonly number[]) {
  const dates = [...new Set(values)].toSorted((a, b) => a - b);
  const positions = new Map(dates.map((date, index) => [date, index]));
  return {
    ticks: dates,
    x: (value: number) =>
      dates.length < 2
        ? 50
        : (positions.get(value)! / (dates.length - 1)) * 100,
  };
}

export const formatHistoryDate = (timestamp: number, includeYear = false) =>
  new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    year: includeYear ? "numeric" : undefined,
    timeZone: "UTC",
  }).format(timestamp);

/** Returns run dates at a consistent run interval whose labels fit the available width. */
export function historyDateTicks(values: readonly number[], width: number) {
  const dates = [...new Set(values)].toSorted((a, b) => a - b);
  if (dates.length < 2) {
    return dates;
  }
  const first = dates[0];
  const last = dates.at(-1)!;
  const includeYear =
    new Date(first).getUTCFullYear() !== new Date(last).getUTCFullYear();
  const labelWidth = includeYear ? 90 : 52;
  const stride = Math.max(
    1,
    Math.ceil(
      ((labelWidth * 1.5 + 16) * (dates.length - 1)) / Math.max(width, 1),
    ),
  );
  return dates.filter((_, index) => index % stride === 0);
}

const niceStep = (range: number) => {
  const magnitude = 10 ** Math.floor(Math.log10(range));
  const fraction = range / magnitude;
  const nice = [1, 2, 2.5, 5, 10].find((step) => fraction <= step) ?? 10;
  return nice * magnitude;
};

/** Returns rounded axis ticks covering the values and a function mapping a value to its position, 0 at the top and 100 at the bottom. */
export const historyScale = (values: readonly number[]) => {
  const low = Math.min(...values);
  const high = Math.max(...values);
  const step = niceStep(Math.max(high - low, high * 0.1, 1) / 4);
  const min = Math.max(0, Math.floor(low / step) * step);
  const max = Math.ceil(high / step) * step;
  const ticks = Array.from(
    { length: Math.round((max - min) / step) + 1 },
    (_, index) => min + index * step,
  );
  return {
    ticks,
    y: (value: number) => (1 - (value - min) / (max - min || 1)) * 100,
  };
};

/** Returns the newest point of a series without a failed execution, or undefined when every run failed. */
export const latestCompletedPoint = (series: HistorySeries) =>
  series.points.findLast((point) => !point.failed);

/** Returns the relative throughput change from a series' oldest to newest version without a failed execution, or undefined for fewer than two such versions. */
export const seriesChange = (series: HistorySeries) => {
  const completed = series.points.filter((point) => !point.failed);
  if (completed.length < 2) {
    return undefined;
  }
  const first = completed[0];
  const last = completed.at(-1)!;
  return first.rps > 0 ? (last.rps - first.rps) / first.rps : undefined;
};

/** Formats a relative change as an unsigned percentage with one decimal. */
export const formatChange = (value: number) =>
  `${Math.abs(value * 100).toFixed(1)}%`;

/** A result with throughput and request measurements: a completed result, or a failed one that recorded runs. */
export type BenchmarkMeasuredVariantResult = BenchmarkVariantResult &
  Pick<BenchmarkCompletedVariantResult, "throughput" | "requests">;

/** Returns whether a result has throughput and request measurements. */
export const isMeasured = (
  row: BenchmarkVariantResult,
): row is BenchmarkMeasuredVariantResult =>
  row.throughput !== undefined && row.requests !== undefined;

/** Returns whether every request of a result succeeded. */
export const isReliable = (
  row: BenchmarkVariantResult,
): row is BenchmarkCompletedVariantResult =>
  row.status === "complete" &&
  row.requests.total > 0 &&
  row.requests.successful === row.requests.total;

/** One composition table of a scenario. */
export interface ResultsGroup {
  readonly id: Composition;
  readonly name: string;
  readonly shortName: string;
  readonly description: string;
  readonly rows: readonly BenchmarkVariantResult[];
  /** The highest median throughput in the table, used to scale its bars. */
  readonly maxRps: number;
  /** The highest median throughput without failed requests, or 0 when every row failed. */
  readonly winnerRps: number;
}

/** Returns whether a row is the highlighted winner of its table. */
export const isWinner = (row: BenchmarkVariantResult, group: ResultsGroup) =>
  isReliable(row) && row.throughput.medianRps === group.winnerRps;

const scenarioRows = (scenario: Pick<BenchmarkScenario, "results">) =>
  scenario.results.flatMap((result) => result.variants);

/** Returns whether the page shows a variant: Fusion only as its stable Native AOT build on .NET 11, every other gateway with all its variants. */
export function isShownVariant({ gateway, variant }: BenchmarkCatalogEntry) {
  return (
    gateway.id !== "fusion" ||
    (!isPreview(variant) &&
      variant.platform.runtime?.version?.split(".")[0] === "11" &&
      (variant.platform.features ?? []).some((feature) => feature.id === "aot"))
  );
}

/** Returns the ids of the gateways with at least one variant that is not a preview. */
export function releasedGateways(
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
): ReadonlySet<string> {
  return new Set(
    [...variants.values()]
      .filter(({ variant }) => !isPreview(variant))
      .map(({ gateway }) => gateway.id),
  );
}

/** Returns the names of the gateways that have had a stable release without a trailing Router or Gateway, Fusion first, and whether the report contains other gateways. */
export function releasedGatewayNames(report: BenchmarkReport): {
  readonly names: readonly string[];
  readonly others: boolean;
} {
  const released = releasedGateways(benchmarkVariants(report));
  const named = report.gateways
    .filter((gateway) => released.has(gateway.id))
    .toSorted((a, b) => Number(b.id === "fusion") - Number(a.id === "fusion"));
  return {
    names: [
      ...new Set(
        named.map((gateway) =>
          gateway.name.replace(/\s+(Router|Gateway)$/, ""),
        ),
      ),
    ],
    others: named.length < report.gateways.length,
  };
}

/** Returns the page description, naming each gateway that has had a stable release. */
export function benchmarkPageDescription(report: BenchmarkReport): string {
  const list = new Intl.ListFormat("en", { type: "conjunction" }).format(
    releasedGatewayNames(report).names,
  );
  return `Compare GraphQL federation router performance: throughput, latency, memory and CPU for ${list}, benchmarked daily under identical conditions.`;
}

/** Returns the non-empty composition tables of a scenario, rows sorted by median throughput. */
export function scenarioGroups(
  scenario: BenchmarkScenario,
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
): readonly ResultsGroup[] {
  const rows = scenarioRows(scenario).toSorted(
    (a, b) =>
      (b.status === "complete" ? b.throughput.medianRps : -1) -
        (a.status === "complete" ? a.throughput.medianRps : -1) ||
      a.variant.localeCompare(b.variant),
  );
  return COMPOSITION_GROUPS.flatMap((group) => {
    const groupRows = rows.filter(
      (row) => variants.get(row.variant)!.variant.composition === group.id,
    );
    if (groupRows.length === 0) {
      return [];
    }
    return {
      ...group,
      rows: groupRows,
      maxRps: Math.max(
        0,
        ...groupRows.map((row) =>
          isMeasured(row) ? row.throughput.medianRps : 0,
        ),
      ),
      winnerRps: Math.max(
        0,
        ...groupRows.map((row) =>
          isReliable(row) ? row.throughput.medianRps : -1,
        ),
      ),
    };
  });
}

/** Returns the accessible caption of a results table. */
export const tableCaption = (
  scenario: BenchmarkScenario,
  group: ResultsGroup,
  report: BenchmarkReport,
) =>
  `${SCENARIO_INFO[scenario.id].name} benchmark, ${group.name} composition, run ${report.run.number}. Median requests per second across measured runs, with 95th and 99.9th percentile latency, peak router memory in MiB, average router CPU utilization, the share of requests that succeeded, and run-to-run throughput variation.`;

/** Formats a request rate as a whole number. */
export const formatNumber = (value: number) =>
  new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(value);

/** Formats KiB as whole MiB, without the unit. */
export const formatMemory = (kibibytes: number) =>
  new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(
    kibibytes / 1024,
  );

/** Formats milliseconds with one decimal below 100 ms, without the unit. */
export const formatLatency = (milliseconds: number) =>
  new Intl.NumberFormat("en-US", {
    minimumFractionDigits: milliseconds < 100 ? 1 : 0,
    maximumFractionDigits: milliseconds < 100 ? 1 : 0,
  }).format(milliseconds);

/** Formats the share of successful requests, rounded down so a failure never shows as 100%. */
export const formatReliability = (successful: number, total: number) => {
  if (total === 0) {
    return "—";
  }
  if (successful === total) {
    return "100%";
  }
  return `${(Math.floor((successful / total) * 10_000) / 100).toFixed(2)}%`;
};

/** Formats a date as day, month name and year in UTC. */
export const formatDate = (timestamp: string) =>
  new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(timestamp));

/** The run, hardware and link facts of a report. */
export interface BenchmarkFacts {
  readonly runNumber: number;
  /** The completion time, or the start time while the run is unfinished, or null. */
  readonly timestamp: string | null;
  readonly completed: boolean;
  /** The formatted `timestamp`, or undefined when it is null. */
  readonly date: string | undefined;
  /** The GitHub Actions run, or undefined when the report does not link one. */
  readonly runUrl: string | undefined;
  readonly sourceCommit: string;
  readonly hardware: readonly (readonly [label: string, value: string])[];
  readonly cpu: string;
  readonly cpuTopology: string;
  readonly memoryGb: number;
  readonly os: string;
  readonly subgraphs: string;
  /** The CPU cores the gateway is pinned to, taken from the first scenario that records them. */
  readonly gatewayCores: number | undefined;
  readonly warmupRuns: number | undefined;
  readonly measuredRuns: number | undefined;
  readonly links: {
    readonly repository: string;
    readonly query: string;
    readonly readme: string;
    readonly issues: string;
    readonly guild: string;
    readonly fusionDocs: string;
  };
}

/** Collects the facts every design presents about a report. */
export function benchmarkFacts(report: BenchmarkReport): BenchmarkFacts {
  const { completedAt, startedAt } = report.run;
  const timestamp = completedAt ?? startedAt;
  const runId = benchmarkRunId(report);
  const source = `${BENCHMARK_REPOSITORY}/blob/${report.run.sourceCommit}`;
  const configs = report.scenarios.map((scenario) => scenario.config);
  const subgraphs = `${[report.subgraphs.framework.name, report.subgraphs.framework.version].filter(Boolean).join(" ")} (${formatPlatform(report.subgraphs.platform)})`;
  const cpuTopology =
    report.hardware.physicalCores === undefined
      ? `${report.hardware.hardwareThreads} hardware threads.`
      : `${report.hardware.physicalCores} physical cores (${report.hardware.hardwareThreads} hardware threads${(report.hardware.threadsPerCore ?? 1) > 1 ? ", SMT" : ""}).`;
  return {
    runNumber: report.run.number,
    timestamp,
    completed: completedAt !== undefined,
    date: timestamp === null ? undefined : formatDate(timestamp),
    runUrl: runId && `${BENCHMARK_REPOSITORY}/actions/runs/${runId}`,
    sourceCommit: report.run.sourceCommit,
    hardware: [
      ["Processor", report.hardware.cpuModel],
      ["CPU topology", cpuTopology],
      ["Memory", `${report.hardware.memoryGb} GB`],
      ["Operating system", report.hardware.os],
      ["Subgraphs", subgraphs],
    ],
    cpu: report.hardware.cpuModel,
    cpuTopology,
    memoryGb: report.hardware.memoryGb,
    os: report.hardware.os,
    subgraphs,
    gatewayCores: configs.find((config) => config.cpuAllocation?.gateway)
      ?.cpuAllocation?.gateway?.physicalCores,
    warmupRuns: configs[0]?.warmupRuns,
    measuredRuns: configs[0]?.measuredRuns,
    links: {
      repository: BENCHMARK_REPOSITORY,
      query: `${source}/k6/k6.js`,
      readme: `${source}/README.md`,
      issues: `${BENCHMARK_REPOSITORY}/issues`,
      guild: "https://github.com/graphql-hive/graphql-gateways-benchmark",
      fusionDocs: BENCHMARK_FUSION_DOCS_URL,
    },
  };
}

/** The process groups that logical CPUs are pinned to. */
export const CPU_GROUPS = [
  { id: "system", name: "OS & monitoring", color: "#9085e9" },
  { id: "k6", name: "Load generator", color: "#d95926" },
  { id: "gateway", name: "Router", color: "#3987e5" },
  { id: "subgraphs", name: "Subgraphs", color: "#199e70" },
] as const;
export type CpuGroupId = (typeof CPU_GROUPS)[number]["id"];

/** Returns hardware threads positioned by physical core, or undefined when topology or allocation is unavailable. */
export function cpuMap(report: BenchmarkReport) {
  const { physicalCores, coreThreads, threadsPerCore } = report.hardware;
  const allocation = report.scenarios.find(
    (scenario) => scenario.config.cpuAllocation,
  )?.config.cpuAllocation;
  if (
    !physicalCores ||
    !coreThreads ||
    !threadsPerCore ||
    !allocation ||
    coreThreads.length !== physicalCores ||
    coreThreads.some((core) => core.length !== threadsPerCore)
  ) {
    return undefined;
  }
  const cells = coreThreads.flatMap((threads, core) =>
    threads.map((thread, row) => ({
      thread,
      core,
      row,
      group: CPU_GROUPS.find(({ id }) =>
        allocation[id]?.threadIds.includes(thread),
      )?.id,
    })),
  );
  return cells.every((cell) => cell.group !== undefined) ? cells : undefined;
}

/** The fastest successful gateway variant and its nearest competing gateway, across both compositions. */
export interface ScenarioVerdict {
  readonly leader: BenchmarkCompletedVariantResult;
  /** The fastest successful variant of another gateway, or undefined when no other gateway succeeded. */
  readonly runnerUp: BenchmarkCompletedVariantResult | undefined;
  /** The leader's throughput advantage over the runner-up as a fraction, or 0 without a runner-up. */
  readonly lead: number;
  /** Whether the lead is smaller than the run variance of either result. */
  readonly tied: boolean;
}

/** Returns the verdict of a scenario, or undefined when no result is free of failed requests. */
export function scenarioVerdict(
  scenario: BenchmarkScenario,
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
): ScenarioVerdict | undefined {
  const ranked = scenarioGroups(scenario, variants)
    .flatMap((group) => group.rows)
    .filter(isReliable)
    .toSorted((a, b) => b.throughput.medianRps - a.throughput.medianRps);
  const leader = ranked[0];
  if (!leader) {
    return undefined;
  }
  const gatewayId = variants.get(leader.variant)!.gateway.id;
  const runnerUp = ranked.find(
    (row) => variants.get(row.variant)!.gateway.id !== gatewayId,
  );
  const lead =
    runnerUp && runnerUp.throughput.medianRps > 0
      ? leader.throughput.medianRps / runnerUp.throughput.medianRps - 1
      : 0;
  const variance = Math.max(
    leader.throughput.cvPercent,
    runnerUp?.throughput.cvPercent ?? 0,
  );
  return {
    leader,
    runnerUp,
    lead,
    tied: runnerUp !== undefined && lead * 100 < variance,
  };
}

/** Returns a report that keeps only the results of one composition. */
export function compositionReport(
  report: BenchmarkReport,
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
  composition: Composition,
): BenchmarkReport {
  return {
    ...report,
    scenarios: report.scenarios.map((scenario) => ({
      ...scenario,
      results: scenario.results.map((result) => ({
        ...result,
        variants: result.variants.filter(
          (row) =>
            variants.get(row.variant)!.variant.composition === composition,
        ),
      })),
    })),
  };
}
