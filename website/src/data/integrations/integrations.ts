// Seed integrations rendered by /integrations and /integrations/[slug]. Each
// entry is the listing-as-data: index card front-matter (name, type, category,
// tagline, monogram letter), sticky sidebar metadata, and a body[] of section
// { heading, paragraphs[], code? }. Same shape as templates.ts so the two
// surfaces share vocabulary and tooling.
//
// Tagging discipline: every integration carries a value for type, category,
// and at least one product. Missing any of those breaks faceted filtering or
// the sidebar silently.

import type { ProductKey } from "@/data/templates/filters";

import type { CategoryKey } from "./categories";

export type IntegrationType = "native" | "community";

export interface IntegrationCodeBlock {
  readonly language: string;
  readonly code: string;
}

export interface IntegrationSection {
  readonly heading: string;
  readonly paragraphs: readonly string[];
  readonly code?: IntegrationCodeBlock;
}

export interface IntegrationLinks {
  readonly docs?: string;
  readonly nuget?: string;
  readonly github?: string;
  readonly website?: string;
}

export interface Integration {
  readonly slug: string;
  readonly name: string;
  // Single-letter monogram for the square logo tile. Cards intentionally use
  // a stroked monogram rather than partner-supplied SVG to keep the seed grid
  // visually coherent until we ship real brand marks.
  readonly letter: string;
  readonly type: IntegrationType;
  readonly category: CategoryKey;
  readonly tagline: string;
  readonly products: readonly ProductKey[];
  readonly minVersion: string;
  readonly maintainer: string;
  // Featured cards float to the top of the index "Featured" rail, in author
  // order, regardless of category.
  readonly featured?: boolean;
  // Recently-added drives the "Recently Added" Type sub-filter and the
  // "Recently added" sub-rail. Lower number = older; a fresh integration gets
  // an `addedRank` higher than every existing one.
  readonly addedRank: number;
  readonly githubStars?: number;
  readonly links: IntegrationLinks;
  readonly install: string;
  readonly body: readonly IntegrationSection[];
}

// -----------------------------------------------------------------------------
// Native integrations (14)
// -----------------------------------------------------------------------------

const opentelemetry: Integration = {
  slug: "opentelemetry",
  name: "OpenTelemetry",
  letter: "O",
  type: "native",
  category: "observability",
  tagline:
    "Distributed tracing and metrics on every resolver, gateway hop, and message, out of the box.",
  products: ["hot-chocolate"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  featured: true,
  addedRank: 100,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/server/instrumentation",
    nuget: "https://www.nuget.org/packages/HotChocolate.Diagnostics",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://opentelemetry.io",
  },
  install: "dotnet add package HotChocolate.Diagnostics",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Hot Chocolate emits OpenTelemetry spans for every operation, every resolver, and every middleware in the pipeline. Spans carry the operation name, the document hash, the validation outcome, and field-level timings.",
        "OTEL is the wire format. The same spans flow into Tempo, Datadog, Honeycomb, Jaeger, or your own collector without code changes, and into Nitro Hosted for the federation-aware view.",
      ],
    },
    {
      heading: "How it works",
      paragraphs: [
        "AddInstrumentation() registers a HotChocolate ActivitySource and wires the request, parser, validation, and resolver phases as nested spans. The Activity follows W3C traceparent, so spans link cleanly to upstream HTTP and downstream database calls.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: ["Install the package and register the instrumentation:"],
      code: {
        language: "csharp",
        code: `services
    .AddGraphQLServer()
    .AddInstrumentation(o =>
    {
        o.IncludeDocument = true;
        o.RenameRootActivity = true;
    });

services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter());`,
      },
    },
    {
      heading: "Configuration",
      paragraphs: [
        "IncludeDocument controls whether the GraphQL document text is attached as a span attribute. RenameRootActivity replaces the generic HTTP span name with the operation name, which is the single highest-leverage setting for a readable trace view.",
      ],
    },
    {
      heading: "Example",
      paragraphs: [
        "Once installed, every query produces a trace whose root activity is named after the operation, with one child span per resolver. Point your OTLP exporter at the collector of your choice and the spans appear in the same waterfall as your database calls.",
      ],
    },
  ],
};

