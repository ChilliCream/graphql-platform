---
title: "Migrating from Schema Stitching"
---

# Migrating from Schema Stitching to Fusion

If your team uses HotChocolate Schema Stitching to combine multiple GraphQL services into a single API, this guide walks you through migrating to HotChocolate Fusion. Fusion is the evolution of the same idea -- distributed GraphQL under a single endpoint -- but with a fundamentally different architecture that catches schema conflicts at build time, eliminates manual resolver delegation, and aligns with the open [GraphQL Composite Schemas specification](https://graphql.github.io/composite-schemas-spec/).

This guide is self-contained. You can complete the migration by following the steps here. Links to other Fusion documentation pages are provided for deeper dives, but they are not prerequisites.

## How the Concepts Map

If you have worked with Schema Stitching, you already understand the core idea: multiple GraphQL services combined into one schema. Fusion keeps that idea but changes how it works under the hood. Here is how the concepts translate:

| Schema Stitching                                               | Fusion                                             | What Changed                                                                                                                               |
| -------------------------------------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Remote schemas (`AddRemoteSchema()`)                           | Subgraphs (source schemas)                         | Each remote schema becomes its own standalone ASP.NET Core project with HotChocolate. No stitching middleware needed.                      |
| Stitching gateway (`AddGraphQLServer()` + `AddRemoteSchema()`) | Fusion gateway (`AddGraphQLGateway()`)             | The gateway is stateless. No custom resolvers, no delegation logic, no type extensions in the gateway project.                             |
| Schema extensions (`.graphql` extension files)                 | Entity stubs with `[ObjectType<T>]`                | Instead of writing SDL extension files with `@delegate` directives, you define C# types in the subgraph that extends the entity.           |
| Delegating resolvers (`@delegate` directive)                   | Lookups (`[Lookup]` attribute)                     | The gateway handles cross-subgraph resolution automatically. You declare a lookup field in each subgraph; composition wires them together. |
| `@delegate(path: "...")` field references                      | `[Require]` attribute                              | When a field needs data from another subgraph, you declare the dependency as a method parameter with `[Require]`.                          |
| Auto-stitching / runtime schema merging                        | Build-time composition (`nitro fusion compose`)    | Schemas are merged offline by the Nitro CLI, producing a static configuration file. Conflicts are caught before deployment.                |
| `PublishSchemaDefinition()` + Redis                            | `schema export` + `nitro fusion upload`            | Schema distribution uses the Nitro CLI or .NET Aspire instead of Redis pub/sub.                                                            |
| `RenameType()` / `RenameField()` / `IgnoreType()`              | Composition rules + `[Internal]` / `@inaccessible` | Type conflicts are resolved by composition rules. Fields you want to hide use `[Internal]` on lookups or `@inaccessible` on types.         |
| `SchemaDefinition` / `SchemaExtension`                         | `schema.graphqls` + `schema-settings.json`         | Exported automatically by the subgraph on startup. You do not write these by hand.                                                         |

## What Changes Architecturally

The migration is not just swapping one API for another. Fusion changes the architecture in ways that simplify your system but require rethinking where code lives.

### The Gateway Becomes Stateless

In Schema Stitching, the gateway is where you configure everything: register remote schemas, define type extensions, write delegating resolvers, rename types to avoid conflicts, and set up context data propagation. The gateway is the brain of the system.

In Fusion, the gateway has **no custom code**. It loads a pre-composed configuration file (a `.far` archive) and uses it to route queries to subgraphs. All business logic, type definitions, and resolver code live in the subgraphs. If you have custom delegating resolvers or type extensions in your stitching gateway, you will need to move that logic into the appropriate subgraph.

### All Business Logic Moves to Subgraphs

In stitching, it was common to define type extensions and delegating resolvers in the gateway using `.graphql` extension files:

```graphql
# Stitching.graphql (in the gateway project)
extend type Product {
  inStock: Boolean
    @delegate(
      schema: "inventory"
      path: "inventoryInfo(upc: $fields:upc).isInStock"
    )
  shippingEstimate: Int
    @delegate(
      schema: "inventory"
      path: "shippingEstimate(price: $fields:price, weight: $fields:weight)"
    )
}
```

In Fusion, this logic moves to the subgraph that owns the extended field. The Inventory subgraph itself declares how it extends the `Product` type using C# code. The gateway never sees or manages these relationships.

### Composition Replaces Runtime Schema Merging

Schema Stitching merges schemas at runtime when the gateway starts. If two remote schemas define conflicting types, you discover the problem when the gateway fails to start.

Fusion merges schemas at build time using the Nitro CLI (`nitro fusion compose`). You run composition as part of your build or CI pipeline. If schemas conflict, composition fails with a clear error message -- before you deploy anything. This means you catch issues like missing `[Shareable]` annotations, incompatible field types, or enum value mismatches during development, not at 3 AM in production.

### Transport: HTTP Between Gateway and Subgraphs

In some stitching setups, remote schemas could run in-process or communicate over custom protocols. In Fusion, the gateway communicates with subgraphs over HTTP. Each subgraph is a standalone ASP.NET Core application with its own HTTP endpoint. The gateway sends GraphQL requests to each subgraph's `/graphql` endpoint and aggregates the responses.

## Step-by-Step Migration

This section walks through converting a typical Schema Stitching setup to Fusion. The examples use a common pattern: a Products service and an Inventory service, where the gateway extends the `Product` type with inventory data.

### Step 1: Convert Remote Schemas to Standalone Subgraphs

In stitching, your remote schemas are HotChocolate servers that may or may not publish their schema definitions. In Fusion, each remote schema becomes a standalone subgraph -- a regular HotChocolate server with a few additional attributes.

**Before (Stitching remote schema):**

```csharp
// Products service - Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<ProductQueries>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

```csharp
// Products service - ProductQueries.cs
public class ProductQueries
{
    public Product GetProductByUpc(int upc)
        => ProductRepository.GetByUpc(upc);

    public IEnumerable<Product> GetProducts()
        => ProductRepository.GetAll();
}
```

**After (Fusion subgraph):**

```csharp
// Products subgraph - Program.cs
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("products-api")
    .AddProductTypes()
    .AddGlobalObjectIdentification()
    .AddMutationConventions()
    .ExportSchemaOnStartup();

var app = builder.Build();
app.MapGraphQL();
app.RunWithGraphQLCommands(args);
```

```csharp
// Products subgraph - Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static async Task<Product?> GetProductByUpc(
        int upc,
        IProductByUpcDataLoader productByUpc,
        CancellationToken cancellationToken)
        => await productByUpc.LoadAsync(upc, cancellationToken);

    [UsePaging]
    public static async Task<Connection<Product>> GetProducts(
        PagingArguments arguments,
        ProductContext context,
        CancellationToken cancellationToken)
        => await context.Products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Upc)
            .ToPageAsync(arguments, cancellationToken)
            .ToConnectionAsync();
}
```

Key changes:

- **`builder.AddGraphQL("products-api")`** replaces `builder.Services.AddGraphQLServer()`. The string argument is the subgraph name used during composition.
- **`.AddProductTypes()`** is generated by the `HotChocolate.Types.Analyzers` package. It registers all types marked with `[QueryType]`, `[ObjectType<T>]`, etc.
- **`.ExportSchemaOnStartup()`** exports the subgraph's schema as a `.graphqls` file when the server starts (used for composition).
- **`app.RunWithGraphQLCommands(args)`** enables CLI commands like `dotnet run -- schema export`.
- **`[Lookup]`** on `GetProductByUpc` tells Fusion that this field can be used to resolve `Product` entities from other subgraphs. This is the Fusion equivalent of what stitching resolvers delegated to.
- **`[QueryType]`** and `static partial class` use HotChocolate's annotation-based type system.

You also need a `schema-settings.json` file in the subgraph project root:

```json
{
  "name": "products-api",
  "transports": {
    "http": {
      "clientName": "fusion",
      "url": "{{API_URL}}"
    }
  },
  "environments": {
    "development": {
      "API_URL": "http://localhost:5100/graphql"
    },
    "production": {
      "API_URL": "https://products.example.com/graphql"
    }
  }
}
```

This file tells the composition engine the subgraph's name and how the gateway should reach it. The `{{API_URL}}` placeholder is resolved from the active environment.

Add these packages to the subgraph `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="HotChocolate.AspNetCore" />
    <PackageReference Include="HotChocolate.AspNetCore.CommandLine" />
    <PackageReference Include="HotChocolate.Types.Analyzers">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
    </PackageReference>
