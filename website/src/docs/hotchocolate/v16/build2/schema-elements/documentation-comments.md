---
title: "Documentation Comments"
---

Descriptions and deprecation reasons are schema metadata that consumers see in Nitro, IDE tools, code generation, and introspection. They help clients understand which fields to use, what arguments mean, and when to migrate away from deprecated elements.

```graphql
"A product available in the catalog."
type Product {
  "The display name shown to customers."
  name: String!

  "The number of units currently available for purchase."
  availableStock: Int!

  "The legacy stock field. Use availableStock instead."
  stock: Int! @deprecated(reason: "Use availableStock instead.")
}
```

You can add descriptions using XML documentation comments, `[GraphQLDescription]` attributes, or fluent `Description(...)` in code-first configuration. You can add deprecation reasons using `[GraphQLDeprecated]`, `[Obsolete]`, or fluent `.Deprecated(...)`.

This page shows you how to:

- Choose the right documentation source for your schema style
- Document types, fields, arguments, input fields, and enum values
- Add deprecation reasons to guide client migration
- Verify descriptions and deprecations in SDL and introspection

# Choose the right documentation source

Use the approach that fits your schema style and ownership model.

| Situation                                                                         | Recommended source                                   | Why                                                              |
| --------------------------------------------------------------------------------- | ---------------------------------------------------- | ---------------------------------------------------------------- |
| Implementation-first schema and the C# docs already speak to GraphQL clients      | XML `<summary>` and `<param>`                        | Keeps schema and source docs together.                           |
| Implementation-first schema and GraphQL wording differs from C# wording           | `[GraphQLDescription("...")]`                        | Makes schema wording explicit and stable.                        |
| Code-first type configuration owns the schema shape                               | Fluent `.Description("...")`                         | Keeps documentation beside descriptor configuration.             |
| The C# member is obsolete and GraphQL clients should also migrate                 | `[Obsolete("...")]`                                  | Shares lifecycle metadata with .NET callers and GraphQL clients. |
| Only the GraphQL field, argument, input field, or enum value should be deprecated | `[GraphQLDeprecated("...")]` or `.Deprecated("...")` | Avoids marking unrelated C# API surface obsolete.                |

# Add explicit descriptions with attributes

The `[GraphQLDescription]` attribute sets a description on any schema element. This is explicit schema metadata that does not depend on XML documentation file generation.

```csharp
using HotChocolate;

[GraphQLDescription("A product available in the catalog.")]
public class Product
{
    [GraphQLDescription("The display name shown to customers.")]
    public string Name { get; set; } = default!;

    [GraphQLDescription("The number of units currently available for purchase.")]
    public int AvailableStock { get; set; }
}

[QueryType]
public static partial class ProductQueries
{
    [GraphQLDescription("Finds a product by its stable catalog ID.")]
    public static Product? GetProduct(
        [GraphQLDescription("The stable catalog product ID.")] int id,
        ProductService products)
        => products.FindById(id);
}

[GraphQLDescription("The status of a refund request.")]
public enum RefundStatus
{
    [GraphQLDescription("The refund request is awaiting approval.")]
    Pending,

    [GraphQLDescription("The refund has been processed and funds returned.")]
    Completed,

    [GraphQLDescription("The refund request was denied or processing failed.")]
    Failed
}
```

Expected SDL:

```graphql
"A product available in the catalog."
type Product {
  "The display name shown to customers."
  name: String!

  "The number of units currently available for purchase."
  availableStock: Int!
}

type Query {
  "Finds a product by its stable catalog ID."
  product("The stable catalog product ID." id: Int!): Product
}

"The status of a refund request."
enum RefundStatus {
  "The refund request is awaiting approval."
  PENDING

  "The refund has been processed and funds returned."
  COMPLETED

  "The refund request was denied or processing failed."
  FAILED
}
```

The `[GraphQLDescription]` attribute:

