---
title: Sorting
metaTitle: "GraphQL Sorting in Hot Chocolate"
description: "Generate sort input types from .NET models with the [UseSorting] attribute in Hot Chocolate, translating client sort arguments into native database ordering."
---

Hot Chocolate generates sort input types from your .NET models, allowing clients to order results by one or more fields. The default implementation translates sort operations to expression trees applied to `IQueryable`, producing native database queries. For models with nested objects, sorting extends across relationships.

# Getting Started

Sorting is part of the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

Register sorting on the schema:

```csharp
builder
    .AddGraphQL()
    .AddSorting();
```

Apply the `[UseSorting]` attribute to a resolver that returns `IQueryable<T>` or `IEnumerable<T>`:

<ExampleTabs>
<Implementation>

```csharp
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
public class UserQueries
{
    public IQueryable<User> GetUsers(CatalogContext db)
        => db.Users;
}

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

> [!WARNING]
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

The `NullOrdering` enum controls how `null` values sort relative to non-null values. This is relevant when sorting on nullable fields. Set this through `PagingOptions` at the global level:

```csharp
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
public class AscOnlySortEnumType : DefaultSortEnumType
{
    protected override void Configure(ISortEnumTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultSortOperations.Ascending);
    }
}
```

```csharp
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
builder
    .AddGraphQL()
    .AddConvention<ISortConvention, CustomSortConvention>();
```

To extend the default behavior without replacing it, use `SortConventionExtension`:

```csharp
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

# Troubleshooting

## The LINQ expression could not be translated

Sorting fails with an error like this one:

```text
The LINQ expression 'DbSet<Order>()
    .OrderBy(o => o.PlacedAt.DateTime)' could not be translated. Either rewrite the query in a form that can be translated, or switch to client evaluation explicitly by inserting a call to 'AsEnumerable', 'AsAsyncEnumerable', 'ToList', or 'ToListAsync'.
```

Hot Chocolate applies the sort to the `IQueryable<T>` that your resolver returns, after its `Select` projection. EF Core sorts on the expression that the projection assigns to the sorted field, so the database provider has to translate that expression. With the following resolver, sorting on `placedAt` sorts on `o.PlacedAt.DateTime`:

```csharp
[UseSorting]
public static IQueryable<OrderDto> GetOrders(CatalogContext db)
    => db.Orders.Select(o => new OrderDto { Id = o.Id, PlacedAt = o.PlacedAt.DateTime });
```

The same query fails without Hot Chocolate, so the fix belongs in the EF Core model or provider. `DateTimeOffset.DateTime` is a common case, and whether it translates depends on the provider:

| Provider            | Sorting on `DateTimeOffset.DateTime`                                                                                                                                                        |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SQL Server          | Translated from EF Core 11, see [SQL Server function mappings](https://learn.microsoft.com/ef/core/providers/sql-server/functions#date-and-time-functions).                                 |
| PostgreSQL (Npgsql) | Translated.                                                                                                                                                                                 |
| SQLite              | Not translated. EF Core cannot sort `DateTimeOffset` values on SQLite either, see [SQLite limitations](https://learn.microsoft.com/ef/core/providers/sqlite/limitations#query-limitations). |

To fix the error, use one of these approaches:

- **Upgrade to EF Core 11** when you use SQL Server.
- **Project the `DateTimeOffset` itself.** Declare `OrderDto.PlacedAt` as `DateTimeOffset` and assign `o.PlacedAt`. SQL Server and PostgreSQL sort `DateTimeOffset` values by their UTC instant, which can differ from the local date and time order of `DateTimeOffset.DateTime`.
- **Store a `DateTime` on SQLite.** The [SQLite limitations](https://learn.microsoft.com/ef/core/providers/sqlite/limitations#query-limitations) page recommends `DateTime` instead of `DateTimeOffset`.
- **Add the translation yourself** when you use SQL Server with EF Core 8, 9, or 10. The following code translates `DateTimeOffset.DateTime` to `CONVERT(datetime2, value)`, the same SQL that EF Core 11 generates:

```csharp
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

public static class DateTimeOffsetTranslationExtensions
{
    public static SqlServerDbContextOptionsBuilder UseDateTimeOffsetDateTimeTranslation(
        this SqlServerDbContextOptionsBuilder builder)
    {
        var optionsBuilder =
            ((IRelationalDbContextOptionsBuilderInfrastructure)builder).OptionsBuilder;
        var extension =
            optionsBuilder.Options.FindExtension<DateTimeOffsetTranslationOptionsExtension>()
            ?? new DateTimeOffsetTranslationOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return builder;
    }
}

// Registers the translator with the EF Core services of the DbContext.
public sealed class DateTimeOffsetTranslationOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
        => new EntityFrameworkRelationalServicesBuilder(services)
            .TryAdd<IMemberTranslatorPlugin, DateTimeOffsetDateTimeTranslator>();

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension)
        : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using DateTimeOffset.DateTime translation ";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["DateTimeOffsetTranslation"] = "1";
    }
}

public sealed class DateTimeOffsetDateTimeTranslator(
    ISqlExpressionFactory sqlExpressionFactory,
    IRelationalTypeMappingSource typeMappingSource)
    : IMemberTranslatorPlugin, IMemberTranslator
{
    public IEnumerable<IMemberTranslator> Translators => [this];

    public SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        // Only translate columns stored as datetimeoffset.
        if (member.DeclaringType != typeof(DateTimeOffset)
            || member.Name != nameof(DateTimeOffset.DateTime)
            || instance?.TypeMapping is not { StoreTypeNameBase: "datetimeoffset" })
        {
            return null;
        }

        // CONVERT(datetime2, value) keeps the local date and time and drops the offset.
        return sqlExpressionFactory.Function(
            "CONVERT",
            [sqlExpressionFactory.Fragment("datetime2"), instance],
            nullable: true,
            argumentsPropagateNullability: [false, true],
            returnType,
            typeMappingSource.FindMapping(typeof(DateTime)));
    }
}
```

Register the translation where you configure the SQL Server provider:

```csharp
builder.Services.AddDbContext<CatalogContext>(
    options => options.UseSqlServer(
        connectionString,
        sqlServer => sqlServer.UseDateTimeOffsetDateTimeTranslation()));
```

With the translation registered, the sort runs in the database:

```sql
SELECT [o].[Id], CONVERT(datetime2, [o].[PlacedAt]) AS [PlacedAt]
FROM [Orders] AS [o]
ORDER BY CONVERT(datetime2, [o].[PlacedAt])
```

# Next Steps

- **Need to filter results?** See [Filtering](./filtering.md).
- **Need to page through results?** See [Pagination](./pagination.md).
- **Need to optimize database queries?** See [Projections](./projections.md).
- **Need to protect against expensive queries?** See [Cost Analysis](../security/cost-analysis.md).