</ItemGroup>
```

Repeat this for every remote schema in your stitching setup. Each one becomes its own ASP.NET Core project.

### Step 2: Replace Schema Extensions with Entity Stubs

In stitching, when you want to add fields from one service to a type owned by another service, you write SDL extension files with `@delegate` directives. In Fusion, you create **entity stubs** in the subgraph that contributes the additional fields.

**Before (Stitching -- SDL extension file in the gateway or domain service):**

```graphql
# Stitching.graphql
extend type Product {
  inStock: Boolean
    @delegate(
      schema: "inventory"
      path: "inventoryInfo(upc: $fields:upc).isInStock"
    )
  shippingEstimate: Int
    @delegate(
      schema: "inventory"
      path: "shippingEstimate(price: $fields:price, weight: $fields:weight)"
    )
}
```

**After (Fusion -- C# code in the Inventory subgraph):**

First, create an entity stub for `Product` in the Inventory subgraph. This is a lightweight C# type that declares "I know `Product` exists, identified by `upc`, and I want to add fields to it":

```csharp
// Inventory subgraph - Types/Product.cs
[EntityKey("upc")]
public sealed record Product(int Upc)
{
    public bool GetInStock(
        [Parent(requires: nameof(Upc))] Product product,
        InventoryRepository repository)
        => repository.IsInStock(product.Upc);

