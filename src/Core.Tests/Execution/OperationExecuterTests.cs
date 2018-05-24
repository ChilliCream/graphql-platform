using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Execution
{
    public class OperationExecuterTests
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

            // act
            OperationExecuter operationExecuter = new OperationExecuter();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, query, null, new Dictionary<string, IValueNode>(),
                null, CancellationToken.None);

            // assert
            Assert.NotNull(result);
            Assert.Null(result.Errors);
            Assert.Collection(result.Data,
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

            // act
            OperationExecuter operationExecuter = new OperationExecuter();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, query, null, null, null, CancellationToken.None);

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
