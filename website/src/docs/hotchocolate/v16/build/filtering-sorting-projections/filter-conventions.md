---
title: Filter conventions
---

A filter convention provides schema-wide configuration for filtering in Hot Chocolate. It determines the default `where` argument name, operation field names and descriptions, runtime type bindings, and the provider responsible for translating filter input at execution time.

This page covers the convention APIs. To learn about input shapes for a specific entity or field, see [filter types](filter-types.md). For custom providers and handlers, refer to [Extending Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering).

# How filtering conventions work

Filtering applies a convention before a request reaches your data source:

```text
Resolver field
  -> [UseFiltering] or .UseFiltering(scope)
  -> Convention selected by scope or name
  -> Determines filter input type, argument name, operation names, runtime bindings
  -> Chooses filter provider and handlers
  -> Produces IQueryable expression or provider-native filter
```

Key terms:

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

Choose a filter convention when you want a rule to apply to every field that uses that convention:

- Change the `where` argument name for a schema or scope
- Rename or describe operations globally
- Bind runtime types to different operation filter input types
- Select a provider, such as IQueryable, MongoDB, Marten, RavenDB, or a custom provider
- Disable `and` or `or` for all inferred filter types in a convention
- Add a named convention for another data source or migration path

Use a filter type when a single entity or field needs a smaller input shape, a field allowlist, a field rename, or a field-specific operation type. Use a provider or handler only when you need to change translation behavior.

# Register the default convention

First, install `HotChocolate.Data`. Then, register filtering with your schema configuration:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering();
```

Fields opt in to filtering using attributes or descriptors:

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

Or, using a descriptor:

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

The default convention sets these schema defaults:

| Default                 | Value                                                                |
| ----------------------- | -------------------------------------------------------------------- |
| Argument name           | `where`                                                              |
| Provider                | `QueryableFilterProvider` with default field handlers                |
| Runtime type bindings   | Common scalar runtime types to built-in operation filter input types |
| Combinators             | `and` and `or` enabled                                               |
| Default operation names | Added by `AddDefaultOperations()`                                    |

Calling `AddFiltering()` is equivalent to configuring a convention with `AddDefaults()`. The `AddDefaults()` method includes:

| Building block           | What it adds                                               |
| ------------------------ | ---------------------------------------------------------- |
| `AddDefaultOperations()` | Built-in operation IDs and public GraphQL operation names. |
| `BindDefaultTypes()`     | Common .NET runtime type bindings.                         |
| `UseQueryableProvider()` | The IQueryable provider and its default handlers.          |

# Default operation names

Each operation has a stable internal ID from `DefaultFilterOperations`. The convention maps these IDs to public GraphQL field names:

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

# Default runtime type bindings

The default convention binds common .NET runtime types to their corresponding operation filter input types:

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

You can pass a descriptor callback to `AddFiltering` to make small, schema-wide changes to the default convention:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .ArgumentName("filter"));
```

When using a callback, include `AddDefaults()` unless you plan to configure all operations, runtime bindings, and the provider yourself.

Common configuration examples:

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

If you want to reuse the same setup across applications, modules, or packages, create a subclass of `FilterConvention`:

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

Register your convention:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering<ApiFilterConvention>();
```

For advanced naming rules, `FilterConvention` exposes virtual naming methods such as `GetTypeName`. Treat these overrides as public schema contract changes.

# Extend an existing convention

To make additive changes to an already registered convention, use `FilterConventionExtension`. This approach is helpful for packages and integrations:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering()
    .AddConvention<IFilterConvention>(
        new FilterConventionExtension(c =>
            c.BindRuntimeType<string, DefaultStringOperationFilterInputType>()));
```

Convention extensions can merge runtime bindings, operation configurations, filter type configurations, and provider extensions. They can also replace the provider or a non-default argument name if those values are configured.

For example, spatial filtering uses this pattern: `.AddSpatialFiltering()` extends an existing IQueryable filtering convention with spatial operations, type bindings, and provider extensions.

# Rename the filter argument

You can set the filter argument name using `ArgumentName` on the convention descriptor:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .ArgumentName("filter"));
```

This changes the generated SDL for every field using that convention:

```graphql
type Query {
  products(filter: ProductFilterInput): [Product!]!
}
```

Argument names must be valid GraphQL names. Filter type descriptors do not rename the field argument. If only some fields need a different argument name, register a named convention and opt those fields into that scope.

# Configure operation names and descriptions

To change operation names or add descriptions, use `Operation(id)` on the convention descriptor. The ID is the internal key, and the name is the public GraphQL field:

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

Operation semantics should remain consistent across the schema. If `equals` means equality for strings, it should also mean equality for other operation input types that use `DefaultFilterOperations.Equals`.

Custom operation IDs should be greater than `1024` to avoid collisions with framework IDs. To implement execution behavior for custom operations, see [Extending Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering).

# Bind runtime types globally

Runtime type bindings allow the convention to select a different filter input type whenever a .NET type is inferred.

For example, the following operation input type enables equality and prefix matching for inferred string filters:

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

Bind this as the default for `string`:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .BindRuntimeType<string, DefaultStringOperationFilterInputType>());
```

