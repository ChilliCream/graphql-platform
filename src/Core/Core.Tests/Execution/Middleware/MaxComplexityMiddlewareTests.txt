using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Moq;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class MaxComplexityMiddlewareTests
    {
        [InlineData(5, false)]
        [InlineData(4, true)]
        [Theory]
        public async Task ValidateMaxComplexityWithMiddleware(
            int count, bool valid)
        {
            // arrange
            var schema = Schema.Create(
                @"
                type Query {
                    foo(i: Int): String
                        @cost(complexity: 5 multipliers: [""i""])
                }
                ",
                c =>
                {
                    c.BindResolver(() => "Hello")
                        .To("Query", "foo");
                });

            var options = new Mock<IValidateQueryOptionsAccessor>();
            options.SetupGet(t => t.MaxOperationComplexity).Returns(20);
            options.SetupGet(t => t.UseComplexityMultipliers).Returns(true);

            DocumentNode query = Utf8GraphQLParser.Parse(
                "{ foo(i:" + count + ") }");

            OperationDefinitionNode operationNode = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var operation = new Operation
            (
                query,
                operationNode,
                new VariableValueBuilder(
                    schema,
                    operationNode)
                    .CreateValues(new Dictionary<string, object>()),
                schema.QueryType,
                null
            );

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ a }")
                    .Create();

            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(IErrorHandler),
                    ErrorHandler.Default));

            var context = new QueryContext
            (
                schema,
                services.CreateRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            )
            {
                Document = query,
                Operation = operation,
            };

            var middleware = new MaxComplexityMiddleware(
                c => Task.CompletedTask,
                options.Object,
                null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            if (valid)
            {
                Assert.Null(context.Result);
            }
            else
            {
                context.Result.MatchSnapshot(
                    new SnapshotNameExtension("complexity", count));
            }
        }

        [InlineData(5, false)]
        [InlineData(4, true)]
        [Theory]
        public async Task ValidateMaxComplexityWithMiddlewareWithVariables(
            int count, bool valid)
        {
            // arrange
            var schema = Schema.Create(
                @"
                type Query {
                    foo(i: Int): String
                        @cost(complexity: 5 multipliers: [""i""])
                }
                ",
                c =>
                {
                    c.BindResolver(() => "Hello")
                        .To("Query", "foo");
                });

            var options = new Mock<IValidateQueryOptionsAccessor>();
            options.SetupGet(t => t.MaxOperationComplexity).Returns(20);
            options.SetupGet(t => t.UseComplexityMultipliers).Returns(true);

            DocumentNode query = Utf8GraphQLParser.Parse(
                "query f($i: Int) { foo(i: $i) }");

            OperationDefinitionNode operationNode = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var operation = new Operation
            (
                query,
                operationNode,
                new VariableValueBuilder(
                    schema,
                    operationNode)
                    .CreateValues(new Dictionary<string, object>
                    {
                        { "i", count }
                    }),
                schema.QueryType,
                null
            );

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ a }")
                    .Create();

            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(IErrorHandler),
                    ErrorHandler.Default));

            var context = new QueryContext
            (
                schema,
                services.CreateRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            )
            {
                Document = query,
                Operation = operation
            };

            var middleware = new MaxComplexityMiddleware(
                c => Task.CompletedTask,
                options.Object,
                null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            if (valid)
            {
                Assert.Null(context.Result);
            }
            else
            {
                context.Result.MatchSnapshot(
                    new SnapshotNameExtension("complexity", count));
            }
        }

        [InlineData(5, false)]
        [InlineData(4, true)]
        [Theory]
        public async Task ValidateMaxComplexityWithMiddlewareWithObjects(
            int count, bool valid)
        {
            // arrange
            var schema = Schema.Create(
                @"
                type Query {
                    foo(i: FooInput): String
                        @cost(complexity: 5 multipliers: [""i.index""])
                }

                input FooInput {
                    index : Int
                }
                ",
                c =>
                {
                    c.BindResolver(() => "Hello")
                        .To("Query", "foo");
                });

            var options = new Mock<IValidateQueryOptionsAccessor>();
            options.SetupGet(t => t.MaxOperationComplexity).Returns(20);
            options.SetupGet(t => t.UseComplexityMultipliers).Returns(true);

            DocumentNode query = Utf8GraphQLParser.Parse(
                "{ foo(i: { index:" + count + " }) }");

            OperationDefinitionNode operationNode = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var operation = new Operation
            (
                query,
                operationNode,
                new VariableValueBuilder(
                    schema,
                    operationNode)
                    .CreateValues(new Dictionary<string, object>()),
                schema.QueryType,
                null
            );

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ a }")
                    .Create();

            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(IErrorHandler),
                    ErrorHandler.Default));

            var context = new QueryContext
            (
                schema,
                services.CreateRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            )
            {
                Document = query,
                Operation = operation
            };

            var middleware = new MaxComplexityMiddleware(
                c => Task.CompletedTask,
                options.Object,
                null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            if (valid)
            {
                Assert.Null(context.Result);
            }
            else
            {
                context.Result.MatchSnapshot(
                    new SnapshotNameExtension("complexity", count));
            }
        }

        [InlineData(5, false)]
        [InlineData(4, true)]
        [Theory]
        public async Task ValidateMaxComplexityWithMiddlewareWithObjectsAndVar(
            int count, bool valid)
        {
            // arrange
            var schema = Schema.Create(
                @"
                type Query {
                    foo(i: FooInput): String
                        @cost(complexity: 5 multipliers: [""i.index""])
                }

                input FooInput {
                    index : Int
                }
                ",
                c =>
                {
                    c.BindResolver(() => "Hello")
                        .To("Query", "foo");
                });

            var options = new Mock<IValidateQueryOptionsAccessor>();
            options.SetupGet(t => t.MaxOperationComplexity).Returns(20);
            options.SetupGet(t => t.UseComplexityMultipliers).Returns(true);

            DocumentNode query = Utf8GraphQLParser.Parse(
                "query f($i:Int) { foo(i: { index:$i }) }");

            OperationDefinitionNode operationNode = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var operation = new Operation
            (
                query,
                operationNode,
                new VariableValueBuilder(
                    schema,
                    operationNode)
                    .CreateValues(new Dictionary<string, object>
                    {
                        { "i", count }
                    }),
                schema.QueryType,
                null
            );

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ a }")
                    .Create();

            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(IErrorHandler),
                    ErrorHandler.Default));

            var context = new QueryContext
            (
                schema,
                services.CreateRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            )
            {
                Document = query,
                Operation = operation
            };

            var middleware = new MaxComplexityMiddleware(
                c => Task.CompletedTask,
                options.Object,
                null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            if (valid)
            {
                Assert.Null(context.Result);
            }
            else
            {
                context.Result.MatchSnapshot(
                    new SnapshotNameExtension("complexity", count));
            }
        }

        [Fact]
        public async Task Validate_Multiple_Levels_Valid()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                @"
                type Query {
                    foo(i: Int = 2): Foo
                        @cost(complexity: 1 multipliers: [""i""])
                }

                type Foo {
                    bar: Bar
                    qux: String
                }

                type Bar {
                    baz: String
                }
                ")
                .Use(next => context =>
                {
                    context.Result = "baz";
                    return Task.CompletedTask;
                })
                .Create();

            IQueryExecutor executor = schema.MakeExecutable(new QueryExecutionOptions
            {
                UseComplexityMultipliers = true,
                MaxOperationComplexity = 4
            });

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("query { foo { bar { baz } } }")
                .Create();

            IExecutionResult result = await executor.ExecuteAsync(request);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Validate_Multiple_Levels_Invalid()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                @"
                type Query {
                    foo(i: Int = 2): Foo
                        @cost(complexity: 1 multipliers: [""i""])
                }

                type Foo {
                    bar: Bar
                    qux: String
                }

                type Bar {
                    baz: String
                }
                ")
                .Use(next => context =>
                {
                    context.Result = "baz";
                    return Task.CompletedTask;
                })
                .Create();

            IQueryExecutor executor = schema.MakeExecutable(new QueryExecutionOptions
            {
                UseComplexityMultipliers = true,
                MaxOperationComplexity = 4
            });

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("query { foo(i: 2) { bar { baz } qux } }")
                .Create();

            IExecutionResult result = await executor.ExecuteAsync(request);

            result.MatchSnapshot();
        }
    }
}
