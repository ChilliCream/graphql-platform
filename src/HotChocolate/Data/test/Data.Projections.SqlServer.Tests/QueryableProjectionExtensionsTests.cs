using System.Reflection;
using CookieCrumble;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Projections;

public class QueryableProjectionExtensionsTests
{
    private static readonly Foo[] _fooEntities =
    [
        new Foo { Bar = true, Baz = "a", }, new Foo { Bar = false, Baz = "b", },
    ];

    [Fact]
    public async Task Extensions_Should_ProjectQuery()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // act
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
                .SetDocument("{ shouldWork { bar baz }}")
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
            .AddProjections()
            .CreateExceptionExecutor();

        // act
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
                .SetDocument("{ typeMissmatch { bar baz }}")
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
            .AddProjections()
            .CreateExceptionExecutor();

        // act
        var res1 = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
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
        [UseProjection]
        public IEnumerable<Foo> ShouldWork(IResolverContext context)
        {
            return _fooEntities.Project(context);
        }

        [CatchErrorMiddleware]
        [UseProjection]
        [AddTypeMissmatchMiddleware]
        public IEnumerable<Foo> TypeMissmatch(IResolverContext context)
        {
            return _fooEntities.Project(context);
        }

        [CatchErrorMiddleware]
        public IEnumerable<Foo> MissingMiddleware(IResolverContext context)
        {
            return _fooEntities.Project(context);
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
                        QueryableProjectionProvider.ContextApplyProjectionKey,
                        CreateApplicatorAsync<Foo>());

                return next(context);
            });
        }

        private static ApplyProjection CreateApplicatorAsync<TEntityType>() =>
            (context, input) => new object();
    }
}
