import { BRAND, CC, FONTS, TYPE } from "../../brand";

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

/**
 * Width of a scene box at a 375px viewport: the page gutters take 40px off
 * the `max-w-6xl` container and the scene spans the full column below `lg`.
 */
const SCENE_BOX_PX = 335;

/**
 * A size from the site type scale as viewBox units, for a scene drawn in a
 * viewBox `viewBoxWidth` units wide. The scene box is the full column width on
 * a phone, so one unit renders at `SCENE_BOX_PX / viewBoxWidth` pixels.
 */
export function svgFont(px: number, viewBoxWidth: number): number {
  return Math.ceil((px * viewBoxWidth) / SCENE_BOX_PX);
}

/** Every block scene is drawn 640 x 480, the 4:3 box the page gives it. */
export const SCENE_W = 640;
export const SCENE_H = 480;
/** The traffic-desk scene is drawn wider, at 16:9. */
export const METER_W = 720;

/** SVG text sizes inside a block scene, in viewBox units. */
export const FONT = {
  /** The floor: renders at `TYPE.label` (11px) on a 375px screen. */
  label: svgFont(TYPE.label, SCENE_W),
  /** Building signage: renders at `TYPE.caption` (14px). */
  caption: svgFont(TYPE.caption, SCENE_W),
} as const;

/** The same two sizes inside the wider traffic-desk scene. */
export const METER_FONT = {
  label: svgFont(TYPE.label, METER_W),
  caption: svgFont(TYPE.caption, METER_W),
} as const;

/**
 * The hero is drawn 1200 x 700 and painted with `preserveAspectRatio` "slice"
 * into a band at least `88svh` tall, so its scale is
 * `max(boxWidth / 1200, boxHeight / 700)` and on a phone the height wins. The
 * shortest viewport the site sizes for is 568px tall, where 88svh is 500px and
 * the city renders at 500 / 700 of its unit size.
 */
const HERO_MIN_SCALE = 500 / 700;

/** A size from the site type scale as hero viewBox units. */
function heroFont(px: number): number {
  return Math.ceil(px / HERO_MIN_SCALE);
}

/** SVG text sizes inside the hero city, in viewBox units. */
export const HERO_FONT = {
  /** The floor: renders at `TYPE.label` (11px) on the shortest phone. */
  label: heroFont(TYPE.label),
  /** Building signage. */
  caption: heroFont(TYPE.caption),
  /** The plaza's own sign. */
  heading: heroFont(TYPE.h6),
} as const;

/**
 * City roles mapped onto the site tokens: a dusk-blue ground mixed from the
 * brand violet over `CC.surface`, warm windows on the brand amber, and the
 * signal colours the rest of the site already uses. No role owns a colour of
 * its own - every value here is a token, a brand accent, or a mix of the two.
 */
export const CITY = {
  /** Night sky behind the city: the page background itself. */
  sky: CC.bg,
  /** The horizon the night sky fades into. */
  skyLow: `color-mix(in srgb, ${BRAND.violet} 22%, ${CC.surface})`,
  /** Daylight sky, top to bottom, for the hero's day-night wash. */
  dawnTop: `color-mix(in srgb, ${BRAND.violet} 26%, ${CC.surface})`,
  dawnMid: `color-mix(in srgb, ${BRAND.violet} 46%, ${CC.surface})`,
  dawnLow: `color-mix(in srgb, ${BRAND.coral} 30%, ${CC.surface})`,
  ground: `color-mix(in srgb, ${BRAND.violet} 12%, ${CC.surface})`,
  road: `color-mix(in srgb, ${BRAND.violet} 20%, ${CC.surface})`,
  roadLine: CC.cardBorderHover,
  plazaTop: `color-mix(in srgb, ${BRAND.violet} 34%, ${CC.surface})`,
  plazaLeft: `color-mix(in srgb, ${BRAND.violet} 10%, ${CC.surface})`,
  plazaRight: `color-mix(in srgb, ${BRAND.violet} 22%, ${CC.surface})`,
  blockTop: `color-mix(in srgb, ${BRAND.violet} 42%, ${CC.surface})`,
  blockLeft: `color-mix(in srgb, ${BRAND.violet} 12%, ${CC.surface})`,
  blockRight: `color-mix(in srgb, ${BRAND.violet} 26%, ${CC.surface})`,
  /** The non-GraphQL facades on the same road: the same blocks, cooler. */
  facadeTop: `color-mix(in srgb, ${BRAND.slate} 34%, ${CC.surface})`,
  facadeLeft: `color-mix(in srgb, ${BRAND.slate} 10%, ${CC.surface})`,
  facadeRight: `color-mix(in srgb, ${BRAND.slate} 20%, ${CC.surface})`,
  edge: CC.inkFaint,
  window: BRAND.amber,
  windowDim: `color-mix(in srgb, ${BRAND.amber} 22%, transparent)`,
  accent: CC.accent,
  ok: BRAND.teal,
  stop: CC.danger,
  warn: CC.warning,
  ink: CC.ink,
  heading: CC.heading,
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
  fontFamily: FONTS.mono,
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
