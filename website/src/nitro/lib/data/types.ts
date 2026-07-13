export interface ThroughputPoint {
  epoch: number;
  opm: number;
  totalCount: number;
  totalCountWithError: number;
}

export interface LatencyPoint {
  epoch: number;
  mean: number;
  p95: number;
  p99: number;
}

export interface HistogramBin {
  bin: number;
  successFrequency: number;
  errorFrequency: number;
}

export interface LatencyHistogram {
  bins: HistogramBin[];
  p95: number;
}

export interface Client {
  name: string;
  total: number;
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
  averageLatency: number;
  opm: number;
  errorRate: number;
  impact: number;
  latencySeries: number[];
  throughputSeries: number[];
}

export interface VersionMarker {
  version: string;
  firstSeenAt: number;
}

export type TraceStatus = "ok" | "error";

export interface TraceSample {
  id: string;
  epoch: number;
  durationMs: number;
  status: TraceStatus;
}

export interface DistributionBin {
  x0: number;
  x1: number;
  success: number;
  error: number;
}

export interface LatencyDistribution {
  bins: DistributionBin[];
  total: number;
  p95: number;
  current: number;
  min: number;
  max: number;
}

export type SpanKindWf = "server" | "graphql" | "http" | "internal";

export interface TraceSpan {
  id: string;
  name: string;
  kind: SpanKindWf;
  startMs: number;
  durationMs: number;
  depth: number;
}

export interface Trace {
  spans: TraceSpan[];
  totalMs: number;
}

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
