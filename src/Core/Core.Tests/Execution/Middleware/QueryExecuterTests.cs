using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryExecuterTests
    {
        [Fact]
        public void SchemaIsNull_ShouldThrow()
        {
            // act
            Action a = () => QueryExecutionBuilder.New().Build(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public async Task ExecuteShortHandQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest("{ a }");

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteWithMissingVariables_Error()
        {
            // arrange
            var variableValues =
                new Dictionary<string, object>();

            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest(
                "query x($a: String!) { b(a: $a) }")
            {
                VariableValues = variableValues
            };

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteWithNonNullVariableNull_Error()
        {
            // arrange
            var variableValues =
                new Dictionary<string, object>()
                {
                    { "a", NullValueNode.Default }
                };

            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest(
                "query x($a: String!) { b(a: $a) }")
            {
                VariableValues = variableValues
            };

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteWithNonNullVariableInvalidType_Error()
        {
            // arrange
            var variableValues = new Dictionary<string, object>()
            {
                { "a", new IntValueNode(123) }
            };

            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest(
                "query x($a: String!) { b(a: $a) }")
            {
                VariableValues = variableValues
            };

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteWithNonNullVariableValidValue_NoErrors()
        {
            // arrange
            var variableValues =
                new Dictionary<string, object>()
                {
                    { "a", new StringValueNode("123") }
                };

            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest(
                "query x($a: String!) { b(a: $a) }")
            {
                VariableValues = variableValues
            };

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteQueryWith2OperationsAndNoOperationName_Error()
        {
            // arrange
            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest(
                "query a { a } query b { a }");

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteQueryWith2OperationsAndOperationName_NoErrors()
        {
            // arrange
            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest(
                "query a { a } query b { a }", "a");

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteQueryWith2OperationsAndInvalidOpName_Error()
        {
            // arrange
            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest(
                "query a { a } query b { a }", "c");

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteFieldWithResolverResult()
        {
            // arrange
            var variableValues =
                new Dictionary<string, IValueNode>();

            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest("{ x xasync }");

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteFieldWithResolverResultError()
        {
            // arrange
            var variableValues =
                new Dictionary<string, IValueNode>();

            Schema schema = CreateSchema();
            var executer = QueryExecutionBuilder.BuildDefault(schema);
            var request = new QueryRequest("{ y yasync }");

            // act
            IExecutionResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.Snapshot();
        }


        private Schema CreateSchema()
        {
            return Schema.Create(@"
                type Query {
                    a: String
                    b(a: String!): String
                    x: String
                    y: String
                    xasync: String
                    yasync: String
                }
                ", c =>
            {
                c.BindResolver(() => "hello world a")
                    .To("Query", "a");
                c.BindResolver(
                    ctx => "hello world " + ctx.Argument<string>("a"))
                    .To("Query", "b");
                c.BindResolver(
                    () => ResolverResult<string>
                        .CreateValue("hello world x"))
                    .To("Query", "x");
                c.BindResolver(
                    () => ResolverResult<string>
                        .CreateError("hello world y"))
                    .To("Query", "y");
                c.BindResolver(
                    async () => await Task.FromResult(
                        ResolverResult<string>
                            .CreateValue("hello world xasync")))
                    .To("Query", "xasync");
                c.BindResolver(
                    async () => await Task.FromResult(
                        ResolverResult<string>
                            .CreateError("hello world yasync")))
                    .To("Query", "yasync");
            });
        }
    }
}
