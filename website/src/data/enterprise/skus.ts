// Three named enterprise SKUs (Section 08 of the design brief). Each one is
// a single noun with a one-line tagline, mirroring Vercel's
// Conformance / Secure Compute / Rolling Release pattern so platform teams
// can paste them straight into a vendor matrix.

export type SkuKey = "fusion-mesh" | "nitro-control-plane" | "agent-bridge";

export interface Sku {
  readonly key: SkuKey;
  readonly name: string;
  readonly tagline: string;
  readonly bullets: readonly string[];
  readonly docsHref: string;
  readonly docsLabel: string;
}

export const SKUS: readonly Sku[] = [
  {
    key: "fusion-mesh",
    name: "Fusion Mesh",
    tagline: "Federate any backend in any language, on .NET or off.",
    bullets: [
      "Build-time composition, zero runtime gateway logic",
      "Polyglot subgraphs — Java, Go, Rust, Python, .NET",
      "Schema lineage and breaking-change detection",
      "Run on Nitro, your own infra, or air-gapped",
    ],
    docsHref: "/docs/fusion",
    docsLabel: "Read Fusion docs",
  },
  {
    key: "nitro-control-plane",
    name: "Nitro Control Plane",
    tagline: "Schema registry, observability, governance, and rollouts.",
    bullets: [
      "Schema registry with environment promotion",
      "OpenTelemetry traces, metrics, and logs",
      "RBAC, SSO/SAML, SCIM, audit log export",
      "Canary rollouts and instant rollback",
    ],
    docsHref: "/docs/nitro",
    docsLabel: "Read Nitro docs",
  },
  {
    key: "agent-bridge",
    name: "Agent Bridge",
    tagline:
      "Your platform, legible to your coding agent — behind your firewall.",
    bullets: [
      "MCP server backed by your live schema",
      "Trace + topology context, not just types",
      "Per-agent RBAC and audit trail",
      "On-prem or VPC-bound, never leaves your network",
    ],
    docsHref: "/docs/agents",
    docsLabel: "Read Agent Bridge docs",
  },
];
