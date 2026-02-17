---
title: "Coming from Apollo Federation"
---

# Coming from Apollo Federation

If you have experience with Apollo Federation, you already understand the core idea behind distributed GraphQL: multiple services contribute types and fields to a single, unified schema, and a gateway coordinates query execution across them. HotChocolate Fusion solves the same problem with a different approach -- one grounded in the open [GraphQL Composite Schemas specification](https://graphql.github.io/composite-schemas-spec/) rather than a proprietary federation spec.

This guide maps Apollo Federation concepts to their Fusion equivalents, explains behavioral differences, and walks you through migrating subgraphs, the gateway, and your CI/CD pipeline. It is self-contained: you can complete a migration by following this guide alone. Links to other Fusion docs pages are provided for deeper dives, not as prerequisites.

## Concept Mapping

The table below maps Apollo Federation concepts to their Fusion equivalents. Some are straightforward renames; others involve meaningful behavioral changes. The "Key Difference" column flags which is which.

| Apollo Federation | HotChocolate Fusion | Key Difference |
|---|---|---|
| `@key(fields: "id")` | `[Lookup]` on a Query field | Fusion uses explicit, typed lookup fields instead of implicit `_entities`. No `@key` directive needed. |
| `__resolveReference` / `_entities` query | Regular Query fields with `[Lookup]` | Lookups are real fields you can call and test directly. |
| `@external` | `[External]` (rarely needed) | Same concept, but less frequently needed in Fusion. |
| `@requires(fields: "...")` on a field | `[Require("...")]` on an argument | Argument-level, not field-level. Required arguments are hidden from the composite schema. |
| `@provides(fields: "...")` | `[Provides("...")]` / `[Parent(requires: "...")]` | Same optimization concept. |
| `@shareable` | `[Shareable]` | Same concept and semantics. Key fields are automatically shareable. |
| `@override(from: "...")` | `[Override(from: "...")]` | Same concept. |
| `@inaccessible` | `[Inaccessible]` | Same concept. |
| `@tag` | `[Tag]` | Same concept. |
| Apollo Router / Gateway | Fusion Gateway (`AddGraphQLGateway()`) | A .NET ASP.NET Core app, not a separate binary. |
| `rover` CLI | Nitro CLI (`nitro fusion ...`) | Schema composition, validation, and delivery. |
| GraphOS managed federation | Nitro cloud or local CI/CD composition | Build-time composition. Works fully offline. |
| Supergraph schema (SDL) | Composite schema + `.far` archive | Binary archive containing the composed schema and subgraph metadata. |
| Federation subgraph library (`@apollo/subgraph`) | No equivalent needed | Subgraphs are standard HotChocolate servers. No federation library. |
| `_service { sdl }` introspection | `dotnet run -- schema export` | Schema export is a CLI command, not a runtime introspection field. |

## What Fusion Does Not Need

Several things from Apollo's model have no Fusion equivalent because the architecture handles them differently.

**No `_entities` query.** In Apollo Federation, the gateway resolves entities by calling a hidden `_entities` root field with typed representations. In Fusion, entity resolution happens through regular Query fields annotated with `[Lookup]`. These are real, typed fields that you can call directly from any GraphQL client for testing and debugging.

**No `__resolveReference` resolvers.** In Apollo, every subgraph that contributes to an entity must implement a `__resolveReference` function. In Fusion, you write a normal Query field (like `GetProductById`) and add `[Lookup]`. The gateway calls this field like any other query.

**No federation subgraph library.** Apollo subgraphs require `@apollo/subgraph` (or the equivalent in your language) to add federation-specific fields and middleware. Fusion subgraphs are standard HotChocolate servers. You add a few attributes (`[Lookup]`, `[Shareable]`, etc.) and export the schema -- no special federation runtime.

**No `_service` introspection.** Apollo subgraphs expose their SDL via `_service { sdl }`. Fusion subgraphs export their schema as a `.graphqls` file using the command `dotnet run -- schema export`. The schema file and its companion `schema-settings.json` are what composition reads.

**No `@key` directive.** In Apollo, `@key(fields: "id")` tells the gateway which fields identify an entity. In Fusion, the gateway infers entity keys from the arguments of your `[Lookup]` fields. If your lookup is `GetProductById(int id)`, the gateway knows that `id` is the key for `Product`. You can use `[EntityKey("id")]` for explicit key declaration when needed, but it is rarely necessary.

## Behavioral Differences in Depth

Beyond naming, several concepts work fundamentally differently in Fusion. Understanding these differences is important for a successful migration.

### Entity Resolution: Lookups vs. `_entities`

This is the most significant architectural difference between Apollo Federation and Fusion.

**Apollo approach:** The gateway sends a batch request to the `_entities` field, passing an array of typed representations (like `{ __typename: "Product", id: "1" }`). Each subgraph's `__resolveReference` function handles these representations.

```graphql
# Apollo: hidden _entities query (you never write this yourself)
query {
  _entities(representations: [{ __typename: "Product", id: "1" }]) {
    ... on Product {
      name
      price
    }
  }
}
```

**Fusion approach:** The gateway calls a regular Query field that you define and annotate with `[Lookup]`. There is no hidden protocol.

```csharp
// Fusion: a regular query field with [Lookup]
[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static async Task<Product?> GetProductById(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);
}
```

```graphql
# Fusion: the gateway calls a regular query
query {
  productById(id: 1) {
    name
    price
  }
}
```

The practical benefit: you can call `productById` directly in your GraphQL IDE (like Nitro / Banana Cake Pop) to test entity resolution. In Apollo, `_entities` is hidden and awkward to test manually.

**Multiple lookups per entity.** In Apollo, an entity can have multiple `@key` directives to support different keys. In Fusion, you define multiple `[Lookup]` fields -- one per key:

```csharp
[QueryType]
public static partial class UserQueries
{
    [Lookup]
    public static async Task<User?> GetUserById(
        int id,
        IUserByIdDataLoader userById,
        CancellationToken cancellationToken)
        => await userById.LoadAsync(id, cancellationToken);

    [Lookup]
    public static async Task<User?> GetUserByUsername(
        string username,
        IUserByNameDataLoader userByName,
        CancellationToken cancellationToken)
        => await userByName.LoadAsync(username, cancellationToken);
}
```

The gateway automatically discovers all available lookups and uses whichever one has the keys it needs.

**Internal lookups.** When a subgraph extends an entity from another subgraph, it needs a lookup the gateway can use for entity resolution -- but you may not want that lookup exposed to clients. In Apollo, `_entities` handles this implicitly. In Fusion, you mark the lookup with `[Internal]`:

```csharp
// In the Reviews subgraph: an internal-only lookup for Product
[QueryType]
public static partial class ProductQueries
{
    [Lookup, Internal]
    public static Product GetProductById([ID<Product>] int id)
        => new(id);
}
```

The `[Internal]` attribute hides this field from the composite schema. Only the gateway uses it during query planning.

For more on lookups and entity resolution patterns, see [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).

### `@require` Operates on Arguments, Not Fields

In Apollo Federation, `@requires` is a field-level directive. It declares that a field depends on data from another subgraph:

```graphql
# Apollo: @requires on the field
type Product @key(fields: "id") {
  id: ID!
  weight: Float @external
  shippingEstimate: Int @requires(fields: "weight")
}
```

In Fusion, `[Require]` is an argument-level attribute. The required data arrives as a method parameter, and that parameter is hidden from the composite schema:

```csharp
// Fusion: [Require] on the argument
[EntityKey("id")]
public sealed record Product([property: ID<Product>] int Id)
{
    public int GetDeliveryEstimate(
        string zip,
        [Require("""
            {
              weight,
              length: dimension.length,
              width: dimension.width,
              height: dimension.height
            }
            """)]
        ProductDimensionInput dimension)
    {
        // dimension.Weight, dimension.Length, etc. are provided by the gateway
        // from whatever subgraph owns those fields
        var volume = dimension.Length * dimension.Width * dimension.Height;
        return CalculateEstimate(zip, volume, dimension.Weight);
    }
}
```

In the composite schema, clients see `deliveryEstimate(zip: String!)` -- the `dimension` parameter is invisible. The gateway resolves `weight` and `dimension { length width height }` from the owning subgraph and passes them to the Shipping subgraph automatically.

This changes how you design resolvers. In Apollo, the required data is available on `this` (the entity object). In Fusion, it arrives as a typed argument, which makes the dependency explicit and testable.

Cross-subgraph data dependencies with `[Require]` are also covered in the [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph) page. A dedicated deep-dive will be available in future documentation.

### Entity Stubs: How Subgraphs Reference Foreign Entities

In Apollo, when a subgraph extends an entity from another subgraph, it uses `extend type` with `@key`:

```graphql
# Apollo: Reviews subgraph extending Product
extend type Product @key(fields: "id") {
  id: ID! @external
  reviews: [Review!]!
}
```

In Fusion, you create an **entity stub** -- a minimal C# type that declares the entity's key and adds your fields:

```csharp
// Fusion: Reviews subgraph's entity stub for Product
public sealed record Product([property: ID<Product>] int Id)
{
    [UsePaging(ConnectionName = "ProductReviews")]
    public async Task<Connection<Review>> GetReviewsAsync(
        PagingArguments pagingArgs,
        IReviewsByProductIdDataLoader reviewByProductId,
        CancellationToken cancellationToken)
        => await reviewByProductId
            .With(pagingArgs)
            .LoadAsync(Id, cancellationToken)
            .ToConnectionAsync();
}
```

The stub is not a copy of the full Product type. It only declares the key (`Id`) and the fields this subgraph contributes (`reviews`). The gateway merges it with the full `Product` type from the Products subgraph during composition.

### Composition: Build Step, Not Cloud Operation

In Apollo, composition typically happens in GraphOS cloud when you run `rover subgraph publish`. The router downloads the composed supergraph schema from GraphOS at startup.

In Fusion, composition is a local build step you run on your machine or in CI:

```bash
nitro fusion compose \
  --source-schema-file ./products/schema.graphqls \
  --source-schema-file ./reviews/schema.graphqls \
  --archive gateway.far
```

This produces a `.far` (Fusion Archive) file -- a binary archive containing the composed schema and subgraph metadata. You can inspect what composition produced, run it locally, and validate it in CI before deployment.

You can also use Nitro cloud for managed composition (similar to Apollo's GraphOS), but it is not required. Everything works fully offline.

For more on composition rules and error resolution, see [Composition](/docs/fusion/v16/composition).

### The Gateway Is Code, Not a Separate Binary

Apollo Router is a standalone binary (written in Rust) that you configure via YAML. Fusion's gateway is an ASP.NET Core application that you write and control:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

Because it is a standard ASP.NET Core app, you get full access to the middleware pipeline, dependency injection, authentication, header propagation, and everything else in the .NET ecosystem. There is no separate binary to deploy or configure.

## Step-by-Step Migration

### Phase 1: Migrate Subgraphs

For each Apollo Federation subgraph, follow these steps.

#### Step 1: Replace Apollo Packages with HotChocolate

Remove the Apollo subgraph library and add HotChocolate packages.

**Apollo (Node.js / TypeScript):**

```json
{
  "dependencies": {
    "@apollo/subgraph": "^2.x",
    "graphql": "^16.x"
  }
}
```

**Fusion (C#):**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <PackageReference Include="HotChocolate.AspNetCore" />
    <PackageReference Include="HotChocolate.AspNetCore.CommandLine" />
    <PackageReference Include="HotChocolate.Types.Analyzers">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

Set up a standard HotChocolate server in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("my-subgraph")
    .AddTypes()
    .AddMutationConventions()
    .AddGlobalObjectIdentification();

var app = builder.Build();
app.MapGraphQL();
app.RunWithGraphQLCommands(args);
```

The call to `RunWithGraphQLCommands(args)` enables `dotnet run -- schema export`, which is how Fusion extracts the subgraph schema for composition.

#### Step 2: Convert `@key` + `__resolveReference` to `[Lookup]`

This is the core conversion. For every entity type that has a `@key` directive and a `__resolveReference` resolver, create a `[Lookup]` query field.

**Apollo (TypeScript):**

```typescript
// Apollo: schema
// type Product @key(fields: "id") {
//   id: ID!
//   name: String!
//   price: Float!
// }

const resolvers = {
  Product: {
    __resolveReference(ref) {
      return fetchProductById(ref.id);
    },
  },
  Query: {
    product: (_, { id }) => fetchProductById(id),
  },
};
```

**Fusion (C#):**

```csharp
// The entity type
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
}

// The lookup replaces both __resolveReference AND the query field
[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static async Task<Product?> GetProductById(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);
}
```

The `[Lookup]` attribute serves double duty: it makes the field callable by clients (like a normal query) and tells the gateway to use it for entity resolution. If you only want the gateway to use it, add `[Internal]`:

```csharp
[Lookup, Internal]
public static Product GetProductById(int id) => new(id);
```

**Key point:** You do not need a `@key` directive. The gateway infers the entity key from the lookup's arguments. If your lookup takes `int id`, the gateway knows `id` is the key.

#### Step 3: Convert `@requires` to `[Require]`

Replace field-level `@requires` with argument-level `[Require]`.

**Apollo (GraphQL SDL):**

```graphql
type Product @key(fields: "id") {
  id: ID!
  weight: Float @external
  shippingEstimate: Int @requires(fields: "weight")
}
```

```typescript
const resolvers = {
  Product: {
    shippingEstimate(product) {
      // product.weight is available because of @requires
      return calculateEstimate(product.weight);
    },
  },
};
```

**Fusion (C#):**

```csharp
[EntityKey("id")]
public sealed record Product([property: ID<Product>] int Id)
{
    public int GetShippingEstimate(
        [Require("weight")] int weight)
    {
        return CalculateEstimate(weight);
    }
}
```

The `weight` parameter is hidden from the composite schema. Clients call `shippingEstimate` with no arguments -- the gateway resolves `weight` from whichever subgraph owns it and passes it to this resolver.

For complex requirements that map multiple fields into an input object:

```csharp
public int GetDeliveryEstimate(
    string zip,
    [Require("""
        {
          weight,
          length: dimension.length,
          width: dimension.width,
          height: dimension.height
        }
        """)]
    ProductDimensionInput dimension)
{
    // Use dimension.Weight, dimension.Length, etc.
}
```

#### Step 4: Convert `@external` / `@provides`

**`@external`** has a direct equivalent in `[External]`, but it is less frequently needed. In Apollo, you must mark any field referenced by `@requires` as `@external`. In Fusion, the `[Require]` selection syntax references fields from the composed graph directly -- no `@external` annotation is needed on the entity type.

**`@provides`** maps to `[Parent(requires: "...")]`. This optimization hint tells the gateway that a field can resolve certain nested fields locally, avoiding an extra subgraph call:

**Apollo:**

```graphql
type Review {
  author: User @provides(fields: "email")
}

type User @key(fields: "id") {
  id: ID!
  email: String! @external
}
```

**Fusion:**

```csharp
[ObjectType<Review>]
public static partial class ReviewNode
{
    public static User GetAuthor(
        [Parent(requires: nameof(Review.AuthorId))] Review review)
        => new User(review.AuthorId, review.AuthorEmail);
}
```

#### Step 5: Handle `[Shareable]`

`[Shareable]` works the same in both systems. If multiple subgraphs define the same field on the same type, each definition must be marked as shareable.

**Apollo (GraphQL SDL):**

```graphql
# Both subgraphs define User.name
type User @key(fields: "id") {
  id: ID!
  name: String! @shareable
}
```

**Fusion (C#):**

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [Shareable]
    public static string GetName([Parent] User user) => user.Name!;
}
```

One difference: in Fusion, key fields (like `id`) are automatically shareable. You do not need to annotate them.

#### Step 6: Create `schema-settings.json`

Every Fusion subgraph needs a `schema-settings.json` file that tells composition where the subgraph lives and how to connect to it:

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

Place this file next to your project. The `name` field must be unique across all subgraphs. The `clientName` field (`"fusion"`) must match the named HTTP client configured in the gateway.

#### Step 7: Export the Schema

Run the schema export command:

```bash
dotnet run -- schema export
```

This generates a `.graphqls` file containing your subgraph's schema with Fusion-specific directives. This file, together with `schema-settings.json`, is what composition reads.

### Phase 2: Migrate the Gateway

Replace Apollo Router with a Fusion gateway ASP.NET Core project.

#### Step 1: Create the Gateway Project

```bash
dotnet new web -n Gateway
cd Gateway
dotnet add package HotChocolate.Fusion.AspNetCore
```

#### Step 2: Configure the Gateway

**Minimal gateway (`Program.cs`):**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the Fusion gateway
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

This loads the gateway configuration from a local `.far` file. To use Nitro cloud for configuration delivery instead (similar to Apollo's managed federation):

```csharp
builder
    .AddGraphQLGateway()
    .AddNitro();
```

#### Step 3: Set Up Header Propagation

If your Apollo Router forwards headers (like `Authorization`) to subgraphs, configure the same in Fusion:

**Apollo Router (YAML):**

```yaml
headers:
  all:
    request:
      - propagate:
          named: Authorization
```

**Fusion Gateway (C#):**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure header propagation
builder.Services.AddHeaderPropagation(options =>
{
    options.Headers.Add("Authorization");
});

// The named HTTP client must match "clientName" in schema-settings.json
builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();
app.UseHeaderPropagation();
app.MapGraphQL();
app.Run();
```

#### Step 4: Compose and Run

Compose your subgraph schemas into a gateway archive:

```bash
nitro fusion compose \
  --source-schema-file ./products/schema.graphqls \
  --source-schema-file ./reviews/schema.graphqls \
  --archive gateway.far
```

Then start the gateway:

```bash
cd Gateway
dotnet run
```

Navigate to `http://localhost:5000/graphql` to open the Nitro IDE and run cross-subgraph queries.

### Phase 3: Migrate CI/CD

Replace Apollo's `rover` commands with Nitro CLI equivalents.

| Apollo (`rover`) | Fusion (`nitro`) |
|---|---|
| `rover subgraph check` | `nitro fusion validate` |
| `rover subgraph publish` | `nitro fusion upload` + `nitro fusion publish` |
| `rover supergraph compose` | `nitro fusion compose` |

#### Schema Upload (Replaces `rover subgraph publish`)

In Apollo, publishing a subgraph triggers server-side composition. In Fusion, this is a two-step process: upload the schema, then publish to trigger composition.

**Apollo:**

```bash
rover subgraph publish my-graph@production \
  --name products \
  --schema ./schema.graphqls \
  --routing-url https://products.example.com/graphql
```

**Fusion:**

```bash
# Step 1: Upload the source schema
nitro fusion upload \
  --source-schema-file ./schema.graphqls \
  --tag v1.0.0 \
  --api-id $NITRO_API_ID \
  --api-key $NITRO_API_KEY

# Step 2: Publish to trigger composition and deploy to a stage
nitro fusion publish \
  --source-schema products-api \
  --tag v1.0.0 \
  --stage production \
  --api-id $NITRO_API_ID \
  --api-key $NITRO_API_KEY
```

#### Schema Validation (Replaces `rover subgraph check`)

**Apollo:**

```bash
rover subgraph check my-graph@production \
  --name products \
  --schema ./schema.graphqls
```

**Fusion:**

```bash
nitro fusion validate \
  --source-schema-file ./schema.graphqls \
  --stage production \
  --api-id $NITRO_API_ID \
  --api-key $NITRO_API_KEY
```

#### Local Composition (Replaces `rover supergraph compose`)

**Apollo:**

```bash
rover supergraph compose --config ./supergraph-config.yaml --output supergraph.graphql
```

**Fusion:**

```bash
nitro fusion compose \
  --source-schema-file ./products/schema.graphqls \
  --source-schema-file ./reviews/schema.graphqls \
  --archive gateway.far
```

#### Example GitHub Actions Workflow

```yaml
name: Deploy Subgraph
on:
  push:
    branches: [main]
    paths:
      - "src/Products/**"

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Build and publish container
        working-directory: src/Products
        run: dotnet publish -c Release

      - name: Export schema
        working-directory: src/Products
        run: dotnet run -- schema export

      - name: Upload source schema
        run: |
          nitro fusion upload \
            --source-schema-file src/Products/schema.graphqls \
            --tag ${{ github.sha }} \
            --api-id ${{ secrets.NITRO_API_ID }} \
            --api-key ${{ secrets.NITRO_API_KEY }}

      - name: Publish to production
        run: |
          nitro fusion publish \
            --source-schema products-api \
            --tag ${{ github.sha }} \
            --stage production \
            --api-id ${{ secrets.NITRO_API_ID }} \
            --api-key ${{ secrets.NITRO_API_KEY }}
```

For more on deployment workflows, see [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd).

## Mindset Shifts

If you have spent significant time with Apollo Federation, some habits need adjusting. These are not just naming differences -- they change how you think about your graph.

### Entity Resolution Is Explicit and Testable

In Apollo, entity resolution happens through a hidden protocol (`_entities` + `__resolveReference`). You cannot easily call `_entities` from a GraphQL client to debug resolution issues. In Fusion, entity resolution is a regular query field. You can open your IDE, call `productById(id: 1)`, and see exactly what your lookup returns. This makes debugging straightforward.

### You Don't Need to Think About Entity Ownership the Same Way

Apollo Federation has a strong concept of entity "ownership" -- one subgraph is the "defining" subgraph for an entity, and others "extend" it. In Fusion, all subgraphs contribute fields to shared entity types. The gateway uses lookups to resolve entities wherever they need to be fetched. The question is not "who owns this entity?" but "which subgraphs provide lookups for it?"

### Composition Is a Build Step, Not a Cloud Operation

In Apollo, composition typically happens in GraphOS when you publish a subgraph. In Fusion, composition is a command you run locally or in CI:

```bash
nitro fusion compose --archive gateway.far
```

You can run this on your machine, see the output, inspect errors, and fix them before pushing. There is no cloud service in the loop unless you choose to use Nitro cloud.

### `@require` Operates on Arguments, Not Fields

This changes resolver design. In Apollo, required data appears on the entity object. In Fusion, it arrives as a method parameter:

```csharp
// The 'weight' parameter is injected by the gateway
public int GetShippingEstimate([Require("weight")] int weight)
{
    return CalculateEstimate(weight);
}
```

This makes dependencies explicit in the method signature. You can see exactly what data a resolver needs by looking at its parameters.

### The Gateway Is Your Code

Apollo Router is a pre-built binary you configure externally. Fusion's gateway is an ASP.NET Core application you control. You write `Program.cs`, configure middleware, add authentication, and deploy it like any other .NET service. This means you have full control but also full responsibility for the gateway's behavior.

## What Gets Simpler

Migrating to Fusion resolves several common pain points from Apollo Federation.

**No federation library required.** Apollo subgraphs need `@apollo/subgraph` (or an equivalent library in your language). Fusion subgraphs are standard HotChocolate servers. If you already have a HotChocolate GraphQL server, it is already a valid Fusion subgraph -- you just need to add a few attributes and export the schema.

**Lookups are testable.** In Apollo, `_entities` is hidden and awkward to test. In Fusion, lookups are regular query fields. You can write integration tests that call them directly, use them in your GraphQL IDE, and verify their behavior in isolation.

**Build-time composition catches errors early.** Apollo's managed federation composes schemas when you publish. If composition fails, you find out after pushing. Fusion's composition runs locally as a build step -- you can catch schema conflicts the same way you catch compilation errors: before you commit.

**.NET-native tooling.** If your team is a .NET shop, Fusion means your gateway, subgraphs, and tooling are all .NET. No Node.js dependency for the gateway or CLI, no context-switching between languages.

**Open standards.** Fusion implements the [GraphQL Composite Schemas specification](https://graphql.github.io/composite-schemas-spec/), an open, vendor-neutral standard under the GraphQL Foundation. Your subgraph schemas are portable -- they are not locked into any vendor's directive syntax.

**Simpler subgraph setup.** Without a federation library, there are fewer moving parts. A minimal Fusion subgraph is just a HotChocolate server with `[Lookup]` on its entity query fields and a `schema-settings.json` file. That is the entire federation surface area.

## FAQ

### Can I migrate incrementally -- some subgraphs on Apollo, some on Fusion?

No. Apollo Federation and Fusion use different gateway protocols and composition models. You cannot run a mixed fleet where some subgraphs speak Apollo's federation protocol and others speak Fusion's. You need to migrate all subgraphs and the gateway together. However, you can migrate one subgraph at a time by converting and testing each one before switching the gateway over.

### Do I need Nitro cloud?

No. Nitro cloud provides managed composition and gateway configuration delivery (similar to Apollo's GraphOS), but everything works without it. You can compose schemas locally with `nitro fusion compose`, load the `.far` file from disk with `AddFileSystemConfiguration()`, and never touch a cloud service. Nitro cloud is optional for teams that want managed schema delivery.

### What about Apollo Federation directives like `@authenticated` and `@policy`?

Fusion uses standard ASP.NET Core authentication and authorization. You configure JWT/cookie authentication in the gateway's middleware pipeline and use HotChocolate's `[Authorize]` attribute on fields and types in your subgraphs. There is no Fusion-specific auth directive -- you use the same patterns you already know from ASP.NET Core.

### Can Fusion handle subscriptions?

Yes. Fusion supports real-time subscriptions through the gateway via SSE (Server-Sent Events) and WebSocket transports. Subgraphs can use any HotChocolate subscription provider, including Postgres-backed subscriptions for multi-instance scenarios.

### Where do I go from here?

After migrating, these pages provide deeper coverage of specific topics:

- [Getting Started](/docs/fusion/v16/getting-started) -- The full tutorial for building a Fusion setup from scratch
- [Entities and Lookups](/docs/fusion/v16/entities-and-lookups) -- Deep dive into entity resolution patterns
- [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph) -- Adding subgraphs with `[Require]` for cross-subgraph data
- [Composition](/docs/fusion/v16/composition) -- Composition rules, merging behavior, and error reference
- [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd) -- Production deployment and pipeline setup
- [Nitro CLI Reference](/docs/fusion/v16/nitro-cli-reference) -- Complete CLI command reference
