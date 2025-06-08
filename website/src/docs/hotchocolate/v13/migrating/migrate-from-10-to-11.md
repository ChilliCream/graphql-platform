---
title: Migrate Hot Chocolate from 10 to 11
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

Hot Chocolate server now embraces the new endpoint routing API from ASP.NET core and with that brings a lot of new features. Head over [here](/docs/hotchocolate/v11/api-reference/aspnetcore) to read more about the ASP.NET Core integration.

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

## Request Interceptor

The query request interceptor was reworked and we renamed it to `IHttpRequestInterceptor`.

```csharp
public interface IHttpRequestInterceptor
{
    ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken);
}
```

**Old:**

```csharp
services.AddQueryRequestInterceptor(
    (context, builder, ct) =>
    {
        // your code
    });
```

**New:**

```csharp
services.AddGraphQLServer()
    ...
    .AddHttpRequestInterceptor(
    (context, executor, builder, ct) =>
    {
        // your code
    });
```

You can also extend `DefaultHttpRequestInterceptor` and inject it like the following.

```csharp
services.AddGraphQLServer()
    ...
    .AddHttpRequestInterceptor<MyCustomExecutor>();
```

> A request interceptor is a service that is used by all hosted schemas.

## Entity Framework Serial Execution

The serial execution for Entity Framework compatibility is gone. If you use Entity Framework Core we recommend using version 5 and the new context factory in combination with context pooling. This allows the execution engine to execute in parallel and still be memory efficient since context objects are pooled.

Another variant here is to use our scoped service feature that scopes services for the resolver pipeline. This is explained in our GraphQL Workshop project.

<https://github.com/ChilliCream/graphql-workshop>

# Schema / Resolvers

## Field ordering

Hot Chocolate 11 follows the spec and returns the fields in the order they were defined. This feature
makes migrations harder because the schema snapshot looks different compared to version 11. You can change this behavior with the following setting.

```csharp
    builder.ModifyOptions(x => x.SortFieldsByName = true)
```

## DataLoaders

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

## Node Resolver

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

## Pagination

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

## Projections

The selection middleware, that was available in `HotChocolate.Types.Selections` was replaced by the projection middleware from `HotChocolate.Data`.

**Old:**

```csharp
descriptor.Field(...).UseSelection()...
```

**New:**

```csharp
descriptor.Field(...).UseProjection()...
```

Similarly, the attribute `[UseSelection]` was replaced by `[UseProjection]`.

To use projections with your GraphQL endpoint you have to register it on the schema:

```csharp
services.AddGraphQLServer()
  // Your schema configuration
  .AddProjections();
```

## Enum Type

Hot Chocolate server 11 now follows the spec recommendation with the new enum name conventions and formats the enum values by default as UPPER_SNAIL_CASE.

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

