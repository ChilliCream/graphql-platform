/**
 * Curated, mostly-constant data for the 5-tab Nitro product reel (see PLAN-tabs.md).
 * Hand-authored to mirror the real EShops federated GraphQL domain so the clones read as
 * the genuine product; a couple of series are seeded for organic chart shapes.
 */
import { mulberry32 } from "./rng";

/* ─────────────────────────── shared helpers ─────────────────────────── */

/** A seeded smooth series (two sine components + light noise), length n, in [lo,hi]-ish. */
export function smoothSeries(
  seed: number,
  n: number,
  base: number,
  amp: number,
): number[] {
  const r = mulberry32((seed ^ 0x9e3779b9) >>> 0);
  const ph1 = r() * Math.PI * 2;
  const ph2 = r() * Math.PI * 2;
  return Array.from({ length: n }, (_, i) => {
    const x = (i / n) * Math.PI * 2;
    const wave = Math.sin(x * 1.3 + ph1) * 0.6 + Math.sin(x * 2.4 + ph2) * 0.4;
    return Math.max(0, base + amp * wave + (r() - 0.5) * amp * 0.25);
  });
}

/* ─────────────────────────── 1. Compose (query editor) ─────────────────────────── */

export const composeData = {
  query: `query GetOrder($id: ID!) {
  orderById(id: $id) {
    id
    status
    total
    createdAt
    customer {
      name
      email
    }
    items {
      product {
        name
        price
      }
      quantity
    }
  }
}`,
  operationName: "GetOrder",
  variables: `{
  "id": "ord_8F2KQ7"
}`,
  response: `{
  "data": {
    "orderById": {
      "id": "ord_8F2KQ7",
      "status": "PROCESSING",
      "total": 129.97,
      "createdAt": "2026-06-19T14:32:08Z",
      "customer": {
        "name": "Ada Lovelace",
        "email": "ada@eshops.io"
      },
      "items": [
        { "product": { "name": "Mechanical Keyboard", "price": 89.99 }, "quantity": 1 },
        { "product": { "name": "USB-C Cable", "price": 19.99 }, "quantity": 2 }
      ]
    }
  }
}`,
  status: { code: "200", duration: "142 ms", size: "2.4 KB" },
  history: [
    {
      name: "GetOrder",
      kind: "query" as const,
      time: "14:32:08",
      took: "142 ms",
      ok: true,
    },
    {
      name: "createOrder",
      kind: "mutation" as const,
      time: "14:30:51",
      took: "318 ms",
      ok: true,
    },
    {
      name: "SearchProducts",
      kind: "query" as const,
      time: "14:29:12",
      took: "1.2 s",
      ok: false,
    },
  ],
};

/* ─────────────────────────── 2. Trace (waterfall + DB span) ─────────────────────────── */

export type TabSpanKind = "server" | "graphql" | "internal" | "http" | "db";

export interface TabSpan {
  id: string;
  name: string;
  kind: TabSpanKind;
  depth: number;
  startMs: number;
  durationMs: number;
  hasChildren?: boolean;
  /** the slow database span the reel drills into */
  target?: boolean;
}

