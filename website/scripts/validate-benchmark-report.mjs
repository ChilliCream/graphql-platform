import { readFileSync } from "node:fs";
import {
  parseBenchmarkHistory,
  parseBenchmarkReport,
} from "../src/helpers/benchmarkReport.ts";

const report = parseBenchmarkReport(
  readFileSync(
    new URL("../src/data/federationBenchmarkReport.json", import.meta.url),
    "utf8",
  ),
);

console.log(
  `Validated federation benchmark report for run ${report.run.number}.`,
);

const history = parseBenchmarkHistory(
  readFileSync(
    new URL("../src/data/federationBenchmarkHistory.json", import.meta.url),
    "utf8",
  ),
);

console.log(
  `Validated federation benchmark history up to run ${history.latestRun.number}.`,
);
