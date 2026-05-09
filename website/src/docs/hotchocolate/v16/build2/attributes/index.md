---
title: "Attributes"
---

Attributes let you configure a Hot Chocolate schema near the C# type, member, or parameter that needs the configuration. Use them when the rule is static, local, and short enough to read at the declaration.

This page is the overview for the v16 attributes section. It teaches the mental model, shows how common attributes fit together, and routes you to the focused pages for each attribute. It is not a complete reference for every Hot Chocolate attribute.

# Configure a field with attributes

The following implementation-first resolver uses attributes to rename a field and add data middleware:

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [GraphQLName("products")]
    [UsePaging(MaxPageSize = 100)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetCatalogProducts(ProductStore store)
    {
        return store.Products;
    }
}

public sealed class Product
{
    public int Id { get; set; }

    public string Sku { get; set; } = default!;

    public string Name { get; set; } = default!;

    public decimal Price { get; set; }
}

public sealed class ProductAuditEntry
{
    public int Id { get; set; }

    public string Message { get; set; } = default!;
}

public sealed class ProductStore
{
    private static readonly Product[] s_products =
    [
        new() { Id = 1, Sku = "banana", Name = "Banana", Price = 1.20m },
        new() { Id = 2, Sku = "coffee", Name = "Coffee", Price = 9.99m }
    ];

    private static readonly ProductAuditEntry[] s_auditEntries =
    [
        new() { Id = 1, Message = "Price changed" }
    ];

    public IQueryable<Product> Products => s_products.AsQueryable();

    public IQueryable<ProductAuditEntry> ProductAuditEntries => s_auditEntries.AsQueryable();

    public IEnumerable<Product> FeaturedProducts => s_products;

    public Task<Product?> FindByIdAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult(s_products.SingleOrDefault(t => t.Id == id));
    }
}
```

Clients see the configured GraphQL field, not the C# method name:

```graphql
type Query {
  products(
    first: Int
    after: String
    last: Int
    before: String
    where: ProductFilterInput
    order: [ProductSortInput!]
  ): ProductsConnection
}
```

The attributes are read while Hot Chocolate builds the schema. Some attributes, such as `[GraphQLName]`, change schema metadata. Others, such as `[UsePaging]`, add field middleware that changes execution behavior.

# What attributes can configure

| Category                     | What it changes                                                                                                        | Examples in this section                                                                    |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| Schema metadata              | Names, descriptions, directives, type metadata, and cost metadata in the generated schema.                             | `[GraphQLName]`, `[Cost]`, `[ListSize]`                                                     |
| Field middleware             | Runtime behavior around field execution. Middleware can page, project, filter, sort, authorize, or apply custom logic. | `[UsePaging]`, `[UseProjection]`, `[UseFiltering]`, `[UseSorting]`, `[Authorize]`           |
| Resolver parameter binding   | Where a resolver parameter value comes from. This is related to attributes, but it is a separate resolver task.        | `[Parent]`, `[Service]`, `[GlobalState]`, `[ScopedState]`, `[LocalState]`, `[EventMessage]` |
| Custom descriptor attributes | Reusable attributes that package descriptor configuration behind a domain-specific name.                               | `ObjectFieldDescriptorAttribute`, `DescriptorAttribute`                                     |

Resolver parameter attributes do not add schema middleware. They bind method parameters to values from the GraphQL request, dependency injection, parent object, request state, or subscription event. See [Resolver Parameter Attributes](../resolvers/parameter-attributes) when your main question is where a resolver argument comes from.

# Choose attributes or descriptors

Hot Chocolate also exposes fluent descriptor APIs. Attributes and descriptors can often express the same schema rule. Choose the form that keeps the schema clear for your team.

| Question                  | Attributes                                                                           | Fluent descriptors                                                         |
| ------------------------- | ------------------------------------------------------------------------------------ | -------------------------------------------------------------------------- |
| Best for                  | Static, local configuration on one type, member, or parameter.                       | Longer, conditional, shared, or convention-based configuration.            |
| Where configuration lives | At the C# declaration.                                                               | In an `ObjectType`, type extension, or central schema configuration class. |
| Complex options           | Good when the attribute remains short, for example `[UsePaging(MaxPageSize = 100)]`. | Better when options need lambdas, helper methods, or repeated setup.       |
| Reviewability             | Makes important local rules visible, for example `[Authorize]` on a sensitive field. | Helps teams review schema policy in one place.                             |
| Reuse                     | Use a custom descriptor attribute when the repeated annotation has a domain name.    | Use helper methods, conventions, or shared type classes.                   |

For example, these two field configurations express the same data middleware stack:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UsePaging(MaxPageSize = 100)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(ProductStore store)
    {
        return store.Products;
    }
}
```

