import {
  BENCHMARK_DATA,
  BENCHMARK_PAGE,
} from "@/src/components/benchmarks/benchmarkRoutes";
import {
  orderedScenarios,
  SCENARIO_INFO,
} from "@/src/components/benchmarks/benchmarkView";
import {
  BENCHMARK_REPOSITORY,
  type BenchmarkHistory,
  type BenchmarkReport,
} from "@/src/helpers/benchmarkReport";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  ORGANIZATION_ID,
  type JsonLdNode,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

export const BENCHMARK_DATASET_ID = schemaId(BENCHMARK_PAGE.path, "dataset");

const RESULT_PATH = "scenarios[].results[].variants[]";

const VARIABLES = [
  {
    name: "Median requests per second",
    field: "throughput.medianRps",
    unitText: "requests/s",
  },
  { name: "p95 latency", field: "latencyMs.p95", unitText: "ms" },
  { name: "p99.9 latency", field: "latencyMs.p99_9", unitText: "ms" },
  { name: "Peak memory", field: "memory.peakRssKiB", unitText: "KiB" },
  {
    name: "Average CPU utilization",
    field: "cpu.avgUtilizationPercent",
    unitText: "%",
  },
  {
    name: "Successful requests",
    field: "requests.successful",
    unitText: "requests",
  },
  { name: "Failed requests", field: "requests.failed", unitText: "requests" },
] as const;

/** Returns the Dataset node describing the benchmark results and their JSON downloads. */
export function createBenchmarkDatasetNode(
  report: BenchmarkReport,
  history: BenchmarkHistory,
): JsonLdNode {
  const timestamps = history.scenarios
    .flatMap((scenario) => scenario.variants)
    .flatMap((variant) => variant.results)
    .map((result) => result.run.completedAt ?? result.run.startedAt)
    .filter((value): value is string => typeof value === "string")
    .toSorted((a, b) => Date.parse(a) - Date.parse(b));
  const completedAt = report.run.completedAt ?? report.run.startedAt;
  const gateways = new Intl.ListFormat("en", { type: "conjunction" }).format(
    report.gateways.map((gateway) => gateway.name),
  );
  const scenarios = new Intl.ListFormat("en", { type: "conjunction" }).format(
    orderedScenarios(report).map((scenario) => SCENARIO_INFO[scenario.id].name),
  );

  return {
    "@type": "Dataset",
    "@id": BENCHMARK_DATASET_ID,
    name: "GraphQL Federation Benchmark Results",
    description: `Throughput, latency, memory, CPU and reliability measurements for the GraphQL federation routers and gateways ${gateways}, benchmarked on the same hardware with the same subgraphs and GraphQL operation in the ${scenarios} scenarios. Contains the full results of the latest run and six months of per-run history.`,
    url: toAbsoluteUrl(BENCHMARK_PAGE.path),
    creator: schemaRef(ORGANIZATION_ID),
    publisher: schemaRef(ORGANIZATION_ID),
    license: "https://opensource.org/licenses/MIT",
    isAccessibleForFree: true,
    isBasedOn: BENCHMARK_REPOSITORY,
    inLanguage: "en",
    keywords: [
      "GraphQL",
      "GraphQL federation",
      "benchmark",
      "router",
      "gateway",
      "performance",
    ],
    ...(completedAt ? { dateModified: completedAt } : {}),
    ...(timestamps.length > 0
      ? { temporalCoverage: `${timestamps[0]}/${timestamps.at(-1)}` }
      : {}),
    measurementTechnique:
      "k6 load tests run against one router at a time on dedicated CPU cores, reporting the median of the measured runs after a warmup run.",
    variableMeasured: VARIABLES.map((variable) => ({
      "@type": "PropertyValue",
      name: variable.name,
      description: `JSON field ${RESULT_PATH}.${variable.field}`,
      unitText: variable.unitText,
    })),
    distribution: [
      {
        "@type": "DataDownload",
        name: BENCHMARK_DATA.report.title,
        description: `Every gateway variant, scenario, measurement, hardware and CPU allocation detail of benchmark run ${report.run.number}.`,
        encodingFormat: "application/json",
        contentUrl: toAbsoluteUrl(BENCHMARK_DATA.report.path),
      },
      {
        "@type": "DataDownload",
        name: BENCHMARK_DATA.history.title,
        description:
          "Per-run results of every gateway variant and scenario over the six months up to the latest run.",
        encodingFormat: "application/json",
        contentUrl: toAbsoluteUrl(BENCHMARK_DATA.history.path),
      },
    ],
  };
}
