---
title: Extending filtering
---

Hot Chocolate generates filter input types and translates them into data-source predicates automatically, but production APIs often need more control: expose only indexed fields, rename operations for schema compatibility, add a domain-specific search operation, or adapt translation for a particular database. This page shows how to extend filtering step by step, from changing the schema shape that clients see to wiring up provider translation for custom operations.

The page assumes familiarity with basic filtering. If you have not set up `[UseFiltering]`, explored built-in operation families, or defined a simple custom `FilterInputType<T>`, start with the [Filtering basics](/docs/hotchocolate/v16/build2/filtering-sorting-projections) page first.

## Vocabulary

The following terms are used throughout this page.

| Term                     | Meaning                                                                                                                                                                     |
| ------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Field**                | A member path such as `product.name` or `brand.id` that appears in a filter input type.                                                                                     |
| **Operation**            | A semantic comparison on a field value: `eq`, `contains`, `like`, or a custom operation. Each operation has a stable integer ID.                                            |
| **Operation input type** | A GraphQL input type such as `StringOperationFilterInput` that lists the available operation fields for one runtime type.                                                   |
| **Convention**           | Metadata that links operation IDs to GraphQL names, binds runtime types to operation input types, selects the active provider, and names the filter argument.               |
| **Provider**             | The runtime component that attaches a handler to each filter field during schema completion and translates filter input values into data-source predicates at request time. |
| **Handler**              | A provider-specific translator bound to a filter field or operation field. Each handler exposes a `CanHandle` check. The first matching handler wins.                       |

The schema and the provider must agree on three things: the operation ID, the operation input type, and handler support. When any of those three are misaligned, schema creation fails or queries execute without applying the filter.

```text
Client where input
        |
        v
Filter convention: argument name, operation names/IDs, runtime bindings, provider
        |
        v
Filter input types: fields and operation fields clients can write
        |
        v
Provider binds matching handlers during schema completion
        |
        v
Runtime visitor executes handlers for each operation field in the input
        |
        v
Provider-specific output: IQueryable expression, MongoDB filter definition, etc.
```

## Choose the right extension point

Use this table to decide which extension point applies to your goal before reading the detailed sections.

| Goal                                                             | Extension point                                                                                  |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Expose fewer fields on a filter input type                       | `FilterInputType<T>` with `BindFieldsExplicitly()`                                               |
| Expose fewer operations for a runtime type                       | Custom operation input type and `BindRuntimeType<TRuntime, TFilter>()`                           |
| Expose different operations on one specific field                | Field-level `.Type<TOperationInput>()`                                                           |
| Rename an existing operation                                     | `descriptor.Operation(DefaultFilterOperations.X).Name("...")`                                    |
| Add a new domain operation                                       | Operation ID, convention name, operation input field, and provider handler                       |
| Change how an existing operation translates                      | Provider extension or handler replacement                                                        |
| Target MongoDB                                                   | `.AddMongoDbFiltering()` from the MongoDB integration                                            |
| Target Marten                                                    | `.AddMartenFiltering()` from the Marten integration                                              |
| Target spatial data                                              | `.AddSpatialFiltering()` from the spatial integration                                            |
| Apply a default filter across all requests (tenant, soft-delete) | Resolver query composition or `IFilterContext` inspection in the resolver, not a custom provider |

## Scenario: product catalog filtering

The examples on this page build toward a product catalog API. The requirements are:

- Expose only indexed fields: `brandId`, `typeId`, and `name`.
- Restrict default string operations to `eq` and `contains` to avoid expensive full-table scans.
- Allow a richer `like` operation on `name` only.
- Keep translation aligned with the active queryable provider.

## Customize the filter input shape

### Restrict fields with explicit binding

By default Hot Chocolate generates a filter input field for every public property. Use `BindFieldsExplicitly()` to expose only the fields your API contract requires.

```csharp
// Types/Filtering/ProductFilterInputType.cs
using HotChocolate.Data.Filters;

namespace Catalog.Api.Types.Filtering;

public sealed class ProductFilterInputType : FilterInputType<Product>
{
    protected override void Configure(IFilterInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.BrandId);
        descriptor.Field(p => p.TypeId);
        descriptor.Field(p => p.Name);
    }
}
```