const jaeger: Integration = {
  slug: "jaeger",
  name: "Jaeger",
  letter: "J",
  type: "native",
  category: "observability",
  tagline:
    "View federation traces in Jaeger with one config line. Field-level latency at your fingertips.",
  products: ["hot-chocolate", "nitro"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  addedRank: 80,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/server/instrumentation",
    nuget: "https://www.nuget.org/packages/HotChocolate.Diagnostics",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://www.jaegertracing.io",
  },
  install: "dotnet add package OpenTelemetry.Exporter.Jaeger",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Jaeger is the simplest path to a self-hosted trace UI. Hot Chocolate's OTEL spans flow into a Jaeger agent through the OTLP exporter and render as a per-operation flame graph, with one span per resolver and one per subgraph hop.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Add the OTLP exporter and point it at your Jaeger endpoint:",
      ],
      code: {
        language: "csharp",
        code: `services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("http://jaeger:4318");
        }));`,
      },
    },
    {
      heading: "Example",
      paragraphs: [
        "Run Jaeger locally with docker run -p 16686:16686 -p 4318:4318 jaegertracing/all-in-one. Open http://localhost:16686 and select the service name your Hot Chocolate process registers.",
      ],
    },
  ],
};

const tempo: Integration = {
  slug: "tempo",
  name: "Tempo",
  letter: "T",
  type: "native",
  category: "observability",
  tagline:
    "Grafana Tempo as a backend for federation traces. Long-retention, high-cardinality, no glue.",
  products: ["nitro"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  addedRank: 70,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/server/instrumentation",
    nuget: "https://www.nuget.org/packages/HotChocolate.Diagnostics",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://grafana.com/oss/tempo/",
  },
  install: "dotnet add package HotChocolate.Diagnostics",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Tempo trades indexed search for cheap, high-cardinality storage. Hot Chocolate's per-resolver spans land cleanly: every operation, every field path, every tenant ID can ride along as an attribute without exploding ingest cost.",
        "Pair Tempo with Grafana for the trace view, Loki for logs, Mimir for metrics, and Hot Chocolate's OTEL exporter for the spans, no extra adapters, no glue code.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: ["Point the OTLP exporter at your Tempo distributor:"],
      code: {
        language: "csharp",
        code: `services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("http://tempo:4318");
        }));`,
      },
    },
  ],
};

const datadog: Integration = {
  slug: "datadog",
  name: "Datadog",
  letter: "D",
  type: "native",
  category: "observability",
  tagline:
    "Stream traces, metrics, and errors into Datadog. Alert on resolver SLOs the same day you turn it on.",
  products: ["hot-chocolate", "nitro"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  featured: true,
  addedRank: 90,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/server/instrumentation",
    nuget: "https://www.nuget.org/packages/HotChocolate.Diagnostics",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://www.datadoghq.com",
  },
  install: "dotnet add package HotChocolate.Diagnostics",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Datadog ingests Hot Chocolate's OTEL spans through its OTLP endpoint and groups them by operation, by resolver, and by service tag. Define an SLO on p95 of a resolver and Datadog will alert on the field, not the request.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: ["Configure the OTLP exporter against your Datadog Agent:"],
      code: {
        language: "csharp",
        code: `services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("http://datadog-agent:4318");
        }));`,
      },
    },
  ],
};

const honeycomb: Integration = {
  slug: "honeycomb",
  name: "Honeycomb",
  letter: "H",
  type: "native",
  category: "observability",
  tagline:
    "BubbleUp on resolver paths. High-cardinality fields, federation-aware spans, no sampling surprises.",
  products: ["hot-chocolate", "nitro"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  addedRank: 60,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/server/instrumentation",
    nuget: "https://www.nuget.org/packages/HotChocolate.Diagnostics",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://www.honeycomb.io",
  },
  install: "dotnet add package HotChocolate.Diagnostics",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Honeycomb's BubbleUp turns Hot Chocolate's per-resolver spans into structured queries: which user, which field, which tenant, which build. Federation traces stay readable because every subgraph hop is a span on the same trace.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Point the OTLP exporter at Honeycomb's ingest endpoint with your team API key:",
      ],
      code: {
        language: "csharp",
        code: `services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddHotChocolateInstrumentation()
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("https://api.honeycomb.io");
            o.Headers = "x-honeycomb-team=YOUR_API_KEY";
        }));`,
      },
    },
  ],
};

