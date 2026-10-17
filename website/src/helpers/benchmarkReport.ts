export const BENCHMARK_REPOSITORY =
  "https://github.com/ChilliCream/graphql-federation-benchmarks";

export const BENCHMARK_FUSION_DOCS_URL = "/docs/fusion";

const VARIANT_NAMES: Readonly<Record<string, string>> = {
  fusion: "Fusion",
  "fusion-nightly": "Fusion",
  "fusion-nightly-net11": "Fusion",
  "fusion-nightly-fed": "Fusion",
  "hive-router": "Hive Router",
  "hive-gateway": "Hive Gateway",
  "hive-gateway-router-runtime": "Hive Gateway · Router Runtime",
  "apollo-router": "Apollo Router",
  "apollo-router-v3": "Apollo Router",
  "apollo-gateway": "Apollo Gateway",
  cosmo: "Cosmo Router",
  grafbase: "Grafbase Gateway",
  feddi: "feddi",
  caliban: "Caliban",
};

const GATEWAY_LINKS: Readonly<Record<string, string>> = {
  fusion: BENCHMARK_FUSION_DOCS_URL,
  "hive-router": "https://github.com/graphql-hive/router",
  "hive-gateway": "https://github.com/graphql-hive/gateway",
  "apollo-router": "https://github.com/apollographql/router",
  "apollo-gateway": "https://github.com/apollographql/federation",
  cosmo: "https://github.com/wundergraph/cosmo",
  grafbase: "https://github.com/grafbase/grafbase",
  feddi: "https://github.com/feddi-dev/feddi-gateway",
  caliban: "https://github.com/ghostdogpr/caliban",
};

const RUNTIME_TAGS: Readonly<Record<string, string>> = {
  "Node.js": "Node",
};

export const COMPOSITIONS = {
  "apollo-federation": "Apollo Federation",
  "composite-schema": "GraphQL Federation",
} as const;

export type ScenarioId = "constant-latency" | "burst" | "constant";
export type Composition = keyof typeof COMPOSITIONS;

export interface BenchmarkFeature {
  readonly id: string;
  readonly description?: string;
}

export interface BenchmarkPlatform {
  readonly language: { readonly name: string };
  readonly runtime?: { readonly name: string; readonly version?: string };
  readonly features?: readonly BenchmarkFeature[];
}

export interface BenchmarkCpuSet {
  readonly physicalCores?: number;
  readonly hardwareThreads: number;
  readonly threadIds: readonly number[];
}

export interface BenchmarkVariant {
  readonly id: string;
  readonly version: string | null;
  readonly label?: string;
  readonly preview?: boolean;
  readonly composition: Composition;
  readonly platform: BenchmarkPlatform;
  readonly features?: readonly BenchmarkFeature[];
  readonly notes?: string;
}

export interface BenchmarkGateway {
  readonly id: string;
  readonly name: string;
  readonly vendor?: string;
  readonly repositoryUrl?: string;
  readonly variants: readonly BenchmarkVariant[];
}

export interface BenchmarkCompletedVariantResult {
  readonly status: "complete";
  readonly variant: string;
  readonly throughput: {
    readonly medianRps: number;
    readonly bestRps: number;
    readonly worstRps: number;
    readonly cvPercent: number;
    readonly runsRps?: readonly number[];
  };
  readonly requests: {
    readonly total: number;
    readonly successful: number;
    readonly failed: number;
  };
  readonly latencyMs?: {
    readonly avg: number;
    readonly min?: number;
    readonly med: number;
    readonly p90: number;
    readonly p95: number;
    readonly p99?: number;
    readonly p99_9: number;
    readonly max: number;
  };
  readonly memory?: { readonly peakRssKiB: number };
  readonly cpu?: {
    readonly avgUtilizationPercent: number;
    readonly maxUtilizationPercent: number;
  };
}

export type BenchmarkVariantResult =
  | BenchmarkCompletedVariantResult
  | ({ readonly variant: string; readonly status: "failed" } & Partial<
      Omit<BenchmarkCompletedVariantResult, "variant" | "status">
    >);

