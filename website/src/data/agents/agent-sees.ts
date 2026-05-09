// Six surfaces the agent has access to via the Nitro MCP endpoint. Order is
// stable across the page (data tile order = visual grid order, top-left to
// bottom-right). Tile keys are referenced by `WhatAgentSees` to pick a
// renderer for the per-tile mini visual.

export type AgentSeesKind =
  | "traces"
  | "metrics"
  | "logs"
  | "messaging"
  | "graph"
  | "code";

export interface AgentSeesTile {
  readonly key: AgentSeesKind;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
}

export const AGENT_SEES_TILES: readonly AgentSeesTile[] = [
  {
    key: "traces",
    eyebrow: "Distributed traces",
    title: "Every span across the federation.",
    body: "One waterfall, gateway plus every owning service. Field-level latency, every hop.",
  },
  {
    key: "metrics",
    eyebrow: "Metrics & perf",
    title: "p95 by resolver, by field.",
    body: "Allocations, hot fields, and per-tenant breakdowns. The agent reads them like a senior engineer.",
  },
  {
    key: "logs",
    eyebrow: "Logs",
    title: "Correlated to graph fields.",
    body: "Every log line carries the field path that emitted it. Search by trace, replay by request id.",
  },
  {
    key: "messaging",
    eyebrow: "Messaging topology",
    title: "Mocha publishers and subscribers.",
    body: "Commands, queries, events, and the topics that carry them. Topology, not just logs.",
  },
  {
    key: "graph",
    eyebrow: "The API graph",
    title: "Type → field → resolver.",
    body: "Walk the supergraph. Know which subgraph owns each field. Jump to source from any node.",
  },
  {
    key: "code",
    eyebrow: "Code references",
    title: "CQRS, REST, and resolvers.",
    body: "The agent reads your conventions before it suggests a single line of code. No greenfield drift.",
  },
];