const auth0: Integration = {
  slug: "auth0",
  name: "Auth0",
  letter: "A",
  type: "native",
  category: "auth",
  tagline:
    "Secure your GraphQL endpoint with Auth0. JWT validation, scope-based field auth, three lines in Program.cs.",
  products: ["hot-chocolate"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  featured: true,
  addedRank: 95,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/security/authorization",
    nuget:
      "https://www.nuget.org/packages/HotChocolate.AspNetCore.Authorization",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://auth0.com",
  },
  install: "dotnet add package HotChocolate.AspNetCore.Authorization",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Auth0 issues the JWT, ASP.NET Core's bearer middleware validates it, and Hot Chocolate's @authorize directive enforces scopes per field. The flow is the standard one, only the schema-typed authorization is new.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Wire the Auth0 issuer into JWT bearer auth, then enable Hot Chocolate authorization:",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = "https://YOUR_TENANT.auth0.com/";
        o.Audience = "https://api.your-app.com";
    });

services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>();

// In your schema:
[Authorize(Policy = "read:projects")]
public IQueryable<Project> Projects() => _db.Projects;`,
      },
    },
    {
      heading: "Configuration",
      paragraphs: [
        "Auth0 scopes map to ASP.NET Core policies one-to-one. Set the policy name to the scope and the @authorize(policy:) directive enforces it at field resolution, not at the endpoint.",
      ],
    },
  ],
};

const entraId: Integration = {
  slug: "microsoft-entra-id",
  name: "Microsoft Entra ID",
  letter: "M",
  type: "native",
  category: "auth",
  tagline:
    "Azure AD / Entra ID integration with conditional-access policies and field-level RBAC.",
  products: ["hot-chocolate"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  addedRank: 85,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/security/authorization",
    nuget:
      "https://www.nuget.org/packages/HotChocolate.AspNetCore.Authorization",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://learn.microsoft.com/entra/",
  },
  install: "dotnet add package Microsoft.Identity.Web",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Entra ID (formerly Azure AD) issues OIDC tokens that Hot Chocolate consumes through Microsoft.Identity.Web. Conditional-access policies, group claims, and roles flow through the same @authorize directive used for any other JWT issuer.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Bind Microsoft.Identity.Web to your Entra app registration:",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(config.GetSection("AzureAd"));

services
    .AddGraphQLServer()
    .AddAuthorization();`,
      },
    },
  ],
};

const oidc: Integration = {
  slug: "openid-connect",
  name: "OpenID Connect",
  letter: "I",
  type: "native",
  category: "auth",
  tagline:
    "Any OIDC provider. JWT bearer middleware that respects schema-typed authorization.",
  products: ["hot-chocolate"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  addedRank: 75,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/security/authorization",
    nuget:
      "https://www.nuget.org/packages/HotChocolate.AspNetCore.Authorization",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://openid.net/connect/",
  },
  install: "dotnet add package HotChocolate.AspNetCore.Authorization",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Hot Chocolate is OIDC-agnostic. Any provider that issues a signed JWT, Auth0, Keycloak, Okta, Cognito, Entra ID, OneLogin, slots into the same JWT bearer middleware. The schema-typed authorization is the new piece, not the auth flow.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Point JWT bearer at the issuer's discovery document and let it do the rest:",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = "https://your-issuer.example.com/";
        o.Audience = "your-api";
    });

