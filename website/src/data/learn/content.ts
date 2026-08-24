// Seed content for /learn. Templates are the README-as-data model that used
// to live in the now-removed src/data/templates/templates.ts: index card front-matter,
// sticky deploy sidebar metadata, and a body[] of section
// { heading, paragraphs[], code? }. This is a mechanical migration of the
// existing 8 templates onto the LearnItem union (type: "template" added,
// content otherwise unchanged) — the body schema is still the seam to swap
// for remote README fetches once template repos exist.
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

// 1. Fusion 3-Service Federation
const fusion3ServiceFederation: TemplateItem = {
  type: "template",
  slug: "fusion-3-service-federation",
  title: "Fusion 3-Service Federation",
  tagline: "Three services, one graph.",
  topology: "federation",
  useCases: ["starter"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate", "fusion"],
  stack: ["postgres"],
  agentReady: false,
  githubUrl: "https://github.com/ChilliCream/templates/tree/main/fusion-3-service-federation",
  demoUrl: "https://demo.chillicream.com/fusion-3-service-federation",
  license: "MIT",
  updatedRelative: "3 days ago",
  cli: [
    {
      key: "dotnet",
      label: "dotnet",
      code: "dotnet new chillicream-fusion-3-service",
    },
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/templates fusion-3-service-federation",
    },
  ],
  body: [
    {
      heading: "What you get",
      paragraphs: [
        "A products + reviews + inventory federation wired with Fusion, ready to deploy. Three independent Hot Chocolate services, each owning its slice of the schema, composed at build time into a single supergraph.",
        "Each subgraph is a normal ASP.NET Core service. The supergraph manifest lives in /gateway and is regenerated from the subgraph schemas on every build. There is no router DSL, no runtime composition, no sidecar.",
      ],
    },
    {
      heading: "Architecture",
      paragraphs: [
        "Three .NET 9 services run on ports 5101 / 5102 / 5103. The Fusion gateway runs on 5100 and is the only public surface. Composition happens during dotnet build of the gateway project, which pulls each subgraph schema over HTTP and emits the supergraph artifact.",
        "Postgres backs reviews and inventory; products is in-memory for the starter. Replace the in-memory store with your own data source — the resolver shape stays the same.",
      ],
      code: {
        language: "text",
        code: `gateway     :5100  →  Fusion supergraph
products    :5101  →  Hot Chocolate
reviews     :5102  →  Hot Chocolate + Postgres
inventory   :5103  →  Hot Chocolate + Postgres`,
      },
    },
    {
      heading: "Run it locally",
      paragraphs: [
        "Bring up Postgres with docker compose, then start the four services with the included tye configuration. The gateway will fail fast if any subgraph is unreachable; that's intentional.",
      ],
      code: {
        language: "bash",
        code: `docker compose up -d postgres
dotnet run --project gateway`,
      },
    },
    {
      heading: "Customize",
      paragraphs: [
        "Add a fourth subgraph by scaffolding another Hot Chocolate project, registering it in gateway/fusion.config.json, and rebuilding. Composition catches breaking changes at build time, not at 2 AM.",
        "The reviews subgraph illustrates @lookup directives for cross-subgraph entity resolution. Copy that pattern into any new subgraph that needs to extend a type defined elsewhere.",
      ],
    },
    {
      heading: "What to read next",
      paragraphs: [
        "The Fusion docs cover the composition rules, the @lookup directive, and the build-time schema-check workflow that ships with every subgraph PR.",
      ],
    },
  ],
};

