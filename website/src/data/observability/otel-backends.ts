// OTEL-compatible backends shown inside the OTEL & integrations panel
// (Section 07). These are the tools Nitro federation traces drop into without
// glue code. Each tile renders as a single-letter stroke monogram with the
// backend name in monospace underneath: same vocabulary as Act5's brewers and
// EnterpriseHero's customer-segment monograms. We do NOT ship real brand
// assets, this is intentional.

export interface OtelBackend {
  readonly key: string;
  readonly letter: string;
  readonly name: string;
}

export const OTEL_BACKENDS: readonly OtelBackend[] = [
  { key: "jaeger", letter: "J", name: "Jaeger" },
  { key: "tempo", letter: "T", name: "Tempo" },
  { key: "datadog", letter: "D", name: "Datadog" },
  { key: "honeycomb", letter: "H", name: "Honeycomb" },
  { key: "grafana", letter: "G", name: "Grafana" },
  { key: "newrelic", letter: "N", name: "New Relic" },
];
