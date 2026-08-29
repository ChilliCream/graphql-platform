// Seed content for /learn.
//
// Templates (website-yw2.1): the 8 seed template entries below were removed.
// Every one carried a githubUrl under github.com/ChilliCream/templates, and
// that org/repo does not exist (`gh api repos/ChilliCream/templates` returns
// 404 for the repo itself, not just a path within it). TEMPLATE_ITEMS is an
// empty array until website-yw2.2 rebuilds the catalog from real
// ChilliCream repos. The TemplateItem shape (index card front-matter,
// sticky deploy sidebar metadata, body[] of section
// { heading, paragraphs[], code? }) stays in place as the seam yw2.2 fills.
//
// Video/tutorial/example/workshop content (website-5yo.6) links out to real,
// verified material: existing ChilliCream YouTube videos, docs tutorials
// already published under /docs, and public github.com/ChilliCream example
// and workshop repos. No detail pages exist for these types yet (see the
// parent ticket); externalUrl is the only place a reader lands.
//
// Tagging discipline: every template carries a value for all 6 filter axes
// (Topology, Use case, Language, Client, Product mix, Agent-ready). Missing
// any axis breaks faceted filtering silently.

import { resolveYouTubePoster } from "@/src/components/YouTubePoster";
import type {
  ExampleItem,
  LearnItem,
  LearnItemSummary,
  TemplateItem,
  TemplateSummary,
  TutorialItem,
  WorkshopItem,
} from "./types";
import type { VideoItem } from "./types";

// -----------------------------------------------------------------------------
// Templates
// -----------------------------------------------------------------------------

// The 8 seed templates (fusion-3-service-federation, agent-ready-api,
// polyglot-federation, cqrs-with-mocha, realtime-subscriptions,
// fusion-with-nitro-observability, multi-tenant-saas-starter,
// blazor-strawberry-shake) were removed here: every githubUrl and demoUrl
// pointed at github.com/ChilliCream/templates or demo.chillicream.com, and
// the templates org/repo itself returns 404 (verified via
// `gh api repos/ChilliCream/templates`, website-yw2.1). Their detail pages
// at /learn/templates/<slug> and their sitemap entries disappear along with
// them (generateStaticParams and the sitemap both read off this array).
// website-yw2.2 repopulates it from real repos.
export const TEMPLATE_ITEMS: readonly TemplateItem[] = [];

// -----------------------------------------------------------------------------
// Videos
// -----------------------------------------------------------------------------

const graphqlObservabilityVideo: VideoItem = {
  type: "video",
  slug: "graphql-observability-elastic-opentelemetry",
  title: "GraphQL Observability with Elastic and OpenTelemetry",
  tagline: "Michael Staib instruments a Hot Chocolate server with OpenTelemetry and traces it through Elastic.",
  products: ["hot-chocolate"],
  // Subject is observability, not the server itself; `products` alone would
  // only place it in GraphQL & Federation (see src/data/learn/hubs.ts).
  hubs: ["tooling-observability"],
  level: "intermediate",
  url: "https://www.youtube.com/watch?v=nCLSfJMihsg",
  duration: "51:49",
  updatedRelative: "4 years ago",
};

const getStartedGraphqlBlazorVideo: VideoItem = {
  type: "video",
  slug: "getting-started-graphql-blazor",
  title: "Getting Started with GraphQL and Blazor",
  tagline: "A walkthrough of wiring a Strawberry Shake GraphQL client into a Blazor WebAssembly app.",
  products: ["strawberry-shake"],
  level: "beginner",
  url: "https://www.youtube.com/watch?v=-oq7YEciouM",
  duration: "21:46",
  updatedRelative: "4 years ago",
};

// The 7 videos migrated from the retired ChilliCream TV site (website-hnm.2).
// Source of truth: .nitro/agents/tv-videos-snapshot.json, a snapshot of
// persisted query 72db3d7fc8aa8fce76906ccc1adb0d14 against that site's
// GraphQL API. Descriptions are cleaned of the repeated social-links/support
// boilerplate and the dead "Source Code" link every TV description carried
// (it pointed back at the retired site); the intro paragraphs, Courses
// link, and Hot Chocolate version note are kept. Each entry originally
// carried an exampleRepoUrl under github.com/ChilliCream/examples/tree/main/<slug>,
// following the template githubUrl convention; that org/repo returns 404
// (verified via `gh api repos/ChilliCream/examples`, website-yw2.1), so the
// field was dropped rather than left linking to a repo that doesn't exist
// (website-kbx.23 tracked creating it).

