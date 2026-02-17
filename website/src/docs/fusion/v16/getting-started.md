---
title: "Getting Started"
---

# Getting Started with HotChocolate Fusion

## What Is Fusion and Why Use It?

HotChocolate Fusion lets you split a GraphQL API across multiple independent services -- called **subgraphs** -- and present them to clients as a single, unified schema (called a **composite schema** in the spec). Each subgraph contributes types and fields to the overall graph, runs in its own process, and can be developed, deployed, and scaled independently. A **gateway** sits in front of all your subgraphs, receives client queries, routes each part of the query to the subgraphs that own the requested fields, and combines the results. From the client's perspective, they are querying a single GraphQL endpoint with one schema -- the gateway handles everything transparently.

Instead of building one monolithic GraphQL server that knows about every domain in your system, you let each team own and ship their own GraphQL service. Fusion handles the hard part -- composing those services into a single schema that clients query without knowing (or caring) how many services are behind it.

**When should you use Fusion?** If your GraphQL API is served by a single HotChocolate server and that works for your team, there is no reason to add the complexity of multiple subgraphs. Fusion becomes valuable when your API spans multiple domains (products, reviews, accounts, shipping), when different teams own different parts of the graph, or when you need to deploy and scale parts of your API independently. If you find yourself wanting to break a growing monolith into smaller, team-owned services without forcing clients to stitch together calls to multiple endpoints, Fusion is the tool for the job.

## Thinking in Composed Graphs

When you first encounter distributed GraphQL, there is a natural assumption about how subgraphs interact that turns out to be wrong. Getting the right mental model now will save you from the most common design mistakes.

### One Graph, Many Contributors

In Fusion, all subgraphs contribute to **one shared graph**. There is no "Products API" and "Reviews API" that exist as separate GraphQL schemas -- there is one graph that describes your entire domain, and different subgraphs are responsible for different parts of it.

This means a single type can have fields contributed by multiple subgraphs. For example, the `Product` type might get its `name` and `price` fields from the Products subgraph, its `reviews` field from the Reviews subgraph, and its `deliveryEstimate` field from the Shipping subgraph. From the client's perspective, `Product` is one type with all those fields. The gateway figures out where each field lives and fetches it from the right place.

### Subgraphs Don't Call Each Other

When you split an API across multiple services, it is tempting to think each subgraph needs to call other subgraphs directly. Consider two subgraphs: one that manages Tenants, and one that manages Users. A `User` belongs to a `Tenant`.

**You might assume this is how it works:**

> "My Users subgraph stores a `tenantId` on each User. To resolve the full Tenant object, I need my Users subgraph to call the Tenants subgraph and fetch it."

This seems reasonable, but it creates a problem: your subgraphs become coupled. The Users subgraph has to know where the Tenants subgraph lives, how to call it, and how to handle failures. As you add more subgraphs, these cross-service dependencies multiply.

**How Fusion actually works:**

> "My Users subgraph knows that each User has a `tenantId`, and I declare that `user.tenant` returns a `Tenant` identified by that ID. I don't need to know *how* or *where* the Tenant gets resolved -- the gateway handles that."

In Fusion, your subgraph never calls another subgraph. Instead, it says: "this field returns a Tenant with this ID" and trusts the gateway to figure out the rest. The gateway knows which subgraph can resolve a Tenant by ID (using a **lookup** -- a query field that resolves an entity by its key). The Tenants subgraph provides this lookup resolver, and the gateway calls it when it needs to turn a tenant ID into a full Tenant object. The Users subgraph never talks to the Tenants subgraph directly -- the gateway handles all coordination.

### Why This Matters

This design has practical consequences:

- **Subgraphs stay independent.** The Users subgraph does not import anything from the Tenants subgraph. It just knows that `Tenant` is an entity with an `id` field.
- **Adding new subgraphs is safe.** If a new Billing subgraph wants to add a `billingPlan` field to the `Tenant` type, it can -- without modifying the Tenants or Users subgraphs.
- **The gateway is the coordinator.** Cross-subgraph data fetching is the gateway's job, not yours. Your subgraph only needs to know how to resolve the fields it owns and how to look up its own entities by key.

When you build your subgraphs in the following sections, keep this model in mind: each subgraph contributes to one graph, declares its entities and lookups, and trusts the gateway to wire everything together.

## Key Concepts

Before diving into code, here are the core terms you will encounter throughout this guide. Each builds on the mental model from the previous section.

**Subgraph** -- An independent GraphQL service that contributes types and fields to the overall graph. Each subgraph runs in its own process, owns its own data, and defines resolvers for the fields it contributes. In HotChocolate, a subgraph is a standard HotChocolate server with a few additional attributes that tell Fusion how its types fit into the larger graph.

**Source Schema** -- The GraphQL schema exposed by a single subgraph. When you export a subgraph's schema (as a `.graphqls` file), that exported schema is the source schema. Fusion's composition engine reads source schemas from all your subgraphs and merges them into the composite schema.

