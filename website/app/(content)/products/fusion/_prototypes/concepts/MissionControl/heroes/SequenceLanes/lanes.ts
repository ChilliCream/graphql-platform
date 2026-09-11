import { BRAND } from "../../../../brand";
import { MC, SOURCES, STATIONS } from "../../palette";
import type { StationSpec } from "../../palette";

/**
 * The cast of the sequence diagram and the timeline it is drawn on: one lane
 * per participant, one sequence per client, and the fractions of a column at
 * which each hop happens. `index.tsx` and `parts.tsx` share these.
 */

export type LaneKind = "client" | "gateway" | "subgraph" | "source";

export interface Lane {
  readonly name: string;
  /** Language, role or protocol line under the name. */
  readonly meta: string;
  /** Optional third line, where a plate has room for one. */
  readonly note?: string;
  readonly spec?: StationSpec;
  readonly kind: LaneKind;
  /** Lane colour: return arrow, activation bar and spec swatch. */
  readonly accent: string;
}

/** Federation specs, mixed across the subgraphs, each with its own swatch. */
export const SPEC_ACCENT: Record<StationSpec, string> = {
  "GraphQL Federation": MC.phosphor,
  "Apollo Federation": BRAND.violet,
};

/**
 * The four clients. The shared `CLIENTS` roster carries only three, under the
 * short console names, so the hero keeps its own list (see heroes/README.md:
 * content the palette lacks stays local to the approach).
 */
const CLIENT_LANES: readonly Lane[] = [
  { name: "Web app", meta: "browser", kind: "client", accent: MC.signal },
  {
    name: "Mobile app",
    meta: "iOS / Android",
    kind: "client",
    accent: MC.signal,
  },
  {
    name: "Partner API",
    meta: "server to server",
    kind: "client",
    accent: MC.signal,
  },
  { name: "AI agent", meta: "tool calls", kind: "client", accent: MC.signal },
];

const GATEWAY_LANE: Lane = {
  name: "Fusion",
  meta: "gateway",
  note: "composite schema",
  kind: "gateway",
  accent: MC.phosphor,
};

/** Full language names; `STATIONS` carries the short console tags. */
const SUBGRAPH_LANGUAGE: Record<string, string> = {
  Catalog: "TypeScript",
  Billing: "Java",
  Ordering: "Go",
  Shipping: "Ruby",
  Accounts: "C#",
};

const SUBGRAPH_LANES: readonly Lane[] = STATIONS.map((station) => ({
  name: station.name,
  meta: SUBGRAPH_LANGUAGE[station.name] ?? station.language,
  spec: station.spec,
  kind: "subgraph" as const,
  accent: SPEC_ACCENT[station.spec],
}));

/** The languages behind the two non-GraphQL sources complete the mix. */
const SOURCE_LANGUAGE: Record<string, string> = {
  OpenAPI: "Python",
  gRPC: "Go",
};

const SOURCE_LANES: readonly Lane[] = SOURCES.map((source) => ({
  name: source.name,
  meta: `${SOURCE_LANGUAGE[source.kind]} · ${source.kind}`,
  kind: "source" as const,
  accent: MC.amber,
}));

export const LANES: readonly Lane[] = [
  ...CLIENT_LANES,
  GATEWAY_LANE,
  ...SUBGRAPH_LANES,
  ...SOURCE_LANES,
];

/** Lane index of the gateway; clients sit above it, source schemas below. */
export const GATEWAY = CLIENT_LANES.length;

const laneIndex = (name: string) => LANES.findIndex((l) => l.name === name);

export interface Sequence {
  /** Lane index of the client that issued the request. */
  readonly client: number;
  /** Lane indices the gateway fans out to, in fan-out order. */
  readonly targets: readonly number[];
}

export const SEQUENCES: readonly Sequence[] = [
  {
    client: 0,
    targets: [
      laneIndex("Catalog"),
      laneIndex("Ordering"),
      laneIndex("Accounts"),
    ],
  },
  {
    client: 1,
    targets: [
      laneIndex("Catalog"),
      laneIndex("Shipping"),
      laneIndex("Inventory"),
    ],
  },
  {
    client: 2,
    targets: [
      laneIndex("Billing"),
      laneIndex("Ordering"),
      laneIndex("Payments"),
    ],
  },
  { client: 3, targets: [laneIndex("Catalog"), laneIndex("Accounts")] },
];

/** One column of the trace scrolls past per `SCROLL_MS`; a sequence spans four. */
export const SCROLL_MS = 2200;
export const SEQ_MS = SCROLL_MS * SEQUENCES.length;
/** The still frame names the web app's completed trace. */
export const REST_STEP = 0;

/**
 * Where each hop sits in its column, as a fraction of the column: x is time,
 * so a hop's animation delay is its own fraction of the sequence.
 */
export const T = {
  request: 0.08,
  fanOut: 0.26,
  firstReturn: 0.52,
  returnGap: 0.08,
  response: 0.82,
} as const;

/** The returns arrive one after another and merge on the gateway lane. */
export const returnAt = (i: number) => T.firstReturn + i * T.returnGap;

/** Vertical centre of a lane, on the shared lane pitch. */
export const laneTop = (lane: number) =>
  `calc(var(--mc-seq-lane) * ${lane + 0.5})`;

export const wash = (color: string, percent: number) =>
  `color-mix(in srgb, ${color} ${percent}%, transparent)`;

export const KEYFRAMES = `
@keyframes mc-seq-scroll { to { transform: translateX(-50%); } }
@keyframes mc-seq-draw {
  0% { transform: scaleY(0); opacity: 0; }
  2% { opacity: 1; }
  10% { transform: scaleY(1); }
  90% { transform: scaleY(1); opacity: 1; }
  100% { transform: scaleY(1); opacity: 0; }
}
@keyframes mc-seq-head {
  0%, 9% { opacity: 0; }
  12%, 90% { opacity: 1; }
  100% { opacity: 0; }
}
@keyframes mc-seq-fill {
  0% { transform: scaleX(0); opacity: 0; }
  2% { opacity: 1; }
  16% { transform: scaleX(1); }
  90% { transform: scaleX(1); opacity: 1; }
  100% { transform: scaleX(1); opacity: 0; }
}
`;