const dataLoaderStateContextVideo: VideoItem = {
  type: "video",
  slug: "dataloader-state-context-aware-fetching",
  title: "How to Use State in DataLoader for Context-Aware Fetching",
  tagline:
    "Michael Staib shows how to make DataLoaders stateful, so you can scope fetches by tenant or auth context without losing batching.",
  description:
    "In this episode, we're going to explore how to make your DataLoaders stateful, so you can pass in things like tenant IDs or authorization context to fetch data in the correct scope without compromising performance.\n\nSo buckle up, and let's jump in!\n\nCourses: https://courses.chillicream.com/youtube/4Mw2A548OGM\n\nHot Chocolate GraphQL .NET server version used in this video: https://www.nuget.org/packages/HotChocolate/15.1.4",
  products: ["hot-chocolate"],
  url: "https://www.youtube.com/watch?v=4Mw2A548OGM",
  youtubeId: "4Mw2A548OGM",
  duration: "15:52",
  publishedAt: "2025-05-01T09:51:56.995282Z",
  updatedRelative: "over a year ago",
};

const efCoreProjectionsVideo: VideoItem = {
  type: "video",
  slug: "ef-core-projections-graphql-performance",
  title: "Boost GraphQL Performance with EF Core Projections",
  tagline: "Michael Staib tours Hot Chocolate's new EF Core projections engine for faster, smarter GraphQL queries.",
  description:
    "In this episode, we're taking a look at the new projections engine in Hot Chocolate: what's changed, what's new, and how it can help you write smarter, faster GraphQL queries with EF Core.\n\nSo buckle up, and let's jump in!\n\nCourses: https://courses.chillicream.com/youtube/dYSqssul4jY\n\nHot Chocolate GraphQL .NET server version used in this video: https://www.nuget.org/packages/HotChocolate/15.1.3",
  products: ["hot-chocolate"],
  url: "https://www.youtube.com/watch?v=dYSqssul4jY",
  youtubeId: "dYSqssul4jY",
  duration: "15:28",
  publishedAt: "2025-04-01T09:17:50.285112Z",
  updatedRelative: "over a year ago",
};

const openTelemetryForServicesVideo: VideoItem = {
  type: "video",
  slug: "opentelemetry-for-services",
  title: "Open Telemetry for All Your Services (and More!)",
  tagline: "Michael Staib wires up OpenTelemetry across your services for unified tracing in Hot Chocolate.",
  // Subject is observability, not the server itself; `products` alone would
  // only place it in GraphQL & Federation (see src/data/learn/hubs.ts).
  hubs: ["tooling-observability"],
  description:
    "In this episode, we're diving into how you can leverage the new Connections API in Hot Chocolate to add aggregations to your GraphQL connections, giving your users easy access to insights like totals, counts, and more, right alongside their data.\n\nSo buckle up, and let's jump in!\n\nCourses: https://courses.chillicream.com/youtube/DtISlxOBmPQ\n\nHot Chocolate GraphQL .NET server version used in this video: https://www.nuget.org/packages/HotChocolate/15.1.1",
  products: ["hot-chocolate"],
  url: "https://www.youtube.com/watch?v=DtISlxOBmPQ",
  youtubeId: "DtISlxOBmPQ",
  duration: "12:45",
  publishedAt: "2025-03-24T23:38:17.186856Z",
  updatedRelative: "over a year ago",
};

const relativeCursorsVsOffsetPaginationVideo: VideoItem = {
  type: "video",
  slug: "relative-cursors-vs-offset-pagination",
  title: "Offset Pagination is Dead! Meet Relative Cursors",
  tagline: "Michael Staib introduces Hot Chocolate 15.1's relative-cursor paging, replacing offset pagination.",
  description:
    "In this episode, we will have a look at the new paging capabilities that come with Hot Chocolate 15.1, which will make offset pagination obsolete.\n\nSo buckle up and jump in!\n\nCourses: https://courses.chillicream.com/youtube/ZHq1pBjo0Qk\n\nHot Chocolate GraphQL .NET server version used in this video: https://www.nuget.org/packages/HotChocolate/15.1.0-p.7",
  products: ["hot-chocolate"],
  url: "https://www.youtube.com/watch?v=8TQ2oDUQ1ng",
  youtubeId: "8TQ2oDUQ1ng",
  duration: "16:02",
  publishedAt: "2025-03-17T22:53:33.686727Z",
  updatedRelative: "over a year ago",
};