Register the custom filter type when setting up filtering.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddFiltering(c => c.AddDefaults())
    .AddType<ProductFilterInputType>();
```

### Restrict available operations for a runtime type

Hot Chocolate binds a runtime type such as `string` to an operation input type through the convention. Override this binding to expose a smaller set of operations for all `string` fields.

```csharp
// Types/Filtering/DefaultStringOperationFilterInputType.cs
using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace Catalog.Api.Types.Filtering;

public sealed class DefaultStringOperationFilterInputType
    : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.NotEquals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.NotContains).Type<StringType>();
    }
}
```

Bind the restricted type to the `string` runtime type in the convention.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddFiltering(c =>
        c.AddDefaults()
         .BindRuntimeType<string, DefaultStringOperationFilterInputType>())
    .AddType<ProductFilterInputType>();
```

All `string` fields now expose only `eq`, `neq`, `contains`, and `ncontains`.

### Use a different operation input type for one field

When one field needs a different set of operations than the bound default, apply a field-specific operation input type through the filter input type descriptor.

```csharp
// Types/Filtering/SearchStringOperationFilterInputType.cs
using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace Catalog.Api.Types.Filtering;

public sealed class SearchStringOperationFilterInputType
    : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Like).Type<StringType>();
    }
}
```

Apply it to the `Name` field inside `ProductFilterInputType`.

```csharp
// Types/Filtering/ProductFilterInputType.cs
public sealed class ProductFilterInputType : FilterInputType<Product>
{
    protected override void Configure(IFilterInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.BrandId);
        descriptor.Field(p => p.TypeId);

        descriptor.Field(p => p.Name)
                  .Type<SearchStringOperationFilterInputType>();
    }
}
```

`brandId` and `typeId` keep the restricted default string operations. `name` gains the additional `like` operation.

## Configure operation names and IDs

### Built-in operation IDs

`DefaultFilterOperations` defines the IDs that built-in handlers recognize.

| Range      | Operations                                                                                                                                                             |
| ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 0-1        | `Equals`, `NotEquals`                                                                                                                                                  |
| 2-3        | `Contains`, `NotContains`                                                                                                                                              |
| 4-5        | `In`, `NotIn`                                                                                                                                                          |
| 6-9        | `StartsWith`, `NotStartsWith`, `EndsWith`, `NotEndsWith`                                                                                                               |
| 10-11      | `And`, `Or`                                                                                                                                                            |
| 16-23      | Comparable: `GreaterThan`, `NotGreaterThan`, `GreaterThanOrEquals`, `NotGreaterThanOrEquals`, `LowerThan`, `NotLowerThan`, `LowerThanOrEquals`, `NotLowerThanOrEquals` |
| 24-27      | List: `Some`, `All`, `None`, `Any`                                                                                                                                     |
| 28         | `Like`                                                                                                                                                                 |
| 29         | `Data`                                                                                                                                                                 |
| 513-525    | `SpatialFilterOperations`                                                                                                                                              |
| above 1024 | Application-specific custom operations                                                                                                                                 |

`DefaultFilterOperations.Like` (ID 28) is a built-in ID. Adding it to an operation input type makes the field appear in the schema, but it requires a provider handler that understands `like` semantics. The default queryable handlers do not wire up `like` translation automatically.

For application-specific operations, define a constants class with IDs above 1024 to avoid conflicts with current and future framework IDs.

```csharp
// Types/Filtering/CustomFilterOperations.cs
namespace Catalog.Api.Types.Filtering;

public static class CustomFilterOperations
{
    public const int NormalizedSearch = 1025;
}
```

### Name an operation in the convention

Every operation ID used in the schema must have a name registered in the convention. Operations without a name cause schema creation to fail.

Use `FilterConventionExtension` to add names without replacing the full convention.

