import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import {
  benchmarkRunId,
  parseBenchmarkHistory,
  benchmarkVariants,
  decisiveFeatures,
  formatPlatform,
  formatPlatformTag,
  formatRuntimeTag,
  gatewayLink,
  parseBenchmarkReport,
  variantName,
  isPreview,
} from "../../src/helpers/benchmarkReport.ts";

const historyFixture = readFileSync(
  new URL("./fixtures/federation-benchmark-history.json", import.meta.url),
  "utf8",
);

const fixture = readFileSync(
  new URL("./fixtures/federation-benchmark-report.json", import.meta.url),
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
    system: { threadIds: [0, 8], physicalCores: 1, hardwareThreads: 2 },
    gateway: {
      physicalCores: 2,
      hardwareThreads: 4,
      threadIds: [2, 3, 10, 11],
    },
    k6: { physicalCores: 1, hardwareThreads: 2, threadIds: [1, 9] },
    subgraphs: {
      physicalCores: 4,
      hardwareThreads: 8,
      threadIds: [4, 5, 6, 7, 12, 13, 14, 15],
    },
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
      cpu: { avgUtilizationPercent: 85.5, maxUtilizationPercent: 89.3 },
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
    "missing required fields",
    (report) => {
      delete report.run.sourceCommit;
    },
  ],
  [
    "numeric strings",
    (report) => {
      report.hardware.hardwareThreads = "16";
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
    "CPU allocations without hardware threads",
    (report) => {
      report.scenarios[0].config.cpuAllocation.k6.hardwareThreads = 0;
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
      firstResult(report).cpu.avgUtilizationPercent = 101;
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

test("lists only the platform features that tell sibling variants apart", () => {
  const dotnet11 = (id, features) => ({
    gateway: { id: "fusion" },
    variant: {
      id,
      composition: "composite-schema",
      platform: {
        language: { name: "C#" },
        runtime: { name: ".NET", version: "11.0.0" },
        features,
      },
    },
  });
  const runtimeAsync = { id: "runtime-async", name: "Runtime Async" };
  const aot = { id: "aot", name: "AOT" };
  const variants = new Map(
    [
      dotnet11("fusion-net-11", [runtimeAsync]),
      dotnet11("fusion-aot-11", [runtimeAsync, aot]),
    ].map((entry) => [entry.variant.id, entry]),
  );
  const features = decisiveFeatures(variants);
  assert.deepEqual(
    [...variants.values()].map(({ variant }) =>
      formatPlatformTag(variant.platform, features.get(variant.id)),
    ),
    [".NET 11", ".NET 11 AOT"],
  );
});

test("accepts a report with only the fields the schema requires", () => {
  const report = parse((report) => {
    report.run = {
      number: 1,
      startedAt: null,
      sourceCommit: report.run.sourceCommit,
      event: report.run.event,
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
        status: "complete",
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
      features: [{ id: "query-plan-cache", name: "Query Plan Cache" }],
    });
    Object.assign(firstResult(report).latencyMs, { p99: 250 });
    report.scenarios[0].config.cpuAllocation = {
      gateway: { threadIds: [2, 3, 10, 11], hardwareThreads: 4 },
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
      ["Fusion", "/products/fusion"],
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
      variants: variants.map(({ variant, results }) => [
        variant,
        results.map(({ version, run, throughput }) => [
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
  history.scenarios[0].variants[0].results[0];

const invalidHistories = [
  [
    "results without timestamps",
    (history) => {
      delete firstHistoryVersion(history).run.completedAt;
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

test("accepts older hardware without topology", () => {
  const report = parse((report) => {
    delete report.hardware.physicalCores;
    delete report.hardware.threadsPerCore;
    delete report.hardware.coreThreads;
    for (const role of Object.values(
      report.scenarios[0].config.cpuAllocation,
    )) {
      delete role.physicalCores;
    }
  });
  assert.equal(report.hardware.hardwareThreads, 16);
  assert.equal(report.hardware.coreThreads, undefined);
});

for (const [name, mutate] of [
  [
    "partial topology",
    (report) => {
      delete report.hardware.threadsPerCore;
    },
  ],
  [
    "duplicate sibling threads",
    (report) => {
      report.hardware.coreThreads[0] = [0, 0];
    },
  ],
  [
    "duplicate role threads",
    (report) => {
      report.scenarios[0].config.cpuAllocation.system.threadIds = [0, 0];
    },
  ],
  [
    "CPU utilization above 100 percent",
    (report) => {
      firstResult(report).cpu.maxUtilizationPercent = 101;
    },
  ],
]) {
  test(`rejects ${name}`, () => {
    assert.throws(() => parse(mutate));
  });
}

test("uses explicit variant labels and preview metadata", () => {
  const entries = benchmarkVariants(parseBenchmarkReport(fixture));
  assert.deepEqual(
    ["fusion", "fusion-nightly", "fusion-nightly-net11", "feddi"].map((id) => {
      const entry = entries.get(id);
      return {
        id,
        name: variantName(entry),
        preview: isPreview(entry.variant),
      };
    }),
    [
      { id: "fusion", name: "Fusion", preview: false },
      { id: "fusion-nightly", name: "Fusion Nightly", preview: true },
      { id: "fusion-nightly-net11", name: "Fusion Nightly", preview: true },
      { id: "feddi", name: "feddi", preview: true },
    ],
  );
});
test("rejects preview metadata that is not boolean", () => {
  assert.throws(() =>
    parse((report) => {
      report.gateways[0].variants[0].preview = "true";
    }),
  );
});

test("accepts unknown fields throughout reports and histories", () => {
  const addUnknownFields = (value) => {
    if (Array.isArray(value)) {
      value.forEach(addUnknownFields);
    } else if (value !== null && typeof value === "object") {
      Object.values(value).forEach(addUnknownFields);
      value.futureField = { enabled: true };
    }
  };
  const report = JSON.parse(fixture);
  const history = JSON.parse(historyFixture);
  addUnknownFields(report);
  addUnknownFields(history);
  assert.deepEqual(parseBenchmarkReport(JSON.stringify(report)), report);
  assert.deepEqual(parseBenchmarkHistory(JSON.stringify(history)), history);
});

test("ignores scenario status values and accepts missing status", () => {
  const report = parse((report) => {
    delete report.scenarios[0].status;
    report.scenarios[1].status = "cancelled";
  });
  assert.equal(report.scenarios[0].status, undefined);
  assert.equal(report.scenarios[1].status, "cancelled");
});

test("accepts failed variants without measurements", () => {
  const report = parse((report) => {
    report.scenarios[0].results[0].variants[0] = {
      variant: firstResult(report).variant,
      status: "failed",
    };
  });
  assert.deepEqual(firstResult(report), {
    variant: "apollo-gateway",
    status: "failed",
  });
});
for (const [name, mutate] of [
  [
    "missing variant status",
    (report) => {
      delete firstResult(report).status;
    },
  ],
  [
    "unknown variant status",
    (report) => {
      firstResult(report).status = "pending";
    },
  ],
  [
    "failed variant with invalid measurements",
    (report) => {
      firstResult(report).status = "failed";
      firstResult(report).throughput.medianRps = -1;
    },
  ],
]) {
  test(`rejects ${name}`, () => {
    assert.throws(() => parse(mutate));
  });
}
test("accepts physical core counts without sibling mappings", () => {
  const report = parse((report) => {
    delete report.hardware.coreThreads;
  });
  assert.equal(report.hardware.physicalCores, 8);
});
test("accepts fewer runs than configured for a failed variant", () => {
  const report = parse((report) => {
    const variant = firstResult(report);
    variant.status = "failed";
    variant.throughput.runsRps = [
      variant.throughput.worstRps,
      variant.throughput.bestRps,
    ];
  });
  assert.deepEqual(firstResult(report).throughput.runsRps, [
    firstResult(report).throughput.worstRps,
    firstResult(report).throughput.bestRps,
  ]);
});
test("accepts the current generated report and history", () => {
  for (const [name, parser] of [
    ["federation-benchmark-report.json", parseBenchmarkReport],
    ["federation-benchmark-history.json", parseBenchmarkHistory],
  ]) {
    const json = readFileSync(
      new URL(`../../src/data/${name}`, import.meta.url),
      "utf8",
    );
    assert.deepEqual(parser(json), JSON.parse(json));
  }
});
