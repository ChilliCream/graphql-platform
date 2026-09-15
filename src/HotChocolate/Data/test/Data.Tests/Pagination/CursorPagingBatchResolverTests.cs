using System.Reflection;
using CookieCrumble;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Pagination;

public class CursorPagingBatchResolverTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Paging_Should_ComposeDataAndAuthorization_When_BatchReturnsQuery(bool offset, bool executable)
    {
        // arrange
        var batches = new List<int[]>();
        var handler = new PagingAuthorizationHandler();
        var observer = new ProductObserver();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddAuthorizationHandler(_ => handler)
            .AddFiltering().AddSorting().AddProjections()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("parents").Resolve(new[] { new PagingHandlerTestLog.PagingParent(1), new PagingHandlerTestLog.PagingParent(2) });
            })
            .AddType(new ObjectType<PagingHandlerTestLog.PagingParent>(d =>
            {
                var field = d.Field("products").Authorize();
                field.Extend().Configuration.ResultType = typeof(PagedProduct[]);
                field.UseBatch(next => async contexts =>
                {
                    foreach (var context in contexts)
                    {
                        context.RegisterPageObserver(observer);
                    }
                    await next(contexts);
                });
                if (offset)
                {
                    field.UseOffsetPaging<ObjectType<PagedProduct>>(options: new PagingOptions { IncludeTotalCount = true });
                }
                else
                {
                    field.UsePaging<ObjectType<PagedProduct>>(options: new PagingOptions { IncludeTotalCount = true });
                }
                field.UseProjection<PagedProduct>().UseFiltering<PagedProduct>().UseSorting<PagedProduct>()
                    .ResolveBatch(contexts =>
                    {
                        batches.Add(contexts.Select(c => c.Parent<PagingHandlerTestLog.PagingParent>().Id).ToArray());
                        var query = new[]
                        {
                            new PagedProduct { Rank = 1, Name = "P1", Unselected = "secret" },
                            new PagedProduct { Rank = 2, Name = "P2", Unselected = "secret" },
                            new PagedProduct { Rank = 3, Name = "P3", Unselected = "secret" }
                        }.AsQueryable();
                        return new ValueTask<IReadOnlyList<ResolverResult>>(contexts.Select(_ =>
                            ResolverResult.Ok(executable ? (object)Executable.From(query) : query)).ToArray());
                    });
            }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(offset
                ? "{parents{products(take:1,where:{rank:{gt:1}},order:{rank:DESC}){items{name rank} totalCount}}}"
                : "{parents{products(first:1,where:{rank:{gt:1}},order:{rank:DESC}){nodes{name rank} totalCount}}}",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new[] { 2 }, batches.Single());
        Assert.Equal(new[] { 1, 2 }, handler.Parents);
        new Snapshot(postFix: $"{offset}_{executable}")
            .Add(result, "Result")
            .Add(observer.Products, "Projected page items")
            .Add(batches, "Authorized resolver batches")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Paging_Should_PreserveProviderKey_When_HandlerOverridesDefaulting(bool offset)
    {
        // arrange
        var log = new PagingHandlerTestLog(offset, "customDefault");
        var executor = await log.CreateExecutor();
        IReadOnlyDictionary<string, object?>[] sets =
        [
            new Dictionary<string, object?> { ["id"] = 1 },
            new Dictionary<string, object?> { ["id"] = 2, ["size"] = 7 },
            new Dictionary<string, object?> { ["id"] = 3, ["size"] = 10 }
        ];

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument(offset
                ? "query($id:Int!,$size:Int){parents(id:$id){products(take:$size){items}}}"
                : "query($id:Int!,$size:Int){parents(id:$id){products(first:$size){nodes}}}")
            .SetVariableValues(sets).Build(), TestContext.Current.CancellationToken);

        // assert
        var batches = log.Batches.Select(b => b.Order().ToArray()).OrderBy(b => b[0]).ToArray();
        Assert.Equal(new[] { 2, 1 }, batches.Select(b => b.Length));
        Assert.Equal(new int?[] { 7, 7, 10 }, log.Sizes.Order());
        var batch = Assert.IsType<OperationResultBatch>(result);
        new Snapshot(postFix: offset.ToString())
            .Add(batch.Results[0], "Omitted custom default")
            .Add(batch.Results[1], "Explicit custom default")
            .Add(batch.Results[2], "Different size")
            .Add(batches, "Resolver batches")
            .Add(log.Sizes.Order().ToArray(), "Handler page sizes")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false, "validate")]
    [InlineData(true, "validate")]
    [InlineData(false, "publish")]
    [InlineData(true, "publish")]
    [InlineData(false, "slice")]
    [InlineData(true, "slice")]
    [InlineData(false, "null")]
    [InlineData(true, "null")]
    [InlineData(false, "error")]
    [InlineData(true, "error")]
    [InlineData(false, "errors")]
    [InlineData(true, "errors")]
    [InlineData(false, "fieldError")]
    [InlineData(true, "fieldError")]
    [InlineData(false, "cachedPage")]
    [InlineData(true, "cachedPage")]
    [InlineData(false, "middlewareValue")]
    [InlineData(true, "middlewareValue")]
    [InlineData(false, "fieldSuccess")]
    [InlineData(true, "fieldSuccess")]
    [InlineData(false, "reported")]
    [InlineData(true, "reported")]
    [InlineData(false, "reportedCached")]
    [InlineData(true, "reportedCached")]
    public async Task Paging_Should_IsolateEntriesAndNotifyObservers_When_CustomHandlerRuns(
        bool offset, string scenario)
    {
        // arrange
        var log = new PagingHandlerTestLog(offset, scenario);
        var executor = await log.CreateExecutor();

        // act
        var result = await executor.ExecuteAsync(
            offset ? "{ parents { products(take:1){items} } }" : "{ parents { products(first:1){nodes} } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(scenario is "fieldSuccess" or "reportedCached" ? new[] { 1, 2 } : [2], log.Sliced);
        new Snapshot(postFix: $"{offset}_{scenario}")
            .Add(result, "Result")
            .Add(log.Batches, "Resolver batches")
            .Add(log.Widths, "Outer middleware widths")
            .Add(log.Sliced, "Sliced parents")
            .Add(log.Observed, "Observed page items")
            .Add(log.PreservedValue, "Middleware value preserved")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("default", 10, 50, false, true, 2)]
    [InlineData("clamped", 100, 20, false, true, 2)]
    [InlineData("required", 10, 50, true, true, 1)]
    [InlineData("negative", 10, 50, false, true, 1)]
    [InlineData("emptyCursor", 10, 50, false, true, 1)]
    [InlineData("backward", 10, 50, false, true, 1)]
    [InlineData("forwardOnly", 10, 50, false, false, 2)]
    [InlineData("explicitNull", 10, 50, false, true, 2)]
    public async Task UsePaging_Should_NormalizeOnlyOmittedSizes_When_SelectionSpansVariableSets(
        string scenario, int defaultSize, int maxSize, bool required, bool backward, int firstBatchSize)
    {
        // arrange
        var log = new PagingBatchTestLog();
        var executor = await PagingBatchTestLog.CreateExecutor(false, new PagingOptions
        {
            DefaultPageSize = defaultSize,
            MaxPageSize = maxSize,
            RequirePagingBoundaries = required,
            AllowBackwardPagination = backward
        }, log);
        var effective = Math.Min(defaultSize, maxSize);
        var sets = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["id"] = 1 },
            new Dictionary<string, object?> { ["id"] = 2, ["first"] = effective },
            new Dictionary<string, object?> { ["id"] = 3, ["first"] = maxSize + 1 }
        };
        if (scenario is "negative" or "emptyCursor" or "backward")
        {
            sets[0] = new Dictionary<string, object?> { ["id"] = 1, ["first"] = 2 };
            sets[1] = scenario switch
            {
                "negative" => new Dictionary<string, object?> { ["id"] = 2, ["first"] = -1 },
                "emptyCursor" => new Dictionary<string, object?> { ["id"] = 2, ["first"] = 2, ["after"] = "" },
                _ => new Dictionary<string, object?> { ["id"] = 2, ["last"] = 2 }
            };
        }
        var document = backward
            ? "query($id:Int!,$first:Int,$last:Int,$after:String){ products(id:$id,first:$first,last:$last,after:$after){ nodes pageInfo { hasNextPage hasPreviousPage } } }"
            : "query($id:Int!,$first:Int){ products(id:$id,first:$first){ nodes pageInfo { hasNextPage hasPreviousPage } } }";
        if (scenario == "explicitNull")
        {
            sets[0] = new Dictionary<string, object?> { ["id"] = 1, ["first"] = null };
        }

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument(document).SetVariableValues(sets).Build(), TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Equal(firstBatchSize, log.Batches[0].Length);
        Assert.Equal(firstBatchSize == 2 ? new[] { 2, 1 } : [1, 1, 1], log.Partitions.Select(p => p.Length));
        Assert.Empty(batch.Results[required ? 1 : 0].ExpectOperationResult().Errors);
        new Snapshot(postFix: scenario)
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(batch.Results[2], "Invalid set")
            .Add(log.Batches, "Resolver batches")
            .Add(log.Arguments, "Per-entry published and raw arguments")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task UsePaging_Should_SeparateAliases_When_PlainListBatchResolverHasIdenticalArgs()
    {
        // arrange
        BrandExtensions.BatchCallCount = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    name
                    small: products(first: 1) {
                        nodes {
                            name
                        }
                    }
                    alsoSmall: products(first: 1) {
                        nodes {
                            name
                        }
                    }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, BrandExtensions.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 1 P1"
                        }
                      ]
                    },
                    "alsoSmall": {
                      "nodes": [
                        {
                          "name": "Brand 1 P1"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 P1"
                        }
                      ]
                    },
                    "alsoSmall": {
                      "nodes": [
                        {
                          "name": "Brand 2 P1"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }

    public class Query
    {
        public List<Brand> GetBrands()
            =>
            [
                new(1, "Brand 1"),
                new(2, "Brand 2")
            ];
    }

    [ExtendObjectType<Brand>]
    public class BrandExtensions
    {
        public static int BatchCallCount { get; set; }

        [UsePaging]
        [BatchResolver]
        public List<List<Product>> GetProducts([Parent] List<Brand> brands)
        {
            BatchCallCount++;
            var result = new List<List<Product>>(brands.Count);

            foreach (var brand in brands)
            {
                result.Add(
                [
                    new Product($"Brand {brand.Id} P1"),
                    new Product($"Brand {brand.Id} P2"),
                    new Product($"Brand {brand.Id} P3")
                ]);
            }

            return result;
        }
    }

    public record Brand(int Id, string Name);

    public record Product(string Name);

    public sealed class PagedProduct
    {
        public int Rank { get; set; }
        public string Name { get; set; } = "";
        public string? Unselected { get; set; }
    }

    private sealed class ProductObserver : IPageObserver
    {
        public List<PagedProduct> Products { get; } = [];
        public void OnAfterSliced<T>(ReadOnlySpan<T> items, IPageInfo pageInfo)
            => Products.AddRange(items.ToArray().Cast<PagedProduct>());
    }

    private sealed class PagingAuthorizationHandler : IAuthorizationHandler
    {
        public List<int> Parents { get; } = [];
        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context, AuthorizeDirective directive, CancellationToken cancellationToken)
        {
            var id = context.Parent<PagingHandlerTestLog.PagingParent>().Id;
            Parents.Add(id);
            return new(id == 1 ? AuthorizeResult.NotAllowed : AuthorizeResult.Allowed);
        }
        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context, IReadOnlyList<AuthorizeDirective> directives, CancellationToken cancellationToken)
            => new(AuthorizeResult.Allowed);
    }
}

