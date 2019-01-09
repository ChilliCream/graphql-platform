using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Execution
{
    public class CoerceVariablesMiddlewareTests
    {
        [Fact]
        public async Task ParseQueryMiddleware_ValidQuery_DocumentIsSet()
        {
            // arrange
            Schema schema = CreateSchema();

            var request = new QueryRequest("query foo($a: String) { a }")
            {
                VariableValues = new Dictionary<string, object>
                {
                    { "a", "abc" }
                }
            }.ToReadOnly();

            var context = new QueryContext(
                schema, MiddlewareTools.CreateEmptyRequestServiceScope(), request);
            context.Document = Parser.Default.Parse(request.Query);
            context.Operation = new Operation(
                context.Document,
                context.Document.Definitions
                    .OfType<OperationDefinitionNode>()
                    .First(),
                schema.QueryType,
                null);

            var middleware = new CoerceVariablesMiddleware(
                c => Task.CompletedTask);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Variables);
            Assert.Equal("abc", context.Variables.GetVariable<string>("a"));
        }

        private Schema CreateSchema()
        {
            return Schema.Create(@"
                type Query { a(b:String): String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });
        }
    }
}
