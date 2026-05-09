// Five stages of the Nitro agent loop. Each stage corresponds to a ChilliCream
// primitive (see .work/analysis/04-agents.md §5). The order is fixed:
// Observe → Reason → Act → Compose → Ship. The page-level diagram reads this
// list left-to-right and maps each stage to a circle node + label.

export interface LoopStage {
  readonly key: string;
  readonly label: string;
  readonly primitive: string;
  readonly body: string;
}

export const LOOP_STAGES: readonly LoopStage[] = [
  {
    key: "observe",
    label: "Observe",
    primitive: "Nitro telemetry · Mocha topology",
    body: "Traces, metrics, logs, and the live messaging topology stream into Nitro from every owning service.",
  },
  {
    key: "reason",
    label: "Reason",
    primitive: "MCP server · graph schema",
    body: "The MCP surface exposes the federated schema, code references, and team conventions as typed tools.",
  },
  {
    key: "act",
    label: "Act",
    primitive: "Hot Chocolate · Mocha · Fusion",
    body: "Resolvers, CQRS commands, and Fusion routes are first-class objects the agent can read and propose changes to.",
  },
  {
    key: "compose",
    label: "Compose",
    primitive: "Fusion composition",
    body: "Fusion stitches the change across services, validates field ownership, and runs breaking-change detection in CI.",
  },
  {
    key: "ship",
    label: "Ship",
    primitive: "Strawberry Shake · OpenAPI · MCP",
    body: "Clients regenerate. OpenAPI updates. The MCP tool surface widens. The loop closes without a single hand-edit.",
  },
];