// 2. Agent-Ready API
const agentReadyApi: TemplateItem = {
  type: "template",
  slug: "agent-ready-api",
  title: "Agent-Ready API",
  tagline: "A Hot Chocolate service that exposes itself as an MCP server.",
  topology: "solo",
  useCases: ["llm-mcp"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate", "nitro"],
  stack: ["mcp"],
  agentReady: true,
  githubUrl: "https://github.com/ChilliCream/templates/tree/main/agent-ready-api",
  demoUrl: "https://demo.chillicream.com/agent-ready-api",
  license: "MIT",
  updatedRelative: "1 week ago",
  cli: [
    { key: "nitro", label: "nitro init", code: "nitro init agent-ready-api" },
    {
      key: "dotnet",
      label: "dotnet",
      code: "dotnet new chillicream-agent-ready",
    },
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/templates agent-ready-api",
    },
  ],
  body: [
    {
      heading: "What you get",
      paragraphs: [
        "A solo Hot Chocolate service that talks two protocols out of the same schema: the human-facing GraphQL endpoint your team already knows, and an MCP server that lets Claude, Cursor, and any other MCP-capable agent introspect, query, and mutate the same surface with proper auth.",
        "The MCP surface is generated from the schema. Every query and mutation becomes a tool; every type becomes a resource. Add a field to your schema and your agents pick it up on the next reload.",
      ],
    },
    {
      heading: "Architecture",
      paragraphs: [
        "The service runs on .NET 9 with a single Hot Chocolate executor. The MCP transport is a HotChocolate.Mcp adapter that listens on a separate port (3000 by default) and translates MCP calls into normal GraphQL operations. Auth tokens flow through the same middleware chain.",
        "Nitro is wired in for observability: every agent call shows up alongside human calls in the same trace view, with the agent identity attached to the operation.",
      ],
      code: {
        language: "csharp",
        code: `builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddMcpServer()        // expose schema as MCP
    .AddInstrumentation();  // Nitro tracing`,
      },
    },
    {
      heading: "Run it locally",
      paragraphs: [
        "Start the service, then point your MCP client at http://localhost:3000/mcp. Claude Desktop, Cursor, and Continue all support MCP servers via local config.",
      ],
      code: {
        language: "bash",
        code: `dotnet run --project src/AgentReadyApi
# in another shell:
nitro mcp inspect http://localhost:3000/mcp`,
      },
    },
    {
      heading: "Customize",
      paragraphs: [
        "Use [McpHidden] to exclude internal mutations from agent surfaces. Use [McpDescription] on resolvers to give agents better tool-selection hints. The schema is still the source of truth.",
        "Pair this template with Nitro Hosted to get per-agent rate-limiting, audit logs, and capability scopes — the same primitives you'd use for human API keys.",
      ],
    },
    {
      heading: "What to read next",
      paragraphs: [
        "The agent-ready guide covers tool naming conventions, descriptions that survive RAG, and the mutation-confirmation flow you almost always want for agent-driven writes.",
      ],
    },
  ],
};

// 3. Polyglot Federation
const polyglotFederation: TemplateItem = {
  type: "template",
  slug: "polyglot-federation",
  title: "Polyglot Federation",
  tagline: "A C# Hot Chocolate service and a Node Yoga service, composed by Fusion.",
  topology: "polyglot",
  useCases: ["starter"],
  language: "mixed",
  clients: ["none"],
  products: ["hot-chocolate", "fusion"],
  stack: ["nodejs"],
  agentReady: false,
  githubUrl: "https://github.com/ChilliCream/templates/tree/main/polyglot-federation",
  license: "MIT",
  updatedRelative: "5 days ago",
  cli: [
    { key: "dotnet", label: "dotnet", code: "dotnet new chillicream-polyglot" },
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/templates polyglot-federation",
    },
  ],
  body: [
    {
      heading: "What you get",
      paragraphs: [
        "Proof your federation isn't ideologically picky. A products subgraph in C# Hot Chocolate, a reviews subgraph in TypeScript GraphQL Yoga, and a Fusion gateway that doesn't care which one is which.",
        "The two subgraphs share entities through @lookup directives. The web client makes one request and gets a joined payload from both runtimes — no client-side stitching, no schema duplication.",
      ],
    },
    {
      heading: "Architecture",
      paragraphs: [
        "products runs on .NET 9 with Hot Chocolate. reviews runs on Node 22 with Yoga. The gateway is a .NET 9 Fusion service that composes both at build time and serves the unified schema.",
        "Composition runs in CI. Either subgraph can land breaking changes; the gateway build will reject them before deploy. The two teams keep their toolchains and their on-call rotations.",
      ],
      code: {
        language: "text",
        code: `gateway     :5100  →  Fusion supergraph (.NET)
products    :5101  →  Hot Chocolate (C#)
reviews     :5102  →  GraphQL Yoga (TypeScript)`,
      },
    },
    {
      heading: "Run it locally",
      paragraphs: [
        "Both runtimes ship with their own dev script. The included tye config orchestrates them so a single command brings up the whole mesh.",
      ],
      code: {
        language: "bash",
        code: `cd reviews && pnpm install && pnpm dev &
dotnet run --project gateway`,
      },
    },
    {
      heading: "Customize",
      paragraphs: [
        "Add a third subgraph in any language that speaks the federation spec. Apollo subgraphs, Pothos, gqlgen — Fusion's composition reads the schema, not the runtime.",
        "Both subgraphs export OpenTelemetry spans on the same context so the gateway trace shows the full fan-out across runtimes.",
      ],
    },
    {
      heading: "What to read next",
      paragraphs: [
        "The polyglot guide walks through the @lookup directive on both runtimes and the entity-resolution contract that lets a reviews row reference a products row across language boundaries.",
      ],
    },
  ],
};

