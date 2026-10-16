import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import {
  benchmarkRunId,
  parseBenchmarkHistory,
  benchmarkVariants,
  formatPlatform,
  formatRuntimeTag,
  gatewayLink,
  parseBenchmarkReport,
  variantName,
} from "../../src/helpers/benchmarkReport.ts";

const historyFixture = readFileSync(
  new URL("../../src/data/federationBenchmarkHistory.json", import.meta.url),
  "utf8",
);

const fixture = readFileSync(
  new URL("../../src/data/federationBenchmarkReport.json", import.meta.url),
  "utf8",
);

const variant = (report, scenario, id) =>
  report.scenarios
    .find((item) => item.id === scenario)
    .results.flatMap((result) => result.variants)
    .find((item) => item.variant === id);

test("accepts the report and preserves its source measurements", () => {
  const report = parseBenchmarkReport(fixture);
  assert.deepEqual(
    report.scenarios.map(({ id, status, config, results }) => ({
      id,
      status,
      measuredRuns: config.measuredRuns,
      count: results.flatMap((result) => result.variants).length,
    })),
    [
      { id: "constant", status: "complete", measuredRuns: 9, count: 14 },
      {
        id: "constant-latency",
        status: "complete",
        measuredRuns: 9,
        count: 14,
      },
      { id: "burst", status: "complete", measuredRuns: 9, count: 13 },
    ],
  );
  assert.deepEqual(report.scenarios[1].config.cpuAllocation, {
    gateway: { cores: 4, cpuset: "2-3,10-11" },
    k6: { cores: 2, cpuset: "1,9" },
    subgraphs: { cores: 8, cpuset: "4-7,12-15" },
  });
  const fusion = variant(report, "constant-latency", "fusion");
  assert.deepEqual(
    {
      throughput: fusion.throughput.medianRps,
      p95: fusion.latencyMs.p95,
      memory: fusion.memory,
      cpu: fusion.cpu,
    },
    {
      throughput: 1886,
      p95: 32.076,
      memory: { peakRssKiB: 381300 },
      cpu: { avgCores: 3.42, maxCores: 3.57 },
    },
  );
  assert.deepEqual(
    {
      grafbase: variant(report, "burst", "grafbase").requests,
      feddi: variant(report, "burst", "feddi"),
    },
    {
      grafbase: { total: 1026823, successful: 704033, failed: 322790 },
      feddi: undefined,
    },
  );
});

test("derives the run ID and platform labels", () => {
  const report = parseBenchmarkReport(fixture);
  const platforms = Object.fromEntries(
    report.gateways
      .flatMap((gateway) => gateway.variants)
      .map((item) => [item.id, formatPlatform(item.platform)]),
  );
  assert.deepEqual(
    {
      runId: benchmarkRunId(report),
      fusion: platforms["fusion-nightly-net11"],
      router: platforms["apollo-router"],
      subgraphs: formatPlatform(report.subgraphs.platform),
    },
    {
      runId: "38806982970",
      fusion: ".NET 11.0.0-rc.2.26504.105",
      router: "Rust",
      subgraphs: ".NET 10.0.13",
    },
  );
});

const firstResult = (report) => report.scenarios[0].results[0].variants[0];

const parse = (mutate) => {
  const report = JSON.parse(fixture);
  mutate(report);
  return parseBenchmarkReport(JSON.stringify(report));
};