    public int GetShippingEstimate(
        [Require("""
            {
              weight,
              price
            }
            """)]
        ShippingInput input)
        => ShippingCalculator.Estimate(input.Weight, input.Price);
}
```

```csharp
// Inventory subgraph - Types/ShippingInput.cs
public sealed class ShippingInput
{
    public int Weight { get; init; }
    public double Price { get; init; }
}
```

Then add an internal lookup so the gateway can resolve `Product` references within this subgraph:

```csharp
// Inventory subgraph - Types/InventoryQueries.cs
[QueryType]
public static partial class InventoryQueries
{
    [Lookup, Internal]
    public static Product GetProductByUpc(int upc)
        => new(upc);
}
```

Key concepts:

- **Entity stub**: `record Product(int Upc)` is not a copy of the Products subgraph's `Product` class. It is a minimal declaration that tells Fusion: "I know `Product` exists, it has a `upc` key, and I want to contribute fields to it." The gateway merges these fields with the full `Product` from the Products subgraph.
- **`[EntityKey("upc")]`**: Declares which field identifies this entity. This replaces the `$fields:upc` reference in stitching's `@delegate` directive.
- **`[Require]`**: Declares that `GetShippingEstimate` needs `weight` and `price` from the composed `Product` type. The gateway fetches these fields from the Products subgraph before calling this resolver. This replaces `$fields:price` and `$fields:weight` from the `@delegate` path. The `[Require]` attribute uses a GraphQL-like selection syntax to map fields from the composed type into a C# input object.
- **`[Lookup, Internal]`**: The internal lookup lets the gateway resolve `Product` references within the Inventory subgraph. `[Internal]` means this lookup is not exposed to clients -- it is only used internally by the gateway during query planning. This replaces the implicit entity resolution that stitching handled through `@delegate`.

### Step 3: Replace the Stitching Gateway with a Fusion Gateway

The Fusion gateway is dramatically simpler than a stitching gateway because it has no custom resolver logic.

**Before (Stitching gateway):**

```csharp
// Gateway - Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient("products", c =>
        c.BaseAddress = new Uri("http://localhost:5100/graphql"));
builder.Services
    .AddHttpClient("inventory", c =>
        c.BaseAddress = new Uri("http://localhost:5200/graphql"));

builder.Services
    .AddGraphQLServer()
    .AddRemoteSchema("products")
    .AddRemoteSchema("inventory")
    .AddTypeExtensionsFromFile("./Stitching.graphql");

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

**After (Fusion gateway):**

```csharp
// Gateway - Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();

builder.Services.AddHeaderPropagation(c =>
{
    c.Headers.Add("Authorization");
});

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();
app.UseHeaderPropagation();
app.MapGraphQL();
app.Run();
```