- Accepts a single non-empty string. The constructor rejects null or empty values.
- Applies to classes, structs, interfaces, properties, methods, enums, parameters, and fields.
- Must be placed on the member Hot Chocolate exposes. If you ignore, rename, or replace a member with descriptor configuration, document the exposed member or the descriptor instead.

# Generate descriptions from XML documentation comments

Hot Chocolate can extract descriptions from standard C# XML documentation comments. This keeps your schema docs and C# docs together.

```csharp
/// <summary>
/// A product available in the catalog.
/// </summary>
public class Product
{
    /// <summary>
    /// The display name shown to customers.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// The number of units currently available for purchase.
    /// </summary>
    public int AvailableStock { get; set; }
}

[QueryType]
public static partial class ProductQueries
{
    /// <summary>
    /// Finds a product by its stable catalog ID.
    /// </summary>
    /// <param name="id">The stable catalog product ID.</param>
    public static Product? GetProduct(int id, ProductService products)
        => products.FindById(id);
}

/// <summary>
/// The status of a refund request.
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// The refund request is awaiting approval.
    /// </summary>
    Pending,

    /// <summary>
    /// The refund has been processed and funds returned.
    /// </summary>
    Completed,

    /// <summary>
    /// The refund request was denied or processing failed.
    /// </summary>
    Failed
}
```

Configure your project file to generate XML documentation:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

`<NoWarn>1591</NoWarn>` is optional. It suppresses warnings for members without XML comments.

XML documentation extraction is enabled by default. You can disable it globally:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options => options.UseXmlDocumentation = false);
```

XML summaries that flow into the schema should be written for GraphQL clients, not only for C# maintainers.

| Option                                  | Default                     | Where                    | Note                                                                                                     |
| --------------------------------------- | --------------------------- | ------------------------ | -------------------------------------------------------------------------------------------------------- |
| `UseXmlDocumentation`                   | `true`                      | `ModifyOptions`          | Turns XML documentation extraction on or off for schema descriptions.                                    |
| `ResolveXmlDocumentationFileName`       | `null`                      | `ModifyOptions`          | Use when XML documentation files are generated or deployed under custom names.                           |
| `ModuleOptions.DisableXmlDocumentation` | Not part of `SchemaOptions` | Source-generated modules | Disables XML doc extraction for source-generated types while preserving explicit `[GraphQLDescription]`. |

# Document arguments, input fields, and enum values

Arguments, input fields, and enum values are high-value documentation targets:

- Arguments clarify identifiers, filters, units, default behavior, paging semantics, and null handling.
- Input fields clarify mutation contracts.
- Enum value descriptions explain business states that names alone rarely capture.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static IEnumerable<Product> GetProducts(
        [GraphQLDescription("Filter products by brand ID. Omit to include all brands.")]
        int? brandId,
        ProductService products)
        => products.Search(brandId);
}

[GraphQLDescription("Filter criteria for product search.")]
public class ProductFilterInput
{
    [GraphQLDescription("Filter by brand ID. Null includes all brands.")]
    public int? BrandId { get; set; }

    [GraphQLDescription("Filter by product type ID. Null includes all types.")]
    public int? TypeId { get; set; }
}
```

Expected SDL:

```graphql
type Query {
  products(
    "Filter products by brand ID. Omit to include all brands."
    brandId: Int
  ): [Product!]!
}

"Filter criteria for product search."
input ProductFilterInput {
  "Filter by brand ID. Null includes all brands."
  brandId: Int

  "Filter by product type ID. Null includes all types."
  typeId: Int
}
```

**Gotcha:** Service parameters are not GraphQL arguments. XML `<param>` or `[GraphQLDescription]` on injected services will not appear as argument descriptions.

# Add descriptions in code-first type configuration

Fluent code-first descriptors provide a `Description(...)` method for all schema elements.

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Description("A product available in the catalog.");

        descriptor
            .Field(p => p.Name)
            .Description("The display name shown to customers.");

        descriptor
            .Field(p => p.AvailableStock)
            .Description("The number of units currently available for purchase.");
    }
}

