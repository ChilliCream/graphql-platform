---
title: "Schema Evolution"
---

GraphQL schemas evolve. New fields get added, old ones get retired. Unlike REST APIs with URL versioning, GraphQL schemas use additive changes and deprecation to manage that lifecycle. This page covers the tools Hot Chocolate provides for evolving your schema without breaking clients.

# Document Your Schema

Descriptions are the first line of defense against breaking changes. When every field has a clear description, consumers understand what they depend on and can adapt when you announce deprecations.

Hot Chocolate generates descriptions from standard C# XML documentation comments. Enable the XML documentation file in your `.csproj`:

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

The `<NoWarn>` element suppresses compiler warnings for types without documentation comments. With this in place, XML `<summary>` tags on your types and properties become GraphQL descriptions automatically.

```csharp
// Types/Product.cs
/// <summary>
/// A product available in the catalog.
/// </summary>
public class Product
{
    /// <summary>
    /// The unique product identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The display name shown to customers.
    /// </summary>
    public string Name { get; set; }
}
```

For cases where the C# summary does not work well as a GraphQL description, use the `[GraphQLDescription]` attribute. This takes precedence over XML docs.

<ExampleTabs>
<Implementation>

```csharp
// Types/Product.cs
public class Product
{
    public int Id { get; set; }

    [GraphQLDescription("The display name shown to customers.")]
    public string Name { get; set; }

    [GraphQLDescription("The current price in the seller's default currency.")]
    public decimal Price { get; set; }
}
```

</Implementation>
<Code>

```csharp
// Types/ProductType.cs
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Description("A product available in the catalog.");

        descriptor
            .Field(f => f.Name)
            .Description("The display name shown to customers.");

        descriptor
            .Field(f => f.Price)
            .Description("The current price in the seller's default currency.");
    }
}
```

</Code>
</ExampleTabs>

Descriptions appear in introspection results and in tools like [Nitro](/products/nitro). The more descriptive your schema, the fewer support questions you receive when fields change.

[Learn more about schema documentation](/docs/hotchocolate/v16/building-a-schema/documentation)

# Deprecate Fields Instead of Removing Them

When a field is no longer the recommended way to access data, deprecate it. Deprecated fields remain functional, but introspection marks them with the `@deprecated` directive so tools can warn consumers.

<ExampleTabs>
<Implementation>

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    [GraphQLDeprecated("Use `productById` instead.")]
    public static Product? GetProduct(int id, CatalogService catalog)
        => catalog.GetById(id);

    public static Product? GetProductById(int id, CatalogService catalog)
        => catalog.GetById(id);
}
```

The .NET `[Obsolete("reason")]` attribute works the same way as `[GraphQLDeprecated("reason")]`. If your field is also obsolete from the C# perspective, `[Obsolete]` covers both.

</Implementation>
<Code>

```csharp
// Types/ProductQueriesType.cs
public class ProductQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Deprecated("Use `productById` instead.")
            .Argument("id", a => a.Type<NonNullType<IntType>>())
            .Resolve(context =>
            {
                // ...
            });

        descriptor
            .Field("productById")
            .Argument("id", a => a.Type<NonNullType<IntType>>())
            .Resolve(context =>
            {
                // ...
            });
    }
}
```

</Code>
</ExampleTabs>

The resulting SDL includes the `@deprecated` directive:

```graphql
type Query {
  product(id: Int!): Product @deprecated(reason: "Use `productById` instead.")
  productById(id: Int!): Product
}
```

You can deprecate output fields, input fields, arguments, and enum values. Keep deprecated fields for at least one release cycle so consumers have time to migrate. When you are confident no consumers depend on a deprecated field, remove it.

> Warning: You cannot deprecate non-null arguments or input fields that have no default value. Deprecating a required field would silently break queries that depend on it. Add a default value first, then apply the deprecation.

[Learn more about deprecation](/docs/hotchocolate/v16/building-a-schema/versioning)

# Opt-In Features with @requiresOptIn

While `@deprecated` marks fields that are going away, `@requiresOptIn` marks fields that are not yet stable. This is useful for rolling out experimental features where consumers should make a deliberate choice to use them.

Fields marked with `@requiresOptIn` are hidden from introspection by default. Consumers opt in by specifying the feature name in their introspection queries.

## Enable Opt-In Features

Opt-in support is disabled by default. Enable it in your schema options:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.EnableOptInFeatures = true);
```

## Mark Fields as Opt-In

<ExampleTabs>
<Implementation>

```csharp
// Types/Product.cs
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }

    [RequiresOptIn("experimentalRecommendations")]
    public List<Product>? Recommendations { get; set; }
}
```

</Implementation>
<Code>

```csharp
// Types/ProductType.cs
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field(f => f.Recommendations)
            .RequiresOptIn("experimentalRecommendations");
    }
}
```

</Code>
</ExampleTabs>

Consumers discover opt-in fields by passing the `includeOptIn` argument in introspection:

```graphql
{
  __type(name: "Product") {
    fields(includeOptIn: ["experimentalRecommendations"]) {
      name
      requiresOptIn
    }
  }
}
```

## Declare Feature Stability

You can declare the stability level of each opt-in feature so consumers understand whether it is experimental, preview, or something else.

<ExampleTabs>
<Implementation>

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .OptInFeatureStability("experimentalRecommendations", "experimental");
```

</Implementation>
<Code>

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .SetSchema(s => s
        .OptInFeatureStability("experimentalRecommendations", "experimental"));
```

</Code>
</ExampleTabs>

Consumers query feature stability through introspection:

```graphql
{
  __schema {
    optInFeatureStability {
      feature
      stability
    }
  }
}
```

[Learn more about opt-in features](/docs/hotchocolate/v16/building-a-schema/versioning)

# Additive Changes Are Safe

Not all changes are equal. Some changes are safe for every consumer, while others can break existing queries.

**Non-breaking changes:**

- Adding a new field to an existing type
- Adding a new type
- Adding a new argument with a default value
- Adding a new enum value (for output enums)

**Breaking changes:**

- Removing a field
- Renaming a field
- Changing a field's return type
- Adding a required argument (one without a default value)
- Removing an enum value

The general principle: if an existing, valid query could fail or return different data after the change, the change is breaking. Additive changes expand what clients can query without affecting what they already query.

When you need to make a breaking change, follow this sequence:

1. Add the new field or type alongside the old one.
2. Deprecate the old field with a clear reason pointing to the replacement.
3. Wait for consumers to migrate (monitor usage if possible).
4. Remove the deprecated field.

# Troubleshooting

## Deprecated field still returned in queries

Deprecation does not remove a field. The field continues to work normally. Deprecation marks the field in introspection so tools can warn consumers. Remove the field from the schema when you are confident no consumers depend on it.

## Opt-in field not visible in introspection

Fields with `@requiresOptIn` are hidden by default. Use the `includeOptIn` argument in introspection queries to reveal them. Also verify that `EnableOptInFeatures = true` is set in your schema options.

## Descriptions not appearing in schema

Verify that `GenerateDocumentationFile` is set to `true` in your `.csproj`. Without this setting, the XML file is not generated and Hot Chocolate has no documentation to read.

# Next Steps

- **Schema documentation reference:** [Documentation](/docs/hotchocolate/v16/building-a-schema/documentation) covers `[GraphQLDescription]`, XML docs, and priority order.
- **Versioning reference:** [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning) covers `@deprecated`, `@requiresOptIn`, and feature stability in full detail.
- **Building a public API:** [Public API Guide](/docs/hotchocolate/v16/guides/public-api) covers cost analysis, pagination, and authorization for APIs consumed by external developers.
