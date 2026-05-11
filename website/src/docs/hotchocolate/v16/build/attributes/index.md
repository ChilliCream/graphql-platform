---
title: "Attributes"
---

Attributes allow you to configure a Hot Chocolate schema directly on the C# type, member, or parameter that requires configuration. They are best used for static, local rules that are concise enough to be understood at the declaration site.

This page provides an overview of attributes in Hot Chocolate. It explains the core concepts, demonstrates how common attributes work together, and links to detailed pages for each attribute. This is not an exhaustive reference for every attribute.

# Configuring a Field with Attributes

Here is an example of an implementation-first resolver that uses attributes to rename a field and add data middleware:

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

From the client perspective, the GraphQL field appears as configured, not as the C# method name:

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

Hot Chocolate reads these attributes while building the schema. Some, like `[GraphQLName]`, modify schema metadata. Others, such as `[UsePaging]`, add field middleware that changes how fields execute.

# What Attributes Can Configure

| Category                     | What it changes                                                                                                        | Examples in this section                                                                    |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| Schema metadata              | Names, descriptions, directives, type metadata, and cost metadata in the generated schema.                             | `[GraphQLName]`, `[Cost]`, `[ListSize]`                                                     |
| Field middleware             | Runtime behavior around field execution. Middleware can page, project, filter, sort, authorize, or apply custom logic. | `[UsePaging]`, `[UseProjection]`, `[UseFiltering]`, `[UseSorting]`, `[Authorize]`           |
| Resolver parameter binding   | Where a resolver parameter value comes from. This is related to attributes, but it is a separate resolver task.        | `[Parent]`, `[Service]`, `[GlobalState]`, `[ScopedState]`, `[LocalState]`, `[EventMessage]` |
| Custom descriptor attributes | Reusable attributes that package descriptor configuration behind a domain-specific name.                               | `ObjectFieldDescriptorAttribute`, `DescriptorAttribute`                                     |

Resolver parameter attributes do not add schema middleware. Instead, they bind method parameters to values from the GraphQL request, dependency injection, parent object, request state, or subscription event. If your main concern is where a resolver argument comes from, see [Resolver Parameter Attributes](../resolvers/parameter-attributes).

# Choosing Between Attributes and Descriptors

Hot Chocolate also provides fluent descriptor APIs. Both attributes and descriptors can often express the same schema rule. Choose the approach that keeps your schema clear and maintainable for your team.

| Question                  | Attributes                                                                           | Fluent descriptors                                                         |
| ------------------------- | ------------------------------------------------------------------------------------ | -------------------------------------------------------------------------- |
| Best for                  | Static, local configuration on one type, member, or parameter.                       | Longer, conditional, shared, or convention-based configuration.            |
| Where configuration lives | At the C# declaration.                                                               | In an `ObjectType`, type extension, or central schema configuration class. |
| Complex options           | Good when the attribute remains short, for example `[UsePaging(MaxPageSize = 100)]`. | Better when options need lambdas, helper methods, or repeated setup.       |
| Reviewability             | Makes important local rules visible, for example `[Authorize]` on a sensitive field. | Helps teams review schema policy in one place.                             |
| Reuse                     | Use a custom descriptor attribute when the repeated annotation has a domain name.    | Use helper methods, conventions, or shared type classes.                   |

For example, both of the following field configurations create the same data middleware stack:

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

Begin with attributes when they make the declaration clearer. Switch to descriptors if a resolver signature becomes difficult to read or if a rule should be part of shared schema policy.

# Understanding the Attribute Model

Attributes are processed during schema building, before any GraphQL request is executed:

```text
C# attribute
  -> descriptor configuration
  -> GraphQL schema field or type
  -> execution middleware pipeline (if the attribute adds middleware)
```

A descriptor attribute configures the same descriptor objects as the fluent APIs. Most `Use*` attributes add field middleware. Since middleware wraps resolver execution, the order of these attributes can affect the outcome.

If you create custom middleware attributes, maintain the correct order by accepting the generated `order` value and assigning it to `Order`. For the full pattern, see [Custom descriptor attributes](./custom-descriptor-attributes).

# Maintain the Required Order for Data Middleware

When combining data attributes, apply them in the following top-to-bottom order:

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

Think of this stack as a data pipeline:

```text
paging -> projection -> filtering -> sorting -> resolver result
```

Paging determines the list or connection shape. Projection selects the required fields from the data source. Filtering and sorting allow clients to shape the query results.

The analyzer checks method declarations for known data attributes and reports ordering issues. It cannot catch every ordering problem on properties or custom descriptor attributes. If you see an ordering warning, reorder the attributes as shown and ensure that projections, filtering, and sorting are registered in the schema.

# Common Attributes in This Section

