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
                    .Resolve("bar"))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            Task Action() => executor.ExecuteAsync(null!, default);

            // assert
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentNullException>(Action);
            Assert.Equal("request", exception.ParamName);
        }

        [Fact]
        public void Schema_Property_IsCorrectly_Set()
        {
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("foo")
                    .Resolve("bar"))
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
            var tokenWasCorrectlyPassedToResolver = false;

            var cts = new CancellationTokenSource();
            void Cancel() => cts.Cancel();

            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("foo")
                    .Resolve(ctx =>
                    {
                        // we cancel the cts in the resolver so we are sure
                        // that we reach this point and the passed in ct was correctly
                        // passed.
                        Cancel();

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
        public async Task Ensure_Errors_Do_Not_Result_In_Timeouts()
        {
            using var cts = new CancellationTokenSource(1000);

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Name("abc").Field("a").Resolve("a"))
                .AddMutationType<Mutation>()
                .ExecuteRequestAsync(@"
                    mutation
                    {
                        test
                        {
                            test
                            {
                                bar
                                __typename
                            }
                            __typename
                        }  
                    }",
                    cancellationToken: cts.Token)
                .MatchSnapshotAsync();
        }

        public class Mutation
        {
            public TestMutationPayload Test()
            {
                throw new Exception("Error");
            }
        }

        public class TestMutationPayload
        {
            public Test Test { get; set; }
        }

        public class Test
        {
            public string Foo { get; set; } = "Foo";
            public string Bar { get; set; } = "Bar";
        }
    }
}
