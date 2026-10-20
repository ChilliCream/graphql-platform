import type { ScenarioId } from "@/src/helpers/benchmarkReport";

export const BENCHMARK_PAGE = {
  title: "GraphQL Federation Benchmarks",
  description:
    "Compare GraphQL federation router and gateway performance for Fusion, Apollo, Hive, Cosmo, Grafbase, Caliban, and feddi. See throughput, latency, memory and CPU usage, and hardware.",
  path: "/benchmarks/graphql-federation",
} as const;

export const BENCHMARK_SCENARIO_PATHS: Readonly<Record<ScenarioId, string>> = {
  "constant-latency": BENCHMARK_PAGE.path,
  constant: `${BENCHMARK_PAGE.path}/synthetic`,
  burst: `${BENCHMARK_PAGE.path}/burst-traffic`,
};
