export const DESKTOP_PRODUCTS = [
  { key: "hot-chocolate", label: "Hot Chocolate" },
  { key: "nitro", label: "Nitro" },
  { key: "mocha", label: "Mocha" },
  { key: "strawberry-shake", label: "Strawberry Shake" },
] as const;

export const DESKTOP_SERVICES = [
  { key: "catalog", label: "Catalog", color: "var(--cc-col-cat)" },
  { key: "billing", label: "Billing", color: "var(--cc-col-bil)" },
  { key: "ordering", label: "Ordering", color: "var(--cc-col-ord)" },
  { key: "shipping", label: "Shipping", color: "var(--cc-col-shi)" },
  { key: "users", label: "Users", color: "var(--cc-col-usr)" },
] as const;

export const DESKTOP_ADAPTERS = [
  { key: "graphql", label: "GraphQL" },
  { key: "openapi", label: "OpenAPI" },
  { key: "mcp", label: "MCP" },
  { key: "grpc", label: "gRPC" },
] as const;

// Per-adapter fan-out width — how many exit lanes the adapter pill expands
// into below itself before reaching the clients tier.
export const ADAPTER_FANOUT: Record<string, number> = {
  graphql: 3,
  openapi: 1,
  mcp: 1,
  grpc: 1,
};

// Per-product mapping into adapter slots, used downstream when wiring
// service stripes into adapters.
export const SERVICE_TO_ADAPTER_IDX = [0, 0, 1, 2, 3];

// adapterExitXs computes the per-adapter exit x positions used to fan a
// pill's dashed outputs into its `ADAPTER_FANOUT` slots. `pillCenterX` is
// the measured horizontal center of the rendered adapter pill in whatever
// coordinate space the caller is laying out in (stage-local pixels in Act 4,
// reference-space pixels in ActClients). The function returns an array of
// x positions in the same coordinate space.
export const adapterExitXs = (
  pillIdx: number,
  pillCenterX: number
): number[] => {
  const n = ADAPTER_FANOUT[DESKTOP_ADAPTERS[pillIdx].key];
  const gap = 12;
  const span = (n - 1) * gap;
  return Array.from({ length: n }, (_, i) => pillCenterX - span / 2 + i * gap);
};
