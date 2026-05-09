---
title: Filter conventions
---

A filter convention is the schema-wide configuration source for filtering. It decides the default `where` argument name, operation field names, operation descriptions, runtime type bindings, and the provider that translates accepted filter input at execution time.

This page focuses on Hot Chocolate v16 convention APIs. For the input shape of one entity or field, see [filter types](filter-types.md). For custom providers and handlers, see [Extending Filtering](/docs/hotchocolate/v16/api-reference/extending-filtering).

# Mental model

Filtering uses a convention before a request reaches your data source:

```text
Resolver field
  -> [UseFiltering] or .UseFiltering(scope)
  -> filter convention selected by scope/name
  -> filter input type, argument name, operation names, runtime bindings
  -> filter provider and handlers
  -> IQueryable expression or provider-native filter
```

Vocabulary:

| Term              | Meaning                                                                                              |
| ----------------- | ---------------------------------------------------------------------------------------------------- |
| Filter convention | Global or named configuration for filtering schema shape and provider selection.                     |
| Filter type       | A `FilterInputType<T>` or operation filter type that describes accepted input fields.                |
| Operation         | A predicate field such as `eq`, `contains`, `gt`, `some`, `and`, or `or`.                            |
| Runtime binding   | A mapping from a .NET runtime type to the filter input type Hot Chocolate should infer.              |
| Provider          | The component selected by the convention to translate filter input for a data source.                |
| Handler           | Provider-level component that translates a specific field or operation.                              |
| Scope             | The field-side name used to select a named convention. Registration APIs call the same value `name`. |

# When to use a convention

Use a filter convention when the rule should apply to every field that uses that convention:

- Rename the `where` argument for a schema or scope.
- Rename or describe operations globally.
- Bind runtime types to different operation filter input types.
- Select a provider such as IQueryable, MongoDB, Marten, RavenDB, or a custom provider.
- Disable `and` or `or` for all inferred filter types in a convention.
- Add a named convention for another data source or migration path.

Use a filter type instead when one entity or field needs a smaller input shape, field allowlist, field rename, or field-specific operation type. Use a provider or handler only when translation behavior needs to change.

# Register the default convention

Install `HotChocolate.Data`, then register filtering once with the schema:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering();
```

Fields opt in with attributes or descriptors:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseFiltering]
    public static IQueryable<Product> GetProducts(CatalogContext db)
    {
        return db.Products;
    }
}
```

```csharp
public sealed class ProductQueryResolvers
{
    public IQueryable<Product> GetProducts(CatalogContext db)
    {
        return db.Products;
    }
}

public sealed class ProductQueryResolversType : ObjectType<ProductQueryResolvers>
{
    protected override void Configure(IObjectTypeDescriptor<ProductQueryResolvers> descriptor)
    {
        descriptor
            .Field(t => t.GetProducts(default!))
            .UseFiltering();
    }
}
```

The default convention contributes these schema defaults:

| Default                 | Value                                                                |
| ----------------------- | -------------------------------------------------------------------- |
| Argument name           | `where`                                                              |
| Provider                | `QueryableFilterProvider` with default field handlers                |
| Runtime type bindings   | Common scalar runtime types to built-in operation filter input types |
| Combinators             | `and` and `or` enabled                                               |
| Default operation names | Added by `AddDefaultOperations()`                                    |

The default `AddFiltering()` call is equivalent to configuring a convention with `AddDefaults()`. `AddDefaults()` expands to:

| Building block           | What it adds                                               |
| ------------------------ | ---------------------------------------------------------- |
| `AddDefaultOperations()` | Built-in operation IDs and public GraphQL operation names. |
| `BindDefaultTypes()`     | Common .NET runtime type bindings.                         |
| `UseQueryableProvider()` | The IQueryable provider and its default handlers.          |

# Default operation names

Operations have stable internal IDs from `DefaultFilterOperations`. The convention maps those IDs to public GraphQL field names.

