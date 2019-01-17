using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Utilities;
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

            OperationDefinitionNode operationNode = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var operation = new Operation(
                query, operationNode, schema.MutationType,
                null);

            IReadOnlyQueryRequest request = new QueryRequest("{ a }")
                .ToReadOnly();

            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(IErrorHandler),
                    ErrorHandler.Default));

            var context = new QueryContext(
                schema, services.CreateRequestServiceScope(), request)
            {
                Document = query,
                Operation = operation,
                Variables = new VariableValueBuilder(
                    schema, operation.Definition)
                    .CreateValues(new Dictionary<string, object>())
            };

            var options = new QueryExecutionOptions();
            var strategyResolver = new ExecutionStrategyResolver(options);

            var middleware = new ExecuteOperationMiddleware(
                c => Task.CompletedTask,
                strategyResolver,
                new Cache<DirectiveLookup>(10));

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Result);
            context.Result.Snapshot();
        }
    }
}
