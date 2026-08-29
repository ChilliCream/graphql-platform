// Seed content for /learn.
//
// Templates (website-yw2.2): the 8 fabricated seed templates website-yw2.1
// removed all carried a githubUrl under github.com/ChilliCream/templates, an
// org/repo that does not exist. TEMPLATE_ITEMS is rebuilt here from the one
// real dotnet-new template source in the ChilliCream org: the
// HotChocolate.Templates NuGet package (MIT, 1.1M+ downloads per `dotnet
// package search HotChocolate.Templates`), whose three project templates
// live at github.com/ChilliCream/graphql-platform/tree/main/templates
// (server, gateway, azure-function). No other public ChilliCream repo is a
// genuine, currently-maintained template gallery entry; see the
// website-yw2.2 task comments for the full repo-by-repo inventory decision.
//
// Video/tutorial/example/workshop content (website-5yo.6) links out to real,
// verified material: existing ChilliCream YouTube videos, docs tutorials
// already published under /docs, and public github.com/ChilliCream example
// and workshop repos.
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

// The three real dotnet-new templates shipped in the HotChocolate.Templates
// NuGet package (github.com/ChilliCream/graphql-platform, MIT-licensed).
// Facts below are read off each template's .template.config/template.json,
// .csproj, and Program.cs under templates/<name> in that repo (verified
// 2026-08-29 via the GitHub contents API); no separate template gallery
// repo exists, and none of the three has its own README.
const graphqlServerTemplate: TemplateItem = {
  type: "template",
  slug: "graphql-server",
  title: "Hot Chocolate GraphQL Server",
  tagline: "The dotnet new graphql starter: a minimal, source-generated Hot Chocolate server.",
  topology: "solo",
  useCases: ["starter"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate"],
  stack: [],
  agentReady: false,
  githubUrl: "https://github.com/ChilliCream/graphql-platform/tree/main/templates/server",
  license: "MIT",
  updatedRelative: "over a week ago",
  cli: [
    { key: "install", label: "install template", code: "dotnet new install HotChocolate.Templates" },
    { key: "new", label: "scaffold project", code: "dotnet new graphql --name MyGraphQLServer" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "The graphql shortName in HotChocolate.Templates scaffolds an ASP.NET Core Web project wired with builder.AddGraphQL().AddTypes() and app.MapGraphQL(). AddTypes() and the [QueryType] attribute wiring come from a source generator (HotChocolate.Types.Analyzers), so there is no manual type registration to write.",
      ],
      code: {
        language: "csharp",
        code: `var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);`,
      },
    },
    {
      heading: "What you build",
      paragraphs: [
        "The scaffold ships an Author and a Book record and a Query class exposing a single book field, the same shape the Getting Started tutorial walks through. RunWithGraphQLCommands(args), from HotChocolate.AspNetCore.CommandLine, adds developer commands on top of the normal run, including exporting the schema as SDL.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Install the template pack once, then scaffold a project with dotnet new graphql. Its Framework parameter targets net10.0 by default and can be switched to net8.0, net9.0, or net11.0. dotnet run starts the server and serves the Nitro GraphQL IDE at /graphql.",
      ],
    },
  ],
};

const graphqlGatewayTemplate: TemplateItem = {
  type: "template",
  slug: "graphql-gateway",
  title: "Hot Chocolate Fusion Gateway",
  tagline: "The dotnet new graphql-gateway starter: a Fusion gateway ready to load a composed schema.",
  topology: "federation",
  useCases: ["starter"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate", "fusion"],
  stack: [],
  agentReady: false,
  githubUrl: "https://github.com/ChilliCream/graphql-platform/tree/main/templates/gateway",
  license: "MIT",
  updatedRelative: "over a week ago",
  cli: [
    { key: "install", label: "install template", code: "dotnet new install HotChocolate.Templates" },
    { key: "new", label: "scaffold project", code: "dotnet new graphql-gateway --name MyGateway" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "The graphql-gateway shortName scaffolds an ASP.NET Core Web project that depends only on HotChocolate.Fusion.AspNetCore. Program.cs registers an HTTP client named fusion, calls AddGraphQLGateway(), and loads its composed schema from a local file with AddFileSystemConfiguration.",
      ],
      code: {
        language: "csharp",
        code: `builder.Services.AddHttpClient("fusion");

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");`,
      },
    },
    {
      heading: "What you build",
      paragraphs: [
        "A gateway project with nothing to compose out of the box: it expects a Fusion archive at ./gateway.far and will not serve traffic until one exists. Produce that archive by composing your subgraphs' exported schemas, the same step the Getting Started with GraphQL Federation tutorial covers.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Install the template pack, scaffold with dotnet new graphql-gateway, compose your subgraph schemas into a gateway.far next to the project, then dotnet run to serve the composed schema at /graphql.",
      ],
    },
  ],
};