| Operation ID             | Default GraphQL name |
| ------------------------ | -------------------- |
| `Equals`                 | `eq`                 |
| `NotEquals`              | `neq`                |
| `GreaterThan`            | `gt`                 |
| `NotGreaterThan`         | `ngt`                |
| `GreaterThanOrEquals`    | `gte`                |
| `NotGreaterThanOrEquals` | `ngte`               |
| `LowerThan`              | `lt`                 |
| `NotLowerThan`           | `nlt`                |
| `LowerThanOrEquals`      | `lte`                |
| `NotLowerThanOrEquals`   | `nlte`               |
| `Contains`               | `contains`           |
| `NotContains`            | `ncontains`          |
| `In`                     | `in`                 |
| `NotIn`                  | `nin`                |
| `StartsWith`             | `startsWith`         |
| `NotStartsWith`          | `nstartsWith`        |
| `EndsWith`               | `endsWith`           |
| `NotEndsWith`            | `nendsWith`          |
| `All`                    | `all`                |
| `None`                   | `none`               |
| `Some`                   | `some`               |
| `Any`                    | `any`                |
| `And`                    | `and`                |
| `Or`                     | `or`                 |
| `Data`                   | `data`               |

# Selected default runtime bindings

The default v16 convention binds common runtime types to operation filter input types.

| Runtime type                                                 | Default filter input type               |
| ------------------------------------------------------------ | --------------------------------------- |
| `string`                                                     | `StringOperationFilterInputType`        |
| `bool`, `bool?`                                              | `BooleanOperationFilterInputType`       |
| `byte`, `byte?`                                              | `UnsignedByteOperationFilterInputType`  |
| `sbyte`, `sbyte?`                                            | `ByteOperationFilterInputType`          |
| `short`, `short?`                                            | `ShortOperationFilterInputType`         |
| `ushort`, `ushort?`                                          | `UnsignedShortOperationFilterInputType` |
| `int`, `int?`                                                | `IntOperationFilterInputType`           |
| `uint`, `uint?`                                              | `UnsignedIntOperationFilterInputType`   |
| `long`, `long?`                                              | `LongOperationFilterInputType`          |
| `ulong`, `ulong?`                                            | `UnsignedLongOperationFilterInputType`  |
| `decimal`, `decimal?`                                        | `DecimalOperationFilterInputType`       |
| `float`, `float?`, `double`, `double?`                       | `FloatOperationFilterInputType`         |
| `Guid`, `Guid?`                                              | `UuidOperationFilterInputType`          |
| `DateOnly`, `DateOnly?`                                      | `LocalDateOperationFilterInputType`     |
| `TimeOnly`, `TimeOnly?`                                      | `LocalTimeOperationFilterInputType`     |
| `DateTime`, `DateTime?`, `DateTimeOffset`, `DateTimeOffset?` | `DateTimeOperationFilterInputType`      |
| `TimeSpan`, `TimeSpan?`                                      | `DurationOperationFilterInputType`      |
| `Uri`, `Uri?`                                                | `UriOperationFilterInputType`           |

# Configure the default convention inline

Pass a descriptor callback to `AddFiltering` when the default convention needs small schema-wide changes.

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .ArgumentName("filter"));
```

When you provide a callback, include `AddDefaults()` unless you configure the operations, runtime bindings, and provider yourself.

Common recipes:

| Goal                                        | API                                                                 |
| ------------------------------------------- | ------------------------------------------------------------------- |
| Change `where` to `filter`                  | `.ArgumentName("filter")`                                           |
| Rename `eq` to `equals`                     | `.Operation(DefaultFilterOperations.Equals).Name("equals")`         |
| Add an operation description                | `.Operation(id).Description("...")`                                 |
| Bind a runtime type globally                | `.BindRuntimeType<string, DefaultStringOperationFilterInputType>()` |
| Configure an operation filter type globally | `.Configure<StringOperationFilterInputType>(...)`                   |
| Disable `and` globally                      | `.AllowAnd(false)`                                                  |
| Disable `or` globally                       | `.AllowOr(false)`                                                   |
| Select a provider                           | `.Provider<TProvider>()` or `.Provider(provider)`                   |
| Add provider extensions                     | `.AddProviderExtension(extension)`                                  |

# Create a reusable convention class

Create a `FilterConvention` subclass when the same setup is reused across applications, modules, or packages.

```csharp
using HotChocolate.Data.Filters;

public sealed class ApiFilterConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor
            .AddDefaults()
            .ArgumentName("filter")
            .Operation(DefaultFilterOperations.Equals)
                .Name("equals")
                .Description("Matches values equal to the supplied value.");
    }
}
```

Register the convention:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering<ApiFilterConvention>();
```

For advanced naming rules, `FilterConvention` also exposes virtual naming methods such as `GetTypeName`. Treat those overrides as public schema contract changes.

# Extend an existing convention

