using System.Reflection;
using CookieCrumble;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
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
                .Create()
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
                .Create()
                .SetDocument("{ shouldWork(where: {bar: {eq: true}}) { bar baz }}")
                .Build());

        // assert
        res1.MatchSnapshot();
    }

    [Fact]
    public async Task Extension_Should_BeTypeMissMatch()
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
                .Create()
                .SetDocument("{ typeMissmatch(where: {bar: {eq: true}}) { bar baz }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
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
                .Create()
                .SetDocument("{ missingMiddleware { bar baz }}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
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
        [AddTypeMissmatchMiddleware]
        public IEnumerable<Foo> TypeMissmatch(IResolverContext context)
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

    public class AddTypeMissmatchMiddlewareAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Use(next => context =>
            {
                context.LocalContextData =
                    context.LocalContextData.SetItem(
                        QueryableFilterProvider.ContextApplyFilteringKey,
                        CreateApplicatorAsync<Foo>());

                return next(context);
            });
        }

        private static ApplyFiltering CreateApplicatorAsync<TEntityType>() =>
            (context, input) => new object();
    }
}
