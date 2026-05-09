// Seed templates rendered by /templates and /templates/[slug]. Each entry
// is the README-as-data: index card front-matter, sticky deploy sidebar
// metadata, and a body[] of section { heading, paragraphs[], code? }. We
// deliberately keep the body minimal-but-real-feeling rather than wiring an
// MDX pipeline for eight pages — the body schema is the seam to swap for
// remote README fetches once template repos exist.
//
// Tagging discipline: every template carries a value for all 6 filter axes
// (Topology, Use case, Language, Client, Product mix, Agent-ready). Missing
// any axis breaks faceted filtering silently.

import type {
  ClientKey,
  LanguageKey,
  ProductKey,
  TopologyKey,
  UseCaseKey,
} from "./filters";

export type ThumbnailKind =
  | "federation"
  | "solo"
  | "polyglot"
  | "agents"
  | "subscriptions"
  | "observability"
  | "tenancy"
  | "blazor";

// Per-template accent token. Each thumbnail picks one hue from the same
// palette the page-accent system uses (see redesign-system/AccentThread),
// so federation thumbnails read enterprise-blue, polyglot reads solutions-
// plum, agent-ready reads agents-amber, etc. Drives the --cc-accent custom
// property on the card and on the featured-template hero.
export type ThumbnailAccent =
  | "enterprise"
  | "solutions"
  | "agents"
  | "observability"
  | "pricing"
  | "customers"
  | "integrations"
  | "templates";

export const THUMBNAIL_ACCENT_TOKENS: Record<
  ThumbnailAccent,
  {
    primary: string;
    soft: string;
    line: string;
    gradient: string;
    glow: string;
  }
> = {
  enterprise: {
    primary: "oklch(0.72 0.14 230)",
    soft: "rgba(108, 156, 220, 0.10)",
    line: "rgba(108, 156, 220, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.72 0.14 230), oklch(0.78 0.10 220))",
    glow: "rgba(108, 156, 220, 0.18)",
  },
  solutions: {
    primary: "oklch(0.74 0.16 320)",
    soft: "rgba(220, 140, 220, 0.10)",
    line: "rgba(220, 140, 220, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.74 0.16 320), oklch(0.72 0.18 350))",
    glow: "rgba(220, 140, 220, 0.18)",
  },
  agents: {
    primary: "oklch(0.78 0.16 70)",
    soft: "rgba(247, 186, 100, 0.10)",
    line: "rgba(247, 186, 100, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.78 0.16 70), oklch(0.74 0.18 40))",
    glow: "rgba(247, 186, 100, 0.20)",
  },
  observability: {
    primary: "oklch(0.78 0.14 200)",
    soft: "rgba(96, 200, 220, 0.10)",
    line: "rgba(96, 200, 220, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.78 0.14 200), oklch(0.82 0.12 180))",
    glow: "rgba(96, 200, 220, 0.20)",
  },
  pricing: {
    primary: "oklch(0.78 0.13 250)",
    soft: "rgba(140, 160, 240, 0.10)",
    line: "rgba(140, 160, 240, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.78 0.13 250), oklch(0.74 0.16 290))",
    glow: "rgba(140, 160, 240, 0.18)",
  },
  customers: {
    primary: "oklch(0.74 0.10 70)",
    soft: "rgba(220, 200, 160, 0.10)",
    line: "rgba(220, 200, 160, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.74 0.10 70), oklch(0.72 0.12 40))",
    glow: "rgba(220, 200, 160, 0.16)",
  },
  integrations: {
    primary: "oklch(0.74 0.16 150)",
    soft: "rgba(80, 200, 140, 0.10)",
    line: "rgba(80, 200, 140, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.74 0.16 150), oklch(0.78 0.14 170))",
    glow: "rgba(80, 200, 140, 0.18)",
  },
  templates: {
    primary: "oklch(0.78 0.14 60)",
    soft: "rgba(220, 180, 120, 0.10)",
    line: "rgba(220, 180, 120, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.78 0.14 60), oklch(0.76 0.16 30))",
    glow: "rgba(220, 180, 120, 0.18)",
  },
};