## IResolverContext.Source

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
                            ctx.ScopedContextData.SetItem(nameof(Foo), ctx.Parent<Foo>());
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
                        if (ctx.ScopedContextData.TryGetValue(nameof(Foo), out object? potentialFoo) &&
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

## Authorization

If you use authorization, you need to add a package reference to `HotChocolate.AspNetCore.Authorization`.

**Old:**

```csharp
    builder.AddAuthorizeDirectiveType()
```

**New:**

```csharp
    builder.AddAuthorization()
```

## TypeBinding

We have renamed the binding method from `BindClrType` to `BindRuntimeType` to make it more clear what it does.

**Old:**

```csharp
    builder.BindClrType<DateTime, DateTimeType>()
```

**New:**

```csharp
    builder.BindRuntimeType<DateTime, DateTimeType>()
```

## FieldMiddleware

Since all configuration APIs were integrated into one, we needed to make it more specific for what a middleware is defined. `UseField` defines a middleware that is applied to the resolver pipeline / field pipeline whereas `UseRequest` defines a middleware that is defined for the request processing.

**Old:**

```csharp
    builder.Use<CustomMiddleware>()
```

**New:**

```csharp
    builder.UseField<CustomMiddleware>()
```

# Stitching

The schema stitching configuration API has been completely integrated into the new configuration API. This means that a Gateway is nothing more than a GraphQL schema, which will make it easier for new users. However, you will need to completely rewire your stitching configuration.

## Configuration

The stitching builder no longer exists in version 11 and you need to use the new configuration API to configure your gateway.

**Old:**

```csharp
    services.AddStitchedSchema(x => ....);
```

**New:**

```csharp
    services.AddGraphQLServer()....
```

### AddSchemaFromHttp

Registering a remote schema has slightly changed in version 11 to make it more clear that we are adding a remote schema into the local gateway schema. Removing, root types and importing a remote schema can be done in one go now.

**Old:**

```csharp
    builder.AddSchemaFromHttp("SomeSchema").IgnoreRootTypes("SomeSchema");
```

**New:**

```csharp
    builder.AddRemoteSchema("SomeSchema", ignoreRootTypes: true);
```

## AddSchemaConfiguration

In version 11 it is now much easier to configure the gateway schema.

**Old:**

```csharp
    services.AddStitchedSchema(x => x.AddSchemaConfiguration(y => y.RegisterType<FooType>()));
```

**New:**

```csharp
    services
        .AddGraphQLServer()
        .AddType<FooType>();
```

## IgnoreField

The order of the parameters in ignore field and ignore type has changed since we moved optional parameters to the end.

**Old:**

```csharp
    services.AddStitchedSchema(x => x.IgnoreField("SchemaName", "TypeName, "FieldName"));
```

**New:**

```csharp
    services
        .AddGraphQLServer()
        .IgnoreField("TypeName, "FieldName", "SchemaName")
```

## SetExecutionOptions

Execution options can now be configured on the root schema directly like for any other schema:

**Old:**

```csharp
    services.AddStitchedSchema(
        x => x.SetExecutionOptions(
            new QueryExecutionOptions
                {
                    TracingPreference = TracingPreference.OnDemand
                }));
```

**New:**

```csharp
    services
        .AddGraphQLServer()
        .ModifyRequestOptions(x => x.TracingPreference = TracingPreference.OnDemand);
```

## Configuring a downstream schema

In case you want to configure a downstream schema, you can now just use the new configuration API since all downstream schemas have an in-memory representation.

```csharp
    services
        .AddGraphQLServer()
        .AddRemoteSchema("SomeSchema");

    services
        .AddGraphQL("SomeSchema")
        .AddType(new IntType("SpecialIntegerType"));
```

## PaginationAmount

The `PaginationAmount` scalar was removed since it caused a lot of issues with clients and only provided limited benefit. The arguments `first` and `last` use now `Int` as a type. To avoid breaking schemas on a stitched schema, you can add a rewriter that rewrites all
`first: Int` and `last: Int` on a connection to `first: PaginationAmount` and `last: PaginationAmount`.
You also have to make sure that you register a new `IntType` on the root schema and rewrite all
downstream schemas.

**Configuration:**

```csharp
    services
        .AddGraphQLServer()
        .AddRemoteSchema("SomeSchema")
        .ConfigureSchema(x =>
             x.AddType(new IntType())
             .AddType(new IntType("PaginationAmount")))
        .AddMergedDocumentRewriter(
            d => (DocumentNode)new PagingAmountRewriter().Rewrite(d, null));

    services
        .AddGraphQL("SomeSchema")
        .ConfigureSchema(x =>
             x.AddType(new IntType())
             .AddType(new IntType("PaginationAmount")));
```

**PagingAmountRewriter:**

```csharp
    internal class PagingAmountRewriter : SchemaSyntaxRewriter<object?>
    {
        protected override FieldDefinitionNode RewriteFieldDefinition(
            FieldDefinitionNode node,
            object? context)
        {
            if (node.Type.NamedType().Name.Value.EndsWith("Connection") &&
                (node.Arguments.Any(
                    t => t.Name.Value.EqualsOrdinal("first") &&
                        t.Type.NamedType().Name.Value.EqualsOrdinal("Int"))
                || node.Arguments.Any(
                    t => t.Name.Value.EqualsOrdinal("last") &&
                        t.Type.NamedType().Name.Value.EqualsOrdinal("Int"))
                ))
            {
                var arguments = node.Arguments.ToList();

                InputValueDefinitionNode first =
                    arguments.FirstOrDefault(t => t.Name.Value.EqualsOrdinal("first"));

                InputValueDefinitionNode last =
                    arguments.FirstOrDefault(t => t.Name.Value.EqualsOrdinal("last"));

                if (first != null) arguments[arguments.IndexOf(first)] = first.WithType(RewriteType(first.Type, "PaginationAmount"));

                if (last != null) arguments[arguments.IndexOf(last)] = last.WithType(RewriteType(last.Type, "PaginationAmount"));

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

## Batch responses

In v10, responses to batched operations were returned as a JsonArray. In v11 the default is to return MultiPartChunked responses. To switch back to JsonArray, configure the HttpResult serializer as follows:

```csharp
services.AddHttpResultSerializer(
    batchSerialization: HttpResultSerialization.JsonArray
);
```

# Testing

We have added a couple of test helpers to make the transition to the new configuration API easier.

## Schema Snapshot Tests

**Old:**

```csharp
    SchemaBuilder.New()
        .AddQueryType<Query>()
        .Create()
        .ToString()
        .MatchSnapshot();
```

**New:**

```csharp
    ISchema schema =
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

    schema.Print().MatchSnapshot();
```

## Request Tests

**Old:**

```csharp
    IQueryExecutor executor =
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .Create()
            .MakeExecutable();
```

**New:**

```csharp
    IRequestExecutor executor =
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

    IExecutionResult result =
        await executor.ExecuteAsync("{ __typename }");

    result.ToJson().MatchSnapshot();
```

Or you can directly build and execute:

```csharp
    IExecutionResult result =
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ExecuteRequestAsync("{ __typename }");

    result.ToJson().MatchSnapshot();
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

// TODO : Type Converter