services
    .AddGraphQLServer()
    .AddAuthorization();`,
      },
    },
  ],
};

const kafka: Integration = {
  slug: "kafka",
  name: "Apache Kafka",
  letter: "K",
  type: "native",
  category: "messaging",
  tagline:
    "Stream events through Mocha with Kafka as the transport. At-least-once delivery, partitioned consumers.",
  products: ["mocha"],
  minVersion: "1.0",
  maintainer: "ChilliCream",
  addedRank: 88,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/mocha",
    nuget: "https://www.nuget.org/packages/Mocha.Kafka",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://kafka.apache.org",
  },
  install: "dotnet add package Mocha.Kafka",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Mocha's command bus and event stream over Apache Kafka. Producers publish to topics; consumers subscribe through Mocha's typed handlers; partitions give you ordered delivery per aggregate.",
        "At-least-once is the default. Exactly-once is available via Kafka's transactional producer API and Mocha's idempotency-key plumbing.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: ["Wire the Kafka transport in DI:"],
      code: {
        language: "csharp",
        code: `services
    .AddMocha()
    .AddKafkaTransport(o =>
    {
        o.BootstrapServers = "kafka:9092";
        o.GroupId = "orders-service";
    })
    .AddCommandHandler<PlaceOrderHandler>()
    .AddEventHandler<OrderPlacedHandler>();`,
      },
    },
    {
      heading: "Configuration",
      paragraphs: [
        'Topic names default to the typed event class name. Override per handler with [MochaTopic("...")] when you need to bridge an existing event stream.',
      ],
    },
  ],
};

const azureServiceBus: Integration = {
  slug: "azure-service-bus",
  name: "Azure Service Bus",
  letter: "Z",
  type: "native",
  category: "messaging",
  tagline:
    "Mocha on Azure Service Bus. Sessions, dead-letter queues, and topic subscriptions, schema-typed.",
  products: ["mocha"],
  minVersion: "1.0",
  maintainer: "ChilliCream",
  addedRank: 78,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/mocha",
    nuget: "https://www.nuget.org/packages/Mocha.AzureServiceBus",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://azure.microsoft.com/products/service-bus",
  },
  install: "dotnet add package Mocha.AzureServiceBus",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Azure Service Bus brings sessions (FIFO per session ID), dead-letter queues, scheduled delivery, and managed topic subscriptions. Mocha typed handlers consume them as if they were ordinary in-process events.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Configure the Service Bus connection string and Mocha picks up the rest:",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddMocha()
    .AddAzureServiceBusTransport(o =>
    {
        o.ConnectionString = config["ServiceBus:ConnectionString"];
        o.UseSessions = true;
    });`,
      },
    },
  ],
};

const postgresql: Integration = {
  slug: "postgresql",
  name: "PostgreSQL",
  letter: "P",
  type: "native",
  category: "data",
  tagline:
    "Cursor-based pagination, projections, filtering, and sorting, pushed all the way down to Postgres.",
  products: ["hot-chocolate"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  addedRank: 92,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/fetching-data",
    nuget: "https://www.nuget.org/packages/HotChocolate.Data",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://www.postgresql.org",
  },
  install: "dotnet add package HotChocolate.Data.EntityFramework",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Hot Chocolate's data integrations translate GraphQL filtering, sorting, projection, and cursor pagination directly into SQL. With Postgres on the other side that means index-friendly queries, composite cursors, and field-level projection of JSON columns.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Wire the EF Core provider for Postgres and turn on the data middlewares:",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddDbContext<AppDbContext>(o =>
        o.UseNpgsql(config.GetConnectionString("Postgres")));

services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

public class Query
{
    [UsePaging, UseProjection, UseFiltering, UseSorting]
    public IQueryable<Project> GetProjects(AppDbContext db) => db.Projects;
}`,
      },
    },
  ],
};

const efCore: Integration = {
  slug: "entity-framework-core",
  name: "Entity Framework Core",
  letter: "E",
  type: "native",
  category: "data",
  tagline:
    "First-class EF Core support: filter, sort, project to your DbContext with one annotation.",
  products: ["hot-chocolate"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  addedRank: 91,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/hotchocolate/v14/integrations/entity-framework",
    nuget: "https://www.nuget.org/packages/HotChocolate.Data.EntityFramework",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://learn.microsoft.com/ef/core/",
  },
  install: "dotnet add package HotChocolate.Data.EntityFramework",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "[UseProjection] reads the GraphQL selection set and rewrites the IQueryable to select only the requested columns. [UseFiltering] and [UseSorting] expose typed where/order arguments backed by EF Core expression trees, no runtime string-building, no Linqkit.",
        "Works against every EF Core provider: Postgres, SQL Server, SQLite, Cosmos, MySQL.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: ["Annotate the resolver and Hot Chocolate handles the rest:"],
      code: {
        language: "csharp",
        code: `services
    .AddGraphQLServer()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

