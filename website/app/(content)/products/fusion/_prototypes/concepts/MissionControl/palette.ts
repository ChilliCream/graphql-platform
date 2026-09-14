/**
 * Mission Control concept (prototype v1): the shared ops-room palette and the
 * station roster every visual in this folder draws from, so the wall map, the
 * patch bay, the checklist and the flight recorder name the same subgraphs,
 * languages and specifications.
 *
 * The palette is a mapping of ops-room role -> site token: every entry is a
 * `CC.*` token, a `BRAND.*` accent (mixed with opacity where the scene needs a
 * wash) or `FONTS.mono`, so the concept carries no colour of its own.
 */

import { BRAND, CC, FONTS } from "../../brand";

export const MC = {
  /** Ops-room floor: the page background, so a scene never darkens the page. */
  bg: CC.bg,
  /** Console plates: the site surface, lifted so a panel reads on the floor. */
  panel: `color-mix(in srgb, ${CC.surface} 92%, ${CC.heading})`,
  /** Plate edges and rules: the site's card border. */
  panelEdge: CC.cardBorder,
  /** Wall-map grid: the brand cyan at a wash. */
  grid: `color-mix(in srgb, ${BRAND.cyan} 9%, transparent)`,
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
  /** Caution: risky change, held countdown. */
  amber: BRAND.amber,
  /** Abort: composition conflict, breaking change. */
  alert: BRAND.coral,
  mono: FONTS.mono,
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
