/**
 * Single source of truth for Nitro pricing, shared by the landing "Brew it your
 * Way" selector (NitroPricing) and every pricing page / preview variation.
 *
 * Tiers, in order: Free (shared) -> Pay as you go (shared) -> Dedicated
 * (single-tenant, volume based) -> Self-Hosted (your infrastructure). Schemas and
 * environments are included on every tier. As monthly consumption grows,
 * additional support and deployment options unlock (see UNLOCKS).
 */

export type TierId = "free" | "payg" | "dedicated" | "self";

export interface Tier {
  readonly id: TierId;
  readonly name: string;
  readonly tagline: string;
  /** Display price, e.g. "$0", "$20", "from $400", "Custom". */
  readonly price: string;
  /** Small note under the price, e.g. "forever", "per month", "talk to us". */
  readonly priceNote: string;
  /** Headline bullets for the plan card. */
  readonly features: readonly string[];
  readonly cta: string;
  readonly ctaHref: string;
  /** The highlighted / recommended tier. */
  readonly popular?: boolean;
}

export const TIERS: readonly Tier[] = [
  {
    id: "free",
    name: "Free",
    tagline: "Shared cloud, fully managed.",
    price: "$0",
    priceNote: "forever",
    features: [
      "Shared multi-tenant cloud",
      "Schemas & environments included",
      "1M operations / month",
      "2 GB ingest / month",
      "3-day log & trace retention",
      "Community support",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
  },
  {
    id: "payg",
    name: "Pay as you go",
    tagline: "Shared cloud, usage based.",
    price: "$20",
    priceNote: "per month",
    features: [
      "Shared multi-tenant cloud",
      "5M operations included, then $2 / million",
      "2 GB ingest per 1M ops, then $1.15 / GB",
      "60-day log & trace retention",
      "Email support",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
  },
  {
    id: "dedicated",
    name: "Dedicated",
    tagline: "Single-tenant, volume based.",
    price: "from $400",
    priceNote: "per month",
    features: [
      "Single-tenant cloud or BYOC",
      "Priced by instance size",
      "Configurable retention",
      "Private networking",
      "SSO, audit log, role-based access",
    ],
    cta: "Talk to Us",
    ctaHref: "/services/support/contact",
    popular: true,
  },
  {
    id: "self",
    name: "Self-Hosted",
    tagline: "Your infrastructure.",
    price: "Custom",
    priceNote: "talk to us",
    features: [
      "Run on your own infrastructure",
      "Air-gapped & on-prem supported",
      "Configurable retention",
      "Priority engineering support",
      "Long-term release channel",
    ],
    cta: "Talk to Us",
    ctaHref: "/services/support/contact",
  },
];

export type Cell = boolean | string;

export interface ComparisonRow {
  readonly label: string;
  readonly free: Cell;
  readonly payg: Cell;
  readonly dedicated: Cell;
  readonly self: Cell;
}

export interface ComparisonGroup {
  readonly title: string;
  readonly rows: readonly ComparisonRow[];
}

/** Shorthand for a row where every tier includes the capability. */
function all(label: string): ComparisonRow {
  return { label, free: true, payg: true, dedicated: true, self: true };
}

export const COMPARISON: readonly ComparisonGroup[] = [
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

export interface Unlock {
  /** The monthly consumption threshold, e.g. "$2,000 / mo". */
  readonly spend: string;
  /** What unlocks at that level. */
  readonly title: string;
  readonly description: string;
}

/**
 * As monthly consumption grows, more unlocks. Thresholds are indicative and
 * still being finalized.
 */
export const UNLOCKS: readonly Unlock[] = [
  {
    spend: "$2,000 / mo",
    title: "Business Support",
    description: "Faster response times and a named support contact.",
  },
  {
    spend: "$4,000 / mo",
    title: "Enterprise Support",
    description: "Priority engineering and a dedicated solution architect.",
  },
  {
    spend: "$6,000 / mo",
    title: "BYOC",
    description: "Bring your own cloud, run Nitro in your own account.",
  },
];

export const UNLOCKS_NOTE = "Thresholds are indicative and being finalized.";

export interface PricingFaq {
  readonly question: string;
  readonly answer: string;
}

export const FAQ: readonly PricingFaq[] = [
  {
    question: "Is the Free tier really free?",
    answer:
      "Yes. The Free tier runs on shared cloud and includes schemas and environments, 1M operations and 2 GB of ingest per month, and 3-day retention, at no cost. When you need more, move to Pay as you go.",
  },
  {
    question: "How does Pay as you go billing work?",
    answer:
      "Pay as you go is $20 per month and includes 5M operations and 2 GB of ingest per million operations, with 60-day retention. Beyond the included volume you pay $2 per additional million operations and $1.15 per additional gigabyte of ingest.",
  },
  {
    question: "How is the Dedicated instance priced?",
    answer:
      "A Dedicated instance is single-tenant and priced by volume, the compute, storage, and nodes it runs on, starting from $400 per month. Retention is configurable, and you can run it in your own cloud (BYOC).",
  },
  {
    question: "What support do I get, and how does it grow?",
    answer:
      "Free includes community support and Pay as you go adds email support. As your monthly consumption grows you unlock Business support at $2,000 a month and Enterprise support at $4,000 a month, with priority engineering and a dedicated solution architect. Talk to us about the right plan.",
  },
  {
    question: "Do you support SSO and audit logs?",
    answer:
      "SSO via OIDC, role-based access control, and an admin audit log are included on the Dedicated and Self-Hosted plans. The Free and Pay as you go tiers ship role-based access control.",
  },
  {
    question: "Can I move between tiers later?",
    answer:
      "You can move between Free and Pay as you go yourself at any time, and your schemas, environments, and telemetry come with you. Moving to a Dedicated instance or self-hosting is a conversation, talk to us and we'll help you migrate.",
  },
];