```csharp
// Extensions/CustomFilterConventionExtension.cs
using HotChocolate.Data.Filters;

namespace Catalog.Api.Extensions;

public sealed class CustomFilterConventionExtension : FilterConventionExtension
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        // Name the built-in Like operation so it is recognized in the convention.
        descriptor
            .Operation(DefaultFilterOperations.Like)
            .Name("like")
            .Description("Matches a pattern using SQL-style % and _ wildcards.");

        // Name an application-specific operation.
        descriptor
            .Operation(CustomFilterOperations.NormalizedSearch)
            .Name("normalizedSearch")
            .Description("Case-insensitive search using normalized text.");
    }
}
```

Register the extension alongside the base filtering setup.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddFiltering(c => c.AddDefaults()
                        .BindRuntimeType<string, DefaultStringOperationFilterInputType>())
    .AddConvention<IFilterConvention>(new CustomFilterConventionExtension())
    .AddType<ProductFilterInputType>();
```

`Operation(id).Name(...)` registers metadata only. It does not add a field to any operation input type and does not translate the operation. Those steps require adding an operation field and a provider handler.

## Add an operation field to an operation input type

After naming an operation in the convention, add the corresponding field to the operation input type where it should appear. The `SearchStringOperationFilterInputType` above declares the `like` field directly. Use `Configure<TFilterType>` on the convention descriptor when you want to modify an existing type.

```csharp
// Extensions/CustomFilterConventionExtension.cs
protected override void Configure(IFilterConventionDescriptor descriptor)
{
    descriptor
        .Operation(DefaultFilterOperations.Like)
        .Name("like");

    descriptor
        .Operation(CustomFilterOperations.NormalizedSearch)
        .Name("normalizedSearch");

    // Add the normalizedSearch operation field to SearchStringOperationFilterInputType.
    descriptor.Configure<SearchStringOperationFilterInputType>(d =>
    {
        d.Operation(CustomFilterOperations.NormalizedSearch).Type<StringType>();
    });
}
```

You can also declare operation fields directly inside the operation input type class, which is how the `like` field is defined above. Use one location for each operation field to keep the final input type unambiguous.

> **Important:** The operation field must be present on the operation input type _and_ the convention must name the operation ID _and_ the provider must have a matching handler. Missing any of these three causes schema creation to fail or the filter to be silently ignored.

## Translate an operation with a queryable handler

### When queryable handlers apply

The default `QueryableFilterProvider` translates filter input into `Expression<Func<T, bool>>` predicates. It applies when the resolver returns `IQueryable<T>`, `IEnumerable<T>`, or a queryable executable type. Use it when:

- The resolver returns one of those source types.
- The expression can be translated by the target LINQ provider (EF Core, in-memory LINQ, or similar).
- There is no dedicated provider integration (MongoDB, Marten, Spatial) for the data source.

> **Warning:** Database LINQ providers such as EF Core or Marten do not translate every LINQ expression. String manipulation, custom functions, and certain case operations can fail at runtime or produce inefficient queries. Test generated SQL or provider output for every custom handler before exposing it in production.

### Handler skeleton for a string operation

Derive from `QueryableStringOperationHandler` for string operations. The base class binds to `StringOperationFilterInputType` and an operation ID, parses the input value using `InputParser`, performs null checks, and delegates to `HandleOperation`.

```csharp
// Types/Filtering/QueryableLikeHandler.cs
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace Catalog.Api.Types.Filtering;

public sealed class QueryableLikeHandler : QueryableStringOperationHandler
{
    // The base class constructor requires InputParser for literal parsing.
    public QueryableLikeHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    // Match this handler to the Like operation ID.
    protected override int Operation => DefaultFilterOperations.Like;

    // Build the expression that implements like semantics.
    // parsedValue is the string the client supplied, or null.
    protected override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        Expression property = context.GetInstance();

        if (parsedValue is not string pattern)
        {
            throw new InvalidOperationException(
                "The like operation requires a non-null string pattern.");
        }

        // Convert the SQL-style pattern to a regex or string predicate.
        // This example delegates to a helper method that EF Core may not translate.
        // Verify translation with your target provider before enabling this in production.
        MethodInfo containsMethod = typeof(string)
            .GetMethod(nameof(string.Contains), [typeof(string)])!;

