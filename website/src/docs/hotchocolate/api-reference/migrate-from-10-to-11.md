---
title: Migrate from Hot Chocolate GraphQL server 10 to 11
---

This guide will walk you through the manual migration steps to get you Hot Chocolate GraphQL server to version 11.

As a general preparation, we recommend removing all HotChocolate.\* package references from your project. Then start by adding the `HotChocolate.AspNetCore` package. The server package now contains most of the needed packages.

When do I need to add other Hot Chocolate packages explicitly?

We have now added the most common packages to the Hot Chocolate core. But there are certain areas where we still need to add some additional packages.

| Package                                  | Topic                                                                                                                                                                |
| ---------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| HotChocolate.AspNetCore.Authorization    | The authorization package adds the authorization directive and integrates with Microsoft Authorization Policies                                                      |
| HotChocolate.Data                        | The new data package represents our integration with all kinds of data sources. This package provides the fundamentals for filtering, sorting, and projection logic. |
| HotChocolate.Types.Spatial               | This package provides GeoJson spatial types.                                                                                                                         |
| HotChocolate.Data.Spatial                | The package integrates the spatial types with the data package to allow for spatial filtering, sorting, and projections.                                             |
| HotChocolate.Subscriptions.Redis         | The in-memory subscription provider, is now integrated by default. To have an integration with Redis, you need to add this package.                                  |
| HotChocolate.PersistedQueries.FileSystem | This package provides a persisted query storage for the file system.                                                                                                 |
| HotChocolate.PersistedQueries.Redis      | This package provides a persisted query storage for Redis.                                                                                                           |

# ASP.NET Core

One of the main focuses of version 11 was to create a new configuration API that brings all our builders together into one unified API. This also means that we had to introduce breaking changes to the way we
configure schemas.

After you have cleaned up your packages, head over to the `Startup.cs` to start with the new configuration API migration.

## ConfigureServices

In your `Startup.cs` head over to the `ConfigureServices` methods.
The configuration of a schema has slightly changed, and the new configuration API has replaced the `SchemaBuilder`.

We now start with `AddGraphQLServer` to define a new GraphQL server, `AddGraphQLServer`, returns the new `IRequestExecutorBuilder` that lets us apply all the configuration methods that used to be on the `SchemaBuilder`, `StitchingBuilder` and the `QueryExecutionBuilder`.

**Old:**

```csharp
services.AddGraphQL(sp  =>
    SchemaBuilder.New()
        .AddServices(sp)
        .AddQueryType<QueryType>()
        .AddMutationType<MutationType>()
        ...
        .Create());
```

**New:**

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<QueryType>()
    .AddMutationType<MutationType>()
    ...
```

If you were using the `QueryRequestBuilder` to configure request options or change the request pipeline, you need to add those things to the configuration chain of the ```IRequestExecutorBuilder`.

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<QueryType>()
    .AddMutationType<MutationType>()
    ...
    .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(180));
```

## Configure

After migrating the schema configuration, the next area that has fundamentally changed is the schema middleware.

Hot Chocolate server now embraces the new endpoint routing API from ASP.NET core and with that brings a lot of new features. Head over [here](aspnetcore) to read more about the ASP.NET Core integration.

**Old:**

```csharp
app.UseGraphQL();
```

**New:**

```csharp
app.UseRouting();

// routing area

app.UseEndpoints(x => x.MapGraphQL());
```

# DataLoaders

With Hot Chocolate server 11, we have embraced the new DataLoader spec version 2. With that, we have decoupled the scheduler from the DataLoader itself, meaning you now have to pass on the `IBatchScheduler` to the base implementation of the DataLoader.
Apart from that, DataLoader now uses `ValueTask` instead of `Task` when doing async work.

If you were adding the `DataLoaderRegistry` to the services, remove that code since `service.AddDataLoaderRegistry` is no longer needed.

**Old:**

```csharp
public class FooDataLoader : DataLoaderBase<Guid, Foo>
{
    private readonly IFooRepository _fooRepository;

    public FooDataLoader(IFooRepository fooRepository)
    {
        _fooRepository = fooRepository;
    }


    protected override async Task<IReadOnlyList<Result<Foo>>> FetchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        ....
    }
}
```

**New:**

