/**
 * Palette and cast for the Blueprint concept (prototype v7).
 *
 * The concept reads the composite schema as an engineering drawing: white
 * line-work on blueprint blue, every scene a plate on a drafting sheet with
 * its own title block. The gateway is the main assembly, each subgraph is a
 * sub-assembly with a part number, the language its server is written in and
 * the federation specification its source schema is written to are stamps in
 * the title block, and a source that is not a GraphQL server is a bought-in
 * part that arrives with an adapter drawing.
 */

/** Sheet colours, shared by all six plates so the page reads as one drawing set. */
export const BP = {
  /** The blueprint paper itself. */
  paper: "#0a2137",
  /** A plate cut out of the paper, one shade deeper. */
  plate: "#071a2c",
  /** Minor grid squares. */
  grid: "rgba(206, 231, 255, 0.07)",
  /** Major grid squares, every fifth line. */
  gridMajor: "rgba(206, 231, 255, 0.13)",
  /** Line-work: outlines, frames, lettering. */
  ink: "#dbe9fb",
  /** Secondary lettering: field labels, notes. */
  inkDim: "#8fb2d6",
  /** Construction lines, hatching, anything not yet drawn. */
  inkFaint: "rgba(219, 233, 251, 0.3)",
  /** Dimension lines, leaders and balloons - the measuring layer. */
  dim: "#5ec8ef",
  /** An approved check: a passing tolerance, a safe operation. */
  ok: "#7ee0b8",
  /** A queried tolerance: risky, needs review. */
  query: "#f6c177",
  /** Red-line revision: a conflict, a rejected drawing, a breaking change. */
  redline: "#ff7a6b",
} as const;

/** The one lettering stack every plate is drafted in. */
export const DRAFT =
  "ui-monospace, SFMono-Regular, Menlo, Consolas, 'Liberation Mono', monospace";

export type SubgraphSpec = "GraphQL Federation" | "Apollo Federation";

export interface Part {
  /** Part number as it appears in the balloon and the parts list. */
  readonly no: string;
  /** Catalog, Billing, Ordering, Shipping, Accounts. */
  readonly name: string;
  /** The language the subgraph's server is written in. */
  readonly language: string;
  /** The specification its source schema is written to. */
  readonly spec: SubgraphSpec;
  /** The field this sub-assembly contributes to the dimension chain. */
  readonly feature: string;
}

/** The main assembly: the gateway every sub-assembly is drawn against. */
export const ASSEMBLY = {
  no: "ASSY-100",
  name: "Fusion Gateway",
  note: "Composite schema · distributed executor",
} as const;

/** The five GraphQL sub-assemblies, in parts-list order; C# is never first. */
export const PARTS: readonly Part[] = [
  {
    no: "P-201",
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
    feature: "product",
  },
  {
    no: "P-202",
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    feature: "price",
  },
  {
    no: "P-203",
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    feature: "order",
  },
  {
    no: "P-204",
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    feature: "eta",
  },
  {
    no: "P-205",
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    feature: "account",
  },
];

export type SourceKind = "OpenAPI" | "gRPC";

export interface BoughtInPart {
  readonly no: string;
  readonly name: string;
  readonly kind: SourceKind;
  /** The drawing that mates it to the main assembly. */
  readonly adapter: string;
  readonly feature: string;
}

/** Sources that are not GraphQL servers: bought-in parts with adapter drawings. */
export const BOUGHT_IN: readonly BoughtInPart[] = [
  {
    no: "B-311",
    name: "Payments",
    kind: "OpenAPI",
    adapter: "ADAPTER A",
    feature: "refund",
  },
  {
    no: "B-312",
    name: "Inventory",
    kind: "gRPC",
    adapter: "ADAPTER B",
    feature: "stock",
  },
];

/** The clients the composite schema is drawn for. */
export const CLIENTS = ["Web", "Mobile", "Partner API", "Agent"] as const;