        return Expression.Call(property, containsMethod, Expression.Constant(pattern));
    }

    // Static factory used during handler registration.
    public static QueryableLikeHandler Create(FilterProviderContext context)
        => new(context.InputParser);
}
```

Handlers are singletons. Do not store per-request state on the handler instance. Use the `QueryableFilterContext` (the visitor context) to carry request-scoped state when needed.

### Register the handler through a provider extension

`QueryableFilterProviderExtension` adds handlers to the default queryable provider without replacing the entire provider. Handlers registered through a provider extension are prepended before the defaults, so a custom handler takes precedence over a built-in handler for the same operation.

```csharp
// Extensions/CustomFilterConventionExtension.cs
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace Catalog.Api.Extensions;

public sealed class CustomFilterConventionExtension : FilterConventionExtension
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Like).Name("like");
        descriptor.Operation(CustomFilterOperations.NormalizedSearch).Name("normalizedSearch");

        // SearchStringOperationFilterInputType declares the like field.

        // Register the handler through a provider extension.
        descriptor.AddProviderExtension(
            new QueryableFilterProviderExtension(p =>
                p.AddFieldHandler(QueryableLikeHandler.Create)));
    }
}
```

Register everything in the DI container.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddFiltering(c =>
        c.AddDefaults()
         .BindRuntimeType<string, DefaultStringOperationFilterInputType>())
    .AddConvention<IFilterConvention>(new CustomFilterConventionExtension())
    .AddType<ProductFilterInputType>();
```

## Provider-specific extension points

### IQueryable (default data package)

| Type                               | Role                                                                                      |
| ---------------------------------- | ----------------------------------------------------------------------------------------- |
| `QueryableFilterProvider`          | Default provider for `IQueryable` and `IEnumerable` sources.                              |
| `QueryableFilterProviderExtension` | Adds or prepends handlers without replacing the provider.                                 |
| `QueryableOperationHandlerBase`    | Base for operation-level handlers with full control over parsing and expression building. |
| `QueryableStringOperationHandler`  | Narrows `CanHandle` to `StringOperationFilterInputType` and a single operation ID.        |

### MongoDB

`.AddMongoDbFiltering()` replaces the provider with `MongoDbFilterProvider`, which generates `FilterDefinition<T>` objects instead of expression trees. Custom handlers for MongoDB must derive from MongoDB-specific handler bases and produce MongoDB filter definitions.

Extending MongoDB filtering follows the same structural pattern as queryable handlers: name the operation in the convention, add the operation field to the relevant operation input type, and register a MongoDB-compatible handler. See the [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb) page for scoped registration and provider setup.

### Marten

`.AddMartenFiltering()` registers a Marten-compatible provider. Marten uses its own LINQ provider, which does not translate every expression that compiles cleanly in-memory. Test handler output against the Marten LINQ provider before enabling a custom handler for Marten resolvers. See the [Marten integration](/docs/hotchocolate/v16/integrations/marten) page for setup details.

### Raven

`.AddRavenFiltering()` is the Raven provider registration in v16 source. Treat Raven filters as provider-specific even when the expression resembles the default queryable path. Verify the generated Raven query or run an integration test against Raven before exposing a custom operation on a Raven-backed resolver.

### Spatial

`.AddSpatialFiltering()` adds geometry operation input types and spatial handlers to the queryable provider. `SpatialFilterOperations` occupies IDs 513-525. `GeometryFilterInputType` and its generic variant expose geometry operation fields. See the [Spatial data integration](/docs/hotchocolate/v16/integrations/spatial-data) page for setup details.

> **Note:** An operation field that appears in the schema is useful only when the active provider for that resolver can translate it. A resolver that uses the MongoDB provider cannot use handlers designed for `IQueryable`, and vice versa.

## Test filtering extensions

Validating a custom filter requires three distinct layers of testing.

### Schema shape tests

Snapshot the generated SDL to verify that only the intended fields and operations are present, and that unintended fields or operations are absent.

