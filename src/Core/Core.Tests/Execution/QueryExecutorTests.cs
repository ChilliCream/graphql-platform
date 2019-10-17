using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class QueryExecutorTests
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

            IQueryExecutor executor = schema.MakeExecutable();

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
            IQueryExecutor executor = schema.MakeExecutable();

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

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ foo }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request, cts.Token);

            // assert
            // match snapshot ... in case of a cancellation the whole result is canceled
            // and we return ther error result without any data.
            result.MatchSnapshot(o => o.IgnoreField("Errors[0].Exception"));

            // the cancellation token was correctly passed to the resolver.
            Assert.True(tokenWasCorrectlyPassedToResolver);
        }
    }
}
