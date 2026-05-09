// The seven solution records. Each entry is a fully editorial page: hero,
// proof metrics, pillars, diagram pick, optional code snippet, testimonials,
// feature card IDs from the shared library, collateral offer, logos, final
// CTA stack, related slugs.
//
// 5 use-case pages + 2 industry pages. Industry pages omit the code
// snippet by design (Solutions teardown rule: code lives on /docs, never
// on industry pages). Everything else is the same template.

import type { SolutionRecord } from "./types";

const POLYGLOT_FUSION_YAML = `# fusion.yaml — three subgraphs, three languages, one supergraph

subgraphs:
  - name: catalog
    runtime: node
    schema: ./catalog/schema.graphql
    url: https://catalog.svc.internal/graphql

  - name: billing
    runtime: java-spring
    schema: ./billing/schema.graphql
    url: https://billing.svc.internal/graphql

  - name: shipping
    runtime: python-strawberry
    schema: ./shipping/schema.graphql
    url: https://shipping.svc.internal/graphql

gateway:
  runtime: nitro
  composition: build-time
  output: ./dist/supergraph.json
`;

const HOT_CHOCOLATE_PROGRAM_CS = `// Program.cs — production-ready GraphQL in fifteen lines.

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddAuthorization()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddInstrumentation();

var app = builder.Build();

app.MapGraphQL();
app.Run();
`;

const FEDERATION_FUSION_YAML = `# fusion.yaml — four BFF teams, one composed supergraph.

subgraphs:
  - name: identity
    owner: team-platform
    schema: ./identity/schema.graphql

  - name: orders
    owner: team-commerce
    schema: ./orders/schema.graphql

  - name: catalog
    owner: team-catalog
    schema: ./catalog/schema.graphql

  - name: support
    owner: team-cx
    schema: ./support/schema.graphql

gateway:
  runtime: nitro
  composition: build-time
  breakingChanges: fail-ci
`;

const AGENTS_MCP_TOOL_CALL = `// MCP tool call — agent reads a trace, walks the resolver, proposes a fix.

mcp.call("graph.search", {
  since: "1h",
  orderBy: "p95"
});
// → 1 result · Billing.charge · p95 412ms

mcp.call("resolver.read", {
  path: "Billing.charge"
});
// → Billing.Resolvers/Charge.cs · 84 lines · 1 hot path

mcp.call("trace.explain", {
  span: "billing.charge:db.read"
});
// → N+1 on Customer.invoices · suggested DataLoader
`;

const MOCHA_COMMAND = `// CancelOrder.cs — same schema, same auth, async via the Mocha bus.

[Command]
public sealed record CancelOrder(Guid OrderId, string Reason);

public sealed class CancelOrderHandler
    : ICommandHandler<CancelOrder>
{
    public async Task<CommandResult> HandleAsync(
        CancelOrder cmd,
        CancellationToken ct)
    {
        var order = await _orders.LoadAsync(cmd.OrderId, ct);
        order.Cancel(cmd.Reason);

        await _bus.PublishAsync(
            new OrderCancelled(cmd.OrderId, cmd.Reason),
            ct);

        return CommandResult.Ok();
    }
}
`;

