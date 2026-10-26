/** The one-sentence definition shared by the What is section and structured data. */
export const FEDERATION_DEFINITION =
  "GraphQL Federation is an architectural approach that combines multiple independent GraphQL services, called subgraphs, into a single unified GraphQL API.";

export interface SubgraphServerExample {
  readonly server: string;
  readonly language: string;
}

/**
 * GraphQL servers named as subgraph examples, in one order for every place the
 * list renders. Hot Chocolate is one server among many and stays last.
 */
export const SUBGRAPH_SERVER_EXAMPLES: readonly SubgraphServerExample[] = [
  { server: "Spring for GraphQL", language: "Java or Kotlin" },
  { server: "NestJS or GraphQL Yoga", language: "Node.js" },
  { server: "gqlgen", language: "Go" },
  { server: "Strawberry", language: "Python" },
  { server: "async-graphql", language: "Rust" },
  { server: "graphql-ruby", language: "Ruby" },
  { server: "Hot Chocolate", language: ".NET" },
];
