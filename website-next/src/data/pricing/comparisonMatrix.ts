// 5-column feature matrix for /pricing.
// Columns are ordered: OSS, Nitro Free, Nitro Hosted, Nitro Self-Hosted, Enterprise.
// Cells use a discriminated union so the renderer can present unit prices the
// same way Vercel does (included quantity + per-unit overage in one cell).

export type ComparisonColumnKey =
  | "oss"
  | "nitro-free"
  | "nitro-hosted"
  | "nitro-self-hosted"
  | "enterprise";

export interface ComparisonColumn {
  readonly key: ComparisonColumnKey;
  readonly label: string;
  readonly priceLabel: string;
  readonly subLabel: string;
  readonly accent?: boolean;
}

export const COMPARISON_COLUMNS: readonly ComparisonColumn[] = [
  {
    key: "oss",
    label: "Open Source",
    priceLabel: "MIT-licensed",
    subLabel: "Run it yourself, forever",
    accent: true,
  },
  {
    key: "nitro-free",
    label: "Nitro Free",
    priceLabel: "$0",
    subLabel: "Hosted, shared, pay-per-request",
  },
  {
    key: "nitro-hosted",
    label: "Nitro Hosted",
    priceLabel: "From $499 / mo",
    subLabel: "Single-tenant, reserved capacity",
  },
  {
    key: "nitro-self-hosted",
    label: "Nitro Self-Hosted",
    priceLabel: "License",
    subLabel: "Your infra, your network",
  },
  {
    key: "enterprise",
    label: "Enterprise",
    priceLabel: "Custom",
    subLabel: "SLA, support, federation governance",
  },
];

export type Cell =
  | { readonly kind: "check" }
  | { readonly kind: "value"; readonly label: string }
  | {
      readonly kind: "meter";
      readonly included: string;
      readonly unit?: string;
      readonly overage?: string;
    }
  | { readonly kind: "custom" }
  | { readonly kind: "none" };

export type Cells = Readonly<Record<ComparisonColumnKey, Cell>>;

export interface Row {
  readonly label: string;
  readonly hint?: string;
  readonly cells: Cells;
}

export interface RowGroup {
  readonly title: string;
  readonly summary?: string;
  readonly rows: readonly Row[];
}

const check: Cell = { kind: "check" };
const dash: Cell = { kind: "none" };
const custom: Cell = { kind: "custom" };
const value = (label: string): Cell => ({ kind: "value", label });
const meter = (included: string, unit?: string, overage?: string): Cell => ({
  kind: "meter",
  included,
  unit,
  overage,
});