export const traceData = {
  operation: "query GetHomePageQuery",
  totalMs: 842.6,
  kind: "server",
  timeAgo: "2 minutes ago",
  spans: [
    {
      id: "s0",
      name: "POST /graphql",
      kind: "server",
      depth: 0,
      startMs: 0,
      durationMs: 842.6,
      hasChildren: true,
    },
    {
      id: "s1",
      name: "query GetHomePageQuery",
      kind: "graphql",
      depth: 1,
      startMs: 12,
      durationMs: 818,
      hasChildren: true,
    },
    {
      id: "s2",
      name: "Parse Request",
      kind: "internal",
      depth: 2,
      startMs: 15,
      durationMs: 0.9,
    },
    {
      id: "s3",
      name: "Validate Request",
      kind: "internal",
      depth: 2,
      startMs: 18,
      durationMs: 1.4,
    },
    {
      id: "s4",
      name: "products: SearchProducts",
      kind: "graphql",
      depth: 2,
      startMs: 60,
      durationMs: 540,
      hasChildren: true,
    },
    {
      id: "s5",
      name: "HTTP POST products",
      kind: "http",
      depth: 3,
      startMs: 70,
      durationMs: 360,
      hasChildren: true,
    },
    {
      id: "s6",
      name: "db.query SELECT products",
      kind: "db",
      depth: 4,
      startMs: 95,
      durationMs: 312,
      target: true,
    },
    {
      id: "s7",
      name: "user: viewer",
      kind: "graphql",
      depth: 2,
      startMs: 620,
      durationMs: 180,
      hasChildren: true,
    },
    {
      id: "s8",
      name: "HTTP POST accounts",
      kind: "http",
      depth: 3,
      startMs: 625,
      durationMs: 150,
    },
  ] as TabSpan[],
  // flyout for the DB span
  dbSpan: {
    name: "db.query SELECT products",
    duration: "312.4 ms",
    kindBadge: "client",
    general: [
      ["ID", "7b3f1a9c2e4d8f60"],
      ["Parent ID", "a1c4e6b80d2f3597"],
      ["Trace ID", "4e2d9f7a6c1b08e3f5a2c7d49b6e0123"],
      ["Timestamp", "2026-06-19T14:22:08.317Z"],
      ["Status Message", "Ok"],
    ] as [string, string][],
    database: [
      ["Name", "eshop"],
      ["Operation", "SELECT"],
      ["System", "postgresql"],
      ["Instance", "eshop-prod"],
      ["User", "eshop_reader"],
    ] as [string, string][],
    statement:
      "SELECT p.id, p.name, p.price, p.currency, p.rating\nFROM products AS p\nWHERE p.category_id = $1 AND p.in_stock = true\nORDER BY p.rating DESC\nLIMIT 24",
  },
};

/* ─────────────────────────── 3. Diagnose (errors) ─────────────────────────── */

export interface ErrRow {
  op: string;
  kind: "query" | "mutation" | "subscription";
  errorRate: number; // 0..1
  requests: string;
  p95: string;
}

export const diagnoseData = {
  // flat baseline → sharp spike ~70% across → settle (32 buckets)
  spike: [
    1, 0, 2, 1, 0, 1, 2, 1, 0, 1, 1, 0, 2, 1, 1, 0, 1, 2, 1, 0, 3, 5, 9, 18, 34,
    52, 47, 31, 18, 9, 4, 2,
  ],
  peak: 52,
  grid: [
    {
      op: "createOrder",
      kind: "mutation",
      errorRate: 0.124,
      requests: "18,204",
      p95: "142 ms",
    },
    {
      op: "AddToCart",
      kind: "mutation",
      errorRate: 0.031,
      requests: "42,910",
      p95: "38 ms",
    },
    {
      op: "GetOrderHistory",
      kind: "query",
      errorRate: 0.018,
      requests: "14,772",
      p95: "88 ms",
    },
    {
      op: "SearchProducts",
      kind: "query",
      errorRate: 0.004,
      requests: "128,540",
      p95: "61 ms",
    },
    {
      op: "UpdateAccountAddress",
      kind: "mutation",
      errorRate: 0.002,
      requests: "3,015",
      p95: "19 ms",
    },
    {
      op: "GetProductReviews",
      kind: "query",
      errorRate: 0.0,
      requests: "96,330",
      p95: "24 ms",
    },
  ] as ErrRow[],
  failingOp: "createOrder",
  spanName: "Orders.createOrder",
  duration: "142 ms",
  exception: {
    type: "System.InvalidOperationException",
    message: "Sequence contains no elements",
    stack: [
      "at System.Linq.ThrowHelper.ThrowNoElementsException()",
      "at System.Linq.Enumerable.First[TSource](IEnumerable`1 source)",
      "at EShops.Orders.OrderService.ReserveInventoryAsync(CreateOrderInput input)",
      "    in /src/Orders/OrderService.cs:line 87",
      "at EShops.Orders.Mutations.CreateOrderAsync(CreateOrderInput input, CancellationToken ct)",
      "    in /src/Orders/Mutations.cs:line 42",
      "at HotChocolate.Resolvers.Expressions.ExpressionHelper.AwaitTaskHelper[T](Task`1 task)",
    ],
  },
  error: {
    message: "Unexpected Execution Error",
    code: "INTERNAL_SERVER_ERROR",
    path: "createOrder",
  },
};

