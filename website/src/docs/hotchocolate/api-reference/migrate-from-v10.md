---
title: Migrate from 10 to 11
---

# Configuration

## Startup

### ConfigureServices

The creation of a schema has slightly changed. `AddGraphQL` is now called `AddGraphQLServer`.
All methods that have been on the `SchemaBuilder` are also available on the `RequestExectuorBuilder`
that is returned by `AddGraphQLServer`

Old

```csharp
    services.AddGraphQL(sp  =>
        SchemaBuilder.New()
            .AddServices(sp)
            .AddQueryType<QueryType>()
            .AddMutationType<MutationType>()
            ...
            .Create());
```

New

```csharp
    services
        .AddGraphQLServer()
        .AddQueryType<QueryType>()
        .AddMutationType<MutationType>()
        ...
```

### Configure

HotChocolate uses AspNetCore endpoint routing. The registration of the middleware changed.

Old

```csharp
    app.UseGraphQL();
```

New

```csharp
    app.UseEndpoints(x => x.MapGraphQL());
```

## DataLoaders

Dataloaders require a new constructor and now returns `ValueTask` instead of `Task`.
It is not longer needed to call `AddDataLoaderRegistery()` on the service collection.

Old

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

New

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

## Pagination

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

## IResolverContext.Source

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