const invalidReports = [
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
    "missing required fields",
    (report) => {
      delete report.run.sourceCommit;
    },
  ],
  [
    "numeric strings",
    (report) => {
      report.hardware.logicalCpus = "16";
    },
  ],
  [
    "fractional integers",
    (report) => {
      firstResult(report).requests.total = 1.5;
    },
  ],
  [
    "commit IDs that are not hexadecimal",
    (report) => {
      report.run.sourceCommit = "../../.github/workflows/publish.yml";
    },
  ],
  [
    "run URLs that are not URIs",
    (report) => {
      report.run.url = "not a uri";
    },
  ],
  [
    "impossible dates",
    (report) => {
      report.run.startedAt = "2026-02-30T00:00:00Z";
    },
  ],
  [
    "dates without a time zone",
    (report) => {
      report.run.completedAt = "2026-10-16T10:24:10";
    },
  ],
  [
    "runs completed before they started",
    (report) => {
      report.run.completedAt = "2026-10-14T00:00:00Z";
    },
  ],
  [
    "versions with a v prefix",
    (report) => {
      report.gateways[0].variants[0].version = "v2.14.4";
    },
  ],
  [
    "empty labels",
    (report) => {
      report.gateways[0].variants[0].label = "";
    },
  ],
  [
    "unknown compositions",
    (report) => {
      report.gateways[0].variants[0].composition = "schema-stitching";
    },
  ],
  [
    "unknown scenario IDs",
    (report) => {
      report.scenarios[0].id = "soak";
    },
  ],
  [
    "unknown scenario statuses",
    (report) => {
      report.scenarios[0].status = "cancelled";
    },
  ],
  [
    "CPU sets without cores",
    (report) => {
      report.scenarios[0].config.cpuAllocation.k6.cores = 0;
    },
  ],
  [
    "gateways without variants",
    (report) => {
      report.gateways[0].variants = [];
    },
  ],
  [
    "duplicate gateway IDs",
    (report) => {
      report.gateways[1].id = report.gateways[0].id;
    },
  ],
  [
    "duplicate variant IDs",
    (report) => {
      report.gateways[0].variants.push(report.gateways[1].variants[0]);
    },
  ],
  [
    "results for unknown gateways",
    (report) => {
      report.scenarios[0].results[0].gateway = "constructor";
    },
  ],
  [
    "results for variants missing from the catalog",
    (report) => {
      report.gateways[1].variants.pop();
    },
  ],
  [
    "results under the wrong gateway",
    (report) => {
      firstResult(report).variant = "apollo-router";
    },
  ],
  [
    "duplicate gateway results",
    (report) => {
      report.scenarios[0].results[1].gateway =
        report.scenarios[0].results[0].gateway;
    },
  ],
  [
    "duplicate variant results",
    (report) => {
      const variants = report.scenarios[0].results[1].variants;
      variants[1] = structuredClone(variants[0]);
    },
  ],
  [
    "duplicate scenarios",
    (report) => {
      report.scenarios[1].id = report.scenarios[0].id;
    },
  ],
  [
    "result status fields",
    (report) => {
      firstResult(report).status = "ok";
    },
  ],
  [
    "results without throughput",
    (report) => {
      delete firstResult(report).throughput;
    },
  ],
  [
    "negative throughput",
    (report) => {
      firstResult(report).throughput.worstRps = -1;
    },
  ],
  [
    "a median outside the best and worst runs",
    (report) => {
      firstResult(report).throughput.medianRps = 1e6;
    },
  ],
  [
    "a best run that does not match the runs",
    (report) => {
      firstResult(report).throughput.bestRps += 1;
    },
  ],
  [
    "a run count that does not match the configuration",
    (report) => {
      firstResult(report).throughput.runsRps.pop();
    },
  ],
  [
    "unordered latency percentiles",
    (report) => {
      firstResult(report).latencyMs.p95 = 0;
    },
  ],
  [
    "average CPU above its maximum",
    (report) => {
      firstResult(report).cpu.avgCores = 99;
    },
  ],
  [
    "null memory",
    (report) => {
      firstResult(report).memory = null;
    },
  ],
];

for (const [name, mutate] of invalidReports) {
  test(`rejects ${name}`, () => {
    assert.throws(() => parse(mutate));
  });
}

test("rejects oversized and non-finite input", () => {
  assert.throws(() => parseBenchmarkReport(" ".repeat(262_145)));
  assert.throws(() =>
    parseBenchmarkReport(
      fixture.replace('"medianRps": 266', '"medianRps": 1e999'),
    ),
  );
});