Now, every inferred string filter under that convention uses `DefaultStringOperationFilterInputType`. You can still use a different operation input type for a field by applying a custom [filter type](filter-types.md).

You can also bind an entity runtime type to a custom entity filter input:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .BindRuntimeType<Product, ProductFilterInputType>());
```

# Configure filter types from the convention

The `Configure<TFilterType>` method applies additional configuration whenever that filter type is used by the convention. For example, to remove a string operation globally:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .Configure<StringOperationFilterInputType>(d =>
            d.Operation(DefaultFilterOperations.Contains).Ignore()));
```

Use this approach for broad policy changes. If only one field needs the change, use a custom filter type for that field instead.

# Configure named conventions and scopes

In registration APIs, the term `name` is used, while field APIs use `scope`. Both refer to the same lookup key.

To register the default IQueryable convention and a named MongoDB convention:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering()
    .AddMongoDbFiltering("mongo");
```

Select the named convention using an attribute:

```csharp
[UseFiltering(Scope = "mongo")]
public IExecutable<Person> GetPersons(IMongoCollection<Person> collection)
{
    return collection.AsExecutable();
}
```

Or select it with a descriptor:

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

You can also register named conventions with the core filtering APIs:

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

If a field does not specify a scope, it uses the default convention.

# Select a provider or integration

Provider selection is handled by the convention. The provider must match the resolver result and the underlying data source.

| Data source or feature                       | Registration                                                                             | Notes                                                                                                           |
| -------------------------------------------- | ---------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| LINQ, EF Core, or in-memory `IEnumerable<T>` | `.AddFiltering()`                                                                        | Uses the default IQueryable provider. The LINQ provider determines which expressions translate to the database. |
| Custom IQueryable configuration              | `.AddFiltering(c => c.AddDefaultOperations().BindDefaultTypes().UseQueryableProvider())` | Use this when you want the default provider but not every default convention setting.                           |
| MongoDB                                      | `.AddMongoDbFiltering()` or `.AddMongoDbFiltering("mongo")`                              | Uses MongoDB filtering. Use a named convention if both MongoDB and IQueryable fields are in the same schema.    |
| Marten                                       | `.AddMartenFiltering()`                                                                  | Registers Marten-specific filtering as the active filtering convention.                                         |
| RavenDB                                      | `.AddRavenFiltering()`                                                                   | Registers the RavenDB filtering convention and provider.                                                        |
| Spatial over IQueryable                      | `.AddFiltering().AddSpatialTypes().AddSpatialFiltering()`                                | Extends the IQueryable convention with spatial operation names, bindings, and handlers.                         |
| Custom provider                              | `.AddFiltering(c => c.AddDefaultOperations().BindDefaultTypes().Provider<TProvider>())`  | Provider and handler implementation details are covered in the extending filtering guide.                       |

Handlers are registered on provider descriptors, not on the convention descriptor. For the default IQueryable provider, `UseQueryableProvider()` creates a `QueryableFilterProvider` and calls `AddDefaultFieldHandlers()`:

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

To remove combinators from all inferred filter input types under a convention, use convention-level settings:

```csharp
builder.Services
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .AllowAnd(false)
        .AllowOr(false));
```

If you want to disable these for only one filter type, configure that filter type directly instead.

# Keep naming stable during migration

Filter convention choices are part of your public schema contract. The following changes can break clients and persisted operations:

- Renaming `where` with `ArgumentName`
- Renaming operations such as `eq` or `contains`
- Binding runtime types to different operation filter input types
- Changing provider integrations in a way that adds or removes supported operations
- Disabling `and` or `or`

Use generated SDL snapshots before and after a convention change to catch breaking changes. For migrations, consider adding a named convention for new fields or a new schema version, while keeping the existing default convention stable.

The default and MongoDB registration APIs include a `compatibilityMode` parameter for older filter naming behavior. Compare the generated SDL with your required schema contract before enabling it.

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

| Problem                                                                                   | Likely cause                                                                                | Solution                                                                                                   |
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

Conventions are broad, while filter types are local contracts. Use a filter type when you need to:

- Hide sensitive or expensive fields on a single entity
- Allow a richer string operation type on one search field
- Rename a filter field without changing the same member everywhere
- Configure explicit field binding for a public API
- Keep a provider-safe allowlist for one resolver

For example, you can keep string filters restrictive by convention, but allow `contains` on one field through a filter type:

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

Apply the local filter type to the field, or bind it as the convention default for `Product` if every inferred product filter should use it.

# Next steps

- Begin with the [filtering, sorting, and projections overview](index.md)
- Design field-level filter contracts using [filter types](filter-types.md)
- Extend execution with [custom filtering providers and handlers](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering)
- Configure data-source integrations for [MongoDB](/docs/hotchocolate/v16/_leagcy/integrations/mongodb), [Marten](/docs/hotchocolate/v16/_leagcy/integrations/marten), RavenDB, and [spatial data](/docs/hotchocolate/v16/_leagcy/integrations/spatial-data)
- Compare these concepts with [sort types](sort-types.md) and sorting configuration
