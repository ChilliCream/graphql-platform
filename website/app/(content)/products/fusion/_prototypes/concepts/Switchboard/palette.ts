import { BRAND, CC, FONTS } from "../../brand";

/**
 * Concept v3 "Switchboard": the shared look and vocabulary of the operator
 * board. A telephone exchange cabinet - dark panel, warm jack field, cloth
 * patch cords, indicator lamps - so the concept reads as one machine across
 * the hero and the five section visuals.
 *
 * Every role below maps to a site token (`CC.*`) or a brand accent
 * (`BRAND.*`) from `../../brand`, varied only with `color-mix`; the file is a
 * role -> token mapping, never a source of colour. The cabinet sits on the
 * site's own surfaces (`CC.surface` panel, `CC.bg` recess), the brass
 * engraving is the brand amber at decreasing strengths, and the lamps keep
 * semantic tokens (`CC.success` live, `CC.danger` fault).
 *
 * SVG text: use `FONTS.mono` (`MONO`) and the `TYPE` px scale, floored at
 * `TYPE.label`. Each scene draws in a 560-unit-wide `viewBox`, so a label is
 * rendered at roughly 0.6x on a 375px viewport; `TYPE.labelTight` is the
 * fallback for the two dense labels that cannot hold `TYPE.label` inside
 * their box.
 *
 * Only this concept imports from here.
 */

export const MONO = FONTS.mono;

/** Cabinet and jack field. */
export const PANEL = CC.surface;
export const PANEL_TOP = `color-mix(in srgb, ${CC.surface} 88%, ${BRAND.slate})`;
export const FIELD = CC.bg;
export const EDGE = `color-mix(in srgb, ${BRAND.amber} 24%, transparent)`;
export const EDGE_SOFT = `color-mix(in srgb, ${BRAND.amber} 11%, transparent)`;

/** Engraved brass lettering. */
export const BRASS = BRAND.amber;
export const BRASS_DIM = `color-mix(in srgb, ${BRAND.amber} 66%, transparent)`;
export const BRASS_FAINT = `color-mix(in srgb, ${BRAND.amber} 38%, transparent)`;

/** The brass wash a scanner leaves on the row it is reading. */
export const BRASS_WASH = `color-mix(in srgb, ${BRAND.amber} 8%, transparent)`;

/** The wash behind the closing band's card. */
export const BAND_WASH = `linear-gradient(180deg, ${BRASS_WASH}, transparent)`;

/** Lamps. */
export const LAMP_ON = BRAND.amber;
export const LAMP_OFF = `color-mix(in srgb, ${BRAND.amber} 14%, transparent)`;
export const LAMP_FAULT = CC.danger;
export const LAMP_LIVE = CC.success;

/** Cloth cord colours, one per line on the board. */
export const CORD = [
  BRAND.cyan,
  BRAND.teal,
  BRAND.amber,
  BRAND.coral,
  BRAND.violet,
  BRAND.slate,
  BRAND.green,
] as const;

export type Spec = "GraphQL Federation" | "Apollo Federation";

export interface Line {
  /** Catalog, Billing, Ordering, Shipping, Accounts. */
  readonly name: string;
  /** The language the subgraph's server is written in. */
  readonly lang: string;
  readonly spec: Spec;
}

/** Short engraving for a spec badge, so both fit the same jack strip. */
export const SPEC_TAG: Record<Spec, string> = {
  "GraphQL Federation": "GQL FED",
  "Apollo Federation": "APOLLO FED",
};

/** The five subgraph lines of the board, in strip order. */
export const LINES: readonly Line[] = [
  { name: "CATALOG", lang: "JS/TS", spec: "GraphQL Federation" },
  { name: "BILLING", lang: "JAVA", spec: "Apollo Federation" },
  { name: "ORDERING", lang: "GO", spec: "GraphQL Federation" },
  { name: "SHIPPING", lang: "RUBY", spec: "Apollo Federation" },
  { name: "ACCOUNTS", lang: "PYTHON", spec: "GraphQL Federation" },
];

/** The two sources that are not GraphQL servers, patched through an adapter. */
export const SOURCES = [
  { name: "PAYMENTS", kind: "OPENAPI" },
  { name: "INVENTORY", kind: "GRPC" },
] as const;

/** The clients calling in on the left of the board. */
export const CALLERS = ["WEB", "MOBILE", "PARTNER"] as const;

/** A drooping patch cord from one jack to another. */
export function cord(
  x1: number,
  y1: number,
  x2: number,
  y2: number,
  sag = 26,
): string {
  const cx1 = x1 + (x2 - x1) * 0.35;
  const cx2 = x1 + (x2 - x1) * 0.65;
  return `M ${x1} ${y1} C ${cx1} ${y1 + sag}, ${cx2} ${y2 + sag}, ${x2} ${y2}`;
}

export function clamp01(v: number): number {
  return v < 0 ? 0 : v > 1 ? 1 : v;
}

/** Ease-in-out used for cord draws and plug travel. */
export function ease(v: number): number {
  const t = clamp01(v);
  return t < 0.5 ? 4 * t * t * t : 1 - (-2 * t + 2) ** 3 / 2;
}