export interface BenchmarkScenario {
  readonly id: ScenarioId;
  readonly name: string;
  readonly description?: string;
  readonly config: {
    readonly durationSeconds?: number;
    readonly virtualUsers?: number;
    readonly subgraphDelayMs: number;
    readonly warmupRuns: number;
    readonly measuredRuns?: number;
    readonly cpuAllocation?: {
      readonly system?: BenchmarkCpuSet;
      readonly gateway?: BenchmarkCpuSet;
      readonly subgraphs?: BenchmarkCpuSet;
      readonly k6?: BenchmarkCpuSet;
    };
  };
  readonly results: readonly {
    readonly gateway: string;
    readonly variants: readonly BenchmarkVariantResult[];
  }[];
}

export interface BenchmarkReport {
  readonly run: {
    readonly number: number;
    readonly url?: string;
    readonly startedAt: string | null;
    readonly completedAt?: string;
    readonly sourceCommit: string;
    readonly event: string;
    readonly inputs?: Readonly<Record<string, unknown>>;
  };
  readonly hardware: {
    readonly host: string;
    readonly cpuModel: string;
    readonly physicalCores?: number;
    readonly hardwareThreads: number;
    readonly threadsPerCore?: number;
    readonly coreThreads?: readonly (readonly number[])[];
    readonly memoryGb: number;
    readonly os: string;
  };
  readonly subgraphs: {
    readonly framework: { readonly name: string; readonly version?: string };
    readonly platform: BenchmarkPlatform;
  };
  readonly gateways: readonly BenchmarkGateway[];
  readonly scenarios: readonly BenchmarkScenario[];
}

export interface BenchmarkHistoryRun {
  readonly number: number;
  readonly url?: string;
}

export type BenchmarkHistoryVersion = Omit<
  BenchmarkCompletedVariantResult,
  "variant" | "status"
> & {
  readonly version: string;
  readonly run: BenchmarkHistoryRun & {
    readonly startedAt?: string | null;
    readonly completedAt?: string | null;
  };
  readonly subgraphs: BenchmarkReport["subgraphs"];
  readonly platform: BenchmarkPlatform;
};

export interface BenchmarkHistoryScenario {
  readonly id: ScenarioId;
  readonly name: string;
  readonly config: BenchmarkScenario["config"];
  readonly variants: readonly {
    readonly variant: string;
    readonly gateway: string;
    readonly results: readonly BenchmarkHistoryVersion[];
  }[];
}

export interface BenchmarkHistory {
  readonly latestRun: BenchmarkHistoryRun;
  readonly gateways: readonly BenchmarkGateway[];
  readonly scenarios: readonly BenchmarkHistoryScenario[];
}

/** A catalog variant together with the gateway that declares it. */
export interface BenchmarkCatalogEntry {
  readonly gateway: BenchmarkGateway;
  readonly variant: BenchmarkVariant;
}

const RUN_URL =
  /^https:\/\/github\.com\/ChilliCream\/graphql-federation-benchmarks\/actions\/runs\/(\d{1,20})$/;
const DATE_TIME =
  /^(\d{4})-(\d{2})-(\d{2})[Tt](\d{2}):(\d{2}):(\d{2})(\.\d+)?([Zz]|[+-](\d{2}):(\d{2}))$/;

function record(
  value: unknown,
  required: readonly string[],
): Record<string, unknown> {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new Error("Expected a benchmark report object.");
  }
  const result = value as Record<string, unknown>;
  if (required.some((key) => !Object.hasOwn(result, key))) {
    throw new Error("Missing required benchmark report fields.");
  }
  return result;
}

function object(value: unknown): void {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new Error("Expected a benchmark report object.");
  }
}

function list(value: unknown, min = 0): unknown[] {
  if (!Array.isArray(value) || value.length < min) {
    throw new Error("Invalid benchmark report list.");
  }
  return value;
}

