import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { parseBenchmarkReport } from "../../src/helpers/benchmarkReport.ts";

const fixture = readFileSync(
  new URL("../../public/benchmarks/federation/latest.json", import.meta.url),
  "utf8",
);

test("accepts the example report and preserves its source measurements", () => {
  const report = parseBenchmarkReport(fixture);
  assert.deepEqual(
    report.scenarios.map(({ id, status, measuredRuns, results }) => ({
      id,
      status,
      measuredRuns,
      count: results.length,
    })),
    [
      {
        id: "constant-latency",
        status: "complete",
        measuredRuns: 1,
        count: 14,
      },
      { id: "burst", status: "pending", measuredRuns: 1, count: 0 },
      { id: "constant", status: "complete", measuredRuns: 1, count: 14 },
    ],
  );
  assert.equal(
    report.scenarios[0].results.find((row) => row.gateway === "fusion")
      .medianRps,
    1940,
  );
  assert.deepEqual(
    report.scenarios[0].results.find((row) => row.gateway === "fusion").memory,
    { peakRssKiB: 375064, artifactId: 11969913443 },
  );
});

const invalidReports = [
  [
    "negative peak RSS",
    (report) => {
      report.scenarios[0].results[0].memory.peakRssKiB = -1;
    },
  ],
  [
    "fractional peak RSS",
    (report) => {
      report.scenarios[0].results[0].memory.peakRssKiB = 1.5;
    },
  ],
  [
    "oversized peak RSS",
    (report) => {
      report.scenarios[0].results[0].memory.peakRssKiB = 1e20;
    },
  ],
  [
    "memory without artifact provenance",
    (report) => {
      delete report.scenarios[0].results[0].memory.artifactId;
    },
  ],
  [
    "artifact paths instead of IDs",
    (report) => {
      report.scenarios[0].results[0].memory.artifactId = "../../workflow.yml";
    },
  ],
  [
    "unknown memory fields",
    (report) => {
      report.scenarios[0].results[0].memory.url = "https://example.com";
    },
  ],
  [
    "unknown fields",
    (report) => {
      report.script = "run me";
    },
  ],
  [
    "prototype fields",
    (report) => {
      Object.defineProperty(report.hardware, "__proto__", {
        value: {},
        enumerable: true,
      });
    },
  ],
  [
    "HTML in hardware",
    (report) => {
      report.hardware.cpu = "<script>alert(1)</script>";
    },
  ],
  [
    "shell syntax in versions",
    (report) => {
      report.scenarios[0].results[0].version = "$(touch /tmp/injected)";
    },
  ],
  [
    "paths in commit IDs",
    (report) => {
      report.run.reportCommit = "../../.github/workflows/publish.yml";
    },
  ],
  [
    "unknown gateways",
    (report) => {
      report.scenarios[0].results[0].gateway = "constructor";
    },
  ],
  [
    "duplicate gateways",
    (report) => {
      report.scenarios[0].results[1].gateway =
        report.scenarios[0].results[0].gateway;
    },
  ],
  [
    "duplicate scenarios",
    (report) => {
      report.scenarios[1].id = report.scenarios[0].id;
    },
  ],
  [
    "negative throughput",
    (report) => {
      report.scenarios[0].results[0].medianRps = -1;
    },
  ],
  [
    "numeric strings",
    (report) => {
      report.hardware.logicalCpus = "16";
    },
  ],
  [
    "inconsistent ranges",
    (report) => {
      report.scenarios[0].results[0].bestRps = 0;
    },
  ],
  [
    "failed rows with throughput",
    (report) => {
      report.scenarios[0].results[0].status = "incompatible";
    },
  ],
  [
    "successful rows without versions",
    (report) => {
      report.scenarios[0].results[0].version = null;
    },
  ],
  [
    "pending scenarios with rows",
    (report) => {
      report.scenarios[0].status = "pending";
    },
  ],
  [
    "completed reports with pending scenarios",
    (report) => {
      report.run.status = "complete";
    },
  ],
  [
    "impossible dates",
    (report) => {
      report.run.startedAt = "2026-02-30T00:00:00Z";
    },
  ],
  [
    "unsupported schema versions",
    (report) => {
      report.schemaVersion = 2;
    },
  ],
];

for (const [name, mutate] of invalidReports) {
  test(`rejects ${name}`, () => {
    const report = JSON.parse(fixture);
    mutate(report);
    assert.throws(() => parseBenchmarkReport(JSON.stringify(report)));
  });
}

test("rejects oversized and non-finite input", () => {
  assert.throws(() => parseBenchmarkReport(" ".repeat(262_145)));
  assert.throws(() =>
    parseBenchmarkReport(
      fixture.replace('"medianRps": 1945', '"medianRps": 1e999'),
    ),
  );
});

test("accepts an incompatible response without presenting it as zero throughput", () => {
  const report = JSON.parse(fixture);
  Object.assign(report.scenarios[0].results[0], {
    status: "incompatible",
    medianRps: null,
    bestRps: null,
    worstRps: null,
    cvPercent: null,
    memory: null,
  });
  assert.equal(
    parseBenchmarkReport(JSON.stringify(report)).scenarios[0].results[0]
      .medianRps,
    null,
  );
});

test("accepts missing memory without presenting it as zero", () => {
  const report = JSON.parse(fixture);
  report.scenarios[0].results[0].memory = null;
  assert.equal(
    parseBenchmarkReport(JSON.stringify(report)).scenarios[0].results[0].memory,
    null,
  );
});

test("rejects memory on an unsuccessful result", () => {
  const report = JSON.parse(fixture);
  Object.assign(report.scenarios[0].results[0], {
    status: "failed",
    medianRps: null,
    bestRps: null,
    worstRps: null,
    cvPercent: null,
  });
  assert.throws(
    () => parseBenchmarkReport(JSON.stringify(report)),
    /Unsuccessful results must have null memory/,
  );
});