internal sealed class PagingHandlerTestLog(bool offset, string scenario)
{
    public List<int[]> Batches { get; } = [];
    public List<int> Widths { get; } = [];
    public List<int> Sliced { get; } = [];
    public List<int?> Sizes { get; } = [];
    public List<int[]> Observed { get; } = [];
    public bool PreservedValue { get; private set; }

    public async Task<IRequestExecutor> CreateExecutor()
    {
        var builder = new ServiceCollection().AddGraphQL().AddQueryType(d =>
        {
            d.Name("Query");
            d.Field("parents").Argument("id", a => a.Type<IntType>()).Resolve(context =>
                context.ArgumentValue<int?>("id") is { } id
                    ? new[] { new PagingParent(id) }
                    : [new PagingParent(1), new PagingParent(2)]);
        }).AddType(new ObjectType<PagingParent>(d =>
        {
            var field = d.Field("products");
            field.Extend().Configuration.ResultType = typeof(PagingSource);
            if (scenario == "customDefault")
            {
                SetPartitionKey(field, context => (ulong)(context.ArgumentValue<int?>(offset ? "take" : "first") ?? 7));
            }
            field.UseBatch(next => async contexts =>
            {
                Widths.Add(contexts.Length);
                foreach (var context in contexts)
                {
                    context.RegisterPageObserver(new Observer(this));
                }
                await next(contexts);
                foreach (var context in contexts)
                {
                    if (context.Result is MiddlewareValue)
                    {
                        PreservedValue = true;
                        context.Result = null;
                    }
                    else if (scenario == "fieldError" && context.Result is IFieldResult { IsError: true })
                    {
                        PreservedValue = true;
                        context.ReportError(TestErrorHelper.Create("denied", context.Path));
                        context.Result = null;
                    }
                    else if (context.Result is IEnumerable<IError> errors)
                    {
                        PreservedValue = true;
                        foreach (var error in errors)
                        {
                            context.ReportError(error);
                        }
                        context.Result = null;
                    }
                }
            });
            if (offset)
            {
                field.UseOffsetPaging<IntType>(options: new PagingOptions { ProviderName = "custom" });
            }
            else
            {
                field.UsePaging<IntType>(options: new PagingOptions { ProviderName = "custom" });
            }
            field.UseBatch(next => async contexts =>
            {
                foreach (var context in contexts)
                {
                    if (context.Parent<PagingParent>().Id != 1)
                    {
                        continue;
                    }
                    switch (scenario)
                    {
                        case "null":
                            context.Result = null;
                            break;
                        case "error":
                            context.Result = TestErrorHelper.Create("denied", context.Path);
                            break;
                        case "errors":
                            context.Result = new[] { TestErrorHelper.Create("denied", context.Path) };
                            break;
                        case "fieldError":
                            context.Result = new FieldResult<PagingSource>(TestErrorHelper.Create("denied", context.Path));
                            break;
                        case "reported":
                            context.ReportError(TestErrorHelper.Create("reported", context.Path));
                            break;
                        case "reportedCached":
                            context.ReportError(TestErrorHelper.Create("reported", context.Path));
                            context.Result = new PagingSource(1);
                            break;
                        case "cachedPage":
                            context.Result = Page(99);
                            break;
                        case "middlewareValue":
                            context.Result = new MiddlewareValue();
                            break;
                    }
                }
                await next(contexts);
            });
            field.ResolveBatch(contexts =>
            {
                Batches.Add(contexts.Select(c => c.Parent<PagingParent>().Id).ToArray());
                return new ValueTask<IReadOnlyList<ResolverResult>>(contexts.Select(c =>
                {
                    var source = new PagingSource(c.Parent<PagingParent>().Id);
                    return ResolverResult.Ok(scenario == "fieldSuccess"
                        ? new FieldResult<PagingSource>(source)
                        : (object)source);
                }).ToArray());
            });
        }));
        if (offset)
        {
            builder.AddOffsetPagingProvider(_ => new OffsetProvider(this), "custom", true);
        }
        else
        {
            builder.AddCursorPagingProvider(_ => new CursorProvider(this), "custom", true);
        }
        return await builder.BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    private void Fail(IResolverContext context, string phase)
    {
        if (scenario == phase && context.Parent<PagingParent>().Id == 1)
        {
            throw TestThrowHelper.PagingFailure(phase);
        }
    }

    private static void SetPartitionKey(IObjectFieldDescriptor descriptor, Func<IMiddlewareContext, ulong> key)
    {
        // The provider key is internal to Types, while these runtime tests live in Data.Tests.
        var configuration = descriptor.Extend().Configuration;
        var property = configuration.GetType().GetProperty(
            "BatchPartitionKeyResolver", BindingFlags.Instance | BindingFlags.NonPublic)!;
        property.SetValue(configuration, key.Method.CreateDelegate(property.PropertyType, key.Target));
    }

    private IPage Slice(IResolverContext context, object source)
    {
        Fail(context, "slice");
        var value = ((PagingSource)source).Id;
        Sliced.Add(value);
        Sizes.Add(offset
            ? context.GetLocalState<OffsetPagingArguments>(WellKnownContextData.PagingArguments).Take
            : context.GetLocalState<CursorPagingArguments>(WellKnownContextData.PagingArguments).First);
        return Page(value);
    }

    private void Publish(IResolverContext context)
    {
        if (scenario == "customDefault")
        {
            if (offset)
            {
                context.SetLocalState(WellKnownContextData.PagingArguments,
                    new OffsetPagingArguments(context.ArgumentValue<int?>("skip"), context.ArgumentValue<int?>("take") ?? 7));
            }
            else
            {
                context.SetLocalState(WellKnownContextData.PagingArguments,
                    new CursorPagingArguments(context.ArgumentValue<int?>("first") ?? 7, null, null, null));
            }
        }
    }

    private IPage Page(int value)
        => offset
            ? new CollectionSegment<int>([value], new CollectionSegmentInfo(false, false), 1)
            : new Connection<int>([new Edge<int>(value, value.ToString())], new ConnectionPageInfo(false, false, null, null), 1);

    private sealed class Observer(PagingHandlerTestLog log) : IPageObserver
    {
        public void OnAfterSliced<T>(ReadOnlySpan<T> items, IPageInfo pageInfo)
            => log.Observed.Add(items.ToArray().Cast<int>().ToArray());
    }

    private sealed class CursorProvider(PagingHandlerTestLog log) : CursorPagingProvider
    {
        public override bool CanHandle(IExtendedType source) => source.Type == typeof(PagingSource);
        protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
            => new CursorHandler(log, options);
    }

    private sealed class CursorHandler(PagingHandlerTestLog log, PagingOptions options)
        : CursorPagingHandler(options), IPagingHandler
    {
        void IPagingHandler.ValidateContext(IResolverContext context)
        {
            log.Fail(context, "validate");
            ValidateContext(context);
        }
        void IPagingHandler.PublishPagingArguments(IResolverContext context)
        {
            log.Fail(context, "publish");
            PublishPagingArguments(context);
            log.Publish(context);
        }
        protected override ValueTask<Connection> SliceAsync(IResolverContext context, object source, CursorPagingArguments arguments)
            => new((Connection)log.Slice(context, source));
    }

    private sealed class OffsetProvider(PagingHandlerTestLog log) : OffsetPagingProvider
    {
        public override bool CanHandle(IExtendedType source) => source.Type == typeof(PagingSource);
        protected override OffsetPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
            => new OffsetHandler(log, options);
    }

    private sealed class OffsetHandler(PagingHandlerTestLog log, PagingOptions options)
        : OffsetPagingHandler(options), IPagingHandler
    {
        void IPagingHandler.ValidateContext(IResolverContext context)
        {
            log.Fail(context, "validate");
            ValidateContext(context);
        }
        void IPagingHandler.PublishPagingArguments(IResolverContext context)
        {
            log.Fail(context, "publish");
            PublishPagingArguments(context);
            log.Publish(context);
        }
        protected override ValueTask<CollectionSegment> SliceAsync(IResolverContext context, object source, OffsetPagingArguments arguments)
            => new((CollectionSegment)log.Slice(context, source));
    }

    public sealed record PagingParent(int Id);
    public sealed record PagingSource(int Id);
    private sealed class MiddlewareValue;
}

internal sealed class PagingBatchTestLog
{
    public List<int[]> Batches { get; } = [];

