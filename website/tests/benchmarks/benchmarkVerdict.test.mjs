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
const { scenarioVerdict } =
  await import("../../src/components/benchmarks/benchmarkView.ts");
hooks.deregister();

const variants = new Map(
  [
    ["fusion", "fusion", "Fusion", "16.6.8"],
    ["fusion-nightly", "fusion", "Fusion", "16.7.0-p.28"],
    ["fusion-nightly-net11", "fusion", "Fusion", "16.7.0-p.28"],
    ["hive-router", "hive-router", "The Guild", "0.3.5"],
    ["hive-gateway", "hive-gateway", "The Guild", "2.15.2"],
  ].map(([id, gateway, vendor, version]) => [
    id,
    {
      gateway: { id: gateway, vendor },
      variant: {
        id,
        version,
        composition: "composite-schema",
        platform: { language: { name: "Test" } },
      },
    },
  ]),
);

const result = (variant, medianRps, cvPercent = 1) => ({
  variant,
  status: "complete",
  throughput: { medianRps, cvPercent },
  requests: { total: 100, successful: 100, failed: 0 },
});
const verdict = (rows, stableOnly = false) =>
  scenarioVerdict({ results: [{ variants: rows }] }, variants, stableOnly);
const summary = (value) =>
  value && {
    leader: value.leader.variant,
    runnerUp: value.runnerUp?.variant,
    leadPercent: Math.round(value.lead * 100),
    tied: value.tied,
  };

test("compares the fastest Fusion variant with a different gateway", () => {
  assert.deepEqual(
    summary(
      verdict([
        result("fusion-nightly-net11", 1200),
        result("fusion-nightly", 1199),
        result("fusion", 1100),
        result("hive-router", 1000),
        result("hive-gateway", 900),
      ]),
    ),
    {
      leader: "fusion-nightly-net11",
      runnerUp: "hive-router",
      leadPercent: 20,
      tied: false,
    },
  );
});

test("respects the stable release filter before selecting a gateway's variant", () => {
  const rows = [
    result("fusion-nightly", 1200),
    result("fusion", 900),
    result("hive-router", 1000),
  ];
  assert.equal(verdict(rows, true).leader.variant, "hive-router");
  assert.equal(verdict(rows, true).runnerUp.variant, "fusion");
});

test("retains ties within variance between different gateways", () => {
  const value = verdict([
    result("fusion-nightly", 1005),
    result("fusion", 1004),
    result("hive-router", 1000),
  ]);
  assert.equal(value.runnerUp.variant, "hive-router");
  assert.equal(value.tied, true);
});

test("keeps different gateways from the same vendor separate", () => {
  const value = verdict([
    result("hive-router", 1200),
    result("hive-gateway", 1000),
    result("fusion", 900),
  ]);
  assert.equal(value.runnerUp.variant, "hive-gateway");
});

test("excludes failed variants and results with failed requests", () => {
  const value = verdict([
    { variant: "fusion-nightly", status: "failed" },
    {
      ...result("hive-router", 1200),
      requests: { total: 100, successful: 99, failed: 1 },
    },
    result("fusion", 900),
  ]);
  assert.equal(value.leader.variant, "fusion");
  assert.equal(value.runnerUp, undefined);
});

test("does not treat another version as a competing gateway", () => {
  const value = verdict([
    result("fusion-nightly", 1200),
    result("fusion", 1000),
  ]);
  assert.equal(value.runnerUp, undefined);
  assert.equal(value.tied, false);
});

test("returns no verdict when no gateway completed successfully", () => {
  assert.equal(verdict([{ variant: "fusion", status: "failed" }]), undefined);
});
