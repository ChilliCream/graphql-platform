export interface FederationFaqItem {
  readonly question: string;
  readonly answer: string;
}

export const FEDERATION_FAQ_ITEMS: readonly FederationFaqItem[] = [
  {
    question: "What is federation in GraphQL?",
    answer:
      "GraphQL Federation serves one GraphQL API from many independent services. Each service, called a subgraph, publishes a source schema; composition merges those into one composite schema; and a gateway serves it at a single endpoint, with a distributed executor fetching each field from the subgraph that provides it. Clients see one graph.",
  },
  {
    question: "What is a federated graph, a supergraph, or a composite schema?",
    answer:
      "Supergraph and composite schema name the same thing: the single schema clients query, produced by merging the schemas of several services. Supergraph is the word in the Apollo Federation ecosystem; the GraphQL Federation specification calls it the composite schema. Federated graph is the informal name for the whole arrangement: the subgraphs, the composite schema, and the gateway.",
  },
  {
    question: "What is GraphQL, and why is it used?",
    answer:
      "GraphQL is a query language for APIs. An API publishes a schema, a typed document that lists every field a client can ask for, and a client sends one query naming exactly the fields it needs and gets exactly those back in one response. It is used because one request can describe a whole screen and because the schema is a contract that tools can check.",
  },
  {
    question: "How is GraphQL Federation different from schema stitching?",
    answer:
      "Stitching merges schemas at runtime with hand-written glue resolvers in the gateway, and that glue drifts as the underlying schemas change. Federation puts the relationships into the source schemas themselves, as keys, lookups, and requirements, and validates the merged result at build time, so a conflict is a failed build instead of a runtime surprise.",
  },
  {
    question:
      "What is the difference between Apollo Federation and GraphQL Federation?",
    answer:
      "Both describe the same architecture: subgraphs, composition, and a gateway. Apollo Federation is Apollo's specification; GraphQL Federation is the vendor-neutral specification developed at the GraphQL Foundation. Apollo Federation fetches entities through a hidden _entities field backed by reference resolvers and declares requirements with @requires on a field. GraphQL Federation uses ordinary query fields marked @lookup and declares requirements on arguments with @require, so a subgraph has no subgraph specification to implement.",
  },
  {
    question:
      "Do I need a plugin or library to make my GraphQL server a subgraph?",
    answer:
      "No. Under the GraphQL Federation specification, only its schema changes: you declare keys with @key, lookups with @lookup, and requirements with @require. There are no hidden fields to add, no reference resolvers to write, and no subgraph specification to implement. The distributed executor talks to your server with ordinary GraphQL queries, so any server that answers a GraphQL query can be a subgraph.",
  },
  {
    question: "Which languages can subgraphs be written in?",
    answer:
      "Any language with a GraphQL server: Java or Kotlin with Spring for GraphQL, Node.js with NestJS or GraphQL Yoga, Go with gqlgen, Python with Strawberry, Rust with async-graphql, Ruby with graphql-ruby, .NET with Hot Chocolate. The gateway talks to a subgraph with ordinary GraphQL queries over HTTP, so the language behind the endpoint does not matter. A service that only speaks REST or gRPC joins through a GraphQL server in front of it, or through a gateway that composes OpenAPI and gRPC sources directly.",
  },
  {
    question:
      "Does GraphQL Federation cause N+1 requests between the gateway and subgraphs?",
    answer:
      "It does not have to. The executor plans a query once, calls independent subgraphs in parallel, and treats batching as a transport concern: variable batching, being added to the GraphQL over HTTP specification, sends one query with a list of variable sets, so a hundred products become one request rather than a hundred lookups. What remains is one extra hop and a plan that most gateways cache after the first request.",
  },
  {
    question: "Is GraphQL Federation overkill for a small team?",
    answer:
      "Usually. One team on one service is better served by a single GraphQL server. Because every GraphQL server is already a valid subgraph under the GraphQL Federation specification, you can start with one server and federate later by declaring keys and lookups in its schema, without changing its clients.",
  },
  {
    question: "What is the difference between a subgraph and a source schema?",
    answer:
      "The subgraph is the service running behind the gateway. The source schema is the schema document that subgraph publishes. Composition reads source schemas only; it never sees the services.",
  },
  {
    question: "What are entities, keys, and lookups?",
    answer:
      "An entity is a type with a stable key that can be referenced across subgraphs, such as a Product identified by its id. The key, declared with @key, gives identity. A lookup, declared with @lookup on an ordinary query field, gives recall: it fetches the entity by one of its keys. A key with no matching lookup can still identify or cache an entity but cannot fetch it.",
  },
  {
    question:
      "Can Apollo Federation and GraphQL Federation subgraphs be mixed in one gateway?",
    answer:
      "The specifications are distinct, but one gateway can compose both. Fusion composes source schemas from subgraphs written to either specification into one composite schema, so a migration can happen one subgraph at a time, or not at all.",
  },
  {
    question: "What happened to the Composite Schemas Specification?",
    answer:
      "It is being renamed. In 2023 Apollo, ChilliCream, and The Guild formed the Composite Schemas Working Group at the GraphQL Foundation to write a vendor-neutral standard for federated GraphQL schemas. That specification is becoming the GraphQL Federation Specification: the same document and the same working group.",
  },
];