const graphqlAzureFunctionTemplate: TemplateItem = {
  type: "template",
  slug: "graphql-azure-function",
  title: "Hot Chocolate GraphQL Azure Function",
  tagline: "The dotnet new graphql-azf starter: a Hot Chocolate server on the isolated-worker Azure Functions model.",
  topology: "solo",
  useCases: ["starter"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate"],
  stack: [],
  agentReady: false,
  githubUrl: "https://github.com/ChilliCream/graphql-platform/tree/main/templates/azure-function",
  license: "MIT",
  updatedRelative: "over a week ago",
  cli: [
    { key: "install", label: "install template", code: "dotnet new install HotChocolate.Templates" },
    { key: "new", label: "scaffold project", code: "dotnet new graphql-azf --name MyGraphQLFunction" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "The graphql-azf shortName scaffolds an Azure Functions v4 isolated-worker project. Program.cs configures the worker with AddGraphQLFunction(b => b.AddQueryType<Query>()); a GraphQLHttpFunction bound to an HTTP trigger at graphql/{**slug} forwards every request to Hot Chocolate's IGraphQLRequestExecutor.",
      ],
      code: {
        language: "csharp",
        code: `[Function("GraphQLHttpFunction")]
public Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "graphql/{**slug}")]
    HttpRequestData request)
    => _executor.ExecuteAsync(request);`,
      },
    },
    {
      heading: "What you build",
      paragraphs: [
        "A single-function app exposing a GraphQL endpoint, scaffolded with a Query class returning one Person field as its starting point, plus a host.json and local.settings.json for the Functions runtime.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Install the template pack, scaffold with dotnet new graphql-azf, then run it locally with the Azure Functions Core Tools (func start), the standard way to run any isolated-worker Functions app of this shape.",
      ],
    },
  ],
};

export const TEMPLATE_ITEMS: readonly TemplateItem[] = [
  graphqlServerTemplate,
  graphqlGatewayTemplate,
  graphqlAzureFunctionTemplate,
];

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
      code: "dotnet graphql init https://demo.chillicream.cloud/graphql/ --clientName CryptoClient",
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
        "A Blazor WebAssembly page that queries ChilliCream's public demo GraphQL server (demo.chillicream.cloud/graphql) for a list of assets and renders their name and price. After a .graphql query document is added and the project is built, the source generator emits both the typed client and a UseGetAssets Razor component with built-in ChildContent, LoadingContent, and ErrorContent slots.",
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

const fusionApolloFederationDemoExample: ExampleItem = {
  type: "example",
  slug: "fusion-apollo-federation-demo",
  title: "Fusion + Apollo Federation Demo",
  tagline:
    "A Hot Chocolate Fusion gateway composing Apollo Server subgraphs: polyglot federation across .NET and Node.",
  products: ["hot-chocolate", "fusion"],
  level: "advanced",
  externalUrl: "https://github.com/ChilliCream/Fusion-Federation-Demo",
  githubUrl: "https://github.com/ChilliCream/Fusion-Federation-Demo",
  updatedRelative: "over a month ago",
  cli: [
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/Fusion-Federation-Demo && cd Fusion-Federation-Demo",
    },
    { key: "install", label: "install", code: "npm install" },
    { key: "restore", label: "restore Aspire", code: "npm run aspire:restore" },
    { key: "dev", label: "run", code: "npm run dev" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "One shop GraphQL API exposed through a Hot Chocolate Fusion gateway, while the underlying subgraphs are Apollo Server running Apollo Federation v2, not Hot Chocolate. Where fusion-demo shows Fusion composing an all-.NET graph, this demo shows the same gateway composing subgraphs written in a different language and a different federation implementation.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "A TypeScript Aspire AppHost starts one PostgreSQL server with separate accounts, products, and reviews databases, Apollo Server subgraphs for each domain (ports 4001-4003), a one-shot Nitro composition resource that produces gateway/gateway.far, and the Fusion gateway itself at :4000/graphql once composition and health checks pass. Apollo Server doesn't implement Fusion's variable- or request-batching transports, so both are explicitly disabled in each subgraph's schema-settings.json.",
      ],
      code: {
        language: "graphql",
        code: `query Shop {
  topProducts(first: 4) {
    id
    name
    price
    inStock
    reviews {
      rating
      body
      author {
        name
        username
      }
    }
  }
}`,
      },
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Requires Docker, Node.js 20.19+/22.13+/24+, the .NET 10 SDK, and the Aspire CLI. npm install, npm run aspire:restore, then npm run dev boots the whole stack; open the printed Aspire dashboard URL or query the gateway directly with the query above, which starts in the products subgraph and joins reviews and their authors through Apollo's entity-resolution protocol.",
      ],
    },
  ],
};

