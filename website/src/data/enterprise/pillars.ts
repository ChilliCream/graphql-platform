// Three platform pillars for /enterprise (Section 03). One-liners are taken
// verbatim from the design brief (.work/analysis/12-enterprise.md §6) so the
// page reads as the canonical statement of what the platform does.

export type PillarKey = "federate" | "operate" | "agents";

export interface Pillar {
  readonly key: PillarKey;
  readonly title: string;
  readonly tagline: string;
}

export const PILLARS: readonly Pillar[] = [
  {
    key: "federate",
    title: "Federate",
    tagline:
      "Compose hundreds of services into one GraphQL mesh. Polyglot, build-time, no .NET required at the edges.",
  },
  {
    key: "operate",
    title: "Operate",
    tagline:
      "Nitro on your infra. Tracing, schema registry, RBAC, and audit log — air-gapped if you need it.",
  },
  {
    key: "agents",
    title: "Adopt agents",
    tagline:
      "Your schema, your traces, your topology, exposed to any LLM via MCP — without leaving your firewall.",
  },
];
