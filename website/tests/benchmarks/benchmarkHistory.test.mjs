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
const {
  historySeries,
  historyTimeScale,
  historyDateTicks,
  latestCompletedPoint,
  seriesChange,
} = await import("../../src/components/benchmarks/benchmarkView.ts");
hooks.deregister();
const fixture = readFileSync(
  new URL("./fixtures/federation-benchmark-history.json", import.meta.url),
  "utf8",
);
const day = 86_400_000;

test("fits more run-date labels on wider charts at a consistent run interval", () => {
  const start = Date.parse("2026-07-22T12:00:00Z");
  const dates = [
    0, 1, 5, 8, 10, 14, 20, 29, 33, 36, 38, 40, 42, 49, 53, 57, 59, 61, 87,
  ].map((offset) => start + offset * day);
  const wide = historyDateTicks(dates, 1200);
  const narrow = historyDateTicks(dates, 260);
  assert.ok(wide.length >= 9);
  assert.ok(narrow.length < wide.length);
  for (const [ticks, width] of [
    [wide, 1200],
    [narrow, 260],
  ]) {
    assert.equal(ticks[0], dates[0]);
    assert.ok(ticks.every((tick) => dates.includes(tick)));
    const gaps = ticks
      .slice(1)
      .map((tick, index) => dates.indexOf(tick) - dates.indexOf(ticks[index]));
    assert.ok(Math.max(...gaps) === Math.min(...gaps));
    const bounds = ticks.map((tick, index) => {
      const x = (historyTimeScale(dates).x(tick) / 100) * width;
      return index === 0
        ? [0, 52]
        : tick === dates.at(-1)
          ? [width - 52, width]
          : [x - 26, x + 26];
    });
    assert.ok(
      bounds.slice(1).every(([left], index) => left - bounds[index][1] >= 16),
    );
  }
});

test("spaces recorded runs equally despite missing dates", () => {
  const start = Date.parse("2026-10-01T12:00:00Z");
  const scale = historyTimeScale([start, start + day, start + 4 * day]);
  assert.deepEqual(
    [scale.x(start), scale.x(start + day), scale.x(start + 4 * day)],
    [0, 50, 100],
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
test("sorts and deduplicates dates across series without adding missing dates", () => {
  const start = Date.parse("2026-07-22T12:00:00Z");
  const end = Date.parse("2026-10-17T12:00:00Z");
  const scale = historyTimeScale([end, start, start + day, end, start]);
  assert.deepEqual(scale.ticks, [start, start + day, end]);
  assert.deepEqual(scale.ticks.map(scale.x), [0, 50, 100]);
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
  const series = historySeries(scenario, benchmarkVariants(history))[0];
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
  const series = historySeries(parsed.scenarios[0], benchmarkVariants(parsed));
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
  const history = parseBenchmarkHistory(fixture);
  const scenario = structuredClone(history.scenarios[0]);
  const variant = scenario.variants[0];
  const base = variant.results[0];
  const start = Date.parse("2026-04-17T12:00:00Z");
  variant.results = Array.from({ length: 184 }, (_, index) => ({
    ...base,
    run: {
      number: 1000 + index,
      completedAt: new Date(start + index * day).toISOString(),
    },
  }));
  const points = historySeries(scenario, benchmarkVariants(history))[0].points;
  const gaps = points
    .slice(1)
    .map((point, index) => (point.timestamp - points[index].timestamp) / day);
  assert.deepEqual(
    [points[0].runNumber, points.at(-1).runNumber, points.length],
    [1000, 1183, 20],
  );
  assert.ok(gaps.every((gap) => gap === 9 || gap === 10));
});
test("keeps failed runs that fall between samples", () => {
  const history = parseBenchmarkHistory(fixture);
  const scenario = structuredClone(history.scenarios[0]);
  const variant = scenario.variants[0];
  const base = variant.results[0];
  const start = Date.parse("2026-04-17T12:00:00Z");
  variant.results = Array.from({ length: 184 }, (_, index) => ({
    ...base,
    ...(index === 5 ? { status: "failed" } : {}),
    run: {
      number: 1000 + index,
      completedAt: new Date(start + index * day).toISOString(),
    },
  }));
  const points = historySeries(scenario, benchmarkVariants(history))[0].points;
  assert.deepEqual(
    [
      points.filter((point) => point.failed).map((point) => point.runNumber),
      points.length,
    ],
    [[1005], 21],
  );
});
test("excludes failed runs from the change and latest completed point", () => {
  const history = parseBenchmarkHistory(fixture);
  const scenario = structuredClone(history.scenarios[0]);
  const variant = scenario.variants[0];
  const base = variant.results[0];
  variant.results = [100, 110, 50].map((rps, index) => ({
    ...base,
    ...(index === 2 ? { status: "failed" } : {}),
    run: { number: index + 1, completedAt: `2026-10-0${index + 1}T12:00:00Z` },
    throughput: { ...base.throughput, medianRps: rps },
  }));
  const series = historySeries(scenario, benchmarkVariants(history))[0];
  assert.deepEqual(
    [seriesChange(series), latestCompletedPoint(series).runNumber],
    [(110 - 100) / 100, 2],
  );
});
test("accepts failed history results with fewer runs than configured", () => {
  const history = JSON.parse(fixture);
  const result = history.scenarios[0].variants[0].results[0];
  result.status = "failed";
  result.throughput.runsRps = [
    result.throughput.worstRps,
    result.throughput.bestRps,
  ];
  const parsed = parseBenchmarkHistory(JSON.stringify(history));
  assert.equal(parsed.scenarios[0].variants[0].results[0].status, "failed");
});
test("rejects unknown history result statuses", () => {
  const history = JSON.parse(fixture);
  history.scenarios[0].variants[0].results[0].status = "complete";
  assert.throws(
    () => parseBenchmarkHistory(JSON.stringify(history)),
    /Unknown benchmark report value/,
  );
});
