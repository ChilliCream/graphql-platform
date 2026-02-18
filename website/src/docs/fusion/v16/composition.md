---
title: "Composition"
---

# Composition: Build-Time Schema Validation

Composition merges your subgraph schemas into a single gateway configuration -- catching conflicts at build time, not at 3 AM.

When you run `nitro fusion compose`, the Nitro CLI reads each subgraph's exported schema, validates that they are compatible, merges them into a unified composite schema, and produces a Fusion archive (`.far` file) that the gateway loads at startup. If anything is wrong -- conflicting types, missing lookups, incompatible field definitions -- you find out now, in your terminal or CI pipeline, before any code reaches production.

This page explains what composition does, how it merges different type kinds, how to control what appears in the composite schema, and how to fix the errors you will inevitably encounter.

## What Composition Does

Composition is a three-phase process that transforms multiple source schemas into one gateway configuration.

### Phase 1: Validate

Each source schema is validated in isolation. The composition engine checks:

- The schema is valid GraphQL according to the base specification.
- All Fusion directives and attributes are used correctly (e.g., `@external` fields are referenced by `@key` or `@provides`).
- Key fields reference valid scalar or object types.
- Lookup fields have the correct argument-to-entity-field mappings.

If any source schema fails validation, composition stops and reports the specific error.

### Phase 2: Merge

Types with the same name across subgraphs are merged according to type-specific rules (detailed in the next section). The composition engine:

- Combines fields from different subgraphs into unified types.
- Verifies that non-key, non-shareable fields appear in only one subgraph.
- Resolves entity references and lookup routing.
- Applies visibility controls (`@inaccessible`, `@internal`).

### Phase 3: Produce

After merging, the composition engine performs post-merge validation and produces:

- The **composite schema** -- the client-facing schema that the gateway exposes.
- The **execution schema** -- an internal schema annotated with routing metadata that tells the gateway which subgraph owns each field and how to resolve entity references.
- The **Fusion archive** (`.far` file) -- a binary package containing both schemas plus the transport configuration for each subgraph.

Post-merge validation checks that the composed result is internally consistent:

- All object and interface types have at least one accessible field.
- All enums and unions have at least one accessible member.
- All interface implementations are accessible.
- Required input fields are not marked `@inaccessible`.
- All type references resolve to accessible types.

## Merging Rules

When two or more subgraphs define a type with the same name, composition merges them according to the type kind. Understanding these rules helps you design schemas that compose cleanly.

### Objects

Object type fields from all subgraphs are combined into one type. The key rule: **non-key, non-shareable fields must appear in exactly one subgraph.**

If two subgraphs both define `Product.name` without marking it `[Shareable]`, composition fails. Key fields (fields referenced by `@key` or used in `[Lookup]` arguments) are automatically shareable and do not need the attribute.

```graphql
# Products subgraph
type Product @key(fields: "id") {
  id: ID!
  name: String!
  price: Float!
}

# Reviews subgraph
type Product @key(fields: "id") {
  id: ID!
  reviews: [Review!]!
}

# Composed result
type Product {
  id: ID!
  name: String!
  price: Float!
  reviews: [Review!]!
}
```

The `id` field appears in both subgraphs without conflict because it is a key field. The `name`, `price`, and `reviews` fields each come from one subgraph, so no `[Shareable]` is needed.

If both subgraphs need to resolve the same field (for example, both cache `Product.name`), mark it `[Shareable]` in **all** subgraphs that define it:

```csharp
// Products subgraph
public class Product
{
    public int Id { get; set; }

    [Shareable]
    public required string Name { get; set; }

    public double Price { get; set; }
}

// Reviews subgraph (entity stub)
public sealed record Product(int Id)
{
    [Shareable]
    public string Name => GetCachedProductName(Id);
}
```

### Enums

Enum values must be consistent across subgraphs, but the rules differ depending on how the enum is used.

**Output enums** (enums returned by fields): values are merged as a **union**. If subgraph A defines `OrderStatus { PENDING, SHIPPED }` and subgraph B defines `OrderStatus { PENDING, DELIVERED }`, the composed enum is `OrderStatus { PENDING, SHIPPED, DELIVERED }`. This is safe because each subgraph only returns the values it knows about.