**Composite Schema** -- The unified, client-facing GraphQL schema produced by merging all source schemas. All types and fields from every subgraph appear in the composite schema, and clients can query across subgraph boundaries in a single request -- as if they were querying a single monolithic GraphQL server. They never interact with individual subgraphs directly.

**Gateway** -- The service that sits between clients and subgraphs. It exposes the composite schema, receives client queries, analyzes each query, routes parts of it to the appropriate subgraphs, and combines the results into a single response. In HotChocolate, you create a gateway by calling `AddGraphQLGateway()` in a standard ASP.NET Core project.

**Query Planning** -- The process the gateway uses to execute a client query across multiple subgraphs. When a query touches fields from different subgraphs, the gateway analyzes the query, determines which subgraphs own which fields, and creates an execution plan that fetches data in the right order. For example, if a query asks for `product.reviews.author.name`, the gateway must first fetch the Product (from the Products subgraph), then fetch Reviews (to get author IDs), then fetch Users (to get names) -- coordinating across three subgraphs in sequence. This happens automatically -- you do not write query plans yourself.

**Entity** -- A type that can be uniquely identified and resolved across subgraphs. For example, `Product` is an entity because the Products subgraph defines it, the Reviews subgraph extends it with a `reviews` field, and the gateway can resolve a `Product` in any subgraph using its key (like `id`). Entities are defined by their **key fields** (like `id` or `sku`) that uniquely identify each instance. The gateway uses these keys to resolve entity references across subgraphs, which is what makes cross-subgraph types possible.

**Shareable** -- A marker that allows multiple subgraphs to define the same field on the same type. By default, Fusion requires that each non-key field belongs to exactly one subgraph -- if two subgraphs define the same field, composition fails with an error. When you mark a field with `[Shareable]`, you are telling Fusion: "this field is intentionally defined in multiple subgraphs, and all definitions return the same data." The gateway can then resolve the field from whichever subgraph is most convenient for a given query.

**Lookup** -- A query field in a subgraph that resolves an entity by its key. When the gateway needs to fetch a `Product` from the Products subgraph, it calls that subgraph's lookup field (e.g., `productById(id: 1)`). In HotChocolate, you mark a resolver as a lookup by adding the `[Lookup]` attribute. Every entity needs at least one lookup so the gateway can resolve references to it. Lookups come in two flavors:

- A **public lookup** (without `[Internal]`) serves two purposes: clients can call it directly as a query field, and the gateway uses it for entity resolution. Because clients can call it, a public lookup should validate that the entity exists and return `null` if it does not.
- An **internal lookup** (marked with `[Internal]`) is hidden from the composite schema and is only used by the gateway during query planning. Internal lookups often just construct a stub object from an ID without checking whether the entity actually exists -- this is fine because the gateway only calls them as part of entity resolution, where the entity's existence has already been established by another subgraph.

## Prerequisites

To follow this guide, you need the following installed on your machine.

### .NET SDK

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) or later. Verify your installation:

```bash
dotnet --version
```

You should see `10.0.100` or higher.

### Nitro CLI

The Nitro CLI is a .NET tool that handles schema composition. Install it globally:

```bash
dotnet tool install -g ChilliCream.Nitro.CLI
```

Verify the installation:

```bash
nitro version
```

### NuGet Packages

You do not need to install packages manually -- each project you create will reference them in its `.csproj` file. For reference, these are the HotChocolate packages used in this guide:

**For subgraphs:**

- `HotChocolate.AspNetCore` -- The HotChocolate GraphQL server for ASP.NET Core
- `HotChocolate.Types.Analyzers` -- Source generator that auto-registers your types

**For the gateway:**

- `HotChocolate.Fusion.AspNetCore` -- The Fusion gateway for ASP.NET Core

All packages use prerelease version `16.0.0-p.11.2` or later, available on nuget.org. At the time of writing, the latest preview is `16.0.0-p.11.2`.

### What You Do Not Need

This guide deliberately keeps infrastructure simple so you can focus on Fusion concepts. You do **not** need:

- A database -- all data is in-memory
- Docker or containers
- .NET Aspire
- Authentication or Keycloak
- A Nitro cloud account

Everything runs locally on your machine.

## Create Your First Subgraph (Products)

Time to write code. You will create a Products subgraph that exposes a few products through a GraphQL API. This subgraph will later become part of your composed graph.

### Project Setup

Create a new ASP.NET Core web project and add the required packages:

```bash
mkdir fusion-getting-started
cd fusion-getting-started

dotnet new web -n Products
cd Products

dotnet add package HotChocolate.AspNetCore --version "16.0.0-p.11.2"
dotnet add package HotChocolate.Types.Analyzers --version "16.0.0-p.11.2"

cd ..
```

Your `Products/Products.csproj` should now look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="HotChocolate.AspNetCore" Version="16.0.0-p.11.2" />
    <PackageReference Include="HotChocolate.Types.Analyzers" Version="16.0.0-p.11.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

### Configure the Port

By default, ASP.NET Core picks a port for you. To keep things predictable, configure the Products subgraph to run on port 5001. Edit `Products/Properties/launchSettings.json` and set the `applicationUrl` under the `http` profile to:

