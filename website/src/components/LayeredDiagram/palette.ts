import { BRAND, CC } from "./tokens";

export const MC = {
  bg: CC.bg,
  panel: `color-mix(in srgb, ${CC.surface} 92%, ${CC.heading})`,
  panelEdge: CC.cardBorder,
  line: `color-mix(in srgb, ${BRAND.slate} 45%, transparent)`,
  ink: CC.heading,
  dim: CC.inkDim,
  phosphor: BRAND.teal,
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

export function specTag(spec: StationSpec, short = false): string {
  if (short) return spec === "Apollo Federation" ? "APOLLO" : "GRAPHQL";
  return spec === "Apollo Federation" ? "APOLLO FED" : "GRAPHQL FED";
}

/** Fusion-only: the protocol a client enters the router with; every call still runs as a GraphQL query behind it. */
export type Protocol = "GraphQL" | "OpenAPI" | "MCP";

export function protocolTag(protocol: Protocol): string {
  return protocol.toUpperCase();
}

export function protocolColor(protocol: Protocol): string {
  if (protocol === "OpenAPI") return BRAND.amber;
  if (protocol === "MCP") return BRAND.coral;
  return MC.phosphor;
}
