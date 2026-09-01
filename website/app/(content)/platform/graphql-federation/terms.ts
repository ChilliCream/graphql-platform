/** The one-sentence definition shared by the hero copy and the structured data. */
export const FEDERATION_DEFINITION =
  "GraphQL Federation is an architectural pattern that combines the schemas of independent GraphQL services (subgraphs), each owned by a different team, into one composite schema (also called a supergraph) that a gateway serves to clients as a single API.";

export interface FederationTerm {
  readonly term: string;
  readonly meaning: string;
}

/** Vocabulary as the GraphQL Federation specification defines it. */
export const FEDERATION_TERMS: readonly FederationTerm[] = [
  { term: "Subgraph", meaning: "An upstream service behind the gateway." },
  {
    term: "Source schema",
    meaning: "The schema document a subgraph publishes.",
  },
  {
    term: "Composition",
    meaning:
      "The build step that validates the source schemas and merges them into one schema.",
  },
  {
    term: "Composite schema",
    meaning:
      "The single, client-facing schema that composition produces. Clients query it as if it were one API.",
  },
  {
    term: "Gateway",
    meaning:
      "The public entry point. It receives queries written against the composite schema.",
  },
  {
    term: "Distributed executor",
    meaning:
      "The part that plans a query across subgraphs, fetches from each and assembles one response.",
  },
  {
    term: "Entity",
    meaning:
      "A type with a stable key that can be referenced across subgraphs.",
  },
  {
    term: "Key",
    meaning: "The fields that identify an entity, declared with @key.",
  },
  {
    term: "Lookup",
    meaning:
      "A field that fetches an entity by one of its keys, declared with @lookup.",
  },
];
