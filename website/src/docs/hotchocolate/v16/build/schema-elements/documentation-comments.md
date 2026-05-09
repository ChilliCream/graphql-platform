---
title: "Documentation Comments"
---

Descriptions and deprecation reasons are important schema metadata that appear in Nitro, IDE tools, code generation, and introspection. They help clients understand which fields to use, clarify argument meanings, and indicate when to migrate away from deprecated elements.

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

You can provide descriptions using XML documentation comments, `[GraphQLDescription]` attributes, or the fluent `Description(...)` method in code-first configuration. Deprecation reasons can be added with `[GraphQLDeprecated]`, `[Obsolete]`, or fluent `.Deprecated(...)`.

This page will help you:

- Select the best documentation source for your schema style
- Document types, fields, arguments, input fields, and enum values
- Add deprecation reasons to guide client migration
- Verify descriptions and deprecations in SDL and introspection

# Choosing the right documentation source

Select the approach that matches your schema style and how you manage ownership.

| Situation                                                                         | Recommended source                                   | Reason                                                           |
| --------------------------------------------------------------------------------- | ---------------------------------------------------- | ---------------------------------------------------------------- |
| Implementation-first schema and the C# docs already speak to GraphQL clients      | XML `<summary>` and `<param>`                        | Keeps schema and source docs together.                           |
| Implementation-first schema and GraphQL wording differs from C# wording           | `[GraphQLDescription("...")]`                        | Makes schema wording explicit and stable.                        |
| Code-first type configuration owns the schema shape                               | Fluent `.Description("...")`                         | Keeps documentation next to descriptor configuration.            |
| The C# member is obsolete and GraphQL clients should also migrate                 | `[Obsolete("...")]`                                  | Shares lifecycle metadata with .NET callers and GraphQL clients. |
| Only the GraphQL field, argument, input field, or enum value should be deprecated | `[GraphQLDeprecated("...")]` or `.Deprecated("...")` | Avoids marking unrelated C# API surface obsolete.                |

# Adding explicit descriptions with attributes

The `[GraphQLDescription]` attribute allows you to set a description on any schema element. This provides explicit schema metadata and does not rely on XML documentation file generation.

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

Expected SDL output:

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

- Requires a single non-empty string. The constructor does not allow null or empty values.
- Can be applied to classes, structs, interfaces, properties, methods, enums, parameters, and fields.
- Must be placed on the member that Hot Chocolate exposes. If you ignore, rename, or replace a member with descriptor configuration, document the exposed member or the descriptor instead.

# Generating descriptions from XML documentation comments

Hot Chocolate can extract descriptions from standard C# XML documentation comments. This approach keeps your schema documentation and C# documentation in sync.

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

To enable XML documentation, configure your project file as follows:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

`<NoWarn>1591</NoWarn>` is optional. It suppresses warnings for members without XML comments.

