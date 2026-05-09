// Four guardrails for Section 07. Each line maps to a real Nitro mechanism:
// schema validation, scoped tokens, the Mocha audit topic, sandboxed
// migrations. Tone: serious, ops-grade, no marketing fluff.

export type GuardrailIcon = "schema" | "token" | "audit" | "sandbox";

export interface Guardrail {
  readonly key: GuardrailIcon;
  readonly title: string;
  readonly body: string;
}

export const GUARDRAILS: readonly Guardrail[] = [
  {
    key: "schema",
    title: "Schema-typed responses",
    body: "No hallucinated fields. The agent's outputs are validated against the live federated schema before they leave Nitro.",
  },
  {
    key: "token",
    title: "Scoped tokens",
    body: "Per-agent identity, per-environment scope, per-resource RBAC. Same auth surface as your engineers.",
  },
  {
    key: "audit",
    title: "Audit log via Mocha",
    body: "Every tool call, query, and mutation is published to a Mocha topic. Replayable, exportable, no lossy summary.",
  },
  {
    key: "sandbox",
    title: "Sandbox-runnable migrations",
    body: "Schema changes run in a staging slice with replayed traffic before they touch the production graph.",
  },
];
