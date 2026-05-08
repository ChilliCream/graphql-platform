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

export const ADAPTER_W = 200;
export const ADAPTER_H = 64;
export const ADAPTER_GAP = 60;
export const ADAPTER_PILLS_TOTAL =
  DESKTOP_ADAPTERS.length * ADAPTER_W +
  (DESKTOP_ADAPTERS.length - 1) * ADAPTER_GAP;
export const ADAPTER_PILLS_X0 = (1480 - ADAPTER_PILLS_TOTAL) / 2;
export const ADAPTER_X = DESKTOP_ADAPTERS.map(
  (_, i) => ADAPTER_PILLS_X0 + i * (ADAPTER_W + ADAPTER_GAP)
);
export const ADAPTER_CX = ADAPTER_X.map((x) => x + ADAPTER_W / 2);

export const ADAPTER_FANOUT: Record<string, number> = {
  graphql: 3,
  openapi: 1,
  mcp: 1,
  grpc: 1,
};

export const adapterExitXs = (pillIdx: number): number[] => {
  const n = ADAPTER_FANOUT[DESKTOP_ADAPTERS[pillIdx].key];
  const gap = 12;
  const span = (n - 1) * gap;
  const cx = ADAPTER_CX[pillIdx];
  return Array.from({ length: n }, (_, i) => cx - span / 2 + i * gap);
};

export const SERVICE_TO_ADAPTER_IDX = [0, 0, 1, 2, 3];
