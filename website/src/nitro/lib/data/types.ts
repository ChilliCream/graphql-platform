/**
 * Synthetic telemetry shapes — mirror the real GraphQL fragments by eye.
 * No GraphQL, no network: these are plain value types produced by the seeded generators.
 */

export interface ThroughputPoint {
  epoch: number;
  /** operations per minute */
  opm: number;
  totalCount: number;
  totalCountWithError: number;
}

export interface LatencyPoint {
  epoch: number;
  /** milliseconds */
  mean: number;
  p95: number;
  p99: number;
}

export interface HistogramBin {
  /** lower edge of the latency bin, in ms */
  bin: number;
  successFrequency: number;
  errorFrequency: number;
}

export interface LatencyHistogram {
  bins: HistogramBin[];
  /** aggregate p95 marker, in ms */
  p95: number;
}

export interface Client {
  name: string;
  total: number;
  /** 0..100 */
  impact: number;
}

export type SpanKind =
  | "server"
  | "client"
  | "internal"
  | "producer"
  | "consumer";

export interface InsightRow {
  id: string;
  spanKind: SpanKind;
  name: string;
  /** ms */
  averageLatency: number;
  opm: number;
  /** 0..1 */
  errorRate: number;
  /** 0..100 */
  impact: number;
  /** mini series for inline sparklines */
  latencySeries: number[];
  throughputSeries: number[];
}

export interface VersionMarker {
  version: string;
  firstSeenAt: number;
}

export type TraceStatus = "ok" | "error";

/** One sampled trace, for the trace-sample timeline scatter. */
export interface TraceSample {
  id: string;
  epoch: number;
  /** total trace duration, ms */
  durationMs: number;
  status: TraceStatus;
}

/** One bar of the fine latency-distribution histogram (log duration axis). */
export interface DistributionBin {
  /** bin lower/upper edge, ms */
  x0: number;
  x1: number;
  success: number;
  error: number;
}

export interface LatencyDistribution {
  bins: DistributionBin[];
  total: number;
  /** ms */
  p95: number;
  /** ms — the "current" marker */
  current: number;
  min: number;
  max: number;
}

export type SpanKindWf = "server" | "graphql" | "http" | "internal";

/** One span of a trace waterfall (flamegraph). */
export interface TraceSpan {
  id: string;
  name: string;
  kind: SpanKindWf;
  /** start offset from trace start, ms */
  startMs: number;
  durationMs: number;
  /** nesting depth (row) */
  depth: number;
}

export interface Trace {
  spans: TraceSpan[];
  totalMs: number;
}

/** Everything the Monitoring Overview demo needs, from one seed. */
export interface MonitoringData {
  now: number;
  windowMs: number;
  throughput: ThroughputPoint[];
  latency: LatencyPoint[];
  errors: ThroughputPoint[];
  histogram: LatencyHistogram;
  clients: Client[];
  insights: InsightRow[];
  versionMarker: VersionMarker;
  totals: {
    opm: number;
    errorRate: number;
    p95: number;
    requests: number;
  };
}