function number(
  value: unknown,
  min = -Number.MAX_VALUE,
  max = Number.MAX_VALUE,
): asserts value is number {
  if (
    typeof value !== "number" ||
    !Number.isFinite(value) ||
    value < min ||
    value > max
  ) {
    throw new Error("Invalid benchmark report number.");
  }
}

function integer(value: unknown, min: number): asserts value is number {
  number(value, min);
  if (!Number.isSafeInteger(value)) {
    throw new Error("Invalid benchmark report integer.");
  }
}

function text(value: unknown, pattern?: RegExp): asserts value is string {
  if (typeof value !== "string" || (pattern && !pattern.test(value))) {
    throw new Error("Invalid benchmark report text.");
  }
}

function optional<T>(value: unknown, check: (value: unknown) => T): void {
  if (value !== undefined) {
    check(value);
  }
}

function member(value: unknown, values: readonly unknown[]): void {
  if (!values.includes(value)) {
    throw new Error("Unknown benchmark report value.");
  }
}

function unique(seen: Set<unknown>, value: unknown): void {
  if (seen.has(value)) {
    throw new Error("Duplicate benchmark report identity.");
  }
  seen.add(value);
}

function uri(value: unknown): void {
  text(value);
  try {
    new URL(value);
  } catch {
    throw new Error("Invalid benchmark report URI.");
  }
}

function dateTime(value: unknown): number {
  text(value);
  const parts = DATE_TIME.exec(value);
  if (!parts) {
    throw new Error("Invalid benchmark report date.");
  }
  const [year, month, day, hour, minute, second] = parts
    .slice(1, 7)
    .map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));
  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day ||
    hour > 23 ||
    minute > 59 ||
    second > 60 ||
    (parts[9] !== undefined &&
      (Number(parts[9]) > 23 || Number(parts[10]) > 59))
  ) {
    throw new Error("Invalid benchmark report date.");
  }
  return Date.parse(value.replace(/:60(?=[.Zz+-])/, ":59"));
}

function features(value: unknown): void {
  for (const item of list(value)) {
    const feature = record(item, ["id"]);
    text(feature.id);
    optional(feature.description, text);
  }
}

function platform(value: unknown): void {
  const result = record(value, ["language"]);
  text(record(result.language, ["name"]).name);
  optional(result.runtime, (value) => {
    const runtime = record(value, ["name"]);
    text(runtime.name);
    optional(runtime.version, text);
  });
  optional(result.features, features);
}

function cpuSet(value: unknown): void {
  const set = record(value, ["threadIds", "hardwareThreads"]);
  const seen = new Set<unknown>();
  for (const id of list(set.threadIds, 1)) {
    integer(id, 0);
    unique(seen, id);
  }
  integer(set.hardwareThreads, 1);
  optional(set.physicalCores, (value) => integer(value, 1));
}

function measurement(
  value: Record<string, unknown>,
  runs: number | undefined,
): void {
  optional(value.throughput, (value) => {
    const throughput = record(value, [
      "medianRps",
      "bestRps",
      "worstRps",
      "cvPercent",
    ]);
    number(throughput.worstRps, 0);
    number(throughput.medianRps, throughput.worstRps);
    number(throughput.bestRps, throughput.medianRps);
    number(throughput.cvPercent, 0);
    optional(throughput.runsRps, (value) => {
      const samples = list(value);
      samples.forEach((sample) => number(sample, 0));
      if (
        (runs !== undefined && samples.length !== runs) ||
        (samples.length > 0 &&
          (Math.min(...(samples as number[])) !== throughput.worstRps ||
            Math.max(...(samples as number[])) !== throughput.bestRps))
      ) {
        throw new Error("Benchmark throughput does not match its runs.");
      }
    });
  });

  optional(value.requests, (value) => {
    const requests = record(value, ["total", "successful", "failed"]);
    integer(requests.total, 0);
    integer(requests.successful, 0);
    integer(requests.failed, 0);
  });

  optional(value.latencyMs, (value) => {
    const latency = record(value, ["avg", "med", "p90", "p95", "p99_9", "max"]);
    let previous = -Number.MAX_VALUE;
    for (const key of ["min", "med", "p90", "p95", "p99", "p99_9", "max"]) {
      if (latency[key] !== undefined) {
        number(latency[key], previous);
        previous = latency[key];
      }
    }
    number(
      latency.avg,
      (latency.min as number | undefined) ?? -Number.MAX_VALUE,
      previous,
    );
  });

  optional(value.memory, (value) => {
    integer(record(value, ["peakRssKiB"]).peakRssKiB, 0);
  });

  optional(value.cpu, (value) => {
    const cpu = record(value, [
      "avgUtilizationPercent",
      "maxUtilizationPercent",
    ]);
    number(cpu.maxUtilizationPercent, 0, 100);
    number(cpu.avgUtilizationPercent, 0, cpu.maxUtilizationPercent);
  });
}