```csharp
public class FooDataLoader : DataLoaderBase<Guid, Foo>
{
    private readonly IFooRepository _fooRepository;

    public FooDataLoader(
        //    ▼
        IBatchScheduler scheduler,
        IFooRepository fooRepository)
        : base(scheduler)
    {
        _fooRepository = fooRepository;
    }


    //                          ▼
    protected override async ValueTask<IReadOnlyList<Result<Foo>>> FetchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {

    ....
}
```

# Node Resolver

With version 11, we have reworked how Relay node types are defined. Furthermore, we added pure code-first (annotation-based) support.

**Old:**

```csharp
descriptor
    .AsNode()
    .IdField(d => d.Id)
    .NodeResolver(async (ctx, id) => await ctx
        .DataLoader<FooDataLoader>()
        .LoadAsync(id, ctx.RequestAborted))
```

**New:**

The following example essentially aligns very closely to the old variant.

```csharp
descriptor
    .ImplementsNode()
    .IdField(d => d.Id)
    .ResolveNode(async (ctx, id) => await ctx
        .DataLoader<FooDataLoader>()
        .LoadAsync(id, ctx.RequestAborted))
```

But, we can now also use an external resolver like with standard resolvers. This allows us to write better testable code that takes advantage of the method parameter injection we use in everyday resolvers.

```csharp
descriptor
    .ImplementsNode()
    .IdField(d => d.Id)
    .ResolveNodeWith<NodeResolver>(t => t.GetNodeAsync(default, default));
```

But we can go even further now with pure code-first (annotation-based) support. By just annotating the entity with the `NodeAttribute`, we essentially told the schema builder that this is a node. The type initialization can then try to infer the node resolver directly from the type.

```csharp
[Node]
public class MyEntity
{
    public string Id { get; set; }

    public async Task<MyEntity> GetAsync(....)
    {
        ....
    }
}
```

Often, however, we want the repository logic decoupled from our domain object/entity. In this case, we can specify the entity resolver type.

```csharp
[Node(NodeResolverType = typeof(MyEntityResolver))]
public class MyEntity
{
    public string Id { get; set; }
}

public class MyEntityResolver
{
    public async Task<MyEntity> GetAsync(....)
    {
        ....
    }
}
```

There are more variants possible, but to give an impression of the new convenience and flexibility around nodes. As a side note, if you do not want the node attribute on the domain objects, you can also now add your very own attribute or interface to mark this and rewrite that in the schema building process to the `NodeAttribute`.

# Pagination

The first thing to note around pagination is that we listened to a lot of feedback and have removed the `PaginationAmountType`.

Moreover, we have introduced new PagingOptions, which can be set with the new configuration API on the schema level. With the new options, you can configure the `MaxPageSize`, `DefaultPageSize` and whether the total count shall be included `IncludeTotalCount`.

```csharp
builder.SetPagingOptions(
    new PagingOptions()
    {
        MaxPageSize = searchOptions.PaginationAmount,
        DefaultPageSize = searchOptions.PaginationAmount,
        IncludeTotalCount = true
    });
```

Further, you can override the paging option on the resolver level.

```csharp
[UsePaging(MaxPageSize = 100)]
```

```csharp
descriptor.Field(...).UsePaging(maxPageSize = 100)...
```

# Enums

HotChocolate server 11 now follows the spec recommendation with the new enum name conventions and formats the enum values by default as UPPER_SNAIL_CASE.

To avoid breaking changes to your schema, you will have to override the naming convention:

**Configuration:**

```csharp
    builder
        .AddConvention<INamingConventions>(new CompatibilityNamingConvention())
```

**Convention:**

```csharp
    public class CompatibilityNamingConvention
        : DefaultNamingConventions
    {
        public override NameString GetEnumValueName(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.ToString().ToUpperInvariant();
        }
    }
```

# IResolverContext.Source

The source result stack was removed from the resolver context for performance reasons. If you need such a functionality, you can write a middleware that aggregates the resulting path on the scoped context.

**Old:**

```csharp
    public class FooType : ObjectType<Foo>
    {
        private static readonly object _empty = new object();

        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor
                .Field("bar")
                .Type<NonNullType<BarType>>()
                .Resolver(_empty);
        }
    }

    public class BarType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("baz")
                .Type<DateTimeType>()
                .Resolve(ctx =>
                    {
                        Foo foo = (Foo)ctx.Source.Pop().Peek();
                        return foo.Baz;
                    });
        }
    }

```

