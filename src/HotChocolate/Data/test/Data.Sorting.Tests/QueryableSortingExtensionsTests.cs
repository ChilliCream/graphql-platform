using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Sorting
{
    public class QueryableSortingExtensionsTests
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true, Baz = "a" }, new Foo { Bar = false, Baz = "b" }
        };

        [Fact]
        public async Task Extensions_Should_SortQuery()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddSorting()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult res1 = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ shouldWork(order: {bar: DESC}) { bar baz }}")
                    .Create());

            // assert
            res1.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Extension_Should_BeTypeMissMatch()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddSorting()
                .CreateExecptionExecutor();

            // act
            IExecutionResult res1 = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ typeMissmatch(order: {bar: DESC}) { bar baz }}")
                    .Create());

            // assert
            res1.MatchException();
        }

        [Fact]
        public async Task Extension_Should_BeMissingMiddleware()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddSorting()
                .CreateExecptionExecutor();

            // act
            IExecutionResult res1 = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ missingMiddleware { bar baz }}")
                    .Create());

            // assert
            res1.MatchException();
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
            [AddTypeMissmatchMiddleware]
            public IEnumerable<Foo> TypeMissmatch(IResolverContext context)
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

            public string Baz { get; set; }

            public string Computed() => "Foo";

            public string? NotSettable { get; }
        }

        public class AddTypeMissmatchMiddlewareAttribute : ObjectFieldDescriptorAttribute
        {
            public override void OnConfigure(
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
}