public class ProductQueryType : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Argument("id", a => a
                .Type<NonNullType<IntType>>()
                .Description("The stable catalog product ID."))
            .Description("Finds a product by its stable catalog ID.")
            .Resolve(context =>
            {
                var id = context.ArgumentValue<int>("id");
                var products = context.Service<ProductService>();
                return products.FindById(id);
            });
    }
}

public class ProductFilterInputType : InputObjectType<ProductFilterInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<ProductFilterInput> descriptor)
    {
        descriptor
            .Field(f => f.BrandId)
            .Description("Filter by brand ID. Null includes all brands.");
    }
}

public class RefundStatusType : EnumType<RefundStatus>
{
    protected override void Configure(IEnumTypeDescriptor<RefundStatus> descriptor)
    {
        descriptor.Description("The status of a refund request.");

        descriptor
            .Value(RefundStatus.Pending)
            .Description("The refund request is awaiting approval.");

        descriptor
            .Value(RefundStatus.Completed)
            .Description("The refund has been processed and funds returned.");

        descriptor
            .Value(RefundStatus.Failed)
            .Description("The refund request was denied or processing failed.");
    }
}
```

Expected SDL:

```graphql
"A product available in the catalog."
type Product {
  "The display name shown to customers."
  name: String!

  "The number of units currently available for purchase."
  availableStock: Int!
}

type Query {
  "Finds a product by its stable catalog ID."
  product("The stable catalog product ID." id: Int!): Product
}

input ProductFilterInput {
  "Filter by brand ID. Null includes all brands."
  brandId: Int
}

"The status of a refund request."
enum RefundStatus {
  "The refund request is awaiting approval."
  PENDING

  "The refund has been processed and funds returned."
  COMPLETED

  "The refund request was denied or processing failed."
  FAILED
}
```

**Precedence:** Fluent `Description(...)` in code-first configuration takes precedence over attributes and XML docs. This applies even when the provided value is null or empty. Do not use empty strings as a fallback to XML docs.

# Deprecate schema elements with reasons

Descriptions answer what an element means. Deprecation reasons answer whether clients should still use it and where they should migrate.

```csharp
[QueryType]
public static partial class ProductQueries
{
    [GraphQLDeprecated("Use product(id:) instead.")]
    public static Product? GetProductBySku(string sku, ProductService products)
        => products.FindBySku(sku);
}

public class Product
{
    public string Name { get; set; } = default!;

    [Obsolete("Use availableStock instead.")]
    public int Stock => AvailableStock;

    public int AvailableStock { get; set; }
}

public enum RefundStatus
{
    Pending,
    Completed,

    [GraphQLDeprecated("No longer used. Failed refunds are logged separately.")]
    Failed
}
```

Expected SDL:

```graphql
type Query {
  productBySku(sku: String!): Product
    @deprecated(reason: "Use product(id:) instead.")
}

type Product {
  name: String!
  stock: Int! @deprecated(reason: "Use availableStock instead.")
  availableStock: Int!
}

enum RefundStatus {
  PENDING
  COMPLETED
  FAILED
    @deprecated(reason: "No longer used. Failed refunds are logged separately.")
}
```

Fluent code-first deprecation:

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field(p => p.Stock)
            .Deprecated("Use availableStock instead.");
    }
}

public class ProductQueryType : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("productBySku")
            .Argument("sku", a => a.Type<NonNullType<StringType>>())
            .Deprecated("Use product(id:) instead.")
            .Resolve(context =>
            {
                var sku = context.ArgumentValue<string>("sku");
                var products = context.Service<ProductService>();
                return products.FindBySku(sku);
            });
    }
}

public class RefundStatusType : EnumType<RefundStatus>
{
    protected override void Configure(IEnumTypeDescriptor<RefundStatus> descriptor)
    {
        descriptor
            .Value(RefundStatus.Failed)
            .Deprecated("No longer used. Failed refunds are logged separately.");
    }
}
```