// 4. CQRS with Mocha
const cqrsWithMocha: TemplateItem = {
  type: "template",
  slug: "cqrs-with-mocha",
  title: "CQRS with Mocha",
  tagline: "Hot Chocolate + Mocha on the same schema. Commands, queries, events — same surface.",
  topology: "solo",
  useCases: ["cqrs"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate", "mocha"],
  stack: ["postgres"],
  agentReady: false,
  githubUrl: "https://github.com/ChilliCream/templates/tree/main/cqrs-with-mocha",
  license: "MIT",
  updatedRelative: "2 days ago",
  cli: [
    {
      key: "dotnet",
      label: "dotnet",
      code: "dotnet new chillicream-cqrs-mocha",
    },
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/templates cqrs-with-mocha",
    },
  ],
  body: [
    {
      heading: "What you get",
      paragraphs: [
        "A CQRS-shaped service where the same schema describes the read model, the write model, and the event stream. Hot Chocolate handles the queries, Mocha owns the command handlers and the event projections, and you only maintain one schema.",
        "The template ships with an order-domain example: PlaceOrder, CancelOrder, FulfillOrder commands; OrderQuery for reads; OrderPlaced / OrderCancelled / OrderFulfilled events. Real Postgres event store, real projections, real subscriptions.",
      ],
    },
    {
      heading: "Architecture",
      paragraphs: [
        "Mocha registers command handlers as mutations on the GraphQL schema. The command bus dispatches them; handlers persist events to Postgres. Projections rebuild the read model that Hot Chocolate's resolvers query.",
        "Subscriptions are wired off the event stream — every consumer sees ordered, replayable events without separate WebSocket plumbing.",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddGraphQLServer()
    .AddMochaCommands<OrderCommands>()
    .AddMochaProjections<OrderProjections>()
    .AddSubscriptionType<OrderSubscriptions>()
    .AddPostgresEventStore();`,
      },
    },
    {
      heading: "Run it locally",
      paragraphs: [
        "Bring up Postgres with the included docker-compose, run the migrations, then start the service. The Mocha visualizer shows the full command/event flow as it happens.",
      ],
      code: {
        language: "bash",
        code: `docker compose up -d postgres
dotnet ef database update --project src/CqrsWithMocha
dotnet run --project src/CqrsWithMocha`,
      },
    },
    {
      heading: "Customize",
      paragraphs: [
        "Add a new aggregate by scaffolding a Commands / Events / Projections triple. Mocha's aggregate base class takes care of optimistic concurrency, event versioning, and snapshotting.",
        "Replace Postgres with EventStoreDB or Marten if you already have one. Mocha's event-store interface is intentionally narrow.",
      ],
    },
    {
      heading: "What to read next",
      paragraphs: [
        "The CQRS guide covers the command-handler contract, the projection rebuild story, and how to keep the GraphQL schema and the event schema in sync without duplicating types.",
      ],
    },
  ],
};

// 5. Realtime Subscriptions
const realtimeSubscriptions: TemplateItem = {
  type: "template",
  slug: "realtime-subscriptions",
  title: "Realtime Subscriptions",
  tagline: "WebSockets via SSE. Live order updates from a single resolver.",
  topology: "solo",
  useCases: ["realtime"],
  language: "dotnet",
  clients: ["react-strawberry-shake"],
  products: ["hot-chocolate", "strawberry-shake"],
  stack: ["react", "redis"],
  agentReady: false,
  githubUrl: "https://github.com/ChilliCream/templates/tree/main/realtime-subscriptions",
  demoUrl: "https://demo.chillicream.com/realtime-subscriptions",
  license: "MIT",
  updatedRelative: "1 day ago",
  cli: [
    { key: "dotnet", label: "dotnet", code: "dotnet new chillicream-realtime" },
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/templates realtime-subscriptions",
    },
  ],
  body: [
    {
      heading: "What you get",
      paragraphs: [
        "A live order-tracking page powered by a single subscription resolver on the server and a single useSubscription hook on the React client. Strawberry Shake generates the typed client; you write the resolver.",
        "Subscriptions ride graphql-sse over standard HTTP/2 — no separate WebSocket gateway, no sticky sessions, no special LB rules. The same TLS termination that handles your queries handles your subscriptions.",
      ],
    },
    {
      heading: "Architecture",
      paragraphs: [
        "Hot Chocolate's subscription engine sits on top of an in-process pub/sub for development and a Redis-backed pub/sub for production. The example ships both — flip a config flag to switch.",
        "Strawberry Shake's React integration handles reconnect, last-event-id resumption, and message ordering. The generated hook gives you a typed observable; the rest is React state.",
      ],
      code: {
        language: "csharp",
        code: `public class OrderSubscriptions
{
    [Subscribe]
    public Order OnOrderUpdated([EventMessage] Order order) => order;
}

services
    .AddGraphQLServer()
    .AddSubscriptionType<OrderSubscriptions>()
    .AddRedisSubscriptions();`,
      },
    },
    {
      heading: "Run it locally",
      paragraphs: [
        "Bring up Redis, start the server, then start the React client. Open two browser tabs to see live propagation.",
      ],
      code: {
        language: "bash",
        code: `docker compose up -d redis
dotnet run --project server &
cd client && pnpm install && pnpm dev`,
      },
    },
    {
      heading: "Customize",
      paragraphs: [
        "Add a new subscription by writing a [Subscribe] resolver and a corresponding event publisher. Strawberry Shake regenerates the client when you re-run codegen.",
        "Swap Redis for any IPubSub implementation — NATS, Kafka, RabbitMQ. The interface is one publish and one subscribe method.",
      ],
    },
    {
      heading: "What to read next",
      paragraphs: [
        "The subscriptions guide covers backpressure, multi-tenant topic isolation, and the reconnect contract Strawberry Shake honors out of the box.",
      ],
    },
  ],
};

// 6. Fusion + Nitro Observability
const fusionWithNitroObservability: TemplateItem = {
  type: "template",
  slug: "fusion-with-nitro-observability",
  title: "Fusion + Nitro Observability",
  tagline: "3-service Fusion mesh with Nitro tracing wired in. The Operator's Window starter.",
  topology: "federation",
  useCases: ["observability"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate", "fusion", "nitro"],
  stack: ["opentelemetry", "postgres"],
  agentReady: false,
  featured: true,
  githubUrl: "https://github.com/ChilliCream/templates/tree/main/fusion-with-nitro-observability",
  demoUrl: "https://demo.chillicream.com/fusion-with-nitro-observability",
  license: "MIT",
  updatedRelative: "4 days ago",
  cli: [
    {
      key: "nitro",
      label: "nitro init",
      code: "nitro init fusion-observability",
    },
    {
      key: "dotnet",
      label: "dotnet",
      code: "dotnet new chillicream-fusion-nitro",
    },
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/templates fusion-with-nitro-observability",
    },
  ],
  body: [
    {
      heading: "What you get",
      paragraphs: [
        "The federation starter, but with Nitro's observability stack already wired in. Every query trace shows the gateway plan, the per-subgraph fan-out, the resolver-level timings, and the database calls underneath. The Operator's Window starts populating the moment you run the first query.",
        "OTEL is the wire format. The same spans flow into Nitro Hosted for the polished view and into your existing OTEL collector if you have one.",
      ],
    },
    {
      heading: "Architecture",
      paragraphs: [
        "Three Hot Chocolate subgraphs, one Fusion gateway, one Nitro instrumentation package on every service. The gateway emits per-operation traces with the full plan attached. The subgraphs emit per-resolver spans linked to the gateway trace by traceparent.",
        "Postgres queries pick up automatic instrumentation via Npgsql.OpenTelemetry. No code changes; spans appear in the same waterfall.",
      ],
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
        .AddNpgsql()
        .AddOtlpExporter());`,
      },
    },
    {
      heading: "Run it locally",
      paragraphs: [
        "Bring up Postgres + the OTEL collector with the included docker-compose, then start the four services. Open the Nitro UI on http://localhost:4180 to see the live trace feed.",
      ],
      code: {
        language: "bash",
        code: `docker compose up -d postgres otel-collector
dotnet run --project gateway`,
      },
    },
    {
      heading: "Customize",
      paragraphs: [
        "The OTLP exporter target is a single env var. Point it at Tempo, Honeycomb, Datadog, or Nitro Hosted. Nitro adds federation-aware grouping, breaking-change diffs, and per-operation budgeting; the others get the raw spans.",
        "Add per-operation cost tracking by enabling the cost-analysis middleware. Costs appear as span attributes and are aggregable in the Nitro dashboard.",
      ],
    },
    {
      heading: "What to read next",
      paragraphs: [
        "The observability guide covers trace shape, attribute conventions, and the recommended sampling strategy for each tier of operation (read / mutation / agent).",
      ],
    },
  ],
};

