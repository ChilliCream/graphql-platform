import { BRAND, CC, FONTS } from "../../brand";

/**
 * Concept v4 "Orchestra": the shared score vocabulary.
 *
 * The gateway is the conductor, every subgraph is an orchestra section, and a
 * query is a line of the conductor's score. The stage light is warm - the
 * brand amber and coral - over the site's navy surface, so the concept reads
 * as a concert hall rather than a diagram, and the two federation
 * specifications are two notations printed on the same score: neither is
 * styled as the default.
 *
 * Nothing below is a colour of its own: every role maps to a `--color-cc-*`
 * token or a brand accent from `../../brand`, so the concept follows the site
 * theme instead of freezing its own hexes.
 */

export interface Desk {
  /** Subgraph name, e.g. "Catalog". */
  readonly name: string;
  /** Orchestra section standing in for the subgraph. */
  readonly section: string;
  /** Language the source schema's server is written in. */
  readonly language: string;
  /** Federation specification the source schema is written to. */
  readonly spec: "GraphQL Federation" | "Apollo Federation";
  /** Stage light for this desk. */
  readonly hue: string;
}

export interface Guest {
  /** Source name, e.g. "Payments". */
  readonly name: string;
  /** Contract the source publishes instead of a GraphQL schema. */
  readonly kind: "OpenAPI" | "gRPC";
  readonly hue: string;
}

/**
 * Scene role -> site token. Warm brass, cool strings, a lit rostrum and the
 * unlit hall behind them, all drawn from the site palette: the hall is the
 * page ground, the boards are the card surface, and the desk lights are the
 * brand accents the rest of the site already uses.
 */
export const STAGE = {
  /** The unlit hall: the scene's own ground, the page background token. */
  hall: CC.bg,
  /** Lit boards and plaques: the site's solid navy surface. */
  boards: CC.surface,
  /** Printed staff lines and panel edges. */
  rule: CC.cardBorder,
  /** The faintest engraved hairlines. */
  ruleFaint: `color-mix(in srgb, ${CC.cardBorder} 55%, transparent)`,
  /** Engraved secondary lettering. */
  ink: CC.ink,
  /** Engraved primary lettering. */
  heading: CC.heading,
  /** The baton itself. */
  baton: CC.heading,
  /** The rostrum light: the site accent. */
  rostrum: BRAND.cyan,
  /** Brass under the warm stage light. */
  brass: BRAND.amber,
  /** Strings under the cool stage light. */
  strings: BRAND.teal,
  /** Woodwind. */
  wood: BRAND.violet,
  /** Percussion, the warmest desk light. */
  perc: BRAND.coral,
  /** Harp. */
  harp: BRAND.green,
  /** A clash the rehearsal stops on. */
  clash: BRAND.coral,
  /** A part that still composes. */
  safe: BRAND.green,
} as const;

/** The score's engraved lettering: the site's mono face. */
export const ENGRAVE = FONTS.mono;

/**
 * The five desks on stage, in score order (top staff first). Hot Chocolate's
 * language never opens the score, and both specifications appear on it.
 */
export const DESKS: readonly Desk[] = [
  {
    name: "Catalog",
    section: "Strings",
    language: "JS/TS",
    spec: "GraphQL Federation",
    hue: STAGE.strings,
  },
  {
    name: "Billing",
    section: "Brass",
    language: "Java",
    spec: "Apollo Federation",
    hue: STAGE.brass,
  },
  {
    name: "Ordering",
    section: "Woodwind",
    language: "Go",
    spec: "GraphQL Federation",
    hue: STAGE.wood,
  },
  {
    name: "Shipping",
    section: "Percussion",
    language: "Ruby",
    spec: "Apollo Federation",
    hue: STAGE.perc,
  },
  {
    name: "Accounts",
    section: "Harp",
    language: "C#",
    spec: "GraphQL Federation",
    hue: STAGE.harp,
  },
];

/** Sources that are not GraphQL servers: guest instruments on the same score. */
export const GUESTS: readonly Guest[] = [
  { name: "Payments", kind: "OpenAPI", hue: STAGE.rostrum },
  { name: "Inventory", kind: "gRPC", hue: STAGE.wood },
];

/** The audience: everything that listens to the composite schema. */
export const LISTENERS: readonly string[] = [
  "Web",
  "Mobile",
  "Partner API",
  "Agent",
];

/** Short spec label for a staff's notation stamp. */
export function notation(spec: Desk["spec"]): string {
  return spec === "Apollo Federation" ? "APOLLO FED" : "GRAPHQL FED";
}