test("formats runtime tags with the major version", () => {
  const report = parseBenchmarkReport(fixture);
  assert.deepEqual(
    Object.fromEntries(
      report.gateways
        .flatMap((gateway) => gateway.variants)
        .map((item) => [item.id, formatRuntimeTag(item.platform)]),
    ),
    {
      "apollo-gateway": "Node 22",
      "apollo-router": "Rust",
      "apollo-router-v3": "Rust",
      caliban: "JVM 25",
      cosmo: "Go 1.27",
      grafbase: "Rust",
      "hive-gateway": "Node 22",
      "hive-gateway-router-runtime": "Node 22",
      "hive-router": "Rust",
      fusion: ".NET 10",
      "fusion-nightly": ".NET 10",
      "fusion-nightly-net11": ".NET 11",
      "fusion-nightly-fed": ".NET 10",
      feddi: "JVM 25",
    },
  );
});

test("accepts a report with only the fields the schema requires", () => {
  const report = parse((report) => {
    report.run = {
      number: 1,
      startedAt: null,
      sourceCommit: report.run.sourceCommit,
    };
    delete report.subgraphs.framework.version;
    delete report.subgraphs.platform.runtime.version;
    for (const gateway of report.gateways) {
      delete gateway.vendor;
      delete gateway.repositoryUrl;
      for (const variant of gateway.variants) {
        variant.version = null;
        delete variant.label;
        delete variant.features;
        delete variant.platform.features;
      }
    }
    for (const scenario of report.scenarios) {
      delete scenario.description;
      scenario.config = { subgraphDelayMs: 0, warmupRuns: 0 };
      for (const result of scenario.results) {
        for (const variant of result.variants) {
          delete variant.throughput.runsRps;
          delete variant.latencyMs;
          delete variant.memory;
          delete variant.cpu;
        }
      }
    }
  });
  assert.deepEqual(
    {
      runId: benchmarkRunId(report),
      version: report.gateways[0].variants[0].version,
      runtime: formatRuntimeTag(report.subgraphs.platform),
      subgraphs: formatPlatform(report.subgraphs.platform),
      result: firstResult(report),
    },
    {
      runId: undefined,
      version: null,
      runtime: ".NET",
      subgraphs: ".NET",
      result: {
        variant: "apollo-gateway",
        throughput: {
          medianRps: 266,
          bestRps: 271,
          worstRps: 265,
          cvPercent: 0.7,
        },
        requests: { total: 290303, successful: 290303, failed: 0 },
      },
    },
  );
});

test("accepts every optional field the schema allows", () => {
  const report = parse((report) => {
    Object.assign(report.run, {
      startedAt: "2026-10-15T23:04:03.5+02:00",
      inputs: { gateways: "fusion", repeat: 3 },
    });
    Object.assign(report.gateways[0].variants[0], {
      label: "Apollo Gateway (custom)",
      notes: "Built from a fork.",
      features: [{ id: "query-plan-cache" }],
    });
    Object.assign(firstResult(report).latencyMs, { p99: 250 });
    report.scenarios[0].config.cpuAllocation = {
      gateway: { cpuset: "any value", cores: 4 },
    };
    report.scenarios[0].status = "in-progress";
    report.scenarios[1].status = "failed";
    report.scenarios[2] = {
      ...report.scenarios[2],
      status: "skipped",
      results: [],
    };
  });
  assert.deepEqual(
    {
      inputs: report.run.inputs,
      notes: report.gateways[0].variants[0].notes,
      p99: firstResult(report).latencyMs.p99,
      statuses: report.scenarios.map((scenario) => scenario.status),
    },
    {
      inputs: { gateways: "fusion", repeat: 3 },
      notes: "Built from a fork.",
      p99: 250,
      statuses: ["in-progress", "failed", "skipped"],
    },
  );
});