```json
"applicationUrl": "http://localhost:5001"
```

This ensures the subgraph runs on port 5001, which matches what we will configure for composition later.

### Define the Product Type

Create a file called `Product.cs` in the Products project:

```csharp
namespace Products;

public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public double Price { get; set; }
}
```

This is a plain C# class -- nothing GraphQL-specific yet.

### Add In-Memory Data

Create a file called `ProductRepository.cs`:

```csharp
namespace Products;

public static class ProductRepository
{
    private static readonly List<Product> Products =
    [
        new Product { Id = 1, Name = "Table", Price = 899.99 },
        new Product { Id = 2, Name = "Couch", Price = 1299.50 },
        new Product { Id = 3, Name = "Chair", Price = 54.00 },
    ];

    public static Product? GetById(int id)
        => Products.FirstOrDefault(p => p.Id == id);

    public static List<Product> GetAll()
        => Products;
}
```

In production, you would use `[DataLoader]` to batch database lookups -- this prevents N+1 queries when the gateway resolves entities across subgraphs. See the fusion-demo repository for DataLoader examples.

### Expose Products Through GraphQL

Create a file called `ProductQueries.cs`. This class defines the Query root fields for the Products subgraph:

```csharp
namespace Products;

[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static Product? GetProductById(int id)
        => ProductRepository.GetById(id);

    public static List<Product> GetProducts()
        => ProductRepository.GetAll();
}
```

The `partial` keyword is required because the source generator adds generated code to this class at compile time.

Two attributes to notice:

- **`[QueryType]`** tells HotChocolate that this class contributes fields to the `Query` root type. The source generator (from `HotChocolate.Types.Analyzers`) automatically registers these fields.
- **`[Lookup]`** marks `GetProductById` as a lookup resolver. This is how the gateway resolves a `Product` when another subgraph references it. Without this attribute, the gateway would have no way to fetch a Product by its ID from this subgraph. Because this lookup is **public** (not marked `[Internal]`), it also appears in the composite schema as a query field clients can call directly. Notice it returns `Product?` (nullable) -- if the ID does not match any product, it returns `null`. This is important for public lookups because clients can call them with arbitrary IDs.

### Configure the Server

Replace the contents of `Program.cs` with:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddTypes();

var app = builder.Build();

app.MapGraphQL();
app.RunWithGraphQLCommands(args);
```

This is a minimal HotChocolate server. `AddTypes()` is a source-generated method that automatically registers all types discovered by the `HotChocolate.Types.Analyzers` package -- including `ProductQueries` and the `Product` type. The `RunWithGraphQLCommands(args)` method (instead of `app.Run()`) enables CLI commands, including schema export which you will use shortly.

### Test the Subgraph

Run the Products subgraph:

```bash
cd Products
dotnet run
```

Open your browser to `http://localhost:5001/graphql/` to access the Banana Cake Pop GraphQL IDE. Try this query:

```graphql
query {
  products {
    id
    name
    price
  }
}
```

You should see:

```json
{
  "data": {
    "products": [
      { "id": 1, "name": "Table", "price": 899.99 },
      { "id": 2, "name": "Couch", "price": 1299.5 },
      { "id": 3, "name": "Chair", "price": 54 }
    ]
  }
}
```

Also test the lookup resolver, which the gateway will use later:

```graphql
query {
  productById(id: 1) {
    id
    name
    price
  }
}
```

Stop the server with `Ctrl+C` before continuing.

### Export the Schema

The gateway needs each subgraph's schema for composition. HotChocolate can export the schema files automatically using a CLI command. Add the command-line package:

```bash
cd Products
dotnet add package HotChocolate.AspNetCore.CommandLine --version "16.0.0-p.11.2"
```

Now export the schema. From the `fusion-getting-started` directory:

```bash
dotnet run ./Products -- schema export
```

This generates two files in the Products project directory:

- **`schema.graphqls`** -- The Products subgraph's GraphQL schema, describing its types and fields.
- **`schema-settings.json`** -- A companion file that tells Fusion the subgraph's name and runtime URL.

Open `Products/schema-settings.json` and verify it contains the subgraph name. You will need to set the transport URL so the gateway knows where to reach this subgraph at runtime. Update it to:

```json
{
  "name": "products",
  "transports": {
    "http": {
      "url": "http://localhost:5001/graphql"
    }
  }
}
```

The `name` field is the unique identifier for this subgraph in the composed graph. The `url` is where the gateway will send requests to this subgraph at runtime.

### What You Built

Your Products subgraph now:

- Exposes a `products` query that returns all products
- Provides a `productById` lookup that the gateway will use to resolve Product references from other subgraphs
- Has an exported schema file (`schema.graphqls`) describing its types
- Has a settings file (`schema-settings.json`) telling Fusion where to reach it at runtime

In the next section, you will create a second subgraph (Reviews) that references Products -- this is where the power of Fusion starts to show.

## Create a Second Subgraph (Reviews)

Now you will create a Reviews subgraph that adds review data to the graph. The key part: the Reviews subgraph will add a `reviews` field to the `Product` type that lives in the Products subgraph. This is Fusion's core capability -- multiple subgraphs contributing fields to the same type.

