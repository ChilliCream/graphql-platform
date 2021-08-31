using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Projections
{
    public class QueryableProjectionExtensionsTests
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true, Baz = "a" }, new Foo { Bar = false, Baz = "b" }
        };

        [Fact]
        public async Task Extensions_Should_ProjectQuery()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddProjections()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult res1 = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ shouldWork { bar baz }}")
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
                .AddProjections()
                .CreateExecptionExecutor();

            // act
            IExecutionResult res1 = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ typeMissmatch { bar baz }}")
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
                .AddProjections()
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
                            QueryableProjectionProvider.ContextApplyProjectionKey,
                            CreateApplicatorAsync<Foo>());

                    return next(context);
                });
            }

            private static ApplyProjection CreateApplicatorAsync<TEntityType>() =>
                (context, input) => new object();
        }
    }
}
