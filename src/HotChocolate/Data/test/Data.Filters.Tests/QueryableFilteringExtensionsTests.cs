using System.Reflection;
using CookieCrumble;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class QueryableFilteringExtensionsTests
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, Baz = "a", },
        new() { Bar = false, Baz = "b", },
    ];

    [Fact]
    public async Task Test()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x =>
            {
                x.Name("Query");
                x.Field("foo")
                    .Type(new ObjectType(x => x.Name("A").Field("bar").Resolve("a")))
                    .Resolve(new object());
                x.Field("bar")
                    .Type(new ObjectType(x => x.Name("B").Field("bar").Resolve("a")))
                    .Resolve(new object());
            })
            .BuildRequestExecutorAsync();

        // act
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ shouldWork(where: {bar: {eq: true}}) { bar baz }}")
                .Build());

        // assert
        res1.MatchSnapshot();
    }

    [Fact]
    public async Task Extensions_Should_FilterQuery()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ shouldWork(where: {bar: {eq: true}}) { bar baz }}")
                .Build());

        // assert
        res1.MatchSnapshot();
    }

    [Fact]
    public async Task Extension_Should_BeTypeMismatch()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddFiltering()
            .CreateExceptionExecutor();

        // act
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ typeMismatch(where: {bar: {eq: true}}) { bar baz }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Extension_Should_BeMissingMiddleware()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddFiltering()
            .CreateExceptionExecutor();

        // act
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ missingMiddleware { bar baz }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }


    [Fact]
    public async Task RelayIds_Should_Work()
    {
        // arrange
        var bars = new BarQuery().GetBars.First();
        var id =  new DefaultNodeIdSerializer().Format("Bar", bars.Id);
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<BarQuery>()
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
//         var query = $$"""
//                       { getBars(where: { id: { in: ["{{id}}"] } }) {  id  } }
//                       """;
        var query = $$"""
                      { getBars(where: { id: { in: ["{{id}}"] } }) {  id  } }
                      """;
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(query)
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    public class Query
    {
        [UseFiltering]
        public IEnumerable<Foo> ShouldWork(IResolverContext context)
        {
            return _fooEntities.Filter(context);
        }

        [CatchErrorMiddleware]
        [UseFiltering]
        [AddTypeMismatchMiddleware]
        public IEnumerable<Foo> TypeMismatch(IResolverContext context)
        {
            return _fooEntities.Filter(context);
        }

        [CatchErrorMiddleware]
        public IEnumerable<Foo> MissingMiddleware(IResolverContext context)
        {
            return _fooEntities.Filter(context);
        }
    }

    public class Foo
    {
        public int Id { get; set; }

        public bool Bar { get; set; }

        public string? Baz { get; set; }

        public string Computed() => "Foo";

        public string? NotSettable { get; }
    }

    public class BarQuery
    {
        [UseFiltering]
        public IQueryable<Bar> GetBars => new List<Bar>{
            new() { Id = Guid.Parse("2a874c44944c463a9eaf47191813266d") },
            new() { Id = Guid.Parse("6872d2a0e0954e2db96958ded680b3cc") }
            }.AsQueryable();
    }

    public class Bar
    {
        [ID]
        public Guid Id { get; set; }
    }

    public class AddTypeMismatchMiddlewareAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Use(next => ctx =>
            {
                ctx.LocalContextData =
                    ctx.LocalContextData.SetItem(
                        QueryableFilterProvider.ContextApplyFilteringKey,
                        CreateApplicatorAsync<Foo>());

                return next(ctx);
            });
        }

        private static ApplyFiltering CreateApplicatorAsync<TEntityType>() =>
            (context, input) => new object();
    }
}
