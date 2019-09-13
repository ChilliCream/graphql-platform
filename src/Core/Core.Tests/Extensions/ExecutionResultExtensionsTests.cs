using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class ExecutionResultExtensionsTests
    {
        [Fact]
        public async Task ToJson()
        {
            // arrange
            IQueryExecutor executor = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create()
                .MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ToJson_NoIndentation()
        {
            // arrange
            IQueryExecutor executor = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create()
                .MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ foo }");

            // assert
            result.ToJson(false).MatchSnapshot();
        }

        [Fact]
        public void ToJson_ResponseStream_ShouldFail()
        {
            // arrange
            // act
            Action action = () =>
                Mock.Of<ISubscriptionExecutionResult>().ToJson();

            // assert
            Assert.Throws<NotSupportedException>(action);
        }

        [Fact]
        public void ToJson_ResultIsNull_ShouldFail()
        {
            // arrange
            // act
            Action action = () =>
                default(IReadOnlyQueryResult).ToJson();

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