/* ─────────────────────────── 4. Schema (reference + insights) ─────────────────────────── */

export type SchemaKind =
  | "query"
  | "mutation"
  | "object"
  | "scalar"
  | "enum"
  | "field"
  | "input"
  | "interface"
  | "union";

export interface SchemaRow {
  name: string;
  kind: SchemaKind;
  drillable?: boolean;
  /** syntax-colored signature for field rows */
  sig?: { field: string; type: string; bang?: boolean; deprecated?: boolean };
}

export const schemaData = {
  stats: [
    ["184", "Types"],
    ["1,243", "Fields"],
    ["12", "Directives"],
  ] as [string, string][],
  // Column 1 — types grouped
  typeGroups: [
    {
      group: "Objects",
      rows: [
        "Query",
        "Mutation",
        "Product",
        "Review",
        "Order",
        "Account",
        "Cart",
        "Money",
        "Brand",
      ].map(
        (name): SchemaRow => ({
          name,
          kind:
            name === "Query"
              ? "query"
              : name === "Mutation"
                ? "mutation"
                : "object",
          drillable: true,
        }),
      ),
    },
    {
      group: "Inputs",
      rows: ["CreateOrderInput", "ProductFilterInput"].map(
        (name): SchemaRow => ({ name, kind: "input", drillable: true }),
      ),
    },
    {
      group: "Enums",
      rows: ["OrderStatus", "Currency"].map(
        (name): SchemaRow => ({ name, kind: "enum", drillable: true }),
      ),
    },
    {
      group: "Scalars",
      rows: ["ID", "String", "Int", "Float", "DateTime"].map(
        (name): SchemaRow => ({ name, kind: "scalar" }),
      ),
    },
  ],
  // Column 2 — Query fields
  queryFields: [
    {
      name: "products",
      kind: "field",
      drillable: true,
      sig: { field: "products", type: "ProductsConnection", bang: true },
    },
    {
      name: "productById",
      kind: "field",
      drillable: true,
      sig: { field: "productById", type: "Product" },
    },
    {
      name: "searchProducts",
      kind: "field",
      drillable: true,
      sig: {
        field: "searchProducts",
        type: "SearchProductsResult",
        bang: true,
      },
    },
    {
      name: "orderById",
      kind: "field",
      drillable: true,
      sig: { field: "orderById", type: "Order" },
    },
    {
      name: "me",
      kind: "field",
      drillable: true,
      sig: { field: "me", type: "Account" },
    },
  ] as SchemaRow[],
  // Column 3 — Product fields
  productFields: [
    { name: "id", kind: "field", sig: { field: "id", type: "ID", bang: true } },
    {
      name: "name",
      kind: "field",
      sig: { field: "name", type: "String", bang: true },
    },
    {
      name: "price",
      kind: "field",
      drillable: true,
      sig: { field: "price", type: "Money", bang: true },
    },
    {
      name: "brand",
      kind: "field",
      drillable: true,
      sig: { field: "brand", type: "Brand", bang: true },
    },
    {
      name: "reviews",
      kind: "field",
      drillable: true,
      sig: { field: "reviews", type: "ReviewsConnection", bang: true },
    },
    {
      name: "averageRating",
      kind: "field",
      sig: { field: "averageRating", type: "Float", bang: true },
    },
    {
      name: "inStock",
      kind: "field",
      sig: { field: "inStock", type: "Boolean", bang: true, deprecated: true },
    },
  ] as SchemaRow[],
  breadcrumb: ["Query", "products", "Product", "reviews"],
  // Insights — coordinates
  coordinates: [
    { coord: "Query.products", kind: "field", usage: "48.2K" },
    { coord: "Product.reviews", kind: "field", usage: "31.7K" },
    { coord: "Product.price", kind: "field", usage: "29.4K" },
    { coord: "Query.productById", kind: "field", usage: "18.9K" },
    { coord: "Review.rating", kind: "field", usage: "12.4K" },
    { coord: "Mutation.createOrder", kind: "field", usage: "9.8K" },
    {
      coord: "Product.inStock",
      kind: "field",
      usage: "4.1K",
      deprecated: true,
    },
    { coord: "Query.searchProducts", kind: "field", usage: "3.6K" },
    { coord: "Order.status", kind: "field", usage: "2.2K" },
    { coord: "Account.email", kind: "field", usage: "1.1K" },
  ] as {
    coord: string;
    kind: SchemaKind;
    usage: string;
    deprecated?: boolean;
  }[],
  hero: "Product.reviews",
  heroDetail: [
    ["First seen", "3 months ago"],
    ["Last seen", "2 minutes ago"],
    ["Requests", "31,742"],
    ["Error rate", "0.21%"],
    ["Mean duration", "14.8 ms"],
    ["Throughput", "1,058 opm"],
  ] as [string, string][],
  sourceSchemas: ["Products", "Reviews"],
  clients: [
    { name: "Web Storefront", ops: "128 Operations", requests: "22.4K" },
    { name: "iOS App", ops: "64 Operations", requests: "7.1K" },
    { name: "Admin Dashboard", ops: "41 Operations", requests: "1.9K" },
    { name: "Partner API", ops: "12 Operations", requests: "312" },
  ],
  requestsSeries: smoothSeries(11, 40, 700, 380),
  latencyMean: smoothSeries(12, 40, 14, 3),
  latencyP95: smoothSeries(13, 40, 28, 6),
  latencyP99: smoothSeries(14, 40, 52, 12),
};