XML documentation extraction is enabled by default. You can disable it globally if needed:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(options => options.UseXmlDocumentation = false);
```

When writing XML summaries that will appear in the schema, use language intended for GraphQL clients, not only for C# maintainers.

| Option                                  | Default                     | Where                    | Note                                                                                                     |
| --------------------------------------- | --------------------------- | ------------------------ | -------------------------------------------------------------------------------------------------------- |
| `UseXmlDocumentation`                   | `true`                      | `ModifyOptions`          | Enables or disables XML documentation extraction for schema descriptions.                                |
| `ResolveXmlDocumentationFileName`       | `null`                      | `ModifyOptions`          | Use when XML documentation files are generated or deployed under custom names.                           |
| `ModuleOptions.DisableXmlDocumentation` | Not part of `SchemaOptions` | Source-generated modules | Disables XML doc extraction for source-generated types while preserving explicit `[GraphQLDescription]`. |

# Documenting arguments, input fields, and enum values

Arguments, input fields, and enum values are especially important to document:

- Arguments clarify identifiers, filters, units, default behavior, paging, and null handling.
- Input fields define mutation contracts.
- Enum value descriptions explain business states that names alone may not capture.

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

Expected SDL output:

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

**Note:** Service parameters are not GraphQL arguments. XML `<param>` or `[GraphQLDescription]` on injected services will not appear as argument descriptions.

# Adding descriptions in code-first type configuration

Fluent code-first descriptors offer a `Description(...)` method for all schema elements.

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

**Precedence:** Fluent `Description(...)` in code-first configuration overrides attributes and XML documentation. This is true even if the value is null or empty. Avoid using empty strings as a fallback to XML documentation.

# Deprecating schema elements with reasons

Descriptions explain what an element means. Deprecation reasons tell clients whether they should continue using an element and where to migrate instead.

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

Expected SDL output:

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

You can also call `.Deprecated()` without a reason, which produces a default GraphQL deprecation message. For public APIs, always provide an explicit migration reason.

**Constraints:**

- The `[GraphQLDeprecated]` constructor does not allow null or empty reasons.
- The attribute can be applied to fields, properties, parameters, and methods.
- Output fields, input fields, arguments, and enum values can be deprecated.
- Non-null arguments and input fields without default values cannot be deprecated. Make them optional with a safe default first, or use a different migration path.

See the [schema evolution](/docs/hotchocolate/v16/_leagcy/guides/schema-evolution) guide for deprecation policy and removal workflow.

# Precedence and fallback for documentation sources

When multiple documentation sources are present, Hot Chocolate uses the following order:

| Source                                                  | Applies to                                                   | Priority                               | Notes                                                                                                     |
| ------------------------------------------------------- | ------------------------------------------------------------ | -------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Fluent `Description(...)`                               | Code-first descriptors                                       | Highest                                | Overrides all others, even for null or empty values.                                                      |
| `[GraphQLDescription("...")]`                           | Implementation-first types, members, parameters, enum values | Explicit implementation-first metadata | Constructor does not allow null or empty values, so XML fallback only applies if the attribute is absent. |
| XML `<summary>`                                         | Types, members, enum values, resolver methods                | Fallback                               | Requires XML doc file generation and `UseXmlDocumentation = true`.                                        |
| XML `<param>`                                           | Resolver parameters exposed as GraphQL arguments             | Fallback                               | The `name` must match the C# parameter.                                                                   |
| `[GraphQLDeprecated]`, `[Obsolete]`, `.Deprecated(...)` | Field, argument, input field, enum value deprecation         | Separate lifecycle metadata            | Produces `@deprecated` and introspection deprecation fields, not a description.                           |

**Custom naming conventions:** If you use a custom `INamingConventions` implementation that does not carry forward `IDocumentationProvider`, XML descriptions may not be resolved. The documentation shows how to pass an `IDocumentationProvider` and construct an `XmlDocumentationProvider` with `XmlDocumentationFileResolver` and `ObjectPool<StringBuilder>`. See the [API reference for custom attributes](/docs/hotchocolate/v16/build/attributes/custom-descriptor-attributes) for more details.

# Verifying generated SDL documentation

## Downloading SDL from the server

For endpoints configured with `MapGraphQL()`, you can append `?sdl` to the GraphQL endpoint URL:

```
https://localhost:5000/graphql?sdl
```

Alternatively, download the static schema file:

```
https://localhost:5000/graphql/schema.graphql
```

If you use a dedicated schema endpoint, use `MapGraphQLSchema()` and the configured route.

Endpoint controls include `EnableSchemaRequests` and `EnableSchemaFileSupport`. See the [server endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints) documentation for configuration details.

## Exporting SDL from the command line

Export the schema to a file:

```shell
dotnet run -- schema export --output schema.graphql
```

| Option          | Purpose                                                       |
| --------------- | ------------------------------------------------------------- |
| `--output`      | Writes SDL to a file instead of stdout.                       |
| `--schema-name` | Exports a named schema when the app has more than one schema. |

See the [command-line documentation](/docs/hotchocolate/v16/build/server-configuration/command-line) for more export options.

You can also export the schema on startup:

```csharp
builder
    .AddGraphQL()
    .ExportSchemaOnStartup("./schema.graphql");
```

## Inspecting SDL in tests

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

Use schema snapshots to catch regressions in descriptions and deprecation reasons. See the [testing guide](/docs/hotchocolate/v16/_leagcy/guides/testing) for snapshot testing with CookieCrumble.

# Verifying introspection metadata

You can use introspection queries to check descriptions and deprecation metadata at runtime.

To query type and field descriptions:

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

To query enum value descriptions and deprecations:

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

**Note:** Disabling introspection prevents expensive recursive introspection queries, but does not hide descriptions or the schema contract. SDL downloads may still be available depending on endpoint configuration. See the [introspection security](/docs/hotchocolate/v16/build/security/introspection) documentation for controls such as `AllowIntrospection(false)`.

# Troubleshooting missing or incorrect descriptions

| Symptom                                                 | Likely cause                                                                                             | Solution                                                                       |
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

- Use client-facing language.
  - Good: "The stable catalog product ID."
  - Avoid: "Primary key from the Products table."
- State what an argument identifies or filters.
- Indicate if units, time zones, currencies, or null behavior are important.
- For deprecations, always specify the replacement and migration action.
- Keep descriptions concise. Refer to external documentation for long workflows.
- Avoid documenting implementation details, data store names, or resolver internals unless they are part of the public contract.

# Next steps

- [Object types](/docs/hotchocolate/v16/build/schema-elements/object-types) for defining fields and object members
- [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments) for argument binding
- [Input object types](/docs/hotchocolate/v16/build/schema-elements/input-object-types) for input field modeling
- [Enums](/docs/hotchocolate/v16/build/schema-elements/enums) for enum modeling
- [Schema evolution](/docs/hotchocolate/v16/_leagcy/guides/schema-evolution) for deprecation policy and removal workflow
- [Server endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints) for SDL download and endpoint configuration
- [Command-line tools](/docs/hotchocolate/v16/build/server-configuration/command-line) for schema export
- [Testing](/docs/hotchocolate/v16/_leagcy/guides/testing) for schema snapshots
- [Introspection security](/docs/hotchocolate/v16/build/security/introspection) for introspection controls
- [Custom attributes API](/docs/hotchocolate/v16/build/attributes/custom-descriptor-attributes) for attribute reference
- [Options API](/docs/hotchocolate/v16/build/server-configuration/schema-options) for schema options reference