```csharp
using HotChocolate.Types;

public sealed class ProductQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("products")
            .Resolve(context => context.Service<ProductStore>().Products)
            .UsePaging(options => options.MaxPageSize = 100)
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}
```

Start with attributes when they make the declaration clearer. Move to descriptors when a resolver signature becomes hard to scan or when a rule belongs in shared schema policy.

# Use the attribute mental model

Attributes participate in schema building before any GraphQL request executes:

```text
C# attribute
  -> descriptor configuration
  -> GraphQL schema field or type
  -> execution middleware pipeline, when the attribute adds middleware
```

A descriptor attribute configures the same descriptor objects that fluent APIs configure. `Use*` attributes commonly add field middleware. Middleware wraps resolver execution, so the order of middleware attributes can affect the result.

If you create your own middleware attribute, preserve ordering by accepting the generated `order` value and assigning it to `Order`. See [Custom descriptor attributes](./custom-descriptor-attributes) for the full pattern.

# Keep data middleware in the required order

When you combine the data attributes, apply them from top to bottom in this order:

1. `[UsePaging]`
2. `[UseProjection]`
3. `[UseFiltering]`
4. `[UseSorting]`

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products;
}
```

Think of the stack as a data pipeline:

```text
paging -> projection -> filtering -> sorting -> resolver result
```

Paging defines the list or connection shape. Projection selects the fields needed from the data source. Filtering and sorting compose client-driven query operations on that source.

The v16 analyzer checks method declarations for the known data attributes and reports ordering problems. It cannot prevent every ordering issue on properties or custom descriptor attributes. If you see an ordering warning, reorder the attributes as shown and verify that projections, filtering, and sorting are registered on the schema.

# Common attributes in this section

| Attribute                    | Package or namespace                                      | Primary targets                                                                                                 | What it changes                                                           | Details                                                        |
| ---------------------------- | --------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- | -------------------------------------------------------------- |
| `[Authorize]`                | `HotChocolate.Authorization`                              | Object types and object fields                                                                                  | Requires authentication, roles, or policies.                              | [Authorize](./authorize)                                       |
| `[Cost]`                     | `HotChocolate.CostAnalysis.Types`                         | Supported schema elements such as object fields, object types, arguments, enum types, input fields, and scalars | Adds `@cost` weight metadata for static cost analysis.                    | [Cost](./cost)                                                 |
| `[GraphQLName]`              | `HotChocolate`                                            | Types, members, enum values, and parameters                                                                     | Overrides the inferred GraphQL name.                                      | [GraphQLName](./graphqlname)                                   |
| `[ListSize]`                 | `HotChocolate.CostAnalysis.Types`                         | Object fields                                                                                                   | Adds `@listSize` metadata so cost analysis can estimate list cardinality. | [ListSize](./listsize)                                         |
| `[UseFiltering]`             | `HotChocolate.Data`                                       | Resolver methods and properties that become fields                                                              | Adds a `where` argument and filtering middleware.                         | [UseFiltering](./usefiltering)                                 |
| `[UsePaging]`                | `HotChocolate.Types`                                      | Resolver methods and properties that become fields                                                              | Adds cursor pagination and connection or list paging behavior.            | [UsePaging](./usepaging)                                       |
| `[UseProjection]`            | `HotChocolate.Data`                                       | Object fields                                                                                                   | Projects requested fields into the data source.                           | [UseProjection](./useprojection)                               |
| `[UseSorting]`               | `HotChocolate.Data`                                       | Resolver methods and properties that become fields                                                              | Adds an `order` argument and sorting middleware.                          | [UseSorting](./usesorting)                                     |
| Custom descriptor attributes | `HotChocolate.Types` and `HotChocolate.Types.Descriptors` | Depends on the descriptor base class                                                                            | Packages descriptor configuration into a reusable attribute.              | [Custom descriptor attributes](./custom-descriptor-attributes) |

Looking for another attribute?

- Root operation attributes such as `[QueryType]`, `[MutationType]`, and `[SubscriptionType]` belong with schema building and operation root types.
- Resolver parameter attributes such as `[Parent]`, `[Service]`, and state attributes belong with [resolver parameter binding](../resolvers/parameter-attributes).
- Subscription topic and event attributes belong with subscriptions.
- Relay and node attributes belong with Relay and global object identification.
- Fusion and federation attributes belong with the Fusion and federation docs.
- DataLoader source generator attributes belong with DataLoader docs.

# Rename schema elements with `[GraphQLName]`

Use `[GraphQLName]` when the public GraphQL name should differ from the C# symbol name.

```csharp
using HotChocolate;