### Project Setup

From the `fusion-getting-started` directory:

```bash
dotnet new web -n Reviews
cd Reviews

dotnet add package HotChocolate.AspNetCore --version "16.0.0-p.11.2"
dotnet add package HotChocolate.AspNetCore.CommandLine --version "16.0.0-p.11.2"
dotnet add package HotChocolate.Types.Analyzers --version "16.0.0-p.11.2"

cd ..
```

### Configure the Port

Edit `Reviews/Properties/launchSettings.json` and set the `applicationUrl` under the `http` profile to:

```json
"applicationUrl": "http://localhost:5002"
```

The Reviews subgraph runs on port 5002, while the Products subgraph runs on 5001.

### Define the Review Type

Create `Review.cs` in the Reviews project:

```csharp
namespace Reviews;

public class Review
{
    public int Id { get; set; }

    public required string Body { get; set; }

    public int Stars { get; set; }

    public int ProductId { get; set; }
}
```

Each review has a `ProductId` that references a product from the Products subgraph.

### Add In-Memory Data

Create `ReviewRepository.cs`:

```csharp
namespace Reviews;

public static class ReviewRepository
{
    private static readonly List<Review> Reviews =
    [
        new Review { Id = 1, Body = "Sturdy and well-built.", Stars = 5, ProductId = 1 },
        new Review { Id = 2, Body = "A bit wobbly.", Stars = 3, ProductId = 1 },
        new Review { Id = 3, Body = "Very comfortable!", Stars = 5, ProductId = 2 },
        new Review { Id = 4, Body = "Good value for the price.", Stars = 4, ProductId = 3 },
    ];

    public static Review? GetById(int id)
        => Reviews.FirstOrDefault(r => r.Id == id);

    public static List<Review> GetByProductId(int productId)
        => Reviews.Where(r => r.ProductId == productId).ToList();

    public static List<Review> GetAll()
        => Reviews;
}
```

Notice that reviews reference products by `ProductId`. Reviews 1 and 2 belong to Product 1 (Table), Review 3 belongs to Product 2 (Couch), and Review 4 belongs to Product 3 (Chair).

### Define the Product Stub

This is the most important file in this section. Create `Product.cs`:

```csharp
namespace Reviews;

public sealed record Product(int Id)
{
    public List<Review> GetReviews()
        => ReviewRepository.GetByProductId(Id);
}
```

This is **not** a duplicate of the Product type from the Products subgraph. It is an **entity stub** -- a lightweight declaration that says: "I know `Product` exists in the graph, identified by `Id`, and I want to add a `reviews` field to it."

The Reviews subgraph does not define `name`, `price`, or any other Product fields. It only contributes the `reviews` field. When the gateway composes the graph, it merges this stub with the full `Product` type from the Products subgraph. Clients see one `Product` type with:

- `id`, `name`, and `price` from the Products subgraph
- `reviews` from the Reviews subgraph

All fields appear on a single unified `Product` type in the composite schema.

### Add Query Resolvers

Create `ReviewQueries.cs`:

```csharp
namespace Reviews;

[QueryType]
public static partial class ReviewQueries
{
    [Lookup]
    public static Review? GetReviewById(int id)
        => ReviewRepository.GetById(id);

    public static List<Review> GetReviews()
        => ReviewRepository.GetAll();
}
```

And create `ProductQueries.cs` -- this provides the internal lookup that lets the gateway resolve Product references within the Reviews subgraph:

```csharp
namespace Reviews;

[QueryType]
public static partial class ProductQueries
{
    [Lookup, Internal]
    public static Product GetProductById(int id)
        => new(id);
}
```

Two attributes to notice:

- **`[Lookup]`** makes this a lookup resolver for the `Product` entity. The gateway calls this when it needs to resolve a Product reference.
- **`[Internal]`** hides this field from the composite schema. Clients cannot call `productById` on the Reviews subgraph directly -- it exists only for the gateway's internal use during query planning.

Why is the internal lookup needed? When a client queries `review.product.reviews`, the gateway needs a way to enter the Reviews subgraph's `Product` type so it can resolve the `reviews` field. The internal lookup provides this entry point -- given a product ID (which the gateway already knows from another subgraph), it constructs a `Product` stub that the `reviews` field can then resolve against.

Notice that this lookup returns `Product` (non-nullable) and does `new(id)` -- it just constructs a stub from the ID without checking whether that product actually exists. This is safe because internal lookups are never called by clients directly. The gateway only calls them during entity resolution, after another subgraph has already confirmed the entity exists. Compare this to the Products subgraph's public `GetProductById`, which calls `ProductRepository.GetById(id)` and returns `Product?` (nullable) because clients can call it with any ID.

### Configure the Server

Replace the contents of `Program.cs` with:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddTypes();

var app = builder.Build();

app.MapGraphQL();
app.RunWithGraphQLCommands(args);
```

This is identical to the Products subgraph's `Program.cs`.

### Add the ObjectType Extension

There is one more piece needed. The `Product` record defines the `reviews` field, but HotChocolate needs to know that this is an **extension** of the existing `Product` type, not a new type. Create `ProductNode.cs`:

```csharp
using HotChocolate.Types;

