using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryExecutorTests
    {
        [Fact]
        public void SchemaIsNull_ShouldThrow()
        {
            // act
            Action a = () => QueryExecutionBuilder.New().Build((ISchema)null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void ServicesIsNull_ShouldThrow()
        {
            // act
            Action a = () => QueryExecutionBuilder.New().Build((ISchema)null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public async Task ExecuteShortHandQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            IQueryExecutor executor = QueryExecutionBuilder
                .BuildDefault(schema);
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("{ a }")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteWithMissingVariables_Error()
        {
            // arrange
            var variableValues =
                new Dictionary<string, object>();
            Schema schema = CreateSchema();
            IQueryExecutor executor = QueryExecutionBuilder
                .BuildDefault(schema);
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("query x($a: String!) { b(a: $a) }")
                    .SetVariableValues(variableValues)
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
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
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("query x($a: String!) { b(a: $a) }")
                    .SetVariableValues(variableValues)
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task InputObjectVariable_NonNullField_Is_Null()
        {
            // arrange
            var variableValues =
                new Dictionary<string, object>()
                {
                    { "foo", new Dictionary<string, object>()
                        {
                            { "bar", new Dictionary<string, object>() }
                        }
                    }
                };

            Schema schema = CreateSchema();
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery(@"
                        query foo($foo: Foo!)
                        {
                            z(foo: $foo)
                        }")
                    .SetVariableValues(variableValues)
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task InputObjectArgument_NonNullField_Is_Null()
        {
            // arrange
            Schema schema = CreateSchema();
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery(@"
                        {
                            z(foo: { bar: { baz: null } })
                        }")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task InputObject_NonNullField_Has_Variable_With_Null()
        {
            // arrange
            var variableValues =
                new Dictionary<string, object>()
                {
                    { "baz", null }
                };


            Schema schema = CreateSchema();
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery(@"
                        query foo($baz: String!)
                        {
                            z(foo: { bar: { baz: $baz } })
                        }")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task InputObject_NonNullField_Has_Nullable_Variable()
        {
            // arrange
            var variableValues =
                new Dictionary<string, object>()
                {
                    { "baz", null }
                };


            Schema schema = CreateSchema();
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery(@"
                        query foo($baz: String)
                        {
                            z(foo: { bar: { baz: $baz } })
                        }")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
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
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("query x($a: String!) { b(a: $a) }")
                    .SetVariableValues(variableValues)
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
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
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("query x($a: String!) { b(a: $a) }")
                    .SetVariableValues(variableValues)
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteQueryWith2OperationsAndNoOperationName_Error()
        {
            // arrange
            Schema schema = CreateSchema();
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("query a { a } query b { a }")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteQueryWith2OperationsAndOperationName_NoErrors()
        {
            // arrange
            Schema schema = CreateSchema();
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("query a { a } query b { a }")
                    .SetOperation("a")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteQueryWith2OperationsAndInvalidOpName_Error()
        {
            // arrange
            Schema schema = CreateSchema();
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("query a { a } query b { a }")
                    .SetOperation("c")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteFieldWithResolverResult()
        {
            // arrange
            var variableValues =
                new Dictionary<string, IValueNode>();

            Schema schema = CreateSchema();
            IQueryExecutor executor =
                QueryExecutionBuilder.BuildDefault(schema);
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("{ x xasync }")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteFieldWithResolverResultError()
        {
            // arrange
            var variableValues =
                new Dictionary<string, IValueNode>();

            Schema schema = CreateSchema();
            IQueryExecutor executor = schema.MakeExecutable();
            var request =
                QueryRequestBuilder.New()
                    .SetQuery("{ y yasync }")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        private Schema CreateSchema()
        {
            return Schema.Create(@"
                type Query {
                    a: String
                    b(a: String!): String
                    x: String
                    y: String
                    z(foo: Foo): String
                    xasync: String
                    yasync: String
                }

                input Foo {
                    bar: Bar!
                }

                input Bar {
                    baz: String!
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
                c.BindResolver(() => "hello world z")
                    .To("Query", "z");
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