const dataLoaderInLayeredArchitectureVideo: VideoItem = {
  type: "video",
  slug: "dataloader-in-layered-architecture",
  title: "Master DataLoader in Layered Architecture!",
  tagline: "Michael Staib shows how to use DataLoader in your business layer with zero dependency on Hot Chocolate.",
  description:
    "In this episode, have a look at how we can use DataLoader in our business layer without having any dependencies on Hot Chocolate.\n\nSo buckle up and jump in!\n\nCourses: https://courses.chillicream.com/youtube/ZHq1pBjo0Qk\n\nHot Chocolate GraphQL .NET server version used in this video: https://www.nuget.org/packages/HotChocolate/15.1.0-p.7",
  products: ["hot-chocolate"],
  url: "https://www.youtube.com/watch?v=ZHq1pBjo0Qk",
  youtubeId: "ZHq1pBjo0Qk",
  duration: "18:26",
  publishedAt: "2025-03-05T11:41:17.926764Z",
  updatedRelative: "over a year ago",
};

const greenDonutInActionVideo: VideoItem = {
  type: "video",
  slug: "greendonut-in-action",
  title: "The Future of Data APIs: GreenDonut in Action!",
  tagline:
    "Michael Staib demos GreenDonut.Data: paging, projections, filtering, and sorting in a layered architecture.",
  description:
    "In this episode, we will dive into the new GreenDonut.Data package and explore how we can use things like paging, projections, filtering and sorting when working with a layered architecture.\n\nSo buckle up and join me!\n\nCourses: https://courses.chillicream.com/youtube/FhNK7KMAnXc\n\nHot Chocolate GraphQL .NET server version used in this video: https://www.nuget.org/packages/HotChocolate/15.1.0-p.7",
  products: ["hot-chocolate"],
  url: "https://www.youtube.com/watch?v=FhNK7KMAnXc",
  youtubeId: "FhNK7KMAnXc",
  duration: "41:28",
  publishedAt: "2025-02-27T08:38:58.355214Z",
  updatedRelative: "over a year ago",
};

const dataLoaderExplainedVideo: VideoItem = {
  type: "video",
  slug: "dataloader-explained",
  title: "DataLoader Explained: What, Why & Where It Belongs!",
  tagline: "Michael Staib explains what DataLoader is, why you need it, and where it belongs in your project.",
  description:
    "In this episode, we will take a peek at DataLoader with Green Donut and Hot Chocolate 15. We will look at what they are, why you should use them, and where you should put them in your project.\n\nSo buckle up, join me!\n\nCourses: https://courses.chillicream.com/youtube/e0CKt3MVUfI\n\nHot Chocolate GraphQL .NET server version used in this video: https://www.nuget.org/packages/HotChocolate/15.1.0-p.3",
  products: ["hot-chocolate"],
  url: "https://www.youtube.com/watch?v=gVIxde5nlWE",
  youtubeId: "gVIxde5nlWE",
  duration: "12:19",
  publishedAt: "2025-02-19T10:23:44.273404Z",
  updatedRelative: "over a year ago",
};

// Resolved once here (rather than per-consumer) so every surface that reads
// from `VIDEO_ITEMS` (LEARN_SUMMARIES, LEARN_ITEMS, the Watch rail, the
// latest-videos rail) gets the self-hosted poster without its own lookup.
export const VIDEO_ITEMS: readonly VideoItem[] = [
  graphqlObservabilityVideo,
  getStartedGraphqlBlazorVideo,
  dataLoaderStateContextVideo,
  efCoreProjectionsVideo,
  openTelemetryForServicesVideo,
  relativeCursorsVsOffsetPaginationVideo,
  dataLoaderInLayeredArchitectureVideo,
  greenDonutInActionVideo,
  dataLoaderExplainedVideo,
].map((video) => (video.youtubeId ? { ...video, poster: resolveYouTubePoster(video.youtubeId) } : video));

// -----------------------------------------------------------------------------
// Tutorials
// -----------------------------------------------------------------------------

