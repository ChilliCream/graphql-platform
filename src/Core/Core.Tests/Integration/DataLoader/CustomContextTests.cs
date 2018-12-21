using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Runtime;
using Moq;
using Xunit;

namespace HotChocolate.Integration.DataLoader
{
    public class CustomContextTests
    {
        [Fact]
        public async Task RequestCustomContext()
        {
            // arrange
            var options = new Mock<IQueryExecutionOptionsAccessor>();

            options
                .SetupGet(o => o.ExecutionTimeout)
                .Returns(TimeSpan.FromSeconds(30));

            ISchema schema = CreateSchema(ExecutionScope.Request);
            IQueryExecuter executer = QueryExecutionBuilder
                .BuildDefault(schema, options.Object);

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executer.ExecuteAsync(
                new QueryRequest("{ a: a b: a }")));
            results.Add(await executer.ExecuteAsync(
                new QueryRequest("{ a: a b: a }")));
            results.Add(await executer.ExecuteAsync(
                new QueryRequest("{ a: a b: a }")));
            results.Add(await executer.ExecuteAsync(
                new QueryRequest("{ a: a b: a }")));

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
            results.Snapshot();
        }

        [Fact]
        public async Task GlobalCustomContext()
        {
            // arrange
            ISchema schema = CreateSchema(ExecutionScope.Global);
            IQueryExecuter executer =
                QueryExecutionBuilder.BuildDefault(schema);

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executer.ExecuteAsync(new QueryRequest("{ a }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest("{ a }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest("{ a }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest("{ a }")));

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
            results.Snapshot();
        }

        private static ISchema CreateSchema(ExecutionScope scope)
        {
            return Schema.Create("type Query { a: String }", c =>
            {
                c.RegisterCustomContext<MyCustomContext>(scope);
                c.BindResolver(ctx =>
                {
                    MyCustomContext cctx = ctx.CustomContext<MyCustomContext>();
                    cctx.Count = cctx.Count + 1;
                    return cctx.Count.ToString();
                }).To("Query", "a");
            });
        }
    }
}
