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
 */

export const CN = {
  /** Deep-sky backdrop, darker than the site surface. */
  bg: "#04070f",
  panel: "rgba(9,14,28,0.82)",
  panelEdge: "rgba(150,175,235,0.16)",
  /** Orbit lines and faint sky grid. */
  orbit: "rgba(160,185,240,0.28)",
  orbitFaint: "rgba(160,185,240,0.12)",
  ink: "#d7dff4",
  dim: "#7d8aad",
  /** The star at the centre: the gateway. */
  star: "#ffe7b4",
  starCore: "#fffdf6",
  /** A query in flight. */
  beam: "#7fd6ff",
  /** Validated, safe. */
  clear: "#5ce0b4",
  /** Risky, held. */
  caution: "#f2c14e",
  /** Collision, breaking, halted. */
  alert: "#ff7a7a",
  mono: "ui-monospace, SFMono-Regular, Menlo, monospace",
} as const;

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
    colour: "#e8c766",
    radius: 92,
    angle: -34,
    period: 34,
  },
  {
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    colour: "#e58a6a",
    radius: 132,
    angle: 128,
    period: 48,
  },
  {
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    colour: "#6fd0ef",
    radius: 172,
    angle: 24,
    period: 62,
  },
  {
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    colour: "#e277a6",
    radius: 212,
    angle: 196,
    period: 78,
  },
  {
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    colour: "#8fdcae",
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
