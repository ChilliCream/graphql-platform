/**
 * Mission Control concept (prototype v1): the shared ops-room palette and the
 * station roster every visual in this folder draws from, so the wall map, the
 * patch bay, the checklist and the flight recorder name the same subgraphs,
 * languages and specifications.
 */

export const MC = {
  /** Ops-room backdrop, darker than the site surface. */
  bg: "#050a12",
  panel: "rgba(10,18,32,0.86)",
  panelEdge: "rgba(150,200,225,0.16)",
  grid: "rgba(120,190,220,0.09)",
  line: "rgba(140,190,215,0.3)",
  ink: "#cbdae6",
  dim: "#728a9e",
  /** Radar phosphor: sweep, lit stations, healthy telemetry. */
  phosphor: "#4fe0bd",
  /** Tracked traffic: client signals and query blips. */
  signal: "#16b9e4",
  /** Caution: risky change, held countdown. */
  amber: "#f0b429",
  /** Abort: composition conflict, breaking change. */
  alert: "#ff6b6b",
  mono: "ui-monospace, SFMono-Regular, Menlo, monospace",
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
