import history from "@/src/data/federation-benchmark-history.json";
import exampleReport from "@/src/data/federation-benchmark-report.json";
import {
  parseBenchmarkHistory,
  parseBenchmarkReport,
} from "@/src/helpers/benchmarkReport";

export const federationBenchmarks = parseBenchmarkReport(
  JSON.stringify(exampleReport),
);
export const federationBenchmarkHistory = parseBenchmarkHistory(
  JSON.stringify(history),
);