test("names and links unknown gateways from the report", () => {
  const report = parse((report) => {
    report.gateways.push({
      id: "new-router",
      name: "New Router",
      repositoryUrl: "https://github.com/example/new-router",
      variants: [
        {
          id: "new-router-preview",
          label: "New Router Preview",
          version: "0.1.0",
          composition: "apollo-federation",
          platform: { language: { name: "Zig" } },
        },
      ],
    });
    report.gateways.push({
      id: "unsafe-router",
      name: "Unsafe Router",
      repositoryUrl: "javascript:alert(1)",
      variants: [
        {
          id: "unsafe-router",
          version: "1.0.0",
          composition: "apollo-federation",
          platform: { language: { name: "C" } },
        },
      ],
    });
  });
  const variants = benchmarkVariants(report);
  assert.deepEqual(
    ["fusion", "new-router-preview", "unsafe-router"].map((id) => {
      const entry = variants.get(id);
      return [variantName(entry), gatewayLink(entry.gateway)];
    }),
    [
      ["Fusion", "/docs/fusion"],
      ["New Router Preview", "https://github.com/example/new-router"],
      ["Unsafe Router", undefined],
    ],
  );
});

test("accepts the history and keeps versions oldest first", () => {
  const history = parseBenchmarkHistory(historyFixture);
  assert.deepEqual(
    history.scenarios.map(({ id, variants }) => ({
      id,
      variants: variants.map(({ variant, versions }) => [
        variant,
        versions.map(({ version, run, throughput }) => [
          version,
          run.number,
          throughput.medianRps,
        ]),
      ]),
    }))[0],
    {
      id: "constant",
      variants: [
        [
          "apollo-router",
          [
            ["2.16.0", 397, 426],
            ["2.17.0", 398, 433],
            ["2.18.0", 400, 446],
          ],
        ],
        ["apollo-router-v3", [["3.0.0-preview.0", 400, 379]]],
        [
          "hive-router",
          [
            ["0.3.3", 397, 2388],
            ["0.3.4", 399, 2463],
            ["0.3.5", 400, 2501],
          ],
        ],
        [
          "fusion",
          [
            ["16.6.6", 397, 2304],
            ["16.6.7", 399, 2377],
            ["16.6.8", 400, 2413],
          ],
        ],
      ],
    },
  );
});

const firstHistoryVersion = (history) =>
  history.scenarios[0].variants[0].versions[0];

const invalidHistories = [
  [
    "schema versions",
    (history) => {
      history.schemaVersion = 1;
    },
  ],
  [
    "run completion times",
    (history) => {
      history.latestRun.completedAt = "2026-10-16T10:24:10Z";
    },
  ],
  [
    "version run completion times",
    (history) => {
      firstHistoryVersion(history).run.completedAt = "2026-10-16T10:24:10Z";
    },
  ],
  [
    "variants without versions",
    (history) => {
      history.scenarios[0].variants[0].versions = [];
    },
  ],
  [
    "variants under the wrong gateway",
    (history) => {
      history.scenarios[0].variants[0].gateway = "hive-router";
    },
  ],
  [
    "unknown gateways",
    (history) => {
      history.scenarios[0].variants[0].gateway = "constructor";
    },
  ],
  [
    "duplicate variants",
    (history) => {
      const { variants } = history.scenarios[0];
      variants.push(structuredClone(variants[0]));
    },
  ],
  [
    "a best run that does not match the runs",
    (history) => {
      firstHistoryVersion(history).throughput.bestRps += 1;
    },
  ],
  [
    "versions without throughput",
    (history) => {
      delete firstHistoryVersion(history).throughput;
    },
  ],
];

for (const [name, mutate] of invalidHistories) {
  test(`rejects histories with ${name}`, () => {
    const history = JSON.parse(historyFixture);
    mutate(history);
    assert.throws(() => parseBenchmarkHistory(JSON.stringify(history)));
  });
}