**New:**

```csharp
    public class FooType : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor
                .Field("bar")
                .Type<NonNullType<BarType>>()
                .Resolve(
                    ctx =>
                    {
                        ctx.ScopedContextData =
                          ctx.ScopedContextData.SetItem(n ameof(Foo), ctx.Parent<Foo>());

                        return new object();
                    });
        }
    }

    public class BarType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("baz")
                .Type<DateTimeType>()
                .Resolve(
                    ctx =>
                    {
                        if (ctx.ScopedContextData.TryGetValue(
                                nameof(Foo),
                                out object? potentialFoo) &&
                            potentialFoo is Foo foo)
                        {
                            return foo.Baz;
                        }

                        throw new GraphQLException(
                            ErrorBuilder.New()
                                .AddLocation(ctx.Field.SyntaxNode)
                                .SetMessage("Foo was not pushed down.")
                                .SetPath(ctx.Path)
                                .Build());
                    });
        }
    }
```

## Autorization

If you use authorization, you need to add a package reference to `HotChocolate.AspNetCore.Authorization`.

Old

```csharp
    builder.AddAuthorizeDirectiveType()
```

New

```csharp
    builder.AddAuthorization()
```

## TypeBinding

Old

```csharp
    builder.BindClrType<DateTime, DateTimeType>()
```

New

```csharp
    builder.BindRuntimeType<DateTime, DateTimeType>()
```

## FieldMiddleware

Old

```csharp
    builder.Use<CustomMiddleware>()
```

New

```csharp
    builder.UseField<CustomMiddleware>()
```

# Stitching

## Configuration

In Version 11 there is no stiching builder anymore. Stiching can be configured on the normal schema
builder.
Old:

```
    services.AddStitchedSchema(x => ....);
```

New:

```
    services.AddGraphQLServer()....
```

### AddSchemaFromHttp

Registering a remote schema has slightly changed in V11. You can also remote the root types directly
when you configure the remote schema:
Old:

```csharp
    builder.AddSchemaFromHttp("SomeSchema").IngoreRootTypes("SomeSchema");
```

New:

```csharp
    builder.AddRemoteSchema("SomeSchema", ignoreRootTypes: true);
```

## AddSchemaConfiguration

As we do not have a dedicated schema builder for stitched schemas, you can just configure the schema
directly on this builder:

Old

```csharp
    services.AddStitchedSchema(x => x.AddSchemaConfiguration(y => y.RegisterType<FooType>()));
```

New

```csharp
    services
        .AddGraphQLServer()
        .AddType<FooType>();
```

## SetExecutionOptions

Execution options can now be configured on the root schema directly:

Old

```csharp
    services.AddStitchedSchema(
        x => x.SetExecutionOptions(
            new QueryExecutionOptions
                {
                    TracingPreference = TracingPreference.OnDemand
                }));
```

New

```csharp
    services
        .AddGraphQLServer()
        .ModifyRequestOptions(x => x.TracingPreference = TracingPreference.OnDemand);
```

## Configuring a downstream schema

In case you want to configure a downstream schema, you can now just use the schema builder:

```csharp
    services
        .AddGraphQLServer()
        .AddRemoteSchema("SomeSchema");
    services
        .AddGraphQL("SomeSchema")
        .AddType(new IntType());
```

## PaginationAmount

The `PaginationAmount` scalar was removed in v11. `first` and `last` are now just normal `Int`.
To avoid breaking schemas on the stitched schema, you can add a rewriter that rewrites all
`first: Int` and `last: Int` on a connection to `first: PaginationAmount` and `last: PaginationAmount`.
You also have to make sure that you register a new `IntType` on the root schema and all rewrittern
downstream schemas.

Configuration:

```csharp
    services
        .AddGraphQLServer()
        .AddRemoteSchema("SomeSchema")
        .AddType(new IntType())
        .AddType(new IntType("PaginationAmount"))
        .AddMergedDocumentRewriter(
            d => (DocumentNode)new PagingAmountRewriter().Rewrite(d, null));

    services
        .AddGraphQL("SomeSchema")
        .AddType(new IntType())
        .AddType(new IntType("PaginationAmount"));
```

