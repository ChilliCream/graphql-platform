/**
 * Airport concept (prototype v5): the shared apron palette and the gate roster
 * every visual in this folder draws from, so the departures board, the flight
 * plan, the gate markings, the clearance strip and the boarding check name the
 * same subgraphs, languages and specifications.
 *
 * The palette is a mapping of apron role -> site token: every entry is a
 * `CC.*` token, a `BRAND.*` accent (mixed with opacity where the scene needs a
 * wash) or `FONTS.mono`, so the concept carries no colour of its own.
 */

import { BRAND, CC, FONTS } from "../../brand";

export const AP = {
  /** The apron itself: the page background, so a scene never darkens the page. */
  bg: CC.bg,
  /** Terminal plates and gate signs: the site surface, lifted to read on the apron. */
  panel: `color-mix(in srgb, ${CC.surface} 92%, ${CC.heading})`,
  /** Plate edges and sign frames: the site's card border. */
  panelEdge: CC.cardBorder,
  /** Painted apron markings and taxiway edges: the brand slate at a wash. */
  paint: `color-mix(in srgb, ${BRAND.slate} 45%, transparent)`,
  /** Faint light fill: board rows, sign plates, meter tracks. */
  wash: `color-mix(in srgb, ${CC.heading} 4%, transparent)`,
  /** The lit asphalt of the taxiway and the one runway. */
  deck: `color-mix(in srgb, ${CC.heading} 7%, transparent)`,
  /** Night sky over the concourse: the page colour with a cyan cast. */
  sky: `color-mix(in srgb, ${BRAND.cyan} 12%, ${CC.bg})`,
  /** Sign lettering: the brightest ink on the site. */
  ink: CC.heading,
  /** Secondary lettering under it. */
  dim: CC.inkDim,
  /** Split-flap glyphs and gate signage: the brand amber. */
  amber: BRAND.amber,
  /** Taxiway centre line, cleared plans, healthy gates: the brand teal. */
  taxi: BRAND.teal,
  /** Approach and runway lights, tracked aircraft: the brand cyan. */
  approach: BRAND.cyan,
  /** Grounded plan, breaking change: the brand coral. */
  stop: BRAND.coral,
  mono: FONTS.mono,
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
