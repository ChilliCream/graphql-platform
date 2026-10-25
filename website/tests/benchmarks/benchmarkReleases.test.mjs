import assert from "node:assert/strict";
import { registerHooks } from "node:module";
import test from "node:test";

// Resolve the website alias when loading the view helpers outside Next.js.
const hooks = registerHooks({
  resolve(specifier, context, nextResolve) {
    return nextResolve(
      specifier === "@/src/helpers/benchmarkReport"
        ? new URL("../../src/helpers/benchmarkReport.ts", import.meta.url).href
        : specifier,
      context,
    );
  },
});
const { benchmarkPageDescription, releasedGateways, scenarioGroups } =
  await import("../../src/components/benchmarks/benchmarkView.ts");
hooks.deregister();

const variants = new Map(
  [
    ["fusion-net-10", "fusion", "16.6.9", "10.0.13", "composite-schema"],
    ["fusion-aot-11", "fusion", "16.6.9", "11.0.0-rc.2", "composite-schema"],
    [
      "fusion-nightly-net-10",
      "fusion",
      "16.7.0-p.38",
      "10.0.13",
      "composite-schema",
    ],
    ["fusion-fed-net-10", "fusion", "16.6.9", "10.0.13", "apollo-federation"],
    ["hive-gateway", "hive-gateway", "2.15.2", "24.0.0", "composite-schema"],
    [
      "hive-gateway-router-runtime",
      "hive-gateway",
      "2.15.2",
      "24.0.0",
      "composite-schema",
    ],
  ].map(([id, gateway, version, runtimeVersion, composition]) => [
    id,
    {
      gateway: { id: gateway, name: gateway },
      variant: {
        id,
        version,
        composition,
        platform: {
          language: { name: "Test" },
          runtime: { name: "Runtime", version: runtimeVersion },
        },
      },
    },
  ]),
);

const result = (variant, medianRps, failed = 0) => ({
  variant,
  status: "complete",
  throughput: { medianRps, cvPercent: 1 },
  requests: { total: 100, successful: 100 - failed, failed },
});
const shown = (rows) =>
  scenarioGroups({ results: [{ variants: rows }] }, variants)
    .flatMap((group) => group.rows)
    .map((row) => row.variant);

test("keeps the best preview and non-preview variant of each gateway per composition", () => {
  assert.deepEqual(
    shown([
      result("fusion-net-10", 1000),
      result("fusion-aot-11", 1200),
      result("fusion-nightly-net-10", 1100),
      result("fusion-fed-net-10", 900),
      result("hive-gateway", 400),
      result("hive-gateway-router-runtime", 500),
    ]),
    [
      "fusion-aot-11",
      "fusion-net-10",
      "hive-gateway-router-runtime",
      "fusion-fed-net-10",
    ],
  );
});
test("prefers a result without failed requests over a faster one with failures", () => {
  assert.deepEqual(
    shown([
      result("hive-gateway", 400),
      result("hive-gateway-router-runtime", 500, 5),
    ]),
    ["hive-gateway"],
  );
});
test("prefers a failed result with measurements over one without", () => {
  assert.deepEqual(
    shown([
      { variant: "hive-gateway", status: "failed" },
      { ...result("hive-gateway-router-runtime", 300), status: "failed" },
    ]),
    ["hive-gateway-router-runtime"],
  );
});

const catalog = (gateways) => ({
  gateways: gateways.map(([id, name, versions]) => ({
    id,
    name,
    variants: versions.map((version, index) => ({
      id: `${id}-${index}`,
      version,
      platform: { language: { name: "Test" } },
    })),
  })),
});
test("counts a gateway as released when any of its variants is not a preview", () => {
  const report = catalog([
    ["router-a", "Router A", ["16.7.0-p.39", "16.6.9"]],
    ["router-b", "Router B", ["0.4.0-rc.1"]],
  ]);
  assert.deepEqual(
    [
      ...releasedGateways(
        new Map(
          report.gateways.flatMap((gateway) =>
            gateway.variants.map((variant) => [
              variant.id,
              { gateway, variant },
            ]),
          ),
        ),
      ),
    ],
    ["router-a"],
  );
});
test("describes the page with released gateways, Fusion first and without generic suffixes", () => {
  assert.equal(
    benchmarkPageDescription(
      catalog([
        ["apollo-gateway", "Apollo Gateway", ["2.14.4"]],
        ["apollo-router", "Apollo Router", ["2.18.0"]],
        ["hive-router", "Hive Router", ["0.3.5"]],
        ["router-b", "Router B", ["0.4.0-rc.1"]],
        ["fusion", "Fusion", ["16.6.9"]],
      ]),
    ),
    "Compare GraphQL federation router performance: throughput, latency, memory and CPU for Fusion, Apollo, and Hive, benchmarked daily under identical conditions.",
  );
});
