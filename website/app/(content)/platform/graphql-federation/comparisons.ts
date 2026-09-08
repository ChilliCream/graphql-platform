export interface FederationComparison {
  readonly slug: string;
  readonly title: string;
  readonly summary: string;
  readonly href: string;
  /**
   * Meta description of the comparison page, when the summary is too long to
   * serve as one. Defaults to {@link FederationComparison.summary}.
   */
  readonly metaDescription?: string;
  /** Search intent the comparison page is written for. Editors only; not emitted. */
  readonly keywords?: readonly string[];
}

/**
 * The comparison link list shown in the "What is GraphQL Federation?" section.
 * Each `href` points at an in-page anchor for now; the comparison-page tasks
 * (epic graphql-platform-7ok) will repoint each one to its own route at
 * `/platform/graphql-federation/<slug>`.
 */
export const COMPARISONS: readonly FederationComparison[] = [
  {
    slug: "vs-bff",
    title: "GraphQL Federation vs BFF (backend for frontend)",
    summary:
      "A backend for frontend wins while one client has needs no other client shares; GraphQL Federation wins once several clients need the same data assembled and nobody wants a separate backend per client.",
    href: "#vs-bff",
  },
  {
    slug: "vs-individual-apis",
    title: "GraphQL Federation vs individual APIs",
    summary:
      "Calling each service directly wins while a screen needs one or two of them; GraphQL Federation wins once a screen needs several and every client would otherwise assemble the pieces again.",
    href: "#vs-individual-apis",
  },
  {
    slug: "vs-graphql-monolith",
    title: "GraphQL Federation vs a GraphQL monolith",
    summary:
      "One GraphQL server wins for one team, or for teams that can still ship together; GraphQL Federation wins once teams need to change and deploy their part of the API on their own schedules.",
    href: "#vs-graphql-monolith",
  },
  {
    slug: "vs-apollo-federation",
    title: "GraphQL Federation vs Apollo Federation",
    summary:
      "Apollo Federation wins where a team is already invested in its router and its subgraph libraries; GraphQL Federation wins where the goal is an open standard, under the GraphQL Foundation, that any GraphQL server can join with nothing beyond its schema.",
    href: "#vs-apollo-federation",
  },
];
