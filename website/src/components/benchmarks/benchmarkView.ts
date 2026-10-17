import {
  BENCHMARK_REPOSITORY,
  benchmarkRunId,
  formatPlatform,
  formatRuntimeTag,
  isPreview,
  variantName,
  type BenchmarkCatalogEntry,
  type BenchmarkHistoryScenario,
  type BenchmarkReport,
  type BenchmarkScenario,
  type BenchmarkVariantResult,
  type Composition,
  type ScenarioId,
  type ScenarioStatus,
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
    "50 virtual users run the GraphQL operation for 120 seconds. Every subgraph request waits 4 ms to simulate downstream I/O.",
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

/** The composition families, in table order. */
export const COMPOSITION_GROUPS = [
  { id: "composite-schema", name: "Composite Schema" },
  { id: "apollo-federation", name: "Apollo Federation" },
] as const satisfies readonly { id: Composition; name: string }[];

/** Labels and empty-state messages for each scenario status. */
export const SCENARIO_STATUSES: Readonly<
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
  "#3987e5",
  "#d95926",
  "#199e70",
  "#c98500",
  "#d55181",
  "#008300",
  "#9085e9",
  "#e66767",
] as const;

/** The number of newest versions the history chart shows per variant. */
export const HISTORY_SLOTS = 3;

/** One version of a variant in the history chart. */
export interface HistoryPoint {
  /** The chart column, 0 to `HISTORY_SLOTS - 1`, newest last and right-aligned. */
  readonly slot: number;
  readonly version: string;
  readonly rps: number;
}

/** One variant's line in the history chart. */
export interface HistorySeries {
  readonly id: string;
  readonly name: string;
  readonly runtime: string;
  readonly composition: Composition;
  readonly color: string;
  /** Whether the line is dashed because the palette ran out of colors. */
  readonly dashed: boolean;
  /** Up to `HISTORY_SLOTS` versions, oldest first. */
  readonly points: readonly HistoryPoint[];
}