    public List<int[]> Partitions { get; } = [];

    public List<object> Arguments { get; } = [];

    public static async Task<IRequestExecutor> CreateExecutor(
        bool offset, PagingOptions options, PagingBatchTestLog log)
        => await new ServiceCollection().AddGraphQL().AddQueryType(d =>
        {
            d.Name("Query");
            d.Field("noop").Resolve(0);
            var field = d.Field("products").Argument("id", a => a.Type<NonNullType<IntType>>());
            field.Extend().Configuration.ResultType = typeof(int[]);
            field.UseBatch(next => contexts =>
            {
                log.Partitions.Add(contexts.Select(c => c.ArgumentValue<int>("id")).ToArray());
                return next(contexts);
            });
            if (offset)
            {
                field.UseOffsetPaging<IntType>(options: options);
            }
            else
            {
                field.UsePaging<IntType>(options: options);
            }
            field.ResolveBatch(contexts =>
            {
                log.Batches.Add(contexts.Select(c => c.ArgumentValue<int>("id")).ToArray());
                var results = new ResolverResult[contexts.Count];
                for (var i = 0; i < contexts.Count; i++)
                {
                    var context = contexts[i];
                    var id = context.ArgumentValue<int>("id");
                    log.Arguments.Add(offset
                        ? new
                        {
                            Id = id,
                            Raw = context.ArgumentValue<int?>("take"),
                            Published = context.GetLocalState<OffsetPagingArguments>(WellKnownContextData.PagingArguments)
                        }
                        : (object)new
                        {
                            Id = id,
                            Raw = context.ArgumentValue<int?>("first"),
                            Published = context.GetLocalState<CursorPagingArguments>(WellKnownContextData.PagingArguments)
                        });
                    results[i] = ResolverResult.Ok(new[] { id * 10 + 1, id * 10 + 2, id * 10 + 3 });
                }
                return new ValueTask<IReadOnlyList<ResolverResult>>(results);
            });
        }).BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    public static (string Document, List<IReadOnlyDictionary<string, object?>> Sets) CountRequest(
        bool offset, int padding, bool includeTotalCount)
    {
        var definitions = string.Join(",", Enumerable.Range(0, padding).Select(i => $"$p{i}:Boolean!"));
        var selections = string.Join(" ", Enumerable.Range(0, padding).Select(i => $"p{i}:noop @include(if:$p{i})"));
        var count = includeTotalCount ? "totalCount @include(if:$count)" : "__typename @include(if:$count)";
        var document = $"query($id:Int!,$count:Boolean!{(padding > 0 ? "," + definitions : "")}) {{ {selections} products(id:$id) {{ {(offset ? "items" : "nodes")} {count} }} }}";
        var sets = new List<IReadOnlyDictionary<string, object?>>();
        for (var id = 1; id <= 2; id++)
        {
            var variables = new Dictionary<string, object?> { ["id"] = id, ["count"] = id == 2 };
            for (var i = 0; i < padding; i++)
            {
                variables[$"p{i}"] = false;
            }
            sets.Add(variables);
        }
        return (document, sets);
    }
}

file static class TestErrorHelper
{
    public static IError Create(string message, Path path)
        => ErrorBuilder.New().SetMessage(message).SetPath(path).Build();
}

file static class TestThrowHelper
{
    public static GraphQLException PagingFailure(string phase)
        => new(TestErrorHelper.Create(phase, Path.Root.Append("foreign")));
}
