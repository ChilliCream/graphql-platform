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
    ["router-a", "router-a", "Vendor A", "16.6.8"],
    ["router-a-preview", "router-a", "Vendor A", "16.7.0-p.28"],
    ["router-a-preview-runtime", "router-a", "Vendor A", "16.7.0-p.28"],
    ["router-b", "router-b", "Vendor B", "0.3.5"],
    ["router-c", "router-c", "Vendor B", "2.15.2"],
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
const verdict = (rows) =>
  scenarioVerdict({ results: [{ variants: rows }] }, variants);
const summary = (value) =>
  value && {
    leader: value.leader.variant,
    runnerUp: value.runnerUp?.variant,
    leadPercent: Math.round(value.lead * 100),
    tied: value.tied,
  };

test("compares the fastest variant of each gateway", () => {
  assert.deepEqual(
    summary(
      verdict([
        result("router-a-preview-runtime", 1200),
        result("router-a-preview", 1199),
        result("router-a", 1100),
        result("router-b", 1000),
        result("router-c", 900),
      ]),
    ),
    {
      leader: "router-a-preview-runtime",
      runnerUp: "router-b",
      leadPercent: 20,
      tied: false,
    },
  );
});

test("retains ties within variance between different gateways", () => {
  const value = verdict([
    result("router-a-preview", 1005),
    result("router-a", 1004),
    result("router-b", 1000),
  ]);
  assert.equal(value.runnerUp.variant, "router-b");
  assert.equal(value.tied, true);
});

test("keeps different gateways from the same vendor separate", () => {
  const value = verdict([
    result("router-b", 1200),
    result("router-c", 1000),
    result("router-a", 900),
  ]);
  assert.equal(value.runnerUp.variant, "router-c");
});

test("excludes failed variants and results with failed requests", () => {
  const value = verdict([
    { variant: "router-a-preview", status: "failed" },
    {
      ...result("router-b", 1200),
      requests: { total: 100, successful: 99, failed: 1 },
    },
    result("router-a", 900),
  ]);
  assert.equal(value.leader.variant, "router-a");
  assert.equal(value.runnerUp, undefined);
});

test("does not treat another version as a competing gateway", () => {
  const value = verdict([
    result("router-a-preview", 1200),
    result("router-a", 1000),
  ]);
  assert.equal(value.runnerUp, undefined);
  assert.equal(value.tied, false);
});

test("returns no verdict when no gateway completed successfully", () => {
  assert.equal(verdict([{ variant: "router-a", status: "failed" }]), undefined);
});

for (const id of ["router-a", "router-b", "router-c"]) {
  test(`selects ${id} when it has the highest successful throughput`, () => {
    const rows = ["router-a", "router-b", "router-c"].map((variant) =>
      result(variant, variant === id ? 1200 : 1000),
    );
    assert.equal(verdict(rows).leader.variant, id);
  });
}