const graphqlFederationSubgraphExample: ExampleItem = {
  type: "example",
  slug: "graphql-federation-subgraph",
  title: "graphql-federation-subgraph",
  tagline:
    "A server-agnostic npm package implementing the GraphQL Federation Spec's directives for any Node.js server.",
  products: ["fusion"],
  level: "intermediate",
  externalUrl: "https://github.com/ChilliCream/graphql-federation-subgraph",
  githubUrl: "https://github.com/ChilliCream/graphql-federation-subgraph",
  updatedRelative: "over a week ago",
  cli: [{ key: "install", label: "npm install", code: "npm install graphql-federation-subgraph graphql" }],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Plays the same role for the GraphQL Federation Spec (the spec Fusion implements) that @apollo/subgraph plays for Apollo Federation: it lets any TypeScript/JavaScript GraphQL server use the federation directives (@key, @lookup, @shareable, and more) without defining them, and export its schema for composition. Unlike @apollo/subgraph it isn't tied to one server: the result is a plain GraphQLSchema; the package has zero runtime dependencies, and graphql is its only peer dependency.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "The Federation Spec has no Query._entities/__resolveReference side-channel: entity resolution happens through ordinary fields annotated @lookup, called directly by the distributed executor, so a subgraph built with this package needs no reference-resolver machinery at all. buildSubgraphSchema() injects the directive and scalar definitions into your typeDefs; printSourceSchema() and createSourceSchemaHandler() then print or serve that schema, directives included, at a route like /graphql/schema.graphql for Fusion's composition tooling to read. Confirmed working with GraphQL Yoga, Apollo Server, Mercurius, graphql-http, and NestJS.",
      ],
      code: {
        language: "ts",
        code: `import { buildSubgraphSchema } from "graphql-federation-subgraph";

const typeDefs = /* GraphQL */ \`
  type Query {
    productById(id: ID!): Product @lookup
  }

  type Product @key(fields: "id") {
    id: ID!
    name: String!
  }
\`;

const products = [{ id: "1", name: "Chair" }];

const resolvers = {
  Query: {
    productById: (_parent: unknown, args: { id: string }) =>
      products.find((product) => product.id === args.id),
  },
};

const schema = buildSubgraphSchema({ typeDefs, resolvers });`,
      },
    },
    {
      heading: "How to use it",
      paragraphs: [
        "npm install it alongside your GraphQL server of choice, add @key/@lookup directives to your schema, build the schema with buildSubgraphSchema, and mount createSourceSchemaHandler's handler at the schema-document route your composition step expects.",
      ],
    },
  ],
};

