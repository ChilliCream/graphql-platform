/**
 * Airport concept (prototype v5): the night-apron palette and the gate roster
 * every visual in this folder draws from, so the departures board, the flight
 * plan, the gate markings, the clearance strip and the boarding check name the
 * same subgraphs, languages and specifications.
 */

export const AP = {
  /** Apron at night, darker than the site surface. */
  bg: "#080d13",
  panel: "rgba(14,21,30,0.9)",
  panelEdge: "rgba(180,205,230,0.16)",
  /** Painted apron markings and taxiway edges. */
  paint: "rgba(200,220,240,0.22)",
  ink: "#d7e2ee",
  dim: "#7d90a5",
  /** Split-flap glyphs and gate signage. */
  amber: "#ffb020",
  /** Taxiway centre line, cleared plans, healthy gates. */
  taxi: "#3ddc97",
  /** Approach and runway lights, tracked aircraft. */
  approach: "#5cc8ff",
  /** Grounded plan, breaking change. */
  stop: "#ff6b6b",
  mono: "ui-monospace, SFMono-Regular, Menlo, monospace",
} as const;

export type GateSpec = "GraphQL Federation" | "Apollo Federation";

export interface Gate {
  /** Gate sign, e.g. "A1". */
  readonly stand: string;
  /** Catalog, Billing, Ordering, Shipping, Accounts. */
  readonly name: string;
  /** Language painted on the tail. */
  readonly language: string;
  readonly spec: GateSpec;
}

export const GATES: readonly Gate[] = [
  {
    stand: "A1",
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
  },
  { stand: "A2", name: "Billing", language: "Java", spec: "Apollo Federation" },
  { stand: "B1", name: "Ordering", language: "Go", spec: "GraphQL Federation" },
  {
    stand: "B2",
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
  },
  {
    stand: "C1",
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
  },
];

export interface GroundSource {
  readonly name: string;
  readonly kind: "OpenAPI" | "gRPC";
  /** What the ground vehicle carries onto the apron. */
  readonly cargo: string;
}

/** Not GraphQL servers: ground vehicles feeding the same gates. */
export const GROUND_SOURCES: readonly GroundSource[] = [
  { name: "Payments", kind: "OpenAPI", cargo: "OPENAPI DOCUMENT" },
  { name: "Inventory", kind: "gRPC", cargo: "GRPC DEFINITION" },
];

/** Aircraft on approach: the clients of the composite schema. */
export const CLIENTS = ["Web", "Mobile", "Partner API", "Agent"] as const;

/** Short marking for a cramped gate sign. */
export function specMark(spec: GateSpec): string {
  return spec === "Apollo Federation" ? "APOLLO FED" : "GRAPHQL FED";
}
