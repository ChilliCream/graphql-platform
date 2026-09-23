import { CLIENTS, SOURCES, STATIONS, specTag } from "./palette";
import type { StationSpec } from "./palette";

/**
 * The three tiers the Layered Diagram hero draws, the request script it
 * replays, and the lane geometry both of its layouts share.
 */

export interface ClientNode {
  /** Roster name; the first three come from the palette's `CLIENTS`. */
  readonly key: string;
  /** Card title. */
  readonly label: string;
  /** How that client talks to the gateway. */
  readonly detail: string;
}

export const CLIENT_NODES: readonly ClientNode[] = [
  { key: CLIENTS[0], label: "Web app", detail: "browser" },
  { key: CLIENTS[1], label: "Mobile app", detail: "iOS · Android" },
  { key: CLIENTS[2], label: "Partner API", detail: "server to server" },
  { key: "AI agent", label: "AI agent", detail: "tool call" },
];

/** The language each subgraph is written in, spelled out for the card. */
const SUBGRAPH_LANGUAGE: Readonly<Record<string, string>> = {
  Catalog: "TypeScript",
  Billing: "Java",
  Ordering: "Go",
  Shipping: "Ruby",
  Accounts: "C#",
};

/** The language behind each non-GraphQL source. */
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
    badge: specTag(station.spec),
    spec: station.spec as StationSpec | null,
  })),
  ...SOURCES.map((source) => ({
    name: source.name,
    language: SOURCE_LANGUAGE[source.name] ?? "",
    badge: source.kind,
    spec: null,
  })),
];

/** Both specifications, spelled out under the badges the cards carry. */
export const SPEC_LEGEND: readonly StationSpec[] = [
  "GraphQL Federation",
  "Apollo Federation",
];

/** What the gateway readout says about the schema it serves. */
export const COMPOSITE_LINE = `Coherent graph · ${STATIONS.length} subgraphs · OpenAPI and gRPC sources`;

export interface Request {
  /** Index into `CLIENT_NODES`. */
  readonly client: number;
  readonly operation: string;
  /** Names of the `TIER_NODES` this operation needs. */
  readonly targets: readonly string[];
}

/** One operation per client, each needing a different two or three nodes of the bottom tier. */
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
/**
 * Rest frame: the Checkout fan-out. The server render, the reduced-motion
 * render and the off-screen render all show one client asking, the gateway
 * planning and the three subgraphs that query needs lit up.
 */
export const REST_STEP = 1 * PHASE_LABEL.length + 1;

/** Grid gap between cards, in px; `gap-2` on both tiers. */
export const CARD_GAP = 8;

/** Which tier a connector band joins to the gateway. */
export type BandFlow = "to-gateway" | "from-gateway";

/**
 * Height of the horizontal run inside a band, in percent: the lanes meet
 * above the gateway and one stem enters it, and the other way round below.
 */
export const BUS_Y: Readonly<Record<BandFlow, number>> = {
  "to-gateway": 70,
  "from-gateway": 30,
};

export type LaneSide = "left" | "right" | "center";

export interface Lane {
  readonly key: string;
  /** Centre of the grid column this lane serves, as a CSS length. */
  readonly x: string;
  readonly side: LaneSide;
  readonly lit: boolean;
}

/**
 * Centre of grid column `index` of `columns`, accounting for the gaps, so a
 * connector lands on the middle of its card instead of near it.
 */
function laneX(index: number, columns: number): string {
  const gaps = (columns - 1) * CARD_GAP;
  return `calc((100% - ${gaps}px) / ${columns} * ${index + 0.5} + ${index * CARD_GAP}px)`;
}

function laneSide(index: number, columns: number): LaneSide {
  const middle = (columns - 1) / 2;
  if (index < middle) return "left";
  if (index > middle) return "right";
  return "center";
}

/**
 * One lane per grid column: the tiers reflow from two columns to four or
 * seven, and a lane is lit when any card in its column is.
 */
export function columnLanes(
  count: number,
  columns: number,
  lit: readonly number[],
): readonly Lane[] {
  return Array.from({ length: Math.min(columns, count) }, (_, column) => ({
    key: `lane-${column}`,
    x: laneX(column, columns),
    side: laneSide(column, columns),
    lit: lit.some((index) => index % columns === column),
  }));
}