| Attribute                    | Package or namespace                                      | Primary targets                                                                                                  | What it changes                                                           | Details                                                        |
| ---------------------------- | --------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- | -------------------------------------------------------------- |
| `[Authorize]`                | `HotChocolate.Authorization`                              | Object types and object fields                                                                                   | Requires authentication, roles, or policies.                              | [Authorize](./authorize)                                       |
| `[Cost]`                     | `HotChocolate.CostAnalysis.Types`                         | Supported type definitions such as object fields, object types, arguments, enum types, input fields, and scalars | Adds `@cost` weight metadata for static cost analysis.                    | [Cost](./cost)                                                 |
| `[GraphQLName]`              | `HotChocolate`                                            | Types, members, enum values, and parameters                                                                      | Overrides the inferred GraphQL name.                                      | [GraphQLName](./graphqlname)                                   |
| `[ListSize]`                 | `HotChocolate.CostAnalysis.Types`                         | Object fields                                                                                                    | Adds `@listSize` metadata so cost analysis can estimate list cardinality. | [ListSize](./listsize)                                         |
| `[UseFiltering]`             | `HotChocolate.Data`                                       | Resolver methods and properties that become fields                                                               | Adds a `where` argument and filtering middleware.                         | [UseFiltering](./usefiltering)                                 |
| `[UsePaging]`                | `HotChocolate.Types`                                      | Resolver methods and properties that become fields                                                               | Adds cursor pagination and connection or list paging behavior.            | [UsePaging](./usepaging)                                       |
| `[UseProjection]`            | `HotChocolate.Data`                                       | Object fields                                                                                                    | Projects requested fields into the data source.                           | [UseProjection](./useprojection)                               |
| `[UseSorting]`               | `HotChocolate.Data`                                       | Resolver methods and properties that become fields                                                               | Adds an `order` argument and sorting middleware.                          | [UseSorting](./usesorting)                                     |
| Custom descriptor attributes | `HotChocolate.Types` and `HotChocolate.Types.Descriptors` | Depends on the descriptor base class                                                                             | Packages descriptor configuration into a reusable attribute.              | [Custom descriptor attributes](./custom-descriptor-attributes) |

Looking for a different attribute?

- Root operation attributes like `[QueryType]`, `[MutationType]`, and `[SubscriptionType]` are covered in schema building and operation root type documentation.
- Resolver parameter attributes such as `[Parent]`, `[Service]`, and state attributes are discussed in [resolver parameter binding](../resolvers/parameter-attributes).
- Subscription topic and event attributes are found in the subscriptions documentation.
- Relay and node attributes are explained in the Relay and global object identification docs.
- Fusion and federation attributes are described in the Fusion and federation documentation.
- DataLoader source generator attributes are in the DataLoader documentation.

# Rename types and members with `[GraphQLName]`

Apply `[GraphQLName]` when the public GraphQL name should differ from the C# symbol name.

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

Use `[GraphQLName]` to control naming. If your main concern is where a value comes from, use resolver parameter binding attributes such as `[Argument]`.

# Protect Fields with `[Authorize]`

To authorize GraphQL types and fields, use `HotChocolate.Authorization.AuthorizeAttribute` (not the ASP.NET Core MVC authorization attribute).

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

Register GraphQL authorization in your schema setup, and configure ASP.NET Core authentication and authorization in the application pipeline. The attribute defines the GraphQL policy, while the server setup enforces it at runtime.

# Add Cost Metadata with `[Cost]` and `[ListSize]`

Cost analysis uses field weights and list-size estimates to prevent overly expensive operations before they run.

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

Use `[Cost]` to assign a weight, and `[ListSize]` to describe the expected number of items a list field can return.

# Create Custom Descriptor Attributes for Repeated Rules

Define a custom descriptor attribute when multiple fields share the same descriptor configuration and the rule has a meaningful domain name.

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

Use a specific base class, such as `ObjectFieldDescriptorAttribute`, when the attribute targets a single descriptor kind. Use the more general `DescriptorAttribute` if the attribute must handle multiple descriptor types.

# Troubleshooting Attribute Issues

## Filtering, Sorting, Projection, or Paging Does Not Behave as Expected

- Register the corresponding schema feature, such as `.AddProjections()`, `.AddFiltering()`, or `.AddSorting()`.
- Ensure the resolver returns a supported shape, like `IQueryable<T>`, `IEnumerable<T>`, or `Connection<T>` for the attribute in use.
- Reorder data attributes to `[UsePaging]`, `[UseProjection]`, `[UseFiltering]`, `[UseSorting]`.
- If you use `QueryContext<T>`, do not combine it with `[UseProjection]` on the same field.

## Authorization Does Not Run

- Make sure you have imported `HotChocolate.Authorization`.
- Register GraphQL authorization with the schema.
- Configure ASP.NET Core authentication and authorization middleware in the correct order.
- Double-check policy and role names.

## Projection Returns Default Values

- Ensure projected properties have public setters.
- Avoid custom resolvers for fields that need to be projected into the database, or separate the projected field from the custom resolved field.

## An Attribute Has No Effect

- Verify that the package reference and `using` directive match the attribute.
- Confirm the attribute supports the target where you placed it.
- Ensure the type or member is included in the schema being built.
- Move complex or conditional configuration to a descriptor if the attribute form hides the actual rule.

# Next Steps

Refer to the focused pages for options, package setup, or edge cases:

- [Authorize](./authorize)
- [Cost](./cost)
- [Custom descriptor attributes](./custom-descriptor-attributes)
- [GraphQLName](./graphqlname)
- [ListSize](./listsize)
- [UseFiltering](./usefiltering)
- [UsePaging](./usepaging)
- [UseProjection](./useprojection)
- [UseSorting](./usesorting)

For related topics, see [Type System](../type-system), [Resolvers](../resolvers), [Resolver Parameter Attributes](../resolvers/parameter-attributes), [Field Middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware), [Pagination](/docs/hotchocolate/v16/build/pagination), [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types), [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options), [Authorization](/docs/hotchocolate/v16/build/security/authorization), and [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis).