Use `FilterConventionExtension` for additive changes to an already registered convention. This pattern is useful for packages and integrations.

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering()
    .AddConvention<IFilterConvention>(
        new FilterConventionExtension(c =>
            c.BindRuntimeType<string, DefaultStringOperationFilterInputType>()));
```

Convention extensions can merge runtime bindings, operation configurations, filter type configurations, and provider extensions. They can also replace the provider or a non-default argument name if they configure those values.

Spatial filtering uses this pattern: `.AddSpatialFiltering()` extends an existing IQueryable filtering convention with spatial operations, type bindings, and provider extensions.

# Rename the filter argument

`ArgumentName` is supported on the convention descriptor:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .ArgumentName("filter"));
```

Generated SDL changes for every field that uses that convention:

```graphql
type Query {
  products(filter: ProductFilterInput): [Product!]!
}
```

Argument names must be valid GraphQL names. Filter type descriptors do not rename the field argument. If a subset of fields needs a different argument name, register a named convention and opt those fields into that scope.

# Configure operation names and descriptions

Use `Operation(id)` on the convention descriptor. The ID is the internal key, and the name is the public GraphQL field.

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .Operation(DefaultFilterOperations.Equals)
            .Name("equals")
            .Description("Matches values equal to the supplied value."));
```

Before:

```graphql
input StringOperationFilterInput {
  eq: String
  neq: String
}
```

After:

```graphql
input StringOperationFilterInput {
  "Matches values equal to the supplied value."
  equals: String
  neq: String
}
```

Operation semantics should stay consistent across the schema. If `equals` means equality for strings, it should also mean equality for other operation input types that use `DefaultFilterOperations.Equals`.

Custom operation IDs should be higher than `1024` to avoid collisions with framework IDs. Implementing the execution behavior for custom operations belongs on the provider and handler side, so use [Extending Filtering](/docs/hotchocolate/v16/api-reference/extending-filtering) for that part.

# Bind runtime types globally

Runtime type bindings let the convention pick a different filter input type whenever a .NET type is inferred.

The following operation input type allows equality and prefix matching for inferred string filters:

```csharp
using HotChocolate.Data.Filters;
using HotChocolate.Types;

public sealed class DefaultStringOperationFilterInputType
    : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>();
    }
}
```

Bind it as the default for `string`:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .BindRuntimeType<string, DefaultStringOperationFilterInputType>());
```

Every inferred string filter under that convention now uses `DefaultStringOperationFilterInputType`. A field can still use a different operation input type through a custom [filter type](filter-types.md).

You can also bind an entity runtime type to a custom entity filter input:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .BindRuntimeType<Product, ProductFilterInputType>());
```

# Configure filter types from the convention

`Configure<TFilterType>` applies additional configuration whenever that filter type is used by the convention. For example, remove a string operation globally:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .Configure<StringOperationFilterInputType>(d =>
            d.Operation(DefaultFilterOperations.Contains).Ignore()));
```

Use this for broad policy changes. If only one field needs the change, prefer a custom filter type applied to that field.

# Configure named conventions and scopes

Registration APIs use the word `name`. Field APIs use the word `scope`. They refer to the same lookup key.

Register the default IQueryable convention and a named MongoDB convention:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering()
    .AddMongoDbFiltering("mongo");
```

Select the named convention with an attribute:

```csharp
[UseFiltering(Scope = "mongo")]
public IExecutable<Person> GetPersons(IMongoCollection<Person> collection)
{
    return collection.AsExecutable();
}
```

Select the same convention with a descriptor:

```csharp
public sealed class PersonQueriesType : ObjectType<PersonQueries>
{
    protected override void Configure(IObjectTypeDescriptor<PersonQueries> descriptor)
    {
        descriptor
            .Field(t => t.GetPersons(default!))
            .UseFiltering("mongo");
    }
}
```

You can register named conventions with the core filtering APIs too:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering()
    .AddFiltering<ApiFilterConvention>("public")
    .AddFiltering(c => c
        .AddDefaults()
        .ArgumentName("criteria"),
        name: "criteria");
```

If a field omits the scope, it uses the default convention.

# Select a provider or integration

Provider selection belongs to the convention. The provider must match the resolver result and backing data source.

