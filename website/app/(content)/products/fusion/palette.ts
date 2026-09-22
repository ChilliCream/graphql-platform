/**
 * The console visuals' colour roles and the subgraph station roster they
 * share, so every visual on the page names the same subgraphs, languages and
 * specifications.
 *
 * Every colour role is a `CC.*` token or a `BRAND.*` accent (mixed with
 * opacity where a role needs a wash), so this module carries no colour of
 * its own.
 */

import { BRAND, CC } from "./tokens";

export const MC = {
  /** Ops-room floor: the page background, so a scene never darkens the page. */
  bg: CC.bg,
  /** Console plates: the site surface, lifted so a panel reads on the floor. */
  panel: `color-mix(in srgb, ${CC.surface} 92%, ${CC.heading})`,
  /** Plate edges and rules: the site's card border. */
  panelEdge: CC.cardBorder,
  /** Plotted lines, unlit links and rings: the brand slate at a wash. */
  line: `color-mix(in srgb, ${BRAND.slate} 45%, transparent)`,
  /** Plate lettering: the brightest ink on the site. */
  ink: CC.heading,
  /** Secondary lettering under it. */
  dim: CC.inkDim,
  /** Radar phosphor: sweep, lit stations, healthy telemetry. */
  phosphor: BRAND.teal,
  /** Tracked traffic: client signals and query blips. */
  signal: BRAND.cyan,
} as const;

export type StationSpec = "GraphQL Federation" | "Apollo Federation";

export interface Station {
  readonly name: string;
  readonly language: string;
  readonly spec: StationSpec;
  /** Position on the wall map, in percent of the map box. */
  readonly x: number;
  readonly y: number;
}

export const STATIONS: readonly Station[] = [
  {
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
    x: 18,
    y: 30,
  },
  {
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    x: 36,
    y: 74,
  },
  {
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    x: 64,
    y: 22,
  },
  {
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    x: 82,
    y: 62,
  },
  {
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    x: 52,
    y: 88,
  },
];

export interface Source {
  readonly name: string;
  readonly kind: "OpenAPI" | "gRPC";
}

export const SOURCES: readonly Source[] = [
  { name: "Payments", kind: "OpenAPI" },
  { name: "Inventory", kind: "gRPC" },
];

export const CLIENTS = ["Web", "Mobile", "Partner API"] as const;

/** Short spec tag for the cramped station plates. */
export function specTag(spec: StationSpec): string {
  return spec === "Apollo Federation" ? "APOLLO FED" : "GRAPHQL FED";
}
