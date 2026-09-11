import { BRAND } from "../../../../brand";
import type { Source, Station, StationSpec } from "../../palette";
import { CLIENTS, MC, SOURCES, STATIONS } from "../../palette";

/**
 * Roster and geometry for the Depth Stack hero (prototype v14).
 *
 * The scene is three planes in perspective: the client plane nearest the
 * viewer, the Fusion gateway plane in the middle and the source plane at the
 * back, holding the five subgraphs plus the two non-GraphQL sources at its
 * edges. Every plane is real DOM, so the labels stay crisp text; this module
 * holds only the data and the two layouts, the component renders them.
 *
 * What the shared palette does not carry lives here, as the hero contract
 * requires: the fourth client (the AI agent), the device notes, the per
 * subgraph language the epic asks for (the palette's tag is shorter), the
 * language of the two non-GraphQL sources and the accent of a spec badge.
 */

/** One point in the scene: percent of the stage box, plus a plane depth. */
export interface P3 {
  readonly x: number;
  readonly y: number;
  readonly z: number;
}

/** A panel: centre in percent of the stage box, size in CSS pixels. */
export interface Spot {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

export interface ClientCard {
  readonly name: string;
  readonly note: string;
  /** Device silhouette: the mobile card is the tall one. */
  readonly radius: number;
}

export interface Plate {
  readonly name: string;
  readonly language: string;
  readonly spec: StationSpec;
}

export interface SourceCard {
  readonly name: string;
  readonly kind: Source["kind"];
  readonly language: string;
}

/** Full names and device notes for the three clients the palette names. */
const CLIENT_CARD: Readonly<Record<(typeof CLIENTS)[number], ClientCard>> = {
  Web: { name: "Web App", note: "BROWSER", radius: 10 },
  Mobile: { name: "Mobile App", note: "IOS · ANDROID", radius: 16 },
  "Partner API": {
    name: "Partner API",
    note: "SERVER TO SERVER",
    radius: 10,
  },
};

/** The palette's three clients, plus the AI agent the epic adds. */
export const CLIENT_CARDS: readonly ClientCard[] = [
  ...CLIENTS.map((client) => CLIENT_CARD[client]),
  { name: "AI Agent", note: "MCP TOOL CALLS", radius: 10 },
];

/** Full language names for the plates; the palette carries a shorter tag. */
const LANGUAGE: Readonly<Record<string, string>> = {
  Catalog: "TypeScript",
  Billing: "Java",
  Ordering: "Go",
  Shipping: "Ruby",
  Accounts: "C#",
};

export const PLATES: readonly Plate[] = STATIONS.map((station: Station) => ({
  name: station.name,
  language: LANGUAGE[station.name] ?? station.language,
  spec: station.spec,
}));

/** The two non-GraphQL sources carry the sixth language of the mix. */
const SOURCE_LANGUAGE: Readonly<Record<Source["kind"], string>> = {
  OpenAPI: "Python",
  gRPC: "Java",
};

export const SOURCE_CARDS: readonly SourceCard[] = SOURCES.map((source) => ({
  name: source.name,
  kind: source.kind,
  language: SOURCE_LANGUAGE[source.kind],
}));

/** Badge accent per specification, so the mix reads at a glance. */
export const SPEC_ACCENT: Readonly<Record<StationSpec, string>> = {
  "GraphQL Federation": MC.phosphor,
  "Apollo Federation": BRAND.violet,
};

/**
 * The query plan each client sends: the two or three back-plane sources the
 * gateway has to reach before it can answer, named as the plates and the
 * non-GraphQL sources are named.
 */
export const PLANS: readonly (readonly string[])[] = [
  ["Catalog", "Ordering", "Accounts"],
  ["Catalog", "Shipping", "Inventory"],
  ["Billing", "Ordering", "Payments"],
  ["Catalog", "Accounts", "Inventory"],
];

export interface Stage {
  /** Wide lays the planes out in depth, narrow stacks them down the screen. */
  readonly variant: "wide" | "narrow";
  readonly perspective: number;
  /** Base `rotateX` of the whole world, in degrees. */
  readonly tilt: number;
  /** Pointer parallax swings the world by at most this many degrees. */
  readonly swing: number;
  readonly clientZ: number;
  readonly backZ: number;
  readonly gateway: Spot;
  readonly clients: readonly Spot[];
  readonly plates: readonly Spot[];
  readonly sources: readonly Spot[];
}

/**
 * Wide layout: clients low and near, gateway in the middle of the box, the
 * five plates staggered across the back plane with the two non-GraphQL
 * sources at its left and right edge.
 *
 * The back plane renders at `1250 / (1250 + 290)` = 0.81x, so its smallest
 * label (14px) lands at about 11.4 rendered px, above the 11px floor; the
 * client plane renders at 1.22x, so its 11px note lands at about 13px.
 */
export const WIDE: Stage = {
  variant: "wide",
  perspective: 1250,
  tilt: 9,
  swing: 5,
  clientZ: 230,
  backZ: -290,
  gateway: { x: 50, y: 50, w: 300, h: 100 },
  clients: [
    { x: 20, y: 76, w: 170, h: 58 },
    { x: 40, y: 76, w: 116, h: 78 },
    { x: 60, y: 76, w: 170, h: 58 },
    { x: 80, y: 76, w: 162, h: 58 },
  ],
  plates: [
    { x: 26, y: 14, w: 196, h: 72 },
    { x: 32, y: 32, w: 196, h: 72 },
    { x: 50, y: 14, w: 196, h: 72 },
    { x: 68, y: 32, w: 196, h: 72 },
    { x: 74, y: 14, w: 196, h: 72 },
  ],
  sources: [
    { x: 4, y: 32, w: 158, h: 66 },
    { x: 96, y: 32, w: 158, h: 66 },
  ],
};

/**
 * Narrow layout (below `md`): the same three planes at a shallower angle and
 * a shallower depth, stacked down the screen so a 375px viewport shows all of
 * them. The back plane renders at `900 / (900 + 60)` = 0.94x, so its 14px
 * chips land at about 13 rendered px.
 */
export const NARROW: Stage = {
  variant: "narrow",
  perspective: 900,
  tilt: 4,
  swing: 0,
  clientZ: 60,
  backZ: -60,
  gateway: { x: 50, y: 40, w: 270, h: 80 },
  clients: [
    { x: 27, y: 9, w: 150, h: 50 },
    { x: 73, y: 9, w: 134, h: 56 },
    { x: 27, y: 21, w: 150, h: 50 },
    { x: 73, y: 21, w: 150, h: 50 },
  ],
  plates: [
    { x: 50, y: 55.2, w: 230, h: 46 },
    { x: 50, y: 63.5, w: 230, h: 46 },
    { x: 50, y: 71.8, w: 230, h: 46 },
    { x: 50, y: 80.1, w: 230, h: 46 },
    { x: 50, y: 88.4, w: 230, h: 46 },
  ],
  sources: [
    { x: 27, y: 95.5, w: 160, h: 30 },
    { x: 73, y: 95.5, w: 160, h: 30 },
  ],
};

/** Centre of a panel as a scene point. */
export function at(spot: Spot, z: number): P3 {
  return { x: spot.x, y: spot.y, z };
}

/**
 * The back-plane point a query plan entry names, plate or source. Every name
 * in `PLANS` is one of the two rosters above; an unknown one falls back to the
 * first source rather than dropping the dot out of the scene.
 */
export function targetPoint(stage: Stage, name: string): P3 {
  const plate = PLATES.findIndex((entry) => entry.name === name);
  if (plate >= 0) return at(stage.plates[plate], stage.backZ);

  const source = SOURCE_CARDS.findIndex((entry) => entry.name === name);
  return at(stage.sources[Math.max(source, 0)], stage.backZ);
}

export function lerp3(a: P3, b: P3, t: number): P3 {
  return {
    x: a.x + (b.x - a.x) * t,
    y: a.y + (b.y - a.y) * t,
    z: a.z + (b.z - a.z) * t,
  };
}