Key changes:

- **`AddGraphQLGateway()`** replaces `AddGraphQLServer()` + `AddRemoteSchema()`. The gateway does not know about individual subgraphs -- it reads the composed configuration.
- **`.AddFileSystemConfiguration("./gateway.far")`** loads the composed Fusion archive. This file is produced by `nitro fusion compose` (see Step 4). Alternatively, use `.AddNitro()` to download the configuration from ChilliCream's cloud platform.
- **No type extensions, no `@delegate`, no remote schema registration.** The gateway is pure routing infrastructure.
- **Header propagation** is configured through standard ASP.NET Core middleware. The named HTTP client `"fusion"` is what the gateway uses to call subgraphs. You add `AddHeaderPropagation()` on it to forward headers like `Authorization` to subgraphs.

Add these packages to the gateway `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="HotChocolate.Fusion.AspNetCore" />
    <PackageReference Include="Microsoft.AspNetCore.HeaderPropagation" />
</ItemGroup>
```

Notice what is gone: no `HotChocolate.Stitching` package, no per-service HTTP clients with hardcoded URLs, no extension files. The gateway project is minimal.

### Step 4: Compose Your Schemas

In stitching, schema merging happens at runtime when the gateway starts. In Fusion, you compose schemas offline using the Nitro CLI.

First, export each subgraph's schema:

```bash
# In the Products subgraph project directory
dotnet run -- schema export

# In the Inventory subgraph project directory
dotnet run -- schema export
```

Each command produces a `schema.graphqls` file alongside the `schema-settings.json` in the subgraph project.

Then compose all subgraphs into a Fusion archive:

```bash
nitro fusion compose \
  --source-schema-file ./src/Products/schema.graphqls \
  --source-schema-file ./src/Inventory/schema.graphqls \
  --archive ./src/Gateway/gateway.far \
  --environment development
```

If composition succeeds, you get a `gateway.far` file that the gateway loads at startup. If it fails, you get error messages telling you exactly which types or fields conflict and how to fix them.

During development, you can use watch mode to recompose automatically when schema files change:

```bash
nitro fusion compose \
  --source-schema-file ./src/Products/schema.graphqls \
  --source-schema-file ./src/Inventory/schema.graphqls \
  --archive ./src/Gateway/gateway.far \
  --environment development \
  --watch
```

Install the Nitro CLI if you have not already:

```bash
dotnet tool install -g ChilliCream.Nitro.CLI
```

### Step 5: Move Gateway-Side Logic to Subgraphs

This is the step that requires the most thought. In stitching, it was common to put logic in the gateway:

- **Type renames** (`RenameType()`, `RenameField()`): In Fusion, naming conflicts are resolved by composition rules. If two subgraphs define the same type and their definitions are compatible, composition merges them automatically. If they conflict, composition fails with an error. To resolve conflicts, you adjust the subgraph schemas rather than renaming at the gateway level.

- **Field ignoring** (`IgnoreField()`, `IgnoreType()`): In Fusion, use `[Internal]` on lookups to hide them from the composed schema, or `@inaccessible` on types/fields you want to exclude from the composite schema.

- **Context data propagation** (`$contextData`, `$scopedContextData`): In Fusion, cross-subgraph data dependencies are declared with `[Require]`. The gateway resolves the required fields automatically -- you do not pass context data manually.

- **Custom middleware** (`UseField`, `UseRequest`): Any per-field middleware or request interceptors that lived in the stitching gateway must move to the appropriate subgraph. Each subgraph is a full HotChocolate server and supports the same middleware pipeline.

- **Redis-based schema publishing** (`PublishToRedis()`): Replace with the Nitro CLI workflow. Subgraphs export their schemas on startup (via `ExportSchemaOnStartup()`), and the Nitro CLI handles composition and distribution.

### Step 6: Verify and Test

Once all subgraphs are converted and the gateway is set up:

1. **Start all subgraphs** -- each runs as its own ASP.NET Core application.
2. **Export schemas** from each subgraph (`dotnet run -- schema export`).
3. **Run composition** (`nitro fusion compose`) and fix any errors.
4. **Start the gateway** with the composed `.far` file.
5. **Run your existing queries** against the gateway endpoint and verify the results match what the stitching gateway returned.