function catalog(value: unknown): {
  readonly parents: ReadonlyMap<unknown, unknown>;
  readonly gatewayIds: ReadonlySet<unknown>;
} {
  const parents = new Map<unknown, unknown>();
  const gatewayIds = new Set<unknown>();
  for (const item of list(value)) {
    const gateway = record(item, ["id", "name", "variants"]);
    text(gateway.id);
    unique(gatewayIds, gateway.id);
    text(gateway.name);
    optional(gateway.vendor, text);
    optional(gateway.repositoryUrl, uri);
    for (const entry of list(gateway.variants, 1)) {
      const variant = record(entry, [
        "id",
        "version",
        "composition",
        "platform",
      ]);
      optional(variant.preview, (value) => {
        if (typeof value !== "boolean") {
          throw new Error("Invalid benchmark preview flag.");
        }
      });
      text(variant.id);
      if (parents.has(variant.id)) {
        throw new Error("Duplicate benchmark report identity.");
      }
      parents.set(variant.id, gateway.id);
      if (variant.version !== null) {
        text(variant.version, /^[^vV]/);
      }
      optional(variant.label, (value) => text(value, /^[\s\S]/));
      member(variant.composition, Object.keys(COMPOSITIONS));
      platform(variant.platform);
      optional(variant.features, features);
      optional(variant.notes, text);
    }
  }
  return { parents, gatewayIds };
}

function scenarioConfig(value: unknown): number | undefined {
  const config = record(value, ["subgraphDelayMs", "warmupRuns"]);
  optional(config.durationSeconds, (value) => integer(value, 0));
  optional(config.virtualUsers, (value) => integer(value, 1));
  number(config.subgraphDelayMs, 0);
  integer(config.warmupRuns, 0);
  optional(config.measuredRuns, (value) => integer(value, 1));
  optional(config.cpuAllocation, (value) => {
    const allocation = record(value, []);
    optional(allocation.system, cpuSet);
    optional(allocation.gateway, cpuSet);
    optional(allocation.subgraphs, cpuSet);
    optional(allocation.k6, cpuSet);
  });
  return config.measuredRuns as number | undefined;
}

