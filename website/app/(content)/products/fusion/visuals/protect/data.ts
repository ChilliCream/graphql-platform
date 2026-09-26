export interface ClientOp {
  readonly id: string;
  readonly label: string;
}

export const CLIENT_OPS: readonly ClientOp[] = [
  { id: "web", label: "web@2.4.0" },
  { id: "mobile", label: "mobile@5.1.0" },
  { id: "partner", label: "partner-api" },
];

export const SCHEMA_FILE = "orders-api · schema.graphql";

export const V14 = {
  version: "v14",
  diff: "- Product.rating",
  verdict: "breaking · publish blocked",
} as const;

export const V15 = {
  version: "v15",
  diff: "~ Product.rating @deprecated",
  verdict: "safe · published",
} as const;

export const MOBILE_IMPACT = "4,213 requests / 7 days";
export const MOBILE_IMPACT_COMPACT = "4,213 req / 7d";
