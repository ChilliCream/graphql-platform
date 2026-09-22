/**
 * The copy for the Fusion product page: the hero, the body sections, the
 * feature grid and the closing Nitro band.
 */

export interface CopyLink {
  readonly label: string;
  readonly href: string;
}

/**
 * A titled block of the page copy, where a paragraph's linked phrase appears
 * in `links` with its href and verbatim in one of the paragraphs.
 * `inPractice` holds the links of the trailing "In practice: ..." line,
 * rendered as the labels joined by " and " and closed with a period.
 */
export interface CopySection {
  readonly id: string;
  readonly title: string;
  readonly paragraphs: readonly string[];
  readonly links: readonly CopyLink[];
  readonly inPractice: readonly CopyLink[];
}

export const HERO = {
  title: "Fusion",
  teaser:
    "The API gateway that composes your teams' subgraphs into one coherent graph and executes every query across them. Fusion is the only API gateway that supports both the GraphQL Federation specification and Apollo Federation, and it composes OpenAPI and gRPC sources too.",
  buttons: [
    { label: "Get Started", href: "/docs/fusion/getting-started" },
    {
      label: "Contact an Expert",
      href: "/services/support/contact?subject=Sales&context=Fusion",
    },
  ],
} as const satisfies {
  readonly title: string;
  readonly teaser: string;
  readonly buttons: readonly CopyLink[];
};

export const SECTIONS: readonly CopySection[] = [
  {
    id: "what-is-fusion",
    title: "What is Fusion?",
    paragraphs: [
      "Fusion is an API gateway. Your teams keep their own services and their own schemas; Fusion composes those subgraphs into one coherent graph and serves it at a single endpoint. A client sends one query, the gateway works out which subgraphs hold the data, calls them, and returns one response.",
      "Composition happens in your build, not at runtime, so contract conflicts are caught before anything is deployed. A subgraph is an ordinary service: a GraphQL server in any language, or a service that publishes an OpenAPI document or a gRPC definition. If federation itself is new to you, start with the GraphQL Federation page.",
    ],
    links: [
      {
        label: "the GraphQL Federation page",
        href: "/platform/graphql-federation",
      },
    ],
    inPractice: [],
  },
  {
    id: "both-specifications",
    title: "Both specifications, one gateway",
    paragraphs: [
      "Fusion is the only gateway that supports both the GraphQL Federation specification and Apollo Federation. Subgraphs written to either protocol compose into the same graph, so a team can adopt either one, run both side by side, or move a single subgraph across without a coordinated cutover.",
      "Fusion also composes sources that are not GraphQL servers at all: a service that publishes an OpenAPI document or a gRPC definition joins the same graph, with its contract validated in the same composition step.",
    ],
    links: [],
    inPractice: [
      {
        label: "migrating a subgraph from Apollo Federation",
        href: "/docs/fusion/migration/coming-from-apollo-federation",
      },
    ],
  },
  {
    id: "any-server",
    title: "Any GraphQL server, no plugin",
    paragraphs: [
      "Subgraphs can be written in any language. A GraphQL subgraph stays an ordinary GraphQL server: it declares its keys and lookups in its own schema, the gateway calls it with ordinary GraphQL queries, and there is no distributed-runtime package or vendor protocol layer to install alongside it. The servers listed on the GraphQL Federation page qualify on the same terms, and so does any other GraphQL server.",
      "The one build step you add is composition. It validates the subgraphs against one another, and type conflicts, missing fields and incompatible enums fail the pipeline instead of the gateway.",
    ],
    links: [
      {
        label: "listed on the GraphQL Federation page",
        href: "/platform/graphql-federation#specification",
      },
    ],
    inPractice: [
      {
        label: "how composition validates subgraphs",
        href: "/docs/fusion/composition",
      },
      {
        label: "how to run it in your pipeline",
        href: "/docs/fusion/deployment-and-ci-cd",
      },
    ],
  },
  {
    id: "client-safety",
    title: "Composition protects the graph, Nitro protects your clients",
    paragraphs: [
      "Composition catches conflicts between subgraphs, and that is where federation's guarantees end. Nothing in it stops a team from removing a field that a mobile app still queries: the subgraphs still compose, the build stays green, and the query fails in the hands of a client the subgraph team never sees.",
      "Nitro closes that gap. Its schema governance compares every schema change with the operations published by real clients and tells the team what is safe, risky or breaking before the change is merged.",
    ],
    links: [],
    inPractice: [
      { label: "schema governance in Nitro", href: "/products/nitro#schema" },
    ],
  },
];

export const NITRO_BAND = {
  id: "nitro",
  title: "Know what a schema change does to real clients.",
  description:
    "Nitro validates every schema change against the operations your registered clients actually run, and its Fusion dashboard reports latency, throughput and error rate for the gateway and for each subgraph behind it.",
  buttons: [
    { label: "Start Nitro for Free", href: "https://nitro.chillicream.com" },
    { label: "Meet Nitro", href: "/products/nitro" },
  ],
} as const satisfies {
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly buttons: readonly CopyLink[];
};

export interface Feature {
  readonly title: string;
  readonly description: string;
}

export const FEATURES: readonly Feature[] = [
  {
    title: "Both Federation Protocols",
    description:
      "Subgraphs written to the GraphQL Federation specification and subgraphs written to Apollo Federation compose into the same graph, so you can move one subgraph at a time.",
  },
  {
    title: "OpenAPI and gRPC Sources",
    description:
      "A service that publishes an OpenAPI document or a gRPC definition joins the same graph, with its contract validated in the same composition step.",
  },
  {
    title: "Any Language, Any Server",
    description:
      "A GraphQL subgraph stays an ordinary GraphQL server that declares its keys and lookups in its own schema. There is no distributed-runtime package to install alongside it.",
  },
  {
    title: "Composition in Your Pipeline",
    description:
      "Composition validates the subgraphs against one another before deployment, so type conflicts, missing fields, and incompatible enums fail the build instead of the gateway.",
  },
  {
    title: "Hot-Swapped Configuration",
    description:
      "Publish a composed Fusion configuration from your pipeline to Nitro. The gateway subscribes to the latest archive and swaps it in without a restart.",
  },
  {
    title: "Federated Subscriptions",
    description:
      "Stream results that span several subgraphs with broker-backed federated event streams, or subscribe to a subgraph over Server-Sent Events.",
  },
];