/** Parses a benchmark report of at most 256 KiB and validates known fields while allowing unknown fields. */
export function parseBenchmarkReport(json: string): BenchmarkReport {
  if (new TextEncoder().encode(json).byteLength > 262_144) {
    throw new Error("Benchmark report exceeds 256 KiB.");
  }
  const report = record(JSON.parse(json), [
    "run",
    "hardware",
    "subgraphs",
    "gateways",
    "scenarios",
  ]);

  const run = record(report.run, [
    "number",
    "startedAt",
    "sourceCommit",
    "event",
  ]);
  integer(run.number, -Number.MAX_SAFE_INTEGER);
  optional(run.url, uri);
  const startedAt =
    run.startedAt === null ? undefined : dateTime(run.startedAt);
  const completedAt =
    run.completedAt === undefined ? undefined : dateTime(run.completedAt);
  if (
    startedAt !== undefined &&
    completedAt !== undefined &&
    completedAt < startedAt
  ) {
    throw new Error("Benchmark run completed before it started.");
  }
  text(run.sourceCommit, /^[0-9a-f]{40}$/);
  text(run.event, /^[\s\S]/);
  optional(run.inputs, object);

  const hardware = record(report.hardware, [
    "host",
    "cpuModel",
    "hardwareThreads",
    "memoryGb",
    "os",
  ]);
  text(hardware.host);
  text(hardware.cpuModel);
  text(hardware.os);
  integer(hardware.hardwareThreads, 1);
  number(hardware.memoryGb, 0);
  optional(hardware.physicalCores, (value) => integer(value, 1));
  optional(hardware.threadsPerCore, (value) => integer(value, 1));
  optional(hardware.coreThreads, (value) => {
    integer(hardware.physicalCores, 1);
    integer(hardware.threadsPerCore, 1);
    for (const core of list(value, 1)) {
      const seen = new Set<unknown>();
      for (const id of list(core, 1)) {
        integer(id, 0);
        unique(seen, id);
      }
    }
  });

  const subgraphs = record(report.subgraphs, ["framework", "platform"]);
  const framework = record(subgraphs.framework, ["name"]);
  text(framework.name);
  optional(framework.version, text);
  platform(subgraphs.platform);

  const { parents, gatewayIds } = catalog(report.gateways);

  const scenarioIds = new Set<unknown>();
  for (const value of list(report.scenarios)) {
    const scenario = record(value, ["id", "name", "config", "results"]);
    member(scenario.id, ["constant-latency", "burst", "constant"]);
    unique(scenarioIds, scenario.id);
    text(scenario.name);
    optional(scenario.description, text);
    const measuredRuns = scenarioConfig(scenario.config);

    const resultGateways = new Set<unknown>();
    const resultVariants = new Set<unknown>();
    for (const item of list(scenario.results)) {
      const result = record(item, ["gateway", "variants"]);
      text(result.gateway);
      if (!gatewayIds.has(result.gateway)) {
        throw new Error("Benchmark result references an unknown gateway.");
      }
      unique(resultGateways, result.gateway);
      for (const entry of list(result.variants, 1)) {
        const variant = record(entry, ["variant", "status"]);
        member(variant.status, ["complete", "failed"]);
        if (variant.status === "complete") {
          record(variant, ["throughput", "requests"]);
        }
        if (parents.get(variant.variant) !== result.gateway) {
          throw new Error("Benchmark result variant is not in its gateway.");
        }
        unique(resultVariants, variant.variant);
        measurement(variant, measuredRuns);
      }
    }
  }
  return report as unknown as BenchmarkReport;
}

function historyRun(value: unknown): void {
  const run = record(value, ["number"]);
  integer(run.number, -Number.MAX_SAFE_INTEGER);
  optional(run.url, uri);
}

/** Parses a benchmark history of at most 32 MiB and validates known fields while allowing unknown fields. */
export function parseBenchmarkHistory(json: string): BenchmarkHistory {
  if (new TextEncoder().encode(json).byteLength > 33_554_432) {
    throw new Error("Benchmark history exceeds 32 MiB.");
  }
  const history = record(JSON.parse(json), [
    "latestRun",
    "gateways",
    "scenarios",
  ]);
  historyRun(history.latestRun);
  const { parents, gatewayIds } = catalog(history.gateways);

  const scenarioIds = new Set<unknown>();
  for (const value of list(history.scenarios)) {
    const scenario = record(value, ["id", "name", "config", "variants"]);
    member(scenario.id, ["constant-latency", "burst", "constant"]);
    unique(scenarioIds, scenario.id);
    text(scenario.name);
    const measuredRuns = scenarioConfig(scenario.config);

    const variants = new Set<unknown>();
    for (const item of list(scenario.variants)) {
      const variant = record(item, ["variant", "gateway", "results"]);
      text(variant.gateway);
      if (!gatewayIds.has(variant.gateway)) {
        throw new Error("Benchmark history references an unknown gateway.");
      }
      if (parents.get(variant.variant) !== variant.gateway) {
        throw new Error("Benchmark history variant is not in its gateway.");
      }
      unique(variants, variant.variant);
      for (const entry of list(variant.results)) {
        const version = record(entry, [
          "version",
          "run",
          "platform",
          "throughput",
          "requests",
        ]);
        text(version.version);
        historyRun(version.run);
        const run = record(version.run, ["number"]);
        const startedAt =
          run.startedAt == null ? undefined : dateTime(run.startedAt);
        const completedAt =
          run.completedAt == null ? undefined : dateTime(run.completedAt);
        if (startedAt === undefined && completedAt === undefined) {
          throw new Error("Benchmark history result requires a timestamp.");
        }
        if (
          startedAt !== undefined &&
          completedAt !== undefined &&
          completedAt < startedAt
        ) {
          throw new Error("Benchmark history run completed before it started.");
        }
        const subgraphs = record(version.subgraphs, ["framework", "platform"]);
        const framework = record(subgraphs.framework, ["name"]);
        text(framework.name);
        optional(framework.version, text);
        platform(subgraphs.platform);
        const throughput = record(version.throughput, ["runsRps"]);
        list(throughput.runsRps, 1);
        platform(version.platform);
        measurement(version, measuredRuns);
      }
    }
  }
  return history as unknown as BenchmarkHistory;
}

