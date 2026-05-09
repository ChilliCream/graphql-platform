// Attributed proof stats for the /customers proof strip. Each stat is
// bound to a single customer (named or anonymous-but-specific) so the
// numbers read as audit evidence, not aggregate marketing math. Refresh
// quarterly. Don't wire to a live API: a glitching dashboard is a
// bigger marketing risk than slightly stale numbers.

export interface AggregateStat {
  readonly key: string;
  /** Display number, e.g. "47", "9 wks", "p99 480 → 90 ms". */
  readonly value: string;
  /** Sentence-case fact line directly under the value. */
  readonly label: string;
  /** Uppercase mono attribution, e.g. "TIER-1 EU BANK". */
  readonly attribution: string;
}

export const AGGREGATE_STATS: readonly AggregateStat[] = [
  {
    key: "bffs",
    value: "47 → 1",
    label: "Hand-rolled BFFs unified into a single Fusion mesh.",
    attribution: "TIER-1 EU BANK",
  },
  {
    key: "rollout",
    value: "9 wks",
    label: "Federation rollout for an 18-service group, audit included.",
    attribution: "NORTH-AMERICAN FSI",
  },
  {
    key: "p99",
    value: "480 → 90 ms",
    label: "Mobile p99 latency after one Fusion gateway replaced six BFFs.",
    attribution: "RETAIL BANK · DACH",
  },
  {
    key: "polyglot",
    value: "12 langs",
    label: "Polyglot subgraphs composed under one supergraph.",
    attribution: "LOGISTICS PAAS",
  },
];

export const TRUST_AGGREGATE =
  "Trusted across 27 banks, 14 insurers, 6 of the top 20 European retailers, and 3 national rail operators.";