namespace Reviews;

[ObjectType<Product>]
public static partial class ProductNode
{
    [BindMember(nameof(Review.ProductId))]
    public static Product GetProduct([Parent] Review review)
        => new(review.ProductId);
}
```

- **`[ObjectType<Product>]`** tells HotChocolate that this class extends the `Product` type. Fields defined on the `Product` record (like `GetReviews()`) become part of the `Product` type in GraphQL.
- **`[BindMember(nameof(Review.ProductId))]`** replaces the raw `ProductId` integer on `Review` with a resolved `Product` object. In the exported schema, clients see `review.product` (returning a full `Product`) instead of `review.productId` (returning a raw integer).
- **`[Parent]`** tells HotChocolate to inject the parent object (the `Review`) into the resolver. This is how `GetProduct()` accesses the `ProductId` from the review it belongs to.

This transformation happens in the GraphQL schema, not your C# classes. Your `Review` class still has a `ProductId` field internally, but in the exported schema that field is replaced with `product: Product`. When a client queries `review.product`, HotChocolate calls `GetProduct()`, reads the `ProductId` from the parent Review, and returns a `Product` stub with just that ID. The gateway then uses the Product lookup to fetch the full product data from whichever subgraph owns it.

### Test the Subgraph

Run the Reviews subgraph:

```bash
cd Reviews
dotnet run
```

Open `http://localhost:5002/graphql/` and try:

```graphql
query {
  reviews {
    id
    body
    stars
  }
}
```

You should see:

```json
{
  "data": {
    "reviews": [
      { "id": 1, "body": "Sturdy and well-built.", "stars": 5 },
      { "id": 2, "body": "A bit wobbly.", "stars": 3 },
      { "id": 3, "body": "Very comfortable!", "stars": 5 },
      { "id": 4, "body": "Good value for the price.", "stars": 4 }
    ]
  }
}
```

Now try a query that exercises the entity stub and `[BindMember]`:

```graphql
query {
  reviews {
    id
    body
    product {
      id
      reviews {
        body
      }
    }
  }
}
```

You should see each review with its product reference, and each product with its reviews. Notice that `product.name` and `product.price` are not available here -- those fields live in the Products subgraph and will only appear after composition in the gateway. Within the Reviews subgraph alone, `Product` only has the fields the Reviews subgraph contributes: `id` and `reviews`.

Stop the server with `Ctrl+C`.

### Export the Schema

Export the Reviews subgraph's schema. From the `fusion-getting-started` directory:

```bash
dotnet run ./Reviews -- schema export
```

This generates `schema.graphqls` and `schema-settings.json` in the Reviews project directory. Open `Reviews/schema-settings.json` and update the transport URL:

```json
{
  "name": "reviews",
  "transports": {
    "http": {
      "url": "http://localhost:5002/graphql"
    }
  }
}
```

### What You Built

Your Reviews subgraph now:

- Exposes a `reviews` query and a `reviewById` lookup
- Adds a `reviews` field to the `Product` type using an entity stub -- without duplicating any Product data
- Replaces the raw `productId` on reviews with a resolved `Product` reference via `[BindMember]`
- Provides an internal `productById` lookup that the gateway uses for cross-subgraph resolution
- Has an exported schema and settings file ready for composition

You now have two independent subgraphs that contribute to one shared graph. In the next section, you will compose them into a single composite schema.

## Compose the Schemas with Nitro CLI

You now have two subgraphs, each with an exported schema and a settings file. The next step is **composition** -- merging these source schemas into a single composite schema that the gateway will serve to clients.

Composition is handled by the Nitro CLI. It reads each subgraph's `.graphqls` schema file and its companion `-settings.json` file, validates that the schemas are compatible, and produces a **Fusion archive** (`.far` file) that contains everything the gateway needs: the composite schema, the execution schema with routing metadata, and the transport configuration for each subgraph.

### Verify Your File Structure

Before running composition, make sure your project looks like this:

```
fusion-getting-started/
├── Products/
│   ├── Product.cs
│   ├── ProductQueries.cs
│   ├── ProductRepository.cs
│   ├── Program.cs
│   ├── Products.csproj
│   ├── schema.graphqls          <-- exported schema
│   └── schema-settings.json     <-- subgraph settings
└── Reviews/
    ├── Review.cs
    ├── ReviewRepository.cs
    ├── ReviewQueries.cs
    ├── ProductQueries.cs
    ├── Product.cs
    ├── ProductNode.cs
    ├── Program.cs
    ├── Reviews.csproj
    ├── schema.graphqls          <-- exported schema
    └── schema-settings.json     <-- subgraph settings
```

Each subgraph directory has a `schema.graphqls` and a `schema-settings.json`. The Nitro CLI matches these files by their shared prefix (`schema`).

### Run Composition

From the `fusion-getting-started` directory, run:

```bash
nitro fusion compose \
  --source-schema-file Products/schema.graphqls \
  --source-schema-file Reviews/schema.graphqls \
  --archive gateway.far
```

