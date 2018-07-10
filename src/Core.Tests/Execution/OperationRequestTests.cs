using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Execution
{
    public class OperationRequestTests
    {
        [Fact]
        public async Task ResolveSimpleOneLevelQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                }");
            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().FirstOrDefault();

            // act
            OperationExecuter operationRequest =
                new OperationExecuter(schema, query, operation);
            IExecutionResult result = await operationRequest.ExecuteAsync(
                new Dictionary<string, IValueNode>(),
                null, CancellationToken.None);

            // assert
            Assert.NotNull(result);
            Assert.Null(result.Errors);
            Assert.Collection(((QueryResult)result).Data,
                item =>
                {
                    Assert.Equal("a", item.Key);
                    Assert.Equal("hello world", item.Value);
                });
        }

        [Fact]
        public async Task ExecuteMutationSerially()
        {
            // arrange
            int state = 0;

            Schema schema = Schema.Create(
                FileResource.Open("MutationExecutionSchema.graphql"),
                cnf =>
                {
                    cnf.BindResolver(() => state).To("Query", "state");
                    cnf.BindResolver(() => state).To("CurrentState", "theNumber");
                    cnf.BindResolver(ctx => state = ctx.Argument<int>("newNumber"))
                        .To("Mutation", "changeTheNumber");
                });

            DocumentNode query = Parser.Default.Parse(
                FileResource.Open("MutationExecutionQuery.graphql"));
            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().FirstOrDefault();

            // act
            OperationExecuter operationRequest =
                new OperationExecuter(schema, query, operation);
            IExecutionResult result = await operationRequest.ExecuteAsync();

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        private Schema CreateSchema()
        {
            return Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });
        }
    }
}