**Input enums** (enums used as arguments or input field types): values are merged as an **intersection**. Since any subgraph receiving the enum as input must understand all values, only the values defined in every subgraph survive. If subgraph A defines `SortOrder { ASC, DESC, RELEVANCE }` and subgraph B defines `SortOrder { ASC, DESC }`, the composed input enum is `SortOrder { ASC, DESC }`.

**Enums used as both input and output**: the intersection rule (most restrictive) applies, because the enum must be valid in both directions.

Values marked `@inaccessible` in a subgraph are excluded from the merge for that subgraph. If all definitions of a value are inaccessible, the value does not appear in the composite schema but can still be used internally.

If merging results in an empty enum (no values survive), composition fails.

### Input Objects

Input object fields across subgraphs are reconciled:

- Fields that appear in multiple subgraphs must have compatible types.
- Default values must match across subgraphs. If subgraph A defines `limit: Int = 10` and subgraph B defines `limit: Int = 20`, composition fails.
- A required field (`name: String!`) in one subgraph and an optional field (`name: String`) in another are merged as **required** (most restrictive), because the subgraph that requires it must always receive a value.

### Interfaces

Interfaces are merged like objects -- fields from all subgraphs are combined. After merging, every type that implements the interface must satisfy the merged interface definition. If the merged interface has fields `name` and `email`, every implementing type must provide both fields (potentially across subgraphs).

### Unions

Union member types from all subgraphs are combined. If subgraph A defines `SearchResult = Product | Review` and subgraph B defines `SearchResult = Product | User`, the composed union is `SearchResult = Product | Review | User`. The union must remain non-empty after merge.

### Scalars

Custom scalars with the same name must be semantically compatible across subgraphs. Built-in scalars (`String`, `Int`, `Float`, `Boolean`, `ID`) merge trivially. For custom scalars (like `DateTime` or `Decimal`), ensure all subgraphs use the same definition.

## Nullable and Non-Nullable Merging

When the same field appears in multiple subgraphs with different nullability, the merge result depends on the field's position.

### Output Fields (Least Restrictive)

For output fields -- fields returned by resolvers -- the merged type uses the **least restrictive** nullability. Nullable wins:

| Subgraph A      | Subgraph B      | Composed Result |
| --------------- | --------------- | --------------- |
| `name: String!` | `name: String!` | `name: String!` |
| `name: String!` | `name: String`  | `name: String`  |
| `name: String`  | `name: String`  | `name: String`  |

Why? If one subgraph says a field can be null, the gateway must account for that possibility. The composed schema reflects the reality that the field might be null from at least one source.

### Arguments and Input Fields (Most Restrictive)

For arguments and input object fields -- values provided by clients -- the merged type uses the **most restrictive** nullability. Non-nullable wins:

| Subgraph A    | Subgraph B    | Composed Result |
| ------------- | ------------- | --------------- |
| `limit: Int!` | `limit: Int!` | `limit: Int!`   |
| `limit: Int!` | `limit: Int`  | `limit: Int!`   |
| `limit: Int`  | `limit: Int`  | `limit: Int`    |

Why? If one subgraph requires a non-nullable argument, the gateway must always provide it. Clients must supply a value that satisfies the strictest subgraph.

## Visibility Controls

Not everything in your subgraph schemas should appear in the composite schema. Fusion provides three mechanisms for controlling what clients see.

### `@inaccessible` / `[Inaccessible]`

Hides a type or field from the client-facing composite schema. The element still exists in the execution schema and can be used internally -- for example, as a source for `[Require]` field dependencies.

```csharp
// Products subgraph
public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public double Price { get; set; }

    [Inaccessible]
    public int InternalSkuCode { get; set; }
}
```

In this example, `InternalSkuCode` does not appear in the composite schema. Clients cannot query it. But it can be used by other subgraphs via `[Require]` -- for example, a Warehouse subgraph could require the SKU code for inventory lookups without exposing it to clients.

The equivalent in GraphQL SDL:

```graphql
type Product @key(fields: "id") {
  id: ID!
  name: String!
  price: Float!
  internalSkuCode: Int! @inaccessible
}
```

