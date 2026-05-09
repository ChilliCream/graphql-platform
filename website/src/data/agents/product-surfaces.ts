// Six product surface tiles for Section 06. Each tile maps to one ChilliCream
// primitive that feeds the agent loop. Icon keys are matched to hand-rolled
// stroke icons in `ProductSurfaceTiles.tsx` so the data and the visual
// vocabulary stay decoupled.

export type ProductSurfaceIcon =
  | "mcp"
  | "hotchocolate"
  | "mocha"
  | "fusion"
  | "shake"
  | "tracing";

export interface ProductSurface {
  readonly key: ProductSurfaceIcon;
  readonly tag: string;
  readonly title: string;
  readonly body: string;
}

export const PRODUCT_SURFACES: readonly ProductSurface[] = [
  {
    key: "mcp",
    tag: "Endpoint",
    title: "Nitro MCP Server",
    body: "The endpoint your agent talks to. Schema-typed, scoped, audited.",
  },
  {
    key: "hotchocolate",
    tag: "Server",
    title: "Hot Chocolate",
    body: "Server framework with first-class agent telemetry. Every resolver, every span.",
  },
  {
    key: "mocha",
    tag: "Messaging",
    title: "Mocha",
    body: "Every command, query, and event observable. Replayable. Auditable.",
  },
  {
    key: "fusion",
    tag: "Federation",
    title: "Fusion",
    body: "Walk the supergraph. Know which subgraph owns each field, in code or at runtime.",
  },
  {
    key: "shake",
    tag: "Client",
    title: "Strawberry Shake",
    body: "Regenerated when the graph changes. Your clients stay in sync without a PR.",
  },
  {
    key: "tracing",
    tag: "Telemetry",
    title: "Distributed tracing",
    body: "Field-level latency, error origins, cost attribution. Federated by default.",
  },
];
