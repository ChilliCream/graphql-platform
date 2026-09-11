export interface PrototypeVersion {
  readonly n: number;
  readonly slug: string;
  readonly name: string;
  readonly component: string;
}

/**
 * Registry of every Fusion product-page concept prototype, in switcher order.
 *
 * Entries are listed up front so the shell and the switcher can render the
 * full set while the concepts are still being built: v1..v10 are the ten
 * concepts, v11..v15 branch concept v1 and differ only in the hero visual
 * they pass to `MissionControl`.
 *
 * `component` names the concept entry point under `_prototypes/concepts/`
 * (for a v1 branch, the hero module inside it), recorded here as a plain
 * string on purpose: a real import would drag every concept into the bundle of
 * every prototype route, and the registry is also read by server components.
 * Each version task owns its own `v<n>/page.tsx` route and imports what it
 * renders directly; this file only records the map.
 *
 * Stable exports (imported by the version tasks): `PrototypeVersion`,
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
  {
    n: 11,
    slug: "v11",
    name: "Layered Diagram",
    component: "MissionControl/heroes/LayeredDiagram",
  },
  {
    n: 12,
    slug: "v12",
    name: "Radial Hub",
    component: "MissionControl/heroes/RadialHub",
  },
  {
    n: 13,
    slug: "v13",
    name: "Query Plan Trace",
    component: "MissionControl/heroes/QueryPlanTrace",
  },
  {
    n: 14,
    slug: "v14",
    name: "Depth Stack",
    component: "MissionControl/heroes/DepthStack",
  },
  {
    n: 15,
    slug: "v15",
    name: "Sequence Lanes",
    component: "MissionControl/heroes/SequenceLanes",
  },
];