public class Query
{
    [UseDbContext(typeof(AppDbContext))]
    [UsePaging, UseProjection, UseFiltering, UseSorting]
    public IQueryable<Project> GetProjects(
        [ScopedService] AppDbContext db) => db.Projects;
}`,
      },
    },
  ],
};

const mcp: Integration = {
  slug: "model-context-protocol",
  name: "Model Context Protocol (MCP)",
  letter: "M",
  type: "native",
  category: "ai-agents",
  tagline:
    "Expose your schema to Claude, Cursor, and Copilot via MCP. Agents query and mutate with the same auth as users.",
  products: ["hot-chocolate", "nitro"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  featured: true,
  addedRank: 99,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/nitro/agents",
    nuget: "https://www.nuget.org/packages/HotChocolate.Mcp",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://modelcontextprotocol.io",
  },
  install: "dotnet add package HotChocolate.Mcp",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "MCP is the protocol Anthropic, OpenAI, and the major IDE vendors agreed on for tool-use. Hot Chocolate's MCP adapter exposes every query and mutation on your schema as an MCP tool and every type as an MCP resource, with the same authorization as your human callers.",
        "Add a field to your schema and Claude, Cursor, Copilot Chat, and any MCP-aware client pick it up on the next reload.",
      ],
    },
    {
      heading: "How it works",
      paragraphs: [
        "The MCP transport listens on a separate port and translates MCP tool calls into normal GraphQL operations. Auth tokens flow through the same middleware chain as the human surface, including @authorize, tenant resolution, and rate limiting. Use [McpHidden] to keep an internal mutation off the agent surface.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Add the MCP server alongside your normal GraphQL pipeline:",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddMcpServer()
    .AddAuthorization()
    .AddInstrumentation();

app.MapGraphQL();
app.MapMcp("/mcp");`,
      },
    },
    {
      heading: "Example",
      paragraphs: [
        "Point Claude Desktop, Cursor, or Continue at http://localhost:5000/mcp through their local config. The agent introspects the schema, the available tools surface in the assistant UI, and the calls show up next to your human traffic in Nitro.",
      ],
    },
  ],
};

const nextjs: Integration = {
  slug: "nextjs",
  name: "Next.js",
  letter: "N",
  type: "native",
  category: "frontend",
  tagline:
    "Strawberry Shake for Next.js with App Router and Server Components. Typed clients, no extra GraphQL fetcher.",
  products: ["strawberry-shake"],
  minVersion: "14.0",
  maintainer: "ChilliCream",
  addedRank: 96,
  githubStars: 5400,
  links: {
    docs: "https://chillicream.com/docs/strawberryshake",
    nuget: "https://www.nuget.org/packages/StrawberryShake",
    github: "https://github.com/ChilliCream/graphql-platform",
    website: "https://nextjs.org",
  },
  install: "npm install @strawberryshake/client",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Strawberry Shake's TypeScript output drops into Next.js App Router projects: typed query and mutation hooks, Server Component fetchers, and the same store you use on the client. No extra GraphQL fetcher, no manual DTO duplication, no any.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Install, run codegen, and call the typed client from a Server Component:",
      ],
      code: {
        language: "ts",
        code: `// .graphqlrc.yml
schema: http://localhost:5000/graphql
documents: app/**/*.graphql

// app/projects/page.tsx
import { getProjects } from "@/gql/projects.queries";

export default async function ProjectsPage() {
  const data = await getProjects();
  return <ProjectsList projects={data.projects} />;
}`,
      },
    },
  ],
};

// -----------------------------------------------------------------------------
// Community integrations (6)
// -----------------------------------------------------------------------------

const keycloak: Integration = {
  slug: "keycloak",
  name: "Keycloak",
  letter: "K",
  type: "community",
  category: "auth",
  tagline:
    "Open-source IAM with realms, federated identity, and SAML/OIDC. Maintained by @sjmuller.",
  products: ["hot-chocolate"],
  minVersion: "14.0",
  maintainer: "@sjmuller",
  addedRank: 50,
  links: {
    docs: "https://github.com/sjmuller/HotChocolate.Keycloak",
    nuget: "https://www.nuget.org/packages/HotChocolate.Keycloak",
    github: "https://github.com/sjmuller/HotChocolate.Keycloak",
    website: "https://www.keycloak.org",
  },
  install: "dotnet add package HotChocolate.Keycloak",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Keycloak is the standard self-hosted IAM. This community package wires its OIDC discovery into Hot Chocolate with realm-aware role mapping, so Keycloak roles project onto ASP.NET Core policies and onto @authorize directives without glue code.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Install the community package and bind to your Keycloak realm:",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddKeycloakAuthentication(o =>
    {
        o.Authority = "https://keycloak.example.com/realms/your-realm";
        o.Audience = "your-api";
    });