```csharp
// Tests/Filtering/ProductFilterInputTypeTests.cs
using CookieCrumble;
using HotChocolate;
using HotChocolate.Execution;

namespace Catalog.Api.Tests.Filtering;

public sealed class ProductFilterInputTypeTests
{
    [Fact]
    public async Task Schema_Should_MatchSnapshot_When_ProductFilterInputTypeIsRegistered()
    {
        // arrange
        ISchema schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddFiltering(c =>
                c.AddDefaults()
                 .BindRuntimeType<string, DefaultStringOperationFilterInputType>())
            .AddConvention<IFilterConvention>(new CustomFilterConventionExtension())
            .AddType<ProductFilterInputType>()
            .AddQueryType(d => d.Field("products")
                .Type<ListType<ObjectType<Product>>>()
                .UseFiltering<ProductFilterInputType>()
                .Resolve(Array.Empty<Product>()))
            .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }
}
```

### Schema initialization and handler binding tests

Build the schema with the exact convention and provider extension used in production and confirm it initializes without error. Cover failure cases: missing operation name, missing provider, missing runtime type binding, and missing handler.

```csharp
// Tests/Filtering/FilterConventionTests.cs
[Fact]
public async Task Schema_Should_Throw_When_LikeOperationHasNoHandler()
{
    // arrange
    Func<Task> buildSchema = async () =>
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddFiltering(c => c.AddDefaults())
            // Register the operation name but omit the handler.
            .AddConvention<IFilterConvention>(new FilterConventionExtension(d =>
                d.Operation(DefaultFilterOperations.Like).Name("like")))
            .AddType<SearchStringOperationFilterInputType>()
            .AddQueryType(d => d.Field("products")
                .Type<ListType<ObjectType<Product>>>()
                .UseFiltering<ProductFilterInputType>()
                .Resolve(Array.Empty<Product>()))
            .BuildSchemaAsync();
    };

    // assert
    await buildSchema.Should().ThrowAsync<SchemaException>();
}
```

### Provider translation tests

For queryable handlers, follow the style used in `QueryableFilterVisitorStringTests`: create a schema and executor using the same convention and provider extension, execute a query that passes a filter input, and assert the resulting data or predicate behavior.

```csharp
// Tests/Filtering/QueryableLikeHandlerTests.cs
[Fact]
public async Task LikeHandler_Should_ReturnMatchingProducts_When_PatternIsApplied()
{
    // arrange
    Product[] products =
    [
        new Product { Name = "Wireless Keyboard" },
        new Product { Name = "USB Mouse" },
        new Product { Name = "Wireless Mouse" },
    ];

    // act
    IExecutionResult result = await new ServiceCollection()
        .AddGraphQLServer()
        .AddFiltering(c =>
            c.AddDefaults()
             .BindRuntimeType<string, DefaultStringOperationFilterInputType>())
        .AddConvention<IFilterConvention>(new CustomFilterConventionExtension())
        .AddType<ProductFilterInputType>()
        .AddQueryType(d => d.Field("products")
            .Type<ListType<ObjectType<Product>>>()
            .UseFiltering<ProductFilterInputType>()
            .Resolve(products.AsQueryable()))
        .ExecuteRequestAsync(
            """
            {
                products(where: { name: { like: "Wireless" } }) {
                    name
                }
            }
            """);

    // assert
    result.MatchSnapshot();
}
```

For EF Core, Marten, or other database LINQ providers, run integration tests against the real provider or a provider-compatible test harness. In-memory LINQ accepts many expressions that a database provider rejects at runtime.

## Troubleshooting

### Operation is missing from the schema

- The operation was named in the convention but was not added to the relevant operation input type through `descriptor.Operation(id).Type<...>()` or directly in the type class.
- The runtime type was not bound to the expected operation input type. Check `BindRuntimeType<TRuntime, TFilter>()`.
- The resolver uses a filtering scope different from where the convention change was registered. Check `[UseFiltering(Scope = "...")]` and scope registration.

### Schema creation fails at startup