const agentReadyGraphqlApiDemoExample: ExampleItem = {
  type: "example",
  slug: "agent-ready-graphql-api-demo",
  title: "Agent-Ready GraphQL API Demo",
  tagline:
    "A Fusion gateway with semantic introspection and a REST bridge, case-studied against how cheaply an LLM agent can discover and query it.",
  products: ["hot-chocolate", "fusion"],
  // Subject is building an agent-ready API and comparing LLM discovery
  // strategies against it, not GraphQL/Federation fundamentals; `products`
  // alone would only place this in GraphQL & Federation (see
  // src/data/learn/hubs.ts).
  hubs: ["agents"],
  level: "advanced",
  externalUrl: "https://github.com/ChilliCream/singapore-demo",
  githubUrl: "https://github.com/ChilliCream/singapore-demo",
  updatedRelative: "3 months ago",
  cli: [
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/singapore-demo && cd singapore-demo/src/AppHost",
    },
    { key: "run", label: "run the AppHost", code: "dotnet run" },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "A demo and case study, built for apidays Singapore, showing how an AI agent discovers and queries a federated GraphQL API backed by ten Singapore open-data domains (weather, air quality, traffic, parking, dengue clusters, food hygiene, healthcare, education, housing, demographics). The case study compares the token cost of different API-discovery strategies (REST with OpenAPI, GraphQL with the schema in the prompt, GraphQL with semantic introspection) when an LLM answers a natural-language question.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "An Aspire AppHost launches a Hot Chocolate Fusion gateway (composed from one subgraph per data domain, each serving cached JSON fixtures) plus a YARP reverse proxy that mirrors the same data as REST, so the same API can be queried either way. The gateway exposes semantic introspection (__search, __definitions) alongside the composed GraphQL schema and a merged OpenAPI document at /openapi/v1.json.",
      ],
      code: {
        language: "graphql",
        code: `{
  __search(query: "outdoor safety air quality taxis", first: 5) {
    coordinate
    score
  }
}`,
      },
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Requires the .NET 10 SDK; dotnet run from src/AppHost boots the gateway and all ten subgraphs, serving GraphQL at :5110/graphql and the merged OpenAPI document at :5110/openapi/v1.json. The case-study scripts under case-study/ additionally need the Claude Code CLI on PATH to run the discovery-strategy comparisons; the gateway itself does not.",
      ],
    },
  ],
};

const bookshopCleanArchitectureExample: ExampleItem = {
  type: "example",
  slug: "bookshop-clean-architecture",
  title: "Bookshop Clean Architecture",
  tagline: "A runnable GraphQL bookshop demonstrating domain-driven design and clean architecture.",
  products: ["hot-chocolate", "mocha"],
  // Pins the primary hub regardless of `products` ordering, since
  // Mocha.Mediator is used in-process for commands/queries here, not
  // Mocha's message bus (see src/data/learn/hubs.ts).
  hubs: ["graphql-federation"],
  level: "advanced",
  externalUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Architecture/Bookshop",
  githubUrl: "https://github.com/ChilliCream/platform-examples/tree/main/examples/Architecture/Bookshop",
  updatedRelative: "this week",
  cli: [
    {
      key: "clone",
      label: "clone the examples repo",
      code: "git clone https://github.com/ChilliCream/platform-examples.git && cd platform-examples/examples/Architecture/Bookshop",
    },
    { key: "postgres", label: "start PostgreSQL", code: "docker compose up -d" },
    {
      key: "run",
      label: "run the host",
      code: "ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Bookshop.Host",
    },
  ],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "A runnable GraphQL bookshop demonstrating domain-driven design and clean architecture with Hot Chocolate, source-generated GreenDonut DataLoaders, Mocha.Mediator, EF Core, and PostgreSQL.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "Five projects with dependencies pointing inward: Bookshop.Domain (aggregates, domain services, events, exceptions, repository contracts), Bookshop.Application (commands, queries, authorization policies, read models), Bookshop.Infrastructure (EF Core, repositories, DataLoaders, transactions, event dispatch), Bookshop.GraphQL (Hot Chocolate query, mutation, and object type extensions), and Bookshop.Host (composition root, health checks, migrations, seed data).",
        "Commands and queries live in the Application layer, with authorization enforced in their handlers so it applies independently of transport. Root collection queries are backed by source-generated DataLoaders; by-id and relationship fields in the GraphQL layer use DataLoaders and projection-aware QueryContext<T>. Command middleware opens an EF transaction, and UnitOfWork saves changes and dispatches the collected domain events, with ActivitySource diagnostics on the dispatch. The example deliberately lets add-book domain exceptions propagate as ordinary GraphQL execution errors; a production API would translate expected exceptions into a stable public error contract instead.",
      ],
      code: {
        language: "graphql",
        code: `query Books {
  books(first: 10) {
    nodes {
      id
      title
      isbn
      price
      currency
      stockQuantity
    }
  }
}`,
      },
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Start PostgreSQL, then run the host in Development so it uses the compose port and inserts sample data; the host applies pending migrations automatically. The GraphQL endpoint is /graphql, with health checks at /_health/live and /_health/ready.",
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
  fusionApolloFederationDemoExample,
  graphqlFederationSubgraphExample,
  agentReadyGraphqlApiDemoExample,
  bookshopCleanArchitectureExample,
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