/** Returns one series per variant with its newest versions, skipping previews when `stableOnly` is set. Series that share a name and runtime are told apart by their newest major version. */
export function historySeries(
  scenario: BenchmarkHistoryScenario,
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
  stableOnly: boolean,
  colors: readonly string[] = SERIES_COLORS,
): readonly HistorySeries[] {
  const catalogOrder = [...variants.keys()];
  const series = scenario.variants.flatMap((history) => {
    const entry = variants.get(history.variant)!;
    const versions = history.versions
      .filter(
        (version) =>
          !stableOnly || !isPreview(version.version, version.platform),
      )
      .slice(-HISTORY_SLOTS);
    if (versions.length === 0) {
      return [];
    }
    const colorIndex = catalogOrder.indexOf(history.variant);
    return {
      id: history.variant,
      name: variantName(entry),
      runtime: formatRuntimeTag(versions.at(-1)!.platform),
      composition: entry.variant.composition,
      color: colors[colorIndex % colors.length],
      dashed: colorIndex >= colors.length,
      points: versions.map((version, index) => ({
        slot: HISTORY_SLOTS - versions.length + index,
        version: version.version,
        rps: version.throughput.medianRps,
      })),
    };
  });
  return series.map((item) =>
    series.some(
      (other) =>
        other !== item &&
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

/** Returns the relative throughput change from a series' oldest to newest version, or undefined for a single version. */
export const seriesChange = (series: HistorySeries) => {
  const first = series.points[0];
  const last = series.points.at(-1)!;
  return series.points.length > 1 && first.rps > 0
    ? (last.rps - first.rps) / first.rps
    : undefined;
};

/** Formats a relative change as an unsigned percentage with one decimal. */
export const formatChange = (value: number) =>
  `${Math.abs(value * 100).toFixed(1)}%`;

/** Returns whether every request of a result succeeded. */
export const isReliable = (row: BenchmarkVariantResult) =>
  row.requests.total > 0 && row.requests.successful === row.requests.total;

/** One composition table of a scenario. */
export interface ResultsGroup {
  readonly id: Composition;
  readonly name: string;
  readonly rows: readonly BenchmarkVariantResult[];
  /** The highest median throughput in the table, used to scale its bars. */
  readonly maxRps: number;
  /** The highest median throughput without failed requests, or 0 when every row failed. */
  readonly winnerRps: number;
}

/** Returns whether a row is the highlighted winner of its table. */
export const isWinner = (row: BenchmarkVariantResult, group: ResultsGroup) =>
  isReliable(row) && row.throughput.medianRps === group.winnerRps;

/** Returns the non-empty composition tables of a scenario, rows sorted by median throughput. */
export function scenarioGroups(
  scenario: BenchmarkScenario,
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
  stableOnly: boolean,
): readonly ResultsGroup[] {
  const rows = scenario.results
    .flatMap((result) => result.variants)
    .filter((row) => {
      const { variant } = variants.get(row.variant)!;
      return !stableOnly || !isPreview(variant.version, variant.platform);
    })
    .toSorted(
      (a, b) =>
        b.throughput.medianRps - a.throughput.medianRps ||
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
      maxRps: Math.max(0, ...groupRows.map((row) => row.throughput.medianRps)),
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
  `${SCENARIO_INFO[scenario.id].name} benchmark, ${group.name} composition, run ${report.run.number}. Median requests per second across measured runs, with 95th and 99.9th percentile latency, peak router memory in MiB, average router CPU cores, the share of requests that succeeded, and run-to-run throughput variation.`;

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
  readonly logicalCpus: number;
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
  return {
    runNumber: report.run.number,
    timestamp,
    completed: completedAt !== undefined,
    date: timestamp === null ? undefined : formatDate(timestamp),
    runUrl: runId && `${BENCHMARK_REPOSITORY}/actions/runs/${runId}`,
    sourceCommit: report.run.sourceCommit,
    hardware: [
      ["Processor", report.hardware.cpu],
      ["Threads", String(report.hardware.logicalCpus)],
      ["Memory", `${report.hardware.memoryGb} GB`],
      ["Operating system", report.hardware.os],
      ["Subgraphs", subgraphs],
    ],
    cpu: report.hardware.cpu,
    logicalCpus: report.hardware.logicalCpus,
    memoryGb: report.hardware.memoryGb,
    os: report.hardware.os,
    subgraphs,
    gatewayCores: configs.find((config) => config.cpuAllocation?.gateway)
      ?.cpuAllocation?.gateway?.cores,
    warmupRuns: configs[0]?.warmupRuns,
    measuredRuns: configs[0]?.measuredRuns,
    links: {
      repository: BENCHMARK_REPOSITORY,
      query: `${source}/k6/k6.js`,
      readme: `${source}/README.md`,
      issues: `${BENCHMARK_REPOSITORY}/issues`,
      guild: "https://github.com/graphql-hive/graphql-gateways-benchmark",
      fusionDocs: "/docs/fusion",
    },
  };
}

/** The process groups that logical CPUs are pinned to. */
export const CPU_GROUPS = [
  { id: "gateway", name: "Router", color: "#3987e5" },
  { id: "subgraphs", name: "Subgraphs", color: "#199e70" },
  { id: "k6", name: "Load generator", color: "#d95926" },
] as const;

export type CpuGroupId = (typeof CPU_GROUPS)[number]["id"];

const parseCpuset = (cpuset: string): readonly number[] =>
  cpuset.split(",").flatMap((part) => {
    const [start, end = start] = part.trim().split("-").map(Number);
    return Number.isInteger(start) && Number.isInteger(end) && end >= start
      ? Array.from({ length: end - start + 1 }, (_, index) => start + index)
      : [];
  });

/** Returns the process group of each logical CPU, undefined for CPUs no group is pinned to, or undefined when the report records no CPU allocation. */
export function cpuMap(
  report: BenchmarkReport,
): readonly (CpuGroupId | undefined)[] | undefined {
  const allocation = report.scenarios.find(
    (scenario) => scenario.config.cpuAllocation,
  )?.config.cpuAllocation;
  if (!allocation) {
    return undefined;
  }
  const groups: (CpuGroupId | undefined)[] = Array.from({
    length: report.hardware.logicalCpus,
  });
  for (const { id } of CPU_GROUPS) {
    for (const cpu of parseCpuset(allocation[id]?.cpuset ?? "")) {
      if (cpu < groups.length) {
        groups[cpu] = id;
      }
    }
  }
  return groups;
}
