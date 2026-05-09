---
title: UseFiltering attribute
---

`[UseFiltering]` adds Hot Chocolate filtering to one collection field. Use it when a resolver returns a list, array, `IEnumerable<T>`, or `IQueryable<T>` and clients need a filter argument for that field.

Filtering is part of the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

# Filter a collection field

Register filtering once in your schema setup, then place `[UseFiltering]` on the resolver that returns the collection.

```csharp
#nullable enable

using HotChocolate.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDbContext<AppDbContext>()
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddFiltering();

public sealed class Query
{
    [UseFiltering]
    public IQueryable<User> GetUsers(AppDbContext dbContext)
    {
        return dbContext.Users;
    }
}

public sealed class User
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public string Email { get; set; } = default!;

    public bool IsActive { get; set; }
}
```

With the default filter convention, Hot Chocolate adds a `where` argument to the field:

```graphql
type Query {
  users(where: UserFilterInput): [User!]!
}

input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  name: StringOperationFilterInput
  email: StringOperationFilterInput
  isActive: BooleanOperationFilterInput
}
```

Clients can now send filter values with the query:

```graphql
query {
  users(where: { name: { contains: "Ada" }, isActive: { eq: true } }) {
    id
    name
    email
  }
}
```

The argument name comes from the active filter convention. The default convention uses `where`, but a custom convention can use another name.

# What the attribute adds

`[UseFiltering]` is the attribute form of the field descriptor `.UseFiltering()` API. It configures the annotated object field, or interface field, to use Hot Chocolate filtering.

The attribute:

- adds a filter argument to the field,
- creates or selects a filter input type for the field result element type,
- uses the active filter convention and provider,
- applies to method and property resolvers,
- can point to an explicit filter input type,
- can select a named convention scope.

The generated input type reflects the public members that the active convention exposes. Built-in operation inputs include string operations such as `eq`, `contains`, and `startsWith`, comparable operations such as `gt` and `lte`, boolean operations such as `eq`, enum operations such as `in`, nested object filters, and list filters such as `some` and `none`.

# Attribute reference

| API                                              | Use                                                                                                                                         |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| `UseFilteringAttribute`                          | Adds filtering to a method or property resolver.                                                                                            |
| `UseFilteringAttribute(Type? filterType = null)` | Supplies an optional runtime type or filter input type. Without a type, Hot Chocolate infers the filter from the field result element type. |
| `UseFilteringAttribute.Type`                     | Gets or sets the runtime type or filter input type used for the field.                                                                      |
| `UseFilteringAttribute.Scope`                    | Selects a named filter convention and provider scope.                                                                                       |
| `UseFilteringAttribute<T>`                       | Generic convenience attribute for supplying the runtime type or filter input type.                                                          |

The attribute is inherited by derived members and can appear more than once in source. Most fields should use one filtering attribute. Use a named `Scope` or an explicit filter type when one field needs non-default behavior.

# Put filtering in the data middleware order

When you combine data middleware attributes, place them in this top-to-bottom order:

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<User> GetUsers(AppDbContext dbContext)
{
    return dbContext.Users;
}
```

The field middleware pipeline expects this order:

```text
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
```

Incorrect order can change the field shape seen by the next middleware. The analyzer can also report: `Data attributes must be ordered correctly: [UsePaging], [UseProjection], [UseFiltering], [UseSorting]`.

# Return queryable data when you want provider translation

For database-backed fields, prefer returning `IQueryable<T>` from the resolver:

```csharp
[UseFiltering]
public IQueryable<User> GetUsers(AppDbContext dbContext)
{
    return dbContext.Users;
}
```

The default queryable filtering provider builds expression trees. Your underlying query provider, such as Entity Framework, decides which expressions become database queries.

`IEnumerable<T>` is supported, but the data may already be loaded before filtering runs:

```csharp
[UseFiltering]
public IEnumerable<User> GetFeaturedUsers(UserRepository repository)
{
    return repository.GetFeaturedUsers();
}
```

Use `IEnumerable<T>` for bounded in-memory collections. Avoid calling `ToList()` before returning a database query when you expect filtering to be handled by the data source.

Providers have limits. A provider must know how to handle the requested runtime types, operations, and backends. If you need filtering for a non-`IQueryable` backend or a provider-specific operation, create or select a convention and provider. See [Extending Filtering](/docs/hotchocolate/v16/api-reference/extending-filtering).

# Use the inferred filter type

The parameterless attribute is the common path:

```csharp
[UseFiltering]
public IQueryable<User> GetUsers(AppDbContext dbContext)
{
    return dbContext.Users;
}
```

Hot Chocolate infers the filter input from the field result element type. For `IQueryable<User>`, the active convention creates or selects a filter input for `User`.

Use inference when the generated filter is part of your public API and the active convention already exposes the right fields and operations.

# Use an explicit filter type

Use an explicit `FilterInputType<T>` when you need to hide fields, expose selected fields, change operations, add descriptions, or keep the public GraphQL API stable.

```csharp
using HotChocolate.Data.Filters;

