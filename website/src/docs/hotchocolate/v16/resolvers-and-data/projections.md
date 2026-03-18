---
title: Projections
---

GraphQL clients specify which fields they need. Projections take advantage of this by translating the requested fields directly into optimized database queries. If a client requests only `name` and `email`, Hot Chocolate queries only those columns from the database.

```graphql
{
  users {
    email
    address {
      street
    }
  }
}
```

```sql
SELECT "u"."Email", "a"."Id" IS NOT NULL, "a"."Street"
FROM "Users" AS "u"
LEFT JOIN "Address" AS "a" ON "u"."AddressId" = "a"."Id"
```

Projections operate on `IQueryable` by default. Custom providers can extend this to other data sources.

> Projections require a public setter on fields they operate on. Without a public setter, the default-constructed value is returned.

# Getting Started

Projections are part of the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

Register projections on the schema:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddProjections();
```

Apply the `[UseProjection]` attribute to a resolver that returns `IQueryable<T>`:

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UseProjection]
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
        descriptor.Field(f => f.GetUsers(default!)).UseProjection();
    }
}
```

</Code>
</ExampleTabs>

The projection middleware creates a `Select` expression for the entire subtree of the field. Fields with custom resolvers are not projected to the database. If the middleware encounters a nested field that also specifies `UseProjection()`, that field is handled separately.

> **Middleware order matters.** When combining multiple middleware, apply them in this order: `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`.

# QueryContext&lt;T&gt; Pattern

In v16, `QueryContext<T>` provides an alternative to the `[UseProjection]` middleware. Instead of applying projections as middleware, you return a `QueryContext<T>` from your resolver and Hot Chocolate applies projections, filtering, and sorting at execution time.

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static QueryContext<User> GetUsers(CatalogContext db)
        => db.Users.AsQueryContext();
}
```

`QueryContext<T>` integrates projection, filtering, and sorting into a single return type. This can reduce middleware stacking and make your resolver signatures cleaner.

## HC0099 Analyzer Warning

Do not combine `QueryContext<T>` with `[UseProjection]` on the same field. The HC0099 analyzer warns when both are present because they conflict: each tries to apply its own `Select` expression, leading to unexpected behavior or runtime errors.

**Incorrect:**

```csharp
// This triggers HC0099
[UseProjection]
public static QueryContext<User> GetUsers(CatalogContext db)
    => db.Users.AsQueryContext();
```

**Correct:** Use one approach or the other:

```csharp
// Option 1: QueryContext<T> (handles projections internally)
public static QueryContext<User> GetUsers(CatalogContext db)
    => db.Users.AsQueryContext();

// Option 2: [UseProjection] middleware
[UseProjection]
public static IQueryable<User> GetUsers(CatalogContext db)
    => db.Users;
```

# Combining with Filtering, Sorting, and Pagination

Projections work with filtering, sorting, and pagination. Maintain the correct middleware order:

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<User> GetUsers(CatalogContext db)
        => db.Users;
}
```

Filtering and sorting can project over relationships. Projections cannot project pagination over relationships. For nested collections that need filtering or sorting, apply those attributes to the collection property:

```csharp
// Models/User.cs
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    [UseFiltering]
    [UseSorting]
    public ICollection<Address> Addresses { get; set; }
}
```

```graphql
{
  users(where: { name: { eq: "ChilliCream" } }, order: [{ name: DESC }]) {
    nodes {
      email
      addresses(where: { street: { eq: "Sesame Street" } }) {
        street
      }
    }
  }
}
```

# FirstOrDefault / SingleOrDefault

When you want a field to return a single entity instead of a list, use `[UseFirstOrDefault]` or `[UseSingleOrDefault]`. These rewrite the return type from `IQueryable<T>` to `T?` and apply the corresponding LINQ operation:

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UseFirstOrDefault]
    [UseProjection]
    [UseFiltering]
    public static IQueryable<User> GetUser(CatalogContext db)
        => db.Users;
}
```

This produces a schema field that returns a single `User` (or null) instead of a list:

```graphql
type Query {
  user(where: UserFilterInput): User
}
```

# Always Project Fields

Resolvers on a type sometimes need data from the parent that the client did not request. Mark a field with `[IsProjected(true)]` to ensure it is always included in the database query:

<ExampleTabs>
<Implementation>

```csharp
// Models/User.cs
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    [IsProjected(true)]
    public string Email { get; set; }
    public Address Address { get; set; }
}
```

</Implementation>
<Code>

```csharp
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(f => f.Email).IsProjected(true);
    }
}
```

</Code>
</ExampleTabs>

Even if the client does not request `email`, the SQL query includes the `Email` column so that resolvers depending on it have the data they need.

# Exclude Fields from Projection

Use `[IsProjected(false)]` to exclude a field from projection. The field remains in the schema but is not included in the database query:

<ExampleTabs>
<Implementation>

```csharp
// Models/User.cs
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    [IsProjected(false)]
    public string InternalNotes { get; set; }
    public Address Address { get; set; }
}
```

</Implementation>
<Code>

```csharp
// Types/UserType.cs
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(f => f.InternalNotes).IsProjected(false);
    }
}
```

</Code>
</ExampleTabs>

# Troubleshooting

## Properties return default values

Projections require a public setter on each property they operate on. If a property has only a getter (or uses `init`), the projected value cannot be assigned and the default value is returned.

## HC0099: Do not combine QueryContext with UseProjection

Remove either `[UseProjection]` or `QueryContext<T>`. Using both on the same field produces conflicting projection behavior.

## Nested properties are null

Verify that the navigation property is included in the EF Core model. If the relationship is not configured in the database context, the projection middleware cannot include it in the query.

## All columns are loaded despite using projections

Ensure the resolver returns `IQueryable<T>`, not a materialized collection. If you call `.ToList()` or `.AsEnumerable()` before returning, the projection middleware has no `IQueryable` to apply `Select` to.

# Next Steps

- **Need to filter results?** See [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering).
- **Need to sort results?** See [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).
- **Need to page through results?** See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).
- **Need to integrate with Entity Framework?** See [Entity Framework Integration](/docs/hotchocolate/v16/integrations/entity-framework).
