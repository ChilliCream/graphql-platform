using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Utilities;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
    public class ExecuteOperationMiddlewareTests
    {
        [Fact]
        public async Task ExecuteOperationMiddleware_Mutation_ExecutedSerially()
        {
            // arrange
            var state = 0;

            var schema = Schema.Create(
                FileResource.Open("MutationExecutionSchema.graphql"),
                cnf =>
                {
                    cnf.BindResolver(() => state)
                        .To("Query", "state");
                    cnf.BindResolver(() => state)
                        .To("CurrentState", "theNumber");
                    cnf.BindResolver(
                        ctx => state = ctx.Argument<int>("newNumber"))
                        .To("Mutation", "changeTheNumber");
                });

            DocumentNode query = Parser.Default.Parse(
                FileResource.Open("MutationExecutionQuery.graphql"));

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var request = new QueryRequest("{ a }").ToReadOnly();

            var context = new QueryContext(
                schema, new EmptyServiceProvider(), request)
            {
                Document = query,
                Operation = operation
            };

            var middleware = new ExecuteOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Result);
            context.Result.Snapshot();
        }
    }
}