[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProduct(
        [GraphQLName("sku")] string stockKeepingUnit,
        ProductStore products)
    {
        return products.Products.SingleOrDefault(t => t.Sku == stockKeepingUnit);
    }
}
```

Expected SDL:

```graphql
type Query {
  product(sku: String!): Product
}
```

Use `[GraphQLName]` for naming. Use resolver parameter binding attributes, such as `[Argument]`, when the main question is where the value comes from.

# Protect fields with `[Authorize]`

Use `HotChocolate.Authorization.AuthorizeAttribute`, not the ASP.NET Core MVC authorization attribute, to authorize GraphQL types and fields.

```csharp
using HotChocolate.Authorization;

[QueryType]
public static partial class AdminQueries
{
    [Authorize(Roles = ["Administrator"])]
    public static IQueryable<ProductAuditEntry> GetProductAuditLog(ProductStore store)
    {
        return store.ProductAuditEntries;
    }
}
```

Register GraphQL authorization in your schema setup and configure ASP.NET Core authentication and authorization in the application pipeline. The attribute expresses the GraphQL policy. The server setup enforces it at runtime.

# Add cost metadata with `[Cost]` and `[ListSize]`

Cost analysis uses field weights and list-size estimates to reject overly expensive operations before execution.

```csharp
using HotChocolate.CostAnalysis.Types;

[QueryType]
public static partial class ProductQueries
{
    [Cost(100)]
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        ProductStore products,
        CancellationToken cancellationToken)
    {
        return await products.FindByIdAsync(id, cancellationToken);
    }

    [ListSize(
        AssumedSize = 100,
        SlicingArguments = ["first", "last"],
        SizedFields = ["edges", "nodes"],
        RequireOneSlicingArgument = false)]
    public static IEnumerable<Product> GetFeaturedProducts(ProductStore store)
    {
        return store.FeaturedProducts;
    }
}
```

The generated schema can include cost directives like this:

```graphql
type Query {
  productById(id: Int!): Product @cost(weight: "100")

  featuredProducts(first: Int, last: Int): [Product!]
    @listSize(
      assumedSize: 100
      slicingArguments: ["first", "last"]
      sizedFields: ["edges", "nodes"]
      requireOneSlicingArgument: false
    )
}
```

Use `[Cost]` to assign weight. Use `[ListSize]` to describe how many items a list field can return.

# Create custom descriptor attributes for repeated rules

Create a custom descriptor attribute when several fields repeat the same descriptor configuration and the rule has a meaningful name in your domain.

```csharp
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

public sealed class UseTenantAttribute : ObjectFieldDescriptorAttribute
{
    public UseTenantAttribute([CallerLineNumber] int order = 0)
    {
        Order = order;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
    {
        descriptor.Use(next => async resolverContext =>
        {
            var tenantId = resolverContext.ContextData["tenantId"];

            resolverContext.LocalContextData =
                resolverContext.LocalContextData.SetItem("tenantId", tenantId);

            await next(resolverContext);
        });
    }
}
```

Use a specific base class, such as `ObjectFieldDescriptorAttribute`, when the attribute targets one descriptor kind. Use the lower-level `DescriptorAttribute` when one attribute must handle multiple descriptor kinds.

# Troubleshoot attribute issues

## Filtering, sorting, projection, or paging does not behave as expected

- Register the matching schema feature, for example `.AddProjections()`, `.AddFiltering()`, and `.AddSorting()`.
- Check that the resolver returns a supported shape, such as `IQueryable<T>`, `IEnumerable<T>`, or `Connection<T>` for the attribute you applied.
- Reorder data attributes to `[UsePaging]`, `[UseProjection]`, `[UseFiltering]`, `[UseSorting]`.
- If you use `QueryContext<T>`, do not combine it with `[UseProjection]` on the same field.

## Authorization does not run

- Confirm that you imported `HotChocolate.Authorization`.
- Register GraphQL authorization with the schema.
- Configure ASP.NET Core authentication and authorization middleware in the correct application order.
- Verify policy and role names.

## Projection returns default values

- Ensure projected properties have public setters.
- Avoid custom resolvers for fields that must be projected into the database, or split the projected field from the custom resolved field.

## An attribute has no effect

- Confirm that the package reference and `using` directive match the attribute.
- Confirm that the attribute supports the target where you placed it.
- Confirm that the type or member is part of the schema being built.
- Move complex or conditional configuration to a descriptor when the attribute form hides the actual rule.

# Next steps

Read the focused pages when you need options, package setup, or edge cases:

- [Authorize](./authorize)
- [Cost](./cost)
- [Custom descriptor attributes](./custom-descriptor-attributes)
- [GraphQLName](./graphqlname)
- [ListSize](./listsize)
- [UseFiltering](./usefiltering)
- [UsePaging](./usepaging)
- [UseProjection](./useprojection)
- [UseSorting](./usesorting)

For adjacent topics, continue with [Schema Elements](../schema-elements), [Resolvers](../resolvers), [Resolver Parameter Attributes](../resolvers/parameter-attributes), [Field Middleware](/docs/hotchocolate/v16/execution-engine/field-middleware), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization), and [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).
