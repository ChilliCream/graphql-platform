/**
 * Roster and script for the Query Plan Trace hero (prototype v13).
 *
 * The shared `../../palette.ts` is owned by v1 and the console visuals, so the
 * content this approach needs beyond it - a fourth client, the epic's exact
 * language names, the full specification names and the request/response text -
 * stays local to this hero, exactly as `../README.md` asks.
 */

export type TraceSpec = "GraphQL Federation" | "Apollo Federation";

interface NonGraphQLSource {
  readonly name: string;
  readonly kind: "OpenAPI" | "gRPC";
  readonly language: string;
}

interface PlanStep {
  /** Subgraph the gateway fetches from. */
  readonly subgraph: string;
  readonly language: string;
  readonly spec: TraceSpec;
  /** The selection this step resolves. */
  readonly selection: string;
  /** 0 root fetch, 1 the parallel fan-out, 2 the follow-up fetch. */
  readonly phase: number;
  /** The JSON fragment this subgraph returns. */
  readonly fragment: string;
  /** The line that fragment contributes to the merged response. */
  readonly merged: string;
  /** The non-GraphQL source behind the subgraph, where there is one. */
  readonly source?: NonGraphQLSource;
}

interface TraceClient {
  readonly label: string;
  /** The operation this client sends, one array entry per typed line. */
  readonly query: readonly string[];
}

interface TimingSegment {
  readonly label: string;
  readonly ms: number;
  /** Plan phase that has to be done before the segment fills. */
  readonly phase: number;
}

/**
 * Four clients, one operation each. The shapes differ the way real callers
 * differ - a page, a card, a partner projection, an agent lookup - so the tab
 * that is live visibly changes what the gateway is asked for.
 */
export const TRACE_CLIENTS: readonly TraceClient[] = [
  {
    label: "Web app",
    query: [
      "query ProductPage($id: ID!) {",
      "  product(id: $id) {",
      "    name",
      "    price { amount currency }",
      "    orders { placedAt }",
      "    delivery { etaDays }",
      "    account { tier }",
      "  }",
      "}",
    ],
  },
  {
    label: "Mobile app",
    query: [
      "query ProductCard($id: ID!) {",
      "  product(id: $id) {",
      "    name",
      "    price { amount }",
      "    orders { placedAt }",
      "    delivery { etaDays }",
      "    account { tier }",
      "  }",
      "}",
    ],
  },
  {
    label: "Partner API",
    query: [
      "query PartnerFeed($id: ID!) {",
      "  product(id: $id) {",
      "    name",
      "    price { amount currency }",
      "    orders { placedAt status }",
      "    delivery { etaDays }",
      "    account { tier }",
      "  }",
      "}",
    ],
  },
  {
    label: "AI agent",
    query: [
      "query AgentLookup($id: ID!) {",
      "  product(id: $id) {",
      "    name",
      "    price { amount }",
      "    orders { placedAt }",
      "    delivery { etaDays }",
      "    account { tier }",
      "  }",
      "}",
    ],
  },
];

/**
 * The plan Fusion builds for that operation: the root fetch, then the three
 * subgraphs that can run at the same time, then the follow-up that needs the
 * order before it can resolve.
 */
export const PLAN_STEPS: readonly PlanStep[] = [
  {
    subgraph: "Catalog",
    language: "TypeScript",
    spec: "GraphQL Federation",
    selection: "product { name }",
    phase: 0,
    fragment: '"name": "Aeron Chair"',
    merged: '  "name": "Aeron Chair",',
  },
  {
    subgraph: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    selection: "price { amount }",
    phase: 1,
    fragment: '"price": { "amount": 1395 }',
    merged: '  "price": { "amount": 1395 },',
    source: { name: "Payments", kind: "OpenAPI", language: "Python" },
  },
  {
    subgraph: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    selection: "orders { placedAt }",
    phase: 1,
    fragment: '"orders": [{ "placedAt": "2026-09-02" }]',
    merged: '  "orders": [{ "placedAt": "2026-09-02" }],',
    source: { name: "Inventory", kind: "gRPC", language: "Go" },
  },
  {
    subgraph: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    selection: "delivery { etaDays }",
    phase: 1,
    fragment: '"delivery": { "etaDays": 2 }',
    merged: '  "delivery": { "etaDays": 2 },',
  },
  {
    subgraph: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    selection: "account { tier }",
    phase: 2,
    fragment: '"account": { "tier": "GOLD" }',
    merged: '  "account": { "tier": "GOLD" }',
  },
];

/** Headline per plan phase, in phase order. */
export const PHASE_LABELS = [
  "01 ROOT FETCH",
  "02 PARALLEL FETCH",
  "03 FOLLOW-UP FETCH",
] as const;

/** The timing bar under the merged response. */
export const TIMING: readonly TimingSegment[] = [
  { label: "root", ms: 34, phase: 0 },
  { label: "parallel", ms: 52, phase: 1 },
  { label: "follow-up", ms: 28, phase: 2 },
  { label: "merge", ms: 14, phase: 3 },
];

export const TOTAL_MS = TIMING.reduce((sum, segment) => sum + segment.ms, 0);
