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
    private static readonly Foo[] s_fooEntities =
    [
        new() { Bar = true, Baz = "a" },
        new() { Bar = false, Baz = "b" }
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

    [Fact]
    public async Task Extensions_Should_Not_Fail_On_Projected_Query_With_Existing_Order()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                      projectedWithExistingOrder(order: { someProperty: DESC }) {
                        someProperty
                      }
                    }
                    """)
                .Build());

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "projectedWithExistingOrder": [
                  {
                    "someProperty": "b"
                  },
                  {
                    "someProperty": "a"
                  }
                ]
              }
            }
            """);
    }

    public class Query
    {
        private static readonly Source[] s_source =
        [
            new() { SomeProperty = "a", CreateDate = new DateTime(2020, 1, 1) },
            new() { SomeProperty = "b", CreateDate = new DateTime(2021, 1, 1) }
        ];

        [UseSorting]
        public IEnumerable<Foo> ShouldWork(IResolverContext context)
        {
            return s_fooEntities.Sort(context);
        }

        [CatchErrorMiddleware]
        [UseSorting]
        [AddTypeMismatchMiddleware]
        public IEnumerable<Foo> TypeMismatch(IResolverContext context)
        {
            return s_fooEntities.Sort(context);
        }

        [CatchErrorMiddleware]
        public IEnumerable<Foo> MissingMiddleware(IResolverContext context)
        {
            return s_fooEntities.Sort(context);
        }

        [UseSorting]
        public IQueryable<Projection> ProjectedWithExistingOrder()
        {
            return s_source
                .AsQueryable()
                .OrderBy(x => x.CreateDate)
                .Select(x => new Projection { SomeProperty = x.SomeProperty });
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

    public class Source
    {
        public string SomeProperty { get; set; } = default!;
        public DateTime CreateDate { get; set; }
    }

    public class Projection
    {
        public string SomeProperty { get; set; } = default!;
    }

    public class AddTypeMismatchMiddlewareAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
        {
            descriptor.Use(
                static next => ctx =>
                {
                    ctx.LocalContextData =
                        ctx.LocalContextData.SetItem(
                            QueryableSortProvider.ContextApplySortingKey,
                            CreateApplicatorAsync());

                    return next(ctx);
                });
        }

        private static ApplySorting CreateApplicatorAsync() =>
            (_, _) => new object();
    }
}
