export const BENCHMARK_REPOSITORY =
  "https://github.com/ChilliCream/graphql-federation-benchmarks";

export const GATEWAYS = {
  fusion: {
    name: "Fusion",
    runtime: ".NET 10",
    family: "GraphQL Federation",
    preview: false,
  },
  "fusion-nightly": {
    name: "Fusion Preview",
    runtime: ".NET 10",
    family: "GraphQL Federation",
    preview: true,
  },
  "fusion-nightly-net11": {
    name: "Fusion Preview",
    runtime: ".NET 11 Preview",
    family: "GraphQL Federation",
    preview: true,
  },
  "fusion-nightly-fed": {
    name: "Fusion Preview · Apollo Federation",
    runtime: ".NET 10",
    family: "Apollo Federation",
    preview: true,
  },
  "hive-router": {
    name: "Hive Router",
    runtime: "Rust",
    family: "Apollo Federation",
    preview: false,
  },
  "hive-gateway": {
    name: "Hive Gateway",
    runtime: "Node.js",
    family: "Apollo Federation",
    preview: false,
  },
  "hive-gateway-router-runtime": {
    name: "Hive Gateway · Router Runtime",
    runtime: "Node.js",
    family: "Apollo Federation",
    preview: false,
  },
  "apollo-router": {
    name: "Apollo Router",
    runtime: "Rust",
    family: "Apollo Federation",
    preview: false,
  },
  "apollo-router-v3": {
    name: "Apollo Router v3 Preview",
    runtime: "Rust",
    family: "Apollo Federation",
    preview: true,
  },
  "apollo-gateway": {
    name: "Apollo Gateway",
    runtime: "Node.js",
    family: "Apollo Federation",
    preview: false,
  },
  cosmo: {
    name: "Cosmo Router",
    runtime: "Go",
    family: "Apollo Federation",
    preview: false,
  },
  grafbase: {
    name: "Grafbase Gateway",
    runtime: "Rust",
    family: "Apollo Federation",
    preview: false,
  },
  feddi: {
    name: "feddi",
    runtime: "JVM",
    family: "GraphQL Federation",
    preview: false,
  },
  caliban: {
    name: "Caliban",
    runtime: "JVM",
    family: "Apollo Federation",
    preview: false,
  },
} as const;

export type GatewayId = keyof typeof GATEWAYS;
export type ScenarioId = "constant-latency" | "burst" | "constant";

export interface BenchmarkResult {
  readonly gateway: GatewayId;
  readonly version: string | null;
  readonly status: "ok" | "failed" | "incompatible" | "not-run";
  readonly medianRps: number | null;
  readonly bestRps: number | null;
  readonly worstRps: number | null;
  readonly cvPercent: number | null;
  readonly memory: {
    readonly peakRssKiB: number;
    readonly artifactId: number;
  } | null;
}

export interface BenchmarkScenario {
  readonly id: ScenarioId;
  readonly status: "complete" | "pending";
  readonly durationSeconds: number;
  readonly virtualUsers: number;
  readonly subgraphDelayMs: number;
  readonly warmupRuns: number;
  readonly measuredRuns: number;
  readonly results: readonly BenchmarkResult[];
}

export interface BenchmarkReport {
  readonly schemaVersion: 1;
  readonly example: boolean;
  readonly run: {
    readonly id: number;
    readonly number: number;
    readonly startedAt: string;
    readonly sourceCommit: string;
    readonly reportCommit: string;
    readonly status: "complete" | "in-progress";
  };
  readonly hardware: {
    readonly host: string;
    readonly cpu: string;
    readonly logicalCpus: number;
    readonly memoryGb: number;
    readonly os: string;
  };
  readonly subgraphs: {
    readonly implementation: string;
    readonly version: string;
  };
  readonly scenarios: readonly BenchmarkScenario[];
}

function record(
  value: unknown,
  keys: readonly string[],
): Record<string, unknown> {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new Error("Expected a benchmark report object.");
  }
  const result = value as Record<string, unknown>;
  if (
    Object.keys(result).length !== keys.length ||
    keys.some((key) => !Object.hasOwn(result, key))
  ) {
    throw new Error("Unexpected or missing benchmark report fields.");
  }
  return result;
}

function number(
  value: unknown,
  min: number,
  max: number,
  integer = false,
): asserts value is number {
  if (
    typeof value !== "number" ||
    !Number.isFinite(value) ||
    value < min ||
    value > max ||
    (integer && !Number.isSafeInteger(value))
  ) {
    throw new Error("Invalid benchmark report number.");
  }
}

