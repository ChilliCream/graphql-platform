import type { ScenarioId } from "@/src/helpers/benchmarkReport";

export const BENCHMARK_PAGE = {
  title: "GraphQL Federation Router Benchmark",
  path: "/benchmarks/graphql-federation",
} as const;

/** The short title on the benchmark page's share image. */
export const BENCHMARK_SHARE_TITLE = "GraphQL Federation Benchmark";

/** The published JSON files behind the benchmark page. */
export const BENCHMARK_DATA = {
  report: {
    title: "Latest GraphQL federation benchmark run",
    path: `${BENCHMARK_PAGE.path}/federation-benchmark-report.json`,
  },
  history: {
    title: "GraphQL federation benchmark history",
    path: `${BENCHMARK_PAGE.path}/federation-benchmark-history.json`,
  },
} as const;

export const BENCHMARK_SCENARIO_PATHS: Readonly<Record<ScenarioId, string>> = {
  "constant-latency": BENCHMARK_PAGE.path,
  constant: `${BENCHMARK_PAGE.path}/synthetic`,
  burst: `${BENCHMARK_PAGE.path}/burst-traffic`,
};
