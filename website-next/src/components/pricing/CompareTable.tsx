import { FeatureComparison } from "@/src/components/FeatureComparison";
import { TIERS } from "@/src/components/pricing/pricingData";

/** Shorthand for a row where every tier includes the capability. */
function all(label: string) {
  return { label, free: true, payg: true, dedicated: true, self: true };
}

const COMPARISON = [
  {
    title: "Plans & usage",
    rows: [
      {
        label: "Monthly price",
        free: "$0",
        payg: "$20 / month",
        dedicated: "from $400 / month",
        self: "Custom",
      },
      {
        label: "Deployment model",
        free: "Multi-tenant cloud",
        payg: "Multi-tenant cloud",
        dedicated: "Single-tenant cloud or BYOC",
        self: "Your infrastructure",
      },
      {
        label: "Included operations / month",
        free: "1M",
        payg: "5M, then $2 / million",
        dedicated: "Volume based",
        self: "Unmetered",
      },
      {
        label: "Included ingest",
        free: "2 GB",
        payg: "2 GB per 1M ops, then $1.15 / GB",
        dedicated: "Volume based",
        self: "Unmetered",
      },
      {
        label: "Data retention",
        free: "3 days",
        payg: "60 days",
        dedicated: "Configurable",
        self: "Configurable",
      },
      {
        label: "Pricing model",
        free: "Free, capped",
        payg: "Usage based",
        dedicated: "Volume based",
        self: "Your infrastructure",
      },
    ],
  },
  {
    title: "Gateway & server",
    rows: [
      all("OAuth 2.0 & OpenID Connect"),
      all("Authorization policies & roles"),
      all("Rate limiting"),
      all("Response caching"),
      all("Realtime subscriptions"),
      all("GraphQL Federation"),
      all("Custom middleware & plugins"),
    ],
  },
  {
    title: "Schema lifecycle",
    rows: [
      all("Schema registry with history & rollback"),
      all("Client registry"),
      all("Breaking-change classification"),
      all("CI schema & client checks"),
      all("Stage promotion with approval gates"),
      all("Fusion deployment orchestration"),
      all(".NET Aspire integration"),
    ],
  },
  {
    title: "Observability",
    rows: [
      all("OpenTelemetry-native traces, metrics, logs"),
      all("Operation insights"),
      all("Per-client tracking"),
      all("Resolver-level insights"),
      all("Distributed tracing across Fusion subgraphs"),
      all("Service monitoring for any .NET service"),
      all("Operation reporting"),
    ],
  },
  {
    title: "Operations & delivery",
    rows: [
      all("Persisted / trusted operations enforcement"),
      all("Query cost analysis"),
      all("Request limits"),
      all("Deployment audit log"),
      all("Rollback by republishing an earlier tag"),
      all("Persisted-op distribution cache"),
    ],
  },
  {
    title: "Security & access",
    rows: [
      {
        label: "Roles & stage-scoped publish permissions",
        free: false,
        payg: false,
        dedicated: true,
        self: true,
      },
      {
        label: "SSO",
        free: false,
        payg: false,
        dedicated: true,
        self: true,
      },
      {
        label: "Audit log",
        free: false,
        payg: false,
        dedicated: true,
        self: true,
      },
      all("API keys and PATs"),
    ],
  },
  {
    title: "Developer experience",
    rows: [
      {
        label: "Built-in GraphQL IDE",
        free: "Served from your endpoint",
        payg: "Served from your endpoint",
        dedicated: "Served from your endpoint",
        self: "Served from your endpoint",
      },
      all("MCP adapter"),
      all("OpenAPI adapter"),
    ],
  },
  {
    title: "Support",
    rows: [
      {
        label: "Support channel",
        free: "Community",
        payg: "Email",
        dedicated: "Email + private chat",
        self: "Priority engineering",
      },
      {
        label: "Release channel",
        free: "Continuous",
        payg: "Continuous",
        dedicated: "Continuous",
        self: "Long-term release channel",
      },
      {
        label: "Onboarding & training",
        free: "Docs & community",
        payg: "Docs & community",
        dedicated: "Guided onboarding",
        self: "Custom training",
      },
    ],
  },
];

/**
 * The pricing feature comparison: maps the tier-keyed comparison data onto the
 * shared `FeatureComparison` table.
 */
export function CompareTable() {
  const columns = TIERS.map((tier) => tier.name);
  const groups = COMPARISON.map((group) => ({
    title: group.title,
    rows: group.rows.map((row) => ({
      label: row.label,
      cells: TIERS.map((tier) => row[tier.id]),
    })),
  }));

  return (
    <FeatureComparison
      id="compare"
      className="mt-24 scroll-mt-24 sm:mt-28"
      eyebrow="Compare plans"
      heading="Feature comparison"
      columns={columns}
      groups={groups}
    />
  );
}