You can also call `.Deprecated()` without a reason. This produces a default GraphQL deprecation message. For public APIs, provide an explicit migration reason.

**Constraints:**

- `[GraphQLDeprecated]` constructor rejects null or empty reasons.
- Attribute targets are field, property, parameter, and method.
- Output fields, input fields, arguments, and enum values can be deprecated.
- Non-null arguments and input fields without default values cannot be deprecated. Make them optional with a safe default first, or use a different migration path.

See the [schema evolution](/docs/hotchocolate/v16/guides/schema-evolution) guide for deprecation policy and removal workflow.

# Understand precedence and fallback

When multiple documentation sources are present, Hot Chocolate uses this precedence:

| Source                                                  | Applies to                                                   | Priority                               | Notes                                                                                                |
| ------------------------------------------------------- | ------------------------------------------------------------ | -------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| Fluent `Description(...)`                               | Code-first descriptors                                       | Highest                                | Takes precedence even for null or empty values.                                                      |
| `[GraphQLDescription("...")]`                           | Implementation-first types, members, parameters, enum values | Explicit implementation-first metadata | Constructor rejects null or empty values, so XML fallback only matters when the attribute is absent. |
| XML `<summary>`                                         | Types, members, enum values, resolver methods                | Fallback                               | Requires XML doc file generation and `UseXmlDocumentation = true`.                                   |
| XML `<param>`                                           | Resolver parameters exposed as GraphQL arguments             | Fallback                               | The `name` must match the C# parameter.                                                              |
| `[GraphQLDeprecated]`, `[Obsolete]`, `.Deprecated(...)` | Field, argument, input field, enum value deprecation         | Separate lifecycle metadata            | Produces `@deprecated` and introspection deprecation fields, not a description.                      |

**Custom naming conventions:** If a custom `INamingConventions` implementation replaces default conventions without carrying forward `IDocumentationProvider`, XML descriptions may not be resolved. Current docs show passing an `IDocumentationProvider` and constructing an `XmlDocumentationProvider` with `XmlDocumentationFileResolver` and `ObjectPool<StringBuilder>`. See the [API reference for custom attributes](/docs/hotchocolate/v16/api-reference/custom-attributes) for more details.

# Verify generated SDL documentation

## Download SDL from the server

For endpoints configured with `MapGraphQL()`, append `?sdl` to the GraphQL endpoint URL:

```
https://localhost:5000/graphql?sdl
```

Or download the static schema file:

```
https://localhost:5000/graphql/schema.graphql
```

If using a dedicated schema endpoint, use `MapGraphQLSchema()` and the configured route.

Endpoint controls: `EnableSchemaRequests` and `EnableSchemaFileSupport`. See the [server endpoints](/docs/hotchocolate/v16/server/endpoints) documentation for configuration details.

## Export SDL from the command line

Export the schema to a file:

```shell
dotnet run -- schema export --output schema.graphql
```

| Option          | Purpose                                                       |
| --------------- | ------------------------------------------------------------- |
| `--output`      | Writes SDL to a file instead of stdout.                       |
| `--schema-name` | Exports a named schema when the app has more than one schema. |

See the [command-line documentation](/docs/hotchocolate/v16/server/command-line) for more export options.

You can also export the schema on startup:

```csharp
builder
    .AddGraphQL()
    .ExportSchemaOnStartup("./schema.graphql");
```

## Inspect SDL in tests

Print the schema in tests:

```csharp
var schema = await new ServiceCollection()
    .AddGraphQL()
    .AddQueryType<ProductQueryType>()
    .BuildSchemaAsync();

var sdl = schema.ToString();
```

Or with formatting:

```csharp
var sdl = schema.ToSyntaxNode().Print(indented: true);
```

Use schema snapshots to catch regressions in descriptions and deprecation reasons. See the [testing guide](/docs/hotchocolate/v16/guides/testing) for snapshot testing with CookieCrumble.

# Verify introspection metadata

Introspection queries can verify descriptions and deprecation metadata at runtime.

