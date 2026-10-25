import assert from "node:assert/strict";
import test from "node:test";
import {
  benchmarkTechnology,
  matchesBenchmarkTechnology,
} from "../../src/components/benchmarks/benchmarkFilters.ts";
import { BENCHMARK_SCENARIO_PATHS } from "../../src/components/benchmarks/benchmarkRoutes.ts";

const entry = (name, runtime) => ({
  variant: {
    platform: { language: { name }, ...(runtime ? { runtime } : {}) },
  },
});
const dotnet = entry("C#", { name: ".NET", version: "11.0" });
const rust = entry("Rust");

test("uses runtime families without versions and language for native platforms", () => {
  assert.equal(benchmarkTechnology(dotnet), ".NET");
  assert.equal(benchmarkTechnology(rust), "Rust");
});
test("an empty technology selection includes all platforms", () => {
  assert.deepEqual(
    [dotnet, rust].map((value) => matchesBenchmarkTechnology(value, [])),
    [true, true],
  );
});
test("a single selected technology includes only that family", () => {
  assert.deepEqual(
    [dotnet, rust].map((value) => matchesBenchmarkTechnology(value, ["Rust"])),
    [false, true],
  );
});
test("multiple selected technologies include either family", () => {
  assert.deepEqual(
    [dotnet, rust].map((value) =>
      matchesBenchmarkTechnology(value, [".NET", "Rust"]),
    ),
    [true, true],
  );
});
test("scenario paths have a single default URL and two static alternatives", () => {
  assert.deepEqual(BENCHMARK_SCENARIO_PATHS, {
    "constant-latency": "/benchmarks/graphql-federation",
    constant: "/benchmarks/graphql-federation/synthetic",
    burst: "/benchmarks/graphql-federation/burst-traffic",
  });
});