Pay special attention to:

- Fields that were added via `@delegate` -- verify they resolve correctly through the new entity stubs and lookups.
- Queries that relied on type renames or field renames -- the composed schema may have different names.
- Context data that was propagated through `$contextData` -- verify that `[Require]` provides the equivalent data to resolvers.

## What Changes at Runtime

Beyond the code changes, Fusion behaves differently at runtime compared to Schema Stitching.

### Query Planning vs. Delegation

In stitching, the gateway delegates individual field resolutions to remote schemas using the `@delegate` directive. Each delegated field is a separate remote call, and the stitching engine handles the orchestration.

In Fusion, the gateway creates a **query plan** at request time. It analyzes the full query, determines which subgraphs own which fields, groups fetches to minimize round trips, and executes them in an optimized order. This means Fusion can batch entity lookups (fetching multiple products in a single call) and parallelize independent fetches -- something that stitching's per-field delegation could not do efficiently.

### Error Handling

In stitching, errors from remote schemas are propagated through the delegation chain. The error format and propagation behavior depended on the delegation configuration.

In Fusion, when a subgraph returns an error, the gateway includes it in the response's `errors` array with path information pointing to the field that failed. If a subgraph is unreachable, the gateway returns `null` for fields from that subgraph (if the field is nullable) or propagates the error upward. You can configure HTTP resilience on the gateway's named HTTP client using `Microsoft.Extensions.Http.Resilience`:

```csharp
builder.Services
    .AddHttpClient("fusion")
    .AddStandardResilienceHandler();
```

### Transport

Stitching communicated with remote schemas via HTTP, but some setups used in-process schema registration or Redis for schema discovery. Fusion uses HTTP for all gateway-to-subgraph communication. Each subgraph must be reachable at the URL specified in its `schema-settings.json`. The gateway uses a single named HTTP client (`"fusion"` by convention) for all subgraph calls.

## What Gets Simpler

After migrating, several things that required manual work in stitching become automatic:

- **No manual schema delegation.** You do not write `@delegate` directives or manage delegation paths. Composition figures out how to resolve cross-subgraph fields from the lookups and entity stubs you declare.

- **Entity resolution is a simple lookup field.** Instead of constructing complex `@delegate(path: "...")` expressions with `$fields` references, you write a C# method with `[Lookup]` that takes an ID and returns an entity. The gateway calls it when needed.

- **Composition catches conflicts at build time.** In stitching, type conflicts were discovered when the gateway started (or worse, when a specific query triggered the conflict at runtime). Fusion's offline composition validates everything upfront.

- **Independent subgraph deployment.** Each subgraph is a standalone ASP.NET Core application. You can deploy, scale, and update them independently. Adding a new subgraph does not require changes to existing subgraphs or the gateway code.

- **No gateway code changes when adding subgraphs.** In stitching, adding a remote schema meant updating the gateway's `Program.cs` (adding `AddRemoteSchema()`, HTTP clients, and extension files). In Fusion, you add the new subgraph's schema to the composition step and redeploy the gateway with the updated `.far` file. No gateway code changes.

- **No Redis dependency for schema distribution.** If you used `PublishToRedis()` for schema federation, Fusion replaces that with the Nitro CLI or .NET Aspire orchestration.

## Common Pitfalls

### Forgetting That the Gateway is Stateless

The most common mistake is trying to put logic in the Fusion gateway. If you have type extensions, custom resolvers, middleware, or `UseField` hooks in your stitching gateway, all of that must move to a subgraph. The Fusion gateway project should contain only the gateway setup (`AddGraphQLGateway()`), HTTP client configuration, and middleware pipeline (auth, CORS, header propagation).

### Confusing Entity Stubs with Data Duplication

When you create `record Product(int Upc)` in the Inventory subgraph, it looks like you are duplicating the `Product` type. You are not. This entity stub is a declaration: "I know `Product` exists, it has a `upc` key, and I want to add fields to it." The stub does not contain the full `Product` definition -- the Products subgraph owns that. Composition merges the stub's contributed fields with the full type.

### Missing Internal Lookups