On Windows (PowerShell or cmd), put everything on one line:

```bash
nitro fusion compose --source-schema-file Products/schema.graphqls --source-schema-file Reviews/schema.graphqls --archive gateway.far
```

If composition succeeds, you will see output similar to:

```
Validating source schemas...
Merging schemas...
Fusion archive created: gateway.far
```

The exact output may vary by Nitro CLI version, but you should see confirmation that the archive was created. The `gateway.far` file is a binary package containing the composed gateway configuration. You do not need to look inside this file -- the gateway reads it directly. You will pass it to the gateway in the next section.

Notice that composition validates your schemas at build time. If there were conflicts between subgraphs -- say, two subgraphs defining the same field without `[Shareable]` -- you would find out now, not when a user hits a broken query path in production. This is one of Fusion's key advantages.

### What Happens During Composition

The Nitro CLI performs three steps:

1. **Validate** each source schema individually -- are they valid GraphQL? Are the Fusion attributes used correctly?
2. **Merge** the source schemas -- combine types with the same name, verify that non-shared fields appear in only one subgraph, and resolve entity references.
3. **Produce** the composite schema -- the unified schema clients will query, plus the internal routing metadata the gateway needs to plan queries.

In this case, composition merges the `Product` type from the Products subgraph (with `id`, `name`, `price`) and the `Product` entity stub from the Reviews subgraph (with `reviews`). The result is one `Product` type with all four fields.

Lookup resolvers are also merged into the gateway configuration. The gateway knows it can resolve a Product by ID from the Products subgraph (for full product data) or from the Reviews subgraph (for the entity stub). The internal lookup in Reviews (marked `[Internal]`) is hidden from the composite schema but available to the gateway for query planning.

### Troubleshooting Composition Errors

If composition fails, the CLI reports specific errors. Common issues include:

- **"Field X is defined in multiple subgraphs without `[Shareable]`"** -- Two subgraphs define the same field on the same type. Either mark the field as `[Shareable]` in both subgraphs (if they return the same data), or remove the duplicate.
- **"No lookup found for entity X"** -- A subgraph references an entity type but no subgraph provides a lookup for it. Add a `[Lookup]` resolver.
- **"Entity X has no key fields"** -- An entity type is referenced across subgraphs but does not have a key field. Make sure your entity stub and main entity definition both have the same key field (usually `Id`).
- **"Schema file not found" or "settings file not found"** -- The `.graphqls` file does not have a matching `-settings.json` file in the same directory. Make sure they share the same prefix (e.g., `schema.graphqls` and `schema-settings.json`).

If you see errors, check that you exported the schemas after your latest code changes (`dotnet run ./<ProjectDir> -- schema export` from the solution root).

With composition complete, you now have a `gateway.far` file containing your composed graph. In the next section, you will create a gateway that loads this file and serves the unified schema to clients.

## Run the Fusion Gateway

The gateway is the service that clients connect to. It loads the composed configuration from the `.far` file, exposes the composite schema, and routes queries to the appropriate subgraphs at runtime.

### Create the Gateway Project

From the `fusion-getting-started` directory:

```bash
dotnet new web -n Gateway
cd Gateway

dotnet add package HotChocolate.Fusion.AspNetCore --version "16.0.0-p.11.2"

cd ..
```

Your `Gateway/Gateway.csproj` should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="HotChocolate.Fusion.AspNetCore" Version="16.0.0-p.11.2" />
  </ItemGroup>

</Project>
```

### Configure the Port

Edit `Gateway/Properties/launchSettings.json` and set the `applicationUrl` under the `http` profile to:

```json
"applicationUrl": "http://localhost:5000"
```

The gateway runs on port 5000. The subgraphs run on ports 5001 (Products) and 5002 (Reviews).

### Copy the Fusion Archive

Copy the `gateway.far` file you created during composition into the Gateway project directory:

```bash
cp gateway.far Gateway/gateway.far
```

On Windows:

```bash
copy gateway.far Gateway\gateway.far
```

### Configure the Gateway

Replace the contents of `Gateway/Program.cs` with:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient("fusion");

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

app.MapGraphQL();
app.Run();
```

Three things to notice:

- **`AddHttpClient("fusion")`** registers a named HTTP client called `"fusion"`. The gateway uses this client to send requests to the subgraphs. The name `"fusion"` is the default HTTP client name that Fusion uses when no explicit `clientName` is specified in the subgraph's `schema-settings.json`.
- **`AddGraphQLGateway()`** registers the Fusion gateway services. This is what makes this project a gateway rather than a regular GraphQL server.
- **`AddFileSystemConfiguration("./gateway.far")`** tells the gateway to load its composed configuration from a local file. In production, you would typically use `.AddNitro()` to download the configuration from the Nitro cloud, but for local development the file system approach is simpler.

### Start Everything

You need all three services running at the same time: both subgraphs and the gateway. Open three terminal windows and start the subgraphs first.

**Important:** Start the Products and Reviews subgraphs before the gateway. The gateway connects to each subgraph on startup and may log errors if they are not reachable.