Query type and field descriptions:

```graphql
{
  __type(name: "Product") {
    description
    fields(includeDeprecated: true) {
      name
      description
      isDeprecated
      deprecationReason
      args {
        name
        description
        defaultValue
      }
    }
  }
}
```

Query enum value descriptions and deprecations:

```graphql
{
  __type(name: "RefundStatus") {
    enumValues(includeDeprecated: true) {
      name
      description
      isDeprecated
      deprecationReason
    }
  }
}
```

**Note:** Disabling introspection prevents expensive recursive introspection queries, but it does not hide descriptions or the schema contract. SDL downloads may still be available depending on endpoint configuration. See the [introspection security](/docs/hotchocolate/v16/securing-your-api/introspection) documentation for controls such as `AllowIntrospection(false)`.

# Troubleshooting missing or wrong descriptions

| Symptom                                                 | Likely cause                                                                                             | Fix                                                                            |
| ------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| XML summaries do not appear in SDL                      | XML documentation file is not generated                                                                  | Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and rebuild. |
| XML summaries work locally but not in deployment        | XML file is not available under the expected runtime name or path                                        | Check output publishing and `ResolveXmlDocumentationFileName`.                 |
| XML docs never appear                                   | `UseXmlDocumentation` is `false`                                                                         | Remove the override or set it to `true`.                                       |
| Source-generated type descriptions are missing          | `ModuleOptions.DisableXmlDocumentation` is set                                                           | Remove the flag or add explicit `[GraphQLDescription]`.                        |
| Attribute description is ignored                        | Fluent descriptor configuration overrides it                                                             | Put the description in the descriptor or remove the override.                  |
| Argument description is missing                         | The parameter is a service, or `<param name="...">` does not match the resolver parameter                | Document only exposed GraphQL arguments and match the C# parameter name.       |
| Enum value description is missing                       | The XML comment or attribute is on the enum type, not the enum field                                     | Document each enum value separately.                                           |
| Deprecated fields do not show in an introspection query | The query omitted `includeDeprecated: true`                                                              | Add `includeDeprecated: true` for fields or enum values.                       |
| Deprecating an argument fails                           | The argument is non-null and has no default value                                                        | Make it optional with a safe default first, or use a different migration path. |
| Description disappears after custom naming changes      | A custom naming convention replaced the default path without carrying forward the documentation provider | Pass or reuse `IDocumentationProvider` as shown in the current docs.           |

# Practical wording checklist

- Prefer client-facing wording.
  - Good: "The stable catalog product ID."
  - Avoid: "Primary key from the Products table."
- Say what an argument identifies or filters.
- Say whether units, time zones, currencies, or null behavior matter.
- For deprecations, always name the replacement and migration action.
- Keep descriptions concise. Use external docs for long workflows.
- Avoid documenting implementation details, data store names, and resolver internals unless they are part of the public contract.

# Next steps

- [Object types](/docs/hotchocolate/v16/build2/schema-elements/object-types) for defining fields and object members
- [Arguments](/docs/hotchocolate/v16/build2/schema-elements/arguments) for argument binding
- [Input object types](/docs/hotchocolate/v16/build2/schema-elements/input-object-types) for input field modeling
- [Enums](/docs/hotchocolate/v16/build2/schema-elements/enums) for enum modeling
- [Schema evolution](/docs/hotchocolate/v16/guides/schema-evolution) for deprecation policy and removal workflow
- [Server endpoints](/docs/hotchocolate/v16/server/endpoints) for SDL download and endpoint configuration
- [Command-line tools](/docs/hotchocolate/v16/server/command-line) for schema export
- [Testing](/docs/hotchocolate/v16/guides/testing) for schema snapshots
- [Introspection security](/docs/hotchocolate/v16/securing-your-api/introspection) for introspection controls
- [Custom attributes API](/docs/hotchocolate/v16/api-reference/custom-attributes) for attribute reference
- [Options API](/docs/hotchocolate/v16/api-reference/options) for schema options reference
