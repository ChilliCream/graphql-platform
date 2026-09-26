import { CLIENTS, SOURCES, STATIONS, specTag } from "./palette";
import type { StationSpec } from "./palette";

export interface ClientNode {
  /** Roster name; the first three come from the palette's `CLIENTS`. */
  readonly key: string;
  readonly label: string;
  readonly detail: string;
}

export const CLIENT_NODES: readonly ClientNode[] = [
  { key: CLIENTS[0], label: "Web app", detail: "browser" },
  { key: CLIENTS[1], label: "Mobile app", detail: "iOS · Android" },
  { key: CLIENTS[2], label: "Partner API", detail: "server to server" },
  { key: "AI agent", label: "AI agent", detail: "tool call" },
];

const SUBGRAPH_LANGUAGE: Readonly<Record<string, string>> = {
  Catalog: "TypeScript",
  Billing: "Java",
  Ordering: "Go",
  Shipping: "Ruby",
  Accounts: "C#",
};

const SOURCE_LANGUAGE: Readonly<Record<string, string>> = {
  Payments: "Python",
  Inventory: "Go",
};

export interface TierNode {
  readonly name: string;
  readonly language: string;
  /** Spec badge for a subgraph, wire protocol for a non-GraphQL source. */
  readonly badge: string;
  /** `null` for the two sources, which implement no federation spec. */
  readonly spec: StationSpec | null;
}

/** Bottom tier, left to right: the five subgraphs, then the two sources. */
export const TIER_NODES: readonly TierNode[] = [
  ...STATIONS.map((station) => ({
    name: station.name,
    language: SUBGRAPH_LANGUAGE[station.name] ?? station.language,
    badge: specTag(station.spec, true),
    spec: station.spec as StationSpec | null,
  })),
  ...SOURCES.map((source) => ({
    name: source.name,
    language: SOURCE_LANGUAGE[source.name] ?? "",
    badge: source.kind,
    spec: null,
  })),
];

export const SPEC_LEGEND: readonly StationSpec[] = [
  "GraphQL Federation",
  "Apollo Federation",
];

export const COMPOSITE_LINE = `Coherent graph · ${STATIONS.length} subgraphs · OpenAPI and gRPC sources`;

export interface Request {
  /** Index into `CLIENT_NODES`. */
  readonly client: number;
  readonly operation: string;
  /** Names of the `TIER_NODES` this operation needs. */
  readonly targets: readonly string[];
}

export const REQUESTS: readonly Request[] = [
  {
    client: 0,
    operation: "query Storefront",
    targets: ["Catalog", "Accounts", "Inventory"],
  },
  {
    client: 1,
    operation: "query Checkout",
    targets: ["Catalog", "Ordering", "Billing"],
  },
  {
    client: 2,
    operation: "query Fulfillment",
    targets: ["Ordering", "Shipping"],
  },
  {
    client: 3,
    operation: "query AccountSummary",
    targets: ["Accounts", "Billing", "Payments"],
  },
];

/** Every request runs the same three phases, one cycle step each. */
export const PHASE_LABEL = ["Receive", "Fan out", "Merge"] as const;

export const PHASE_MS = 1200;

/** Clamps to the last request when a caller's script has fewer than two entries. */
export function restStep(requestCount: number): number {
  const index = Math.min(1, Math.max(0, requestCount - 1));
  return index * PHASE_LABEL.length + 1;
}

/** Grid gap between cards, in px; `gap-2` on both tiers. */
export const CARD_GAP = 8;

export type BandFlow = "to-gateway" | "from-gateway";

/** Height of the horizontal run inside a band, in percent. */
export const BUS_Y: Readonly<Record<BandFlow, number>> = {
  "to-gateway": 70,
  "from-gateway": 30,
};

/** Fusion's opt-in: both buses centred in their band, instead of BUS_Y's 70/30. */
export const BUS_Y_CENTERED: Readonly<Record<BandFlow, number>> = {
  "to-gateway": 50,
  "from-gateway": 50,
};

export type LaneSide = "left" | "right" | "center";

export interface Lane {
  readonly key: string;
  /** Centre of the grid column this lane serves, as a CSS length. */
  readonly x: string;
  readonly side: LaneSide;
  readonly lit: boolean;
}

/** Centre of grid column `index` of `columns`, accounting for the gaps. */
function laneX(index: number, columns: number, gap: number = CARD_GAP): string {
  const gaps = (columns - 1) * gap;
  return `calc((100% - ${gaps}px) / ${columns} * ${index + 0.5} + ${index * gap}px)`;
}

function laneSide(index: number, columns: number): LaneSide {
  const middle = (columns - 1) / 2;
  if (index < middle) return "left";
  if (index > middle) return "right";
  return "center";
}

/** `gap` must match that column's actual rendered gap so a connector still centres on its card. */
export function columnLanes(
  count: number,
  columns: number,
  lit: readonly number[],
  gap: number = CARD_GAP,
): readonly Lane[] {
  return Array.from({ length: Math.min(columns, count) }, (_, column) => ({
    key: `lane-${column}`,
    x: laneX(column, columns, gap),
    side: laneSide(column, columns),
    lit: lit.some((index) => index % columns === column),
  }));
}