**Terminal 1 -- Products subgraph:**

```bash
cd fusion-getting-started/Products
dotnet run
```

**Terminal 2 -- Reviews subgraph:**

```bash
cd fusion-getting-started/Reviews
dotnet run
```

**Terminal 3 -- Gateway:**

```bash
cd fusion-getting-started/Gateway
dotnet run
```

Wait for all three services to start. You should see output like this in each terminal:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

The port number will differ for each service (5001, 5002, and 5000). If you see errors like "Address already in use," another process is using that port -- either stop it or choose a different port in `launchSettings.json`.

### Verify the Gateway

Open your browser to `http://localhost:5000/graphql/` to access the Banana Cake Pop GraphQL IDE on the gateway. Try a simple query to verify the gateway is working:

```graphql
query {
  products {
    id
    name
    price
  }
}
```

You should see the same product data as when you queried the Products subgraph directly. The difference is that this query went through the gateway, which routed it to the Products subgraph behind the scenes. If you look at the terminal running the Products subgraph (Terminal 1), you should see a log entry showing it received and processed the request -- this confirms the gateway successfully routed the query.

### What You Built

Your gateway now:

- Loads the composed configuration from the `gateway.far` file
- Exposes the unified composite schema on port 5000
- Routes queries to the Products and Reviews subgraphs as needed
- Acts as the single entry point for clients -- they never talk to the subgraphs directly

In the next section, you will run queries that demonstrate the gateway coordinating data across both subgraphs in a single request.

## Query Across Subgraphs

This is the moment everything comes together. With all three services running (Products on 5001, Reviews on 5002, Gateway on 5000), you can now run a single query that fetches data from both subgraphs.

### The Cross-Subgraph Query

Open the Banana Cake Pop IDE at `http://localhost:5000/graphql/` and run this query:

```graphql
query {
  products {
    id
    name
    price
    reviews {
      body
      stars
    }
  }
}
```

You should see:

```json
{
  "data": {
    "products": [
      {
        "id": 1,
        "name": "Table",
        "price": 899.99,
        "reviews": [
          { "body": "Sturdy and well-built.", "stars": 5 },
          { "body": "A bit wobbly.", "stars": 3 }
        ]
      },
      {
        "id": 2,
        "name": "Couch",
        "price": 1299.5,
        "reviews": [
          { "body": "Very comfortable!", "stars": 5 }
        ]
      },
      {
        "id": 3,
        "name": "Chair",
        "price": 54,
        "reviews": [
          { "body": "Good value for the price.", "stars": 4 }
        ]
      }
    ]
  }
}
```

Look at what happened: `name` and `price` came from the Products subgraph, while `reviews` came from the Reviews subgraph. The client sent one query to one endpoint, and the gateway coordinated the rest.

### What the Gateway Did

Behind the scenes, the gateway executed a query plan with multiple steps:

1. **Fetched the products** from the Products subgraph -- this returned `id`, `name`, and `price` for each product.
2. **Resolved the reviews** from the Reviews subgraph -- using each product's `id`, the gateway called the Reviews subgraph's internal `productById` lookup to get a `Product` stub, then resolved the `reviews` field on each stub.
3. **Combined the results** into a single response that looks exactly like it came from one GraphQL server.

The client never knew that two separate services were involved. This is the core promise of Fusion: multiple subgraphs, one unified API.

### Try Another Query

You can also query in the other direction -- start from reviews and reach into product data:

```graphql
query {
  reviews {
    body
    stars
    product {
      name
      price
    }
  }
}
```

You should see:

```json
{
  "data": {
    "reviews": [
      {
        "body": "Sturdy and well-built.",
        "stars": 5,
        "product": { "name": "Table", "price": 899.99 }
      },
      {
        "body": "A bit wobbly.",
        "stars": 3,
        "product": { "name": "Table", "price": 899.99 }
      },
      {
        "body": "Very comfortable!",
        "stars": 5,
        "product": { "name": "Couch", "price": 1299.5 }
      },
      {
        "body": "Good value for the price.",
        "stars": 4,
        "product": { "name": "Chair", "price": 54 }
      }
    ]
  }
}
```

This time, `body` and `stars` came from the Reviews subgraph, while `product.name` and `product.price` came from the Products subgraph. The gateway resolved the product references by calling the Products subgraph's `productById` lookup with each review's product ID.

### Compare: Before and After Composition

Remember in the previous section, when you queried `review.product` on the Reviews subgraph directly? You could see `product.id` and `product.reviews`, but `product.name` and `product.price` were missing -- those fields did not exist in the Reviews subgraph.

Now, through the gateway, `product.name` and `product.price` are available. Composition merged the Product type from both subgraphs, and the gateway resolves each field from the subgraph that owns it. This is what it means for subgraphs to contribute to one shared graph.

### Lookup a Single Product

You can also look up a single product by ID:

```graphql
query {
  productById(id: 1) {
    name
    price
    reviews {
      body
      stars
    }
  }
}
```