// 7. Multi-tenant SaaS Starter
const multiTenantSaasStarter: TemplateItem = {
  type: "template",
  slug: "multi-tenant-saas-starter",
  title: "Multi-tenant SaaS Starter",
  tagline: "Per-tenant schema isolation, RBAC, audit log.",
  topology: "solo",
  useCases: ["multi-tenant", "auth"],
  language: "dotnet",
  clients: ["nextjs"],
  products: ["hot-chocolate", "nitro"],
  stack: ["nextjs", "postgres"],
  agentReady: false,
  githubUrl: "https://github.com/ChilliCream/templates/tree/main/multi-tenant-saas-starter",
  demoUrl: "https://demo.chillicream.com/multi-tenant-saas-starter",
  license: "MIT",
  updatedRelative: "6 days ago",
  cli: [
    { key: "nitro", label: "nitro init", code: "nitro init multi-tenant-saas" },
    {
      key: "dotnet",
      label: "dotnet",
      code: "dotnet new chillicream-saas-starter",
    },
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/templates multi-tenant-saas-starter",
    },
  ],
  body: [
    {
      heading: "What you get",
      paragraphs: [
        "A SaaS-shaped service with the per-tenant primitives most teams reinvent: tenant resolution from the request, row-level isolation in Postgres, RBAC on every field, an audit log of every mutation, and a Next.js admin console wired up to all of it.",
        "Tenants are first-class — every entity carries a TenantId, every query is automatically scoped, and Nitro's audit log captures the actor / tenant / operation triple for compliance review.",
      ],
    },
    {
      heading: "Architecture",
      paragraphs: [
        "A tenant-resolver middleware reads X-Tenant or a JWT claim and stuffs the resolved Tenant onto the resolver context. Hot Chocolate's authorization integrates with ASP.NET Core's policy system; field-level @authorize directives compose with the tenant scope.",
        "The Next.js console talks to the same GraphQL endpoint over Strawberry Shake. RBAC roles control which fields render, not which routes exist; introspection respects roles so a tenant viewer can't even see fields they're not allowed to read.",
      ],
      code: {
        language: "csharp",
        code: `services
    .AddTenantResolver<HeaderTenantResolver>()
    .AddGraphQLServer()
    .AddAuthorization()
    .AddInstrumentation();

[Authorize(Policy = "TenantMember")]
public partial class Query
{
    public IQueryable<Project> GetProjects(
        [Tenant] Tenant t,
        AppDbContext db) =>
        db.Projects.Where(p => p.TenantId == t.Id);
}`,
      },
    },
    {
      heading: "Run it locally",
      paragraphs: [
        "Bring up Postgres, run migrations, seed two tenants, then start the API and the Next.js console. The seed script prints credentials for an admin and a viewer in each tenant.",
      ],
      code: {
        language: "bash",
        code: `docker compose up -d postgres
dotnet ef database update --project src/Api
dotnet run --project src/Api &
cd console && pnpm install && pnpm dev`,
      },
    },
    {
      heading: "Customize",
      paragraphs: [
        "Swap header-based tenant resolution for JWT-claim or subdomain-based. The resolver interface is one method.",
        "Hard isolation requirements? The template includes a schema-per-tenant Postgres mode behind a config flag. Slower writes, simpler audit, easier export.",
      ],
    },
    {
      heading: "What to read next",
      paragraphs: [
        "The multi-tenant guide covers tenant-resolution strategies, the trade-offs between row-level and schema-level isolation, and the audit-log shape Nitro expects for compliance dashboards.",
      ],
    },
  ],
};

