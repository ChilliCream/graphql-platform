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
const {
  benchmarkPageDescription,
  isShownVariant,
  releasedGateways,
  scenarioGroups,
} = await import("../../src/components/benchmarks/benchmarkView.ts");
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

test("shows every variant of each gateway, fastest first within each composition", () => {
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
      "fusion-nightly-net-10",
      "fusion-net-10",
      "hive-gateway-router-runtime",
      "hive-gateway",
      "fusion-fed-net-10",
    ],
  );
});
const catalog = (gateways) => ({
  gateways: gateways.map(([id, name, previews]) => ({
    id,
    name,
    variants: previews.map((preview, index) => ({
      id: `${id}-${index}`,
      version: "1.0.0",
      ...(preview ? { preview } : {}),
      platform: { language: { name: "Test" } },
    })),
  })),
});
test("counts a gateway as released when any of its variants is not a preview", () => {
  const report = catalog([
    ["router-a", "Router A", [true, false]],
    ["router-b", "Router B", [true]],
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
        ["apollo-gateway", "Apollo Gateway", [false]],
        ["apollo-router", "Apollo Router", [false]],
        ["hive-router", "Hive Router", [false]],
        ["router-b", "Router B", [true]],
        ["fusion", "Fusion", [false]],
      ]),
    ),
    "Compare GraphQL federation router performance: throughput, latency, memory and CPU for Fusion, Apollo, and Hive, benchmarked daily under identical conditions.",
  );
});

for (const composition of ["graphql-federation", "apollo-federation"]) {
  test(`shows all Fusion previews and only stable .NET 11 AOT for ${composition}`, () => {
    // arrange
    const fusion = (id, runtimeVersion, aot, preview) => ({
      gateway: { id: "fusion" },
      variant: {
        id,
        composition,
        ...(preview ? { preview } : {}),
        platform: {
          language: { name: "C#" },
          runtime: { name: ".NET", version: runtimeVersion },
          features: aot ? [{ id: "aot", name: "AOT" }] : [],
        },
      },
    });
    const entries = [
      fusion("fusion-aot-11", "11.0.0-rc.2", true, false),
      fusion("fusion-net-11", "11.0.0-rc.2", false, false),
      fusion("fusion-aot-10", "10.0.13", true, false),
      fusion("fusion-nightly-aot-11", "11.0.0-rc.2", true, true),
      fusion("fusion-nightly-net-11", "11.0.0-rc.2", false, true),
      fusion("fusion-nightly-aot-10", "10.0.13", true, true),
      fusion("fusion-nightly-net-10", "10.0.13", false, true),
      {
        gateway: { id: "hive-router" },
        variant: {
          id: "hive-router",
          platform: { language: { name: "Rust" } },
        },
      },
    ];

    // act
    const visibility = entries.map((entry) => [
      entry.variant.id,
      isShownVariant(entry),
    ]);

    // assert
    assert.deepEqual(visibility, [
      ["fusion-aot-11", true],
      ["fusion-net-11", false],
      ["fusion-aot-10", false],
      ["fusion-nightly-aot-11", true],
      ["fusion-nightly-net-11", true],
      ["fusion-nightly-aot-10", true],
      ["fusion-nightly-net-10", true],
      ["hive-router", true],
    ]);
  });
}