services
    .AddGraphQLServer()
    .AddAuthorization();`,
      },
    },
  ],
};

const nats: Integration = {
  slug: "nats",
  name: "NATS",
  letter: "N",
  type: "community",
  category: "messaging",
  tagline:
    "NATS JetStream as a Mocha transport. At-least-once, exactly-once, KV streams. Maintained by @nats-community.",
  products: ["mocha"],
  minVersion: "1.0",
  maintainer: "@nats-community",
  addedRank: 45,
  links: {
    docs: "https://github.com/nats-community/Mocha.Nats",
    nuget: "https://www.nuget.org/packages/Mocha.Nats",
    github: "https://github.com/nats-community/Mocha.Nats",
    website: "https://nats.io",
  },
  install: "dotnet add package Mocha.Nats",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "NATS JetStream gives you ordered, replayable streams with optional exactly-once semantics and a key-value abstraction on top of the same primitive. The community Mocha.Nats transport surfaces all three to typed handlers.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: ["Install and configure the JetStream connection:"],
      code: {
        language: "csharp",
        code: `services
    .AddMocha()
    .AddNatsTransport(o =>
    {
        o.Url = "nats://nats:4222";
        o.UseJetStream = true;
    });`,
      },
    },
  ],
};

const mongoAtlas: Integration = {
  slug: "mongodb-atlas",
  name: "MongoDB Atlas",
  letter: "M",
  type: "community",
  category: "data",
  tagline:
    "MongoDB driver with Hot Chocolate filtering and projection. Maintained by @mongo-graphql.",
  products: ["hot-chocolate"],
  minVersion: "14.0",
  maintainer: "@mongo-graphql",
  addedRank: 40,
  links: {
    docs: "https://github.com/mongo-graphql/HotChocolate.Data.MongoDb",
    nuget: "https://www.nuget.org/packages/HotChocolate.Data.MongoDb",
    github: "https://github.com/mongo-graphql/HotChocolate.Data.MongoDb",
    website: "https://www.mongodb.com/atlas",
  },
  install: "dotnet add package HotChocolate.Data.MongoDb",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Hot Chocolate's filtering and projection middlewares with a MongoDB driver underneath. Filters compile into BSON, projections drop unselected fields server-side, and Atlas Search exposes a fts: argument when you wire the search index.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Register the Mongo collection and turn on the data middlewares:",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddSingleton<IMongoClient>(_ =>
        new MongoClient(config["Mongo:ConnectionString"]));

services
    .AddGraphQLServer()
    .AddMongoDbFiltering()
    .AddMongoDbProjections()
    .AddMongoDbSorting();`,
      },
    },
  ],
};

const awsLambda: Integration = {
  slug: "aws-lambda",
  name: "AWS Lambda",
  letter: "A",
  type: "community",
  category: "cloud",
  tagline:
    "Run Hot Chocolate behind API Gateway + Lambda. Cold-start optimised. Maintained by @aws-graphql-community.",
  products: ["hot-chocolate"],
  minVersion: "14.0",
  maintainer: "@aws-graphql-community",
  addedRank: 35,
  links: {
    docs: "https://github.com/aws-graphql-community/HotChocolate.AwsLambda",
    nuget: "https://www.nuget.org/packages/HotChocolate.AwsLambda",
    github: "https://github.com/aws-graphql-community/HotChocolate.AwsLambda",
    website: "https://aws.amazon.com/lambda/",
  },
  install: "dotnet add package HotChocolate.AwsLambda",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "A Lambda host that pre-warms the schema at construction time so a cold start pays the schema-build cost once, not on every request. API Gateway's HTTP API integration handles the wire format; Hot Chocolate sees a normal ASP.NET Core request.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: [
        "Use the provided Lambda entry-point and your existing GraphQL pipeline:",
      ],
      code: {
        language: "csharp",
        code: `var builder = LambdaApplication.CreateBuilder(args);
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();
app.MapGraphQL();
app.Run();`,
      },
    },
  ],
};