const getStartedNetCoreTutorial: TutorialItem = {
  type: "tutorial",
  slug: "get-started-with-graphql-in-net-core",
  title: "Getting Started with GraphQL in .NET Core",
  tagline: "Scaffold a Hot Chocolate server from the project template and run your first query in Nitro.",
  products: ["hot-chocolate", "nitro"],
  level: "beginner",
  externalUrl: "/docs/hotchocolate/get-started-with-graphql-in-net-core",
  updatedRelative: "2 months ago",
  cli: [
    { key: "install", label: "install template", code: "dotnet new install HotChocolate.Templates" },
    { key: "new", label: "scaffold project", code: "dotnet new graphql --name GettingStarted" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "The Hot Chocolate project template scaffolds a runnable GraphQL server: two record types (Author, Book), a Query class that exposes a book field, and a Program.cs wired with AddGraphQL() and MapGraphQL(). Hot Chocolate infers the schema from the C# types, so there is no SDL to hand-write to get a first query running.",
        "The [QueryType] attribute registers a class's public methods as fields on the root Query type; a source generator does the wiring at build time. RunWithGraphQLCommands(args) adds developer commands on top of the normal Run(), including exporting the schema as SDL with dotnet run -- schema export.",
      ],
      code: {
        language: "csharp",
        code: `[QueryType]
public static partial class Query
{
    public static Book GetBook()
        => new Book("C# in depth.", new Author("Jon Skeet"));
}`,
      },
    },
    {
      heading: "What you build",
      paragraphs: [
        "A server that responds to a book query with a title and nested author. Running dotnet run starts it on a local port (the template defaults to http://localhost:5095) and serves the Nitro GraphQL IDE at /graphql, where you create a document, run the query, and inspect the schema.",
      ],
    },
    {
      heading: "Next steps",
      paragraphs: [
        "The tutorial's own next steps: read Defining a Schema for the full type system, DataLoader or the Entity Framework integration for fetching real data, the GraphQL Workshop repo for a longer hands-on walkthrough, or the official GraphQL introduction if you're new to the query language itself.",
      ],
    },
  ],
};

const getStartedFederationTutorial: TutorialItem = {
  type: "tutorial",
  slug: "getting-started-with-graphql-federation",
  title: "Getting Started with GraphQL Federation",
  tagline: "Build two subgraphs, compose them with Fusion, and query the unified API through the gateway.",
  products: ["hot-chocolate", "fusion"],
  level: "intermediate",
  externalUrl: "/docs/fusion/getting-started",
  updatedRelative: "2 weeks ago",
  cli: [
    { key: "nitro", label: "install Nitro CLI", code: "dotnet tool install -g ChilliCream.Nitro.CommandLine" },
    { key: "new", label: "scaffold a subgraph", code: "dotnet new graphql -n Products" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Fusion lets you split a GraphQL API across independent subgraphs and present them to clients as one composite schema. A gateway in front of the subgraphs routes each part of a query to the subgraph that owns it and combines the results; a subgraph never calls another subgraph directly, it only declares its entities and lookups and trusts the gateway to resolve references across the graph.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "A Products subgraph (localhost:5001) and a Reviews subgraph (localhost:5002), each a normal Hot Chocolate server. Reviews contributes a reviews field to the Product type from Products using an entity stub, without duplicating any Product data. The Nitro CLI composes both subgraphs' exported schemas into a Fusion archive that a gateway (localhost:5000) loads and serves as one unified schema.",
      ],
      code: {
        language: "graphql",
        code: `type Query {
  productById(id: ID!): Product @lookup
}`,
      },
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Install the .NET 10 SDK, the Nitro CLI, and the Hot Chocolate templates. Scaffold each subgraph with dotnet new graphql, run nitro fusion compose against their exported schema.graphqls files to produce gateway.far, then start both subgraphs and a gateway project (dotnet new graphql-gateway) that loads the archive with AddFileSystemConfiguration.",
      ],
    },
    {
      heading: "Next steps",
      paragraphs: [
        "From here: add a third subgraph (Adding a Subgraph), go deeper on entities, lookups, and cross-subgraph field dependencies with [Require] (Entities and Lookups), or move on to Deployment & CI/CD and Authentication and Authorization for a production setup. Coming from Apollo Federation covers the terminology mapping if that's your starting point.",
      ],
    },
  ],
};

const strawberryShakeBlazorTutorial: TutorialItem = {
  type: "tutorial",
  slug: "strawberry-shake-blazor-get-started",
  title: "Add a Strawberry Shake Client to Blazor",
  tagline: "Generate a typed GraphQL client and fetch data from a Blazor WebAssembly component.",
  products: ["strawberry-shake"],
  level: "beginner",
  externalUrl: "/docs/strawberryshake/get-started",
  updatedRelative: "2 months ago",
  cli: [
    { key: "manifest", label: "create a tool manifest", code: "dotnet new tool-manifest" },
    { key: "tools", label: "install the tools", code: "dotnet tool install StrawberryShake.Tools --local" },
    {
      key: "init",
      label: "generate a client",
      code: "dotnet graphql init https://demo.chillicream.com/graphql/ --clientName CryptoClient",
    },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Strawberry Shake generates a typed C# client from .graphql query documents. The dotnet graphql init CLI command points at a running GraphQL endpoint, downloads its schema, and scaffolds a .graphqlrc.json plus the generated client project. Strawberry Shake isn't Blazor-specific: this tutorial builds a Blazor WebAssembly app, but the generated client works in any .NET standard-compliant project.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "A Blazor WebAssembly page that queries ChilliCream's public demo GraphQL server (demo.chillicream.com/graphql) for a list of assets and renders their name and price. After a .graphql query document is added and the project is built, the source generator emits both the typed client and a UseGetAssets Razor component with built-in ChildContent, LoadingContent, and ErrorContent slots.",
      ],
      code: {
        language: "graphql",
        code: `query GetAssets {
  assets {
    nodes {
      name
      price {
        lastPrice
      }
    }
  }
}`,
      },
    },
    {
      heading: "Next steps",
      paragraphs: [
        "The tutorial also links a companion walkthrough video covering the same steps end to end. Swap the demo endpoint for your own Hot Chocolate server's URL to generate a client against your own schema.",
      ],
    },
  ],
};