**Constraints:** You cannot mark a required input field as `@inaccessible` -- if a client must provide a value, they need to see the field. Composition fails if you try.

### `@internal` / `[Internal]`

Declares that a type or field is local to a subgraph and does not participate in standard schema merging. Internal elements do not appear in the composite schema and do not collide with identically-named elements in other subgraphs.

The most common use is on lookup fields that exist only for gateway entity resolution:

```csharp
// Reviews subgraph
[QueryType]
public static partial class ProductQueries
{
    [Lookup, Internal]
    public static Product GetProductById(int id)
        => new(id);
}
```

This lookup is available to the gateway for resolving `Product` entity references within the Reviews subgraph, but clients cannot call it. Without `[Internal]`, this lookup would appear in the composite schema as a second `productById` query, which would conflict with the Products subgraph's public `productById` lookup.

The difference from `@inaccessible`: internal elements are completely invisible to the merging process. Two subgraphs can define `[Internal]` fields with the same name and different types without causing a conflict. `@inaccessible` elements still participate in merging -- they just get removed from the final client-facing schema.

### `[Tag]` for Composition Filtering

Tags allow you to label fields and types for organizational purposes and selectively exclude them during composition using the `--exclude-tag` flag.

```csharp
// Products subgraph
[QueryType]
public static partial class ProductQueries
{
    [Tag("team-products")]
    [Lookup, NodeResolver]
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);

    [Tag("experimental")]
    public static List<Product> GetRecommendations(int productId)
        => RecommendationEngine.GetRecommendations(productId);
}
```

To compose without the experimental features:

```bash
nitro fusion compose \
  --source-schema-file Products/schema.graphqls \
  --source-schema-file Reviews/schema.graphqls \
  --archive gateway.far \
  --exclude-tag experimental
```

The `GetRecommendations` field is excluded from the composed schema. This is useful for:

- Hiding features that are not ready for production.
- Creating different compositions for different environments (dev includes experimental features, production does not).
- Organizing fields by team ownership for filtering.

## The `.far` Archive Format

The Fusion archive (`.far` file) is the output of composition and the input to the gateway. It is a binary package containing:

- **The composite schema** -- the unified, client-facing GraphQL schema.
- **The execution schema** -- the internal schema annotated with routing directives that tell the gateway which subgraph owns each field, how to resolve entity references, and what transport to use.
- **Transport configuration** -- the URL and HTTP client configuration for each subgraph, derived from the `schema-settings.json` files.

The gateway loads the `.far` file at startup:

```csharp
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");
```

Or, for production deployments, the gateway downloads its configuration from Nitro cloud:

```csharp
builder
    .AddGraphQLGateway()
    .AddNitro();
```

You do not need to inspect the contents of the `.far` file directly. The gateway reads it and configures itself automatically. If you need to verify what is inside, you can run `nitro fusion compose` with the `--include-satisfiability-paths` flag to get detailed diagnostic output about the composed schema.

## Running Composition

### Local Composition with the Nitro CLI

The most common composition workflow during development:

```bash
nitro fusion compose \
  --source-schema-file Products/schema.graphqls \
  --source-schema-file Reviews/schema.graphqls \
  --archive gateway.far
```

If you do not specify `--source-schema-file`, the CLI scans the working directory for all `.graphqls` files and their companion `-settings.json` files.

#### Watch Mode

For a faster development loop, use `--watch` to recompose automatically when schema files change:

```bash
nitro fusion compose \
  --source-schema-file Products/schema.graphqls \
  --source-schema-file Reviews/schema.graphqls \
  --archive gateway.far \
  --watch
```

#### Environment Selection

Each subgraph's `schema-settings.json` can define multiple environments with different URLs. Use `--environment` to select which environment to use during composition:

```bash
nitro fusion compose \
  --source-schema-file Products/schema.graphqls \
  --source-schema-file Reviews/schema.graphqls \
  --archive gateway.far \
  --environment dev
```

This resolves the `{{API_URL}}` template variable in the subgraph's settings file using the `dev` environment:

