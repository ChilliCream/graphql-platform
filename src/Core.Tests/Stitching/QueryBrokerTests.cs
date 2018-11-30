
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Moq;
using Xunit;

namespace HotChocolate.Stitching
{
    public class QueryBrokerTests
    {
        [Fact]
        public async Task ExtractRemoteQueryFromField()
        {
            // arrange
            string schema_a = @"
                type Query { foo: Foo }
                type Foo { name: String }";

            string schema_b = @"
                type Query { bar: Bar }
                type Bar { name: String }";

            string query = @"
                {
                    foo
                        @schema(name: ""a"")
                        @delegate(path: ""foo"" operation: QUERY)
                    {
                        name @schema(name: ""a"")
                        bar
                            @schema(name: ""b"")
                            @delegate(path: ""bar"" operation: QUERY)
                        {
                            name @schema(name: ""b"")
                        }
                    }
                }";

            DocumentNode queryDocument = Parser.Default.Parse(query);
            FieldNode fieldSelection = queryDocument.Definitions
                .OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().Last();

            var schemas = new Dictionary<string, IQueryExecuter>();

            schemas["a"] = new QueryExecuter(
                Schema.Create(schema_a, c => c.Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })));

            schemas["b"] = new QueryExecuter(
                Schema.Create(schema_b, c => c.Use(next => context =>
                {
                    context.Result = "bar";
                    return Task.CompletedTask;
                })));

            var stitchingContext = new StitchingContext(schemas);
            var broker = new QueryBroker(stitchingContext);
            var directiveContext = new Mock<IDirectiveContext>();
            directiveContext.SetupGet(t => t.FieldSelection)
                .Returns(fieldSelection);

            // act
            IExecutionResult response = await broker.RedirectQueryAsync(
                directiveContext.Object);

            // assert
            response.Snapshot();
        }
    }
}