- The operation ID has no convention name. Every ID referenced in an operation input type must be named.
- The operation field has no matching handler in the active provider. Add the handler through `QueryableFilterProviderExtension` or the provider-specific registration.
- The runtime type binding is missing for a custom scalar or custom operation input type.
- The provider was not configured. Check that `AddFiltering(c => c.AddDefaults())` or an equivalent provider setup is present.

### Query validates but execution fails or returns wrong results

- The handler parses a value shape different from the GraphQL input type declared on the operation field.
- The handler assumes a non-null value, but the operation input type permits null for the pattern argument.
- The handler returns a predicate that does not match the field accessor path. Use `context.GetInstance()` to get the current member expression.

### Works in memory but fails against a database

- The expression produced by `HandleOperation` cannot be translated by the target LINQ provider.
- The resolver should use a provider-specific integration (MongoDB, Marten, Spatial) rather than the default queryable provider.
- String manipulation such as `ToLower()` or `ToUpper()` can prevent index use or raise a translation error. Prefer database-native collation or full-text search features when targeting a database.

### Handler behaves differently between requests

- The handler stores request-scoped state in an instance field. Handlers are registered as singletons. Move any per-request state to the `QueryableFilterContext` visitor context.

### Multiple providers or conventions conflict

- Register provider-specific conventions under scopes: `.AddFiltering(scope: "mongo")` with `[UseFiltering(Scope = "mongo")]` on the resolver.
- A resolver that uses the MongoDB provider cannot apply handlers designed for the default queryable provider and vice versa.

## When a resolver argument or explicit input type is a better fit

A custom filter operation is not always the right solution.

- **Tenant or soft-delete defaults:** Apply the filter in the resolver or service layer before returning a queryable. `IFilterContext` inspection in the resolver (`filterContext.Handled(false)`) can mark a filter as handled when your code already applied it. This avoids adding convention and provider logic for what is a cross-cutting query concern.
- **One-off domain operations:** When a domain operation is specific to a single field and would only be used by a handful of queries, an explicit GraphQL argument on the field combined with resolver logic is straightforward to test and reason about.
- **Type-safe input shapes:** When the operation input needs a complex input type rather than a scalar, an explicit input type argument gives full control without extending the filter convention.

Reserve convention and provider extension for operations that must participate in the composable filter contract and integrate with the visitor-based predicate system.

## Complete registration example

The following shows the full registration for the product catalog scenario: restricted default string operations, a field-specific search type with `like`, the convention extension naming and adding the operation field, and the queryable handler.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddFiltering(c =>
        c.AddDefaults()
         .BindRuntimeType<string, DefaultStringOperationFilterInputType>())
    .AddConvention<IFilterConvention>(new CustomFilterConventionExtension())
    .AddType<ProductFilterInputType>();
```

```csharp
// Extensions/CustomFilterConventionExtension.cs
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace Catalog.Api.Extensions;

public sealed class CustomFilterConventionExtension : FilterConventionExtension
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        // Name the built-in Like operation.
        descriptor
            .Operation(DefaultFilterOperations.Like)
            .Name("like");

        // SearchStringOperationFilterInputType declares the like field.

        // Register the queryable handler for like.
        descriptor.AddProviderExtension(
            new QueryableFilterProviderExtension(p =>
                p.AddFieldHandler(QueryableLikeHandler.Create)));
    }
}
```

## Next steps

- [Filtering basics](/docs/hotchocolate/v16/build2/filtering-sorting-projections): `[UseFiltering]`, generated filter types, built-in operations, and basic custom filter types.
- [Sorting](/docs/hotchocolate/v16/build2/filtering-sorting-projections/sort-types): sorting conventions and handlers follow a parallel structure to filtering.
- [Projections](/docs/hotchocolate/v16/build2/filtering-sorting-projections/projection-options): middleware order and database translation boundaries.
- [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb): scoped provider registration and MongoDB-specific filter handlers.
- [Marten integration](/docs/hotchocolate/v16/integrations/marten): Marten LINQ provider constraints and integration setup.
- [Spatial data integration](/docs/hotchocolate/v16/integrations/spatial-data): `SpatialFilterOperations`, geometry input types, and spatial handlers.
- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis): assign cost weights to expensive filter operations.
