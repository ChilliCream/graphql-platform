// Aggregate stats for the "by the numbers" band on /customers.
// Locked at publish time. Refresh quarterly. Don't wire to a live API:
// the marketing risk of a glitching dashboard outweighs freshness.

export interface AggregateStat {
  readonly key: string;
  readonly value: string;
  readonly eyebrow: string;
  readonly label: string;
}

export const AGGREGATE_STATS: readonly AggregateStat[] = [
  {
    key: "subgraphs",
    value: "47",
    eyebrow: "Federated",
    label: "subgraphs composed across the customer base",
  },
  {
    key: "requests",
    value: "8.2B",
    eyebrow: "Per month",
    label: "requests served on Nitro Hosted gateways",
  },
  {
    key: "schema-changes",
    value: "3,400+",
    eyebrow: "Governed",
    label: "schema changes reviewed by Nitro composition",
  },
  {
    key: "p99",
    value: "−62%",
    eyebrow: "On average",
    label: "p99 reduction after a Fusion federation rollout",
  },
];

export const TRUST_AGGREGATE =
  "Trusted across 27 banks, 14 insurers, 6 of the top 20 European retailers, and 3 national rail operators.";