export const TUTORIAL_ITEMS: readonly TutorialItem[] = [
  getStartedNetCoreTutorial,
  getStartedFederationTutorial,
  strawberryShakeBlazorTutorial,
];

// -----------------------------------------------------------------------------
// Examples
// -----------------------------------------------------------------------------

const fusionDemoExample: ExampleItem = {
  type: "example",
  slug: "fusion-demo",
  title: "Fusion Demo",
  tagline: "An end-to-end Fusion setup: subgraphs, Aspire composition, and the gateway in action.",
  products: ["hot-chocolate", "fusion"],
  level: "intermediate",
  externalUrl: "https://github.com/ChilliCream/fusion-demo",
  githubUrl: "https://github.com/ChilliCream/fusion-demo",
  updatedRelative: "this week",
  cli: [
    { key: "git", label: "git clone", code: "git clone https://github.com/ChilliCream/fusion-demo && cd fusion-demo" },
    { key: "run", label: "run the AppHost", code: "dotnet run --project src/AppHost/Demo.AppHost.csproj" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Eight subgraphs (Accounts, Cart, Inventory, Order, Payments, Products, Reviews, Shipping) composed behind one Fusion gateway, orchestrated end to end with .NET Aspire. The Fusion getting-started tutorial points here for a fuller picture of DataLoader and batch-resolver patterns across subgraph boundaries than its own two-subgraph walkthrough covers.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "The Aspire AppHost wires up Postgres (a database per subgraph, except Shipping), NATS with JetStream backing the Reviews subgraph's event stream, and Keycloak for authentication, then composes all eight subgraphs' schemas into the Gateway project with global object identification enabled. A frontend project and a load generator project round out the solution.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Clone the repo and run the AppHost project; Aspire starts every subgraph, the gateway, and their infrastructure dependencies together; each subgraph waits on its database and messaging dependencies before serving.",
      ],
    },
  ],
};

const mochaEcommerceDemoExample: ExampleItem = {
  type: "example",
  slug: "mocha-ecommerce-demo",
  title: "Mocha E-Commerce Demo",
  tagline:
    "Three services (Catalog, Billing, Shipping) wired with Mocha messaging, sagas, and the transactional outbox, orchestrated with .NET Aspire.",
  products: ["mocha"],
  level: "advanced",
  externalUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Mocha/Demo",
  githubUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Mocha/Demo",
  updatedRelative: "this week",
  cli: [
    {
      key: "clone",
      label: "clone the examples repo",
      code: "git clone https://github.com/ChilliCream/platform-examples.git && cd platform-examples/examples/Mocha/Demo",
    },
    { key: "run", label: "run the AppHost", code: "dotnet run --project Demo.AppHost" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Mocha's own docs point to this project as its real-world reference: a complete e-commerce system built on the message bus, covering event-driven communication, saga orchestration, batch processing, and the transactional outbox. The AppHost wires up RabbitMQ and one Postgres database per service (catalog, billing, shipping), each service waiting on its dependencies before starting.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "Included .http files (for the VS Code REST Client extension) walk through the saga flows step by step: a basic place-order-then-pay-then-ship sequence, a quick refund with no physical return, a full return saga with inspection and parallel processing, and batch-processing runs. Sample product IDs are seeded so requests chain together without extra setup.",
      ],
      code: {
        language: "text",
        code: `Catalog  ──OrderPlacedEvent──▶  Billing
   ▲                                │
   │  ShipmentCreated/ShippedEvent  │ PaymentCompletedEvent
   │                                ▼
   └────────────────────────  Shipping`,
      },
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Install the REST Client extension, start the Demo AppHost (Aspire), then update the @catalogUrl, @billingUrl, and @shippingUrl variables in each .http file to match the ports Aspire assigns. Run the requests in a file in order, waiting a couple of seconds between steps for async processing.",
      ],
    },
  ],
};