// 8. Blazor + Strawberry Shake
const blazorStrawberryShake: TemplateItem = {
  type: "template",
  slug: "blazor-strawberry-shake",
  title: "Blazor + Strawberry Shake",
  tagline: "Blazor SPA + Strawberry Shake client + Hot Chocolate server. End-to-end typed.",
  topology: "solo",
  useCases: ["starter"],
  language: "dotnet",
  clients: ["blazor-strawberry-shake"],
  products: ["hot-chocolate", "strawberry-shake"],
  stack: ["blazor"],
  agentReady: false,
  githubUrl: "https://github.com/ChilliCream/templates/tree/main/blazor-strawberry-shake",
  license: "MIT",
  updatedRelative: "2 weeks ago",
  cli: [
    {
      key: "dotnet",
      label: "dotnet",
      code: "dotnet new chillicream-blazor-shake",
    },
    {
      key: "git",
      label: "git clone",
      code: "git clone https://github.com/ChilliCream/templates blazor-strawberry-shake",
    },
  ],
  body: [
    {
      heading: "What you get",
      paragraphs: [
        "A Blazor WebAssembly SPA wired to a Hot Chocolate server through Strawberry Shake. Schema-driven, typed end-to-end: the C# types in the Blazor pages are generated from the same schema the server publishes.",
        "Pages, components, and services consume the generated client via standard DI. No HttpClient assembly, no manual DTOs, no string-typed queries.",
      ],
    },
    {
      heading: "Architecture",
      paragraphs: [
        "A single solution with three projects: Server (Hot Chocolate), Client (Blazor WASM), and Shared (Strawberry Shake-generated types). Strawberry Shake runs as a build-time generator; rebuild Shared and the client picks up new types on the next compile.",
        "The Blazor app uses an HttpClient backed by JWT bearer auth; the same handler chains through Strawberry Shake's transport so auth flows transparently.",
      ],
      code: {
        language: "csharp",
        code: `// Client/Program.cs
builder.Services
    .AddBlazorAppClient()
    .ConfigureHttpClient(c =>
        c.BaseAddress = new Uri("https://localhost:5001/graphql"));

// In a Razor component:
@inject IGetProjectsQuery GetProjects

@code {
    var result = await GetProjects.ExecuteAsync(ct);
    var projects = result.Data.Projects;
}`,
      },
    },
    {
      heading: "Run it locally",
      paragraphs: [
        "One dotnet run starts the host, which serves the Blazor static assets and the GraphQL endpoint together. Hot reload works on both sides.",
      ],
      code: {
        language: "bash",
        code: `dotnet run --project src/Server`,
      },
    },
    {
      heading: "Customize",
      paragraphs: [
        "Add a new query by writing the .graphql file next to the page that uses it. Strawberry Shake's generator picks it up and emits a typed client class on rebuild.",
        "Switch to Blazor Server or Blazor United? The Strawberry Shake client is platform-agnostic; only the host changes.",
      ],
    },
    {
      heading: "What to read next",
      paragraphs: [
        "The Strawberry Shake guide covers the codegen contract, the entity store, and the offline-first cache strategy you can layer on for field apps.",
      ],
    },
  ],
};