| Data source or feature                       | Registration                                                                             | Notes                                                                                                        |
| -------------------------------------------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| LINQ, EF Core, or in-memory `IEnumerable<T>` | `.AddFiltering()`                                                                        | Uses the default IQueryable provider. The LINQ provider decides which expressions translate to the database. |
| Custom IQueryable configuration              | `.AddFiltering(c => c.AddDefaultOperations().BindDefaultTypes().UseQueryableProvider())` | Useful when you need the default provider but not every default convention setting.                          |
| MongoDB                                      | `.AddMongoDbFiltering()` or `.AddMongoDbFiltering("mongo")`                              | Uses MongoDB filtering. Use a named convention when MongoDB and IQueryable fields exist in the same schema.  |
| Marten                                       | `.AddMartenFiltering()`                                                                  | Registers Marten-specific filtering as the active filtering convention.                                      |
| RavenDB                                      | `.AddRavenFiltering()`                                                                   | Registers the RavenDB filtering convention and provider.                                                     |
| Spatial over IQueryable                      | `.AddFiltering().AddSpatialTypes().AddSpatialFiltering()`                                | Extends the IQueryable convention with spatial operation names, bindings, and handlers.                      |
| Custom provider                              | `.AddFiltering(c => c.AddDefaultOperations().BindDefaultTypes().Provider<TProvider>())`  | Provider and handler implementation details belong in the extending filtering guide.                         |

Handlers are registered on provider descriptors, not on the convention descriptor. For the default IQueryable provider, `UseQueryableProvider()` creates a `QueryableFilterProvider` and calls `AddDefaultFieldHandlers()`.

```csharp
descriptor.Provider(
    new QueryableFilterProvider(provider =>
        provider.AddDefaultFieldHandlers()));
```

A custom provider setup can add handler instances or factories through the provider descriptor:

```csharp
descriptor.Provider(
    new QueryableFilterProvider(provider =>
    {
        provider.AddDefaultFieldHandlers();
        provider.AddFieldHandler(MyFilterHandler.Create);
    }));
```

# Disable `and` and `or` globally

Use convention-level settings when all inferred filter input types under a convention should omit a combinator.

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .AllowAnd(false)
        .AllowOr(false));
```

For one filter type, configure the filter type instead.

# Keep naming stable during migration

Filter convention choices are public schema choices. The following changes can break clients and persisted operations:

- Renaming `where` with `ArgumentName`.
- Renaming operations such as `eq` or `contains`.
- Binding runtime types to different operation filter input types.
- Changing provider integrations in a way that adds or removes supported operations.
- Disabling `and` or `or`.

Use generated SDL snapshots before and after a convention change. For migrations, consider adding a named convention for new fields or a new schema version while keeping the existing default convention stable.

The default and MongoDB registration APIs include a `compatibilityMode` parameter for older filter naming behavior. Compare the generated SDL with the schema contract you need before enabling it.

# Test the generated SDL

Test convention changes by snapshotting the schema SDL. This catches argument renames, operation renames, missing operation names, and runtime binding changes before clients see them.

```csharp
using CookieCrumble;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Types;

public sealed class FilteringSchemaTests
{
    [Fact]
    public void Schema_Should_Match_Filtering_Contract()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddFiltering(c => c
                .AddDefaults()
                .ArgumentName("filter")
                .Operation(DefaultFilterOperations.Equals)
                    .Name("equals"))
            .Create();

        // assert
        schema.MatchSnapshot();
    }
}
```

Review the snapshot for the field argument and operation fields you changed:

```graphql
type Query {
  products(filter: ProductFilterInput): [Product!]!
}

