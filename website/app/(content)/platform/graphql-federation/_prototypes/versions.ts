export interface PrototypeVersion {
  readonly n: number;
  readonly slug: string;
  readonly name: string;
  readonly round: 1 | 2;
  readonly component: string;
}

/**
 * Registry of every "What problem does GraphQL Federation solve?" backbone
 * concept prototype, in switcher order. v1-v10 are round-1 concepts ranked by
 * the judge deck; v11-v20 are round 2. Each concept task adds its own
 * `<slug>/page.tsx` route and `<component>` under `_prototypes/`; this file
 * only records the map so the switcher and shell can render it.
 */
export const PROTOTYPE_VERSIONS: readonly PrototypeVersion[] = [
  {
    n: 1,
    slug: "v1",
    name: "The Sixth Rail",
    round: 1,
    component: "SixthRail",
  },
  {
    n: 2,
    slug: "v2",
    name: "Assembling the Document",
    round: 1,
    component: "AssemblingTheDocument",
  },
  {
    n: 3,
    slug: "v3",
    name: "Conductor's Score",
    round: 1,
    component: "ConductorsScore",
  },
  {
    n: 4,
    slug: "v4",
    name: "The Dispatch Board",
    round: 1,
    component: "DispatchBoard",
  },
  {
    n: 5,
    slug: "v5",
    name: "Pinned Constellation Stage",
    round: 1,
    component: "PinnedConstellationStage",
  },
  {
    n: 6,
    slug: "v6",
    name: "Authorship Column",
    round: 1,
    component: "AuthorshipColumn",
  },
  {
    n: 7,
    slug: "v7",
    name: "The Document Is the Protagonist",
    round: 1,
    component: "DocumentProtagonist",
  },
  {
    n: 8,
    slug: "v8",
    name: "Interstitial Sigils",
    round: 1,
    component: "InterstitialSigils",
  },
  { n: 9, slug: "v9", name: "Keyed Tiles", round: 1, component: "KeyedTiles" },
  {
    n: 10,
    slug: "v10",
    name: "Route Ledger",
    round: 1,
    component: "RouteLedger",
  },
  {
    n: 11,
    slug: "v11",
    name: "Folio Numerals",
    round: 2,
    component: "FolioNumerals",
  },
  {
    n: 12,
    slug: "v12",
    name: "Front Matter",
    round: 2,
    component: "FrontMatter",
  },
  {
    n: 13,
    slug: "v13",
    name: "The Same Screen",
    round: 2,
    component: "SameScreen",
  },
  {
    n: 14,
    slug: "v14",
    name: "Run Number Rising",
    round: 2,
    component: "RunNumberRising",
  },
  {
    n: 15,
    slug: "v15",
    name: "Scene Slates",
    round: 2,
    component: "SceneSlates",
  },
  { n: 16, slug: "v16", name: "Badge P-42", round: 2, component: "BadgeP42" },
  {
    n: 17,
    slug: "v17",
    name: "Three-Act Measure",
    round: 2,
    component: "ThreeActMeasure",
  },
  { n: 18, slug: "v18", name: "One Night", round: 2, component: "OneNight" },
  {
    n: 19,
    slug: "v19",
    name: "Pieces and Cards",
    round: 2,
    component: "PiecesAndCards",
  },
  { n: 20, slug: "v20", name: "Five Lamps", round: 2, component: "FiveLamps" },
];
