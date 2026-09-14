/**
 * Constellation concept (prototype v9): the night-sky palette and the star
 * system every visual in this folder draws from, so the hero, the light pulse,
 * the ring spectrum, the gravity check and the observatory log name the same
 * subgraphs, languages and specifications.
 *
 * Scene vocabulary in this concept's language: the gateway is the star, a
 * subgraph is a planet on its orbit (language = planet colour, specification =
 * ring style), a client is a probe launching from the rim, and an OpenAPI or
 * gRPC source is a moon captured into the same system.
 *
 * The palette is a mapping of sky role -> site token: every entry is a `CC.*`
 * token, a `BRAND.*` accent (mixed with `color-mix` where the sky needs a wash)
 * or `FONTS.mono`, so the concept carries no colour and no face of its own.
 */

import { BRAND, CC, FONTS } from "../../brand";

export const CN = {
  /** Deep-sky backdrop: the page background, so a scene never darkens the page. */
  bg: CC.bg,
  /** The glow the star casts over the sky: the surface with a cyan cast. */
  sky: `color-mix(in srgb, ${BRAND.cyan} 10%, ${CC.surface})`,
  /** Plates and log panels: the site's card surface. */
  panel: CC.cardBg,
  /** Plate edges: the site's card border. */
  panelEdge: CC.cardBorder,
  /** Orbit lines and faint sky grid: the brand slate at a wash. */
  orbit: `color-mix(in srgb, ${BRAND.slate} 45%, transparent)`,
  orbitFaint: `color-mix(in srgb, ${BRAND.slate} 20%, transparent)`,
  /** Sky lettering: the brightest ink on the site. */
  ink: CC.heading,
  /** Secondary lettering under it. */
  dim: CC.inkDim,
  /** The star at the centre - the gateway - as the brand amber burning white. */
  star: `color-mix(in srgb, ${BRAND.amber} 55%, ${CC.white})`,
  starCore: `color-mix(in srgb, ${BRAND.amber} 12%, ${CC.white})`,
  /** A query in flight: the brand cyan. */
  beam: BRAND.cyan,
  /** Validated, safe. */
  clear: CC.success,
  /** Risky, held. */
  caution: CC.warning,
  /** Collision, breaking, halted. */
  alert: CC.danger,
  mono: FONTS.mono,
} as const;

/**
 * The language a source schema's server is written in, as the brand accent its
 * planet is drawn with; the set is the brand accents, never a new hue.
 */
export const LANGUAGE_COLOUR: Readonly<Record<string, string>> = {
  "JS/TS": BRAND.amber,
  Java: BRAND.coral,
  Go: BRAND.cyan,
  Ruby: BRAND.violet,
  "C#": BRAND.green,
};

export type PlanetSpec = "GraphQL Federation" | "Apollo Federation";

export interface Planet {
  readonly name: string;
  /** Language the server is written in; drives the planet colour. */
  readonly language: string;
  readonly spec: PlanetSpec;
  readonly colour: string;
  /** Orbit radius in SVG units, relative to the star. */
  readonly radius: number;
  /** Rest angle in degrees, so a still frame is a readable constellation. */
  readonly angle: number;
  /** Seconds for one full revolution. */
  readonly period: number;
}

/**
 * Five source schemas. Order is deliberate: the language-agnostic servers come
 * first and C# is never the opening example.
 */
export const PLANETS: readonly Planet[] = [
  {
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
    colour: LANGUAGE_COLOUR["JS/TS"],
    radius: 92,
    angle: -34,
    period: 34,
  },
  {
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    colour: LANGUAGE_COLOUR["Java"],
    radius: 132,
    angle: 128,
    period: 48,
  },
  {
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    colour: LANGUAGE_COLOUR["Go"],
    radius: 172,
    angle: 24,
    period: 62,
  },
  {
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    colour: LANGUAGE_COLOUR["Ruby"],
    radius: 212,
    angle: 196,
    period: 78,
  },
  {
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    colour: LANGUAGE_COLOUR["C#"],
    radius: 252,
    angle: 78,
    period: 96,
  },
];

export interface Moon {
  readonly name: string;
  readonly kind: "OpenAPI" | "gRPC";
  /** Planet the moon is captured around. */
  readonly around: string;
}

export const MOONS: readonly Moon[] = [
  { name: "Payments", kind: "OpenAPI", around: "Billing" },
  { name: "Inventory", kind: "gRPC", around: "Ordering" },
];

/** Clients, drawn as probes launching from the rim of the system. */
export const PROBES = ["Web", "Mobile", "Partner API", "Agent"] as const;

/** Ring dash pattern: solid for one specification, dashed for the other. */
export function ringDash(spec: PlanetSpec): string {
  return spec === "Apollo Federation" ? "5 4" : "0";
}

/** Short spec tag for the cramped plates inside a scene. */
export function specTag(spec: PlanetSpec): string {
  return spec === "Apollo Federation" ? "APOLLO FED" : "GRAPHQL FED";
}

/**
 * Cartesian position of a point on an orbit, in SVG units around the star.
 * `tilt` squashes the circle vertically for scenes that draw the system as a
 * tilted disc; scenes that rotate their orbits leave it at 1 so a CSS rotation
 * follows the path exactly.
 */
export function orbitPoint(
  radius: number,
  angleDeg: number,
  tilt = 1,
): { readonly x: number; readonly y: number } {
  const rad = (angleDeg * Math.PI) / 180;
  return { x: Math.cos(rad) * radius, y: Math.sin(rad) * radius * tilt };
}
