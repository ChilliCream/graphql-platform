/**
 * Seeded synthetic generators. Each produces a steady baseline plus
 * scripted "events" — a traffic ramp, an error+latency spike, a new version marker —
 * so the looping demo tells a story instead of showing noise.
 */
import { mulberry32, uniform, normal, pick, type Rng } from "./rng";
import type {
  Client,
  InsightRow,
  LatencyHistogram,
  LatencyPoint,
  MonitoringData,
  SpanKind,
  ThroughputPoint,
  Trace,
  TraceSample,
  LatencyDistribution,
  VersionMarker,
} from "./types";

/** Fixed epoch, mirrors cloud2's story convention. */
export const NOW = Date.UTC(2026, 3, 25, 12);
const MINUTE = 60_000;

const HIST_EDGES = [0, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000];

const OP_NAMES = [
  "reviews",
  "products",
  "inventory",
  "accounts",
  "shipping",
  "catalog",
  "payments",
];
const SPAN_KINDS: SpanKind[] = [
  "server",
  "server",
  "server",
  "client",
  "internal",
];
const CLIENT_NAMES = ["Web", "Mobile", "Others"];

const smooth = (r: Rng, n: number, base: number, amp: number) => {
  // two sine components with random phase + light noise → organic baseline
  const ph1 = r() * Math.PI * 2;
  const ph2 = r() * Math.PI * 2;
  return Array.from({ length: n }, (_, i) => {
    const x = (i / n) * Math.PI * 2;
    const wave = Math.sin(x * 1.3 + ph1) * 0.6 + Math.sin(x * 2.7 + ph2) * 0.4;
    return base + amp * wave + normal(r, 0, amp * 0.12);
  });
};

/** Index window where the scripted error/latency spike happens. */
const spikeAt = (n: number) => ({
  start: Math.floor(n * 0.62),
  end: Math.floor(n * 0.78),
});

function bump(i: number, start: number, end: number, height: number) {
  if (i < start || i > end) return 0;
  const t = (i - start) / Math.max(1, end - start);
  return Math.sin(t * Math.PI) * height; // smooth 0→height→0
}

export function makeThroughput(
  r: Rng,
  n: number,
  startEpoch: number,
  opts: { base?: number; ramp?: number } = {},
): { points: ThroughputPoint[]; errors: ThroughputPoint[] } {
  const base = opts.base ?? 2600;
  const ramp = opts.ramp ?? 1100;
  const wave = smooth(r, n, base, base * 0.18);
  const { start, end } = spikeAt(n);
  const points: ThroughputPoint[] = [];
  const errors: ThroughputPoint[] = [];
  for (let i = 0; i < n; i++) {
    const opm = Math.max(40, wave[i] + (ramp * i) / n);
    // baseline 1–4% errors, spiking to ~12% during the beat
    const errFrac =
      uniform(r, 0.01, 0.04) + bump(i, start, end, 0.09) + (i > end ? 0.01 : 0);
    const totalCount = Math.round(opm);
    const totalCountWithError = Math.round(opm * errFrac);
    const epoch = startEpoch + i * MINUTE;
    points.push({
      epoch,
      opm: Math.round(opm),
      totalCount,
      totalCountWithError,
    });
    errors.push({
      epoch,
      opm: totalCountWithError,
      totalCount,
      totalCountWithError,
    });
  }
  return { points, errors };
}

export function makeLatency(
  r: Rng,
  n: number,
  startEpoch: number,
): LatencyPoint[] {
  const meanWave = smooth(r, n, 42, 12);
  const { start, end } = spikeAt(n);
  return Array.from({ length: n }, (_, i) => {
    const mean = Math.max(5, meanWave[i] + bump(i, start, end, 70));
    const p95 = mean * uniform(r, 3, 4.5);
    const p99 = p95 * uniform(r, 1.6, 2.4);
    return { epoch: startEpoch + i * MINUTE, mean, p95, p99 };
  });
}

export function makeHistogram(r: Rng): LatencyHistogram {
  // mass concentrated 10–100ms with a long tail
  const weights = [2, 14, 26, 22, 16, 9, 5, 3, 2, 1];
  const bins = HIST_EDGES.map((bin, i) => {
    const w = weights[i];
    const total = Math.round(w * uniform(r, 90, 140));
    const errFrac =
      uniform(r, 0.005, 0.02) + (i >= 6 ? uniform(r, 0.02, 0.08) : 0);
    const errorFrequency = Math.round(total * errFrac);
    return { bin, successFrequency: total - errorFrequency, errorFrequency };
  });
  return { bins, p95: 260 };
}