const hotChocolateExamplesExample: ExampleItem = {
  type: "example",
  slug: "hotchocolate-examples",
  title: "Hot Chocolate Examples",
  tagline: "Runnable Hot Chocolate samples, including websocket authentication for subscriptions.",
  products: ["hot-chocolate"],
  level: "intermediate",
  externalUrl: "https://github.com/ChilliCream/hotchocolate-examples",
  githubUrl: "https://github.com/ChilliCream/hotchocolate-examples",
  updatedRelative: "over a year ago",
  cli: [{ key: "git", label: "git clone", code: "git clone https://github.com/ChilliCream/hotchocolate-examples" }],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "A collection of small, independent Hot Chocolate projects rather than one app: alongside larger Telemetry, Fusion, and workshop directories, each folder under misc/ is a standalone sample with its own README and .csproj, focused on one topic. WebsocketAuthentication demonstrates authenticating a subscription over the GraphQL WebSocket protocol; other folders cover DataLoader, persisted queries, Relay-style schemas, type extensions, OpenTelemetry, schema stitching, and integrations with MongoDB, RavenDB, and Marten.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "Whatever the folder you open covers. There is no single app to run end to end; each sample is scoped to demonstrate one Hot Chocolate feature or integration in isolation, which makes it a reference to dip into rather than a tutorial to follow start to finish.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: ["Clone the repo, open the sample folder for the topic you need, and dotnet run that project."],
    },
  ],
};

const mochaPostgresTransportExample: ExampleItem = {
  type: "example",
  slug: "mocha-postgres-transport",
  title: "Mocha PostgreSQL Transport",
  tagline: "Publish, send, and request/reply over Mocha's Postgres-based message queue, wired up with .NET Aspire.",
  products: ["mocha"],
  level: "intermediate",
  externalUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Mocha/PostgresTransport",
  githubUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Mocha/PostgresTransport",
  updatedRelative: "this week",
  cli: [
    {
      key: "clone",
      label: "clone the examples repo",
      code: "git clone https://github.com/ChilliCream/platform-examples.git && cd platform-examples/examples/Mocha/PostgresTransport",
    },
    { key: "run", label: "run the AppHost", code: "dotnet run --project PostgresTransport.AppHost" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Demonstrates Mocha's PostgreSQL transport: publish, send, and request/reply patterns backed by a Postgres-based message queue, wired up with .NET Aspire. The AppHost provisions a single Postgres instance with a shared messaging-db database.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "OrderService exposes demo HTTP endpoints that publish OrderPlacedEvent, send ProcessOrderCommand and OrderShippedEvent, and issue a GetOrderStatusRequest/response round trip. ShippingService and NotificationService subscribe to those events and log the resulting notifications.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: [
        "From the example directory, run the AppHost, then exercise OrderService's demo endpoints: /api/demo/publish, /api/demo/send, and /api/demo/request-reply.",
      ],
    },
  ],
};

const mochaAotExample: ExampleItem = {
  type: "example",
  slug: "mocha-aot-example",
  title: "Mocha Native AOT Example",
  tagline: "Publish/subscribe, request/reply, and a saga over RabbitMQ, all in AOT-compiled, trim-compatible services.",
  products: ["mocha"],
  level: "advanced",
  externalUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Mocha/AotExample",
  githubUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Mocha/AotExample",
  updatedRelative: "this week",
  cli: [
    {
      key: "clone",
      label: "clone the examples repo",
      code: "git clone https://github.com/ChilliCream/platform-examples.git && cd platform-examples/examples/Mocha/AotExample",
    },
    { key: "rabbitmq", label: "start RabbitMQ", code: "docker compose up -d" },
    { key: "run-order", label: "run OrderService", code: "dotnet run --project AotExample.OrderService" },
    {
      key: "run-fulfillment",
      label: "run FulfillmentService",
      code: "dotnet run --project AotExample.FulfillmentService",
    },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Demonstrates running Mocha under Native AOT: publish/subscribe and request/reply over RabbitMQ, a saga tracking an order through shipment, and a mediator handling in-process commands and queries, all in AOT-compiled and trim-compatible services.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "OrderService and FulfillmentService are both published with PublishAot and IsAotCompatible enabled. OrderService's background worker places orders through the mediator and publishes OrderPlacedEvent/OrderShippedEvent; an OrderSaga tracks each order to shipment with a 30-second timeout. FulfillmentService subscribes to OrderPlacedEvent, checks inventory via a request/reply call, and publishes OrderShippedEvent once stock is confirmed.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Start RabbitMQ, then run OrderService and FulfillmentService each in their own terminal and watch the console output as orders move from placement to inventory check to shipment. Publish both projects with a runtime identifier (e.g. -r linux-x64) to verify they build and run as self-contained native executables.",
      ],
    },
  ],
};

