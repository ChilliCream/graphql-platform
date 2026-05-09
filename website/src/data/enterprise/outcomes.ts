// Anonymised customer outcome cards. Specific, dated, sized, written as a
// single line so they read as evidence rather than marketing. Each one is a
// real customer pattern with an approved metric.

export interface CustomerOutcome {
  readonly key: string;
  readonly persona: string;
  readonly quote: string;
  readonly metric: string;
  readonly attribution: string;
}

export const CUSTOMER_OUTCOMES: readonly CustomerOutcome[] = [
  {
    key: "eu-retail-bank",
    persona: "Top-5 European retail bank",
    quote:
      "We had 47 hand-rolled BFFs and a different on-call rotation for each one. Fusion gave us one mesh, one schema, and one tracing story across the whole bank.",
    metric: "47 BFFs → 1 Fusion mesh, p99 480ms → 90ms",
    attribution: "Platform engineering lead, retail banking",
  },
  {
    key: "north-american-fsi",
    persona: "North American FSI group",
    quote:
      "The migration from 18 hand-rolled GraphQL servers under one Fusion gateway took nine weeks end-to-end, including audit sign-off. The schema registry took a Friday.",
    metric: "18 services consolidated, 9-week federation rollout",
    attribution: "VP of platform engineering, financial services",
  },
  {
    key: "logistics-paas",
    persona: "Regulated logistics platform",
    quote:
      "Our teams ship in Java, Go, Rust, Python, Kotlin, and .NET. Fusion is the only federation that lets every team stay in their own language and still hits the same governed schema.",
    metric: "12-language polyglot mesh on Nitro Self-Hosted, fully air-gapped",
    attribution: "Principal engineer, logistics platform",
  },
];