If a subgraph extends an entity from another subgraph (like the Inventory subgraph extending `Product`), it needs an internal lookup so the gateway can resolve `Product` references within that subgraph:

```csharp
[QueryType]
public static partial class InventoryQueries
{
    [Lookup, Internal]
    public static Product GetProductByUpc(int upc)
        => new(upc);
}
```

Without this lookup, the gateway cannot route queries that involve the Inventory subgraph's contributed fields on `Product`. Composition will report an error if an entity is used in a subgraph without a corresponding lookup.

### schema-settings.json Misconfiguration

Every subgraph needs a `schema-settings.json` file with the correct:

- **`name`**: Must be unique across all subgraphs. This is the identifier used during composition.
- **`transports.http.url`**: Must point to the subgraph's actual GraphQL endpoint. Use `{{API_URL}}` with environment-specific values.
- **`transports.http.clientName`**: Must match the named HTTP client registered in the gateway (typically `"fusion"`).

If the URL is wrong, the gateway will fail to reach the subgraph at runtime. If the name is wrong or duplicated, composition will produce incorrect results.

### Fields That Need `[Shareable]`

In stitching, if two remote schemas defined the same field, the gateway's auto-resolution would prefix one with the schema name. In Fusion, if two subgraphs define the same non-key field on the same type, composition fails unless the field is marked `[Shareable]` in both subgraphs.

For example, if both the Accounts subgraph and the Reviews subgraph define `User.name`:

```csharp
// In BOTH subgraphs:
[ObjectType<User>]
public static partial class UserNode
{
    [Shareable]
    public static string GetName([Parent] User user)
        => user.Name!;
}
```

`[Shareable]` tells Fusion: "this field is intentionally defined in multiple subgraphs and they all return the same value." The gateway can resolve it from whichever subgraph is most convenient for a given query. For more on field ownership rules, see [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).

## Migrating CI/CD

If your stitching setup uses Redis-based schema federation with CI/CD automation, the pipeline changes significantly.

**Before (Stitching with Redis federation):**

1. Build and deploy each domain service.
2. Each service publishes its schema to Redis on startup via `PublishToRedis()`.
3. The gateway subscribes to Redis and picks up schema changes at runtime.

**After (Fusion with Nitro CLI):**

1. Build each subgraph and export its schema: `dotnet run -- schema export`.
2. Upload the schema to ChilliCream Nitro: `nitro fusion upload --source-schema-file schema.graphqls --tag v1.0.0 --api-id <id> --api-key <key>`.
3. Deploy the subgraph container.
4. Publish the composed configuration: `nitro fusion publish --source-schema products-api --tag v1.0.0 --stage production --api-id <id> --api-key <key>`.
5. The gateway downloads the updated configuration from Nitro automatically (via `.AddNitro()` in the gateway setup).

Alternatively, for a simpler setup without Nitro cloud:

1. Export schemas from all subgraphs.
2. Run `nitro fusion compose` locally or in CI to produce a `.far` file.
3. Deploy the `.far` file alongside the gateway.
4. The gateway loads the `.far` file on startup via `.AddFileSystemConfiguration("./gateway.far")`.

For a full CI/CD pipeline reference, see [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd).

## Summary

Migrating from Schema Stitching to Fusion is a structural change, not just an API swap. The key shifts are:

1. **Gateway becomes stateless** -- move all type extensions, delegating resolvers, and custom middleware into subgraphs.
2. **Entity stubs replace schema extensions** -- instead of SDL files with `@delegate`, write C# types with `[Lookup]` and `[Require]` in the subgraph.
3. **Build-time composition replaces runtime merging** -- use `nitro fusion compose` to validate and merge schemas before deployment.
4. **Each subgraph is a standalone server** -- no stitching middleware, no Redis dependencies, just a HotChocolate server with a few extra attributes.

The effort involved depends on the complexity of your stitching setup. If your gateway is mostly auto-stitching with minimal custom delegation, the migration is straightforward. If your gateway has extensive custom resolvers, `$contextData` propagation, and type manipulation, expect to spend more time restructuring that logic into subgraphs. The result is a cleaner architecture where each subgraph owns its complete behavior and the gateway is pure infrastructure.