input StringOperationFilterInput {
  equals: String
  neq: String
}
```

# Convention API reference

| Goal                                                 | API                                                                                                                 | Scope of effect                                     |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------- |
| Register default filtering                           | `.AddFiltering()`                                                                                                   | Default convention.                                 |
| Register named default filtering                     | `.AddFiltering("name")`                                                                                             | Named convention selected by `scope`.               |
| Register named inline convention                     | `.AddFiltering(c => ..., "name")`                                                                                   | Named convention selected by `scope`.               |
| Register reusable convention                         | `.AddFiltering<TConvention>()` or `.AddFiltering<TConvention>("name")`                                              | Default or named convention.                        |
| Select named convention on a field                   | `[UseFiltering(Scope = "name")]` or `.UseFiltering("name")`                                                         | One field.                                          |
| Select named convention with an explicit filter type | `[UseFiltering(typeof(ProductFilterInputType), Scope = "name")]` or `.UseFiltering<ProductFilterInputType>("name")` | One field.                                          |
| Change argument name                                 | `.ArgumentName("filter")`                                                                                           | All fields using the convention.                    |
| Add default operations                               | `.AddDefaultOperations()`                                                                                           | Operation names only.                               |
| Add default type bindings                            | `.BindDefaultTypes()`                                                                                               | Runtime type inference only.                        |
| Add all defaults                                     | `.AddDefaults()`                                                                                                    | Operations, bindings, and IQueryable provider.      |
| Select provider                                      | `.Provider<TProvider>()`, `.Provider(provider)`, or provider-specific defaults                                      | All fields using the convention.                    |
| Extend provider                                      | `.AddProviderExtension(...)`                                                                                        | Provider selected by the convention.                |
| Bind runtime type                                    | `.BindRuntimeType<TRuntime, TFilterType>()`                                                                         | Runtime type inference under the convention.        |
| Configure filter type globally                       | `.Configure<TFilterType>(...)`                                                                                      | Every use of that filter type under the convention. |
| Configure operation metadata                         | `.Operation(id).Name(...).Description(...)`                                                                         | Every operation field with that ID.                 |
| Disable combinators                                  | `.AllowAnd(false)` or `.AllowOr(false)`                                                                             | Inferred filter input types under the convention.   |

# Troubleshooting

| Problem                                                                                   | Likely cause                                                                                | Fix                                                                                                        |
| ----------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Schema creation fails because no provider was found                                       | A custom convention omitted `.AddDefaults()`, `UseQueryableProvider()`, or `Provider(...)`  | Add defaults or configure a provider explicitly.                                                           |
| Schema creation fails because an operation is unknown or unnamed                          | A filter type references an operation ID that the convention did not name                   | Add `.Operation(id).Name("...")`. A description alone is not enough.                                       |
| Schema creation fails because no matching binding was found                               | The convention cannot infer a filter input type for a runtime type                          | Add `.BindRuntimeType<TRuntime, TFilterType>()`, `.BindDefaultTypes()`, or `.AddDefaults()`.               |
| A field uses `where` after you configured `ArgumentName("filter")`                        | The field is using a different convention scope                                             | Register the convention as the default, or set `[UseFiltering(Scope = "...")]` / `.UseFiltering("...")`.   |
| A named MongoDB convention is registered, but the field behaves like IQueryable filtering | The field did not select the named scope                                                    | Use `[UseFiltering(Scope = "mongo")]` or `.UseFiltering("mongo")`.                                         |
| Query execution fails for an operation that appears in SDL                                | The provider cannot translate the accepted operation for the data source                    | Use the provider-specific integration or restrict the operation input type.                                |
| A custom handler is not used                                                              | The handler was not registered on the provider descriptor, or another handler matched first | Register the handler in provider configuration and review handler order.                                   |
| Renaming `eq` or `where` broke clients                                                    | Operation and argument names are public GraphQL schema fields                               | Treat convention renames as schema-breaking changes and migrate with SDL snapshots or a named convention.  |
| `and` or `or` still appears                                                               | The field uses another scope, or the filter type config adds it locally                     | Apply `AllowAnd(false)` / `AllowOr(false)` to the active convention or configure the specific filter type. |

# When filter types are the right tool

Conventions are broad. Filter types are local contracts. Prefer a filter type when you need to:

- Hide sensitive or expensive fields on one entity.
- Allow a richer string operation type on one search field.
- Rename one filter field without changing the same member everywhere.
- Configure explicit field binding for a public API.
- Keep a provider-safe allowlist for one resolver.

Example: keep string filters restrictive by convention, then allow `contains` on one field through a filter type:

```csharp
public sealed class SearchStringOperationFilterInputType
    : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>();
    }
}

public sealed class ProductFilterInputType : FilterInputType<Product>
{
    protected override void Configure(IFilterInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Name).Type<SearchStringOperationFilterInputType>();
        descriptor.Field(t => t.Brand);
        descriptor.Field(t => t.Price);
    }
}
```

Apply the local filter type to the field or bind it as the convention default for `Product` if every inferred product filter should use it.

# Next steps

- Start with the [filtering, sorting, and projections overview](index.md).
- Design field-level filter contracts with [filter types](filter-types.md).
- Extend execution with [custom filtering providers and handlers](/docs/hotchocolate/v16/api-reference/extending-filtering).
- Configure data-source integrations with [MongoDB](/docs/hotchocolate/v16/integrations/mongodb), [Marten](/docs/hotchocolate/v16/integrations/marten), RavenDB, and [spatial data](/docs/hotchocolate/v16/integrations/spatial-data).
- Compare the same convention concepts with [sort types](sort-types.md) and sorting configuration.