/* ─────────────────────────── 5. Fusion (execution plan) ─────────────────────────── */

export type PlanStatus = "success" | "partial" | "failed";
export type PlanKind = "root" | "fetch" | "resolve" | "introspection";

export interface PlanNode {
  id: string;
  kind: PlanKind;
  title: string;
  subtitle?: string;
  durationMs: number;
  status: PlanStatus;
  subOp?: string;
  /** JSON the subgraph returned for this fetch — shown in the "View Raw Data" subgraph tab */
  response?: string;
  /** variable-batching: the array of variable sets the single-entity query is dispatched with */
  viewVariables?: string;
  batch?: number;
  /** rank (column, 0 = root at left) and order within the column (top→bottom) */
  rank: number;
  order: number;
}

export interface PlanEdge {
  from: string;
  to: string;
  label?: string;
}

export const fusionData = {
  rootOp: `query GetOrderSummary($id: ID!) {
  order(id: $id) {
    id
    total
    items {
      product { id name price }
      quantity
    }
    customer { id name email }
  }
}`,
  // A deeper, query-only federated plan (no "Resolve" steps — every node is a real subgraph fetch),
  // 5 ranks deep with several hops: Orders → {Products, Accounts} → {Inventory, Reviews, Loyalty}
  // → {Warehouses, review Authors}.
  nodes: [
    {
      id: "root",
      kind: "root",
      title: "Query Plan",
      subtitle: "0x4a1f9c",
      durationMs: 58,
      status: "success",
      rank: 0,
      order: 0,
      subOp: `query GetOrderSummary($id: ID!) {
  order(id: $id) { id total items { ... } customer { ... } }
}`,
    },
    {
      id: "orders",
      kind: "fetch",
      title: "Fetch from Orders",
      subtitle: "0x8f3a2c",
      durationMs: 12,
      status: "success",
      rank: 1,
      order: 0,
      subOp: `query Fetch_Orders($id: ID!) {
  order(id: $id) {
    id total
    items { productId quantity }
    customerId
  }
}`,
      response: `{
  "order": {
    "id": "ord_8F2KQ7",
    "total": 129.97,
    "items": [
      { "productId": "prod_KB01", "quantity": 1 },
      { "productId": "prod_UC02", "quantity": 2 }
    ],
    "customerId": "acct_31"
  }
}`,
    },
    {
      id: "products",
      kind: "fetch",
      title: "Fetch from Products",
      subtitle: "0x2d77b1",
      durationMs: 9,
      status: "success",
      batch: 2,
      rank: 2,
      order: 0,
      // VARIABLE BATCHING: a single-entity query (productById, one $id) is dispatched once per id —
      // an ARRAY of variable sets in, an ARRAY of results out.
      subOp: `query Fetch_Products($id: ID!) {
  productById(id: $id) {
    id
    name
    price
  }
}`,
      viewVariables: `[
  { "id": "prod_KB01" },
  { "id": "prod_UC02" }
]`,
      response: `[
  { "data": { "productById": { "id": "prod_KB01", "name": "Mechanical Keyboard", "price": 89.99 } } },
  { "data": { "productById": { "id": "prod_UC02", "name": "USB-C Cable", "price": 19.99 } } }
]`,
    },
    {
      id: "accounts",
      kind: "fetch",
      title: "Fetch from Accounts",
      subtitle: "0xb3c5fa",
      durationMs: 7,
      status: "success",
      rank: 2,
      order: 1,
      subOp: `query Fetch_Accounts($id: ID!) {
  accountById(id: $id) { id name email tier }
}`,
      response: `{
  "accountById": { "id": "acct_31", "name": "Ada Lovelace", "email": "ada@eshops.io", "tier": "GOLD" }
}`,
    },
    {
      id: "inventory",
      kind: "fetch",
      title: "Fetch from Inventory",
      subtitle: "0x5c1a90",
      durationMs: 14,
      status: "success",
      batch: 1,
      rank: 3,
      order: 0,
      subOp: `query Fetch_Inventory($ids: [ID!]!) {
  stockByProduct(ids: $ids) { productId inStock warehouseId }
}`,
      response: `{
  "stockByProduct": [
    { "productId": "prod_KB01", "inStock": 42, "warehouseId": "wh_DE1" },
    { "productId": "prod_UC02", "inStock": 318, "warehouseId": "wh_DE1" }
  ]
}`,
    },
    {
      id: "reviews",
      kind: "fetch",
      title: "Fetch from Reviews",
      subtitle: "0x91ee04",
      durationMs: 22,
      status: "partial",
      batch: 1,
      rank: 3,
      order: 1,
      subOp: `query Fetch_Reviews($ids: [ID!]!) {
  reviewsByProduct(ids: $ids) { id rating body authorId }
}`,
      response: `{
  "reviewsByProduct": [
    { "id": "rev_90", "rating": 5, "body": "Fantastic keys", "authorId": "acct_77" }
  ],
  "errors": [{ "path": ["reviewsByProduct", 1], "message": "timeout" }]
}`,
    },
    {
      id: "loyalty",
      kind: "fetch",
      title: "Fetch from Loyalty",
      subtitle: "0x3a8c14",
      durationMs: 5,
      status: "success",
      rank: 3,
      order: 2,
      subOp: `query Fetch_Loyalty($id: ID!) {
  loyaltyByAccount(id: $id) { points tier }
}`,
      response: `{
  "loyaltyByAccount": { "points": 4820, "tier": "GOLD" }
}`,
    },
    {
      id: "warehouses",
      kind: "fetch",
      title: "Fetch from Warehouses",
      subtitle: "0x9d04ab",
      durationMs: 8,
      status: "success",
      rank: 4,
      order: 0,
      subOp: `query Fetch_Warehouses($ids: [ID!]!) {
  warehousesById(ids: $ids) { id region }
}`,
      response: `{
  "warehousesById": [{ "id": "wh_DE1", "region": "eu-central" }]
}`,
    },
    {
      id: "reviewers",
      kind: "fetch",
      title: "Fetch from Accounts",
      subtitle: "0xf12b7d",
      durationMs: 9,
      status: "success",
      batch: 2,
      rank: 4,
      order: 1,
      subOp: `query Fetch_ReviewAuthors($ids: [ID!]!) {
  accountsById(ids: $ids) { id name }
}`,
      response: `{
  "accountsById": [{ "id": "acct_77", "name": "Grace H." }]
}`,
    },
  ] as PlanNode[],
  edges: [
    { from: "root", to: "orders" },
    { from: "orders", to: "products" },
    { from: "orders", to: "accounts" },
    { from: "products", to: "inventory" },
    { from: "products", to: "reviews" },
    { from: "accounts", to: "loyalty" },
    { from: "inventory", to: "warehouses" },
    { from: "reviews", to: "reviewers" },
  ] as PlanEdge[],
};