export interface CodeBlock {
  readonly language: string;
  readonly code: string;
}

export interface CliCommand {
  readonly key: string;
  readonly label: string;
  readonly code: string;
}

export interface TemplateSection {
  readonly heading: string;
  readonly paragraphs: readonly string[];
  readonly code?: CodeBlock;
}

export interface Template {
  readonly slug: string;
  readonly title: string;
  readonly tagline: string;
  readonly thumbnail: ThumbnailKind;
  readonly accent: ThumbnailAccent;
  readonly topology: TopologyKey;
  readonly useCases: readonly UseCaseKey[];
  readonly language: LanguageKey;
  readonly clients: readonly ClientKey[];
  readonly products: readonly ProductKey[];
  readonly agentReady: boolean;
  readonly featured?: boolean;
  readonly githubUrl: string;
  readonly demoUrl?: string;
  readonly license: string;
  readonly updatedRelative: string;
  readonly cli: readonly CliCommand[];
  readonly body: readonly TemplateSection[];
}

// -----------------------------------------------------------------------------
// 1. Fusion 3-Service Federation
// -----------------------------------------------------------------------------
const fusion3ServiceFederation: Template = {
  slug: "fusion-3-service-federation",
  title: "Fusion 3-Service Federation",
  tagline: "Three services, one graph.",
  thumbnail: "federation",
  accent: "enterprise",
  topology: "federation",
  useCases: ["starter"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate", "fusion"],
  agentReady: false,
  githubUrl:
    "https://github.com/ChilliCream/templates/tree/main/fusion-3-service-federation",
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

// -----------------------------------------------------------------------------
// 2. Agent-Ready API
// -----------------------------------------------------------------------------
const agentReadyApi: Template = {
  slug: "agent-ready-api",
  title: "Agent-Ready API",
  tagline: "A Hot Chocolate service that exposes itself as an MCP server.",
  thumbnail: "agents",
  accent: "agents",
  topology: "solo",
  useCases: ["llm-mcp"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate", "nitro"],
  agentReady: true,
  githubUrl:
    "https://github.com/ChilliCream/templates/tree/main/agent-ready-api",
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

// -----------------------------------------------------------------------------
// 3. Polyglot Federation
// -----------------------------------------------------------------------------
const polyglotFederation: Template = {
  slug: "polyglot-federation",
  title: "Polyglot Federation",
  tagline:
    "A C# Hot Chocolate service and a Node Yoga service, composed by Fusion.",
  thumbnail: "polyglot",
  accent: "solutions",
  topology: "polyglot",
  useCases: ["starter"],
  language: "mixed",
  clients: ["none"],
  products: ["hot-chocolate", "fusion"],
  agentReady: false,
  githubUrl:
    "https://github.com/ChilliCream/templates/tree/main/polyglot-federation",
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

// -----------------------------------------------------------------------------
// 4. CQRS with Mocha
// -----------------------------------------------------------------------------
const cqrsWithMocha: Template = {
  slug: "cqrs-with-mocha",
  title: "CQRS with Mocha",
  tagline:
    "Hot Chocolate + Mocha on the same schema. Commands, queries, events — same surface.",
  thumbnail: "solo",
  accent: "customers",
  topology: "solo",
  useCases: ["cqrs"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate", "mocha"],
  agentReady: false,
  githubUrl:
    "https://github.com/ChilliCream/templates/tree/main/cqrs-with-mocha",
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

// -----------------------------------------------------------------------------
// 5. Realtime Subscriptions
// -----------------------------------------------------------------------------
const realtimeSubscriptions: Template = {
  slug: "realtime-subscriptions",
  title: "Realtime Subscriptions",
  tagline: "WebSockets via SSE. Live order updates from a single resolver.",
  thumbnail: "subscriptions",
  accent: "pricing",
  topology: "solo",
  useCases: ["realtime"],
  language: "dotnet",
  clients: ["react-strawberry-shake"],
  products: ["hot-chocolate", "strawberry-shake"],
  agentReady: false,
  githubUrl:
    "https://github.com/ChilliCream/templates/tree/main/realtime-subscriptions",
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

// -----------------------------------------------------------------------------
// 6. Fusion + Nitro Observability
// -----------------------------------------------------------------------------
const fusionWithNitroObservability: Template = {
  slug: "fusion-with-nitro-observability",
  title: "Fusion + Nitro Observability",
  tagline:
    "3-service Fusion mesh with Nitro tracing wired in. The Operator's Window starter.",
  thumbnail: "observability",
  accent: "observability",
  topology: "federation",
  useCases: ["observability"],
  language: "dotnet",
  clients: ["none"],
  products: ["hot-chocolate", "fusion", "nitro"],
  agentReady: false,
  featured: true,
  githubUrl:
    "https://github.com/ChilliCream/templates/tree/main/fusion-with-nitro-observability",
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

// -----------------------------------------------------------------------------
// 7. Multi-tenant SaaS Starter
// -----------------------------------------------------------------------------
const multiTenantSaasStarter: Template = {
  slug: "multi-tenant-saas-starter",
  title: "Multi-tenant SaaS Starter",
  tagline: "Per-tenant schema isolation, RBAC, audit log.",
  thumbnail: "tenancy",
  accent: "integrations",
  topology: "solo",
  useCases: ["multi-tenant", "auth"],
  language: "dotnet",
  clients: ["nextjs"],
  products: ["hot-chocolate", "nitro"],
  agentReady: false,
  githubUrl:
    "https://github.com/ChilliCream/templates/tree/main/multi-tenant-saas-starter",
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

// -----------------------------------------------------------------------------
// 8. Blazor + Strawberry Shake
// -----------------------------------------------------------------------------
const blazorStrawberryShake: Template = {
  slug: "blazor-strawberry-shake",
  title: "Blazor + Strawberry Shake",
  tagline:
    "Blazor SPA + Strawberry Shake client + Hot Chocolate server. End-to-end typed.",
  thumbnail: "blazor",
  accent: "templates",
  topology: "solo",
  useCases: ["starter"],
  language: "dotnet",
  clients: ["blazor-strawberry-shake"],
  products: ["hot-chocolate", "strawberry-shake"],
  agentReady: false,
  githubUrl:
    "https://github.com/ChilliCream/templates/tree/main/blazor-strawberry-shake",
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

export const TEMPLATES: readonly Template[] = [
  fusion3ServiceFederation,
  agentReadyApi,
  polyglotFederation,
  cqrsWithMocha,
  realtimeSubscriptions,
  fusionWithNitroObservability,
  multiTenantSaasStarter,
  blazorStrawberryShake,
];

export const findTemplate = (slug: string): Template | undefined =>
  TEMPLATES.find((t) => t.slug === slug);

// The single template promoted as the index-page hero. Falls back to the
// first non-Starter entry if no template is flagged.
export const findFeaturedTemplate = (): Template => {
  const flagged = TEMPLATES.find((t) => t.featured);
  if (flagged) {
    return flagged;
  }
  const nonStarter = TEMPLATES.find((t) => !t.useCases.includes("starter"));
  return nonStarter ?? TEMPLATES[0];
};

// Related = same topology first, then same product mix overlap, then anything.
// Capped at 3 to match the Vercel pattern: lateral exploration without
// overwhelming the reader.
export const findRelated = (
  template: Template,
  max: number = 3
): readonly Template[] => {
  const others = TEMPLATES.filter((t) => t.slug !== template.slug);
  const sameTopology = others.filter((t) => t.topology === template.topology);
  const productOverlap = others.filter(
    (t) =>
      !sameTopology.includes(t) &&
      t.products.some((p) => template.products.includes(p))
  );
  return [...sameTopology, ...productOverlap, ...others].slice(0, max);
};
