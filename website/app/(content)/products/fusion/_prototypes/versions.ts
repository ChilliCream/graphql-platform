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
  {
    n: 11,
    slug: "v11",
    name: "Plasma Fusion",
    component: "heroes/PlasmaFusion",
  },
  {
    n: 12,
    slug: "v12",
    name: "Tokamak",
    component: "heroes/Tokamak",
  },
];
