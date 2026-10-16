import history from "@/src/data/federationBenchmarkHistory.json";
import exampleReport from "@/src/data/federationBenchmarkReport.json";
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
