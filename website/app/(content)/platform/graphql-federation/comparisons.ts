export interface FederationComparison {
  readonly slug: string;
  readonly title: string;
  readonly summary: string;
  readonly href: string;
}

/**
 * The comparison link list shown in the "What is GraphQL Federation?" section.
 * Each `href` points at the entry's article in the comparison section at
 * `/comparison/graphql-federation-vs-<x>`.
 */
export const COMPARISONS: readonly FederationComparison[] = [
  {
    slug: "vs-bff",
    title: "GraphQL Federation vs BFF (backend for frontend)",
    summary:
      "A backend for frontend wins while one client has needs no other client shares; GraphQL Federation wins once several clients need the same data assembled and nobody wants a separate backend per client.",
    href: "/comparison/graphql-federation-vs-bff",
  },
  {
    slug: "vs-individual-apis",
    title: "GraphQL Federation vs individual APIs",
    summary:
      "Calling each service directly wins while a screen needs one or two of them; GraphQL Federation wins once a screen needs several and every client would otherwise assemble the pieces again.",
    href: "/comparison/graphql-federation-vs-individual-apis",
  },
  {
    slug: "vs-graphql-monolith",
    title: "GraphQL Federation vs a GraphQL monolith",
    summary:
      "One GraphQL server wins for one team, or for teams that can still ship together; GraphQL Federation wins once teams need to change and deploy their part of the API on their own schedules.",
    href: "/comparison/graphql-federation-vs-graphql-monolith",
  },
  {
    slug: "vs-apollo-federation",
    title: "GraphQL Federation vs Apollo Federation",
    summary:
      "Apollo Federation wins where a team is already invested in its router and its subgraph libraries; GraphQL Federation wins where the goal is an open standard, under the GraphQL Foundation, that any GraphQL server can join with nothing beyond its schema.",
    href: "/comparison/graphql-federation-vs-apollo-federation",
  },
];
