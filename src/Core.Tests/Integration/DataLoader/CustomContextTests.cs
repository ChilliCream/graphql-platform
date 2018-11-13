using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Runtime;
using Xunit;

namespace HotChocolate.Integration.DataLoader
{
    public class CustomContextTests
    {
        [Fact]
        public async Task RequestCustomContext()
        {
            // arrange
            ISchema schema = CreateSchema(ExecutionScope.Request);
            QueryExecuter executer = new QueryExecuter(schema, 10);

            // act
            List<IExecutionResult> results = new List<IExecutionResult>();
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
            QueryExecuter executer = new QueryExecuter(schema, 10);

            // act
            List<IExecutionResult> results = new List<IExecutionResult>();
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
                c.Options.ExecutionTimeout = TimeSpan.FromSeconds(30);
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
