/**
 * Concept v3 "Switchboard": the shared look and vocabulary of the operator
 * board. A telephone exchange cabinet - walnut panel, brass jack field, cloth
 * patch cords, indicator lamps - so the concept reads as one machine across
 * the hero and the five section visuals.
 *
 * Only this concept imports from here.
 */

export const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

/** Cabinet and jack field. */
export const PANEL = "#151119";
export const PANEL_TOP = "#1d1822";
export const FIELD = "#0d0b12";
export const EDGE = "rgba(226, 200, 148, 0.22)";
export const EDGE_SOFT = "rgba(226, 200, 148, 0.1)";

/** Engraved brass lettering. */
export const BRASS = "#e2c894";
export const BRASS_DIM = "rgba(226, 200, 148, 0.55)";
export const BRASS_FAINT = "rgba(226, 200, 148, 0.3)";

/** Lamps. */
export const LAMP_ON = "#ffd27d";
export const LAMP_OFF = "rgba(255, 210, 125, 0.14)";
export const LAMP_FAULT = "#f0786a";
export const LAMP_LIVE = "#5eead4";

/** Cloth cord colours, one per line on the board. */
export const CORD = [
  "#16b9e4",
  "#5eead4",
  "#c9a227",
  "#f0786a",
  "#a78bfa",
  "#7c92c6",
  "#34d399",
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