function text(
  value: unknown,
  pattern = /^[A-Za-z0-9 .+(),/#:_-]{1,160}$/,
): asserts value is string {
  if (typeof value !== "string" || !pattern.test(value)) {
    throw new Error("Invalid benchmark report text.");
  }
}

function member(value: unknown, values: readonly unknown[]): void {
  if (!values.includes(value)) {
    throw new Error("Unknown benchmark report value.");
  }
}

/** Parses a bounded version 1 report and rejects unknown fields and identities. */
export function parseBenchmarkReport(json: string): BenchmarkReport {
  if (new TextEncoder().encode(json).byteLength > 262_144) {
    throw new Error("Benchmark report exceeds 256 KiB.");
  }
  const report = record(JSON.parse(json), [
    "schemaVersion",
    "example",
    "run",
    "hardware",
    "subgraphs",
    "scenarios",
  ]);
  member(report.schemaVersion, [1]);
  member(report.example, [true, false]);

  const run = record(report.run, [
    "id",
    "number",
    "startedAt",
    "sourceCommit",
    "reportCommit",
    "status",
  ]);
  number(run.id, 1, Number.MAX_SAFE_INTEGER, true);
  number(run.number, 1, 1_000_000_000, true);
  text(run.startedAt, /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$/);
  if (
    !Number.isFinite(Date.parse(run.startedAt)) ||
    new Date(run.startedAt).toISOString() !==
      run.startedAt.replace("Z", ".000Z")
  ) {
    throw new Error("Invalid benchmark run date.");
  }
  text(run.sourceCommit, /^[a-f0-9]{40}$/);
  text(run.reportCommit, /^[a-f0-9]{40}$/);
  member(run.status, ["complete", "in-progress"]);

  const hardware = record(report.hardware, [
    "host",
    "cpu",
    "logicalCpus",
    "memoryGb",
    "os",
  ]);
  text(hardware.host);
  text(hardware.cpu);
  text(hardware.os);
  number(hardware.logicalCpus, 1, 4096, true);
  number(hardware.memoryGb, 1, 65_536);
  const subgraphs = record(report.subgraphs, ["implementation", "version"]);
  member(subgraphs.implementation, ["Hot Chocolate"]);
  text(subgraphs.version, /^[A-Za-z0-9.+_-]{1,80}$/);

  if (!Array.isArray(report.scenarios) || report.scenarios.length !== 3) {
    throw new Error("Expected exactly three benchmark scenarios.");
  }
  const scenarioIds = new Set<unknown>();
  for (const value of report.scenarios) {
    const scenario = record(value, [
      "id",
      "status",
      "durationSeconds",
      "virtualUsers",
      "subgraphDelayMs",
      "warmupRuns",
      "measuredRuns",
      "results",
    ]);
    member(scenario.id, ["constant-latency", "burst", "constant"]);
    if (scenarioIds.has(scenario.id)) {
      throw new Error("Duplicate benchmark scenario.");
    }
    scenarioIds.add(scenario.id);
    member(scenario.status, ["complete", "pending"]);
    number(scenario.durationSeconds, 1, 3600, true);
    number(scenario.virtualUsers, 1, 100_000, true);
    number(scenario.subgraphDelayMs, 0, 1000);
    number(scenario.warmupRuns, 1, 100, true);
    number(scenario.measuredRuns, 1, 100, true);
    if (
      !Array.isArray(scenario.results) ||
      scenario.results.length > Object.keys(GATEWAYS).length
    ) {
      throw new Error("Invalid benchmark result count.");
    }
    if (
      (scenario.status === "pending") !== (scenario.results.length === 0) ||
      (run.status === "complete" && scenario.status === "pending")
    ) {
      throw new Error("Benchmark scenario status does not match its results.");
    }
    const gateways = new Set<unknown>();
    for (const item of scenario.results) {
      const result = record(item, [
        "gateway",
        "version",
        "status",
        "medianRps",
        "bestRps",
        "worstRps",
        "cvPercent",
        "memory",
      ]);
      member(result.gateway, Object.keys(GATEWAYS));
      if (gateways.has(result.gateway)) {
        throw new Error("Duplicate gateway result.");
      }
      gateways.add(result.gateway);
      if (result.version !== null) {
        text(result.version, /^[A-Za-z0-9.+_-]{1,80}$/);
      }
      member(result.status, ["ok", "failed", "incompatible", "not-run"]);
      if (result.memory !== null) {
        const memory = record(result.memory, ["peakRssKiB", "artifactId"]);
        number(memory.peakRssKiB, 0, 68_719_476_736, true);
        number(memory.artifactId, 1, Number.MAX_SAFE_INTEGER, true);
        if (result.status !== "ok") {
          throw new Error(
            "Unsuccessful results must have null memory metrics.",
          );
        }
      }
      if (result.status === "ok") {
        text(result.version, /^[A-Za-z0-9.+_-]{1,80}$/);
        number(result.medianRps, 0, 100_000_000);
        number(result.bestRps, result.medianRps, 100_000_000);
        number(result.worstRps, 0, result.medianRps);
        number(result.cvPercent, 0, 10_000);
      } else if (
        [
          result.medianRps,
          result.bestRps,
          result.worstRps,
          result.cvPercent,
        ].some((metric) => metric !== null)
      ) {
        throw new Error("Unsuccessful results must have null metrics.");
      }
    }
  }
  return report as unknown as BenchmarkReport;
}

export function benchmarkSourceUrl(
  report: BenchmarkReport,
  file: string,
): string {
  return `${BENCHMARK_REPOSITORY}/blob/${report.run.reportCommit}/benchmark-runs/${report.run.number}/${file}`;
}
