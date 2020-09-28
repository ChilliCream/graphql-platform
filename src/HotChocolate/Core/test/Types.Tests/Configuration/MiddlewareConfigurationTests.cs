using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Configuration
{
    public class MiddlewareConfigurationTests
    {
        [Fact]
        public async Task MiddlewareConfig_MapWithDelegate()
        {
            // arrange
            ISchema schema = Schema.Create(
                "type Query { a: String b: String }",
                c => c.Map(
                        new FieldReference("Query", "a"),
                        next => context =>
                        {
                            context.Result = "123";
                            return default(ValueTask);
                        })
                    .Map(
                        new FieldReference("Query", "b"),
                        next => context =>
                        {
                            context.Result = "456";
                            return default(ValueTask);
                        }));

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a b }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task MiddlewareConfig_MapWithClass()
        {
            // arrange
            ISchema schema = Schema.Create(
                "type Query { a: String b: String }",
                c => c.Map<TestFieldMiddleware>(
                        new FieldReference("Query", "a"))
                    .Map(
                        new FieldReference("Query", "b"),
                        next => context =>
                        {
                            context.Result = "456";
                            return default(ValueTask);
                        }));

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a b }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task MiddlewareConfig_MapWithClassFactory()
        {
            // arrange
            ISchema schema = Schema.Create(
                "type Query { a: String b: String }",
                c => c.Map(
                        new FieldReference("Query", "a"),
                        (services, next) => new TestFieldMiddleware(next))
                    .Map(
                        new FieldReference("Query", "b"),
                        next => context =>
                        {
                            context.Result = "456";
                            return default(ValueTask);
                        }));

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a b }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class TestFieldMiddleware
        {
            private FieldDelegate _next;

            public TestFieldMiddleware(FieldDelegate next)
            {
                _next = next ?? throw new ArgumentNullException(nameof(next));
            }

            public ValueTask InvokeAsync(IMiddlewareContext context)
            {
                context.Result = "123456789";
                return _next(context);
            }
        }
    }
}
