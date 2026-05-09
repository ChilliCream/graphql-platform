// Filter taxonomy for the /templates gallery. Six axes mirror the design
// brief verbatim: Topology, Use case, Language, Client, Product mix,
// Agent-ready. Vercel ships the same shape with their six axes; we just
// substitute the values for our reality.
//
// The schema is intentionally narrow: only chip-style multi-select / single-
// select / toggle. Anything richer (range slider, search) would invite
// freeform queries, which the filter-as-search bet specifically rejects.
//
// Each axis is bound by a stringly-typed key. The keys are also the URL
// query-string identifiers, so renaming an axis is a breaking link change.
// Add new options at the end; never reuse a key.

export type FilterKind = "single" | "multi" | "toggle";

export type TopologyKey = "solo" | "federation" | "polyglot";
export type UseCaseKey =
  | "starter"
  | "cqrs"
  | "realtime"
  | "observability"
  | "llm-mcp"
  | "auth"
  | "multi-tenant";
export type LanguageKey = "dotnet" | "ts-node" | "mixed";
export type ClientKey =
  | "none"
  | "react-strawberry-shake"
  | "blazor-strawberry-shake"
  | "nextjs";
export type ProductKey =
  | "hot-chocolate"
  | "mocha"
  | "fusion"
  | "nitro"
  | "strawberry-shake";

export interface FilterOption<TKey extends string = string> {
  readonly key: TKey;
  readonly label: string;
}

export interface FilterAxisDef<TKey extends string = string> {
  readonly key: string;
  readonly label: string;
  readonly kind: FilterKind;
  readonly options: readonly FilterOption<TKey>[];
}

export const TOPOLOGY_OPTIONS: readonly FilterOption<TopologyKey>[] = [
  { key: "solo", label: "Solo service" },
  { key: "federation", label: "Federation (Fusion)" },
  { key: "polyglot", label: "Polyglot federation" },
];

export const USE_CASE_OPTIONS: readonly FilterOption<UseCaseKey>[] = [
  { key: "starter", label: "Starter" },
  { key: "cqrs", label: "CQRS" },
  { key: "realtime", label: "Realtime / Subscriptions" },
  { key: "observability", label: "Observability" },
  { key: "llm-mcp", label: "LLM / MCP" },
  { key: "auth", label: "Auth" },
  { key: "multi-tenant", label: "Multi-tenant" },
];

export const LANGUAGE_OPTIONS: readonly FilterOption<LanguageKey>[] = [
  { key: "dotnet", label: "C# / .NET" },
  { key: "ts-node", label: "TypeScript / Node" },
  { key: "mixed", label: "Mixed" },
];

export const CLIENT_OPTIONS: readonly FilterOption<ClientKey>[] = [
  { key: "none", label: "None (server only)" },
  { key: "react-strawberry-shake", label: "React + Strawberry Shake" },
  { key: "blazor-strawberry-shake", label: "Blazor + Strawberry Shake" },
  { key: "nextjs", label: "Next.js" },
];

export const PRODUCT_OPTIONS: readonly FilterOption<ProductKey>[] = [
  { key: "hot-chocolate", label: "Hot Chocolate" },
  { key: "mocha", label: "Mocha" },
  { key: "fusion", label: "Fusion" },
  { key: "nitro", label: "Nitro" },
  { key: "strawberry-shake", label: "Strawberry Shake" },
];

export const FILTER_AXES = [
  {
    key: "topology",
    label: "Topology",
    kind: "single",
    options: TOPOLOGY_OPTIONS,
  } as const,
  {
    key: "use",
    label: "Use case",
    kind: "multi",
    options: USE_CASE_OPTIONS,
  } as const,
  {
    key: "language",
    label: "Language",
    kind: "multi",
    options: LANGUAGE_OPTIONS,
  } as const,
  {
    key: "client",
    label: "Client",
    kind: "multi",
    options: CLIENT_OPTIONS,
  } as const,
  {
    key: "product",
    label: "Product mix",
    kind: "multi",
    options: PRODUCT_OPTIONS,
  } as const,
  {
    key: "agent",
    label: "Agent-ready",
    kind: "toggle",
    options: [{ key: "yes", label: "Yes" }],
  } as const,
] as const;

export type FilterAxisKey = (typeof FILTER_AXES)[number]["key"];

export const productLabel = (key: ProductKey): string =>
  PRODUCT_OPTIONS.find((p) => p.key === key)?.label ?? key;

export const topologyLabel = (key: TopologyKey): string =>
  TOPOLOGY_OPTIONS.find((p) => p.key === key)?.label ?? key;

export const useCaseLabel = (key: UseCaseKey): string =>
  USE_CASE_OPTIONS.find((p) => p.key === key)?.label ?? key;

export const languageLabel = (key: LanguageKey): string =>
  LANGUAGE_OPTIONS.find((p) => p.key === key)?.label ?? key;

export const clientLabel = (key: ClientKey): string =>
  CLIENT_OPTIONS.find((p) => p.key === key)?.label ?? key;
