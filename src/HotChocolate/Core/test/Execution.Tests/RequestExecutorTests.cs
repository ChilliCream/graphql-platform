using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class RequestExecutorTests
    {
        [Fact]
        public async Task Request_Is_Null_ArgumentNullException()
        {
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            Func<Task> action = () => executor.ExecuteAsync(null, default);

            // assert
            ArgumentException exception =
                await Assert.ThrowsAsync<ArgumentNullException>(action);
            Assert.Equal("request", exception.ParamName);
        }

        [Fact]
        public void Schema_Property_IsCorrectly_Set()
        {
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // act
            IRequestExecutor executor = schema.MakeExecutable();

            // assert
            Assert.Equal(schema, executor.Schema);
        }

        [Fact]
        public async Task CancellationToken_Is_Passed_Correctly()
        {
            // arrange
            bool tokenWasCorrectlyPassedToResolver = false;

            var cts = new CancellationTokenSource();
            Action cancel = () => cts.Cancel();

            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("foo")
                    .Resolver(ctx =>
                    {
                        // we cancel the cts in the resolver so we are sure
                        // that we reach this point and the passed in ct was correctly
                        // passed.
                        cancel();

                        try
                        {
                            ctx.RequestAborted.ThrowIfCancellationRequested();
                            return "bar";
                        }
                        catch (OperationCanceledException)
                        {
                            tokenWasCorrectlyPassedToResolver = true;
                            throw new QueryException("CancellationRaised");
                        }
                    }))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ foo }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request, cts.Token);

            // assert
            // match snapshot ... in case of a cancellation the whole result is canceled
            // and we return there error result without any data.
            result.MatchSnapshot();

            // the cancellation token was correctly passed to the resolver.
            Assert.True(tokenWasCorrectlyPassedToResolver);
        }

        [Fact]
        public async Task Exec()
        {
            Snapshot.FullName();

            var executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Name("Query").Field("abc").Resolve("abc"))
                .AddMutationType(d => d.Name("Mutation"))
                .AddTypeExtension<TestException>()
                .BuildRequestExecutorAsync();

            var list = new List<Task<IExecutionResult>>();

            for (int i = 0; i < 1000; i++)
            {
                list.Add(executor.ExecuteAsync("mutation { test }"));
            }

            await Task.WhenAll(list);

            foreach (var result in list)
            {
                result.Result.ToJson().MatchSnapshot();
            }
        }

        [ExtendObjectType(Name = "Mutation")]
        public class TestException
        {
            public Task<bool> Test()
            {
                throw new GraphQLException("test");
            }
        }
    }
}
