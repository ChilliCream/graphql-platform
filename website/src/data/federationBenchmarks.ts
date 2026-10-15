import exampleReport from "@/public/benchmarks/federation/latest.json";
import { parseBenchmarkReport } from "@/src/helpers/benchmarkReport";

export const federationBenchmarks = parseBenchmarkReport(
  JSON.stringify(exampleReport),
);