const mochaExceptionPoliciesExample: ExampleItem = {
  type: "example",
  slug: "mocha-exception-policies",
  title: "Mocha Exception Policies",
  tagline:
    "Dead-lettering, discarding, retrying, redelivering, and chained resilience policies keyed on exception state.",
  products: ["mocha"],
  level: "intermediate",
  externalUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Mocha/ExceptionPolicies",
  githubUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Mocha/ExceptionPolicies",
  updatedRelative: "this week",
  cli: [
    {
      key: "clone",
      label: "clone the examples repo",
      code: "git clone https://github.com/ChilliCream/platform-examples.git && cd platform-examples/examples/Mocha/ExceptionPolicies",
    },
    { key: "run", label: "run the host", code: "dotnet run" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Demonstrates Mocha's per-exception resilience policies: dead-lettering, discarding, retrying, redelivering, and chaining retry into redelivery into dead-lettering, including conditional policies keyed on exception state. Uses the InMemory transport, no external dependencies.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "A single host registers eight event handlers, each throwing a different exception to exercise one policy shape, all configured in one AddResilience call in Program.cs. HttpServiceException branches on its status code to three different policies for the same exception type, and a catch-all On<Exception>() covers anything unmatched.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Run the host: it registers the handlers and policies but doesn't publish any messages itself, so the process just idles. Read Program.cs alongside Handlers/Handlers.cs as a policy-configuration reference, or wire in a publisher to observe retries, redeliveries, and dead-letters in the console output.",
      ],
    },
  ],
};

export const EXAMPLE_ITEMS: readonly ExampleItem[] = [
  fusionDemoExample,
  mochaEcommerceDemoExample,
  mochaPostgresTransportExample,
  mochaAotExample,
  mochaExceptionPoliciesExample,
  hotChocolateExamplesExample,
];

// -----------------------------------------------------------------------------
// Workshops
// -----------------------------------------------------------------------------

const fullstackWorkshop: WorkshopItem = {
  type: "workshop",
  slug: "fullstack-graphql-workshop",
  title: "Full Stack GraphQL Workshop",
  tagline:
    "A two-day, hands-on workshop building a distributed web shop with Hot Chocolate, Relay, Fusion, and .NET Aspire.",
  products: ["hot-chocolate", "fusion"],
  level: "advanced",
  externalUrl: "/learn/articles/fullstack-workshop",
  updatedRelative: "over 2 years ago",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "A two-day, hands-on workshop that builds a distributed web shop from scratch with Hot Chocolate, Relay.js, Fusion across multiple subgraphs, and .NET Aspire, alongside domain-driven design, CQRS, and clean architecture concepts.",
      ],
    },
    {
      heading: "What you learn",
      paragraphs: [
        "Fourteen modules across two days. Day one covers GraphQL fundamentals, building efficient APIs with EF Core (paging, filtering, sorting, projections), layered architecture with DataLoaders, schema evolution patterns, and Relay.js from the basics through advanced fetching and store internals. Day two moves into GraphQL mutation patterns, Relay mutations and optimistic updates, schema evolution with client and schema registries, distributed GraphQL with Fusion, authentication and authorization, CQRS and DDD, and subscription patterns, closing with an open Q&A.",
      ],
    },
    {
      heading: "How to attend",
      paragraphs: [
        "Public sessions are announced on the ChilliCream blog; book a seat through the link there. The workshop can also be run privately, tailored to a team's specific focus areas.",
      ],
    },
    {
      heading: "Next steps",
      paragraphs: ["Questions before booking go to contact@chillicream.com or the ChilliCream Slack."],
    },
  ],
};

const graphqlWorkshop: WorkshopItem = {
  type: "workshop",
  slug: "graphql-workshop",
  title: "Getting started with GraphQL on ASP.NET Core and Hot Chocolate",
  tagline: "A self-paced, hands-on workshop repo building a conference-planner GraphQL server, one session at a time.",
  products: ["hot-chocolate"],
  level: "beginner",
  externalUrl: "https://github.com/ChilliCream/graphql-workshop",
  githubUrl: "https://github.com/ChilliCream/graphql-workshop",
  updatedRelative: "3 months ago",
  cli: [{ key: "git", label: "git clone", code: "git clone https://github.com/ChilliCream/graphql-workshop" }],
  body: [
    {
      heading: "What you build",
      paragraphs: [
        "A conference-planner GraphQL server built with ASP.NET Core and Hot Chocolate from File → New, up through custom middleware, complex filters, subscriptions, Relay support, and tests. A hosted copy of the finished server is browsable at workshop.chillicream.com.",
      ],
    },
    {
      heading: "Sessions",
      paragraphs: [
        "Seven sessions, each with its own guide: creating the server project, understanding DataLoader, schema design, middleware, complex filters, subscriptions, and testing. Each session builds on the previous one's code.",
      ],
    },
    {
      heading: "Prerequisites",
      paragraphs: [
        "The .NET SDK 8.0, an editor (VS Code, Visual Studio, or JetBrains Rider), and the Nitro GraphQL IDE.",
      ],
    },
    {
      heading: "How to start",
      paragraphs: [
        "Clone the repo, then work through the session guides under docs/ in order, starting with Session 1.",
      ],
    },
  ],
};