export function makeClients(r: Rng, count = 3): Client[] {
  const names = [...CLIENT_NAMES].slice(0, count);
  // power-law totals
  const totals = names.map((_, i) =>
    Math.round(9000 * Math.pow(0.62, i) + uniform(r, 0, 300)),
  );
  const max = Math.max(...totals);
  return names
    .map((name, i) => ({
      name,
      total: totals[i],
      impact: Math.round((totals[i] / max) * uniform(r, 70, 100)),
    }))
    .sort((a, b) => b.total - a.total);
}

const miniSeries = (r: Rng, len: number, base: number, amp: number) =>
  smooth(r, len, base, amp).map((v) => Math.max(0, v));

export function makeInsights(r: Rng, count = 7): InsightRow[] {
  const names = [...OP_NAMES];
  return Array.from({ length: count }, (_, i) => {
    const averageLatency = Math.round(uniform(r, 8, 320) * (1 + i * 0.15));
    const opm = Math.round(uniform(r, 60, 1800) * Math.pow(0.85, i));
    const errorRate = Math.max(0, normal(r, 0.02 + i * 0.006, 0.01));
    const impact = Math.round(uniform(r, 12, 100) * Math.pow(0.92, i));
    return {
      id: `op-${i}`,
      spanKind: pick(r, SPAN_KINDS),
      name: names[i % names.length],
      averageLatency,
      opm,
      errorRate,
      impact,
      latencySeries: miniSeries(r, 8, averageLatency, averageLatency * 0.35),
      throughputSeries: miniSeries(r, 8, opm, opm * 0.3),
    };
  }).sort((a, b) => b.impact - a.impact);
}

export function makeVersionMarker(
  r: Rng,
  n: number,
  startEpoch: number,
): VersionMarker {
  const minor = 10 + Math.floor(r() * 8);
  return {
    version: `v2.${minor}.0`,
    firstSeenAt: startEpoch + Math.floor(n * 0.74) * MINUTE,
  };
}

/**
 * Sampled traces for the trace-sample timeline: each a point in
 * (time, duration) space, colored by status. Mass sits at 8–140 ms with a tail; the
 * scripted degradation window slows things down and produces more errors.
 */
export function makeTraceSamples(seed = 1, n = 90): TraceSample[] {
  const r = mulberry32((seed ^ 0x9e3779b9) >>> 0);
  const windowMs = 60 * MINUTE;
  const start = NOW - windowMs;
  return Array.from({ length: n }, (_, i) => {
    const frac = i / n;
    const epoch = start + frac * windowMs + uniform(r, 0, windowMs / n);
    // log-uniform base duration, 8–140 ms
    let durationMs = Math.exp(uniform(r, Math.log(8), Math.log(140)));
    const inSpike = frac > 0.62 && frac < 0.78;
    if (inSpike) durationMs *= uniform(r, 2, 6);
    if (r() < 0.06) durationMs *= uniform(r, 3, 12); // occasional slow outlier
    const errProb = (inSpike ? 0.18 : 0.03) + (durationMs > 500 ? 0.2 : 0);
    const status = r() < errProb ? "error" : "ok";
    return { id: `t-${i}`, epoch, durationMs: Math.round(durationMs), status };
  });
}

/**
 * Fine latency-distribution histogram over a log duration axis (1ms → 10s), with a sharp
 * low-latency peak + long sparse tail, errors concentrated in the tail. For the Operation
 * Detail "Latency Distribution" panel.
 */
export function makeLatencyDistribution(
  seed = 1,
  nbins = 48,
): LatencyDistribution {
  const r = mulberry32((seed ^ 0x85ebca6b) >>> 0);
  const min = 1;
  const max = 10000;
  const lmin = Math.log10(min);
  const lmax = Math.log10(max);
  const peakLog = Math.log10(18); // mode ~18ms
  const bins: LatencyDistribution["bins"] = [];
  let total = 0;
  for (let i = 0; i < nbins; i++) {
    const l0 = lmin + ((lmax - lmin) * i) / nbins;
    const l1 = lmin + ((lmax - lmin) * (i + 1)) / nbins;
    const center = Math.pow(10, (l0 + l1) / 2);
    const g = Math.exp(-Math.pow((Math.log10(center) - peakLog) / 0.34, 2) / 2);
    // tall narrow peak + a sparse, noisy tail
    let count = g * 2600;
    if (center > 60) count += Math.max(0, r() < 0.55 ? r() * 16 : 0);
    count = Math.round(count * uniform(r, 0.8, 1.15));
    const errFrac = center > 250 ? uniform(r, 0.05, 0.25) : uniform(r, 0, 0.02);
    const error = Math.round(count * errFrac);
    const success = Math.max(0, count - error);
    total += success + error;
    bins.push({ x0: Math.pow(10, l0), x1: Math.pow(10, l1), success, error });
  }
  return { bins, total, p95: 260, current: 42, min, max };
}

