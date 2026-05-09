// Five pillars for /products/nitro/observability. One-liners pulled from the
// design brief (.work/analysis/03-observability.md §4 + §5) so the page reads
// as the canonical statement of what Nitro observability delivers. Each pillar
// also declares the plan(s) it ships in — this drives the inline plan chip
// underneath the section H2.

import type { PlanChipVariant } from "@/components/observability/PlanChip";

export type PillarKey =
  | "traces"
  | "errors"
  | "replay"
  | "schema-diffs"
  | "agents";

export interface Pillar {
  readonly key: PillarKey;
  readonly chips: readonly PlanChipVariant[];
  readonly eyebrow: string;
  readonly headline: string;
  readonly sub: string;
  readonly bullets: readonly string[];
}

export const PILLARS: readonly Pillar[] = [
  {
    key: "traces",
    chips: ["nitro", "oss"],
    eyebrow: "Federation-aware traces",
    headline: "Trace the whole graph.",
    sub: "One waterfall, every hop. Field-level latency from the gateway through every owning service.",
    bullets: [
      "Single trace across gateway and subgraphs",
      "Field-level p95 per resolver",
      "OpenTelemetry under the hood",
    ],
  },
  {
    key: "errors",
    chips: ["nitro"],
    eyebrow: "Origin-tagged errors",
    headline: "Errors with addresses.",
    sub: "Every error carries the field path that produced it. Click through to the resolver in the offending service.",
    bullets: [
      "Field path on every error",
      "Click-through to resolver source",
      "Per-service error budgets",
    ],
  },
  {
    key: "replay",
    chips: ["nitro"],
    eyebrow: "Query replay",
    headline: "Reproduce, don't speculate.",
    sub: "Capture a failing prod query, replay it against staging with the same headers, variables, identity. Same control surface, every environment.",
    bullets: [
      "Capture from any environment",
      "Replay with identity preserved",
      "Diff prod vs staging side-by-side",
    ],
  },
  {
    key: "schema-diffs",
    chips: ["fusion", "nitro"],
    eyebrow: "Schema diffs & audit",
    headline: "Catch breaking changes in CI.",
    sub: "Composition runs on every PR. Breaking-change detection with field-level diffs. Full audit log of every schema your gateway has ever served.",
    bullets: [
      "PR check on every commit",
      "Field-level breaking diff",
      "Immutable audit history",
    ],
  },
  {
    key: "agents",
    chips: ["nitro"],
    eyebrow: "MCP for agents",
    headline: "Your agents can read it too.",
    sub: "The same observability data, exposed through Nitro's MCP surface. Traces, schema diffs, replay, queryable by any MCP-aware agent.",
    bullets: [
      "Traces over MCP",
      "Schema diffs over MCP",
      "Replay queries over MCP",
    ],
  },
];
