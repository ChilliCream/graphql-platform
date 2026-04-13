---
title: Filtering
---

Hot Chocolate generates filter input types from your .NET models and translates client-supplied filter arguments into native database queries. The default implementation builds expression trees that apply to `IQueryable`, but you can customize filters for other data sources.

Given a model like `User` with a `Name` property, Hot Chocolate generates a `UserFilterInput` with string operations such as `eq`, `contains`, `startsWith`, and more. Clients use the `where` argument to filter results.

# Getting Started

Filtering is part of the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

Register filtering on the schema:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddFiltering();
```

Apply the `[UseFiltering]` attribute to a resolver that returns `IQueryable<T>` or `IEnumerable<T>`:

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UseFiltering]
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
        descriptor
            .Field(f => f.GetUsers(default!))
            .Type<ListType<NonNullType<UserType>>>()
            .UseFiltering();
    }
}
```

</Code>
</ExampleTabs>

Clients can now filter users:

```graphql
query {
  users(where: { name: { contains: "Alice" } }) {
    name
    email
  }
}
```

> **Middleware order matters.** When combining multiple middleware, apply them in this order: `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`.

# Filter Types

Hot Chocolate generates filter operations based on the .NET type of each property.

## String Filters

Operations: `eq`, `neq`, `contains`, `ncontains`, `in`, `nin`, `startsWith`, `nstartsWith`, `endsWith`, `nendsWith`.

## Boolean Filters

Operations: `eq`, `neq`.

## Comparable Filters

For numeric types (`int`, `long`, `float`, `double`, `decimal`), `Guid`, `DateTime`, `DateTimeOffset`, and `TimeSpan`.

Operations: `eq`, `neq`, `in`, `nin`, `gt`, `ngt`, `gte`, `ngte`, `lt`, `nlt`, `lte`, `nlte`.

## Enum Filters

Operations: `eq`, `neq`, `in`, `nin`.

## Object Filters

Filters are generated for nested objects, supporting filtering across database relationships:

```csharp
// Models/User.cs
public class User
{
    public string Name { get; set; }
    public Address Address { get; set; }
}

// Models/Address.cs
public class Address
{
    public string City { get; set; }
}
```

```graphql
query {
  users(where: { address: { city: { eq: "Berlin" } } }) {
    name
  }
}
```

## List Filters

For collection properties, Hot Chocolate generates `all`, `none`, `some`, and `any` operations:

```graphql
query {
  users(where: { orders: { some: { total: { gt: 100 } } } }) {
    name
  }
}
```

# Combining Filters with "and" / "or"

Every filter input type includes `and` and `or` fields for composing multiple conditions:

```graphql
query {
  users(
    where: {
      or: [{ name: { contains: "Alice" } }, { name: { contains: "Bob" } }]
    }
  ) {
    name
  }
}
```

The `or` field must be used at the top level of the filter. Placing it inside a field operation results in `and` semantics instead.

## Removing "and" / "or"

Disable these combinators in a custom filter type:

```csharp
// Types/UserFilterType.cs
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.AllowAnd(false).AllowOr(false);
    }
}
```

# Custom Filter Types

Customize which fields are filterable and which operations are available by extending `FilterInputType<T>`:

```csharp
// Types/UserFilterType.cs
public class UserFilterType : FilterInputType<User>
{
    protected override void Configure(IFilterInputTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(f => f.Name);
        descriptor.Field(f => f.Email).Type<CustomStringOperationFilterInputType>();
    }
}
```

Restrict operations on a field by defining a custom operation type:

```csharp
// Types/CustomStringOperationFilterInputType.cs
public class CustomStringOperationFilterInputType : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>();
    }
}
```

Apply the custom filter type:

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UseFiltering(typeof(UserFilterType))]
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
        descriptor
            .Field(f => f.GetUsers(default!))
            .UseFiltering<UserFilterType>();
    }
}
```

</Code>
</ExampleTabs>

# Filter Conventions

Filter conventions let you change filtering behavior globally across your schema.

## Setting Up a Convention

Extend `FilterConvention` and override `Configure`, or use `FilterConventionExtension` to build on top of the defaults:

```csharp
// Conventions/CustomFilterConvention.cs
public class CustomFilterConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        descriptor.ArgumentName("filter");
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddConvention<IFilterConvention, CustomFilterConvention>();
```

## Binding Filter Types Globally

Bind custom filter types to .NET types through the convention:

```csharp
// Conventions/CustomFilterConvention.cs
public class CustomFilterConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        descriptor.BindRuntimeType<User, UserFilterType>();
    }
}
```

## Custom Scalar Filters

When you use custom scalars (including those from `HotChocolate.Types.Scalars`), you must create and bind a filter type for each scalar:

```csharp
// Types/EmailAddressOperationFilterInputType.cs
public class EmailAddressOperationFilterInputType : FilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<EmailAddressType>();
        descriptor.Operation(DefaultFilterOperations.NotEquals).Type<EmailAddressType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<EmailAddressType>();
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddFiltering(x => x
        .AddDefaults()
        .BindRuntimeType<string, EmailAddressOperationFilterInputType>());
```

# Next Steps

- **Need to sort results?** See [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).
- **Need to page through results?** See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).
- **Need to optimize database queries?** See [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).
- **Need to protect against expensive filter queries?** See [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).
