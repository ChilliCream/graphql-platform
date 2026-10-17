import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { registerHooks } from "node:module";
import test from "node:test";
import {
  benchmarkVariants,
  parseBenchmarkHistory,
} from "../../src/helpers/benchmarkReport.ts";

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
const { historySeries, historyTimeScale } =
  await import("../../src/components/benchmarks/benchmarkView.ts");
hooks.deregister();
const fixture = readFileSync(
  new URL("./fixtures/federationBenchmarkHistory.json", import.meta.url),
  "utf8",
);
const day = 86_400_000;

test("spans the full width with proportional spacing for a short history", () => {
  const start = Date.parse("2026-10-01T12:00:00Z");
  const scale = historyTimeScale([start, start + day, start + 4 * day]);
  assert.deepEqual(
    [scale.x(start), scale.x(start + day), scale.x(start + 4 * day)],
    [0, 25, 100],
  );
});
test("centers a single run without invalid coordinates", () => {
  const date = Date.parse("2026-10-01T12:00:00Z");
  const scale = historyTimeScale([date]);
  assert.deepEqual(
    { ticks: scale.ticks, position: scale.x(date) },
    { ticks: [date], position: 50 },
  );
});
test("uses ten-day ticks over six months", () => {
  const start = Date.parse("2026-04-17T12:00:00Z");
  const scale = historyTimeScale([start, start + 183 * day]);
  assert.ok(scale.ticks.length >= 18);
  assert.ok(
    scale.ticks
      .slice(1)
      .every((tick, index) => tick - scale.ticks[index] === 10 * day),
  );
});
test("retains repeated versions and all runs sorted by completion time", () => {
  const history = parseBenchmarkHistory(fixture);
  const scenario = structuredClone(history.scenarios[0]);
  const variant = scenario.variants[0];
  const base = variant.results[0];
  variant.results = Array.from({ length: 5 }, (_, index) => ({
    ...base,
    run: {
      number: index + 1,
      startedAt: "2026-09-01T00:00:00Z",
      completedAt: `2026-10-0${5 - index}T12:00:00Z`,
    },
  }));
  const series = historySeries(scenario, benchmarkVariants(history), false)[0];
  assert.deepEqual(
    series.points.map((point) => [point.runNumber, point.version]),
    [5, 4, 3, 2, 1].map((number) => [number, base.version]),
  );
});
test("allows empty result lists and start-time fallback", () => {
  const history = JSON.parse(fixture);
  history.scenarios[0].variants[0].results = [];
  const result = history.scenarios[0].variants[1].results[0];
  result.run.startedAt = result.run.completedAt;
  result.run.completedAt = null;
  const parsed = parseBenchmarkHistory(JSON.stringify(history));
  const series = historySeries(
    parsed.scenarios[0],
    benchmarkVariants(parsed),
    false,
  );
  assert.equal(series[0].points[0].timestamp, Date.parse(result.run.startedAt));
});
test("rejects malformed dates and missing subgraph metadata", () => {
  for (const mutate of [
    (result) => {
      result.run.completedAt = "2026-02-30T12:00:00Z";
    },
    (result) => {
      delete result.subgraphs;
    },
  ]) {
    const history = JSON.parse(fixture);
    mutate(history.scenarios[0].variants[0].results[0]);
    assert.throws(() => parseBenchmarkHistory(JSON.stringify(history)));
  }
});

test("all showcase histories validate and span their advertised ranges", () => {
  for (const [name, days, count] of [
    ["one-week", 7, 8],
    ["two-months", 61, 17],
    ["six-months", 183, 20],
    ["cross-year", 62, 17],
  ]) {
    const history = parseBenchmarkHistory(
      readFileSync(
        new URL(
          `../../public/benchmarks/history-${name}.json`,
          import.meta.url,
        ),
        "utf8",
      ),
    );
    const points = historySeries(
      history.scenarios[0],
      benchmarkVariants(history),
      false,
    )[0].points;
    assert.deepEqual(
      {
        count: points.length,
        days: (points.at(-1).timestamp - points[0].timestamp) / day,
      },
      { count, days },
    );
  }
});
test("includes years when requested for a cross-year axis", async () => {
  const { formatHistoryDate } =
    await import("../../src/components/benchmarks/benchmarkView.ts");
  assert.deepEqual(
    [
      formatHistoryDate(Date.parse("2025-12-31T12:00:00Z"), true),
      formatHistoryDate(Date.parse("2026-01-01T12:00:00Z"), true),
    ],
    ["31 Dec 2025", "1 Jan 2026"],
  );
});

test("distributes six-month samples evenly without a short final interval", () => {
  const history = parseBenchmarkHistory(
    readFileSync(
      new URL(
        "../../public/benchmarks/history-six-months.json",
        import.meta.url,
      ),
      "utf8",
    ),
  );
  const points = historySeries(
    history.scenarios[0],
    benchmarkVariants(history),
    false,
  )[0].points;
  const gaps = points
    .slice(1)
    .map((point, index) => (point.timestamp - points[index].timestamp) / day);
  assert.deepEqual(
    [points[0].runNumber, points.at(-1).runNumber, points.length],
    [1000, 1183, 20],
  );
  assert.ok(gaps.every((gap) => gap === 9 || gap === 10));
});