/** Returns the numeric GitHub Actions run ID, or undefined when the run URL is not a run of the benchmark repository. */
export function benchmarkRunId(report: BenchmarkReport): string | undefined {
  return report.run.url ? RUN_URL.exec(report.run.url)?.[1] : undefined;
}

/** Returns each catalog variant with its gateway, keyed by variant ID. */
export function benchmarkVariants(
  report: Pick<BenchmarkReport, "gateways">,
): ReadonlyMap<string, BenchmarkCatalogEntry> {
  return new Map(
    report.gateways.flatMap((gateway) =>
      gateway.variants.map(
        (variant) => [variant.id, { gateway, variant }] as const,
      ),
    ),
  );
}

/** Returns the display name of a variant: its explicit label, otherwise the site name or gateway name. */
export function variantName({
  gateway,
  variant,
}: BenchmarkCatalogEntry): string {
  return variant.label ?? VARIANT_NAMES[variant.id] ?? gateway.name;
}

/** Returns the link for a gateway: the site link for known gateways, otherwise its HTTPS repository URL, or undefined. */
export function gatewayLink(gateway: BenchmarkGateway): string | undefined {
  if (Object.hasOwn(GATEWAY_LINKS, gateway.id)) {
    return GATEWAY_LINKS[gateway.id];
  }
  return gateway.repositoryUrl?.startsWith("https://")
    ? gateway.repositoryUrl
    : undefined;
}

const PRERELEASE = /^\d+(\.\d+)*-/;

/** Returns the explicit preview flag, or infers preview status from the gateway and runtime versions when absent. */
export function isPreview(
  version: string | null,
  platform: BenchmarkPlatform,
  preview: boolean | undefined,
): boolean {
  return (
    preview ??
    ((version !== null && PRERELEASE.test(version)) ||
      (platform.runtime?.version !== undefined &&
        PRERELEASE.test(platform.runtime.version)))
  );
}

/** Formats a platform as its runtime name and major version (major and minor for 0.x and 1.x), or its language when it has no runtime. */
export function formatRuntimeTag(platform: BenchmarkPlatform): string {
  if (!platform.runtime) {
    return platform.language.name;
  }
  const name = RUNTIME_TAGS[platform.runtime.name] ?? platform.runtime.name;
  if (!platform.runtime.version) {
    return name;
  }
  const [major, minor] = platform.runtime.version.split(/[.+-]/);
  return `${name} ${Number(major) < 2 && minor ? `${major}.${minor}` : major}`;
}

/** Formats a platform as its runtime name and version, or its language when it has no runtime. */
export function formatPlatform(platform: BenchmarkPlatform): string {
  if (!platform.runtime) {
    return platform.language.name;
  }
  return platform.runtime.version
    ? `${platform.runtime.name} ${platform.runtime.version}`
    : platform.runtime.name;
}
