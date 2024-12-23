using System.Reflection;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class QueryableSortingExtensionsTests
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, Baz = "a", },
        new() { Bar = false, Baz = "b", },
    ];

    [Fact]
    public async Task Extensions_Should_SortQuery()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ shouldWork(order: {bar: DESC}) { bar baz }}")
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
            .AddSorting()
            .CreateExceptionExecutor();

        // act
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ typeMismatch(order: {bar: DESC}) { bar baz }}")
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
            .AddSorting()
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

    public class Query
    {
        [UseSorting]
        public IEnumerable<Foo> ShouldWork(IResolverContext context)
        {
            return _fooEntities.Sort(context);
        }

        [CatchErrorMiddleware]
        [UseSorting]
        [AddTypeMismatchMiddleware]
        public IEnumerable<Foo> TypeMismatch(IResolverContext context)
        {
            return _fooEntities.Sort(context);
        }

        [CatchErrorMiddleware]
        public IEnumerable<Foo> MissingMiddleware(IResolverContext context)
        {
            return _fooEntities.Sort(context);
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

    public class AddTypeMismatchMiddlewareAttribute : ObjectFieldDescriptorAttribute
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
                        QueryableSortProvider.ContextApplySortingKey,
                        CreateApplicatorAsync<Foo>());

                return next(context);
            });
        }

        private static ApplySorting CreateApplicatorAsync<TEntityType>() =>
            (context, input) => new object();
    }
}
