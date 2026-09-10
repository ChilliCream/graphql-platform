/**
 * Concept v4 "Orchestra": the shared score vocabulary.
 *
 * The gateway is the conductor, every subgraph is an orchestra section, and a
 * query is a line of the conductor's score. Colours are warm stage light on
 * the site's navy so the concept reads as a concert hall rather than a
 * diagram, and the two federation specifications are two notations printed on
 * the same score - neither is styled as the default.
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

/** Warm brass, cool strings, a lit rostrum and the unlit hall behind them. */
export const STAGE = {
  hall: "#080c16",
  boards: "#0c1322",
  rule: "rgba(245, 241, 234, 0.14)",
  ruleFaint: "rgba(245, 241, 234, 0.07)",
  ink: "#a1a3af",
  heading: "#f5f0ea",
  baton: "#f5f0ea",
  rostrum: "#16b9e4",
  brass: "#fbbf24",
  strings: "#5eead4",
  wood: "#7c92c6",
  perc: "#f0786a",
  harp: "#34d399",
  clash: "#f0786a",
  safe: "#34d399",
} as const;

/** Monospace stack for the score's engraved labels. */
export const ENGRAVE =
  "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";

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
