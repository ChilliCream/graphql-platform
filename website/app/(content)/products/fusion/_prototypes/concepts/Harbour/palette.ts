/**
 * Palette and cast for the Harbour concept (prototype v2).
 *
 * The concept reads the gateway as a harbour at dusk: clients are ships that
 * all dock at one pier, subgraphs are warehouses along the quay, composition
 * is the customs check every manifest passes before departure, and Nitro is
 * the harbour log of which ship actually carries which container.
 */

/** Dusk harbour colours, kept out of the visuals so the six scenes match. */
export const DUSK = {
  skyTop: "#0a0e1a",
  skyMid: "#16233d",
  skyGlow: "#4a2f4e",
  sun: "#f2a15c",
  water: "#0d1d31",
  waterDeep: "#091524",
  shimmer: "rgba(22, 185, 228, 0.55)",
  quay: "#1a2540",
  quayTop: "#243252",
  edge: "rgba(245, 241, 234, 0.14)",
  edgeBright: "rgba(245, 241, 234, 0.34)",
  hull: "#e7e2da",
  lamp: "#f6c177",
  accent: "#16b9e4",
  ok: "#5eead4",
  stop: "#f87171",
  ink: "#a1a3af",
  heading: "#f5f0ea",
} as const;

export type SubgraphSpec = "GraphQL Federation" | "Apollo Federation";

export interface Warehouse {
  /** Warehouse over the quay door: the subgraph name. */
  readonly name: string;
  /** Flag flown over the warehouse: the language the server is written in. */
  readonly language: string;
  /** Pennant next to the flag: the federation specification it is written to. */
  readonly spec: SubgraphSpec;
  /** Short pennant text; both specs are drawn the same way on purpose. */
  readonly pennant: string;
}

/** The five GraphQL warehouses along the quay, in berth order. */
export const WAREHOUSES: readonly Warehouse[] = [
  {
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
    pennant: "GraphQL Fed",
  },
  {
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    pennant: "Apollo Fed",
  },
  {
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    pennant: "GraphQL Fed",
  },
  {
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    pennant: "Apollo Fed",
  },
  {
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    pennant: "GraphQL Fed",
  },
];

export type SourceKind = "OpenAPI" | "gRPC";

export interface Source {
  readonly name: string;
  readonly kind: SourceKind;
}

/** Warehouses on the same quay that are not GraphQL servers at all. */
export const SOURCES: readonly Source[] = [
  { name: "Payments", kind: "OpenAPI" },
  { name: "Inventory", kind: "gRPC" },
];

export interface Ship {
  /** The client the ship stands for. */
  readonly name: string;
  /** Hull silhouette drawn for it. */
  readonly kind: "liner" | "speedboat" | "freighter" | "drone";
  /** Hull length in the 1200-wide harbour viewBox. */
  readonly length: number;
}

/** The four ships that dock at the one pier. */
export const SHIPS: readonly Ship[] = [
  { name: "Web", kind: "liner", length: 132 },
  { name: "Mobile", kind: "speedboat", length: 74 },
  { name: "Partner API", kind: "freighter", length: 118 },
  { name: "Agent", kind: "drone", length: 58 },
];

/** Mono label styling shared by the SVG captions in every Harbour scene. */
export const LABEL = {
  fontFamily:
    "ui-monospace, SFMono-Regular, Menlo, Consolas, 'Liberation Mono', monospace",
  letterSpacing: "0.12em",
} as const;
