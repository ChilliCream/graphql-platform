import { BRAND, CC, FONTS, TYPE } from "../../brand";

/**
 * Palette and cast for the Harbour concept (prototype v2).
 *
 * The concept reads the gateway as a harbour at dusk: clients are ships that
 * all dock at one pier, subgraphs are warehouses along the quay, composition
 * is the customs check every manifest passes before departure, and Nitro is
 * the harbour log of which ship actually carries which container.
 *
 * Nothing here is a colour of its own: every scene role is a site token from
 * `../../brand`, or a brand accent mixed into one, so the harbour follows the
 * theme instead of freezing a dusk of its own.
 */

/**
 * Dusk harbour roles, kept out of the visuals so the six scenes match. Each
 * one maps to a `CC` token or a `BRAND` accent mixed into a token.
 */
export const DUSK = {
  /** Deepest ground of a scene box; also the cut-out behind lit shapes. */
  skyTop: CC.bg,
  /** The band of sky over the water. */
  skyMid: CC.surface,
  /** Dusk wash where the sky meets the headland: violet over the surface. */
  skyGlow: `color-mix(in srgb, ${BRAND.violet} 26%, ${CC.surface})`,
  /** The setting sun: amber pulled towards coral. */
  sun: `color-mix(in srgb, ${BRAND.amber} 72%, ${BRAND.coral})`,
  water: `color-mix(in srgb, ${CC.surface} 82%, ${BRAND.navy})`,
  waterDeep: `color-mix(in srgb, ${CC.bg} 86%, ${BRAND.navy})`,
  /** Light broken on the water, and every mooring line: cyan at 55%. */
  shimmer: `color-mix(in srgb, ${BRAND.cyan} 55%, transparent)`,
  /** Quay stone and warehouse walls. */
  quay: `color-mix(in srgb, ${CC.surface} 88%, ${BRAND.slate})`,
  /** The lit top face of the quay, a step lighter than its wall. */
  quayTop: `color-mix(in srgb, ${CC.surface} 74%, ${BRAND.slate})`,
  /** Hairline around a scene panel. */
  edge: CC.cardBorder,
  /** The same hairline where it has to carry a shape on its own. */
  edgeBright: CC.cardBorderHover,
  /** Ship hulls and anything else that reads as lit paint. */
  hull: CC.heading,
  /** Quay lamps, beacon light and the specification pennants. */
  lamp: BRAND.amber,
  /** The harbour's own accent: cyan, on cargo, traces and readouts. */
  accent: BRAND.cyan,
  /** A passing check. */
  ok: BRAND.teal,
  /** A stopped build or a breaking operation. */
  stop: CC.danger,
  ink: CC.inkDim,
  heading: CC.heading,
} as const;

/** Width of a scene box at a 375px viewport: the page gutters take 40px. */
const SCENE_BOX_PX = 335;

/**
 * A size from the site type scale as viewBox units, for a scene drawn in a
 * viewBox `viewBoxWidth` units wide. The scene box is the full container width
 * on a phone, so a unit renders at `SCENE_BOX_PX / viewBoxWidth` pixels.
 */
export function svgFont(px: number, viewBoxWidth: number): number {
  return Math.ceil((px * viewBoxWidth) / SCENE_BOX_PX);
}

/** Every section scene is drawn 640 units wide. */
export const SCENE_W = 640;

/** SVG text sizes inside a section scene, in viewBox units. */
export const FONT = {
  /** The floor: renders at `TYPE.label` (11px) on a 375px screen. */
  label: svgFont(TYPE.label, SCENE_W),
  caption: svgFont(TYPE.caption, SCENE_W),
} as const;

/** The hero scene is drawn 1200 units wide. */
export const HERO_W = 1200;

/** SVG text sizes inside the hero scene, in viewBox units. */
export const HERO_FONT = {
  label: svgFont(TYPE.label, HERO_W),
} as const;

export type SubgraphSpec = "GraphQL Federation" | "Apollo Federation";

export interface Warehouse {
  /** Warehouse over the quay door: the subgraph name. */
  readonly name: string;
  /** Flag flown over the warehouse: the language the server is written in. */
  readonly language: string;
  /** Pennant next to the flag: the federation specification it is written to. */
  readonly spec: SubgraphSpec;
  /** Short pennant text; both specs are drawn the same way on purpose. */
  readonly pennant: string;
}

/** The five GraphQL warehouses along the quay, in berth order. */
export const WAREHOUSES: readonly Warehouse[] = [
  {
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
    pennant: "GraphQL Fed",
  },
  {
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    pennant: "Apollo Fed",
  },
  {
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    pennant: "GraphQL Fed",
  },
  {
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    pennant: "Apollo Fed",
  },
  {
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    pennant: "GraphQL Fed",
  },
];

export type SourceKind = "OpenAPI" | "gRPC";

export interface Source {
  readonly name: string;
  readonly kind: SourceKind;
}

/** Warehouses on the same quay that are not GraphQL servers at all. */
export const SOURCES: readonly Source[] = [
  { name: "Payments", kind: "OpenAPI" },
  { name: "Inventory", kind: "gRPC" },
];

export interface Ship {
  /** The client the ship stands for. */
  readonly name: string;
  /** Hull silhouette drawn for it. */
  readonly kind: "liner" | "speedboat" | "freighter" | "drone";
  /** Hull length in the 1200-wide harbour viewBox. */
  readonly length: number;
}

/** The four ships that dock at the one pier. */
export const SHIPS: readonly Ship[] = [
  { name: "Web", kind: "liner", length: 268 },
  { name: "Mobile", kind: "speedboat", length: 168 },
  { name: "Partner API", kind: "freighter", length: 244 },
  { name: "Agent", kind: "drone", length: 132 },
];

/** Mono label styling shared by the SVG captions in every Harbour scene. */
export const LABEL = {
  fontFamily: FONTS.mono,
  letterSpacing: "0.06em",
} as const;

/** The same label where a pennant or a long caption needs the tracking closed up. */
export const LABEL_TIGHT = {
  fontFamily: FONTS.mono,
  letterSpacing: "0.01em",
} as const;