export const TEMPLATE_ITEMS: readonly TemplateItem[] = [
  fusion3ServiceFederation,
  agentReadyApi,
  polyglotFederation,
  cqrsWithMocha,
  realtimeSubscriptions,
  fusionWithNitroObservability,
  multiTenantSaasStarter,
  blazorStrawberryShake,
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
// link, and Hot Chocolate version note are kept. exampleRepoUrl follows the
// template githubUrl convention (github.com/ChilliCream/<repo>/tree/main/<slug>),
// pointing at github.com/ChilliCream/examples; these repos are a placeholder
// the same way template repos are and still need to be created (website-kbx.23).

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
  exampleRepoUrl: "https://github.com/ChilliCream/examples/tree/main/dataloader-state-context-aware-fetching",
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
  exampleRepoUrl: "https://github.com/ChilliCream/examples/tree/main/ef-core-projections-graphql-performance",
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
  exampleRepoUrl: "https://github.com/ChilliCream/examples/tree/main/opentelemetry-for-services",
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
  exampleRepoUrl: "https://github.com/ChilliCream/examples/tree/main/relative-cursors-vs-offset-pagination",
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
  exampleRepoUrl: "https://github.com/ChilliCream/examples/tree/main/dataloader-in-layered-architecture",
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
  exampleRepoUrl: "https://github.com/ChilliCream/examples/tree/main/greendonut-in-action",
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
  exampleRepoUrl: "https://github.com/ChilliCream/examples/tree/main/dataloader-explained",
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
    { key: "git", label: "git clone", code: "git clone https://github.com/ChilliCream/fusion-demo" },
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
        "The Aspire AppHost wires up Postgres (one database per subgraph), NATS with JetStream backing the Reviews subgraph's event stream, and Keycloak for authentication, then composes all eight subgraphs' schemas into the Gateway project with global object identification enabled. A frontend project and a load generator project round out the solution.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Clone the repo and run the AppHost project; Aspire starts every subgraph, the gateway, and their infrastructure dependencies together and waits on each one before the gateway comes up.",
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
  externalUrl: "https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/examples/Demo",
  githubUrl: "https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/examples/Demo",
  updatedRelative: "this week",
  cli: [
    {
      key: "clone",
      label: "clone the platform repo",
      code: "git clone https://github.com/ChilliCream/graphql-platform.git && cd graphql-platform/src/Mocha/examples/Demo",
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

export const EXAMPLE_ITEMS: readonly ExampleItem[] = [
  fusionDemoExample,
  mochaEcommerceDemoExample,
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

const graphqlWorkshopRepo: WorkshopItem = {
  type: "workshop",
  slug: "graphql-workshop-repo",
  title: "GraphQL Workshop",
  tagline: "A self-paced, hands-on workshop repo covering types, resolvers, DataLoaders, and filtering.",
  products: ["hot-chocolate"],
  level: "beginner",
  externalUrl: "https://github.com/ChilliCream/graphql-workshop",
  githubUrl: "https://github.com/ChilliCream/graphql-workshop",
  updatedRelative: "3 months ago",
  cli: [{ key: "git", label: "git clone", code: "git clone https://github.com/ChilliCream/graphql-workshop" }],
  body: [
    {
      heading: "Overview",
      paragraphs: [
        "A self-paced repo that builds a conference-planner GraphQL server with ASP.NET Core and Hot Chocolate from File → New, one session at a time. A hosted copy of the finished server is browsable at workshop.chillicream.com.",
      ],
    },
    {
      heading: "What you build",
      paragraphs: [
        "Seven sessions, each with its own doc: creating the GraphQL server project, understanding DataLoader, schema design approaches, understanding middleware, adding complex filter capabilities, real-time functionality with subscriptions, and testing the GraphQL server. Each session builds on the previous one's code.",
      ],
    },
    {
      heading: "How to run it",
      paragraphs: [
        "Install the .NET SDK and the Nitro GraphQL IDE, then work through the session docs in order starting with Session 1.",
      ],
    },
  ],
};

export const WORKSHOP_ITEMS: readonly WorkshopItem[] = [fullstackWorkshop, graphqlWorkshopRepo];

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
// to the first non-Starter entry if no template is flagged.
export const findFeaturedTemplate = (): TemplateItem => {
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
