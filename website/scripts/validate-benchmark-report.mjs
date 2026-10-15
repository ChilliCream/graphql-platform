import { readFileSync } from "node:fs";
import { parseBenchmarkReport } from "../src/helpers/benchmarkReport.ts";

const report = parseBenchmarkReport(
  readFileSync(
    new URL("../public/benchmarks/federation/latest.json", import.meta.url),
    "utf8",
  ),
);

console.log(
  `Validated federation benchmark report for run ${report.run.number}.`,
);