PagingAmountRewriter:

```csharp
    internal class PagingAmountRewriter : SchemaSyntaxRewriter<object?>
    {
        protected override FieldDefinitionNode RewriteFieldDefinition(
            FieldDefinitionNode node,
            object? context)
        {
            if (node.Type.NamedType().Name.Value.EndsWith("Connection") &&
                node.Arguments.Any(
                    t => t.Name.Value.EqualsOrdinal("first") &&
                        t.Type.NamedType().Name.Value.EqualsOrdinal("Int")))
            {
                var arguments = node.Arguments.ToList();

                InputValueDefinitionNode first =
                    arguments.First(t => t.Name.Value.EqualsOrdinal("first"));

                InputValueDefinitionNode last =
                    arguments.First(t => t.Name.Value.EqualsOrdinal("last"));

                arguments[arguments.IndexOf(first)] =
                    first.WithType(RewriteType(first.Type, "PaginationAmount"));

                arguments[arguments.IndexOf(last)] =
                    first.WithType(RewriteType(first.Type, "PaginationAmount"));

                node = node.WithArguments(arguments);
            }

            return base.RewriteFieldDefinition(node, context);
        }

        private static ITypeNode RewriteType(ITypeNode type, NameString name)
        {
            if (type is NonNullTypeNode nonNullType)
            {
                return new NonNullTypeNode(
                    (INullableTypeNode)RewriteType(nonNullType.Type, name));
            }

            if (type is ListTypeNode listType)
            {
                return new ListTypeNode(RewriteType(listType.Type, name));
            }

            return new NamedTypeNode(name);
        }
    }

    internal static class StringExtensions
    {
        public static bool EqualsOrdinal(this string value, string other) =>
            string.Equals(value, other, StringComparison.Ordinal);
    }
```

# Testing

## Building a schema

There are new overloads on the `RequestExecutorBuilder`. Instead of creating the schema and
make it executable, it is now possible to directly create a request executor.
Be aware, the new overload is async

Old

```csharp
    var queryExecutor = builder.New()......Create().MakeExecutable();
```

Old

```csharp
    var schema = await builder.New()......BuildSchmeaAsync();
    var queryExecutor = await builder.New()......BuildExecutorAsync();

    // These overlads are also available on IServiceProvider
    var schema = await serviceProvider.GetSchemaAsync();
    var queryExecutor = await serviceProvider.GetRequestExecutorAsync();
```

## Schema Snapshots Break

Due to the new feature, `@defer` snapshots that have been taken directly on a result will fail.
These have to be updated. As you are updating them anyway, you can also add `ToJson()` to the result.
This will be more stable with upcoming releases than just snapshotting the result object
`result.ToJson().MatchSnapshot()`

### **Field ordering**

Hot Chocolate 11 follows the spec and returns the fields in the order they were defined. This feature
makes migrations harder because the schema snapshot looks different.
You can change this behavior with the following setting

```
    builder.ModifyOptions(x => x.SortFieldsByName = true)
```

### **Types are ordered differently**

Especially in stitching, it could be that the types are orderer differently when you export the schema.
When you use schema snapshots to track changes, this makes the comparison much harder.
To work around this issue you can parse the schemas and sort the types alphabetically.
This way schema snapshots are easier to compare.

```csharp
string schemaAsString = /* load your schema from somwhere */
DocumentNode schema = Utf8GraphQLParser.Parse(schemaAsString);
schema = schema.WithDefinitions(
    schema.Definitions.OfType<IHasName>()
      .OrderBy(x => x.Name.Value )
      .Cast<IDefinitionNode>()
      .Concat(schema.Definitions.Where(x => !(x is IHasName)))
      .ToArray());

schemaAsString = schema.Print();
```

## DataLoader Testing

Due to the changed constructor you now need to also create a scheduler for the dataloaders

Old

```csharp
    FooDataLoader dataLoader = new FooDataLoader( fooRepoMock.Object);
```

New

```csharp
    var scheduler = new BatchScheduler();
    FooDataLoader dataLoader = new FooDataLoader(
        scheduler,
        fooRepoMock.Object);
```

A AA
