import type { Client, Trace } from "@/src/nitro/lib/data/types";

/** One Fusion request: the gateway parses the plan, fans out catalog and orders in parallel, then billing — the slow span. */
export const REQUEST_TRACE: Trace = {
  totalMs: 150,
  spans: [
    {
      id: "s1",
      name: "POST /graphql",
      kind: "server",
      startMs: 0,
      durationMs: 150,
      depth: 0,
    },
    {
      id: "s2",
      name: "operation plan",
      kind: "graphql",
      startMs: 3,
      durationMs: 145,
      depth: 1,
    },
    {
      id: "s3",
      name: "catalog",
      kind: "http",
      startMs: 9,
      durationMs: 26,
      depth: 2,
    },
    {
      id: "s4",
      name: "orders",
      kind: "http",
      startMs: 9,
      durationMs: 33,
      depth: 2,
    },
    {
      id: "s5",
      name: "billing",
      kind: "http",
      startMs: 44,
      durationMs: 92,
      depth: 2,
    },
  ],
};

export const LATENCY_DRIVER = "billing drives 61% of latency";

export const REQUEST_COST = 184;
export const COST_BUDGET = 1000;
export const RATE_LIMIT_STATUS = "rate limit ok";

/** Top clients by cost units, for the usage-based billing card. */
export const USAGE_CLIENTS: readonly Client[] = [
  { name: "web", total: 420, impact: 92 },
  { name: "mobile", total: 260, impact: 68 },
  { name: "partner-api", total: 95, impact: 41 },
];