This uses the `productById` lookup from the Products subgraph -- the public one that appears in the composite schema. The gateway then fetches reviews from the Reviews subgraph using its internal lookup. Public lookups serve as both client-facing query fields and gateway entity resolution entry points, while internal lookups are only used by the gateway behind the scenes.

### What You Accomplished

You proved that Fusion works:

- Clients send queries to one endpoint (the gateway)
- The gateway routes parts of each query to the appropriate subgraphs
- Multiple subgraphs contribute fields to the same types (`Product`, `Review`)
- Cross-subgraph entity resolution happens automatically through lookups
- The result is a unified API that feels like one service

The Products and Reviews subgraphs are completely independent -- they do not import each other's code, do not call each other directly, and can be deployed and scaled separately. Yet clients can query across both as if they were one unified schema.

## Sharing Fields with `[Shareable]`

In the tutorial so far, the Products and Reviews subgraphs each contribute **different** fields to the `Product` type -- Products owns `name` and `price`, while Reviews owns `reviews`. There is no overlap, so composition works without any special annotations.

But what happens when two subgraphs need to define the **same** field on the same type? By default, Fusion treats this as an error. If two subgraphs both define `Product.name`, composition fails because Fusion does not know which subgraph should be authoritative.

The `[Shareable]` attribute solves this. When you mark a field as shareable in both subgraphs, you are telling Fusion: "these definitions are intentional and return the same data -- use whichever is most efficient."

### When You Need It

A common scenario: your Reviews subgraph needs to display the product name alongside each review. You could always fetch the name from the Products subgraph via the gateway, but if performance matters, you might want the Reviews subgraph to cache product names locally. In that case, both subgraphs would define `Product.name`:

**Products subgraph:**

```csharp
public class Product
{
    public int Id { get; set; }

    [Shareable]
    public required string Name { get; set; }

    public double Price { get; set; }
}
```

**Reviews subgraph (entity stub):**

```csharp
public sealed record Product(int Id)
{
    [Shareable]
    public string Name => ProductRepository.GetProductName(Id);

    public List<Review> GetReviews()
        => ReviewRepository.GetByProductId(Id);
}
```

Both subgraphs define `Product.name` and mark it `[Shareable]`. Composition succeeds, and the gateway can resolve `name` from either subgraph depending on what else the query needs. If a query only asks for `product.name` and `product.reviews`, the gateway might fetch everything from the Reviews subgraph in a single call instead of making a separate trip to the Products subgraph.

### The Rule

Without `[Shareable]`, a non-key field must exist in exactly one subgraph. Key fields (like `id`) are automatically shareable -- you do not need the attribute on them. For any other field that appears in multiple subgraphs, add `[Shareable]` to **all** definitions of that field. If even one subgraph forgets to mark it, composition fails.

### When Not to Use It

Do not mark a field as shareable unless the subgraphs genuinely return the same data. If two subgraphs define `Product.name` but one returns the display name and the other returns an internal code name, marking them shareable would give clients inconsistent results depending on which subgraph the gateway happens to call.

If the fields return **different** data, they should have **different** names (e.g., `displayName` vs `codeName`) and each lives in its own subgraph without `[Shareable]`.

## What's Next

You now have a working Fusion setup: two subgraphs contributing to one composed graph, served through a single gateway. Here are some directions to explore next based on what you need:

- **I want to add another subgraph to this project** -- [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph)
- **I want to understand entities more deeply** -- [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)
- **I need to deploy this** -- [Deployment & CI/CD](/docs/fusion/v16/deployment-and-ci-cd)
- **I need to secure this** -- [Authentication and Authorization](/docs/fusion/v16/authentication-and-authorization)
- **I'm coming from Apollo** -- [Coming from Apollo Federation](/docs/fusion/v16/coming-from-apollo-federation)

**Other useful resources:**

- **Use `[Require]` for cross-subgraph data dependencies.** If a field resolver needs data that lives in another subgraph, use the `[Require]` attribute on a method argument to declare the dependency. For example, a Shipping subgraph calculating delivery estimates might need a product's weight from the Products subgraph:

  ```csharp
  public int GetDeliveryEstimate(
      string zip,
      [Require] int weight)
  {
      // weight is fetched from the Products subgraph automatically
      return CalculateShipping(zip, weight);
  }
  ```

  When the argument name matches the entity field name (like `weight` above), HotChocolate infers the mapping automatically, so you can use `[Require]` without parameters. If the names differ, specify the entity field explicitly: `[Require("weight")] int productWeight`. The gateway resolves the required fields from their owning subgraph before calling your resolver. The required arguments are hidden from the composite schema -- clients never see them.

- **You may see `[NodeResolver]` in demo code alongside `[Lookup]`.** This enables Relay-style global object identification (`node(id: ...)` queries). The fusion-demo uses it on every entity lookup. It is not required for basic Fusion setups.

- **Explore the fusion-demo repository.** The [ChilliCream fusion-demo](https://github.com/ChilliCream/fusion-demo) is a full production-style example with eight subgraphs, .NET Aspire orchestration, PostgreSQL databases, authentication, subscriptions, and CI/CD pipelines. It shows everything this guide simplified for learning purposes.
