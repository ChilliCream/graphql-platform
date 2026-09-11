import { BRAND, CC, FONTS, TYPE } from "../../brand";

/**
 * Palette and cast for the Blueprint concept (prototype v7).
 *
 * The concept reads the composite schema as an engineering drawing: cyan
 * line-work on the site's navy surfaces, every scene a plate on a drafting
 * sheet with its own title block. The gateway is the main assembly, each
 * subgraph is a sub-assembly with a part number, the language its server is
 * written in and the federation specification its source schema is written to
 * are stamps in the title block, and a source that is not a GraphQL server is
 * a bought-in part that arrives with an adapter drawing.
 *
 * Nothing here is a colour of its own: every sheet role is a site token from
 * `../../brand`, or a brand accent mixed into one, so the drawing set follows
 * the theme instead of freezing a blueprint blue of its own.
 */

/** Sheet colours, shared by all six plates so the page reads as one drawing set. */
export const BP = {
  /** The drafting sheet itself: the site's solid navy surface. */
  paper: CC.surface,
  /** A plate cut out of the sheet, one step deeper: the page ground. */
  plate: CC.bg,
  /** Minor grid squares: the blueprint cyan, barely there. */
  grid: `color-mix(in srgb, ${BRAND.cyan} 7%, transparent)`,
  /** Major grid squares, every fifth line. */
  gridMajor: `color-mix(in srgb, ${BRAND.cyan} 13%, transparent)`,
  /** Line-work: outlines, frames, lettering. */
  ink: CC.heading,
  /** Secondary lettering: field labels, notes. */
  inkDim: CC.inkDim,
  /** Construction lines, hatching, anything not yet drawn. */
  inkFaint: `color-mix(in srgb, ${CC.ink} 30%, transparent)`,
  /** Dimension lines, leaders and balloons - the measuring layer. */
  dim: BRAND.cyan,
  /** An approved check: a passing tolerance, a safe operation. */
  ok: CC.success,
  /** A queried tolerance: risky, needs review. */
  query: BRAND.amber,
  /** Red-line revision: a conflict, a rejected drawing, a breaking change. */
  redline: CC.danger,
} as const;

/** The one lettering stack every plate is drafted in: the site's mono face. */
export const DRAFT = FONTS.mono;

/** Width of a scene box at a 375px viewport: the page gutters take 40px. */
const SCENE_BOX_PX = 335;

/**
 * A size from the site type scale as viewBox units, for a plate drawn in a
 * viewBox `viewBoxWidth` units wide. A plate fills the container width on a
 * phone, so one unit renders at `SCENE_BOX_PX / viewBoxWidth` pixels.
 */
export function svgFont(px: number, viewBoxWidth: number): number {
  return Math.ceil((px * viewBoxWidth) / SCENE_BOX_PX);
}

/** Every plate is drafted in a viewBox this many units wide. */
export const SHEET_W = 480;

/** Lettering sizes inside a plate, in viewBox units. */
export const FONT = {
  /** The floor: renders at `TYPE.label` (11px) on a 375px screen. */
  label: svgFont(TYPE.label, SHEET_W),
  caption: svgFont(TYPE.caption, SHEET_W),
  /** Plate headings: the assembly name, the stamp on a rejected drawing. */
  title: svgFont(TYPE.h6, SHEET_W),
} as const;

export type SubgraphSpec = "GraphQL Federation" | "Apollo Federation";

export interface Part {
  /** Part number as it appears in the balloon and the parts list. */
  readonly no: string;
  /** Catalog, Billing, Ordering, Shipping, Accounts. */
  readonly name: string;
  /** The language the subgraph's server is written in. */
  readonly language: string;
  /** The specification its source schema is written to. */
  readonly spec: SubgraphSpec;
  /** The field this sub-assembly contributes to the dimension chain. */
  readonly feature: string;
}

/** The main assembly: the gateway every sub-assembly is drawn against. */
export const ASSEMBLY = {
  no: "ASSY-100",
  name: "Fusion Gateway",
  note: "Composite schema · distributed executor",
} as const;

/** The five GraphQL sub-assemblies, in parts-list order; C# is never first. */
export const PARTS: readonly Part[] = [
  {
    no: "P-201",
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
    feature: "product",
  },
  {
    no: "P-202",
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    feature: "price",
  },
  {
    no: "P-203",
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    feature: "order",
  },
  {
    no: "P-204",
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    feature: "eta",
  },
  {
    no: "P-205",
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    feature: "account",
  },
];

export type SourceKind = "OpenAPI" | "gRPC";

export interface BoughtInPart {
  readonly no: string;
  readonly name: string;
  readonly kind: SourceKind;
  /** The drawing that mates it to the main assembly. */
  readonly adapter: string;
  readonly feature: string;
}

/** Sources that are not GraphQL servers: bought-in parts with adapter drawings. */
export const BOUGHT_IN: readonly BoughtInPart[] = [
  {
    no: "B-311",
    name: "Payments",
    kind: "OpenAPI",
    adapter: "ADAPTER A",
    feature: "refund",
  },
  {
    no: "B-312",
    name: "Inventory",
    kind: "gRPC",
    adapter: "ADAPTER B",
    feature: "stock",
  },
];

/** The clients the composite schema is drawn for. */
export const CLIENTS = ["Web", "Mobile", "Partner API", "Agent"] as const;
