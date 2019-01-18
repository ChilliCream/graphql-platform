using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Utilities;
using Moq;
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

            DocumentNode query = Parser.Default.Parse(
                "{ foo(i:" + count + ") }");

            OperationDefinitionNode operationNode = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var operation = new Operation(
                query, operationNode, schema.QueryType,
                null);

            IReadOnlyQueryRequest request = new QueryRequest("{ a }")
                .ToReadOnly();

            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(IErrorHandler),
                    ErrorHandler.Default));

            var context = new QueryContext
            (
                schema,
                services.CreateRequestServiceScope(),
                request,
                fs => fs.Field.Middleware
            )
            {
                Document = query,
                Operation = operation,
                Variables = new VariableValueBuilder(
                    schema, operation.Definition)
                    .CreateValues(new Dictionary<string, object>())
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
                context.Result.Snapshot(
                    "ValidateMaxComplexityWithMiddleware" +
                    count);
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

            DocumentNode query = Parser.Default.Parse(
                "query f($i: Int) { foo(i: $i) }");

            OperationDefinitionNode operationNode = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var operation = new Operation(
                query, operationNode, schema.QueryType,
                null);

            IReadOnlyQueryRequest request = new QueryRequest("{ a }")
                .ToReadOnly();

            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(IErrorHandler),
                    ErrorHandler.Default));

            var context = new QueryContext
            (
                schema,
                services.CreateRequestServiceScope(),
                request,
                fs => fs.Field.Middleware
            )
            {
                Document = query,
                Operation = operation,
                Variables = new VariableValueBuilder(
                    schema, operation.Definition)
                    .CreateValues(new Dictionary<string, object>
                    {
                        { "i", count }
                    })
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
                context.Result.Snapshot(
                    "ValidateMaxComplexityWithMiddlewareWithVariables" +
                    count);
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

            DocumentNode query = Parser.Default.Parse(
                "{ foo(i: { index:" + count + " }) }");

            OperationDefinitionNode operationNode = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var operation = new Operation(
                query, operationNode, schema.QueryType,
                null);

            IReadOnlyQueryRequest request = new QueryRequest("{ a }")
                .ToReadOnly();

            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(IErrorHandler),
                    ErrorHandler.Default));

            var context = new QueryContext
            (
                schema,
                services.CreateRequestServiceScope(),
                request,
                fs => fs.Field.Middleware
            )
            {
                Document = query,
                Operation = operation,
                Variables = new VariableValueBuilder(
                    schema, operation.Definition)
                    .CreateValues(new Dictionary<string, object>())
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
                context.Result.Snapshot(
                    "ValidateMaxComplexityWithMiddlewareWithObjects" +
                    count);
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

            DocumentNode query = Parser.Default.Parse(
                "query f($i:Int) { foo(i: { index:$i }) }");

            OperationDefinitionNode operationNode = query.Definitions
                .OfType<OperationDefinitionNode>()
                .FirstOrDefault();

            var operation = new Operation(
                query, operationNode, schema.QueryType,
                null);

            IReadOnlyQueryRequest request = new QueryRequest("{ a }")
                .ToReadOnly();

            var services = new DictionaryServiceProvider(
                new KeyValuePair<Type, object>(
                    typeof(IErrorHandler),
                    ErrorHandler.Default));

            var context = new QueryContext
            (
                schema,
                services.CreateRequestServiceScope(),
                request,
                fs => fs.Field.Middleware
            )
            {
                Document = query,
                Operation = operation,
                Variables = new VariableValueBuilder(
                   schema, operation.Definition)
                   .CreateValues(new Dictionary<string, object>
                   {
                        { "i", count }
                   })
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
                context.Result.Snapshot(
                    "ValidateMaxComplexityWithMiddlewareWithObjectsAndVar" +
                    count);
            }
        }
    }
}
