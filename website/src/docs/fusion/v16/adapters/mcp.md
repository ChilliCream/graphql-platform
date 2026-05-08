---
title: "MCP Adapter"
---

The [Model Context Protocol (MCP)](https://modelcontextprotocol.io) is an open standard that lets AI assistants connect to external systems through a uniform tool and prompt interface. The `HotChocolate.Fusion.Adapters.Mcp` package turns a Fusion gateway into an MCP server. You author tools based on GraphQL operation documents, and prompts with a JSON configuration file, publish them, and the gateway handles loading, execution, transport, and live updates.

You wire MCP onto an existing Fusion gateway with two calls: `AddMcp()` during service registration and `MapGraphQLMcp()` during endpoint mapping. The adapter exposes the MCP server over Streamable HTTP at `/graphql/mcp` by default, so any MCP client (Claude Desktop, an editor extension, an agent runtime) can connect directly to the gateway.

This page covers wiring and configuration. For authoring tools and prompts, see the [Nitro MCP](/docs/nitro/adapters/mcp) section.

## Prerequisites

You need an existing Fusion gateway. If you do not have one yet, follow the [Getting Started](/docs/fusion/v16/getting-started) tutorial first.

Add the adapter package to the gateway project:

```bash
dotnet add package HotChocolate.Fusion.Adapters.Mcp
```

## Enabling MCP on the Gateway

Two adapter calls turn a Fusion gateway into an MCP server. `AddMcp()` registers the MCP server, schema services, and a startup warmup that loads tool and prompt definitions from storage. `MapGraphQLMcp()` exposes the MCP transport endpoints.

```csharp
builder
    .AddGraphQLGateway()
    .AddMcp();
    // Storage is required: see "Connecting a Tool and Prompt Source" below.

// ...

app.MapGraphQLMcp();
```

The gateway needs a tool and prompt source. Without one, `MapGraphQLMcp()` throws `InvalidOperationException` during startup. Wire up storage by either [using Nitro](#using-nitro-for-tools-and-prompts) or providing a custom `IMcpStorage`.

## Connecting a Tool and Prompt Source

The adapter does not ship tools or prompts of its own. It asks an `IMcpStorage` implementation for them at startup, and listens for change notifications afterwards. You have two options:

1. **Use Nitro** (recommended for production). Nitro publishes versioned MCP feature collections to the gateway and supplies an `IMcpStorage` automatically. Skip ahead to [Using Nitro](#using-nitro-for-tools-and-prompts).
2. **Provide your own `IMcpStorage`** for self-hosted scenarios where you manage tool definitions outside Nitro.

To register a custom storage, implement `IMcpStorage` and pass it to `AddMcpStorage()`:

```csharp
builder
    .AddGraphQLGateway()
    .AddMcp()
    .AddMcpStorage<MyMcpStorage>();
```

Three overloads cover the common registration patterns:

| Overload                                             | Use when                                                                  |
| ---------------------------------------------------- | ------------------------------------------------------------------------- |
| `AddMcpStorage(IMcpStorage instance)`                | You already have a singleton instance.                                    |
| `AddMcpStorage<T>()`                                 | You want DI to construct the storage. `T` is activated from app services. |
| `AddMcpStorage(Func<IServiceProvider, IMcpStorage>)` | You need a factory for custom construction or scoping.                    |

`IMcpStorage` returns `OperationToolDefinition` and `PromptDefinition` collections, and exposes `IObservable` streams so the gateway can apply update and remove events without restarting. Reach for this extension point only when you cannot use Nitro, since implementing it correctly involves change diffing, caching, and reactive subscriptions.

## Configuring the MCP Server

`AddMcp()` accepts two configuration delegates. The first targets `McpServerOptions` (server behavior), the second targets `IMcpServerBuilder` (tool, prompt, and resource registration from the underlying MCP SDK):

```csharp
builder
    .AddGraphQLGateway()
    .AddMcp(
        configureServerOptions: options =>
        {
            options.InitializationTimeout = TimeSpan.FromSeconds(30);
        },
        configureServer: server =>
        {
            // Register additional MCP server features here.
        });
```

## Mapping the MCP Endpoint

`MapGraphQLMcp()` accepts two optional arguments:

```csharp
app.MapGraphQLMcp(pattern: "/graphql/mcp", schemaName: null);
```

- **`pattern`**: the URL prefix for the MCP transport. Defaults to `/graphql/mcp`. Change it when the default conflicts with another route or when you expose multiple gateways from the same host.
- **`schemaName`**: the named Fusion schema to expose. The adapter resolves this automatically when the gateway has a single schema. Pass it explicitly when the gateway hosts multiple schemas, so each schema gets its own MCP endpoint:

```csharp
app.MapGraphQLMcp("/graphql/public/mcp", schemaName: "Public");
app.MapGraphQLMcp("/graphql/internal/mcp", schemaName: "Internal");
```

The endpoint speaks Streamable HTTP. POST carries JSON-RPC requests and returns either `text/event-stream` for streaming responses or `202 Accepted` for queued ones.

## Using Nitro for Tools and Prompts

Nitro is the easiest way to manage MCP tools and prompts. You author tools and prompts on disk, upload them as a tagged version of a feature collection with the Nitro CLI, and publish that version to a stage. The gateway loads the collection from the configured stage and picks up new versions automatically. When Nitro is wired up alongside `AddMcp()`, it registers an `IMcpStorage` for you, so you do not call `AddMcpStorage()` yourself.

### Install the Nitro packages

```bash
dotnet add package ChilliCream.Nitro
dotnet add package ChilliCream.Nitro.Fusion
```

`ChilliCream.Nitro` is the core package and includes a source generator that emits an `AddDefaults()` extension method based on which integration packages are referenced in the project. With `ChilliCream.Nitro.Fusion` referenced, `AddDefaults()` calls `AddFusion()` for you.

### Wire Nitro into the gateway

Call `AddNitro().AddDefaults()` (or the explicit `AddNitro().AddFusion()`) before configuring the gateway. `AddNitro()` configures the shared connection options (`ApiId`, `ApiKey`, `Stage`) on `NitroServiceOptions`. `ModifyNitroOptions()` on the gateway builder configures gateway-specific options (MCP, OpenAPI, persisted operations, metrics, and so on):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddNitro(o =>
    {
        o.ApiId = builder.Configuration["Nitro:ApiId"]!;
        o.ApiKey = builder.Configuration["Nitro:ApiKey"]!;
        o.Stage = builder.Configuration["Nitro:Stage"]!;
    })
    .AddDefaults();

builder
    .AddGraphQLGateway()
    .ModifyNitroOptions(o =>
    {
        // Modify Nitro options here.
    })
    .AddMcp();

var app = builder.Build();

app.MapGraphQL();
app.MapGraphQLMcp();

app.Run();
```

If you prefer environment variables over inline configuration, set `NITRO_API_ID`, `NITRO_STAGE`, and `NITRO_API_KEY`. The Nitro service options bind to these automatically and you can drop the `AddNitro` configuration delegate entirely.

> Order matters: `AddNitro().AddDefaults()` (or `AddFusion()`) must run before the gateway builder calls so that the Fusion gateway pipeline picks up the Nitro contributions during registration.

### What you get

With Nitro and MCP both enabled:

- The gateway loads the published MCP feature collection for the configured stage on startup.
- Tool and prompt definitions are cached locally so cold starts work without a round trip to Nitro.
- Stage change events flow over the Nitro change feed. When you publish a new version, the gateway updates its tool and prompt set in place, no restart required.
- If Nitro configuration is incomplete (any of `ApiId`, `ApiKey`, or `Stage` not set), MCP integration is disabled with a warning, the storage returns no definitions, and the gateway continues to start.
- If configuration is set but the API key is rejected, the storage uses the local cache when one is available and logs the sync failure. Without a usable cache, the exception propagates and host startup fails.

For authoring tools and prompts, publishing feature collection versions, and managing stages, see the [Nitro MCP](/docs/nitro/adapters/mcp) section.

## Troubleshooting

### `InvalidOperationException: Call AddMcp() when configuring the GraphQL server.`

`MapGraphQLMcp()` was called but `AddMcp()` was not registered on the gateway builder. Add `.AddMcp()` to the chain that starts with `AddGraphQLGateway()`.

### MCP endpoint returns 404 Not Found

The route pattern does not match what your client uses. The default is `/graphql/mcp`. If you passed a custom pattern to `MapGraphQLMcp()`, point your client at it.

### `InvalidOperationException: No IMcpStorage is registered for schema '<name>'.`

Two possible causes:

- **No storage source for that schema.** `AddMcp()` was registered, but no `IMcpStorage` is wired up. Either reference `ChilliCream.Nitro.Fusion` and call `AddNitro().AddDefaults()` (or `AddNitro().AddFusion()`), or register a custom storage with `AddMcpStorage(...)`.
- **Endpoint mapped to the wrong schema.** `MapGraphQLMcp(pattern, schemaName)` was called with a `schemaName` that does not match any gateway registered through `AddGraphQLGateway(name)` plus `AddMcp()`. With multiple named gateways, pass the matching name. With a single unnamed gateway, omit `schemaName` and the adapter resolves it automatically.

### Tools and prompts list is empty

Storage is registered but returned no definitions. With Nitro, ensure a published MCP feature collection version exists for the configured stage and that the API key has read access to it. With a custom `IMcpStorage`, verify that the implementation completes its initial fetch before the warmup task times out and that it returns the expected definitions.

### Nitro logs `MCP integration is disabled because Nitro is not properly configured.`

`NitroServiceOptions` is missing one or more of `ApiId`, `ApiKey`, or `Stage`. Set them through the `AddNitro()` configuration delegate or via the `NITRO_API_ID`, `NITRO_API_KEY`, and `NITRO_STAGE` environment variables.

## Next Steps

- Author tools and prompts and publish them to a stage in the [Nitro MCP](/docs/nitro/adapters/mcp) section.
- Deploy your gateway across stages with [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd).