```json
{
  "name": "products-api",
  "transports": {
    "http": {
      "url": "{{API_URL}}"
    }
  },
  "environments": {
    "dev": {
      "API_URL": "https://products.dev.example.com/graphql"
    },
    "prod": {
      "API_URL": "https://products.example.com/graphql"
    }
  }
}
```

### Composition with Aspire

If you use .NET Aspire for local development, composition happens automatically on build. The Aspire AppHost orchestrates schema extraction and composition:

```csharp
// AppHost/Program.cs
builder.AddGraphQLOrchestrator();

var productsApi = builder
    .AddProject<Projects.Products>("products-api")
    .WithGraphQLSchemaEndpoint();

var reviewsApi = builder
    .AddProject<Projects.Reviews>("reviews-api")
    .WithGraphQLSchemaEndpoint();

builder
    .AddProject<Projects.Gateway>("gateway-api")
    .WithGraphQLSchemaComposition(
        settings: new GraphQLCompositionSettings
        {
            EnableGlobalObjectIdentification = true,
            EnvironmentName = "aspire"
        })
    .WithReference(productsApi)
    .WithReference(reviewsApi);
```

With this setup, every time you build the AppHost, Aspire extracts schemas from each subgraph and composes them into the gateway configuration. No manual `nitro fusion compose` step needed.

### Composition in CI

For production deployments, run composition as part of your CI pipeline. Use `nitro fusion validate` to catch breaking changes before deployment:

```bash
nitro fusion validate \
  --source-schema-file Products/schema.graphqls \
  --stage production \
  --api-id $NITRO_API_ID \
  --api-key $NITRO_API_KEY
```

This validates that the source schema composes successfully with the other schemas already published to the specified stage. See [Deployment & CI/CD](/docs/fusion/v16/deployment-and-ci-cd) for the full pipeline setup.

You can also run composition in Nitro cloud as part of your schema delivery pipeline.

## Common Composition Errors

These are the errors you will encounter most often, with examples showing what went wrong and how to fix it.

### Field Defined in Multiple Subgraphs Without `[Shareable]`

**Error:** `Field "Product.name" is defined in multiple subgraphs without @shareable.`

**What went wrong:** Two subgraphs define the same field on the same type, and neither marks it `[Shareable]`.

```csharp
// Products subgraph
public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }  // NOT shareable
}

// Reviews subgraph
public sealed record Product(int Id)
{
    public string Name => GetCachedName(Id);  // NOT shareable -- conflict!
}
```

**Fix:** Either mark the field `[Shareable]` in **all** subgraphs that define it, or remove the duplicate definition from one subgraph:

```csharp
// Option 1: Mark as shareable in both subgraphs
[Shareable]
public required string Name { get; set; }

// Option 2: Remove the duplicate from the Reviews subgraph
// and let the gateway fetch it from Products
```

### No Lookup Found for Entity

**Error:** `No lookup found for entity "Product".`

**What went wrong:** A subgraph references `Product` as an entity (via a stub or entity key), but no subgraph provides a `[Lookup]` resolver that can resolve it by key.

```csharp
// Reviews subgraph has a Product stub...
public sealed record Product(int Id);

// ...but no subgraph has a [Lookup] for Product!
```

**Fix:** Add a `[Lookup]` resolver to the subgraph that owns the entity:

```csharp
// Products subgraph
[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static Product? GetProductById(int id)
        => ProductRepository.GetById(id);
}
```

Or, if the subgraph only uses the entity as a reference (like the Reviews subgraph), add an internal lookup:

```csharp
// Reviews subgraph
[QueryType]
public static partial class ProductQueries
{
    [Lookup, Internal]
    public static Product GetProductById(int id)
        => new(id);
}
```

### Enum Value Mismatch

**Error:** `Enum value "CANCELLED" is defined in subgraph "Orders" but not in subgraph "Payments". The enum "OrderStatus" is used as an input type and requires consistent values across all defining subgraphs.`

**What went wrong:** An enum used as an input type has different values across subgraphs. Since clients can send any value from the composed enum, every subgraph that accepts it as input must understand all values.

```graphql
# Orders subgraph
enum OrderStatus {
  PENDING
  SHIPPED
  DELIVERED
  CANCELLED
}

# Payments subgraph (used as input)
enum OrderStatus {
  PENDING
  SHIPPED
  DELIVERED
}
# Missing CANCELLED!
```

