export interface PrototypeVersion {
  readonly n: number;
  readonly slug: string;
  readonly name: string;
  readonly component: string;
}

/**
 * Registry of every Fusion product-page concept prototype, in switcher order.
 *
 * All ten entries are listed up front so the shell and the switcher can render
 * the full set while the concepts are still being built. `component` is the
 * exported React component name the concept task must use for its concept
 * entry point (`_prototypes/concepts/<component>/`), recorded here as a plain
 * string on purpose: a real import would drag every concept into the bundle of
 * every prototype route, and the registry is also read by server components.
 * Each concept task owns its own `v<n>/page.tsx` route and imports its concept
 * component directly; this file only records the map.
 *
 * Stable exports (imported by the ten concept tasks): `PrototypeVersion`,
 * `PROTOTYPE_VERSIONS`.
 */
export const PROTOTYPE_VERSIONS: readonly PrototypeVersion[] = [
  { n: 1, slug: "v1", name: "Mission Control", component: "MissionControl" },
  { n: 2, slug: "v2", name: "Harbour", component: "Harbour" },
  { n: 3, slug: "v3", name: "Switchboard", component: "Switchboard" },
  { n: 4, slug: "v4", name: "Orchestra", component: "Orchestra" },
  { n: 5, slug: "v5", name: "Airport", component: "Airport" },
  { n: 6, slug: "v6", name: "Isometric City", component: "IsometricCity" },
  { n: 7, slug: "v7", name: "Blueprint", component: "Blueprint" },
  { n: 8, slug: "v8", name: "Terminal", component: "Terminal" },
  { n: 9, slug: "v9", name: "Constellation", component: "Constellation" },
  { n: 10, slug: "v10", name: "Editorial", component: "Editorial" },
];
