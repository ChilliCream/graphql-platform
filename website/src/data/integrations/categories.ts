// Eight canonical categories for the /integrations index. The set is fixed:
// each integration picks exactly one home, mirroring Vercel's tight list and
// avoiding the tag-soup that comes from letting authors invent their own.
//
// The keys are URL-stable (used as ?category= filter values and on the
// detail-page sidebar chip links). Renaming a key is a breaking link change.
// Add new categories at the end; never reuse a key.

export type CategoryKey =
  | "ai-agents"
  | "auth"
  | "observability"
  | "messaging"
  | "data"
  | "cloud"
  | "ci-cd"
  | "frontend";

export interface Category {
  readonly key: CategoryKey;
  readonly label: string;
  readonly tagline: string;
}

export const CATEGORIES: readonly Category[] = [
  {
    key: "ai-agents",
    label: "AI & Agents",
    tagline: "Expose your schema to LLMs and the IDEs your team already runs.",
  },
  {
    key: "auth",
    label: "Authentication & Authorization",
    tagline:
      "JWT, OIDC, RBAC. Field-level policies that compose with the schema.",
  },
  {
    key: "observability",
    label: "Observability",
    tagline:
      "Distributed tracing, metrics, and field-level latency on every hop.",
  },
  {
    key: "messaging",
    label: "Messaging (Mocha)",
    tagline:
      "Schema-typed event streams. At-least-once delivery, partitioned consumers.",
  },
  {
    key: "data",
    label: "Data & Persistence",
    tagline:
      "Pagination, filtering, and projection pushed all the way down to the database.",
  },
  {
    key: "cloud",
    label: "Cloud & Edge",
    tagline:
      "Run the platform on the cloud you already pay for. Cold-start optimised.",
  },
  {
    key: "ci-cd",
    label: "CI / CD",
    tagline:
      "Schema checks, composition checks, breaking-change diffs on every PR.",
  },
  {
    key: "frontend",
    label: "Frontend Frameworks",
    tagline: "Typed Strawberry Shake clients for the UI framework you ship.",
  },
];

export const categoryLabel = (key: CategoryKey): string =>
  CATEGORIES.find((c) => c.key === key)?.label ?? key;

export const categoryTagline = (key: CategoryKey): string =>
  CATEGORIES.find((c) => c.key === key)?.tagline ?? "";
