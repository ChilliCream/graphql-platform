/**
 * Projection, palette and cast for the Isometric City concept (prototype v6).
 *
 * The concept reads the gateway as a 2.5D city block seen from above: the
 * gateway is the plaza building with one door, clients are vehicles that
 * arrive at that plaza, subgraphs are buildings with language signage and a
 * specification flag on the roof, OpenAPI and gRPC are buildings with a
 * different facade on the same road, composition is the zoning inspection a
 * building passes before it opens, and Nitro is the traffic camera log of
 * which vehicle really drives which road.
 *
 * Every scene draws with the same projection so the five visuals read as one
 * city rather than five unrelated diagrams.
 */

/** Half tile width in user units; a tile is 64 x 32, the classic 2:1 iso. */
export const TILE_W = 32;
/** Half tile height. */
export const TILE_H = 16;

/**
 * Grid cell (gx, gy) and height gz (already in user units, up is positive) to
 * a point in the SVG's own coordinate space. Scenes translate the whole city
 * into place, so the projection itself has no origin baked in.
 */
export function iso(gx: number, gy: number, gz = 0): readonly [number, number] {
  return [(gx - gy) * TILE_W, (gx + gy) * TILE_H - gz];
}

/** Joins projected points into an SVG `points` attribute. */
export function poly(points: readonly (readonly [number, number])[]): string {
  return points.map(([x, y]) => `${x},${y}`).join(" ");
}

export interface BoxFaces {
  /** Roof of the block. */
  readonly top: string;
  /** The face turned to the lower right of the viewer. */
  readonly right: string;
  /** The face turned to the lower left of the viewer. */
  readonly left: string;
  /** Projected centre of the roof, for signage and flag poles. */
  readonly roof: readonly [number, number];
}

/**
 * The three visible faces of an axis-aligned block standing on the cells
 * `[gx, gx + sx) x [gy, gy + sy)` and rising `h` user units.
 */
export function box(
  gx: number,
  gy: number,
  sx: number,
  sy: number,
  h: number,
): BoxFaces {
  const a = iso(gx, gy, h);
  const b = iso(gx + sx, gy, h);
  const c = iso(gx + sx, gy + sy, h);
  const d = iso(gx, gy + sy, h);

  return {
    top: poly([a, b, c, d]),
    right: poly([b, c, iso(gx + sx, gy + sy, 0), iso(gx + sx, gy, 0)]),
    left: poly([d, c, iso(gx + sx, gy + sy, 0), iso(gx, gy + sy, 0)]),
    roof: iso(gx + sx / 2, gy + sy / 2, h),
  };
}

/** Flat tile quad for a road or plaza cell. */
export function tile(gx: number, gy: number, sx = 1, sy = 1): string {
  return poly([
    iso(gx, gy),
    iso(gx + sx, gy),
    iso(gx + sx, gy + sy),
    iso(gx, gy + sy),
  ]);
}

/** City colours: a dusk-blue ground with warm windows and two signal colours. */
export const CITY = {
  sky: "#0b1020",
  skyLow: "#1b2440",
  ground: "#131b30",
  road: "#1d2740",
  roadLine: "rgba(245, 241, 234, 0.30)",
  plazaTop: "#2a3a5e",
  plazaLeft: "#16203a",
  plazaRight: "#1f2c4c",
  blockTop: "#33456d",
  blockLeft: "#18213a",
  blockRight: "#243254",
  facadeTop: "#3d4a5e",
  facadeLeft: "#1d2431",
  facadeRight: "#2a3444",
  edge: "rgba(245, 241, 234, 0.16)",
  window: "#f6c177",
  windowDim: "rgba(246, 193, 119, 0.22)",
  accent: "#16b9e4",
  ok: "#5eead4",
  stop: "#f87171",
  warn: "#f2a15c",
  ink: "#a1a3af",
  heading: "#f5f0ea",
} as const;

export type BuildingSpec = "GraphQL Federation" | "Apollo Federation";

export interface CityBlock {
  /** Signage over the door: the subgraph name. */
  readonly name: string;
  /** The language the server is written in, painted on the facade. */
  readonly language: string;
  /** The federation specification the source schema is written to. */
  readonly spec: BuildingSpec;
  /** Short flag text; both specifications fly the same kind of flag. */
  readonly flag: string;
  /** Grid cell of the block's near corner. */
  readonly cell: readonly [number, number];
  /** Storey height of the block in user units. */
  readonly height: number;
}

/** The five GraphQL buildings around the plaza, in the order the city grew. */
export const BLOCKS: readonly CityBlock[] = [
  {
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
    flag: "GraphQL Fed",
    cell: [0, 0],
    height: 74,
  },
  {
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    flag: "Apollo Fed",
    cell: [4, 0],
    height: 58,
  },
  {
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    flag: "GraphQL Fed",
    cell: [8, 2],
    height: 66,
  },
  {
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    flag: "Apollo Fed",
    cell: [4, 6],
    height: 50,
  },
  {
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    flag: "GraphQL Fed",
    cell: [0, 4],
    height: 62,
  },
];

export type SourceKind = "OpenAPI" | "gRPC";

export interface SourceBlock {
  readonly name: string;
  readonly kind: SourceKind;
  readonly cell: readonly [number, number];
  readonly height: number;
}

/** Two buildings on the same road whose facade is not a GraphQL server. */
export const SOURCE_BLOCKS: readonly SourceBlock[] = [
  { name: "Payments", kind: "OpenAPI", cell: [8, 6], height: 44 },
  { name: "Inventory", kind: "gRPC", cell: [-4, 2], height: 48 },
];

export type VehicleKind = "bus" | "scooter" | "van" | "robot";

export interface Vehicle {
  /** The client the vehicle stands for. */
  readonly name: string;
  readonly kind: VehicleKind;
  /** Body length in user units. */
  readonly length: number;
  readonly tint: string;
}

/** The four vehicles that arrive at the plaza's one door. */
export const VEHICLES: readonly Vehicle[] = [
  { name: "Web", kind: "bus", length: 44, tint: CITY.accent },
  { name: "Mobile", kind: "scooter", length: 22, tint: CITY.window },
  { name: "Partner API", kind: "van", length: 34, tint: CITY.ok },
  { name: "Agent", kind: "robot", length: 18, tint: CITY.warn },
];

/** Mono label styling shared by the SVG captions in every city scene. */
export const LABEL = {
  fontFamily:
    "ui-monospace, SFMono-Regular, Menlo, Consolas, 'Liberation Mono', monospace",
  letterSpacing: "0.12em",
} as const;

/**
 * A quad on the face turned to the lower right (constant `gxEdge`), from
 * `gy` to `gy + w` and from height `gz` to `gz + h`. Used for windows, doors
 * and painted signage.
 */
export function rightQuad(
  gxEdge: number,
  gy: number,
  gz: number,
  w: number,
  h: number,
): string {
  return poly([
    iso(gxEdge, gy, gz + h),
    iso(gxEdge, gy + w, gz + h),
    iso(gxEdge, gy + w, gz),
    iso(gxEdge, gy, gz),
  ]);
}

/** The same quad on the face turned to the lower left (constant `gyEdge`). */
export function leftQuad(
  gyEdge: number,
  gx: number,
  gz: number,
  w: number,
  h: number,
): string {
  return poly([
    iso(gx, gyEdge, gz + h),
    iso(gx + w, gyEdge, gz + h),
    iso(gx + w, gyEdge, gz),
    iso(gx, gyEdge, gz),
  ]);
}