export const SOLUTIONS: readonly SolutionRecord[] = [
  // ============================================================
  // 1. Polyglot federation — flagship, the differentiator page.
  // ============================================================
  {
    slug: "polyglot-federation",
    category: "use-case",
    title: "Polyglot federation",
    metaDescription:
      "Compose Node, Java, Python, Go, and .NET subgraphs into a single federated GraphQL API with Fusion. Your teams keep their stack. Your clients see one schema.",
    hero: {
      eyebrow: "Solutions / Polyglot federation",
      headline: "One graph. Every language.",
      headlineAccent: "Every language.",
      sub: "Compose Node, Java, Python, Go, and .NET subgraphs into a single federated API with Fusion. Your teams keep their stack. Your clients see one schema.",
      primaryCta: {
        label: "Compose your first graph",
        href: "/docs/fusion/getting-started",
      },
      secondaryCta: {
        label: "Talk to a Fusion engineer",
        href: "/contact/sales",
      },
    },
    proofMetrics: [
      {
        value: "12",
        outcome: "languages composed in a single supergraph",
        customer: "Logistics PaaS",
      },
      {
        value: "47 → 1",
        outcome: "BFFs collapsed into one Fusion mesh",
        customer: "Tier-1 EU Bank",
      },
      {
        value: "5",
        outcome: "product graphs unified for end-to-end checkout",
        customer: "Microsoft commerce",
      },
      {
        value: "9 wks",
        outcome: "from kick-off to first production release",
        customer: "North-American FSI",
      },
    ],
    pillars: {
      headline: "Stack-agnostic by design.",
      sub: "Three load-bearing promises. Together they remove the single biggest objection to Fusion: “is this a .NET-only product?”",
      items: [
        {
          title: "Stack-agnostic by design.",
          body: "Fusion composes any GraphQL subgraph that speaks the spec. We never look at your runtime.",
          icon: "compose",
        },
        {
          title: "No .NET tax on your team.",
          body: "Operate Fusion as a managed gateway through Nitro. Your Node team writes Node. Your Java team writes Java. Nobody writes C# unless they want to.",
          icon: "stack",
        },
        {
          title: "One contract for clients.",
          body: "A unified, type-safe schema your frontend, mobile, and AI agents can all consume, without knowing or caring which service answers which field.",
          icon: "schema",
        },
      ],
    },
    diagram: "polyglot",
    codeSnippet: {
      language: "yaml",
      fileName: "fusion.yaml",
      source: POLYGLOT_FUSION_YAML,
    },
    testimonials: [
      {
        quote:
          "We had Java, Node, and Python BFFs that nobody wanted to rewrite. Fusion let us compose all three in a week. The .NET part was invisible.",
        author: "Head of Platform Engineering",
        title: "Head of Platform Engineering",
        company: "EU Tier-1 Bank",
        monogram: "EB",
      },
      {
        quote:
          "The polyglot story is the only reason we got buy-in from the Java org. Six months in, the Java team has never opened the gateway repo. That is exactly what we promised them.",
        author: "Principal Architect",
        title: "Principal Architect",
        company: "Logistics PaaS",
        monogram: "LP",
      },
    ],
    featureCards: [
      "dx",
      "performance",
      "observability",
      "security",
      "scale",
      "openness",
    ],
    collateral: {
      title: "Get the Polyglot Federation Playbook",
      href: "/resources/polyglot-federation-playbook",
      kind: "playbook",
    },
    logos: [
      "microsoft",
      "adidas",
      "sbb",
      "allianz",
      "euTier1Bank",
      "top3EuInsurer",
      "logisticsPaaS",
      "naHealthNetwork",
      "fsiGroup",
      "globalCardNetwork",
    ],
    logoCaption: "Polyglot federations live in production at",
    finalCta: {
      headline: "Ship the federation your teams will actually adopt.",
      sub: "A managed gateway your Node, Java, and Python teams never have to learn. A composition pipeline that fails CI on breaking changes. A bill that scales with traffic, not seats.",
      primary: { label: "Get started", href: "/docs/fusion/getting-started" },
      secondary: { label: "Talk to us", href: "/contact/sales" },
      tertiary: { label: "See Nitro", href: "/products/nitro" },
    },
    related: ["federation", "agents", "single-graph"],
    // Polyglot keeps the magenta-violet-blue gradient from the original hero;
    // extend it across the whole page so the accent is a system, not a sticker.
    accent: {
      primary: "oklch(0.74 0.18 320)",
      soft: "rgba(220, 130, 220, 0.12)",
      line: "rgba(220, 130, 220, 0.34)",
      gradient:
        "linear-gradient(120deg, oklch(0.78 0.16 330), oklch(0.72 0.18 290) 55%, oklch(0.70 0.18 250))",
      glow: "rgba(180, 130, 230, 0.22)",
    },
    heroMotif: "orbit",
  },

  // ============================================================
  // 2. Single-graph — top-of-funnel volume play.
  // ============================================================
  {
    slug: "single-graph",
    category: "use-case",
    title: "A single GraphQL service",
    metaDescription:
      "Hot Chocolate is a complete GraphQL server framework. No federation required. From dotnet new to production-ready in fifteen lines.",
    hero: {
      eyebrow: "Solutions / Single graph",
      headline: "Just a great GraphQL server.",
      headlineAccent: "great",
      sub: "Hot Chocolate is a complete server framework. No federation required. From dotnet new to production-ready, with the same schema your federation will eventually use.",
      primaryCta: {
        label: "Start with Hot Chocolate",
        href: "/docs/hotchocolate",
      },
      secondaryCta: {
        label: "Watch the 10-minute tour",
        href: "/blog/hot-chocolate-tour",
      },
    },
    proofMetrics: [
      {
        value: "12 min",
        outcome: "from dotnet new to first deployed query",
        customer: "Public-sector cloud",
      },
      {
        value: "0.4 ms",
        outcome: "median resolver overhead at p50 under load",
        customer: "Nordic Telco",
      },
      {
        value: "120k req/s",
        outcome: "single-instance steady-state on a 4-core box",
        customer: "Global Card Network",
      },
      {
        value: "100%",
        outcome: "schema portability when the team adopted Fusion",
        customer: "DACH Reinsurer",
      },
    ],
    pillars: {
      headline: "Production-ready out of the box.",
      sub: "Hot Chocolate ships the parts most teams glue together over six months. You compose, we ship the engine.",
      items: [
        {
          title: "Schema-first or code-first.",
          body: "Pick the style your team prefers and switch later. The runtime treats them as the same schema.",
          icon: "schema",
        },
        {
          title: "First-class .NET DI.",
          body: "Your services, your scopes, your conventions. No bespoke container, no hidden lifetimes, no resolver registry to keep in sync.",
          icon: "compose",
        },
        {
          title: "Production-ready out of the box.",
          body: "Persisted queries, complexity limits, projections, filtering, sorting, OpenTelemetry. Configured by extension methods, not yaml.",
          icon: "speed",
        },
        {
          title: "Ready when you grow.",
          body: "When your single service splits into many, your existing schema becomes a Fusion subgraph. No rewrite, no second framework.",
          icon: "scale",
        },
      ],
    },
    diagram: "single-graph",
    codeSnippet: {
      language: "csharp",
      fileName: "Program.cs",
      source: HOT_CHOCOLATE_PROGRAM_CS,
    },
    testimonials: [
      {
        quote:
          "Two engineers, one weekend, a production GraphQL surface backing our entire customer portal. The same schema is now half of our Fusion supergraph.",
        author: "Engineering Lead",
        title: "Engineering Lead",
        company: "DACH Reinsurer",
        monogram: "DR",
      },
    ],
    featureCards: [
      "dx",
      "performance",
      "observability",
      "security",
      "openness",
      "scale",
    ],
    collateral: {
      title: "Get the Hot Chocolate Starter Kit",
      href: "/templates/hot-chocolate-starter",
      kind: "starter",
    },
    logos: [
      "microsoft",
      "sbb",
      "adidas",
      "swissgrid",
      "publicSector",
      "nordicTelco",
      "ukChallengerBank",
      "iberianRetailBank",
      "globalCardNetwork",
    ],
    logoCaption: "Single-service Hot Chocolate is shipped by",
    finalCta: {
      headline: "Build a GraphQL service you will actually want to maintain.",
      sub: "MIT-licensed, .NET-native, federation-ready when you need it. The fastest path from a blank repo to a typed API your clients can consume.",
      primary: { label: "Get started", href: "/docs/hotchocolate" },
      secondary: { label: "Talk to us", href: "/contact/sales" },
      tertiary: { label: "See Nitro", href: "/products/nitro" },
    },
    related: ["federation", "event-driven", "agents"],
    // Single-graph: a focused, calmer cyan-teal. The page is about one
    // service, one schema, one beam from origin to client.
    accent: {
      primary: "oklch(0.78 0.14 200)",
      soft: "rgba(96, 200, 220, 0.10)",
      line: "rgba(96, 200, 220, 0.32)",
      gradient:
        "linear-gradient(120deg, oklch(0.80 0.13 200), oklch(0.76 0.14 220))",
      glow: "rgba(96, 200, 220, 0.20)",
    },
    heroMotif: "light-cone",
  },

  // ============================================================
  // 3. Federation — the multi-team play.
  // ============================================================
  {
    slug: "federation",
    category: "use-case",
    title: "Federation",
    metaDescription:
      "Fusion composes a single API from every team's subgraph at build time. Stop coordinating release trains. Each team owns their slice of the schema.",
    hero: {
      eyebrow: "Solutions / Federation",
      headline: "Many teams. One graph.",
      headlineAccent: "One graph.",
      sub: "Stop coordinating release trains across BFF teams. Fusion composes a single API from every team's subgraph at build time, so each team owns their slice of the schema.",
      primaryCta: {
        label: "Compose a federation",
        href: "/docs/fusion/getting-started",
      },
      secondaryCta: {
        label: "Read the Federation Playbook",
        href: "/resources/federation-playbook",
      },
    },
    proofMetrics: [
      {
        value: "23",
        outcome: "subgraphs composed every commit",
        customer: "Microsoft commerce",
      },
      {
        value: "0",
        outcome: "release trains coordinated since adoption",
        customer: "Adidas",
      },
      {
        value: "98%",
        outcome: "of breaking changes caught in CI, not staging",
        customer: "EU Tier-1 Bank",
      },
      {
        value: "11 → 4",
        outcome: "hours of monthly cross-team gateway syncs",
        customer: "Top-3 EU Insurer",
      },
    ],
    pillars: {
      headline: "Federation that ships per team, not per train.",
      sub: "Fusion's composition contract is build-time. Each team's pipeline owns its subgraph; the gateway is regenerated, signed, and deployed atomically.",
      items: [
        {
          title: "Build-time composition.",
          body: "No runtime gateway DSL, no Rust router to keep alive. Composition happens in CI; the gateway boots from a signed supergraph artifact.",
          icon: "compose",
        },
        {
          title: "Per-team ownership.",
          body: "Every subgraph has a code owner, a CI pipeline, and a clear ownership boundary in the supergraph. No “who owns Order.id” arguments.",
          icon: "stack",
        },
        {
          title: "Breaking-change detection in CI.",
          body: "Fusion compares the candidate supergraph against the live one. Removed fields, narrowed return types, changed nullability — all flagged before merge.",
          icon: "shield",
        },
        {
          title: "Federation-aware tracing.",
          body: "Every gateway hop is a span. p95 by subgraph, p99 by resolver, OpenTelemetry-native. Find the slow team, not the slow query.",
          icon: "graph",
        },
      ],
    },
    diagram: "federation",
    codeSnippet: {
      language: "yaml",
      fileName: "fusion.yaml",
      source: FEDERATION_FUSION_YAML,
    },
    testimonials: [
      {
        quote:
          "We replaced four years of cross-team meetings with a CI check. The first time Fusion failed a merge for a removed field, the room went quiet.",
        author: "Director of API Platform",
        title: "Director of API Platform",
        company: "Microsoft commerce",
        monogram: "MS",
      },
      {
        quote:
          "We have 23 subgraphs across nine teams. The gateway redeploys on every commit. Nobody has to ask permission to ship.",
        author: "Staff Engineer, Platform",
        title: "Staff Engineer, Platform",
        company: "Top-3 EU Insurer",
        monogram: "EI",
      },
    ],
    featureCards: [
      "dx",
      "performance",
      "security",
      "observability",
      "scale",
      "openness",
    ],
    collateral: {
      title: "Get the Federation Playbook",
      href: "/resources/federation-playbook",
      kind: "playbook",
    },
    logos: [
      "microsoft",
      "adidas",
      "sbb",
      "swissgrid",
      "allianz",
      "euTier1Bank",
      "top3EuInsurer",
      "iberianRetailBank",
      "fsiGroup",
      "ukChallengerBank",
    ],
    logoCaption: "Federations ship daily at",
    finalCta: {
      headline: "Trade release trains for a typed contract.",
      sub: "Fusion lets every BFF team ship on their own schedule. The gateway gets a new supergraph on every merge. Your clients never see the seams.",
      primary: { label: "Get started", href: "/docs/fusion/getting-started" },
      secondary: { label: "Talk to us", href: "/contact/sales" },
      tertiary: { label: "See Nitro", href: "/products/nitro" },
    },
    related: ["polyglot-federation", "single-graph", "agents"],
    // Federation: violet-indigo. Many teams converging into one supergraph,
    // a perspective grid vanishing toward a central horizon.
    accent: {
      primary: "oklch(0.74 0.16 280)",
      soft: "rgba(160, 140, 230, 0.10)",
      line: "rgba(160, 140, 230, 0.32)",
      gradient:
        "linear-gradient(120deg, oklch(0.76 0.16 280), oklch(0.74 0.18 250))",
      glow: "rgba(160, 140, 230, 0.20)",
    },
    heroMotif: "perspective-grid",
  },

  // ============================================================
  // 4. Agents — positioning play.
  // ============================================================
  {
    slug: "agents",
    category: "use-case",
    title: "Agents",
    metaDescription:
      "Hot Chocolate, Mocha, and Fusion expose your live federation over MCP. Agents read traces, walk resolvers, and propose changes safely.",
    hero: {
      eyebrow: "Solutions / Agents",
      headline: "The platform agents can operate.",
      headlineAccent: "agents can operate.",
      sub: "Hot Chocolate, Mocha, and Fusion expose your live federation over MCP. Agents don't just write code that calls your API — they read traces, walk resolvers, and propose changes safely.",
      primaryCta: {
        label: "Connect an MCP client",
        href: "/docs/agents/mcp",
      },
      secondaryCta: {
        label: "See the agent loop",
        href: "/products/nitro/agents",
      },
    },
    proofMetrics: [
      {
        value: "4",
        outcome: "agent clients live against the same MCP surface",
        customer: "FSI Group",
      },
      {
        value: "92%",
        outcome: "of incident triage starts at the agent terminal",
        customer: "Logistics PaaS",
      },
      {
        value: "30 min",
        outcome: "average time-to-first-fix on a strange p99 spike",
        customer: "EU Tier-1 Bank",
      },
      {
        value: "0",
        outcome: "agent calls that bypass the audit log",
        customer: "Top-3 EU Insurer",
      },
    ],
    pillars: {
      headline: "An API platform an agent can actually drive.",
      sub: "MCP-native, schema-typed, and audited end to end. Agents work the same way humans do: read first, reason, then act.",
      items: [
        {
          title: "MCP-native.",
          body: "Hot Chocolate and Fusion ship a first-class MCP surface. Cursor, Claude, Copilot, and your own agents all see the same tools.",
          icon: "agent",
        },
        {
          title: "Schema-typed responses.",
          body: "Tool calls return GraphQL types, not JSON soup. Agents reason in the same vocabulary your engineers do.",
          icon: "schema",
        },
        {
          title: "Audited via Mocha.",
          body: "Every agent call is a command on the bus. Replay-safe, signed, and inspectable in the same trace as a human operator.",
          icon: "audit",
        },
        {
          title: "Read-and-write capable.",
          body: "Agents propose changes, scaffold commands, and regenerate clients. Mocha guarantees the write path is auditable, reversible, and rate-limited.",
          icon: "compose",
        },
      ],
    },
    diagram: "agents",
    codeSnippet: {
      language: "typescript",
      fileName: "mcp.session.ts",
      source: AGENTS_MCP_TOOL_CALL,
    },
    testimonials: [
      {
        quote:
          "Our SREs stopped grepping logs. They open the agent terminal, ask for the slowest resolver this hour, and the answer comes back with the resolver source attached.",
        author: "Head of Reliability",
        title: "Head of Reliability",
        company: "Logistics PaaS",
        monogram: "LP",
      },
    ],
    featureCards: [
      "dx",
      "observability",
      "security",
      "scale",
      "openness",
      "performance",
    ],
    collateral: {
      title: "Get the Agent-Ready Federation Workshop",
      href: "/services/training/agents",
      kind: "workshop",
    },
    logos: [
      "microsoft",
      "adidas",
      "logisticsPaaS",
      "fsiGroup",
      "euTier1Bank",
      "top3EuInsurer",
      "naHealthNetwork",
      "globalCardNetwork",
      "nordicTelco",
    ],
    logoCaption: "Agent-driven platforms shipping today",
    finalCta: {
      headline: "Stop bolting agents onto your API. Build the API agents want.",
      sub: "MCP-native primitives, schema-typed responses, Mocha-audited writes. The same surface humans use, exposed to the agents you actually want to give the keys to.",
      primary: { label: "Get started", href: "/docs/agents/mcp" },
      secondary: { label: "Talk to us", href: "/contact/sales" },
      tertiary: { label: "See Nitro", href: "/products/nitro" },
    },
    related: ["federation", "event-driven", "single-graph"],
    // Agents: amber. Reuse the agents-page accent so the system signal
    // ("the agent is doing something") is consistent across the suite.
    accent: {
      primary: "oklch(0.78 0.16 70)",
      soft: "rgba(247, 186, 100, 0.12)",
      line: "rgba(247, 186, 100, 0.34)",
      gradient:
        "linear-gradient(120deg, oklch(0.80 0.16 80), oklch(0.74 0.18 40))",
      glow: "rgba(247, 186, 100, 0.22)",
    },
    heroMotif: "ray-burst",
  },

  // ============================================================
  // 5. Event-driven — push, don't poll.
  // ============================================================
  {
    slug: "event-driven",
    category: "use-case",
    title: "Event-driven systems",
    metaDescription:
      "Mocha brings commands, queries, events, and topology to GraphQL. Subscriptions, change-streams, and async resolvers in one schema.",
    hero: {
      eyebrow: "Solutions / Event-driven",
      headline: "Push, don't poll.",
      headlineAccent: "Push,",
      sub: "Mocha brings commands, queries, events, and topology to GraphQL. Subscriptions, change-streams, and async resolvers — same schema, same auth, same tracing.",
      primaryCta: {
        label: "Add Mocha to your graph",
        href: "/docs/mocha/getting-started",
      },
      secondaryCta: {
        label: "See the bus topology",
        href: "/docs/mocha/topology",
      },
    },
    proofMetrics: [
      {
        value: "1.4M",
        outcome: "live subscriptions on a single Nitro region",
        customer: "Nordic Telco",
      },
      {
        value: "12 ms",
        outcome: "median end-to-end latency, command to event",
        customer: "Global Card Network",
      },
      {
        value: "0",
        outcome: "polling endpoints in customer-facing apps",
        customer: "UK Challenger Bank",
      },
      {
        value: "3",
        outcome: "transports unified under one schema",
        customer: "Logistics PaaS",
      },
    ],
    pillars: {
      headline: "Real-time as a first-class citizen of the schema.",
      sub: "Mocha sits next to Hot Chocolate. Every command is a query. Every event is a subscription. The topology is a graph you can read.",
      items: [
        {
          title: "Subscriptions over WebSockets / SSE.",
          body: "Same auth, same field-level RBAC, same persisted-query gate. The transport is an implementation detail.",
          icon: "bus",
        },
        {
          title: "Mocha bus over Kafka / Service Bus / NATS.",
          body: "Bring your broker. Mocha adapts to the topology you already operate. No second control plane to deploy.",
          icon: "compose",
        },
        {
          title: "Topology you can read.",
          body: "The bus is a graph. Producers, consumers, retries, and dead-letter queues are first-class nodes you can query.",
          icon: "graph",
        },
        {
          title: "Backpressure baked in.",
          body: "Per-tenant rate limits, fan-out caps, and replay-safe consumer groups. The runtime defends itself.",
          icon: "shield",
        },
      ],
    },
    diagram: "event-bus",
    codeSnippet: {
      language: "csharp",
      fileName: "CancelOrderHandler.cs",
      source: MOCHA_COMMAND,
    },
    testimonials: [
      {
        quote:
          "We retired three custom polling layers when Mocha shipped. The mobile app dropped a quarter of its battery use overnight.",
        author: "Mobile Platform Lead",
        title: "Mobile Platform Lead",
        company: "UK Challenger Bank",
        monogram: "UC",
      },
    ],
    featureCards: [
      "performance",
      "scale",
      "observability",
      "security",
      "dx",
      "openness",
    ],
    collateral: {
      title: "Get the Mocha Topology Starter",
      href: "/templates/mocha-event-driven",
      kind: "starter",
    },
    logos: [
      "adidas",
      "sbb",
      "swissgrid",
      "nordicTelco",
      "ukChallengerBank",
      "logisticsPaaS",
      "globalCardNetwork",
      "naHealthNetwork",
      "fsiGroup",
    ],
    logoCaption: "Event-driven platforms running on Mocha",
    finalCta: {
      headline: "One schema for queries, commands, and events.",
      sub: "Mocha bridges the synchronous and asynchronous halves of your platform without forcing a second framework on the team.",
      primary: { label: "Get started", href: "/docs/mocha/getting-started" },
      secondary: { label: "Talk to us", href: "/contact/sales" },
      tertiary: { label: "See Nitro", href: "/products/nitro" },
    },
    related: ["single-graph", "federation", "agents"],
    // Event-driven: lime-mint. Things in motion, fan-out, denser orbits.
    accent: {
      primary: "oklch(0.78 0.18 150)",
      soft: "rgba(120, 220, 160, 0.10)",
      line: "rgba(120, 220, 160, 0.32)",
      gradient:
        "linear-gradient(120deg, oklch(0.80 0.18 150), oklch(0.78 0.16 180))",
      glow: "rgba(120, 220, 160, 0.20)",
    },
    heroMotif: "orbit",
  },

  // ============================================================
  // 6. Banking — industry, lean, no code.
  // ============================================================
  {
    slug: "banking",
    category: "industry",
    title: "Banking & financial services",
    metaDescription:
      "Air-gapped Nitro, federation governance, audit by Mocha. The platform stack chosen by Tier-1 European banks for regulated workloads.",
    hero: {
      eyebrow: "Industries / Banking",
      headline: "Banking-grade GraphQL.",
      headlineAccent: "Banking-grade",
      sub: "Air-gapped Nitro, federation governance, audit by Mocha. The platform stack chosen by Tier-1 European banks for customer-facing and regulated workloads.",
      primaryCta: {
        label: "Talk to a banking architect",
        href: "/contact/sales?industry=banking",
      },
      secondaryCta: {
        label: "Read the compliance brief",
        href: "/resources/banking-compliance",
      },
    },
    proofMetrics: [
      {
        value: "47 → 1",
        outcome: "BFFs collapsed into one Fusion mesh",
        customer: "Tier-1 EU Bank",
      },
      {
        value: "100%",
        outcome: "of customer-facing graphs run inside the bank's VPC",
        customer: "Iberian Retail Bank",
      },
      {
        value: "9 mo",
        outcome: "from procurement signature to first prod release",
        customer: "UK Challenger Bank",
      },
      {
        value: "0",
        outcome: "outbound calls from the gateway, ever",
        customer: "FSI Group",
      },
    ],
    pillars: {
      headline: "What banking buyers ask for first.",
      sub: "We have shipped this stack into Tier-1 European banks. The pillars below are not aspirational; they are the four things procurement and architecture review boards always check first.",
      items: [
        {
          title: "Air-gapped Nitro Self-Hosted.",
          body: "The gateway runs entirely inside your VPC, with your database, your object store, and your observability backend. We ship the binaries; you keep the data.",
          icon: "lock",
        },
        {
          title: "Federation governance.",
          body: "Signed supergraphs, breaking-change CI gates, per-subgraph approvers. The audit story is built into the composition pipeline, not bolted on after.",
          icon: "shield",
        },
        {
          title: "Audit log via Mocha.",
          body: "Every command, every gateway hop, every agent call is a replay-safe entry on the bus. Inspectable, exportable, retention-policy-aware.",
          icon: "audit",
        },
        {
          title: "LTS support with security backports.",
          body: "Two-year LTS branches, signed releases, CVE response SLAs. The legal team can read the contract, not the source.",
          icon: "scale",
        },
      ],
    },
    diagram: "compliance",
    testimonials: [
      {
        quote:
          "Procurement signed off in nine weeks. The architecture review board, in another six. ChilliCream is the only GraphQL vendor that came in with a Helm chart, an air-gapped tarball, and a CVE-response SLA on day one.",
        author: "Group CTO",
        title: "Group CTO",
        company: "Tier-1 EU Bank",
        monogram: "EB",
      },
      {
        quote:
          "Our regulators do not care which framework we use. They care that the audit log is replay-safe and the gateway never phones home. Mocha and Nitro Self-Hosted gave us both, in one stack.",
        author: "Head of Information Security",
        title: "Head of Information Security",
        company: "Iberian Retail Bank",
        monogram: "IB",
      },
    ],
    featureCards: [
      "security",
      "observability",
      "scale",
      "performance",
      "dx",
      "openness",
    ],
    collateral: {
      title: "Get the Banking Compliance Brief",
      href: "/resources/banking-compliance",
      kind: "playbook",
    },
    logos: [
      "allianz",
      "swissgrid",
      "publicSector",
      "euTier1Bank",
      "iberianRetailBank",
      "ukChallengerBank",
      "fsiGroup",
      "globalCardNetwork",
      "dachReinsurer",
      "top3EuInsurer",
    ],
    logoCaption: "Banking-grade deployments under our care",
    finalCta: {
      headline: "Federations your regulator will not blink at.",
      sub: "Air-gapped, signed, audited, LTS-backed. Talk to a banking architect about your specific deployment topology.",
      primary: {
        label: "Talk to a banking architect",
        href: "/contact/sales?industry=banking",
      },
      secondary: {
        label: "Read the compliance brief",
        href: "/resources/banking-compliance",
      },
      tertiary: { label: "See Nitro", href: "/products/nitro" },
    },
    related: ["regulated", "federation", "agents"],
    // Banking: deep cobalt. Trust, gravitas, regulated weight.
    accent: {
      primary: "oklch(0.62 0.18 250)",
      soft: "rgba(80, 110, 220, 0.10)",
      line: "rgba(80, 110, 220, 0.34)",
      gradient:
        "linear-gradient(120deg, oklch(0.62 0.18 250), oklch(0.66 0.14 230))",
      glow: "rgba(80, 110, 220, 0.22)",
    },
    heroMotif: "hemisphere",
  },

  // ============================================================
  // 7. Regulated industries — industry, lean, no code.
  // ============================================================
  {
    slug: "regulated",
    category: "industry",
    title: "Insurance & regulated industries",
    metaDescription:
      "Insurance, healthcare, public sector. The same Fusion + Nitro stack adapted for SOC 2, ISO 27001, GDPR, and sector-specific regulators.",
    hero: {
      eyebrow: "Industries / Regulated",
      headline: "Federations that pass audit.",
      headlineAccent: "pass audit.",
      sub: "Insurance, healthcare, public sector. The same Fusion + Nitro stack adapted for SOC 2, ISO 27001, GDPR, and sector-specific regulators.",
      primaryCta: {
        label: "Talk to a compliance architect",
        href: "/contact/sales?industry=regulated",
      },
      secondaryCta: {
        label: "Read the audit brief",
        href: "/resources/regulated-audit",
      },
    },
    proofMetrics: [
      {
        value: "ISO 27001",
        outcome: "passed without a single waiver on the stack",
        customer: "Top-3 EU Insurer",
      },
      {
        value: "GDPR",
        outcome: "field-level erasure shipped in two sprints",
        customer: "DACH Reinsurer",
      },
      {
        value: "100%",
        outcome: "of patient-data hops covered by Mocha audit",
        customer: "NA Health Network",
      },
      {
        value: "0",
        outcome: "regulator findings against the gateway",
        customer: "Public-sector cloud",
      },
    ],
    pillars: {
      headline: "Compliance is a default, not a project.",
      sub: "Regulated buyers never start with features. They start with controls. The four pillars below are the controls our regulated customers ask for first; they are on by default in Fusion + Nitro.",
      items: [
        {
          title: "Compliance-first defaults.",
          body: "Persisted queries, signed supergraphs, retention policies on by default. You opt out of controls, not in to them.",
          icon: "shield",
        },
        {
          title: "Field-level RBAC + audit.",
          body: "Authorize per field, log per access, and prove who saw what. The audit log is replay-safe and exportable to your SIEM of record.",
          icon: "audit",
        },
        {
          title: "Data-residency controls.",
          body: "Region-pinned gateways, per-tenant routing, and a control plane that never moves data across borders without policy.",
          icon: "globe",
        },
        {
          title: "Procurement-ready docs.",
          body: "DPA templates, SOC 2 mappings, ISO 27001 SoA references, and pen-test summaries. Your legal and security teams have already seen them.",
          icon: "lock",
        },
      ],
    },
    diagram: "compliance",
    testimonials: [
      {
        quote:
          "We brought ChilliCream into the room with our auditor on the second call. The gateway architecture answered fourteen of the seventeen control objectives without us writing a slide.",
        author: "VP Information Risk",
        title: "VP Information Risk",
        company: "Top-3 EU Insurer",
        monogram: "EI",
      },
      {
        quote:
          "Patient-data routing, field-level RBAC, regional pinning. We do not buy infrastructure that does not handle all three. Nitro Self-Hosted does.",
        author: "Head of Platform",
        title: "Head of Platform",
        company: "NA Health Network",
        monogram: "NH",
      },
    ],
    featureCards: [
      "security",
      "observability",
      "scale",
      "openness",
      "dx",
      "performance",
    ],
    collateral: {
      title: "Get the Regulated-Industries Audit Brief",
      href: "/resources/regulated-audit",
      kind: "playbook",
    },
    logos: [
      "allianz",
      "swissgrid",
      "publicSector",
      "top3EuInsurer",
      "naHealthNetwork",
      "dachReinsurer",
      "euTier1Bank",
      "iberianRetailBank",
      "fsiGroup",
      "globalCardNetwork",
    ],
    logoCaption: "Regulated platforms running ChilliCream in production",
    finalCta: {
      headline: "An API platform your auditor will sign off on.",
      sub: "We have shipped this stack into insurance, healthcare, and public sector. The controls are not bolt-ons; they are the runtime.",
      primary: {
        label: "Talk to a compliance architect",
        href: "/contact/sales?industry=regulated",
      },
      secondary: {
        label: "Read the audit brief",
        href: "/resources/regulated-audit",
      },
      tertiary: { label: "See Nitro", href: "/products/nitro" },
    },
    related: ["banking", "federation", "single-graph"],
    // Regulated: muted teal-slate. Audit, controls, compliance grid.
    accent: {
      primary: "oklch(0.66 0.10 200)",
      soft: "rgba(120, 170, 200, 0.10)",
      line: "rgba(120, 170, 200, 0.32)",
      gradient:
        "linear-gradient(120deg, oklch(0.68 0.10 200), oklch(0.66 0.12 220))",
      glow: "rgba(120, 170, 200, 0.20)",
    },
    heroMotif: "hemisphere",
  },
];

export const findSolution = (slug: string): SolutionRecord | undefined =>
  SOLUTIONS.find((s) => s.slug === slug);

export const findRelatedSolutions = (
  record: SolutionRecord
): readonly SolutionRecord[] => {
  const lookup = new Map(SOLUTIONS.map((s) => [s.slug, s] as const));
  return record.related
    .map((slug) => lookup.get(slug))
    .filter((s): s is SolutionRecord => s !== undefined);
};
