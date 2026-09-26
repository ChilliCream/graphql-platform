/** Copy and row data for the "Protect your clients" registry animation. */

export interface ClientOp {
  readonly id: string;
  readonly label: string;
}

/** Registered client operations the registry checks a schema change against. */
export const CLIENT_OPS: readonly ClientOp[] = [
  { id: "web", label: "web@2.4.0" },
  { id: "mobile", label: "mobile@5.1.0" },
  { id: "partner", label: "partner-api" },
];

export const SCHEMA_FILE = "orders-api · schema.graphql";

/** The failing document: still queried by mobile in production. */
export const V14 = {
  version: "v14",
  diff: "- Product.rating",
  verdict: "breaking · publish blocked",
} as const;

/** The passing document: deprecates instead of removing the field. */
export const V15 = {
  version: "v15",
  diff: "~ Product.rating @deprecated",
  verdict: "safe · published",
} as const;

export const MOBILE_IMPACT = "4,213 requests / 7 days";
/** Same figure, compact enough for the one-line client-row outcome. */
export const MOBILE_IMPACT_COMPACT = "4,213 req / 7d";