public sealed class Query
{
    [UseFiltering(typeof(UserFilterType))]
    public IQueryable<User> GetUsers(AppDbContext dbContext)
    {
        return dbContext.Users;
    }
}

public sealed class UserFilterType : FilterInputType<User>
{
    protected override void Configure(IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Email);
        descriptor.Field(t => t.IsActive);
    }
}
```

The schema now exposes only the fields selected by `UserFilterType`:

```graphql
input UserFilterInput {
  and: [UserFilterInput!]
  or: [UserFilterInput!]
  name: StringOperationFilterInput
  email: StringOperationFilterInput
  isActive: BooleanOperationFilterInput
}
```

If your project uses a C# version that supports generic attributes, you can use the generic convenience form:

```csharp
[UseFiltering<UserFilterType>]
public IQueryable<User> GetUsers(AppDbContext dbContext)
{
    return dbContext.Users;
}
```

`UseFilteringAttribute.Type` accepts either a filter input type or a runtime type. When the type implements Hot Chocolate's filter input type contract, Hot Chocolate uses that filter schema type. Otherwise, Hot Chocolate treats the type as the runtime type and wraps it in a generated filter input type.

# Select a convention scope

Use `Scope` when a field must use a named filter convention. Conventions control the filter argument name, operation names, type bindings, and provider selection.

```csharp
using HotChocolate.Data.Filters;

builder.Services
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddFiltering()
    .AddConvention<IFilterConvention, SearchFilterConvention>("Search");

public sealed class Query
{
    [UseFiltering(Scope = "Search")]
    public IQueryable<User> GetUsers(SearchDbContext dbContext)
    {
        return dbContext.Users;
    }
}

public sealed class SearchFilterConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        descriptor.ArgumentName("filter");
    }
}
```

With this scope, clients use the argument name configured by the `Search` convention:

```graphql
query {
  users(filter: { name: { eq: "Ada" } }) {
    id
    name
  }
}
```

The scope name on the attribute must match a registered convention name. If no convention exists for the scope, schema creation fails and tells you to register a convention for that name.

When paging, projections, filtering, and sorting all target the same named backend, keep their scopes aligned so each middleware uses the matching convention and provider.

# Choose attributes or descriptor configuration

Use `[UseFiltering]` when field-level configuration belongs next to the resolver and stays short.

Use fluent descriptor configuration or filter types when you need:

- centralized configuration in an `ObjectType` or type extension,
- conditional setup,
- inline descriptor callbacks,
- repeated rules shared across many fields,
- convention-level type bindings,
- custom operations or provider handlers.

The descriptor form is equivalent for a field:

```csharp
public sealed class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(t => t.GetUsers(default!))
            .UseFiltering<UserFilterType>();
    }
}
```

# Troubleshoot filtering attributes

## The filter argument is missing

Check these items:

- `.AddFiltering()` is registered in the `.AddGraphQL()` chain.
- `[UseFiltering]` is on the method or property that exposes the collection field.
- The resolver returns a collection with an inferable element type, or the attribute supplies an explicit type.
- A custom convention may have renamed `where` to another argument name.

## Schema creation fails for a scope

Confirm that the scope string matches the registered convention name:

```csharp
.AddConvention<IFilterConvention, SearchFilterConvention>("Search")
```

```csharp
[UseFiltering(Scope = "Search")]
```

If the names differ, register the convention with the same name or change the attribute.

## Filtering runs in memory or is slow

Check whether the resolver returns `IEnumerable<T>` or calls `ToList()` before returning. For database-backed data, return `IQueryable<T>` and confirm the active provider supports the requested operations.

## Attribute order diagnostics appear

Reorder the data middleware attributes:

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
```

## A field should not be filterable

Create a `FilterInputType<T>` and call `BindFieldsExplicitly()`, or ignore the field in filter configuration. Use a convention binding when many fields should share the same filter type.

## A custom operation is not available

Confirm that the active convention defines the operation, the field uses the intended `Scope`, and the provider has a handler for the operation.

# Next steps

- Learn filter operations and conventions in [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering).
- Customize operations, handlers, and providers in [Extending Filtering](/docs/hotchocolate/v16/api-reference/extending-filtering).
- Add paging to filtered lists with [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).
- Project selected fields from a data source with [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).
- Add sorting after filtering with [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).
- Review attribute middleware order in [Attributes](/docs/hotchocolate/v16/build2/attributes).