/**
 * A single GraphQL request trace, as nested spans for the waterfall (flamegraph).
 * Hand-shaped to mirror the reference (root /graphql → query → parse / http post → ...).
 */
export function makeTrace(seed = 1): Trace {
  const r = mulberry32((seed ^ 0xc2b2ae35) >>> 0);
  const totalMs = +uniform(r, 7, 9).toFixed(1);
  const j = (v: number) => +(v * uniform(r, 0.95, 1.05)).toFixed(2);
  // All spans nest within [0, totalMs]; none START past ~38% so their left-anchored
  // labels never overflow the panel. Two sibling HTTP fetches — the 2nd is the slow one.
  const f1 = totalMs * 0.3; // userById fetch starts ~here
  const f2 = totalMs * 0.36; // products fetch (slow) starts ~here
  const spans: Trace["spans"] = [
    {
      id: "s0",
      name: "/graphql",
      kind: "server",
      startMs: 0,
      durationMs: totalMs,
      depth: 0,
    },
    {
      id: "s1",
      name: "query GetHomePageQuery { userById products }",
      kind: "graphql",
      startMs: j(0.12),
      durationMs: j(totalMs - 0.3),
      depth: 1,
    },
    {
      id: "s2",
      name: "Parse request",
      kind: "internal",
      startMs: j(0.15),
      durationMs: 0.05,
      depth: 2,
    },
    {
      id: "s3",
      name: "HTTP POST users",
      kind: "http",
      startMs: j(0.5),
      durationMs: j(f1 + 1.4 - 0.5),
      depth: 2,
    },
    {
      id: "s4",
      name: "/graphql",
      kind: "server",
      startMs: j(0.62),
      durationMs: j(f1 + 1.2 - 0.62),
      depth: 3,
    },
    {
      id: "s5",
      name: "query { userById }",
      kind: "graphql",
      startMs: j(0.68),
      durationMs: j(f1 + 1.1 - 0.68),
      depth: 4,
    },
    {
      id: "s6",
      name: "HTTP POST products",
      kind: "http",
      startMs: j(f2),
      durationMs: j(totalMs - 0.6 - f2),
      depth: 2,
    },
    {
      id: "s7",
      name: "/graphql",
      kind: "server",
      startMs: j(f2 + 0.12),
      durationMs: j(totalMs - 0.85 - f2),
      depth: 3,
    },
    {
      id: "s8",
      name: "query { products }",
      kind: "graphql",
      startMs: j(f2 + 0.18),
      durationMs: j(totalMs - 0.95 - f2),
      depth: 4,
    },
  ];
  return { spans, totalMs };
}

/** Aggregate everything the Monitoring Overview needs from one seed. */
export function makeMonitoringData(seed = 1, n = 60): MonitoringData {
  const r = mulberry32(seed);
  const windowMs = n * MINUTE;
  const startEpoch = NOW - windowMs;
  const { points: throughput, errors } = makeThroughput(r, n, startEpoch);
  const latency = makeLatency(r, n, startEpoch);
  const histogram = makeHistogram(r);
  const clients = makeClients(r, 3);
  const insights = makeInsights(r, 7);
  const versionMarker = makeVersionMarker(r, n, startEpoch);

  const requests = throughput.reduce((s, p) => s + p.totalCount, 0);
  const withError = throughput.reduce((s, p) => s + p.totalCountWithError, 0);
  return {
    now: NOW,
    windowMs,
    throughput,
    latency,
    errors,
    histogram,
    clients,
    insights,
    versionMarker,
    totals: {
      opm: throughput[throughput.length - 1].opm,
      errorRate: withError / requests,
      p95: Math.round(latency.reduce((s, p) => s + p.p95, 0) / latency.length),
      requests,
    },
  };
}
