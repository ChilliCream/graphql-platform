import type { CSSProperties } from "react";

import { BRAND, CC, FONTS, TYPE } from "../../brand";

/**
 * Palette, cast and drawing helpers for the Editorial concept (prototype v10).
 *
 * The concept reads the page as a printed magazine feature that is being
 * annotated by hand: the gateway is a sketched hub, subgraphs are sticky notes
 * with handwritten language and specification labels, clients are doodled
 * devices, composition is a red-pen correction on the proof sheet, and Nitro is
 * a highlighter drawn over the operations real clients run.
 *
 * Nothing here is a colour or a face of its own: every scene role maps to a
 * `CC` token, a `BRAND` accent mixed into one, or a `FONTS` face from
 * `../../brand`, so the spread follows the site theme instead of freezing a
 * cream stock of its own. The hand stays in the drawing - the wobble filter,
 * the doubled strokes and the tilted notes - not in a novelty font.
 */

/** Ink-on-paper roles, kept out of the visuals so the six scenes match. */
export const PAPER = {
  /** The sheet every scene is drawn on: the site's solid surface. */
  sheet: CC.surface,
  sheetEdge: CC.cardBorder,
  /** Shadow under a taped or pinned element: the brand navy, half opaque. */
  shade: `color-mix(in srgb, ${BRAND.navy} 55%, transparent)`,
  /** Main marker ink: the brightest lettering on the sheet. */
  ink: CC.heading,
  /** Second pass of the same marker, a step quieter. */
  inkSoft: CC.ink,
  /** Pencil marginalia. */
  pencil: CC.inkDim,
  /** Editor's red pen: the brand coral. */
  red: BRAND.coral,
  /** Proof-reader's blue pencil: the brand cyan. */
  blue: BRAND.cyan,
  /** Green pen used for a passing check. */
  green: BRAND.green,
  /** Highlighter wash: the brand amber, thin enough to read ink through. */
  highlight: `color-mix(in srgb, ${BRAND.amber} 24%, transparent)`,
  highlightEdge: `color-mix(in srgb, ${BRAND.amber} 60%, transparent)`,
  /** Sticky note stocks, in cast order: brand accents washed into the sheet. */
  noteA: `color-mix(in srgb, ${BRAND.amber} 22%, ${CC.surface})`,
  noteB: `color-mix(in srgb, ${BRAND.coral} 22%, ${CC.surface})`,
  noteC: `color-mix(in srgb, ${BRAND.green} 22%, ${CC.surface})`,
  noteD: `color-mix(in srgb, ${BRAND.cyan} 22%, ${CC.surface})`,
  noteE: `color-mix(in srgb, ${BRAND.violet} 22%, ${CC.surface})`,
} as const;

/** Marginalia and note labels: the site's body face. */
export const HAND = {
  fontFamily: FONTS.body,
} as const;

/** Lettering for a heading drawn inside a scene: the site's heading face. */
export const MARKER = {
  fontFamily: FONTS.heading,
  letterSpacing: "0.01em",
} as const;

/** Set in type for folio marks and small caps captions inside a scene. */
export const FOLIO = {
  fontFamily: FONTS.mono,
  letterSpacing: "0.16em",
} as const;

/** Width of a scene box at a 375px viewport: the page gutters take 40px. */
const SCENE_BOX_PX = 335;

/**
 * A size from the site type scale as viewBox units, for a scene drawn in a
 * viewBox `viewBoxWidth` units wide. A plate spans the full column on a phone,
 * so one unit renders at `SCENE_BOX_PX / viewBoxWidth` pixels.
 */
export function svgFont(px: number, viewBoxWidth: number): number {
  return Math.ceil((px * viewBoxWidth) / SCENE_BOX_PX);
}

/** Every plate is drawn 640 units wide. */
export const SCENE_W = 640;

/** Lettering sizes inside a plate, in viewBox units. */
export const FONT = {
  /** The floor: renders at `TYPE.label` (11px) on a 375px screen. */
  label: svgFont(TYPE.label, SCENE_W),
  /** Note names and the writing a scene is read by. */
  caption: svgFont(TYPE.caption, SCENE_W),
  /** A heading drawn inside a scene. */
  heading: svgFont(TYPE.h6, SCENE_W),
} as const;

/**
 * The cover drawing is 1200 x 700 and painted with `preserveAspectRatio`
 * "slice" into a band at least `88svh` tall, so its scale is
 * `max(boxWidth / 1200, boxHeight / 700)` and on a phone the height wins. The
 * shortest viewport the site sizes for is 568px tall, where 88svh is 500px and
 * the drawing renders at 500 / 700 of its unit size.
 */
const HERO_MIN_SCALE = 500 / 700;

/** A size from the site type scale as cover viewBox units. */
function heroFont(px: number): number {
  return Math.ceil(px / HERO_MIN_SCALE);
}

/** Lettering sizes inside the cover drawing, in viewBox units. */
export const HERO_FONT = {
  /** The floor: renders at `TYPE.label` (11px) on the shortest phone. */
  label: heroFont(TYPE.label),
  /** Client names and note captions. */
  caption: heroFont(TYPE.caption),
  /** Margin annotations. */
  heading: heroFont(TYPE.h6),
  /** The gateway, lettered on the hub itself. */
  display: heroFont(TYPE.h5),
} as const;

export type SubgraphSpec = "GraphQL Federation" | "Apollo Federation";

export interface Note {
  /** The subgraph the sticky note stands for. */
  readonly name: string;
  /** Handwritten under the name: the language its server is written in. */
  readonly language: string;
  /** The federation specification the source schema is written to. */
  readonly spec: SubgraphSpec;
  /** Short handwritten spec label; both specs are drawn the same way. */
  readonly specLabel: string;
  /** Note stock colour. */
  readonly stock: string;
}

/** The five GraphQL subgraphs, in reading order. */
export const NOTES: readonly Note[] = [
  {
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
    specLabel: "GraphQL Fed",
    stock: PAPER.noteA,
  },
  {
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    specLabel: "Apollo Fed",
    stock: PAPER.noteB,
  },
  {
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    specLabel: "GraphQL Fed",
    stock: PAPER.noteC,
  },
  {
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    specLabel: "Apollo Fed",
    stock: PAPER.noteD,
  },
  {
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    specLabel: "GraphQL Fed",
    stock: PAPER.noteE,
  },
];

export type SourceKind = "OpenAPI" | "gRPC";

export interface Source {
  readonly name: string;
  readonly kind: SourceKind;
}

/** Index cards taped to the same spread: sources that are not GraphQL servers. */
export const SOURCES: readonly Source[] = [
  { name: "Payments", kind: "OpenAPI" },
  { name: "Inventory", kind: "gRPC" },
];

export interface Client {
  readonly name: string;
  /** Doodle drawn for it. */
  readonly device: "laptop" | "phone" | "card" | "robot";
}

/** The four doodled clients that send queries to the one endpoint. */
export const CLIENTS: readonly Client[] = [
  { name: "Web", device: "laptop" },
  { name: "Mobile", device: "phone" },
  { name: "Partner API", device: "card" },
  { name: "Agent", device: "robot" },
];

/**
 * Style for a stroke that draws itself: the path length the keyframes count
 * down from, plus the delay that stages it after the lines drawn before it.
 * A generous length is fine - the stroke simply finishes a little early.
 */
export function drawStyle(length: number, delay = 0): CSSProperties {
  return { "--len": length, animationDelay: `${delay}s` } as CSSProperties;
}

/** Style for an annotation that fades in after the lines around it. */
export function fadeStyle(delay: number): CSSProperties {
  return { animationDelay: `${delay}s` };
}