const githubActions: Integration = {
  slug: "github-actions",
  name: "GitHub Actions",
  letter: "G",
  type: "community",
  category: "ci-cd",
  tagline:
    "Schema check + Fusion composition checks on every PR. Breaks builds on breaking changes. Maintained by @gha-graphql.",
  products: ["hot-chocolate", "fusion"],
  minVersion: "14.0",
  maintainer: "@gha-graphql",
  addedRank: 55,
  links: {
    docs: "https://github.com/gha-graphql/chillicream-action",
    github: "https://github.com/gha-graphql/chillicream-action",
    website: "https://github.com/features/actions",
  },
  install: "uses: gha-graphql/chillicream-action@v1",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "A reusable GitHub Action that runs Hot Chocolate schema checks and Fusion composition checks on every pull request. It pulls the registered schema, computes the diff, classifies it, and fails the check on a breaking change unless the PR carries an override label.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: ["Drop the action into your workflow:"],
      code: {
        language: "yaml",
        code: `name: schema-check
on: [pull_request]
jobs:
  check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: gha-graphql/chillicream-action@v1
        with:
          subgraph: products
          gateway-endpoint: https://gateway.example.com/graphql`,
      },
    },
  ],
};

const blazorStrawberryShake: Integration = {
  slug: "blazor-strawberry-shake",
  name: "Blazor + Strawberry Shake",
  letter: "B",
  type: "community",
  category: "frontend",
  tagline:
    "Typed Strawberry Shake clients for Blazor SPA and Blazor Server. Maintained by @blazor-graphql.",
  products: ["strawberry-shake"],
  minVersion: "14.0",
  maintainer: "@blazor-graphql",
  addedRank: 30,
  links: {
    docs: "https://github.com/blazor-graphql/StrawberryShake.Blazor",
    nuget: "https://www.nuget.org/packages/StrawberryShake.Blazor",
    github: "https://github.com/blazor-graphql/StrawberryShake.Blazor",
    website: "https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor",
  },
  install: "dotnet add package StrawberryShake.Blazor",
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "Strawberry Shake's typed clients with Blazor-aware DI registration: scoped per circuit on Blazor Server, transient on WASM, and a fluent builder that mirrors AddBlazorAppClient on the .NET side.",
      ],
    },
    {
      heading: "Setup",
      paragraphs: ["Install the Blazor extension and register the client:"],
      code: {
        language: "csharp",
        code: `builder.Services
    .AddBlazorAppClient()
    .ConfigureHttpClient(c =>
        c.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));`,
      },
    },
  ],
};

// -----------------------------------------------------------------------------
// Catalog
// -----------------------------------------------------------------------------

export const INTEGRATIONS: readonly Integration[] = [
  // Native
  mcp,
  opentelemetry,
  nextjs,
  auth0,
  postgresql,
  efCore,
  datadog,
  kafka,
  entraId,
  jaeger,
  azureServiceBus,
  oidc,
  tempo,
  honeycomb,
  // Community
  githubActions,
  keycloak,
  nats,
  mongoAtlas,
  awsLambda,
  blazorStrawberryShake,
];

export const findIntegration = (slug: string): Integration | undefined =>
  INTEGRATIONS.find((i) => i.slug === slug);

// Related = same category first, then product overlap, then anything. Capped
// at 4 to fit the bottom rail without crowding.
export const findRelatedIntegrations = (
  integration: Integration,
  max: number = 4
): readonly Integration[] => {
  const others = INTEGRATIONS.filter((i) => i.slug !== integration.slug);
  const sameCategory = others.filter(
    (i) => i.category === integration.category
  );
  const productOverlap = others.filter(
    (i) =>
      !sameCategory.includes(i) &&
      i.products.some((p) => integration.products.includes(p))
  );
  return [...sameCategory, ...productOverlap, ...others].slice(0, max);
};

// "Recently Added" = top N by addedRank. The Type filter exposes this as a
// pseudo-type alongside Native and Community.
export const recentlyAdded = (max: number = 6): readonly Integration[] => {
  return [...INTEGRATIONS]
    .sort((a, b) => b.addedRank - a.addedRank)
    .slice(0, max);
};

export const featuredIntegrations = (): readonly Integration[] =>
  INTEGRATIONS.filter((i) => i.featured);

export const integrationsByCategory = (
  category: CategoryKey
): readonly Integration[] =>
  INTEGRATIONS.filter((i) => i.category === category);

export const nativeIntegrations = (): readonly Integration[] =>
  INTEGRATIONS.filter((i) => i.type === "native");

export const communityIntegrations = (): readonly Integration[] =>
  INTEGRATIONS.filter((i) => i.type === "community");