export const COMPARISON_MATRIX: readonly RowGroup[] = [
  {
    title: "Schema & runtime",
    summary:
      "The engine itself: Hot Chocolate server, Mocha messaging, Strawberry Shake client, and the OSS edition of Fusion.",
    rows: [
      {
        label: "Hot Chocolate (server)",
        cells: {
          oss: check,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Strawberry Shake (client)",
        cells: {
          oss: check,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Mocha (messaging)",
        cells: {
          oss: check,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Fusion (gateway)",
        cells: {
          oss: check,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "GraphiQL + IDE tooling",
        cells: {
          oss: check,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
    ],
  },
  {
    title: "Schema registry & CI",
    summary:
      "Track every schema version, catch breaking changes before they ship, govern federation composition.",
    rows: [
      {
        label: "Hosted schema registry",
        cells: {
          oss: dash,
          "nitro-free": value("3 environments"),
          "nitro-hosted": value("Unlimited"),
          "nitro-self-hosted": value("Unlimited"),
          enterprise: value("Unlimited"),
        },
      },
      {
        label: "Breaking-change detection in CI",
        cells: {
          oss: dash,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Schema diffs + version history",
        cells: {
          oss: dash,
          "nitro-free": value("30 days"),
          "nitro-hosted": value("365 days"),
          "nitro-self-hosted": value("Unlimited"),
          enterprise: value("Unlimited"),
        },
      },
      {
        label: "Fusion composition checks",
        cells: {
          oss: dash,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Composition policies + access control",
        hint: "Block subgraph publishes that violate platform-wide policies.",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": dash,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
    ],
  },
  {
    title: "Operate & observe",
    summary:
      "OpenTelemetry-native traces, error rates, cost per operation. Hosted retention so you don't pay your APM vendor twice.",
    rows: [
      {
        label: "OpenTelemetry export",
        cells: {
          oss: check,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Hosted traces + retention",
        cells: {
          oss: dash,
          "nitro-free": value("7 days"),
          "nitro-hosted": value("30 days"),
          "nitro-self-hosted": value("BYO storage"),
          enterprise: value("Custom"),
        },
      },
      {
        label: "Error rate + p95/p99 dashboards",
        cells: {
          oss: dash,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Cost-per-operation analytics",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "SLO dashboards + burn-rate alerts",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
    ],
  },
  {
    title: "Identity & security",
    rows: [
      {
        label: "GitHub / Google login",
        cells: {
          oss: dash,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "SSO (SAML / OIDC)",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "SCIM provisioning",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "RBAC + custom roles",
        cells: {
          oss: dash,
          "nitro-free": value("3 roles"),
          "nitro-hosted": value("Unlimited"),
          "nitro-self-hosted": value("Unlimited"),
          enterprise: value("Unlimited"),
        },
      },
      {
        label: "Audit log retention",
        cells: {
          oss: dash,
          "nitro-free": value("30 days"),
          "nitro-hosted": value("365 days"),
          "nitro-self-hosted": value("BYO storage"),
          enterprise: value("Custom"),
        },
      },
      {
        label: "IP allowlist + private networking",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
    ],
  },
  {
    title: "Compute & throughput",
    summary:
      "What you actually pay per request, with the unit price next to the included quantity. No surprise on the invoice.",
    rows: [
      {
        label: "Requests / month included",
        cells: {
          oss: value("Unlimited"),
          "nitro-free": meter("1M / mo", "1M requests", "$1.00"),
          "nitro-hosted": meter("10M / mo", "1M requests", "$0.40"),
          "nitro-self-hosted": value("Unlimited"),
          enterprise: custom,
        },
      },
      {
        label: "Concurrent connections",
        cells: {
          oss: value("Unlimited"),
          "nitro-free": value("100"),
          "nitro-hosted": value("10,000"),
          "nitro-self-hosted": value("Unlimited"),
          enterprise: custom,
        },
      },
      {
        label: "Region pinning",
        cells: {
          oss: check,
          "nitro-free": dash,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Hard usage cap + budget alerts",
        hint: "Traffic is throttled at the cap. Pay-as-you-go is opt-in.",
        cells: {
          oss: dash,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
    ],
  },
  {
    title: "Deployment shape",
    rows: [
      {
        label: "Managed (we run it)",
        cells: {
          oss: dash,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": dash,
          enterprise: check,
        },
      },
      {
        label: "Single-tenant cluster",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Bring-your-own infrastructure",
        cells: {
          oss: check,
          "nitro-free": dash,
          "nitro-hosted": dash,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Air-gapped install",
        cells: {
          oss: check,
          "nitro-free": dash,
          "nitro-hosted": dash,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
    ],
  },
  {
    title: "Support",
    rows: [
      {
        label: "Community (GitHub + Discord)",
        cells: {
          oss: check,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "Business-hours response SLA",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": value("8h"),
          "nitro-self-hosted": value("8h"),
          enterprise: value("1h"),
        },
      },
      {
        label: "24x7 oncall",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": dash,
          "nitro-self-hosted": value("Add-on"),
          enterprise: check,
        },
      },
      {
        label: "Dedicated solution architect",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": dash,
          "nitro-self-hosted": dash,
          enterprise: check,
        },
      },
      {
        label: "Custom uptime SLA",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": value("99.9%"),
          "nitro-self-hosted": value("99.9%"),
          enterprise: value("99.99%+"),
        },
      },
    ],
  },
  {
    title: "Compliance",
    summary:
      "Procurement-ready evidence. Reports and questionnaires available under NDA.",
    rows: [
      {
        label: "GDPR-ready DPA",
        cells: {
          oss: check,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "SOC 2 Type II",
        cells: {
          oss: dash,
          "nitro-free": check,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "ISO 27001",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": check,
          "nitro-self-hosted": check,
          enterprise: check,
        },
      },
      {
        label: "HIPAA BAA",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": dash,
          "nitro-self-hosted": value("Add-on"),
          enterprise: check,
        },
      },
      {
        label: "Custom security questionnaires",
        cells: {
          oss: dash,
          "nitro-free": dash,
          "nitro-hosted": dash,
          "nitro-self-hosted": value("Add-on"),
          enterprise: check,
        },
      },
      {
        label: "Data residency (EU / US / custom)",
        cells: {
          oss: check,
          "nitro-free": value("US"),
          "nitro-hosted": value("EU / US"),
          "nitro-self-hosted": check,
          enterprise: custom,
        },
      },
    ],
  },
];
