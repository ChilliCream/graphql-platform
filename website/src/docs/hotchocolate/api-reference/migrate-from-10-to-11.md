---
title: Migrate from Hot Chocolate GraphQL server 10 to 11
---

This guide will walk you through the manual migration steps to get you Hot Chocolate GraphQL server to version 11.

As a general preparation, we recommend first to remove all package references to your project. Then start by adding the `HotChocolate.AspNetCore` package. The server package now contains most of the needed packages.

When do I need to add other Hot Chocolate packages explicitly?

We have now added the most common packages to the Hot Chocolate core. But there are certain areas where we still need to add some additional packages.

TABLE

# Startup

One of the main focuses of version 11 was to create a new configuration API that brings all our builders together in one unified API. This also means that we had to introduce breaking changes to the way we
configure schemas.

After you have cleaned up your packages, head over to the `Startup.cs` to start with your migration.

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

If you were using the `QueryRequestBuilder` to configure request options or change the request pipeline, the you can add those things now also to the configuration chain.

```csharp
    services
        .AddGraphQLServer()
        .AddQueryType<QueryType>()
        .AddMutationType<MutationType>()
        ...
        .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(180));
```

# Configure

After migrating the schema configuration, the next area that has fundamentally changed is the schema middleware.

Hot Chocolate server now embraces the new endpoint routing API from ASP.NET core and with that brings a lot of new features. Head over [here]() to read more about the ASP.NET Core integration.

**Old:**

```csharp
    app.UseGraphQL();
```

**New:**

```csharp
app.UseRouting();
    app.UseEndpoints(x => x.MapGraphQL());
```

# DataLoaders

With Hot Chocolate server 11, we have embraced the new DataLoader spec version 2. With that, we have decoupled the scheduler from the DataLoader itself, meaning you now have to pass on the `IBatchScheduler` to the base implementation of the DataLoader.
Apart from that DataLoader now uses `ValueTask` instead of `Task` when doing async work.

If you were adding the `DataLoaderRegistry` to the services, you could remove that code since `service.AddDataLoaderRegistry` is no longer needed.

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

There are new APIs to specify Nodes.

Old

```csharp
    descriptor
        .AsNode()
        .IdField(d => d.Id)
        .NodeResolver(async (ctx, id) => await ctx
            .DataLoader<FooDataLoader>()
            .LoadAsync(id, ctx.RequestAborted))
        .Authorize(AuthorizationPolicies.Names.EmployeeAccess, ApplyPolicy.AfterResolver);
```

New

```csharp
    descriptor
        .ImplementsNode()
        .IdField(d => d.Id)
        .ResolveNode(async (ctx, id) => await ctx
            .DataLoader<FooDataLoader>()
            .LoadAsync(id, ctx.RequestAborted))
        .Authorize(AuthorizationPolicies.Names.EmployeeAccess, ApplyPolicy.AfterResolver);
```

# Pagination

You can configure paging options `MaxPageSize`, `DefaultPageSize` and `IncludeTotalCount` on the
builder globally

```csharp
    builder.SetPagingOptions(
        new PagingOptions()
        {
            MaxPageSize = searchOptions.PaginationAmount,
            DefaultPageSize = searchOptions.PaginationAmount,
            IncludeTotalCount = true
        });
```

## ENUM_VALUES

HotChocolate 11 now follows the by the spec recommended way of formatting enums.
To avoid breaking changes you will have to override the naming convetion:

Configuration:

```csharp
    builder
        .AddConvention<INamingConventions>(new CompatibilityNamingConvention())
```

Convention:

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

This API was removed. The reason for this is that it violates the concept of a tree.
A GraphQL Type should never look up in a tree. All the data that is needed has to be pushed down
from the previous type.

Old

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

New

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

If you use authorization you need to add a package reference to `HotChocolate.AspNetCore.Authorization`.

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
