export interface PrototypeVersion {
  readonly n: number;
  readonly slug: string;
  readonly name: string;
  readonly component: string;
}

/**
 * Registry of every Fusion hero-graphic prototype, in switcher order.
 *
 * `component` names the hero module under `_prototypes/heroes/` as a plain
 * string on purpose: a real import would drag every hero into the bundle of
 * every prototype route, and the registry is also read by server components.
 * Each version task owns its own `v<n>/page.tsx` route and imports what it
 * renders directly; this file only records the map.
 *
 * Stable exports (imported by the version tasks): `PrototypeVersion`,
 * `PROTOTYPE_VERSIONS`.
 */
export const PROTOTYPE_VERSIONS: readonly PrototypeVersion[] = [
  { n: 1, slug: "v1", name: "Prism", component: "heroes/Prism" },
  {
    n: 2,
    slug: "v2",
    name: "Spectrum Bands",
    component: "heroes/SpectrumBands",
  },
  { n: 3, slug: "v3", name: "Subway Map", component: "heroes/SubwayMap" },
  { n: 4, slug: "v4", name: "Confluence", component: "heroes/Confluence" },
  {
    n: 5,
    slug: "v5",
    name: "Isometric Stack",
    component: "heroes/IsometricStack",
  },
  { n: 6, slug: "v6", name: "Loom", component: "heroes/Loom" },
  {
    n: 7,
    slug: "v7",
    name: "Aurora Ribbons",
    component: "heroes/AuroraRibbons",
  },
  {
    n: 8,
    slug: "v8",
    name: "Waveform Mix",
    component: "heroes/WaveformMix",
  },
  {
    n: 9,
    slug: "v9",
    name: "Orbital Rings",
    component: "heroes/OrbitalRings",
  },
  {
    n: 10,
    slug: "v10",
    name: "Particle Stream",
    component: "heroes/ParticleStream",
  },
];