export const WORKSHOP_ITEMS: readonly WorkshopItem[] = [fullstackWorkshop, graphqlWorkshop];

export const LEARN_ITEMS: readonly LearnItem[] = [
  ...TEMPLATE_ITEMS,
  ...VIDEO_ITEMS,
  ...TUTORIAL_ITEMS,
  ...EXAMPLE_ITEMS,
  ...WORKSHOP_ITEMS,
];

export const TEMPLATE_SUMMARIES: readonly TemplateSummary[] = TEMPLATE_ITEMS.map(
  ({ type, slug, title, tagline, topology, useCases, language, clients, products, stack, agentReady }) => ({
    type,
    slug,
    title,
    tagline,
    topology,
    useCases,
    language,
    clients,
    products,
    stack,
    agentReady,
  }),
);

/** Summaries for the /learn hub grid, across every content type. */
export const LEARN_SUMMARIES: readonly LearnItemSummary[] = [
  ...TEMPLATE_SUMMARIES,
  ...VIDEO_ITEMS,
  ...TUTORIAL_ITEMS,
  ...EXAMPLE_ITEMS,
  ...WORKSHOP_ITEMS,
];

export const findTemplate = (slug: string): TemplateItem | undefined => TEMPLATE_ITEMS.find((t) => t.slug === slug);

export const findLearnItem = (slug: string): LearnItem | undefined => LEARN_ITEMS.find((i) => i.slug === slug);

// The single template promoted as the /templates index-page hero. Falls back
// to the first non-Starter entry if no template is flagged, and to
// `undefined` while TEMPLATE_ITEMS is empty (website-yw2.1); callers must
// treat a missing featured template as "no templates yet", not an error.
export const findFeaturedTemplate = (): TemplateItem | undefined => {
  const flagged = TEMPLATE_ITEMS.find((t) => t.featured);
  if (flagged) {
    return flagged;
  }
  const nonStarter = TEMPLATE_ITEMS.find((t) => !t.useCases.includes("starter"));
  return nonStarter ?? TEMPLATE_ITEMS[0];
};

// Related = same topology first, then same product mix overlap, then anything.
// Capped at 3 to match the Vercel pattern: lateral exploration without
// overwhelming the reader.
export const findRelatedTemplates = (template: TemplateItem, max: number = 3): readonly TemplateItem[] => {
  const others = TEMPLATE_ITEMS.filter((t) => t.slug !== template.slug);
  const sameTopology = others.filter((t) => t.topology === template.topology);
  const productOverlap = others.filter(
    (t) => !sameTopology.includes(t) && t.products.some((p) => template.products.includes(p)),
  );
  return [...sameTopology, ...productOverlap, ...others].slice(0, max);
};

/**
 * Related items for a tutorial/example/workshop detail page: same-type items
 * sharing a product first, then other content types sharing a product,
 * capped at `max`. Templates carry their own topology-aware `findRelated` in
 * the templates route, since topology has no equivalent on these types.
 */
export const findRelatedCatalogItems = (
  item: TutorialItem | ExampleItem | WorkshopItem,
  max: number = 3,
): readonly LearnItemSummary[] => {
  const others = LEARN_SUMMARIES.filter((i) => i.slug !== item.slug);
  const sameType = others.filter((i) => i.type === item.type);
  const sameTypeProductOverlap = sameType.filter((i) => i.products.some((p) => item.products.includes(p)));
  const primary = (sameTypeProductOverlap.length > 0 ? sameTypeProductOverlap : sameType).slice(0, max);
  if (primary.length >= max) {
    return primary;
  }
  const usedSlugs = new Set([item.slug, ...primary.map((i) => i.slug)]);
  const otherType = others.filter(
    (i) => i.type !== item.type && !usedSlugs.has(i.slug) && i.products.some((p) => item.products.includes(p)),
  );
  return [...primary, ...otherType].slice(0, max);
};
