---
title: UseFiltering attribute
---

The `[UseFiltering]` attribute enables Hot Chocolate filtering on a collection field. Apply it to a resolver that returns a list, array, `IEnumerable<T>`, or `IQueryable<T>` when you want clients to filter the results of that field.

Filtering is provided by the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

# Filtering a Collection Field

First, register filtering in your schema setup. Then, decorate the resolver that returns the collection with `[UseFiltering]`.

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

Clients can now send filter values in their queries:

```graphql
query {
  users(where: { name: { contains: "Ada" }, isActive: { eq: true } }) {
    id
    name
    email
  }
}
```

The argument name is determined by the active filter convention. By default, it is `where`, but a custom convention can specify a different name.

# What the Attribute Does

`[UseFiltering]` is the attribute-based equivalent of the `.UseFiltering()` field descriptor API. It configures the annotated object or interface field to use Hot Chocolate filtering.

The attribute:

- Adds a filter argument to the field
- Creates or selects a filter input type for the field's result element type
- Uses the active filter convention and provider
- Applies to method and property resolvers
- Can reference an explicit filter input type
- Can select a named convention scope

The generated input type reflects the public members exposed by the active convention. Built-in operation inputs include string operations (`eq`, `contains`, `startsWith`), comparable operations (`gt`, `lte`), boolean operations (`eq`), enum operations (`in`), nested object filters, and list filters (`some`, `none`).

# Attribute Reference

| API                                              | Use                                                                                                                                        |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `UseFilteringAttribute`                          | Adds filtering to a method or property resolver.                                                                                           |
| `UseFilteringAttribute(Type? filterType = null)` | Optionally supplies a runtime type or filter input type. If omitted, Hot Chocolate infers the filter from the field's result element type. |
| `UseFilteringAttribute.Type`                     | Gets or sets the runtime type or filter input type for the field.                                                                          |
| `UseFilteringAttribute.Scope`                    | Selects a named filter convention and provider scope.                                                                                      |
| `UseFilteringAttribute<T>`                       | Generic convenience attribute for specifying the runtime type or filter input type.                                                        |

The attribute is inherited by derived members and can appear multiple times in source. Most fields should use a single filtering attribute. Use a named `Scope` or an explicit filter type when a field requires non-default behavior.

# Ordering Data Middleware Attributes

When combining data middleware attributes, use this top-to-bottom order:

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

The field middleware pipeline expects this sequence:

```text
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
```

Incorrect ordering can change the field shape for the next middleware. The analyzer may report: `Data attributes must be ordered correctly: [UsePaging], [UseProjection], [UseFiltering], [UseSorting]`.

# Returning Queryable Data for Provider Translation

For database-backed fields, return `IQueryable<T>` from the resolver:

```csharp
[UseFiltering]
public IQueryable<User> GetUsers(AppDbContext dbContext)
{
    return dbContext.Users;
}
```

The default queryable filtering provider builds expression trees. Your underlying query provider, such as Entity Framework, determines which expressions are translated into database queries.

`IEnumerable<T>` is also supported, but the data may already be loaded before filtering runs:

```csharp
[UseFiltering]
public IEnumerable<User> GetFeaturedUsers(UserRepository repository)
{
    return repository.GetFeaturedUsers();
}
```

Use `IEnumerable<T>` for bounded in-memory collections. Avoid calling `ToList()` before returning a database query if you want filtering to be handled by the data source.

Providers have limitations. A provider must support the requested runtime types, operations, and backends. If you need filtering for a non-`IQueryable` backend or a provider-specific operation, create or select a convention and provider. See [Extending Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering).

# Using the Inferred Filter Type

The most common usage is the parameterless attribute:

```csharp
[UseFiltering]
public IQueryable<User> GetUsers(AppDbContext dbContext)
{
    return dbContext.Users;
}
```

Hot Chocolate infers the filter input from the field's result element type. For `IQueryable<User>`, the active convention creates or selects a filter input for `User`.

Use inference when the generated filter is part of your public API and the active convention exposes the appropriate fields and operations.

# Using an Explicit Filter Type

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

`UseFilteringAttribute.Type` accepts either a filter input type or a runtime type. If the type implements Hot Chocolate's filter input type contract, Hot Chocolate uses that filter schema type. Otherwise, it treats the type as the runtime type and wraps it in a generated filter input type.

# Selecting a Convention Scope

Use `Scope` when a field should use a named filter convention. Conventions control the filter argument name, operation names, type bindings, and provider selection.

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

The scope name on the attribute must match a registered convention name. If no convention exists for the scope, schema creation fails and instructs you to register a convention for that name.

When paging, projections, filtering, and sorting all target the same named backend, keep their scopes aligned so each middleware uses the matching convention and provider.

# Choosing Attributes or Descriptor Configuration

Use `[UseFiltering]` when field-level configuration belongs next to the resolver and remains concise.

Use fluent descriptor configuration or filter types when you need:

- Centralized configuration in an `ObjectType` or type extension
- Conditional setup
- Inline descriptor callbacks
- Repeated rules shared across many fields
- Convention-level type bindings
- Custom operations or provider handlers

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

# Troubleshooting Filtering Attributes

## The Filter Argument Is Missing

Check the following:

- `.AddFiltering()` is registered in the `.AddGraphQL()` chain
- `[UseFiltering]` is applied to the method or property exposing the collection field
- The resolver returns a collection with an inferable element type, or the attribute specifies an explicit type
- A custom convention may have renamed `where` to another argument name

## Schema Creation Fails for a Scope

Ensure the scope string matches the registered convention name:

```csharp
.AddConvention<IFilterConvention, SearchFilterConvention>("Search")
```

```csharp
[UseFiltering(Scope = "Search")]
```

If the names differ, register the convention with the correct name or update the attribute.

## Filtering Runs In Memory or Is Slow

Check if the resolver returns `IEnumerable<T>` or calls `ToList()` before returning. For database-backed data, return `IQueryable<T>` and confirm the active provider supports the requested operations.

## Attribute Order Diagnostics Appear

Reorder the data middleware attributes:

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
```

## A Field Should Not Be Filterable

Create a `FilterInputType<T>` and call `BindFieldsExplicitly()`, or ignore the field in filter configuration. Use a convention binding when many fields should share the same filter type.

## A Custom Operation Is Not Available

Ensure the active convention defines the operation, the field uses the intended `Scope`, and the provider has a handler for the operation.

# Next Steps

- Learn about filter operations and conventions in [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types).
- Customize operations, handlers, and providers in [Extending Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering).
- Add paging to filtered lists with [Pagination](/docs/hotchocolate/v16/build/pagination).
- Project selected fields from a data source with [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options).
- Add sorting after filtering with [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types).
- Review attribute middleware order in [Attributes](/docs/hotchocolate/v16/build/attributes).