**Fix:** Add the missing values to all subgraphs that use the enum as input, or mark values that should not be in the composite schema as `@inaccessible`:

```graphql
# Orders subgraph
enum OrderStatus {
  PENDING
  SHIPPED
  DELIVERED
  CANCELLED @inaccessible
}
```

### Interface Implementation Missing

**Error:** `Type "DigitalProduct" implements interface "Purchasable" in subgraph "Products" but does not provide field "refundPolicy" required by the merged interface.`

**What went wrong:** An interface was extended with new fields in one subgraph, but a type implementing that interface in another subgraph does not have the new field.

```graphql
# Products subgraph
interface Purchasable {
  price: Float!
}

type DigitalProduct implements Purchasable {
  price: Float!
}

# Payments subgraph
interface Purchasable {
  price: Float!
  refundPolicy: String! # New field added here
}
```

After merging, the `Purchasable` interface has both `price` and `refundPolicy`. But `DigitalProduct` in the Products subgraph only has `price`.

**Fix:** Add the missing field to the implementing type in the appropriate subgraph, or mark the new interface field as `@inaccessible` if it should not be in the composite schema.

### Required Input Field Marked `@inaccessible`

**Error:** `Required input field "CreateProductInput.name" is marked @inaccessible. Required input fields cannot be hidden from the composite schema.`

**What went wrong:** A non-nullable input field was marked `@inaccessible`. Since clients must provide a value for required input fields, hiding the field makes it impossible for clients to use the input type.

**Fix:** Either remove `@inaccessible` from the required field, or make the field optional (`String` instead of `String!`) so clients do not need to provide it.

### Schema File Not Found

**Error:** `Settings file not found for schema file "Products/schema.graphqls". Expected "Products/schema-settings.json".`

**What went wrong:** The `.graphqls` file does not have a matching `-settings.json` file. The Nitro CLI expects both files to share the same prefix in the same directory.

**Fix:** Ensure both files exist and share the same prefix:

```text
Products/
├── schema.graphqls
└── schema-settings.json
```

If you renamed files, make sure the names match. Run `dotnet run -- schema export` to regenerate both files.

## `schema-settings.json` Reference

Each subgraph's `schema-settings.json` configures how the subgraph participates in composition. The primary documentation lives on the [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph) page. Here is a quick reference of the fields relevant to composition:

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
    "dev": {
      "API_URL": "https://products.dev.example.com/graphql"
    },
    "prod": {
      "API_URL": "https://products.example.com/graphql"
    }
  }
}
```

- **`name`** -- The unique identifier for this subgraph in the composed graph. Must be unique across all subgraphs.
- **`transports.http.clientName`** -- The named HTTP client the gateway uses to reach this subgraph. Defaults to `"fusion"`.
- **`transports.http.url`** -- The subgraph's GraphQL endpoint URL. Supports `{{VARIABLE}}` template syntax, resolved from the active environment.
- **`environments`** -- Per-environment variable substitutions. Selected via `--environment` during composition or `EnvironmentName` in Aspire.

## Next Steps

Where to go from here depends on what you are working on:

- **"I need to understand entities and lookups more deeply"** -- [Entities and Lookups](/docs/fusion/v16/entities-and-lookups) covers entity stubs, public vs. internal lookups, `@is` for argument mapping, batch lookups, and field ownership in detail.
- **"I need to add a new subgraph to my project"** -- [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph) walks through creating a subgraph that extends existing entity types, including the `schema-settings.json` setup.
- **"I need cross-subgraph field dependencies"** -- The `[Require]` attribute enables resolvers to depend on data from other subgraphs. Cross-subgraph data dependencies will be covered in future documentation.
- **"I'm ready to deploy"** -- [Deployment & CI/CD](/docs/fusion/v16/deployment-and-ci-cd) covers independent subgraph deployment, Nitro cloud schema delivery, and CI pipeline setup.
- **"Something is broken"** -- Review the common composition errors section above, or check the [Nitro CLI Reference](/docs/fusion/v16/nitro-cli-reference) for command details.
