import exampleReport from "@/src/data/federationBenchmarkReport.json";
import { parseBenchmarkReport } from "@/src/helpers/benchmarkReport";

export const federationBenchmarks = parseBenchmarkReport(
  JSON.stringify(exampleReport),
);
