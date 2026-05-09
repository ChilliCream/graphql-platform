---
title: Sorting
---

Hot Chocolate generates sort input types from your .NET models, allowing clients to order results by one or more fields. The default implementation translates sort operations to expression trees applied to `IQueryable`, producing native database queries. For models with nested objects, sorting extends across relationships.

# Getting Started

Sorting is part of the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

Register sorting on the schema:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddSorting();
```

Apply the `[UseSorting]` attribute to a resolver that returns `IQueryable<T>` or `IEnumerable<T>`:

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UseSorting]
    public static IQueryable<User> GetUsers(CatalogContext db)
        => db.Users;
}
```

</Implementation>
<Code>

```csharp
// Types/UserQueries.cs
public class UserQueries
{
    public IQueryable<User> GetUsers(CatalogContext db)
        => db.Users;
}

// Types/UserQueriesType.cs
public class UserQueriesType : ObjectType<UserQueries>
{
    protected override void Configure(IObjectTypeDescriptor<UserQueries> descriptor)
    {
        descriptor.Field(f => f.GetUsers(default!)).UseSorting();
    }
}
```

</Code>
</ExampleTabs>

Clients use the `order` argument to sort results:

```graphql
query {
  users(order: [{ name: ASC }]) {
    name
    email
  }
}
```

> **Middleware order matters.** When combining multiple middleware, apply them in this order: `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`.

# Sorting on Nested Fields

Sorting extends to properties of nested objects:

```graphql
query {
  users(order: [{ address: { city: ASC } }]) {
    name
    address {
      city
    }
  }
}
```

# Multi-Field Sorting

Pass multiple sort conditions as an array. The database applies them in order:

```graphql
query {
  users(order: [{ name: ASC }, { address: { city: DESC } }]) {
    name
    address {
      city
    }
  }
}
```

# NullOrdering Enum

In v16, the `NullOrdering` enum controls how `null` values sort relative to non-null values. This is relevant when sorting on nullable fields. Set this through `PagingOptions` at the global level:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .ModifyPagingOptions(opt => opt.NullOrdering = NullOrdering.NativeNullsLast);
```

| Value              | When to use                                                             |
| ------------------ | ----------------------------------------------------------------------- |
| `Unspecified`      | Default. Auto-detected for known EF Core providers.                     |
| `NativeNullsFirst` | Nulls sort before non-null values (SQL Server, SQLite, in-memory LINQ). |
| `NativeNullsLast`  | Nulls sort after non-null values (PostgreSQL default).                  |

# Custom Sort Types

Customize which fields are sortable by extending `SortInputType<T>`:

```csharp
// Types/UserSortType.cs
public class UserSortType : SortInputType<User>
{
    protected override void Configure(ISortInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(f => f.Name);
        descriptor.Field(f => f.CreatedAt);
    }
}
```

Restrict sort directions on a field by providing a custom enum type:

```csharp
// Types/AscOnlySortEnumType.cs
public class AscOnlySortEnumType : DefaultSortEnumType
{
    protected override void Configure(ISortEnumTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultSortOperations.Ascending);
    }
}
```

```csharp
// Types/UserSortType.cs
public class UserSortType : SortInputType<User>
{
    protected override void Configure(ISortInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(f => f.Name).Type<AscOnlySortEnumType>();
    }
}
```

Apply the custom sort type:

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UseSorting(typeof(UserSortType))]
    public static IQueryable<User> GetUsers(CatalogContext db)
        => db.Users;
}
```

</Implementation>
<Code>

```csharp
// Types/UserQueriesType.cs
public class UserQueriesType : ObjectType<UserQueries>
{
    protected override void Configure(IObjectTypeDescriptor<UserQueries> descriptor)
    {
        descriptor.Field(f => f.GetUsers(default!)).UseSorting<UserSortType>();
    }
}
```

</Code>
</ExampleTabs>

# Sort Conventions

Sort conventions let you change sorting behavior globally across your schema.

## Setting Up a Convention

Extend `SortConvention` and override `Configure`:

```csharp
// Conventions/CustomSortConvention.cs
public class CustomSortConvention : SortConvention
{
    protected override void Configure(ISortConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        descriptor.ArgumentName("sortBy");
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddConvention<ISortConvention, CustomSortConvention>();
```

To extend the default behavior without replacing it, use `SortConventionExtension`:

```csharp
// Conventions/CustomSortConventionExtension.cs
public class CustomSortConventionExtension : SortConventionExtension
{
    protected override void Configure(ISortConventionDescriptor descriptor)
    {
        descriptor.Configure<DefaultSortEnumType>(
            x => x.Operation(DefaultSortOperations.Ascending).Description("Sort ascending"));
    }
}
```

## Binding Sort Types Globally

Bind custom sort types to .NET types through the convention:

```csharp
// Conventions/CustomSortConvention.cs
public class CustomSortConvention : SortConvention
{
    protected override void Configure(ISortConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        descriptor.BindRuntimeType<User, UserSortType>();
    }
}
```

## Default Binding

For fields where no explicit binding exists, `DefaultSortEnumType` (with `ASC` and `DESC`) is used. Override this with `DefaultBinding`:

```csharp
descriptor.AddDefaults().DefaultBinding<AscOnlySortEnumType>();
```

# Next Steps

- **Need to filter results?** See [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering).
- **Need to page through results?** See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).
- **Need to optimize database queries?** See [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).
- **Need to protect against expensive queries?** See [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).
