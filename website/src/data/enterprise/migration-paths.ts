// Migration cards for Section 12. Three sources, one outcome: a Fusion mesh
// running on Nitro. Each card states the source plainly, the changed shape
// of the deployment, and offers a solution architect.

export type MigrationKey = "apollo" | "hasura" | "bff";

export interface MigrationPath {
  readonly key: MigrationKey;
  readonly source: string;
  readonly headline: string;
  readonly body: string;
  readonly cta: string;
  readonly ctaHref: string;
}

export const MIGRATION_PATHS: readonly MigrationPath[] = [
  {
    key: "apollo",
    source: "Apollo Federation",
    headline: "From Apollo Router to Fusion Mesh.",
    body: "Build-time composition replaces runtime supergraph negotiation. Same federation contract, polyglot subgraphs, no Rust router to operate.",
    cta: "Talk to a solution architect",
    ctaHref: "/contact/sales?interest=fusion",
  },
  {
    key: "hasura",
    source: "Hasura",
    headline: "From Hasura to Fusion + Nitro.",
    body: "Keep the auto-generated CRUD layer where it serves you. Federate it with the rest of your services and run it on infra you control.",
    cta: "Talk to a solution architect",
    ctaHref: "/contact/sales?interest=fusion",
  },
  {
    key: "bff",
    source: "Hand-rolled BFFs",
    headline: "From N BFFs to one mesh.",
    body: "Stop maintaining a per-product backend-for-frontend. Compose one governed surface and let product teams own their slice without re-implementing auth, tracing, and caching every time.",
    cta: "Talk to a solution architect",
    ctaHref: "/contact/sales?interest=fusion",
  },
];
